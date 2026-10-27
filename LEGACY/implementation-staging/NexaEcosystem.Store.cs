using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Threading;

namespace SessionDashboard;

internal sealed class NexaEcosystem : IDisposable
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
    private double previous, checkpoint;
    private bool disposed, blocked;
    private readonly bool ownerBlocked;
    internal BugManager Manager { get; }
    internal string FilePath { get; }
    internal string? Status { get; private set; }
    internal event Action? Changed;
    internal int ViewCount => views.Count;
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
        Manager.Configure(preferences); ApplySeason(store.Load().Workspace?.Settings ?? new());
        SetCadence(); timer.Tick += Tick;
    }
    internal void Attach(object view, Func<bool> visible)
    {
        if (disposed) throw new ObjectDisposedException(nameof(NexaEcosystem));
        views[view] = visible;
        if (!timer.IsEnabled) { previous = 0; clock.Restart(); timer.Start(); }
    }
    internal void Detach(object view)
    { views.Remove(view); if (views.Count == 0) { timer.Stop(); clock.Stop(); Save(); } }
    internal void ApplyPreferences(BugPreferences preferences)
    {
        Manager.Configure(preferences with { Ecosystem = (preferences.Ecosystem ?? new()).Safe() });
        foreach (var bug in Manager.Bugs) bug.Scale = Manager.EcosystemSettings.Size;
        SetCadence(); Save(); Changed?.Invoke();
    }
    internal void ApplySeason(NexaOptions options)
    {
        var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);
        Manager.SeasonalCostumes = active && options.HalloweenBugCostumes && Manager.EcosystemSettings.Costumes;
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
        if (!views.Values.Any(v => v())) return;
        var began = Stopwatch.GetTimestamp();
        if (!blocked) Manager.Advance(dt);
        LastStepMilliseconds = Stopwatch.GetElapsedTime(began).TotalMilliseconds;
        checkpoint += dt; if (checkpoint >= 30) { checkpoint = 0; Save(); }
        Changed?.Invoke();
    }
    internal bool Save()
    {
        if (!CanSave) return false;
        try
        {
            var doc = Manager.CaptureEcosystem(store.UserId); doc.Validate(store.UserId);
            StateStore.AtomicWrite(FilePath, JsonSerializer.Serialize(doc, json), true); Status = null; return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        { Status = "The ecosystem could not be saved. Its latest live state is retained; saving will retry."; return false; }
    }
    internal void Interaction() { Save(); Changed?.Invoke(); }
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
            var doc = ReadDocument(FilePath + ".bak"); doc.Options = Manager.EcosystemSettings;
            if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".before-restore-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
            StateStore.AtomicWrite(FilePath, JsonSerializer.Serialize(doc, json), true);
            Manager.RestoreEcosystem(doc); blocked = false; Status = null; Changed?.Invoke(); return true;
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
            var doc = candidate.CaptureEcosystem(store.UserId); doc.Validate(store.UserId);
            if (File.Exists(FilePath)) File.Copy(FilePath, FilePath + ".before-reset-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"), false);
            StateStore.AtomicWrite(FilePath, JsonSerializer.Serialize(doc, json), true);
            Manager.RestoreEcosystem(doc); blocked = false; Status = null; Changed?.Invoke(); return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        { Status = "The ecosystem could not be reset. The live world was preserved."; Changed?.Invoke(); return false; }
    }
    internal void RecordSquash(BugEntity bug, MainWindow? owner)
    {
        if (owner != null) owner.RecordEcosystemSquash(bug);
        else
        {
            try
            {
                var state = store.Load();
                if (ProductivityScoring.DirectSquash(state.Productivity, bug.InteractionId.ToString(), DateTimeOffset.UtcNow))
                { state.Workspace ??= Continuity.Migrate(state, store.UserId); state.Workspace.AllTimeSquashes++; store.Save(state); }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { Status = "The squash was applied, but its productivity record could not be saved."; }
        }
        Interaction();
    }
    public void Dispose()
    { if (disposed) return; Save(); disposed = true; timer.Stop(); timer.Tick -= Tick; views.Clear(); clock.Stop(); }
}
