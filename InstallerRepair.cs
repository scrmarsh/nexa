using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace SessionDashboard;

internal static class InstallerRepair
{
    internal sealed record Integrity(int Schema,string Version,string PayloadSHA256,string SetupSHA256);
    internal static string? Cached(string executable)
    {
        var folder=Path.GetDirectoryName(Path.GetFullPath(executable))!;
        var manifest=Path.Combine(folder,"installer-integrity.json");var setup=Path.Combine(folder,"setup-cache","NexaSetup.exe");
        if(!File.Exists(manifest)||!File.Exists(setup))return null;
        UpdateTransaction.RejectLinks(manifest);UpdateTransaction.RejectLinks(setup);
        var identity=JsonSerializer.Deserialize<Integrity>(File.ReadAllText(manifest))??throw new InvalidDataException("Installer integrity information is missing.");
        if(identity.Schema!=1 || identity.Version!=App.AppVersion)throw new InvalidDataException("The cached installer belongs to a different version.");
        UpdateTransaction.ValidateBinary(setup,identity.SetupSHA256,identity.Version,"nexa installer");return setup;
    }
    internal static async Task<string> Resolve(StateStore store,CancellationToken token=default)
    {
        string? cached=null;
        try{cached=Cached(Environment.ProcessPath??throw new IOException("Application path unavailable."));}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException){/* Fetch a digest-verified published installer when the local cache is unusable. */}
        if(cached!=null)return cached;
        using var client=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};
        var updater=new ExperimentalUpdater(client);var release=await updater.DiscoverAsync(token,"Alpha");
        if(release==null || release.Portable || release.CanonicalVersion!=new Version(App.AppVersion))throw new IOException("No verified published installer for this version is available. Use the official release installer when it is published.");
        var setup=await updater.DownloadAsync(release,Path.Combine(store.UserPreferencesDirectory,"updates","repair"),cancellationToken:token);
        UpdateTransaction.ValidateBinary(setup,release.Sha256,release.Version.ToString(3),"nexa installer");return setup;
    }
}
