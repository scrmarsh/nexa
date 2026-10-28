using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SessionDashboard;

public sealed class DashboardDraft
{
    public bool Initialized { get; set; }
    public string Title { get; set; } = "Focused work";
    public string Duration { get; set; } = "50";
    public string Interval { get; set; } = "10";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public int StartMinutes { get; set; }
    public int EndMinutes { get; set; }
    public string TaskInput { get; set; } = "";
    public Guid? SelectedPresetId { get; set; }
    public List<DayOfWeek> ScheduledDays { get; set; } = [];
}

public sealed class WorkspaceDocument
{
    public NexaOptions Settings { get; set; } = new();
    public long AllTimeSquashes { get; set; }
    public Dictionary<string,long> AllTimeSquashesBySpecies {get;set;} = [];
    public long AllTimeProductivityScore { get; set; }
    public int FormatVersion { get; set; } = 2;
    public string UserId { get; set; } = "";
    public Guid ActiveDashboardId { get; set; }
    public bool CleanShutdown { get; set; } = true;
    public DateTimeOffset SavedAt { get; set; }
    public List<DashboardRecord> Dashboards { get; set; } = [];
}

public sealed class DashboardRecord
{
    public bool SpaceScoreInitialized {get;set;}
    public long SpaceProductivityScore {get;set;}
    public HashSet<Guid> CreditedScoreEvents {get;set;} = [];
    public Guid DashboardId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "My dashboard";
    public string PresetOrigin { get; set; } = "Custom";
    public DashboardSession Current { get; set; } = new();
    public List<DashboardSession> History { get; set; } = [];
    public List<JsonElement> PreservedHistory { get; set; } = [];
    public override string ToString() => Name;
}

public sealed class DashboardSession
{
    public long? ScoreAtStart {get;set;}
    public long? ScoreAtEnd {get;set;}
    public SessionRecap? Recap {get;set;}
    public Guid? PresetId { get; set; }
    public string PresetName { get; set; } = "";
    public string TimeZoneId { get; set; } = "Automatic";
    public DateTimeOffset? ScheduledStart { get; set; }
    public DateTimeOffset? ScheduledEnd { get; set; }
    public DateTimeOffset? CompletedCheckpointAt { get; set; }
    public DateTimeOffset? ResetForStart { get; set; }
    public DateTimeOffset? ResetPostponedFor { get; set; }
    public bool ResetDeferred { get; set; }
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public Guid DashboardId { get; set; }
    public string UserId { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Title { get; set; } = "Untitled session";
    public bool IsActive { get; set; } = true;
    public bool WasReset { get; set; }
    public Guid? ContinuedFrom { get; set; }
    public List<Guid> MergedFrom { get; set; } = [];
    public DashboardData Data { get; set; } = new();
    public List<AutosaveSnapshot> Snapshots { get; set; } = [];
    [JsonIgnore] public string SessionDate => OpenedAt.LocalDateTime.ToString("MMMM d, yyyy");
    [JsonIgnore] public string Summary => $"{Data.Tasks.Count} tasks · {Data.Notes.Length} note characters";
    [JsonIgnore] public string Display => $"{OpenedAt.LocalDateTime:h:mm tt} — {Title}\n{Summary} · {(IsActive ? "Current" : WasReset ? "Reset / archived" : "Archived")}\nLast accessed {LastAccessedAt.LocalDateTime:g}";
}

public sealed class AutosaveSnapshot
{
    public Guid SnapshotId { get; set; } = Guid.NewGuid();
    public DateTimeOffset SavedAt { get; set; }
    public DashboardData Data { get; set; } = new();
}

// Portable schema deliberately has no owner IDs, history, or credential properties.
public sealed class DashboardExport
{
    public int FormatVersion { get; set; } = 1;
    public string Application { get; set; } = "Nexa";
    public string DashboardName { get; set; } = "";
    public string PresetOrigin { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public bool IncludesSessionData { get; set; }
    public DashboardData Data { get; set; } = new();
}

public static class Continuity
{
    internal static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions ImportJson = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    public static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Json), Json)!;
    public static DashboardData Capture(AppState state) => Clone<DashboardData>(state);
    public static AppState Activate(WorkspaceDocument workspace, string userId, string region = "US", string key = "")
    {
        var record = Current(workspace, userId);
        var state = JsonSerializer.Deserialize<AppState>(JsonSerializer.Serialize(record.Current.Data, Json), Json)!;
        state.Workspace = workspace; state.HedyRegion = region; state.HedyProtectedKey = key;
        return state;
    }
    public static DashboardRecord Current(WorkspaceDocument workspace, string userId)
    {
        if (workspace.UserId != userId) throw new UnauthorizedAccessException("This workspace belongs to another Windows user.");
        return workspace.Dashboards.Single(d => d.DashboardId == workspace.ActiveDashboardId && d.UserId == userId);
    }
    public static WorkspaceDocument Migrate(AppState state, string userId)
    {
        var workspace = new WorkspaceDocument { UserId = userId };
        var dashboard = NewDashboard("My dashboard", "Legacy workspace", Capture(state), userId);
        workspace.Dashboards.Add(dashboard); workspace.ActiveDashboardId = dashboard.DashboardId;
        return workspace;
    }
    public static DashboardRecord NewDashboard(string name, string origin, DashboardData data, string userId)
    {
        data.InitialViewPending=true;
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new InvalidDataException("Dashboard name must contain 1–100 characters.");
        var record = new DashboardRecord { UserId = userId, Name = name.Trim(), PresetOrigin = origin };
        record.Current = NewSession(record, data);
        return record;
    }
    private static DashboardSession NewSession(DashboardRecord dashboard, DashboardData data) => new()
    {
        DashboardId = dashboard.DashboardId, UserId = dashboard.UserId, Data = Clone(data),
        Title = data.Session?.Title ?? (string.IsNullOrWhiteSpace(data.Draft.Title) ? dashboard.Name : data.Draft.Title)
    };
    public static void CaptureCurrent(AppState state, string userId, DateTimeOffset now)
    {
        state.Workspace ??= Migrate(state, userId);
        var session = Current(state.Workspace, userId).Current;
        var priorAwards=session.Data.Productivity.Events.Select(e=>e.Id).ToHashSet();
        ProductivityScoring.Observe(session.Data, state, now,state.Workspace.Settings);
        state.Workspace.AllTimeProductivityScore += state.Productivity.Events.Where(e=>!priorAwards.Contains(e.Id)).Sum(e=>e.BasePoints+e.BonusPoints);
        if (session.ScheduledStart == null && session.PresetName.Length == 0)
        {
            session.PresetId = state.Draft.SelectedPresetId;
            session.PresetName = state.Presets.FirstOrDefault(p => p.Id == session.PresetId)?.Name ?? state.Session?.Title ?? "";
        }
        session.TimeZoneId = state.Session?.TimeZoneId ?? state.Workspace.Settings.TimeZoneId;
        session.ScheduledStart = state.Session?.Start; session.ScheduledEnd = state.Session?.End;
        session.Data = Capture(state); SpaceScores.Reconcile(Current(state.Workspace,userId)); session.UpdatedAt = now.ToUniversalTime();
        session.Title = state.Session?.Title ?? state.Draft.Title;
        // Only current sessions retain crash-recovery generations; history is a single immutable state.
        session.Snapshots.Add(new() { SavedAt = now, Data = Clone(session.Data) });
        if (session.Snapshots.Count > 3) session.Snapshots.RemoveRange(0, session.Snapshots.Count - 3);
        state.Workspace.SavedAt = now;
    }
    public static void Access(DashboardRecord dashboard)
    {
        dashboard.Current.LastAccessedAt = DateTimeOffset.Now;
        dashboard.Current.ClosedAt = null;
    }
    public static void Archive(DashboardRecord dashboard, bool reset = false)
    {
        SpaceScores.Reconcile(dashboard);
        var archived = Clone(dashboard.Current);
        archived.IsActive = false; archived.WasReset = reset; archived.ClosedAt = DateTimeOffset.Now;
        archived.Snapshots.Clear();
        dashboard.History.Add(archived);
    }
    public static DashboardData ConfigurationOnly(DashboardData source)
    {
        var data = Clone(source);
        data.Session = null; data.Notes = ""; data.Tasks.Clear(); data.Productivity = new();
        data.StoryNode = "arrival"; data.StoryHistory.Clear(); data.StoryEndings.Clear();
        foreach (var widget in data.Widgets) WidgetLibrary.ClearContent(widget);
        // Draft titles and dates can contain private meeting information, so exports use defaults.
        data.Draft = new DashboardDraft
        {
            Initialized = true, Duration = source.Draft.Duration, Interval = source.Draft.Interval,
            StartMinutes = source.Draft.StartMinutes, EndMinutes = source.Draft.EndMinutes,
            SelectedPresetId = source.Draft.SelectedPresetId
        };
        return data;
    }
    public static void Reset(DashboardRecord dashboard)
    {
        var blank = WeeklySchedule.ResetSessionData(dashboard.Current.Data);
        blank.Draft.Title = dashboard.Name;
        Archive(dashboard, true);
        dashboard.Current = NewSession(dashboard, blank);
    }
    private static DashboardSession Historical(DashboardRecord dashboard, Guid sessionId, string userId)
    {
        if (dashboard.UserId != userId) throw new UnauthorizedAccessException();
        return dashboard.History.Single(s => s.SessionId == sessionId && s.UserId == userId && s.DashboardId == dashboard.DashboardId);
    }
    public static void Continue(DashboardRecord dashboard, Guid sessionId, string userId)
    {
        var source = Historical(dashboard, sessionId, userId);
        var data = Clone(source.Data);
        Archive(dashboard);
        dashboard.Current = NewSession(dashboard, data);
        dashboard.Current.ContinuedFrom = source.SessionId;
        source.LastAccessedAt = DateTimeOffset.Now;
    }
    private static string Append(string current, string incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming) || current.Contains(incoming, StringComparison.Ordinal)) return current;
        return string.IsNullOrWhiteSpace(current) ? incoming : current + "\n\n" + incoming;
    }
    public static void Merge(DashboardRecord dashboard, Guid sessionId, string userId, bool includeConfiguration)
    {
        var source = Historical(dashboard, sessionId, userId);
        var target = dashboard.Current;
        if (target.MergedFrom.Contains(sessionId)) return;
        var data = target.Data;
        data.Notes = Append(data.Notes, source.Data.Notes);
        foreach (var task in source.Data.Tasks.Where(t => !t.Done))
            if (!data.Tasks.Any(t => string.Equals(t.Text.Trim(), task.Text.Trim(), StringComparison.OrdinalIgnoreCase))) data.Tasks.Add(Clone(task));
        foreach (var incoming in source.Data.Widgets)
        {
            var existing = data.Widgets.FirstOrDefault(w => w.Id == incoming.Id && w.Kind == incoming.Kind);
            if (existing == null && incoming.PanelKey.Length > 0) existing = data.Widgets.FirstOrDefault(w => w.PanelKey == incoming.PanelKey);
            if (existing == null) { WidgetLibrary.Add(data, Clone(incoming)); continue; }
            existing.Text = Append(existing.Text, incoming.Text);
            foreach (var item in incoming.Items.Where(t => !t.Done))
                if (!existing.Items.Any(t => string.Equals(t.Text.Trim(), item.Text.Trim(), StringComparison.OrdinalIgnoreCase))) existing.Items.Add(Clone(item));
            foreach (var record in incoming.Records)
                if (!existing.Records.Any(r => r.Id == record.Id)) existing.Records.Add(Clone(record));
            foreach (var pin in incoming.ScripturePins)
                if (!existing.ScripturePins.Any(p => p.Id == pin.Id)) existing.ScripturePins.Add(Clone(pin));
            existing.Categories = existing.Categories.Concat(incoming.Categories).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            existing.Fields = existing.Fields.Concat(incoming.Fields).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            // Counters and independent timer positions stay current; Continue restores them exactly.
            if (includeConfiguration)
            { existing.Column = incoming.Column; existing.Row = incoming.Row; existing.Span = incoming.Span; existing.Height = incoming.Height; existing.Title = incoming.Title; }
        }
        // Different timers have different interval coordinates. Keep their notes with source labels.
        if (source.Data.Session is { } timer)
        {
            var details = string.Join("\n", timer.IntervalNotes.OrderBy(p => p.Key).Select(p => $"Interval {p.Key + 1}: {p.Value}")
                .Concat(timer.HedyIntervalNotes.OrderBy(p => p.Key).Select(p => $"Hedy interval {p.Key + 1}: {p.Value}")));
            details = Append(details, timer.HedyTranscript);
            details = Append(details, string.Join("\n", timer.TimelineEntries.Select(e => $"{e.CreatedAt.LocalDateTime:g} · {e.Source}: {e.Text}")));
            if (!string.IsNullOrWhiteSpace(details)) data.Notes = Append(data.Notes, $"From {source.Title} ({source.OpenedAt.LocalDateTime:g})\n{details}");
        }
        if (includeConfiguration)
        {
            var merged = Clone(source.Data);
            merged.Notes = data.Notes; merged.Tasks = data.Tasks; merged.Session = data.Session;
            merged.StoryNode = data.StoryNode; merged.StoryHistory = data.StoryHistory; merged.StoryEndings = data.StoryEndings;
            merged.Draft = data.Draft;
            merged.Widgets = data.Widgets; merged.WidgetLayoutInitialized = data.WidgetLayoutInitialized;
            target.Data = merged;
        }
        target.MergedFrom.Add(sessionId); source.LastAccessedAt = DateTimeOffset.Now;
    }
    public static string Export(DashboardRecord record, bool includeData, DashboardSession? session = null)
    {
        if (session != null && (session.DashboardId != record.DashboardId || session.UserId != record.UserId)) throw new UnauthorizedAccessException();
        return JsonSerializer.Serialize(new DashboardExport
        {
            DashboardName = record.Name, PresetOrigin = record.PresetOrigin, IncludesSessionData = includeData,
            Data = includeData ? Clone((session ?? record.Current).Data) : ConfigurationOnly((session ?? record.Current).Data)
        }, Json);
    }
    public static DashboardExport ReadExport(string json)
    {
        if (json.Length > 10_000_000) throw new InvalidDataException("Dashboard file exceeds the 10 MB limit.");
        var file = JsonSerializer.Deserialize<DashboardExport>(json, ImportJson) ?? throw new InvalidDataException("Empty dashboard file.");
        if (file.FormatVersion != 1 || (file.Application != "Nexa" && file.Application != "Nexa Session Dashboard") || string.IsNullOrWhiteSpace(file.DashboardName) || file.DashboardName.Length > 100)
            throw new InvalidDataException("Unsupported or invalid Nexa dashboard file.");
        ValidateData(file.Data);
        if (!file.IncludesSessionData) file.Data = ConfigurationOnly(file.Data);
        return file;
    }
    public static DashboardRecord Import(WorkspaceDocument workspace, DashboardExport file, string userId, bool acceptPrivateData)
    {
        Current(workspace, userId);
        if (file.IncludesSessionData && !acceptPrivateData) throw new InvalidOperationException("Import of private session data requires confirmation.");
        ValidateData(file.Data);
        var record = NewDashboard(file.DashboardName, file.PresetOrigin, file.Data, userId);
        // Presets become owned definitions too, with fresh identities.
        foreach (var preset in record.Current.Data.Presets) preset.Id = Guid.NewGuid();
        foreach (var widget in record.Current.Data.Widgets) widget.Id = Guid.NewGuid();
        record.Current.Data.Draft.SelectedPresetId = null;
        workspace.Dashboards.Add(record); workspace.ActiveDashboardId = record.DashboardId;
        return record;
    }
    internal static void ValidateData(DashboardData? data)
    {
        if (data == null || data.Presets == null || data.Tasks == null || data.StoryHistory == null || data.StoryEndings == null || data.Draft == null ||
            data.Notes == null || data.StoryNode == null || !InputRules.IsColor(data.AccentColor) || !InputRules.IsColor(data.PixelColor) ||
            !double.IsFinite(data.WindowWidth) || !double.IsFinite(data.WindowHeight) || data.WindowWidth is < 300 or > 20000 || data.WindowHeight is < 200 or > 20000 ||
            data.Draft.Title == null || data.Draft.Duration == null || data.Draft.Interval == null || data.Draft.TaskInput == null ||
            data.Draft.StartDate.Date == DateTime.MaxValue.Date || data.Draft.EndDate.Date == DateTime.MaxValue.Date ||
            data.Draft.StartMinutes is < 0 or > 1439 || data.Draft.EndMinutes is < 0 or > 1439 ||
            data.Tasks.Any(t => t == null || t.Text == null) || data.Presets.Any(p => p == null || string.IsNullOrWhiteSpace(p.Name) || p.Minutes is < 1 or > 10080 || p.IntervalMinutes is < 1 or > 10080 ||
                !InputRules.IsColor(p.AccentColor) || !InputRules.IsColor(p.PixelColor) || p.StartClockMinutes is < 0 or > 1439 || p.EndClockMinutes is < 0 or > 1439 || p.EndDayOffset is < 0 or > 7))
            throw new InvalidDataException("Dashboard data contains missing or invalid values.");
        if (data.Session is { } s && (!InputRules.ValidSession(s) || s.IntervalNotes == null || s.HedyIntervalNotes == null || s.IntervalSummaries == null || s.ReachedMilestones == null || s.HedyTranscript == null || s.HedyBaseline == null))
            throw new InvalidDataException("Dashboard timer or interval notes are invalid.");
        if (data.Session is { } recurrence && (recurrence.RecurringStartMinutes is < 0 or > 1439 || recurrence.RecurringEndMinutes is < 0 or > 1439 || recurrence.RecurringEndDayOffset is < 0 or > 7)) throw new InvalidDataException("Invalid recurrence clock values.");
        if (data.Session is { } session && (session.TimelineEntries == null || session.TimelineEntries.Count > 10000 || session.TimelineEntries.Any(e => e == null || e.Id == Guid.Empty || e.Text == null || e.Source == null || e.AudioPath == null || e.Text.Length > 1000000 || e.Source.Length > 100 || e.AudioPath.Length > 32768)))
            throw new InvalidDataException("Session timeline entries are invalid.");
        if (data.Productivity == null || data.Productivity.Events == null || data.Productivity.CounterHighWater == null || data.Productivity.SquashesBySpecies==null || data.Productivity.SquashesBySpecies.Any(p=>!BugManager.AllSpecies.Any(s=>s.ToString()==p.Key) || p.Value<0) ||
            !double.IsFinite(data.Productivity.FrenzyMultiplier) || data.Productivity.FrenzyMultiplier is < 1 or > 10 || data.Productivity.Events.Count > 10000 || data.Productivity.Events.Any(e => e == null || e.Key == null || e.Category == null || e.Description == null || e.BasePoints is < -1000 or > 1000 || e.BonusPoints is < 0 or > 9000 || e.BasePoints<0 && (e.Category is not ("Distraction" or "Alcohol") || e.BonusPoints!=0)) ||
            data.Productivity.Events.Select(e => e.Id).Distinct().Count() != data.Productivity.Events.Count)
            throw new InvalidDataException("Invalid productivity activity.");
        if (data.Widgets == null || data.Widgets.Any(w => w == null || w.IntegrationSnapshots == null || w.HiddenProductivityCategories == null || w.HiddenProductivityCategories.Count > 100 ||
            w.IntegrationSnapshots.Count > 12 || w.IntegrationSnapshots.Any(s => s == null || s.Service == null || s.ListName == null || s.Fingerprint == null || s.Items == null || s.Items.Count > 200 || s.TotalItems < 0 || s.Items.Any(i => i == null || i.Id == null || i.Title == null || i.Status == null))))
            throw new InvalidDataException("Invalid widget activity snapshots.");
        WidgetLibrary.Validate(data);
    }
    internal static bool RecoverSnapshots(WorkspaceDocument workspace, string userId)
    {
        if (workspace.UserId != userId) throw new UnauthorizedAccessException("This workspace belongs to another Windows user.");
        var recovered = false;
        foreach (var dashboard in workspace.Dashboards ?? [])
        {
            if (dashboard?.Current is not { } current || current.Snapshots == null) continue;
            if (dashboard.UserId != userId || current.UserId != userId || current.DashboardId != dashboard.DashboardId) throw new UnauthorizedAccessException("Invalid snapshot ownership.");
            try { ValidateData(current.Data); }
            catch (InvalidDataException)
            {
                foreach (var snapshot in current.Snapshots.Where(s => s != null).OrderByDescending(s => s.SavedAt))
                {
                    try
                    {
                        ValidateData(snapshot.Data); current.Data = Clone(snapshot.Data); current.UpdatedAt = snapshot.SavedAt;
                        recovered = true; break;
                    }
                    catch (InvalidDataException) { }
                }
            }
            // A damaged older recovery generation must not prevent reading a valid current state.
            current.Snapshots.RemoveAll(snapshot =>
            {
                try { ValidateData(snapshot?.Data); return false; }
                catch (InvalidDataException) { return true; }
            });
        }
        return recovered;
    }
    internal static void ValidateWorkspace(WorkspaceDocument workspace, string userId)
    {
        if (workspace.Settings == null || workspace.AllTimeSquashes < 0 || workspace.AllTimeSquashesBySpecies==null || workspace.AllTimeSquashesBySpecies.Any(p=>!BugManager.AllSpecies.Any(s=>s.ToString()==p.Key) || p.Value<0)) throw new InvalidDataException("Invalid workspace settings.");
        if (workspace.FormatVersion != 2) throw new NotSupportedException("This workspace was saved by an unsupported version of Nexa. Use that version to open it.");
        if (workspace.UserId != userId) throw new UnauthorizedAccessException("This workspace belongs to another Windows user.");
        if (workspace.Dashboards == null || workspace.Dashboards.Count == 0 || workspace.Dashboards.Any(d => d == null) || workspace.Dashboards.Select(d => d.DashboardId).Distinct().Count() != workspace.Dashboards.Count)
            throw new InvalidDataException("Invalid dashboard identities.");
        foreach (var d in workspace.Dashboards)
        {
            if (d.UserId != userId) throw new UnauthorizedAccessException("Dashboard owner does not match the current user.");
            if (d.DashboardId == Guid.Empty || d.Current == null || d.History == null || string.IsNullOrWhiteSpace(d.Name)) throw new InvalidDataException("Invalid dashboard identity.");
            var sessions = d.History.Prepend(d.Current).ToList();
            if (sessions.Any(s => s == null)) throw new InvalidDataException("Missing session record.");
            if (sessions.Select(s => s.SessionId).Distinct().Count() != sessions.Count) throw new InvalidDataException("Duplicate session identities.");
            foreach (var s in sessions)
            {
                if (s.UserId != userId || s.DashboardId != d.DashboardId) throw new UnauthorizedAccessException("Session owner or dashboard does not match.");
                if (s.SessionId == Guid.Empty || s.Snapshots == null || s.MergedFrom == null) throw new InvalidDataException("Invalid session identity.");
                ValidateData(s.Data);
                foreach (var snapshot in s.Snapshots) ValidateData(snapshot?.Data);
            }
        }
        if (!workspace.Dashboards.Any(d => d.DashboardId == workspace.ActiveDashboardId)) throw new InvalidDataException("Missing active dashboard.");
    }
}
