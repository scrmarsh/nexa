namespace SessionDashboard;

public sealed record PixelTimerReading(TimeSpan Time, string Caption, string Status, double RemainingFraction, bool CountUp);

public static class PixelTiming
{
    public static PixelTimerReading Read(Session? session, DateTimeOffset now)
    {
        if (session == null) return new(TimeSpan.Zero, "READY WHEN YOU ARE", "READY WHEN YOU ARE", 0, false);
        if (session.StoppedAt != null) return new(TimeSpan.Zero, "SESSION ENDED", "SESSION ENDED", 0, false);
        if (now < session.Start)
        {
            var from = session.BeforeSessionCountdownFrom ?? session.Start - (session.End - session.Start);
            var fraction = (session.Start - now).TotalSeconds / Math.Max(1, (session.Start - from).TotalSeconds);
            return new(session.Start - now, "UNTIL CLASS STARTS", "SCHEDULED", Math.Clamp(fraction, 0, 1), false);
        }
        if (now < session.End)
            return new(session.Elapsed(now), "CLASS TIME ELAPSED", "IN SESSION", 1 - session.Progress(now), true);
        if (WeeklySchedule.Next(session, now) is { } next)
        {
            var fraction = (next.Start - now).TotalSeconds / Math.Max(1, (next.Start - session.End).TotalSeconds);
            return new(next.Start - now, "UNTIL NEXT CLASS", "BETWEEN SESSIONS", Math.Clamp(fraction, 0, 1), false);
        }
        return new(TimeSpan.Zero, "SESSION COMPLETE", "SESSION OVER", 0, false);
    }

    public static Session WeeklyClass(string title, IEnumerable<DayOfWeek> weekdays, TimeSpan start, TimeSpan end, string zone, DateTimeOffset now)
    {
        var days = weekdays.Distinct().ToList();
        if (string.IsNullOrWhiteSpace(title) || days.Count == 0 || days.Any(d => !Enum.IsDefined(d)))
            throw new ArgumentException("Enter a class name and choose at least one weekday.");
        if (start < TimeSpan.Zero || start >= TimeSpan.FromDays(1) || end < TimeSpan.Zero || end >= TimeSpan.FromDays(1) || start == end)
            throw new ArgumentException("Choose different valid start and end times.");
        var today = NexaTime.Local(now, zone).Date;
        // Yesterday is needed when an overnight class is still in progress today.
        for (var offset = -1; offset <= 7; offset++)
        {
            var date = today.AddDays(offset);
            if (!days.Contains(date.DayOfWeek)) continue;
            var from = NexaTime.Instant(date + start, zone);
            var to = NexaTime.Instant(date.AddDays(end <= start ? 1 : 0) + end, zone);
            if (to <= now || to <= from) continue;
            return new Session
            {
                Title = title.Trim(), Start = from, End = to, ScheduledDays = days,
                AutomaticScheduling = true, TimeZoneId = zone,
                ConfiguredDurationMinutes = (to - from).TotalMinutes, BeforeSessionCountdownFrom = now
            };
        }
        throw new ArgumentException("The selected schedule has no valid upcoming class.");
    }
}
