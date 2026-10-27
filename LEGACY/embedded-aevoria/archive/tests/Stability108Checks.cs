using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckStability108()
    {
        object? Call(object target, string name, params object?[] args) => target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
        void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
        void Tap(DependencyObject scope, string label) => Descendants(scope).OfType<Button>().Single(b => Equals(b.Content, label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(App.AppVersion == "0.1.11", "Current version is 0.1.11");
        var cases = new Dictionary<string, decimal> { ["2+3*4"] = 14, ["(2+3)*4"] = 20, ["1.5+2.25"] = 3.75m, ["200*10%"] = 20, ["-4+8/2"] = 0, ["9-2"] = 7, ["100/4"] = 25, ["50%"] = .5m };
        foreach (var item in cases) Assert(Calculator.Evaluate(item.Key) == item.Value, "Calculator evaluates " + item.Key);
        foreach (var invalid in new[] { "2/0", "1.2.3", "2+", "x", new string('1', 257) })
        { var rejected = false; try { Calculator.Evaluate(invalid); } catch (Exception ex) when (ex is FormatException or DivideByZeroException) { rejected = true; } Assert(rejected, "Calculator safely rejects " + invalid[..Math.Min(12, invalid.Length)]); }
        var oldHedy = WidgetLibrary.Create("Hedy"); oldHedy.Kind = "Hedy"; oldHedy.Title = "My imported notes"; oldHedy.Text = "Saved Hedy content"; oldHedy.Items.Add(new() { Text = "Keep this too" });
        var timeline = WidgetLibrary.Create("Session timeline", "timeline"); var old = new DashboardData { WidgetLayoutInitialized = true, Widgets = [timeline, oldHedy] };
        var oldId = oldHedy.Id; WidgetLibrary.PrepareLayout(old);
        Assert(timeline.Title == "Session Timeline" && old.Widgets.Count == 2 && oldHedy.Id == oldId && oldHedy.Kind == "Hedy Notes" && oldHedy.Title == "My imported notes" && oldHedy.Text == "Saved Hedy content" && oldHedy.Items.Count == 1, "Hedy migration preserves separate widget identity, custom title and content");
        WidgetLibrary.Validate(old);
        var store = new StateStore(Path.Combine(output, "stability108.json"));
        var initial = DashboardTemplates.Build(DashboardTemplates.All.Single(t => t.Name == "Class"));
        initial.Session = new Session { Title = "Stability class", Start = DateTimeOffset.Now.AddMinutes(-10), End = DateTimeOffset.Now.AddMinutes(20), IntervalMinutes = 5, HedyTranscript = "Keep imported Hedy transcript", HedyIntervalNotes = new() { [0] = "Keep Hedy interval" } };
        initial.Notes = "Keep local notes"; store.Save(Continuity.Activate(Continuity.Migrate(new AppState { Widgets = initial.Widgets, WidgetLayoutInitialized = true, Session = initial.Session, Notes = initial.Notes }, store.UserId), store.UserId));
        var profile = store.LoadProfile(); profile.DisplayName = "Student"; store.SaveProfile(profile);
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(!Descendants(home).OfType<TextBlock>().Any(t => t.Text.StartsWith("Welcome back,") || t.Text.StartsWith("Hey,")) && !Descendants(home).OfType<TextBlock>().Any(t => t.Text.Contains("Auto-saved")), "Home keeps profile data without greetings or autosave clutter");
        Call(home, "OpenSettings", "Profile"); var homeSettings = home.OwnedWindows.OfType<SettingsDialog>().Single(); profile.DisplayName = ""; store.SaveProfile(profile); homeSettings.Close();
        Assert(!Descendants(home).OfType<TextBlock>().Any(t => t.IsVisible && t.Text.StartsWith("Welcome back,")), "Home remains greeting-free after profile changes"); home.Close();
        var window = new MainWindow(store) { Width = 1220 }; window.Show(); Pump(TimeSpan.FromMilliseconds(200));
        Border Frame(WidgetInstance widget) => window.WidgetCanvas.Children.OfType<Border>().Single(b => Equals(b.Tag, widget.Id));
        Rect Bounds(WidgetInstance widget) { var frame = Frame(widget); return new Rect(Canvas.GetLeft(frame), Canvas.GetTop(frame), frame.Width, frame.Height); }
        void PixelEdges(string phase)
        {
            var pixels = window.PixelGrid.Children.OfType<Border>().ToArray();
            Assert(pixels.Length > 0 && pixels.All(p => p.Width > 0 && Math.Abs(p.Width - p.Height) < .01), phase + ": all pixels have square geometry");
            Assert(Math.Abs(pixels.Min(p => Canvas.GetLeft(p))) < .01 && Math.Abs(pixels.Max(p => Canvas.GetLeft(p) + p.Width) - window.PixelArea.ActualWidth) < 1.1, phase + ": pixels reach both canvas edges");
        }
        PixelEdges("Normal"); var normalCount = window.PixelGrid.Children.Count;
        window.Width = 1700; Pump(TimeSpan.FromMilliseconds(100)); PixelEdges("Wide"); Assert(window.PixelGrid.Children.Count > normalCount, "Wider display adds pixels");
        window.Width = 1000; Pump(TimeSpan.FromMilliseconds(100)); PixelEdges("Narrow"); Assert(window.PixelGrid.Children.Count < normalCount, "Narrower display removes pixels");
        window.state.PixelShape = "Circle"; window.RefreshWidgetPreferences(); Assert(window.PixelGrid.Children.OfType<Border>().All(p => Math.Abs(p.CornerRadius.TopLeft - p.Width / 2) < .01), "Circle shape uses shared responsive canvas");
        var timerSession = window.state.Session!; window.UpdateClock(timerSession.End.AddSeconds(1));
        Assert(window.SessionProgressBar.Value == 100 && window.PixelGrid.Effect == null && window.PixelGrid.Children.OfType<Border>().All(p => ((SolidColorBrush)p.Background).Color == ((SolidColorBrush)window.FullContent.FindResource("DashboardPixelBrush")).Color && !p.HasAnimatedProperties), "Finished timer stays filled in theme color without glow or animation");
        window.UpdateClock();
        window.SaveFocusPreferences();
        Call(window, "StoryClick", window, new RoutedEventArgs());
        Assert(((SolidColorBrush)window.SwarmGameZone.Background).Color == ((SolidColorBrush)window.FullContent.FindResource("DashboardBackgroundBrush")).Color, "Game background matches active dashboard theme");
        var before = window.state.Session!.Remaining(DateTimeOffset.Now); Pump(TimeSpan.FromMilliseconds(350)); Call(window, "ExitBoredomMode");
        Assert(window.state.Session.Remaining(DateTimeOffset.Now) < before && window.state.Notes == "Keep local notes", "Session advances during themed game without overwriting notes");
        var settings = new SettingsDialog(window, store, window.state, window, "Hedy"); settings.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text.StartsWith("Hedy is optional")) && !Descendants(settings).OfType<Button>().Any(b => Equals(b.Content, "Hedy")), "Old Hedy destination selects Integrations and removes obsolete Hedy tab");
        settings.SelectSection("Widgets"); Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text == "Pixel Timer") && Descendants(settings).OfType<ComboBox>().Any(c => c.Items.Contains("Circle")), "Widgets settings exposes pixel shape"); settings.Close();
        window.state.FocusPulseEnabled = true; window.state.FocusPulseIntensity = .88; window.state.DistractionEscalationEnabled = false; window.SaveFocusPreferences();
        profile = store.LoadProfile(); profile.DisplayName = "Changed name"; store.SaveProfile(profile);
        var preference = new DashboardData(); store.LoadInteractionPreferences(preference);
        Assert(preference.PixelShape == "Circle" && preference.FocusPulseEnabled && preference.FocusPulseIntensity == .88 && !preference.DistractionEscalationEnabled, "Profile edits preserve new global widget preferences");
        var image = new RenderTargetBitmap(8, 8, 96, 96, PixelFormats.Pbgra32); var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) dc.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 8, 8)); image.Render(visual);
        var imagePath = Path.Combine(output, "background.png"); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using (var stream = File.Create(imagePath)) encoder.Save(stream);
        Assert(window.SaveSpaceBackground(new() { Mode = SpaceBackgroundMode.CustomImage, ImagePath = imagePath, Overlay = .65 }) && window.SpaceBackgroundLayer.Background is ImageBrush && window.SpaceBackgroundOverlay.Opacity == .65, "Custom background loads with readability overlay and saves");
        window.state.Background.ImagePath = Path.Combine(output, "missing.png"); window.ApplySpaceBackground(); Assert(window.SpaceBackgroundLayer.Background == null, "Missing background falls back without crashing"); window.state.Background.ImagePath = imagePath; window.ApplySpaceBackground();
        var savedAt = window.lastSuccessfulSaveAt; Call(window, "ShowSaveStatus", "Auto-saving...", false, false);
        Assert(window.SaveSpinner.Visibility == Visibility.Visible && window.lastSuccessfulSaveAt == savedAt, "Saving spinner leaves success timestamp unchanged");
        Call(window, "ShowSaveStatus", "Save failed", false, true); Assert(window.SaveSpinner.Visibility == Visibility.Collapsed && window.lastSuccessfulSaveAt == savedAt, "Failure stops spinner without updating timestamp");
        Assert(window.SaveNow() && window.SaveStatus.Text.Contains("Auto-saved · Saved at") && window.SaveSpinner.Visibility == Visibility.Collapsed, "Successful dashboard save shows local time and stops spinner");
        Assert(window.WidgetScroller.VerticalScrollBarVisibility == ScrollBarVisibility.Hidden, "Dashboard scrollbar chrome is hidden");
        var timelineWidget = window.AddWidget("Session timeline", new Point(0, 456)); var timelineFrame = Frame(timelineWidget);
        var note = Descendants(timelineFrame).OfType<TextBox>().Single(t => Equals(t.ToolTip, "Add a timeline note")); note.Text = "A provider-neutral manual note"; Tap(timelineFrame, "Add note");
        Assert(window.state.Session.TimelineEntries.Any(e => e.Source == "manual" && e.Text == "A provider-neutral manual note") && !Descendants(timelineFrame).Contains(window.HedySessionPanel), "Standalone Timeline accepts local entries without Hedy controls");
        var hedyWidget = window.AddWidget("Hedy Notes"); Assert(Descendants(Frame(hedyWidget)).OfType<Button>().Any(b => Equals(b.Content, "Connect Hedy")), "Separate Hedy Notes exposes existing integration");
        var calculator = window.AddWidget("Calculator", new Point(0, 744)); var calculatorFrame = Frame(calculator);
        var input = Descendants(calculatorFrame).OfType<TextBox>().Single(t => Equals(t.ToolTip, "Type a calculation; Enter to calculate")); input.Text = "200*10%"; Tap(calculatorFrame, "=");
        Assert(calculator.CalculationHistory.Single() == "200*10% = 20", "Calculator UI evaluates and persists history"); input.Clear(); Tap(calculatorFrame, "7"); Tap(calculatorFrame, "+"); Tap(calculatorFrame, "2"); Tap(calculatorFrame, "="); Assert(input.Text == "7+2" && calculator.CalculationHistory.First() == "7+2 = 9", "Calculator keys insert at caret and calculate without dragging");
        Assert(!Descendants(window.WidgetCanvas).OfType<Button>().Any(b => Equals(b.ToolTip, "Configure")) && Descendants(calculatorFrame).OfType<TextBox>().First().ContextMenu!.Items.OfType<MenuItem>().Any(i => Equals(i.Header, "Edit widget options")), "Configure buttons removed while title editing preserves advanced options");
        var stopwatch = window.AddWidget("Stopwatch"); Tap(Frame(stopwatch), "Start"); Pump(TimeSpan.FromMilliseconds(320)); Tap(Frame(stopwatch), "Pause");
        var digital = Descendants(Frame(stopwatch)).OfType<TextBlock>().Single(t => t.FontFamily.Source == "Consolas");
        var analog = Descendants(Frame(stopwatch)).OfType<AnalogClock>().Single(); var analogValue = (double)typeof(AnalogClock).GetField("elapsedSeconds", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(analog)!;
        Assert(digital.Text.Contains('.') && Math.Abs(analogValue - stopwatch.StopwatchElapsedSeconds) < .001 && analogValue > .15, "Milliseconds and analog stopwatch share exact paused elapsed time");
        var recorder = window.AddWidget("Audio Recorder"); Set(window, "microphoneConsentGranted", true);
        var commands = new List<string>(); var failRecordingSave = false;
        window.RecordingCommand = command => { commands.Add(command); if (command.StartsWith("save")) { if (failRecordingSave) return 1; var path = command.Split('"')[1]; File.WriteAllBytes(path, [0, 1, 2]); } return 0; };
        Tap(Frame(recorder), "Record"); Pump(TimeSpan.FromMilliseconds(80)); Tap(Frame(recorder), "Pause"); Tap(Frame(recorder), "Resume"); failRecordingSave = true; Tap(Frame(recorder), "Stop");
        Assert((bool)typeof(MainWindow).GetField("recordingActive", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)! && window.RecordingStatus.Text.Contains("held in memory"), "Recorder save failure retains recording buffer for retry");
        failRecordingSave = false; Tap(Frame(recorder), "Stop"); var firstPath = window.state.Session.LocalRecordingPath;
        Assert(File.Exists(firstPath) && window.state.Session.TimelineEntries.Any(e => e.Source == "recorder" && e.AudioPath == firstPath) && commands.Any(c => c.StartsWith("pause")), "Recorder pause/resume/stop saves owner-scoped audio and Timeline entry (mocked device)");
        Tap(Frame(recorder), "Record"); Tap(Frame(recorder), "Stop"); Assert(window.state.Session.LocalRecordingPath != firstPath && File.Exists(firstPath), "Repeated recording uses unique files and preserves earlier capture");
        var distraction = window.AddWidget("Distraction counter"); window.state.DistractionEscalationEnabled = true;
        foreach (var value in new[] { 4, 5, 10, 15, 20, 25, 30, 100 }) { distraction.Value = value; window.UpdateClock(); Assert(value < 5 || Descendants(Frame(distraction)).OfType<TextBlock>().Any(t => t.Text == MainWindow.DistractionMessage(value)), "Distraction threshold " + value); }
        Pump(TimeSpan.FromMilliseconds(700)); var distractionLabel = Descendants(Frame(distraction)).OfType<TextBlock>().Single(t => t.Text == "LOCK. IN. NOW."); Assert(Math.Abs(((TranslateTransform)distractionLabel.RenderTransform).X) < .01, "Escalation shake finishes instead of repeating (offset " + ((TranslateTransform)distractionLabel.RenderTransform).X + ")");
        window.state.DistractionEscalationEnabled = false; window.UpdateClock(); Assert(distractionLabel.Text == "", "Distraction escalation can be disabled");
        Call(window, "FocusWidget", calculator.Id); var backdrop = (Border)typeof(MainWindow).GetField("widgetFocusBackdrop", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Assert(backdrop.Background.HasAnimatedProperties && !Frame(calculator).HasAnimatedProperties && Panel.GetZIndex(Frame(calculator)) > Panel.GetZIndex(backdrop), "Only focus backdrop pulses; selected widget stays stable and above it"); Call(window, "ClearWidgetFocus");
        // Active card remains the anchor throughout live resize, including collisions below it.
        window.Width = 1280; window.WidgetCanvas.UpdateLayout(); var anchor = Bounds(calculator);
        var resize = window.WidgetCanvas.Children.OfType<Thumb>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Resize " + calculator.Title);
        resize.RaiseEvent(new DragStartedEventArgs(0, 0)); resize.RaiseEvent(new DragDeltaEventArgs(0, 240));
        Assert(Bounds(calculator).Location == anchor.Location && Bounds(calculator).Height >= anchor.Height + 239, "Active resize keeps anchor instead of teleporting below neighbors"); resize.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(220)); Assert(Bounds(calculator).Location == anchor.Location, "Resize release compacts around preserved anchor");
        var added = window.AddWidget("Notes", new Point(400, 960)); Assert(Math.Abs(Bounds(added).Y - 960) < .01 && Math.Abs(Bounds(added).X - 400) <= (window.WidgetCanvas.ActualWidth + 8) / 20, "Clicked location anchors added widget in shared grid");
        var removed = Continuity.Clone(added); window.RemoveWidgetConfirmed(added); Assert(!window.state.Widgets.Any(w => w.Id == added.Id) && Descendants(window).OfType<Button>().Any(b => Equals(b.Content, "Undo")), "Removal is normal and shows Undo toast");
        Assert(window.HandleWidgetShortcut(Key.Z, ModifierKeys.Control, null), "Ctrl+Z dispatch restores widget"); var restored = window.state.Widgets.Single(w => w.Id == removed.Id);
        Assert(restored.PlacementX == removed.PlacementX && restored.PlacementY == removed.PlacementY && restored.Height == removed.Height && restored.Span == removed.Span, "Undo restores original placement and size");
        Assert(!window.HandleWidgetShortcut(Key.R, ModifierKeys.Control, null) && window.HandleWidgetShortcut(Key.Z, ModifierKeys.Control | ModifierKeys.Shift, null), "Ctrl+Shift+Z redoes removal; Ctrl+R remains unbound"); window.UndoWidgetRemoval();
        var textEditor = new TextBox(); textEditor.AppendText("Typing"); Assert(!window.HandleWidgetShortcut(Key.Z, ModifierKeys.Control, textEditor), "Text editing keeps native Ctrl+Z priority");
        Call(window, "AddWidgetClick", window, new RoutedEventArgs()); Pump(TimeSpan.FromMilliseconds(160)); var picker = Application.Current.Windows.OfType<Window>().Single(w => w.Title == "Nexa · Add widget");
        var sections = Descendants(picker).OfType<TextBlock>().Where(t => t.FontSize == 18).ToArray(); Assert(sections[0].Text == "Recommended" && Descendants(picker).OfType<Button>().Count(b => Equals(b.Tag, "Session timeline")) == 2, "Recommended leads picker and reuses existing widget type"); picker.Close();
        Call(window, "ToggleFullscreen"); Pump(TimeSpan.FromMilliseconds(160));
        Assert(window.DashboardHeader.Padding.Top >= 8 && window.TopNavigation.TranslatePoint(new Point(), (UIElement)window.Content).Y >= 0, "Fullscreen navigation has visible top safe spacing"); Call(window, "ToggleFullscreen");
        window.SaveNow(); var savedLayout = JsonSerializer.Serialize(window.state.Widgets); var savedEntries = window.state.Session.TimelineEntries.Count; RenderPreview((FrameworkElement)window.Content, "stability108-dashboard"); window.Close();
        var reopened = new MainWindow(store); reopened.Show(); Pump(TimeSpan.FromMilliseconds(200));
        Assert(JsonSerializer.Serialize(reopened.state.Widgets) == savedLayout && reopened.state.Notes == "Keep local notes" && reopened.state.Session!.HedyTranscript == "Keep imported Hedy transcript" && reopened.state.Session.TimelineEntries.Count == savedEntries, "Reopen preserves layout, manual/Hedy notes, calculator and recorded entries");
        Assert(reopened.SpaceBackgroundLayer.Background is ImageBrush && reopened.state.PixelShape == "Circle", "Background and global widget preferences survive reopening"); reopened.Close();
        var previewHome = new StateStore(Path.Combine(output, "preview-home108.json"));
        foreach (var scenario in PreviewDashboards.Scenarios)
        {
            var previewStore = new StateStore(Path.Combine(output, scenario + "-preview108.json")); previewStore.Save(Continuity.Activate(PreviewDashboards.Generate(previewStore.UserId, scenario: scenario, random: new Random(3)), previewStore.UserId));
            var preview = new MainWindow(previewStore, previewHome); preview.Show(); Pump(TimeSpan.FromMilliseconds(200));
            var rectangles = preview.WidgetCanvas.Children.OfType<Border>().Where(b => b.Tag is Guid).Select(b => new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.Width, b.Height)).ToArray();
            Assert(rectangles.Length > 0 && rectangles.Min(r => r.Top) == 0 && rectangles.SelectMany((r, i) => rectangles.Skip(i + 1).Select(other => !r.IntersectsWith(other))).All(v => v), scenario + " preview compacts to top without overlap"); preview.Close();
        }
        var recordWindow = new MainWindow(store); recordWindow.Show(); Set(recordWindow, "microphoneConsentGranted", true);
        recordWindow.RecordingCommand = command => { if (command.StartsWith("save")) File.WriteAllBytes(command.Split('"')[1], [0, 1]); return 0; };
        Call(recordWindow, "BeginLocalRecording"); var switchCandidate = recordWindow.CopyWorkspaceState().Workspace!;
        var outgoingId = switchCandidate.ActiveDashboardId;
        var otherDashboard = Continuity.NewDashboard("Other session", "Blank", new DashboardData { WidgetLayoutInitialized = true, Session = Continuity.Clone(recordWindow.state.Session!) }, store.UserId); otherDashboard.Current.Data.Session!.TimelineEntries.Clear(); otherDashboard.Current.Data.Session.LocalRecordingPath = "";
        switchCandidate.Dashboards.Add(otherDashboard); switchCandidate.ActiveDashboardId = otherDashboard.DashboardId;
        Assert(recordWindow.CommitWorkspace(switchCandidate) && recordWindow.state.Session!.LocalRecordingPath == "" && recordWindow.state.Workspace!.Dashboards.Single(d => d.DashboardId == outgoingId).Current.Data.Session!.TimelineEntries.Count > savedEntries, "Switch finalizes capture in outgoing dashboard; matching session times cannot leak audio to another dashboard");
        recordWindow.state.Workspace!.Dashboards.Single(d => d.DashboardId == otherDashboard.DashboardId).PresetOrigin = "Developer mode";
        recordWindow.state.InterfaceSoundsEnabled = false;
        var developerSettings = new SettingsDialog(recordWindow, store, recordWindow.state, recordWindow, "Developer"); developerSettings.Show(); Tap(developerSettings, "Play completion chime");
        Assert(Descendants(developerSettings).OfType<Button>().Any(b => Equals(b.Content, "Developer")) && typeof(MainWindow).GetField("interfacePlayer", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(recordWindow) == null, "Developer sound test is available and respects master sounds Off"); developerSettings.Close(); recordWindow.Close();
    }
}
