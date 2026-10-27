using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private void OpenSessionSettingsLegacy()
    {
        var dialog = Dialog("Nexa · Session", 860, 680);
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(180) }); root.ColumnDefinitions.Add(new()); root.RowDefinitions.Add(new()); root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var navigation = new StackPanel { Margin = new Thickness(16, 24, 16, 16) };
        var sidebar = new Border { Background = Brush("PanelBrush"), Child = navigation }; Grid.SetRowSpan(sidebar, 2); root.Children.Add(sidebar);
        var pageHost = new ContentControl { Margin = new Thickness(28, 24, 28, 24), HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        Grid.SetColumn(pageHost, 1); root.Children.Add(pageHost);
        var feedback = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(28, 0, 28, 20) };
        feedback.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(TextBlock.Text)) { Source = Feedback }); Grid.SetColumn(feedback, 1); Grid.SetRow(feedback, 1); root.Children.Add(feedback);
        var restores = new List<Action>();
        var pages = new Dictionary<string, StackPanel>();
        var tabs = new Dictionary<string, Button>();
        void Select(string name)
        {
            if (pageHost.Content is ScrollViewer previous) previous.Content = null;
            pageHost.Content = new ScrollViewer { Content = pages[name], VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
            foreach (var pair in tabs) { pair.Value.SetResourceReference(Control.BackgroundProperty, pair.Key == name ? "AccentBrush" : "PanelBrush"); pair.Value.SetResourceReference(Control.ForegroundProperty, pair.Key == name ? "BackgroundBrush" : "TextBrush"); }
        }
        StackPanel Page(string title, string hint)
        {
            var page = new StackPanel(); pages.Add(title, page);
            page.Children.Add(new TextBlock { Text = title, FontSize = 25, FontWeight = FontWeights.SemiBold });
            page.Children.Add(new TextBlock { Text = hint, TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 8, 0, 24) });
            var tab = new Button { Content = title, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(0) };
            tab.Click += (_, _) => Select(title); tabs.Add(title, tab); navigation.Children.Add(tab); return page;
        }
        FrameworkElement Move(FrameworkElement control)
        {
            var parent = (Panel)control.Parent; var index = parent.Children.IndexOf(control); var width = control.Width; var margin = control.Margin; var minHeight = control.MinHeight; var alignment = control.HorizontalAlignment;
            parent.Children.Remove(control); control.Width = double.NaN; control.MinHeight = 40; control.Margin = new Thickness(0); control.HorizontalAlignment = HorizontalAlignment.Stretch;
            restores.Add(() => { Detach(control); control.Width = width; control.MinHeight = minHeight; control.Margin = margin; control.HorizontalAlignment = alignment; parent.Children.Insert(Math.Min(index, parent.Children.Count), control); }); return control;
        }
        void Field(Panel page, string label, FrameworkElement control)
        {
            var group = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
            group.Children.Add(new TextBlock { Text = label, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 0, 0, 7) }); group.Children.Add(control); page.Children.Add(group);
        }
        void Pair(StackPanel page, string leftLabel, FrameworkElement left, string rightLabel, FrameworkElement right)
        {
            var grid = new Grid(); grid.ColumnDefinitions.Add(new()); grid.ColumnDefinitions.Add(new() { Width = new GridLength(16) }); grid.ColumnDefinitions.Add(new());
            var first = new StackPanel(); var second = new StackPanel(); Field(first, leftLabel, left); Field(second, rightLabel, right); grid.Children.Add(first); Grid.SetColumn(second, 2); grid.Children.Add(second); page.Children.Add(grid);
        }
        Button Action(Panel page, string label, Action callback, bool primary = false)
        {
            var button = new Button { Content = label, MinHeight = 40, Margin = new Thickness(0, 0, 0, 12), HorizontalContentAlignment = HorizontalAlignment.Center };
            if (primary) button.Style = (Style)FindResource("PrimaryButton"); button.Click += (_, _) => callback(); page.Children.Add(button); return button;
        }
        var times = Page("Timing", "Set the session schedule and interval length. Applying times keeps your current notes and transcript.");
        Pair(times, "Start date", Move(StartDate), "Start time", Move(StartTime));
        Pair(times, "End date", Move(EndDate), "End time", Move(EndTime));
        var dayGroup = new StackPanel { Margin = new Thickness(0, 0, 0, 18) }; dayGroup.Children.Add(new TextBlock { Text = "Days", Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 0, 0, 7) });
        var dayWrap = new WrapPanel(); dayGroup.Children.Add(dayWrap); times.Children.Add(dayGroup);
        foreach (var pair in sessionDayChecks)
        {
            var day = pair.Key; var source = pair.Value;
            var choice = new CheckBox { Content = day.ToString()[..3], IsChecked = source.IsChecked, Margin = new Thickness(0, 0, 14, 6) };
            choice.Click += (_, _) => { source.IsChecked = choice.IsChecked; Changed(); }; dayWrap.Children.Add(choice);
        }
        Pair(times, "Duration (minutes)", Move(DurationInput), "Interval (minutes)", Move(IntervalInput));
        Action(times, "Apply times", () => ApplyInlineSessionTimes(), true);
        var timerActions = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 }; times.Children.Add(timerActions);
        timerActions.Children.Add(Move(StartButton)); timerActions.Children.Add(Move(EndButton)); StartButton.Margin = new Thickness(0, 0, 8, 0); EndButton.Margin = new Thickness(8, 0, 0, 0);
        var presets = Page("Presets", "Choose a saved setup or save the current session as a reusable preset.");
        Field(presets, "Saved preset", Move(PresetPicker));
        var presetName = new TextBox { Text = TitleInput.Text, MaxLength = 100, MinHeight = 40 }; Field(presets, "Session / preset name", presetName);
        void PresetAction(RoutedEventHandler handler) { TitleInput.Text = presetName.Text; handler(dialog, new RoutedEventArgs()); }
        PresetPicker.SelectionChanged += RefreshName;
        void RefreshName(object sender, SelectionChangedEventArgs e) => presetName.Text = TitleInput.Text;
        restores.Add(() => PresetPicker.SelectionChanged -= RefreshName);
        Action(presets, "Save new preset", () => PresetAction(SavePresetClick), true);
        Action(presets, "Update preset", () => PresetAction(UpdatePresetClick));
        Action(presets, "Delete preset", () => DeletePresetClick(dialog, new RoutedEventArgs()));
        var colors = Page("Colors", "Choose an accent and a pixel color. Save color in the picker applies and saves your choice immediately.");
        Field(colors, "Dashboard main color", Move(AccentColorPicker)); Field(colors, "Dashboard accent / pixel color", Move(PixelColorPicker));
        var display = Page("Display", "Choose the dashboard layout and visibility options.");
        foreach (var option in new[] { PinToggle, NotesToggle, ChecklistToggle }) { var control = Move(option); control.Margin = new Thickness(0, 0, 0, 20); display.Children.Add(control); }
        Action(display, "One column", () => { WidgetLibrary.Arrange(state.Widgets, true); Changed(); ArrangeWidgetFrames(); SaveNow(); });
        Action(display, "Two columns", () => { WidgetLibrary.Arrange(state.Widgets, false); Changed(); ArrangeWidgetFrames(); SaveNow(); });
        Action(display, "Three columns", () => { WidgetLibrary.ArrangeThreeColumns(state.Widgets); Changed(); ArrangeWidgetFrames(); SaveNow(); });
        Action(display, "Compact mode", () => { dialog.Close(); CompactClick(dialog, new RoutedEventArgs()); });
        var hedy = Page("Hedy", "Connect your Hedy account to session notes and live transcripts.");
        Action(hedy, "Hedy API settings", () => { dialog.Close(); HedySettingsClick(dialog, new RoutedEventArgs()); }, true);
        var dashboard = Page("Dashboard", "Rename this dashboard or manage its saved data.");
        var name = new TextBox { Text = Continuity.Current(state.Workspace!, store.UserId).Name, MaxLength = 100, MinHeight = 40 }; Field(dashboard, "Dashboard name", name);
        Action(dashboard, "Save name", () => { if (string.IsNullOrWhiteSpace(name.Text) || !SaveNow()) return; var candidate = CopyWorkspaceState().Workspace!; Continuity.Current(candidate, store.UserId).Name = name.Text.Trim(); CommitWorkspace(candidate, preserveWindow: true); }, true);
        Action(dashboard, "New dashboard", () => { dialog.Close(); NewDashboardClick(dialog, new RoutedEventArgs()); });
        Action(dashboard, "Import dashboard", () => { dialog.Close(); ImportDashboardClick(dialog, new RoutedEventArgs()); });
        Action(dashboard, "Export dashboard", () => ExportDashboardClick(dialog, new RoutedEventArgs()));
        Action(dashboard, "Reset dashboard", () => { dialog.Close(); ResetDashboardClick(dialog, new RoutedEventArgs()); });
        dialog.Closed += (_, _) => { pageHost.Content = null; foreach (var restore in restores.AsEnumerable().Reverse()) restore(); };
        dialog.Content = root; Select("Timing"); dialog.ShowDismissible();
    }
}
