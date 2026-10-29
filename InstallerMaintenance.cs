using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SessionDashboard;

// Installer-only maintenance. Every write is explicit and scoped; discovery never executes candidates.
internal static class InstallerMaintenance
{
    internal sealed record OldCopy(string Path,string Hash,string Version,bool VerifiedRelease);
    internal sealed class Plan
    {
        public string UserId {get;set;}="";
        public string DataRoot {get;set;}="";
        public string InstallRoot {get;set;}="";
        public string Backup {get;set;}="";
        public List<OldCopy> Copies {get;set;}=[];
        public List<string> Messages {get;set;}=[];
    }
    private static readonly HashSet<string> known=new(StringComparer.OrdinalIgnoreCase)
    {"D836DE09B79E873E232ED6E9FAE67F9E1F73D5D6B2A62EFCCE1AB31C2E75B420","1D08B896653932F0470BF09EEF5464887BC9ABB0C539E8E91AC549B227E0868C","BADBDB24FC0345CC6ED00C27CC873BB92068F27194CA23F25452CBA0F57CD9CD","A4AF062CFBA9AB946568E6F823B0BC8CF06FF68CEB9A95E9B2CC909CA60681F8"};
    internal static IEnumerable<OldCopy> Discover(IEnumerable<string> roots,string install,CancellationToken cancellation=default)
    {
        var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var root in roots)
        {
            var pending=new Stack<string>();if(Directory.Exists(root))pending.Push(Path.GetFullPath(root));
            while(pending.TryPop(out var folder))
            {
                cancellation.ThrowIfCancellationRequested();
                if(!visited.Add(folder) || UpdateTransaction.IsWithin(install,folder))continue;
                var matches=new List<OldCopy>();
                try
                {
                    if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0 || Directory.Exists(Path.Combine(folder,".git")))continue;
                    foreach(var file in Directory.EnumerateFiles(folder,"*.exe"))
                    {
                        cancellation.ThrowIfCancellationRequested();var leaf=Path.GetFileName(file);
                        if(!leaf.StartsWith("nexa",StringComparison.OrdinalIgnoreCase))continue;
                        OldCopy? found=null;
                        try
                        {
                            if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)continue;
                            var info=FileVersionInfo.GetVersionInfo(file);
                            if(info.ProductName!="Nexa" || !Version.TryParse(info.FileVersion,out var version))continue;
                            if(NexaReleaseIdentity.Canonicalize(version,"Alpha")>=new Version(App.AppVersion))continue;
                            var hash=UpdateTransaction.Hash(file);found=new(file,hash,info.FileVersion!,known.Contains(hash));
                        }
                        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){}
                        if(found!=null)matches.Add(found);
                    }
                    foreach(var child in Directory.EnumerateDirectories(folder))
                    {
                        var name=Path.GetFileName(child);
                        if(name.Equals("Windows",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Program Files",StringComparison.OrdinalIgnoreCase)||name is "$Recycle.Bin" or "System Volume Information" or ".git" or "node_modules" or "updates" or "migration-backups")continue;
                        pending.Push(child);
                    }
                }
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
                foreach(var match in matches)yield return match;
            }
        }
    }
    internal static void CopyVerified(string source,string destination)
    {
        if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Linked migration files are not supported.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);File.Copy(source,destination,false);
        if(UpdateTransaction.Hash(source)!=UpdateTransaction.Hash(destination))throw new IOException("Migration backup checksum mismatch.");
    }
    internal static async Task<Plan> Prepare(string planPath,string dataRoot,string installRoot,IEnumerable<string> roots)
    {
        dataRoot=Path.GetFullPath(dataRoot);installRoot=Path.GetFullPath(installRoot);
        using(var running=new System.Threading.Mutex(false,"Local\\Nexa.SessionDashboard."+System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value))
        {if(!running.WaitOne(0))throw new IOException("Close nexa before installing so all saves can be backed up safely.");running.ReleaseMutex();}
        if(UpdateTransaction.IsWithin(installRoot,dataRoot)||UpdateTransaction.IsWithin(dataRoot,installRoot))throw new InvalidDataException("Application and data folders must be separate.");
        var plan=new Plan{UserId=System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value,DataRoot=dataRoot,InstallRoot=installRoot,Backup=Path.Combine(dataRoot,"migration-backups",DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"))};
        Directory.CreateDirectory(plan.Backup);
        var store=new StateStore(Path.Combine(dataRoot,"state.json"));
        await UpdateTransaction.BackupData(store,plan.Backup);
        var search=roots.ToArray();plan.Copies=Discover(search.Where(Directory.Exists),installRoot).ToList();
        foreach(var source in search.Where(File.Exists))
        {
            var info=FileVersionInfo.GetVersionInfo(source);var hash=UpdateTransaction.Hash(source);
            if(info.ProductName=="Nexa" && (known.Contains(hash)||(info.FileDescription?.Equals("nexa installer migration bridge",StringComparison.OrdinalIgnoreCase)==true || info.FileDescription?.Equals("nexa",StringComparison.OrdinalIgnoreCase)==true) && Version.TryParse(info.FileVersion,out var sourceVersion) && NexaReleaseIdentity.Canonicalize(sourceVersion,"Alpha")<=new Version(App.AppVersion)))
            {UpdateTransaction.RejectLinks(source);plan.Copies.Add(new(Path.GetFullPath(source),hash,info.FileVersion??"",true));}
        }
        plan.Copies=plan.Copies.DistinctBy(c=>c.Path,StringComparer.OrdinalIgnoreCase).ToList();
        foreach(var copy in plan.Copies.Where(c=>c.VerifiedRelease))
        {
            var backup=Path.Combine(plan.Backup,"old-executables",copy.Hash+".exe");if(!File.Exists(backup))CopyVerified(copy.Path,backup);
        }
        try{ImportAdjacentData(plan,store);}
        catch
        {
            // Restore authoritative files changed by a partially completed import.
            // Never remove source saves or the verified recovery backup.
            var original=Path.Combine(plan.Backup,"data-backup","files");
            foreach(var name in new[]{"state.json","state.json.bak","state.json.credentials","preferences.json","interaction-preferences.json","update-preferences.json"})
            {
                var backup=Path.Combine(original,name);var destination=Path.Combine(dataRoot,name);
                if(File.Exists(backup))StateStore.AtomicWrite(destination,File.ReadAllText(backup),false);
                else if(File.Exists(destination) && name.StartsWith("state.json",StringComparison.Ordinal))File.Delete(destination);
            }
            throw;
        }
        plan.Messages.Add(File.Exists(store.FilePath)?"Existing private saves are retained in place.":"No existing shared save was found. A new private store will be created on first launch.");
        foreach(var copy in plan.Copies.Where(c=>!c.VerifiedRelease))plan.Messages.Add("Preserved unverified copy: "+copy.Path);
        StateStore.AtomicWrite(planPath,JsonSerializer.Serialize(plan,new JsonSerializerOptions{WriteIndented=true}),false);return plan;
    }
    private static void CopyTree(string source,string destination)
    {
        UpdateTransaction.RejectLinks(source);Directory.CreateDirectory(destination);
        foreach(var file in Directory.EnumerateFiles(source)){var target=Path.Combine(destination,Path.GetFileName(file));if(!File.Exists(target))CopyVerified(file,target);else if(UpdateTransaction.Hash(file)!=UpdateTransaction.Hash(target))throw new IOException("Migration backup collision.");}
        foreach(var child in Directory.EnumerateDirectories(source))CopyTree(child,Path.Combine(destination,Path.GetFileName(child)));
    }
    private static void ImportAdjacentData(Plan plan,StateStore target)
    {
        var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var copy in plan.Copies.Where(c=>c.VerifiedRelease))
        foreach(var source in new[]{Path.Combine(Path.GetDirectoryName(copy.Path)!,"state.json"),Path.Combine(Path.GetDirectoryName(copy.Path)!,"data","state.json"),Path.Combine(Path.GetDirectoryName(copy.Path)!,"SessionDashboard","state.json")})
        {
            if(!File.Exists(source)||Path.GetFullPath(source).Equals(Path.GetFullPath(target.FilePath),StringComparison.OrdinalIgnoreCase)||!seen.Add(Path.GetFullPath(source)))continue;
            UpdateTransaction.RejectLinks(source);var raw=File.ReadAllText(source);
            using var parsed=JsonDocument.Parse(raw);
            if(!parsed.RootElement.TryGetProperty("Dashboards",out _))
            {plan.Messages.Add("Preserved adjacent legacy data for manual recovery: "+source);continue;}
            var workspace=HistoryArchive.LoadWorkspace(raw,target.UserId,out _);Continuity.ValidateWorkspace(workspace,target.UserId);
            var hash=UpdateTransaction.Hash(source);var sourceRoot=Path.GetDirectoryName(source)!;
            var backup=Path.Combine(plan.Backup,"portable-data",hash);Directory.CreateDirectory(backup);
            if(!File.Exists(Path.Combine(backup,"state.json")))CopyVerified(source,Path.Combine(backup,"state.json"));
            var profile=new StateStore(source).UserPreferencesDirectory;
            foreach(var folder in new[]{"Recordings","history","ecosystem"})
                if(Directory.Exists(Path.Combine(sourceRoot,folder)))CopyTree(Path.Combine(sourceRoot,folder),Path.Combine(backup,folder));
            if(Directory.Exists(profile))CopyTree(profile,Path.Combine(backup,"profiles",Path.GetFileName(profile)));
            foreach(var name in new[]{"preferences.json","interaction-preferences.json","update-preferences.json","state.json.credentials"})
                if(File.Exists(Path.Combine(sourceRoot,name)))CopyVerified(Path.Combine(sourceRoot,name),Path.Combine(backup,name));
            var retained=Path.Combine(plan.DataRoot,"migrated-portables",hash);
            if(!Directory.Exists(retained))CopyTree(backup,retained);
            var node=System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(workspace,Continuity.Json))!;
            void Paths(System.Text.Json.Nodes.JsonNode value)
            {
                if(value is System.Text.Json.Nodes.JsonObject obj)
                    foreach(var pair in obj.ToArray())
                    {
                        if(pair.Value is System.Text.Json.Nodes.JsonValue text && pair.Key is "LocalRecordingPath" or "AudioPath" or "ImagePath" && text.TryGetValue<string>(out var path) && Path.IsPathFullyQualified(path) && UpdateTransaction.IsWithin(sourceRoot,path) && File.Exists(path))
                        {
                            var relative=Path.GetRelativePath(sourceRoot,path);var saved=Path.Combine(retained,relative);
                            if(!File.Exists(saved))CopyVerified(path,saved);obj[pair.Key]=saved;
                        }
                        else if(pair.Value!=null)Paths(pair.Value);
                    }
                else if(value is System.Text.Json.Nodes.JsonArray array)foreach(var child in array)if(child!=null)Paths(child);
            }
            Paths(node);workspace=node.Deserialize<WorkspaceDocument>(Continuity.Json)!;
            var sourceState=new StateStore(source).Load();
            var exists=File.Exists(target.FilePath);var current=exists?target.Load():Continuity.Activate(workspace,target.UserId,sourceState.HedyRegion,sourceState.HedyProtectedKey);
            if(exists && current.Workspace==null)throw new InvalidDataException("Existing saves need recovery before adjacent portable data can be merged.");
            if(exists)
            {
                var destination=current.Workspace!;
                foreach(var dashboard in workspace.Dashboards)
                {
                    var existing=destination.Dashboards.FirstOrDefault(d=>d.DashboardId==dashboard.DashboardId);
                    if(existing!=null)
                    {
                        if(JsonSerializer.Serialize(existing,Continuity.Json)==JsonSerializer.Serialize(dashboard,Continuity.Json))continue;
                        dashboard.DashboardId=new Guid(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hash+dashboard.DashboardId))[..16]);
                        if(destination.Dashboards.Any(d=>d.DashboardId==dashboard.DashboardId))continue;
                        dashboard.Name+=" (portable import)";
                        foreach(var session in dashboard.History.Prepend(dashboard.Current))session.DashboardId=dashboard.DashboardId;
                    }
                    destination.Dashboards.Add(dashboard);
                }
                destination.AllTimeSquashes=Math.Max(destination.AllTimeSquashes,workspace.AllTimeSquashes);
                destination.AllTimeProductivityScore=Math.Max(destination.AllTimeProductivityScore,workspace.AllTimeProductivityScore);
                foreach(var species in workspace.AllTimeSquashesBySpecies)destination.AllTimeSquashesBySpecies[species.Key]=Math.Max(destination.AllTimeSquashesBySpecies.GetValueOrDefault(species.Key),species.Value);
            }
            // Existing preferences win; source originals are retained for recovery.
            foreach(var name in new[]{"preferences.json","interaction-preferences.json","update-preferences.json"})
            {
                var previous=Path.Combine(backup,name);var destination=Path.Combine(plan.DataRoot,name);
                if(File.Exists(previous)&&!File.Exists(destination))CopyVerified(previous,destination);
            }
            var previousProfile=Path.Combine(backup,"profiles",Path.GetFileName(profile));
            if(Directory.Exists(previousProfile))
                foreach(var file in Directory.EnumerateFiles(previousProfile,"*",SearchOption.AllDirectories))
                {var destination=Path.Combine(target.UserPreferencesDirectory,Path.GetRelativePath(previousProfile,file));if(!File.Exists(destination))CopyVerified(file,destination);}
            Continuity.ValidateWorkspace(current.Workspace!,target.UserId);target.Save(current);
            plan.Messages.Add("Backed up and imported adjacent portable Spaces/history: "+source+". Originals retained; conflicting Spaces kept as separate imports.");
        }
    }
    internal static async Task Complete(string planPath,bool removeCopies,Func<string,bool>? health=null)
    {
        var plan=JsonSerializer.Deserialize<Plan>(File.ReadAllText(planPath))??throw new InvalidDataException("Missing migration plan.");
        if(plan.UserId!=System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value)throw new UnauthorizedAccessException("Migration belongs to another Windows account.");
        var installed=Path.Combine(plan.InstallRoot,"Nexa.exe");
        if(!File.Exists(installed))throw new IOException("Installed application is missing; old copies were preserved.");
        UpdateTransaction.ValidateBinary(installed,UpdateTransaction.Hash(installed),App.AppVersion);
        if(!(health?.Invoke(installed)??await UpdateTransaction.Health(installed,Path.Combine(plan.Backup,"installed-health"))))throw new IOException("Installed startup failed; old copies were preserved. Rerun Setup or restore the verified backup.");
        foreach(var copy in plan.Copies)
        {
            if(!removeCopies || !copy.VerifiedRelease || !File.Exists(copy.Path))continue;
            try
            {
                if(UpdateTransaction.IsWithin(plan.InstallRoot,copy.Path)||UpdateTransaction.IsWithin(plan.DataRoot,copy.Path))continue;
                var backup=Path.Combine(plan.Backup,"old-executables",copy.Hash+".exe");
                if(UpdateTransaction.Hash(copy.Path)!=copy.Hash || UpdateTransaction.Hash(backup)!=copy.Hash)throw new IOException("Copy changed after discovery; preserved.");
                File.Delete(copy.Path);plan.Messages.Add("Backed up and removed old executable: "+copy.Path);
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){plan.Messages.Add("Preserved old copy: "+copy.Path+" — "+ex.Message);}
        }
        StateStore.AtomicWrite(planPath,JsonSerializer.Serialize(plan,new JsonSerializerOptions{WriteIndented=true}),false);
    }
    internal static async Task<int> Run(string[] args)
    {
        try
        {
            if(args[0]=="--installer-prepare" && args.Length>=5)
            {
                var roots=args.Skip(4).ToArray();await Prepare(args[1],args[2],args[3],roots);return 0;
            }
            if(args[0]=="--installer-complete" && args.Length==3){await Complete(args[1],args[2]=="remove");return 0;}
            return 2;
        }
        catch(Exception ex)when(HistoryArchive.Recoverable(ex))
        {if(args.Length>1)try{File.WriteAllText(args[1]+".error",ex.Message);}catch(IOException){}return 1;}
    }
}
