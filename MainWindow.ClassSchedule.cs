using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

public partial class MainWindow
{
    internal void ShowWeeklySchedule() => OpenClassSchedule();
    private void OpenClassSchedule()
    {
        if (sessionEditor != null) { sessionEditor.Activate(); return; }
        var dialog = Dialog("Nexa · Pixel Timer", 580, 620);
        sessionEditor = dialog;
        var page = new StackPanel { Margin = new Thickness(24) };
        page.Children.Add(new TextBlock { Text = "Weekly class schedule", FontSize = 23, FontWeight = FontWeights.SemiBold });
        page.Children.Add(new TextBlock { Text = "Count down until class, count up during class, then count down to the next class. Session pixels fill from the top-left; between classes they clear from the bottom-right.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 18) });
        var name = new TextBox { Text = state.Session?.Title ?? (string.IsNullOrWhiteSpace(state.Draft.Title) ? "Class" : state.Draft.Title), MaxLength = 100 };
        page.Children.Add(new TextBlock { Text = "Class / preset name" }); page.Children.Add(name);
        var chosen = state.Session?.ScheduledDays ?? state.Draft.ScheduledDays;
        var days = new WrapPanel { Margin = new Thickness(0, 12, 0, 10) };
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday })
            days.Children.Add(new CheckBox { Content = day.ToString()[..3], Tag = day, IsChecked = chosen.Contains(day), Margin = new Thickness(0, 0, 12, 8) });
        page.Children.Add(new TextBlock { Text = "Recurring weekdays", Margin = new Thickness(0, 14, 0, 0) }); page.Children.Add(days);
        var shortcuts = new WrapPanel();
        void Shortcut(string label, params DayOfWeek[] selection)
        {
            var button = new Button { Content = label, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 8, 8) };
            button.Click += (_, _) => { foreach (var check in days.Children.OfType<CheckBox>()) check.IsChecked = selection.Contains((DayOfWeek)check.Tag); };
            shortcuts.Children.Add(button);
        }
        Shortcut("M/W", DayOfWeek.Monday, DayOfWeek.Wednesday);
        Shortcut("M/W/F", DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Shortcut("T/Th", DayOfWeek.Tuesday, DayOfWeek.Thursday);
        Shortcut("Weekdays", DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
        page.Children.Add(shortcuts);
        var zone = Options.TimeZoneId;
        var start = new ClockPicker { Value = state.Session is { } current ? TimeSpan.FromMinutes(current.RecurringStartMinutes ?? (int)NexaTime.Local(current.Start, current.TimeZoneId).TimeOfDay.TotalMinutes) : TimeSpan.FromMinutes(state.Draft.StartMinutes) };
        var end = new ClockPicker { Value = state.Session is { } currentEnd ? TimeSpan.FromMinutes(currentEnd.RecurringEndMinutes ?? (int)NexaTime.Local(currentEnd.End, currentEnd.TimeZoneId).TimeOfDay.TotalMinutes) : TimeSpan.FromMinutes(state.Draft.EndMinutes) };
        var times = new Grid(); times.ColumnDefinitions.Add(new()); times.ColumnDefinitions.Add(new());
        var first = new StackPanel { Margin = new Thickness(0, 0, 8, 0) }; first.Children.Add(new TextBlock { Text = "Class starts" }); first.Children.Add(start);
        var second = new StackPanel { Margin = new Thickness(8, 0, 0, 0) }; second.Children.Add(new TextBlock { Text = "Class ends" }); second.Children.Add(end);
        Grid.SetColumn(second, 1); times.Children.Add(first); times.Children.Add(second); page.Children.Add(times);
        var duration = new TextBlock { Margin = new Thickness(0, 8, 0, 12), TextWrapping = TextWrapping.Wrap };
        void RefreshDuration() { var span = end.Value - start.Value; if (span <= TimeSpan.Zero) span += TimeSpan.FromDays(1); duration.Text = $"Duration: {span.TotalMinutes:0} minutes · {NexaTime.Zone(zone).DisplayName}"; }
        start.TimeChanged += (_, _) => RefreshDuration(); end.TimeChanged += (_, _) => RefreshDuration(); RefreshDuration(); page.Children.Add(duration);
        var repeat = new CheckBox { Content = "Automatically schedule and reset on these weekdays", IsChecked = state.Session?.AutomaticScheduling ?? true, Margin = new Thickness(0, 8, 0, 8) }; page.Children.Add(repeat);
        var startDate=new DatePicker{SelectedDate=state.Session is {} original?NexaTime.Local(original.Start,zone).Date:NexaTime.Local(DateTimeOffset.UtcNow,zone).Date};
        var endDate=new DatePicker{SelectedDate=state.Session is {} ending?NexaTime.Local(ending.End,zone).Date:startDate.SelectedDate};
        var dates=new StackPanel{Margin=new Thickness(0,8,0,8),IsEnabled=repeat.IsChecked!=true};dates.Children.Add(new TextBlock{Text="One-time start date"});dates.Children.Add(startDate);dates.Children.Add(new TextBlock{Text="One-time end date"});dates.Children.Add(endDate);page.Children.Add(dates);
        repeat.Checked+=(_,_)=>dates.IsEnabled=false;repeat.Unchecked+=(_,_)=>dates.IsEnabled=true;
        var durationInput=new TextBox{Text=state.Session?.ConfiguredDurationMinutes.ToString("0")??"75",Width=100,HorizontalAlignment=HorizontalAlignment.Left};page.Children.Add(new TextBlock{Text="Duration (minutes)"});page.Children.Add(durationInput);
        var setDuration=new Button{Content="Use duration",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,4,0,10)};page.Children.Add(setDuration);
        setDuration.Click+=(_,_)=>{if(int.TryParse(durationInput.Text,out var minutes) && minutes>0 && minutes<=(repeat.IsChecked==true?1439:10080)){var finish=(startDate.SelectedDate??NexaTime.Local(DateTimeOffset.UtcNow,zone).Date)+start.Value+TimeSpan.FromMinutes(minutes);end.Value=finish.TimeOfDay;endDate.SelectedDate=finish.Date;}};
        var savePreset = new CheckBox { Content = "Save this schedule as a reusable preset", IsChecked = true }; page.Children.Add(savePreset);
        var status = new TextBlock { Margin = new Thickness(0, 12, 0, 12), TextWrapping = TextWrapping.Wrap }; page.Children.Add(status);
        var apply = new Button { Content = "Save weekly schedule", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 12) };
        apply.Click += (_, _) =>
        {
            try
            {
                var selected=days.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(DayOfWeek)c.Tag).ToList();
                var schedule=repeat.IsChecked==true?PixelTiming.WeeklyClass(name.Text,selected,start.Value,end.Value,zone,DateTimeOffset.UtcNow):new Session{Title=name.Text.Trim(),Start=NexaTime.Instant((startDate.SelectedDate??DateTime.Today)+start.Value,zone),End=NexaTime.Instant((endDate.SelectedDate??DateTime.Today)+end.Value,zone),TimeZoneId=zone,ScheduleTimeZoneId=zone,ScheduledDays=selected,BeforeSessionCountdownFrom=DateTimeOffset.UtcNow};
                if(string.IsNullOrWhiteSpace(schedule.Title)||schedule.End<=schedule.Start||schedule.End-schedule.Start>TimeSpan.FromDays(7))throw new ArgumentException("Choose an end after start, within seven days.");
                schedule.ConfiguredDurationMinutes=(schedule.End-schedule.Start).TotalMinutes;
                schedule.IntervalMinutes=state.Session?.IntervalMinutes??10;
                schedule.AutomaticScheduling = repeat.IsChecked == true;
                if (ApplyClassSchedule(schedule, savePreset.IsChecked == true)) dialog.CloseWithFade();
                else status.Text = "The schedule could not be saved. Your current dashboard is unchanged.";
            }
            catch (ArgumentException ex) { status.Text = ex.Message; }
        };
        page.Children.Add(apply);
        var once = new Button { Content = "Edit a one-time end date instead", HorizontalAlignment = HorizontalAlignment.Left };
        once.Click += (_, _) => { dialog.Close(); EditOneTimeTarget(this, new RoutedEventArgs()); }; page.Children.Add(once);
        dialog.Content = new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        dialog.Closed += (_, _) => sessionEditor = null; dialog.ShowDismissible();
    }
    internal bool ApplyClassSchedule(Session schedule, bool savePreset)
    {
        if (!SaveNow()) return false;
        var candidate = CopyWorkspaceState().Workspace!;
        var current = Continuity.Current(candidate, store.UserId).Current;
        // Editing a schedule preserves the current report identity and all widget data.
        var session = current.Data.Session ?? new Session();
        session.Title = schedule.Title; session.Start = schedule.Start; session.End = schedule.End; session.IntervalMinutes=current.Data.Widgets.FirstOrDefault(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline")?.SummaryIntervalMinutes??schedule.IntervalMinutes;
        session.ScheduleTimeZoneId = schedule.ScheduleTimeZoneId; session.RecurringStartMinutes = schedule.RecurringStartMinutes;
        session.RecurringEndMinutes = schedule.RecurringEndMinutes; session.RecurringEndDayOffset = schedule.RecurringEndDayOffset;
        session.TimeZoneId = schedule.TimeZoneId; session.ScheduledDays = schedule.ScheduledDays;
        session.AutomaticScheduling = schedule.AutomaticScheduling; session.BeforeSessionCountdownFrom = schedule.BeforeSessionCountdownFrom;
        session.ConfiguredDurationMinutes = schedule.ConfiguredDurationMinutes; session.StoppedAt = null;
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
