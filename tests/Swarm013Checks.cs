using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using SessionDashboard;
using ThemeMode = SessionDashboard.ThemeMode;

internal static partial class Program
{
    private static void CheckSwarm013()
    {
        var seeds = Enumerable.Range(0, 40).Select(i => new TimerPixelSeed(new Point(.12 + (i % 10) * .07, .15 + (i / 10) * .08), 6)
            { PixelSize = new Size(12, 12) }).ToArray();
        var game = new PixelSwarmGame(13, seeds); game.Resize(new Size(1100, 750));
        Assert(game.Phase == SwarmPhase.Menu && game.Pixels.Count == 40 && game.Pixels.All(p => p.FromTimer),
            "Menu preserves captured Pixel Timer pixels without starting the simulation");
        game.Start(); game.Pixels.Clear();
        Assert(game.Phase == SwarmPhase.Playing && game.Health == 3 && game.Weapon == SwarmWeaponKind.PixelBlaster,
            "New run starts with 3 HP and a single-shot Pixel Blaster");
        var from = game.Player; game.Advance(.1, new Vector(1, 1), from + new Vector(100, 0));
        var diagonal = (game.Player - from).Length;
        Assert(diagonal > 20 && diagonal < 28 && game.Player.X > from.X && game.Player.Y > from.Y,
            "WASD/arrow movement is delta-time based and diagonal speed is normalized");
        game.Fire(game.Player + new Vector(100, 0));
        Assert(game.Shots.Count == 1 && game.Shots[0].Velocity.X > 0 && Math.Abs(game.Shots[0].Velocity.Y) < 1,
            "Left click fires one projectile at the mouse aim point");
        game.Advance(.1, new Vector(), game.Player + new Vector(100, 0), true);
        Assert(game.Shots.Count == 1, "Holding fire does not turn the starter weapon into a machine gun");
        var elapsed = game.Survived; var position = game.Player; var shotCount = game.Shots.Count;
        game.TogglePause(); game.Advance(2, new Vector(1, 0), game.Player, true);
        Assert(game.Phase == SwarmPhase.Paused && game.Survived == elapsed && game.Player == position && game.Shots.Count == shotCount,
            "Pause freezes simulation time, movement, spawning and projectiles");
        game.TogglePause(); Assert(game.Phase == SwarmPhase.Playing, "Resume returns to the same run");
        game.Pixels.Add(new PixelSwarmGame.Pixel { Position = game.Player, Previous = game.Player, Radius = 6 });
        for (var i = 0; i < 28; i++) game.Advance(.1, new Vector(), game.Player);
        Assert(game.Health == 2 && game.InvulnerableRemaining > 0, "Contact costs one HP after grace and grants invulnerability");
        game.Advance(.1, new Vector(), game.Player);
        Assert(game.Health == 2, "Continuous contact cannot drain health every frame");
        var stage = new PixelSwarmGame(9); stage.Resize(new Size(1100, 750)); stage.Start();
        for (var i = 0; i < 260; i++) { stage.Pixels.Clear(); stage.Advance(.1, new Vector(), stage.Player); }
        Assert(stage.Stage == 2, "Stage 2 arrives by simulation time and introduces hostile pursuit");
        for (var i = 0; i < 95; i++) { stage.Pixels.Clear(); stage.Advance(.1, new Vector(), stage.Player); }
        Assert(stage.Pickups.Any(p => p.Kind == SwarmPickupKind.Relief), "Stage 2 gradually spawns moving white relief orbs");
        stage.Pickups.Clear(); stage.Pickups.Add(new PixelSwarmGame.Pickup { Position = stage.Player, Kind = SwarmPickupKind.Relief });
        stage.Advance(.01, new Vector(), stage.Player);
        Assert(stage.PowerRemaining > 0, "Collecting a relief orb temporarily suppresses hostile pursuit");
        for (var i = 0; i < 220; i++) { stage.Pixels.Clear(); stage.Advance(.1, new Vector(), stage.Player); }
        Assert(stage.Stage >= 3 && stage.Pixels.Count <= SwarmBalance.EnemyCap(stage.Stage), "Stage 3 intensity grows under a bounded enemy cap");
        var weapons = new PixelSwarmGame(12); weapons.Resize(new Size(1100, 750)); weapons.Start(); weapons.Pixels.Clear();
        weapons.Pickups.Add(new PixelSwarmGame.Pickup { Position = weapons.Player, Kind = SwarmPickupKind.MachineGun });
        weapons.Advance(.01, new Vector(), weapons.Player);
        Assert(weapons.Weapon == SwarmWeaponKind.MachineGun && weapons.Ammo > 0, "Machine Gun pickup grants temporary rapid-fire ammo");
        for (var i = 0; i < 5; i++) weapons.Advance(.1, new Vector(), weapons.Player + new Vector(100, 0), true);
        Assert(weapons.Shots.Count >= 3, "Holding the mouse fires repeatedly only with Machine Gun");
        weapons.Pickups.Add(new PixelSwarmGame.Pickup { Position = weapons.Player, Kind = SwarmPickupKind.GrenadeLauncher });
        weapons.Advance(.01, new Vector(), weapons.Player);
        Assert(weapons.Weapon == SwarmWeaponKind.GrenadeLauncher, "Grenade Launcher is a distinct limited pickup");
        weapons.Shots.Clear(); weapons.Pixels.Clear();
        weapons.Pixels.Add(new PixelSwarmGame.Pixel { Position = weapons.Player + new Vector(70, 0), Previous = weapons.Player + new Vector(70, 0), Radius = 6 });
        weapons.Pixels.Add(new PixelSwarmGame.Pixel { Position = weapons.Player + new Vector(95, 12), Previous = weapons.Player + new Vector(95, 12), Radius = 6 });
        weapons.Advance(.6, new Vector(), weapons.Player);
        weapons.Fire(weapons.Player + new Vector(100, 0));
        for (var i = 0; i < 3; i++) weapons.Advance(.1, new Vector(), weapons.Player + new Vector(100, 0));
        Assert(weapons.Destroyed >= 2 && weapons.Particles.Count > 0,
            "Grenade blast damages multiple enemies and emits bounded pixel feedback");
        var level = new PixelSwarmGame(4); level.Resize(new Size(1100, 750)); level.Start(); level.Pixels.Clear();
        var hit = typeof(PixelSwarmGame).GetMethod("Hit", BindingFlags.NonPublic | BindingFlags.Instance)!;
        for (var i = 0; i < 5; i++)
        { var enemy = new PixelSwarmGame.Pixel { Position = new Point(100, 100) }; level.Pixels.Add(enemy); hit.Invoke(level, [enemy, 1]); }
        Assert(level.Phase == SwarmPhase.LevelUp && level.UpgradeChoices.Count == 3 && level.Level == 2,
            "XP pauses play and offers exactly three eligible upgrades");
        level.ChooseUpgrade(0); Assert(level.Phase == SwarmPhase.Playing && level.UpgradeChoices.Count == 0,
            "Choosing one upgrade applies it and resumes the same run");
        level.Clear(); Assert(level.Pixels.Count == 0 && level.Shots.Count == 0 && level.Pickups.Count == 0 && level.Particles.Count == 0,
            "Leaving the run removes all temporary entities");

        var path = Path.Combine(output, "game-state.json");
        var owner = new StateStore(path, "owner-a"); var other = new StateStore(path, "owner-b");
        owner.SaveSwarmStats(new SwarmStats { BestScore = 555, HighestStage = 4, TotalRuns = 3 });
        Assert(owner.LoadSwarmStats().BestScore == 555 && other.LoadSwarmStats().BestScore == 0,
            "Persistent high scores are isolated by Nexa user and absent from Space state");

        var uiStore = new StateStore(Path.Combine(output, "swarm-ui-013.json"));
        uiStore.Save(new AppState { Notes = "Keep this note" });
        var window = new MainWindow(uiStore) { Width = 1100, Height = 760, Left = -10000, Top = -10000,
            WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(130));
        window.DurationInput.Text = "45"; Click(window, "Start now");
        var activeSession = window.state.Session!; var startAt = activeSession.Start; var endAt = activeSession.End;
        Click(window, "I'm bored"); Pump(TimeSpan.FromMilliseconds(70));
        Snapshot(window, "swarm013-menu", 1100, 760);
        Assert(window.swarmGame?.Phase == SwarmPhase.Menu && Descendants(window.StoryArea).OfType<Button>().Any(b => Equals(b.Content, "Play")),
            "I'm bored opens the Pixel Swarm start menu without starting an extra session");
        var countdownBefore = window.Countdown.Text; Pump(TimeSpan.FromSeconds(1.2));
        Assert(window.Countdown.Text != countdownBefore && ReferenceEquals(window.state.Session, activeSession) &&
            activeSession.Start == startAt && activeSession.End == endAt,
            "Pixel Swarm menu leaves the original session running without changing its timestamps");
        Descendants(window.StoryArea).OfType<Button>().First(b => Equals(b.Content, "Play")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.swarmGame?.Phase == SwarmPhase.Playing && uiStore.LoadSwarmStats().TotalRuns == 1,
            "Play begins one run and persists its run count");
        Snapshot(window, "swarm013-playing", 1100, 760);
        var escape = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), 0, Key.Escape)
        { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        window.RaiseEvent(escape);
        Assert(window.swarmGame?.Phase == SwarmPhase.Paused && Descendants(window.StoryArea).OfType<Button>().Any(b => Equals(b.Content, "Resume")),
            "Escape pauses the active game and shows Resume, Restart and Return actions");
        Snapshot(window, "swarm013-paused", 1100, 760);
        Descendants(window.StoryArea).OfType<Button>().First(b => Equals(b.Content, "Resume")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.swarmGame?.Phase == SwarmPhase.Playing, "Resume returns to gameplay");
        window.NotesBox.Text = "Edited while playing Swarm"; Pump(TimeSpan.FromSeconds(1.2));
        Assert(uiStore.Load().Notes == "Edited while playing Swarm", "Dashboard autosave continues while Pixel Swarm is open");
        Descendants(window.StoryArea).OfType<Button>().First(b => Equals(b.Content, "Return to Nexa")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(window.swarmGame == null && uiStore.Load().Notes == "Edited while playing Swarm" &&
            ReferenceEquals(window.state.Session, activeSession) && window.Countdown.Text == MainWindow.ClockText(activeSession.Remaining(DateTimeOffset.Now)),
            "Returning from Swarm preserves notes, clears the run and shows the original live countdown");
        window.Close();

        var app = Application.Current;
        Theme.Apply(app.Resources, ThemeMode.Dark, AppTheme.Nexa);
        var oldAccent = (Brush)app.Resources["AccentBrush"];
        var probe = new Border { Background = oldAccent }; var appearanceWindow = new Window { Content = probe, Width = 120, Height = 100, ShowInTaskbar = false, Left = -10000, Top = -10000 };
        appearanceWindow.Show();
        Theme.Apply(app.Resources, ThemeMode.Light, AppTheme.Ember);
        Assert(((SolidColorBrush)app.Resources["PanelBrush"]).Color == Colors.White &&
            ((SolidColorBrush)app.Resources["AccentBrush"]).Color == (Color)ColorConverter.ConvertFromString("#FB7185") &&
            ((SolidColorBrush)probe.Background).Color == (Color)ColorConverter.ConvertFromString("#FB7185"),
            "Light mode and accent update live surfaces, including controls with an old direct brush reference");
        owner.SavePreferences(ThemeMode.Light, AppTheme.Ember);
        Theme.Apply(app.Resources, ThemeMode.Dark, AppTheme.Nexa);
        Theme.Apply(app.Resources, owner.LoadThemeMode(), owner.LoadAppTheme());
        Assert(owner.LoadThemeMode() == ThemeMode.Light && owner.LoadAppTheme() == AppTheme.Ember &&
            ((SolidColorBrush)app.Resources["PanelBrush"]).Color == Colors.White,
            "Light mode and accent persist independently and restore together");
        Theme.Apply(app.Resources, ThemeMode.Dark, AppTheme.Ember);
        Assert(((SolidColorBrush)app.Resources["PanelBrush"]).Color != Colors.White &&
            ((SolidColorBrush)app.Resources["AccentBrush"]).Color == (Color)ColorConverter.ConvertFromString("#FB7185"),
            "Light to Dark switches immediately without resetting the chosen accent");
        appearanceWindow.Close();

        var appearanceStore = new StateStore(Path.Combine(output, "appearance-space.json"));
        appearanceStore.Save(new AppState()); appearanceStore.SavePreferences(ThemeMode.Dark, AppTheme.Nexa);
        Theme.Apply(app.Resources, ThemeMode.Dark, AppTheme.Nexa);
        var space = new MainWindow(appearanceStore) { Width = 1100, Height = 760, Left = -10000, Top = -10000,
            WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        space.Show(); Pump(TimeSpan.FromMilliseconds(100));
        var settings = new SettingsDialog(space, appearanceStore, space.state, space, "Appearance"); settings.Show(); Pump(TimeSpan.FromMilliseconds(70));
        var selectors = Descendants(settings).OfType<ComboBox>().ToArray();
        var modeSelector = selectors.Single(c => c.Items.Contains(ThemeMode.Light));
        var accentSelector = selectors.Single(c => c.Items.Contains(AppTheme.Aurora));
        modeSelector.SelectedItem = ThemeMode.Light; accentSelector.SelectedItem = AppTheme.Aurora;
        Pump(TimeSpan.FromMilliseconds(70));
        Assert(((SolidColorBrush)app.Resources["PanelBrush"]).Color == Colors.White &&
            ((SolidColorBrush)space.FullContent.Resources["DashboardPanelBrush"]).Color == Colors.White &&
            ((SolidColorBrush)app.Resources["AccentBrush"]).Color == (Color)ColorConverter.ConvertFromString("#38BDF8"),
            "Space Settings switches global and dashboard chrome to Light/Aurora immediately");
        Assert(((SolidColorBrush)space.Background).Color == Color.FromRgb(239, 243, 248) &&
            ((SolidColorBrush)space.DashboardHeader.Background).Color == Colors.White &&
            ((SolidColorBrush)settings.Background).Color == Color.FromRgb(239, 243, 248),
            "Light selection repaints the visible Space, header and Settings window");
        Snapshot(space, "appearance-light-space", 1100, 760);
        modeSelector.SelectedItem = ThemeMode.Dark; Pump(TimeSpan.FromMilliseconds(70));
        Assert(((SolidColorBrush)space.Background).Color != Color.FromRgb(239, 243, 248) &&
            ((SolidColorBrush)space.DashboardHeader.Background).Color != Colors.White &&
            ((SolidColorBrush)settings.Background).Color != Color.FromRgb(239, 243, 248),
            "Dark selection repaints the visible Space, header and Settings window");
        Snapshot(space, "appearance-dark-space", 1100, 760);
        modeSelector.SelectedItem = ThemeMode.Light;
        settings.FlushPendingChanges(); settings.Close(); space.Close();
        var home = new HomeWindow(appearanceStore) { Left = -10000, Top = -10000,
            WindowStartupLocation = WindowStartupLocation.Manual, ShowActivated = false };
        home.Show(); Pump(TimeSpan.FromMilliseconds(70));
        Assert(appearanceStore.LoadThemeMode() == ThemeMode.Light && appearanceStore.LoadAppTheme() == AppTheme.Aurora &&
            ((SolidColorBrush)home.Background).Color == ((SolidColorBrush)app.Resources["BackgroundBrush"]).Color,
            "Home restores the saved Light mode and accent after Space Settings closes");
        home.Close();
    }
}
