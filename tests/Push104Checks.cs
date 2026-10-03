using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckPush104Models()
    {
        Assert(App.AppVersion == "1.0.5", "Package metadata reports Nexa 1.0.5");
        var start = DateTimeOffset.Now.AddMinutes(-21);
        var session = new Session { Title = "Summary test", Start = start, End = start.AddMinutes(45), IntervalMinutes = 10,
            IntervalNotes = new() { [0] = "Reviewed the first concept.", [2] = "Current interval should stay open." },
            HedyIntervalNotes = new() { [1] = "[Hedy · received] Discussed the second concept." } };
        SessionSummaries.RefreshCompleted(session, DateTimeOffset.Now);
        Assert(session.IntervalSummaries.Count == 2 && session.IntervalSummaries[0].Contains("first concept") && session.IntervalSummaries[1].Contains("second concept") && !session.IntervalSummaries.ContainsKey(2), "Completed intervals receive concise chronological summaries while the current interval stays open");

        var flashcards = WidgetLibrary.Create("Flashcards");
        flashcards.FlashcardDecks.Add(new FlashcardDeck { Name = "Biology", Cards = [new Flashcard { Front = "Cell", Back = "Basic unit of life", StudyState = "Know it" }] });
        flashcards.ActiveFlashcardDeckId = flashcards.FlashcardDecks[0].Id;
        var data = new AppState { Session = session, Widgets = [flashcards], WidgetLayoutInitialized = true,
            Background = new SpaceBackground { Mode = SpaceBackgroundMode.CustomImage, ImagePath = "local.png", Blur = true, BlurStrength = 18, Overlay = .55 } };
        WidgetLibrary.Validate(data);
        var store = new StateStore(Path.Combine(output, "push104-models.json")); store.Save(data); var loaded = store.Load();
        Assert(loaded.Background.Mode == SpaceBackgroundMode.CustomImage && loaded.Background.Blur && loaded.Background.BlurStrength == 18 && loaded.Background.Overlay == .55, "Per-Space image, blur and overlay settings persist");
        Assert(loaded.Widgets.Single().FlashcardDecks.Single().Cards.Single().StudyState == "Know it" && loaded.Widgets.Single().FlashcardDecks.Single().Cards.Single().Back.Contains("unit of life"), "Flashcard decks, content and study state persist locally");
        Assert(WidgetLibrary.Definition("Clock").DefaultSpan == 4 && !WidgetLibrary.Definition("Clock").CanMinimize && WidgetLibrary.Definition("Pixel timer").MinSpan <= 6, "Clock starts compact and the Pixel Timer can span three or more columns");
        Assert(WidgetLibrary.Definition("Bible").CanMinimize == false && WidgetLibrary.Definition("Flashcards").CanMinimize == false, "Purpose-critical reader and study widgets do not expose minimize");
    }

    private static void CheckPush104Ui()
    {
        var homeStore = new StateStore(Path.Combine(output, "push104-home.json"));
        var home = new HomeWindow(homeStore); home.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(home.Body is ScrollViewer && new[] { "Create Space", "Resume", "Import Space" }.All(label => Descendants(home).OfType<TextBlock>().Any(t => t.Text == label)), "Home scrolls naturally and primary actions keep permanent labels");
        Assert(Descendants(home).OfType<Button>().Any(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Settings") && Descendants(home).OfType<TextBlock>().Any(t => t.Text.Contains("Autosaved")), "Home footer contains the animated Settings gear and compact autosave state");
        home.WindowState = WindowState.Maximized; home.UpdateLayout(); Assert(home.Body is ScrollViewer viewer && viewer.VerticalScrollBarVisibility == ScrollBarVisibility.Auto, "Maximized Home retains natural scrolling without a floating footer");

        var noSessionSettings = new SettingsDialog(home, homeStore, home.Saved, null, "Session"); noSessionSettings.Show(); Pump(TimeSpan.FromMilliseconds(100));
        var categoryNames = Descendants(noSessionSettings).OfType<Button>().Select(b => b.Content?.ToString()).ToHashSet();
        Assert(new[] { "Appearance", "Session", "Hedy", "Account", "Data" }.All(categoryNames.Contains), "Settings uses the five-category two-pane architecture");
        Assert(Descendants(noSessionSettings).OfType<TextBlock>().Any(t => t.Text == "No active session"), "Session Settings shows a clear no-active-session state");
        noSessionSettings.Close(); home.Close();

        var store = new StateStore(Path.Combine(output, "push104-ui.json")); var state = store.Load(); var now = DateTimeOffset.Now;
        state.Session = new Session { Title = "Nexa 1.0.4", Start = now.AddMinutes(-12), End = now.AddMinutes(48), IntervalMinutes = 10, ScheduledDays = [DayOfWeek.Monday, DayOfWeek.Wednesday], ConfiguredDurationMinutes = 60, IntervalNotes = new() { [0] = "Completed summary source" } };
        var clock = WidgetLibrary.Create("Clock"); clock.CustomSize = true; clock.Span = 10; clock.Height = 330;
        state.Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), WidgetLibrary.Create("Session timeline", "timeline"), clock, WidgetLibrary.Create("Bible"), WidgetLibrary.Create("Flashcards")]; state.WidgetLayoutInitialized = true; store.Save(state);
        var window = new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(300));
        Assert(window.Timeline.Children.Count > 0 && Descendants(window.Timeline).OfType<TextBlock>().Any(t => t.Text.Contains("Completed summary source")), "Session Notes displays completed summaries in chronological cards");
        Assert(window.NotesSourcePicker.Items.Count == 2 && window.NotesSourcePicker.Items.OfType<ComboBoxItem>().Select(i => i.Content?.ToString()).SequenceEqual(new[] { "Hedy", "Local Recording" }), "Session Notes offers Hedy and Local Recording sources");
        Assert(window.RecordingStatus.Text.Contains("starts only when") && window.RecordingElapsed.Text == "00:00", "Local recording never begins automatically and exposes elapsed status");
        Assert(Descendants(window.LocalRecordingControls).OfType<TextBlock>().Any(t => t.Text.Contains("never uploaded") && t.Text.Contains("unavailable")), "Recording privacy and unavailable transcription are stated without fake output");
        var clockFrame = window.WidgetCanvas.Children.OfType<Border>().Single(frame => Equals(frame.Tag, clock.Id));
        Assert(Descendants(clockFrame).OfType<AnalogClock>().Any(a => a.Visibility == Visibility.Visible), "Enlarged Clock reveals the centered analog face");

        var imagePath = Path.Combine(output, "space-background.png");
        var pixels = new byte[] { 35, 50, 90, 255 }; var bitmap = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var file = File.Create(imagePath)) encoder.Save(file);
        Assert(window.SaveSpaceBackground(new SpaceBackground { Mode = SpaceBackgroundMode.CustomImage, ImagePath = imagePath, Blur = true, BlurStrength = 16, Overlay = .5 }) && window.SpaceBackgroundLayer.Background is ImageBrush && window.SpaceBackgroundLayer.Effect is BlurEffect blur && blur.Radius == 16 && window.SpaceBackgroundOverlay.Opacity == .5, "Custom Space images apply blur only to the background layer with readable dimming");
        Assert(store.Load().Background.ImagePath == imagePath && store.Load().Background.Blur, "Space background restores from saved state");

        var settings = new SettingsDialog(window, store, window.state, window, "Hedy"); settings.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text.StartsWith("Hedy is optional")), "Hedy controls deep-link to the shared Settings Hedy page");
        settings.SelectSection("Account"); Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text == "Coming Soon"), "Account is an honest Coming Soon page");
        settings.SelectSection("Data"); Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text == "Coming Soon"), "Data is an honest Coming Soon page");
        Assert(Application.Current.Resources[typeof(CheckBox)] is Style, "Checkboxes use the shared rounded theme-aware style");
        settings.Close(); window.Close();
    }
}
