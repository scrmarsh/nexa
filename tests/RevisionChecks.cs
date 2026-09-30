using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckRevisionModels()
    {
        var start = DateTimeOffset.Now;
        var session = new Session { Title = "Milestones", Start = start, End = start.AddMinutes(40) };
        Assert(Milestones.Collect(session, start.AddMinutes(19)).Count == 0, "No milestone fires early");
        Assert(Milestones.Collect(session, start.AddMinutes(20)).Single() == "HALFWAY THERE", "Halfway milestone fires at 50 percent");
        Assert(Milestones.Collect(session, start.AddMinutes(21)).Count == 0, "Milestone does not repeat");
        Assert(Milestones.Collect(session, start.AddMinutes(30)).Single() == "THREE QUARTERS COMPLETE", "Three-quarter milestone fires at 75 percent");
        Assert(Milestones.Collect(session, start.AddMinutes(35)).Single() == "5 MINUTES REMAINING", "Five-minute warning fires at threshold");
        Assert(Milestones.Collect(session, start.AddMinutes(40)).Count == 0, "Completion does not trigger milestone flashes");
        var store = new StateStore(Path.Combine(output, "milestones.json"));
        store.Save(new AppState { Session = session });
        Assert(Milestones.Collect(store.Load().Session!, start.AddMinutes(36)).Count == 0, "Milestone deduplication survives restart");
        Assert(new AppState().Presets.Select(p => p.PixelColor).Distinct().Count() == 4, "Default presets have four distinct themes");
        var legacyPath = Path.Combine(output, "legacy.json");
        File.WriteAllText(legacyPath, "{\"Presets\":[{\"Name\":\"Studying\",\"Minutes\":25,\"IntervalMinutes\":5}]}");
        Assert(new StateStore(legacyPath).Load().Presets[0].PixelColor == "#A67AF2", "Existing presets migrate to named themes");

        var notesSession = new Session { Title = "Lecture", Start = start, End = start.AddHours(1), IntervalMinutes = 10 };
        HedyNotes.Apply(notesSession, "First paragraph.", start.AddMinutes(5));
        Assert(notesSession.HedyBaseline == "First paragraph." && notesSession.HedyIntervalNotes.Count == 0, "Initial Hedy transcript is not guessed into historic intervals");
        HedyNotes.Apply(notesSession, "First paragraph. New discussion.", start.AddMinutes(12));
        Assert(notesSession.HedyIntervalNotes.Count == 1 && notesSession.HedyIntervalNotes[1].Contains("New discussion."), "New transcript text maps to interval by receipt time");
        var saved = notesSession.HedyIntervalNotes[1];
        HedyNotes.Apply(notesSession, "First paragraph. New discussion.", start.AddMinutes(13));
        Assert(notesSession.HedyIntervalNotes[1] == saved, "Repeated Hedy poll does not duplicate notes");
        HedyNotes.Apply(notesSession, "Corrected transcript.", start.AddMinutes(14));
        Assert(notesSession.HedyIntervalNotes[1] == saved && notesSession.HedyTranscript == "Corrected transcript.", "Transcript revisions update full text without duplicating interval notes");
        HedyNotes.Apply(notesSession, "Corrected transcript. Late text.", start.AddHours(2));
        Assert(notesSession.HedyIntervalNotes.Count == 1, "Hedy text after session end is not placed in a false interval");
        notesSession.IntervalNotes[1] = "My own note";
        store.Save(new AppState { Session = notesSession });
        Assert(store.Load().Session!.IntervalNotes[1] == "My own note" && store.Load().Session!.HedyIntervalNotes[1] == saved, "Manual and imported interval notes persist separately");
        var encrypted = SecretStorage.Protect("fake-test-key");
        Assert(encrypted != "fake-test-key" && SecretStorage.Unprotect(encrypted) == "fake-test-key", "Remembered Hedy keys are encrypted for current Windows user");
        Assert(SecretStorage.Unprotect("invalid") == "", "Invalid protected credential is handled safely");
        CheckHedyHttp().GetAwaiter().GetResult();
    }
    private sealed class HedyFixture : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];
        public bool AuthCorrect { get; private set; } = true;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsoluteUri);
            AuthCorrect &= request.Headers.Authorization?.Scheme == "Bearer" && request.Headers.Authorization.Parameter == "fixture-key";
            var json = request.RequestUri.AbsolutePath == "/sessions" ? "{\"data\":[{\"sessionId\":\"abc\",\"title\":\"Lecture\"}]}" : "{\"data\":{\"transcript\":\"A live transcript.\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
    private sealed class HedyFailure : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
    }
    private static async Task CheckHedyHttp()
    {
        var fixture = new HedyFixture();
        using var client = new HedyClient("fixture-key", "EU", fixture);
        var sessions = await client.Sessions(CancellationToken.None);
        var transcript = await client.Transcript(sessions.Single().Id, CancellationToken.None);
        Assert(sessions[0].Title == "Lecture" && transcript == "A live transcript.", "Hedy REST fixture parses session list and detail envelope");
        Assert(fixture.AuthCorrect && fixture.Paths.All(p => p.StartsWith("https://eu-api.hedy.bot/")), "Hedy requests use bearer auth and selected region");
        using var bad = new HedyClient("fixture-key", "US", new HedyFailure());
        var handled = false;
        try { await bad.Sessions(CancellationToken.None); } catch (HttpRequestException e) { handled = e.Message.Contains("401") && !e.Message.Contains("fixture-key"); }
        Assert(handled, "Hedy authentication failures are surfaced without leaking credentials");
    }
    private static void CheckRevisionUi()
    {
        var store = new StateStore(Path.Combine(output, "revision-ui.json"));
        var window = new MainWindow(store);
        window.StartDate.SelectedDate = window.EndDate.SelectedDate = DateTime.Today.AddDays(1);
        window.StartTime.Text = "12:30 PM"; window.EndTime.Text = "2:30 PM";
        Click(window, "Use these times");
        Assert(window.state.Session!.End - window.state.Session.Start == TimeSpan.FromHours(2), "Clock menu schedules 12:30 PM to 2:30 PM");
        window.ThemePicker.SelectedIndex = 3;
        window.TitleInput.Text = "Afternoon study"; Click(window, "Save as new");
        var preset = store.Load().Presets.Last();
        Assert(preset.StartClockMinutes == 750 && preset.EndClockMinutes == 870 && preset.PixelColor == "#A67AF2", "Saved preset retains both clock times and its theme");
        var presetCount = window.state.Presets.Count;
        window.EndTime.Text = "11:30 AM"; Click(window, "Save as new");
        Assert(window.state.Presets.Count == presetCount, "Invalid preset clock range is rejected");
        window.EndTime.Text = "2:30 PM";
        window.PresetPicker.SelectedIndex = 1;
        Assert(window.state.PixelColor == "#1DB954", "Selecting work preset applies green theme");
        window.PresetPicker.SelectedIndex = window.PresetPicker.Items.Count - 1;
        Assert(window.StartTime.Value == TimeSpan.FromMinutes(750) && window.state.PixelColor == "#A67AF2", "Reselecting saved preset restores clock selection and violet theme");
        var clockMenu = window.StartTime.ClockMenu;
        clockMenu.Measure(new Size(256, 370)); clockMenu.Arrange(new Rect(0, 0, 256, 370)); clockMenu.UpdateLayout();
        var bitmap = new RenderTargetBitmap(256, 370, 96, 96, PixelFormats.Pbgra32); bitmap.Render(clockMenu);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(Path.Combine(output, "clock-menu.png"))) encoder.Save(stream);
        Assert(Descendants(clockMenu).OfType<Button>().Count() == 13, "Clock menu provides twelve hour choices and a Set time action");
        var start = DateTimeOffset.Now.AddMinutes(-20);
        window.state.Session = new Session { Title = "Milestone preview", Start = start, End = start.AddMinutes(40), IntervalMinutes = 10 };
        window.UpdateClock(start.AddMinutes(20));
        Assert(window.MilestoneLabel.Text.Contains("HALFWAY") && window.MilestoneLabel.Visibility == Visibility.Visible, "Halfway milestone appears in WPF");
        window.UpdateClock(start.AddMinutes(20).AddMilliseconds(250));
        Assert(window.PixelGrid.Children.OfType<Border>().All(p => ((SolidColorBrush)p.Background).Color == Colors.White), "All timer pixels flash white during milestone pulse");
        Snapshot(window, "milestone", 1204, 840);
        window.UpdateClock(start.AddMinutes(20).AddSeconds(7));
        Assert(window.MilestoneLabel.Visibility == Visibility.Collapsed, "Milestone flash expires after six seconds");
        window.UpdateClock(start.AddMinutes(40));
        Assert(window.CompletionBanner.Visibility == Visibility.Visible && window.PixelGrid.Effect != null, "Completion shows SESSION OVER and glowing pixels");
        Snapshot(window, "completion", 1204, 840);
        Click(window, "Dismiss banner"); window.UpdateClock(start.AddMinutes(41));
        Assert(window.CompletionBanner.Visibility == Visibility.Collapsed, "Completion banner can be dismissed");
        Click(window, "I'm bored →");
        Assert(Descendants(window.StoryArea).OfType<StoryArtwork>().Count() == 1, "Story includes native themed artwork");
        window.Left = 0; window.Top = 0;
        Click(window, "Fullscreen");
        Assert(window.WindowStyle == WindowStyle.None && window.FullscreenButton.Content.Equals("Restore"), "Fullscreen is reachable from permanent menu");
        Click(window, "Restore");
        Assert(window.WindowStyle == WindowStyle.SingleBorderWindow, "Restore brings back native window controls");
        Click(window, "Minimize");
        Assert(window.WindowState == WindowState.Minimized, "Minimize menu action works");
        window.WindowState = WindowState.Normal;
        Click(window, "Compact mode");
        Assert(Descendants(window).OfType<Button>().Any(b => Equals(b.Content, "Exit") && b.Visibility == Visibility.Visible), "Exit remains available in compact mode");
        var closed = false; window.Closed += (_, _) => closed = true;
        Click(window, "Exit");
        Assert(closed && File.Exists(store.FilePath), "Exit menu closes window and persists changes");
    }
}
