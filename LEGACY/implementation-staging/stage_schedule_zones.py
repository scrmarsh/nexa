from pathlib import Path
root=Path(r'M:\Nexa')
def read(name):return (root/name).read_text(encoding='utf-8-sig')
def write(name,text):(root/name).write_text(text,encoding='utf-8')
def method(text,signature):
    start=text.index(signature);brace=text.index('{',start);depth=1;end=brace+1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]
text=read('Models.cs')
needle='    public DateTimeOffset? BeforeSessionCountdownFrom { get; set; }'
text=text.replace(needle,needle+'''
    public string? ScheduleTimeZoneId { get; set; }
    public int? RecurringStartMinutes { get; set; }
    public int? RecurringEndMinutes { get; set; }
    public int? RecurringEndDayOffset { get; set; }''')
write('Models.cs',text)
text=read('NexaReliability.cs')
old=method(text,'    public static SessionOccurrence? Next(')
new='''    public static void ChangeTimeZone(Session seed, string zone)
    {
        var from = NexaTime.Local(seed.Start, seed.TimeZoneId);
        var to = NexaTime.Local(seed.End, seed.TimeZoneId);
        seed.RecurringStartMinutes ??= (int)from.TimeOfDay.TotalMinutes;
        seed.RecurringEndMinutes ??= (int)to.TimeOfDay.TotalMinutes;
        seed.RecurringEndDayOffset ??= (to.Date - from.Date).Days;
        seed.ScheduleTimeZoneId = zone;
    }
    public static SessionOccurrence? Next(Session seed, DateTimeOffset after)
    {
        if (!seed.AutomaticScheduling || seed.ScheduledDays.Count == 0) return null;
        var recordedStart = NexaTime.Local(seed.Start, seed.TimeZoneId);
        var recordedEnd = NexaTime.Local(seed.End, seed.TimeZoneId);
        var zone = seed.ScheduleTimeZoneId ?? seed.TimeZoneId;
        var start = TimeSpan.FromMinutes(seed.RecurringStartMinutes ?? (int)recordedStart.TimeOfDay.TotalMinutes);
        var end = TimeSpan.FromMinutes(seed.RecurringEndMinutes ?? (int)recordedEnd.TimeOfDay.TotalMinutes);
        var dayOffset = seed.RecurringEndDayOffset ?? (recordedEnd.Date - recordedStart.Date).Days;
        var date = NexaTime.Local(after, zone).Date;
        for (var i = 0; i <= 8; i++)
        {
            var day = date.AddDays(i);
            if (!seed.ScheduledDays.Contains(day.DayOfWeek)) continue;
            var from = NexaTime.Instant(day + start, zone);
            var to = NexaTime.Instant(day.AddDays(dayOffset) + end, zone);
            if (from > after && from >= seed.Start && to > from) return new(from, to);
        }
        return null;
    }'''
text=text.replace(old,new);write('NexaReliability.cs',text)
text=read('PixelTiming.cs').replace('AutomaticScheduling = true, TimeZoneId = zone,','AutomaticScheduling = true, TimeZoneId = NexaTime.Zone(zone).Id, ScheduleTimeZoneId = zone,\n                RecurringStartMinutes = (int)start.TotalMinutes, RecurringEndMinutes = (int)end.TotalMinutes, RecurringEndDayOffset = end <= start ? 1 : 0,')
write('PixelTiming.cs',text)
text=read('MainWindow.ClassSchedule.cs').replace('var zone = state.Session?.TimeZoneId ?? Options.TimeZoneId;','var zone = Options.TimeZoneId;')
text=text.replace('NexaTime.Local(current.Start, zone).TimeOfDay','TimeSpan.FromMinutes(current.RecurringStartMinutes ?? (int)NexaTime.Local(current.Start, current.TimeZoneId).TimeOfDay.TotalMinutes)')
text=text.replace('NexaTime.Local(currentEnd.End, zone).TimeOfDay','TimeSpan.FromMinutes(currentEnd.RecurringEndMinutes ?? (int)NexaTime.Local(currentEnd.End, currentEnd.TimeZoneId).TimeOfDay.TotalMinutes)')
text=text.replace('        session.TimeZoneId = schedule.TimeZoneId;', '''        session.ScheduleTimeZoneId = schedule.ScheduleTimeZoneId; session.RecurringStartMinutes = schedule.RecurringStartMinutes;
        session.RecurringEndMinutes = schedule.RecurringEndMinutes; session.RecurringEndDayOffset = schedule.RecurringEndDayOffset;
        session.TimeZoneId = schedule.TimeZoneId;''')
write('MainWindow.ClassSchedule.cs',text)
text=read('SettingsDialog.Reliability.cs').replace('                ReliabilityOptions.TimeZoneId = id; QueueSave("time-zone", SaveState); Refresh();','''                foreach (var scheduled in state.Workspace!.Dashboards.Select(d => d.Current.Data.Session).Append(state.Session).Where(s => s?.AutomaticScheduling == true).Cast<Session>())
                    WeeklySchedule.ChangeTimeZone(scheduled, id);
                ReliabilityOptions.TimeZoneId = id; QueueSave("time-zone", SaveState); Refresh();''')
write('SettingsDialog.Reliability.cs',text)
text=read('MainWindow.Reliability.cs')
text=text.replace('seed.Start = occurrence.Start; seed.End = occurrence.End; seed.StoppedAt = null;','seed.Start = occurrence.Start; seed.End = occurrence.End; seed.TimeZoneId = NexaTime.Zone(seed.ScheduleTimeZoneId ?? seed.TimeZoneId).Id; seed.StoppedAt = null;')
text=text.replace('        SystemEvents.PowerModeChanged += PowerChanged;','        SystemEvents.PowerModeChanged += PowerChanged;\n        SystemEvents.TimeChanged += ClockChanged;')
text=text.replace('            SystemEvents.PowerModeChanged -= PowerChanged;','            SystemEvents.TimeChanged -= ClockChanged;\n            SystemEvents.PowerModeChanged -= PowerChanged;')
needle='    private void PowerChanged('
start=text.index(needle)
text=text[:start]+'''    private void ClockChanged(object? sender, EventArgs e)
    {
        TimeZoneInfo.ClearCachedData();
        Dispatcher.BeginInvoke(new Action(() => { if (!closing) { lastReconcile = default; ReconcileSchedule(DateTimeOffset.UtcNow); UpdateClock(); } }));
    }
'''+text[start:]
write('MainWindow.Reliability.cs',text)
# Providers receive the selected time zone explicitly; historical UTC instants remain unchanged.
text=read('Integrations.cs')
text=text.replace('Schedule(string calendar,DateTimeOffset day,CancellationToken token)','Schedule(string calendar,DateTimeOffset day,CancellationToken token,string? timeZone=null)')
text=text.replace('var start=day.LocalDateTime.Date; var end=start.AddDays(1);','var start=NexaTime.Local(day,timeZone).Date; var end=start.AddDays(1);')
text=text.replace('new DateTimeOffset(start).ToString("o")','NexaTime.Instant(start,timeZone).ToString("o")').replace('new DateTimeOffset(end).ToString("o")','NexaTime.Instant(end,timeZone).ToString("o")')
text=text.replace('var localDate=day.LocalDateTime.Date;','var localDate=NexaTime.Local(day,timeZone).Date;')
text=text.replace('new DateTimeOffset(localDate).ToUniversalTime()','NexaTime.Instant(localDate,timeZone)').replace('new DateTimeOffset(localDate.AddDays(1)).ToUniversalTime()','NexaTime.Instant(localDate.AddDays(1),timeZone)')
write('Integrations.cs',text)
text=read('MainWindow.Integrations.cs').replace('Schedule(chosen.Id,DateTimeOffset.Now,token)','Schedule(chosen.Id,DateTimeOffset.UtcNow,token,Options.TimeZoneId)')
write('MainWindow.Integrations.cs',text)
text=read('DashboardContinuity.cs').replace('        if (data.Session is { } session && (session.TimelineEntries', '        if (data.Session is { } recurrence && (recurrence.RecurringStartMinutes is < 0 or > 1439 || recurrence.RecurringEndMinutes is < 0 or > 1439 || recurrence.RecurringEndDayOffset is < 0 or > 7)) throw new InvalidDataException("Invalid recurrence clock values.");\n        if (data.Session is { } session && (session.TimelineEntries')
write('DashboardContinuity.cs',text)
print('Separated factual session timestamps from recurrence wall clocks and selected-zone calendar queries.')
