using System.IO;
using System.Text.Json;
namespace SessionDashboard;
internal sealed record ArcadeLoadResult(ArcadeWorldSnapshot? World,bool Blocked,bool HasBackup,string? Warning);
internal sealed class ArcadeWorldStore(string filePath)
{
    internal string FilePath {get;}=Path.GetFullPath(filePath);
    private readonly object gate=new();
    internal static readonly JsonSerializerOptions JsonOptions=new() {IgnoreReadOnlyProperties=true,MaxDepth=32};
    private ArcadeWorldSnapshot Read(string path)
    {
        if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Arcade save exceeds the supported size.");
        var data=JsonSerializer.Deserialize<ArcadeWorldSnapshot>(File.ReadAllText(path),JsonOptions)??throw new InvalidDataException("Empty Arcade save.");
        BugManager.ValidateWorld(data);return data;
    }
    private static bool DataError(Exception ex)=>ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or NullReferenceException or NotSupportedException;
    internal ArcadeLoadResult Load()
    {
        lock(gate)
        {
            if(!File.Exists(FilePath) && !File.Exists(FilePath+".bak"))return new(null,false,false,null);
            string? failure=null;
            try {if(File.Exists(FilePath))return new(Read(FilePath),false,File.Exists(FilePath+".bak"),null);}
            catch(Exception ex) when(DataError(ex)) {failure=ex.Message;}
            try
            {
                if(File.Exists(FilePath+".bak"))return new(Read(FilePath+".bak"),true,true,"Arcade save needs recovery. A valid backup is available; restore it to resume. The original file is preserved.");
            }
            catch(Exception ex) when(DataError(ex)) {failure+=" "+ex.Message;}
            return new(null,true,false,"The Arcade world could not be loaded. Its files are preserved. "+failure);
        }
    }
    internal void Save(ArcadeWorldSnapshot snapshot,bool explicitReplacement=false)
    {
        BugManager.ValidateWorld(snapshot);
        lock(gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temporary=FilePath+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {JsonSerializer.Serialize(stream,snapshot,JsonOptions);stream.Flush(true);}
                var checkedSave=Read(temporary);
                if(checkedSave.WorldId!=snapshot.WorldId)throw new InvalidDataException("Arcade save validation failed.");
                if(File.Exists(FilePath))
                {
                    ArcadeWorldSnapshot? previous=null;
                    try {previous=Read(FilePath);}
                    catch(Exception ex) when(DataError(ex))
                    {
                        if(!explicitReplacement)throw new IOException("The existing Arcade save changed or became unreadable. It was preserved.",ex);
                        File.Copy(FilePath,FilePath+".unreadable-"+Guid.NewGuid().ToString("N"));
                    }
                    if(previous!=null)
                    {
                        if(!explicitReplacement && previous.WorldId!=snapshot.WorldId)throw new IOException("Another Arcade world occupies this save. It was preserved.");
                        File.Replace(temporary,FilePath,FilePath+".bak",true);
                    }
                    else File.Move(temporary,FilePath,true);
                }
                else File.Move(temporary,FilePath);
            }
            finally {if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
    internal ArcadeWorldSnapshot RecoverBackup()
    {
        lock(gate)
        {
            var backup=Read(FilePath+".bak");
            // Save may replace the backup, so keep a durable copy of the selected recovery point.
            File.Copy(FilePath+".bak",FilePath+".recovered-"+Guid.NewGuid().ToString("N"));
            Save(backup,true);return backup;
        }
    }
}
