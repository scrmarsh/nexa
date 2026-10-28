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
    private readonly Dictionary<Guid,(WidgetInstance Widget,ScrollViewer Body)> deferredWidgetBodies=[];
    internal int DeferredWidgetBodies=>deferredWidgetBodies.Count;
    private bool loadingWidgetBodies,arrangingWidgets;
    private bool widgetsReady;
    internal bool editingDashboard;
    private readonly Dictionary<string, Border> legacyPanels = [];
    private readonly StackPanel parkedPanels = new();
    private readonly Dictionary<Guid, Border> widgetFrames = [];
    private readonly Dictionary<Guid, List<(Thumb Handle, ResizeEdge Edge)>> widgetResizeHandles = [];
    private readonly List<Action<DateTimeOffset>> widgetTicks = [];
    private readonly HashSet<Guid> expandedMicroWidgets = [];
    private readonly Dictionary<Guid, Action> collapseMicroWidgets = [];
    private Border? placementPreview;
    private bool widgetGestureActive;
    private bool widgetRenderDeferred;
    private const double MicroWidgetHeight = 46;
    private const double WidgetGap = 8;
    private double pixelResizeWidth;
    private double pixelResizeHeight;
    private SharedEdge? pixelSharedWidth;
    private SharedEdge? pixelSharedHeight;
    private int pixelSharedWidthSteps;
    private int pixelSharedHeightSteps;
    private int pixelInitialSpan;
    private int pixelInitialHeight;
    private Point? addWidgetLocation;
    private Guid? compactedPreviewId;
    private Brush DashboardBrush(string key) => (Brush)FullContent.FindResource("Dashboard" + key);

    private void InitializeWidgets()
    {
        legacyPanels.Add("pixels", PixelPanel); legacyPanels.Add("setup", SetupPanel); legacyPanels.Add("timeline", TimelinePanel);
        legacyPanels.Add("notes", NotesPanel); legacyPanels.Add("tasks", ChecklistPanel); legacyPanels.Add("hedy", HedyPanel);
        LegacyContent.Children.Add(parkedPanels); widgetsReady = true;
        WidgetScroller.SizeChanged += (_, _) => ArrangeWidgetFrames();
        stopwatchRefresh.Tick += (_, _) => { var now = DateTimeOffset.Now; foreach (var tick in stopwatchTicks) tick(now); };
        Loaded+=(_,_)=>LoadVisibleWidgetBodies();
        Closed += (_, _) => stopwatchRefresh.Stop();
        PixelBannerHost.SizeChanged += (_, _) => ArrangeWidgetFrames();
        PixelWidthHandle.DragStarted += (_, _) =>
        {
            pixelResizeWidth = PixelBannerContainer.Width;
            pixelInitialSpan = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer")?.BannerSpan ?? 13;
            pixelSharedWidth = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer") is { } pixel && PixelObstacle is { } bounds
                ? FindSharedEdge(pixel, bounds, ResizeEdge.Right) : null;
            pixelSharedWidthSteps = 0;
            if (pixelSharedWidth != null) widgetGestureActive = true;
        };
        PixelWidthHandle.DragCompleted += (_, e) =>
        {
            if (pixelSharedWidth is { } pair)
            {
                if (e.Canceled) ShowSharedResize(pair, 0);
                else if (pixelSharedWidthSteps != 0) CommitSharedResize(pair, pixelSharedWidthSteps);
                widgetGestureActive = false; pixelSharedWidth = null;
            }
            else if (e.Canceled)
            {
                if (state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer") is { } pixel) pixel.BannerSpan = pixelInitialSpan;
            }
            else { Changed(); SaveNow(); }
            ArrangeWidgetFrames();
        };
        PixelHeightHandle.DragStarted += (_, _) =>
        {
            pixelResizeHeight = PixelBannerContainer.Height;
            pixelInitialHeight = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer")?.BannerHeight ?? 360;
            pixelSharedHeight = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer") is { } pixel && PixelObstacle is { } bounds
                ? FindSharedEdge(pixel, bounds, ResizeEdge.Bottom) : null;
            pixelSharedHeightSteps = 0;
            if (pixelSharedHeight != null) widgetGestureActive = true;
        };
        PixelHeightHandle.DragCompleted += (_, e) =>
        {
            if (pixelSharedHeight is { } pair)
            {
                if (e.Canceled) ShowSharedResize(pair, 0);
                else if (pixelSharedHeightSteps != 0) CommitSharedResize(pair, pixelSharedHeightSteps);
                widgetGestureActive = false; pixelSharedHeight = null;
            }
            else if (e.Canceled)
            {
                if (state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer") is { } pixel) pixel.BannerHeight = pixelInitialHeight;
            }
            else { Changed(); SaveNow(); }
            ArrangeWidgetFrames();
        };
        WidgetCanvas.MouseLeftButtonDown += (_, e) => { if (ReferenceEquals(e.OriginalSource, WidgetCanvas)) { addWidgetLocation = e.GetPosition(WidgetCanvas); e.Handled = true; AddWidgetClick(WidgetCanvas, new RoutedEventArgs()); } };
        WidgetScroller.ScrollChanged += (_,e)=>{
            if(!ReferenceEquals(e.OriginalSource,WidgetScroller))return;
            if(e.ViewportHeightChange!=0 || e.ViewportWidthChange!=0)ArrangeWidgetFrames();
            LoadVisibleWidgetBodies();
        };
        var modelWatch=System.Diagnostics.Stopwatch.StartNew();
        var adaptedWidgets = WidgetLibrary.PrepareLayout(state);
        var migrated = !state.WidgetLayoutInitialized; WidgetLibrary.EnsureLayout(state);
        var removedSessionControls = state.Widgets.RemoveAll(w => w.Kind == "Session controls" || w.PanelKey == "setup") > 0;
        var expandedPixelTimers = WidgetLibrary.UpgradeGrid(state.Widgets);
        ConstructionProfile["Widget model preparation"]=modelWatch.Elapsed.TotalMilliseconds;
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
        if (widgetGestureActive) { widgetRenderDeferred = true; return; }
        var visualWatch=System.Diagnostics.Stopwatch.StartNew();
        ClearWidgetFocus(); stopwatchRefresh.Stop(); stopwatchTicks.Clear();
        if (WidgetLibrary.PrepareLayout(state)) Changed();
        foreach (var panel in legacyPanels.Values) { Detach(panel); parkedPanels.Children.Add(panel); }
        var existingWidgetIds=widgetFrames.Keys.ToHashSet();deferredWidgetBodies.Clear();
        WidgetCanvas.Children.Clear(); scoreAreas.Clear(); widgetFrames.Clear(); widgetResizeHandles.Clear(); widgetTicks.Clear(); expandedMicroWidgets.Clear(); collapseMicroWidgets.Clear();
        EditToolbar.Visibility = editingDashboard ? Visibility.Visible : Visibility.Collapsed;
        PixelBannerHost.Content = null;
        var primaryPixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        PixelBannerContainer.Visibility = primaryPixel == null ? Visibility.Collapsed : Visibility.Visible;
        if (primaryPixel != null)
        {
            Detach(PixelPanel);
            PixelPanel.Margin = new Thickness(0);
            PixelPanel.Padding = new Thickness(16);
            PixelPanel.Background = DashboardBrush("PanelBrush");
            PixelPanel.BorderBrush = DashboardBrush("LineBrush");
            PixelPanel.BorderThickness = new Thickness(1);
            PixelPanel.CornerRadius = new CornerRadius(12);
            PixelPanel.ClipToBounds = true;
            PixelBannerHost.Content = PixelPanel;
        }
        foreach (var widget in state.Widgets.Where(w => w != primaryPixel))
        {
            var frame = new Border { Background = DashboardBrush("PanelBrush"), BorderBrush = DashboardBrush("LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Tag = widget.Id, ClipToBounds = true };
            var layout = new StackPanel(); frame.Child = layout;
            var header = new DockPanel { Margin = new Thickness(12, 8, 10, 8) }; layout.Children.Add(header);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(buttons, Dock.Right); header.Children.Add(buttons);
            void Action(string glyph, string title, Action action)
            {
                var b = new Button { Content = glyph, ToolTip = title, Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(3, 0, 0, 0) };
                System.Windows.Automation.AutomationProperties.SetName(b, title + " " + widget.Title); b.Click += (_, _) => action(); buttons.Children.Add(b); b.Content = NexaIcons.Create(title);
            }
            if (WidgetActivity.HasHistory(widget)) Action("◷", "Notes / History", () => NotesHistoryClick(this, new RoutedEventArgs()));
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
                Changed(); RenderWidgets(); CompactWidgetLayout();
            });
            var titleEditor = new TextBox { Text = widget.Title, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Padding = new Thickness(0), MaxLength = 100, ToolTip = "Edit widget title; drag the grip to move" };
            System.Windows.Automation.AutomationProperties.SetName(titleEditor, "Widget title " + widget.Title);
            titleEditor.TextChanged += (_, _) => { if (!string.IsNullOrWhiteSpace(titleEditor.Text)) { widget.Title = titleEditor.Text.Trim(); Changed(); TutorialAction("renamed",widget.Id); } };
            titleEditor.ToolTip="Click to rename; double-click or right-click for widget options";
            titleEditor.MouseDoubleClick+=(_,e)=> { ConfigureWidget(widget); e.Handled=true; };
            titleEditor.KeyDown+=(_,e)=> { if(e.Key==Key.F2) { ConfigureWidget(widget); e.Handled=true; } };
            var options=new ContextMenu(); var edit=new MenuItem { Header="Edit widget options" }; edit.Click+=(_,_)=>ConfigureWidget(widget); options.Items.Add(edit);
            var focus = new MenuItem { Header = "Focus this widget" }; focus.Click += (_, _) => FocusWidget(widget.Id); options.Items.Add(focus);
            if (widget.Kind == "Counter")
            {
                var distraction = new MenuItem { Header = "Distraction mode", IsCheckable = true, IsChecked = widget.CounterMode == "Distraction" };
                distraction.Click += (_, _) => { widget.CounterMode = distraction.IsChecked ? "Distraction" : "Standard"; Changed(); SaveNow(); RenderWidgets(); };
                options.Items.Add(distraction);
            }
            var permanent = new MenuItem { Header = "Preserve content between sessions", IsCheckable = true, IsChecked = !widget.SessionScoped };
            permanent.Click += (_, _) => { widget.SessionScoped = !permanent.IsChecked; Changed(); };
            options.Items.Add(permanent);
            titleEditor.ContextMenu=options;
            header.Children.Add(titleEditor);
            UIElement content;var deferBody=false;
            if (widget.Kind == "Pixel timer" && state.Widgets.First(w => w.Kind == "Pixel timer").Id == widget.Id)
            { Detach(PixelPanel); PixelPanel.Margin = new Thickness(0); PixelPanel.Padding = new Thickness(14); PixelPanel.BorderThickness = new Thickness(0); PixelPanel.Background = DashboardBrush("PanelBrush"); content = PixelPanel; }
            else if (widget.PanelKey.Length > 0 && widget.PanelKey is not ("setup" or "timeline" or "hedy" or "notes" or "tasks") && legacyPanels.TryGetValue(widget.PanelKey, out var legacy))
            {
                Detach(legacy); legacy.Margin = new Thickness(0); legacy.Padding = new Thickness(14); legacy.BorderThickness = new Thickness(0); legacy.Background = DashboardBrush("PanelBrush"); content = legacy;
            }
            else if(state.Widgets.Count>12 && widget.Kind is ("Bible" or "Flashcards" or "Calculator" or "Distribution Tracker" or "Session timeline" or "Bugs Squashed"))
            {deferBody=true;content=new Border{Height=Math.Max(40,widget.Height-64),IsHitTestVisible=false};}
            else content = BuildWidgetContent(widget);
            var body = new ScrollViewer { Content = content, VerticalScrollBarVisibility = widget.Kind is "Water Bottle Counter" or "Alcoholic Beverage" or "Stopwatch" ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Tag = "WidgetBody" };
            layout.Children.Add(body);if(deferBody)deferredWidgetBodies[widget.Id]=(widget,body);
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
                    if (widgetGestureActive) return;
                    if (!expandedMicroWidgets.Add(widget.Id)) return;
                    frame.Child = layout;
                    ArrangeWidgetFrames();
                    Panel.SetZIndex(frame, 1000);
                };
                void CollapseMicroWidget()
                {
                    if (!expandedMicroWidgets.Remove(widget.Id)) return;
                    frame.Child = microView;
                    ArrangeWidgetFrames();
                    Panel.SetZIndex(frame, 0);
                }
                collapseMicroWidgets[widget.Id] = CollapseMicroWidget;
                frame.MouseLeave += (_, _) => { if (!widgetGestureActive) CollapseMicroWidget(); };
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
            if(IsLoaded && !existingWidgetIds.Contains(widget.Id))NexaMotion.Entrance(frame,Options);
        }
        ArrangeWidgetFrames(); UpdateWidgetClocks(DateTimeOffset.Now);
        ConstructionProfile["Widget visual creation"]=visualWatch.Elapsed.TotalMilliseconds;
    }
    internal void AnimateWidgetEntrance()=>NexaMotion.Stagger(widgetFrames.Values,Options);
    private ControlTemplate HandleTemplate(string text)
    {
        var visual = new FrameworkElementFactory(typeof(TextBlock)); visual.SetValue(TextBlock.TextProperty, text); visual.SetValue(TextBlock.ForegroundProperty, DashboardBrush("MutedBrush")); visual.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center); visual.SetValue(TextBlock.FontSizeProperty, 20.0);
        return new ControlTemplate(typeof(Thumb)) { VisualTree = visual };
    }
    private double RowUnit => 24;
    private double GridUnit => (Math.Max(1, WidgetCanvas.ActualWidth) + WidgetGap) / 20;
    private int MinimumWidthSteps(WidgetInstance widget) => Math.Clamp((int)Math.Ceiling((Math.Min(WidgetLibrary.MinimumWidth(widget), widget.Kind != "Stopwatch" && widget.Span <= 7 && widget.DisplayMode != "Expanded" ? 240 : double.PositiveInfinity) + WidgetGap) / GridUnit), 1, 20);
    private int DisplaySpan(WidgetInstance widget) => WidgetCanvas.ActualWidth < 640 + WidgetGap ? 20 : widget.IsMicro && !expandedMicroWidgets.Contains(widget.Id) ? 4 : Math.Clamp(widget.Span, MinimumWidthSteps(widget), 20);
    private static double MinimumWidgetHeight(WidgetInstance widget) => WidgetLibrary.MinimumHeight(widget);
    private void AddDisplayModeControls(StackPanel panel, WidgetInstance widget)
    {
        var modes = new WrapPanel { Margin = new Thickness(0, 6, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var mode in new[] { "Compact", "Expanded" })
        {
            var button = new Button { Content = mode, FontSize = 11, Padding = new Thickness(5, 2, 5, 2), Margin = new Thickness(0, 0, 5, 0), Opacity = widget.DisplayMode == mode ? 1 : .6 };
            button.Click += (_, _) =>
            {
                widget.DisplayMode = mode;
                widget.Height = mode == "Compact" ? WidgetLibrary.MinimumHeight(widget) :
                    Math.Max(widget.Height, widget.Kind == "Clock" ? 360 : WidgetLibrary.MinimumHeight(widget));
                widget.Span = mode == "Compact" ? MinimumWidthSteps(widget) : Math.Max(widget.Span, MinimumWidthSteps(widget));
                widget.CustomSize = true;
                Changed(); RenderWidgets(); CompactWidgetLayout();
            };
            modes.Children.Add(button);
        }
        panel.Children.Add(modes);
    }
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
    private void PixelBannerResizeDelta(object sender, DragDeltaEventArgs e)
    {
        var pixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        if (pixel == null || WidgetCanvas.ActualWidth <= 0) return;
        pixelResizeWidth += e.HorizontalChange;
        if (pixelSharedWidth is { } pair)
        {
            pixelSharedWidthSteps = SharedStep(pair, pixelResizeWidth - pair.FirstBounds.Width);
            ShowSharedResize(pair, pixelSharedWidthSteps);
            return;
        }
        pixel.BannerSpan = Math.Clamp((int)Math.Round((pixelResizeWidth + WidgetGap) / GridUnit), 9, 20);
        UpdatePixelBannerWidth(); ArrangeWidgetFrames();
    }
    private void PixelBannerResizeHeightDelta(object sender, DragDeltaEventArgs e)
    {
        var pixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        if (pixel == null) return;
        pixelResizeHeight += e.VerticalChange;
        if (pixelSharedHeight is { } pair)
        {
            pixelSharedHeightSteps = SharedStep(pair, pixelResizeHeight - pair.FirstBounds.Height);
            ShowSharedResize(pair, pixelSharedHeightSteps);
            return;
        }
        pixel.BannerHeight = Math.Clamp((int)Math.Round(pixelResizeHeight / RowUnit) * (int)RowUnit, 312, 960);
        UpdatePixelBannerHeight(); ArrangeWidgetFrames();
    }
    private void UpdatePixelBannerWidth()
    {
        var pixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        if (pixel == null || WidgetCanvas.ActualWidth <= 0) return;
        var minimumSpan = (int)Math.Ceiling((450 + WidgetGap) / GridUnit);
        PixelBannerContainer.Width = Math.Min(WidgetCanvas.ActualWidth, Math.Max(pixel.BannerSpan, minimumSpan) * GridUnit - WidgetGap);
    }
    private void UpdatePixelBannerHeight()
    {
        var pixel = state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer");
        if (pixel == null) return;
        PixelBannerContainer.Height = pixel.BannerHeight;
        PixelArea.Height = Math.Max(260, pixel.BannerHeight - 34);
    }
    private Rect? PixelObstacle => PixelBannerContainer.Visibility == Visibility.Visible && PixelBannerContainer.Height > 0
        ? new Rect(0, 0, PixelBannerContainer.Width, PixelBannerContainer.Height) : null;
    private void LoadVisibleWidgetBodies()
    {
        if(loadingWidgetBodies || !IsLoaded || WidgetScroller.ViewportHeight<=0)return;
        loadingWidgetBodies=true;var loaded=false;
        try
        {
            var top=Math.Max(0,WidgetScroller.VerticalOffset-100);var bottom=WidgetScroller.VerticalOffset+WidgetScroller.ViewportHeight+100;
            foreach(var (id,pending) in deferredWidgetBodies.ToArray())
            {
                if(!widgetFrames.TryGetValue(id,out var frame) || Canvas.GetTop(frame)>bottom || Canvas.GetTop(frame)+frame.Height<top)continue;
                deferredWidgetBodies.Remove(id);pending.Body.Content=BuildWidgetContent(pending.Widget);loaded=true;
            }
            if(loaded){UpdateWidgetClocks(DateTimeOffset.UtcNow);Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames),System.Windows.Threading.DispatcherPriority.Loaded);}
        }
        finally{loadingWidgetBodies=false;}
    }
    private void ArrangeWidgetFrames()
    {
        if(arrangingWidgets)return;arrangingWidgets=true;
        try{ArrangeWidgetFramesCore();}finally{arrangingWidgets=false;}
        LoadVisibleWidgetBodies();
    }
    private void ArrangeWidgetFramesCore()
    {
        // Layout must not fight a freely moving card or rebuild its captured grip.
        if (widgetGestureActive) return;
        if (WidgetScroller.ActualHeight <= 0 || WidgetCanvas.ActualWidth <= 0) return;
        UpdatePixelBannerWidth();
        UpdatePixelBannerHeight();
        var viewportHeight = Math.Max(1, WidgetScroller.ViewportHeight > 0 ? WidgetScroller.ViewportHeight : WidgetScroller.ActualHeight);
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
                FitWidgetBody(frame, widget);
            }
            if (rows[index].Any(w => w.PlacementX == null)) top += height + (index < rows.Count - 1 ? WidgetGap : 0);
        }
        WidgetCanvas.Height = Math.Max(Math.Max(viewportHeight, top), PixelObstacle?.Bottom ?? 0);
        foreach (var widget in state.Widgets.Where(w => widgetFrames.ContainsKey(w.Id) && w.PlacementX != null))
        {
            var frame = widgetFrames[widget.Id];
            var column = Math.Clamp((int)Math.Round(widget.PlacementX!.Value * WidgetCanvas.ActualWidth / GridUnit), 0, 20 - DisplaySpan(widget));
            Canvas.SetLeft(frame, column * GridUnit); Canvas.SetTop(frame, widget.PlacementY);
        }
        // Resolve only actual collisions in visual order, including responsive monitor/window changes.
        var occupied = new List<Rect>(); if (PixelObstacle is { } pixelBounds) occupied.Add(pixelBounds);
        foreach(var frame in widgetFrames.Values.OrderBy(Canvas.GetTop).ThenBy(Canvas.GetLeft))
        {
            var rect=FrameRect(frame);
            if (PixelObstacle is { } pixel && rect.Top < pixel.Bottom && rect.Bottom > pixel.Top)
            {
                var right = Math.Ceiling((pixel.Right + WidgetGap) / GridUnit - .001) * GridUnit;
                if (rect.Left <= right + GridUnit + .5 && right + rect.Width <= WidgetCanvas.ActualWidth + .5 &&
                    !occupied.Skip(1).Any(r => PlacementOverlaps(r, new Rect(right, rect.Y, rect.Width, rect.Height))))
                    rect.X = right;
            }
            // Keep intentional empty space; moving one card must not pull its neighbors along.
            while(occupied.FirstOrDefault(r=>PlacementOverlaps(r,rect)) is var hit && hit.Width>0) rect.Y=Math.Ceiling((hit.Bottom+WidgetGap)/RowUnit)*RowUnit;
            Canvas.SetLeft(frame,rect.X); Canvas.SetTop(frame,rect.Y); occupied.Add(rect);
        }
        WidgetCanvas.Height = Math.Max(WidgetCanvas.Height, widgetFrames.Values.Select(f => Canvas.GetTop(f) + f.Height).DefaultIfEmpty(0).Max());
        WidgetCanvas.Height = Math.Max(WidgetCanvas.Height, Math.Max(PixelObstacle?.Bottom ?? 0, widgetFrames.Values.Select(f => Canvas.GetTop(f) + f.Height).DefaultIfEmpty(0).Max()) + Math.Max(200, WidgetScroller.ViewportHeight));
        foreach (var id in widgetResizeHandles.Keys) PositionResizeHandles(id);
        RefreshSeasonalWorkspace();
        if(IsPreview && state.Workspace is {} workspace && compactedPreviewId!=workspace.ActiveDashboardId)
        {
            compactedPreviewId=workspace.ActiveDashboardId;
            var ordered=state.Widgets.Where(w=>widgetFrames.ContainsKey(w.Id)).OrderBy(w=>Canvas.GetTop(widgetFrames[w.Id])).ThenBy(w=>Canvas.GetLeft(widgetFrames[w.Id]));
            // Preview retains its authored positions as well.
        }
    }
    private void PositionResizeHandles(Guid id)
    {
        if (!widgetResizeHandles.TryGetValue(id, out var handles) || !widgetFrames.TryGetValue(id, out var frame)) return;
        foreach (var (handle, edge) in handles)
        {
            var horizontal = (edge & (ResizeEdge.Left | ResizeEdge.Right)) != 0;
            var vertical = (edge & (ResizeEdge.Top | ResizeEdge.Bottom)) != 0;
            var corner = horizontal && vertical;
            handle.Width = corner ? Math.Min(12, frame.Width) : horizontal ? 12 : Math.Max(1, frame.Width - 24);
            handle.Height = corner ? Math.Min(12, frame.Height) : vertical ? 12 : Math.Max(1, frame.Height - 24);
            Canvas.SetLeft(handle, Canvas.GetLeft(frame) + (edge.HasFlag(ResizeEdge.Right) ? frame.Width - (corner ? handle.Width : handle.Width / 2) : horizontal ? (corner ? 0 : -handle.Width / 2) : 12));
            Canvas.SetTop(handle, Canvas.GetTop(frame) + (edge.HasFlag(ResizeEdge.Bottom) ? frame.Height - (corner ? handle.Height : handle.Height / 2) : vertical ? (corner ? 0 : -handle.Height / 2) : 12));
        }
    }
    [Flags]
    private enum ResizeEdge { Left = 1, Right = 2, Top = 4, Bottom = 8 }
    private void WireDrag(Thumb thumb, WidgetInstance widget, bool resize, ResizeEdge edge = ResizeEdge.Right | ResizeEdge.Bottom)
    {
        Point anchor = new(); Rect origin = new(); double unit = 1, dx = 0, dy = 0;
        int span = 1, height = 288; double left = 0, top = 0;
        bool physicalInput = false;
        SharedEdge? shared = null; int sharedSteps = 0;
        Dictionary<Guid,Rect> resizeOrigin = [];
        System.Windows.Media.Effects.Effect? originalEffect = null;
        var edgeScroll = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        edgeScroll.Tick += (_, _) =>
        {
            if (!widgetGestureActive || !physicalInput || resize) { edgeScroll.Stop(); return; }
            var pointer = Mouse.GetPosition(WidgetScroller);
            var zone = Math.Min(70, WidgetScroller.ViewportHeight / 4);
            var speed = pointer.Y < zone ? -2 - 16 * Math.Clamp((zone - pointer.Y) / zone, 0, 1)
                : pointer.Y > WidgetScroller.ViewportHeight - zone ? 2 + 16 * Math.Clamp((pointer.Y - WidgetScroller.ViewportHeight + zone) / zone, 0, 1) : 0;
            if (speed == 0) return;
            var before = WidgetScroller.VerticalOffset;
            WidgetScroller.ScrollToVerticalOffset(Math.Clamp(before + speed, 0, WidgetScroller.ScrollableHeight));
            if (Math.Abs(WidgetScroller.VerticalOffset - before) < .01) return;
            var now = Mouse.GetPosition(WidgetCanvas); dx = now.X - anchor.X; dy = now.Y - anchor.Y;
            var frame = widgetFrames[widget.Id];
            frame.RenderTransform = new TranslateTransform(origin.X + dx - Canvas.GetLeft(frame), origin.Y + dy - Canvas.GetTop(frame));
            UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);
        };
        thumb.DragStarted += (_, _) =>
        {
            PinCurrentWidgetPositions();
            ClearWidgetFocus();
            var frame = widgetFrames[widget.Id];
            var visualPosition = frame.TranslatePoint(new Point(), WidgetCanvas);
            frame.RenderTransform = Transform.Identity;
            origin = FrameRect(frame); origin.Location = visualPosition; unit = GridUnit;
            anchor = Mouse.GetPosition(WidgetCanvas); dx = dy = 0; span = DisplaySpan(widget); height = (int)origin.Height; left = origin.Left; top = origin.Top;
            physicalInput = thumb.IsMouseCaptured; widgetGestureActive = true;
            if (physicalInput && !resize) edgeScroll.Start();
            resizeOrigin=widgetFrames.ToDictionary(p=>p.Key,p=>FrameRect(p.Value));
            if (resize)
            {
                shared = FindSharedEdge(widget, origin, edge); sharedSteps = 0;
                placementPreview = new Border { Background = new SolidColorBrush(Color.FromArgb(30,139,159,255)), BorderBrush = DashboardBrush("AccentBrush"), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(12), IsHitTestVisible = false };
                WidgetCanvas.Children.Add(placementPreview); Panel.SetZIndex(placementPreview, 2000);
                Canvas.SetLeft(placementPreview, left); Canvas.SetTop(placementPreview, top); placementPreview.Width = origin.Width; placementPreview.Height = origin.Height;
            }
            else
            {
                placementPreview = new Border { Background = new SolidColorBrush(Color.FromArgb(38, 91, 124, 255)), BorderBrush = DashboardBrush("AccentBrush"), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(10), IsHitTestVisible = false };
                WidgetCanvas.Children.Add(placementPreview); Panel.SetZIndex(placementPreview, 1900);
                UpdateWidgetDropPreview(widget, origin.X, origin.Y);
                frame.RenderTransform = new TranslateTransform(origin.X - Canvas.GetLeft(frame), origin.Y - Canvas.GetTop(frame));
                originalEffect = frame.Effect;
                frame.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 16, ShadowDepth = 3, Opacity = .24 };
                Panel.SetZIndex(frame, 2500);
                if (widgetResizeHandles.TryGetValue(widget.Id, out var handles)) foreach (var (handle, _) in handles) handle.IsHitTestVisible = false;
            }
        };
        thumb.DragDelta += (_, e) =>
        {
            if (!widgetGestureActive) return;
            if (physicalInput) { var pointer = Mouse.GetPosition(WidgetCanvas); dx = pointer.X - anchor.X; dy = pointer.Y - anchor.Y; }
            else { dx = e.HorizontalChange; dy = e.VerticalChange; } // Routed input used by validation.
            if (!resize)
            {
                var frame = widgetFrames[widget.Id];
                // Absolute canvas coordinates preserve the grab offset even inside a scrolled viewport.
                frame.RenderTransform = new TranslateTransform(origin.X + dx - Canvas.GetLeft(frame), origin.Y + dy - Canvas.GetTop(frame));
                UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);
                return;
            }
            ResizeAtPointer();
        };
        void ResizeAtPointer()
        {
            if (shared is { } pair)
            {
                sharedSteps = SharedStep(pair, pair.Horizontal ? dx : dy);
                ShowSharedResize(pair, sharedSteps);
                return;
            }
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
            var width = resize ? Math.Max(1, span*unit-WidgetGap) : origin.Width;
            var candidate = new Rect(left,top,width,height);
            if (state.Widgets.Where(w => w.Id != widget.Id && resizeOrigin.ContainsKey(w.Id))
                .Any(w => PlacementOverlaps(resizeOrigin[w.Id], candidate)) ||
                PixelObstacle is { } timer && PlacementOverlaps(timer, candidate))
            {
                candidate = origin;
                left = origin.X; top = origin.Y; width = origin.Width; height = (int)origin.Height;
                span = (int)Math.Round((origin.Width + WidgetGap) / unit);
            }
            ResizeWidgetVisual(widget,candidate,resizeOrigin);RefreshSeasonalWorkspace();
            Canvas.SetLeft(placementPreview!,candidate.X); Canvas.SetTop(placementPreview!,candidate.Y); placementPreview!.Width=width; placementPreview.Height=height;
        }
        thumb.DragCompleted += (_, e) =>
        {
            if (!widgetGestureActive) return;
            edgeScroll.Stop();
            if (physicalInput && !e.Canceled) { var pointer = Mouse.GetPosition(WidgetCanvas); dx = pointer.X - anchor.X; dy = pointer.Y - anchor.Y; }
            if (physicalInput && resize && !e.Canceled) ResizeAtPointer();
            var before = WidgetVisualPositions();
            var frame = widgetFrames[widget.Id]; frame.RenderTransform = Transform.Identity;
            if (placementPreview != null) { WidgetCanvas.Children.Remove(placementPreview); placementPreview = null; }
            ClearWidgetDropPreview();
            if (!resize)
            {
                frame.Effect = originalEffect; Panel.SetZIndex(frame, 0);
                if (widgetResizeHandles.TryGetValue(widget.Id, out var handles)) foreach (var (handle, _) in handles) handle.IsHitTestVisible = true;
            }
            widgetGestureActive = false;
            if (widget.IsMicro && !frame.IsMouseOver && collapseMicroWidgets.TryGetValue(widget.Id, out var collapse)) collapse();
            if (shared is { } pair)
            {
                if (e.Canceled) ShowSharedResize(pair, 0);
                else if (sharedSteps != 0) CommitSharedResize(pair, sharedSteps);
                shared = null;
                if (widgetRenderDeferred) { widgetRenderDeferred = false; RenderWidgets(); } else ArrangeWidgetFrames();
                WidgetCanvas.UpdateLayout(); AnimateWidgetSettle(before);
                return;
            }
            if (!e.Canceled && (Math.Abs(dx)>3 || Math.Abs(dy)>3))
            {
                if (resize)
                {
                    widget.Span=span; widget.Height=Math.Clamp(height,(int)MinimumWidgetHeight(widget),1440); widget.CustomSize=true;
                    CommitWidgetDrop(widget,left,top,preserveAnchor:true);
                }
                else CommitWidgetDrop(widget, origin.X + dx, origin.Y + dy);
            }
            if (widgetRenderDeferred) { widgetRenderDeferred = false; RenderWidgets(); } else ArrangeWidgetFrames();
            // A smaller card leaves its freed space available for a later placement.
            WidgetCanvas.UpdateLayout();
            AnimateWidgetSettle(before);TutorialAction("gesture",widget.Id);
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
        if (editor == "clock") { panel.VerticalAlignment = VerticalAlignment.Center; panel.Margin = new Thickness(8, 2, 8, 2); }
        switch (editor)
        {
            case "squashes":
                BuildSquashesWidget(panel); break;
            case "productivity": BuildProductivityWidget(panel, widget, true); break;
            case "score": BuildProductivityWidget(panel, widget, true); break;
            case "calculator": BuildCalculator(panel, widget); break;
            case "recorder": BuildRecorderWidget(panel); break;
            case "externalTasks": case "calendar": panel.Children.Add(BuildIntegrationWidget(widget)); break;
            case "focus": var reset = new Button { Content = "Take 45 seconds to return to focus", Margin = new Thickness(0,12,0,0) }; reset.Click += FocusResetClick; panel.Children.Add(reset); break; case "bible": panel.Children.Add(BuildBible(widget)); break;
            case "scripture":
                var pins = new StackPanel(); BuildScripturePins(pins, widget);
                panel.Children.Add(new ScrollViewer { Content = pins, Height = Math.Max(190, widget.Height - 74), VerticalScrollBarVisibility = ScrollBarVisibility.Disabled }); break;
            case "text": case "links":
                var notesSurface = widget.Kind == "Notes";
                var text = new TextBox { Text = widget.Text, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 110, VerticalScrollBarVisibility = notesSurface ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled, ToolTip = widget.Placeholder };
                System.Windows.Automation.AutomationProperties.SetName(text, widget.Title);
                if (notesSurface)
                {
                    text.FontSize = 16; text.SetValue(TextBlock.LineHeightProperty, 28.0); text.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight);
                    text.Padding = new Thickness(12, 4, 12, 4);
                    var rule = DashboardBrush("LineBrush").Clone(); rule.Opacity = .45;
                    var paper = new DrawingGroup();
                    paper.Children.Add(new GeometryDrawing(DashboardBrush("PanelBrush"), null, new RectangleGeometry(new Rect(0, 0, 256, 28))));
                    paper.Children.Add(new GeometryDrawing(null, new Pen(rule, 1), new LineGeometry(new Point(0, 27.5), new Point(256, 27.5))));
                    text.Background = new DrawingBrush(paper) { TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute, ViewboxUnits = BrushMappingMode.Absolute, Viewport = new Rect(0, 0, 256, 28), Viewbox = new Rect(0, 0, 256, 28), Stretch = Stretch.None };
                    panel.Margin = new Thickness(6, 2, 6, 4);
                }
                else { text.Loaded += (_, _) => FitTextBoxHeight(text, 110); text.SizeChanged += (_, e) => { if (e.WidthChanged) FitTextBoxHeight(text, 110); }; }
                text.TextChanged += (_, _) => { widget.Text = text.Text; if (notesSurface && widget.PanelKey == "notes") state.Notes = text.Text; if (!notesSurface) FitTextBoxHeight(text, 110); Changed(); if (!notesSurface) Dispatcher.BeginInvoke(new Action(ArrangeWidgetFrames)); }; panel.Children.Add(text);
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
                    void MoveItem(int offset)
                    {
                        var current = widget.Items.IndexOf(item); var target = current + offset;
                        if (current < 0 || target < 0 || target >= widget.Items.Count) return;
                        widget.Items.RemoveAt(current); widget.Items.Insert(target, item); Changed(); RenderWidgets();
                    }
                    var down = new Button { Content = "↓", Padding = new Thickness(5, 2, 5, 2), ToolTip = "Move item down" };
                    down.Click += (_, _) => MoveItem(1); DockPanel.SetDock(down, Dock.Right); row.Children.Add(down);
                    var up = new Button { Content = "↑", Padding = new Thickness(5, 2, 5, 2), ToolTip = "Move item up" };
                    up.Click += (_, _) => MoveItem(-1); DockPanel.SetDock(up, Dock.Right); row.Children.Add(up);
                    var check = new CheckBox { Content = new TextBlock { Text = item.Text, TextWrapping = TextWrapping.Wrap }, IsChecked = item.Done }; AttachTaskPermanence(check, item); check.Click += (_, _) => { item.Done = check.IsChecked == true; if (item.Done) PlayInterfaceSound(); Changed(); }; row.Children.Add(check); panel.Children.Add(row);
                }
                break;
            case "waterBottles":
                BuildWaterBottleCounter(panel, widget); break;
            case "beer":
                BuildBeerCounter(panel, widget); break;
            case "counter":
                var number = new TextBlock { Text = widget.Value.ToString(CultureInfo.CurrentCulture), FontSize = 44, Margin = new Thickness(0, 8, 0, 16) }; panel.Children.Add(number);
                var counts = new UniformGrid { Columns = 2 };
                foreach (var direction in new[] { -1, 1 })
                {
                    var button = new Button { Content = direction == 1 ? "+ " + widget.Increment : "− " + widget.Increment, Margin = new Thickness(3) };
                    button.Click += (_, _) => { try { widget.Value += direction * widget.Increment; number.Text = widget.Value.ToString(CultureInfo.CurrentCulture); Changed(); } catch (OverflowException) { Error("Counter limit reached."); } }; counts.Children.Add(button);
                }
                panel.Children.Add(counts);
                var resetCounter = new Button { Content = "Reset", Margin = new Thickness(3, 8, 3, 0), HorizontalAlignment = HorizontalAlignment.Right };
                resetCounter.Click += (_, _) => { widget.Value = widget.StartingValue; number.Text = widget.Value.ToString(CultureInfo.CurrentCulture); Changed(); };
                panel.Children.Add(resetCounter);
                if (widget.CounterMode == "Distraction") AddDistractionMessage(panel, widget);
                break;
            case "timer": case "stopwatch":
                BuildTimeWidget(panel, widget, editor == "stopwatch"); break;
            case "clock": case "progress":
                var label = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 26, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8), TextTrimming = TextTrimming.CharacterEllipsis }; panel.Children.Add(label);
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
                    AddDisplayModeControls(panel, widget);
                    panel.SizeChanged += (_, e) => { label.FontSize = e.NewSize.Width < 230 ? 20 : e.NewSize.Width < 320 ? 24 : 30; analog.Visibility = widget.DisplayMode == "Compact" ? Visibility.Collapsed : widget.DisplayMode == "Expanded" || e.NewSize.Width >= 330 && e.NewSize.Height >= 210 ? Visibility.Visible : Visibility.Collapsed; };
                }
                var progress = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Foreground = DashboardBrush("AccentBrush"), Background = DashboardBrush("LineBrush"), BorderThickness = new Thickness(0) };
                panel.Children.Add(progress);
                widgetTicks.Add(now =>
                {
                    if (editor == "clock") { label.Cursor = Cursors.Hand; label.ToolTip = "Click to configure clock"; label.Text = NexaTime.Local(now, Options.TimeZoneId).ToString(Options.Use24HourClock ? widget.ShowSeconds ? "HH:mm:ss" : "HH:mm" : widget.ShowSeconds ? "h:mm:ss tt" : "h:mm tt"); analog?.SetTime(NexaTime.Local(now, Options.TimeZoneId).DateTime); progress.Visibility = Visibility.Collapsed; return; }
                    var reading=PixelTiming.ForWidget(widget,state.Session,now); var value=reading.VisualFraction; label.Text=NexaTime.Percentage(value); label.ToolTip=reading.Caption; progress.Value=value;
                });
                break;
            case "tracker":
                BuildTracker(panel, widget); break;
            case "distribution":
                BuildDistributionTracker(panel, widget); break;
            case "flashcards":
                BuildFlashcards(panel, widget); break;
            case "statistics":
                var stats = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 18 };
                widgetTicks.Add(_ => { var records = state.Widgets.SelectMany(w => w.Records).ToList(); stats.Text = $"{records.Count} records\n\n" + string.Join("\n", records.SelectMany(r => r.Categories).GroupBy(c => c).Select(g => $"{g.Key}: {g.Count()}")); }); panel.Children.Add(stats); break;
            case "timeline":
                BuildSessionTimeline(panel, widget);
                break;
            case "hedy": BuildHedyNotes(panel, widget); break;
            case "setup": var buttonToPanel = new Button { Content = "Open session settings" }; buttonToPanel.Click += (_, _) => OpenMainSettings("Session"); panel.Children.Add(buttonToPanel); break;
        }
        return panel;
    }
    private void BuildWaterBottleCounter(StackPanel panel, WidgetInstance widget)
    {
        BuildDrinkCounter(panel, widget, false);
    }
    private void BuildBeerCounter(StackPanel panel, WidgetInstance widget) => BuildDrinkCounter(panel, widget, true);
    private void BuildDrinkCounter(StackPanel panel, WidgetInstance widget, bool beer)
    {
        var goal = Math.Clamp(beer ? widget.BeerGoal : widget.WaterBottleGoal, beer ? 1 : 4, 100);
        var visualHeight = widget.DisplayMode == "Compact" ? 150 : widget.DisplayMode == "Expanded" ? 260 : 210;
        var art = new DrinkArtwork(beer) { Vessel = widget.BeverageVessel, Height = visualHeight, HorizontalAlignment = HorizontalAlignment.Stretch };
        var visual = new Grid(); visual.Children.Add(art); panel.Children.Add(visual);
        var count = new TextBlock { FontSize = 21, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 6) };
        panel.Children.Add(count);
        var warning = new TextBlock { Text = "Slow down—consider drinking less.", FontSize = 11, Foreground = Brush("MutedBrush"), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 4) };
        panel.Children.Add(warning);
        var reminderText = new TextBlock { Text = "Take a sip of water.", TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
        var reminderContent = new DockPanel();
        var dismiss = new Button { Content = "×", Width = 28, Height = 28, Padding = new Thickness(2), Margin = new Thickness(6,0,0,0), ToolTip = "Dismiss water reminder" };
        System.Windows.Automation.AutomationProperties.SetName(dismiss,"Dismiss water reminder");
        DockPanel.SetDock(dismiss,Dock.Right); reminderContent.Children.Add(dismiss); reminderContent.Children.Add(reminderText);
        var reminder = new Border { Child = reminderContent, Background = DashboardBrush("PanelBrush"), BorderBrush = DashboardBrush("LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(8), VerticalAlignment = VerticalAlignment.Bottom, Visibility = Visibility.Collapsed };
        visual.Children.Add(reminder);
        DateTimeOffset? reminderUntil = null;
        dismiss.Click += (_,_) => { reminderUntil = null; reminder.Visibility = Visibility.Collapsed; };
        widgetTicks.Add(now => { if(reminderUntil is { } until && now >= until) { reminderUntil = null; reminder.Visibility = Visibility.Collapsed; } });
        void Refresh()
        {
            count.Text = $"{widget.Value:0.##} / {goal} {(beer ? "drinks" : "bottles")}";
            art.SetFill(Math.Clamp((double)widget.Value / goal, 0, 1));
            warning.Visibility = beer && goal > 4 ? Visibility.Visible : Visibility.Collapsed;
        }
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        Button Small(string text, string tooltip)
        {
            var button = new Button { Content = text, ToolTip = tooltip, MinWidth = 30, MinHeight = 28, FontSize = 11, Padding = new Thickness(7,3,7,3), Margin = new Thickness(3,0,3,0) };
            System.Windows.Automation.AutomationProperties.SetName(button,tooltip); return button;
        }
        var remove = Small(beer ? "− 1 beer" : "− 1 bottle",beer ? "Remove one beer" : "Remove one bottle");
        var add = Small(beer ? "+ 1 drink" : "+ 1 bottle",beer ? "Log one drink" : "Log one bottle");
        void Change(int delta)
        {
            var previous = widget.Value;
            widget.Value = Math.Clamp(Math.Floor(widget.Value) + delta, 0, 1000000);
            if (beer && delta > 0 && widget.Value > previous)
            {
                widget.BeerLoggedSinceReminder++;
                if(widget.BeerLoggedSinceReminder >= 2)
                {
                    widget.BeerLoggedSinceReminder = 0; reminderUntil = DateTimeOffset.Now.AddSeconds(8);
                    reminder.Visibility = Visibility.Visible;
                }
            }
            Refresh(); Changed();
        }
        remove.Click += (_, _) => Change(-1); add.Click += (_, _) => Change(1);
        actions.Children.Add(remove); actions.Children.Add(add); panel.Children.Add(actions);
        if(beer)
        {
            remove.Content="− 1 drink"; remove.ToolTip="Remove one drink";
            System.Windows.Automation.AutomationProperties.SetName(remove,"Remove one drink");
            var vessels=new ComboBox { ItemsSource=new[] { "Wine Glass", "Shot Glass", "Beer Bottle" }, SelectedItem=widget.BeverageVessel,
                FontSize=11, Margin=new Thickness(12,6,12,0), ToolTip="Beverage vessel", MinHeight=28 };
            System.Windows.Automation.AutomationProperties.SetName(vessels,"Beverage vessel");
            vessels.SelectionChanged+=(_,_)=> { if(vessels.SelectedItem is string selected) { widget.BeverageVessel=selected; art.Vessel=selected; art.InvalidateVisual(); Changed(); } };
            panel.Children.Add(vessels);
        }
        var options = new WrapPanel { Margin = new Thickness(0,6,0,0) };
        options.Children.Add(new TextBlock { Text = "Goal", FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,6,0) });
        var goalValue = new TextBlock { Text = goal.ToString(), MinWidth = 22, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontSize = 11 };
        void ChangeGoal(int delta)
        {
            goal = Math.Clamp(goal + delta, beer ? 1 : 4, 100);
            if (beer) widget.BeerGoal = goal; else widget.WaterBottleGoal = goal;
            goalValue.Text = goal.ToString(); Refresh(); Changed(); ArrangeWidgetFrames();
        }
        var less = Small("−", "Decrease goal"); var more = Small("+", "Increase goal");
        less.Click += (_, _) => ChangeGoal(-1); more.Click += (_, _) => ChangeGoal(1);
        options.Children.Add(less); options.Children.Add(goalValue); options.Children.Add(more);
        var reset = Small("Reset count", "Reset count");
        reset.Click += (_, _) => { widget.Value = 0; widget.BeerLoggedSinceReminder = 0; reminderUntil = null; reminder.Visibility = Visibility.Collapsed; Refresh(); Changed(); };
        options.Children.Add(reset);
        var size = Small(widget.DisplayMode == "Compact" ? "Expand" : "Compact", "Change counter size");
        size.Click += (_, _) =>
        {
            widget.DisplayMode = widget.DisplayMode == "Compact" ? "Expanded" : "Compact";
            widget.CustomSize = false; Changed(); RenderWidgets(); SaveNow();
        };
        options.Children.Add(size);
        var settings = new Expander { Header = "Options", Content = options, FontSize = 11, Foreground = Brush("MutedBrush"), Margin = new Thickness(0,6,0,0) };
        settings.Expanded += (_,_) => ArrangeWidgetFrames(); settings.Collapsed += (_,_) => ArrangeWidgetFrames();
        panel.Children.Add(settings); Refresh();
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
    private void UpdateWidgetClocks(DateTimeOffset now) { foreach (var tick in widgetTicks) tick(displayedClockInstant==default?now:displayedClockInstant); if (state.Widgets.Any(w => w.StopwatchStartedAt != null)) stopwatchRefresh.Start(); else stopwatchRefresh.Stop(); }
    private void OpenWeb(string text)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { Error("Enter a valid https:// website address."); return; }
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); } catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { Error("Could not open your browser."); }
    }
    private void EditDashboardClick(object sender, RoutedEventArgs e) { editingDashboard = true; RenderWidgets(); }
    private void DoneEditingClick(object sender, RoutedEventArgs e) { if (!SaveNow()) return; editingDashboard = false; RenderWidgets(); }
    private void AddWidgetClick(object sender, RoutedEventArgs e)
    {
        if (NexaPanels.DismissTopmost(this)) { addWidgetLocation = null; e.Handled = true; return; }
        var location = addWidgetLocation; addWidgetLocation = null;
        var dialog = Dialog("Nexa · Add widget", 680, 720); var root = new DockPanel { Margin = new Thickness(24) };
        var heading = new TextBlock { Text = "Add Widgets", FontSize = 26, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 16) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var search = new TextBox { ToolTip = "Search widgets", Margin = new Thickness(0, 0, 0, 16) }; System.Windows.Automation.AutomationProperties.SetName(search, "Search widgets"); DockPanel.SetDock(search, Dock.Top); root.Children.Add(search);
        var list = new StackPanel(); root.Children.Add(new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false });
        void Refresh()
        {
            list.Children.Clear();
            var matches = WidgetLibrary.Definitions.Where(d => d.Name != "Session controls" && d.Name != "Bugs Squashed" && (d.Name + " " + d.Group + " " + d.Description).Contains(search.Text, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var recommended = new[] { "Session timeline", "Clock", "Notes" }.Select(name => matches.FirstOrDefault(d => d.Name == name)).OfType<WidgetDefinition>().ToArray();
            var groups = new[] { new { Name = "Recommended", Items = recommended } }.Concat(matches.GroupBy(d => d.Group).Select(g => new { Name = g.Key, Items = g.ToArray() }));
            foreach (var group in groups.Where(g => g.Items.Length > 0))
            {
                list.Children.Add(new TextBlock { Text = group.Name, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 10) });
                foreach (var definition in group.Items)
                {
                    var content = new StackPanel(); content.Children.Add(new TextBlock { Text = definition.Name == "Session timeline" ? "Session Timeline" : definition.Name, FontWeight = FontWeights.SemiBold }); content.Children.Add(new TextBlock { Text = definition.Description, TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 5, 0, 0) });
                    var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    FrameworkElement icon = definition.Editor is "waterBottles" or "beer"
                        ? new DrinkArtwork(definition.Editor == "beer", false) { Width = 26, Height = 34, FillFraction = .65, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left }
                        : new TextBlock { Text = WidgetIcon(definition.Editor), FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 22, Foreground = Brush("AccentBrush"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
                    row.Children.Add(icon); Grid.SetColumn(content, 1); row.Children.Add(content);
                    var button = new Button { Content = "Add", Tag = definition.Name, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }; System.Windows.Automation.AutomationProperties.SetName(button, "Add " + definition.Name); Grid.SetColumn(button, 2); row.Children.Add(button);
                    button.Click += (_, _) => { AddWidget(definition.Name, location); NexaChrome.FadeOutAndClose(dialog); }; list.Children.Add(new Border { Child = row, Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8) });
                }
            }
        }
        search.TextChanged += (_, _) => Refresh(); Refresh(); dialog.Content = root; dialog.ShowDismissible();
    }
    internal static FrameworkElement ToolPreview(string kind)
    {
        if(kind=="Pixel timer")
        {
            var grid=new Canvas{Width=28,Height=28};for(var i=0;i<16;i++){var cell=new System.Windows.Shapes.Rectangle{Width=5,Height=5};cell.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,i<7?"PixelBrush":"LineBrush");Canvas.SetLeft(cell,i%4*7);Canvas.SetTop(cell,i/4*7);grid.Children.Add(cell);}return grid;
        }
        if(kind=="Focus Reset")return SpaceIcons.Create("Sparkle",25);
        if(kind=="Water Bottle Counter")return new DrinkArtwork(false){Width=25,Height=32,FillFraction=.5};
        if(kind=="Productivity")return SpaceIcons.Create("Lightning",25);
        if(kind=="Calendar")return SpaceIcons.Create("Calendar",25);
        var symbol=new TextBlock{Text=WidgetIcon(WidgetLibrary.Definition(kind).Editor),FontFamily=new FontFamily("Segoe MDL2 Assets"),FontSize=24,VerticalAlignment=VerticalAlignment.Center};symbol.SetResourceReference(TextBlock.ForegroundProperty,"AccentBrush");return symbol;
    }
    private static string WidgetIcon(string editor) => editor switch
    {
        "timer" or "stopwatch" => "\uE916", "clock" => "\uE823", "bible" or "scripture" => "\uE736",
        "counter" => "\uE8EF", "checklist" or "list" => "\uE9D5", "tracker" or "distribution" => "\uE80A",
        "links" => "\uE71B", "timeline" => "\uE81C", "progress" => "\uE9D9",
        _ => "\uE70B"
    };
    internal WidgetInstance AddWidget(string kind, Point? location = null)
    {
        if (state.Widgets.Count >= 200) throw new InvalidOperationException("A Space supports up to 200 widgets.");
        PinCurrentWidgetPositions();
        var panelKey = kind switch { "Pixel timer" => "pixels", "Session controls" => "setup", "Session timeline" => "timeline", "Hedy" => "hedy", _ => "" };
        if (state.Widgets.Any(w => w.PanelKey == panelKey)) panelKey = "";
        var widget = WidgetLibrary.Create(kind, panelKey); WidgetLibrary.Add(state, widget); PlayInterfaceSound();
        var span = DisplaySpan(widget);
        var width = Math.Max(1, span * GridUnit - WidgetGap);
        var height = Math.Max(widget.Height, WidgetLibrary.MinimumHeight(widget));
        var occupied = widgetFrames.Values.Select(FrameRect).ToArray();
        bool Free(Rect candidate) => !(PixelObstacle is { } timer && PlacementOverlaps(timer, candidate)) &&
            !occupied.Any(rect => PlacementOverlaps(rect, candidate));
        var preferred = location is Point point ? new Rect(Math.Clamp(Math.Round(point.X / GridUnit), 0, 20 - span) * GridUnit,
            Math.Max(0, Math.Round(point.Y / RowUnit) * RowUnit), width, height) : Rect.Empty;
        var target = location != null && Free(preferred) ? preferred : Rect.Empty;
        if (target.IsEmpty)
        {
            var last = Math.Max(WidgetCanvas.ActualHeight, occupied.Select(r => r.Bottom).DefaultIfEmpty(0).Max());
            for (var row = 0; row <= Math.Ceiling((last + height + WidgetGap) / RowUnit) && target.IsEmpty; row++)
                for (var column = 0; column <= 20 - span; column++)
                {
                    var candidate = new Rect(column * GridUnit, row * RowUnit, width, height);
                    if (Free(candidate)) { target = candidate; break; }
                }
        }
        widget.PlacementX = target.X / Math.Max(1, WidgetCanvas.ActualWidth);
        widget.PlacementY = target.Y;
        widget.Column = Math.Clamp((int)Math.Round(target.X / GridUnit), 0, 20 - span);
        widget.Row = Math.Clamp((int)Math.Round(target.Y / RowUnit), 0, 30000);
        Changed(); RenderWidgets(); WidgetCanvas.UpdateLayout();
        SaveNow(); TutorialAction("added",widget.Id); return widget;
    }
    internal WidgetInstance DuplicateWidget(WidgetInstance source)
    {
        PinCurrentWidgetPositions();
        var clone = Continuity.Clone(source); clone.Id = Guid.NewGuid(); clone.PanelKey = ""; clone.Title += " copy";
        if (source.PanelKey == "notes") clone.Text = state.Notes;
        if (source.PanelKey == "tasks") clone.Items = Continuity.Clone(state.Tasks);
        WidgetLibrary.Add(state, clone);
        clone.PlacementX = 0;
        clone.PlacementY = Math.Ceiling((Math.Max(PixelObstacle?.Bottom ?? 0,
            widgetFrames.Values.Select(frame => FrameRect(frame).Bottom).DefaultIfEmpty(0).Max()) + WidgetGap) / RowUnit) * RowUnit;
        clone.Column = 0; clone.Row = (int)Math.Round(clone.PlacementY / RowUnit);
        Changed(); RenderWidgets(); SaveNow(); return clone;
    }
    private void RemoveWidget(WidgetInstance widget)
    {
        RemoveWidgetConfirmed(widget);
    }
    internal void RemoveWidgetConfirmed(WidgetInstance widget)
    {
        PinCurrentWidgetPositions();
        if (!SaveNow()) return;
        var removed=new RemovedWidget(Continuity.Clone(widget),state.Widgets.IndexOf(widget),state.Workspace!.ActiveDashboardId);
        var candidate = CopyWorkspaceState().Workspace!; var d = Continuity.Current(candidate, store.UserId);
        // Archive first so even a confirmed removal remains recoverable from history.
        Continuity.Archive(d); d.Current.SessionId = Guid.NewGuid(); d.Current.OpenedAt = DateTimeOffset.Now;
        d.Current.Data.Widgets.RemoveAll(w => w.Id == widget.Id);
        if (CommitWorkspace(candidate, preserveWidgetUndo: true)) { widgetUndo.Push(removed); widgetRedo.Clear(); while(widgetUndo.Count>20) { var recent=widgetUndo.Take(20).Reverse().ToArray(); widgetUndo.Clear(); foreach(var item in recent) widgetUndo.Push(item); } RenderWidgets(); CompactWidgetLayout(); ShowWidgetRemovedToast(); }
    }
    private void ConfigureWidget(WidgetInstance widget)
    {
        if (widget.Kind == "Distribution Tracker") { ManageDistribution(widget); RenderWidgets(); return; }
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
        CheckBox? linked=null;DatePicker? progressStartDate=null,progressEndDate=null;ClockPicker? progressStartTime=null,progressEndTime=null;
        if(type=="progress" && widget.PanelKey!="pixels")
        {
            linked=new CheckBox{Content="Use current session timing",IsChecked=widget.ProgressLinkedToSession,Margin=new Thickness(0,14,0,8)};panel.Children.Add(linked);
            var custom=new StackPanel{IsEnabled=!widget.ProgressLinkedToSession};panel.Children.Add(custom);
            var from=NexaTime.Local(widget.TimerStart??DateTimeOffset.UtcNow,Options.TimeZoneId);var to=NexaTime.Local(widget.TimerEnd??DateTimeOffset.UtcNow.AddHours(1),Options.TimeZoneId);
            progressStartDate=new DatePicker{SelectedDate=from.Date};progressEndDate=new DatePicker{SelectedDate=to.Date};progressStartTime=new ClockPicker{Value=from.TimeOfDay};progressEndTime=new ClockPicker{Value=to.TimeOfDay};
            custom.Children.Add(new TextBlock{Text="Independent start"});custom.Children.Add(progressStartDate);custom.Children.Add(progressStartTime);custom.Children.Add(new TextBlock{Text="Independent end"});custom.Children.Add(progressEndDate);custom.Children.Add(progressEndTime);
            linked.Checked+=(_,_)=>custom.IsEnabled=false;linked.Unchecked+=(_,_)=>custom.IsEnabled=true;
        }
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
            if(linked!=null)
            {
                var from=NexaTime.Instant((progressStartDate!.SelectedDate??DateTime.Today)+progressStartTime!.Value,Options.TimeZoneId);var to=NexaTime.Instant((progressEndDate!.SelectedDate??DateTime.Today)+progressEndTime!.Value,Options.TimeZoneId);
                if(linked.IsChecked!=true && to<=from){error.Text="Independent end must follow start.";return;}
                widget.ProgressLinkedToSession=linked.IsChecked==true;
                if(!widget.ProgressLinkedToSession){widget.TimerStart=from;widget.TimerEnd=to;widget.TimerStopped=null;}
            }
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
    private void GlobalSettingsClick(object sender, RoutedEventArgs e) => OpenMainSettings("Appearance");
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
    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        if (fullscreen) { ToggleFullscreen(); WindowState = WindowState.Maximized; }
        else NexaChrome.ToggleMaximize(this);
    }
    private async void HomeClick(object sender, RoutedEventArgs e)
    {
        if (NexaNavigation.Busy(this) || !SaveForClose()) return;
        try
        {
            var homeStore=previewHomeStore??store;
            await NexaNavigation.Change(this,async()=>new HomeWindow(homeStore,await Task.Run(homeStore.Load)),Options);
        }
        catch(OperationCanceledException) when(Dispatcher.HasShutdownStarted) { }
        catch(Exception ex) when(HistoryArchive.Recoverable(ex)){await Dispatcher.InvokeAsync(()=>Error("Could not return Home: "+ex.Message));}
    }
}
