from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'MainWindow.PresetTiming.cs',r/'MainWindow.PresetTiming.cs')
edit('MainWindow.xaml.cs',lambda t:t.replace('            ApplyColors(); Changed();','            ApplyColors(); Changed();\n            if(PresetPicker.IsKeyboardFocusWithin && !applyingTimingPreset)ApplyTimingPreset(p);',1))
edit('MainWindow.ClassSchedule.cs',lambda t:t.replace('session.Title = schedule.Title; session.Start = schedule.Start; session.End = schedule.End;','session.Title = schedule.Title; session.Start = schedule.Start; session.End = schedule.End; session.IntervalMinutes=schedule.IntervalMinutes;').replace('        return CommitWorkspace(candidate);','        lastReconcile=default;return CommitWorkspace(candidate);'))
edit('NexaReliability.cs',lambda t:t.replace('if (from > after && from >= seed.Start && to > from)','if (from > after && day>=recordedStart.Date && to > from)').replace('seed.ScheduledDays.Count == 0 || now < seed.Start','seed.ScheduledDays.Count == 0'))
edit('PixelTiming.cs',lambda t:t.replace('''        if(now<session.Start)
        {
            var origin=session.BeforeSessionCountdownFrom ?? session.Start-(session.End-session.Start);
            return Reading(session.Start-now,"NEXT SESSION","SCHEDULED",(session.Start-now).TotalSeconds/Math.Max(.001,(session.Start-origin).TotalSeconds),false,0,origin,session.Start);
        }''','''        if(occurrence==null && (now<session.Start || session.AutomaticScheduling))
        {
            var upcoming=WeeklySchedule.Next(session,now)??new SessionOccurrence(session.Start,session.End);
            var origin=session.BeforeSessionCountdownFrom ?? upcoming.Start-(upcoming.End-upcoming.Start);
            return Reading(upcoming.Start-now,"NEXT SESSION","SCHEDULED",(upcoming.Start-now).TotalSeconds/Math.Max(.001,(upcoming.Start-origin).TotalSeconds),false,0,origin,upcoming.Start);
        }'''))
print('Applied preset timing without replacing report data and corrected same-day recurrence rebasing across time zones.')
