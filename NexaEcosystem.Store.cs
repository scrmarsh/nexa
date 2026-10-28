using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;
using System.Windows;

namespace SessionDashboard;

internal sealed partial class NexaEcosystem : IDisposable
{
    private static readonly Dictionary<string, NexaEcosystem> worlds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions json = new() { WriteIndented = true };
    internal static NexaEcosystem For(StateStore store)
    {
        var key = Path.GetFullPath(store.FilePath);
        if (!worlds.TryGetValue(key, out var world)) worlds[key] = world = new NexaEcosystem(store);
        return world;
    }
    internal static void FlushAll()
    { foreach (var world in worlds.Values.ToArray()) world.Dispose(); worlds.Clear(); }
    internal static void UpdateSeason(StateStore store, NexaOptions options)
    { if (worlds.TryGetValue(Path.GetFullPath(store.FilePath), out var world)) world.ApplySeason(options); }
    private readonly StateStore store;
    private readonly Dictionary<object, Func<bool>> views = [];
    private readonly DispatcherTimer timer = new(DispatcherPriority.Background);
    private readonly Stopwatch clock = new();
    private double previous, checkpoint, scoreRetry;
    private readonly List<EcosystemSquash> pendingSquashes = [];
    private Guid lastSessionId;
    private string? scoreWarning;
    private bool disposed, blocked;
    private readonly bool ownerBlocked;
    internal BugManager Manager { get; }
    internal string FilePath { get; }
    internal string? Status { get; private set; }
    internal event Action? Changed;
    internal int ViewCount => views.Count;
    internal TimeSpan FrameInterval => timer.Interval;
    private NexaOptions seasonalOptions = new();
    internal bool CanSave => !blocked && !disposed;
    internal double LastStepMilliseconds { get; private set; }
    internal double AnimationTime => Manager.SimulationAge;
    private EcosystemDocument ReadDocument(string path)
    {
        if (new FileInfo(path).Length > 8_000_000) throw new InvalidDataException("Ecosystem save exceeds the supported size.");
        var doc = JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(path), json) ?? throw new InvalidDataException("Empty ecosystem save.");
        doc.Validate(store.UserId); return doc;
    }
    private NexaEcosystem(StateStore store)
    {
        this.store = store; FilePath = Path.GetFullPath(store.FilePath) + ".ecosystem.json";
        Manager = new BugManager();
        var preferences = store.LoadBugPreferences(); Manager.Configure(preferences);
        EcosystemDocument? loaded = null;
        foreach (var path in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(path)) continue;
            try
            {
                loaded = ReadDocument(path);
                if (path.EndsWith(".bak", StringComparison.Ordinal)) { blocked = true; Status = "The current ecosystem save is unreadable. Use Restore Backup to recover the available backup."; }
                break;
            }
            catch (UnauthorizedAccessException) { blocked = true; ownerBlocked = true; Status = "This ecosystem cannot be opened because its owner could not be verified."; loaded = null; break; }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
            { loaded = null; blocked = true; Status = "The ecosystem save could not be read. Original files are preserved. Restore a backup or explicitly reset the ecosystem."; }
        }
        if (loaded != null) Manager.RestoreEcosystem(loaded);
        else Manager.NewEcosystem(store.UserId, preferences.Ecosystem ?? new());
        preferences = preferences with { Ecosystem = preferences.Ecosystem ?? loaded?.Options ?? new() };
        Manager.Configure(preferences);
        var savedState = store.Load();
        if(savedState.Workspace != null) lastSessionId = Continuity.Current(savedState.Workspace,store.UserId).Current.SessionId;
        if(loaded != null) pendingSquashes.AddRange(Continuity.Clone(loaded.PendingSquashes));
        ReconcileGeneration(savedState.Workspace?.LiveEcosystemGeneration??Guid.Empty);
        ApplySeason(savedState.Workspace?.Settings ?? new());
        SetCadence(); timer.Tick += Tick;
        if (!blocked && pendingSquashes.Count > 0) FlushScores();
        if (!blocked && !File.Exists(FilePath)) Save();
    }
    internal void Attach(object view, Func<bool> visible)
    {
        if (disposed) throw new ObjectDisposedException(nameof(NexaEcosystem));
        if(views.Count==0)Manager.BeginDashboardArrival();
        views[view] = visible;
        if (!timer.IsEnabled) { previous = 0; clock.Restart(); timer.Start(); }
    }
    internal void Detach(object view)
    { views.Remove(view); if (views.Count == 0) { timer.Stop(); clock.Stop(); Save(); } }
    internal void ApplyPreferences(BugPreferences preferences)
    {
        Manager.Configure(preferences with { Ecosystem = (preferences.Ecosystem ?? new()).Safe() });
        foreach (var bug in Manager.Bugs) bug.Scale = Manager.EcosystemSettings.Size;
        SetCadence(); ApplySeason(seasonalOptions); Changed?.Invoke();
    }
    internal void ApplySeason(NexaOptions options)
    {
        seasonalOptions = options; Manager.VisualOptions=options;
        var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);
        var current=Manager.EcosystemSettings;var mapped=current.ForSeason(active);
        if(mapped!=current)
        {
            var preferences=Manager.Preferences with {Ecosystem=mapped};Manager.Configure(preferences);
            try{store.SaveBugPreferences(preferences);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Status="Bug preferences could not be saved. Your previous settings are preserved.";}
        }
        Manager.SeasonalCostumes = Manager.EcosystemSettings.Costumes &&
            ((!Manager.EcosystemSettings.MatchAppearance && Manager.EcosystemSettings.Appearance == BugAppearance.Halloween) || active && options.HalloweenBugCostumes);
        Manager.SeasonalCandy = active;
    }
    private void SetCadence()
    {
        var fps = Manager.EcosystemSettings.Quality switch { BugAnimationQuality.Performance => 15, BugAnimationQuality.High => 45, _ => 30 };
        timer.Interval = TimeSpan.FromSeconds(1d / fps);
    }
    private void Tick(object? sender, EventArgs e)
    {
        var current = clock.Elapsed.TotalSeconds; var dt = Math.Clamp(current - previous, 0, .1); previous = current;
        scoreRetry += dt;
        if(!responsiveSaving && pendingSquashes.Count > 0 && scoreRetry >= 5)
        {
            scoreRetry=0;
            var owner=Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault(w=>w.IsLoaded && w.SettingsStore.FilePath==store.FilePath);
            if(owner!=null)
                foreach(var squash in pendingSquashes.ToArray())
                    _=RecordSquashResponsive(new BugEntity{InteractionId=squash.BugId,Species=squash.Species},owner);
        }
        if (!views.Values.Any(v => v()) || blocked || Manager.WorldPaused) return;
        var began = Stopwatch.GetTimestamp();
        if (!blocked) Manager.Advance(dt);
        LastStepMilliseconds = Stopwatch.GetElapsedTime(began).TotalMilliseconds;
        Manager.ObserveFrameCost(LastStepMilliseconds);
        checkpoint += dt; if (checkpoint >= 30 && !responsiveSaving) { checkpoint = 0; Save(); }
        Changed?.Invoke();
    }
    internal bool Save()
    {
        if (!CanSave) return false;
        try
        {
            try{diskWrite.GetAwaiter().GetResult();}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
            var doc = Manager.CaptureEcosystem(store.UserId); doc.PendingSquashes = Continuity.Clone(pendingSquashes); doc.Validate(store.UserId);
            var payload=JsonSerializer.Serialize(doc,json);Manager.PersistenceBytes=System.Text.Encoding.UTF8.GetByteCount(payload);if(Manager.PersistenceBytes>8_000_000)throw new InvalidDataException("Resource safety limit reached; the previous save is preserved.");
            StateStore.AtomicWrite(FilePath, payload, true); Status = scoreWarning; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        { Status = "The ecosystem could not be saved. Its latest live state is retained; saving will retry."; return false; }
    }
    internal void Interaction() { Save(); Changed?.Invoke(); }
    internal void RetryHistory() { FlushScores(); Save(); Changed?.Invoke(); }
    internal void TogglePause()
    {
        var prior = Manager.WorldPaused; Manager.WorldPaused = !prior;
        if (!Save()) Manager.WorldPaused = prior;
        Changed?.Invoke();
    }
    internal bool RestoreBackup()
    {
        if (ownerBlocked) return false;
        try
        {
            var doc = ReadDocument(FilePath + ".bak"); doc.Options = Manager.EcosystemSettings; doc.PendingSquashes = Continuity.Clone(pendingSquashes);
            if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".before-restore-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
            StateStore.AtomicWrite(FilePath, JsonSerializer.Serialize(doc, json), true);
            Manager.RestoreEcosystem(doc); blocked = false; Status = scoreWarning; Changed?.Invoke(); return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        { Status = "The backup could not be restored. The current ecosystem and original files are unchanged."; Changed?.Invoke(); return false; }
    }
    internal bool Reset()
    {
        if (ownerBlocked) return false;
        try
        {
            var candidate = new BugManager(); candidate.Configure(Manager.Preferences); candidate.NewEcosystem(store.UserId, Manager.EcosystemSettings);
            var doc = candidate.CaptureEcosystem(store.UserId); doc.PendingSquashes = Continuity.Clone(pendingSquashes); doc.Validate(store.UserId);
            if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".before-reset-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
            StateStore.AtomicWrite(FilePath, JsonSerializer.Serialize(doc, json), true);
            Manager.RestoreEcosystem(doc); blocked = false; Status = scoreWarning; Changed?.Invoke(); return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        { Status = "The ecosystem could not be reset. The live world was preserved."; Changed?.Invoke(); return false; }
    }
    internal void RecordSquash(BugEntity bug, MainWindow? owner)
    {
        if(pendingSquashes.Count >= 500) { Status="The pending squash history is full. Resolve the save error before squashing more creatures."; Changed?.Invoke(); return; }
        var id = owner?.state.Workspace is { } workspace ? Continuity.Current(workspace,store.UserId).Current.SessionId : lastSessionId;
        if(pendingSquashes.Any(s=>s.BugId==bug.InteractionId)) {FlushScores(owner);Interaction();return;}
        pendingSquashes.Add(new(){BugId=bug.InteractionId,Species=bug.Species,SessionId=id,At=DateTimeOffset.UtcNow});
        FlushScores(owner); Interaction();
    }
    private Task diskWrite=Task.CompletedTask;
    private Task responsiveTail=Task.CompletedTask;
    private bool responsiveSaving;
    internal double LastSquashPersistenceMilliseconds {get;private set;}
    internal Task RecordSquashResponsive(BugEntity bug,MainWindow? owner)
        =>timer.Dispatcher.CheckAccess()?QueueSquashResponsive(bug,owner):timer.Dispatcher.InvokeAsync(()=>QueueSquashResponsive(bug,owner)).Task.Unwrap();
    private Task QueueSquashResponsive(BugEntity bug,MainWindow? owner)
    {
        if(!CanSave)return Task.CompletedTask;
        var session=owner?.state.Workspace is {} workspace?Continuity.Current(workspace,store.UserId).Current.SessionId:lastSessionId;
        var squash=pendingSquashes.FirstOrDefault(s=>s.BugId==bug.InteractionId);
        if(squash==null)
        {
            if(pendingSquashes.Count>=500){Status="The pending squash history is full. Resolve the save error before squashing more creatures.";Changed?.Invoke();return Task.CompletedTask;}
            squash=new(){BugId=bug.InteractionId,Species=bug.Species,SessionId=session,At=DateTimeOffset.UtcNow};pendingSquashes.Add(squash);
        }
        var previous=responsiveTail;
        return responsiveTail=Run();
        async Task Run()
        {
            await previous;
            var operation=await timer.Dispatcher.InvokeAsync(()=>RunOnDispatcher(),DispatcherPriority.Background);
            await operation;
        }
        async Task RunOnDispatcher()
        {
            responsiveSaving=true;var watch=Stopwatch.StartNew();
            try
            {
                if(!CanSave)return;
                // Write-ahead event survives interruption; ledger event IDs make replay idempotent.
                await SaveSnapshotAsync();
                if(owner!=null)
                {
                    if(!await timer.Dispatcher.InvokeAsync(()=>owner.ApplyPendingSquash(bug,squash.At,squash.SessionId)))return;
                    if(!await owner.PersistSquashAsync())return;
                    await timer.Dispatcher.InvokeAsync(()=>pendingSquashes.Remove(squash));await SaveSnapshotAsync();
                }
                // Ownerless ecosystem view retains the durable event for existing history recovery.
                await timer.Dispatcher.InvokeAsync(()=>Changed?.Invoke());
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or InvalidOperationException)
            {await timer.Dispatcher.InvokeAsync(()=>{scoreWarning="A squash is awaiting its session-history save. It is kept with the ecosystem and will retry.";Status=scoreWarning;});}
            finally{await timer.Dispatcher.InvokeAsync(()=>{responsiveSaving=false;LastSquashPersistenceMilliseconds=watch.Elapsed.TotalMilliseconds;});}
        }
    }
    private async Task SaveSnapshotAsync()
    {
        try{await diskWrite;}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){}
        var write=await timer.Dispatcher.InvokeAsync(()=>
        {
            var doc=Manager.CaptureEcosystem(store.UserId);doc.PendingSquashes=Continuity.Clone(pendingSquashes);doc.Validate(store.UserId);
            var payload=JsonSerializer.Serialize(doc,json);Manager.PersistenceBytes=System.Text.Encoding.UTF8.GetByteCount(payload);
            if(Manager.PersistenceBytes>8_000_000)throw new InvalidDataException("Resource safety limit reached; the previous save is preserved.");
            return diskWrite=Task.Run(()=>StateStore.AtomicWrite(FilePath,payload,true));
        });
        await write;
    }
    private void FlushScores(MainWindow? owner = null)
    {
        if(blocked) return;
        owner ??= Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault(w=>w.IsLoaded && w.SettingsStore.FilePath==store.FilePath);
        foreach(var squash in pendingSquashes.OrderBy(s=>s.At).ToArray())
        {
            try
            {
                if(owner?.state.Workspace is { } active && (squash.SessionId==Guid.Empty || Continuity.Current(active,store.UserId).Current.SessionId==squash.SessionId))
                {
                    if(!owner.RecordEcosystemSquash(new BugEntity {InteractionId=squash.BugId,Species=squash.Species},squash.At)) throw new IOException("Session save failed.");
                }
                else
                {
                    var state = store.Load();
                    if(state.Workspace==null || store.Warning?.Contains("could not be read",StringComparison.OrdinalIgnoreCase)==true) throw new IOException("The session record is unavailable.");
                    var current = Continuity.Current(state.Workspace,store.UserId).Current; lastSessionId=current.SessionId;
                    var target = squash.SessionId==Guid.Empty ? current : state.Workspace.Dashboards.SelectMany(d=>d.History.Append(d.Current)).FirstOrDefault(s=>s.SessionId==squash.SessionId);
                    if(target==null) throw new IOException("The original session record is unavailable.");
                    var ledger = target.SessionId==current.SessionId ? state.Productivity : target.Data.Productivity;
                    var score=ledger.Score;
                    if(ProductivityScoring.DirectSquash(ledger,squash.BugId.ToString(),squash.At,state.Workspace.Settings.StreakMultiplierCap,squash.Species.ToString()))
                    {state.Workspace.AllTimeSquashes++;var key=squash.Species.ToString();state.Workspace.AllTimeSquashesBySpecies[key]=state.Workspace.AllTimeSquashesBySpecies.GetValueOrDefault(key)+1;if(target.SessionId!=current.SessionId)state.Workspace.AllTimeProductivityScore+=ledger.Score-score;}
                    store.Save(state);
                }
                pendingSquashes.Remove(squash);
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
            {scoreWarning="A squash is awaiting its session-history save. It is kept with the ecosystem and will retry.";Status=scoreWarning;return;}
        }
        scoreWarning=null;
    }
    public void Dispose()
    { if (disposed) return; FlushScores(); Save(); disposed = true; timer.Stop(); timer.Tick -= Tick; views.Clear(); clock.Stop(); }
}
