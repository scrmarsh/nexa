using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static void Throws<T>(Action action, string description) where T : Exception
    {
        try { action(); } catch (T) { Assert(true, description); return; }
        throw new InvalidOperationException("FAIL: " + description);
    }
    private static void CheckContinuityModels()
    {
        var path = Path.Combine(output, "continuity.json");
        var store = new StateStore(path, "user-a");
        var state = new AppState { Notes = "Private lecture notes", Tasks = [new() { Text = "Read chapter" }, new() { Text = "Finished", Done = true }],
            HedyProtectedKey = "protected-test-secret", ShowNotes = false, PixelColor = "#A67AF2" };
        state.Session = new Session { Title = "Networking", Start = DateTimeOffset.Now, End = DateTimeOffset.Now.AddHours(1),
            IntervalNotes = new() { [0] = "An interval note" }, HedyTranscript = "Saved transcript" };
        store.Save(state);
        var dashboard = Continuity.Current(state.Workspace!, store.UserId);
        var dashboardId = dashboard.DashboardId; var oldSessionId = dashboard.Current.SessionId;
        Assert(!File.ReadAllText(path).Contains("protected-test-secret") && store.Load().HedyProtectedKey == "protected-test-secret", "Dashboard and history payloads exclude credentials; separate account credential round-trips");
        for (var i = 0; i < 8; i++) { state.Notes = "Private lecture notes " + i; store.Save(state); }
        Assert(dashboard.Current.Snapshots.Count == 3, "Recovery snapshots are capped at three generations per current dashboard");
        Continuity.Reset(dashboard);
        Assert(dashboard.DashboardId == dashboardId && dashboard.History.Single().SessionId == oldSessionId && dashboard.History.Single().WasReset, "Reset archives current content and preserves dashboard identity");
        Assert(dashboard.Current.Data.Notes == "" && dashboard.Current.Data.Tasks.Count == 0 && dashboard.Current.Data.Session == null && !dashboard.Current.Data.ShowNotes && dashboard.Current.Data.PixelColor == "#A67AF2", "Reset clears session data while preserving colors and panel layout");
        Assert(dashboard.History.Single().Snapshots.Count == 0 && dashboard.History.Single().Data.Notes.EndsWith("7"), "Archived sessions retain final content without redundant snapshots");
        var historyJson = JsonSerializer.Serialize(dashboard.History.Single().Data);
        dashboard.Current.Data.Notes = "Current notes";
        dashboard.Current.Data.Tasks.Add(new() { Text = " read chapter ", Done = true });
        var currentColor = dashboard.Current.Data.PixelColor = "#123456";
        Continuity.Merge(dashboard, oldSessionId, "user-a", false);
        Assert(dashboard.Current.Data.Notes.Contains("Current notes") && dashboard.Current.Data.Notes.Contains("Private lecture notes 7") && dashboard.Current.Data.Notes.Contains("An interval note") && dashboard.Current.Data.Notes.Contains("Saved transcript"), "Merge retains current notes and imports labelled historical notes and transcript");
        Assert(dashboard.Current.Data.Tasks.Count == 1 && dashboard.Current.Data.PixelColor == currentColor && dashboard.Current.MergedFrom.Contains(oldSessionId), "Merge deduplicates tasks and preserves layout with provenance");
        var mergedNotes = dashboard.Current.Data.Notes;
        Continuity.Merge(dashboard, oldSessionId, "user-a", false);
        Assert(dashboard.Current.Data.Notes == mergedNotes && JsonSerializer.Serialize(dashboard.History[0].Data) == historyJson, "Repeated merges do not duplicate content or mutate historical data");
        Continuity.Continue(dashboard, oldSessionId, "user-a");
        Assert(dashboard.Current.ContinuedFrom == oldSessionId && dashboard.Current.SessionId != oldSessionId && dashboard.Current.Data.Notes == "Private lecture notes 7" && dashboard.History.Count == 2, "Continue makes editable copy and archives displaced current session");
        dashboard.Current.Data.Notes = "Edited continuation";
        Assert(dashboard.History[0].Data.Notes == "Private lecture notes 7", "Continued session never aliases its history data");
        var configText = Continuity.Export(dashboard, false);
        Assert(!configText.Contains("Edited continuation") && !configText.Contains("Saved transcript") && !configText.Contains("user-a") && !configText.Contains("protected-test-secret"), "Configuration-only export excludes private session data, user IDs and secrets");
        var configuration = Continuity.ReadExport(configText);
        var imported = Continuity.Import(state.Workspace!, configuration, "user-a", false);
        Assert(imported.DashboardId != dashboardId && imported.UserId == "user-a" && imported.Current.Data.PixelColor == dashboard.Current.Data.PixelColor && imported.Current.Data.Notes == "", "Configuration import creates owned dashboard with new identity and retained configuration");
        var privateFile = Continuity.ReadExport(Continuity.Export(dashboard, true));
        Throws<InvalidOperationException>(() => Continuity.Import(state.Workspace!, privateFile, "user-a", false), "Private session import requires explicit acceptance");
        Assert(Continuity.Import(state.Workspace!, privateFile, "user-a", true).Current.Data.Notes == "Edited continuation", "Explicit private data import preserves session content");
        Throws<InvalidDataException>(() => Continuity.ReadExport(configText.Replace("\"FormatVersion\": 1", "\"FormatVersion\": 999")), "Unsupported export versions rejected");
        Throws<JsonException>(() => Continuity.ReadExport(configText.Replace("\"IncludesSessionData\": false", "\"Password\": \"secret\", \"IncludesSessionData\": false")), "Unknown credential fields in import are rejected");
        Throws<InvalidDataException>(() => Continuity.ReadExport(configText.Replace("\"Tasks\": []", "\"Tasks\": null")), "Malformed collection data rejected before import");
        Throws<UnauthorizedAccessException>(() => Continuity.Current(state.Workspace!, "user-b"), "Another user cannot query the workspace");
        Throws<UnauthorizedAccessException>(() => Continuity.Continue(dashboard, oldSessionId, "user-b"), "Another user cannot continue history");
        Throws<UnauthorizedAccessException>(() => Continuity.Merge(dashboard, oldSessionId, "user-b", false), "Another user cannot merge history");
        Throws<UnauthorizedAccessException>(() => new StateStore(path, "user-b").Load(), "Another user's store cannot load existing autosaves");
        Throws<InvalidOperationException>(() => Continuity.Continue(imported, oldSessionId, "user-a"), "History from a different dashboard cannot be continued");
        state = Continuity.Activate(state.Workspace!, "user-a", state.HedyRegion, state.HedyProtectedKey);
        state.Workspace!.CleanShutdown = false; store.Save(state);
        var recovered = store.Load();
        Assert(recovered.Notes == "Edited continuation" && store.Warning!.Contains("restored your last autosaved"), "Unclean shutdown restores latest saved data with recovery message");
        recovered.Workspace!.CleanShutdown = true; store.Save(recovered); store.Load();
        Assert(store.Warning == null, "Clean shutdown suppresses crash recovery message");
        var damaged = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        var activeDashboard = damaged["Dashboards"]!.AsArray().Single(d => d!["DashboardId"]!.GetValue<Guid>() == recovered.Workspace.ActiveDashboardId)!;
        activeDashboard["Current"]!["Data"]!["Notes"] = null;
        File.WriteAllText(path, damaged.ToJsonString());
        Assert(store.Load().Notes == "Edited continuation" && store.Warning!.Contains("snapshot"), "Damaged current state recovers the latest valid snapshot before older file backup");
    }
    private static void CheckContinuityUi()
    {
        var store = new StateStore(Path.Combine(output, "continuity-ui.json"));
        var window = new MainWindow(store) { WindowStartupLocation=WindowStartupLocation.Manual, Left=-10000, Top=-10000, ShowActivated=false, ShowInTaskbar=false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(150));
        Assert(window.AutosaveInterval == TimeSpan.FromMinutes(5), "WPF default checkpoint interval is five minutes");
        window.NotesBox.Text = "Notes on dashboard A"; window.TaskInput.Text = "Unsubmitted task draft";
        window.TitleInput.Text = "Unstarted meeting title"; window.IntervalInput.Text = "7";
        Assert(window.SaveNow() && window.SaveStatus.Text.Contains("Auto-saved · Saved at"), "Successful save displays the local save time");
        var activeId = window.state.Workspace!.ActiveDashboardId;
        var candidate = window.CopyWorkspaceState().Workspace!;
        var second = Continuity.NewDashboard("Dashboard B", "Meeting", new DashboardData(), store.UserId);
        candidate.Dashboards.Add(second); candidate.ActiveDashboardId = second.DashboardId;
        Assert(window.CommitWorkspace(candidate) && window.NotesBox.Text == "", "Switching to a new dashboard starts with isolated content");
        window.NotesBox.Text = "Notes on dashboard B";
        window.DashboardPicker.SelectedItem = window.state.Workspace!.Dashboards.Single(d => d.DashboardId == activeId);
        Assert(window.NotesBox.Text == "Notes on dashboard A" && window.TaskInput.Text == "Unsubmitted task draft" && window.TitleInput.Text == "Unstarted meeting title" && window.IntervalInput.Text == "7", "Dashboard switch saves outgoing state and restores notes and input drafts independently");
        candidate = window.CopyWorkspaceState().Workspace!;
        var dashboard = Continuity.Current(candidate, store.UserId);
        Continuity.Reset(dashboard);
        Assert(window.CommitWorkspace(candidate) && window.NotesBox.Text == "" && window.state.Session == null && window.Countdown.Text == "00:00:00", "Committed reset clears visible notes and stale timer UI");
        var source = Continuity.Current(window.state.Workspace!, store.UserId).History.Single();
        Assert(MainWindow.PreviewSession(source).Contains("Notes on dashboard A") && window.NotesBox.Text == "", "Read-only history preview leaves current editor untouched");
        candidate = window.CopyWorkspaceState().Workspace!;
        Continuity.Continue(Continuity.Current(candidate, store.UserId), source.SessionId, store.UserId);
        Assert(window.CommitWorkspace(candidate) && window.NotesBox.Text == "Notes on dashboard A", "Continue restores historical content into editable controls");
        window.NotesBox.Text = "Edited after continuing";
        Pump(TimeSpan.FromSeconds(1.3));
        Assert(store.Load().Notes == "Edited after continuing" && !window.HasUnsavedChanges, "Debounced edit saves on background worker and clears dirty state");
        Pump(TimeSpan.FromSeconds(3.6));
        Assert(window.SaveStatus.Opacity == 1 && window.SaveSpinner.Visibility == Visibility.Collapsed, "Success time remains readable with spinner stopped");
        var saved = File.GetLastWriteTimeUtc(store.FilePath);
        window.SaveInBackground().GetAwaiter().GetResult();
        Assert(File.GetLastWriteTimeUtc(store.FilePath) == saved, "Clean background autosave performs no disk write");
        Snapshot(window, "continuity-dashboard", 1204, 840);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000; window.Top = -10000; window.ShowActivated = false; window.ShowInTaskbar = false; window.Show();
        var historyHome = new HomeWindow(store); historyHome.Show();
        // Capture history from Home; selecting an entry is a read-only preview action.
        var historyCapture = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        historyCapture.Tick += (_, _) =>
        {
            var history = historyHome.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title.EndsWith("History"));
            if (history == null) return;
            historyCapture.Stop();
            var list = Descendants(history).OfType<ListBox>().Single();
            list.SelectedIndex = list.Items.Count - 1;
            var content = (FrameworkElement)history.Content;
            content.Measure(new Size(1000, 680)); content.Arrange(new Rect(0, 0, 1000, 680)); content.UpdateLayout();
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1000, 680, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(content);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using (var stream = File.Create(Path.Combine(output, "history-preview.png"))) encoder.Save(stream);
            Assert(Descendants(history).OfType<TextBox>().Any(t => t.IsReadOnly && t.Text.Contains("Previous session") && t.Text.Contains("Notes on dashboard A")), "History dialog exposes read-only dated preview");
            history.Close();
        };
        historyCapture.Start(); Descendants(historyHome).OfType<Button>().Single(b => Descendants(b).OfType<TextBlock>().Any(t=>t.Text=="History")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); historyHome.Close();
        // Explicitly select the supported 30-second interval, then keep the edit debounce busy.
        window.state.Workspace!.Settings.AutosaveSeconds = 30; window.ApplyReliabilityOptions();
        // Only the periodic checkpoint can save during continuous typing.
        var initial = store.Load().Notes; var count = 0;
        var typing = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        typing.Tick += (_, _) => window.NotesBox.Text = "Continuous edit " + ++count;
        typing.Start(); Pump(TimeSpan.FromSeconds(31)); typing.Stop();
        Assert(store.Load().Notes.StartsWith("Continuous edit") && store.Load().Notes != initial, "Real 30-second autosave persists changes even during continuous typing");
        Pump(TimeSpan.FromSeconds(1.3));
        window.NotesBox.Text = "Captured before background write";
        var inFlight = window.SaveInBackground();
        window.NotesBox.Text = "Typed while save was running";
        // Isolate this capture from the independent edit-debounce save.
        ((DispatcherTimer)typeof(MainWindow).GetField("saveDelay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!).Stop();
        var saveWait = System.Diagnostics.Stopwatch.StartNew();
        while (!inFlight.IsCompleted && saveWait.Elapsed < TimeSpan.FromSeconds(5)) Pump(TimeSpan.FromMilliseconds(10));
        Assert(inFlight.IsCompletedSuccessfully && window.HasUnsavedChanges && store.Load().Notes == "Captured before background write", "Edits made during a background write stay dirty after the older capture completes");
        window.NotesBox.Text = "Last change before close";
        window.Close();
        var savedSessionId = Continuity.Current(store.Load().Workspace!, store.UserId).Current.SessionId;
        var reopened = new MainWindow(store);
        Assert(reopened.NotesBox.Text == "Last change before close" && Continuity.Current(reopened.state.Workspace!, store.UserId).Current.SessionId == savedSessionId, "Close flushes pending edits and reopening continues the same session");
        // Force an actual filesystem failure without touching any personal data.
        File.Move(store.FilePath, store.FilePath + ".held"); Directory.CreateDirectory(store.FilePath);
        var successfulAt = reopened.lastSuccessfulSaveAt;
        reopened.NotesBox.Text = "Must remain unsaved";
        Assert(!reopened.SaveNow() && reopened.HasUnsavedChanges && reopened.SaveStatus.Text == "Save failed", "I/O failure retains dirty edits and never shows a false saved indicator");
        Assert(reopened.lastSuccessfulSaveAt == successfulAt && reopened.SaveSpinner.Visibility == Visibility.Collapsed, "Actual I/O failure retains last successful timestamp and stops spinner");
        var failedCandidate = reopened.CopyWorkspaceState().Workspace!;
        Continuity.Reset(Continuity.Current(failedCandidate, store.UserId));
        Assert(!reopened.CommitWorkspace(failedCandidate) && reopened.NotesBox.Text == "Must remain unsaved", "Failed reset commit leaves current editor intact");
        Directory.Delete(store.FilePath); File.Move(store.FilePath + ".held", store.FilePath);
        Assert(reopened.SaveNow(), "Saving retries successfully when storage recovers");
        reopened.Close();
    }
}
