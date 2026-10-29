using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

public partial class MainWindow
{
    internal void ShowWeeklySchedule() => OpenClassSchedule();
    private void OpenClassSchedule() => OpenMainSettings("Session");
    internal FrameworkElement BuildTimerSettings(Action? draftChanged=null)
    {
        var page=new StackPanel{Margin=new Thickness(0,0,0,18)};
        page.Children.Add(new TextBlock{Text="Pixel Timer",FontSize=23,FontWeight=FontWeights.SemiBold});
        page.Children.Add(new TextBlock{Text="Edits are saved as a draft. Apply timer settings updates the active Space's timer. Recurring weekdays do not multiply session duration.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,16)});
        var zone=Options.TimeZoneId;var initial=state.Session;
        var mode=new ComboBox{ItemsSource=new[]{"Manual Mode","Schedule Mode"},SelectedIndex=initial?.AutomaticScheduling==true?1:0};System.Windows.Automation.AutomationProperties.SetName(mode,"Pixel Timer operating mode");page.Children.Add(mode);
        var name=new TextBox{Text=initial?.Title??state.Draft.Title,MaxLength=100,Margin=new Thickness(0,8,0,12)};page.Children.Add(new TextBlock{Text="Session name"});page.Children.Add(name);
        var start=new ClockPicker{Value=initial is {} from?TimeSpan.FromMinutes(from.RecurringStartMinutes??(int)NexaTime.Local(from.Start,zone).TimeOfDay.TotalMinutes):TimeSpan.FromMinutes(state.Draft.StartMinutes)};
        var end=new ClockPicker{Value=initial is {} to?TimeSpan.FromMinutes(to.RecurringEndMinutes??(int)NexaTime.Local(to.End,zone).TimeOfDay.TotalMinutes):TimeSpan.FromMinutes(state.Draft.EndMinutes)};
        var times=new Grid();times.ColumnDefinitions.Add(new());times.ColumnDefinitions.Add(new());var left=new StackPanel{Margin=new Thickness(0,0,8,0)};left.Children.Add(new TextBlock{Text="Starts (AM / PM)"});left.Children.Add(start);var right=new StackPanel{Margin=new Thickness(8,0,0,0)};right.Children.Add(new TextBlock{Text="Ends (AM / PM)"});right.Children.Add(end);Grid.SetColumn(right,1);times.Children.Add(left);times.Children.Add(right);page.Children.Add(times);
        var startDate=new DarkDatePicker{SelectedDate=initial is {} a?NexaTime.Local(a.Start,zone).Date:NexaTime.Local(DateTimeOffset.UtcNow,zone).Date};var endDate=new DarkDatePicker{SelectedDate=initial is {} b?NexaTime.Local(b.End,zone).Date:startDate.SelectedDate};
        var dates=new StackPanel{Margin=new Thickness(0,10,0,10)};dates.Children.Add(new TextBlock{Text="One-time start date"});dates.Children.Add(startDate);dates.Children.Add(new TextBlock{Text="One-time end date"});dates.Children.Add(endDate);page.Children.Add(dates);
        var days=new WrapPanel{Margin=new Thickness(0,10,0,10)};foreach(var day in new[]{DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday,DayOfWeek.Saturday,DayOfWeek.Sunday})days.Children.Add(new CheckBox{Content=day.ToString()[..3],Tag=day,IsChecked=(initial?.ScheduledDays??state.Draft.ScheduledDays).Contains(day),Margin=new Thickness(0,0,12,8)});
        var dayLabel=new TextBlock{Text="Recurring weekdays"};page.Children.Add(dayLabel);page.Children.Add(days);var shortcuts=new WrapPanel();page.Children.Add(shortcuts);
        var duration=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};page.Children.Add(duration);
        var durationInput=new TextBox{Width=110,HorizontalAlignment=HorizontalAlignment.Left};page.Children.Add(new TextBlock{Text="Duration (minutes)"});page.Children.Add(durationInput);
        var useDuration=new Button{Content="Use duration",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,6,0,12)};page.Children.Add(useDuration);
        var savePreset=new CheckBox{Content="Save this schedule as a reusable preset",IsChecked=true};page.Children.Add(savePreset);
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};page.Children.Add(status);
        var apply=new Button{Content="Apply timer settings",HorizontalAlignment=HorizontalAlignment.Left};page.Children.Add(apply);
        var editingSchedule=mode.SelectedIndex==1;var restoring=false;
        List<DayOfWeek> SelectedDays()=>days.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(DayOfWeek)c.Tag).ToList();
        Session Capture(bool scheduled)
        {
            var from=(startDate.SelectedDate??DateTime.Today)+start.Value;var to=(endDate.SelectedDate??from.Date)+end.Value;
            if(to.Date==from.Date && end.Value<start.Value)to=to.AddDays(1);
            return new Session{Title=name.Text.Trim(),Start=NexaTime.Instant(from,zone),End=NexaTime.Instant(to,zone),ScheduledDays=SelectedDays(),AutomaticScheduling=scheduled,TimeZoneId=zone,ScheduleTimeZoneId=zone,BeforeSessionCountdownFrom=DateTimeOffset.UtcNow};
        }
        void RefreshPreview()
        {
            var span=editingSchedule?end.Value-start.Value:Capture(false).End-Capture(false).Start;
            var overnight=editingSchedule?end.Value<start.Value:(Capture(false).End-Capture(false).Start).TotalHours>=24 || end.Value<start.Value;
            if(editingSchedule && span<TimeSpan.Zero)span+=TimeSpan.FromDays(1);
            duration.Text=$"{DateTime.Today.Add(start.Value):h:mm tt} → {DateTime.Today.Add(end.Value):h:mm tt} · Duration: {span.TotalMinutes:0} minutes"+(overnight?" · Crosses midnight":"")+$" · {NexaTime.Zone(zone).DisplayName}";
            durationInput.Text=span.TotalMinutes.ToString("0",System.Globalization.CultureInfo.InvariantCulture);
            dates.Visibility=editingSchedule?Visibility.Collapsed:Visibility.Visible;days.Visibility=shortcuts.Visibility=dayLabel.Visibility=editingSchedule?Visibility.Visible:Visibility.Collapsed;
        }
        void SaveDraft(){var draft=Capture(editingSchedule);if(editingSchedule)state.ScheduleTimerDraft=draft;else state.ManualTimerDraft=draft;}
        void Edited(){if(restoring)return;try{SaveDraft();RefreshPreview();Changed();draftChanged?.Invoke();}catch(ArgumentException ex){status.Text=ex.Message;}}
        void Shortcut(string label,params DayOfWeek[] selected){var button=new Button{Content=label,Padding=new Thickness(8,4,8,4),Margin=new Thickness(0,0,8,8)};button.Click+=(_,_)=>{foreach(var check in days.Children.OfType<CheckBox>())check.IsChecked=selected.Contains((DayOfWeek)check.Tag);Edited();};shortcuts.Children.Add(button);}
        Shortcut("M/W",DayOfWeek.Monday,DayOfWeek.Wednesday);Shortcut("M/W/F",DayOfWeek.Monday,DayOfWeek.Wednesday,DayOfWeek.Friday);Shortcut("T/Th",DayOfWeek.Tuesday,DayOfWeek.Thursday);Shortcut("Weekdays",DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday);
        mode.SelectionChanged+=(_,_)=>{SaveDraft();editingSchedule=mode.SelectedIndex==1;var draft=editingSchedule?state.ScheduleTimerDraft:state.ManualTimerDraft;restoring=true;try{if(draft!=null){name.Text=draft.Title;startDate.SelectedDate=NexaTime.Local(draft.Start,zone).Date;endDate.SelectedDate=NexaTime.Local(draft.End,zone).Date;start.Value=NexaTime.Local(draft.Start,zone).TimeOfDay;end.Value=NexaTime.Local(draft.End,zone).TimeOfDay;foreach(var check in days.Children.OfType<CheckBox>())check.IsChecked=draft.ScheduledDays.Contains((DayOfWeek)check.Tag);}}finally{restoring=false;}Edited();};
        name.TextChanged+=(_,_)=>Edited();start.TimeChanged+=(_,_)=>Edited();end.TimeChanged+=(_,_)=>Edited();startDate.SelectedDateChanged+=(_,_)=>Edited();endDate.SelectedDateChanged+=(_,_)=>Edited();foreach(var check in days.Children.OfType<CheckBox>())check.Click+=(_,_)=>Edited();
        useDuration.Click+=(_,_)=>{if(!int.TryParse(durationInput.Text,out var minutes)||minutes<=0||minutes>(editingSchedule?1439:10080)){status.Text="Enter a positive duration within "+(editingSchedule?"one day.":"seven days.");return;}var finish=(startDate.SelectedDate??DateTime.Today)+start.Value+TimeSpan.FromMinutes(minutes);restoring=true;end.Value=finish.TimeOfDay;endDate.SelectedDate=finish.Date;restoring=false;Edited();};
        apply.Click+=(_,_)=>{try{var session=editingSchedule?PixelTiming.WeeklyClass(name.Text,SelectedDays(),start.Value,end.Value,zone,DateTimeOffset.UtcNow):Capture(false);if(string.IsNullOrWhiteSpace(session.Title)||session.End<=session.Start||session.End-session.Start>TimeSpan.FromDays(7))throw new ArgumentException("Choose a name and an end after start, within seven days.");session.ConfiguredDurationMinutes=(session.End-session.Start).TotalMinutes;session.IntervalMinutes=state.Session?.IntervalMinutes??10;SaveDraft();status.Text=ApplyClassSchedule(session,savePreset.IsChecked==true)?"Session Settings saved.":"The schedule could not be saved. Your active timer is unchanged.";}catch(ArgumentException ex){status.Text=ex.Message;}};
        RefreshPreview();return page;
    }
    internal bool ApplyClassSchedule(Session schedule, bool savePreset)
    {
        if (!SaveNow()) return false;
        var candidate = CopyWorkspaceState().Workspace!;
        var current = Continuity.Current(candidate, store.UserId).Current;
        if(current.Recap!=null)
        {
            if(!current.SessionValuesReset){Continuity.Archive(Continuity.Current(candidate,store.UserId));current.SessionId=Guid.NewGuid();}
            current.Recap=null;current.ScoreAtEnd=null;current.ScoreAtStart=Continuity.Current(candidate,store.UserId).SpaceProductivityScore;current.SessionValuesReset=false;current.IsActive=true;
        }
        // Editing a schedule preserves the current report identity and all widget data.
        var session = current.Data.Session ?? new Session();
        session.Title = schedule.Title; session.Start = schedule.Start; session.End = schedule.End; session.IntervalMinutes=current.Data.Widgets.FirstOrDefault(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline")?.SummaryIntervalMinutes??schedule.IntervalMinutes;
        session.ScheduleTimeZoneId = schedule.ScheduleTimeZoneId; session.RecurringStartMinutes = schedule.RecurringStartMinutes;
        session.RecurringEndMinutes = schedule.RecurringEndMinutes; session.RecurringEndDayOffset = schedule.RecurringEndDayOffset;
        session.TimeZoneId = schedule.TimeZoneId; session.ScheduledDays = schedule.ScheduledDays;
        session.AutomaticScheduling = schedule.AutomaticScheduling; session.BeforeSessionCountdownFrom = schedule.BeforeSessionCountdownFrom;
        session.ConfiguredDurationMinutes = schedule.ConfiguredDurationMinutes > 0 ? schedule.ConfiguredDurationMinutes : (schedule.End - schedule.Start).TotalMinutes; session.StoppedAt = null; session.FinalizedAt=null;
        current.CompletedCheckpointAt = null; current.ResetDeferred = false; current.ResetPostponedFor = null;
        current.Data.Session = session;
        var from = NexaTime.Local(session.Start, session.TimeZoneId); var to = NexaTime.Local(session.End, session.TimeZoneId);
        var draft = current.Data.Draft;
        draft.Initialized = true; draft.Title = session.Title; draft.StartDate = from.Date; draft.EndDate = to.Date;
        draft.StartMinutes = (int)from.TimeOfDay.TotalMinutes; draft.EndMinutes = (int)to.TimeOfDay.TotalMinutes; draft.ScheduledDays = session.ScheduledDays.ToList();
        draft.Duration = session.ConfiguredDurationMinutes.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        if (savePreset)
        {
            var preset = current.Data.Presets.FirstOrDefault(p => p.Name.Equals(session.Title, StringComparison.OrdinalIgnoreCase));
            if (preset == null) { preset = new Preset { Name = session.Title, AccentColor = current.Data.AccentColor, PixelColor = current.Data.PixelColor }; current.Data.Presets.Add(preset); }
            preset.Minutes = (int)Math.Round(session.ConfiguredDurationMinutes); preset.IntervalMinutes = session.IntervalMinutes;
            preset.StartClockMinutes = draft.StartMinutes; preset.EndClockMinutes = draft.EndMinutes; preset.EndDayOffset = (to.Date - from.Date).Days;
            preset.ScheduledDays = session.ScheduledDays.ToList(); preset.AutomaticScheduling = session.AutomaticScheduling;
            draft.SelectedPresetId = preset.Id; current.PresetId = preset.Id; current.PresetName = preset.Name;
        }
        lastReconcile=default;return CommitWorkspace(candidate);
    }
}
