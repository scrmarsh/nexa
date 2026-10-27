using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckAlpha019()
    {
        object? Call(object target, string name, params object?[] args) => target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
        void Capture(Window w, string name)
        {
            w.UpdateLayout(); var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)w.ActualWidth, (int)w.ActualHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(w); var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
        }
        SettingsDialog Open(MainWindow w, string section) { w.OpenMainSettings(section); Pump(TimeSpan.FromMilliseconds(70)); return w.OwnedWindows.OfType<SettingsDialog>().Single(); }
        TextBox Name(SettingsDialog s) => Descendants(s).OfType<TextBox>().Single(t => t.Name == "SessionName");
        void Click(CheckBox b, bool value) { b.IsChecked = value; b.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); }
        Assert(App.AppVersion == "0.1.11" && App.VersionLabel == "Nexa 0.1.11 Alpha", "Current version and shared Alpha label");
        Assert(App.ReleaseLabel("0.1.10") == "Nexa 0.1.10 Alpha", "Two-digit future Alpha patches keep their full version");
        Assert(ReleaseNotes.All.Select(r => r.Version).SequenceEqual(Enumerable.Range(1, 11).Reverse().Select(n => "0.1." + n)), "History includes all eleven ordered Alpha releases");
        Assert(ReleaseNotes.All.All(r => !string.IsNullOrWhiteSpace(r.Title) && !string.IsNullOrWhiteSpace(r.Summary)), "Every release has a title and description");
        Assert(ReleaseNotes.All.Last().Title == "Bug Fixes" && ReleaseNotes.All.Last().Summary == "General bug fixes and stability improvements during early Alpha development.", "Restored early release uses only the documented description");
        var store = new StateStore(Path.Combine(output, "alpha019.json"));
        var initial = new AppState { Notes = "Preserve notes", WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Notes", "notes"), WidgetLibrary.Create("Clock")], Session = new Session { Title = "Live class", Start = DateTimeOffset.Now.AddMinutes(-8).AddMilliseconds(123), End = DateTimeOffset.Now.AddMinutes(28).AddMilliseconds(456), IntervalMinutes = 7, HedyTranscript = "Saved transcript", IntervalNotes = new() { [0] = "Retain interval" } } };
        store.Save(initial);
        // Synthetic opaque credential; never an actual account or service request.
        initial.HedyProtectedKey = "synthetic-opaque-existing-credential";
        var workspace = initial.Workspace!; var dashboard = Continuity.Current(workspace, store.UserId);
        dashboard.History.Add(Continuity.Clone(dashboard.Current)); dashboard.History[0].SessionId = Guid.NewGuid(); dashboard.History[0].Data.Notes = "Older notes";
        var second = Continuity.NewDashboard("Other space", "Study", new DashboardData { Notes = "Other notes" }, store.UserId); workspace.Dashboards.Add(second);
        store.Save(initial); var profile = store.LoadProfile(); profile.DisplayName = "Existing student"; profile.OnboardingCompleted = true; profile.UseCases = ["Classes"]; store.SaveProfile(profile);
        var start = initial.Session!.Start; var end = initial.Session.End;
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(90));
        Assert(Descendants(home).OfType<TextBlock>().Any(t => t.Text == App.VersionLabel), "Home shows centralized version");
        foreach (var method in new[] { "OpenAbout", "OpenChangelog" })
        {
            Call(home, method); Pump(TimeSpan.FromMilliseconds(80)); var dialog = home.OwnedWindows.OfType<NexaDialog>().Single();
            Assert(Descendants(dialog).OfType<TextBlock>().Any(t => t.Text.StartsWith(App.VersionLabel)), method + " shows current Alpha label");
            Capture(dialog, "alpha019-" + method);
            if (method == "OpenChangelog") Assert(Descendants(dialog).OfType<TextBlock>().Any(t => t.Text.StartsWith("Nexa 0.1.1 Alpha")), "Visible history includes restored Alpha release");
            dialog.Close();
        }
        var splash = new SplashWindow(); splash.Show(); Pump(TimeSpan.FromMilliseconds(60)); Assert(Descendants(splash).OfType<TextBlock>().Any(t => t.Text == App.VersionLabel), "Splash shows centralized version"); Capture(splash, "alpha019-splash"); splash.Close();
        Call(home, "OpenSettings", "Session"); Pump(TimeSpan.FromMilliseconds(80)); var hs = home.OwnedWindows.OfType<SettingsDialog>().Single();
        var homeSession = home.Saved.Session!;
        Assert(Name(hs).Text == "Live class" && !Descendants(hs).OfType<TextBlock>().Any(t => t.Text == "No active session"), "Home settings recognizes saved active session");
        Name(hs).Text = "Saved from Home"; hs.Close();
        Assert(ReferenceEquals(home.Saved.Session, homeSession) && homeSession.Start == start && homeSession.End == end && homeSession.StoppedAt == null, "Home close preserves exact session object and timestamps");
        Assert(store.Load().Session!.Title == "Saved from Home", "Closing Home settings immediately flushes valid pending edits"); home.Close();
        var window = new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(120));
        var session = window.state.Session!; var sessionId = Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId;
        var history = JsonSerializer.Serialize(Continuity.Current(window.state.Workspace!, store.UserId).History);
        var widgets = JsonSerializer.Serialize(window.state.Widgets);
        var settings = Open(window, "Session"); var remaining = session.Remaining(DateTimeOffset.Now);
        Assert(ReferenceEquals(session, window.state.Session) && Name(settings).Text == session.Title, "Dashboard Settings binds to live session");
        Capture(settings, "alpha019-settings-session");
        Pump(TimeSpan.FromMilliseconds(1100));
        Assert(session.Remaining(DateTimeOffset.Now) < remaining && session.Start == start && session.End == end && session.StoppedAt == null, "Session clock continues while Settings is open");
        Name(settings).Text = "Renamed in Settings";
        Assert(store.Load().Session!.Title == "Saved from Home", "Text is debounced before disk writes"); Pump(TimeSpan.FromMilliseconds(620));
        Assert(store.Load().Session!.Title == "Renamed in Settings" && window.TitleInput.Text == "Renamed in Settings", "Session name autosaves and updates dashboard");
        Assert(ReferenceEquals(window.state.Session, session) && session.Start == start && session.End == end && session.IntervalMinutes == 7 && session.StoppedAt == null, "Name changes retain exact timestamps and custom interval");
        Name(settings).Text = ""; settings.FlushPendingChanges(); Assert(session.Title == "Renamed in Settings", "Invalid name retains last valid setting"); Name(settings).Text = session.Title; settings.FlushPendingChanges();
        var endClock = Descendants(settings).OfType<ClockPicker>().Last(); endClock.Value = end.LocalDateTime.TimeOfDay.Add(TimeSpan.FromMinutes(2)); settings.FlushPendingChanges();
        var changedEnd = session.End; Assert(changedEnd > end && session.Start == start && ReferenceEquals(window.state.Session, session) && session.StoppedAt == null, "Explicit time edit preserves identity and unedited start precision");
        settings.SelectSection("Appearance"); Pump(TimeSpan.FromMilliseconds(50));
        var volume = Descendants(settings).OfType<Slider>().Single(); var originalVolume = store.LoadProfile().Volume;
        for (var i = 1; i <= 20; i++) volume.Value = .30 + i * .02;
        Assert(store.LoadProfile().Volume == originalVolume, "Rapid slider changes do not write every tick"); Pump(TimeSpan.FromMilliseconds(620));
        Assert(Math.Abs(store.LoadProfile().Volume - .70) < .001, "Slider autosaves final value after debounce");
        var sounds = Descendants(settings).OfType<CheckBox>().Single(c => Equals(c.Content, "Interface sounds")); Click(sounds, true);
        var nudges = Descendants(settings).OfType<CheckBox>().Single(c => c.Content.ToString()!.StartsWith("Playful focus nudges")); Click(nudges, true);
        var theme = Descendants(settings).OfType<ComboBox>().Single(c => c.Items.Contains(AppTheme.Violet)); theme.SelectedItem = AppTheme.Violet; settings.FlushPendingChanges();
        Assert(store.LoadProfile().Sounds && store.LoadProfile().FocusNudges && store.LoadAppTheme() == AppTheme.Violet, "Sound, nudge and theme choices autosave");
        settings.SelectSection("Widgets"); Pump(TimeSpan.FromMilliseconds(30)); Descendants(settings).OfType<ComboBox>().Single().SelectedItem = "Circle"; settings.FlushPendingChanges();
        settings.SelectSection("Profile"); Pump(TimeSpan.FromMilliseconds(30)); var text = Descendants(settings).OfType<TextBox>().ToArray(); text[0].Text = "Updated student"; text[1].Text = "42"; settings.FlushPendingChanges();
        profile = store.LoadProfile(); Assert(profile.DisplayName == "Updated student" && profile.DefaultMinutes == 42 && profile.OnboardingCompleted && profile.Sounds && profile.Theme == AppTheme.Violet, "Profile autosaves while retaining onboarding, sound and theme preferences");
        Assert(window.state.PixelShape == "Circle" && store.LoadProfile().Volume == .70, "Profile autosave preserves widget and volume preferences");
        text[1].Text = "0"; settings.FlushPendingChanges(); Assert(store.LoadProfile().DefaultMinutes == 42, "Invalid duration retains last valid profile value");
        foreach (var section in new[] { "Appearance", "Session", "Widgets", "Profile", "Integrations" })
        {
            settings.SelectSection(section); Pump(TimeSpan.FromMilliseconds(20)); Assert(!Descendants(settings).OfType<Button>().Any(b => new[] { "Apply appearance", "Apply session", "Apply Space appearance", "Save profile", "Save Hedy" }.Contains(b.Content?.ToString())), section + " requires no general Save/Apply button");
        }
        settings.SelectSection("Integrations"); Descendants(settings).OfType<ComboBox>().Single(c => c.Items.Contains("EU")).SelectedItem = "EU"; settings.FlushPendingChanges(); Assert(store.Load().HedyRegion == "EU", "Hedy region autosaves without connecting to a service");
        Assert(store.Load().HedyProtectedKey == "synthetic-opaque-existing-credential" && !File.ReadAllText(store.FilePath).Contains("synthetic-opaque-existing-credential"), "Region-only autosave preserves existing protected key and excludes it from dashboard data");
        settings.SelectSection("Session"); Name(settings).Text = "Flush before switch";
        var candidate = window.CopyWorkspaceState().Workspace!; candidate.ActiveDashboardId = second.DashboardId;
        Assert(window.CommitWorkspace(candidate) && !settings.IsVisible && window.state.Session == null && window.state.Notes == "Other notes", "Switch flushes old Settings and closes it before rebinding live state");
        var outgoing = window.state.Workspace!.Dashboards.Single(d => d.DashboardId == dashboard.DashboardId);
        Assert(outgoing.Current.SessionId == sessionId && outgoing.Current.Data.Session!.Title == "Flush before switch" && outgoing.Current.Data.Session.Start == start && outgoing.Current.Data.Session.End == changedEnd, "Pending edits saved to outgoing session without a new identity");
        Assert(JsonSerializer.Serialize(outgoing.History) == history && JsonSerializer.Serialize(outgoing.Current.Data.Widgets) == widgets && outgoing.Current.Data.Notes == "Preserve notes" && outgoing.Current.Data.Session!.HedyTranscript == "Saved transcript" && outgoing.Current.Data.Session.IntervalNotes[0] == "Retain interval", "History, layouts, notes and Hedy content survive Settings and switching");
        var untimed = Open(window, "Session"); Assert(Descendants(untimed).OfType<TextBlock>().Any(t => t.Text == "No active session"), "Untimed Space reports actual absence of a session");
        Descendants(untimed).OfType<ComboBox>().Single(c => c.Items.Contains(SpaceBackgroundMode.SolidColor)).SelectedItem = SpaceBackgroundMode.SolidColor; untimed.Close();
        Assert(window.state.Session == null && store.Load().Background.Mode == SpaceBackgroundMode.SolidColor, "Untimed Space appearance autosaves without creating a session");
        candidate = window.CopyWorkspaceState().Workspace!; candidate.ActiveDashboardId = dashboard.DashboardId; window.CommitWorkspace(candidate);
        // Close flushes a rapid slider change before the debounce fires.
        settings = Open(window, "Appearance"); Descendants(settings).OfType<Slider>().Single().Value = .41; settings.Close(); window.Close();
        var reopened = new MainWindow(store); reopened.Show(); Pump(TimeSpan.FromMilliseconds(90));
        Assert(reopened.state.Session!.Start == start && reopened.state.Session.End == changedEnd && reopened.state.Session.Title == "Flush before switch" && reopened.state.Session.StoppedAt == null && store.LoadProfile().Volume == .41, "Restart restores exact session and just-closed preference edits");
        Assert(reopened.state.Workspace!.Dashboards.Count == 2 && store.LoadProfile().DisplayName == "Updated student" && store.LoadProfile().OnboardingCompleted, "Existing Spaces and profile remain intact after reopening");
        // An owner dashboard is sufficient even when a caller omits the optional space argument.
        settings = new SettingsDialog(reopened, store, new AppState(), null, "Session"); settings.Show(); Pump(TimeSpan.FromMilliseconds(30)); Assert(Name(settings).Text == "Flush before switch", "Settings resolves same-user dashboard owner instead of a detached state"); settings.Close();
        // Failed filesystem writes retain pending edits and prevent silent loss on close.
        settings = Open(reopened, "Session"); Name(settings).Text = "Retry this edit";
        var closeAttempts = 0; settings.Closing += (_, _) => closeAttempts++;
        using (var locked = new FileStream(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert(!settings.FlushPendingChanges(), "Actual locked-file save failure is reported");
            settings.CloseWithFade();
            for (var wait = 0; closeAttempts == 0 && wait < 20; wait++) Pump(TimeSpan.FromMilliseconds(100));
            Assert(closeAttempts == 1 && settings.IsVisible && settings.Opacity == 1 && reopened.state.Session!.Title == "Retry this edit", "Failed animated dismissal reaches Closing and keeps Settings visible for retry");
        }
        settings.CloseWithFade();
        for (var wait = 0; settings.IsVisible && wait < 20; wait++) Pump(TimeSpan.FromMilliseconds(100));
        Assert(closeAttempts == 2 && !settings.IsVisible && store.Load().Session!.Title == "Retry this edit", "Retrying the same animated close saves edits after filesystem recovery");
        var stoppedAt = DateTimeOffset.Now; reopened.state.Session!.StoppedAt = stoppedAt; reopened.SaveNow(); settings = Open(reopened, "Session");
        Assert(!Descendants(settings).OfType<TextBox>().Any(t => t.Name == "SessionName") && Descendants(settings).OfType<TextBlock>().Any(t => t.Text == "No active session"), "Stopped session shows an empty state without stale edit controls"); settings.Close();
        Assert(reopened.state.Session.StoppedAt == stoppedAt && store.Load().Session!.StoppedAt == stoppedAt, "Settings never restarts a stopped session");
        reopened.Close();
        home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(50));
        // Simulate a local history update while Home is open; Settings must use current saved data.
        var historyUpdate = store.Load(); Continuity.Current(historyUpdate.Workspace!, store.UserId).History[0].Data.Notes = "Updated historical notes"; store.Save(historyUpdate);
        Call(home, "OpenSettings", "Appearance"); hs = home.OwnedWindows.OfType<SettingsDialog>().Single();
        var soundSetting = Descendants(hs).OfType<CheckBox>().Single(b => Equals(b.Content, "Interface sounds")); var soundChoice = soundSetting.IsChecked != true; Click(soundSetting, soundChoice);
        home.OpenDashboard(second.DashboardId); var resumed = (MainWindow)Application.Current.MainWindow; Pump(TimeSpan.FromMilliseconds(350));
        var savedAfterNav = store.Load(); var firstAfterNav = savedAfterNav.Workspace!.Dashboards.Single(d => d.DashboardId == dashboard.DashboardId);
        Assert(savedAfterNav.Workspace.ActiveDashboardId == second.DashboardId && resumed.state.Workspace!.ActiveDashboardId == second.DashboardId && resumed.state.InterfaceSoundsEnabled == soundChoice && firstAfterNav.Current.Data.Session!.Title == "Retry this edit", "Home navigation flushes outgoing Settings before loading a different Space");
        Assert(firstAfterNav.History[0].Data.Notes == "Updated historical notes" && firstAfterNav.Current.Data.Session!.StoppedAt == stoppedAt, "Home Settings refreshes current saved state and cannot overwrite newer history");
        resumed.Close();
    }
}
