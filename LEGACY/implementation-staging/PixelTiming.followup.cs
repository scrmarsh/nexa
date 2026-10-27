namespace SessionDashboard;

public sealed record PixelTimerReading(TimeSpan Time, string Caption, string Status, double RemainingFraction, bool CountUp)
{
    public double SessionProgress { get; init; }
    public DateTimeOffset ClockInstant { get; init; }
    public DateTimeOffset? PhaseStart { get; init; }
    public DateTimeOffset? PhaseEnd { get; init; }
    public double VisualFraction => CountUp || Status is "SESSION COMPLETE" or "SESSION ENDED" ? SessionProgress : RemainingFraction;
}

public static class PixelTiming
{
    public static PixelTimerReading Read(Session? session, DateTimeOffset now)
    {
        now=now.ToUniversalTime();
        PixelTimerReading Reading(TimeSpan time,string caption,string status,double remaining,bool countUp,double elapsed=0,DateTimeOffset? start=null,DateTimeOffset? end=null)
            =>new(time,caption,status,Math.Clamp(remaining,0,1),countUp){ClockInstant=now,SessionProgress=Math.Clamp(elapsed,0,1),PhaseStart=start,PhaseEnd=end};
        if(session==null)return Reading(TimeSpan.Zero,"READY WHEN YOU ARE","READY WHEN YOU ARE",0,false);
        if(session.StoppedAt!=null)return Reading(TimeSpan.Zero,"SESSION ENDED","SESSION ENDED",0,false,session.Progress(now),session.Start,session.End);
        var occurrence=WeeklySchedule.Latest(session,now);
        var from=occurrence?.Start ?? session.Start;var to=occurrence?.End ?? session.End;
        if(now>=from && now<to)
        {
            var fraction=(now-from).TotalSeconds/Math.Max(.001,(to-from).TotalSeconds);
            return Reading(now-from,"SESSION PROGRESS","IN SESSION",1-fraction,true,fraction,from,to);
        }
        if(now<session.Start)
        {
            var origin=session.BeforeSessionCountdownFrom ?? session.Start-(session.End-session.Start);
            return Reading(session.Start-now,"NEXT SESSION","SCHEDULED",(session.Start-now).TotalSeconds/Math.Max(.001,(session.Start-origin).TotalSeconds),false,0,origin,session.Start);
        }
        if(WeeklySchedule.Next(session,now) is {} next)
        {
            // A missed recurrence becomes the gap origin; do not reuse an obsolete week's end.
            return Reading(next.Start-now,"NEXT SESSION","BETWEEN SESSIONS",(next.Start-now).TotalSeconds/Math.Max(.001,(next.Start-to).TotalSeconds),false,1,to,next.Start);
        }
        return Reading(TimeSpan.Zero,"SESSION COMPLETE","SESSION COMPLETE",0,false,1,from,to);
    }
    public static PixelTimerReading ForWidget(WidgetInstance widget,Session? session,DateTimeOffset now)
        =>Read(widget.ProgressLinkedToSession?session:widget.TimerStart is {} start && widget.TimerEnd is {} end?new Session{Start=start,End=end,StoppedAt=widget.TimerStopped}:null,now);
    public static int CompletedPixels(PixelTimerReading reading,int count)
    {
        var fraction=Math.Clamp(reading.VisualFraction,0,1);
        return Math.Clamp((int)(reading.CountUp?Math.Floor(fraction*count):Math.Ceiling(fraction*count)),0,count);
    }
    public static TimeSpan UntilNextSecond(DateTimeOffset now)=>TimeSpan.FromTicks(TimeSpan.TicksPerSecond-now.UtcTicks%TimeSpan.TicksPerSecond);

    public static Session WeeklyClass(string title,IEnumerable<DayOfWeek> weekdays,TimeSpan start,TimeSpan end,string zone,DateTimeOffset now)
    {
        var days=weekdays.Distinct().ToList();
        if(string.IsNullOrWhiteSpace(title)||days.Count==0||days.Any(d=>!Enum.IsDefined(d)))throw new ArgumentException("Enter a class name and choose at least one weekday.");
        if(start<TimeSpan.Zero||start>=TimeSpan.FromDays(1)||end<TimeSpan.Zero||end>=TimeSpan.FromDays(1)||start==end)throw new ArgumentException("Choose different valid start and end times.");
        var today=NexaTime.Local(now,zone).Date;
        for(var offset=-1;offset<=7;offset++)
        {
            var date=today.AddDays(offset);if(!days.Contains(date.DayOfWeek))continue;
            var from=NexaTime.Instant(date+start,zone);var to=NexaTime.Instant(date.AddDays(end<=start?1:0)+end,zone);
            if(to<=now||to<=from)continue;
            return new Session{Title=title.Trim(),Start=from,End=to,ScheduledDays=days,AutomaticScheduling=true,TimeZoneId=NexaTime.Zone(zone).Id,ScheduleTimeZoneId=zone,
                RecurringStartMinutes=(int)start.TotalMinutes,RecurringEndMinutes=(int)end.TotalMinutes,RecurringEndDayOffset=end<=start?1:0,ConfiguredDurationMinutes=(to-from).TotalMinutes,BeforeSessionCountdownFrom=now};
        }
        throw new ArgumentException("The selected schedule has no valid upcoming class.");
    }
}
