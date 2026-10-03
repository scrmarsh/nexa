using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

public partial class MainWindow
{
    private bool widgetsReady;
    internal bool editingDashboard;
    private readonly Dictionary<string, Border> legacyPanels = [];
    private readonly StackPanel parkedPanels = new();
    private readonly Dictionary<Guid, Border> widgetFrames = [];
    private readonly Dictionary<Guid, List<(Thumb Handle, ResizeEdge Edge)>> widgetResizeHandles = [];
    private readonly List<Action<DateTimeOffset>> widgetTicks = [];
    private readonly HashSet<Guid> expandedMicroWidgets = [];
    private Border? placementPreview;
    private const double MicroWidgetHeight = 46;
    private const double WidgetGap = 8;
    private Point? addWidgetLocation;
    private Brush DashboardBrush(string key) => (Brush)FullContent.FindResource("Dashboard" + key);

    private void InitializeWidgets()
    {
        legacyPanels.Add("pixels", PixelPanel); legacyPanels.Add("setup", SetupPanel); legacyPanels.Add("timeline", TimelinePanel);
        legacyPanels.Add("notes", NotesPanel); legacyPanels.Add("tasks", ChecklistPanel); legacyPanels.Add("hedy", HedyPanel);
        LegacyContent.Children.Add(parkedPanels); widgetsReady = true;
        WidgetScroller.SizeChanged += (_, _) => ArrangeWidgetFrames();
        PixelBannerHost.SizeChanged += (_, _) => ArrangeWidgetFrames();
        WidgetCanvas.MouseLeftButtonDown += (_, e) => { if (ReferenceEquals(e.OriginalSource, WidgetCanvas)) { addWidgetLocation = e.GetPosition(WidgetCanvas); e.Handled = true; AddWidgetClick(WidgetCanvas, new RoutedEventArgs()); } };
        WidgetScroller.ScrollChanged += (_, e) => { if (ReferenceEquals(e.OriginalSource, WidgetScroller) && (e.ViewportHeightChange != 0 || e.ViewportWidthChange != 0)) ArrangeWidgetFrames(); };
        var adaptedWidgets = WidgetLibrary.PrepareLayout(state);
        var migrated = !state.WidgetLayoutInitialized; WidgetLibrary.EnsureLayout(state);
        var removedSessionControls = state.Widgets.RemoveAll(w => w.Kind == "Session controls" || w.PanelKey == "setup") > 0;
        var expandedPixelTimers = WidgetLibrary.UpgradeGrid(state.Widgets);
        RenderWidgets();
        if (migrated || adaptedWidgets || removedSessionControls || expandedPixelTimers) { Changed(); SaveNow(); }
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
        if (WidgetLibrary.PrepareLayout(state)) Changed();
        foreach (var panel in legacyPanels.Values) { Detach(panel); parkedPanels.Children.Add(panel); }
        WidgetCanvas.Children.Clear(); widgetFrames.Clear(); widgetResizeHandles.Clear(); widgetTicks.Clear(); expandedMicroWidgets.Clear();
        EditToolbar.Visibility = editingDashboard ? Visibility.Visible : Visibility.Collapsed;
        PixelBannerHost.Content = null;
        var primaryPixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        PixelBannerHost.Visibility = primaryPixel == null ? Visibility.Collapsed : Visibility.Visible;
        if (primaryPixel != null) { Detach(PixelPanel); PixelPanel.Margin = new Thickness(0); PixelPanel.Padding = new Thickness(16); PixelPanel.BorderThickness = new Thickness(0); PixelBannerHost.Content = PixelPanel; }
        foreach (var widget in state.Widgets.Where(w => w != primaryPixel))
        {
            var frame = new Border { Background = DashboardBrush("PanelBrush"), BorderBrush = Brush(editingDashboard ? "AccentBrush" : "LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Tag = widget.Id, ClipToBounds = true };
            var layout = new StackPanel(); frame.Child = layout;
            var header = new DockPanel { Margin = new Thickness(12, 8, 10, 8) }; layout.Children.Add(header);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(buttons, Dock.Right); header.Children.Add(buttons);
            void Action(string glyph, string title, Action action)
            {
                var b = new Button { Content = glyph, ToolTip = title, Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(3, 0, 0, 0) };
                System.Windows.Automation.AutomationProperties.SetName(b, title + " " + widget.Title); b.Click += (_, _) => action(); buttons.Children.Add(b); b.Content = NexaIcons.Create(title);
            }
            Action("⚙", "Configure", () => ConfigureWidget(widget));
            Action("⧉", "Duplicate", () => DuplicateWidget(widget));
            Action("×", "Remove widget", () => RemoveWidget(widget));
            buttons.Opacity = editingDashboard ? 1 : 0;
            frame.MouseEnter += (_, _) => buttons.Opacity = 1;
            frame.MouseLeave += (_, _) => { if (!buttons.IsKeyboardFocusWithin) buttons.Opacity = editingDashboard ? 1 : 0; };
            buttons.GotKeyboardFocus += (_, _) => buttons.Opacity = 1;
            var handle = new Thumb { Width = 24, Height = 28, Cursor = Cursors.SizeAll, Template = HandleTemplate("⠿"), ToolTip = "Drag between or beside widgets to insert" };
            System.Windows.Automation.AutomationProperties.SetName(handle, "Move " + widget.Title); header.Children.Add(handle); WireDrag(handle, widget, false);
            if (WidgetLibrary.Definition(widget.Kind).CanMinimize) Action(widget.IsMicro ? "↗" : "−", widget.IsMicro ? "Keep widget expanded" : "Minimize widget to micro mode", () =>
            {
                widget.IsMicro = !widget.IsMicro;
                expandedMicroWidgets.Remove(widget.Id);
                Changed(); SaveNow(); RenderWidgets();
            });
            var titleEditor = new TextBox { Text = widget.Title, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), MaxLength = 100, ToolTip = "Edit widget title; drag the grip to move" };
            System.Windows.Automation.AutomationProperties.SetName(titleEditor, "Widget title " + widget.Title);
            titleEditor.TextChanged += (_, _) => { if (!string.IsNullOrWhiteSpace(titleEditor.Text)) { widget.Title = titleEditor.Text.Trim(); Changed(); } };
            header.Children.Add(titleEditor);
            UIElement content;
            if (widget.Kind == "Pixel timer" && state.Widgets.First(w => w.Kind == "Pixel timer").Id == widget.Id)
            { Detach(PixelPanel); PixelPanel.Margin = new Thickness(0); PixelPanel.Padding = new Thickness(14); PixelPanel.BorderThickness = new Thickness(0); PixelPanel.Background = DashboardBrush("PanelBrush"); content = PixelPanel; }
            else if (widget.PanelKey.Length > 0 && widget.PanelKey != "setup" && legacyPanels.TryGetValue(widget.PanelKey, out var legacy))
            {
                Detach(legacy); legacy.Margin = new Thickness(0); legacy.Padding = new Thickness(14); legacy.BorderThickness = new Thickness(0); legacy.Background = DashboardBrush("PanelBrush"); content = legacy;
            }
            else content = BuildWidgetContent(widget);
            var body = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Tag = "WidgetBody" };
            layout.Children.Add(body);
            if (widget.IsMicro)
            {
                var microView = new Border
                {
                    Background = DashboardBrush("PanelBrush"),
                    Child = new TextBlock { Text = "▣  " + widget.Title, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 8, 0), FontSize = 11, FontWeight = FontWeights.SemiBold },
                    ToolTip = "Hover to preview " + widget.Title
                };
                frame.Child = microView;
                frame.MouseEnter += (_, _) =>
                {
                    if (!expandedMicroWidgets.Add(widget.Id)) return;
                    frame.Child = layout;
                    ArrangeWidgetFrames();
                    Panel.SetZIndex(frame, 1000);
                };
                frame.MouseLeave += (_, _) =>
                {
                    if (!expandedMicroWidgets.Remove(widget.Id)) return;
                    frame.Child = microView;
                    ArrangeWidgetFrames();
                    Panel.SetZIndex(frame, 0);
                };
            }
            if (!widget.IsMicro)
            {
                var handles = new List<(Thumb, ResizeEdge)>();
                foreach (var edge in new[] { ResizeEdge.Left, ResizeEdge.Right, ResizeEdge.Top, ResizeEdge.Bottom,
                    ResizeEdge.Left | ResizeEdge.Top, ResizeEdge.Right | ResizeEdge.Top,
                    ResizeEdge.Left | ResizeEdge.Bottom, ResizeEdge.Right | ResizeEdge.Bottom })
                {
                    var horizontal = (edge & (ResizeEdge.Left | ResizeEdge.Right)) != 0;
                    var vertical = (edge & (ResizeEdge.Top | ResizeEdge.Bottom)) != 0;
                    var cursor = horizontal && vertical
                        ? (edge == (ResizeEdge.Left | ResizeEdge.Top) || edge == (ResizeEdge.Right | ResizeEdge.Bottom) ? Cursors.SizeNWSE : Cursors.SizeNESW)
                        : horizontal ? Cursors.SizeWE : Cursors.SizeNS;
                    var hitArea = new FrameworkElementFactory(typeof(Border));
                    hitArea.SetValue(Border.BackgroundProperty, Brushes.Transparent);
                    var resize = new Thumb { Cursor = cursor, Template = new ControlTemplate(typeof(Thumb)) { VisualTree = hitArea }, ToolTip = "Drag edge to resize widget" };
                    System.Windows.Automation.AutomationProperties.SetName(resize, edge == (ResizeEdge.Right | ResizeEdge.Bottom) ? "Resize " + widget.Title : "Resize " + edge + " " + widget.Title);
                    handles.Add((resize, edge)); WidgetCanvas.Children.Add(resize); Panel.SetZIndex(resize, 1001);
                    WireDrag(resize, widget, true, edge);
                }
                widgetResizeHandles[widget.Id] = handles;
            }
            WidgetCanvas.Children.Add(frame); widgetFrames.Add(widget.Id, frame);
        }
        var addWidget = new Button { Tag = "AddWidgetShortcut", Width = 88, Height = 88, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(16), Opacity = .28, ToolTip = "Add widget" };
        System.Windows.Automation.AutomationProperties.SetName(addWidget, "Add widget");
        addWidget.Content = NexaIcons.Create("Add widget");
        addWidget.MouseEnter += (_, _) => addWidget.Opacity = .8; addWidget.MouseLeave += (_, _) => addWidget.Opacity = .28;
        addWidget.Click += (_, e) => { addWidgetLocation = new Point(Canvas.GetLeft(addWidget), Canvas.GetTop(addWidget)); AddWidgetClick(addWidget, e); }; WidgetCanvas.Children.Add(addWidget);
        ArrangeWidgetFrames(); UpdateWidgetClocks(DateTimeOffset.Now);
    }
    private ControlTemplate HandleTemplate(string text)
    {
        var visual = new FrameworkElementFactory(typeof(TextBlock)); visual.SetValue(TextBlock.TextProperty, text); visual.SetValue(TextBlock.ForegroundProperty, DashboardBrush("MutedBrush")); visual.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); visual.SetValue(TextBlock.FontSizeProperty, 20.0);
        return new ControlTemplate(typeof(Thumb)) { VisualTree = visual };
    }
    private double RowUnit => 24;
    private double GridUnit => (Math.Max(1, WidgetCanvas.ActualWidth) + WidgetGap) / 20;
    private int MinimumWidthSteps(WidgetInstance widget) => Math.Clamp((int)Math.Ceiling((Math.Min(WidgetLibrary.Definition(widget.Kind).MinWidth, widget.Span <= 7 ? 240 : double.PositiveInfinity) + WidgetGap) / GridUnit), 1, 20);
    private int DisplaySpan(WidgetInstance widget) => WidgetCanvas.ActualWidth < 640 + WidgetGap ? 20 : widget.IsMicro && !expandedMicroWidgets.Contains(widget.Id) ? 4 : Math.Clamp(widget.Span, MinimumWidthSteps(widget), 20);
    private static double MinimumWidgetHeight(WidgetInstance widget) => WidgetLibrary.Definition(widget.Kind).MinHeight;
    private List<List<WidgetInstance>> WidgetRows()
    {
        var rows = new List<List<WidgetInstance>>();
        var used = 20;
        foreach (var widget in state.Widgets.Where(w => widgetFrames.ContainsKey(w.Id) && w.PlacementX == null).OrderBy(w => w.Row).ThenBy(w => w.Column))
        {
            var span = DisplaySpan(widget);
            if (used + span > 20) { rows.Add([]); used = 0; }
            rows[^1].Add(widget); used += span;
        }
        rows.AddRange(state.Widgets.Where(w => widgetFrames.ContainsKey(w.Id) && w.PlacementX != null).Select(w => new List<WidgetInstance> { w }));
        return rows;
    }
    private static void FitTextBoxHeight(TextBox textBox, double minHeight)
    {
        if (textBox.ActualWidth <= 0) return;
        var formatted = new FormattedText(
            string.IsNullOrEmpty(textBox.Text) ? " " : textBox.Text,
            CultureInfo.CurrentCulture,
            textBox.FlowDirection,
            new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
            textBox.FontSize,
            Brushes.Transparent,
            VisualTreeHelper.GetDpi(textBox).PixelsPerDip)
        {
            MaxTextWidth = Math.Max(1, textBox.ActualWidth - textBox.Padding.Left - textBox.Padding.Right - 14)
        };
        textBox.Height = Math.Max(minHeight, Math.Ceiling(formatted.Height + textBox.Padding.Top + textBox.Padding.Bottom + 14));
    }
    private void WidgetCanvasSizeChanged(object sender, SizeChangedEventArgs e) { if (widgetsReady && e.WidthChanged) ArrangeWidgetFrames(); }
    private void ArrangeWidgetFrames()
    {
        // Keep the coordinate system and captured Thumb stationary during a drag.
        if (placementPreview != null) return;
        if (WidgetScroller.ActualHeight <= 0 || WidgetCanvas.ActualWidth <= 0) return;
        var viewportHeight = Math.Max(1, (WidgetScroller.ViewportHeight > 0 ? WidgetScroller.ViewportHeight : WidgetScroller.ActualHeight) - (PixelBannerHost.Visibility == Visibility.Visible ? PixelBannerHost.ActualHeight + PixelBannerHost.Margin.Top + PixelBannerHost.Margin.Bottom : 0));
        if (editingDashboard)
        {
            var lines = new GeometryGroup();
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(GridUnit, 0)));
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, RowUnit)));
            var ink = DashboardBrush("LineBrush").Clone(); ink.Opacity = 0.45;
            WidgetCanvas.Background = new DrawingBrush(new GeometryDrawing(null, new Pen(ink, 1), lines))
            {
                TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute, ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, GridUnit, RowUnit), Viewbox = new Rect(0, 0, GridUnit, RowUnit), Stretch = Stretch.None
            };
        }
        else WidgetCanvas.Background = state.Background.Mode == SpaceBackgroundMode.Default ? DashboardBrush("BackgroundBrush") : Brushes.Transparent;
        // Wrap at the chosen width; retain readable heights and scroll extra rows.
        var rows = WidgetRows();
        var rowHeights = new List<double>();
        var widgetHeights = new Dictionary<Guid, double>();
        foreach (var tiles in rows)
        {
            var left = 0d;
            var preferredHeight = 1d;
            foreach (var widget in tiles)
            {
                if (!widgetFrames.TryGetValue(widget.Id, out var frame)) continue;
                Canvas.SetLeft(frame, left);
                frame.Width = Math.Max(1, DisplaySpan(widget) * GridUnit - WidgetGap);
                left += frame.Width + WidgetGap;
                if (frame.Child is UIElement layout)
                {
                    var stack = layout as StackPanel;
                    var body = stack?.Children.OfType<ScrollViewer>().FirstOrDefault();
                    if (body != null) { body.Height = double.NaN; if (body.Content is FrameworkElement content) content.MaxWidth = Math.Max(1, frame.Width - 2 - content.Margin.Left - content.Margin.Right); }
                    layout.Measure(new Size(Math.Max(1, frame.Width - 2), double.PositiveInfinity));
                    var naturalHeight = Math.Ceiling(layout.DesiredSize.Height + 2);
                    var minimum = MinimumWidgetHeight(widget);
                    var widgetHeight = widget.IsMicro && !expandedMicroWidgets.Contains(widget.Id) ? MicroWidgetHeight : widget.CustomSize ? Math.Max(minimum, widget.Height) : Math.Max(WidgetLibrary.Definition(widget.Kind).DefaultHeight, Math.Clamp(naturalHeight, minimum, Math.Max(minimum, 720)));
                    widgetHeights[widget.Id] = widgetHeight;
                    preferredHeight = Math.Max(preferredHeight, widgetHeight);
                }
            }
            rowHeights.Add(preferredHeight);
        }
        var top = 0d;
        for (var index = 0; index < rows.Count; index++)
        {
            var height = rowHeights[index];
            foreach (var widget in rows[index])
            {
                if (!widgetFrames.TryGetValue(widget.Id, out var frame)) continue;
                Canvas.SetTop(frame, top); frame.Height = widgetHeights[widget.Id];
                if (frame.Child is StackPanel stack && stack.Children.OfType<ScrollViewer>().FirstOrDefault() is { } body)
                    body.Height = Math.Max(0, frame.Height - stack.Children.OfType<FrameworkElement>().Where(c => c != body).Sum(c => c.DesiredSize.Height) - 2);
            }
            if (rows[index].Any(w => w.PlacementX == null)) top += height + (index < rows.Count - 1 ? WidgetGap : 0);
        }
        WidgetCanvas.Height = Math.Max(viewportHeight, top);
        foreach (var widget in state.Widgets.Where(w => widgetFrames.ContainsKey(w.Id) && w.PlacementX != null))
        {
            var frame = widgetFrames[widget.Id]; Canvas.SetLeft(frame, Math.Clamp(widget.PlacementX!.Value * WidgetCanvas.ActualWidth, 0, Math.Max(0, WidgetCanvas.ActualWidth - frame.Width))); Canvas.SetTop(frame, widget.PlacementY);
        }
        // Resolve only actual collisions in visual order, including responsive monitor/window changes.
        var occupied = new List<Rect>();
        foreach(var frame in widgetFrames.Values.OrderBy(Canvas.GetTop).ThenBy(Canvas.GetLeft))
        {
            var rect=FrameRect(frame);
            while(occupied.FirstOrDefault(r=>r.IntersectsWith(rect)) is var hit && hit.Width>0) rect.Y=Math.Ceiling((hit.Bottom+WidgetGap)/RowUnit)*RowUnit;
            Canvas.SetTop(frame,rect.Y); occupied.Add(rect);
        }
        WidgetCanvas.Height = Math.Max(WidgetCanvas.Height, widgetFrames.Values.Select(f => Canvas.GetTop(f) + f.Height).DefaultIfEmpty(0).Max());
        if (WidgetCanvas.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Tag, "AddWidgetShortcut")) is { } add)
        {
            Point? slot = null;
            for (var y = 0d; y + 96 <= WidgetCanvas.Height && slot == null; y += 24)
                for (var x = 0d; x + 96 <= WidgetCanvas.ActualWidth; x += 24)
                {
                    var area = new Rect(x, y, 96, 96);
                    if (widgetFrames.Values.All(f => !area.IntersectsWith(new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height)))) { slot = new Point(x, y); break; }
                }
            if (slot == null) { slot = new Point((WidgetCanvas.ActualWidth - 88) / 2, top + WidgetGap); WidgetCanvas.Height = Math.Max(WidgetCanvas.Height, top + 104); }
            Canvas.SetLeft(add, slot.Value.X); Canvas.SetTop(add, slot.Value.Y);
        }
        foreach (var (id, handles) in widgetResizeHandles)
        {
            var frame = widgetFrames[id];
            foreach (var (handle, edge) in handles)
            {
                var horizontal = (edge & (ResizeEdge.Left | ResizeEdge.Right)) != 0;
                var vertical = (edge & (ResizeEdge.Top | ResizeEdge.Bottom)) != 0;
                var corner = horizontal && vertical;
                handle.Width = corner ? Math.Min(12, frame.Width) : horizontal ? Math.Min(8, frame.Width) : Math.Max(1, frame.Width - 24);
                handle.Height = corner ? Math.Min(12, frame.Height) : vertical ? Math.Min(8, frame.Height) : Math.Max(1, frame.Height - 24);
                Canvas.SetLeft(handle, Canvas.GetLeft(frame) + (edge.HasFlag(ResizeEdge.Right) ? frame.Width - handle.Width : horizontal ? 0 : 12));
                Canvas.SetTop(handle, Canvas.GetTop(frame) + (edge.HasFlag(ResizeEdge.Bottom) ? frame.Height - handle.Height : vertical ? 0 : 12));
            }
        }
    }
    [Flags]
    private enum ResizeEdge { Left = 1, Right = 2, Top = 4, Bottom = 8 }
    private void WireDrag(Thumb thumb, WidgetInstance widget, bool resize, ResizeEdge edge = ResizeEdge.Right | ResizeEdge.Bottom)
    {
        Point anchor = new(); Rect origin = new(); double unit = 1, dx = 0, dy = 0;
        int span = 1, height = 288; double left = 0, top = 0;
        thumb.DragStarted += (_, _) =>
        {
            var frame = widgetFrames[widget.Id]; origin = FrameRect(frame); unit = GridUnit;
            anchor = Mouse.GetPosition(WidgetCanvas); dx = dy = 0; span = DisplaySpan(widget); height = (int)origin.Height; left = origin.Left; top = origin.Top;
            placementPreview = new Border { Background = new SolidColorBrush(Color.FromArgb(30,139,159,255)), BorderBrush = DashboardBrush("AccentBrush"), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(12), IsHitTestVisible = false };
            WidgetCanvas.Children.Add(placementPreview); Panel.SetZIndex(placementPreview, 2000);
            Canvas.SetLeft(placementPreview, left); Canvas.SetTop(placementPreview, top); placementPreview.Width = origin.Width; placementPreview.Height = origin.Height;
        };
        thumb.DragDelta += (_, e) =>
        {
            if (placementPreview == null) return;
            if (thumb.IsMouseCaptured) { var pointer = Mouse.GetPosition(WidgetCanvas); dx = pointer.X - anchor.X; dy = pointer.Y - anchor.Y; }
            else { dx = e.HorizontalChange; dy = e.VerticalChange; } // Routed input used by validation.
            var minSpan = MinimumWidthSteps(widget); var startSpan = Math.Clamp((int)Math.Round((origin.Width + WidgetGap) / unit), minSpan, 20);
            if (resize)
            {
                var right = Math.Min(20, (int)Math.Round(origin.Right / unit));
                var column = (int)Math.Round(origin.Left / unit);
                if (edge.HasFlag(ResizeEdge.Left)) { column = Math.Clamp((int)Math.Round((origin.Left + dx) / unit), 0, Math.Max(0,right-minSpan)); span = right-column; }
                else if (edge.HasFlag(ResizeEdge.Right)) span = Math.Clamp(startSpan + (int)Math.Round(dx/unit), minSpan, Math.Max(minSpan,20-column));
                left = column * unit;
                var minimum = (int)Math.Ceiling(MinimumWidgetHeight(widget)/RowUnit)*24;
                if (edge.HasFlag(ResizeEdge.Top)) { top = Math.Clamp(Math.Round((origin.Top+dy)/RowUnit)*RowUnit, Math.Max(0,origin.Bottom-1440), Math.Max(0,origin.Bottom-minimum)); height = (int)Math.Ceiling((origin.Bottom-top)/RowUnit)*24; }
                else if (edge.HasFlag(ResizeEdge.Bottom)) height = Math.Clamp((int)Math.Round((origin.Height+dy)/RowUnit)*24,minimum,1440);
            }
            else { left = Math.Clamp(Math.Round((origin.Left+dx)/unit)*unit,0,Math.Max(0,WidgetCanvas.ActualWidth-origin.Width)); top = Math.Max(0,Math.Round((origin.Top+dy)/RowUnit)*RowUnit); }
            var width = resize ? Math.Max(1, span*unit-WidgetGap) : origin.Width;
            var candidate = FreeWidgetPlacement(widget, left, top, width, height);
            Canvas.SetLeft(placementPreview,candidate.X); Canvas.SetTop(placementPreview,candidate.Y); placementPreview.Width=width; placementPreview.Height=height;
            // The captured grip stays stationary; the preview alone changes during input.
        };
        thumb.DragCompleted += (_, e) =>
        {
            if (placementPreview == null) return;
            WidgetCanvas.Children.Remove(placementPreview); placementPreview = null;
            if (!e.Canceled && (Math.Abs(dx)>3 || Math.Abs(dy)>3))
            {
                if (resize) { widget.Span=span; widget.Height=Math.Clamp(height,(int)MinimumWidgetHeight(widget),1440); widget.CustomSize=true; }
                CommitWidgetPlacement(widget,left,top,resize ? span*unit-WidgetGap : origin.Width,resize ? widget.Height : origin.Height);
            }
            ArrangeWidgetFrames();
        };
    }
    private static Rect FrameRect(Border frame) => new(Canvas.GetLeft(frame), Canvas.GetTop(frame), frame.Width, frame.Height);
    private void PixelBannerOptionsClick(object sender, RoutedEventArgs e)
    {
        var widget=state.Widgets.FirstOrDefault(w=>w.Kind=="Pixel timer"); if(widget==null) return;
        var menu=new ContextMenu();
        void Add(string label, Action action) { var item=new MenuItem { Header=label }; item.Click+=(_,_)=>action(); menu.Items.Add(item); }
        Add("Configure Pixel Timer",()=>ConfigureWidget(widget)); Add("Duplicate progress widget",()=>DuplicateWidget(widget)); Add("Remove Pixel Timer",()=>RemoveWidget(widget));
        menu.PlacementTarget=(UIElement)sender; menu.IsOpen=true;
    }
    private UIElement BuildWidgetContent(WidgetInstance widget)
    {
        var panel = new StackPanel { Margin = new Thickness(16, 6, 16, 16) };
        var editor = WidgetLibrary.Definition(widget.Kind).Editor;
        switch (editor)
        {
            case "externalTasks": case "calendar": panel.Children.Add(BuildIntegrationWidget(widget)); break;
            case "focus": var reset = new Button { Content = "Take 45 seconds to return to focus", Margin = new Thickness(0,12,0,0) }; reset.Click += FocusResetClick; panel.Children.Add(reset); break; case "bible": panel.Children.Add(BuildBible(widget)); break;
            case "scripture":
                var pins = new StackPanel(); BuildScripturePins(pins, widget);
                panel.Children.Add(new ScrollViewer { Content = pins, Height = Math.Max(190, widget.Height - 74), VerticalScrollBarVisibility = ScrollBarVisibility.Disabled }); break;
            case "text": case "links":
                var text = new TextBox { Text = widget.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 110, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, ToolTip = widget.Placeholder };
                System.Windows.Automation.AutomationProperties.SetName(text, widget.Title);
                text.Loaded += (_, _) => FitTextBoxHeight(text, 110);
                text.SizeChanged += (_, e) => { if (e.WidthChanged) FitTextBoxHeight(text, 110); };
                text.TextChanged += (_, _) => { widget.Text = text.Text; FitTextBoxHeight(text, 110); Changed(); Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames)); }; panel.Children.Add(text);
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
                    var check = new CheckBox { Content = new TextBlock { Text = item.Text, TextWrapping = TextWrapping.Wrap }, IsChecked = item.Done }; check.Click += (_, _) => { item.Done = check.IsChecked == true; if (item.Done) PlayInterfaceSound(); Changed(); }; row.Children.Add(check); panel.Children.Add(row);
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
            case "timer": case "stopwatch":
                BuildTimeWidget(panel, widget, editor == "stopwatch"); break;
            case "clock": case "progress":
                var label = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 26, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 8) }; panel.Children.Add(label);
                if (editor == "clock") label.MouseLeftButtonUp += (_, e) =>
                {
                    var menu = new ContextMenu();
                    var seconds = new MenuItem { Header = "Show seconds", IsCheckable = true, IsChecked = widget.ShowSeconds };
                    seconds.Click += (_, _) => { widget.ShowSeconds = seconds.IsChecked; Changed(); SaveNow(); UpdateClock(); };
                    var advanced = new MenuItem { Header = "More clock settings…" }; advanced.Click += (_, _) => ConfigureWidget(widget);
                    menu.Items.Add(seconds); menu.Items.Add(advanced); menu.PlacementTarget = label; menu.IsOpen = true; e.Handled = true;
                };
                AnalogClock? analog = null;
                if (editor == "clock")
                {
                    analog = new AnalogClock { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 6, 0, 0) }; panel.Children.Add(analog);
                    panel.SizeChanged += (_, e) => analog.Visibility = e.NewSize.Width >= 330 && e.NewSize.Height >= 210 ? Visibility.Visible : Visibility.Collapsed;
                }
                var progress = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Foreground = DashboardBrush("AccentBrush"), Background = DashboardBrush("LineBrush"), BorderThickness = new Thickness(0) };
                panel.Children.Add(progress);
                widgetTicks.Add(now =>
                {
                    if (editor == "clock") { label.Cursor = Cursors.Hand; label.ToolTip = "Click to configure clock"; label.Text = now.LocalDateTime.ToString(widget.ShowSeconds ? "h:mm:ss tt" : "h:mm tt"); analog?.SetTime(now.LocalDateTime); progress.Visibility = Visibility.Collapsed; return; }
                    var value = state.Session?.Progress(now) ?? 0; label.Text = value.ToString("P0"); progress.Value = value;
                });
                break;
            case "tracker":
                BuildTracker(panel, widget); break;
            case "flashcards":
                BuildFlashcards(panel, widget); break;
            case "statistics":
                var stats = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 18 };
                widgetTicks.Add(_ => { var records = state.Widgets.SelectMany(w => w.Records).ToList(); stats.Text = $"{records.Count} records\n\n" + string.Join("\n", records.SelectMany(r => r.Categories).GroupBy(c => c).Select(g => $"{g.Key}: {g.Count()}")); }); panel.Children.Add(stats); break;
            case "timeline":
                panel.Children.Add(new TextBlock { Text = state.Session == null ? "Start a Space session to build its timeline." : $"{state.Session.Title}\n{state.Session.Start.LocalDateTime:t} → {state.Session.End.LocalDateTime:t}", TextWrapping = TextWrapping.Wrap });
                if (state.Session is { } session)
                    foreach (var entry in session.IntervalNotes) panel.Children.Add(new TextBlock { Text = $"Interval {entry.Key + 1}: {entry.Value}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
                if (state.Widgets.FirstOrDefault(w => w.Kind == "Session timeline")?.Id == widget.Id)
                { Detach(HedySessionPanel); panel.Children.Add(HedySessionPanel); }
                break;
            case "hedy": case "setup":
                var buttonToPanel = new Button { Content = editor == "hedy" ? "Open Hedy connection" : "Open session settings" }; buttonToPanel.Click += (_, _) => OpenLegacyPanel(editor == "hedy" ? HedyPanel : SetupPanel, editor == "hedy" ? widget.Title : "Session settings"); panel.Children.Add(buttonToPanel); break;
        }
        return panel;
    }
    private void BuildTracker(StackPanel panel, WidgetInstance widget)
    {
        panel.Children.Add(new TextBlock { Text = $"{widget.Records.Count} records", FontSize = 24, Margin = new Thickness(0, 0, 0, 12) });
        var form = IsPreview ? new StackPanel() : panel;
        var name = new TextBox { ToolTip = "Student or record name" }; form.Children.Add(new TextBlock { Text = "Student / record name", Margin = new Thickness(0, 0, 0, 6) }); form.Children.Add(name);
        var categories = new WrapPanel { Margin = new Thickness(0, 10, 0, 10) };
        foreach (var category in widget.Categories) categories.Children.Add(new CheckBox { Content = category, Margin = new Thickness(0, 0, 12, 8) }); form.Children.Add(categories);
        var fields = new Dictionary<string, TextBox>();
        foreach (var field in widget.Fields.Distinct(StringComparer.OrdinalIgnoreCase)) { form.Children.Add(new TextBlock { Text = field, Margin = new Thickness(0, 8, 0, 4) }); var box = new TextBox(); fields.Add(field, box); form.Children.Add(box); }
        var add = new Button { Content = "Add record", Margin = new Thickness(0, 12, 0, 12) };
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text)) { name.Focus(); return; }
            widget.Records.Add(new() { Name = name.Text.Trim(), Categories = categories.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => (string)c.Content).ToList(), Values = fields.ToDictionary(p => p.Key, p => p.Value.Text) }); Changed(); SaveNow(); RenderWidgets();
        }; form.Children.Add(add);
        if (IsPreview)
        {
            var expander = new Expander { Header = "Add a record", Content = form, Margin = new Thickness(0, 0, 0, 12) };
            expander.Expanded += (_, _) => Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames));
            expander.Collapsed += (_, _) => Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames));
            panel.Children.Add(expander);
        }
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
        var location = addWidgetLocation; addWidgetLocation = null;
        var dialog = Dialog("Nexa · Add widget", 680, 720); var root = new DockPanel { Margin = new Thickness(24) };
        var search = new TextBox { ToolTip = "Search widgets", Margin = new Thickness(0, 0, 0, 16) }; System.Windows.Automation.AutomationProperties.SetName(search, "Search widgets"); DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);
        var list = new StackPanel(); root.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false });
        void Refresh()
        {
            list.Children.Clear();
            foreach (var group in WidgetLibrary.Definitions.Where(d => d.Name is not ("Session controls" or "Hedy") && (d.Name + " " + d.Group + " " + d.Description).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).GroupBy(d => d.Group))
            {
                list.Children.Add(new TextBlock { Text = group.Key, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 10) });
                foreach (var definition in group)
                {
                    var content = new StackPanel(); content.Children.Add(new TextBlock { Text = definition.Name, FontWeight = FontWeights.SemiBold }); content.Children.Add(new TextBlock { Text = definition.Description, TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 5, 0, 0) });
                    var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    var icon = new TextBlock { Text = WidgetIcon(definition.Editor), FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 22, Foreground = Brush("AccentBrush"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                    row.Children.Add(icon); Grid.SetColumn(content, 1); row.Children.Add(content);
                    var button = new Button { Content = "Add", Tag = definition.Name, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; System.Windows.Automation.AutomationProperties.SetName(button, "Add " + definition.Name); Grid.SetColumn(button, 2); row.Children.Add(button);
                    button.Click += (_, _) => { AddWidget(definition.Name, location); NexaChrome.FadeOutAndClose(dialog); }; list.Children.Add(new Border { Child = row, Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8) });
                }
            }
        }
        search.TextChanged += (_, _) => Refresh(); Refresh(); dialog.Content = root; dialog.ShowDismissible();
    }
    private static string WidgetIcon(string editor) => editor switch
    {
        "timer" or "stopwatch" => "\uE916", "clock" => "\uE823", "bible" or "scripture" => "\uE736",
        "counter" => "\uE8EF", "checklist" => "\uE9D5", "tracker" => "\uE80A",
        "links" => "\uE71B", "timeline" => "\uE81C", "progress" => "\uE9D9",
        _ => "\uE70B"
    };
    internal WidgetInstance AddWidget(string kind, Point? location = null)
    {
        if (state.Widgets.Count >= 200) throw new InvalidOperationException("A Space supports up to 200 widgets.");
        var panelKey = kind switch { "Pixel timer" => "pixels", "Session controls" => "setup", "Session timeline" => "timeline", "Hedy" => "hedy", _ => "" };
        if (state.Widgets.Any(w => w.PanelKey == panelKey)) panelKey = "";
        var widget = WidgetLibrary.Create(kind, panelKey); WidgetLibrary.Add(state, widget); PlayInterfaceSound();
        if (location is Point point) { widget.PlacementX = Math.Clamp(point.X / Math.Max(1, WidgetCanvas.ActualWidth), 0, 1); widget.PlacementY = Math.Max(0, point.Y); widget.Span = Math.Min(widget.Span, Math.Max(6, (int)Math.Floor((WidgetCanvas.ActualWidth - point.X + WidgetGap) / GridUnit))); }
        Changed(); RenderWidgets(); SaveNow(); return widget;
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
        var columns = Field("Relative width (1-20)", widget.Span.ToString());
        var height = Field($"Height (24-pixel steps, minimum {(int)Math.Ceiling(MinimumWidgetHeight(widget) / 24)})", Math.Clamp(widget.Height / 24, 1, 60).ToString());
        var autoHeight = new CheckBox { Content = "Fit height to content", IsChecked = !widget.CustomSize, Margin = new Thickness(0, 12, 0, 0) }; panel.Children.Add(autoHeight);
        height.TextChanged += (_, _) => autoHeight.IsChecked = false;
        void StepButtons(TextBox field)
        {
            var steps = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            foreach (var direction in new[] { -1, 1 })
            {
                var step = new Button { Content = direction < 0 ? "-" : "+", Width = 34, Margin = new Thickness(0, 0, 4, 0), ToolTip = direction < 0 ? "One grid step smaller" : "One grid step larger" };
                step.Click += (_, _) => { if (int.TryParse(field.Text, out var value)) field.Text = Math.Clamp(value + direction, ReferenceEquals(field, height) ? (int)Math.Ceiling(MinimumWidgetHeight(widget) / 24) : 1, ReferenceEquals(field, height) ? 60 : 20).ToString(); };
                steps.Children.Add(step);
            }
            panel.Children.Insert(panel.Children.IndexOf(field) + 1, steps);
        }
        StepButtons(columns); StepButtons(height);
        var left = Field("Column position (0-19)", widget.Column.ToString()); var row = Field("Row position (grid steps)", widget.Row.ToString());
        var type = WidgetLibrary.Definition(widget.Kind).Editor;
        TextBox? increment = null, starting = null, minutes = null, categories = null, fields = null;
        var seconds = new CheckBox { Content = "Show seconds", IsChecked = widget.ShowSeconds, Margin = new Thickness(0, 12, 0, 0) };
        var elapsed = new CheckBox { Content = "Show elapsed time", IsChecked = widget.ShowElapsed, Margin = new Thickness(0, 10, 0, 0) };
        if (type == "counter") { increment = Field("Increment", widget.Increment.ToString()); starting = Field("Starting value (used on reset)", widget.StartingValue.ToString()); }
        if (type == "timer") { panel.Children.Add(new TextBlock { Text = "Set hours, minutes and seconds directly on the Timer widget.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) }); panel.Children.Add(seconds); panel.Children.Add(elapsed); }
        if (type == "tracker") { categories = Field("Categories (one per line)", string.Join("\n", widget.Categories)); categories.AcceptsReturn = true; categories.Height = 100; fields = Field("Custom fields (one per line)", string.Join("\n", widget.Fields)); fields.AcceptsReturn = true; fields.Height = 100; }
        panel.Children.Add(new TextBlock { Text = "Widgets use sizes suited to their content. Extra rows scroll vertically; narrow windows stack widgets. Edit titles directly or drag the grip to move. Use EDIT beside TIME REMAINING to change session times.", TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 16, 0, 0) });
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(error);
        var save = new Button { Content = "Save settings", Margin = new Thickness(0, 18, 0, 0) }; panel.Children.Add(save);
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(title.Text) || !int.TryParse(columns.Text, out var span) || !int.TryParse(height.Text, out var h) || !int.TryParse(left.Text, out var c) || !int.TryParse(row.Text, out var r)) { error.Text = "Enter a title and valid whole-number dimensions."; return; }
            if (span is < 1 or > 20 || h < Math.Ceiling(MinimumWidgetHeight(widget) / 24) || h > 60 || c < 0 || c + span > 20 || r is < 0 or > 10000) { error.Text = "Use width from 1 to 20, a height at or above the widget minimum (up to 60), and a position inside the grid."; return; }
            if (increment != null && (!decimal.TryParse(increment.Text, out var step) || step == 0)) { error.Text = "Increment must be a nonzero number."; return; }
            if (starting != null && !decimal.TryParse(starting.Text, out _)) { error.Text = "Starting value must be a number."; return; }
            if (minutes != null && !InputRules.ValidMinutes(minutes.Text, out _)) { error.Text = "Duration must be between 1 and 10,080 minutes."; return; }
            widget.Title = title.Text.Trim(); widget.Placeholder = placeholder.Text;
            if (increment != null) { widget.Increment = decimal.Parse(increment.Text); widget.StartingValue = decimal.Parse(starting!.Text); }
            if (type == "timer") { widget.ShowSeconds = seconds.IsChecked == true; widget.ShowElapsed = elapsed.IsChecked == true; }
            if (categories != null) { widget.Categories = categories.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); widget.Fields = fields!.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList(); }
            WidgetLibrary.Place(state, widget, c, r, span, h * 24); if (autoHeight.IsChecked == true) { widget.CustomSize = false; widget.Height = WidgetLibrary.Create(widget.Kind).Height; } Changed(); RenderWidgets(); if (SaveNow()) NexaChrome.FadeOutAndClose(dialog); else error.Text = "Save failed. Your edits are retained; retry when storage is available.";
        };
        dialog.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDismissible();
    }
    private void LayoutClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        PopulateLayoutChoices(menu);
        menu.PlacementTarget = sender is MenuItem ? OverflowButton : (UIElement)sender; menu.IsOpen = true;
    }
    private void PopulateLayoutChoices(ItemsControl menu)
    {
        foreach (var option in new[] { "One column", "Two columns" }) { var item = new MenuItem { Header = option }; item.Click += (_, _) => { WidgetLibrary.Arrange(state.Widgets, option == "One column"); Changed(); ArrangeWidgetFrames(); SaveNow(); }; menu.Items.Add(item); }
        var threeColumns = new MenuItem { Header = "Three columns" };
        threeColumns.Click += (_, _) => { WidgetLibrary.ArrangeThreeColumns(state.Widgets); Changed(); ArrangeWidgetFrames(); SaveNow(); }; menu.Items.Add(threeColumns);
    }
    private void OpenLegacyPanel(Border panel, string title)
    {
        Detach(panel); var dialog = Dialog("Nexa · " + title, 520, 740); dialog.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        dialog.Closed += (_, _) => { Detach(panel); if (panel == ThemePanel) ((StackPanel)LegacyContent.Children.OfType<ScrollViewer>().Last().Content).Children.Add(panel); RenderWidgets(); }; dialog.ShowDismissible();
    }
    private void ThemeClick(object sender, RoutedEventArgs e) => OpenLegacyPanel(ThemePanel, "Theme & display");
    private void DashboardSettingsClick(object sender, RoutedEventArgs e) => OpenMainSettings("Session");
    private void RenameDashboardClick(object sender, RoutedEventArgs e)
    {
        var value = DashboardNameEditor.Text.Trim();
        if (string.IsNullOrWhiteSpace(value)) { Error("Give the Space a name before saving."); return; }
        if (!SaveNow()) return;
        var candidate = CopyWorkspaceState().Workspace!;
        Continuity.Current(candidate, store.UserId).Name = value;
        if (CommitWorkspace(candidate))
        {
            DashboardNameEditor.Text = value;
            DashboardPicker.SelectedItem = Continuity.Current(state.Workspace!, store.UserId);
        }
    }
    private void OverflowClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.ContextMenu is { IsOpen: true } openMenu) { openMenu.IsOpen = false; return; }
        var menu = new ContextMenu();
        void Add(string label, RoutedEventHandler action) { var item = new MenuItem { Header = label }; item.Click += action; menu.Items.Add(item); }
        var layout = new MenuItem { Header = "Layout" }; PopulateLayoutChoices(layout); menu.Items.Add(layout);
        Add("Reset Space", ResetDashboardClick); Add("New Space", NewDashboardClick); Add("Import Space", ImportDashboardClick); Add("Export Space", ExportDashboardClick); Add("Space settings", DashboardSettingsClick); Add("Theme & display", ThemeClick); Add("Compact mode", CompactClick);
        Add("Duplicate Space", (_, _) =>
        {
            if (!SaveNow()) return; var candidate = CopyWorkspaceState().Workspace!; var current = Continuity.Current(candidate, store.UserId);
            var duplicate = Continuity.NewDashboard(current.Name.Length < 95 ? current.Name + " copy" : "Space copy", current.PresetOrigin, current.Current.Data, store.UserId);
            foreach (var w in duplicate.Current.Data.Widgets) w.Id = Guid.NewGuid(); candidate.Dashboards.Add(duplicate); candidate.ActiveDashboardId = duplicate.DashboardId; CommitWorkspace(candidate);
        });
        button.ContextMenu = menu;
        menu.PlacementTarget = button;
        menu.Placement = PlacementMode.Bottom;
        menu.VerticalOffset = 6;
        menu.IsOpen = true;
    }
    private void MaximizeClick(object sender, RoutedEventArgs e) => NexaChrome.ToggleMaximize(this);
    private void HomeClick(object sender, RoutedEventArgs e)
    {
        if (!SaveNow()) return;
        var home = new HomeWindow(previewHomeStore ?? store); NexaChrome.TransferBounds(this, home); home.Show(); Application.Current.MainWindow = home;
        NexaChrome.FadeOutAndClose(this);
    }
}
