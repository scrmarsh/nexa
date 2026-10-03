using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private void OpenHistory()
    {
        if (!FlushSettingsForNavigation()) return;
        var dialog = new NexaPanel { Title = "Nexa · History", Owner = this, Width = 1000, Height = 700, MinWidth = 740, MinHeight = 500, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var root = new DockPanel { Margin = new Thickness(24) };
        var heading = new TextBlock { Text = "History", FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var search = new TextBox { ToolTip = "Search session names and notes", Margin = new Thickness(0, 0, 0, 16) };
        System.Windows.Automation.AutomationProperties.SetName(search, "Search history");
        var filters = new Grid { Margin = new Thickness(0, 0, 0, 16) }; filters.ColumnDefinitions.Add(new()); filters.ColumnDefinitions.Add(new() { Width = new GridLength(150) }); filters.ColumnDefinitions.Add(new() { Width = new GridLength(160) });
        search.Margin = new Thickness(0, 0, 16, 0); filters.Children.Add(search);
        var date = new DarkDatePicker { ToolTip = "Filter by date", Margin = new Thickness(0, 0, 16, 0) }; Grid.SetColumn(date, 1); filters.Children.Add(date);
        var sort = new ComboBox { ItemsSource = new[] { "Most recent", "Oldest", "Session name" }, SelectedIndex = 0 }; Grid.SetColumn(sort, 2); filters.Children.Add(sort); DockPanel.SetDock(filters, Dock.Top); root.Children.Add(filters);
        var footer = new StackPanel { Margin = new Thickness(0, 16, 0, 0) }; DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush") }; footer.Children.Add(error);
        var keepLayout = new CheckBox { Content = "Include saved layout and colors when merging", Margin = new Thickness(0, 12, 0, 0) }; footer.Children.Add(keepLayout);
        var actions = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4 }; footer.Children.Add(actions);
        var body = new Grid(); body.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) }); body.ColumnDefinitions.Add(new() { Width = new GridLength(20) }); body.ColumnDefinitions.Add(new() { Width = new GridLength(1.6, GridUnitType.Star) }); root.Children.Add(body);
        var list = new ListBox { HorizontalContentAlignment = HorizontalAlignment.Stretch }; list.SetResourceReference(Control.BackgroundProperty, "InputBrush"); list.SetResourceReference(Control.ForegroundProperty, "TextBrush"); body.Children.Add(list);
        var preview = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetColumn(preview, 2); body.Children.Add(preview);
        DashboardSession? selected = null; Guid dashboardId = Guid.Empty;
        void Refresh()
        {
            list.Items.Clear();
            var entries = (store.Load().Workspace?.Dashboards ?? []).SelectMany(d => d.History.Select(s => (Dashboard: d, Session: s)))
                .Where(e => (e.Session.Title + " " + e.Session.Data.Notes + " " + e.Dashboard.Name).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase) && (date.SelectedDate == null || e.Session.OpenedAt.LocalDateTime.Date == date.SelectedDate.Value.Date));
            entries = sort.SelectedIndex switch { 1 => entries.OrderBy(e => e.Session.OpenedAt), 2 => entries.OrderBy(e => e.Session.Title), _ => entries.OrderByDescending(e => e.Session.OpenedAt) };
            foreach (var entry in entries) list.Items.Add(new ListBoxItem { Content = $"{entry.Dashboard.Name}\n{entry.Session.Title}\n{entry.Session.OpenedAt.LocalDateTime:g}", Tag = (entry.Dashboard.DashboardId, entry.Session), Padding = new Thickness(10), Foreground = (Brush)FindResource("TextBrush") });
            selected = null; foreach (Button action in actions.Children) action.IsEnabled = false;
            preview.Text = list.Items.Count == 0 ? "No previous sessions match your search." : "Select a session to view its saved notes and data.";
        }
        void SaveChange(Action<DashboardRecord> change, bool open)
        {
            try
            {
                var saved = store.Load(); var workspace = Continuity.Clone(saved.Workspace!); var dashboard = workspace.Dashboards.Single(d => d.DashboardId == dashboardId);
                change(dashboard); if (open) workspace.ActiveDashboardId = dashboardId; store.Save(Continuity.Activate(workspace, store.UserId, saved.HedyRegion, saved.HedyProtectedKey));
                if (open) { dialog.Close(); OpenDashboard(dashboardId); } else Refresh();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { error.Text = "Could not save history change: " + ex.Message; }
        }
        void Action(string label, Action callback)
        {
            var button = new Button { Content = label, IsEnabled = false, Margin = new Thickness(0, 12, 8, 0) }; button.Click += (_, _) => callback(); actions.Children.Add(button);
        }
        Action("Continue session", () =>
        {
            if (selected == null || MessageBox.Show(dialog, "Archive the current session and continue a copy of this saved session?", "Continue session", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            SaveChange(d => Continuity.Continue(d, selected.SessionId, store.UserId), true);
        });
        Action("Merge to Space", () =>
        {
            if (selected == null || MessageBox.Show(dialog, "Merge notes, incomplete tasks and records into this Space while preserving its layout?", "Merge session", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            SaveChange(d => Continuity.Merge(d, selected.SessionId, store.UserId, keepLayout.IsChecked == true), true);
        });
        Action("Export", () =>
        {
            if (selected == null) return;
            var file = new SaveFileDialog { Filter = "Nexa Space session|*.json", FileName = "session.json" }; if (file.ShowDialog(dialog) != true) return;
            try { var dashboard = store.Load().Workspace!.Dashboards.Single(d => d.DashboardId == dashboardId); File.WriteAllText(file.FileName, Continuity.Export(dashboard, true, selected)); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { error.Text = ex.Message; }
        });
        Action("Delete", () =>
        {
            if (selected == null || MessageBox.Show(dialog, "Permanently delete this historical session?", "Delete history", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            SaveChange(d => d.History.RemoveAll(s => s.SessionId == selected.SessionId), false);
        });
        list.SelectionChanged += (_, _) =>
        {
            if (list.SelectedItem is not ListBoxItem { Tag: ValueTuple<Guid, DashboardSession> entry }) return;
            dashboardId = entry.Item1; selected = entry.Item2;
            preview.Text = MainWindow.PreviewSession(selected);
            foreach (Button action in actions.Children) action.IsEnabled = true;
        };
        search.TextChanged += (_, _) => Refresh(); date.SelectedDateChanged += (_, _) => Refresh(); sort.SelectionChanged += (_, _) => Refresh(); Refresh(); dialog.Content = root; dialog.ShowDismissible();
    }
}
