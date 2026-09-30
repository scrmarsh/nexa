using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SessionDashboard;

public sealed class Preset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Focused work";
    public int Minutes { get; set; } = 50;
    public int IntervalMinutes { get; set; } = 10;
    public string AccentColor { get; set; } = "";
    public string PixelColor { get; set; } = "";
    public int? StartClockMinutes { get; set; }
    public int? EndClockMinutes { get; set; }
    public int EndDayOffset { get; set; }
}

public sealed class Session
{
    public string Title { get; set; } = "";
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
    public int IntervalMinutes { get; set; } = 10;
    public DateTimeOffset? StoppedAt { get; set; }
    public List<string> ReachedMilestones { get; set; } = [];
    public Dictionary<int, string> IntervalNotes { get; set; } = [];
    public Dictionary<int, string> HedyIntervalNotes { get; set; } = [];
    public string HedySessionId { get; set; } = "";
    public string HedySourceTitle { get; set; } = "Hedy";
    public string HedyTranscript { get; set; } = "";
    public string HedyBaseline { get; set; } = "";
    public double Progress(DateTimeOffset now) => End <= Start ? 0 : Math.Clamp(((StoppedAt ?? now) - Start).TotalSeconds / (End - Start).TotalSeconds, 0, 1);
    public TimeSpan Elapsed(DateTimeOffset now) => TimeSpan.FromSeconds(Progress(now) * (End - Start).TotalSeconds);
    public TimeSpan Remaining(DateTimeOffset now) => StoppedAt != null || now >= End ? TimeSpan.Zero : (now < Start ? Start - now : End - now);
    public bool IsRunning(DateTimeOffset now) => StoppedAt == null && now >= Start && now < End;
}

public sealed class TaskItem
{
    public string Text { get; set; } = "";
    public bool Done { get; set; }
}

public sealed class AppState
{
    public List<Preset> Presets { get; set; } =
    [
        new() { Name = "Lecture", Minutes = 75, IntervalMinutes = 5, AccentColor = "#69ACFF", PixelColor = "#2786FF" },
        new() { Name = "Focused work", Minutes = 50, IntervalMinutes = 10, AccentColor = "#6AE58F", PixelColor = "#1DB954" },
        new() { Name = "Meeting", Minutes = 30, IntervalMinutes = 5, AccentColor = "#FFCA78", PixelColor = "#E6A441" },
        new() { Name = "Studying", Minutes = 25, IntervalMinutes = 5, AccentColor = "#C6A5FF", PixelColor = "#A67AF2" }
    ];
    public Session? Session { get; set; }
    public string Notes { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = [];
    public bool ShowNotes { get; set; } = true;
    public bool ShowChecklist { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
    public bool Compact { get; set; }
    public string AccentColor { get; set; } = "#1DB954";
    public string PixelColor { get; set; } = "#2786FF";
    public string StoryNode { get; set; } = "arrival";
    public List<string> StoryHistory { get; set; } = [];
    public List<string> StoryEndings { get; set; } = [];
    public string HedyRegion { get; set; } = "US";
    public string HedyProtectedKey { get; set; } = "";
}

public static class InputRules
{
    public static bool IsColor(string? color) => color != null && Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$");
    public static bool ValidMinutes(string text, out int minutes) => int.TryParse(text, out minutes) && minutes is >= 1 and <= 10080;
    public static bool ValidSession(Session session) => !string.IsNullOrWhiteSpace(session.Title) && session.End > session.Start && (session.End - session.Start).TotalDays <= 7 && session.IntervalMinutes is >= 1 and <= 10080;
}

public sealed class StateStore(string path)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string FilePath { get; } = path;
    public string? Warning { get; private set; }

    public AppState Load()
    {
        foreach (var candidate in new[] { FilePath, FilePath + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(candidate), Options) ?? throw new JsonException("Empty settings.");
                if (state.Presets == null || state.Tasks == null || state.StoryHistory == null || state.StoryEndings == null) throw new JsonException("Incomplete settings.");
                state.Presets.RemoveAll(p => p == null || string.IsNullOrWhiteSpace(p.Name) || p.Minutes is < 1 or > 10080 || p.IntervalMinutes is < 1 or > 10080);
                state.Tasks.RemoveAll(t => t == null || string.IsNullOrWhiteSpace(t.Text));
                state.Notes ??= "";
                if (state.Session != null && !InputRules.ValidSession(state.Session)) state.Session = null;
                foreach (var preset in state.Presets)
                {
                    var theme = Theme.ForName(preset.Name);
                    if (!InputRules.IsColor(preset.AccentColor)) preset.AccentColor = theme.Accent;
                    if (!InputRules.IsColor(preset.PixelColor)) preset.PixelColor = theme.Pixel;
                    if (preset.StartClockMinutes is < 0 or > 1439) preset.StartClockMinutes = null;
                    if (preset.EndClockMinutes is < 0 or > 1439) preset.EndClockMinutes = null;
                    preset.EndDayOffset = Math.Clamp(preset.EndDayOffset, 0, 7);
                }
                if (state.Session is { } session)
                {
                    session.ReachedMilestones ??= [];
                    session.IntervalNotes ??= [];
                    session.HedyIntervalNotes ??= [];
                    session.HedyTranscript ??= "";
                    session.HedyBaseline ??= "";
                }
                if (!InputRules.IsColor(state.AccentColor)) state.AccentColor = "#1DB954";
                if (!InputRules.IsColor(state.PixelColor)) state.PixelColor = "#2786FF";
                if (candidate != FilePath) Warning = "Recovered your last backup after the settings file could not be read.";
                return state;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            { Warning = "Settings could not be read. Defaults loaded; the original file will be preserved on the next save."; }
        }
        return new AppState();
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        if (Warning != null && File.Exists(FilePath))
            File.Copy(FilePath, FilePath + ".recovered-" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), true);
        var temp = FilePath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(state, Options));
        if (File.Exists(FilePath)) File.Replace(temp, FilePath, Warning == null ? FilePath + ".bak" : null);
        else File.Move(temp, FilePath);
        Warning = null;
    }
}
