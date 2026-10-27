using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
namespace SessionDashboard;
internal sealed record AevoriaPreferences(TerrainQuality Quality=TerrainQuality.Balanced,int FrameRate=60,double ZoomSensitivity=3,bool ShowMinimap=true);
// Transitional shared-runtime adapter. Contains no dashboard/window construction.
internal sealed class AevoriaLibrary
{
    internal const int MaximumWorlds=10;
    internal string Root {get;}
    internal StateStore Store {get;}
    internal Guid? Playing {get;set;}
    internal List<string> Warnings {get;}=[];
    private readonly SemaphoreSlim changes=new(1,1);
    internal static string DefaultRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Aevoria");
    internal AevoriaLibrary(string? root=null){Root=Path.GetFullPath(root??DefaultRoot);Store=new StateStore(Path.Combine(Root,"state.json"),gameContext:true);}
    internal string WorldPath(Guid id)=>Path.Combine(LivingWorlds.Root(Store),id.ToString("N"));
    internal LivingWorldInfo[] Worlds()
    {
        Warnings.Clear();var path=LivingWorlds.Root(Store);if(!Directory.Exists(path))return [];
        var worlds=new List<LivingWorldInfo>();foreach(var dir in Directory.EnumerateDirectories(path))
        {
            if(!Guid.TryParseExact(Path.GetFileName(dir),"N",out var id))continue;
            try{var info=LivingFiles.Read<LivingWorldInfo>(Path.Combine(dir,"world.json"));info.Validate();if(info.WorldId!=id)throw new InvalidDataException("World identity mismatch.");worlds.Add(info);}
            catch(Exception ex) when(ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException){Warnings.Add($"{id:N}: {ex.Message}");}
        }
        return worlds.OrderByDescending(i=>i.CreatedAt).ToArray();
    }
    internal Guid? Selected
    {
        get{try{return LivingWorlds.Selection(Store).WorldId;}catch(Exception ex) when(ex is IOException or InvalidDataException or JsonException){return null;}}
    }
    private void Space()
    {
        if(Playing!=null)throw new InvalidOperationException("Return to the menu before changing worlds.");
        var path=LivingWorlds.Root(Store);if(Directory.Exists(path) && Directory.EnumerateDirectories(path).Count(p=>Guid.TryParseExact(Path.GetFileName(p),"N",out _))>=MaximumWorlds)throw new InvalidOperationException("Your ten-world library is full. Back up or delete a world before creating/importing another. Existing worlds have been preserved.");
    }
    internal async Task<LivingWorldSession> Create(string name,string seed,int width=1024,int height=576,GrandGeneration? generation=null,IProgress<string>? progress=null,CancellationToken cancel=default)
    {
        await changes.WaitAsync(cancel);try{Space();var world=await LivingWorlds.CreateGrand(Store,name,seed,width,height,generation??new(),progress,cancel);await Application.Current.Dispatcher.InvokeAsync(()=>{world.Civilization!.EnableEconomy(world.Controller.Manager.MatchAge);world.Controller.SaveNow();});return world;}finally{changes.Release();}
    }
    internal LivingWorldSession Open(Guid id)
    {
        if(Playing!=null && Playing!=id)throw new InvalidOperationException("One world may be open at a time.");
        if(!Worlds().Any(w=>w.WorldId==id))throw new InvalidDataException("This world is unavailable. Its files were preserved.");
        var session=LivingWorlds.Open(Store,id);LivingWorlds.Select(Store,new(ArcadeGame.LivingWorlds,id));return session;
    }
    private void Idle(Guid id)
    {if(Playing==id)throw new InvalidOperationException("Close the world before editing its library entry.");if(!Worlds().Any(w=>w.WorldId==id))throw new InvalidDataException("World is unavailable.");}
    internal void Rename(Guid id,string name)
    {Idle(id);name=name.Trim();if(name.Length is <1 or >80)throw new ArgumentException("World names must have 1–80 characters.");LivingWorlds.Rename(Store,id,name);}
    internal string Delete(Guid id)
    {
        Idle(id);LivingWorlds.Forget(Store,id);var source=WorldPath(id);var trash=Path.Combine(Root,"DeletedWorlds",id.ToString("N")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.GetDirectoryName(trash)!);
        // Both paths are derived from this library and validated before the move.
        if(!Path.GetFullPath(source).StartsWith(Path.GetFullPath(LivingWorlds.Root(Store))+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || !Path.GetFullPath(trash).StartsWith(Root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Invalid library path.");
        Directory.Move(source,trash);if(Selected==id)LivingFiles.Save(Path.Combine(LivingWorlds.Root(Store),"selection.json"),new ArcadeGameSelection());return trash;
    }
    internal void Backup(Guid id,string destination)
    {Idle(id);LivingWorlds.Forget(Store,id);if(File.Exists(destination))throw new IOException("Choose a new backup filename; existing backups are preserved.");ZipFile.CreateFromDirectory(WorldPath(id),destination,CompressionLevel.Fastest,false);}
    internal async Task<string> UpgradeEconomy(Guid id)
    {
        Idle(id);var world=Open(id);await world.PoliticsReady;var civic=world.Civilization??throw new InvalidOperationException("This world needs an existing civilization before enabling its economy.");
        if(civic.Economy!=null)return "Economy already enabled.";
        var directory=Path.Combine(Root,"Backups");Directory.CreateDirectory(directory);var backup=Path.Combine(directory,id.ToString("N")+"-pre-economy-"+Guid.NewGuid().ToString("N")+".zip");
        Backup(id,backup);world=Open(id);await world.PoliticsReady;Idle(id);await Application.Current.Dispatcher.InvokeAsync(()=>{world.Civilization!.EnableEconomy(world.Controller.Manager.MatchAge);world.Controller.SaveNow();});return backup;
    }
    private static LivingWorldInfo ValidateDirectory(string directory)
    {
        var info=LivingFiles.Read<LivingWorldInfo>(Path.Combine(directory,"world.json"));info.Validate();var loaded=new ArcadeWorldStore(Path.Combine(directory,"arcade-world.json")).Load();
        if(loaded.Blocked || loaded.World==null || loaded.World.WorldId!=info.WorldId)throw new InvalidDataException("The imported simulation is damaged or incompatible. Original files were preserved.");
        if(info.CivilizationMode)
        {
            if(loaded.World.LivingPolitics is not {} politics || loaded.World.LivingCivilization is not {} civic)throw new InvalidDataException("Enabled civilization data is missing.");
            var terrain=new LivingTerrain(info);var model=LivingNationWorld.Restore(new PoliticalLand(terrain),info.WorldId,politics);LivingCivilization.Restore(civic,model,directory);
        }
        return info;
    }
    private static void Copy(string source,string destination)
    {
        if((File.GetAttributes(source)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("World imports cannot follow directory links.");Directory.CreateDirectory(destination);
        foreach(var file in Directory.EnumerateFiles(source)){if((File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("World imports cannot follow file links.");File.Copy(file,Path.Combine(destination,Path.GetFileName(file)),false);}
        foreach(var directory in Directory.EnumerateDirectories(source))Copy(directory,Path.Combine(destination,Path.GetFileName(directory)));
    }
    internal async Task<Guid> Import(string source,bool zip=false)
    {
        await changes.WaitAsync();var staging=Path.Combine(Root,"Imports",Guid.NewGuid().ToString("N"));
        try
        {
            Space();var info=await Task.Run(()=>{Directory.CreateDirectory(staging);if(zip)ZipFile.ExtractToDirectory(source,staging,false);else Copy(Path.GetFullPath(source),staging);return ValidateDirectory(staging);});
            var destination=WorldPath(info.WorldId);if(Directory.Exists(destination))throw new IOException("This world identity already exists in Aevoria. No data was overwritten.");Directory.CreateDirectory(LivingWorlds.Root(Store));Directory.Move(staging,destination);return info.WorldId;
        }
        finally
        {
            // Failed imports are retained for diagnosis, never mistaken for library worlds.
            try{if(Directory.Exists(staging)){var failed=Path.Combine(Root,"ImportFailures");Directory.CreateDirectory(failed);Directory.Move(staging,Path.Combine(failed,Path.GetFileName(staging)));}}
            finally{changes.Release();}
        }
    }
    internal AevoriaPreferences Preferences()
    {var path=Path.Combine(Root,"game-settings.json");if(!File.Exists(path))return new();var value=LivingFiles.Read<AevoriaPreferences>(path);if(!Enum.IsDefined(value.Quality) || value.FrameRate is not (30 or 60) || !double.IsFinite(value.ZoomSensitivity) || value.ZoomSensitivity is <1 or >5)throw new InvalidDataException("Unsupported Aevoria settings; original file preserved.");return value;}
    internal void SavePreferences(AevoriaPreferences settings)
    {if(!Enum.IsDefined(settings.Quality) || settings.FrameRate is not (30 or 60) || !double.IsFinite(settings.ZoomSensitivity) || settings.ZoomSensitivity is <1 or >5)throw new ArgumentException("Invalid game settings.");LivingFiles.Save(Path.Combine(Root,"game-settings.json"),settings);}
}
