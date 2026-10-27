using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    private string TrackerAssetDirectory => Path.Combine(store.UserPreferencesDirectory, "tracker-icons");
    private static TextBlock TrackerText(string text, double size = 14) => new() { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 6) };
    private static Button TrackerButton(Panel panel, string text, Action action)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 4, 6, 4), Padding = new Thickness(10, 7, 10, 7) };
        button.Click += (_, _) => action(); panel.Children.Add(button); return button;
    }
    private void TrackerSave() { Changed(); SaveNow(); }
    private bool TrackerConfirm(Window owner, string message, string action) =>
        new NexaConfirmation(owner, action + "?", message, action).ShowDialog() == true;
    private void BuildDistributionTracker(StackPanel panel, WidgetInstance widget)
    {
        var tracker = widget.Distribution ??= new();
        var body = new StackPanel();
        var scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Height = Math.Max(230, widget.Height - 74) };
        panel.Children.Add(scroll);
        var selected = new HashSet<Guid>();
        var toggles = new List<ToggleButton>();
        var categoryLabels = new Dictionary<Guid, TextBlock>();
        var total = TrackerText("", 32); var summary = TrackerText(""); var recent = new StackPanel();
        var choices = new WrapPanel();
        Button? add = null; Button? undo = null;
        DistributionStatistics? stats = null;
        long lastAdd = -10000;
        void AddEntry()
        {
            var now = Environment.TickCount64;
            if (now - lastAdd < 400) return;
            lastAdd = now;
            var session = tracker.ActiveSession ?? tracker.Start(DateTimeOffset.Now);
            session.Add(selected, DateTimeOffset.Now); selected.Clear();
            foreach (var toggle in toggles) toggle.IsChecked = false;
            TrackerSave(); Refresh(); add?.Focus();
        }
        void Manage()
        {
            ManageDistribution(widget); selected.Clear(); RenderWidgets();
        }
        var setup = tracker.ActiveSession?.Configuration ?? tracker.Setup;
        body.Children.Add(total); body.Children.Add(summary);
        if (setup.Categories.All(c => c.Retired))
        {
            body.Children.Add(TrackerText("Choose Argo Pantry Shift or create your own categories in Manage Tracker."));
        }
        body.Children.Add(choices);
        foreach (var category in setup.Categories.Where(c => !c.Retired))
        {
            var label = new StackPanel { Orientation = Orientation.Horizontal };
            label.Children.Add(TrackerIcons.Create(category.IconId, tracker, TrackerAssetDirectory));
            var categoryLabel = new TextBlock { Text = category.Name, MaxWidth = 160, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            categoryLabels[category.Id] = categoryLabel; label.Children.Add(categoryLabel);
            var toggle = new ToggleButton { Content = label, MinHeight = widget.DisplayMode == "Compact" ? 40 : 48, Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(10, 6, 10, 6), ToolTip = category.Name, Tag = category.Id };
            // Base theme has no ToggleButton style. Use explicit theme resources and a strong checked border.
            toggle.SetResourceReference(Control.BackgroundProperty, "PanelBrush");
            toggle.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            toggle.SetResourceReference(Control.BorderBrushProperty, "LineBrush");
            toggle.Checked += (_, _) => { selected.Add(category.Id); toggle.BorderThickness = new Thickness(3); toggle.SetResourceReference(Control.BorderBrushProperty, "AccentBrush"); };
            toggle.Unchecked += (_, _) => { selected.Remove(category.Id); toggle.BorderThickness = new Thickness(1); toggle.SetResourceReference(Control.BorderBrushProperty, "LineBrush"); };
            System.Windows.Automation.AutomationProperties.SetName(toggle, category.Name);
            toggles.Add(toggle); choices.Children.Add(toggle);
        }
        add = TrackerButton(body, "ADD " + setup.Singular.ToUpperInvariant(), AddEntry);
        add.Style = (Style)FindResource("PrimaryButton"); add.MinHeight = 44;
        var actions = new WrapPanel(); body.Children.Add(actions);
        undo = TrackerButton(actions, "Undo last entry", () => { if (tracker.ActiveSession?.Undo() == true) { TrackerSave(); Refresh(); } });
        TrackerButton(actions, "View statistics", () => { if (tracker.ActiveSession is { } session) ShowDistributionStatistics(tracker, session); });
        TrackerButton(actions, "Manage Tracker", Manage);
        var mode = TrackerButton(actions, widget.DisplayMode == "Compact" ? "Expanded view" : "Compact view", () =>
        {
            widget.DisplayMode = widget.DisplayMode == "Compact" ? "Expanded" : "Compact";
            tracker.Setup.DisplayMode = widget.DisplayMode; if (tracker.ActiveSession is { } s) s.Configuration.DisplayMode = widget.DisplayMode;
            TrackerSave(); RenderWidgets(); // Content only; never resize or move another card.
        });
        body.Children.Add(recent);
        void RefreshSummary(DateTimeOffset now)
        {
            if (tracker.ActiveSession is not { } session || stats == null) { summary.Text = "Ready to start a new session"; return; }
            var rate = stats.Rate(session, now);
            var max = stats.CategoryTotals.Values.DefaultIfEmpty(0).Max();
            var most = max == 0 ? "—" : string.Join(", ", session.Configuration.Categories.Where(c => stats.CategoryTotals.GetValueOrDefault(c.Id) == max).Select(c => c.Name)) + $" ({max})";
            var peak = stats.Hours.Count == 0 ? "Not enough data yet" : string.Join(", ", stats.Hours.Where(h => h.Count == stats.Hours.Max(x => x.Count)).Select(h => $"{h.Start:h tt}–{h.Start.AddHours(1):h tt}"));
            summary.Text = $"{setup.Plural} / Hour: {(rate.HasValue ? rate.Value.ToString("0.0") : "Available after 5 min")}\nPeak Hour: {peak}\nMost Selected: {most}";
        }
        void Refresh()
        {
            stats = tracker.ActiveSession is { } session ? DistributionStatistics.Calculate(session) : null;
            foreach (var c in setup.Categories.Where(c => !c.Retired)) categoryLabels[c.Id].Text = c.Name + (widget.DisplayMode == "Compact" ? "" : $" · {stats?.CategoryTotals.GetValueOrDefault(c.Id) ?? 0}");
            total.Text = $"{stats?.Total ?? 0} {setup.Plural} Today";
            if (undo != null) undo.IsEnabled = tracker.ActiveSession?.Entries.Count > 0;
            RefreshSummary(DateTimeOffset.Now);
            recent.Children.Clear();
            if (widget.DisplayMode == "Compact" || tracker.ActiveSession is not { } active) return;
            recent.Children.Add(TrackerText($"Average Items / {setup.Singular}: {stats!.AverageItems:0.0}"));
            recent.Children.Add(TrackerText("Recent entries", 16));
            foreach (var entry in active.Entries.TakeLast(5).Reverse())
            {
                var row = new StackPanel();
                row.Children.Add(TrackerText($"#{entry.Number} · {entry.Timestamp.LocalDateTime:t} · {EntryCategories(active, entry)}"));
                var buttons = new WrapPanel(); row.Children.Add(buttons);
                TrackerButton(buttons, "Edit", () => { EditDistributionEntry(tracker, active, entry); Refresh(); });
                TrackerButton(buttons, "Delete", () => { if (TrackerConfirm(this, $"Delete {setup.Singular} #{entry.Number}? Totals will be recalculated.", "Delete entry")) { active.Entries.Remove(entry); TrackerSave(); Refresh(); } });
                recent.Children.Add(row);
            }
        }
        panel.PreviewKeyDown += (_, e) =>
        {
            if (e.OriginalSource is TextBox || Keyboard.Modifiers != ModifierKeys.None || e.IsRepeat) return;
            var index = e.Key >= Key.D1 && e.Key <= Key.D9 ? e.Key - Key.D1 : e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9 ? e.Key - Key.NumPad1 : -1;
            if (index >= 0 && index < toggles.Count) { toggles[index].IsChecked = toggles[index].IsChecked != true; e.Handled = true; }
            else if (e.Key == Key.Escape) { foreach (var toggle in toggles) toggle.IsChecked = false; e.Handled = true; }
            else if (e.Key == Key.Enter && (e.OriginalSource == add || e.OriginalSource is ToggleButton)) { AddEntry(); e.Handled = true; }
        };
        // Only the elapsed-time division changes with time; entry analytics run on mutations.
        DateTimeOffset lastRate = DateTimeOffset.MinValue;
        widgetTicks.Add(now => { if ((now - lastRate).TotalSeconds >= 30) { lastRate = now; RefreshSummary(now); } });
        Refresh();
    }
    private static string EntryCategories(DistributionSession session, DistributionEntry entry) => entry.CategoryIds.Count == 0 ? "No categories" :
        string.Join(", ", entry.CategoryIds.Select(id => session.Configuration.Categories.FirstOrDefault(c => c.Id == id)?.Name ?? "Unknown category"));
    private void EditDistributionEntry(DistributionTrackerState tracker, DistributionSession session, DistributionEntry entry)
    {
        var dialog = Dialog($"Edit {session.Configuration.Singular} #{entry.Number}", 480, 520);
        var body = new StackPanel { Margin = new Thickness(22) };
        body.Children.Add(TrackerText(entry.Timestamp.LocalDateTime.ToString("f")));
        var boxes = new List<(Guid Id, CheckBox Box)>();
        foreach (var c in session.Configuration.Categories.Where(c => !c.Retired || entry.CategoryIds.Contains(c.Id)))
        {
            var box = new CheckBox { Content = c.Name + (c.Retired ? " (retired)" : ""), IsChecked = entry.CategoryIds.Contains(c.Id), Margin = new Thickness(4, 8, 4, 8) };
            body.Children.Add(box); boxes.Add((c.Id, box));
        }
        TrackerButton(body, "Save entry", () => { entry.CategoryIds = boxes.Where(x => x.Box.IsChecked == true).Select(x => x.Id).ToList(); TrackerSave(); dialog.Close(); });
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDialog();
    }
    private void ShowDistributionStatistics(DistributionTrackerState tracker, DistributionSession session)
    {
        var dialog = Dialog("Distribution statistics", 640, 700); var body = new StackPanel { Margin = new Thickness(22) };
        var stats = DistributionStatistics.Calculate(session); var config = session.Configuration;
        body.Children.Add(TrackerText(session.Title, 24));
        body.Children.Add(TrackerText($"{session.StartedAt.LocalDateTime:g} – {(session.EndedAt is { } end ? end.LocalDateTime.ToString("g") : "Active")}\nPreset: {session.PresetName}"));
        var rate = stats.Rate(session, DateTimeOffset.Now);
        body.Children.Add(TrackerText($"{stats.Total} {config.Plural}\n{config.Plural} / Hour: {(rate.HasValue ? rate.Value.ToString("0.0") : "Available after 5 min")}\nAverage Items / {config.Singular}: {stats.AverageItems:0.00}", 20));
        var max = stats.CategoryTotals.Values.DefaultIfEmpty(0).Max();
        body.Children.Add(TrackerText("Most Selected: " + (max == 0 ? "Not enough data yet" : string.Join(", ", config.Categories.Where(c => stats.CategoryTotals.GetValueOrDefault(c.Id) == max).Select(c => c.Name)) + $" · {max}")));
        foreach (var c in config.Categories)
            body.Children.Add(TrackerText($"{c.Name}{(c.Retired ? " (retired)" : "")}: {stats.CategoryTotals.GetValueOrDefault(c.Id)} · {stats.Percentage(c.Id):0}%"));
        body.Children.Add(TrackerText(config.Plural + " by hour (hours with entries)", 18));
        var peak = stats.Hours.Select(h => h.Count).DefaultIfEmpty(0).Max();
        if (peak == 0) body.Children.Add(TrackerText("Peak Hour: Not enough data yet"));
        foreach (var hour in stats.Hours)
        {
            body.Children.Add(TrackerText($"{hour.Start:MMM d, h tt}–{hour.Start.AddHours(1):h tt} ({hour.Start:zzz}) · {hour.Count}" + (hour.Count == peak ? " · Peak Hour" : "")));
            body.Children.Add(new ProgressBar { Height = 8, Maximum = Math.Max(1, peak), Value = hour.Count });
        }
        body.Children.Add(TrackerText("Entries", 18));
        var list = new ListBox { Height = 190, ItemsSource = session.Entries.AsEnumerable().Reverse().Select(e => $"#{e.Number} · {e.Timestamp.LocalDateTime:g} · {EntryCategories(session, e)}") };
        body.Children.Add(list);
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDialog();
    }
}
