from pathlib import Path
root=Path(r'M:\Nexa')
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
text=read('MainWindow.SessionWidgets.cs')
needle='                rows.AddRange(session.TimelineEntries'
text=text.replace(needle,'                rows.AddRange(session.IntervalSummaries.OrderBy(p => p.Key).Select(p => $"Interval {p.Key + 1} · Summary\\n{p.Value}"));\n'+needle)
text=text.replace('            var content = string.Join("\\n\\n", rows);','            if (rows.Count == 0 && session != null) rows.Add(string.IsNullOrEmpty(session.HedySessionId) ? "Session notes\\nAdd a timestamped note or open the recorder or Hedy Notes." : "Hedy notes\\nWaiting for notes and interval summaries…");\n            var content = string.Join("\\n\\n", rows);')
write('MainWindow.SessionWidgets.cs',text)
text=read('MainWindow.Reliability.cs').replace('now - lastReconcile < TimeSpan.FromSeconds(1)','(now >= lastReconcile && now - lastReconcile < TimeSpan.FromSeconds(1))')
text=text.replace('                $"Next milestone: {(ledger.Score / 500 + 1) * 500:N0}"','                $"Next milestone: {(ledger.Score / 500 + 1) * 500:N0}" + (expanded.IsChecked == true ? $"\\nAll-time score: {state.Workspace?.AllTimeProductivityScore ?? 0:N0}" : "")')
write('MainWindow.Reliability.cs',text)
text=read('SettingsDialog.Session.cs')
needle='            var startEdited = false;'
text=text.replace(needle,'''            var recurring = new CheckBox { Content = "Automatically repeat on these weekdays", IsChecked = session.AutomaticScheduling, Margin = new Thickness(0, 8, 0, 12) }; page.Children.Add(recurring);
'''+needle)
needle='                if (timingChanged) session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;'
text=text.replace(needle,'''                session.AutomaticScheduling = recurring.IsChecked == true;
                if (timingChanged)
                {
                    session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
                    session.TimeZoneId = NexaTime.Zone(ReliabilityOptions.TimeZoneId).Id;
                    session.ScheduleTimeZoneId = ReliabilityOptions.TimeZoneId;
                    var wallStart = NexaTime.Local(session.Start, session.TimeZoneId); var wallEnd = NexaTime.Local(session.End, session.TimeZoneId);
                    session.RecurringStartMinutes = (int)wallStart.TimeOfDay.TotalMinutes; session.RecurringEndMinutes = (int)wallEnd.TimeOfDay.TotalMinutes;
                    session.RecurringEndDayOffset = (wallEnd.Date - wallStart.Date).Days;
                }
                if (state.Presets.FirstOrDefault(p => p.Id == state.Draft.SelectedPresetId) is { } preset)
                {
                    preset.AutomaticScheduling = session.AutomaticScheduling; preset.ScheduledDays = proposedDays.ToList();
                    preset.StartClockMinutes = session.RecurringStartMinutes; preset.EndClockMinutes = session.RecurringEndMinutes; preset.EndDayOffset = session.RecurringEndDayOffset ?? 0;
                }''')
text=text.replace('            name.TextChanged += (_, _) => SaveSession();','            recurring.Click += (_, _) => SaveSession();\n            name.TextChanged += (_, _) => SaveSession();')
write('SettingsDialog.Session.cs',text)
text=read('MainWindow.Session.cs')
text=text.replace('                session.End = NexaTime.Instant(target, Options.TimeZoneId);','                WeeklySchedule.ChangeTimeZone(session, session.ScheduleTimeZoneId ?? session.TimeZoneId);\n                session.End = NexaTime.Instant(target, Options.TimeZoneId);')
needle='        state.Session.ReachedMilestones.Clear();'
text=text.replace(needle,'''        state.Session.TimeZoneId = NexaTime.Zone(Options.TimeZoneId).Id; state.Session.ScheduleTimeZoneId = Options.TimeZoneId;
        state.Session.RecurringStartMinutes = (int)start.TimeOfDay.TotalMinutes; state.Session.RecurringEndMinutes = (int)end.TimeOfDay.TotalMinutes;
        state.Session.RecurringEndDayOffset = (end.Date - start.Date).Days;
'''+needle)
write('MainWindow.Session.cs',text)
text=read('PixelTiming.cs')
text=text.replace('bool CountUp);','bool CountUp) { public double SessionProgress { get; init; } }')
text=text.replace('return new(TimeSpan.Zero, "SESSION ENDED", "SESSION ENDED", 0, false);','return new(TimeSpan.Zero, "SESSION ENDED", "SESSION ENDED", 0, false) { SessionProgress = session.Progress(now) };')
needle='        if (now < session.Start)'
text=text.replace(needle,'''        if (now >= session.End && WeeklySchedule.Latest(session, now) is { } active && active.Start > session.Start && now < active.End)
        {
            var elapsed = now - active.Start;
            var progress = Math.Clamp(elapsed.TotalSeconds / (active.End - active.Start).TotalSeconds, 0, 1);
            return new(elapsed, "CLASS TIME ELAPSED", "IN SESSION", 1 - progress, true) { SessionProgress = progress };
        }
'''+needle)
text=text.replace('1 - session.Progress(now), true);','1 - session.Progress(now), true) { SessionProgress = session.Progress(now) };')
text=text.replace('"BETWEEN SESSIONS", Math.Clamp(fraction, 0, 1), false);','"BETWEEN SESSIONS", Math.Clamp(fraction, 0, 1), false) { SessionProgress = 1 };')
text=text.replace('return new(TimeSpan.Zero, "SESSION COMPLETE", "SESSION OVER", 0, false);','return new(TimeSpan.Zero, "SESSION COMPLETE", "SESSION OVER", 0, false) { SessionProgress = 1 };')
write('PixelTiming.cs',text)
text=read('MainWindow.xaml.cs')
text=text.replace('        var progress = session?.Progress(now) ?? 0;','        var reading = PixelTiming.Read(session, now);\n        var progress = reading.SessionProgress;')
text=text.replace('        var reading = PixelTiming.Read(session, now);\n        Countdown.Text', '        Countdown.Text')
write('MainWindow.xaml.cs',text)
text=read('tests/Nexa024CoreChecks.cs')
needle='        var wednesday = PixelTiming.WeeklyClass('
text=text.replace(needle,'''        var missedStart = PixelTiming.Read(schedule, NexaTime.Instant(new DateTime(2030, 1, 9, 13, 20, 0), zone));
        Assert(missedStart.CountUp && missedStart.Time == TimeSpan.FromMinutes(20) && Math.Abs(missedStart.SessionProgress - 20d / 75) < 1e-10, "Timer reading reconciles a missed class start even while a reset is blocked");
'''+needle)
write('tests/Nexa024CoreChecks.cs',text)
print('Completed timeline summary access, edited recurrence propagation, and clock-jump/missed-start display handling.')
