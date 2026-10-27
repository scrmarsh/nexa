using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private Grid? timerAreaParent;
    private TimerPixelSeed[] swarmSeeds = [];
    internal Color SwarmPixelColor { get; private set; }
    private SwarmStats? swarmStats;
    private bool swarmRecorded, swarmControlsPage, swarmStatsPage;
    private int swarmLastStage;
    private TextBlock? swarmStageText;
    private ProgressBar? swarmXpBar;
    private void StoryClick(object sender, RoutedEventArgs e)
    { if (storyOpen) ExitBoredomMode(); else StartPixelSwarm(); }

    internal void StartPixelSwarm()
    {
        if (!storyOpen)
        {
            SwarmPixelColor = ((SolidColorBrush)DashboardBrush("PixelBrush")).Color;
            UpdateClock(); UpdateLayout();
            var root = (FrameworkElement)Content;
            var now = DateTimeOffset.Now;
            var lit = Math.Min(pixels.Count, (int)Math.Floor((state.Session?.Progress(now) ?? 0) * pixels.Count) + (state.Session?.IsRunning(now) == true ? 1 : 0));
            swarmSeeds = pixels.Take(lit).Where(p => p.ActualWidth > 0 && p.ActualHeight > 0).Select(pixel =>
            {
                var point = pixel.TranslatePoint(new Point(pixel.ActualWidth / 2, pixel.ActualHeight / 2), root);
                return new TimerPixelSeed(new Point(point.X / root.ActualWidth, point.Y / root.ActualHeight), Math.Min(pixel.ActualWidth, pixel.ActualHeight) / 2)
                { PixelSize = new Size(pixel.ActualWidth, pixel.ActualHeight) };
            }).Where(seed => seed.RelativePosition.X is >= 0 and <= 1 && seed.RelativePosition.Y is >= 0 and <= 1).ToArray();
            timerAreaParent = (Grid)StoryArea.Parent;
            Detach(StoryArea); StoryArea.MinHeight = 0; SwarmGameZone.Children.Add(StoryArea);
            DashboardShell.Visibility = Visibility.Hidden; SwarmGameZone.Visibility = Visibility.Visible;
            SwarmGameZone.Background = DashboardBrush("BackgroundBrush");
            swarmStats = store.LoadSwarmStats();
        }
        swarmGame?.Clear(); swarmGame = new PixelSwarmGame(timerPixels: swarmSeeds);
        swarmRecorded = false; swarmControlsPage = swarmStatsPage = false; swarmLastStage = 1;
        storyOpen = true; PixelArea.Visibility = Visibility.Collapsed; StoryArea.Visibility = Visibility.Visible;
        StoryButton.Content = "Return to Nexa"; UpdateClock(); RenderStory();
        swarmFrameClock.Restart(); boredomTicker.Start();
    }
    private void BeginSwarmRun()
    {
        if (swarmGame == null) return;
        SaveSwarmRun();
        swarmGame.Start(); swarmLastStage = 1; swarmRecorded = false;
        swarmStats ??= store.LoadSwarmStats(); swarmStats.TotalRuns++;
        try { store.SaveSwarmStats(swarmStats); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Feedback.Text = "Could not save Pixel Swarm stats yet."; }
        swarmFrameClock.Restart(); ShowSwarmOverlay(); UpdateSwarmScore(); swarmArena?.InvalidateVisual();
    }
    private void SaveSwarmRun()
    {
        if (swarmRecorded || swarmGame == null || swarmStats == null || swarmGame.Survived <= 0) return;
        swarmRecorded = true;
        var game = swarmGame;
        game.NewBest = game.Score > swarmStats.BestScore;
        swarmStats.BestScore = Math.Max(swarmStats.BestScore, game.Score);
        swarmStats.HighestStage = Math.Max(swarmStats.HighestStage, game.Stage);
        swarmStats.LongestRun = Math.Max(swarmStats.LongestRun, game.Survived);
        swarmStats.TotalEnemiesDefeated += game.Destroyed;
        try { store.SaveSwarmStats(swarmStats); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Feedback.Text = "Could not save Pixel Swarm stats yet."; }
    }
    internal void ExitBoredomMode()
    {
        SaveSwarmRun(); boredomTicker.Stop(); swarmFrameClock.Reset(); storyOpen = false;
        swarmGame?.Clear(); swarmGame = null; swarmArena = null;
        swarmScoreText = swarmPauseText = swarmStageText = null; swarmXpBar = null; swarmResult = null;
        StoryArea.Children.Clear(); Detach(StoryArea); timerAreaParent?.Children.Insert(1, StoryArea); timerAreaParent = null;
        StoryArea.MinHeight = 265; SwarmGameZone.Children.Clear(); SwarmGameZone.Visibility = Visibility.Collapsed;
        DashboardShell.Visibility = Visibility.Visible; swarmSeeds = [];
        StoryArea.Visibility = Visibility.Collapsed; PixelArea.Visibility = Visibility.Visible; StoryButton.Content = "I'm bored";
        UpdateClock();
    }
    private static string SurvivalText(double seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
    internal bool SwarmPointerInside(Point point) => swarmArena != null && point.X >= 0 && point.Y >= 0 && point.X <= swarmArena.ActualWidth && point.Y <= swarmArena.ActualHeight;
    private Vector SwarmMovement()
    {
        var x = (Keyboard.IsKeyDown(Key.D) || Keyboard.IsKeyDown(Key.Right) ? 1 : 0) - (Keyboard.IsKeyDown(Key.A) || Keyboard.IsKeyDown(Key.Left) ? 1 : 0);
        var y = (Keyboard.IsKeyDown(Key.S) || Keyboard.IsKeyDown(Key.Down) ? 1 : 0) - (Keyboard.IsKeyDown(Key.W) || Keyboard.IsKeyDown(Key.Up) ? 1 : 0);
        return new Vector(x, y);
    }
    private void UpdateBoredomGame()
    {
        var elapsed = swarmFrameClock.Elapsed.TotalSeconds; swarmFrameClock.Restart();
        if (!storyOpen || swarmArena == null || swarmGame == null) return;
        if (swarmGame.Phase == SwarmPhase.Playing && !IsActive)
        { swarmGame.TogglePause(); ShowSwarmOverlay(); return; }
        if (swarmGame.Phase != SwarmPhase.Playing) return;
        // WPF reports this in arena DIPs, including on mixed-DPI displays.
        var pointer = Mouse.GetPosition(swarmArena);
        swarmGame.Advance(elapsed, SwarmMovement(), pointer, Mouse.LeftButton == MouseButtonState.Pressed);
        if (swarmGame.Stage != swarmLastStage)
        { swarmLastStage = swarmGame.Stage; if (swarmStageText != null) { swarmStageText.Text = $"STAGE {swarmLastStage}"; swarmStageText.Opacity = 1; } }
        else if (swarmStageText != null) swarmStageText.Opacity = Math.Max(0, swarmStageText.Opacity - elapsed / 2);
        swarmArena.InvalidateVisual(); UpdateSwarmScore();
        if (swarmGame.Phase != SwarmPhase.Playing)
        { if (swarmGame.GameOver) SaveSwarmRun(); ShowSwarmOverlay(); }
    }
    private void UpdateSwarmScore()
    {
        if (swarmScoreText == null || swarmGame == null) return;
        var game = swarmGame;
        swarmScoreText.Text = $"♥ {game.Health}/{game.MaxHealth}    STAGE {game.Stage}    SCORE {game.Score:N0}    {SurvivalText(game.Survived)}\n" +
            $"LEVEL {game.Level}    {SwarmBalance.Definition(game.Weapon).Name}" +
            (game.Weapon == SwarmWeaponKind.PixelBlaster ? "" : $" · {game.Ammo} shots") +
            (game.PowerRemaining > 0 ? $"    RELIEF {Math.Ceiling(game.PowerRemaining)}s" : "");
        if (swarmXpBar != null) { swarmXpBar.Maximum = game.ExperienceGoal; swarmXpBar.Value = Math.Min(game.Experience, game.ExperienceGoal); }
    }
    internal void ShowSwarmResult() { SaveSwarmRun(); ShowSwarmOverlay(); }
    private void ShowSwarmOverlay()
    {
        if (swarmGame == null || swarmResult == null) return;
        var game = swarmGame;
        swarmResult.Visibility = game.Phase == SwarmPhase.Playing ? Visibility.Collapsed : Visibility.Visible;
        if (game.Phase == SwarmPhase.Playing) return;
        var stack = new StackPanel { Margin = new Thickness(26), Width = 330 };
        void Label(string value, int size = 15)
            => stack.Children.Add(new TextBlock { Text = value, Foreground = DashboardBrush("TextBrush"), FontSize = size,
                FontWeight = size > 20 ? FontWeights.Bold : FontWeights.Normal, TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) });
        void Action(string name, Action action)
        { var button = new Button { Content = name, MinHeight = 38, Margin = new Thickness(0, 0, 0, 8) }; button.Click += (_, _) => action(); stack.Children.Add(button); }
        switch (game.Phase)
        {
            case SwarmPhase.Menu:
                Label(swarmControlsPage ? "CONTROLS" : swarmStatsPage ? "STATS" : "PIXEL SWARM", 29);
                if (swarmControlsPage) Label("Move: WASD / Arrow Keys\nAim: Mouse\nShoot: Left Click\nPause: Esc");
                else if (swarmStatsPage)
                {
                    var stats = swarmStats ?? new SwarmStats();
                    Label($"Best score: {stats.BestScore:N0}\nHighest stage: {stats.HighestStage}\nLongest run: {SurvivalText(stats.LongestRun)}\nEnemies defeated: {stats.TotalEnemiesDefeated:N0}\nTotal runs: {stats.TotalRuns:N0}");
                }
                else Label($"Survive, aim, and collect upgrades.\nBest score: {swarmStats?.BestScore ?? 0:N0}");
                if (swarmControlsPage || swarmStatsPage) Action("Back", () => { swarmControlsPage = swarmStatsPage = false; ShowSwarmOverlay(); });
                else { Action("Play", BeginSwarmRun); Action("Controls", () => { swarmControlsPage = true; ShowSwarmOverlay(); }); Action("Stats", () => { swarmStatsPage = true; ShowSwarmOverlay(); }); }
                Action("Return to Nexa", ExitBoredomMode); break;
            case SwarmPhase.Paused:
                Label("PAUSED", 29); Label("Your Nexa session timer keeps running.");
                Action("Resume", () => { game.TogglePause(); swarmFrameClock.Restart(); ShowSwarmOverlay(); });
                Action("Restart", BeginSwarmRun); Action("Return to Nexa", ExitBoredomMode); break;
            case SwarmPhase.LevelUp:
                Label($"LEVEL {game.Level} — CHOOSE ONE", 25);
                for (var i = 0; i < game.UpgradeChoices.Count; i++)
                { var index = i; var choice = game.UpgradeChoices[i]; Action($"{choice.Name} · {choice.Description}", () => { game.ChooseUpgrade(index); swarmFrameClock.Restart(); ShowSwarmOverlay(); }); }
                break;
            case SwarmPhase.GameOver:
                Label("GAME OVER", 29);
                Label($"Score {game.Score:N0}{(game.NewBest ? " · NEW BEST" : "")}\nBest {swarmStats?.BestScore ?? 0:N0} · Stage {game.Stage}\n{game.Destroyed} defeated · {SurvivalText(game.Survived)} survived · Level {game.Level}");
                Action("Play Again", BeginSwarmRun); Action("Return to Nexa", ExitBoredomMode); break;
        }
        swarmResult.Child = stack;
    }
    private void RenderStory()
    {
        if (swarmGame == null) return;
        StoryArea.Children.Clear();
        var root = new Grid { ClipToBounds = true };
        swarmArena = new SwarmArena(swarmGame, ((SolidColorBrush)DashboardBrush("AccentBrush")).Color,
            ((SolidColorBrush)DashboardBrush("BackgroundBrush")).Color, ((SolidColorBrush)DashboardBrush("TextBrush")).Color, SwarmPixelColor);
        root.Children.Add(swarmArena);
        swarmArena.MouseLeftButtonDown += (_, e) => { swarmGame?.Fire(Mouse.GetPosition(swarmArena)); swarmArena.Focus(); e.Handled = true; };
        var hud = DashboardBrush("PanelBrush").Clone(); hud.Opacity = .9;
        swarmScoreText = new TextBlock { Foreground = DashboardBrush("TextBrush"), FontWeight = FontWeights.SemiBold, FontSize = 13,
            Margin = new Thickness(12), Padding = new Thickness(10, 7, 10, 7), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, Background = hud, IsHitTestVisible = false };
        root.Children.Add(swarmScoreText);
        swarmStageText = new TextBlock { Foreground = DashboardBrush("AccentBrush"), FontSize = 32, FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 20, 0, 0), IsHitTestVisible = false, Opacity = 0 };
        root.Children.Add(swarmStageText);
        var back = new Button { Content = "Return to Nexa", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(12) };
        back.Click += (_, _) => ExitBoredomMode(); root.Children.Add(back);
        swarmXpBar = new ProgressBar { Height = 6, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12, 0, 12, 12), IsHitTestVisible = false };
        root.Children.Add(swarmXpBar);
        swarmResult = new Border { Background = DashboardBrush("PanelBrush"), CornerRadius = new CornerRadius(12), Padding = new Thickness(10),
            BorderBrush = DashboardBrush("LineBrush"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
        root.Children.Add(swarmResult); StoryArea.Children.Add(root); UpdateSwarmScore(); ShowSwarmOverlay();
    }
}
