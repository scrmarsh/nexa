using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static int passed;
    private static string output = "";
    [STAThread]
    private static int Main(string[] args)
    {
        output = Path.GetFullPath(Path.Combine("test-output", DateTime.Now.ToString("yyyyMMdd-HHmmss")));
        Directory.CreateDirectory(output);
        try
        {
            if(args.Contains("--alpha019"))
            {
                var alphaApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                alphaApp.InitializeComponent(); CheckAlpha019();
                Console.WriteLine($"PASS: {passed} Alpha versioning / session settings checks. Artifacts: {output}"); alphaApp.Shutdown(); return 0;
            }
            if(args.Contains("--stability108"))
            {
                var stabilityApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                stabilityApp.InitializeComponent(); CheckStability108();
                Console.WriteLine($"PASS: {passed} Nexa 1.0.8 checks. Artifacts: {output}"); stabilityApp.Shutdown(); return 0;
            }
            if(args.Contains("--polish107"))
            {
                var polishApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                polishApp.InitializeComponent(); CheckPolish107();
                Console.WriteLine($"PASS: {passed} Nexa 1.0.7 checks. Artifacts: {output}"); polishApp.Shutdown(); return 0;
            }
            if(args.Contains("--continuityaudit"))
            {
                CheckContinuityModels();
                var continuityApp=new App { ShutdownMode=ShutdownMode.OnExplicitShutdown,SuppressNormalStartup=true }; continuityApp.InitializeComponent(); CheckContinuityUi();
                Console.WriteLine($"PASS: {passed} continuity audit checks. Artifacts: {output}"); continuityApp.Shutdown(); return 0;
            }
            if(args.Contains("--directaudit"))
            {
                var directApp=new App { ShutdownMode=ShutdownMode.OnExplicitShutdown,SuppressNormalStartup=true }; directApp.InitializeComponent(); CheckDirectAudit();
                Console.WriteLine($"PASS: {passed} direct-editing audit checks. Artifacts: {output}"); directApp.Shutdown(); return 0;
            }
            if(args.Contains("--audit106"))
            {
                var auditApp=new App { ShutdownMode=ShutdownMode.OnExplicitShutdown,SuppressNormalStartup=true }; auditApp.InitializeComponent(); CheckAudit106();
                Console.WriteLine($"PASS: {passed} targeted verification checks. Artifacts: {output}"); auditApp.Shutdown(); return 0;
            }
            if(args.Contains("--onboarding106"))
            {
                var app106=new App { ShutdownMode=ShutdownMode.OnExplicitShutdown,SuppressNormalStartup=true }; app106.InitializeComponent(); CheckOnboarding106();
                Console.WriteLine($"PASS: {passed} onboarding / integration checks. Artifacts: {output}"); app106.Shutdown(); return 0;
            }
            if (args.Contains("--focus105"))
            {
                var focusApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                focusApp.InitializeComponent(); CheckFocus105();
                Console.WriteLine($"PASS: {passed} Focus First checks. Artifacts: {output}"); focusApp.Shutdown(); return 0;
            }
            if (args.Contains("--polish104"))
            {
                var polishApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                polishApp.InitializeComponent(); CheckPolish104();
                Console.WriteLine($"PASS: {passed} focused polish checks. Artifacts: {output}");
                polishApp.Shutdown(); return 0;
            }
            if (args.Contains("--purpose"))
            {
                var purposeApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                purposeApp.InitializeComponent(); CheckPurposeWidgets();
                Console.WriteLine($"PASS: {passed} purpose and inline editing checks. Artifacts: {output}");
                purposeApp.Shutdown(); return 0;
            }
            if (args.Contains("--tiling"))
            {
                var tileApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                tileApp.InitializeComponent(); CheckWidgetTiling();
                Console.WriteLine($"PASS: {passed} widget tiling checks. Artifacts: {output}");
                tileApp.Shutdown(); return 0;
            }
            if (args.Contains("--sizing"))
            {
                var sizingApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                sizingApp.InitializeComponent(); CheckWidgetSizing();
                Console.WriteLine($"PASS: {passed} widget sizing checks. Artifacts: {output}");
                sizingApp.Shutdown(); return 0;
            }
            if (args.Contains("--bible"))
            {
                var bibleApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                bibleApp.InitializeComponent(); CheckBible();
                Console.WriteLine($"PASS: {passed} Bible checks. Artifacts: {output}");
                bibleApp.Shutdown(); return 0;
            }
            if (args.Contains("--preview"))
            {
                var previewApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                previewApp.InitializeComponent();
                CheckPreview();
                Console.WriteLine($"PASS: {passed} preview checks. Artifacts: {output}");
                previewApp.Shutdown();
                return 0;
            }
            if (args.Contains("--swarm"))
            {
                CheckSwarmModels();
                var swarmApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                swarmApp.InitializeComponent();
                CheckSwarmStandalone();
                Console.WriteLine($"PASS: {passed} Swarm checks. Artifacts: {output}");
                swarmApp.Shutdown();
                return 0;
            }
            if (args.Contains("--push104"))
            {
                CheckPush104Models();
                var pushApp = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
                pushApp.InitializeComponent(); CheckPush104Ui();
                Console.WriteLine($"PASS: {passed} Nexa 1.0.4 PUSH checks. Artifacts: {output}");
                pushApp.Shutdown(); return 0;
            }
            CheckModels();
            CheckSwarmModels();
            CheckStoryGraph();
            CheckRevisionModels();
            CheckContinuityModels();
            var app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown, SuppressNormalStartup = true };
            app.InitializeComponent();
            CheckDashboard();
            CheckRevisionUi();
            CheckContinuityUi();
            CheckPush104Models();
            CheckPush104Ui();
            Console.WriteLine($"PASS: {passed} checks. Artifacts: {output}");
            app.Shutdown();
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void Assert(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        passed++; Console.WriteLine("PASS: " + description);
    }
    private static void CheckModels()
    {
        var start = new DateTimeOffset(2026, 9, 30, 23, 30, 0, TimeSpan.FromHours(-5));
        var session = new Session { Title = "Overnight", Start = start, End = start.AddHours(2), IntervalMinutes = 10 };
        Assert(InputRules.ValidSession(session), "Overnight session is valid");
        Assert(session.Progress(start.AddMinutes(-10)) == 0, "Future session has no elapsed progress");
        Assert(session.Remaining(start.AddMinutes(-10)) == TimeSpan.FromMinutes(10), "Scheduled timer counts down to start");
        Assert(session.Progress(start.AddHours(1)) == 0.5, "Progress is based on timestamps");
        Assert(session.Elapsed(start.AddHours(1)) == TimeSpan.FromHours(1), "Elapsed time is correct");
        Assert(session.Remaining(start.AddHours(1)) == TimeSpan.FromHours(1), "Remaining time is correct");
        Assert(session.Progress(start.AddDays(1)) == 1 && session.Remaining(start.AddDays(1)) == TimeSpan.Zero, "Sleep or late restart clamps to completion");
        session.StoppedAt = start.AddMinutes(30);
        Assert(session.Progress(start.AddHours(1)) == 0.25 && !session.IsRunning(start.AddHours(1)), "Early stop freezes progress");
        Assert(!InputRules.ValidMinutes("0", out _) && !InputRules.ValidMinutes("-5", out _) && !InputRules.ValidMinutes("1.5", out _) && !InputRules.ValidMinutes("10081", out _), "Invalid durations rejected");
        Assert(InputRules.ValidMinutes("10080", out _) && InputRules.IsColor("#2786FF") && !InputRules.IsColor("red"), "Duration and color boundaries");
        Assert(MainWindow.ClockText(TimeSpan.FromMilliseconds(1)) == "00:00:01" && MainWindow.ClockText(TimeSpan.FromHours(26)) == "26:00:00", "Countdown rounds up and supports over 24 hours");
        var path = Path.Combine(output, "roundtrip.json");
        var store = new StateStore(path);
        var state = new AppState { Session = session, Notes = "Unicode notes: café ✓", Compact = true, Tasks = [new() { Text = "Read", Done = true }] };
        state.Presets[0].Name = "Edited lecture";
        store.Save(state);
        var loaded = store.Load();
        Assert(loaded.Presets[0].Name == "Edited lecture" && loaded.Tasks[0].Done && loaded.Notes == state.Notes && loaded.Compact, "Presets, notes, checklist and settings round-trip");
        Assert(loaded.Session!.Start == session.Start && loaded.Session.StoppedAt == session.StoppedAt, "Session timestamps round-trip");
        store.Save(state);
        File.WriteAllText(path, "broken json");
        var recovered = store.Load();
        Assert(recovered.Notes == state.Notes && store.Warning != null, "Corrupt primary file recovers backup");
        store.Save(recovered);
        Assert(Directory.GetFiles(output, "roundtrip.json.recovered-*").Length == 1, "Corrupt source preserved on save");
        var badPath = Path.Combine(output, "bad.json");
        File.WriteAllText(badPath, "null");
        var badStore = new StateStore(badPath);
        Assert(badStore.Load().Presets.Count == 4 && badStore.Warning != null, "Unreadable settings safely load defaults");
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Click(MainWindow window, string content) => Descendants(window).OfType<Button>().First(b => Equals(b.Content, content)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void CheckDashboard()
    {
        var store = new StateStore(Path.Combine(output, "ui-state.json"));
        var window = new MainWindow(store);
        Assert(window.PixelGrid.Children.Count == 540 && window.PresetPicker.Items.Count == 4, "WPF loads 540 pixels and four presets");
        window.TitleInput.Text = "Research session"; window.DurationInput.Text = "50"; window.IntervalInput.Text = "10";
        Click(window, "Save as new");
        Assert(store.Load().Presets.Any(p => p.Name == "Research session"), "Save preset button persists new preset");
        window.DurationInput.Text = "45"; Click(window, "Update preset");
        Assert(store.Load().Presets.Last().Minutes == 45, "Update preset button persists edits");
        Click(window, "Start now");
        Assert(window.state.Session!.Title == "Research session" && window.state.Session.IsRunning(DateTimeOffset.Now), "Start now creates active session");
        Assert(window.PixelGrid.Children.OfType<Border>().Any(p => p.HasAnimatedProperties), "Active pixel head has a running WPF animation");
        Assert(window.Timeline.Children.Count == 5, "Timeline includes final partial interval");
        var session = window.state.Session;
        window.DurationInput.Text = "0"; Click(window, "Start now");
        Assert(ReferenceEquals(session, window.state.Session), "Invalid input leaves active session intact");
        window.NotesBox.Text = "Review the next chapter, then write a short summary.";
        window.TaskInput.Text = "Outline the main ideas"; Click(window, "Add");
        var check = Descendants(window.TaskList).OfType<CheckBox>().Single();
        check.IsChecked = true; check.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        window.SaveNow();
        Assert(store.Load().Notes == window.NotesBox.Text && store.Load().Tasks.Single().Done, "Notes and checklist UI persist");
        window.AccentInput.Text = "bad"; Click(window, "Apply colors");
        Assert(window.state.AccentColor == "#5B7CFF", "Invalid color rejected without changing settings");
        window.AccentInput.Text = "#6C7AEF"; Click(window, "Apply colors");
        Assert(window.state.AccentColor == "#6C7AEF", "Custom color applied");
        window.NotesToggle.IsChecked = false; window.NotesToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        Assert(window.NotesPanel.Visibility == Visibility.Collapsed, "Optional notes panel hides");
        window.NotesToggle.IsChecked = true; window.NotesToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        window.PinToggle.IsChecked = true; window.PinToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        Assert(window.Topmost, "Always-on-top setting reaches window");
        window.PinToggle.IsChecked = false; window.PinToggle.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        window.StartDate.SelectedDate = DateTime.Today.AddDays(1); window.EndDate.SelectedDate = DateTime.Today.AddDays(2);
        window.StartTime.Text = "23:30"; window.EndTime.Text = "01:30"; Click(window, "Use these times");
        Assert(window.state.Session!.End - window.state.Session.Start == TimeSpan.FromHours(2) && window.StatusLabel.Text == "SCHEDULED", "UI schedules overnight dates and counts down to start");
        Assert(!window.PixelGrid.Children.OfType<Border>().Any(p => p.HasAnimatedProperties), "Scheduled session stops the active pixel animation");
        var scheduled = window.state.Session;
        window.EndDate.SelectedDate = DateTime.Today; Click(window, "Use these times");
        Assert(ReferenceEquals(scheduled, window.state.Session), "Invalid schedule preserves current timer");
        window.DurationInput.Text = "45"; Click(window, "Start now");
        session = window.state.Session!;
        // Use the scheduling controls to produce a representative partly complete session.
        window.StartDate.SelectedDate = DateTime.Now.AddMinutes(-20).Date; window.EndDate.SelectedDate = DateTime.Now.AddMinutes(25).Date;
        window.StartTime.Text = DateTime.Now.AddMinutes(-20).ToString("HH:mm"); window.EndTime.Text = DateTime.Now.AddMinutes(25).ToString("HH:mm"); Click(window, "Use these times");
        session = window.state.Session!;
        window.UpdateClock();
        window.SaveNow();
        Snapshot(window, "dashboard", 1204, 840);
        Snapshot(window, "dashboard-small", 944, 620);
        Click(window, "I'm bored");
        Assert(window.StoryArea.Visibility == Visibility.Visible && window.Countdown.Visibility == Visibility.Visible, "Swarm opens inside dashboard with countdown visible");
        var before = window.Countdown.Text;
        Pump(TimeSpan.FromMilliseconds(1300));
        Assert(before != window.Countdown.Text && window.state.Session!.IsRunning(DateTimeOffset.Now), "Dispatcher countdown continues while Swarm is open");
        CheckSwarmUi(window, store);
        Click(window, "Back to timer");
        Click(window, "Compact mode");
        Assert(window.FullContent.Visibility == Visibility.Collapsed && window.ExpandButton.Visibility == Visibility.Visible, "Compact mode keeps timer and expansion control");
        Snapshot(window, "compact", 614, 270);
        window.SaveNow();
        var compactRestored = new MainWindow(store);
        Assert(compactRestored.FullContent.Visibility == Visibility.Collapsed && compactRestored.Width == 630, "Compact mode is restored after reopening");
        compactRestored.Close();
        Click(window, "Expand Space");
        Assert(window.FullContent.Visibility == Visibility.Visible, "Compact mode restores dashboard");
        window.SaveNow();
        window.Close();
        var restored = new MainWindow(store);
        Assert(restored.state.Session!.Start == session.Start && restored.SessionTitle.Text == "Research session", "WPF restart restores unfinished session");
        Click(restored, "I'm bored");
        Assert(restored.swarmGame != null && restored.swarmGame.Score == 0, "Restart opens a fresh temporary Swarm run");
        Click(restored, "Back to timer");
        Click(restored, "End session");
        Assert(store.Load().Session!.StoppedAt != null && restored.Countdown.Text == "00:00:00", "End button saves stopped session");
        restored.state.Session!.StoppedAt = null;
        restored.state.Session.Start = DateTimeOffset.Now.AddMinutes(-10);
        restored.state.Session.End = DateTimeOffset.Now.AddMinutes(-1);
        restored.UpdateClock();
        Assert(restored.PercentLabel.Text.StartsWith("100") && restored.Countdown.Text == "00:00:00" && !restored.EndButton.IsEnabled, "Completed session UI reaches 100 percent and stops");
        Assert(!restored.PixelGrid.Children.OfType<Border>().Any(p => p.HasAnimatedProperties), "Completed session clears pixel animation");
        restored.PresetPicker.SelectedIndex = restored.PresetPicker.Items.Count - 1;
        Click(restored, "Delete selected preset");
        Assert(store.Load().Presets.Count == 4, "Delete preset persists");
        restored.Close();
    }
    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static void CheckStoryGraph()
    {
        var reached = new HashSet<string>();
        void Visit(string id, HashSet<string> path)
        {
            if (!path.Add(id)) throw new InvalidOperationException("Story contains a loop: " + id);
            if (!Story.Scenes.TryGetValue(id, out var node)) throw new InvalidOperationException("Missing scene: " + id);
            reached.Add(id);
            foreach (var choice in node.Choices) Visit(choice.Next, new HashSet<string>(path));
        }
        Visit("arrival", []);
        Assert(reached.Count == Story.Scenes.Count, "Every story scene is reachable with no dead links or loops");
        Assert(reached.Count(id => Story.Scenes[id].IsEnding) == 6, "All six distinct story endings are reachable");
        var state = new AppState();
        Story.Choose(state, 0); Story.Choose(state, 0); Story.Choose(state, 0);
        Assert(state.StoryNode == "beacon" && state.StoryHistory.Count == 3 && state.StoryEndings.Count == 1, "Choices determine the story outcome");
        var store = new StateStore(Path.Combine(output, "story.json")); store.Save(state);
        Assert(store.Load().StoryNode == "beacon" && store.Load().StoryHistory.Count == 3, "Story outcome and decision journal persist");
        Story.Restart(state);
        Assert(state.StoryNode == "arrival" && state.StoryHistory.Count == 0 && state.StoryEndings.Count == 1, "Restart preserves ending collection");
        Story.Choose(state, 2); Story.Choose(state, 1); Story.Choose(state, 1);
        Assert(state.StoryNode == "quiet" && state.StoryEndings.Count == 2, "Different choices lead to a different ending");
    }
    private static void Snapshot(MainWindow window, string name, double width, double height)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
    }
}
