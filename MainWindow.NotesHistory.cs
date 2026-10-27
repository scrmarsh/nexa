using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    internal void ShowNotesHistory()=>NotesHistoryClick(this,new RoutedEventArgs());
    private void NotesHistoryClick(object sender, RoutedEventArgs e)
    {
        if (!SaveNow()) return;
        var dialog = Dialog("Nexa · Notes & History", 1080, 740);
        var root = new DockPanel { Margin = new Thickness(20) };
        var title = new TextBlock { Text = "Notes & History", FontSize = 25, Margin = new Thickness(0, 0, 0, 12) };
        DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
        var filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        var search = new TextBox { Width = 200, ToolTip = "Search notes, tasks, widgets and transcripts", Margin = new Thickness(0, 0, 8, 0) };
        var date = new DatePicker { Width = 130, ToolTip = "Session date in its recorded time zone", Margin = new Thickness(0, 0, 8, 0) };
        var preset = new ComboBox { Width = 180, ToolTip = "Filter by preset", Margin = new Thickness(0, 0, 8, 0) };
        filters.Children.Add(search); filters.Children.Add(date); filters.Children.Add(preset);
        DockPanel.SetDock(filters, Dock.Top); root.Children.Add(filters);
        var controls = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) };
        DockPanel.SetDock(controls, Dock.Bottom); root.Children.Add(controls);
        var columns = new Grid();
        columns.ColumnDefinitions.Add(new() { Width = new GridLength(310) }); columns.ColumnDefinitions.Add(new());
        var list = new ListBox { DisplayMemberPath = "Display", HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 12, 0) };
        ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
        var report = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(report, 1); columns.Children.Add(list); columns.Children.Add(report); root.Children.Add(columns);
        IEnumerable<DashboardSession> All() => state.Workspace!.Dashboards.SelectMany(d => d.History.Append(d.Current));
        preset.ItemsSource = new[] { "All presets" }.Concat(All().Select(s => s.PresetName).Where(x => x.Length > 0).Distinct()).ToArray(); preset.SelectedIndex = 0;
        void Refresh()
        {
            var selected = (list.SelectedItem as DashboardSession)?.SessionId;
            var filtered = All().Where(s =>
                (preset.SelectedIndex <= 0 || s.PresetName == preset.SelectedItem?.ToString()) &&
                (date.SelectedDate == null || NexaTime.Local(s.ScheduledStart ?? s.OpenedAt, s.TimeZoneId).Date == date.SelectedDate.Value.Date) &&
                (string.IsNullOrWhiteSpace(search.Text) || PreviewSession(s).Contains(search.Text, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(s => s.ScheduledStart ?? s.OpenedAt).ToArray();
            list.ItemsSource = filtered;
            list.SelectedItem = filtered.FirstOrDefault(s => s.SessionId == selected) ?? filtered.FirstOrDefault();
            if (filtered.Length == 0) report.Text = "No sessions match these filters.";
        }
        void Button(string label, Action callback)
        {
            var button = new System.Windows.Controls.Button { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 0) };
            button.Click += (_, _) => callback(); controls.Children.Add(button);
        }
        Button("Export summary", () =>
        {
            if (list.SelectedItem is not DashboardSession selected) return;
            var target = new SaveFileDialog { Filter = "Text report (*.txt)|*.txt", FileName = "Nexa-session-" + selected.SessionId.ToString("N")[..8] + ".txt" };
            if (target.ShowDialog(dialog) != true) return;
            try { File.WriteAllText(target.FileName, PreviewSession(selected)); notifications?.Show("export", "Session summary exported.", "✓"); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { notifications?.Show("export", ex.Message, "⚠", 3, null); }
        });
        Button("Edit archived notes", () =>
        {
            if (list.SelectedItem is not DashboardSession { IsActive: false } selected) return;
            var editor = Dialog("Edit archived session notes", 650, 460);
            var layout = new DockPanel { Margin = new Thickness(18) };
            var save = new System.Windows.Controls.Button { Content = "Save notes", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            DockPanel.SetDock(save, Dock.Bottom); layout.Children.Add(save);
            var notes = new TextBox { Text = selected.Data.Notes, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            layout.Children.Add(notes); editor.Content = layout;
            save.Click += (_, _) =>
            {
                if (!SaveNow()) return;
                var candidate = CopyWorkspaceState().Workspace!;
                var archived = candidate.Dashboards.SelectMany(d => d.History).Single(s => s.SessionId == selected.SessionId);
                archived.Data.Notes = notes.Text; archived.UpdatedAt = DateTimeOffset.UtcNow;
                if (CommitWorkspace(candidate)) { editor.CloseWithFade(); Refresh(); }
            };
            editor.ShowDismissible();
        });
        Button("Delete archived record", () =>
        {
            if (list.SelectedItem is not DashboardSession { IsActive: false } selected) return;
            if (MessageBox.Show(dialog, "Permanently delete this session report?", "Delete session", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes || !SaveNow()) return;
            var candidate = CopyWorkspaceState().Workspace!;
            foreach (var dashboard in candidate.Dashboards) dashboard.History.RemoveAll(s => s.SessionId == selected.SessionId);
            if (CommitWorkspace(candidate)) Refresh();
        });
        list.SelectionChanged += (_, _) => { if (list.SelectedItem is DashboardSession selected) report.Text = PreviewSession(selected); };
        search.TextChanged += (_, _) => Refresh(); date.SelectedDateChanged += (_, _) => Refresh(); preset.SelectionChanged += (_, _) => Refresh();
        Refresh(); dialog.Content = root; dialog.ShowDismissible();
    }
}
