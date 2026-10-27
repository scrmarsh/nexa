using System.IO;
using System.Windows;
namespace SessionDashboard;
internal sealed class LivingWorldSession
{
    internal LivingWorldInfo Info {get;}
    internal LivingTerrain Terrain {get;}
    internal TerrainChunks Chunks {get;}
    internal ArcadeWorldController Controller {get;}
    private readonly string metadataPath;
    private PoliticalLand? politicalLand;
    private BugManager? politicalManager;
    internal LivingNationWorld? Nations=>Controller.Manager.Nations;
    internal LivingCivilization? Civilization=>Controller.Manager.Civilization;
    internal string? CivilizationWarning {get;private set;}
    private string? politicsWarning;
    internal string? PoliticsWarning {get=>politicsWarning??Nations?.Diagnostic??CivilizationWarning??Civilization?.Warning;private set=>politicsWarning=value;}
    internal Task PoliticsReady {get;private set;}=Task.CompletedTask;
    internal event Action? PoliticsChanged;
    private static readonly SemaphoreSlim topologyWorker=new(1,1);
    internal LivingWorldSession(string directory,LivingWorldInfo info,LivingTerrain? prepared=null,PoliticalLand? preparedLand=null)
    {
        Info=info;Terrain=prepared??new(info);metadataPath=Path.Combine(directory,"world.json");
        var load=new ArcadeWorldStore(Path.Combine(directory,"arcade-world.json")).Load();
        if(load.World==null || load.World.WorldId!=info.WorldId || load.World.Width!=info.Extent || load.World.Height!=info.HeightExtent || (info.GeneratorVersion>=2 ? load.World.LivingGeneratorVersion!=info.GeneratorVersion : load.World.LivingGeneratorVersion!=0))
            throw new InvalidDataException("Living World simulation does not match its terrain. Its files were preserved.");
        Controller=ArcadeWorlds.For(new StateStore(Path.Combine(directory,"state.json")));
        Controller.BeforeStep=m=>m.PrepareLivingStep();Controller.AfterStep=(m,dt)=> {m.ResolveLivingStep(Terrain,dt);m.Nations?.Tick(m);m.Civilization?.Tick(m);};
        Chunks=new(Terrain,info.GeneratorVersion>=2 ? 96*1024*1024 : 8*1024*1024);
        Controller.Changed+=BindPolitics;
        if(preparedLand!=null){politicalLand=preparedLand;BindPolitics();}else PoliticsReady=PreparePolitics();
    }
    private async Task PreparePolitics()
    {
        try {await topologyWorker.WaitAsync();try {politicalLand=await Task.Run(()=>new PoliticalLand(Terrain));}finally {topologyWorker.Release();}await Application.Current.Dispatcher.InvokeAsync(BindPolitics);}
        catch(Exception ex) when(ex is IOException or InvalidDataException or InvalidOperationException) {await Application.Current.Dispatcher.InvokeAsync(()=>{PoliticsWarning="Territory initialization unavailable: "+ex.Message;PoliticsChanged?.Invoke();});}
    }
    private void BindPolitics()
    {
        var manager=Controller.Manager;if(politicalLand==null || ReferenceEquals(manager,politicalManager) || Controller.LoadBlocked)return;
        politicalManager=manager;
        try
        {
            if(manager.PendingPolitics is {} raw)manager.Nations=LivingNationWorld.Restore(politicalLand,Info.WorldId,raw);
            else
            {
                // A permanent migration recovery point supplements the ordinary rotating backup.
                var backup=Controller.SavePath+".pre-nations";if(File.Exists(Controller.SavePath) && !File.Exists(backup))File.Copy(Controller.SavePath,backup);
                manager.Nations=LivingNationWorld.Initialize(politicalLand,manager,false);
            }
            manager.Nations.BindWriter();PoliticsWarning=manager.Nations.Diagnostic;
            if(manager.Nations.Expansion?.State.LastTick>manager.MatchAge+1e-6)throw new InvalidDataException("Territorial expansion clock exceeds the saved world clock.");
            manager.PendingPolitics=null;
        }
        catch(Exception ex) when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException or NullReferenceException)
        {manager.Nations=null;PoliticsWarning="Optional territory data is unavailable and has been preserved. Restore a backup to recover it. "+ex.Message;}
        if(Info.CivilizationMode && manager.Nations!=null)
        {
            try
            {
                if(manager.PendingCivilization is not {} raw)throw new InvalidDataException("Civilization state is missing; no replacement was generated.");
                var restored=LivingCivilization.Restore(raw,manager.Nations,Path.GetDirectoryName(metadataPath)!);
                if(restored.Economy is {} financial && (financial.State.LastTick>manager.MatchAge+1e-6 || financial.State.Recent.Any(f=>f.Time>manager.MatchAge+1e-6)))throw new InvalidDataException("Economy clock exceeds the saved world clock.");
                if(restored.State.LastTick>manager.MatchAge+1e-6 || restored.State.Nations.Where(n=>n.CurrentRulerId!=null).Any(n=>!manager.Bugs.Any(b=>b.UnitId==restored.State.Royals.First(r=>r.Id==n.CurrentRulerId).UnitId && BugManager.Alive(b))))throw new InvalidDataException("Civilization rulers/clock do not match authoritative units.");
                manager.Civilization=restored;manager.Civilization.BindWriter();manager.PendingCivilization=null;CivilizationWarning=null;
            }
            catch(Exception ex) when(ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException or NullReferenceException or InvalidOperationException)
            {manager.Civilization=null;CivilizationWarning="Civilization data unavailable and preserved. Restore the world and civilization-history directory together. "+ex.Message;}
        }
        PoliticsChanged?.Invoke();
    }
    internal void SaveMetadata() {Info.Validate();LivingFiles.Save(metadataPath,Info);}
}
internal static partial class LivingWorlds
{
    private static readonly Dictionary<string,LivingWorldSession> sessions=new(StringComparer.OrdinalIgnoreCase);
    internal static string Root(StateStore store)=>Path.Combine(Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!,"living-worlds");
    internal static ArcadeGameSelection Selection(StateStore store)
    {
        var path=Path.Combine(Root(store),"selection.json");if(!File.Exists(path))return new();
        var value=LivingFiles.Read<ArcadeGameSelection>(path);
        if(!Enum.IsDefined(value.Game) || value.Game==ArcadeGame.LivingWorlds && value.WorldId==null)throw new InvalidDataException("Invalid Arcade selection. World files were preserved.");
        return value;
    }
    internal static LivingWorldSession? Current(StateStore store)
    {var selection=Selection(store);return selection.Game==ArcadeGame.LivingWorlds && selection.WorldId is {} id ? Open(store,id) : null;}
    internal static LivingWorldSession Open(StateStore store,Guid id,LivingTerrain? prepared=null,PoliticalLand? preparedLand=null)
    {
        var directory=Path.Combine(Root(store),id.ToString("N"));
        if(!sessions.TryGetValue(directory,out var session))
        {
            var info=LivingFiles.Read<LivingWorldInfo>(Path.Combine(directory,"world.json"));info.Validate();
            if(info.WorldId!=id)throw new InvalidDataException("Living World identity mismatch.");
            sessions[directory]=session=new(directory,info,prepared,preparedLand);
        }
        return session;
    }
    internal static IReadOnlyList<LivingWorldInfo> List(StateStore store)
    {
        var result=new List<LivingWorldInfo>();var root=Root(store);if(!Directory.Exists(root))return result;
        foreach(var directory in Directory.EnumerateDirectories(root))
        {
            if(!Guid.TryParseExact(Path.GetFileName(directory),"N",out var id))continue;
            try {var info=LivingFiles.Read<LivingWorldInfo>(Path.Combine(directory,"world.json"));info.Validate();if(info.WorldId==id)result.Add(info);}
            catch(Exception ex) when(ex is IOException or System.Text.Json.JsonException or UnauthorizedAccessException) {System.Diagnostics.Trace.TraceWarning("Living World preserved but unavailable: "+ex.Message);}
        }
        return result.OrderByDescending(w=>w.CreatedAt).ToArray();
    }
    internal static async Task<LivingWorldSession> Create(StateStore store,string name,string seed,int tiles,int tilesY=0,int generatorVersion=2,bool civilizations=false)
    {
        var info=new LivingWorldInfo {Name=string.IsNullOrWhiteSpace(name) ? "Living World" : name.Trim(),Seed=string.IsNullOrWhiteSpace(seed) ? Guid.NewGuid().ToString("N")[..12] : seed.Trim(),Tiles=tiles,TilesY=tilesY,GeneratorVersion=generatorVersion,CivilizationMode=civilizations};info.Validate();
        var directory=Path.Combine(Root(store),info.WorldId.ToString("N"));
        var prepared=await Task.Run(()=>
        {
            var terrain=new LivingTerrain(info);var focus=terrain.FindLand(new Point(info.Extent/2,info.HeightExtent/2));info.FocusX=focus.X;info.FocusY=focus.Y;
            var manager=new BugManager(23);manager.WorldId=info.WorldId;manager.LivingGeneratorVersion=generatorVersion==2 ? 2 : 0;manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings()));manager.InitializeInfiniteWorld(new Size(info.Extent,info.HeightExtent));manager.SeedLivingColonies(terrain);
            var land=new PoliticalLand(terrain);manager.Nations=LivingNationWorld.Initialize(land,manager,true);manager.PlacePoliticalHomes(manager.Nations);
            if(civilizations)manager.Civilization=LivingCivilization.Create(manager,directory);
            var home=manager.Colonies.Values.OrderBy(c=>(c.Territory-focus).LengthSquared).First().Territory;info.FocusX=home.X;info.FocusY=home.Y;
            if(Directory.Exists(directory))throw new IOException("World already exists; no data was replaced.");
            LivingFiles.Save(Path.Combine(directory,"world.json"),info);
            new ArcadeWorldStore(Path.Combine(directory,"arcade-world.json")).Save(manager.CaptureWorld());
            return (terrain,land);
        });
        return await Application.Current.Dispatcher.InvokeAsync(()=> {var session=Open(store,info.WorldId,prepared.terrain,prepared.land);Select(store,new(ArcadeGame.LivingWorlds,info.WorldId));return session;});
    }
    internal static void Select(StateStore store,ArcadeGameSelection selection)
    {
        if(selection.Game==ArcadeGame.LivingWorlds && selection.WorldId is {} id)_=Open(store,id);
        LivingFiles.Save(Path.Combine(Root(store),"selection.json"),selection);
        var prefs=store.LoadBugPreferences() with{Enabled=true,Density=BugDensity.Arcade};store.SaveBugPreferences(prefs);
        foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>string.Equals(Path.GetFullPath(w.SettingsStore.FilePath),Path.GetFullPath(store.FilePath),StringComparison.OrdinalIgnoreCase)).ToArray())window.ApplyBugPreferences(prefs);
    }
    internal static void Flush()
    {
        foreach(var session in sessions.Values)
        {
            try {session.SaveMetadata();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {System.Diagnostics.Trace.TraceError(ex.Message);}
            session.Chunks.Dispose();
        }
        sessions.Clear();
    }
    internal static void Forget(StateStore store,Guid id)
    {
        var directory=Path.Combine(Root(store),id.ToString("N"));
        if(sessions.TryGetValue(directory,out var session))
        {
            if(session.Controller.ActiveSurface!=null)throw new InvalidOperationException("Close the world before changing its library files.");
            session.Controller.SaveNow();session.SaveMetadata();session.Chunks.Dispose();sessions.Remove(directory);
        }
        ArcadeWorlds.Release(new StateStore(Path.Combine(directory,"state.json"),gameContext:true));
    }
    internal static void Rename(StateStore store,Guid id,string name)
    {
        var directory=Path.Combine(Root(store),id.ToString("N"));var path=Path.Combine(directory,"world.json");
        var info=LivingFiles.Read<LivingWorldInfo>(path) with{Name=name};info.Validate();if(info.WorldId!=id)throw new InvalidDataException("World identity mismatch.");LivingFiles.Save(path,info);
        if(sessions.TryGetValue(directory,out var session))session.Info.Name=name;
    }
}
