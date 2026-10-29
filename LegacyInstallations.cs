using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
namespace SessionDashboard;

// Shared installer maintenance: inventory first; remove only identified application files.
// User directories are never recursively deleted and candidate executables are never run.
internal static class LegacyInstallations
{
    internal sealed class Candidate
    {
        public string Executable {get;set;}="";
        public string Version {get;set;}="";
        public string Hash {get;set;}="";
        public bool Eligible {get;set;}
        public string Reason {get;set;}="";
        public List<Registration> Registrations {get;set;}=[];
        public string Outcome {get;set;}="Not removed";
    }
    internal sealed record Registration(string Hive,string View,string Key,string Location);
    internal sealed class Inventory
    {
        public string UserId {get;set;}="";
        public string DataRoot {get;set;}="";
        public string InstallRoot {get;set;}="";
        public string Mode {get;set;}="";
        public bool Fixture {get;set;}
        public List<Candidate> Candidates {get;set;}=[];
        public List<string> Issues {get;set;}=[];
        public int Removed=>Candidates.Count(c=>c.Outcome=="Removed and verified");
        public int Absent=>Candidates.Count(c=>c.Outcome=="Already absent");
        public int Failed=>Candidates.Count(c=>c.Eligible && c.Outcome.StartsWith("Failed:"));
    }
    private static readonly HashSet<string> Releases=new(StringComparer.OrdinalIgnoreCase)
    {
        "67081BDD1415A38B5FEFB277FF8996440616963CED058A125F3CAA35FEF18F5C", // public Beta .1.0 application
        "000641ADDE33ED75F3D55EA32703393036A6DA339BFF2AF10E8B7D0142CC08BA", // public .34
        "D836DE09B79E873E232ED6E9FAE67F9E1F73D5D6B2A62EFCCE1AB31C2E75B420",
        "1D08B896653932F0470BF09EEF5464887BC9ABB0C539E8E91AC549B227E0868C",
        "BADBDB24FC0345CC6ED00C27CC873BB92068F27194CA23F25452CBA0F57CD9CD",
        "A4AF062CFBA9AB946568E6F823B0BC8CF06FF68CEB9A95E9B2CC909CA60681F8"
    };
    private static bool RegisteredIdentity(Candidate candidate)
    {
        try
        {
            var folder=Path.GetDirectoryName(candidate.Executable)!;
            var markerPath=Path.Combine(folder,"installation.json");var manifestPath=Path.Combine(folder,"installer-integrity.json");
            RejectLinks(markerPath);RejectLinks(manifestPath);
            var marker=JsonSerializer.Deserialize<NexaInstallation.Marker>(File.ReadAllText(markerPath));
            if(marker?.AppId is not (NexaInstallation.AppId or NexaInstallation.TestAppId) || !candidate.Registrations.Any(r=>r.Key.EndsWith(marker.AppId+"_is1",StringComparison.OrdinalIgnoreCase)))return false;
            using var manifest=JsonDocument.Parse(File.ReadAllText(manifestPath));
            return manifest.RootElement.GetProperty("PayloadSHA256").GetString()?.Equals(candidate.Hash,StringComparison.OrdinalIgnoreCase)==true;
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException){return false;}
    }
    private static string Full(string path)=>Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    private static bool Same(string first,string second)=>Full(first).Equals(Full(second),StringComparison.OrdinalIgnoreCase);
    private static bool Within(string parent,string child)=>Same(parent,child)||UpdateTransaction.IsWithin(Full(parent),Full(child));
    private static void RejectLinks(string path)
    {
        for(var current=Path.GetFullPath(path);current!=null;current=Path.GetDirectoryName(current))
            if((File.Exists(current)||Directory.Exists(current)) && (File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("Linked cleanup paths are not supported.");
    }
    private static string Sid=>System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
    private static void Scope(Inventory plan)
    {
        if(plan.UserId!=Sid || plan.Mode is not ("clean" or "update" or "cleanup"))throw new InvalidDataException("Unexpected cleanup identity or mode.");
        if(Within(plan.DataRoot,plan.InstallRoot)||Within(plan.InstallRoot,plan.DataRoot))throw new InvalidDataException("Application and data folders must be separate.");
        if(plan.Fixture)
        {
            var root=Path.GetDirectoryName(Full(plan.DataRoot))!;
            if(!Same(plan.DataRoot,Path.Combine(root,"data")) || !Same(plan.InstallRoot,Path.Combine(root,"app")) || !File.Exists(Path.Combine(root,".nexa-installer-fixture")) || Within(NexaData.Root,root) || Within(root,NexaData.Root))throw new InvalidDataException("Invalid cleanup fixture.");
        }
        else if(!Same(plan.DataRoot,NexaData.Root))throw new InvalidDataException("Cleanup data must use the canonical Nexa store.");
    }
    private static List<Registration> Registrations(Inventory plan,List<string> issues)
    {
        var result=new List<Registration>();
        foreach(var hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})foreach(var view in new[]{RegistryView.Registry64,RegistryView.Registry32})
        {
            try
            {
                using var registry=RegistryKey.OpenBaseKey(hive,view);
                const string uninstall=@"Software\Microsoft\Windows\CurrentVersion\Uninstall";
                using var parent=registry.OpenSubKey(uninstall);if(parent==null)continue;
                foreach(var name in parent.GetSubKeyNames())
                {
                    if(plan.Fixture && name!=NexaInstallation.TestAppId+"_is1")continue;
                    using var key=parent.OpenSubKey(name);
                    var display=key?.GetValue("DisplayName") as string??"";
                    if(!display.Equals("nexa",StringComparison.OrdinalIgnoreCase) && !display.StartsWith("nexa v",StringComparison.OrdinalIgnoreCase))continue;
                    if(key?.GetValue("InstallLocation") is not string location || !Path.IsPathFullyQualified(location))continue;
                    result.Add(new(hive.ToString(),view.ToString(),uninstall+"\\"+name,Full(location)));
                }
            }
            catch(Exception ex)when(ex is UnauthorizedAccessException or System.Security.SecurityException or IOException){issues.Add("Could not inspect "+hive+" / "+view+": "+ex.Message);}
        }
        return result;
    }
    private static IEnumerable<string> ShortcutTargets(string folder)
    {
        if(!Directory.Exists(folder))yield break;
        // Only links in known shortcut roots, at bounded depth. Never follow filesystem links.
        var pending=new Stack<(string,int)>();pending.Push((folder,0));
        while(pending.TryPop(out var entry))
        {
            if((File.GetAttributes(entry.Item1)&FileAttributes.ReparsePoint)!=0)continue;
            foreach(var link in Directory.GetFiles(entry.Item1,"*.lnk"))
            {
                string? target=null;
                try{var shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);dynamic shortcut=((dynamic)shell!).CreateShortcut(link);target=(string)shortcut.TargetPath;System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell!);}catch(System.Runtime.InteropServices.COMException){}
                if(target!=null && Path.IsPathFullyQualified(target) && Path.GetFileName(target).StartsWith("nexa",StringComparison.OrdinalIgnoreCase))yield return target;
            }
            if(entry.Item2<2)foreach(var child in Directory.GetDirectories(entry.Item1))pending.Push((child,entry.Item2+1));
        }
    }
    internal static Inventory Scan(string data,string install,string mode,IEnumerable<string> roots,bool fixture=false,Action<int,int,string>? progress=null,CancellationToken cancel=default)
    {
        var plan=new Inventory{UserId=Sid,DataRoot=Full(data),InstallRoot=Full(install),Mode=mode,Fixture=fixture};Scope(plan);
        var registrations=Registrations(plan,plan.Issues);
        var sources=roots.Concat(registrations.Select(r=>r.Location)).Append(install).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if(!fixture)foreach(var folder in new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)})
            try{sources.AddRange(ShortcutTargets(folder));}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){plan.Issues.Add("Could not inspect shortcuts: "+ex.Message);}
        var files=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending=new Queue<(string Path,int Depth)>();foreach(var source in sources)pending.Enqueue((source,0));
        var done=0;
        while(pending.TryDequeue(out var item))
        {
            cancel.ThrowIfCancellationRequested();var path=Full(item.Path);
            if(!visited.Add(path)||Within(data,path))continue;
            if(fixture && !Within(Path.GetDirectoryName(data)!,path))throw new InvalidDataException("Cleanup fixture reference escapes its scope.");
            progress?.Invoke(++done,done+pending.Count,"Scanning "+path);
            try
            {
                if(!File.Exists(path)&&!Directory.Exists(path))continue;
                RejectLinks(path);
                if(File.Exists(path)){if(Path.GetExtension(path).Equals(".exe",StringComparison.OrdinalIgnoreCase))files.Add(path);continue;}
                if(Directory.Exists(Path.Combine(path,".git")))continue;
                foreach(var file in Directory.GetFiles(path,"*.exe"))if(Path.GetFileName(file).StartsWith("nexa",StringComparison.OrdinalIgnoreCase))files.Add(file);
                if(item.Depth<3)foreach(var child in Directory.GetDirectories(path))
                    if(Path.GetFileName(child) is not (".git" or "node_modules" or "updates" or "migration-backups" or "installer-cleanup-backups" or "setup-cache"))pending.Enqueue((child,item.Depth+1));
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){plan.Issues.Add("Could not inspect "+path+": "+ex.Message);}
        }
        var index=0;
        foreach(var file in files.Order(StringComparer.OrdinalIgnoreCase))
        {
            cancel.ThrowIfCancellationRequested();progress?.Invoke(++index,files.Count,"Identifying "+file);
            try
            {
                RejectLinks(file);var info=FileVersionInfo.GetVersionInfo(file);
                if(info.ProductName!="Nexa")continue;
                var entry=new Candidate{Executable=file,Version=info.FileVersion??"Unknown",Hash=UpdateTransaction.Hash(file),Registrations=registrations.Where(r=>Same(r.Location,Path.GetDirectoryName(file)!)).ToList()};
                var version=Version.TryParse(info.FileVersion,out var parsed)?NexaReleaseIdentity.Parse(parsed.ToString(3)+(parsed.Minor==1?"-beta":"-alpha"),true)?.Canonical:null;
                var old=version!=null && version<new Version(App.AppVersion);
                var active=mode!="clean" && (Same(Path.GetDirectoryName(file)!,install) || entry.Registrations.Any(r=>r.Key.EndsWith(NexaInstallation.AppId+"_is1",StringComparison.OrdinalIgnoreCase) || r.Key.EndsWith(NexaInstallation.TestAppId+"_is1",StringComparison.OrdinalIgnoreCase)));
                entry.Eligible=old && !active && (Releases.Contains(entry.Hash)||RegisteredIdentity(entry));
                entry.Reason=active?"Current active installation — preserved":!old?"Current/newer or unknown version — preserved":!entry.Eligible && old?"Identity found; package not independently trusted — preserved for manual review":"Verified obsolete Nexa release — eligible";
                plan.Candidates.Add(entry);
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){plan.Issues.Add("Could not identify "+file+": "+ex.Message);}
        }
        return plan;
    }
    internal static void Save(string path,Inventory plan)=>StateStore.AtomicWrite(path,JsonSerializer.Serialize(plan,new JsonSerializerOptions{WriteIndented=true}),false);
    internal static string Report(Inventory plan)=>$"Installations found: {plan.Candidates.Count}\nRemoved: {plan.Removed}\nAlready absent: {plan.Absent}\nRemoval failures: {plan.Failed}\n"+string.Join("\n",plan.Candidates.Select(c=>$"{c.Version} — {c.Executable}\n{c.Reason}; {c.Outcome}"))+"\n"+string.Join("\n",plan.Issues);
    internal static Inventory Remove(Inventory plan,Action<int,int,string>? progress=null,CancellationToken cancel=default)
    {
        Scope(plan);var eligible=plan.Candidates.Where(c=>c.Eligible).ToArray();var index=0;
        foreach(var candidate in eligible)
        {
            cancel.ThrowIfCancellationRequested();progress?.Invoke(++index,eligible.Length,"Removing obsolete files: "+candidate.Executable);
            try
            {
                var path=Full(candidate.Executable);RejectLinks(path);
                if(plan.Fixture && !Within(Path.GetDirectoryName(plan.DataRoot)!,path))throw new InvalidDataException("Fixture target escapes its scope.");
                if(Within(plan.DataRoot,path) || Same(path,Environment.ProcessPath!) || plan.Mode!="clean" && (Same(Path.GetDirectoryName(path)!,plan.InstallRoot) || candidate.Registrations.Any(r=>r.Key.EndsWith(NexaInstallation.AppId+"_is1",StringComparison.OrdinalIgnoreCase) || r.Key.EndsWith(NexaInstallation.TestAppId+"_is1",StringComparison.OrdinalIgnoreCase))))throw new IOException("Active application preserved.");
                if(!File.Exists(path)){if(Releases.Contains(candidate.Hash)||RegisteredIdentity(candidate))CleanReferences(plan,candidate);candidate.Outcome="Already absent";continue;}
                if((!Releases.Contains(candidate.Hash)&&!RegisteredIdentity(candidate))||UpdateTransaction.Hash(path)!=candidate.Hash)throw new IOException("Application changed after scanning. Scan again.");
                var backup=Path.Combine(plan.DataRoot,"installer-cleanup-backups",candidate.Hash,Path.GetFileName(path));
                if(!File.Exists(backup))InstallerMaintenance.CopyVerified(path,backup);
                if(UpdateTransaction.Hash(backup)!=candidate.Hash)throw new IOException("Recovery copy failed verification.");
                // Opening with FileShare.None detects running/locked binaries before deleting anything.
                using(var locked=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){}
                File.Delete(path);if(File.Exists(path))throw new IOException("Application file still exists.");
                candidate.Outcome="Removed and verified";
                candidate.Reason += " — adjacent content retained at "+Path.GetDirectoryName(path);
                progress?.Invoke(index,eligible.Length,"Cleaning shortcuts and uninstall references");
                CleanReferences(plan,candidate);
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException or System.Runtime.InteropServices.COMException){candidate.Outcome="Failed: "+ex.Message;}
        }
        return plan;
    }
    private static void CleanReferences(Inventory plan,Candidate candidate)
    {
        var directory=Path.GetDirectoryName(candidate.Executable)!;
        var markerPath=Path.Combine(directory,"installation.json");
        if(candidate.Registrations.Count>0 && File.Exists(markerPath))
        {
            var marker=JsonSerializer.Deserialize<NexaInstallation.Marker>(File.ReadAllText(markerPath));
            if(marker?.AppId is NexaInstallation.AppId or NexaInstallation.TestAppId)
            {
                var disposable=new List<string>{markerPath,Path.Combine(directory,"installer-integrity.json")};
                foreach(var uninstaller in Directory.GetFiles(directory,"unins*.exe"))
                    if(FileVersionInfo.GetVersionInfo(uninstaller).FileDescription?.Contains("Uninstall",StringComparison.OrdinalIgnoreCase)==true){disposable.Add(uninstaller);disposable.Add(Path.ChangeExtension(uninstaller,".dat"));}
                var cache=Path.Combine(directory,"setup-cache","NexaSetup.exe");
                if(File.Exists(cache) && FileVersionInfo.GetVersionInfo(cache).ProductName?.Equals("nexa installer",StringComparison.OrdinalIgnoreCase)==true)disposable.Add(cache);
                foreach(var file in disposable.Where(File.Exists))
                {
                    RejectLinks(file);
                    var recovery=Path.Combine(plan.DataRoot,"installer-cleanup-backups",candidate.Hash,"installation-files",Path.GetRelativePath(directory,file));
                    if(!File.Exists(recovery))InstallerMaintenance.CopyVerified(file,recovery);
                    if(UpdateTransaction.Hash(file)!=UpdateTransaction.Hash(recovery))throw new IOException("Installation-file recovery verification failed.");
                    File.Delete(file);if(File.Exists(file))throw new IOException("Installation file could not be removed: "+file);
                }
            }
        }
        foreach(var registration in candidate.Registrations)
        {
            if(!registration.Key.StartsWith(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\",StringComparison.OrdinalIgnoreCase) || registration.Key.Substring(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\".Length).Contains('\\') || !Same(registration.Location,directory) || plan.Fixture && !registration.Key.EndsWith(NexaInstallation.TestAppId+"_is1",StringComparison.OrdinalIgnoreCase))throw new IOException("Unexpected uninstall record.");
            using var hive=RegistryKey.OpenBaseKey(Enum.Parse<RegistryHive>(registration.Hive),Enum.Parse<RegistryView>(registration.View));
            using(var key=hive.OpenSubKey(registration.Key))
            {
                if(key==null)continue;
                if(key.GetValue("DisplayName") is not string display || (!display.Equals("nexa",StringComparison.OrdinalIgnoreCase) && !display.StartsWith("nexa v",StringComparison.OrdinalIgnoreCase)) || key.GetValue("InstallLocation") is not string location || !Same(location,directory))throw new IOException("Uninstall record changed during cleanup.");
            }
            hive.DeleteSubKeyTree(registration.Key,false);
            using var remaining=hive.OpenSubKey(registration.Key);if(remaining!=null)throw new IOException("Uninstall reference could not be removed.");
        }
        var roots=plan.Fixture?new[]{Path.Combine(Path.GetDirectoryName(plan.DataRoot)!,"desktop"),Path.Combine(Path.GetDirectoryName(plan.DataRoot)!,"start-menu")}:new[]{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),Environment.GetFolderPath(Environment.SpecialFolder.StartMenu)};
        foreach(var root in roots)RemoveShortcut(root,candidate.Executable,0);
        // Preserve every other adjacent file, including profiles, recordings and unknown installation files.
        // The exact obsolete executable has a verified recovery copy; no recursive directory removal occurs.
    }
    private static void RemoveShortcut(string folder,string executable,int depth)
    {
        if(!Directory.Exists(folder))return;RejectLinks(folder);
        foreach(var file in Directory.GetFiles(folder,"*.lnk"))
        {
            RejectLinks(file);var shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);dynamic link=((dynamic)shell!).CreateShortcut(file);
            string target=(string)link.TargetPath;System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell!);
            if(Path.IsPathFullyQualified(target) && Same(target,executable)){File.Delete(file);if(File.Exists(file))throw new IOException("Shortcut could not be removed: "+file);}
        }
        if(depth<2)foreach(var child in Directory.GetDirectories(folder))RemoveShortcut(child,executable,depth+1);
    }
    internal static int Run(string[] args)
    {
        try
        {
            void Progress(int current,int total,string text)=>Console.WriteLine($"PROGRESS|{current}|{Math.Max(1,total)}|{text}");
            if(args[0]=="--installer-legacy-scan" && args.Length>=7)
            {
                var plan=Scan(args[2],args[3],args[4],args.Skip(6),args[5]=="fixture",Progress);Save(args[1],plan);File.WriteAllText(args[1]+".report.txt",Report(plan));return 0;
            }
            if(args[0]=="--installer-legacy-remove" && args.Length==2)
            {
                var plan=JsonSerializer.Deserialize<Inventory>(File.ReadAllText(args[1]))??throw new InvalidDataException("Missing cleanup inventory.");
                Remove(plan,Progress);Save(args[1],plan);File.WriteAllText(args[1]+".report.txt",Report(plan));return plan.Failed>0 || plan.Mode=="clean" && plan.Issues.Count>0?1:0;
            }
            return 2;
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
        {if(args.Length>1)File.WriteAllText(args[1]+".error",ex.Message);return 1;}
    }
}
