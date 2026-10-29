using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SessionDashboard;

internal sealed class UpdatePlan
{
    public string UserId { get; set; } = "";
    public string Target { get; set; } = "";
    public string Candidate { get; set; } = "";
    public string CandidateHash { get; set; } = "";
    public string PreviousHash { get; set; } = "";
    public string Version { get; set; } = "";
    public bool VerificationOnly {get;set;}
    public bool Installer {get;set;}
    public bool MigratePortable {get;set;}
    public string PortableSource {get;set;}="";
    public string PreviousSetupHash {get;set;}="";
    public int ParentPid { get; set; }
    public string State { get; set; } = "Prepared";
    public string Error { get; set; } = "";
}

// Only replaces one vetted executable. User data is backed up, never restored over newer data.
internal static class UpdateTransaction
{
    internal static string Hash(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
    internal static void ValidateBinary(string path, string expectedHash, string? version = null,string productName="Nexa")
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expectedHash), Convert.FromHexString(Hash(path)))) throw new InvalidDataException("Update checksum changed.");
        using var stream = File.OpenRead(path); using var reader = new BinaryReader(stream);
        if (reader.ReadUInt16() != 0x5a4d) throw new InvalidDataException("Not a Windows executable.");
        stream.Position = 0x3c; var pe = reader.ReadInt32(); if (pe < 64 || pe > stream.Length - 6) throw new InvalidDataException("Invalid executable header.");
        stream.Position = pe; if (reader.ReadUInt32() != 0x4550 || reader.ReadUInt16() != 0x8664) throw new InvalidDataException("nexa updates require Windows x64.");
        var info = FileVersionInfo.GetVersionInfo(path);
        if (info.ProductName?.Trim() != productName || version != null && (!Version.TryParse(info.FileVersion?.Trim().Split('+')[0], out var installed) || installed.ToString(3) != version)) throw new InvalidDataException("Unexpected nexa package identity/version.");
    }
    internal static async Task<string> Prepare(StateStore store, string target, string candidate, string sha256, string version)
    {
        ValidateBinary(candidate, sha256, version); ValidateBinary(target, Hash(target));
        var directory = Path.Combine(store.UserPreferencesDirectory, "updates", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var staged = Path.Combine(directory, "candidate.exe"); File.Copy(candidate, staged); ValidateBinary(staged, sha256, version);
        File.Copy(target, Path.Combine(directory, "previous.exe"));
        await BackupData(store, directory);
        var plan = new UpdatePlan { UserId = store.UserId, Target = Path.GetFullPath(target), Candidate = staged, CandidateHash = sha256, PreviousHash = Hash(target), Version = version, ParentPid = Environment.ProcessId };
        var path = Path.Combine(directory, "plan.json"); Save(path, plan); return path;
    }
    internal static Task BackupData(StateStore store, string directory) => Task.Run(() =>
    {
        var backup = Path.Combine(directory, "data-backup"); Directory.CreateDirectory(backup);
        // Snapshot the committed workspace under its existing save gate.
        if (File.Exists(store.FilePath)) File.WriteAllText(Path.Combine(backup, "workspace.json"), store.ReadHistoryText(store.FilePath));
        var root = Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!;
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.TryPop(out var current))
        {
            foreach (var child in Directory.EnumerateDirectories(current))
            {
                var name = Path.GetFileName(child);
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0 || name.Equals("migration-backups", StringComparison.OrdinalIgnoreCase) || name.Equals("updates", StringComparison.OrdinalIgnoreCase) || name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase) || IsWithin(backup, child)) continue;
                pending.Push(child);
            }
            foreach (var file in Directory.EnumerateFiles(current))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) continue;
                var destination = Path.Combine(backup, "files", Path.GetRelativePath(root, file)); Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); using var output = File.Create(destination); source.CopyTo(output);
            }
        }
    });
    internal static void StartHelper(string planPath)
    {
        var directory = Path.GetDirectoryName(planPath)!; var helper = Path.Combine(directory, "Nexa.UpdateHelper.exe");
        File.Copy(Environment.ProcessPath!, helper, true);
        using var process=Process.Start(new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, ArgumentList = { "--apply-update", planPath } }) ?? throw new IOException("The update helper could not be started. nexa will remain open.");
    }
    internal static void Save(string path, UpdatePlan plan) => StateStore.AtomicWrite(path, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }), false);
    internal static bool IsWithin(string root, string path) => Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Path.GetFullPath(path).Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase);
    internal static void RejectLinks(string path)
    {
        for (var current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked update paths are not supported.");
    }
    internal static async Task Apply(string path, Func<string,Task<bool>> health, Action<string>? restart = null, bool waitForParent = true)
    {
        var plan = JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(path)) ?? throw new InvalidDataException("Missing update plan.");
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        if(plan.Installer){await InstallerUpdates.Apply(path,plan,health,restart,waitForParent);return;}
        if (plan.UserId != System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value || !Path.GetFullPath(plan.Candidate).Equals(Path.Combine(directory, "candidate.exe"), StringComparison.OrdinalIgnoreCase) || !Path.IsPathFullyQualified(plan.Target) || !Path.GetFileName(plan.Target).Equals("Nexa.exe", StringComparison.OrdinalIgnoreCase) || IsWithin(directory, plan.Target)) throw new UnauthorizedAccessException("Invalid update ownership or target.");
        RejectLinks(path); RejectLinks(plan.Candidate); RejectLinks(plan.Target);
        // An exclusive file handle survives asynchronous continuations; a thread-owned mutex does not.
        using var gate = new FileStream(Path.Combine(Path.GetDirectoryName(plan.Target)!, ".nexa-update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (plan.State == "Installed") return;
        if (plan.State is not ("Prepared" or "Installing")) throw new InvalidDataException("This update transaction is no longer pending.");
        var replaced = false; var adjacent = Path.Combine(Path.GetDirectoryName(plan.Target)!, ".nexa-update-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            if (waitForParent)
            {
                try { using var parent = Process.GetProcessById(plan.ParentPid); if (!parent.HasExited) await parent.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60)); }
                catch (ArgumentException) { }
            }
            ValidateBinary(plan.Candidate, plan.CandidateHash, plan.Version);
            // Recover a helper interrupted after the atomic replacement, before its final checkpoint.
            replaced = plan.State == "Installing" && Hash(plan.Target).Equals(plan.CandidateHash, StringComparison.OrdinalIgnoreCase);
            if (!replaced)
            {
                ValidateBinary(plan.Target, plan.PreviousHash);
                ValidateBinary(Path.Combine(directory, "previous.exe"), plan.PreviousHash);
                File.Copy(plan.Candidate, adjacent); ValidateBinary(adjacent, plan.CandidateHash, plan.Version);
                plan.State = "Installing"; Save(path, plan);
                File.Replace(adjacent, plan.Target, null); replaced = true;
            }
            if (!await health(plan.Target)) throw new IOException("The installed application failed its startup health check.");
            plan.State = "Installed"; Save(path, plan);
        }
        catch (Exception ex)
        {
            if (replaced)
            {
                try
                {
                    // Never replace a file changed by another installer after this transaction.
                    ValidateBinary(plan.Target, plan.CandidateHash, plan.Version);
                    var previous = Path.Combine(directory, "previous.exe"); ValidateBinary(previous, plan.PreviousHash);
                    File.Copy(previous, adjacent, true); File.Replace(adjacent, plan.Target, null);
                    plan.State = "RolledBack";
                }
                catch (Exception rollback) when (rollback is IOException or UnauthorizedAccessException or InvalidDataException)
                { plan.State = "RecoveryRequired"; plan.Error = ex.Message + " Rollback: " + rollback.Message; Save(path, plan); throw; }
            }
            else plan.State = "Failed";
            plan.Error = ex.Message; Save(path, plan); throw;
        }
        finally { if (File.Exists(adjacent)) File.Delete(adjacent); }
        // Restart errors must not roll back a successfully validated installation.
        restart?.Invoke(plan.Target);
    }
    internal static async Task<bool> Health(string target, string directory)
    {
        using var child = Process.Start(new ProcessStartInfo(target) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, ArgumentList = { "--verify-startup", directory } }) ?? throw new IOException("Health process could not start.");
        try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(45)); }
        catch (TimeoutException) { try { child.Kill(); } catch (InvalidOperationException) { } return false; }
        return child.ExitCode == 0 && File.Exists(Path.Combine(directory,"startup.json"));
    }
}
