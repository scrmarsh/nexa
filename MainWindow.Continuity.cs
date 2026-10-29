using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly DispatcherTimer autosave = new() { Interval = TimeSpan.FromMinutes(5) };
    private Task? pendingWrite;
    private long revision, saveTicket;
    private bool closing, updatingDashboardPicker;
    internal bool HasUnsavedChanges => dirty;
    internal TimeSpan AutosaveInterval => autosave.Interval;
    private static bool SaveException(Exception ex) => ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException;

    private void InitializeContinuity()
    {
        var recovery = store.Warning;
        state.Workspace ??= Continuity.Migrate(state, store.UserId);
        InitializeReliability();
        state.Workspace.CleanShutdown = false;
        Continuity.Access(Continuity.Current(state.Workspace, store.UserId));
        ready = false;
        RestoreDraft(); RefreshDashboardPicker();
        fullWidth = state.WindowWidth; fullHeight = state.WindowHeight;
        SetCompact(state.Compact);
        ready = true;
        foreach (var box in new[] { TitleInput, DurationInput, IntervalInput, TaskInput }) box.TextChanged += (_, _) => Changed();
        StartDate.SelectedDateChanged += (_, _) => Changed(); EndDate.SelectedDateChanged += (_, _) => Changed();
        StartTime.TimeChanged += (_, _) => Changed(); EndTime.TimeChanged += (_, _) => Changed();
        SizeChanged += (_, _) => { if (!fullscreen && WindowState == WindowState.Normal) Changed(); };
        autosave.Tick += async (_, _) => await SaveInBackground();
        autosave.Start();
        dirty = !entryAlreadyPersisted;
        if(dirty)SaveNow(); // Navigation already durably wrote the unclean marker before construction.
        if (recovery != null) Feedback.Text = recovery;
    }
    private void CaptureDraft()
    {
        state.Draft = new DashboardDraft
        {
            Initialized = true, Title = TitleInput.Text, Duration = DurationInput.Text, Interval = IntervalInput.Text,
            StartDate = StartDate.SelectedDate ?? DateTime.Today, EndDate = EndDate.SelectedDate ?? DateTime.Today,
            StartMinutes = (int)StartTime.Value.TotalMinutes, EndMinutes = (int)EndTime.Value.TotalMinutes,
            TaskInput = TaskInput.Text, SelectedPresetId = (PresetPicker.SelectedItem as Preset)?.Id,
            ScheduledDays = sessionDayChecks.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key).ToList()
        };
        if (!state.Compact && !fullscreen && WindowState == WindowState.Normal)
        { state.WindowWidth = Width; state.WindowHeight = Height; fullWidth = Width; fullHeight = Height; }
    }
    private void RestoreDraft()
    {
        var d = state.Draft;
        d.ScheduledDays ??= [];
        if (!d.Initialized) return;
        RefreshPresets(d.SelectedPresetId);
        TitleInput.Text = d.Title; DurationInput.Text = d.Duration; IntervalInput.Text = d.Interval;
        StartDate.SelectedDate = d.StartDate; EndDate.SelectedDate = d.EndDate;
        StartTime.Value = TimeSpan.FromMinutes(d.StartMinutes); EndTime.Value = TimeSpan.FromMinutes(d.EndMinutes);
        TaskInput.Text = d.TaskInput;
        foreach (var pair in sessionDayChecks) pair.Value.IsChecked = d.ScheduledDays.Count == 0 ? pair.Key == d.StartDate.DayOfWeek : d.ScheduledDays.Contains(pair.Key);
    }
    private void ShowSaveStatus(string text, bool fade = false, bool warning = false)
    {
        var saving=text=="Auto-saving...";
        if (warning) saveFailureVisible = true;
        else if (!saving) saveFailureVisible = false;
        if (warning) { notifications?.Show("autosave", NexaNotifications.FailureMessage, "⚠", 3, null); saveRetry.Start(); }
        else if (saving && saveFailureVisible) { } // Keep an unresolved failure visible during a retry.
        else if (Options.ShowAutosaveNotifications) notifications?.Show("autosave", saving ? NexaNotifications.SavingMessage : "Autosave complete", saving ? "🔄" : "✓", seconds: saving ? null : 2);
        else notifications?.Dismiss("autosave");
        var rotation=(RotateTransform)SaveSpinner.RenderTransform;
        rotation.BeginAnimation(RotateTransform.AngleProperty,null);
        SaveSpinner.Visibility=saving ? Visibility.Visible : Visibility.Collapsed;
        if(saving && NexaMotion.Allowed(Options)) rotation.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(0,360,TimeSpan.FromSeconds(1.1)) { RepeatBehavior=RepeatBehavior.Forever });
        if(text=="saved") { SavedFeedback(); lastSuccessfulSaveAt=DateTimeOffset.Now; text="Auto-saved · Saved at "+lastSuccessfulSaveAt.Value.LocalDateTime.ToString("t"); }
        SaveStatus.BeginAnimation(OpacityProperty, null);
        SaveStatus.Text = text; SaveStatus.Opacity = 1;
        SaveStatus.Foreground = warning ? Brushes.SandyBrown : Brush("MutedBrush");
    }
    internal async Task SaveInBackground()
    {
        if (!dirty || closing || pendingWrite is { IsCompleted: false }) return;
        var version = revision; var ticket = ++saveTicket;
        try
        {
            CaptureDraft();
            var payload = store.Prepare(state);
            ShowSaveStatus("Auto-saving...");
            pendingWrite = Task.Run(() => store.Write(payload));
            await pendingWrite.ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                if (closing || ticket != saveTicket) return;
                if (version == revision) dirty = false;
                ShowSaveStatus("saved");
            });
        }
        catch (Exception ex) when (SaveException(ex))
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (closing || ticket != saveTicket) return;
                dirty = true; ShowSaveStatus("Save failed", warning: true);
                Feedback.Text = "Could not save changes: " + ex.Message;
            });
        }
    }
    internal bool SaveForClose()
    {

        if (!StopLocalRecording(true)) return false;
        state.Workspace!.CleanShutdown = true;
        var now = DateTimeOffset.Now;
        foreach (var d in state.Workspace.Dashboards) d.Current.ClosedAt = now;
        if (SaveNow()) return true;
        if (MessageBox.Show(this, "Your latest changes could not be saved. Close anyway and lose unsaved changes?", "Unsaved changes", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) return true;
        state.Workspace.CleanShutdown = false;
        Continuity.Access(Continuity.Current(state.Workspace, store.UserId));
        return false;
    }

    private void CloseNoteEditors()
    {
        foreach (var editor in noteWindows.Values.ToList()) editor.Close();
    }
    private void RefreshDashboardPicker()
    {
        updatingDashboardPicker = true;
        SpaceIconHost.Content=SpaceIcons.Create(SpaceIcons.Resolve(state.SpaceIcon,Continuity.Current(state.Workspace!,store.UserId).PresetOrigin),20);
        DashboardPicker.ItemsSource = null;
        DashboardPicker.ItemsSource = state.Workspace!.Dashboards.Where(d => d.UserId == store.UserId).ToList();
        DashboardPicker.SelectedItem = Continuity.Current(state.Workspace, store.UserId);
        if (DashboardNameEditor != null) DashboardNameEditor.Text = Continuity.Current(state.Workspace, store.UserId).Name;
        updatingDashboardPicker = false;
    }
    internal AppState CopyWorkspaceState()
    {
        if (settingsWindow?.FlushPendingChanges() == false) throw new System.IO.IOException("Could not save pending Settings changes.");
        var copy = Continuity.Clone(state.Workspace!);
        return Continuity.Activate(copy, store.UserId, state.HedyRegion, state.HedyProtectedKey);
    }
    // All destructive transitions commit a detached candidate before changing the live editor.
    internal bool CommitWorkspace(WorkspaceDocument candidate, bool preserveWindow = true, bool preserveWidgetUndo = false,bool sessionLoad=false)
    {
        if (settingsWindow?.FlushPendingChanges() == false) return false;
        try
        {
            var keepRecording = recordingActive && preserveWidgetUndo && candidate.ActiveDashboardId == state.Workspace!.ActiveDashboardId && Continuity.Current(candidate, store.UserId).Current.Data.Widgets.Any(w => w.Kind == "Audio Recorder");
            if (recordingActive && !keepRecording)
            {
                var recorded = recordingSession;
                if (!StopLocalRecording(true)) return false;
                if (recorded != null)
                    foreach (var target in candidate.Dashboards.Where(d => d.DashboardId == state.Workspace!.ActiveDashboardId).SelectMany(d => d.History.Append(d.Current)).Select(s => s.Data.Session).Where(s => s != null && s.Start == recorded.Start && s.End == recorded.End && s.Title == recorded.Title))
                    { target!.LocalRecordingPath = recorded.LocalRecordingPath; target.LocalRecordingSeconds = recorded.LocalRecordingSeconds; foreach (var entry in recorded.TimelineEntries.Where(e => e.Source == "recorder")) if (!target.TimelineEntries.Any(e => e.Id == entry.Id)) target.TimelineEntries.Add(Continuity.Clone(entry)); }
            }
            var transition=sessionLoad || candidate.ActiveDashboardId!=state.Workspace!.ActiveDashboardId || Continuity.Current(candidate,store.UserId).Current.SessionId!=Continuity.Current(state.Workspace,store.UserId).Current.SessionId;
            if(transition)candidate.LiveEcosystemGeneration=Guid.NewGuid();
            var next = Continuity.Activate(candidate, store.UserId, state.HedyRegion, state.HedyProtectedKey);
            store.LoadInteractionPreferences(next);
            if (preserveWindow) { next.WindowWidth = fullWidth; next.WindowHeight = fullHeight; next.Compact = state.Compact; }
            WidgetLibrary.EnsureLayout(next);
            try { pendingWrite?.GetAwaiter().GetResult(); } catch (Exception ex) when (SaveException(ex)) { }
            store.Save(next);
            settingsWindow?.Close();
            if (storyOpen) ExitBoredomMode();
            CloseNoteEditors(); PauseHedySync();
            if(state.Workspace?.ActiveDashboardId!=next.Workspace?.ActiveDashboardId || !preserveWidgetUndo && Continuity.Current(state.Workspace!,store.UserId).Current.SessionId != Continuity.Current(next.Workspace!,store.UserId).Current.SessionId) { widgetUndo.Clear(); widgetRedo.Clear(); FadeWidgetToast(null,EventArgs.Empty); }
            ready = false; state = next;
            if(transition){seasonalArtwork?.ResetSessionActivity();NexaEcosystem.For(store).ReconcileGeneration(candidate.LiveEcosystemGeneration);bugGeometryDirty=true;}
            revision++; saveTicket++; dirty = false; saveDelay.Stop();
            InitializeInitialViewport();
            ApplyReliabilityOptions();
            if (keepRecording) recordingSession = state.Session;
            UpdateRecordingSource(); UpdateRecordingStatus();
            NotesBox.Text = state.Notes; NotesToggle.IsChecked = state.ShowNotes; ChecklistToggle.IsChecked = state.ShowChecklist;
            PinToggle.IsChecked = state.AlwaysOnTop; AccentInput.Text = state.AccentColor; PixelInput.Text = state.PixelColor;
            RefreshPresets(state.Presets.FirstOrDefault()?.Id); RestoreDraft();
            if (!state.Draft.Initialized) { TitleInput.Text = Continuity.Current(candidate, store.UserId).Name; TaskInput.Clear(); }
            RefreshDashboardPicker(); UpdateDeveloperToolsVisibility(); RenderTasks(); ApplyColors(); ApplySpaceBackground(); ApplySettings(); BuildTimeline();
            HedyTranscriptBox.Text = state.Session?.HedyTranscript ?? "";
            completionDismissed = false; flashUntil = DateTimeOffset.MinValue;
            fullWidth = state.WindowWidth; fullHeight = state.WindowHeight;
            if (!preserveWindow) SetCompact(state.Compact);
            UpdateClock();
            if (widgetsReady) { editingDashboard = false; RenderWidgets(); }
            if (storyOpen) RenderStory();
            ShowSaveStatus("saved");
            return true;
        }
        catch (Exception ex) when (SaveException(ex))
        { ShowSaveStatus("Save failed", warning: true); Feedback.Text = "Space was not changed: " + ex.Message; return false; }
        finally { ready = true; }
    }
    private async void DashboardSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!ready || updatingDashboardPicker || DashboardPicker.SelectedItem is not DashboardRecord selected || selected.DashboardId == state.Workspace!.ActiveDashboardId) return;
        if (!SaveNow()) { RefreshDashboardPicker(); return; }
        var candidate = CopyWorkspaceState().Workspace!;
        Continuity.Current(candidate, store.UserId).Current.ClosedAt = DateTimeOffset.Now;
        candidate.ActiveDashboardId = selected.DashboardId;
        Continuity.Access(Continuity.Current(candidate, store.UserId));
        if (!await ChangeSpace(candidate)) RefreshDashboardPicker();
    }
    private NexaDialog Dialog(string title, double width = 720, double height = 650) => new NexaPanel()
    {
        Title = title, Width = width, Height = height, MinWidth = 420, MinHeight = 300, Owner = this,
        WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brush("BackgroundBrush"), Foreground = Brush("TextBrush")
    };
    private async void NewDashboardClick(object sender, RoutedEventArgs e)
    {
        var dialog = new DashboardCreation(this, store.UserId, store.LoadProfile());
        if (dialog.ShowDialog() != true || dialog.Result == null || !SaveNow()) return;
        var candidate = CopyWorkspaceState().Workspace!;
        candidate.Dashboards.Add(dialog.Result); candidate.ActiveDashboardId = dialog.Result.DashboardId;
        await ChangeSpace(candidate);
    }
    private void ResetDashboardClick(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Start a fresh Space session?\n\nThis archives the current session and clears session notes, session tasks, counters and timers. Permanent content, widgets, layout, presets, settings and history are preserved.",
            "Reset Space", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        if (!SaveNow()) return;
        var candidate = CopyWorkspaceState().Workspace!;
        Continuity.Reset(Continuity.Current(candidate, store.UserId));
        CommitWorkspace(candidate);
    }
    private void ExportDashboardClick(object sender, RoutedEventArgs e)
    {
        if (!SaveNow()) return;
        ExportDialog(Continuity.Current(state.Workspace!, store.UserId));
    }
    private void ExportDialog(DashboardRecord dashboard, DashboardSession? session = null)
    {
        var dialog = Dialog("Export Space", 510, 330);
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Configuration only is the default. Includes presets, panel visibility, colors and window size.", TextWrapping = TextWrapping.Wrap });
        var include = new CheckBox { Content = "Include current / selected session data", IsChecked = false, Margin = new Thickness(0, 20, 0, 10) }; panel.Children.Add(include);
        panel.Children.Add(new TextBlock { Text = "Session data includes private notes, tasks, interval notes, Hedy transcript and story progress. Credentials and history are never exported.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var save = new Button { Content = "Export .nexa", Margin = new Thickness(0, 20, 0, 0) }; panel.Children.Add(save);
        save.Click += (_, _) =>
        {
            var picker = new SaveFileDialog { Filter = "nexa Space (*.nexa)|*.nexa", DefaultExt = ".nexa", FileName = "space.nexa" };
            if (picker.ShowDialog(dialog) != true) return;
            try { StateStore.AtomicWrite(picker.FileName, Continuity.Export(dashboard, include.IsChecked == true, session), false); NexaChrome.FadeOutAndClose(dialog); Feedback.Text = "Space exported."; }
            catch (Exception ex) when (SaveException(ex)) { MessageBox.Show(dialog, ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
        };
        dialog.Content = panel; dialog.ShowDialog();
    }
    private async void ImportDashboardClick(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "nexa Space (*.nexa)|*.nexa", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 10_000_000) throw new InvalidDataException("Space file exceeds the 10 MB limit.");
            var file = Continuity.ReadExport(File.ReadAllText(picker.FileName));
            if (file.IncludesSessionData && MessageBox.Show(this, "This file includes private notes, tasks and other session content. Import that content into a new Space owned by your Windows account?", "Import session data", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            if (!SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            Continuity.Import(candidate, file, store.UserId, file.IncludesSessionData);
            if (await ChangeSpace(candidate)) Feedback.Text = "Imported as a new Space.";
        }
        catch (Exception ex) when (SaveException(ex)) { MessageBox.Show(this, ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void HistoryClick(object sender, RoutedEventArgs e)
    {
        if (!SaveNow()) return;
        var dialog = Dialog("Previous sessions — " + Continuity.Current(state.Workspace!, store.UserId).Name, 1050, 740);
        var root = new DockPanel { Margin = new Thickness(20) };
        var heading = new TextBlock { Text = "History", FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) }; DockPanel.SetDock(filters, Dock.Top); root.Children.Add(filters);
        filters.Children.Add(new TextBlock { Text = "Search name / notes ", VerticalAlignment = VerticalAlignment.Center });
        var search = new TextBox { Width = 180, Margin = new Thickness(0, 0, 12, 0) }; filters.Children.Add(search);
        var date = new DarkDatePicker { Width = 130, Margin = new Thickness(0, 0, 12, 0), ToolTip = "Filter by date; clear to show all" }; filters.Children.Add(date);
        var sort = new ComboBox { Width = 145, ItemsSource = new[] { "Most Recent", "Oldest", "Last Accessed", "Session name" }, SelectedIndex = 0 }; filters.Children.Add(sort);
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) }; DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
        var keepLayout = new CheckBox { Content = "Include historical layout / configuration when merging", IsChecked = false, Margin = new Thickness(0, 0, 0, 10) };
        DockPanel.SetDock(keepLayout, Dock.Bottom); root.Children.Add(keepLayout);
        var grid = new Grid(); grid.ColumnDefinitions.Add(new() { Width = new GridLength(320) }); grid.ColumnDefinitions.Add(new() { Width = new GridLength(16) }); grid.ColumnDefinitions.Add(new()); root.Children.Add(grid);
        var list = new ListBox { HorizontalContentAlignment = HorizontalAlignment.Stretch, Background = Brush("InputBrush"), Foreground = Brush("TextBrush"), BorderBrush = Brush("LineBrush") };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        var rowTemplate = new DataTemplate();
        var rowText = new FrameworkElementFactory(typeof(TextBlock)); rowText.SetBinding(TextBlock.TextProperty, new Binding("Display"));
        rowText.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap); rowText.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 6, 4, 6));
        rowTemplate.VisualTree = rowText; list.ItemTemplate = rowTemplate;
        var rowStyle = new Style(typeof(ListBoxItem));
        rowStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brush("TextBrush")));
        rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        rowStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        var rowBorder = new FrameworkElementFactory(typeof(Border));
        rowBorder.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });
        var rowPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        rowPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding("Content") { RelativeSource = RelativeSource.TemplatedParent });
        rowPresenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("ContentTemplate") { RelativeSource = RelativeSource.TemplatedParent });
        rowBorder.AppendChild(rowPresenter);
        rowStyle.Setters.Add(new Setter(Control.TemplateProperty, new ControlTemplate(typeof(ListBoxItem)) { VisualTree = rowBorder }));
        var selectedStyle = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selectedStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
        selectedStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brush("AccentBrush")));
        rowStyle.Triggers.Add(selectedStyle); list.ItemContainerStyle = rowStyle;
        var groupStyle = new GroupStyle(); var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(TextBlock)); factory.SetBinding(TextBlock.TextProperty, new Binding("Name"));
        factory.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold); factory.SetValue(FrameworkElement.MarginProperty, new Thickness(4, 12, 4, 8));
        template.VisualTree = factory; groupStyle.HeaderTemplate = template; list.GroupStyle.Add(groupStyle); grid.Children.Add(list);
        var preview = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Text = "Select a previous session to preview its saved content. Your current Space stays unchanged." };
        Grid.SetColumn(preview, 2); grid.Children.Add(preview);
        Button Action(string title, Action callback)
        {
            var button = new Button { Content = title, IsEnabled = false, Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(8, 6, 8, 6) };
            button.Click += (_, _) => callback(); actions.Children.Add(button); return button;
        }
        DashboardRecord Current() => Continuity.Current(state.Workspace!, store.UserId);
        void Refresh()
        {
            IEnumerable<DashboardSession> entries = Current().History.Where(s => s.UserId == store.UserId && s.DashboardId == Current().DashboardId);
            if (!string.IsNullOrWhiteSpace(search.Text)) entries = entries.Where(s => (s.Title + "\n" + s.Data.Notes).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase));
            if (date.SelectedDate is DateTime day) entries = entries.Where(s => s.OpenedAt.LocalDateTime.Date == day.Date);
            entries = sort.SelectedIndex switch { 1 => entries.OrderBy(s => s.OpenedAt), 2 => entries.OrderByDescending(s => s.LastAccessedAt), 3 => entries.OrderBy(s => s.Title), _ => entries.OrderByDescending(s => s.OpenedAt) };
            var view = new ListCollectionView(entries.ToList()); view.GroupDescriptions.Add(new PropertyGroupDescription("SessionDate")); list.ItemsSource = view;
            preview.Text = list.SelectedItem is DashboardSession selected ? PreviewSession(selected) : view.IsEmpty
                ? Current().History.Count == 0 ? "No session history yet." : "No previous sessions match your search."
                : "Select a previous session to preview its saved content. Your current Space stays unchanged.";
        }
        Action("Continue this session", () =>
        {
            if (list.SelectedItem is not DashboardSession selected) return;
            if (MessageBox.Show(dialog, "Archive your current session and continue a copy of this saved session? Its original history entry will remain. Saved timer timestamps are retained.", "Continue session", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK || !SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            Continuity.Continue(Continuity.Current(candidate, store.UserId), selected.SessionId, store.UserId);
            if (CommitWorkspace(candidate)) NexaChrome.FadeOutAndClose(dialog);
        });
        Action("Merge to current Space", () =>
        {
            if (list.SelectedItem is not DashboardSession selected) return;
            if (MessageBox.Show(dialog, "Append notes and source-labelled interval/Hedy notes, and add missing incomplete tasks and tracker records? Widget records are matched by saved identity. Current counters, timers and completed tasks stay intact. Use Continue for an exact restore. Repeated merges of this entry are skipped." +
                (keepLayout.IsChecked == true ? "\n\nHistorical presets, colors and panel configuration will replace the current configuration." : "\n\nYour layout and configuration will be preserved."), "Merge session", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK || !SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            Continuity.Merge(Continuity.Current(candidate, store.UserId), selected.SessionId, store.UserId, keepLayout.IsChecked == true);
            if (CommitWorkspace(candidate)) NexaChrome.FadeOutAndClose(dialog);
        });
        Action("Export", () => { if (list.SelectedItem is DashboardSession selected) ExportDialog(Current(), selected); });
        Action("Delete", () =>
        {
            if (list.SelectedItem is not DashboardSession selected || MessageBox.Show(dialog, "Permanently delete this historical session?", "Delete history", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes || !SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            Continuity.Current(candidate, store.UserId).History.RemoveAll(s => s.SessionId == selected.SessionId && s.UserId == store.UserId);
            if (CommitWorkspace(candidate)) Refresh();
        });
        var clearHistory = new Button { Content = "Clear History", Style = (Style)FindResource("DestructiveButton"), Margin = new Thickness(12, 0, 0, 8) };
        clearHistory.Click += (_, _) =>
        {
            if (!NexaConfirmation.ClearAllHistory(dialog) || !SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            foreach (var dashboard in candidate.Dashboards) dashboard.History.Clear();
            if (CommitWorkspace(candidate)) Refresh();
        };
        actions.Children.Add(clearHistory);
        list.SelectionChanged += (_, _) =>
        {
            foreach (var b in actions.Children.OfType<Button>().Where(b => !ReferenceEquals(b, clearHistory)))
                b.IsEnabled = list.SelectedItem is DashboardSession;
            if (list.SelectedItem is not DashboardSession selected) return;
            preview.Text = PreviewSession(selected);
            // Viewing only updates access metadata, never copies historical content into the editor.
            selected.LastAccessedAt = DateTimeOffset.Now; Changed();
        };
        search.TextChanged += (_, _) => Refresh(); date.SelectedDateChanged += (_, _) => Refresh(); sort.SelectionChanged += (_, _) => Refresh();
        Refresh(); dialog.Content = root; dialog.ShowDismissible();
    }
    internal static string PreviewSession(DashboardSession session) => HistoryArchive.Report(session);
    internal static string BuildSessionReport(DashboardSession session)
    {
        var data = session.Data;
        var text = new StringBuilder($"Previous session\n{session.Title}\n{NexaTime.Local(session.OpenedAt, session.TimeZoneId):F}\nLast accessed: {NexaTime.Local(session.LastAccessedAt, session.TimeZoneId):g}\n{session.Summary}\n\nNOTES\n{data.Notes}\n\nTASKS\n");
        text.Insert(0, $"SESSION REPORT\nID: {session.SessionId}\nPreset: {session.PresetName} ({session.PresetId})\nTime zone: {session.TimeZoneId}\nScheduled UTC: {session.ScheduledStart:O} → {session.ScheduledEnd:O}\nObserved UTC: {session.OpenedAt.ToUniversalTime():O} → {session.UpdatedAt.ToUniversalTime():O}\nArchived UTC: {session.ClosedAt?.ToUniversalTime():O}\n\n");
        var ledger = data.Productivity;
        if(ledger.SquashesBySpecies is {Count:>0})text.AppendLine("Squashes by species: "+string.Join(", ",ledger.SquashesBySpecies.Select(p=>$"{p.Key}: {p.Value}")));
        text.AppendLine($"\nPRODUCTIVITY\nScore: {ledger.Score}\nBase points: {ledger.Events.Sum(e => e.BasePoints)}\nBonus points: {ledger.Events.Sum(e => e.BonusPoints)}\nActions: {ledger.Events.Count}\nBugs squashed: {ledger.BugsSquashed}\nFrenzy activations: {ledger.FrenzyActivations}");
        foreach (var category in ledger.Events.GroupBy(e => e.Category)) text.AppendLine($"{category.Key}: {category.Count()} actions, {category.Sum(e => e.BasePoints + e.BonusPoints)} points");
        foreach (var action in ledger.Events) text.AppendLine($"{NexaTime.Local(action.At, session.TimeZoneId):g} · {action.Category}: {action.Description} ({action.BasePoints + action.BonusPoints:+0;-0;0})");
        text.AppendLine("\nTASKS");
        foreach (var task in data.Tasks) text.AppendLine($"{(task.Done ? "☑" : "☐")} {task.Text}");
        if (data.Session is { } timer)
        {
            text.AppendLine($"\nTIMER\n{timer.Title}\n{NexaTime.Local(timer.Start, session.TimeZoneId):g} → {NexaTime.Local(timer.End, session.TimeZoneId):g}\nStopped: {timer.StoppedAt?.LocalDateTime.ToString("g") ?? "No"}");
            foreach (var note in timer.IntervalNotes.OrderBy(p => p.Key)) text.AppendLine($"\nInterval {note.Key + 1}\n{note.Value}");
            foreach (var note in timer.HedyIntervalNotes.OrderBy(p => p.Key)) text.AppendLine($"\nHedy interval {note.Key + 1}\n{note.Value}");
            text.AppendLine("\nHEDY TRANSCRIPT\n" + timer.HedyTranscript);
            foreach (var summary in timer.IntervalSummaries.OrderBy(p => p.Key)) text.AppendLine($"Summary {summary.Key + 1}: {summary.Value}");
            foreach (var entry in timer.TimelineEntries.OrderBy(e => e.CreatedAt)) text.AppendLine($"{NexaTime.Local(entry.CreatedAt, session.TimeZoneId):g} · {entry.Source}: {entry.Text}\nAudio: {entry.AudioPath}");
            text.AppendLine($"Recording: {timer.LocalRecordingPath} ({timer.LocalRecordingSeconds:0} seconds)");
        }
        if (data.StoryHistory.Count > 0 || data.StoryEndings.Count > 0) text.AppendLine("\nARCHIVED STORY\n" + data.StoryNode + "\n" + string.Join("\n", data.StoryHistory));
        foreach (var widget in data.Widgets)
        {
            text.AppendLine($"\n{widget.Title}\n{widget.Text}");
            text.AppendLine($"Scope: {(widget.SessionScoped ? "Session" : "Permanent")}\nTimer: {widget.TimerStart:O} → {widget.TimerEnd:O} · stopped {widget.TimerStopped:O}\nStopwatch: {widget.StopwatchElapsedSeconds + (widget.StopwatchStartedAt is { } began ? Math.Max(0, (session.UpdatedAt - began).TotalSeconds) : 0):0.###} seconds, {widget.StopwatchLaps.Count} laps\nFlashcards: {widget.FlashcardDecks.Sum(d => d.Cards.Count)} cards in {widget.FlashcardDecks.Count} decks");
            if (widget.StopwatchLaps.Count > 0) text.AppendLine("Laps (seconds): " + string.Join(", ", widget.StopwatchLaps.Select(value => value.ToString("0.###"))));
            if (widget.CalculationHistory.Count > 0) text.AppendLine("Calculations: " + string.Join("; ", widget.CalculationHistory));
            if (widget.Distribution != null) text.AppendLine("Distribution tracker:\n" + JsonSerializer.Serialize(widget.Distribution, Continuity.Json));
            if (WidgetLibrary.Definition(widget.Kind).Editor is "counter" or "waterBottles" or "beer") text.AppendLine("Count: " + widget.Value);
            foreach (var item in widget.Items) text.AppendLine($"{(item.Done ? "☑" : "☐")} {item.Text}");
            foreach (var record in widget.Records) text.AppendLine($"{NexaTime.Local(record.CreatedAt, session.TimeZoneId):g} · {record.Name} · {string.Join(", ", record.Categories)} · {string.Join("; ", record.Values.Select(p => p.Key + ": " + p.Value))}");
            foreach (var deck in widget.FlashcardDecks)
                foreach (var card in deck.Cards) text.AppendLine($"Study · {deck.Name}: {card.Front} · {card.StudyState}");
            foreach (var snapshot in widget.IntegrationSnapshots)
            {
                text.AppendLine($"Observed {NexaTime.Local(snapshot.CapturedAt, session.TimeZoneId):g} · {snapshot.Service} · {snapshot.ListName} · {snapshot.TotalItems} items");
                foreach (var item in snapshot.Items) text.AppendLine($"  {item.Title} · {item.Status} · Due {item.Due} · {item.Start:O} → {item.End:O}");
                if (snapshot.TotalItems > snapshot.Items.Count) text.AppendLine($"  Snapshot limited to the first {snapshot.Items.Count} items.");
            }
            foreach (var pin in widget.ScripturePins) text.AppendLine($"{pin.Reference} · {pin.Translation} · {(pin.Permanent ? "Permanent" : "Session")}\n{pin.Text}\nNote: {pin.Note}");
            text.AppendLine($"Layout: column {widget.Column + 1}, row {widget.Row + 1}, width {widget.Span}/12, height {widget.Height}");
        }
        text.AppendLine($"\nCONFIGURATION\nNotes panel: {(data.ShowNotes ? "Shown" : "Hidden")}\nChecklist: {(data.ShowChecklist ? "Shown" : "Hidden")}\nAlways on top: {data.AlwaysOnTop}\nCompact mode: {data.Compact}\nAccent: {data.AccentColor} · Pixels: {data.PixelColor}\nWindow: {data.WindowWidth:0} × {data.WindowHeight:0}");
        text.AppendLine("\nPRESETS");
        foreach (var p in data.Presets) text.AppendLine($"{p.Name} — {p.Minutes} minutes, {p.IntervalMinutes}-minute intervals · {p.AccentColor} / {p.PixelColor}");
        var draft = data.Draft;
        text.AppendLine($"\nUNSTARTED SESSION DRAFT\n{draft.Title}\nDuration: {draft.Duration} minutes · Interval: {draft.Interval} minutes\n{draft.StartDate.AddMinutes(draft.StartMinutes):g} → {draft.EndDate.AddMinutes(draft.EndMinutes):g}\nTask draft: {draft.TaskInput}");
        return text.ToString();
    }
}
