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
    private readonly bool entryAlreadyPersisted;
    internal AppState state;
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly DispatcherTimer saveDelay = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private readonly List<Border> pixels = [];
    private readonly Stack<Border> sparePixels = [];
    private readonly DispatcherTimer pixelResizeDelay = new() { Interval = TimeSpan.FromMilliseconds(85) };
    private int pixelColumns, pixelRows, litPixels = -1;
    private double pixelSize, pixelGap;
    private string pixelShape = "";
    internal int PixelLayoutChanges { get; private set; }
    internal int PixelVisualsCreated { get; private set; }
    private readonly List<Border> intervals = [];
    internal Dictionary<string,double> ConstructionProfile {get;}=[];
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

    private double fullWidth = 1220, fullHeight = 880;
    private bool fullscreen;
    private bool restoreFullscreenTopmost;
    internal bool IsFullscreen => fullscreen;
    private Rect restoreBounds;
    private WindowState restoreWindowState;
    private DateTimeOffset flashUntil;
    private bool completionDismissed;

    public MainWindow() : this(new StateStore(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionDashboard", "state.json"))) { }

    internal MainWindow(StateStore store, StateStore? previewHomeStore = null,AppState? loadedState=null,bool entryAlreadyPersisted=false)
    {
        this.store = store;this.entryAlreadyPersisted=entryAlreadyPersisted;
        this.previewHomeStore = previewHomeStore;
        void Measure(string name,Action action){var watch=System.Diagnostics.Stopwatch.StartNew();action();ConstructionProfile[name]=watch.Elapsed.TotalMilliseconds;}
        state = loadedState ?? store.Load(); store.LoadInteractionPreferences(state);
        Measure("InitializeComponent",InitializeComponent);
        Title = App.VersionLabel;
        DashboardHeader.SizeChanged += (_, _) => UpdateDashboardHeaderGeometry();
        TopNavigation.SizeChanged += (_, _) => UpdateDashboardHeaderGeometry();
        PreviewBanner.Visibility = IsPreview ? Visibility.Visible : Visibility.Collapsed;
        if (IsPreview) Title = App.VersionLabel + " · Preview";
        NexaChrome.Apply(this);
        StateChanged += (_, _) =>
        {
            UpdateDashboardHeaderGeometry();
            MaximizeButton.Content = WindowState == WindowState.Maximized && !fullscreen ? "\uE923" : "\uE922";
            MaximizeButton.ToolTip = WindowState == WindowState.Maximized && !fullscreen ? "Restore" : "Maximize";
        };
        PixelArea.SizeChanged += (_, _) => QueuePixelLayout();
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(new Action(QueuePixelLayout));
        pixelResizeDelay.Tick += (_, _) => { pixelResizeDelay.Stop(); SizeTimerPixels(); };
        Loaded += (_, _) => SizeTimerPixels();
        NotesBox.Text = state.Notes;
        NotesToggle.IsChecked = state.ShowNotes;
        ChecklistToggle.IsChecked = state.ShowChecklist;
        PinToggle.IsChecked = state.AlwaysOnTop;
        HeaderBrandIcon.Source=NexaChrome.BrandIcon;
        InitializeColorPickers();
        AccentInput.Text = state.AccentColor;
        PixelInput.Text = state.PixelColor;
        StartDate.SelectedDate = EndDate.SelectedDate = DateTime.Today;
        StartTime.Text = DateTime.Now.ToString("HH:mm");
        EndTime.Text = DateTime.Now.AddHours(1).ToString("HH:mm");
        EndDate.SelectedDate = DateTime.Now.AddHours(1).Date;
        RefreshPresets(state.Presets.FirstOrDefault()?.Id);
        RenderTasks();
        Measure("ApplyColors",ApplyColors);
        ApplySpaceBackground();
        Measure("ApplySettings",ApplySettings);
        Measure("BuildTimeline",BuildTimeline);
        Measure("InitializeHedy",InitializeHedy);
        Measure("InitializeLocalRecording",InitializeLocalRecording);
        ready = true;
        UpdateClock();
        Feedback.Text = store.Warning ?? "Saved on this PC • Your workspace is ready";
        clock.Interval=PixelTiming.UntilNextSecond(DateTimeOffset.UtcNow);
        clock.Tick += (_, _) => { UpdateClock(); clock.Interval=PixelTiming.UntilNextSecond(DateTimeOffset.UtcNow); };
        saveDelay.Tick += async (_, _) => { saveDelay.Stop(); await SaveInBackground(); };
        boredomTicker.Tick += (_, _) => UpdateBoredomGame();
        Measure("InitializeContinuity",InitializeContinuity);
        UpdateDeveloperToolsVisibility();
        Measure("InitializeWidgets",InitializeWidgets);
        InitializeInitialViewport();
        Measure("InitializeBugs",InitializeBugs);
        InitializeDashboardTips();
        Countdown.Cursor = System.Windows.Input.Cursors.Hand; Countdown.ToolTip = "Click to edit session duration";
        Countdown.MouseLeftButtonUp += SessionTimeEditClick;
        foreach (var button in TopNavigation.Children.OfType<Button>().Where(b => b.Content is string text && text == "Home")) button.Content = NexaIcons.Label("Home");
        SessionEditButton.Content = NexaIcons.Label("Edit");
        Closed+=(_,_)=>{widgetToastTimer.Stop();completionFill.Stop();RemoveCompletionVisuals();recapWindow?.Close();};
        InitializeInlineSession();
        PreviewKeyDown+=(_,e)=>{if(TutorialActive && e.Key==Key.Escape){FinishQuickStart(true);e.Handled=true;}};
        Loaded+=(_,_)=>{if(state.OfferQuickStart && !Options.QuickStartCompleted)StartQuickStart(false);};
        Closed+=(_,_)=>RemoveQuickStart();
        SpaceIconHost.Cursor=Cursors.Hand;SpaceIconHost.ToolTip="Choose Space icon";SpaceIconHost.MouseLeftButtonUp+=(_,_)=>EditSpaceIcon();
        Loaded+=(_,_)=>{if(!entryAlreadyPersisted)AnimateWidgetEntrance();};
        clock.Start();
        Closing += (_, e) =>
        {
            if (closingAfterTransition) { closingAfterTransition = false; return; }
            if (!SaveForClose()) e.Cancel = true;
        };
        Closed += (_, _) => { if (storyOpen) SaveSwarmRun(); clock.Stop(); saveDelay.Stop(); pixelResizeDelay.Stop(); boredomTicker.Stop(); autosave.Stop(); closing = true; StopLocalRecording(true); StopHedy(); };
    }

    private void QueuePixelLayout()
    {
        if (PixelArea.ActualWidth <= 0 || PixelArea.ActualHeight <= 0) return;
        pixelResizeDelay.Stop(); pixelResizeDelay.Start();
    }

    private void SizeTimerPixels()
    {
        if (PixelArea.ActualWidth <= 0 || PixelArea.ActualHeight <= 0) return;
        var scale = VisualTreeHelper.GetDpi(PixelArea).DpiScaleX;
        var (columns, rows, size, gap) = PixelMetrics(PixelArea.ActualWidth, PixelArea.ActualHeight, scale);
        if (columns == pixelColumns && rows == pixelRows && Math.Abs(size - pixelSize) < .001 && Math.Abs(gap - pixelGap) < .001 && pixelShape == state.PixelShape) return;
        PixelLayoutChanges++;
        pixelColumns = columns; pixelRows = rows; pixelSize = size; pixelGap = gap; pixelShape = state.PixelShape;
        var count = columns * rows;
        if (previousHead >= 0 && previousHead < pixels.Count) pixels[previousHead].BeginAnimation(OpacityProperty, null);
        previousHead = -1;  litPixels = -1;
        while (pixels.Count > count)
        {
            var pixel = pixels[^1]; pixels.RemoveAt(pixels.Count - 1); PixelGrid.Children.Remove(pixel);
            if (sparePixels.Count < 5000) sparePixels.Push(pixel);
        }
        while (pixels.Count < count)
        {
            var pixel = sparePixels.Count > 0 ? sparePixels.Pop() : new Border { IsHitTestVisible = false };
            if (pixel.Background == null) PixelVisualsCreated++;
            pixel.Background = DashboardBrush("PixelOffBrush"); pixels.Add(pixel); PixelGrid.Children.Add(pixel);
        }
        PixelGrid.Width = columns * size + (columns - 1) * gap;
        PixelGrid.Height = rows * size + (rows - 1) * gap;
        for (var i = 0; i < count; i++)
        {
            var pixel = pixels[i]; pixel.Width = pixel.Height = size;
            pixel.CornerRadius = new CornerRadius(state.PixelShape == "Circle" ? size / 2 : 0);
            Canvas.SetLeft(pixel, i % columns * (size + gap)); Canvas.SetTop(pixel, i / columns * (size + gap));
        }
        if (ready) UpdateClock();
    }

    internal static (int Columns, int Rows, double Size, double Gap) PixelMetrics(double width, double height, double scale)
    {
        // Whole device pixels avoid a wider gap every few cells at fractional DPI scales.
        var size = Math.Max(1, Math.Round(12 * scale)) / scale;
        var gap = Math.Max(1, Math.Round(2 * scale)) / scale;
        var columns = Math.Max(1, (int)Math.Floor((width + gap + .001) / (size + gap)));
        var rows = Math.Max(1, (int)Math.Floor((height + gap + .001) / (size + gap)));
        return (columns, rows, size, gap);
    }
    private Brush Brush(string key) => (Brush)FindResource(key);

    private void UpdateDeveloperToolsVisibility()
    {
        var isDeveloperDashboard = state.Workspace != null &&
            Continuity.Current(state.Workspace, store.UserId).PresetOrigin == "Developer mode";
        DeveloperSplashButton.Visibility = isDeveloperDashboard ? Visibility.Visible : Visibility.Collapsed;
        DeveloperSoundButton.Visibility = isDeveloperDashboard ? Visibility.Visible : Visibility.Collapsed;
        DeveloperResetButton.Visibility = isDeveloperDashboard ? Visibility.Visible : Visibility.Collapsed;
    }

    private void DeveloperSoundClick(object sender, RoutedEventArgs e) => PlayInterfaceSound();

    private async void DeveloperSplashClick(object sender,RoutedEventArgs e)
    {
        if(DeveloperSplashButton.IsEnabled==false)return;
        DeveloperSplashButton.IsEnabled=false;
        try{await new SplashWindow(Options).PreviewIn((Grid)Content);}
        catch(Exception ex){await Dispatcher.InvokeAsync(()=>Error("Startup preview failed: "+ex.Message));}
        finally{await Dispatcher.InvokeAsync(()=>DeveloperSplashButton.IsEnabled=true);}
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
        if (settingsWindow?.FlushPendingChanges() == false) return false;
        saveDelay.Stop(); saveTicket++;
        try
        {
            // Await only worker I/O here; its completion does not depend on the dispatcher.
            try { pendingWrite?.GetAwaiter().GetResult(); } catch (Exception ex) when (SaveException(ex)) { }
            CaptureDraft(); ShowSaveStatus("Auto-saving...");
            store.Save(state); dirty = false;
            ShowSaveStatus("saved"); return true;
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
        preset.AutomaticScheduling = state.Session?.AutomaticScheduling == true || (PresetPicker.SelectedItem as Preset)?.AutomaticScheduling == true;
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
        if (NexaTime.Zone(Options.TimeZoneId).IsInvalidTime(start) || NexaTime.Zone(Options.TimeZoneId).IsInvalidTime(end))
        { Error("That time falls in a daylight-saving clock change. Choose a different time."); return; }
        var session = new Session { Title = TitleInput.Text.Trim(), Start = NexaTime.Instant(start, Options.TimeZoneId), End = NexaTime.Instant(end, Options.TimeZoneId), IntervalMinutes = interval, ConfiguredDurationMinutes = (end - start).TotalMinutes, ScheduledDays = SelectedSessionDays() };
        if (!InputRules.ValidSession(session) || session.End <= DateTimeOffset.Now)
        { Error("End must be in the future, after start, and within 7 days of start. For overnight sessions, use tomorrow's end date."); return; }
        BeginSession(session);
    }

    private void BeginSession(Session session)
    {
        session.IntervalMinutes=state.Widgets.FirstOrDefault(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline")?.SummaryIntervalMinutes??session.IntervalMinutes;
        if (session.Start > DateTimeOffset.UtcNow && session.BeforeSessionCountdownFrom == null) session.BeforeSessionCountdownFrom = DateTimeOffset.UtcNow;
        session.TimeZoneId = Options.TimeZoneId == "Automatic" ? TimeZoneInfo.Local.Id : Options.TimeZoneId;
        session.AutomaticScheduling = (PresetPicker.SelectedItem as Preset)?.AutomaticScheduling == true;
        if (session.ConfiguredDurationMinutes <= 0) session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
        if (!SaveNow()) return;
        CloseNoteEditors();
        var candidate = CopyWorkspaceState();
        var dashboard = Continuity.Current(candidate.Workspace!, store.UserId);
        Continuity.Archive(dashboard);
        dashboard.Current = new DashboardSession { UserId = store.UserId, DashboardId = dashboard.DashboardId, Data = WeeklySchedule.ResetSessionData(Continuity.Capture(candidate)), Title = session.Title };
        dashboard.Current.Data.Session = session;
        dashboard.Current.Data.Productivity = new();
        CommitWorkspace(candidate.Workspace!);
    }

    private void EndClick(object sender, RoutedEventArgs e)
    {
        FinalizeSession();
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
            StartDate.SelectedDate = NexaTime.Local(DateTimeOffset.UtcNow,Options.TimeZoneId).Date; EndDate.SelectedDate = StartDate.SelectedDate.Value.AddDays(p.EndDayOffset);
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
            if(PresetPicker.IsKeyboardFocusWithin && !applyingTimingPreset)ApplyTimingPreset(p);
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
        selected.ScheduledDays = preset.ScheduledDays; selected.AutomaticScheduling = preset.AutomaticScheduling;
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
            AttachTaskPermanence(check, item); check.Click += (_, _) => { item.Done = check.IsChecked == true; if (item.Done) PlayInterfaceSound(); Changed(); };
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
        if (fullscreen) restoreFullscreenTopmost = state.AlwaysOnTop;
        Topmost = fullscreen || state.AlwaysOnTop;
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
        Theme.ApplyDashboard(FullContent.Resources, state.AccentColor, state.PixelColor);
        PercentLabel.Foreground = DashboardBrush("PixelBrush");
        previousHead = -1;

        litPixels = -1;
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
    private void UpdateDashboardHeaderGeometry()
    {
        var needsTopInset = !fullscreen && WindowState == WindowState.Maximized;
        DashboardHeader.Height = fullscreen?44:needsTopInset?60:52;
        DashboardHeader.Padding = new Thickness(fullscreen?8:12,needsTopInset?16:4,fullscreen?8:12,4);
        WorkspaceOuter.Margin=fullscreen?new Thickness(8,4,8,8):new Thickness(24);
        if(widgetToast!=null)widgetToast.Margin=fullscreen?new Thickness(8):new Thickness(24,0,0,36);
        foreach(var button in new[]{MinimizeButton,MaximizeButton,ExitButton})button.Visibility=fullscreen?Visibility.Collapsed:Visibility.Visible;
        SessionNameHost.Visibility=SpaceIconHost.Visibility=fullscreen && DashboardHeader.ActualWidth<1300?Visibility.Collapsed:Visibility.Visible;
        SessionNameHost.MaxWidth = Math.Clamp((DashboardHeader.ActualWidth - TopNavigation.ActualWidth) / 2 - 52, 60, 290);
    }
    private System.Windows.Shell.WindowChrome? savedFullscreenChrome;
    internal void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            if (state.Compact) { SetCompact(false); Changed(); }
            restoreWindowState = WindowState;
            restoreBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
            restoreFullscreenTopmost = Topmost;
            savedFullscreenChrome=System.Windows.Shell.WindowChrome.GetWindowChrome(this);
            fullscreen = true;WindowState = WindowState.Normal;
            System.Windows.Shell.WindowChrome.SetWindowChrome(this,null);
            ResizeMode = ResizeMode.NoResize;Topmost=true;WindowState = WindowState.Maximized;
        }
        else
        {
            fullscreen=false;WindowState = WindowState.Normal;
            System.Windows.Shell.WindowChrome.SetWindowChrome(this,savedFullscreenChrome);
            ResizeMode = ResizeMode.CanResize;
            Left=restoreBounds.Left;Top=restoreBounds.Top;Width=restoreBounds.Width;Height=restoreBounds.Height;
            Topmost = restoreFullscreenTopmost;WindowState = restoreWindowState;
        }
        UpdateDashboardHeaderGeometry();ReconcileFullscreenStatus();
        if(fullscreen && Options.FullscreenNotification)ShowFullscreenNotice();
        else DismissFullscreenNotice();
    }
    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (storyOpen && e.Key == Key.Escape)
        {
            swarmGame?.TogglePause();
            swarmFrameClock.Restart();
            ShowSwarmOverlay();
            e.Handled = true; return;
        }
        if (NexaPanels.HandleEscape(this, e)) return;
        if(HandleWidgetUndoKey(e)) return;
        if (e.Key == Key.Escape && focusedWidget != null) { ClearWidgetFocus(); e.Handled = true; return; }
        if(e.Key==Key.Escape && Keyboard.FocusedElement is DependencyObject focused)
        {
            for(var node=focused;node!=null;node=node is Visual?VisualTreeHelper.GetParent(node):LogicalTreeHelper.GetParent(node))
                if(node is ComboBox{IsDropDownOpen:true})return;
        }
        if (!e.Handled && (e.Key == Key.F11 && Options.EnableF11 || e.Key == Key.Escape && fullscreen && Options.EscExitsFullscreen && System.Windows.Input.Keyboard.FocusedElement is not TextBox)) { ToggleFullscreen(); e.Handled = true; }
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
        if (compact) CompactTimerHost.Children.Add(CountdownHost); else PixelInfo.Children.Insert(0, CountdownHost);
        FooterStatus.Visibility = Visibility.Collapsed;
        DashboardTip.Visibility = Visibility.Collapsed;
        SessionRange.Visibility = Visibility.Collapsed;
        Countdown.FontSize = compact ? 32 : 40;
    }

    internal static string ClockText(TimeSpan span, bool roundUp = true)
    {
        var seconds = Math.Max(0L, (long)(roundUp ? Math.Ceiling(span.TotalSeconds) : Math.Floor(span.TotalSeconds)));
        return $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}";
    }

    private DateTimeOffset displayedClockInstant;
    internal void UpdateClock(DateTimeOffset? at = null)
    {
        EndSessionToolbarButton.Content=state.Session?.FinalizedAt!=null?"View Recap":"End Session";
        EndSessionToolbarButton.IsEnabled=state.Session!=null;
        SessionResetButton.Visibility = state.Session?.AutomaticScheduling == true ? Visibility.Collapsed : Visibility.Visible;
        var now = at ?? DateTimeOffset.UtcNow;displayedClockInstant=now;
        ReconcileSchedule(now);
        var session = state.Session;
        var reading = PixelTiming.Read(session, now);
        var progress = completionVisualProgress??reading.VisualFraction;
        UpdateWidgetClocks(now);
        UpdateFocusExperience(now); var complete = session != null && session.StoppedAt == null && now >= session.End;
        if (session != null)
        {
            var milestones = Milestones.Collect(session, now);
            if (milestones.Count > 0) { if(FeedbackBounds(state.Widgets.FirstOrDefault(w=>w.PanelKey=="pixels")?.Id) is {} milestoneBounds) feedbackOverlay?.Show("milestone",milestoneBounds,milestones.Last()); flashUntil = now.AddSeconds(6); MilestoneLabel.Text = string.Join(" · ", milestones); Changed(); }
        }
        var flashing = now < flashUntil && !complete && session?.IsRunning(now) == true;
        MilestoneLabel.Visibility = flashing ? Visibility.Visible : Visibility.Collapsed;
        CompletionBanner.Visibility = complete && !completionDismissed && !storyOpen ? Visibility.Visible : Visibility.Collapsed;
        // Only the current active pixel breathes; completion stays filled in the theme color.
        if (session != null)
        {
            SessionTitle.Text = session.Title;
            SessionRange.Text = $"{NexaTime.Clock(session.Start, Options, true)} → {NexaTime.Clock(session.End, Options, true)}";
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
        PercentLabel.Text = NexaTime.Percentage(progress);
        SessionProgressBar.Value=progress*100;
        var active=reading.Status=="IN SESSION" && session?.FinalizedAt==null;
        var mainTime=active && reading.PhaseEnd is {} phaseEnd?phaseEnd-now:reading.Time;
        Countdown.Text=mainTime.TotalDays>=1?$"{(int)mainTime.TotalDays:00}:{mainTime.Hours:00}:{mainTime.Minutes:00}":ClockText(mainTime);
        CountdownUnits.Text=mainTime.TotalDays>=1?"days        hours        minutes":"hours       minutes      seconds";
        ElapsedCaption.Text=active?"ELAPSED":reading.Status is "SCHEDULED" or "BETWEEN SESSIONS"?"NEXT SESSION DURATION":"SESSION STATUS";
        ElapsedLabel.Text=active && reading.PhaseStart is {} phaseStart?ClockText(now-phaseStart,false):reading.Status is "SCHEDULED" or "BETWEEN SESSIONS"?DurationLabel.Text:"Ready for a fresh session";
        CountdownCaption.Text = active?"TIME REMAINING":reading.Caption; StatusLabel.Text = reading.Status;
        var remainingFraction = reading.VisualFraction;
        UpdateFrenzyFeedback();
        var head = completionVisualProgress is {} fill ? (int)Math.Floor(fill*pixels.Count) : SessionValuesWereReset?0:PixelTiming.CompletedPixels(reading, pixels.Count);
        if (previousHead >= 0 && previousHead < pixels.Count)
        { pixels[previousHead].BeginAnimation(OpacityProperty, null); pixels[previousHead].Opacity = 1; }
        if (head != litPixels)
            for (var i = 0; i < pixels.Count; i++)
            { pixels[i].BeginAnimation(OpacityProperty, null); pixels[i].Opacity = 1; pixels[i].Background = DashboardBrush(i < head ? "PixelBrush" : "PixelOffBrush"); }
        litPixels = head; previousHead = -1;
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
            text.Children.Add(new TextBlock { Text = notePreview.Length == 0 ? (inProgress ? "No transcript available for this interval." : "No transcript available for this interval.") : notePreview, FontSize = 12, MaxHeight = 74, TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 6, 0, 0), Foreground = inProgress ? Brush("AccentBrush") : Brush("TextBrush") });
            var card = new Border { Child = text, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Tag = (start, end) };
            var first = i; var groupCount = Math.Min(stride, count - i);
            var button = new Button { Content = card, Padding = new Thickness(0), Background = Brushes.Transparent, BorderThickness = new Thickness(0), ToolTip = "Open interval notes" };
            button.Click += (_, _) => OpenIntervalNotes(session, first, groupCount);
            button.HorizontalContentAlignment = HorizontalAlignment.Stretch; intervals.Add(card); Timeline.Children.Add(button);
        }
    }

}
