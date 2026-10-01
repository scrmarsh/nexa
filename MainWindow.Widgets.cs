using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private bool widgetsReady;
    internal bool editingDashboard;
    private readonly Dictionary<string, Border> legacyPanels = [];
    private readonly StackPanel parkedPanels = new();
    private readonly Dictionary<Guid, Border> widgetFrames = [];
    private readonly List<Action<DateTimeOffset>> widgetTicks = [];
    private Border? placementPreview;

    private void InitializeWidgets()
    {
        legacyPanels.Add("pixels", PixelPanel); legacyPanels.Add("setup", SetupPanel); legacyPanels.Add("timeline", TimelinePanel);
        legacyPanels.Add("notes", NotesPanel); legacyPanels.Add("tasks", ChecklistPanel); legacyPanels.Add("hedy", HedyPanel);
        LegacyContent.Children.Add(parkedPanels); widgetsReady = true;
        var migrated = !state.WidgetLayoutInitialized; WidgetLibrary.EnsureLayout(state); RenderWidgets();
        if (migrated) { Changed(); SaveNow(); }
    }
    private static void Detach(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel: panel.Children.Remove(element); break;
            case ContentControl content: content.Content = null; break;
            case Decorator decorator: decorator.Child = null; break;
        }
    }
    internal void RenderWidgets()
    {
        if (!widgetsReady) return;
        foreach (var panel in legacyPanels.Values) { Detach(panel); parkedPanels.Children.Add(panel); }
        WidgetCanvas.Children.Clear(); widgetFrames.Clear(); widgetTicks.Clear();
        EditToolbar.Visibility = editingDashboard ? Visibility.Visible : Visibility.Collapsed;
        foreach (var widget in state.Widgets)
        {
            var frame = new Border { Background = Brush("PanelBrush"), BorderBrush = Brush(editingDashboard ? "AccentBrush" : "LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Tag = widget.Id, ClipToBounds = true };
            var layout = new DockPanel(); frame.Child = layout;
            var header = new DockPanel { Margin = new Thickness(12, 8, 10, 8) }; DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
            if (editingDashboard)
            {
                var buttons = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(buttons, Dock.Right); header.Children.Add(buttons);
                void Action(string glyph, string title, Action action)
                {
                    var b = new Button { Content = glyph, ToolTip = title, Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(3, 0, 0, 0) };
                    System.Windows.Automation.AutomationProperties.SetName(b, title + " " + widget.Title); b.Click += (_, _) => action(); buttons.Children.Add(b);
                }
                Action("⚙", "Configure", () => ConfigureWidget(widget)); Action("⧉", "Duplicate", () => DuplicateWidget(widget)); Action("×", "Remove widget", () => RemoveWidget(widget));
                var handle = new Thumb { Width = 24, Height = 28, Cursor = Cursors.SizeAll, Template = HandleTemplate("⠿"), ToolTip = "Drag to move widget" };
                System.Windows.Automation.AutomationProperties.SetName(handle, "Move " + widget.Title); header.Children.Add(handle); WireDrag(handle, widget, false);
                var resize = new Thumb { Height = 20, Width = 32, Cursor = Cursors.SizeNWSE, HorizontalAlignment = HorizontalAlignment.Right, Template = HandleTemplate("↘"), ToolTip = "Drag to resize widget" };
                System.Windows.Automation.AutomationProperties.SetName(resize, "Resize " + widget.Title); DockPanel.SetDock(resize, Dock.Bottom); layout.Children.Add(resize); WireDrag(resize, widget, true);
            }
            header.Children.Add(new TextBlock { Text = widget.Title, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
            UIElement content;
            if (widget.PanelKey.Length > 0 && legacyPanels.TryGetValue(widget.PanelKey, out var legacy))
            {
                Detach(legacy); legacy.Margin = new Thickness(0); legacy.Padding = new Thickness(14); legacy.BorderThickness = new Thickness(0); content = legacy;
            }
            else content = BuildWidgetContent(widget);
            layout.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
            WidgetCanvas.Children.Add(frame); widgetFrames.Add(widget.Id, frame);
        }
        if (state.Widgets.Count == 0)
        {
            var empty = new StackPanel { Margin = new Thickness(32) };
            empty.Children.Add(new TextBlock { Text = "Make this space yours", FontSize = 26, FontWeight = FontWeights.SemiBold });
            empty.Children.Add(new TextBlock { Text = "Add notes, a timer, a tracker, or anything else you need.", Margin = new Thickness(0, 12, 0, 20), Foreground = Brush("MutedBrush") });
            var add = new Button { Content = "Add widget", HorizontalAlignment = HorizontalAlignment.Left }; add.Click += (_, _) => { editingDashboard = true; RenderWidgets(); AddWidgetClick(add, new()); }; empty.Children.Add(add); WidgetCanvas.Children.Add(empty);
        }
        ArrangeWidgetFrames(); UpdateWidgetClocks(DateTimeOffset.Now);
    }
    private ControlTemplate HandleTemplate(string text)
    {
        var visual = new FrameworkElementFactory(typeof(TextBlock)); visual.SetValue(TextBlock.TextProperty, text); visual.SetValue(TextBlock.ForegroundProperty, Brush("MutedBrush")); visual.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); visual.SetValue(TextBlock.FontSizeProperty, 20.0);
        return new ControlTemplate(typeof(Thumb)) { VisualTree = visual };
    }
    private double GridUnit => Math.Max(600, WidgetCanvas.ActualWidth) / 12;
    private void WidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e) { if (widgetsReady && e.WidthChanged) ArrangeWidgetFrames(); }
    private void ArrangeWidgetFrames()
    {
        foreach (var widget in state.Widgets)
        {
            if (!widgetFrames.TryGetValue(widget.Id, out var frame)) continue;
            Canvas.SetLeft(frame, widget.Column * GridUnit); Canvas.SetTop(frame, widget.Row * 24);
            frame.Width = widget.Span * GridUnit - 14; frame.Height = widget.Height;
        }
        WidgetCanvas.Height = state.Widgets.Count == 0 ? 320 : state.Widgets.Max(w => w.Row * 24 + w.Height) + 24;
    }
    private void WireDrag(Thumb thumb, WidgetInstance widget, bool resize)
    {
        double dx = 0, dy = 0; int column = 0, row = 0, span = 0, height = 0;
        thumb.DragStarted += (_, _) =>
        {
            dx = dy = 0; column = widget.Column; row = widget.Row; span = widget.Span; height = widget.Height;
            placementPreview = new Border { Background = new SolidColorBrush(Color.FromArgb(40, 139, 159, 255)), BorderBrush = Brush("AccentBrush"), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(12), IsHitTestVisible = false };
            WidgetCanvas.Children.Add(placementPreview); Panel.SetZIndex(placementPreview, 100); Preview();
        };
        void Preview()
        {
            if (placementPreview == null) return;
            var def = WidgetLibrary.Definition(widget.Kind);
            if (resize) { span = Math.Clamp(widget.Span + (int)Math.Round(dx / GridUnit), def.MinSpan, 12 - widget.Column); height = Math.Clamp(widget.Height + (int)Math.Round(dy / 24) * 24, def.MinHeight, 1440); }
            else { column = Math.Clamp(widget.Column + (int)Math.Round(dx / GridUnit), 0, 12 - widget.Span); row = Math.Clamp(widget.Row + (int)Math.Round(dy / 24), 0, 10000); }
            Canvas.SetLeft(placementPreview, column * GridUnit); Canvas.SetTop(placementPreview, row * 24); placementPreview.Width = span * GridUnit - 14; placementPreview.Height = height;
            WidgetCanvas.Height = Math.Max(WidgetCanvas.Height, row * 24 + height + 24);
        }
        thumb.DragDelta += (_, e) => { dx += e.HorizontalChange; dy += e.VerticalChange; Preview(); };
        thumb.DragCompleted += (_, e) =>
        {
            if (placementPreview != null) WidgetCanvas.Children.Remove(placementPreview); placementPreview = null;
            if (!e.Canceled) { WidgetLibrary.Place(state, widget, column, row, span, height); Changed(); SaveNow(); }
            ArrangeWidgetFrames();
        };
    }
    private UIElement BuildWidgetContent(WidgetInstance widget)
    {
        var panel = new StackPanel { Margin = new Thickness(16, 6, 16, 16) };
        var editor = WidgetLibrary.Definition(widget.Kind).Editor;
        switch (editor)
        {
            case "text": case "bible": case "links":
                var text = new TextBox { Text = widget.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 110, Height = Math.Max(110, widget.Height - 125), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ToolTip = widget.Placeholder };
                System.Windows.Automation.AutomationProperties.SetName(text, widget.Title); text.TextChanged += (_, _) => { widget.Text = text.Text; Changed(); }; panel.Children.Add(text);
                if (editor is "bible" or "links")
                {
                    var open = new Button { Content = editor == "bible" ? "Open passage online" : "Open first link", Margin = new Thickness(0, 10, 0, 0) };
                    open.Click += (_, _) => OpenWeb(editor == "bible" ? "https://www.biblegateway.com/passage/?search=" + Uri.EscapeDataString(widget.Text) : widget.Text.Split('\n')[0].Trim()); panel.Children.Add(open);
                }
                break;
            case "list":
                var addRow = new DockPanel(); var input = new TextBox { MinWidth = 80 }; var add = new Button { Content = "Add", Margin = new Thickness(8, 0, 0, 0) }; DockPanel.SetDock(add, Dock.Right); addRow.Children.Add(add); addRow.Children.Add(input); panel.Children.Add(addRow);
                void AddItem() { if (string.IsNullOrWhiteSpace(input.Text)) return; widget.Items.Add(new() { Text = input.Text.Trim() }); Changed(); RenderWidgets(); }
                add.Click += (_, _) => AddItem(); input.KeyDown += (_, e) => { if (e.Key == Key.Enter) AddItem(); };
                foreach (var item in widget.Items)
                {
                    var row = new DockPanel { Margin = new Thickness(0, 8, 0, 0) }; var remove = new Button { Content = "×", Padding = new Thickness(6, 2, 6, 2) }; DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
                    remove.Click += (_, _) => { widget.Items.Remove(item); Changed(); RenderWidgets(); };
                    var check = new CheckBox { Content = new TextBlock { Text = item.Text, TextWrapping = TextWrapping.Wrap }, IsChecked = item.Done }; check.Click += (_, _) => { item.Done = check.IsChecked == true; Changed(); }; row.Children.Add(check); panel.Children.Add(row);
                }
                break;
            case "counter":
                var number = new TextBlock { Text = widget.Value.ToString(CultureInfo.CurrentCulture), FontSize = 44, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(number);
                var counts = new UniformGrid { Columns = 2 };
                foreach (var direction in new[] { -1, 1 })
                {
                    var button = new Button { Content = direction == 1 ? "+ " + widget.Increment : "− " + widget.Increment, Margin = new Thickness(3) };
                    button.Click += (_, _) => { try { widget.Value += direction * widget.Increment; number.Text = widget.Value.ToString(CultureInfo.CurrentCulture); Changed(); } catch (OverflowException) { Error("Counter limit reached."); } }; counts.Children.Add(button);
                }
                panel.Children.Add(counts); break;
            case "timer": case "clock": case "progress":
                var label = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 38, Margin = new Thickness(0, 10, 0, 15) }; panel.Children.Add(label);
                var progress = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Foreground = Brush("AccentBrush"), Background = Brush("LineBrush"), BorderThickness = new Thickness(0) };
                panel.Children.Add(progress);
                widgetTicks.Add(now =>
                {
                    if (editor == "clock") { label.Text = now.LocalDateTime.ToString(widget.ShowSeconds ? "h:mm:ss tt" : "h:mm tt"); progress.Visibility = Visibility.Collapsed; return; }
                    if (editor == "progress") { var value = state.Session?.Progress(now) ?? 0; label.Text = value.ToString("P0"); progress.Value = value; return; }
                    var end = widget.TimerEnd; var start = widget.TimerStart;
                    var effective = widget.TimerStopped ?? now;
                    var span = start == null || end == null ? TimeSpan.FromMinutes(widget.TimerMinutes) : widget.ShowElapsed ? effective - start.Value : end.Value - effective;
                    label.Text = ClockText(span); if (!widget.ShowSeconds) label.Text = label.Text[..5];
                    progress.Value = start == null || end <= start ? 0 : Math.Clamp((effective - start.Value).TotalSeconds / (end!.Value - start.Value).TotalSeconds, 0, 1);
                });
                if (editor == "timer")
                {
                    var controls = new UniformGrid { Columns = 2, Margin = new Thickness(0, 14, 0, 0) };
                    var start = new Button { Content = widget.TimerStart == null ? "Start timer" : "Restart timer", Margin = new Thickness(0, 0, 6, 0) };
                    start.Click += (_, _) => { widget.TimerStart = DateTimeOffset.Now; widget.TimerEnd = widget.TimerStart.Value.AddMinutes(widget.TimerMinutes); widget.TimerStopped = null; Changed(); SaveNow(); RenderWidgets(); };
                    var stop = new Button { Content = "Stop timer" }; stop.Click += (_, _) => { if (widget.TimerStart == null) return; widget.TimerStopped = DateTimeOffset.Now; Changed(); SaveNow(); }; controls.Children.Add(start); controls.Children.Add(stop); panel.Children.Add(controls);
                }
                break;
            case "tracker":
                BuildTracker(panel, widget); break;
            case "statistics":
                var stats = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 18 };
                widgetTicks.Add(_ => { var records = state.Widgets.SelectMany(w => w.Records).ToList(); stats.Text = $"{records.Count} records\n\n" + string.Join("\n", records.SelectMany(r => r.Categories).GroupBy(c => c).Select(g => $"{g.Key}: {g.Count()}")); }); panel.Children.Add(stats); break;
            case "timeline":
                panel.Children.Add(new TextBlock { Text = state.Session == null ? "Start a dashboard session to build its timeline." : $"{state.Session.Title}\n{state.Session.Start.LocalDateTime:t} → {state.Session.End.LocalDateTime:t}", TextWrapping = TextWrapping.Wrap });
                if (state.Session is { } session)
                    foreach (var entry in session.IntervalNotes) panel.Children.Add(new TextBlock { Text = $"Interval {entry.Key + 1}: {entry.Value}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
                break;
            case "hedy": case "setup":
                var buttonToPanel = new Button { Content = editor == "hedy" ? "Open Hedy connection" : "Open session controls" }; buttonToPanel.Click += (_, _) => OpenLegacyPanel(editor == "hedy" ? HedyPanel : SetupPanel, widget.Title); panel.Children.Add(buttonToPanel); break;
        }
        return panel;
    }
    private void BuildTracker(StackPanel panel, WidgetInstance widget)
    {
        panel.Children.Add(new TextBlock { Text = $"{widget.Records.Count} records", FontSize = 24, Margin = new Thickness(0, 0, 0, 12) });
        var name = new TextBox { ToolTip = "Student or record name" }; panel.Children.Add(new TextBlock { Text = "Student / record name", Margin = new Thickness(0, 0, 0, 6) }); panel.Children.Add(name);
        var categories = new WrapPanel { Margin = new Thickness(0, 10, 0, 10) };
        foreach (var category in widget.Categories) categories.Children.Add(new CheckBox { Content = category, Margin = new Thickness(0, 0, 12, 8) }); panel.Children.Add(categories);
        var fields = new Dictionary<string, TextBox>();
        foreach (var field in widget.Fields.Distinct(StringComparer.OrdinalIgnoreCase)) { panel.Children.Add(new TextBlock { Text = field, Margin = new Thickness(0, 8, 0, 4) }); var box = new TextBox(); fields.Add(field, box); panel.Children.Add(box); }
        var add = new Button { Content = "Add record", Margin = new Thickness(0, 12, 0, 12) };
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { name.Focus(); return; }
            widget.Records.Add(new() { Name = name.Text.Trim(), Categories = categories.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => (string)c.Content).ToList(), Values = fields.ToDictionary(p => p.Key, p => p.Value.Text) }); Changed(); SaveNow(); RenderWidgets();
        }; panel.Children.Add(add);
        foreach (var record in widget.Records.AsEnumerable().Reverse())
        {
            var row = new DockPanel { Margin = new Thickness(0, 6, 0, 0) }; var remove = new Button { Content = "×", Padding = new Thickness(6, 2, 6, 2) }; DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
            remove.Click += (_, _) => { if (MessageBox.Show(this, "Remove this record?", "Remove record", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return; widget.Records.Remove(record); Changed(); SaveNow(); RenderWidgets(); };
            row.Children.Add(new TextBlock { Text = record.Name + " · " + record.CreatedAt.LocalDateTime.ToString("g") + "\n" + string.Join(", ", record.Categories) + "\n" + string.Join(" · ", record.Values.Select(p => p.Key + ": " + p.Value)), TextWrapping = TextWrapping.Wrap }); panel.Children.Add(row);
        }
    }
    private void UpdateWidgetClocks(DateTimeOffset now) { foreach (var tick in widgetTicks) tick(now); }
    private void OpenWeb(string text)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { Error("Enter a valid https:// website address."); return; }
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { Error("Could not open your browser."); }
    }
    private void EditDashboardClick(object sender, RoutedEventArgs e) { editingDashboard = true; RenderWidgets(); }
    private void DoneEditingClick(object sender, RoutedEventArgs e) { if (!SaveNow()) return; editingDashboard = false; RenderWidgets(); }
    private void AddWidgetClick(object sender, RoutedEventArgs e)
    {
        var dialog = Dialog("Nexa · Add widget", 680, 720); var root = new DockPanel { Margin = new Thickness(24) };
        var search = new TextBox { ToolTip = "Search widgets", Margin = new Thickness(0, 0, 0, 16) }; System.Windows.Automation.AutomationProperties.SetName(search, "Search widgets"); DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);
        var list = new StackPanel(); root.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        void Refresh()
        {
            list.Children.Clear();
            foreach (var group in WidgetLibrary.Definitions.Where(d => (d.Name + " " + d.Group + " " + d.Description).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).GroupBy(d => d.Group))
            {
                list.Children.Add(new TextBlock { Text = group.Key, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 10) });
                foreach (var definition in group)
                {
                    var content = new StackPanel(); content.Children.Add(new TextBlock { Text = definition.Name, FontWeight = FontWeights.SemiBold }); content.Children.Add(new TextBlock { Text = definition.Description, TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 5, 0, 0) });
                    var button = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
                    button.Click += (_, _) => { AddWidget(definition.Name); dialog.Close(); }; list.Children.Add(button);
                }
            }
        }
        search.TextChanged += (_, _) => Refresh(); Refresh(); dialog.Content = root; dialog.ShowDialog();
    }
    internal WidgetInstance AddWidget(string kind)
    {
        if (state.Widgets.Count >= 200) throw new InvalidOperationException("A dashboard supports up to 200 widgets.");
        var panelKey = kind switch { "Pixel timer" => "pixels", "Session controls" => "setup", "Session timeline" => "timeline", "Hedy" => "hedy", _ => "" };
        if (state.Widgets.Any(w => w.PanelKey == panelKey)) panelKey = "";
        var widget = WidgetLibrary.Create(kind, panelKey); WidgetLibrary.Add(state, widget); Changed(); RenderWidgets(); SaveNow(); WidgetScroller.ScrollToBottom(); return widget;
    }
    internal WidgetInstance DuplicateWidget(WidgetInstance source)
    {
        var clone = Continuity.Clone(source); clone.Id = Guid.NewGuid(); clone.PanelKey = ""; clone.Title += " copy";
        if (source.PanelKey == "notes") clone.Text = state.Notes;
        if (source.PanelKey == "tasks") clone.Items = Continuity.Clone(state.Tasks);
        WidgetLibrary.Add(state, clone); Changed(); RenderWidgets(); SaveNow(); return clone;
    }
    private void RemoveWidget(WidgetInstance widget)
    {
        var hasData = widget.HasData || (widget.PanelKey == "notes" && state.Notes.Length > 0) || (widget.PanelKey == "tasks" && state.Tasks.Count > 0) || (widget.PanelKey is "pixels" or "timeline" or "hedy" && state.Session != null);
        if (hasData && MessageBox.Show(this, $"Remove {widget.Title}?\n\nThis widget contains saved data. Other widgets will remain unchanged.", "Remove widget", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        RemoveWidgetConfirmed(widget);
    }
    internal void RemoveWidgetConfirmed(WidgetInstance widget)
    {
        if (!SaveNow()) return;
        var candidate = CopyWorkspaceState().Workspace!; var d = Continuity.Current(candidate, store.UserId);
        // Archive first so even a confirmed removal remains recoverable from history.
        Continuity.Archive(d); d.Current.SessionId = Guid.NewGuid(); d.Current.OpenedAt = DateTimeOffset.Now;
        d.Current.Data.Widgets.RemoveAll(w => w.Id == widget.Id);
        if (widget.PanelKey == "notes") d.Current.Data.Notes = "";
        if (widget.PanelKey == "tasks") d.Current.Data.Tasks.Clear();
        if (CommitWorkspace(candidate)) { editingDashboard = true; RenderWidgets(); }
    }
    private void ConfigureWidget(WidgetInstance widget)
    {
        var dialog = Dialog("Nexa · Configure " + widget.Title, 560, 760); var panel = new StackPanel { Margin = new Thickness(24) };
        TextBox Field(string label, string value) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 6) }); var box = new TextBox { Text = value }; panel.Children.Add(box); return box; }
        var title = Field("Widget title", widget.Title); var placeholder = Field("Placeholder / hint", widget.Placeholder);
        var columns = Field("Width (grid columns, 3–12)", widget.Span.ToString()); var height = Field("Height (pixels)", widget.Height.ToString());
        var left = Field("Column position (0–11)", widget.Column.ToString()); var row = Field("Row position (24-pixel steps)", widget.Row.ToString());
        var type = WidgetLibrary.Definition(widget.Kind).Editor;
        TextBox? increment = null, starting = null, minutes = null, categories = null, fields = null;
        var seconds = new CheckBox { Content = "Show seconds", IsChecked = widget.ShowSeconds, Margin = new Thickness(0, 12, 0, 0) };
        var elapsed = new CheckBox { Content = "Show elapsed time", IsChecked = widget.ShowElapsed, Margin = new Thickness(0, 10, 0, 0) };
        if (type == "counter") { increment = Field("Increment", widget.Increment.ToString()); starting = Field("Starting value (used on reset)", widget.StartingValue.ToString()); }
        if (type == "timer") { minutes = Field("Timer duration (minutes)", widget.TimerMinutes.ToString()); panel.Children.Add(seconds); panel.Children.Add(elapsed); }
        if (type == "tracker") { categories = Field("Categories (one per line)", string.Join("\n", widget.Categories)); categories.AcceptsReturn = true; categories.Height = 100; fields = Field("Custom fields (one per line)", string.Join("\n", widget.Fields)); fields.AcceptsReturn = true; fields.Height = 100; }
        panel.Children.Add(new TextBlock { Text = "Edits autosave. Configure session times with the Session controls widget.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 16, 0, 0) });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(error);
        var save = new Button { Content = "Save settings", Margin = new Thickness(0, 18, 0, 0) }; panel.Children.Add(save);
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text) || !int.TryParse(columns.Text, out var span) || !int.TryParse(height.Text, out var h) || !int.TryParse(left.Text, out var c) || !int.TryParse(row.Text, out var r)) { error.Text = "Enter a title and valid whole-number dimensions."; return; }
            var copy = Continuity.Clone(widget);
            if (increment != null && (!decimal.TryParse(increment.Text, out var step) || step == 0)) { error.Text = "Increment must be a nonzero number."; return; }
            if (starting != null && !decimal.TryParse(starting.Text, out _)) { error.Text = "Starting value must be a number."; return; }
            if (minutes != null && !InputRules.ValidMinutes(minutes.Text, out _)) { error.Text = "Duration must be between 1 and 10,080 minutes."; return; }
            widget.Title = title.Text.Trim(); widget.Placeholder = placeholder.Text;
            if (increment != null) { widget.Increment = decimal.Parse(increment.Text); widget.StartingValue = decimal.Parse(starting!.Text); }
            if (minutes != null) { widget.TimerMinutes = int.Parse(minutes.Text); widget.ShowSeconds = seconds.IsChecked == true; widget.ShowElapsed = elapsed.IsChecked == true; }
            if (categories != null) { widget.Categories = categories.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); widget.Fields = fields!.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); }
            WidgetLibrary.Place(state, widget, c, r, span, h); Changed(); RenderWidgets(); if (SaveNow()) dialog.Close(); else error.Text = "Save failed. Your edits are retained; retry when storage is available.";
        };
        dialog.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDialog();
    }
    private void LayoutClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var option in new[] { "One column", "Two columns" }) { var item = new MenuItem { Header = option }; item.Click += (_, _) => { WidgetLibrary.Arrange(state.Widgets, option == "One column"); Changed(); ArrangeWidgetFrames(); SaveNow(); }; menu.Items.Add(item); }
        menu.PlacementTarget = (UIElement)sender; menu.IsOpen = true;
    }
    private void OpenLegacyPanel(Border panel, string title)
    {
        Detach(panel); var dialog = Dialog("Nexa · " + title, 520, 740); dialog.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        dialog.Closed += (_, _) => { Detach(panel); if (panel == ThemePanel) ((StackPanel)LegacyContent.Children.OfType<ScrollViewer>().Last().Content).Children.Add(panel); RenderWidgets(); }; dialog.ShowDialog();
    }
    private void ThemeClick(object sender, RoutedEventArgs e) => OpenLegacyPanel(ThemePanel, "Theme & display");
    private void DashboardSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = Dialog("Nexa · Dashboard settings", 480, 300); var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Dashboard name", Margin = new Thickness(0, 0, 0, 8) }); var name = new TextBox { Text = Continuity.Current(state.Workspace!, store.UserId).Name, MaxLength = 100 }; panel.Children.Add(name);
        var save = new Button { Content = "Save settings", Margin = new Thickness(0, 18, 0, 0) }; save.Click += (_, _) => { if (string.IsNullOrWhiteSpace(name.Text) || !SaveNow()) return; var candidate = CopyWorkspaceState().Workspace!; Continuity.Current(candidate, store.UserId).Name = name.Text.Trim(); if (CommitWorkspace(candidate)) dialog.Close(); }; panel.Children.Add(save); dialog.Content = panel; dialog.ShowDialog();
    }
    private void OverflowClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        void Add(string label, RoutedEventHandler action) { var item = new MenuItem { Header = label }; item.Click += action; menu.Items.Add(item); }
        Add("New dashboard", NewDashboardClick); Add("Import dashboard", ImportDashboardClick); Add("Export dashboard", ExportDashboardClick); Add("Dashboard settings", DashboardSettingsClick); Add("Theme & display", ThemeClick); Add("Compact mode", CompactClick);
        Add("Duplicate dashboard", (_, _) =>
        {
            if (!SaveNow()) return; var candidate = CopyWorkspaceState().Workspace!; var current = Continuity.Current(candidate, store.UserId);
            var duplicate = Continuity.NewDashboard(current.Name.Length < 95 ? current.Name + " copy" : "Dashboard copy", current.PresetOrigin, current.Current.Data, store.UserId);
            foreach (var w in duplicate.Current.Data.Widgets) w.Id = Guid.NewGuid(); candidate.Dashboards.Add(duplicate); candidate.ActiveDashboardId = duplicate.DashboardId; CommitWorkspace(candidate);
        });
        menu.PlacementTarget = (UIElement)sender; menu.IsOpen = true;
    }
    private void MaximizeClick(object sender, RoutedEventArgs e) => NexaChrome.ToggleMaximize(this);
    private void HomeClick(object sender, RoutedEventArgs e)
    {
        if (!SaveNow()) return;
        var home = new HomeWindow(store); home.Show(); Close();
        if (IsVisible) home.Close(); else Application.Current.MainWindow = home;
    }
}
