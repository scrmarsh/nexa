namespace SessionDashboard;

// Source-neutral, local entries. Existing interval dictionaries remain readable and unchanged.
public sealed class TimelineEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public string Source { get; set; } = "manual";
    public string Text { get; set; } = "";
    public string AudioPath { get; set; } = "";
}

public interface ITranscriptionProvider
{
    Task<string> TranscribeAsync(string localAudioPath, CancellationToken cancellationToken);
}
public interface ISessionSummaryProvider
{
    Task<string> SummarizeAsync(IReadOnlyList<TimelineEntry> entries, CancellationToken cancellationToken);
}
