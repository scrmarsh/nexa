using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckPolish107()
    {
        Assert(App.AppVersion == "1.0.7", "Project metadata supplies version 1.0.7");
        var store = new StateStore(Path.Combine(output, "polish107.json"));
        var cards = new[] { "Notes", "Notes", "Notes", "Clock", "Tasks", "Focus Reset" }.Select(kind => WidgetLibrary.Create(kind)).ToList();
        WidgetLibrary.ArrangeThreeColumns(cards);
        foreach (var card in cards) { card.CustomSize = true; card.Height = 288; }
        cards[0].Text = "Keep my class notes"; cards[4].Items.Add(new() { Text = "Keep my task" });
        var initial = new AppState { WidgetLayoutInitialized = true, Notes = "Existing session notes", Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), .. cards],
            Session = new Session { Title = "Polish class", Start = DateTimeOffset.Now.AddMinutes(-6), End = DateTimeOffset.Now.AddMinutes(24), IntervalMinutes = 5 } };
        store.Save(initial);
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(160));
        var gear = Descendants(home).OfType<Button>().Single(b => Equals(b.ToolTip, "Settings"));
        var graphic = (FrameworkElement)gear.Content; var turn = (RotateTransform)graphic.RenderTransform;
        var buttonPosition = gear.TranslatePoint(new Point(), home); var buttonSize = gear.RenderSize;
        gear.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent }); Pump(TimeSpan.FromMilliseconds(130));
        Assert(turn.HasAnimatedProperties && graphic.RenderTransformOrigin == new Point(.5, .5), "Only centered gear has hover animation (angle " + turn.Angle + ")");
        Assert(gear.RenderTransform.Value.IsIdentity && gear.RenderSize == buttonSize && gear.TranslatePoint(new Point(), home) == buttonPosition, "Settings button geometry stays stationary");
        gear.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent }); Pump(TimeSpan.FromMilliseconds(290));
        Assert(Math.Abs(turn.Angle) < .01, "Gear returns smoothly after hover"); home.Close();

        var window = new MainWindow(store) { Width = 1280, Height = 1000, MinWidth = 460 }; window.Show(); Pump(TimeSpan.FromMilliseconds(240));
        var widgets = window.state.Widgets.Where(w => w.Kind != "Pixel timer").ToList();
        Border Frame(WidgetInstance w) => window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, w.Id));
        Rect Bounds(WidgetInstance w) { var f = Frame(w); return new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height); }
        Point Visual(WidgetInstance w) { window.WidgetCanvas.UpdateLayout(); return Frame(w).TranslatePoint(new Point(), window.WidgetCanvas); }
        Thumb Grip(WidgetInstance w) => Descendants(Frame(w)).OfType<Thumb>().Single();
        void Start(WidgetInstance w) => Grip(w).RaiseEvent(new DragStartedEventArgs(0, 0));
        void Delta(WidgetInstance w, double x, double y) => Grip(w).RaiseEvent(new DragDeltaEventArgs(x, y));
        void End(WidgetInstance w, bool canceled = false) => Grip(w).RaiseEvent(new DragCompletedEventArgs(0, 0, canceled));
        void Layout(string phase)
        {
            var rectangles = widgets.Select(Bounds).ToArray();
            Assert(rectangles.All(r => r.X >= -.01 && r.Right <= window.WidgetCanvas.ActualWidth + .01 && r.Height > 0), phase + ": cards fit canvas");
            Assert(rectangles.SelectMany((a, i) => rectangles.Skip(i + 1).Select(b => !a.IntersectsWith(b))).All(v => v), phase + ": no overlaps");
        }
        Assert(widgets.Take(3).All(w => Bounds(w).Y == 0) && Bounds(widgets[2]).X > window.WidgetCanvas.ActualWidth * .65, "Three cards occupy left, middle and right columns at wide width");
        Layout("Existing three-column layout");
        var unchanged = JsonSerializer.Serialize(window.state.Widgets);
        Assert(!Descendants(window).OfType<Button>().Any(b => Equals(b.Content, "Focus Reset")) && WidgetLibrary.Definitions.Any(d => d.Name == "Focus Reset"), "Top Focus Reset removed; library entry retained");
        Assert(Descendants(Frame(widgets[5])).OfType<Button>().Any(b => Equals(b.Content, "Take 45 seconds to return to focus")), "Focus Reset widget action retained");
        Descendants(Frame(widgets[5])).OfType<Button>().Single(b => Equals(b.Content, "Take 45 seconds to return to focus")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Descendants(window).OfType<TextBlock>().Any(t => t.Text.StartsWith("Breathe in gently")), "Focus Reset still starts through its widget");
        Click(window, "Return to session");
        Assert(JsonSerializer.Serialize(window.state.Widgets) == unchanged, "Focus Reset return preserves widget data");
        var pixels = window.PixelGrid.Children.OfType<Border>().ToArray();
        void SquarePixels(string phase)
        {
            Assert(pixels.Length == 540 && pixels.All(p => p.Width > 0 && Math.Abs(p.Width - p.Height) < .01), phase + ": timer has 540 square cells");
            Assert(window.PixelGrid.ActualWidth <= window.PixelArea.ActualWidth + 1 && window.PixelGrid.ActualHeight <= window.PixelArea.ActualHeight + 1 && window.PixelGrid.HorizontalAlignment == HorizontalAlignment.Center, phase + $": pixels fit and center in banner (grid {window.PixelGrid.ActualWidth}x{window.PixelGrid.ActualHeight}, area {window.PixelArea.ActualWidth}x{window.PixelArea.ActualHeight})");
        }
        SquarePixels("Wide");
        var moved = widgets[5]; var target = widgets[2]; var original = Bounds(moved); var neighbors = widgets.Where(w => w != moved).ToDictionary(w => w.Id, Bounds);
        var pickupPosition = Visual(moved); Start(moved);
        Assert((Visual(moved) - pickupPosition).Length < 1 && Panel.GetZIndex(Frame(moved)) > 2000 && Frame(moved).Effect != null, "Pickup lifts actual card without changing grab position");
        for (var i = 1; i <= 10; i++) { Delta(moved, 137.5 * i / 10, -105.25 * i / 10); Assert((Visual(moved) - (pickupPosition + new Vector(137.5 * i / 10, -105.25 * i / 10))).Length < 1, "Slow move follows unsnapped pointer delta " + i); }
        Assert(neighbors.All(p => Bounds(widgets.Single(w => w.Id == p.Key)) == p.Value), "Neighbors stay still for entire held drag");
        var heldFrame = Frame(moved); window.RenderWidgets();
        Assert(ReferenceEquals(heldFrame, Frame(moved)), "Re-render requests cannot destroy captured card");
        var destination = Bounds(target); Delta(moved, destination.X - original.X + 4, destination.Y - original.Y + 5); var releasePosition = Visual(moved); End(moved);
        Assert((Visual(moved) - releasePosition).Length < 1 && Frame(moved).RenderTransform is TranslateTransform, "Drop animation starts at physical release position");
        var settlingPosition = Visual(moved); var dropData = JsonSerializer.Serialize(window.state.Widgets); Start(moved);
        Assert((Visual(moved) - settlingPosition).Length < 1, "Pickup during settle preserves visible grab position"); End(moved, true);
        Assert(JsonSerializer.Serialize(window.state.Widgets) == dropData, "Cancel during settle preserves committed drop");
        Pump(TimeSpan.FromMilliseconds(230));
        Assert(Math.Abs(Bounds(moved).X - destination.X) < .01 && Bounds(moved).Y == 0, "Bottom card intentionally takes occupied top-right column");
        Assert(Bounds(target) != destination, "Occupied drop reflows displaced neighbor"); Layout("Occupied drop");
        Assert(widgets.Count(w => Bounds(w).Y == 0) == 3 && widgets.Select(w => Bounds(w).Y).Distinct().Count() == 2, "Released layout fills top slots without empty rows");
        for (var tick = 0; tick < 10 && widgets.Any(w => !Frame(w).RenderTransform.Value.IsIdentity); tick++) Pump(TimeSpan.FromMilliseconds(50));
        Assert(widgets.All(w => w.PlacementX != null && w.PlacementY % 24 == 0) && widgets.All(w => Frame(w).RenderTransform.Value.IsIdentity), "Final compact grid saved; settle transforms finish " + string.Join(";", widgets.Select(w => $"{w.Kind}:Y={w.PlacementY},offset={Frame(w).RenderTransform.Value.OffsetX},{Frame(w).RenderTransform.Value.OffsetY}")));

        var saved = JsonSerializer.Serialize(window.state.Widgets); original = Bounds(moved);
        Start(moved); Delta(moved, -500.75, 500.25); End(moved, true); Pump(TimeSpan.FromMilliseconds(230));
        Assert(JsonSerializer.Serialize(window.state.Widgets) == saved && Bounds(moved) == original && Frame(moved).Effect == null, "Cancel restores layout and lift without saving changes");
        var unit = (window.WidgetCanvas.ActualWidth + 8) / 20;
        Start(moved); Delta(moved, -original.X, 900); End(moved); Pump(TimeSpan.FromMilliseconds(230));
        Assert(Bounds(moved).X == 0 && Bounds(moved).Y < 900 && widgets.Select(w => Bounds(w).Y).Distinct().Count() == 2, "Low drop keeps requested left column and fills avoidable slots above"); Layout("Low drop compaction");
        var resize = window.WidgetCanvas.Children.OfType<Thumb>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Resize " + moved.Title);
        resize.RaiseEvent(new DragStartedEventArgs(0, 0)); resize.RaiseEvent(new DragDeltaEventArgs(unit, 48)); resize.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(230));
        Assert(moved.CustomSize && moved.Height == 336 && moved.Height % 24 == 0, "Existing resize still saves useful grid dimensions"); Layout("Resized");
        original = Bounds(moved); window.WidgetScroller.ScrollToVerticalOffset(120); Pump(TimeSpan.FromMilliseconds(40));
        Start(moved); Delta(moved, -original.X, -original.Y); End(moved); Pump(TimeSpan.FromMilliseconds(230));
        Assert(Bounds(moved).Location == new Point(), "Fast move after resize and scroll reaches top-left slot"); Layout("Move after resize");

        window.Width = 820; Pump(TimeSpan.FromMilliseconds(130)); SquarePixels("Medium"); Layout("Medium");
        Assert(widgets.Count(w => Bounds(w).Y == 0) == 2, "Medium width supports two columns");
        window.Width = 600; Pump(TimeSpan.FromMilliseconds(130)); SquarePixels("Narrow"); Layout("Narrow");
        Assert(widgets.Count(w => Bounds(w).Y == 0) == 1 && widgets.All(w => Math.Abs(Bounds(w).Width - window.WidgetCanvas.ActualWidth) < .1), "Narrow canvas uses one full-width column");
        Assert(JsonSerializer.Serialize(window.state.Widgets) != unchanged, "Only deliberate drop/resize updates saved layout");
        window.Width = 1280; Pump(TimeSpan.FromMilliseconds(150)); Layout("Return to wide");
        saved = JsonSerializer.Serialize(window.state.Widgets); var positions = widgets.ToDictionary(w => w.Id, Bounds);
        var now = DateTimeOffset.Now; window.UpdateClock(now);
        Assert(window.state.Session!.Start == initial.Session!.Start && window.state.Session.End == initial.Session.End && window.Countdown.Text == MainWindow.ClockText(initial.Session.Remaining(now)), "Pixel visual change preserves shared session timestamps and countdown");
        RenderPreview((FrameworkElement)window.Content, "polish107-dashboard"); window.SaveNow(); window.Close();
        var reopened = new MainWindow(store) { Width = 1280 }; reopened.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(JsonSerializer.Serialize(reopened.state.Widgets) == saved && reopened.state.Notes == "Existing session notes" && reopened.state.Widgets.Any(w => w.Text == "Keep my class notes") && reopened.state.Widgets.Any(w => w.Items.Any(i => i.Text == "Keep my task")), "Reload preserves compact layout and original notes/tasks");
        Assert(reopened.WidgetCanvas.Children.OfType<Border>().Where(b => b.Tag is Guid).All(f => positions[(Guid)f.Tag] == new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height)), "Reload restores final visual bounds");
        var micro = reopened.state.Widgets.Single(w => w.Kind == "Clock"); micro.IsMicro = true; reopened.RenderWidgets(); reopened.WidgetCanvas.UpdateLayout();
        var microFrame = reopened.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, micro.Id));
        microFrame.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseEnterEvent }); reopened.WidgetCanvas.UpdateLayout();
        var microGrip = Descendants(microFrame).OfType<Thumb>().Single(); var microData = JsonSerializer.Serialize(reopened.state.Widgets);
        microGrip.RaiseEvent(new DragStartedEventArgs(0, 0)); microGrip.RaiseEvent(new DragDeltaEventArgs(120, 80));
        microFrame.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent });
        Assert(Descendants(microFrame).Contains(microGrip), "Micro hover cannot detach captured grip during movement");
        microGrip.RaiseEvent(new DragCompletedEventArgs(0, 0, true));
        Assert(JsonSerializer.Serialize(reopened.state.Widgets) == microData, "Canceled micro drag retains saved layout"); reopened.Close();

        var bibleStore = new StateStore(Path.Combine(output, "polish107-bible.json"));
        var bibleWidget = WidgetLibrary.Create("Bible"); bibleWidget.CustomSize = true; bibleWidget.Height = 480; bibleWidget.BibleLocation = new() { Translation = "NLT", Book = "Psalms", Chapter = 119, Verse = 1 };
        bibleStore.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [bibleWidget] });
        var bibleWindow = new MainWindow(bibleStore); bibleWindow.Show(); Pump(TimeSpan.FromMilliseconds(600));
        var bible = bibleWindow.state.Widgets.Single(); var reader = Descendants(bibleWindow).OfType<BibleReader>().Single();
        var passageHeight = reader.Passage.ActualHeight; var headerHeight = reader.RowDefinitions[0].ActualHeight; var footerHeight = reader.RowDefinitions[3].ActualHeight;
        var edge = bibleWindow.WidgetCanvas.Children.OfType<Thumb>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Resize " + bible.Title);
        edge.RaiseEvent(new DragStartedEventArgs(0, 0)); edge.RaiseEvent(new DragDeltaEventArgs(0, 240)); edge.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(230));
        Assert(bible.Height == 720 && Math.Abs(reader.Passage.ActualHeight - passageHeight - 240) < 1, "Expanded Bible gives all added height to passage area");
        Assert(reader.RowDefinitions[0].ActualHeight == headerHeight && reader.RowDefinitions[3].ActualHeight == footerHeight, "Bible header/footer keep natural height");
        reader.Passage.ScrollToEnd(); Pump(TimeSpan.FromMilliseconds(70)); Assert(reader.Passage.VerticalOffset > 0, "Long chapter remains scrollable after expansion");
        RenderPreview((FrameworkElement)bibleWindow.Content, "polish107-bible-expanded");
        edge.RaiseEvent(new DragStartedEventArgs(0, 0)); edge.RaiseEvent(new DragDeltaEventArgs(0, -240)); edge.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(230));
        Assert(bible.Height == 480 && Math.Abs(reader.Passage.ActualHeight - passageHeight) < 1, "Bible shrinks back without unused content space"); bibleWindow.Close();
    }
}
