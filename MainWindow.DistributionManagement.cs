using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private void ManageDistribution(WidgetInstance widget)
    {
        var tracker = widget.Distribution ??= new();
        var dialog = Dialog("Manage Tracker", 650, 740); var body = new StackPanel { Margin = new Thickness(22) };
        void Refresh()
        {
            body.Children.Clear();
            body.Children.Add(TrackerText(widget.Title, 24));
            TrackerButton(body, "Edit title, terminology and categories", () =>
            {
                var config = Continuity.Clone(tracker.ActiveSession?.Configuration ?? tracker.Setup);
                if (!EditDistributionSetup(tracker, config, false)) return;
                tracker.Setup = Continuity.Clone(config);
                if (tracker.ActiveSession is { } session) session.Configuration = config;
                widget.Title = config.Title; widget.DisplayMode = config.DisplayMode; TrackerSave(); Refresh();
            });
            body.Children.Add(TrackerText("Presets", 20));
            body.Children.Add(TrackerText("Presets contain configuration only. Loading one archives the current session and starts a separate session."));
            var presets = new ComboBox { ItemsSource = tracker.Presets, DisplayMemberPath = "Name", SelectedItem = tracker.Presets.FirstOrDefault(p => p.Id == tracker.LoadedPresetId) ?? tracker.Presets.FirstOrDefault(), Margin = new Thickness(0, 4, 0, 8) }; body.Children.Add(presets);
            var row = new WrapPanel(); body.Children.Add(row);
            TrackerButton(row, "Load preset", () =>
            {
                if (presets.SelectedItem is not DistributionPreset preset) return;
                if (tracker.ActiveSession != null && !TrackerConfirm(dialog, "Archive the active session and start a new session with this preset? All existing entries remain in history.", "Load preset")) return;
                tracker.Load(preset, DateTimeOffset.Now); widget.Title = tracker.Setup.Title; widget.DisplayMode = tracker.Setup.DisplayMode; TrackerSave(); Refresh();
            });
            TrackerButton(row, "New preset", () =>
            {
                var preset = new DistributionPreset();
                if (EditDistributionSetup(tracker, preset, true)) { tracker.Presets.Add(preset); TrackerSave(); Refresh(); }
            });
            TrackerButton(row, "Save setup as preset", () =>
            {
                var preset = (tracker.ActiveSession?.Configuration ?? tracker.Setup).ConfigurationCopy(); preset.Id = Guid.NewGuid(); preset.Name = preset.Title + " preset";
                if (EditDistributionSetup(tracker, preset, true)) { tracker.Presets.Add(preset); TrackerSave(); Refresh(); }
            });
            TrackerButton(row, "Edit / rename preset", () =>
            {
                if (presets.SelectedItem is not DistributionPreset preset) return;
                var copy = Continuity.Clone(preset);
                if (EditDistributionSetup(tracker, copy, true)) { tracker.Presets[tracker.Presets.IndexOf(preset)] = copy; TrackerSave(); Refresh(); }
            });
            TrackerButton(row, "Duplicate preset", () =>
            {
                if (presets.SelectedItem is not DistributionPreset preset) return;
                var copy = preset.ConfigurationCopy(); copy.Id = Guid.NewGuid(); copy.Name += " copy"; tracker.Presets.Add(copy); TrackerSave(); Refresh();
            });
            TrackerButton(row, "Delete preset", () =>
            {
                if (presets.SelectedItem is not DistributionPreset preset || !TrackerConfirm(dialog, "Delete this reusable preset? Active and historical sessions will be kept.", "Delete preset")) return;
                tracker.Presets.Remove(preset); if (tracker.DefaultPresetId == preset.Id) tracker.DefaultPresetId = null; TrackerSave(); Refresh();
            });
            TrackerButton(row, "Set default", () => { if (presets.SelectedItem is DistributionPreset preset) { tracker.DefaultPresetId = preset.Id; TrackerSave(); Refresh(); } });
            TrackerButton(row, "Clear default", () => { tracker.DefaultPresetId = null; TrackerSave(); Refresh(); });
            TrackerButton(row, "Restore Argo preset", () => { tracker.Presets.Add(DistributionPreset.Argo()); TrackerSave(); Refresh(); });
            body.Children.Add(TrackerText("Default for new sessions: " + (tracker.Presets.FirstOrDefault(p => p.Id == tracker.DefaultPresetId)?.Name ?? "Current setup")));
            body.Children.Add(TrackerText("Session", 20));
            if (tracker.ActiveSession is { } active)
            {
                body.Children.Add(TrackerText($"{active.Title} · {active.StartedAt.LocalDateTime:g} · {active.Entries.Count} {active.Configuration.Plural}"));
                var title = new TextBox { Text = active.Title, ToolTip = "Session title" }; body.Children.Add(title);
                TrackerButton(body, "Save session title", () => { if (!string.IsNullOrWhiteSpace(title.Text)) { active.Title = title.Text.Trim(); TrackerSave(); Refresh(); } });
            }
            var sessions = new WrapPanel(); body.Children.Add(sessions);
            TrackerButton(sessions, "Start new session", () =>
            {
                if (tracker.ActiveSession != null && !TrackerConfirm(dialog, "End and archive the current session, then start a new one?", "Start new session")) return;
                var preset = tracker.Presets.FirstOrDefault(p => p.Id == tracker.DefaultPresetId);
                if (preset != null) { tracker.Load(preset, DateTimeOffset.Now); widget.Title = tracker.Setup.Title; widget.DisplayMode = tracker.Setup.DisplayMode; }
                else tracker.Start(DateTimeOffset.Now);
                TrackerSave(); Refresh();
            });
            TrackerButton(sessions, "End session", () =>
            {
                if (tracker.ActiveSession == null || !TrackerConfirm(dialog, "End this session and save it in history?", "End session")) return;
                tracker.End(DateTimeOffset.Now); TrackerSave(); Refresh();
            });
            TrackerButton(sessions, "Reset session", () =>
            {
                if (tracker.ActiveSession == null || !TrackerConfirm(dialog, "Clear this session's entries, total count, category totals and statistics? Previous sessions are kept.", "Reset session")) return;
                tracker.ActiveSession.Entries.Clear(); tracker.ActiveSession.NextNumber = 1; tracker.ActiveSession.StartedAt = DateTimeOffset.Now; TrackerSave(); Refresh();
            });
            body.Children.Add(TrackerText("Session history", 20));
            var history = new ListBox { ItemsSource = tracker.History.AsEnumerable().Reverse().ToList(), Height = 160 };
            history.ItemsSource = null;
            foreach (var session in tracker.History.AsEnumerable().Reverse()) history.Items.Add(new ListBoxItem { Content = $"{session.StartedAt.LocalDateTime:d} · {session.Title} · {session.Entries.Count} {session.Configuration.Plural}", Tag = session });
            body.Children.Add(history);
            TrackerButton(body, "Open selected session", () => { if (history.SelectedItem is ListBoxItem { Tag: DistributionSession session }) ShowDistributionStatistics(tracker, session); });
        }
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Refresh(); dialog.ShowDialog();
    }
    private bool EditDistributionSetup(DistributionTrackerState tracker, DistributionPreset draft, bool isPreset)
    {
        var dialog = Dialog(isPreset ? "Edit preset" : "Tracker setup", 700, 760); var body = new StackPanel { Margin = new Thickness(22) }; bool saved = false;
        TextBox Field(string label, string value)
        {
            body.Children.Add(TrackerText(label)); var field = new TextBox { Text = value, Margin = new Thickness(0, 0, 0, 8) }; body.Children.Add(field); return field;
        }
        var name = isPreset ? Field("Preset name", draft.Name) : null;
        var title = Field("Widget title", draft.Title);
        var singular = Field("Singular (Visitor, Student, Family, or custom)", draft.Singular);
        var plural = Field("Plural (Visitors, Students, Families, or custom)", draft.Plural);
        body.Children.Add(TrackerText("Display"));
        var display = new ComboBox { ItemsSource = new[] { "Auto", "Compact", "Expanded" }, SelectedItem = draft.DisplayMode }; body.Children.Add(display);
        body.Children.Add(TrackerText("Categories · drag-free ordering with Up / Down", 18));
        var categories = new StackPanel(); body.Children.Add(categories);
        void RefreshCategories()
        {
            categories.Children.Clear();
            foreach (var category in draft.Categories.Where(c => !c.Retired).ToList())
            {
                var row = new StackPanel { Margin = new Thickness(0, 4, 0, 10) }; categories.Children.Add(row);
                var input = new TextBox { Text = category.Name, ToolTip = "Category name" }; input.TextChanged += (_, _) => category.Name = input.Text; row.Children.Add(input);
                var actions = new WrapPanel(); row.Children.Add(actions);
                var icon = TrackerButton(actions, "Choose icon", () => { PickTrackerIcon(tracker, category, dialog); RefreshCategories(); });
                var iconContent = new StackPanel { Orientation = Orientation.Horizontal }; iconContent.Children.Add(TrackerIcons.Create(category.IconId, tracker, TrackerAssetDirectory)); iconContent.Children.Add(TrackerText(" Choose icon")); icon.Content = iconContent;
                TrackerButton(actions, "Remove icon", () => { category.IconId = null; RefreshCategories(); });
                void Move(int delta)
                {
                    var visible = draft.Categories.Where(c => !c.Retired).ToList(); var index = visible.IndexOf(category); var target = index + delta;
                    if (target < 0 || target >= visible.Count) return;
                    var other = visible[target]; var a = draft.Categories.IndexOf(category); var b = draft.Categories.IndexOf(other);
                    (draft.Categories[a], draft.Categories[b]) = (draft.Categories[b], draft.Categories[a]); RefreshCategories();
                }
                TrackerButton(actions, "Up", () => Move(-1)); TrackerButton(actions, "Down", () => Move(1));
                TrackerButton(actions, "Delete category", () =>
                {
                    if (!TrackerConfirm(dialog, "Remove this category from future entries? Earlier entries and totals retain its stable ID and name.", "Delete category")) return;
                    category.Retired = true; RefreshCategories();
                });
            }
        }
        RefreshCategories();
        TrackerButton(body, "Add category", () => { draft.Categories.Add(new()); RefreshCategories(); });
        var error = TrackerText(""); body.Children.Add(error);
        TrackerButton(body, "Save setup", () =>
        {
            if (new[] { title.Text, singular.Text, plural.Text, name?.Text ?? "preset" }.Any(string.IsNullOrWhiteSpace) || draft.Categories.Any(c => !c.Retired && string.IsNullOrWhiteSpace(c.Name))) { error.Text = "Enter a title, singular/plural terms, and a name for each category."; return; }
            draft.Title = title.Text.Trim(); draft.Singular = singular.Text.Trim(); draft.Plural = plural.Text.Trim(); draft.Name = name?.Text.Trim() ?? draft.Name; draft.DisplayMode = display.SelectedItem as string ?? "Auto";
            saved = true; dialog.Close();
        });
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDialog(); return saved;
    }
}
