namespace SessionDashboard;
public partial class MainWindow
{
    private bool applyingTimingPreset;
    internal bool ApplyTimingPreset(Preset preset,DateTimeOffset? at=null)
    {
        if(applyingTimingPreset)return false;applyingTimingPreset=true;
        try
        {
            var now=at??DateTimeOffset.UtcNow;Session session;
            if(preset.StartClockMinutes is {} from && preset.EndClockMinutes is {} to)
            {
                var date=NexaTime.Local(now,Options.TimeZoneId).Date;
                session=preset.AutomaticScheduling?PixelTiming.WeeklyClass(preset.Name,preset.ScheduledDays.Count>0?preset.ScheduledDays:[date.DayOfWeek],TimeSpan.FromMinutes(from),TimeSpan.FromMinutes(to),Options.TimeZoneId,now)
                    :new Session{Title=preset.Name,Start=NexaTime.Instant(date.AddMinutes(from),Options.TimeZoneId),End=NexaTime.Instant(date.AddDays(preset.EndDayOffset).AddMinutes(to),Options.TimeZoneId),TimeZoneId=Options.TimeZoneId,BeforeSessionCountdownFrom=now,ScheduledDays=preset.ScheduledDays.ToList()};
            }
            else session=new Session{Title=preset.Name,Start=now,End=now.AddMinutes(preset.Minutes),TimeZoneId=Options.TimeZoneId};
            if(session.End<=session.Start)return false;
            session.AutomaticScheduling=preset.AutomaticScheduling;session.IntervalMinutes=preset.IntervalMinutes;session.ConfiguredDurationMinutes=(session.End-session.Start).TotalMinutes;
            return ApplyClassSchedule(session,false);
        }
        finally{applyingTimingPreset=false;}
    }
}
