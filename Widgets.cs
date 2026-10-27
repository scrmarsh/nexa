using System.IO;

namespace SessionDashboard;

public sealed class WidgetInstance
{
    public bool SessionScoped { get; set; } = true;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "Notes";
    public string Title { get; set; } = "Notes";
    public string PanelKey { get; set; } = "";
    public bool IsMicro { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
    public double? PlacementX { get; set; }
    public double PlacementY { get; set; }
    public int Span { get; set; } = 6;
    public int GridColumns { get; set; } = 12;
    public bool CustomSize { get; set; }
    public int Height { get; set; } = 288;
    public string Placeholder { get; set; } = "Write something…";
    public decimal Increment { get; set; } = 1;
    public decimal StartingValue { get; set; }
    public int TimerMinutes { get; set; } = 25;
    public int? SummaryIntervalMinutes {get;set;}
    public int? TimerDurationSeconds { get; set; }
    public double StopwatchElapsedSeconds { get; set; }
    public DateTimeOffset? StopwatchStartedAt { get; set; }
    public List<double> StopwatchLaps { get; set; } = [];
    [System.Text.Json.Serialization.JsonIgnore]
    public int DurationSeconds => TimerDurationSeconds ?? TimerMinutes * 60;
    public bool ShowSeconds { get; set; } = true;
    public bool ShowElapsed { get; set; }
    public string ExternalListId { get; set; } = "";
    public string CalendarService { get; set; } = "Google Calendar";
    public string IntegrationView { get; set; } = "Today";
    public List<string> Categories { get; set; } = [];
    public List<string> Fields { get; set; } = [];
    public string Text { get; set; } = "";
    public List<string> CalculationHistory { get; set; } = [];
    public decimal Value { get; set; }
    public int WaterBottleGoal { get; set; } = 8;
    public int BeerGoal { get; set; } = 1;
    public int BeerLoggedSinceReminder { get; set; }
    public string BeverageVessel { get; set; } = "Beer Bottle";
    public DistributionTrackerState? Distribution { get; set; }
    public int BannerSpan { get; set; } = 13;
    public int BannerHeight { get; set; } = 360;
    public string CounterMode { get; set; } = "Standard";
    public string DisplayMode { get; set; } = "Auto";
    public List<TaskItem> Items { get; set; } = [];
    public List<TrackerRecord> Records { get; set; } = [];
    public List<ScripturePin> ScripturePins { get; set; } = [];
    public List<IntegrationSnapshot> IntegrationSnapshots { get; set; } = [];
    public List<string> HiddenProductivityCategories { get; set; } = [];
    public BibleReaderState? BibleLocation { get; set; }
    public bool ProgressLinkedToSession { get; set; } = true;
    public DateTimeOffset? TimerStart { get; set; }
    public DateTimeOffset? TimerEnd { get; set; }
    public DateTimeOffset? TimerStopped { get; set; }
    public List<FlashcardDeck> FlashcardDecks { get; set; } = [];
    public Guid? ActiveFlashcardDeckId { get; set; }
    public int FlashcardIndex { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasData => IntegrationSnapshots.Count > 0 || Distribution != null || Text.Length > 0 || Items.Count > 0 || Records.Count > 0 || ScripturePins.Count > 0 || FlashcardDecks.Any(deck => deck.Cards.Count > 0) || Value != StartingValue || TimerStart != null || StopwatchStartedAt != null || StopwatchElapsedSeconds > 0 || StopwatchLaps.Count > 0;
}
public sealed class FlashcardDeck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New deck";
    public List<Flashcard> Cards { get; set; } = [];
}
public sealed class Flashcard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public string StudyState { get; set; } = "New";
}
public sealed class TrackerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<string> Categories { get; set; } = [];
    public Dictionary<string, string> Values { get; set; } = [];
}
public sealed record WidgetDefinition(string Name, string Group, string Description, string Editor, int MinSpan = 3, int MinHeight = 288, int DefaultSpan = 10, int DefaultHeight = 288, int MinWidth = 320, bool CanMinimize = true);

public static class WidgetLibrary
{
    public static IReadOnlyList<WidgetDefinition> Definitions { get; } =
    [
        new("Bugs Squashed", "Bugs", "Direct user squashes in this session and across all sessions.", "squashes", DefaultHeight: 200),
        new("Productivity Meter", "Session", "Meaningful actions and recent activity, separate from Session Timeline.", "productivity", DefaultHeight: 336),
        new("Productivity Score", "Session", "Session points, milestones, category breakdown and Bug Frenzy.", "score", DefaultHeight: 336),
        new("Timer", "Session", "Independent countdown or elapsed timer.", "timer", MinHeight: 192, DefaultSpan: 6, DefaultHeight: 216, MinWidth: 240, CanMinimize: false),
        new("Stopwatch", "Session", "Digital and analog elapsed time with pause and resume.", "stopwatch", MinHeight: 360, DefaultSpan: 6, DefaultHeight: 384, MinWidth: 280, CanMinimize: false),
        new("Clock", "Session", "Your local date and time.", "clock", MinHeight: 96, DefaultSpan: 4, DefaultHeight: 120, MinWidth: 200, CanMinimize: false),
        new("Todoist", "Integrations", "Optional connected tasks.", "externalTasks", MinHeight:288, DefaultHeight:384, CanMinimize:false),
        new("Google Tasks", "Integrations", "Optional connected task lists.", "externalTasks", MinHeight:288, DefaultHeight:384, CanMinimize:false),
        new("Calendar", "Integrations", "Optional Google or Outlook schedule.", "calendar", MinHeight:216, DefaultHeight:240, CanMinimize:false),
        new("Progress bar", "Session", "Progress of your dashboard session.", "progress", MinHeight: 120, DefaultSpan: 6, DefaultHeight: 144, MinWidth: 240, CanMinimize: false),
        new("Pixel timer", "Session", "The original pixel display and story.", "progress", 6, 480, CanMinimize: false),
        new("Session controls", "Session", "Schedule sessions and manage timer presets.", "setup", 4, 480),
        new("Session timeline", "Session", "Manual notes and a timeline for any session, with optional connected sources.", "timeline", 4, 288, DefaultHeight: 360, CanMinimize: false),
        new("Audio Recorder", "Session", "Record locally; keep audio alongside your Session Timeline.", "recorder", MinHeight: 240, DefaultHeight: 288, MinWidth: 280, CanMinimize: false),
        new("Calculator", "Productivity", "Quick calculations with a small recent history.", "calculator", MinHeight: 480, DefaultHeight: 528, MinWidth: 280, CanMinimize: false),
        new("Break timer", "Session", "A short independent countdown.", "timer", MinHeight: 192, DefaultSpan: 6, DefaultHeight: 216, MinWidth: 240, CanMinimize: false),
        new("Focus Reset", "Mindfulness", "A short offline reset to return to your session.", "focus", MinHeight: 120, DefaultHeight: 144, DefaultSpan: 6, MinWidth: 240), new("Notes", "Productivity", "Capture your ideas with autosave.", "text"),
        new("Task List", "Productivity", "A reusable checklist with editable titles and independent items.", "list", MinHeight: 216, DefaultHeight: 288, MinWidth: 260),
        new("Quick links", "Productivity", "Save and open web links.", "links"),
        new("Counter", "Tracking", "A configurable count and increment.", "counter", MinHeight: 192, DefaultSpan: 5, DefaultHeight: 192, MinWidth: 220),
        new("Water Bottle Counter", "Tracking", "Count water bottles in this Space and track a simple goal.", "waterBottles", MinHeight: 216, DefaultSpan: 5, DefaultHeight: 240, MinWidth: 220, CanMinimize: false),
        new("Alcoholic Beverage", "Tracking", "Track beverages with a wine glass, shot glass or beer bottle.", "beer", MinHeight: 216, DefaultSpan: 5, DefaultHeight: 240, MinWidth: 220, CanMinimize: false),
        new("Record tracker", "Tracking", "Records with categories and custom fields.", "tracker", 4, 336),
        new("Distribution Tracker", "Tracking", "Track visits, distributions, attendance, or check-ins with customizable categories and live statistics.", "distribution", MinHeight: 336, DefaultHeight: 480, MinWidth: 280, CanMinimize: false),
        new("Statistics", "Tracking", "Totals from trackers on this dashboard.", "statistics"),
        new("Hedy Notes", "Integrations", "Connect Hedy for live session notes.", "hedy", 4, 480),
        new("Class timeline", "School", "Class intervals and notes.", "timeline", 4, 288),
        new("Bible", "Church", "Read and search the integrated NLT, CSB and NKJV Bibles; select text to pin.", "bible", MinHeight: 480, DefaultHeight: 552, CanMinimize: false),
        new("Scripture pins", "Church", "Save selected Scripture, its translation and your notes.", "scripture", DefaultHeight: 336, CanMinimize: false),
        new("Flashcards", "Study", "Create, import and study offline decks.", "flashcards", MinHeight: 288, DefaultSpan: 10, DefaultHeight: 384, CanMinimize: false),
        new("Website shortcut", "Other", "Open a saved website in your browser.", "links")
    ];
    private static string CanonicalKind(string kind) => kind switch
    {
        "Pomodoro" => "Timer", "Hedy" => "Hedy Notes", "Beer Counter" => "Alcoholic Beverage",
        "Task" or "Tasks" or "Checklist" or "Action items" or "Action Items" or "Agenda" or "Main passage" or "Main Passage" or "Prayer requests" or "Prayer Requests" or "Assignment tracker" or "Questions to ask" => "Task List",
        "Topics" or "Custom fields" or "Custom Fields" or "Custom text" => "Notes",
        "Custom counter" or "Custom Counter" or "Distraction counter" or "Distraction Counter" => "Counter",
        _ => kind
    };
    public static WidgetDefinition Definition(string kind) => Definitions.Single(d => d.Name == CanonicalKind(kind));
    public static WidgetInstance Create(string kind, string panelKey = "")
    {
        var d = Definition(kind);
        var canonical = CanonicalKind(kind);
        var title = kind is "Task" or "Tasks" ? "Task List" : kind == "Session timeline" ? "Session Timeline" : kind == "Beer Counter" ? canonical : kind == canonical ? canonical : kind;
        return new() { Kind = canonical, Title = title, PanelKey = panelKey, GridColumns = 20, Span = d.DefaultSpan, Height = Math.Max(d.DefaultHeight, d.MinHeight), TimerMinutes = kind == "Break timer" ? 5 : 25,
            CounterMode = kind is "Distraction counter" or "Distraction Counter" ? "Distraction" : "Standard", DisplayMode = canonical is "Clock" or "Timer" or "Break timer" or "Stopwatch" or "Calendar" ? "Compact" : "Auto" };
    }
    public static void EnsureLayout(DashboardData data)
    {
        if (data.WidgetLayoutInitialized) { PrepareLayout(data); return; }
        data.WidgetLayoutInitialized = true;
        data.Widgets = [Create("Pixel timer", "pixels"), Create("Session timeline", "timeline"), Create("Notes", "notes"), Create("Task List", "tasks")];
        Arrange(data.Widgets, false);
    }
    public static bool PrepareLayout(DashboardData data)
    {
        var changed = data.Widgets.RemoveAll(w=>LegacyBugHabitats.KindSpecies.ContainsKey(w.Kind))>0;
        foreach (var widget in data.Widgets)
        {
            var previousKind = widget.Kind;
            var canonical = CanonicalKind(previousKind);
            if (canonical != previousKind)
            {
                if (canonical == "Task List" && (previousKind is "Agenda" or "Main passage" or "Main Passage") &&
                    !string.IsNullOrWhiteSpace(widget.Text) && !widget.Items.Any(item => item.Text == widget.Text))
                    widget.Items.Add(new TaskItem { Text = widget.Text });
                if (canonical == "Notes" && previousKind == "Topics" && widget.Items.Count > 0)
                {
                    var previousTopics = string.Join("\n", widget.Items.Select(item => $"[{(item.Done ? "x" : " ")}] {item.Text}"));
                    widget.Text = MergeLegacyText(widget.Text, previousTopics);
                }
                if (canonical == "Notes" && (previousKind is "Custom fields" or "Custom Fields") && widget.Fields.Count > 0)
                    widget.Text = MergeLegacyText(widget.Text, string.Join("\n", widget.Fields.Select(field => field + ":")));
                if (previousKind is "Distraction counter" or "Distraction Counter") widget.CounterMode = "Distraction";
                widget.Kind = canonical;
                if (previousKind is "Pomodoro" or "Hedy" or "Beer Counter" && widget.Title == previousKind) widget.Title = canonical;
                if (previousKind is "Task" or "Tasks" && widget.Title == previousKind) widget.Title = "Task List";
                changed = true;
            }
            if (widget.Kind == "Water Bottle Counter" && widget.WaterBottleGoal < 4)
            { widget.WaterBottleGoal = 4; changed = true; }
            if(widget.BeverageVessel is not ("Wine Glass" or "Shot Glass" or "Beer Bottle"))
            { widget.BeverageVessel="Beer Bottle"; changed=true; }
            if (widget.Kind == "Notes" && widget.PanelKey == "notes")
            {
                var combined = MergeLegacyText(data.Notes, widget.Text);
                if (widget.Text != combined || data.Notes != combined) { widget.Text = data.Notes = combined; changed = true; }
            }
            if (widget.Kind == "Task List" && widget.PanelKey == "tasks")
            {
                foreach (var item in data.Tasks)
                    if (!widget.Items.Any(existing => existing.Text == item.Text && existing.Done == item.Done))
                    { widget.Items.Add(item); changed = true; }
                data.Tasks = widget.Items;
            }
            var definition = Definition(widget.Kind);
            if (!definition.CanMinimize && widget.IsMicro) { widget.IsMicro = false; changed = true; }
            if (!widget.CustomSize && widget.Span == 10 && widget.Height == 288 && definition.DefaultSpan != 10)
            { widget.Span = definition.DefaultSpan; widget.Height = Math.Max(definition.DefaultHeight, definition.MinHeight); changed = true; }
            widget.FlashcardDecks ??= [];
            widget.CalculationHistory ??= [];
        }
        return changed;
    }
    private static string MergeLegacyText(string first, string second) =>
        string.IsNullOrWhiteSpace(first) ? second : string.IsNullOrWhiteSpace(second) || first.Contains(second, StringComparison.Ordinal) ? first :
        second.Contains(first, StringComparison.Ordinal) ? second : first + "\n\n" + second;
    public static int MinimumWidth(WidgetInstance widget) => widget.DisplayMode == "Expanded" && widget.Kind is ("Clock" or "Timer" or "Break timer" or "Stopwatch") ? 340 : Definition(widget.Kind).MinWidth;
    public static int MinimumHeight(WidgetInstance widget) => widget.Kind == "Calendar" ? widget.DisplayMode == "Expanded" ? 384 : 216
        : widget.Kind == "Stopwatch" ? widget.DisplayMode == "Compact" ? 192 : 360
        : widget.Kind == "Clock" ? widget.DisplayMode == "Compact" ? 120 : 288
        : widget.DisplayMode == "Expanded" && widget.Kind is ("Timer" or "Break timer") ? 288 : Definition(widget.Kind).MinHeight;
    public static void Arrange(List<WidgetInstance> widgets, bool singleColumn)
    {
        UpgradeGrid(widgets);
        var row = 0; var column = 0; var height = 0;
        foreach (var widget in widgets)
        {
            widget.PlacementX = null;
            widget.Span = singleColumn ? 20 : Definition(widget.Kind).DefaultSpan;
            if (column + widget.Span > 20) { row += (int)Math.Ceiling(height / 24.0) + 1; column = 0; height = 0; }
            widget.Column = column; widget.Row = row; column += widget.Span; height = Math.Max(height, widget.Height);
        }
    }
    public static void ArrangeThreeColumns(List<WidgetInstance> widgets)
    {
        UpgradeGrid(widgets);
        for (var index = 0; index < widgets.Count; index++)
        {
            widgets[index].PlacementX = null;
            var position = index % 3;
            widgets[index].Span = position == 2 ? 6 : 7;
            widgets[index].Column = position * 7;
            widgets[index].Row = index / 3 * 61;
        }
    }
    public static void Add(DashboardData data, WidgetInstance widget)
    {
        widget.Row = data.Widgets.Count == 0 ? 0 : data.Widgets.Max(w => w.Row + (int)Math.Ceiling(w.Height / 24.0) + 1);
        data.Widgets.Add(widget); data.WidgetLayoutInitialized = true;
    }
    public static void Place(DashboardData data, WidgetInstance widget, int column, int row, int span, int height)
    {
        UpgradeGrid(data.Widgets);
        widget.GridColumns = 20; widget.CustomSize = true;
        widget.Span = Math.Clamp(span, 1, 20); widget.Column = Math.Clamp(column, 0, 20 - widget.Span);
        widget.Row = Math.Clamp(row, 0, 10000); widget.Height = Math.Clamp(height / 24 * 24, 24, 1440);
        // Push overlapping cards down; preserve order and ensure every final rectangle is disjoint.
        var placed = new List<WidgetInstance> { widget };
        foreach (var other in data.Widgets.Where(w => w.Id != widget.Id).OrderBy(w => w.Row).ThenBy(w => w.Column))
        {
            while (placed.FirstOrDefault(w => Overlap(w, other)) is { } collision) other.Row = collision.Row + (int)Math.Ceiling(collision.Height / 24.0) + 1;
            placed.Add(other);
        }
    }
    public static bool UpgradeGrid(IEnumerable<WidgetInstance> widgets)
    {
        var changed = false;
        foreach (var w in widgets.Where(w => w.GridColumns == 12))
        {
            var right = (int)Math.Round((w.Column + w.Span) * 20.0 / 12);
            w.Column = (int)Math.Round(w.Column * 20.0 / 12);
            w.Span = Math.Clamp(right - w.Column, 1, 20 - w.Column);
            w.GridColumns = 20; changed = true;
        }
        return changed;
    }
    private static bool Overlap(WidgetInstance a, WidgetInstance b) => a.Column < b.Column + b.Span && b.Column < a.Column + a.Span && a.Row * 24 < b.Row * 24 + b.Height && b.Row * 24 < a.Row * 24 + a.Height;
    public static void ClearContent(WidgetInstance w)
    { w.Text = ""; w.CalculationHistory.Clear(); w.IntegrationSnapshots.Clear(); w.Value = w.StartingValue; w.Items.Clear(); w.Records.Clear(); w.ScripturePins.Clear(); w.TimerStart = w.TimerEnd = w.TimerStopped = null; w.StopwatchElapsedSeconds = 0; w.StopwatchStartedAt = null; w.StopwatchLaps.Clear(); w.BeerLoggedSinceReminder = 0;
        if (w.Distribution is { } tracker) { tracker.ActiveSession = null; tracker.History.Clear(); }
    }
    public static void Validate(DashboardData data)
    {
        if (data.Widgets == null || data.Widgets.Count > 200 || data.Widgets.Any(w => w == null) || data.Widgets.Select(w => w.Id).Distinct().Count() != data.Widgets.Count) throw new InvalidDataException("Invalid widget list.");
        var panels = new HashSet<string>();
        foreach (var w in data.Widgets)
        {
            w.Distribution?.Validate();
            if(w.ExternalListId==null || w.ExternalListId.Length>1024 || w.IntegrationView is not ("Today" or "Upcoming" or "All") ||
                w.CalendarService is not ("Google Calendar" or "Outlook Calendar")) throw new InvalidDataException("Invalid integration widget configuration.");
            if (w.ScripturePins == null || w.ScripturePins.Count > 10000 || w.ScripturePins.Any(p => p == null || p.Id == Guid.Empty ||
                !BibleLibrary.Books.Contains(p.Book) || !BibleLibrary.Translations.Contains(p.Translation) || p.Chapter is < 1 or > 150 ||
                p.Verse is < 1 or > 176 || p.EndVerse < p.Verse || p.EndVerse > 176 || p.Text == null || p.Note == null) ||
                w.BibleLocation is { } location && (!BibleLibrary.Translations.Contains(location.Translation) || !BibleLibrary.Books.Contains(location.Book) ||
                    location.Chapter is < 1 or > 150 || location.Verse is < 1 or > 176)) throw new InvalidDataException("Invalid Scripture data.");
            if (w.StopwatchLaps == null || w.StopwatchLaps.Count > 10000 || w.StopwatchLaps.Any(v => !double.IsFinite(v) || v < 0) ||
                w.StopwatchLaps.Zip(w.StopwatchLaps.Skip(1)).Any(pair => pair.First > pair.Second) || w.BeerLoggedSinceReminder is < 0 or > 1) throw new InvalidDataException("Invalid stopwatch laps or beer reminder progress.");
            var def = Definitions.FirstOrDefault(d => d.Name == CanonicalKind(w.Kind));
            if (w.CalculationHistory == null || w.CalculationHistory.Count > 20 || w.CalculationHistory.Any(c => c == null || c.Length > 1024)) throw new InvalidDataException("Invalid calculator history.");
            if (w.PlacementX is double x && (!double.IsFinite(x) || x < 0 || x > 1) || !double.IsFinite(w.PlacementY) || w.PlacementY < 0 || w.PlacementY > 720000) throw new InvalidDataException("Invalid widget placement.");
            if (def == null && (w.Kind==null || !LegacyBugHabitats.KindSpecies.ContainsKey(w.Kind)) || w.Id == Guid.Empty || w.Title == null || w.Placeholder == null || w.PanelKey == null || w.Text == null || w.Items == null || w.Records == null || w.Categories == null || w.Fields == null || w.FlashcardDecks == null ||
                w.Items.Any(t => t == null || t.Text == null) || w.Records.Any(r => r == null || r.Name == null || r.Categories == null || r.Values == null) ||
                w.FlashcardDecks.Count > 1000 || w.FlashcardDecks.Any(deck => deck == null || deck.Id == Guid.Empty || string.IsNullOrWhiteSpace(deck.Name) || deck.Name.Length > 200 || deck.Cards == null || deck.Cards.Count > 10000 || deck.Cards.Any(card => card == null || card.Id == Guid.Empty || card.Front == null || card.Back == null || card.StudyState == null || card.Front.Length > 10000 || card.Back.Length > 10000)) ||
                w.Column < 0 || w.GridColumns is not (12 or 20) || w.Span < 1 || w.Column + w.Span > w.GridColumns || w.Row is < 0 or > 30000 || w.Height < 24 || w.Height > 1440 || w.SummaryIntervalMinutes is < 1 or > 10080 || w.TimerMinutes is < 1 or > 10080 || w.TimerDurationSeconds is < 1 or > 604800 || !double.IsFinite(w.StopwatchElapsedSeconds) || w.StopwatchElapsedSeconds < 0 ||
                w.Categories.Any(c => c == null) || w.Fields.Any(f => f == null) || w.Increment == 0 ||
                w.CounterMode is not ("Standard" or "Distraction") || w.DisplayMode is not ("Auto" or "Compact" or "Expanded") || w.WaterBottleGoal is < 1 or > 100 || w.BeerGoal is < 1 or > 100 || w.BannerSpan is < 9 or > 20 || w.BannerHeight is < 312 or > 960) throw new InvalidDataException("Invalid widget configuration.");
            if (w.PanelKey.Length > 0 && (!new[] { "pixels", "setup", "timeline", "notes", "tasks", "hedy" }.Contains(w.PanelKey) || !panels.Add(w.PanelKey))) throw new InvalidDataException("Invalid linked panel.");
        }
    }
}

public sealed record DashboardTemplate(string Name, string Description, string Accent, string[] Widgets);
public static class DashboardTemplates
{
    public static IReadOnlyList<DashboardTemplate> All { get; } =
    [
        new("Class", "A session banner, notes and clock.", "#69ACFF", ["Pixel timer", "Notes", "Clock"]),
        new("Argo Pantry", "Students, categories, shift notes and statistics.", "#6AE58F", ["Record tracker", "Statistics", "Notes", "Counter"]),
        new("Church", "A session banner, Bible, notes and Scripture pins.", "#AFA9EC", ["Pixel timer", "Bible", "Notes", "Scripture pins"]),
        new("Study", "A session banner, notes and a short Focus Reset.", "#C6A5FF", ["Pixel timer", "Notes", "Focus Reset"]),
        new("Meeting", "A session banner, notes and agenda.", "#FFCA78", ["Pixel timer", "Notes", "Agenda"]),
        new("Focus", "Your pixel timer, notes and a short Focus Reset.", "#6AE58F", ["Pixel timer", "Notes", "Focus Reset"]),
        new("Blank", "An empty canvas. Add any widgets you need.", "#8B9FFF", [])
    ];
    public static DashboardData Build(DashboardTemplate template)
    {
        var data = new DashboardData { AccentColor = template.Accent, WidgetLayoutInitialized = true };
        foreach (var kind in template.Widgets)
        {
            var key = kind switch { "Pixel timer" => "pixels", "Session controls" => "setup", "Notes" => "notes", "Tasks" => "tasks", "Session timeline" => "timeline", "Hedy" => "hedy", _ => "" };
            var widget = WidgetLibrary.Create(kind, key);
            if (template.Name == "Argo Pantry" && kind == "Record tracker") { widget.Title = "Students helped"; widget.Categories = ["Freezer", "Fridge", "Fruit", "Canned goods", "Bread"]; widget.Fields = ["Student ID"]; }
            data.Widgets.Add(widget);
        }
        WidgetLibrary.Arrange(data.Widgets, false); return data;
    }
}
