using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    private void PickTrackerIcon(DistributionTrackerState tracker, DistributionCategory category, Window owner)
    {
        var dialog = Dialog("Choose tracker icon", 720, 740); dialog.Owner = owner;
        var dock = new DockPanel { Margin = new Thickness(20) };
        var controls = new StackPanel(); DockPanel.SetDock(controls, Dock.Top); dock.Children.Add(controls);
        controls.Children.Add(TrackerText("Choose icon for " + category.Name, 22));
        var search = new TextBox { ToolTip = "Search icons", Margin = new Thickness(0, 4, 0, 8) }; controls.Children.Add(search);
        System.Windows.Automation.AutomationProperties.SetName(search, "Search icons");
        var filters = new[] { "All", "Recent" }.Concat(TrackerIcons.BuiltIn.Select(i => i.Group).Distinct()).Append("Custom").ToArray();
        var filter = new ComboBox { ItemsSource = filters, SelectedIndex = 0 }; controls.Children.Add(filter);
        var customActions = new WrapPanel(); controls.Children.Add(customActions);
        var message = TrackerText(""); controls.Children.Add(message);
        var grid = new WrapPanel(); dock.Children.Add(new ScrollViewer { Content = grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        CustomTrackerIcon? selected = null;
        var selectedName = new TextBox { Width = 180, ToolTip = "Custom icon display name", Margin = new Thickness(0, 4, 6, 4) }; customActions.Children.Add(selectedName);
        string? PickFile()
        {
            var picker = new OpenFileDialog { Filter = "Icon images|*.png;*.jpg;*.jpeg", Title = "Import tracker icon (PNG with transparency recommended)" };
            return picker.ShowDialog(dialog) == true ? picker.FileName : null;
        }
        void Safely(Action action)
        {
            try { action(); message.Text = ""; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.IO.FileFormatException) { message.Text = "Icon could not be imported: " + ex.Message; }
        }
        TrackerButton(customActions, "Import", () => Safely(() =>
        {
            if (PickFile() is not { } path) return;
            var icon = TrackerIcons.Import(path, TrackerAssetDirectory); tracker.CustomIcons.Add(icon); selected = icon; selectedName.Text = icon.Name; filter.SelectedItem = "Custom"; TrackerSave(); Refresh();
        }));
        TrackerButton(customActions, "Rename", () =>
        {
            if (selected == null || string.IsNullOrWhiteSpace(selectedName.Text)) { message.Text = "Select a custom icon below and enter its name."; return; }
            selected.Name = selectedName.Text.Trim(); TrackerSave(); Refresh();
        });
        TrackerButton(customActions, "Replace", () => Safely(() =>
        {
            if (selected == null) { message.Text = "Select a custom icon first."; return; }
            if (PickFile() is not { } path) return;
            var replacement = TrackerIcons.Import(path, TrackerAssetDirectory);
            // Keep the old managed file: another widget or historical workspace may reference it.
            selected.StoredFile = replacement.StoredFile; selected.OriginalFilename = replacement.OriginalFilename; selected.ImportedAt = replacement.ImportedAt;
            TrackerSave(); Refresh();
        }));
        TrackerButton(customActions, "Delete", () =>
        {
            if (selected == null) { message.Text = "Select a custom icon first."; return; }
            if (!TrackerConfirm(dialog, "Remove this icon from this tracker's library? Assigned categories and presets will display a clipboard fallback. Original managed files are retained for other widgets and saved workspaces.", "Delete icon")) return;
            tracker.CustomIcons.Remove(selected); tracker.RecentIcons.Remove(selected.Id); selected = null; selectedName.Text = ""; TrackerSave(); Refresh();
        });
        void Assign(string id)
        {
            category.IconId = id; tracker.RecentIcons.Remove(id); tracker.RecentIcons.Insert(0, id); tracker.RecentIcons = tracker.RecentIcons.Take(12).ToList(); TrackerSave(); dialog.Close();
        }
        void Refresh()
        {
            grid.Children.Clear(); var group = filter.SelectedItem as string ?? "All"; var query = search.Text.Trim();
            var icons = TrackerIcons.BuiltIn.Select(i => (i.Id, i.Name, i.Group)).Concat(tracker.CustomIcons.Select(i => (i.Id, i.Name, Group: "Custom")));
            foreach (var icon in icons.Where(i => (group == "All" || group == i.Group || group == "Recent" && tracker.RecentIcons.Contains(i.Id)) && (i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || i.Group.Contains(query, StringComparison.OrdinalIgnoreCase))))
            {
                var cell = new StackPanel { Width = 122, Margin = new Thickness(4) };
                var content = new StackPanel(); content.Children.Add(TrackerIcons.Create(icon.Id, tracker, TrackerAssetDirectory, 28)); content.Children.Add(new TextBlock { Text = icon.Name, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });
                var choose = TrackerButton(cell, icon.Name, () => Assign(icon.Id)); choose.Content = content; choose.MinHeight = 78; choose.ToolTip = "Assign " + icon.Name;
                System.Windows.Automation.AutomationProperties.SetName(choose, "Assign " + icon.Name);
                if (icon.Group == "Custom") TrackerButton(cell, "Manage", () => { selected = tracker.CustomIcons.First(i => i.Id == icon.Id); selectedName.Text = selected.Name; message.Text = "Selected: " + selected.Name; });
                grid.Children.Add(cell);
            }
        }
        search.TextChanged += (_, _) => Refresh(); filter.SelectionChanged += (_, _) => Refresh();
        dialog.Content = dock; Refresh(); dialog.ShowDialog();
    }
}
