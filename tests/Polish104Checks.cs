using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckPolish104()
    {
        void Tap(DependencyObject scope, string name) => Descendants(scope).OfType<Button>().Single(b => Equals(b.Content, name)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void Capture(Window window, string name)
        {
            window.UpdateLayout(); var body = (FrameworkElement)window.Content;
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(body.ActualWidth), (int)Math.Ceiling(body.ActualHeight), 96, 96, PixelFormats.Pbgra32); bitmap.Render(body);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
        }
        Assert(App.AppVersion == "1.0.6", "App uses current project version 1.0.6");
        var splash = new SplashWindow(); splash.Show(); Pump(TimeSpan.FromMilliseconds(220));
        var tagline = Descendants(splash).OfType<TextBlock>().Single(t => t.Text == "A space for whatever comes next.");
        Assert(tagline.ActualWidth >= tagline.DesiredSize.Width && Descendants(splash).OfType<TextBlock>().Any(t => t.Text == "v" + App.AppVersion), "Splash tagline and current version are present and fit"); Capture(splash, "polish-splash"); splash.Close();
        var store = new StateStore(Path.Combine(output, "polish-state.json"));
        var normal = Continuity.NewDashboard("Existing Space", "Blank", new DashboardData { WidgetLayoutInitialized = true, Notes = "Keep these notes" }, store.UserId);
        var dev = Continuity.NewDashboard("Developer mode", "Developer mode", new DashboardData { WidgetLayoutInitialized = true, Notes = "Keep developer data" }, store.UserId);
        var workspace = new WorkspaceDocument { UserId = store.UserId, Dashboards = [normal, dev], ActiveDashboardId = normal.DashboardId };
        store.Save(Continuity.Activate(workspace, store.UserId));
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(220));
        Assert(home.Recent.Children.Count == 1 && Descendants(home.Recent).OfType<TextBlock>().Any(t => t.Text == "Existing Space"), "Recent Spaces excludes developer spaces");
        Assert(Descendants(home).OfType<TextBlock>().Any(t => t.Text.Contains("v" + App.AppVersion)), "Home shows shared current version"); Capture(home, "polish-home"); home.Close();
        var window = new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(220));
        Assert(window.state.Notes == "Keep these notes", "Existing dashboard opens with its data intact");
        var timer = window.AddWidget("Timer"); var stopwatch = window.AddWidget("Stopwatch");
        Border Frame(WidgetInstance w) => window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, w.Id));
        TextBox Duration(string name) => Descendants(Frame(timer)).OfType<TextBox>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Timer " + name);
        Duration("Hours").Text = "1"; Duration("Minutes").Text = "2"; Duration("Seconds").Text = "3"; Tap(Frame(timer), "Set");
        Assert(timer.DurationSeconds == 3723, "Timer accepts hours, minutes and seconds");
        Tap(Frame(timer), "Start"); var end = timer.TimerEnd; Pump(TimeSpan.FromMilliseconds(150)); Tap(Frame(timer), "Pause"); var stopped = timer.TimerStopped;
        Pump(TimeSpan.FromMilliseconds(150)); Assert(timer.TimerStopped == stopped && timer.TimerEnd == end, "Timer pause freezes its timestamps");
        Tap(Frame(timer), "Resume"); Assert(timer.TimerStopped == null && timer.TimerEnd > end, "Timer resumes by preserving remaining duration");
        var runningStart = timer.TimerStart; var runningEnd = timer.TimerEnd;
        WidgetLibrary.Place(window.state, timer, 0, 0, 8, 336);
        typeof(MainWindow).GetMethod("RenderWidgets", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null); window.SaveNow();
        Assert(timer.TimerStart == runningStart && timer.TimerEnd == runningEnd && store.Load().Widgets.Single(w => w.Id == timer.Id).TimerEnd == runningEnd, "Moving, resizing, rerendering and autosave preserve timer state");
        Tap(Frame(timer), "Reset"); Assert(timer.TimerStart == null && Descendants(Frame(timer)).OfType<TextBlock>().Any(t => t.Text == "01:02:03"), "Timer reset returns to selected duration");
        Tap(Frame(stopwatch), "Start"); Pump(TimeSpan.FromMilliseconds(180)); Tap(Frame(stopwatch), "Pause"); var elapsed = stopwatch.StopwatchElapsedSeconds;
        Pump(TimeSpan.FromMilliseconds(120)); Assert(elapsed > 0 && stopwatch.StopwatchElapsedSeconds == elapsed, "Stopwatch starts and pauses");
        Tap(Frame(stopwatch), "Resume"); var stopwatchStart = stopwatch.StopwatchStartedAt;
        WidgetLibrary.Place(window.state, stopwatch, 8, 0, 8, 288); typeof(MainWindow).GetMethod("RenderWidgets", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null); window.SaveNow();
        Assert(stopwatch.StopwatchStartedAt == stopwatchStart && store.Load().Widgets.Single(w => w.Id == stopwatch.Id).StopwatchStartedAt == stopwatchStart, "Stopwatch resume survives layout changes and autosave");
        Tap(Frame(stopwatch), "Pause"); Assert(stopwatch.StopwatchElapsedSeconds > elapsed, "Stopwatch resume accumulates additional elapsed time");
        stopwatch.StopwatchElapsedSeconds = 3661; typeof(MainWindow).GetMethod("RenderWidgets", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
        Assert(Descendants(Frame(stopwatch)).OfType<TextBlock>().Any(t => t.Text == "01:01:01"), "Stopwatch displays hours when needed");
        Tap(Frame(stopwatch), "Reset"); Assert(stopwatch.StopwatchElapsedSeconds == 0 && stopwatch.StopwatchStartedAt == null, "Stopwatch resets"); Capture(window, "polish-widgets");
        typeof(MainWindow).GetMethod("AddWidgetClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, [window, new RoutedEventArgs()]); Pump(TimeSpan.FromMilliseconds(220));
        var picker = Application.Current.Windows.OfType<Window>().Single(w => w.Title == "Nexa · Add widget");
        var scroll = Descendants(picker).OfType<ScrollViewer>().Single(v => v.Content is StackPanel);
        Assert(scroll.ScrollableHeight > 0, "Widget picker has a bounded scrollable list"); scroll.ScrollToBottom(); Pump(TimeSpan.FromMilliseconds(50)); Assert(scroll.VerticalOffset > 0, "Widget picker scrolls down"); Capture(picker, "polish-picker");
        var before = window.state.Widgets.Count; var add = Descendants(picker).OfType<Button>().Single(b => Equals(b.Tag, "Stopwatch"));
        Assert(Equals(add.Content, "Add"), "Stopwatch has a clear Add button"); add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(220)); Assert(window.state.Widgets.Count == before + 1 && window.state.Widgets.Count(w => w.Kind == "Stopwatch") == 2, "Picker Add places a Stopwatch on current dashboard");
        window.Close(); var restored = new MainWindow(store); restored.Show(); Pump(TimeSpan.FromMilliseconds(200));
        Assert(restored.state.Widgets.Count(w => w.Kind == "Stopwatch") == 2 && restored.state.Notes == "Keep these notes" && store.Load().Workspace!.Dashboards.Single(d => d.DashboardId == dev.DashboardId).Current.Data.Notes == "Keep developer data", "Dashboards reopen and normal/developer data are preserved"); restored.Close();
        var saved = store.Load(); saved.Workspace!.ActiveDashboardId = dev.DashboardId; store.Save(Continuity.Activate(saved.Workspace, store.UserId));
        var developer = new MainWindow(store); developer.Show(); Pump(TimeSpan.FromMilliseconds(200));
        Assert(developer.DeveloperSplashButton.IsVisible && developer.DeveloperSplashButton.ActualWidth >= 132, "Developer mode retains Preview Splash with sufficient button width"); Capture(developer, "polish-developer"); developer.Close();
    }
}
