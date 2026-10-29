using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SessionDashboard;

internal sealed class NexaDeletionConsent
{
    internal DateTimeOffset? Deadline {get;private set;}
    private bool canceled,consumed;
    internal bool Begin(string text,DateTimeOffset now)
    {if(text!="CONFIRM" || canceled || consumed)return false;Deadline=now.AddSeconds(30);return true;}
    internal void Cancel()=>canceled=true;
    internal bool Authorize(DateTimeOffset now,bool deleteNow=false)
    {if(canceled || consumed || Deadline==null || !deleteNow && now<Deadline)return false;consumed=true;return true;}
}

// All application-owned path operations reject links, including links in parent directories.
internal static class NexaData
{
    internal static readonly object SnapshotGate=new();
    private static readonly HashSet<string> frozenRoots=new(StringComparer.OrdinalIgnoreCase);
    internal static bool Frozen(string path)=>frozenRoots.Any(root=>Path.GetFullPath(path).StartsWith(root,StringComparison.OrdinalIgnoreCase));
    internal static void GuardWrite(string path){if(Frozen(path))throw new IOException("Local data is closed for deletion. Restart nexa to use a new workspace.");}
    internal static string[] Delete(string root,bool isolatedFixture=false)
    {
        root=Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));Regular(root);
        if(!isolatedFixture && !root.Equals(Root,StringComparison.OrdinalIgnoreCase) && !root.Equals(NexaInstallation.FixtureDataRoot,StringComparison.OrdinalIgnoreCase))throw new IOException("Only the canonical nexa data folder can be reset.");
        if(!File.Exists(Inside(root,"state.json")))throw new IOException("No owned workspace was found.");
        using(var doc=JsonDocument.Parse(File.ReadAllText(Inside(root,"state.json"))))
            if(!doc.RootElement.TryGetProperty("UserId",out var owner)||owner.GetString()!=System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value)
                throw new IOException("Workspace ownership could not be verified.");
        if(Environment.ProcessPath is {} executable && Path.GetFullPath(executable).StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Data deletion cannot include the running application.");
        lock(SnapshotGate)
        {
            var files=Files(root).ToArray(); // Validate every entry before any deletion, including links.
            // Commit the restart guard before removing any data. If it cannot be made durable,
            // abort rather than allowing a later launch to silently reimport legacy originals.
            var marker=Inside(root,"Settings/legacy-import-disabled");Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            if(!File.Exists(marker))StateStore.AtomicWrite(marker,"Legacy import disabled after explicit local reset.",false);
            // Installer/update executables can be cached under the data root. They are app files,
            // and are preserved even during an explicit data reset.
            files=files.Where(f=>!f.Equals(marker,StringComparison.OrdinalIgnoreCase) && Path.GetExtension(f).ToLowerInvariant() is not (".exe" or ".dll" or ".msi" or ".sys")).ToArray();
            frozenRoots.Add(root+Path.DirectorySeparatorChar);var errors=new List<string>();
            foreach(var file in files)try{Regular(file);File.Delete(file);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){errors.Add(Path.GetRelativePath(root,file)+": "+ex.Message);}
            return errors.ToArray();
        }
    }
    internal static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Nexa");
    internal static string Hash(string file){using var stream=File.OpenRead(file);return Convert.ToHexString(SHA256.HashData(stream));}
    internal static void Regular(string path)
    {
        for(var item=Path.GetFullPath(path);!string.IsNullOrEmpty(item);item=Path.GetDirectoryName(item))
            if((File.Exists(item)||Directory.Exists(item)) && (File.GetAttributes(item)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Linked paths are not supported: "+item);
    }
    internal static string Inside(string root,string relative)
    {
        var full=Path.GetFullPath(Path.Combine(root,relative));
        if(Path.IsPathRooted(relative) || !full.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
            throw new IOException("Path is outside the application data folder.");
        Regular(full);return full;
    }
    internal static IEnumerable<string> Files(string root)
    {
        Regular(root);if(!Directory.Exists(root))yield break;
        foreach(var item in Directory.EnumerateFileSystemEntries(root).Order(StringComparer.OrdinalIgnoreCase))
        {
            Regular(item);
            if(Directory.Exists(item)){foreach(var file in Files(item))yield return file;}
            else yield return item;
        }
    }
    internal sealed record Entry(string Path,long Size,string SHA256);
    internal sealed record MigrationReport(int Copied,int Conflicts,int Verified,string[] Sources);
    // Copy originals into a verified, restart-safe inventory before adopting them. Never remove legacy data.
    internal static MigrationReport Migrate(string legacy,string documents,string destination)
    {
        Regular(legacy);Regular(documents);Regular(destination);
        if(UpdateTransaction.IsWithin(legacy,destination)||UpdateTransaction.IsWithin(destination,legacy))throw new IOException("Migration roots overlap.");
        Directory.CreateDirectory(destination);
        var sources=new List<(string Source,string Relative)>();
        if(Directory.Exists(legacy))
            foreach(var file in Files(legacy))
            {
                var relative=Path.GetRelativePath(legacy,file);
                if(relative.Split(Path.DirectorySeparatorChar).Any(x=>x.Equals("updates",StringComparison.OrdinalIgnoreCase)||x.Equals("installer-cache",StringComparison.OrdinalIgnoreCase)||x.Equals("migration-backups",StringComparison.OrdinalIgnoreCase)))continue;
                if(Path.GetExtension(file).Equals(".exe",StringComparison.OrdinalIgnoreCase)||Path.GetExtension(file).Equals(".dll",StringComparison.OrdinalIgnoreCase))continue;
                sources.Add((file,relative));
            }
        foreach(var category in new[]{"Spaces","History"})
            if(Directory.Exists(Path.Combine(documents,category)))
                sources.AddRange(Files(Path.Combine(documents,category)).Select(f=>(f,Path.Combine(category,Path.GetRelativePath(Path.Combine(documents,category),f)))));
        var count=0;var conflicts=0;var inventory=new List<Entry>();
        foreach(var (source,relative) in sources)
        {
            var digest=Hash(source);var backup=Inside(destination,Path.Combine("Backups","migration-originals",relative+"."+digest));
            CopyVerified(source,backup,digest);inventory.Add(new(relative,new FileInfo(source).Length,digest));
            var target=Inside(destination,relative);
            if(File.Exists(target))
            {
                if(Hash(target)==digest)continue;
                // An existing .34 file always wins. Retain the other version and report the conflict.
                conflicts++;continue;
            }
            CopyVerified(source,target,digest);count++;
        }
        // Relink structured absolute recording/image paths after all originals are verified.
        foreach(var file in Files(destination).Where(f=>!Path.GetRelativePath(destination,f).StartsWith("Backups"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) && Path.GetExtension(f).Equals(".json",StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            JsonNode? node;try{node=JsonNode.Parse(File.ReadAllText(file));}catch(JsonException){continue;}
            bool Rewrite(JsonNode? value)
            {
                var changed=false;
                if(value is JsonObject obj)foreach(var key in obj.Select(p=>p.Key).ToArray())
                {
                    if(obj[key] is JsonValue v && v.TryGetValue<string>(out var text))
                    {
                        foreach(var pair in new[]{(legacy,destination),(documents,destination)})
                            if(text.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(pair.Item1))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                            {var next=Inside(pair.Item2,Path.GetRelativePath(pair.Item1,text));if(File.Exists(next)){obj[key]=next;changed=true;}break;}
                    }
                    else changed|=Rewrite(obj[key]);
                }
                else if(value is JsonArray list)foreach(var child in list)changed|=Rewrite(child);
                return changed;
            }
            if(Rewrite(node))StateStore.AtomicWrite(file,node!.ToJsonString(),true);
        }
        foreach(var folder in new[]{"Spaces","History","Recordings","Settings","Backups","Ecosystem","Logs"})Directory.CreateDirectory(Inside(destination,folder));
        var report=new MigrationReport(count,conflicts,inventory.Count,new[]{legacy,documents});
        StateStore.AtomicWrite(Inside(destination,"Backups/migration-inventory.json"),JsonSerializer.Serialize(new{Schema=1,Report=report,Files=inventory}),false);
        return report;
    }
    private static void CopyVerified(string source,string destination,string digest)
    {
        Regular(source);Regular(destination);Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if(File.Exists(destination)){if(Hash(destination)!=digest)throw new IOException("Migration backup has changed: "+destination);return;}
        var temp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.Copy(source,temp);if(Hash(temp)!=digest)throw new IOException("Source changed during migration.");File.Move(temp,destination);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    private static bool SecretName(string name)
    {
        var key=name.Replace("_","").Replace("-","").ToLowerInvariant();
        return key.Contains("credential")||key.Contains("protectedkey")||key.Contains("apikey")||key.Contains("accesstoken")||key.Contains("refreshtoken")||key.Contains("password")||key.Contains("clientsecret")||key.Contains("authorization")||key=="secret"||key=="token";
    }
    private static byte[] ExportBytes(string file)
    {
        var bytes=File.ReadAllBytes(file);
        if(!Path.GetFileName(file).Contains(".json",StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(file).StartsWith("state.json",StringComparison.OrdinalIgnoreCase))return bytes;
        // Malformed structured data cannot be exported without proving it contains no secrets.
        var node=JsonNode.Parse(File.ReadAllText(file))??throw new InvalidDataException("Empty structured save: "+file);
        void Scrub(JsonNode? value)
        {
            if(value is JsonObject obj)foreach(var key in obj.Select(p=>p.Key).ToArray()){if(SecretName(key))obj.Remove(key);else Scrub(obj[key]);}
            else if(value is JsonArray list)foreach(var child in list)Scrub(child);
        }
        Scrub(node);return Encoding.UTF8.GetBytes(node.ToJsonString());
    }
    private static string CopyAndHash(Stream input,Stream? output,CancellationToken cancel)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[81920];int count;
        while((count=input.Read(buffer,0,buffer.Length))>0){cancel.ThrowIfCancellationRequested();hash.AppendData(buffer,0,count);output?.Write(buffer,0,count);}
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    internal static int Export(string root,string destination,CancellationToken cancel,IProgress<string>? progress=null)
    {
        Regular(root);Regular(destination);
        if(File.Exists(destination))throw new IOException("Choose a new backup filename; existing files are preserved.");
        var temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        var snapshot=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(destination))!,".nexa-backup-stage-"+Guid.NewGuid().ToString("N"));
        var entries=new List<Entry>();
        try
        {
            lock(SnapshotGate)
            {
                progress?.Report("Inventorying local data…");
                var files=Files(root).Where(f=>!f.Equals(temporary,StringComparison.OrdinalIgnoreCase)&&!f.Equals(destination,StringComparison.OrdinalIgnoreCase))
                    .Where(f=>!Path.GetRelativePath(root,f).Split(Path.DirectorySeparatorChar).Any(part=>SecretName(part)||part.StartsWith(".nexa-backup-stage-",StringComparison.OrdinalIgnoreCase)||part.Equals("Backups",StringComparison.OrdinalIgnoreCase)||part.Equals("updates",StringComparison.OrdinalIgnoreCase)||part.Equals("installer-cache",StringComparison.OrdinalIgnoreCase)||part.Equals("migration-backups",StringComparison.OrdinalIgnoreCase)))
                    .Where(f=>Path.GetExtension(f).ToLowerInvariant() is not (".exe" or ".dll" or ".tmp" or ".zip") && !Path.GetFileName(f).Contains("vault",StringComparison.OrdinalIgnoreCase) && !Path.GetRelativePath(root,f).Equals("nexa-backup-manifest.json",StringComparison.OrdinalIgnoreCase)).ToArray();
                cancel.ThrowIfCancellationRequested();Directory.CreateDirectory(snapshot);
                foreach(var file in files)
                {
                    cancel.ThrowIfCancellationRequested();var relative=Path.GetRelativePath(root,file).Replace('\\','/');Inside(root,relative);
                    progress?.Report("Checkpointing "+relative);var staged=Inside(snapshot,relative);Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
                    string digest;
                    if(Path.GetFileName(file).Contains(".json",StringComparison.OrdinalIgnoreCase))
                    {
                        if(new FileInfo(file).Length>128_000_000)throw new InvalidDataException("Structured save exceeds the supported backup size: "+relative);
                        var bytes=ExportBytes(file);File.WriteAllBytes(staged,bytes);digest=Convert.ToHexString(SHA256.HashData(bytes));
                    }
                    else
                    {
                        using var input=File.OpenRead(file);using var output=new FileStream(staged,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                        digest=CopyAndHash(input,output,cancel);output.Flush(true);
                    }
                    entries.Add(new(relative,new FileInfo(staged).Length,digest));
                }
            }
            // The save lock protects only checkpoint construction. Compression and verification
            // use immutable sanitized copies, so application saves can continue during a large ZIP.
            using(var archive=ZipFile.Open(temporary,ZipArchiveMode.Create))
            {
                foreach(var expected in entries)
                {
                    cancel.ThrowIfCancellationRequested();progress?.Report("Archiving "+expected.Path);
                    using var input=File.OpenRead(Inside(snapshot,expected.Path));using var output=archive.CreateEntry(expected.Path,CompressionLevel.Optimal).Open();
                    if(CopyAndHash(input,output,cancel)!=expected.SHA256)throw new InvalidDataException("Checkpoint changed during export.");
                }
                using var manifest=archive.CreateEntry("nexa-backup-manifest.json").Open();
                JsonSerializer.Serialize(manifest,new{Schema=1,AppVersion=App.VersionLabel,CreatedUtc=DateTimeOffset.UtcNow,Compatibility="nexa 0.0.34 Alpha; manual recovery only",Credentials="Excluded. Reconnect integrations after recovery.",Files=entries});
            }
            cancel.ThrowIfCancellationRequested();progress?.Report("Verifying archive hashes…");
            using(var archive=ZipFile.OpenRead(temporary))foreach(var expected in entries)
            {
                cancel.ThrowIfCancellationRequested();var entry=archive.GetEntry(expected.Path)??throw new InvalidDataException("Missing archived file.");
                using var input=entry.Open();if(entry.Length!=expected.Size || CopyAndHash(input,null,cancel)!=expected.SHA256)throw new InvalidDataException("Backup verification failed.");
            }
            cancel.ThrowIfCancellationRequested();File.Move(temporary,destination);progress?.Report("Backup verified. Integration credentials were excluded; reconnect after recovery.");return entries.Count;
        }
        finally
        {
            if(File.Exists(temporary))File.Delete(temporary);
            if(Directory.Exists(snapshot)){Regular(snapshot);foreach(var file in Files(snapshot).ToArray())Regular(file);Directory.Delete(snapshot,true);}
        }
    }
}
