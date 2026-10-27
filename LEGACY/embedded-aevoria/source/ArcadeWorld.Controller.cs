using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
namespace SessionDashboard;
// The only update loop for the persistent world. Views render this manager, never advance it.
internal sealed class ArcadeWorldController : IDisposable
{
    private readonly ArcadeWorldStore storage;
    private readonly DispatcherTimer ticker;
    private readonly Stopwatch clock=new();
    private readonly Dispatcher dispatcher=Dispatcher.CurrentDispatcher;
    private readonly Dictionary<object,(int Priority,bool Active)> surfaces=[];
    private Task saving=Task.CompletedTask;
    private double saveAge;
    internal BugManager Manager {get;private set;}
    internal bool LoadBlocked {get;private set;}
    internal bool HasBackup {get;private set;}
    internal string? Status {get;private set;}
    internal object? ActiveSurface {get;private set;}
    internal bool Running=>ticker.IsEnabled;
    internal string SavePath=>storage.FilePath;
    internal event Action? Changed;
    internal Action<BugManager>? BeforeStep;
    internal Action<BugManager,double>? AfterStep;
    internal ArcadeWorldController(string filePath,ArcadeSettings? defaults=null)
    {
        storage=new(filePath);var loaded=storage.Load();LoadBlocked=loaded.Blocked;HasBackup=loaded.HasBackup;Status=loaded.Warning;
        Manager=loaded.World is {} snapshot ? BugManager.RestoreWorld(snapshot) : NewWorld(defaults);
        if(LoadBlocked)Manager.WorldPaused=true;
        ticker=new DispatcherTimer(DispatcherPriority.Background) {Interval=TimeSpan.FromMilliseconds(Manager.BattleOptions.TargetFps==30 ? 33 : 16)};
        ticker.Tick+=(_,_)=>Tick();
        if(loaded.World==null && !LoadBlocked)
            try {SaveNow();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {Status="Arcade save failed: "+ex.Message;}
    }
    private static BugManager NewWorld(ArcadeSettings? options)
    {
        var m=new BugManager();m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,(options??new()) with{Mode=ArcadeRuleSet.InfiniteWarfare}));
        m.InitializeInfiniteWorld(new Size(1600,1000));return m;
    }
    internal void Attach(object view,int priority=0) {surfaces[view]=(priority,true);UpdateActivity();}
    internal void SetActive(object view,bool active)
    {
        var wasActive=surfaces.TryGetValue(view,out var value) && value.Active;
        if(surfaces.ContainsKey(view))surfaces[view]=(value.Priority,active);UpdateActivity();
        if(wasActive && !active && !LoadBlocked)BeginSave();
    }
    internal void Detach(object view)
    {
        surfaces.Remove(view);UpdateActivity();if(!LoadBlocked)BeginSave();
    }
    private void UpdateActivity()
    {
        ActiveSurface=surfaces.Where(p=>p.Value.Active).OrderByDescending(p=>p.Value.Priority).Select(p=>p.Key).FirstOrDefault();
        var active=ActiveSurface!=null && !Manager.WorldPaused && !LoadBlocked;
        if(active && !ticker.IsEnabled) {clock.Restart();ticker.Start();}
        if(!active) {ticker.Stop();clock.Reset();}
    }
    private void Tick()
    {
        var elapsed=clock.Elapsed.TotalSeconds;clock.Restart();
        if(LoadBlocked || Manager.WorldPaused || ActiveSurface==null)return;
        var dt=Math.Min(.08,elapsed);BeforeStep?.Invoke(Manager);Manager.Advance(dt);AfterStep?.Invoke(Manager,dt);saveAge+=elapsed;Changed?.Invoke();
        if(saveAge>=15) {saveAge=0;BeginSave();}
    }
    internal void SetPaused(bool value)
    {
        if(LoadBlocked) {Changed?.Invoke();return;}
        Manager.WorldPaused=value;UpdateActivity();if(!LoadBlocked)BeginSave();Changed?.Invoke();
    }
    internal void ApplySettings(ArcadeSettings settings)
    {
        Manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,settings with{Mode=ArcadeRuleSet.InfiniteWarfare}));
        ticker.Interval=TimeSpan.FromMilliseconds(Manager.BattleOptions.TargetFps==30 ? 33 : 16);
        if(!LoadBlocked)BeginSave();Changed?.Invoke();
    }
    private void BeginSave()
    {
        if(LoadBlocked || !saving.IsCompleted)return;
        ArcadeWorldSnapshot snapshot;
        try{snapshot=Manager.CaptureWorld();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException){Status="Arcade save failed: "+ex.Message;return;}
        saving=Task.Run(()=>
        {
            try {storage.Save(snapshot);HasBackup=File.Exists(storage.FilePath+".bak");Status=null;}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) {Status="Arcade save failed: "+ex.Message;}
            finally {if(!dispatcher.HasShutdownStarted)dispatcher.BeginInvoke(()=>Changed?.Invoke());}
        });
    }
    internal void SaveNow()
    {
        if(LoadBlocked)return;
        saving.GetAwaiter().GetResult();
        try {storage.Save(Manager.CaptureWorld());HasBackup=File.Exists(storage.FilePath+".bak");Status=null;}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {Status="Arcade save failed: "+ex.Message;throw;}
    }
    internal void RecoverBackup()
    {
        saving.GetAwaiter().GetResult();var restored=storage.RecoverBackup();Manager=BugManager.RestoreWorld(restored);
        LoadBlocked=false;Status=null;UpdateActivity();Changed?.Invoke();
    }
    // Only the confirmed UI action calls this; the previous valid world becomes the backup.
    internal void ResetConfirmed()
    {
        saving.GetAwaiter().GetResult();if(!LoadBlocked)SaveNow();var fresh=NewWorld(Manager.BattleOptions);storage.Save(fresh.CaptureWorld(),true);
        Manager=fresh;LoadBlocked=false;HasBackup=File.Exists(storage.FilePath+".bak");Status=null;saveAge=0;UpdateActivity();Changed?.Invoke();
    }
    public void Dispose() {ticker.Stop();clock.Stop();if(!LoadBlocked)SaveNow();surfaces.Clear();}
}
internal static class ArcadeWorlds
{
    private static readonly Dictionary<string,ArcadeWorldController> controllers=new(StringComparer.OrdinalIgnoreCase);
    internal static bool TryGet(StateStore store,out ArcadeWorldController? world)=>controllers.TryGetValue(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!,"arcade-world.json"),out world);
    internal static ArcadeWorldController For(StateStore store)
    {
        // Production sessions share the per-user Nexa data directory. Isolated previews/tests
        // naturally use their own explicit store directory without touching the production save.
        var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!,"arcade-world.json");
        if(!controllers.TryGetValue(path,out var world))controllers[path]=world=new(path,store.LoadBugPreferences().Arcade);
        return world;
    }
    internal static void FlushAll()
    {
        foreach(var world in controllers.Values)
            try {world.Dispose();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {Trace.TraceError("Arcade shutdown save: "+ex.Message);}
        controllers.Clear();
    }
    internal static void Release(StateStore store)
    {
        var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(store.FilePath))!,"arcade-world.json");
        if(!controllers.TryGetValue(path,out var controller))return;
        if(controller.ActiveSurface!=null)throw new InvalidOperationException("Close the active world first.");controller.Dispose();controllers.Remove(path);
    }
}
