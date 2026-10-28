using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckNexaweenCore()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        var zone = "America/Chicago";
        DateTimeOffset At(int year, int month, int day, int hour, int minute = 0) => NexaTime.Instant(new DateTime(year, month, day, hour, minute, 0), zone);
        var seed = new Session { Title = "M/W class", Start = At(2030,1,7,13), End = At(2030,1,7,14,15), ScheduledDays = [DayOfWeek.Monday, DayOfWeek.Wednesday], TimeZoneId = zone, AutomaticScheduling = true };
        var next = WeeklySchedule.Next(seed, seed.End)!;
        Assert(next.Start == At(2030,1,9,13) && next.Start - seed.End == TimeSpan.FromHours(46.75), "Monday ends with a 46h45 countdown to Wednesday");
        var third = WeeklySchedule.Next(seed, next.End)!;
        Assert(third.Start == At(2030,1,14,13) && third.Start - next.End == TimeSpan.FromHours(118.75), "Wednesday ends with a 118h45 countdown to Monday");
        Assert(WeeklySchedule.Latest(seed, next.Start.AddMinutes(20))?.Start == next.Start, "Missed-start recovery selects the active occurrence");
        var overnight = Continuity.Clone(seed); overnight.Start = At(2030,1,7,23); overnight.End = At(2030,1,8,1);
        var midnight = WeeklySchedule.Next(overnight, overnight.End)!;
        Assert(NexaTime.Local(midnight.End, zone).Date == new DateTime(2030,1,10) && midnight.End - midnight.Start == TimeSpan.FromHours(2), "Recurring midnight crossings preserve local dates and duration");
        Assert(NexaTime.Local(At(2026,3,8,2,30), zone).Hour == 3, "Spring DST gap advances to a valid wall time");
        Assert(At(2026,11,1,1,30).Offset == TimeSpan.Zero && NexaTime.Local(At(2026,11,1,1,30), zone).Offset == TimeSpan.FromHours(-6), "Fall DST overlap deterministically selects standard time and stores UTC");
        Assert(NexaTime.Zone("Central Standard Time").GetUtcOffset(At(2026,7,1,12)) == TimeSpan.FromHours(-5), "IANA and Windows zones agree in summer");
        Assert(NexaTime.Percentage(0) == "0.000%" && NexaTime.Percentage(.12345) == "12.345%" && NexaTime.Percentage(.5) == "50.000%", "Progress formats exactly three decimals");
        Assert(NexaTime.Percentage(.999999999) == "99.999%" && NexaTime.Percentage(1) == "100.000%", "Only actual completion can display 100.000 percent");
        foreach (var fraction in new[] { 1d, .999d, .75d, .5d, .001d, 0d })
            Assert(NexaTime.LitPixels(fraction, 540) is >= 0 and <= 540, "Sequential pixel count is bounded at " + fraction);
        var ledger = new ProductivityLedger(); var at = At(2030,1,7,13);
        Assert(ProductivityScoring.DirectSquash(ledger, "one", at), "Direct squash awards points and starts Frenzy");
        Assert(!ProductivityScoring.DirectSquash(ledger, "one", at.AddSeconds(1)) && ledger.BugsSquashed == 1, "The same bug cannot earn duplicate points");
        ProductivityScoring.Add(ledger, "productive", "Task", "Complete task", 100, at.AddSeconds(1));
        Assert(ledger.Score == 210 && ledger.Events.Last().BonusPoints == 100, "Eligible actions receive 2x points during the direct-squash Bug Frenzy window");
        ProductivityScoring.Add(ledger, "expired", "Notes", "Later note", 20, at.AddSeconds(121));
        Assert(ledger.Events.Last().BonusPoints == 0 && ledger.FrenzyUntil == at.AddMinutes(2), "Frenzy expires at a saved timestamp without a running animation");
        var before = new DashboardData { Tasks = [new TaskItem { Text = "Finish worksheet" }] };
        var after = Continuity.Clone(before); after.Tasks[0].Done = true;
        ProductivityScoring.Observe(before, after, at);
        var score = after.Productivity.Score; ProductivityScoring.Observe(before, after, at.AddSeconds(2));
        Assert(score == 100 && after.Productivity.Score == score, "Repeated observations do not farm task completion");
        var reopened = Continuity.Clone(after); reopened.Tasks[0].Done = false; ProductivityScoring.Observe(after, reopened, at.AddSeconds(4));
        var redone = Continuity.Clone(reopened); redone.Tasks[0].Done = true; ProductivityScoring.Observe(reopened, redone, at.AddSeconds(6));
        Assert(redone.Productivity.Score == 100, "Reopening and completing a task again never adds points");
        var counter = WidgetLibrary.Create("Counter"); before.Widgets = [counter]; after = Continuity.Clone(before); after.Widgets[0].Value = 1;
        ProductivityScoring.Observe(before, after, at);
        var down = Continuity.Clone(after); down.Widgets[0].Value = 0; ProductivityScoring.Observe(after, down, at.AddSeconds(2));
        var up = Continuity.Clone(down); up.Widgets[0].Value = 1; ProductivityScoring.Observe(down, up, at.AddSeconds(4));
        Assert(up.Productivity.Score == after.Productivity.Score, "Counter decrement/increment farming earns no repeat points");
        var note = WidgetLibrary.Create("Notes"); note.Text = "Session notes"; note.PlacementY = 900;
        var permanent = WidgetLibrary.Create("Notes"); permanent.Text = "Permanent library"; permanent.SessionScoped = false;
        var tasks = WidgetLibrary.Create("Task List"); tasks.Items = [new() { Text = "Keep", Permanent = true }, new() { Text = "Clear" }];
        var data = new DashboardData { Notes = "Shared notes", Widgets = [note, permanent, tasks], Tasks = [new() { Text = "Global permanent", Permanent = true }, new() { Text = "Session task" }], Session = seed, Productivity = ledger };
        var reset = WeeklySchedule.ResetSessionData(data);
        Assert(reset.Notes == "" && reset.Widgets[0].Text == "" && reset.Widgets[0].PlacementY == 900, "Reset clears session notes while preserving widget identity and placement");
        Assert(reset.Widgets[1].Text == "Permanent library" && reset.Tasks.Count == 1 && reset.Widgets[2].Items.Count == 1, "Reset preserves permanent notes and tasks");
        Assert(reset.Productivity.Score == 0 && data.Productivity.Score > 0 && data.Notes == "Shared notes", "Detached reset leaves the original record and score intact");
        Assert(Continuity.ConfigurationOnly(data).Productivity.Events.Count == 0, "Configuration-only exports never leak productivity activity");

        var path = Path.Combine(output, "nexaween-state.json");
        var store = new StateStore(path);
        var appState = new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), note, permanent, tasks, WidgetLibrary.Create("Productivity Score")], Notes = "Preserve me", Session = seed };
        store.Save(appState); var initialId = Continuity.Current(appState.Workspace!, store.UserId).Current.SessionId;
        appState.Notes = "Saved without duplication"; store.Save(appState);
        Assert(Continuity.Current(store.Load().Workspace!, store.UserId).Current.SessionId == initialId, "Continuous saves preserve one durable session ID");
        appState.Workspace!.CleanShutdown = false; store.Save(appState);
        Assert(store.Load().Notes == "Saved without duplication" && store.Warning != null, "Unclean restart recovers the latest durable content");
        Theme.Apply(Application.Current.Resources, SessionDashboard.ThemeMode.Dark, AppTheme.Nexa);
        var window = new MainWindow(store) { Width = 1450, Height = 950 };
        window.Show(); Pump(TimeSpan.FromMilliseconds(220));
        foreach (var interval in NexaOptions.SaveIntervals)
        {
            window.state.Workspace!.Settings.AutosaveSeconds = interval; window.ApplyReliabilityOptions();
            Assert(window.AutosaveInterval == TimeSpan.FromSeconds(interval) && window.SaveNow() && store.Load().Workspace!.Settings.AutosaveSeconds == interval, "Autosave interval applies immediately and persists: " + interval);
        }
        Assert(window.FooterStatus.Visibility == Visibility.Collapsed && window.DashboardTip.Visibility == Visibility.Collapsed, "Fixed footer and old fixed tip reserve no layout space");
        var bottom = Math.Max(window.PixelBannerContainer.ActualHeight, window.WidgetCanvas.Children.OfType<Border>().Where(b => b.Tag is Guid).Select(b => Canvas.GetTop(b) + b.Height).DefaultIfEmpty(0).Max());
        Assert(window.WidgetCanvas.Height >= bottom + window.WidgetScroller.ViewportHeight - 1, "Canvas maintains one viewport below the lowest widget");
        window.UpdateClock(seed.Start.AddMinutes(20));
        var pixelBrush = (Brush)window.FullContent.Resources["DashboardPixelBrush"];
        var sequence = window.PixelGrid.Children.OfType<Border>().Select(p => Equals(p.Background, pixelBrush)).ToArray();
        Assert(sequence.TakeWhile(x => x).Count() == sequence.Count(x => x), "Rendered timer pixels form one contiguous prefix after timestamp jumps");
        window.UpdateClock(seed.End.AddMinutes(6));
        Assert(window.state.Notes == "Saved without duplication" && window.state.Session?.Start == seed.Start, "Session completion and review checkpoint preserve visible work");
        var record = Continuity.Current(window.state.Workspace!, store.UserId).Current;
        Assert(record.CompletedCheckpointAt >= seed.End.AddMinutes(5) && window.SaveNow(), "The review checkpoint updates the existing session");
        var report = MainWindow.PreviewSession(record);
        Assert(report.Contains(initialId.ToString()) && report.Contains("PRODUCTIVITY") && report.Contains("Permanent library"), "Historical report includes identity, scores, and meaningful widget content");
        var beforeResetId = record.SessionId; var historyCount = Continuity.Current(window.state.Workspace!, store.UserId).History.Count;
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            window.UpdateClock(next.Start.AddMinutes(-5));
            Assert(window.state.Notes == "Saved without duplication" && Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == beforeResetId, "Persistence failure prevents destructive reset");
        }
        window.UpdateClock(next.Start.AddMinutes(-5).AddSeconds(2));
        var updated = Continuity.Current(window.state.Workspace!, store.UserId);
        Assert(updated.History.Count == historyCount + 1 && updated.History.Last().SessionId == beforeResetId && updated.History.Last().Data.Notes == "Saved without duplication", "Successful reset archives the complete prior record once");
        Assert(window.state.Notes == "" && window.state.Session?.Start == next.Start && window.state.Widgets.Single(w => w.Id == permanent.Id).Text == "Permanent library", "Pre-session reset prepares the right recurrence and preserves permanent content");
        Assert(window.state.Widgets.Single(w => w.Id == note.Id).PlacementY == 900 && updated.Current.SessionId != beforeResetId, "New occurrence retains layout with a new session identity");
        Assert(window.SaveNow() && store.Load().Session?.Start == next.Start, "Prepared next occurrence survives restart");
        CheckNexaweenMore(window, store, third, path);
        Snapshot(window, "nexaween-core-dashboard", 1450, 950);
        window.Close(); Pump(TimeSpan.FromMilliseconds(80));
        File.WriteAllText(Path.Combine(output, "nexaween-core-results.txt"), $"PASS: {passed} focused core checks.\n");
    }
}
