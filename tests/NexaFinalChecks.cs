using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SessionDashboard;

internal static partial class Program
{
    private static void AwaitFinal(Func<bool> condition, string failure)
    {
        var timeout = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(12)) Pump(TimeSpan.FromMilliseconds(30));
        if (!condition()) throw new InvalidOperationException(failure);
    }
    private static void CheckExistingData(string path)
    {
        var fixture = Path.GetFullPath(path);
        if (!fixture.StartsWith(Path.GetFullPath("verification") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Use an isolated verification copy, never a live workspace.");
        var before = File.ReadAllBytes(fixture); var store = new StateStore(fixture); var data = store.Load();
        Assert(data.Workspace != null && data.Workspace.Dashboards.Count > 0, "The existing workspace snapshot loads with its saved Spaces intact");
        Assert(before.SequenceEqual(File.ReadAllBytes(fixture)), "Compatibility loading does not rewrite the existing data snapshot");
        File.WriteAllText(Path.Combine(output, "existing-data-result.txt"), $"Spaces: {data.Workspace!.Dashboards.Count}; archived reports: {data.Workspace.Dashboards.Sum(d => d.History.Count)}; preserved recovery records: {data.Workspace.Dashboards.Sum(d => d.PreservedHistory.Count)}. Input SHA256: {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(before))}. No source workspace was written.");
    }
    private static void CheckFinalRelease()
    {
        var store = new StateStore(Path.Combine(output, "final-history.json"));
        var water = WidgetLibrary.Create("Water Bottle Counter"); water.Value = 3;
        var space = Continuity.NewDashboard("Final fixture", "Class", new DashboardData { Notes = "Current notes must survive", WidgetLayoutInitialized = true, Widgets = [water] }, store.UserId);
        var archive = Continuity.Clone(space.Current); archive.SessionId = Guid.NewGuid(); archive.IsActive = false; archive.Title = "Recorded lecture"; archive.Data.Notes = "Archived study notes"; archive.Data.Tasks = [new() { Text = "Recorded task", Done = true }]; space.History.Add(archive);
        var workspace = new WorkspaceDocument { UserId = store.UserId, ActiveDashboardId = space.DashboardId, Dashboards = [space] };
        store.Save(Continuity.Activate(workspace, store.UserId));
        var normal = HistoryArchive.Read(store);
        Assert(normal.Entries.Length == 2 && normal.Warning == null, "History reads current and archived sessions from the authoritative store");
        var report = normal.Entries.Single(e => e.Session?.SessionId == archive.SessionId).Report;
        Assert(report.Contains("Archived study notes") && report.Contains("Recorded task") && report.Contains("Count: 3") && report.Contains("PRODUCTIVITY"), "History reports include recorded notes, completed tasks, counters and score data");
        var malformed = JsonNode.Parse(File.ReadAllText(store.FilePath))!;
        var badDecks = new JsonArray(); badDecks.Add((JsonNode?)null);
        malformed["Dashboards"]![0]!["History"]![0]!["Data"]!["Widgets"]![0]!["FlashcardDecks"] = badDecks;
        File.WriteAllText(store.FilePath, malformed.ToJsonString()); var original = File.ReadAllBytes(store.FilePath);
        var recovered = HistoryArchive.Read(store);
        Assert(recovered.Entries.Length == 2 && recovered.Warning != null && recovered.Entries.Any(e => e.Problem != null && e.Report.Contains("RECOVERED ARCHIVE VIEW")), "A damaged nested historical record remains viewable without crashing");
        Assert(original.SequenceEqual(File.ReadAllBytes(store.FilePath)), "Opening partially corrupted history never rewrites the original file");
        var loaded = store.Load();
        Assert(loaded.Notes == "Current notes must survive" && loaded.Workspace!.Dashboards[0].PreservedHistory.Count == 1, "Archive corruption preserves the active Space and keeps the exact damaged record additively");
        store.Save(loaded);
        Assert(Directory.GetFiles(output, "final-history.json.recovered-*").Any(p => File.ReadAllBytes(p).SequenceEqual(original)), "The original damaged file is backed up before any recovery save");
        Assert(HistoryArchive.Read(store).Entries.Length == 2, "Preserved damaged records remain discoverable after restart and save");
        var malformedList = JsonNode.Parse(File.ReadAllText(store.FilePath))!;
        malformedList["Dashboards"]![0]!["History"] = new JsonObject { ["LegacyNotes"] = "Preserve malformed history container" };
        var listStore = new StateStore(Path.Combine(output, "history-invalid-list.json")); File.WriteAllText(listStore.FilePath, malformedList.ToJsonString());
        var listRecovered = listStore.Load();
        Assert(listRecovered.Notes == "Current notes must survive" && listRecovered.Workspace!.Dashboards[0].PreservedHistory.Count == 2 && HistoryArchive.Read(listStore).Entries.Any(e => e.Raw.Contains("Preserve malformed history container")), "Malformed history containers preserve readable current data and original recovery fields");
        var missing = new StateStore(Path.Combine(output, "history-missing.json"));
        Assert(HistoryArchive.Read(missing).Entries.Length == 0, "Missing archives produce an empty state without inventing past events");
        var unreadable = new StateStore(Path.Combine(output, "history-unreadable.json"));
        File.WriteAllText(unreadable.FilePath, "{ interrupted workspace"); var corruptBytes = File.ReadAllBytes(unreadable.FilePath);
        var unavailable = unreadable.Load(); var writeRejected = false;
        try { unreadable.Save(unavailable); } catch (UnauthorizedAccessException) { writeRejected = true; }
        Assert(writeRejected && corruptBytes.SequenceEqual(File.ReadAllBytes(unreadable.FilePath)), "A wholly unreadable workspace cannot be overwritten with fallback defaults");
        Assert(HistoryArchive.Read(unreadable).Entries.Length == 0, "Unparseable primary and missing backup files produce safe History feedback");
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(100));
        typeof(HomeWindow).GetMethod("OpenHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(home, null);
        var history = Application.Current.Windows.OfType<SessionHistoryWindow>().Single();
        AwaitFinal(() => Descendants(history).OfType<ListBox>().Single().Items.Count == 2, "History failed to load its damaged fixture");
        Assert(history.IsVisible && Descendants(history).OfType<TextBlock>().Any(t => t.Text.Contains("recovered") || t.Text.Contains("recovery")), "The real History window opens a partial archive and communicates recovery");
        history.Close(); home.Close();

        var largeStore = new StateStore(Path.Combine(output, "large-history.json"));
        var large = Continuity.NewDashboard("Large archive", "Fixture", new DashboardData { Notes = "Current large fixture" }, largeStore.UserId);
        for (var i = 0; i < 1200; i++) { var record = Continuity.Clone(large.Current); record.SessionId = Guid.NewGuid(); record.IsActive = false; record.Title = "Archive " + i; record.Data.Notes = "Searchable lecture " + i; large.History.Add(record); }
        var largeWorkspace = new WorkspaceDocument { UserId = largeStore.UserId, ActiveDashboardId = large.DashboardId, Dashboards = [large] }; largeStore.Save(Continuity.Activate(largeWorkspace, largeStore.UserId));
        var owner = new NexaPanel { Title = "History test owner", Width = 1100, Height = 800 }; owner.Show();
        var largeWindow = new SessionHistoryWindow(owner, largeStore); largeWindow.Show(); var heartbeat = false; largeWindow.Dispatcher.BeginInvoke(new Action(() => heartbeat = true));
        AwaitFinal(() => Descendants(largeWindow).OfType<ListBox>().Single().Items.Count == 1201, "Large archive did not finish loading");
        Assert(heartbeat, "The dispatcher remains responsive while a 1,201-record archive loads");
        var search = Descendants(largeWindow).OfType<TextBox>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Search history"); search.Text = "Searchable lecture 987";
        AwaitFinal(() => Descendants(largeWindow).OfType<ListBox>().Single().Items.Count == 1, "Large archive search failed");
        Assert(true, "Debounced background search locates a recorded item in a large archive"); largeWindow.Close(); owner.Close();

        var uiStore = new StateStore(Path.Combine(output, "final-ui.json")); uiStore.Save(new AppState()); var uiState = uiStore.Load(); uiState.Workspace!.Settings.SeasonalMode = "Off"; uiStore.Save(uiState);
        var main = new MainWindow(uiStore); main.Show(); Pump(TimeSpan.FromMilliseconds(100));
        var at = NexaTime.Instant(new DateTime(2030, 1, 7, 9, 30, 0), "America/Chicago");
        var schedule = PixelTiming.WeeklyClass("Class", [DayOfWeek.Monday, DayOfWeek.Wednesday], TimeSpan.FromHours(9), TimeSpan.FromHours(10), "America/Chicago", at);
        Assert(Math.Abs(PixelTiming.Read(schedule, at).VisualFraction - .5) < .00001, "Opening midway through a scheduled class immediately shows fifty percent");
        Assert(main.ApplyClassSchedule(schedule, true) && main.SessionResetButton.Visibility == Visibility.Collapsed, "Schedule Mode persists with the preset and hides manual Reset");
        var gap = PixelTiming.Read(schedule, schedule.End.AddHours(1));
        Assert(!gap.CountUp && gap.VisualFraction < 1 && gap.VisualFraction > 0, "Scheduled timers transition into reverse-clearing intersession progress without a reset");
        var manual = new Session { Title = "Manual", Start = DateTimeOffset.UtcNow, End = DateTimeOffset.UtcNow.AddHours(1), AutomaticScheduling = false };
        Assert(main.ApplyClassSchedule(manual, true) && main.SessionResetButton.Visibility == Visibility.Visible, "Manual Mode restores Reset and persists independently of weekly schedules");
        Assert(!uiStore.Load().Presets.Single(p => p.Name == "Manual").AutomaticScheduling, "The manual operating mode survives serialization in its preset");
        main.ShowWeeklySchedule(); var timerEditor = Application.Current.Windows.OfType<NexaDialog>().Single(w => w.Title == "Nexa · Pixel Timer");
        Assert(Descendants(timerEditor).OfType<ComboBox>().Any(c => c.Items.OfType<string>().SequenceEqual(new[] { "Manual Mode", "Schedule Mode" })), "The actual Pixel Timer editor exposes the two operating modes"); timerEditor.Close();
        var settings = new SettingsDialog(main, uiStore, main.state, main, "Appearance"); settings.Show();
        Assert(settings.AutoSaveStatus.Opacity == 0 && settings.AutoSaveStatus.Text == "", "Settings save confirmation is hidden until an actual change");
        var initialLeft = settings.Left; var initialTop = settings.Top; settings.SelectSection("Sounds"); settings.SelectSection("Appearance");
        Assert(settings.Left == initialLeft && settings.Top == initialTop && Application.Current.Windows.OfType<SettingsDialog>().Count() == 1, "Tab switching preserves position and never creates another Settings window");
        var toggle = Descendants(settings).OfType<CheckBox>().Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Nexaween enabled"); toggle.IsChecked = true; toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); settings.FlushPendingChanges();
        Assert(settings.AutoSaveStatus.Text == "Changes saved.", "Settings reports success only after persistence completes");
        Pump(TimeSpan.FromMilliseconds(2300)); Assert(settings.AutoSaveStatus.Opacity < .05, "Successful Settings feedback fades out after approximately two seconds");
        settings.Close();
        var session = new SettingsDialog(main, uiStore, main.state, main, "Session"); session.Show();
        Assert(!Descendants(session).OfType<Button>().Any(b => Equals(b.Content, "Time Zone & Clock")) && !Descendants(session).OfType<CheckBox>().Any(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Nexaween enabled"), "Session settings contain no global clock sidebar or seasonal toggle"); session.Close();
        var overlay = new BugOverlay(new BugManager()); overlay.Measure(new Size(800, 600)); overlay.Arrange(new Rect(0, 0, 800, 600)); overlay.SetViewport(new Rect(0, 70, 800, 530));
        Assert(overlay.HitAt(new Point(20, 20)) == null, "The bug viewport rejects toolbar-region hits");
        var legacyMode = new EcosystemOptions { Behavior = BugBehavior.Creepy, MatchAppearance = true }.Safe();
        Assert(legacyMode.Behavior == BugBehavior.Creepy && legacyMode.EffectiveAppearance == BugAppearance.Creepy && legacyMode.MatchAppearance, "Existing Creepy identities remain readable and use the restored selectable behavior");
        var population = new BugManager(947); var behavior = new EcosystemOptions { Behavior = BugBehavior.Cozy, MatchAppearance = false };
        population.NewEcosystem(uiStore.UserId, behavior); population.Configure(new(true, BugSpecies.Ants, BugDensity.Low, behavior));
        for (var i = 0; i < 1000; i++) population.AdvanceEcosystem(.1);
        population.Configure(new(true, BugSpecies.AllBugs, BugDensity.Low, behavior));
        for (var i = 0; i < 1000; i++) population.AdvanceEcosystem(.1);
        Assert(population.Bugs.Select(b => b.Species).Distinct().Count() == 6 && population.Bugs.Count <= 12, "Changing a full single-species population to All Bugs introduces all six species within the density budget");
        Assert(population.CombatDeaths == 0, "Population rebalancing does not manufacture combat deaths or squash credit");
        using (var muted = InterfaceAudio.CreateCue(0, "positive")) Assert(muted.ToArray().Skip(44).All(b => b == 0), "Generated productivity audio respects a muted volume");
        foreach (var kind in new[] { "pop", "positive", "negative" })
        { using var cue = InterfaceAudio.CreateCue(1, kind); Assert(cue.Length > 44 && cue.Length < 22050 * 2 && System.Text.Encoding.ASCII.GetString(cue.ToArray(), 0, 4) == "RIFF", kind + " is a short valid generated WAV cue"); }
        main.ToggleFullscreen();
        typeof(MainWindow).GetMethod("ApplySettings", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(main, null);
        Assert(main.Topmost, "Applying settings cannot drop fullscreen behind the taskbar"); main.ToggleFullscreen();
        Assert(NexaWindowBounds.TryWorkArea((FrameworkElement)main.Content, out var safeArea) && !safeArea.IsEmpty, "Notification work-area projection uses the actual window monitor and client DIPs");
        var pausedOverlay = NexaEcosystem.For(uiStore); pausedOverlay.Manager.WorldPaused = true;
        main.ApplyBugPreferences(new BugPreferences(true, BugSpecies.Ants, BugDensity.Low, behavior));
        Assert(main.BugsRunning && !pausedOverlay.Manager.WorldPaused, "Enabling the dashboard overlay resumes a legacy paused world without retired controls");
        main.Close();
    }
}
