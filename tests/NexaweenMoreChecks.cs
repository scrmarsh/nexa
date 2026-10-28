using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckNexaweenMore(MainWindow window, StateStore store, SessionOccurrence third, string outputPath)
    {
        var options = window.state.Workspace!.Settings;
        options.TimeZoneId = "America/Chicago";
        DateTimeOffset At(int month, int day, int hour = 12) => NexaTime.Instant(new DateTime(2026,month,day,hour,0,0), options.TimeZoneId);
        options.SeasonalMode = "Automatic";
        Assert(!NexaSeason.Active(options, At(9,30)) && NexaSeason.Active(options, At(10,1)), "Automatic seasonal activation begins October 1 in the selected zone");
        Assert(NexaSeason.Active(options, At(11,2,23)) && !NexaSeason.Active(options, At(11,3)), "Automatic seasonal activation includes all of November 2");
        options.SeasonalMode = "On"; Assert(NexaSeason.Active(options, At(2,1)), "Manual seasonal activation works outside October");
        options.SeasonalMode = "Off"; Assert(!NexaSeason.Active(options, At(10,31)), "Manual Off overrides Halloween dates");
        var savedAccent = window.state.AccentColor; var savedPixel = window.state.PixelColor;
        options.SeasonalMode = "On"; window.ApplyReliabilityOptions(); window.RefreshSeasonalWorkspace();
        Assert(Theme.SeasonalEnabled && ((SolidColorBrush)Application.Current.Resources["BackgroundBrush"]).Color == (Color)ColorConverter.ConvertFromString("#100D19"), "Seasonal palette applies the requested dark purple background");
        Assert(window.state.AccentColor == savedAccent && window.state.PixelColor == savedPixel, "Seasonal styling does not rewrite saved normal colors");
        var artwork = window.WidgetCanvas.Children.OfType<NexaweenDecorations>().Single();
        Assert(!artwork.IsHitTestVisible && artwork.Visibility == Visibility.Visible, "Seasonal artwork cannot intercept dashboard controls");
        options.SeasonalDecorations = false; window.ApplyReliabilityOptions(); Pump(TimeSpan.FromMilliseconds(350));
        Assert(artwork.Visibility == Visibility.Collapsed, "Decoration toggle hides artwork after its bounded fade");
        options.SeasonalDecorations = true; options.DecorationDensity = "High"; window.ApplyReliabilityOptions(); window.RefreshSeasonalWorkspace();
        Assert(window.WidgetCanvas.Children.OfType<Button>().Any(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Add Widget in empty workspace" && System.Windows.Controls.Canvas.GetTop(b) > 0), "Contextual Add Widget appears below existing content");
        options.SeasonalMode = "Off"; window.ApplyReliabilityOptions();
        Assert(!Theme.SeasonalEnabled && ((SolidColorBrush)window.FullContent.Resources["DashboardPixelBrush"]).Color == (Color)ColorConverter.ConvertFromString(savedPixel), "Disabling Halloween restores the saved dashboard palette");

        var settings = new SettingsDialog(window, store, window.state, window, "Session");
        settings.Show(); Pump(TimeSpan.FromMilliseconds(80));
        var interval = Descendants(settings).OfType<ComboBox>().Single(c => c.Items.Count == 7);
        interval.SelectedIndex = 0; Pump(TimeSpan.FromMilliseconds(700));
        Assert(window.AutosaveInterval == TimeSpan.FromSeconds(30) && store.Load().Workspace!.Settings.AutosaveSeconds == 30, "Actual saving settings UI applies and saves a changed interval");
        settings.Close(); Pump(TimeSpan.FromMilliseconds(60));
        var originalId = Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId;
        var originalNotes = window.state.Notes;
        window.ShowNotesHistory();
        Pump(TimeSpan.FromMilliseconds(70));
        var history = Application.Current.Windows.OfType<Window>().Single(w => w.Title == "Nexa · Notes & History");
        var list = Descendants(history).OfType<ListBox>().Single();
        AwaitFinal(() => list.Items.Count >= 2, "The asynchronous History snapshot did not load");
        Assert(list.Items.Count >= 2 && window.state.Notes == originalNotes && Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == originalId, "Opening readable history includes live and archived records without replaying a session");
        list.SelectedItem = list.Items.OfType<HistoryEntry>().First(e => e.Editable);
        Descendants(history).OfType<Button>().Single(b => Equals(b.Content, "Edit archived notes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(60));
        var editor = Application.Current.Windows.OfType<Window>().Single(w => w.Title == "Edit archived session notes");
        Descendants(editor).OfType<TextBox>().Single().Text = "Retrospective note edited without restarting the session.";
        Descendants(editor).OfType<Button>().Single(b => Equals(b.Content, "Save notes")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(260));
        Assert(store.Load().Workspace!.Dashboards.SelectMany(d => d.History).Any(s => s.Data.Notes.StartsWith("Retrospective note")) && Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == originalId, "Editing an archive persists retrospective notes without changing the active session identity");
        history.Close(); Pump(TimeSpan.FromMilliseconds(60));

        window.UpdateClock(third.Start.AddMinutes(-10));
        var postpone = Descendants(window).OfType<Button>().Single(b => Equals(b.Content, "Postpone Reset"));
        postpone.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Continuity.Current(store.Load().Workspace!, store.UserId).Current.ResetDeferred, "Reset postponement is persisted immediately");
        window.UpdateClock(third.Start.AddMinutes(-5));
        Assert(Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == originalId, "Postponed reset keeps the current dashboard past the deadline");
        window.CompleteDeferredResetAt(third.Start.AddMinutes(-4));
        Assert(window.state.Session?.Start == third.Start && !Continuity.Current(store.Load().Workspace!, store.UserId).Current.ResetDeferred, "Manual deferred-reset completion safely prepares the correct occurrence");
        var ordering = new StateStore(Path.Combine(output, "save-order.json")); var state = new AppState();
        state.Notes = "old"; var stale = ordering.Prepare(state);
        state.Notes = "new"; var newer = ordering.Prepare(state); ordering.Write(newer);
        var rejected = false; try { ordering.Write(stale); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected && ordering.Load().Notes == "new", "An older detached background payload cannot overwrite a newer completed save");
        options = window.state.Workspace!.Settings; options.SeasonalMode = "On"; options.DecorationDensity = "High"; window.ApplyReliabilityOptions(); window.RefreshSeasonalWorkspace();
        window.WidgetScroller.ScrollToEnd(); Pump(TimeSpan.FromMilliseconds(80));
        Snapshot(window, "nexaween-seasonal-workspace", 1450, 950);
        Assert(App.AppVersion == "0.0.31" && App.VersionLabel == "nexa v0.0.31 alpha", "Development candidate identifies itself as Nexa v0.0.28 Alpha");
    }
}
