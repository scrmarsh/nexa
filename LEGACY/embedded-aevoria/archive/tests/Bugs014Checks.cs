using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using SessionDashboard;
using ThemeMode = SessionDashboard.ThemeMode;

internal static partial class Program
{
    private static void CheckBugs014()
    {
        var store = new StateStore(Path.Combine(output, "bugs-store.json"), "bugs-owner");
        Assert(!store.LoadBugPreferences().Enabled, "Existing/missing preferences default to no bugs");
        store.SavePreferences(ThemeMode.Light, AppTheme.Ember);
        var prefs = new BugPreferences(true, BugSpecies.AllThree, BugDensity.Infestation); store.SaveBugPreferences(prefs);
        Assert(store.LoadBugPreferences() == prefs && store.LoadThemeMode() == ThemeMode.Light && store.LoadAppTheme() == AppTheme.Ember, "Bug settings persist without replacing theme settings");
        store.SavePreferences(ThemeMode.Dark, AppTheme.Aurora);
        Assert(store.LoadBugPreferences() == prefs, "Appearance saves preserve Add Bugs settings");
        Assert(!new StateStore(store.FilePath, "other-owner").LoadBugPreferences().Enabled, "Bug preferences stay isolated between Nexa users");
        var manager = new BugManager(14); manager.Configure(prefs);
        manager.SetSurfaces([new("one", new Rect(20, 30, 300, 180)), new("two", new Rect(340, 30, 250, 200))], new Size(800, 600));
        for (var i = 0; i < 350; i++) manager.Advance(.033);
        Assert(manager.Bugs.Count == BugManager.InfestationCap && manager.Bugs.Select(b => b.Species).Distinct().Count() == 3, "All Three shares one 600-bug infestation cap");
        Assert(manager.Bugs.Any(b => b.Species == BugSpecies.Ants && b.Surface != null), "Ants associate with widget surfaces");
        Assert(manager.Bugs.Where(b => b.Species == BugSpecies.Roaches).Min(b => b.Speed) > manager.Bugs.Where(b => b.Species == BugSpecies.Ants).Max(b => b.Speed), "Roaches move faster than ants");
        var corner = BugManager.EdgePoint(new Rect(0, 0, 100, 100), .25);
        Assert(corner == new Point(100, 0) && BugManager.EdgePoint(new Rect(0, 0, 100, 100), .5) == new Point(100, 100), "Edge paths turn at widget corners");
        var fly = manager.Bugs.First(b => b.Species == BugSpecies.Flies);
        fly.Surface = "one"; fly.State = BugState.Landed; fly.EdgeProgress = .1; fly.Decision = 2;
        manager.SetSurfaces([new("one", new Rect(70, 80, 400, 200))], new Size(800, 600));
        Assert(fly.Position == BugManager.EdgePoint(new Rect(70, 80, 400, 200), .1), "Landed fly follows a moved and resized widget");
        for (var i = 0; i < 65; i++) manager.Advance(.033);
        Assert(fly.State is BugState.Flying or BugState.Crossing, "Landed fly takes off after its resting interval");
        manager.SetSurfaces([], new Size(800, 600));
        Assert(manager.Bugs.All(b => b.Surface == null), "Removing widgets releases all associated surface IDs");
        var victim = manager.Bugs[0]; victim.Position = new Point(100, 100); victim.State = BugState.Crossing;
        Assert(manager.Squash(victim) && !manager.Squash(victim), "Squash accepts a bug once and rejects repeat clicks");
        var point = victim.Position; manager.Advance(.08);
        Assert(victim.State == BugState.Squashed && victim.Position == point && manager.Hit(point) != null,
            "Squash freezes movement and retains its tiny hitbox to consume repeat clicks during compression");
        manager.Advance(.08);
        Assert(!manager.Bugs.Contains(victim) && manager.Splats.Count == 1, "Squash removes the bug after 160 ms and creates one splat");
        for (var i = 0; i < 51; i++) manager.Advance(.08);
        Assert(manager.Splats.Count == 0, "Splats are removed after four seconds");
        var watch = Stopwatch.StartNew(); for (var i = 0; i < 18000; i++) manager.Advance(1d / 30); watch.Stop();
        Console.WriteLine($"Infestation simulation: 18,000 frames in {watch.ElapsedMilliseconds} ms");
        Assert(manager.Bugs.Count <= BugManager.InfestationCap && manager.Bugs.All(b => b.Position.X >= 0 && b.Position.X <= 800 && b.Position.Y >= 0 && b.Position.Y <= 600), "Long simulation stays bounded within the arena and population cap");
        manager.Configure(new(false)); Assert(manager.Bugs.Count == 0 && manager.Splats.Count == 0, "Disabling clears all bugs and splats immediately");
        foreach (var density in Enum.GetValues<BugDensity>())
        {
            manager.Configure(new(true, BugSpecies.Flies, density));
            manager.SetSurfaces([new("landing", new Rect(20, 30, 300, 180))], new Size(800, 600));
            var landed = false;
            for (var i = 0; i < 3000; i++) { manager.Advance(.033); landed |= manager.Bugs.Any(b => b.State == BugState.Landed); }
            Assert(manager.Bugs.Count == manager.Cap && landed, $"{density} caps population and flying bugs naturally land on surfaces");
        }

        var overlay = new BugOverlay(); var button = new Button { Content = "Underlying button" };
        var grid = new Grid(); grid.Children.Add(button); grid.Children.Add(overlay);
        var hitWindow = new Window { Content = grid, Width = 400, Height = 300, Left = -10000, Top = -10000, ShowActivated = false };
        hitWindow.Show(); Pump(TimeSpan.FromMilliseconds(70));
        overlay.Manager.Bugs.Add(new BugEntity { Species = BugSpecies.Roaches, Position = new Point(50, 50), State = BugState.Crossing }); overlay.RefreshFrame();
        Pump(TimeSpan.FromMilliseconds(40));
        Assert(ReferenceEquals(grid.InputHitTest(new Point(50, 50)), overlay), "Only the visible insect hitbox targets the overlay");
        Assert(!ReferenceEquals(grid.InputHitTest(new Point(200, 150)), overlay), "Empty overlay space passes input to the underlying button");
        overlay.ContentOrigin = new Point(0, -30);
        Assert(ReferenceEquals(grid.InputHitTest(new Point(50, 20)), overlay), "Bug hit testing follows scrolled dashboard content");
        hitWindow.Close();

        store.SaveBugPreferences(new()); store.Save(new AppState { Notes = "Bugs must not eat this note" });
        var window = new MainWindow(store) { Width = 1100, Height = 760, Left = -10000, Top = -10000, ShowActivated = false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(150));
        Assert(window.bugsOverlay == null && !window.BugsRunning, "Disabled dashboard has no overlay or running bug timer");
        var settings = new SettingsDialog(window, store, window.state, window, "Add Bugs"); settings.Show(); Pump(TimeSpan.FromMilliseconds(60));
        var enabled = Descendants(settings).OfType<CheckBox>().Single(c => Equals(c.Content, "Add Bugs"));
        var choices = Descendants(settings).OfType<RadioButton>().ToArray();
        void Choose(string label)
        {
            var choice = choices.Single(c => Equals(c.Content, label));
            Assert(choice.IsEnabled && choice.IsVisible, $"{label} is available without opening a dropdown");
            var peer = new System.Windows.Automation.Peers.RadioButtonAutomationPeer(choice);
            ((System.Windows.Automation.Provider.ISelectionItemProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.SelectionItem)).Select();
        }
        Choose("Flies"); Choose("Low");
        settings.FlushPendingChanges();
        Assert(store.LoadBugPreferences() == new BugPreferences(false, BugSpecies.Flies, BugDensity.Low) && window.bugsOverlay == null,
            "Type and intensity can be configured while bugs are disabled");
        enabled.IsChecked = true; enabled.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        foreach (var species in Enum.GetValues<BugSpecies>())
        {
            Choose(species == BugSpecies.AllThree ? "All Three" : species == BugSpecies.AllBugs ? "All Bugs" : species.ToString());
            Assert(window.bugsOverlay?.Manager.Preferences.Species == species, $"Selecting {species} applies immediately");
        }
        Choose("All Three");
        foreach (var density in Enum.GetValues<BugDensity>())
        {
            Choose(density.ToString());
            Assert(window.bugsOverlay?.Manager.Preferences.Density == density, $"Selecting {density} applies immediately");
        }
        Choose("Infestation");
        Assert(choices.Count(c => c.IsChecked == true) == 2, "Exactly one type and one intensity stay selected");
        Pump(TimeSpan.FromMilliseconds(70));
        var settingsBody = (FrameworkElement)settings.Content;
        var settingsBitmap = new RenderTargetBitmap((int)Math.Ceiling(settingsBody.ActualWidth), (int)Math.Ceiling(settingsBody.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        settingsBitmap.Render(settingsBody); var settingsPng = new PngBitmapEncoder(); settingsPng.Frames.Add(BitmapFrame.Create(settingsBitmap));
        using (var stream = File.Create(Path.Combine(output, "bugs-settings.png"))) settingsPng.Save(stream);
        settings.Close(); Pump(TimeSpan.FromMilliseconds(300));
        Assert(store.LoadBugPreferences() == prefs && window.bugsOverlay != null && window.BugsRunning, "Experimental Settings applies species/density live and flushes preferences on close");
        var reopenedSettings = new SettingsDialog(window, store, window.state, window, "Add Bugs");
        reopenedSettings.Show(); Pump(TimeSpan.FromMilliseconds(60));
        var restoredChoices = Descendants(reopenedSettings).OfType<RadioButton>().Where(c => c.IsChecked == true).Select(c => c.Content).ToArray();
        Assert(restoredChoices.Contains("All Three") && restoredChoices.Contains("Infestation"), "Reopening Settings restores selected type and intensity");
        reopenedSettings.Close();
        Assert(window.bugsOverlay!.Manager.Surfaces.Count > 0, "Overlay discovers current widget bounds");
        var frame = window.WidgetCanvas.Children.OfType<Border>().First(f => f.Tag is Guid);
        var frameId = ((Guid)frame.Tag).ToString(); var oldBounds = window.bugsOverlay.Manager.Surfaces[frameId];
        frame.RenderTransform = new TranslateTransform(0, -20);
        window.FullContent.RaiseEvent(new DragDeltaEventArgs(0, -20) { RoutedEvent = Thumb.DragDeltaEvent });
        for (var attempt = 0; attempt < 10 && window.bugsOverlay.Manager.Surfaces[frameId].Top >= oldBounds.Top; attempt++)
            Pump(TimeSpan.FromMilliseconds(80));
        Assert(window.bugsOverlay.Manager.Surfaces[frameId].Top < oldBounds.Top,
            $"Drag events refresh transformed widget geometry while moving (old {oldBounds.Top}, new {window.bugsOverlay.Manager.Surfaces[frameId].Top})");
        frame.RenderTransform = Transform.Identity; window.UpdateLayout();
        for (var i = 0; i < 350; i++) window.bugsOverlay.Manager.Advance(.033);
        window.bugsOverlay.RefreshFrame(); Snapshot(window, "bugs-infestation-dark", 1100, 760);
        var renderWatch = Stopwatch.StartNew(); var render = new RenderTargetBitmap(1100, 760, 96, 96, PixelFormats.Pbgra32);
        for (var i = 0; i < 120; i++) { window.bugsOverlay.Manager.Advance(1d / 30); window.bugsOverlay.InvalidateVisual(); window.bugsOverlay.UpdateLayout(); render.Clear(); render.Render(window.bugsOverlay); }
        renderWatch.Stop(); Console.WriteLine($"Infestation WPF rendering: 120 overlay frames in {renderWatch.ElapsedMilliseconds} ms");
        Theme.Apply(Application.Current.Resources, ThemeMode.Light, AppTheme.Ember);
        Snapshot(window, "bugs-infestation-light", 1100, 760);
        window.WindowState = WindowState.Minimized; Pump(TimeSpan.FromMilliseconds(60));
        Assert(!window.BugsRunning, "Minimizing stops the shared bug update loop");
        window.WindowState = WindowState.Normal; Pump(TimeSpan.FromMilliseconds(60)); Assert(window.BugsRunning, "Restoring restarts the same bug manager");
        window.StartPixelSwarm(); Pump(TimeSpan.FromMilliseconds(60)); Assert(!window.BugsRunning, "Leaving the dashboard for Swarm pauses decorative bugs");
        window.ExitBoredomMode(); Pump(TimeSpan.FromMilliseconds(60)); Assert(window.BugsRunning, "Returning to the dashboard resumes bugs");
        for (var i = 0; i < 20; i++) { window.ApplyBugPreferences(new(false)); window.ApplyBugPreferences(prefs); }
        window.ApplyBugPreferences(new(false));
        Assert(window.bugsOverlay == null && !window.BugsRunning && window.state.Notes == "Bugs must not eat this note", "Repeated enable/disable cleans up without changing note content");
        window.Close(); Assert(!window.BugsRunning, "Shutdown leaves no active bug timer");
        var restored = new MainWindow(store) { Left = -10000, Top = -10000, ShowActivated = false }; restored.Show(); Pump(TimeSpan.FromMilliseconds(150));
        Assert(restored.bugsOverlay?.Manager.Preferences == prefs && restored.state.Notes == "Bugs must not eat this note", "Reopening restores saved bug settings and existing Space content"); restored.Close();
        CheckShared012(withBugs: true);
    }
}
