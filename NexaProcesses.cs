using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace SessionDashboard;
internal static class NexaProcesses
{
    internal sealed record Running(int Pid,string Path,DateTime StartedUtc);
    internal static bool VerifiedApplication(string path)
    {
        try
        {
            UpdateTransaction.RejectLinks(path);var info=FileVersionInfo.GetVersionInfo(path);
            if(!string.Equals(info.ProductName,"Nexa",StringComparison.OrdinalIgnoreCase))return false;
            var hash=UpdateTransaction.Hash(path);
            if(LegacyInstallations.ApprovedHash(hash)||Environment.ProcessPath is {} helper && UpdateTransaction.Hash(helper)==hash)return true;
            var folder=System.IO.Path.GetDirectoryName(path)!;
            var marker=JsonSerializer.Deserialize<NexaInstallation.Marker>(File.ReadAllText(System.IO.Path.Combine(folder,"installation.json")));
            var manifest=JsonSerializer.Deserialize<InstallerRepair.Integrity>(File.ReadAllText(System.IO.Path.Combine(folder,"installer-integrity.json")));
            return NexaInstallation.IsInstalled(path) && marker?.AppId is NexaInstallation.AppId or NexaInstallation.TestAppId && manifest?.Schema==1 && string.Equals(manifest.PayloadSHA256,hash,StringComparison.OrdinalIgnoreCase);
        }
        catch(Exception ex)when(HistoryArchive.Recoverable(ex)||ex is System.ComponentModel.Win32Exception){return false;}
    }
    internal static List<Running> Find(IEnumerable<string>? paths=null)
    {
        var allowed=paths?.Select(System.IO.Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);var found=new List<Running>();
        foreach(var process in Process.GetProcesses())using(process)
        {
            try{if(process.Id==Environment.ProcessId || process.MainWindowHandle==IntPtr.Zero)continue;
                var path=process.MainModule?.FileName;if(path==null || allowed!=null&&!allowed.Contains(path)||!VerifiedApplication(path))continue;
                found.Add(new(process.Id,path,process.StartTime.ToUniversalTime()));}
            catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException){/* Inaccessible processes are never treated as verified Nexa. */}
        }
        return found;
    }
    internal static bool Close(IEnumerable<Running> targets,TimeSpan timeout)
    {
        foreach(var target in targets)
        {
            try{using var process=Process.GetProcessById(target.Pid);if(process.StartTime.ToUniversalTime()!=target.StartedUtc || !string.Equals(process.MainModule?.FileName,target.Path,StringComparison.OrdinalIgnoreCase)||!VerifiedApplication(target.Path))return false;
                if(!process.CloseMainWindow() || !process.WaitForExit((int)timeout.TotalMilliseconds))return false;}
            catch(ArgumentException){/* Already exited. */}
            catch(Exception ex)when(ex is InvalidOperationException or System.ComponentModel.Win32Exception){return false;}
        }
        return true;
    }
    internal static int Run(string[] args)
    {
        try
        {
            if(args[0]=="--installer-close-apps")
            {
                var targets=JsonSerializer.Deserialize<List<Running>>(File.ReadAllText(args[1]))??[];
                return Close(targets,TimeSpan.FromSeconds(15))?0:1;
            }
            IEnumerable<string>? paths=null;
            if(args.Length>2 && args[2]!="workspace")
            {
                var inventory=JsonSerializer.Deserialize<LegacyInstallations.Inventory>(File.ReadAllText(args[2]))??throw new IOException("Missing cleanup inventory.");
                paths=inventory.Candidates.Where(c=>c.Eligible).Select(c=>c.Executable);
            }
            var found=Find(paths);File.WriteAllText(args[1],JsonSerializer.Serialize(found));File.WriteAllText(args[1]+".txt",string.Join("\r\n",found.Select(p=>p.Path)));
            return found.Count==0?0:3;
        }
        catch(Exception ex)when(HistoryArchive.Recoverable(ex)){File.WriteAllText(args[1]+".txt","Could not inspect or close the requested application: "+ex.Message);return 1;}
    }
}
