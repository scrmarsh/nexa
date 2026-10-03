using System.IO;

namespace SessionDashboard;

public sealed class WidgetInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Kind { get; set; } = "Notes";
    public string Title { get; set; } = "Notes";
    public string PanelKey { get; set; } = "";
    public int Column { get; set; }
    public int Row { get; set; }
    public int Span { get; set; } = 6;
    public int Height { get; set; } = 288;
    public string Placeholder { get; set; } = "Write something…";
    public decimal Increment { get; set; } = 1;
    public decimal StartingValue { get; set; }
    public int TimerMinutes { get; set; } = 25;
    public int? TimerDurationSeconds { get; set; }
    public double StopwatchElapsedSeconds { get; set; }
    public DateTimeOffset? StopwatchStartedAt { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public int DurationSeconds => TimerDurationSeconds ?? TimerMinutes * 60;
    public bool ShowSeconds { get; set; } = true;
    public bool ShowElapsed { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<string> Fields { get; set; } = [];
    public string Text { get; set; } = "";
    public decimal Value { get; set; }
    public List<TaskItem> Items { get; set; } = [];
    public List<TrackerRecord> Records { get; set; } = [];
    public DateTimeOffset? TimerStart { get; set; }
    public DateTimeOffset? TimerEnd { get; set; }
    public DateTimeOffset? TimerStopped { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasData => Text.Length > 0 || Items.Count > 0 || Records.Count > 0 || Value != StartingValue || TimerStart != null || StopwatchStartedAt != null || StopwatchElapsedSeconds > 0;
}
public sealed class TrackerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public List<string> Categories { get; set; } = [];
    public Dictionary<string, string> Values { get; set; } = [];
}
public sealed record WidgetDefinition(string Name, string Group, string Description, string Editor, int MinSpan = 3, int MinHeight = 192);

public static class WidgetLibrary
{
    public static IReadOnlyList<WidgetDefinition> Definitions { get; } =
    [
        new("Timer", "Session", "Independent countdown or elapsed timer.", "timer"),
        new("Stopwatch", "Session", "Measure elapsed time with pause and resume.", "stopwatch"),
        new("Clock", "Session", "Your local date and time.", "clock"),
        new("Progress bar", "Session", "Progress of your dashboard session.", "progress"),
        new("Pixel timer", "Session", "The original pixel display and story.", "progress", 6, 480),
        new("Session controls", "Session", "Schedule sessions and manage timer presets.", "setup", 4, 480),
        new("Session timeline", "Session", "Session intervals and saved notes.", "timeline", 4, 288),
        new("Pomodoro", "Session", "A configurable focus countdown.", "timer"),
        new("Break timer", "Session", "A short independent countdown.", "timer"),
        new("Notes", "Productivity", "Capture your ideas with autosave.", "text"),
        new("Tasks", "Productivity", "An actionable checklist.", "list"),
        new("Checklist", "Productivity", "Track completed steps.", "list"),
        new("Quick links", "Productivity", "Save and open web links.", "links"),
        new("Agenda", "Productivity", "Plan your discussion.", "text"),
        new("Action items", "Productivity", "Follow-ups for your meeting.", "list"),
        new("Distraction counter", "Productivity", "Count distractions without losing focus.", "counter"),
        new("Counter", "Tracking", "A configurable count and increment.", "counter"),
        new("Record tracker", "Tracking", "Records with categories and custom fields.", "tracker", 4, 336),
        new("Statistics", "Tracking", "Totals from trackers on this dashboard.", "statistics"),
        new("Custom fields", "Tracking", "Labelled session information.", "text"),
        new("Hedy", "School", "Connect Hedy and retain interval notes.", "hedy", 4, 480),
        new("Class timeline", "School", "Class intervals and notes.", "timeline", 4, 288),
        new("Assignment tracker", "School", "Keep assignments and completion state.", "list"),
        new("Questions to ask", "School", "Questions to revisit.", "list"),
        new("Topics", "School", "Topics reviewed in this session.", "list"),
        new("Bible", "Church", "Save a passage reference and open it online.", "bible"),
        new("Scripture pins", "Church", "Keep references and verse notes.", "list"),
        new("Prayer requests", "Church", "A private prayer checklist.", "list"),
        new("Main passage", "Church", "Your main reference and passage notes.", "text"),
        new("Custom text", "Other", "A flexible text panel.", "text"),
        new("Custom counter", "Other", "Name and configure your own counter.", "counter"),
        new("Website shortcut", "Other", "Open a saved website in your browser.", "links")
    ];
    public static WidgetDefinition Definition(string kind) => Definitions.Single(d => d.Name == kind);
    public static WidgetInstance Create(string kind, string panelKey = "")
    {
        var d = Definition(kind);
        return new() { Kind = kind, Title = kind, PanelKey = panelKey, Span = Math.Max(6, d.MinSpan), Height = Math.Max(288, d.MinHeight), TimerMinutes = kind == "Break timer" ? 5 : 25 };
    }
    public static void EnsureLayout(DashboardData data)
    {
        if (data.WidgetLayoutInitialized) return;
        data.WidgetLayoutInitialized = true;
        data.Widgets = [Create("Pixel timer", "pixels"), Create("Session controls", "setup"), Create("Session timeline", "timeline"), Create("Notes", "notes"), Create("Tasks", "tasks"), Create("Hedy", "hedy")];
        Arrange(data.Widgets, false);
    }
    public static void Arrange(List<WidgetInstance> widgets, bool singleColumn)
    {
        var row = 0;
        for (var i = 0; i < widgets.Count;)
        {
            var first = widgets[i++]; first.Column = 0; first.Row = row; first.Span = singleColumn ? 12 : 6;
            var height = first.Height;
            if (!singleColumn && i < widgets.Count) { var second = widgets[i++]; second.Column = 6; second.Row = row; second.Span = 6; height = Math.Max(height, second.Height); }
            row += (int)Math.Ceiling(height / 24.0) + 1;
        }
    }
    public static void Add(DashboardData data, WidgetInstance widget)
    {
        widget.Row = data.Widgets.Count == 0 ? 0 : data.Widgets.Max(w => w.Row + (int)Math.Ceiling(w.Height / 24.0) + 1);
        data.Widgets.Add(widget); data.WidgetLayoutInitialized = true;
    }
    public static void Place(DashboardData data, WidgetInstance widget, int column, int row, int span, int height)
    {
        var def = Definition(widget.Kind); widget.Span = Math.Clamp(span, def.MinSpan, 12); widget.Column = Math.Clamp(column, 0, 12 - widget.Span);
        widget.Row = Math.Clamp(row, 0, 10000); widget.Height = Math.Clamp(height / 24 * 24, def.MinHeight, 1440);
        // Push overlapping cards down; preserve order and ensure every final rectangle is disjoint.
        var placed = new List<WidgetInstance> { widget };
        foreach (var other in data.Widgets.Where(w => w.Id != widget.Id).OrderBy(w => w.Row).ThenBy(w => w.Column))
        {
            while (placed.FirstOrDefault(w => Overlap(w, other)) is { } collision) other.Row = collision.Row + (int)Math.Ceiling(collision.Height / 24.0) + 1;
            placed.Add(other);
        }
    }
    private static bool Overlap(WidgetInstance a, WidgetInstance b) => a.Column < b.Column + b.Span && b.Column < a.Column + a.Span && a.Row * 24 < b.Row * 24 + b.Height && b.Row * 24 < a.Row * 24 + a.Height;
    public static void ClearContent(WidgetInstance w)
    { w.Text = ""; w.Value = w.StartingValue; w.Items.Clear(); w.Records.Clear(); w.TimerStart = w.TimerEnd = w.TimerStopped = null; w.StopwatchElapsedSeconds = 0; w.StopwatchStartedAt = null; }
    public static void Validate(DashboardData data)
    {
        if (data.Widgets == null || data.Widgets.Count > 200 || data.Widgets.Any(w => w == null) || data.Widgets.Select(w => w.Id).Distinct().Count() != data.Widgets.Count) throw new InvalidDataException("Invalid widget list.");
        var panels = new HashSet<string>();
        foreach (var w in data.Widgets)
        {
            var def = Definitions.FirstOrDefault(d => d.Name == w.Kind);
            if (def == null || w.Id == Guid.Empty || w.Title == null || w.Placeholder == null || w.PanelKey == null || w.Text == null || w.Items == null || w.Records == null || w.Categories == null || w.Fields == null ||
                w.Items.Any(t => t == null || t.Text == null) || w.Records.Any(r => r == null || r.Name == null || r.Categories == null || r.Values == null) ||
                w.Column < 0 || w.Span < def.MinSpan || w.Column + w.Span > 12 || w.Row is < 0 or > 30000 || w.Height < def.MinHeight || w.Height > 1440 || w.TimerMinutes is < 1 or > 10080 || w.TimerDurationSeconds is < 1 or > 604800 || !double.IsFinite(w.StopwatchElapsedSeconds) || w.StopwatchElapsedSeconds < 0 ||
                w.Categories.Any(c => c == null) || w.Fields.Any(f => f == null) || w.Increment == 0) throw new InvalidDataException("Invalid widget configuration.");
            if (w.PanelKey.Length > 0 && (!new[] { "pixels", "setup", "timeline", "notes", "tasks", "hedy" }.Contains(w.PanelKey) || !panels.Add(w.PanelKey))) throw new InvalidDataException("Invalid linked panel.");
        }
    }
}

public sealed record DashboardTemplate(string Name, string Description, string Accent, string[] Widgets);
public static class DashboardTemplates
{
    public static IReadOnlyList<DashboardTemplate> All { get; } =
    [
        new("Class", "Lecture notes, intervals and questions.", "#69ACFF", ["Pixel timer", "Session controls", "Notes", "Tasks", "Session timeline", "Hedy", "Questions to ask"]),
        new("Argo Pantry", "Students, categories, shift notes and statistics.", "#6AE58F", ["Record tracker", "Statistics", "Notes", "Counter"]),
        new("Church", "Passage notes, scripture pins and prayer requests.", "#AFA9EC", ["Bible", "Main passage", "Notes", "Scripture pins", "Prayer requests"]),
        new("Study", "A focus goal, assignments and topics to review.", "#C6A5FF", ["Pomodoro", "Tasks", "Notes", "Topics", "Distraction counter"]),
        new("Meeting", "Agenda, decisions and action items.", "#FFCA78", ["Timer", "Agenda", "Notes", "Action items"]),
        new("Focus", "Your pixel timer, notes and next steps.", "#6AE58F", ["Pixel timer", "Session controls", "Notes", "Tasks"]),
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
