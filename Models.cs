using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SessionDashboard;

public sealed class Preset
{
    public bool AutomaticScheduling { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Focused work";
    public int Minutes { get; set; } = 50;
    public int IntervalMinutes { get; set; } = 10;
    public string AccentColor { get; set; } = "";
    public string PixelColor { get; set; } = "";
    public int? StartClockMinutes { get; set; }
    public int? EndClockMinutes { get; set; }
    public int EndDayOffset { get; set; }
    public List<DayOfWeek> ScheduledDays { get; set; } = [];
}

public sealed class Session
{
    public string Title { get; set; } = "";
    public string TimeZoneId { get; set; } = "Automatic";
    public bool AutomaticScheduling { get; set; }
    public DateTimeOffset? BeforeSessionCountdownFrom { get; set; }
    public string? ScheduleTimeZoneId { get; set; }
    public int? RecurringStartMinutes { get; set; }
    public int? RecurringEndMinutes { get; set; }
    public int? RecurringEndDayOffset { get; set; }
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int IntervalMinutes { get; set; } = 10;
    public DateTimeOffset? StoppedAt { get; set; }
    public List<DayOfWeek> ScheduledDays { get; set; } = [];
    public double ConfiguredDurationMinutes { get; set; }
    public List<string> ReachedMilestones { get; set; } = [];
    public Dictionary<int, string> IntervalNotes { get; set; } = [];
    public Dictionary<int, string> HedyIntervalNotes { get; set; } = [];
    public Dictionary<int, string> IntervalSummaries { get; set; } = [];
    public string HedySessionId { get; set; } = "";
    public string HedySourceTitle { get; set; } = "Hedy";
    public string HedyTranscript { get; set; } = "";
    public string HedyBaseline { get; set; } = "";
    public string NotesSource { get; set; } = "Hedy";
    public string LocalRecordingPath { get; set; } = "";
    public double LocalRecordingSeconds { get; set; }
    public List<TimelineEntry> TimelineEntries { get; set; } = [];
    public double Progress(DateTimeOffset now) => End <= Start ? 0 : Math.Clamp(((StoppedAt ?? now) - Start).TotalSeconds / (End - Start).TotalSeconds, 0, 1);
    public TimeSpan Elapsed(DateTimeOffset now) => TimeSpan.FromSeconds(Progress(now) * (End - Start).TotalSeconds);
    public TimeSpan Remaining(DateTimeOffset now) => StoppedAt != null || now >= End ? TimeSpan.Zero : (now < Start ? Start - now : End - now);
    public bool IsRunning(DateTimeOffset now) => StoppedAt == null && now >= Start && now < End;
}

public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Permanent { get; set; }
    public string Text { get; set; } = "";
    public bool Done { get; set; }
}

// This allowlisted payload contains dashboard data only, never account credentials.
public class DashboardData
{
    public ProductivityLedger Productivity { get; set; } = new();
    public List<Preset> Presets { get; set; } =
    [
        new() { Name = "Lecture", Minutes = 75, IntervalMinutes = 5, AccentColor = "#5B7CFF", PixelColor = "#3B82F6" },
        new() { Name = "Focused work", Minutes = 50, IntervalMinutes = 10, AccentColor = "#7C3AED", PixelColor = "#8B5CF6" },
        new() { Name = "Meeting", Minutes = 30, IntervalMinutes = 5, AccentColor = "#7C8CF7", PixelColor = "#A855F7" },
        new() { Name = "Studying", Minutes = 25, IntervalMinutes = 5, AccentColor = "#C6A5FF", PixelColor = "#A67AF2" }
    ];
    public bool FocusNudgesEnabled { get; set; }
    public bool InterfaceSoundsEnabled { get; set; }
    public double InterfaceSoundVolume { get; set; } = .25;
    public string PixelShape { get; set; } = "Square";
    public bool DistractionEscalationEnabled { get; set; } = true;
    public bool FocusPulseEnabled { get; set; }
    public double FocusPulseIntensity { get; set; } = .85;
    public Session? Session { get; set; }
    public string Notes { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = [];
    public bool ShowNotes { get; set; } = true;
    public bool ShowChecklist { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool Compact { get; set; }
    public string AccentColor { get; set; } = "#5B7CFF";
    public string PixelColor { get; set; } = "#8B5CF6";
    public string StoryNode { get; set; } = "arrival";
    public List<string> StoryHistory { get; set; } = [];
    public List<string> StoryEndings { get; set; } = [];
    public DashboardDraft Draft { get; set; } = new();
    public double WindowWidth { get; set; } = 1220;
    public double WindowHeight { get; set; } = 880;
    public bool WidgetLayoutInitialized { get; set; }
    public List<WidgetInstance> Widgets { get; set; } = [];
    public SpaceBackground Background { get; set; } = new();
}

public enum SpaceBackgroundMode { Default, SolidColor, CustomImage }
public sealed class SpaceBackground
{
    public SpaceBackgroundMode Mode { get; set; }
    public string SolidColor { get; set; } = "#080B16";
    public string ImagePath { get; set; } = "";
    public bool Blur { get; set; }
    public double BlurStrength { get; set; } = 12;
    public double Overlay { get; set; } = .42;
}

public sealed class AppState : DashboardData
{
    [System.Text.Json.Serialization.JsonIgnore]
    public WorkspaceDocument? Workspace { get; set; }
    public string HedyRegion { get; set; } = "US";
    public string HedyProtectedKey { get; set; } = "";
}

public static class InputRules
{
    public static bool IsColor(string? color) => color != null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
    public static bool ValidMinutes(string text, out int minutes) => int.TryParse(text, out minutes) && minutes is >= 1 and <= 10080;
    public static bool ValidSession(Session session) => !string.IsNullOrWhiteSpace(session.Title) && session.End > session.Start && (session.End - session.Start).TotalDays <= 7 && session.IntervalMinutes is >= 1 and <= 10080;
}

public sealed partial class StateStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly object writeLock = new();
    private long nextWriteSequence, committedWriteSequence;
    private bool preserveUnreadable;
    private bool blocked;
    private string? writeBlockReason;
    public string FilePath { get; }
    public string UserId { get; }
    // Explicit host capability, never inferred from saved bug preferences or selection.json.
    internal bool GameContext { get; }
    public string? Warning { get; private set; }
    private string PreferencesPath => ScopedPreferencePath("preferences.json");
    public StateStore(string path, string? userId = null, bool gameContext = false)
    {
        FilePath = path;
        GameContext = gameContext;
        UserId = userId ?? System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
    }

    private string InteractionPreferencesPath => ScopedPreferencePath("interaction-preferences.json");
    internal void LoadInteractionPreferences(DashboardData state)
    {
        try
        {
            if (!File.Exists(InteractionPreferencesPath)) return;
            var preferences = JsonSerializer.Deserialize<InteractionPreferences>(File.ReadAllText(InteractionPreferencesPath), Options);
            if (preferences == null) return;
            state.InterfaceSoundsEnabled=preferences.Sounds; state.FocusNudgesEnabled=preferences.Nudges;
            state.InterfaceSoundVolume=double.IsFinite(preferences.Volume) ? Math.Clamp(preferences.Volume,0,1) : .25;
            state.PixelShape=preferences.PixelShape == "Circle" ? "Circle" : "Square";
            state.DistractionEscalationEnabled=preferences.DistractionEscalation;
            state.FocusPulseEnabled=preferences.FocusPulse;
            state.FocusPulseIntensity=double.IsFinite(preferences.PulseIntensity) ? Math.Clamp(preferences.PulseIntensity,.5,.95) : .85;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    internal void SaveInteractionPreferences(DashboardData state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(InteractionPreferencesPath)!);
        AtomicWrite(InteractionPreferencesPath,JsonSerializer.Serialize(new InteractionPreferences { Sounds=state.InterfaceSoundsEnabled,Nudges=state.FocusNudgesEnabled,Volume=state.InterfaceSoundVolume,PixelShape=state.PixelShape,DistractionEscalation=state.DistractionEscalationEnabled,FocusPulse=state.FocusPulseEnabled,PulseIntensity=state.FocusPulseIntensity },Options),false);
    }
    private sealed class InteractionPreferences
    {
        public bool Sounds { get; set; }
        public bool Nudges { get; set; }
        public double Volume { get; set; } = .25;
        public string PixelShape { get; set; } = "Square";
        public bool DistractionEscalation { get; set; } = true;
        public bool FocusPulse { get; set; }
        public double PulseIntensity { get; set; } = .85;
    }
    public ThemeMode LoadThemeMode()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return ThemeMode.System;
            var preferences = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PreferencesPath), Options);
            return Enum.TryParse(preferences?.GetValueOrDefault("ThemeMode"), true, out ThemeMode mode) ? mode : ThemeMode.System;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return ThemeMode.System; }
    }

    public AppTheme LoadAppTheme()
    {
        try
        {
            if (!File.Exists(PreferencesPath)) return AppTheme.Nexa;
            var preferences = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PreferencesPath), Options);
            return Enum.TryParse(preferences?.GetValueOrDefault("AppTheme"), true, out AppTheme theme) ? theme : AppTheme.Nexa;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return AppTheme.Nexa; }
    }

    public void SaveThemeMode(ThemeMode mode)
    {
        SavePreferences(mode, LoadAppTheme());
    }

    public void SavePreferences(ThemeMode mode, AppTheme theme)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
        var preferences = ReadPreferenceValues();
        preferences["ThemeMode"] = mode.ToString(); preferences["AppTheme"] = theme.ToString();
        AtomicWrite(PreferencesPath, JsonSerializer.Serialize(preferences, Options), false);
    }

    private Dictionary<string, string> ReadPreferenceValues()
    {
        try { return File.Exists(PreferencesPath) ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PreferencesPath), Options) ?? new() : new(); }
        catch (JsonException ex) { throw new IOException("Saved preferences could not be read; the original file was preserved.", ex); }
    }
    internal BugPreferences LoadBugPreferences()
    {
        try
        {
            var values = ReadPreferenceValues();
            return new(values.GetValueOrDefault("AddBugs") == "True",
                Enum.TryParse<BugSpecies>(values.GetValueOrDefault("BugSpecies"), out var species) && Enum.IsDefined(species) ? species==BugSpecies.AllThree ? BugSpecies.AllBugs : species : BugSpecies.Ants,
                values.GetValueOrDefault("BugDensity") == "Infestation" && values.GetValueOrDefault("BugDensitySchema") != "2" ? BugDensity.High : Enum.TryParse<BugDensity>(values.GetValueOrDefault("BugDensity"), out var density) && Enum.IsDefined(density) ? density : BugDensity.Normal,
                values.TryGetValue("EcosystemSettings",out var eco) ? EcosystemOptions.Existing(JsonSerializer.Deserialize<EcosystemOptions>(eco,Options) ?? new()).Safe() : new());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    internal void SaveBugPreferences(BugPreferences bugs)
    {
        var values = ReadPreferenceValues();
        values["EcosystemSettings"] = JsonSerializer.Serialize((bugs.Ecosystem ?? new()).Safe(),Options);
        values["BugDensitySchema"] = "2"; values["AddBugs"] = bugs.Enabled.ToString(); values["BugSpecies"] = bugs.Species.ToString(); values["BugDensity"] = bugs.Density.ToString();
        Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
        AtomicWrite(PreferencesPath, JsonSerializer.Serialize(values, Options), false);
    }

    public AppState Load()
    {
        // A UI refresh/reopen must not interpret a concurrent checkpoint's file
        // replacement as an unreadable workspace and fall back to empty defaults.
        lock (writeLock) return LoadCommitted();
    }
    internal string ReadHistoryText(string path)
    {
        lock (writeLock)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
    private AppState LoadCommitted()
    {
        Warning = null; blocked = false; writeBlockReason = null;
        foreach (var candidate in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                var json = File.ReadAllText(candidate);
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("FormatVersion", out _))
                {
                    var workspace = HistoryArchive.LoadWorkspace(json, UserId, out var recoveredHistory);
                    if (recoveredHistory) { preserveUnreadable = true; Warning = "Damaged archive entries are preserved for recovery; readable Spaces and reports are available."; }
                    var recoveredSnapshot = Continuity.RecoverSnapshots(workspace, UserId);
                    Continuity.ValidateWorkspace(workspace, UserId);
                    if (!workspace.CleanShutdown) Warning = $"Nexa restored your last autosaved session from {workspace.SavedAt.LocalDateTime:g}.";
                    if (recoveredSnapshot) { preserveUnreadable = true; Warning = "Nexa recovered damaged Space data from its latest valid autosave snapshot."; }
                    if (candidate != FilePath) Warning = "Recovered your last backup after the save file could not be read.";
                    var loaded = Continuity.Activate(workspace, UserId);
                    LoadCredentials(loaded);
                    return loaded;
                }
                var state = JsonSerializer.Deserialize<AppState>(json, Options) ?? throw new JsonException("Empty settings.");
                if (state.Presets == null || state.Tasks == null || state.StoryHistory == null || state.StoryEndings == null) throw new JsonException("Incomplete settings.");
                state.Presets.RemoveAll(p => p == null || string.IsNullOrWhiteSpace(p.Name) || p.Minutes is < 1 or > 10080 || p.IntervalMinutes is < 1 or > 10080);
                state.Tasks.RemoveAll(t => t == null || string.IsNullOrWhiteSpace(t.Text));
                state.Notes ??= "";
                state.Background ??= new();
                state.Background.BlurStrength = Math.Clamp(state.Background.BlurStrength, 0, 40);
                state.Background.Overlay = Math.Clamp(state.Background.Overlay, 0, .9);
                if (state.Session != null && !InputRules.ValidSession(state.Session)) state.Session = null;
                foreach (var preset in state.Presets)
                {
                    preset.ScheduledDays ??= [];
                    var theme = Theme.ForName(preset.Name);
                    if (!InputRules.IsColor(preset.AccentColor)) preset.AccentColor = theme.Accent;
                    if (!InputRules.IsColor(preset.PixelColor)) preset.PixelColor = theme.Pixel;
                    if (preset.StartClockMinutes is < 0 or > 1439) preset.StartClockMinutes = null;
                    if (preset.EndClockMinutes is < 0 or > 1439) preset.EndClockMinutes = null;
                    preset.EndDayOffset = Math.Clamp(preset.EndDayOffset, 0, 7);
                }
                if (state.Session is { } session)
                {
                    session.ScheduledDays ??= [];
                    if (session.ConfiguredDurationMinutes <= 0) session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
                    session.ReachedMilestones ??= [];
                    session.IntervalNotes ??= [];
                    session.HedyIntervalNotes ??= [];
                    session.IntervalSummaries ??= [];
                    session.HedyTranscript ??= "";
                    session.HedyBaseline ??= "";
                }
                if (!InputRules.IsColor(state.AccentColor)) state.AccentColor = "#5B7CFF";
                if (!InputRules.IsColor(state.PixelColor)) state.PixelColor = "#8B5CF6";
                if (candidate != FilePath) Warning = "Recovered your last backup after the settings file could not be read.";
                return state;
            }
            catch (UnauthorizedAccessException)
            { blocked = true; throw; }
            catch (Exception ex) when (HistoryArchive.Recoverable(ex))
            { preserveUnreadable = true; Warning = "Settings could not be read. Defaults loaded; the original file will be preserved on the next save."; }
        }
        if (preserveUnreadable)
        {
            blocked = true;
            writeBlockReason = "The existing workspace cannot be read. Its files are preserved; recover a readable copy before saving.";
            Warning = writeBlockReason;
        }
        return new AppState();
    }

    public void Save(AppState state)
    {
        Write(Prepare(state));
    }
    // Called on the UI thread. Background writes receive a detached immutable capture.
    internal SavePayload Prepare(AppState state)
    {
        if (blocked) throw new UnauthorizedAccessException(writeBlockReason ?? "Saving this workspace is blocked because its owner could not be verified.");
        Continuity.CaptureCurrent(state, UserId, DateTimeOffset.Now);
        Continuity.ValidateWorkspace(state.Workspace!, UserId);
        return new(Continuity.Clone(state.Workspace!), state.HedyRegion, state.HedyProtectedKey, Interlocked.Increment(ref nextWriteSequence));
    }
    internal void Write(SavePayload payload)
    {
        lock (writeLock)
        {
            if (payload.Sequence > 0 && payload.Sequence < committedWriteSequence) throw new InvalidOperationException("A newer save has already completed.");
            Continuity.ValidateWorkspace(payload.Workspace, UserId);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
            if (preserveUnreadable && File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".recovered-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), true);
            // Keep the DPAPI-protected Hedy credential outside all dashboard and history payloads.
            var credentials = JsonSerializer.Serialize(new Credentials { UserId = UserId, Region = payload.Region, ProtectedKey = payload.ProtectedKey }, Options);
            var credentialPath = FilePath + ".credentials";
            if (!File.Exists(credentialPath) || File.ReadAllText(credentialPath) != credentials) AtomicWrite(credentialPath, credentials, false);
            AtomicWrite(FilePath, JsonSerializer.Serialize(payload.Workspace, Options), !preserveUnreadable);
            committedWriteSequence = Math.Max(committedWriteSequence, payload.Sequence);
            preserveUnreadable = false; Warning = null;
        }
    }
    internal static void AtomicWrite(string path, string text, bool backup)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 4096, true);
                writer.Write(text); writer.Flush(); stream.Flush(true);
            }
            if (File.Exists(path))
            {
                for(var attempt=0;;attempt++)
                {
                    try{File.Replace(temp,path,backup ? path+".bak":null);break;}
                    catch(IOException ex) when(attempt<4 && (ex.HResult&65535) is 32 or 33 or 1175)
                    {System.Threading.Thread.Sleep(10<<attempt);}
                }
            }
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private void LoadCredentials(AppState state)
    {
        try
        {
            var path = FilePath + ".credentials";
            if (!File.Exists(path)) return;
            var value = JsonSerializer.Deserialize<Credentials>(File.ReadAllText(path));
            if (value?.UserId != UserId) return;
            state.HedyRegion = value.Region; state.HedyProtectedKey = value.ProtectedKey;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { Warning = "Your Space was restored. Reconnect Hedy because its saved credential could not be read."; }
    }
    internal sealed record SavePayload(WorkspaceDocument Workspace, string Region, string ProtectedKey, long Sequence = 0);
    private sealed class Credentials
    {
        public string UserId { get; set; } = "";
        public string Region { get; set; } = "US";
        public string ProtectedKey { get; set; } = "";
    }
}