using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SessionDashboard;

internal static partial class Program
{
    private sealed class CalendarReplies : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.RequestUri!.AbsolutePath.EndsWith("/calendars", StringComparison.Ordinal)
                ? """{"value":[{"id":"school","name":"School"}]}"""
                : """{"value":[{"subject":"Class","start":{"dateTime":"2026-10-03T14:00:00","timeZone":"UTC"},"end":{"dateTime":"2026-10-03T15:00:00","timeZone":"UTC"},"isAllDay":false}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new StringContent(body) });
        }
    }
    private static void CheckDashboard012()
    {
        var store = new StateStore(Path.Combine(output, "dashboard012.json"));
        var pixel = WidgetLibrary.Create("Pixel timer", "pixels");
        var notes = WidgetLibrary.Create("Notes", "notes");
        var clock = WidgetLibrary.Create("Clock");
        pixel.BannerSpan = 12;
        clock.Span = 8;
        var calendar = WidgetLibrary.Create("Calendar");
        var stopwatch = WidgetLibrary.Create("Stopwatch");
        var water = WidgetLibrary.Create("Water Bottle Counter");
        var data = new AppState { WidgetLayoutInitialized = true, Widgets = [pixel, notes, clock, calendar, stopwatch, water], Notes = "Keep this note" };
        store.Save(data);
        var window = new MainWindow(store) { Width = 1450, Height = 950 };
        window.Show(); Pump(TimeSpan.FromMilliseconds(250));
        Border Frame(WidgetInstance widget) => window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, widget.Id));
        Rect Bounds(WidgetInstance widget) { var f = Frame(widget); return new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height); }
        var pixelWidth = window.PixelBannerContainer.ActualWidth;
        var pixelHeight = window.PixelBannerContainer.ActualHeight;
        Assert(pixelWidth >= 450 && pixelWidth < window.WidgetCanvas.ActualWidth && window.PixelBannerContainer.HorizontalAlignment == HorizontalAlignment.Left,
            "Pixel Timer occupies only its anchored visible width");
        Assert(Bounds(clock).Top == 0 && Bounds(clock).Left >= pixelWidth,
            "A compact Clock can share the Pixel Timer top row");
        Descendants(Frame(clock)).OfType<Button>().Single(b => Equals(b.Content,"Expanded")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        Assert(Math.Abs(window.PixelBannerContainer.ActualHeight - Bounds(clock).Height) < 1 && window.PixelBannerContainer.ActualHeight == 360,
            "Pixel Timer defaults to the expanded digital-and-analog Clock height");
        Descendants(Frame(clock)).OfType<Button>().Single(b => Equals(b.Content,"Compact")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        window.state.Widgets.Single(w => w.Id == clock.Id).Span = 8;
        typeof(MainWindow).GetMethod("ArrangeWidgetFrames", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null);
        var clockWidth = Bounds(clock).Width;
        var resize = window.PixelWidthHandle;
        resize.RaiseEvent(new DragStartedEventArgs(0, 0));
        resize.RaiseEvent(new DragDeltaEventArgs(100, 0));
        resize.RaiseEvent(new DragCompletedEventArgs(0, 0, false));
        Pump(TimeSpan.FromMilliseconds(120));
        Assert(window.state.Widgets.Single(w => w.Id == pixel.Id).BannerSpan > pixel.BannerSpan &&
            window.PixelBannerContainer.ActualWidth > pixelWidth && Math.Abs(window.PixelBannerContainer.ActualHeight - pixelHeight) < 1 &&
            Bounds(clock).Width < clockWidth && Bounds(clock).Top == 0 &&
            Math.Abs(window.PixelBannerContainer.ActualWidth + Bounds(clock).Width - pixelWidth - clockWidth) < 1,
            "Pixel right-edge drag grows the timer and shrinks the adjacent Clock in place");
        Assert(window.state.Widgets.Single(w => w.Id == pixel.Id).BannerSpan == store.Load().Widgets.Single(w => w.Id == pixel.Id).BannerSpan,
            "Pixel width saves through existing widget state");
        var widened = window.PixelBannerContainer.ActualWidth;
        var savedNotes = window.state.Widgets.Single(w => w.Id == notes.Id);
        savedNotes.Height = 360; savedNotes.CustomSize = true;
        typeof(MainWindow).GetMethod("ArrangeWidgetFrames", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null);
        var notesHeight = Bounds(notes).Height;
        var heightGrip = window.PixelHeightHandle;
        heightGrip.RaiseEvent(new DragStartedEventArgs(0,0));
        heightGrip.RaiseEvent(new DragDeltaEventArgs(0,48));
        heightGrip.RaiseEvent(new DragCompletedEventArgs(0,0,false));
        Pump(TimeSpan.FromMilliseconds(100));
        Assert(window.PixelBannerContainer.ActualHeight == pixelHeight + 48 && Bounds(notes).Height == notesHeight - 48 && Math.Abs(window.PixelBannerContainer.ActualWidth - widened) < 1 &&
            window.PixelBannerContainer.VerticalAlignment == VerticalAlignment.Top,
            "Bottom-edge drag grows the Pixel Timer and shrinks the widget below");
        Assert(store.Load().Widgets.Single(w => w.Id == pixel.Id).BannerHeight == pixelHeight + 48,
            "Pixel Timer height persists to the current Space");
        heightGrip.RaiseEvent(new DragStartedEventArgs(0,0));
        heightGrip.RaiseEvent(new DragDeltaEventArgs(0,-48));
        heightGrip.RaiseEvent(new DragCompletedEventArgs(0,0,false));
        Pump(TimeSpan.FromMilliseconds(100));
        Assert(window.PixelBannerContainer.ActualHeight == pixelHeight,
            "Dragging upward restores the default banner height");
        Assert(Descendants(Frame(clock)).OfType<Button>().Any(b => Equals(b.Content, "Compact")) &&
            Descendants(Frame(stopwatch)).OfType<Button>().Any(b => Equals(b.Content, "Expanded")) &&
            Descendants(Frame(calendar)).OfType<Button>().Any(b => Equals(b.Content, "Expanded")),
            "Clock, Stopwatch and Calendar expose direct display modes");
        var stopwatchFrame = Frame(stopwatch);
        var labels = Descendants(stopwatchFrame).OfType<Button>().Select(b => b.Content?.ToString()).ToArray();
        Assert(labels.Contains("Start") && labels.Contains("Pause") && labels.Contains("Reset") && !labels.Contains("Resume"),
            "Stopwatch uses one Pause/Resume control in its compact row");
        Descendants(stopwatchFrame).OfType<Button>().Single(b => Equals(b.Content,"Expanded")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.state.Widgets.Single(w=>w.Id==stopwatch.Id).Height >= 360 &&
            Descendants(Frame(stopwatch)).OfType<AnalogClock>().Any(a=>a.Visibility==Visibility.Visible),
            "Expanded Stopwatch keeps its analog display and usable height");
        Descendants(Frame(stopwatch)).OfType<Button>().Single(b => Equals(b.Content,"Compact")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.state.Widgets.Single(w=>w.Id==stopwatch.Id).Height == 192 &&
            Descendants(Frame(stopwatch)).OfType<AnalogClock>().All(a=>a.Visibility==Visibility.Collapsed),
            "Compact Stopwatch hides analog and shrinks to its saved minimum");
        var calendarFrame = Frame(calendar);
        Descendants(calendarFrame).OfType<Button>().Single(b=>Equals(b.Content,"Expanded")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.state.Widgets.Single(w=>w.Id==calendar.Id).Height >= 384,
            "Expanded Calendar grows to its agenda minimum");
        Descendants(Frame(calendar)).OfType<Button>().Single(b=>Equals(b.Content,"Compact")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(80));
        calendarFrame=Frame(calendar);
        Assert(Descendants(calendarFrame).OfType<TextBlock>().Any(t => t.Text.Contains("Not connected")) &&
            Descendants(calendarFrame).OfType<ComboBox>().Any(c => c.Items.OfType<string>().Contains("Outlook Calendar")),
            "Disconnected Calendar stays usable and offers both sources");
        var source = Descendants(calendarFrame).OfType<ComboBox>().First(c => c.Items.OfType<string>().Contains("Outlook Calendar"));
        source.SelectedItem = "Outlook Calendar"; Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.state.Widgets.Single(w => w.Id == calendar.Id).CalendarService == "Outlook Calendar" &&
            store.Load().Widgets.Single(w => w.Id == calendar.Id).CalendarService == "Outlook Calendar",
            "Calendar source selection is saved per widget and Space");
        var session = new Session { Title = "Class", Start = DateTimeOffset.Now.AddMinutes(-10), End = DateTimeOffset.Now.AddMinutes(40) };
        window.state.Session = session;
        var start = session.Start;
        typeof(MainWindow).GetMethod("SessionTimeEditClick", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(window, [window.Countdown, new RoutedEventArgs()]);
        Pump(TimeSpan.FromMilliseconds(100));
        var editor = window.OwnedWindows.Cast<Window>().Single(w => w.IsVisible);
        var today = Descendants(editor).OfType<Button>().Single(b => Equals(b.Content, "Use Today's Date"));
        today.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var expected = DateTime.Now.AddHours(1);
        var selected = Descendants(editor).OfType<DatePicker>().Single().SelectedDate;
        Assert(selected == expected.Date && Descendants(editor).OfType<TextBox>().Any(t => t.Text.Contains(expected.ToString("h:mm"))),
            "Use Today's Date selects the local date and time one hour ahead");
        Assert(MainWindow.SuggestedPixelTarget(new DateTime(2026,10,3,23,30,0)) == new DateTime(2026,10,4,0,30,0),
            "One-hour target rolls its date across midnight");
        Descendants(editor).OfType<Button>().Single(b => Equals(b.Content, "Set target")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(100));
        Assert(ReferenceEquals(session, window.state.Session) && session.Start == start && session.End > DateTimeOffset.Now.AddMinutes(55),
            "Pixel target editor updates the existing session without starting a new one");
        var noteSurface = Descendants(Frame(notes)).OfType<TextBox>().FirstOrDefault(t => t.AcceptsReturn);
        Assert(noteSurface != null && noteSurface.Text == "Keep this note" && noteSurface.Height >= 110,
            "Notepad surface retains existing text and fills available widget height");
        Assert(Descendants(Frame(water)).OfType<System.Windows.Shapes.Path>().Count() >= 2,
            "Water counter shows the glass and fill shapes");
        var storedNotes = window.state.Widgets.Single(w => w.Id == notes.Id);
        storedNotes.PlacementX = 0; storedNotes.PlacementY = 1200;
        typeof(MainWindow).GetMethod("ArrangeWidgetFrames", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null);
        typeof(MainWindow).GetMethod("CompactWidgetLayout", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null);
        Assert(Bounds(notes).Top < 1200 && window.state.Widgets.Single(w => w.Id == notes.Id).PlacementY == Bounds(notes).Top,
            "A gap compacts upward and the resulting position is saved");
        var rectangles = window.state.Widgets.Where(w => w.Kind != "Pixel timer").Select(Bounds).ToArray();
        Assert(rectangles.SelectMany((a,i) => rectangles.Skip(i+1).Select(b => a.IntersectsWith(b))).All(overlap => !overlap),
            "Compaction leaves widget cards disjoint");
        heightGrip.RaiseEvent(new DragStartedEventArgs(0,0));
        heightGrip.RaiseEvent(new DragDeltaEventArgs(0,24));
        heightGrip.RaiseEvent(new DragCompletedEventArgs(0,0,false));
        Pump(TimeSpan.FromMilliseconds(80));
        window.Close();
        var reopenedWindow = new MainWindow(store) { Width = 1450, Height = 950 };
        reopenedWindow.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(reopenedWindow.PixelBannerContainer.ActualHeight == pixelHeight + 24 &&
            reopenedWindow.state.Widgets.Single(w => w.Id == pixel.Id).BannerHeight == pixelHeight + 24,
            "Saved Pixel Timer height restores when the Space reopens");
        reopenedWindow.Close();
        using var http = new HttpClient(new CalendarReplies());
        var client = new IntegrationClient(store, http);
        client.Vault.Write("Outlook Calendar", new IntegrationCredential { AccessToken = "synthetic", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        var provider = client.CalendarProvider("Outlook Calendar");
        var lists = provider.Calendars(CancellationToken.None).GetAwaiter().GetResult();
        var events = provider.Schedule("school", DateTimeOffset.Now, CancellationToken.None).GetAwaiter().GetResult();
        Assert(lists.Single().Name == "School" && events.Single().Title == "Class" &&
            events.Single().End > events.Single().Start,
            "Outlook provider maps Graph calendars and event times");
    }
}
