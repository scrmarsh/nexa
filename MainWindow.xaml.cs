using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow : Window
{
    private readonly StateStore store;
    internal AppState state;
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer saveDelay = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly List<Border> pixels = [];
    private readonly List<Border> intervals = [];
    private bool ready;
    private bool dirty;
    private bool storyOpen;
    private int previousHead = -1;
    private bool headAnimating;
    private double fullWidth = 1220, fullHeight = 880;
    private bool fullscreen;
    private Rect restoreBounds;
    private WindowState restoreWindowState;
    private DateTimeOffset flashUntil;
    private bool completionDismissed;
    private readonly SolidColorBrush flashBrush = new(Colors.White);
    private SolidColorBrush completionBrush = new(Colors.White);

    public MainWindow() : this(new StateStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionDashboard", "state.json"))) { }

    internal MainWindow(StateStore store)
    {
        this.store = store;
        state = store.Load();
        InitializeComponent();
        NexaChrome.Apply(this);
        for (var i = 0; i < 540; i++)
        {
            var pixel = new Border { Margin = new Thickness(2), CornerRadius = new CornerRadius(2), Background = Brush("PixelOffBrush") };
            pixels.Add(pixel);
            PixelGrid.Children.Add(pixel);
        }
        NotesBox.Text = state.Notes;
        NotesToggle.IsChecked = state.ShowNotes;
        ChecklistToggle.IsChecked = state.ShowChecklist;
        PinToggle.IsChecked = state.AlwaysOnTop;
        AccentInput.Text = state.AccentColor;
        PixelInput.Text = state.PixelColor;
        StartDate.SelectedDate = EndDate.SelectedDate = DateTime.Today;
        StartTime.Text = DateTime.Now.ToString("HH:mm");
        EndTime.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
        EndDate.SelectedDate = DateTime.Now.AddHours(1).Date;
        RefreshPresets(state.Presets.FirstOrDefault()?.Id);
        RenderTasks();
        ApplyColors();
        ApplySettings();
        BuildTimeline();
        InitializeHedy();
        ready = true;
        UpdateClock();
        Feedback.Text = store.Warning ?? "Saved on this PC • Your workspace is ready";
        clock.Tick += (_, _) => UpdateClock();
        saveDelay.Tick += async (_, _) => { saveDelay.Stop(); await SaveInBackground(); };
        InitializeContinuity();
        InitializeWidgets();
        clock.Start();
        Closing += (_, e) =>
        {
            if (!SaveForClose()) e.Cancel = true;
        };
        Closed += (_, _) => { clock.Stop(); saveDelay.Stop(); autosave.Stop(); closing = true; StopHedy(); };
    }

    private Brush Brush(string key) => (Brush)FindResource(key);
    private void Changed(string? message = null)
    {
        if (!ready) return;
        dirty = true; revision++;
        saveDelay.Stop();
        saveDelay.Start();
        if (message != null) Feedback.Text = message;
    }

    internal bool SaveNow()
    {
        saveDelay.Stop(); saveTicket++;
        try
        {
            // Await only worker I/O here; its completion does not depend on the dispatcher.
            try { pendingWrite?.GetAwaiter().GetResult(); } catch (Exception ex) when (SaveException(ex)) { }
            CaptureDraft(); ShowSaveStatus("Saving...");
            store.Save(state); dirty = false;
            ShowSaveStatus("Saved at " + DateTime.Now.ToString("t"), fade: true); return true;
        }
        catch (Exception ex) when (SaveException(ex))
        { dirty = true; ShowSaveStatus("Save failed", warning: true); Feedback.Text = "Could not save changes: " + ex.Message; return false; }
    }

    private void Error(string text) => Feedback.Text = text;
    private bool ReadPreset(out Preset preset)
    {
        preset = new Preset();
        if (string.IsNullOrWhiteSpace(TitleInput.Text)) { Error("Give your session a title first."); return false; }
        if (!InputRules.ValidMinutes(DurationInput.Text, out var minutes) || !InputRules.ValidMinutes(IntervalInput.Text, out var interval))
        { Error("Duration and interval must be whole minutes between 1 and 10,080 (7 days)."); return false; }
        preset.Name = TitleInput.Text.Trim(); preset.Minutes = minutes; preset.IntervalMinutes = interval;
        preset.AccentColor = state.AccentColor; preset.PixelColor = state.PixelColor;
        preset.StartClockMinutes = (int)StartTime.Value.TotalMinutes;
        preset.EndClockMinutes = (int)EndTime.Value.TotalMinutes;
        preset.EndDayOffset = Math.Clamp((int)((EndDate.SelectedDate ?? DateTime.Today) - (StartDate.SelectedDate ?? DateTime.Today)).TotalDays, 0, 7);
        return true;
    }

    private void StartClick(object sender, RoutedEventArgs e)
    {
        if (!ReadPreset(out var preset)) return;
        var now = DateTimeOffset.Now;
        BeginSession(new Session { Title = preset.Name, Start = now, End = now.AddMinutes(preset.Minutes), IntervalMinutes = preset.IntervalMinutes });
    }

    private void ScheduleClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) || !InputRules.ValidMinutes(IntervalInput.Text, out var interval))
        { Error("Enter a title and a valid interval in minutes."); return; }
        if (StartDate.SelectedDate == null || EndDate.SelectedDate == null ||
            !DateTime.TryParse(StartTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var startTime) ||
            !DateTime.TryParse(EndTime.Text, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var endTime))
        { Error("Enter both dates and valid times, such as 09:00 or 2:30 PM."); return; }
        var start = StartDate.SelectedDate.Value.Date + startTime.TimeOfDay;
        var end = EndDate.SelectedDate.Value.Date + endTime.TimeOfDay;
        if (TimeZoneInfo.Local.IsInvalidTime(start) || TimeZoneInfo.Local.IsInvalidTime(end))
        { Error("That time falls in a daylight-saving clock change. Choose a different time."); return; }
        var session = new Session { Title = TitleInput.Text.Trim(), Start = new DateTimeOffset(start), End = new DateTimeOffset(end), IntervalMinutes = interval };
        if (!InputRules.ValidSession(session) || session.End <= DateTimeOffset.Now)
        { Error("End must be in the future, after start, and within 7 days of start. For overnight sessions, use tomorrow's end date."); return; }
        BeginSession(session);
    }

    private void BeginSession(Session session)
    {
        if (!SaveNow()) return;
        CloseNoteEditors();
        var candidate = CopyWorkspaceState();
        var dashboard = Continuity.Current(candidate.Workspace!, store.UserId);
        Continuity.Archive(dashboard);
        dashboard.Current = new DashboardSession { UserId = store.UserId, DashboardId = dashboard.DashboardId, Data = Continuity.Capture(candidate), Title = session.Title };
        dashboard.Current.Data.Session = session;
        CommitWorkspace(candidate.Workspace!);
    }

    private void EndClick(object sender, RoutedEventArgs e)
    {
        if (state.Session == null) return;
        state.Session.StoppedAt = DateTimeOffset.Now;
        UpdateClock(); Changed(); SaveNow();
    }

    private void RefreshPresets(Guid? selected)
    {
        PresetPicker.ItemsSource = null;
        PresetPicker.ItemsSource = state.Presets;
        PresetPicker.SelectedItem = state.Presets.FirstOrDefault(p => p.Id == selected);
    }

    private void PresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (PresetPicker.SelectedItem is not Preset p) return;
        TitleInput.Text = p.Name; DurationInput.Text = p.Minutes.ToString(); IntervalInput.Text = p.IntervalMinutes.ToString();
        if (p.StartClockMinutes.HasValue && p.EndClockMinutes.HasValue)
        {
            StartTime.Value = TimeSpan.FromMinutes(p.StartClockMinutes.Value); EndTime.Value = TimeSpan.FromMinutes(p.EndClockMinutes.Value);
            StartDate.SelectedDate = DateTime.Today; EndDate.SelectedDate = DateTime.Today.AddDays(p.EndDayOffset);
        }
        if (ready)
        {
            var theme = Theme.ForName(p.Name);
            state.AccentColor = InputRules.IsColor(p.AccentColor) ? p.AccentColor : theme.Accent;
            state.PixelColor = InputRules.IsColor(p.PixelColor) ? p.PixelColor : theme.Pixel;
            AccentInput.Text = state.AccentColor; PixelInput.Text = state.PixelColor;
            ThemePicker.SelectedIndex = -1;
            ApplyColors(); Changed();
        }
    }

    private void SavePresetClick(object sender, RoutedEventArgs e)
    {
        if (!ReadPreset(out var preset) || !ValidatePresetTimes()) return;
        state.Presets.Add(preset); RefreshPresets(preset.Id); Changed(); SaveNow();
    }

    private void UpdatePresetClick(object sender, RoutedEventArgs e)
    {
        if (PresetPicker.SelectedItem is not Preset selected) { Error("Select a saved preset to update."); return; }
        if (!ReadPreset(out var preset) || !ValidatePresetTimes()) return;
        selected.Name = preset.Name; selected.Minutes = preset.Minutes; selected.IntervalMinutes = preset.IntervalMinutes;
        selected.AccentColor = preset.AccentColor; selected.PixelColor = preset.PixelColor;
        selected.StartClockMinutes = preset.StartClockMinutes; selected.EndClockMinutes = preset.EndClockMinutes; selected.EndDayOffset = preset.EndDayOffset;
        RefreshPresets(selected.Id); Changed(); SaveNow();
    }

    private void DeletePresetClick(object sender, RoutedEventArgs e)
    {
        if (PresetPicker.SelectedItem is not Preset selected) { Error("Select a saved preset to delete."); return; }
        state.Presets.Remove(selected); RefreshPresets(state.Presets.FirstOrDefault()?.Id); Changed(); SaveNow();
    }
    private bool ValidatePresetTimes()
    {
        if (StartDate.SelectedDate == null || EndDate.SelectedDate == null) { Error("Choose start and end dates before saving the preset."); return false; }
        var span = EndDate.SelectedDate.Value.Date + EndTime.Value - (StartDate.SelectedDate.Value.Date + StartTime.Value);
        if (span <= TimeSpan.Zero || span > TimeSpan.FromDays(7)) { Error("Preset end must be after start, within seven days. Use the next end date for overnight sessions."); return false; }
        return true;
    }

    private void NotesChanged(object sender, TextChangedEventArgs e) { if (ready) { state.Notes = NotesBox.Text; Changed(); } }
    private void AddTaskClick(object sender, RoutedEventArgs e)
    {
        var text = TaskInput.Text.Trim();
        if (text.Length == 0) return;
        state.Tasks.Add(new TaskItem { Text = text }); TaskInput.Clear(); RenderTasks(); Changed();
    }
    private void TaskInputKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) { AddTaskClick(sender, e); e.Handled = true; } }
    private void RenderTasks()
    {
        TaskList.Children.Clear();
        foreach (var item in state.Tasks)
        {
            var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
            var remove = new Button { Content = "×", Padding = new Thickness(8, 2, 8, 2), ToolTip = "Remove item" };
            System.Windows.Automation.AutomationProperties.SetName(remove, "Remove " + item.Text);
            DockPanel.SetDock(remove, Dock.Right);
            remove.Click += (_, _) => { state.Tasks.Remove(item); RenderTasks(); Changed(); };
            row.Children.Add(remove);
            var check = new CheckBox { Content = new TextBlock { Text = item.Text, TextWrapping = TextWrapping.Wrap }, IsChecked = item.Done, Margin = new Thickness(0, 4, 8, 4) };
            check.Click += (_, _) => { item.Done = check.IsChecked == true; Changed(); };
            row.Children.Add(check); TaskList.Children.Add(row);
        }
    }

    private void SettingsChanged(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        state.ShowNotes = NotesToggle.IsChecked == true; state.ShowChecklist = ChecklistToggle.IsChecked == true; state.AlwaysOnTop = PinToggle.IsChecked == true;
        ApplySettings(); Changed();
    }
    private void ApplySettings()
    {
        Topmost = state.AlwaysOnTop;
        NotesPanel.Visibility = state.ShowNotes ? Visibility.Visible : Visibility.Collapsed;
        ChecklistPanel.Visibility = state.ShowChecklist ? Visibility.Visible : Visibility.Collapsed;
    }
    private void ColorsClick(object sender, RoutedEventArgs e)
    {
        if (!InputRules.IsColor(AccentInput.Text.Trim()) || !InputRules.IsColor(PixelInput.Text.Trim())) { Error("Use six-digit hex colors, for example #1DB954 or #2786FF."); return; }
        state.AccentColor = AccentInput.Text.Trim(); state.PixelColor = PixelInput.Text.Trim(); ApplyColors(); UpdateClock(); Changed();
    }
    private void ApplyColors()
    {
        if (previousHead >= 0 && previousHead < pixels.Count) pixels[previousHead].BeginAnimation(OpacityProperty, null);
        Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(state.AccentColor));
        Resources["PixelBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(state.PixelColor));
        var color = ((SolidColorBrush)Resources["AccentBrush"]).Color;
        Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb((byte)(5 + color.R * .018), (byte)(6 + color.G * .018), (byte)(7 + color.B * .018)));
        Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb((byte)(14 + color.R * .07), (byte)(14 + color.G * .07), (byte)(14 + color.B * .07)));
        Resources["InputBrush"] = new SolidColorBrush(Color.FromRgb((byte)(7 + color.R * .025), (byte)(7 + color.G * .025), (byte)(7 + color.B * .025)));
        Resources["PixelOffBrush"] = new SolidColorBrush(Color.FromRgb((byte)(14 + color.R * .04), (byte)(14 + color.G * .04), (byte)(14 + color.B * .04)));
        Resources["PanelBrush"] = new SolidColorBrush(Color.FromRgb((byte)(8 + color.R * .045), (byte)(10 + color.G * .045), (byte)(10 + color.B * .045)));
        Resources["LineBrush"] = new SolidColorBrush(Color.FromRgb((byte)(24 + color.R * .12), (byte)(24 + color.G * .12), (byte)(24 + color.B * .12)));
        var pixelColor = ((SolidColorBrush)Resources["PixelBrush"]).Color;
        completionBrush = new SolidColorBrush(Color.FromRgb((byte)(220 + pixelColor.R * .13), (byte)(220 + pixelColor.G * .13), (byte)(220 + pixelColor.B * .13)));
        previousHead = -1;
        headAnimating = false;
        if (storyOpen) RenderStory();
    }
    private void ThemeSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || ThemePicker.SelectedIndex < 0) return;
        var theme = Theme.ForName(new[] { "Lecture", "Focused work", "Meeting", "Studying" }[ThemePicker.SelectedIndex]);
        state.AccentColor = AccentInput.Text = theme.Accent; state.PixelColor = PixelInput.Text = theme.Pixel;
        ApplyColors(); UpdateClock(); Changed();
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitClick(object sender, RoutedEventArgs e) => Close();
    private void FullscreenClick(object sender, RoutedEventArgs e) => ToggleFullscreen();
    internal void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            if (state.Compact) { SetCompact(false); Changed(); }
            restoreBounds = new Rect(Left, Top, Width, Height); restoreWindowState = WindowState;
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized; fullscreen = true; FullscreenButton.Content = "Restore";
        }
        else
        {
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
            Left = restoreBounds.Left; Top = restoreBounds.Top; Width = restoreBounds.Width; Height = restoreBounds.Height;
            WindowState = restoreWindowState; fullscreen = false; FullscreenButton.Content = "Fullscreen";
        }
    }
    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11 || (e.Key == Key.Escape && fullscreen)) { ToggleFullscreen(); e.Handled = true; }
    }
    private void DismissCompletionClick(object sender, RoutedEventArgs e) { completionDismissed = true; CompletionBanner.Visibility = Visibility.Collapsed; }
    private void CompactClick(object sender, RoutedEventArgs e) { SetCompact(!state.Compact); Changed(); }
    private void SetCompact(bool compact)
    {
        if (fullscreen) ToggleFullscreen();
        if (compact && FullContent.Visibility == Visibility.Visible && ready) { state.WindowWidth = fullWidth = Width; state.WindowHeight = fullHeight = Height; }
        state.Compact = compact;
        Feedback.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        Toolbar.Visibility = FullContent.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        ExpandButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        WindowState = WindowState.Normal;
        MinWidth = compact ? 560 : Math.Min(960, SystemParameters.WorkArea.Width); MinHeight = compact ? 290 : Math.Min(660, SystemParameters.WorkArea.Height);
        Width = compact ? 630 : Math.Min(fullWidth, SystemParameters.WorkArea.Width); Height = compact ? 310 : Math.Min(fullHeight, SystemParameters.WorkArea.Height);
        SessionTitle.FontSize = compact ? 18 : 24;
        Countdown.FontSize = compact ? 32 : 44;
    }

    internal static string ClockText(TimeSpan span, bool roundUp = true)
    {
        var seconds = Math.Max(0L, (long)(roundUp ? Math.Ceiling(span.TotalSeconds) : Math.Floor(span.TotalSeconds)));
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }

    internal void UpdateClock(DateTimeOffset? at = null)
    {
        UpdateWidgetClocks(at ?? DateTimeOffset.Now);
        var session = state.Session;
        var now = at ?? DateTimeOffset.Now;
        var progress = session?.Progress(now) ?? 0;
        var complete = session != null && session.StoppedAt == null && now >= session.End;
        if (session != null)
        {
            var milestones = Milestones.Collect(session, now);
            if (milestones.Count > 0) { flashUntil = now.AddSeconds(6); MilestoneLabel.Text = string.Join(" · ", milestones); Changed(); }
        }
        var flashing = now < flashUntil && !complete && session?.IsRunning(now) == true;
        MilestoneLabel.Visibility = flashing ? Visibility.Visible : Visibility.Collapsed;
        CompletionBanner.Visibility = complete && !completionDismissed ? Visibility.Visible : Visibility.Collapsed;
        // A slow pulse, one cycle per 1.5 seconds, then a permanent pale glow at completion.
        var whitePhase = flashing && (flashUntil - now).TotalSeconds % 1.5 > .75;
        if (session != null)
        {
            SessionTitle.Text = session.Title;
            SessionRange.Text = $"{session.Start.LocalDateTime:MMM d, h:mm tt} → {session.End.LocalDateTime:MMM d, h:mm tt}";
            StatusLabel.Text = session.StoppedAt != null ? "SESSION ENDED" : now < session.Start ? "SCHEDULED" : now >= session.End ? "SESSION OVER" : "IN SESSION";
            Countdown.Text = ClockText(session.Remaining(now));
            CountdownCaption.Text = session.StoppedAt != null ? "SESSION ENDED" : now < session.Start ? "UNTIL START" : "TIME REMAINING";
            ElapsedLabel.Text = ClockText(session.Elapsed(now), false);
            DurationLabel.Text = $"{(session.End - session.Start).TotalMinutes:0.#} min";
            IntervalLabel.Text = $"{session.IntervalMinutes} min";
            EndButton.IsEnabled = session.StoppedAt == null && now < session.End;
        }
        else
        {
            SessionTitle.Text = state.Workspace == null ? "Make room for focus" : Continuity.Current(state.Workspace, store.UserId).Name;
            SessionRange.Text = "Choose a preset or create your own session.";
            StatusLabel.Text = "READY WHEN YOU ARE"; Countdown.Text = ElapsedLabel.Text = "00:00:00";
            CountdownCaption.Text = "TIME REMAINING"; DurationLabel.Text = IntervalLabel.Text = "—"; EndButton.IsEnabled = false;
        }
        PercentLabel.Text = $"{progress * 100:0.0}%";
        var head = (int)Math.Floor(progress * pixels.Count);
        for (var i = 0; i < pixels.Count; i++)
        {
            pixels[i].Background = complete ? completionBrush : whitePhase ? flashBrush : Brush(i < head ? "PixelBrush" : "PixelOffBrush");
            if (i != head && i == previousHead) { pixels[i].BeginAnimation(OpacityProperty, null); headAnimating = false; }
        }
        if (head < pixels.Count && session?.IsRunning(now) == true)
        {
            pixels[head].Background = whitePhase ? flashBrush : Brush("PixelBrush");
            if (head != previousHead || !headAnimating)
            {
                pixels[head].BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromSeconds(0.75)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
                headAnimating = true;
            }
        }
        else if (previousHead >= 0 && previousHead < pixels.Count) { pixels[previousHead].BeginAnimation(OpacityProperty, null); headAnimating = false; }
        previousHead = head;
        PixelGrid.Effect = complete ? new System.Windows.Media.Effects.DropShadowEffect { Color = completionBrush.Color, BlurRadius = 14, ShadowDepth = 0, Opacity = .75 } : null;
        if (session == null) return;
        var effectiveNow = session.StoppedAt ?? now;
        foreach (var card in intervals)
        {
            var (start, end) = ((DateTimeOffset, DateTimeOffset))card.Tag;
            var current = effectiveNow >= start && effectiveNow < end;
            card.BorderBrush = Brush(current ? "PixelBrush" : "LineBrush");
            card.Background = effectiveNow >= end ? new SolidColorBrush(Color.FromRgb(18, 48, 30)) : Brush("PanelBrush");
        }
    }

    private void BuildTimeline()
    {
        Timeline.Children.Clear(); intervals.Clear();
        var session = state.Session;
        TimelineHint.Visibility = session == null ? Visibility.Visible : Visibility.Collapsed;
        if (session == null) return;
        var count = (int)Math.Ceiling((session.End - session.Start).TotalMinutes / session.IntervalMinutes);
        var stride = Math.Max(1, (int)Math.Ceiling(count / 120.0));
        if (stride > 1) { TimelineHint.Text = $"Each card groups {stride} intervals of {session.IntervalMinutes} minutes."; TimelineHint.Visibility = Visibility.Visible; }
        for (var i = 0; i < count; i += stride)
        {
            var start = session.Start.AddMinutes(i * session.IntervalMinutes);
            var end = start.AddMinutes(stride * session.IntervalMinutes);
            if (end > session.End) end = session.End;
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = $"{start.LocalDateTime:h:mm}–{end.LocalDateTime:h:mm tt}", FontSize = 11, Foreground = Brush("MutedBrush") });
            text.Children.Add(new TextBlock { Text = stride == 1 ? $"Interval {i + 1:00}" : $"Intervals {i + 1}–{Math.Min(i + stride, count)}", FontSize = 12, Margin = new Thickness(0, 7, 0, 0) });
            var notePreview = string.Join(" ", Enumerable.Range(i, Math.Min(stride, count - i)).Select(index => (session.IntervalNotes.GetValueOrDefault(index) ?? "") + " " + (session.HedyIntervalNotes.GetValueOrDefault(index) ?? ""))).Trim();
            text.Children.Add(new TextBlock { Text = notePreview.Length == 0 ? "＋ Add notes" : notePreview, FontSize = 11, MaxHeight = 62, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 8, 0, 0), Foreground = Brush("MutedBrush") });
            var card = new Border { Child = text, Width = 132, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(10), CornerRadius = new CornerRadius(7), BorderThickness = new Thickness(1), Tag = (start, end) };
            var first = i; var groupCount = Math.Min(stride, count - i);
            var button = new Button { Content = card, Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0), ToolTip = "Open interval notes" };
            button.Click += (_, _) => OpenIntervalNotes(session, first, groupCount);
            intervals.Add(card); Timeline.Children.Add(button);
        }
    }

    private void StoryClick(object sender, RoutedEventArgs e)
    {
        storyOpen = !storyOpen;
        PixelArea.Visibility = storyOpen ? Visibility.Collapsed : Visibility.Visible;
        StoryArea.Visibility = storyOpen ? Visibility.Visible : Visibility.Collapsed;
        StoryButton.Content = storyOpen ? "← Back to focus" : "I'm bored →";
        if (storyOpen) RenderStory();
    }
    private void RenderStory()
    {
        StoryArea.Children.Clear();
        var scene = Story.Current(state);
        var panel = new StackPanel();
        var artwork = new StoryArtwork(state.StoryNode, ((SolidColorBrush)Brush("AccentBrush")).Color, ((SolidColorBrush)Brush("PixelBrush")).Color) { Height = 170, Margin = new Thickness(0, 0, 0, 18) };
        System.Windows.Automation.AutomationProperties.SetName(artwork, "Minimal illustration for " + scene.Title);
        panel.Children.Add(artwork);
        panel.Children.Add(new TextBlock { Text = scene.Chapter, Foreground = Brush("AccentBrush"), FontSize = 11, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = scene.Title, FontSize = 27, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 12) });
        panel.Children.Add(new TextBlock { Text = scene.Text, TextWrapping = TextWrapping.Wrap, FontSize = 15, LineHeight = 24, Margin = new Thickness(0, 0, 0, 16) });
        for (var i = 0; i < scene.Choices.Length; i++)
        {
            var index = i;
            var button = new Button { Content = new TextBlock { Text = $"{i + 1:00}   {scene.Choices[i].Text}", TextWrapping = TextWrapping.Wrap }, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 8) };
            System.Windows.Automation.AutomationProperties.SetName(button, scene.Choices[i].Text);
            button.Click += (_, _) => { Story.Choose(state, index); Changed(); SaveNow(); RenderStory(); };
            panel.Children.Add(button);
        }
        panel.Children.Add(new TextBlock { Text = $"{state.StoryEndings.Count(id => Story.Scenes.TryGetValue(id, out var ending) && ending.IsEnding)} / 6 endings discovered · Progress saves with every choice", Foreground = Brush("MutedBrush"), FontSize = 11, Margin = new Thickness(0, 8, 0, 8), TextWrapping = TextWrapping.Wrap });
        if (state.StoryHistory.Count > 0)
        {
            var journal = new Expander { Header = "Your choices" };
            journal.Content = new TextBlock { Text = string.Join("\n", state.StoryHistory.Select((choice, index) => $"{index + 1}. {choice}")), TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 8, 0, 8) };
            panel.Children.Add(journal);
            var restart = new Button { Content = scene.IsEnding ? "Try another path" : "Restart story", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) };
            restart.Click += (_, _) => { Story.Restart(state); Changed(); SaveNow(); RenderStory(); };
            panel.Children.Add(restart);
        }
        StoryArea.Children.Add(panel);
    }
}
