namespace SessionDashboard;

public static class Theme
{
    public static (string Accent, string Pixel) ForName(string name) => name.ToLowerInvariant() switch
    {
        "lecture" => ("#69ACFF", "#2786FF"),
        "focused work" => ("#6AE58F", "#1DB954"),
        "meeting" => ("#FFCA78", "#E6A441"),
        "studying" => ("#C6A5FF", "#A67AF2"),
        _ => ("#6AE58F", "#2786FF")
    };
}

public static class Milestones
{
    public static List<string> Collect(Session session, DateTimeOffset now)
    {
        var result = new List<string>();
        if (!session.IsRunning(now)) return result;
        void Add(string id, bool reached) { if (reached && !session.ReachedMilestones.Contains(id)) { session.ReachedMilestones.Add(id); result.Add(id); } }
        Add("HALFWAY THERE", session.Progress(now) >= .5);
        Add("THREE QUARTERS COMPLETE", session.Progress(now) >= .75);
        Add("5 MINUTES REMAINING", session.Remaining(now) <= TimeSpan.FromMinutes(5));
        return result;
    }
}
