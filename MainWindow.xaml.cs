using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly StateStore store;
    internal AppState state;
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer saveDelay = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly List<Border> pixels = [];
    private readonly List<Border> intervals = [];
    private bool ready;
    private bool dirty;
    private bool closingAfterTransition;
    private bool storyOpen;
    private readonly DispatcherTimer boredomTicker = new() { Interval = TimeSpan.FromMilliseconds(16) };
    internal PixelSwarmGame? swarmGame;
    private SwarmArena? swarmArena;
    private TextBlock? swarmScoreText, swarmPauseText;
    private Border? swarmResult;
    private readonly System.Diagnostics.Stopwatch swarmFrameClock = new();
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

    public MainWindow() : this(new StateStore(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionDashboard", "state.json"))) { }

    internal MainWindow(StateStore store, StateStore? previewHomeStore = null)
    {
        this.store = store;
        this.previewHomeStore = previewHomeStore;
        state = store.Load(); store.LoadInteractionPreferences(state);
        InitializeComponent();
        PreviewBanner.Visibility = IsPreview ? Visibility.Visible : Visibility.Collapsed;
        if (IsPreview) Title = "Nexa · Preview";
        NexaChrome.Apply(this);
        for (var i = 0; i < 540; i++)
        {
            var pixel = new Border { Margin = new Thickness(2), CornerRadius = new CornerRadius(2), Background = Brush("PixelOffBrush") };
            pixels.Add(pixel);
            PixelGrid.Children.Add(pixel);
        }
        PixelArea.SizeChanged += (_, _) => SizeTimerPixels();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(new Action(SizeTimerPixels));
        NotesBox.Text = state.Notes;
        NotesToggle.IsChecked = state.ShowNotes;
        ChecklistToggle.IsChecked = state.ShowChecklist;
        PinToggle.IsChecked = state.AlwaysOnTop;
        InitializeColorPickers();
        AccentInput.Text = state.AccentColor;
        PixelInput.Text = state.PixelColor;
        StartDate.SelectedDate = EndDate.SelectedDate = DateTime.Today;
        StartTime.Text = DateTime.Now.ToString("HH:mm");
        EndTime.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
        EndDate.SelectedDate = DateTime.Now.AddHours(1).Date;
        RefreshPresets(state.Presets.FirstOrDefault()?.Id);
        RenderTasks();
        ApplyColors();
        ApplySpaceBackground();
        ApplySettings();
        BuildTimeline();
        InitializeHedy();
        InitializeLocalRecording();
        ready = true;
        UpdateClock();
        Feedback.Text = store.Warning ?? "Saved on this PC • Your workspace is ready";
        clock.Tick += (_, _) => UpdateClock();
        saveDelay.Tick += async (_, _) => { saveDelay.Stop(); await SaveInBackground(); };
        boredomTicker.Tick += (_, _) => UpdateBoredomGame();
        InitializeContinuity();
        UpdateDeveloperToolsVisibility();
        InitializeWidgets();
        Countdown.Cursor = System.Windows.Input.Cursors.Hand; Countdown.ToolTip = "Click to edit session duration";
        Countdown.MouseLeftButtonUp += SessionTimeEditClick;
        foreach (var button in TopNavigation.Children.OfType<Button>().Where(b => b.Content is string text && text == "Home")) button.Content = NexaIcons.Label("Home");
        SessionEditButton.Content = NexaIcons.Label("Edit");
        Closed += (_, _) => { interfacePlayer?.Stop(); interfacePlayer?.Dispose(); interfaceAudio?.Dispose(); };
        InitializeInlineSession();
        clock.Start();
        Closing += (_, e) =>
        {
            if (closingAfterTransition) { closingAfterTransition = false; return; }
            if (!SaveForClose()) e.Cancel = true;
        };
        Closed += (_, _) => { clock.Stop(); saveDelay.Stop(); boredomTicker.Stop(); autosave.Stop(); closing = true; StopLocalRecording(true); StopHedy(); };
    }

    private void SizeTimerPixels()
    {
        if(PixelArea.ActualWidth<=0 || PixelArea.ActualHeight<=0) return;
        var cell=Math.Min(PixelArea.ActualWidth/PixelGrid.Columns,PixelArea.ActualHeight/PixelGrid.Rows);
        // Round down once in device pixels so WPF cannot round every child upward and overflow the banner.
        var scale=VisualTreeHelper.GetDpi(PixelArea).DpiScaleX;
        cell=Math.Max(1/scale,Math.Floor(cell*scale)/scale);
        PixelGrid.Width=cell*PixelGrid.Columns; PixelGrid.Height=cell*PixelGrid.Rows;
        var gap=Math.Floor(cell*.14*scale)/scale;
        foreach(var pixel in pixels) { pixel.Width=pixel.Height=Math.Max(.1,cell-2*gap); pixel.Margin=new Thickness(gap); pixel.CornerRadius=new CornerRadius(Math.Min(2,cell*.2)); }
    }
    private Brush Brush(string key) => (Brush)FindResource(key);

    private void UpdateDeveloperToolsVisibility()
    {
        var isDeveloperDashboard = state.Workspace != null &&
            Continuity.Current(state.Workspace, store.UserId).PresetOrigin == "Developer mode";
        DeveloperSplashButton.Visibility = isDeveloperDashboard ? Visibility.Visible : Visibility.Collapsed;
        DeveloperResetButton.Visibility = isDeveloperDashboard ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DeveloperSplashClick(object sender, RoutedEventArgs e)
    {
        var preview = new SplashWindow { Owner = this };
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SplashWindow.DisplayMilliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); preview.Close(); };
        preview.ContentRendered += (_, _) => timer.Start();
        preview.Closed += (_, _) => timer.Stop();
        preview.ShowDialog();
    }

    private void DeveloperResetClick(object sender, RoutedEventArgs e)
    {
        if (state.Workspace == null || Continuity.Current(state.Workspace, store.UserId).PresetOrigin != "Developer mode") return;
        var dialog = Dialog("Nexa · Reset DEV", 500, 280); var body = new StackPanel { Margin = new Thickness(24) };
        body.Children.Add(new TextBlock { Text = "Reset DEV Space?", FontSize = 22, FontWeight = FontWeights.SemiBold });
        body.Children.Add(new TextBlock { Text = "Restart the developer timer and clear current widget data? The current session will remain in Space history.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 22) });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; var cancel = new Button { Content = "Cancel", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) }; cancel.Click += (_, _) => dialog.CloseWithFade(); actions.Children.Add(cancel); var reset = new Button { Content = "Reset DEV", MinWidth = 100, Style = (Style)FindResource("PrimaryButton") }; reset.Click += (_, _) => { dialog.CloseWithFade(); ResetDeveloperConfirmed(); }; actions.Children.Add(reset); body.Children.Add(actions); dialog.Content = body; dialog.ShowDismissible();
    }

    private void ResetDeveloperConfirmed()
    {
        if (state.Workspace == null || Continuity.Current(state.Workspace, store.UserId).PresetOrigin != "Developer mode") return;
        if (!SaveNow()) return;

        var candidate = CopyWorkspaceState().Workspace!;
        var dashboard = Continuity.Current(candidate, store.UserId);
        Continuity.Reset(dashboard);
        var data = dashboard.Current.Data;
        var start = DateTimeOffset.Now.AddMinutes(-5);
        var end = start.AddMinutes(10);
        var localStart = start.LocalDateTime;
        var localEnd = end.LocalDateTime;
        data.Session = new Session { Title = "Developer mode", Start = start, End = end, IntervalMinutes = 5 };
        Milestones.Collect(data.Session, start.AddMinutes(5));
        data.Draft = new DashboardDraft
        {
            Initialized = true,
            Title = "Developer mode",
            Duration = "10",
            Interval = "5",
            StartDate = localStart.Date,
            EndDate = localEnd.Date,
            StartMinutes = localStart.Hour * 60 + localStart.Minute,
            EndMinutes = localEnd.Hour * 60 + localEnd.Minute
        };
        CommitWorkspace(candidate);
    }

    private void GentleMouseWheel(object sender, MouseWheelEventArgs e)
    {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source != null && source is not ScrollViewer)
        {
            // FlowDocument content is not a Visual. Follow its content parents
            // to the editor, whose own scroller must keep the wheel event.
            if (source is System.Windows.Controls.Primitives.TextBoxBase editor)
            {
                var canScroll = e.Delta < 0 ? editor.VerticalOffset + editor.ViewportHeight < editor.ExtentHeight - .1 : editor.VerticalOffset > .1;
                if (canScroll)
                {
                    editor.ScrollToVerticalOffset(editor.VerticalOffset - e.Delta / 120.0 * 48);
                    e.Handled = true;
                    return;
                }
            }
            source = source switch
            {
                ContentElement content => ContentOperations.GetParent(content)
                    ?? (content as FrameworkContentElement)?.Parent,
                Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(source),
                _ => LogicalTreeHelper.GetParent(source)
            };
        }
        if (source is not ScrollViewer scroller) return;

        var canScrollViewer = e.Delta < 0 ? scroller.VerticalOffset < scroller.ScrollableHeight - .1 : scroller.VerticalOffset > .1;
        var target = canScrollViewer ? scroller : WidgetScroller;
        target.ScrollToVerticalOffset(target.VerticalOffset - e.Delta / 120.0 * 48);
        e.Handled = true;
    }

    private void InitializeColorPickers()
    {
        AccentColorPicker.SelectedColor = state.AccentColor;
        PixelColorPicker.SelectedColor = state.PixelColor;
    }

    private static void SetColorPickerSelection(ColorPickerButton picker, string color) => picker.SelectedColor = color;

    private static string GetColorFromPicker(ColorPickerButton picker, TextBox fallback, string fallbackHex)
    {
        var candidate = fallback.Text.Trim();
        if (InputRules.IsColor(candidate)) return candidate;
        if (InputRules.IsColor(picker.SelectedColor)) return picker.SelectedColor;
        return fallbackHex;
    }

    private void AccentColorPickerColorChanged(object? sender, EventArgs e)
    {
        AccentInput.Text = AccentColorPicker.SelectedColor;
        ColorsClick(AccentColorPicker, new RoutedEventArgs()); SaveNow();
    }

    private void PixelColorPickerColorChanged(object? sender, EventArgs e)
    {
        PixelInput.Text = PixelColorPicker.SelectedColor;
        ColorsClick(PixelColorPicker, new RoutedEventArgs()); SaveNow();
    }

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
            CaptureDraft(); ShowSaveStatus("◌  Autosaving…");
            store.Save(state); dirty = false;
            ShowSaveStatus("✓  Autosaved", fade: true); return true;
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
        preset.ScheduledDays = SelectedSessionDays();
        return true;
    }

    private void StartClick(object sender, RoutedEventArgs e)
    {
        if (!ReadPreset(out var preset)) return;
        var now = DateTimeOffset.Now;
        BeginSession(new Session { Title = preset.Name, Start = now, End = now.AddMinutes(preset.Minutes), IntervalMinutes = preset.IntervalMinutes, ConfiguredDurationMinutes = preset.Minutes, ScheduledDays = SelectedSessionDays() });
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
        var session = new Session { Title = TitleInput.Text.Trim(), Start = new DateTimeOffset(start), End = new DateTimeOffset(end), IntervalMinutes = interval, ConfiguredDurationMinutes = (end - start).TotalMinutes, ScheduledDays = SelectedSessionDays() };
        if (!InputRules.ValidSession(session) || session.End <= DateTimeOffset.Now)
        { Error("End must be in the future, after start, and within 7 days of start. For overnight sessions, use tomorrow's end date."); return; }
        BeginSession(session);
    }

    private void BeginSession(Session session)
    {
        if (session.ConfiguredDurationMinutes <= 0) session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
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
        if (p.ScheduledDays.Count > 0) foreach (var pair in sessionDayChecks) pair.Value.IsChecked = p.ScheduledDays.Contains(pair.Key);
        if (ready)
        {
            var theme = Theme.ForName(p.Name);
            state.AccentColor = InputRules.IsColor(p.AccentColor) ? p.AccentColor : theme.Accent;
            state.PixelColor = InputRules.IsColor(p.PixelColor) ? p.PixelColor : theme.Pixel;
            AccentInput.Text = state.AccentColor; PixelInput.Text = state.PixelColor;
            SetColorPickerSelection(AccentColorPicker, state.AccentColor);
            SetColorPickerSelection(PixelColorPicker, state.PixelColor);
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
        selected.ScheduledDays = preset.ScheduledDays;
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

    private void NotesChanged(object sender, TextChangedEventArgs e)
    {
        if (!ready) return;
        state.Notes = NotesBox.Text; FitTextBoxHeight(NotesBox, 100); Changed();
        if (widgetsReady) Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames));
    }
    private void NotesSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged) FitTextBoxHeight(NotesBox, 100);
    }
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
            check.Click += (_, _) => { item.Done = check.IsChecked == true; if (item.Done) PlayInterfaceSound(); Changed(); };
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
        var accent = GetColorFromPicker(AccentColorPicker, AccentInput, state.AccentColor);
        var pixel = GetColorFromPicker(PixelColorPicker, PixelInput, state.PixelColor);
        if (!InputRules.IsColor(accent) || !InputRules.IsColor(pixel)) { Error("Use six-digit hex colors, for example #5B7CFF or #8B5CF6."); return; }
        state.AccentColor = AccentInput.Text = accent; state.PixelColor = PixelInput.Text = pixel;
        SetColorPickerSelection(AccentColorPicker, state.AccentColor);
        SetColorPickerSelection(PixelColorPicker, state.PixelColor);
        ApplyColors(); UpdateClock(); Changed();
    }
    private void ApplyColors()
    {
        if (previousHead >= 0 && previousHead < pixels.Count) pixels[previousHead].BeginAnimation(OpacityProperty, null);
        Theme.Apply(Application.Current.Resources, store.LoadThemeMode(), store.LoadAppTheme());
        Theme.ApplyDashboard(FullContent.Resources, state.AccentColor, state.PixelColor);
        var pixelColor = ((SolidColorBrush)DashboardBrush("PixelBrush")).Color;
        PercentLabel.Foreground = DashboardBrush("PixelBrush");
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
        SetColorPickerSelection(AccentColorPicker, state.AccentColor);
        SetColorPickerSelection(PixelColorPicker, state.PixelColor);
        ApplyColors(); UpdateClock(); Changed();
    }
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    internal void CloseWithFade()
    {
        if (!SaveForClose()) return;
        closingAfterTransition = true;
        NexaChrome.FadeOutAndClose(this);
    }

    private void ExitClick(object sender, RoutedEventArgs e) => CloseWithFade();
    private void FullscreenClick(object sender, RoutedEventArgs e) => ToggleFullscreen();
    internal void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            if (state.Compact) { SetCompact(false); Changed(); }
            restoreBounds = new Rect(Left, Top, Width, Height); restoreWindowState = WindowState;
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized; fullscreen = true; FullscreenButton.Content = "\uE923"; FullscreenButton.ToolTip = "Restore window (Esc)";
        }
        else
        {
            WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
            Left = restoreBounds.Left; Top = restoreBounds.Top; Width = restoreBounds.Width; Height = restoreBounds.Height;
            WindowState = restoreWindowState; fullscreen = false; FullscreenButton.Content = "\uE922"; FullscreenButton.ToolTip = "Fullscreen (F11)";
        }
        DashboardShell.Opacity = 0.72; DashboardShell.BeginAnimation(OpacityProperty, new DoubleAnimation(.72, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        var scale = new ScaleTransform(.985, .985); DashboardShell.RenderTransformOrigin = new Point(.5, .5); DashboardShell.RenderTransform = scale;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.985, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.985, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
    }
    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && storyOpen) { ExitBoredomMode(); e.Handled = true; return; }
        if (e.Key == Key.F11 || (e.Key == Key.Escape && fullscreen)) { ToggleFullscreen(); e.Handled = true; }
    }
    private void DismissCompletionClick(object sender, RoutedEventArgs e) { completionDismissed = true; CompletionBanner.Visibility = Visibility.Collapsed; }
    private void CompactClick(object sender, RoutedEventArgs e) { SetCompact(!state.Compact); Changed(); }
    private void SetCompact(bool compact)
    {
        if (storyOpen) ExitBoredomMode();
        if (fullscreen) ToggleFullscreen();
        if (compact && FullContent.Visibility == Visibility.Visible && ready) { state.WindowWidth = fullWidth = Width; state.WindowHeight = fullHeight = Height; }
        state.Compact = compact;
        Feedback.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        FullContent.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        ExpandButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        WindowState = WindowState.Normal;
        MinWidth = compact ? 560 : Math.Min(960, SystemParameters.WorkArea.Width); MinHeight = compact ? 290 : Math.Min(660, SystemParameters.WorkArea.Height);
        Width = compact ? 630 : Math.Min(fullWidth, SystemParameters.WorkArea.Width); Height = compact ? 310 : Math.Min(fullHeight, SystemParameters.WorkArea.Height);
        SessionTitle.FontSize = compact ? 18 : 24;
        TitleInput.FontSize = 16;
        Detach(CountdownHost); CompactTimerHost.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        if (compact) CompactTimerHost.Children.Add(CountdownHost); else ((StackPanel)PixelPanel.Child).Children.Insert(0, CountdownHost);
        FooterStatus.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        SessionRange.Visibility = Visibility.Collapsed;
        Countdown.FontSize = compact ? 32 : 52;
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
        UpdateFocusExperience(now); var complete = session != null && session.StoppedAt == null && now >= session.End;
        if (session != null)
        {
            var milestones = Milestones.Collect(session, now);
            if (milestones.Count > 0) { flashUntil = now.AddSeconds(6); MilestoneLabel.Text = string.Join(" · ", milestones); Changed(); }
        }
        var flashing = now < flashUntil && !complete && session?.IsRunning(now) == true;
        MilestoneLabel.Visibility = flashing ? Visibility.Visible : Visibility.Collapsed;
        CompletionBanner.Visibility = complete && !completionDismissed && !storyOpen ? Visibility.Visible : Visibility.Collapsed;
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
            IntervalLabel.Text = session.IsRunning(now)
                ? $"{session.IntervalMinutes} min / next {ClockText(TimeSpan.FromSeconds(Math.Min(session.Remaining(now).TotalSeconds, session.IntervalMinutes * 60 - session.Elapsed(now).TotalSeconds % (session.IntervalMinutes * 60))))[3..]}"
                : $"{session.IntervalMinutes} min";
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
            pixels[i].Background = complete ? completionBrush : whitePhase ? flashBrush : DashboardBrush(i < head ? "PixelBrush" : "PixelOffBrush");
            if (i != head && i == previousHead) { pixels[i].BeginAnimation(OpacityProperty, null); headAnimating = false; }
        }
        if (head < pixels.Count && session?.IsRunning(now) == true)
        {
            pixels[head].Background = whitePhase ? flashBrush : DashboardBrush("PixelBrush");
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
            card.Background = effectiveNow >= end ? Brush("SurfaceBrush") : Brush("PanelBrush");
        }
    }

    private void BuildTimeline()
    {
        Timeline.Children.Clear(); intervals.Clear();
        var session = state.Session;
        TimelineHint.Visibility = session == null ? Visibility.Visible : Visibility.Collapsed;
        if (session == null) return;
        SessionSummaries.RefreshCompleted(session, DateTimeOffset.Now);
        var count = (int)Math.Ceiling((session.End - session.Start).TotalMinutes / session.IntervalMinutes);
        var stride = Math.Max(1, (int)Math.Ceiling(count / 120.0));
        if (stride > 1) { TimelineHint.Text = $"Each card groups {stride} intervals of {session.IntervalMinutes} minutes."; TimelineHint.Visibility = Visibility.Visible; }
        for (var i = 0; i < count; i += stride)
        {
            var start = session.Start.AddMinutes(i * session.IntervalMinutes);
            var end = start.AddMinutes(stride * session.IntervalMinutes);
            if (end > session.End) end = session.End;
            var text = new StackPanel();
            text.Children.Add(new TextBlock { Text = $"{start.LocalDateTime:h:mm}–{end.LocalDateTime:h:mm tt}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brush("MutedBrush") });
            var notePreview = string.Join(" ", Enumerable.Range(i, Math.Min(stride, count - i)).Select(index => session.IntervalSummaries.GetValueOrDefault(index) ?? "")).Trim();
            var inProgress = DateTimeOffset.Now >= start && DateTimeOffset.Now < end && session.StoppedAt == null;
            text.Children.Add(new TextBlock { Text = notePreview.Length == 0 ? (inProgress ? "●  Summarizing…" : "No notes captured") : notePreview, FontSize = 12, MaxHeight = 74, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 6, 0, 0), Foreground = inProgress ? Brush("AccentBrush") : Brush("TextBrush") });
            var card = new Border { Child = text, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Tag = (start, end) };
            var first = i; var groupCount = Math.Min(stride, count - i);
            var button = new Button { Content = card, Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0), ToolTip = "Open interval notes" };
            button.Click += (_, _) => OpenIntervalNotes(session, first, groupCount);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch; intervals.Add(card); Timeline.Children.Add(button);
        }
    }

}
