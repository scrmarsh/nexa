using System.IO;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckRelease016()
    {
        Assert(App.AppVersion == "0.1.18" && ReleaseNotes.All[0].Version == "0.1.18" &&
            ReleaseNotes.All.Any(note => note.Version == "0.1.16" && note.Title.Contains("Layout")), "Assembly and changelog show the current release and preserve 0.1.16 history");
        Assert(WidgetLibrary.Definitions.Count(d => d.Name == "Alcoholic Beverage") == 1,
            "Beer Counter appears once in the normal widget catalog");
        var beer = WidgetLibrary.Create("Beer Counter"); beer.BeerGoal = 1; beer.Value = 0;
        var water = WidgetLibrary.Create("Water Bottle Counter"); water.WaterBottleGoal = 4;
        var notes = WidgetLibrary.Create("Notes"); notes.PlacementX = .05; notes.PlacementY = 480;
        beer.PlacementX = .6; beer.PlacementY = 480;
        var store = new StateStore(Path.Combine(output, "release016.json"));
        store.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), notes, beer, water] });
        var window = new MainWindow(store) { Width = 1250, Height = 850, Left = -10000, Top = -10000, ShowActivated = false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var noteFrame = window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, notes.Id));
        var beerFrame = window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, beer.Id));
        var noteLocation = new Point(Canvas.GetLeft(noteFrame), Canvas.GetTop(noteFrame));
        var beerLocation = new Point(Canvas.GetLeft(beerFrame), Canvas.GetTop(beerFrame));
        var drop = typeof(MainWindow).GetMethod("CommitWidgetDrop", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        drop.Invoke(window, [window.state.Widgets.Single(w => w.Id == beer.Id), noteLocation.X + 80, noteLocation.Y + 70, false]);
        window.UpdateLayout();
        Assert(Canvas.GetLeft(noteFrame) == noteLocation.X && Canvas.GetTop(noteFrame) == noteLocation.Y &&
            Canvas.GetLeft(beerFrame) == beerLocation.X && Canvas.GetTop(beerFrame) == beerLocation.Y,
            "Dropping on an occupied card preserves both saved positions");
        Assert(Descendants(beerFrame).OfType<Button>().Any(b => Equals(b.Content, "+ 1 drink")),
            "Beer Counter renders its own count controls");
        window.ApplyBugPreferences(new BugPreferences(true, BugSpecies.Ants, BugDensity.Low));
        Pump(TimeSpan.FromMilliseconds(3200));
        for (var attempt = 0; attempt < 10 &&
            window.bugsOverlay?.Manager.Surfaces.ContainsKey(notes.Id.ToString()) != true; attempt++)
            Pump(TimeSpan.FromMilliseconds(100));
        Assert(window.bugsOverlay != null && window.bugsOverlay.Manager.Surfaces.ContainsKey(notes.Id.ToString()),
            $"Bug overlay maps widget surfaces in dashboard coordinates (overlay {window.bugsOverlay != null}, running {window.BugsRunning}, surfaces {string.Join(",", window.bugsOverlay?.Manager.Surfaces.Keys.Cast<string>() ?? Enumerable.Empty<string>())})");
        var contentBounds = window.bugsOverlay!.Manager.Surfaces[notes.Id.ToString()];
        var contentOrigin = window.bugsOverlay.ContentOrigin;
        Assert(window.WidgetScroller.ScrollableHeight > 40, "Test dashboard has scrollable widget content");
        window.WidgetScroller.ScrollToVerticalOffset(40);
        for (var attempt = 0; attempt < 10 && window.bugsOverlay.ContentOrigin.Y >= contentOrigin.Y - 30; attempt++)
            Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.bugsOverlay.ContentOrigin.Y < contentOrigin.Y - 30 &&
            window.bugsOverlay.Manager.Surfaces[notes.Id.ToString()] == contentBounds,
            $"Scrolling moves the bug drawing origin without changing its widget coordinates (old {contentOrigin.Y}, new {window.bugsOverlay.ContentOrigin.Y})");
        var plus = Descendants(beerFrame).OfType<Button>().Single(b => Equals(b.Content, "+ 1 drink"));
        plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.state.Widgets.Single(w => w.Id == beer.Id).Value == 1,
            "Beer goal one is reached after one increment");
        var minusGoal = Descendants(beerFrame).OfType<Button>().Single(b => Equals(b.ToolTip, "Decrease goal"));
        minusGoal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.state.Widgets.Single(w => w.Id == beer.Id).BeerGoal == 1 &&
            window.state.Widgets.Single(w => w.Id == water.Id).WaterBottleGoal == 4,
            "Beer minimum is one while water minimum remains four");
        var plusGoal = Descendants(beerFrame).OfType<Button>().Single(b => Equals(b.ToolTip, "Increase goal"));
        plusGoal.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.state.Widgets.Single(w => w.Id == beer.Id).BeerGoal == 2 &&
            Canvas.GetLeft(noteFrame) == noteLocation.X && Canvas.GetTop(noteFrame) == noteLocation.Y,
            "Changing the Beer goal does not shift another widget");
        Pump(TimeSpan.FromMilliseconds(350)); Snapshot(window, "beer-counter", 1250, 850);
        window.AddWidget("Clock"); Pump(TimeSpan.FromMilliseconds(80));
        Assert(Canvas.GetLeft(noteFrame) == noteLocation.X && Canvas.GetTop(noteFrame) == noteLocation.Y,
            "Adding a widget leaves an existing card in place");
        window.SaveNow(); window.Close();
        Assert(store.Load().Widgets.Single(w => w.Id == beer.Id) is { BeerGoal: 2, Value: 1 },
            "Beer goal and count persist across a save");
        Assert(beerLocation.X > noteLocation.X, "Saved independent widget positions remain separate");
        var afterSave = new MainWindow(store) { Width = 1250, Height = 850, Left = -10000, Top = -10000, ShowActivated = false };
        afterSave.Show(); Pump(TimeSpan.FromMilliseconds(140));
        var beerAfterSave = afterSave.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, beer.Id));
        var beforeRemove = new Point(Canvas.GetLeft(beerAfterSave), Canvas.GetTop(beerAfterSave));
        afterSave.RemoveWidgetConfirmed(afterSave.state.Widgets.Single(w => w.Id == notes.Id));
        Pump(TimeSpan.FromMilliseconds(100));
        beerAfterSave = afterSave.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, beer.Id));
        Assert(Canvas.GetLeft(beerAfterSave) == beforeRemove.X && Canvas.GetTop(beerAfterSave) == beforeRemove.Y,
            "Removing a widget leaves its space open without moving Beer Counter");
        afterSave.Close();
        var home = new HomeWindow(store) { Width = 900, Height = 620, Left = -10000, Top = -10000, ShowActivated = false };
        home.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(home).OfType<TextBlock>().Any(t => t.Text == "Nexa 0.1.18 alpha"),
            "Home uses the lowercase alpha version label");
        var about = Descendants(home).OfType<Button>().Single(b => Equals(b.Content, "About Nexa"));
        Assert(about.TranslatePoint(new Point(0, about.ActualHeight), home).Y < home.ActualHeight - 8,
            "Home footer button remains above the window bottom at compact size");
        home.WindowState = WindowState.Maximized; Pump(TimeSpan.FromMilliseconds(120));
        Assert(about.TranslatePoint(new Point(0, about.ActualHeight), home).Y < home.ActualHeight - 8,
            "Home footer button remains above the window bottom when maximized");
        home.Close();
    }
}
