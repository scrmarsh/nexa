using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static BugManager CreateWorld(BugBehavior behavior, int seed = 124)
    {
        var manager = new BugManager(seed);
        var options = new EcosystemOptions { Behavior = behavior };
        manager.Configure(new(true, BugSpecies.AllBugs, BugDensity.Normal, options)); manager.NewEcosystem("synthetic-owner", options, seed);
        manager.SeasonalCandy = true; manager.SeasonalCostumes = true; return manager;
    }
    private static void Step(BugManager manager, double seconds)
    { for (var i = 0; i < (int)Math.Ceiling(seconds / .1); i++) manager.Advance(.1); }
    private static void CheckEcosystem()
    {
        var cozy = CreateWorld(BugBehavior.Cozy); var activities = new HashSet<BugActivity>();
        var measured = Stopwatch.StartNew();
        for (var tick = 0; tick < 4000; tick++) { cozy.Advance(.1); foreach (var b in cozy.Bugs) activities.Add(b.Activity); }
        measured.Stop();
        Assert(cozy.Bugs.Count == 36 && cozy.Bugs.Select(b => b.Species).Distinct().Count() == 6, "Cozy ecosystem reaches its bounded population with all six species");
        Assert(cozy.CombatDeaths == 0 && cozy.Bugs.All(b => b.Health > 0), "Cozy does not default to continuous combat");
        Assert(cozy.Nests.Sum(n => n.Deliveries) > 0 && cozy.Nests.Any(n => n.Construction > .3), "Species actually gather resources, deliver them and build nests");
        Assert(activities.Contains(BugActivity.Gathering) && activities.Contains(BugActivity.Carrying) && activities.Contains(BugActivity.Sleeping) && activities.Contains(BugActivity.Socializing) && activities.Contains(BugActivity.Grooming), "Cozy includes functional carrying, rest, social and grooming activities");
        Assert(cozy.Webs.Count > 0 && cozy.Webs.Count <= 24, "Spiders construct bounded persistent webs without a battle loop");
        var beforePositions = cozy.Bugs.Select(b => b.Position).ToArray(); cozy.WorldPaused = true; Step(cozy, 5);
        Assert(beforePositions.SequenceEqual(cozy.Bugs.Select(b => b.Position)), "Pausing the world freezes authoritative creature positions"); cozy.WorldPaused = false;
        var capture = cozy.CaptureEcosystem("synthetic-owner"); capture.Validate("synthetic-owner");
        var restored = new BugManager(55); restored.RestoreEcosystem(Continuity.Clone(capture));
        Assert(restored.WorldId == cozy.WorldId && restored.Bugs.Select(b => b.InteractionId).SequenceEqual(cozy.Bugs.Select(b => b.InteractionId)) && restored.Bugs.Select(b => b.Position).SequenceEqual(cozy.Bugs.Select(b => b.Position)), "World identity, creature identity and positions survive complete serialization");
        Assert(restored.Nests.Sum(n => n.Deliveries) == cozy.Nests.Sum(n => n.Deliveries) && restored.Webs.Count == cozy.Webs.Count && restored.Resources.Count == cozy.Resources.Count, "Nests, resources and webs survive reload together");
        Throws<UnauthorizedAccessException>(() => capture.Validate("another-owner"), "An ecosystem cannot be loaded by an unverified owner");
        var bad = Continuity.Clone(capture); bad.Bugs[0].X = double.NaN;
        Throws<InvalidDataException>(() => bad.Validate("synthetic-owner"), "Non-finite creature coordinates are rejected before restoration");
        var creepy = CreateWorld(BugBehavior.Creepy); var creepyActivities = new HashSet<BugActivity>();
        for (var tick = 0; tick < 1200; tick++) { creepy.Advance(.1); foreach (var b in creepy.Bugs) creepyActivities.Add(b.Activity); }
        Assert(creepyActivities.Contains(BugActivity.Hiding) && !creepyActivities.Contains(BugActivity.Raiding), "Creepy behavior hides and scuttles without organized warfare");
        var combat = CreateWorld(BugBehavior.Combative); Step(combat, 10);
        foreach (var b in combat.Bugs) { b.Position = new(800 + (int)b.Species, 500); b.Target = b.Position; b.Health = 12; b.Decision = 0; }
        Step(combat, 12);
        Assert(combat.CombatDeaths > 0 && combat.Bugs.Any(b => b.Kills > 0), "Combative behavior causes actual territorial encounters and casualties");
        var worldId = combat.WorldId; var ids = combat.Bugs.Select(b => b.InteractionId).ToArray();
        combat.Configure(combat.Preferences with { Ecosystem = combat.EcosystemSettings with { Behavior = BugBehavior.Cozy } });
        Assert(combat.WorldId == worldId && ids.SequenceEqual(combat.Bugs.Select(b => b.InteractionId)) && combat.Bugs.All(b => b.EnemyId == null), "Changing behavior preserves the world and immediately ends obsolete attack orders");
        var policy = new EcosystemOptions { Behavior = BugBehavior.Cozy, Appearance = BugAppearance.Creepy, MatchAppearance = true };
        Assert(policy.EffectiveAppearance == BugAppearance.Cozy && (policy with { MatchAppearance = false }).EffectiveAppearance == BugAppearance.Cozy, "Behavior mapping safely ignores retired legacy appearance overrides");
        Assert(new EcosystemOptions().Size == 2 && new EcosystemOptions().Adorable && (new EcosystemOptions { Size = 20 }).Safe().Size == 4, "Default size is 200 percent with Adorable Mode enabled and bounded size settings");
        var culture = CultureInfo.CurrentCulture; CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        foreach (var species in BugManager.AllSpecies)
        {
            var normal = BugVisuals.Body(species, BugAppearance.Normal); var soft = BugVisuals.Body(species, BugAppearance.Cozy); var detailed = BugVisuals.Body(species, BugAppearance.Creepy);
            Assert(normal.IsFrozen && soft.IsFrozen && detailed.IsFrozen && !ReferenceEquals(soft, detailed) && soft.Bounds != detailed.Bounds, species + " has distinct cached native appearances");
            Assert(BugVisuals.Costume(species).IsFrozen && !BugVisuals.Costume(species).Bounds.IsEmpty, species + " has its own seasonal costume overlay");
        }
        CultureInfo.CurrentCulture = culture;
        RenderCreatureSheet();
        var path = Path.Combine(output, "shared-ecosystem.json"); var store = new StateStore(path); store.Save(new AppState());
        var preferences = new BugPreferences(true, BugSpecies.AllBugs, BugDensity.Normal, new EcosystemOptions { Behavior = BugBehavior.Cozy, Appearance = BugAppearance.Creepy, MatchAppearance = false, Size = 2.5 });
        store.SaveBugPreferences(preferences);
        var world = NexaEcosystem.For(store);
        Assert(ReferenceEquals(world, NexaEcosystem.For(new StateStore(path))), "Dashboard and ecosystem view obtain one authoritative world per workspace");
        Step(world.Manager, 5); Assert(world.Save(), "A populated ecosystem is saved atomically");
        var firstId = world.Manager.WorldId; var firstPosition = world.Manager.Bugs[0].Position;
        world.Manager.Bugs[0].Position = new(300, 300); Assert(world.Save() && world.RestoreBackup(), "Explicit backup restoration succeeds on valid same-owner data");
        Assert(world.Manager.Bugs[0].Position == firstPosition && world.Manager.WorldId == firstId, "Backup restoration restores the previous world without replacing its identity");
        using (var locked = new FileStream(world.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert(!world.Reset() && world.Manager.WorldId == firstId, "A failed ecosystem reset cannot replace the live world");
        Assert(world.Reset() && world.Manager.WorldId != firstId && Directory.GetFiles(output, "*.before-reset-*").Length > 0, "Explicit reset creates a new world and preserves a recovery copy");
        world.ApplySeason(new NexaOptions { SeasonalMode = "On", HalloweenBugCostumes = true });
        Assert(world.Manager.SeasonalCostumes && world.Manager.SeasonalCandy, "Manual Nexaween activation enables costumes and candy outside calendar restrictions");
        world.ApplySeason(new NexaOptions { SeasonalMode = "Off" });
        Assert(!world.Manager.SeasonalCostumes && world.Manager.EcosystemSettings.Appearance == BugAppearance.Creepy, "Disabling Halloween preserves the manually selected underlying appearance");
        var invalidStore = new StateStore(Path.Combine(output, "invalid-world.json")); invalidStore.Save(new AppState());
        File.WriteAllText(invalidStore.FilePath + ".ecosystem.json", "not a valid world");
        var invalid = NexaEcosystem.For(invalidStore);
        Assert(!invalid.CanSave && !invalid.Save() && File.ReadAllText(invalid.FilePath) == "not a valid world", "Unreadable ecosystem files are preserved instead of silently overwritten");
        Step(world.Manager, 2); var squashed = world.Manager.Bugs.First(b=>b.Species==BugSpecies.Ants); Assert(world.Manager.Squash(squashed), "A direct ant squash is accepted exactly once");
        using(var locked = new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))
        {
            world.RecordSquash(squashed,null);
            var pending=JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!;
            Assert(pending.PendingSquashes.Count==1 && pending.PendingSquashes[0].BugId==squashed.InteractionId, "A failed session-history write leaves a durable squash event in the ecosystem");
        }
        world.RetryHistory(); var scored=store.Load();
        Assert(scored.Productivity.BugsSquashed==1 && scored.Workspace!.AllTimeSquashes==1 && JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!.PendingSquashes.Count==0, "Recovered storage commits pending squash statistics once and clears the durable queue");
        world.RecordSquash(squashed,null); Assert(store.Load().Workspace!.AllTimeSquashes==1,"Replaying an acknowledged squash never duplicates all-time totals");
        var populationBefore=world.Manager.Bugs.Count;
        var uiPreferences=store.LoadBugPreferences(); world.ApplyPreferences(uiPreferences with {Ecosystem=uiPreferences.Ecosystem! with {Quality=BugAnimationQuality.Performance}});
        Assert(world.FrameInterval==TimeSpan.FromSeconds(1d/15) && world.Manager.Bugs.Count==populationBefore,"Performance quality changes cadence without resetting the population");
        world.ApplyPreferences(uiPreferences);
        CheckEcosystemUi(store, preferences);
        File.WriteAllText(Path.Combine(output, "simulation-measurement.txt"), $"400 simulated seconds, {cozy.Bugs.Count} final creatures, {measured.Elapsed.TotalMilliseconds:0.###} ms total CPU simulation wall time. This is not a GPU/FPS or extended-session benchmark.");
    }
    private static void RenderCreatureSheet()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(BugVisuals.B("#100D19"), null, new Rect(0, 0, 1200, 1150));
            for (var row = 0; row < 6; row++) for (var column = 0; column < 4; column++)
            {
                var species = BugManager.AllSpecies[row]; var bug = new BugEntity { Species = species };
                var options = new EcosystemOptions { MatchAppearance = false, Appearance = (BugAppearance)Math.Min(column, 2), Behavior=column==1?BugBehavior.Cozy:column>=2?BugBehavior.Creepy:BugBehavior.Classic, Adorable = true };
                dc.PushTransform(new TranslateTransform(150 + column * 300, 95 + row * 180)); dc.PushTransform(new ScaleTransform(5, 5));
                BugVisuals.Draw(dc, bug, options, column == 3); dc.Pop(); dc.Pop();
                var label = new FormattedText(species + " · " + (column == 3 ? "Nexaween" : options.Appearance.ToString()), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 15, Brushes.PaleGreen, 1);
                dc.DrawText(label, new Point(35 + column * 300, 158 + row * 180));
            }
        }
        var image = new RenderTargetBitmap(1200, 1150, 96, 96, PixelFormats.Pbgra32); image.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var stream = File.Create(Path.Combine(output, "creature-styles-costumes.png")); encoder.Save(stream);
    }
    private static void CheckEcosystemUi(StateStore store, BugPreferences preferences)
    {
        var window = new MainWindow(store) { Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var world = NexaEcosystem.For(store);
        Assert(window.bugsOverlay != null && ReferenceEquals(window.bugsOverlay.Manager, world.Manager), "Dashboard bugs use the same authoritative manager as the ecosystem service");
        var settings = new SettingsDialog(window, store, window.state, window, "Add Bugs"); settings.Show(); Pump(TimeSpan.FromMilliseconds(80));
        Assert(Descendants(settings).OfType<ComboBox>().Any(c => c.Items.OfType<BugBehavior>().Count() == 4) && Descendants(settings).OfType<CheckBox>().Any(c => Equals(c.Content, "Adorable Mode")), "Actual Add Bugs settings expose behavior, appearance and Adorable Mode controls");
        var size = Descendants(settings).OfType<Slider>().Single(s => s.Minimum == .75 && s.Maximum == 4); size.Value = 3;
        var behavior = Descendants(settings).OfType<ComboBox>().Single(c => c.Items.OfType<BugBehavior>().Count() == 4); behavior.SelectedItem = BugBehavior.Creepy;
        Pump(TimeSpan.FromMilliseconds(800));
        Assert(store.LoadBugPreferences().Ecosystem?.Size == 3 && world.Manager.EcosystemSettings.Behavior == BugBehavior.Creepy, "Real size and behavior controls apply immediately and persist"); settings.Close();
        var scene = new EcosystemWindow(window, store) { WindowState = WindowState.Normal, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        scene.Show(); Pump(TimeSpan.FromMilliseconds(120));
        Assert(ReferenceEquals(scene.World.Manager, window.bugsOverlay!.Manager) && world.ViewCount == 2, "A separate ecosystem view shares the existing population without spawning a second simulation");
        Assert(Descendants(scene).OfType<Button>().Any(b => Equals(b.Content, "Restore Backup")) && Descendants(scene).OfType<Button>().Any(b => Equals(b.Content, "Reset Ecosystem")), "Ecosystem view includes backup and explicit reset controls");
        scene.Close(); window.Close(); Pump(TimeSpan.FromMilliseconds(80));
        Assert(world.ViewCount == 0, "Closing both views releases their simulation subscriptions");
    }
}
