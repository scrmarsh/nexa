using System.Text.Json.Serialization;

namespace SessionDashboard;

public sealed class DistributionCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Category";
    public string? IconId { get; set; }
    public string? Color { get; set; }
    // Retired categories remain available to interpret earlier entries.
    public bool Retired { get; set; }
}
public sealed class DistributionPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New preset";
    public string Title { get; set; } = "Distribution Tracker";
    public string Singular { get; set; } = "Person";
    public string Plural { get; set; } = "People";
    public string DisplayMode { get; set; } = "Auto";
    public List<DistributionCategory> Categories { get; set; } = [];
    public static DistributionPreset Argo() => new()
    {
        Name = "Argo Pantry Shift", Title = "Argo Pantry", Singular = "Visitor", Plural = "Visitors",
        Categories = [new() { Name = "Fruit", IconId = "apple" }, new() { Name = "Freezer", IconId = "snowflake" },
            new() { Name = "Fridge", IconId = "refrigerator" }, new() { Name = "Canned", IconId = "canned-food" },
            new() { Name = "Toiletries", IconId = "soap" }]
    };
    public DistributionPreset ConfigurationCopy()
    {
        var copy = Continuity.Clone(this);
        copy.Categories.RemoveAll(c => c.Retired);
        return copy;
    }
}
public sealed class DistributionEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long Number { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public List<Guid> CategoryIds { get; set; } = [];
}
public sealed class DistributionSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "Distribution session";
    public Guid? PresetId { get; set; }
    public string PresetName { get; set; } = "Custom";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? EndedAt { get; set; }
    public DistributionPreset Configuration { get; set; } = new();
    public long NextNumber { get; set; } = 1;
    public List<DistributionEntry> Entries { get; set; } = [];
    public DistributionEntry Add(IEnumerable<Guid> categories, DateTimeOffset now)
    {
        if (EndedAt != null) throw new InvalidOperationException("This session has ended.");
        var valid = Configuration.Categories.Where(c => !c.Retired).Select(c => c.Id).ToHashSet();
        var entry = new DistributionEntry { Number = NextNumber++, Timestamp = now, CategoryIds = categories.Where(valid.Contains).Distinct().ToList() };
        Entries.Add(entry); return entry;
    }
    public bool Undo()
    {
        if (EndedAt != null || Entries.Count == 0) return false;
        Entries.RemoveAt(Entries.Count - 1); return true;
    }
}
public sealed class DistributionTrackerState
{
    public void Validate()
    {
        static void Configuration(DistributionPreset p)
        {
            if (p == null || p.Id == Guid.Empty || string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Title) ||
                string.IsNullOrWhiteSpace(p.Singular) || string.IsNullOrWhiteSpace(p.Plural) || p.Categories == null ||
                p.DisplayMode is not ("Auto" or "Compact" or "Expanded") || p.Categories.Any(c => c == null || c.Id == Guid.Empty || string.IsNullOrWhiteSpace(c.Name)) ||
                p.Categories.Select(c => c.Id).Distinct().Count() != p.Categories.Count) throw new System.IO.InvalidDataException("Invalid Distribution Tracker configuration.");
        }
        Configuration(Setup);
        if (Presets == null || History == null || CustomIcons == null || RecentIcons == null) throw new System.IO.InvalidDataException("Incomplete Distribution Tracker data.");
        foreach (var preset in Presets) Configuration(preset);
        foreach (var session in History.Concat(ActiveSession == null ? [] : new[] { ActiveSession }))
        {
            if (session == null || session.Id == Guid.Empty || session.Entries == null || session.Title == null || session.EndedAt < session.StartedAt) throw new System.IO.InvalidDataException("Invalid distribution session.");
            Configuration(session.Configuration);
            var ids = session.Configuration.Categories.Select(c => c.Id).ToHashSet();
            if (session.Entries.Any(e => e == null || e.Id == Guid.Empty || e.Number < 1 || e.CategoryIds == null || e.CategoryIds.Any(id => !ids.Contains(id))) ||
                session.Entries.Select(e => e.Id).Distinct().Count() != session.Entries.Count || session.NextNumber <= session.Entries.Select(e => e.Number).DefaultIfEmpty(0).Max()) throw new System.IO.InvalidDataException("Invalid distribution entries.");
        }
        if (ActiveSession?.EndedAt != null || History.Any(s => s.EndedAt == null) || CustomIcons.Any(i => i == null || string.IsNullOrWhiteSpace(i.Id) || string.IsNullOrWhiteSpace(i.Name) || string.IsNullOrWhiteSpace(i.StoredFile) || System.IO.Path.GetFileName(i.StoredFile) != i.StoredFile)) throw new System.IO.InvalidDataException("Invalid distribution history or icons.");
    }
    public DistributionPreset Setup { get; set; } = new();
    public List<DistributionPreset> Presets { get; set; } = [DistributionPreset.Argo()];
    public Guid? DefaultPresetId { get; set; }
    public Guid? LoadedPresetId { get; set; }
    public string LoadedPresetName { get; set; } = "Custom";
    public DistributionSession? ActiveSession { get; set; }
    public List<DistributionSession> History { get; set; } = [];
    public List<CustomTrackerIcon> CustomIcons { get; set; } = [];
    public List<string> RecentIcons { get; set; } = [];
    public DistributionSession Start(DateTimeOffset now)
    {
        End(now);
        return ActiveSession = new DistributionSession { StartedAt = now, Title = LoadedPresetName == "Custom" ? Setup.Title : LoadedPresetName,
            PresetId = LoadedPresetId, PresetName = LoadedPresetName, Configuration = Setup.ConfigurationCopy() };
    }
    public void End(DateTimeOffset now)
    {
        if (ActiveSession == null) return;
        ActiveSession.EndedAt = now < ActiveSession.StartedAt ? ActiveSession.StartedAt : now;
        History.Add(ActiveSession); ActiveSession = null;
    }
    public void Load(DistributionPreset preset, DateTimeOffset now)
    {
        End(now); Setup = preset.ConfigurationCopy(); LoadedPresetId = preset.Id; LoadedPresetName = preset.Name; Start(now);
    }
}
public sealed record DistributionHour(DateTimeOffset Start, int Count);
public sealed class DistributionStatistics
{
    public int Total { get; private init; }
    public Dictionary<Guid, int> CategoryTotals { get; private init; } = [];
    public List<DistributionHour> Hours { get; private init; } = [];
    public double AverageItems { get; private init; }
    public double Percentage(Guid category) => Total == 0 ? 0 : 100.0 * CategoryTotals.GetValueOrDefault(category) / Total;
    public double? Rate(DistributionSession session, DateTimeOffset now)
    {
        var elapsed = ((session.EndedAt ?? now) - session.StartedAt).TotalHours;
        return elapsed < 1.0 / 12 ? null : Total / elapsed; // Wait five minutes before projecting an hourly rate.
    }
    public static DistributionStatistics Calculate(DistributionSession session)
    {
        var totals = session.Entries.SelectMany(e => e.CategoryIds.Distinct()).GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
        return new() { Total = session.Entries.Count, CategoryTotals = totals,
            AverageItems = session.Entries.Count == 0 ? 0 : (double)totals.Values.Sum() / session.Entries.Count,
            Hours = session.Entries.GroupBy(e => new DateTimeOffset(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0, e.Timestamp.Offset))
                .OrderBy(g => g.Key).Select(g => new DistributionHour(g.Key, g.Count())).ToList() };
    }
}
public sealed class CustomTrackerIcon
{
    public string Id { get; set; } = "custom-" + Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Custom icon";
    public string StoredFile { get; set; } = "";
    public string OriginalFilename { get; set; } = "";
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.Now;
}
