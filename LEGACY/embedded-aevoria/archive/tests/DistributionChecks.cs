using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckDistribution()
    {
        var start = new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.FromHours(-5));
        var tracker = new DistributionTrackerState(); var argo = tracker.Presets.Single();
        Assert(argo.Name == "Argo Pantry Shift" && argo.Singular == "Visitor" && argo.Plural == "Visitors", "Argo preset terminology");
        Assert(argo.Categories.Select(c => c.Name).SequenceEqual(new[] { "Fruit", "Freezer", "Fridge", "Canned", "Toiletries" }), "Argo category order");
        Assert(argo.Categories.Select(c => c.IconId).SequenceEqual(new[] { "apple", "snowflake", "refrigerator", "canned-food", "soap" }), "Pantry default icons");
        Assert(TrackerIcons.BuiltIn.Count == 100 && TrackerIcons.BuiltIn.Select(i => i.Id).Distinct().Count() == 100, "100 stable built-in icon IDs");
        Assert(TrackerIcons.BuiltIn.Select(i => i.Group).Distinct().Count() == 10, "Icon groups");
        tracker.Load(argo, start); var session = tracker.ActiveSession!; var ids = session.Configuration.Categories.Select(c => c.Id).ToArray();
        session.Add([ids[0], ids[1], ids[3]], start.AddMinutes(1));
        var stats = DistributionStatistics.Calculate(session);
        Assert(stats.Total == 1 && stats.CategoryTotals[ids[0]] == 1 && !stats.CategoryTotals.ContainsKey(ids[2]), "Visitor 1 distribution");
        session.Add([ids[2], ids[3], ids[4]], start.AddMinutes(2)); stats = DistributionStatistics.Calculate(session);
        Assert(stats.Total == 2 && stats.CategoryTotals[ids[3]] == 2 && stats.CategoryTotals[ids[2]] == 1 && stats.AverageItems == 3, "Visitor 2 distribution");
        Assert(stats.Percentage(ids[3]) == 100 && stats.Percentage(ids[0]) == 50, "Independent category percentages");
        Assert(stats.Rate(session, start) == null && stats.Rate(session, start.AddMinutes(4)) == null && stats.Rate(session, start.AddHours(1)) == 2, "Rate floor and hourly denominator");
        Assert(session.Undo() && DistributionStatistics.Calculate(session).Total == 1 && !DistributionStatistics.Calculate(session).CategoryTotals.ContainsKey(ids[2]), "Undo recalculates totals");
        var empty = session.Add([], start.AddHours(1)); Assert(empty.Number == 3, "Entry numbers are not reused after undo");
        stats = DistributionStatistics.Calculate(session); Assert(stats.Total == 2 && stats.AverageItems == 1.5 && stats.Hours.Count == 2, "Empty entry and hourly breakdown");
        empty.CategoryIds = [ids[0], ids[4]]; stats = DistributionStatistics.Calculate(session);
        Assert(stats.CategoryTotals[ids[0]] == 2 && stats.AverageItems == 2.5, "Editing updates category counts and averages");
        session.Entries.Remove(empty); Assert(DistributionStatistics.Calculate(session).Total == 1, "Delete recalculates");
        session.Configuration.Categories[3].Name = "Shelf Stable"; session.Configuration.Categories[3].Retired = true;
        Assert(DistributionStatistics.Calculate(session).CategoryTotals[ids[3]] == 1 && session.Configuration.Categories[3].Id == ids[3], "Rename and retirement retain historical associations");
        Assert(session.Configuration.ConfigurationCopy().Categories.All(c => c.Id != ids[3]), "Presets omit retired categories");
        tracker.Load(argo, start.AddHours(2));
        Assert(tracker.History.Count == 1 && tracker.History[0].Entries.Count == 1 && tracker.ActiveSession!.Entries.Count == 0, "Loading presets archives active data without remapping");
        Assert(tracker.History[0].Configuration.Categories[3].Name == "Shelf Stable" && argo.Categories[3].Name == "Canned", "History and presets are independent snapshots");
        Assert(DistributionStatistics.Calculate(tracker.History[0]).Rate(tracker.History[0], start.AddDays(20)) == .5, "Ended-session rate is frozen");
        tracker.Validate();
        var widget = WidgetLibrary.Create("Distribution Tracker"); widget.Distribution = tracker; widget.Title = "Argo Pantry"; widget.CustomSize = true; widget.Span = 10; widget.Height = 600; widget.PlacementX = 0; widget.PlacementY = 0;
        var notes = WidgetLibrary.Create("Notes"); notes.PlacementX = .55; notes.PlacementY = 0; notes.Span = 8; notes.Text = "Preserve me";
        var store = new StateStore(Path.Combine(output, "distribution.json"));
        store.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [widget, notes] });
        var loaded = store.Load().Widgets.Single(w => w.Id == widget.Id).Distribution!;
        Assert(loaded.History.Single().Entries.Count == 1 && loaded.ActiveSession!.Entries.Count == 0 && loaded.Presets.Count == 1, "StateStore roundtrip retains history, active shift and presets");
        var configOnly = Continuity.ConfigurationOnly(new DashboardData { Widgets = [widget] });
        Assert(configOnly.Widgets[0].Distribution is { ActiveSession: null, History.Count: 0, Presets.Count: 1 } && tracker.History.Count == 1, "Configuration-only export omits entries/history without changing original");
        var window = new MainWindow(store) { Width = 1250, Height = 900, Left = -10000, Top = -10000, ShowActivated = false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(160));
        var live = window.state.Widgets.Single(w => w.Id == widget.Id); var liveTracker = live.Distribution!;
        Border Frame() => window.WidgetCanvas.Children.OfType<Border>().Single(b => Equals(b.Tag, widget.Id));
        Button FindButton(string label) => Descendants(Frame()).OfType<Button>().Single(b => Equals(b.Content, label));
        var noteFrame = window.WidgetCanvas.Children.OfType<Border>().Single(b => Equals(b.Tag, notes.Id)); var location = new Point(Canvas.GetLeft(noteFrame), Canvas.GetTop(noteFrame));
        var toggles = Descendants(Frame()).OfType<ToggleButton>().ToList();
        Assert(toggles.Count == 5 && FindButton("ADD VISITOR") != null, "Rapid-entry UI uses preset terminology and five toggles");
        foreach (var i in new[] { 0, 1, 3 }) toggles[i].IsChecked = true;
        FindButton("ADD VISITOR").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        FindButton("ADD VISITOR").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(liveTracker.ActiveSession!.Entries.Count == 1 && toggles.All(t => t.IsChecked == false), "Add clears selections and suppresses immediate duplicate click");
        Pump(TimeSpan.FromMilliseconds(430));
        foreach (var i in new[] { 2, 3, 4 }) toggles[i].IsChecked = true;
        FindButton("ADD VISITOR").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(DistributionStatistics.Calculate(liveTracker.ActiveSession).CategoryTotals[ids[3]] == 2, "Real UI adds second multi-category visitor");
        FindButton("Undo last entry").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(liveTracker.ActiveSession.Entries.Count == 1, "Real UI undo");
        foreach (var mode in new[] { SessionDashboard.ThemeMode.Light, SessionDashboard.ThemeMode.Dark })
        {
            Theme.Apply(Application.Current.Resources, mode, AppTheme.Aurora);
            Snapshot(window, "distribution-" + mode, 1250, 900);
            Assert(Descendants(Frame()).OfType<ToggleButton>().All(t => t.Foreground is SolidColorBrush), "Theme resources resolve for category toggles " + mode);
        }
        FindButton("Compact view").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(40));
        var newNoteFrame = window.WidgetCanvas.Children.OfType<Border>().Single(b => Equals(b.Tag, notes.Id));
        Assert(live.DisplayMode == "Compact" && Canvas.GetLeft(newNoteFrame) == location.X && Canvas.GetTop(newNoteFrame) == location.Y, "Compact mode preserves unrelated widget placement");
        FindButton("Expanded view").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(live.DisplayMode == "Expanded" && live.Height == 600, "Expanded content does not resize saved geometry");
        window.SaveNow(); window.Close();
        var reopened = store.Load().Widgets.Single(w => w.Id == widget.Id).Distribution!;
        Assert(reopened.ActiveSession!.Entries.Count == 1 && reopened.ActiveSession.Entries[0].CategoryIds.Count == 3 && reopened.History.Count == 1, "Shift and selections survive actual window save/close/reload");
        var iconsDirectory = Path.Combine(output, "icon-assets");
        var pngPath = Path.Combine(output, "source-icon.png");
        var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, new byte[16], 8);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var stream = File.Create(pngPath)) encoder.Save(stream);
        var custom = TrackerIcons.Import(pngPath, iconsDirectory); var originalFile = custom.StoredFile; custom.Name = "Fresh Produce";
        Assert(File.Exists(Path.Combine(iconsDirectory, originalFile)) && custom.OriginalFilename == "source-icon.png" && custom.Name == "Fresh Produce", "Managed icon copy and rename metadata");
        File.Delete(pngPath); Assert(TrackerIcons.Decode(Path.Combine(iconsDirectory, originalFile)).PixelWidth > 0, "Custom icon independent of original path");
        reopened.CustomIcons.Add(custom); Assert(TrackerIcons.Create(custom.Id, reopened, iconsDirectory) is Image, "Custom icon renders");
        reopened.CustomIcons.Clear(); Assert(TrackerIcons.Create(custom.Id, reopened, iconsDirectory) is TextBlock, "Deleted icon provides safe fallback");
        var rejected = false; try { TrackerIcons.AssetPath(iconsDirectory, "../outside.png"); } catch (IOException) { rejected = true; } Assert(rejected, "Managed icon paths reject traversal");
        var catalog = new WrapPanel(); foreach (var icon in TrackerIcons.BuiltIn) catalog.Children.Add(TrackerIcons.Create(icon.Id, reopened, iconsDirectory));
        Assert(catalog.Children.Count == 100, "All built-in icons instantiate");
    }
}

