using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private Window? sessionEditor;
    internal static DateTime SuggestedPixelTarget(DateTime localNow) => localNow.AddHours(1);
    private readonly Dictionary<DayOfWeek, CheckBox> sessionDayChecks = [];
    private List<DayOfWeek> SelectedSessionDays() => sessionDayChecks.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key).ToList();
    internal void SetSessionDays(IEnumerable<DayOfWeek> days)
    {
        var selected = days.ToHashSet(); foreach (var pair in sessionDayChecks) pair.Value.IsChecked = selected.Contains(pair.Key);
    }
    private void SessionTimeEditClick(object sender, RoutedEventArgs e) => OpenClassSchedule();
    private void EditOneTimeTarget(object sender, RoutedEventArgs e)
    {
        if (sessionEditor != null) { sessionEditor.Activate(); return; }
        var dialog = Dialog("nexa · Pixel Timer", 390, 300);
        sessionEditor = dialog;
        var content = new StackPanel { Margin = new Thickness(24) };
        content.Children.Add(new TextBlock { Text = "When should this session end?", FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) });
        var initial = (state.Session is { } initialSession ? NexaTime.Local(initialSession.End, Options.TimeZoneId).DateTime : (DateTime?)null) ?? state.Draft.EndDate.AddMinutes(state.Draft.EndMinutes);
        var date = new DatePicker { SelectedDate = initial.Date, Margin = new Thickness(0, 0, 0, 10) };
        var time = new TextBox { Text = initial.ToString("h:mm tt", CultureInfo.CurrentCulture), Margin = new Thickness(0, 0, 0, 12) };
        content.Children.Add(new TextBlock { Text = "Date" }); content.Children.Add(date);
        content.Children.Add(new TextBlock { Text = "Time" }); content.Children.Add(time);
        var today = new Button { Content = "Use Today's Date", Style = (Style)FindResource("PrimaryButton"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 14) };
        today.Click += (_, _) => { var target = SuggestedPixelTarget(NexaTime.Local(DateTimeOffset.UtcNow, Options.TimeZoneId).DateTime); date.SelectedDate = target.Date; time.Text = target.ToString("h:mm tt", CultureInfo.CurrentCulture); };
        content.Children.Add(today);
        var status = new TextBlock { Foreground = Brush("MutedBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) }; content.Children.Add(status);
        var apply = new Button { Content = "Set target", HorizontalAlignment = HorizontalAlignment.Left };
        apply.Click += (_, _) =>
        {
            if (date.SelectedDate == null || !DateTime.TryParse(time.Text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var parsed)) { status.Text = "Choose a valid date and time."; return; }
            var target = date.SelectedDate.Value.Date + parsed.TimeOfDay;
            if (NexaTime.Zone(Options.TimeZoneId).IsInvalidTime(target) || NexaTime.Instant(target, Options.TimeZoneId) <= DateTimeOffset.UtcNow) { status.Text = "Choose a future local time."; return; }
            if (state.Session is { } session)
            {
                if (NexaTime.Instant(target, Options.TimeZoneId) <= session.Start || NexaTime.Instant(target, Options.TimeZoneId) > session.Start.AddDays(7)) { status.Text = "Target must be after session start and within seven days."; return; }
                WeeklySchedule.ChangeTimeZone(session, session.ScheduleTimeZoneId ?? session.TimeZoneId);
                session.End = NexaTime.Instant(target, Options.TimeZoneId);
                session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
                session.ReachedMilestones.Clear(); Milestones.Collect(session, DateTimeOffset.Now);
                BuildTimeline(); UpdateClock();
            }
            state.Draft.EndDate = target.Date; state.Draft.EndMinutes = target.Hour * 60 + target.Minute;
            EndDate.SelectedDate = target.Date; EndTime.Value = target.TimeOfDay;
            Changed("Target updated."); if (SaveNow()) dialog.CloseWithFade();
        };
        content.Children.Add(apply);
        dialog.Content = content;
        dialog.Closed += (_, _) => sessionEditor = null;
        dialog.ShowDismissible();
    }
    private void InitializeInlineSession()
    {
        Detach(TitleInput);
        TitleInput.FontSize = 16; TitleInput.FontWeight = FontWeights.SemiBold;
        TitleInput.Background = Brushes.Transparent; TitleInput.BorderThickness = new Thickness(0);
        TitleInput.Padding = new Thickness(0); TitleInput.ToolTip = "Edit session name";
        SessionNameHost.Content = TitleInput;
        TitleInput.TextChanged += (_, _) =>
        {
            if (!ready || !TitleInput.IsKeyboardFocusWithin || state.Session is not { } session || string.IsNullOrWhiteSpace(TitleInput.Text)) return;
            session.Title = TitleInput.Text.Trim(); Changed(); UpdateClock();
        };
        TitleInput.LostKeyboardFocus += (_, _) =>
        {
            if (!ready || state.Session is not { } session || string.IsNullOrWhiteSpace(TitleInput.Text)) return;
            if (session.Title == TitleInput.Text.Trim()) return;
            session.Title = TitleInput.Text.Trim(); UpdateClock(); Changed(); SaveNow();
        };
        var times = new WrapPanel(); InlineSessionHost.Children.Add(times);
        void TimeGroup(string label, FrameworkElement date, FrameworkElement time)
        {
            var group = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 6) };
            group.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0), Foreground = Brush("MutedBrush"), FontSize = 12 });
            Detach(date); Detach(time); date.Width = 110; time.Width = 104;
            date.Margin = new Thickness(0, 0, 6, 0); group.Children.Add(date); group.Children.Add(time); times.Children.Add(group);
        }
        TimeGroup("Start", StartDate, StartTime); TimeGroup("End", EndDate, EndTime);
        var days = new WrapPanel { Margin = new Thickness(0, 4, 0, 10) };
        days.Children.Add(new TextBlock { Text = "Days", Foreground = Brush("MutedBrush"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day))
        {
            var check = new CheckBox { Content = day.ToString()[..3], Margin = new Thickness(0, 0, 10, 4), IsChecked = state.Draft.ScheduledDays.Count == 0 ? day == (StartDate.SelectedDate ?? DateTime.Today).DayOfWeek : state.Draft.ScheduledDays.Contains(day) };
            check.Click += (_, _) => Changed(); sessionDayChecks[day] = check; days.Children.Add(check);
        }
        InlineSessionHost.Children.Add(days);
        var apply = new Button { Content = "Apply times", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 6), ToolTip = "Update this session without clearing notes or creating a new session" };
        apply.Click += (_, _) => { if (ApplyInlineSessionTimes()) sessionEditor?.Close(); }; times.Children.Add(apply);
        var actions = new WrapPanel(); InlineSessionHost.Children.Add(actions);
        void Field(string label, FrameworkElement input, double width)
        {
            var group = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 8, 6) };
            group.Children.Add(new TextBlock { Text = label, Foreground = Brush("MutedBrush"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) });
            Detach(input); input.Width = width; group.Children.Add(input); actions.Children.Add(group);
        }
        Field("Minutes", DurationInput, 50); Field("Interval", IntervalInput, 45);
        foreach (var button in new[] { StartButton, EndButton }) { Detach(button); button.Margin = new Thickness(0, 0, 6, 6); button.Padding = new Thickness(10, 6, 10, 6); actions.Children.Add(button); }
        var hedySettings = new Button { Content = "Hedy settings", Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 0, 6) };
        hedySettings.Click += HedySettingsClick; actions.Children.Add(hedySettings);
        Detach(HedySessionPanel); ((StackPanel)TimelinePanel.Child).Children.Add(HedySessionPanel);
    }

    internal bool ApplyInlineSessionTimes()
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) || !InputRules.ValidMinutes(IntervalInput.Text, out var interval))
        { Error("Enter a session name and a valid interval in minutes."); return false; }
        if (StartDate.SelectedDate == null || EndDate.SelectedDate == null ||
            !DateTime.TryParse(StartTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var startTime) ||
            !DateTime.TryParse(EndTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var endTime))
        { Error("Enter both dates and valid start and end times."); return false; }
        var start = StartDate.SelectedDate.Value.Date + startTime.TimeOfDay;
        var end = EndDate.SelectedDate.Value.Date + endTime.TimeOfDay;
        if (NexaTime.Zone(Options.TimeZoneId).IsInvalidTime(start) || NexaTime.Zone(Options.TimeZoneId).IsInvalidTime(end))
        { Error("Choose a time outside the daylight-saving clock change."); return false; }
        var selectedDays = SelectedSessionDays();
        if (selectedDays.Count == 0) { Error("Choose at least one day for this session."); return false; }
        var proposed = new Session { Title = TitleInput.Text.Trim(), Start = NexaTime.Instant(start, Options.TimeZoneId), End = NexaTime.Instant(end, Options.TimeZoneId), TimeZoneId = Options.TimeZoneId, IntervalMinutes = interval, ScheduledDays = selectedDays, ConfiguredDurationMinutes = (end - start).TotalMinutes };
        if (!InputRules.ValidSession(proposed) || proposed.End <= DateTimeOffset.Now)
        { Error("End must be in the future, after start, and within 7 days of start."); return false; }
        if (state.Session == null) { BeginSession(proposed); return state.Session != null; }
        // Change timestamps in place: identity, transcripts and interval notes survive.
        state.Session.Title = proposed.Title; state.Session.Start = proposed.Start; state.Session.End = proposed.End;
        state.Session.IntervalMinutes = interval; state.Session.StoppedAt = null; state.Session.FinalizedAt=null; state.Session.ScheduledDays = selectedDays; state.Session.ConfiguredDurationMinutes = proposed.ConfiguredDurationMinutes;
        state.Session.TimeZoneId = NexaTime.Zone(Options.TimeZoneId).Id; state.Session.ScheduleTimeZoneId = Options.TimeZoneId;
        state.Session.RecurringStartMinutes = (int)start.TimeOfDay.TotalMinutes; state.Session.RecurringEndMinutes = (int)end.TimeOfDay.TotalMinutes;
        state.Session.RecurringEndDayOffset = (end.Date - start.Date).Days;
        state.Session.ReachedMilestones.Clear(); Milestones.Collect(state.Session, DateTimeOffset.Now);
        completionDismissed = false; flashUntil = DateTimeOffset.MinValue;
        if (hedy != null && !string.IsNullOrWhiteSpace(state.Session.HedySessionId)) hedyTimer.Start();
        BuildTimeline(); UpdateClock(); Changed("Session times updated."); return SaveNow();
    }

    private void ResetSessionTimerClick(object sender, RoutedEventArgs e)
    {
        if (state.Session?.AutomaticScheduling == true) return;
        if (state.Session is not { } session) { Error("Start or schedule a session before resetting its timer."); return; }
        var dialog = Dialog("nexa · Reset timer", 460, 250);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Reset this timer?", FontSize = 22, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "The countdown will return to its configured session length. Notes, widgets, history and Space data will stay in place.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 22) });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) }; cancel.Click += (_, _) => dialog.CloseWithFade(); actions.Children.Add(cancel);
        var reset = new Button { Content = "Reset", MinWidth = 90, Style = (Style)FindResource("PrimaryButton") };
        reset.Click += (_, _) =>
        {
            var minutes = session.ConfiguredDurationMinutes > 0 ? session.ConfiguredDurationMinutes : Math.Max(1, (session.End - session.Start).TotalMinutes);
            var now = DateTimeOffset.Now; session.Start = now; session.End = now.AddMinutes(minutes); session.StoppedAt = null; session.FinalizedAt=null; session.ReachedMilestones.Clear();
            completionDismissed = false; flashUntil = DateTimeOffset.MinValue; BuildTimeline(); UpdateClock(); Changed("Timer reset."); SaveNow(); dialog.CloseWithFade();
        };
        actions.Children.Add(reset); panel.Children.Add(actions); dialog.Content = panel; dialog.ShowDismissible();
    }

    private void HedySettingsClick(object sender, RoutedEventArgs e) => OpenMainSettings("Hedy");
    private void RefreshSessionHedyClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(HedyKeyInput.Password)) HedySettingsClick(sender, e);
        else ConnectHedyClick(sender, e);
    }
}
