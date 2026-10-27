using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed class SessionHistoryWindow : NexaPanel
{
    private readonly StateStore store;
    private readonly MainWindow? space;
    private readonly Action<Guid>? openSpace;
    private readonly ListBox list = new() { DisplayMemberPath = "Display", HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly TextBox search = new() { ToolTip = "Search recorded names, notes, tasks, widgets and events" };
    private readonly DarkDatePicker date = new() { Width = 145 };
    private readonly ComboBox presets = new() { Width = 160 };
    private readonly TextBox report = new() { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private readonly List<Button> mutationButtons = [];
    private readonly CheckBox includeLayout = new() { Content = "Include saved layout and colors when merging", Margin = new Thickness(0, 4, 10, 4) };
    private Button? clearHistoryButton;
    private HistoryEntry[] entries = [];
    private bool closed, loading;
    private long query, selection;
    internal SessionHistoryWindow(Window owner, StateStore store, MainWindow? space = null, Action<Guid>? openSpace = null)
    {
        this.store = store; this.space = space; this.openSpace = openSpace;
        Owner = owner; Title = space == null ? "Nexa · History" : "Nexa · Notes & History";
        Width = 1080; Height = 740; MinWidth = 700; MinHeight = 480; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(22) };
        var header = new TextBlock { Text = "Notes & History", FontSize = 26, Margin = new Thickness(0, 0, 0, 14) }; DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var filters = new Grid(); filters.ColumnDefinitions.Add(new()); filters.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); filters.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        search.Margin = new Thickness(0, 0, 10, 0); date.Margin = new Thickness(0, 0, 10, 0); filters.Children.Add(search); Grid.SetColumn(date, 1); filters.Children.Add(date); Grid.SetColumn(presets, 2); filters.Children.Add(presets);
        DockPanel.SetDock(filters, Dock.Top); root.Children.Add(filters); DockPanel.SetDock(status, Dock.Top); root.Children.Add(status);
        var actions = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) }; DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
        var columns = new Grid(); columns.ColumnDefinitions.Add(new() { Width = new GridLength(300) }); columns.ColumnDefinitions.Add(new());
        VirtualizingPanel.SetIsVirtualizing(list, true); VirtualizingPanel.SetVirtualizationMode(list, VirtualizationMode.Recycling); ScrollViewer.SetCanContentScroll(list, true);
        list.Margin = new Thickness(0, 0, 14, 0); columns.Children.Add(list); Grid.SetColumn(report, 1); columns.Children.Add(report); root.Children.Add(columns);
        Button Action(string label, System.Action callback, bool mutation = false)
        {
            var button = new Button { Content = label, Margin = new Thickness(0, 0, 8, 6) };
            button.Click += (_, _) => { try { callback(); } catch (Exception ex) when (HistoryArchive.Recoverable(ex)) { Fail(ex); } };
            if (mutation) { mutationButtons.Add(button); button.IsEnabled = false; } actions.Children.Add(button); return button;
        }
        Action("Export summary", () => Export(false)); Action("Export recorded data", () => Export(true));
        Action("Edit archived notes", EditNotes, true);
        Action("Delete archived record", () =>
        {
            if (list.SelectedItem is not HistoryEntry { Editable: true, Session: {} selected }) return;
            if (MessageBox.Show(this, "Permanently delete this historical record?", "Delete record", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                Change(d => d.History.RemoveAll(s => s.SessionId == selected.SessionId));
        }, true);
        if (openSpace != null)
        {
            actions.Children.Add(includeLayout);
            Action("Continue session", () => Continue(false), true); Action("Merge to Space", () => Continue(true), true);
        }
        clearHistoryButton = Action("Clear History", ClearHistory);
        clearHistoryButton.IsEnabled = false;
        clearHistoryButton.SetResourceReference(StyleProperty, "DestructiveButton");
        Content = root;
        System.Windows.Automation.AutomationProperties.SetName(search, "Search history");
        search.TextChanged += (_, _) => QueueQuery(); date.SelectedDateChanged += (_, _) => QueueQuery(); presets.SelectionChanged += (_, _) => QueueQuery();
        debounce.Tick += async (_, _) => { debounce.Stop(); await Filter(); };
        list.SelectionChanged += async (_, _) => await ShowReport();
        Loaded += async (_, _) => await LoadArchive();
        Closed += (_, _) => { closed = true; query++; selection++; debounce.Stop(); };
    }
    private async Task LoadArchive()
    {
        if (!Dispatcher.CheckAccess()) { await OnUi(() => _ = LoadArchive()); return; }
        loading = true; status.Text = "Loading recorded sessions…";
        try
        {
            var snapshot = await Task.Run(() => HistoryArchive.Read(store));
            await OnUi(() =>
            {
            if (closed) return;
            entries = snapshot.Entries; status.Text = snapshot.Warning ?? $"{entries.Length} recorded sessions. Opening a report never starts a session.";
            presets.ItemsSource = new[] { "All presets" }.Concat(entries.Select(e => e.Session?.PresetName).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()!).ToArray(); presets.SelectedIndex = 0;
            mutationsAllowed = snapshot.Warning == null;
            if (clearHistoryButton != null) clearHistoryButton.IsEnabled = mutationsAllowed && entries.Any(e => e.Editable);
            });
        }
        catch (Exception ex) when (HistoryArchive.Recoverable(ex)) { Fail(ex); }
        finally { await OnUi(() => loading = false); }
        await OnUi(() => _ = Filter());
    }
    private bool mutationsAllowed;
    private async Task OnUi(System.Action action)
    {
        if (closed || Dispatcher.HasShutdownStarted) return;
        try { await Dispatcher.InvokeAsync(() => { if (!closed) action(); }); }
        catch (TaskCanceledException) when (closed || Dispatcher.HasShutdownStarted) { }
    }
    private void Fail(Exception ex)
    {
        if (closed) return;
        if (!Dispatcher.CheckAccess()) { _ = OnUi(() => Fail(ex)); return; }
        HistoryArchive.Log(store, ex); status.Text = "History could not complete this action: " + ex.Message + " Your original records are preserved.";
    }
    private void QueueQuery() { if (loading) return; query++; debounce.Stop(); debounce.Start(); }
    private async Task Filter()
    {
        if (!Dispatcher.CheckAccess()) { await OnUi(() => _ = Filter()); return; }
        if (closed || loading) return;
        var token = ++query; var needle = search.Text; var day = date.SelectedDate?.Date; var preset = presets.SelectedIndex > 0 ? presets.SelectedItem?.ToString() : null; var source = entries;
        try
        {
            var matches = await Task.Run(() => source.Where(e => (preset == null || e.Session?.PresetName == preset) &&
                (day == null || e.Session != null && NexaTime.Local(e.Session.ScheduledStart ?? e.Session.OpenedAt, e.Session.TimeZoneId).Date == day) &&
                (string.IsNullOrWhiteSpace(needle) || e.Raw.Contains(needle, StringComparison.OrdinalIgnoreCase) || e.Space.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(e => e.Session?.ScheduledStart ?? e.Session?.OpenedAt ?? DateTimeOffset.MinValue).ToArray());
            await OnUi(() =>
            {
            if (closed || token != query) return;
            list.ItemsSource = matches; list.SelectedIndex = matches.Length == 0 ? -1 : 0;
            if (matches.Length == 0) report.Text = entries.Length == 0 ? "No recorded sessions yet. Your saved sessions will appear here." : "No sessions match these filters.";
            });
        }
        catch (Exception ex) when (HistoryArchive.Recoverable(ex)) { Fail(ex); }
    }
    private async Task ShowReport()
    {
        if (!Dispatcher.CheckAccess()) { await OnUi(() => _ = ShowReport()); return; }
        var token = ++selection; var entry = list.SelectedItem as HistoryEntry;
        foreach (var button in mutationButtons) button.IsEnabled = mutationsAllowed && entry?.Editable == true;
        if (entry == null) return;
        report.Text = "Loading session report…";
        try { var text = await Task.Run(() => entry.Report); await OnUi(() => { if (token == selection) report.Text = text; }); }
        catch (Exception ex) when (HistoryArchive.Recoverable(ex)) { Fail(ex); }
    }
    private void Export(bool raw)
    {
        if (list.SelectedItem is not HistoryEntry entry) return;
        var target = new SaveFileDialog { Filter = raw ? "Recorded JSON|*.json" : "Readable report|*.txt", FileName = raw ? "Nexa-record.json" : "Nexa-session.txt" };
        if (target.ShowDialog(this) == true) File.WriteAllText(target.FileName, raw ? entry.Raw : entry.Report);
    }
    private void Change(System.Action<DashboardRecord> update, bool open = false)
    {
        if (!mutationsAllowed || list.SelectedItem is not HistoryEntry { Editable: true } entry) return;
        if (space != null && !space.SaveNow()) throw new IOException("The current session could not be saved.");
        var saved = space?.CopyWorkspaceState() ?? store.Load();
        if (saved.Workspace == null) throw new InvalidDataException("The saved workspace is unavailable.");
        var candidate = Continuity.Clone(saved.Workspace); var dashboard = candidate.Dashboards.Single(d => d.DashboardId == entry.DashboardId);
        update(dashboard); if (open) candidate.ActiveDashboardId = dashboard.DashboardId;
        if (space != null) { if (!space.CommitWorkspace(candidate)) throw new IOException("The archive change could not be saved."); }
        else store.Save(Continuity.Activate(candidate, store.UserId, saved.HedyRegion, saved.HedyProtectedKey));
        if (open) { Close(); openSpace?.Invoke(dashboard.DashboardId); }
        else _ = LoadArchive();
    }
    private void EditNotes()
    {
        if (list.SelectedItem is not HistoryEntry { Editable: true, Session: {} selected }) return;
        var editor = new NexaPanel { Owner = this, Title = "Edit archived session notes", Width = 650, Height = 460, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var dock = new DockPanel { Margin = new Thickness(18) }; var save = new Button { Content = "Save notes" }; DockPanel.SetDock(save, Dock.Bottom); dock.Children.Add(save);
        var text = new TextBox { Text = selected.Data.Notes, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dock.Children.Add(text); editor.Content = dock;
        save.Click += (_, _) => { try { Change(d => { var record = d.History.Single(s => s.SessionId == selected.SessionId); record.Data.Notes = text.Text; record.UpdatedAt = DateTimeOffset.UtcNow; }); editor.Close(); } catch (Exception ex) when (HistoryArchive.Recoverable(ex)) { Fail(ex); } };
        editor.ShowDismissible();
    }
    private void ClearHistory()
    {
        if (!mutationsAllowed || !NexaConfirmation.ClearAllHistory(this)) return;
        if (space != null && !space.SaveNow()) throw new IOException("The current session could not be saved.");
        var saved = space?.CopyWorkspaceState() ?? store.Load();
        if (saved.Workspace == null) throw new InvalidDataException("The workspace is unavailable.");
        var candidate = Continuity.Clone(saved.Workspace);
        foreach (var dashboard in candidate.Dashboards) dashboard.History.Clear();
        if (space != null) { if (!space.CommitWorkspace(candidate)) throw new IOException("The history change could not be saved."); }
        else store.Save(Continuity.Activate(candidate, store.UserId, saved.HedyRegion, saved.HedyProtectedKey));
        _ = LoadArchive();
    }
    private void Continue(bool merge)
    {
        if (list.SelectedItem is not HistoryEntry { Editable: true, Session: {} selected }) return;
        if (MessageBox.Show(this, merge ? "Merge recorded notes and tasks into this Space?" : "Archive the current session and continue a copy of this saved session?", "Change active Space", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
        Change(d => { if (merge) Continuity.Merge(d, selected.SessionId, store.UserId, includeLayout.IsChecked == true); else Continuity.Continue(d, selected.SessionId, store.UserId); }, true);
    }
}
