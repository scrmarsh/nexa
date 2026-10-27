using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

public sealed class IntegrationSnapshot
{
    public DateTimeOffset CapturedAt { get; set; }
    public string Service { get; set; } = "";
    public string ListName { get; set; } = "";
    public int TotalItems { get; set; }
    public string Fingerprint { get; set; } = "";
    public List<IntegrationHistoryItem> Items { get; set; } = [];
}
public sealed class IntegrationHistoryItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public DateOnly? Due { get; set; }
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
}
internal static class WidgetActivity
{
    internal static bool Capture(WidgetInstance widget, string service, string listName, IEnumerable<IntegrationHistoryItem> source, DateTimeOffset now)
    {
        var all = source.ToArray();
        var items = all.Take(200).ToList();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { service, listName, Total = all.Length, items }))));
        if (widget.IntegrationSnapshots.LastOrDefault()?.Fingerprint == fingerprint) return false;
        widget.IntegrationSnapshots.Add(new() { CapturedAt = now.ToUniversalTime(), Service = service, ListName = listName, TotalItems = all.Length, Items = items, Fingerprint = fingerprint });
        if (widget.IntegrationSnapshots.Count > 12) widget.IntegrationSnapshots.RemoveRange(0, widget.IntegrationSnapshots.Count - 12);
        return true;
    }
    internal static bool HasHistory(WidgetInstance widget) => widget.HasData || WidgetLibrary.Definition(widget.Kind).Editor is
        "productivity" or "score" or "timeline" or "hedy" or "text" or "list" or "counter" or "waterBottles" or "beer" or "timer" or "stopwatch" or "scripture" or "flashcards" or "integration";
}

public partial class MainWindow
{
    private void CaptureTaskSnapshot(WidgetInstance widget, string service, string listName, IEnumerable<ExternalTask> tasks)
    {
        if (WidgetActivity.Capture(widget, service, listName, tasks.Select(t => new IntegrationHistoryItem { Id = t.Id, Title = t.Title, Due = t.Due, Status = "Open" }), DateTimeOffset.UtcNow)) Changed();
    }
    private void CaptureCalendarSnapshot(WidgetInstance widget, string service, string listName, IEnumerable<CalendarEntry> events)
    {
        if (WidgetActivity.Capture(widget, service, listName, events.Select(t => new IntegrationHistoryItem { Title = t.Title, Start = t.Start.ToUniversalTime(), End = t.End.ToUniversalTime(), Status = t.AllDay ? "All day" : "Scheduled" }), DateTimeOffset.UtcNow)) Changed();
    }
    private void TrackExternalCompletion(WidgetInstance widget, string service, string taskId, string title)
    {
        ProductivityScoring.Add(state.Productivity, "external:complete:" + service + ":" + widget.ExternalListId + ":" + taskId, "Task completed", title, 100, DateTimeOffset.UtcNow);
        var latest = widget.IntegrationSnapshots.LastOrDefault();
        if (latest != null)
        {
            var completed = Continuity.Clone(latest.Items);
            var item = completed.FirstOrDefault(i => i.Id == taskId);
            if (item != null) item.Status = "Completed";
            WidgetActivity.Capture(widget, service, latest.ListName, completed, DateTimeOffset.UtcNow);
        }
        Changed(); SaveNow();
    }
}
