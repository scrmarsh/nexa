using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private Grid? timerAreaParent;
    private TimerPixelSeed[] swarmSeeds = [];
    private void StoryClick(object sender, RoutedEventArgs e)
    {
        if (storyOpen) ExitBoredomMode(); else StartPixelSwarm();
    }
    internal void StartPixelSwarm()
    {
        if (!storyOpen)
        {
            UpdateClock(); UpdateLayout();
            var root = (FrameworkElement)Content;
            var now = DateTimeOffset.Now;
            var lit = Math.Min(pixels.Count, (int)Math.Floor((state.Session?.Progress(now) ?? 0) * pixels.Count) + (state.Session?.IsRunning(now) == true ? 1 : 0));
            swarmSeeds = pixels.Take(lit).Where(p => p.ActualWidth > 0 && p.ActualHeight > 0).Select(pixel =>
            {
                var point = pixel.TranslatePoint(new Point(pixel.ActualWidth / 2, pixel.ActualHeight / 2), root);
                return new TimerPixelSeed(new Point(point.X / root.ActualWidth, point.Y / root.ActualHeight), Math.Min(pixel.ActualWidth, pixel.ActualHeight) / 2)
                { PixelSize = new Size(pixel.ActualWidth, pixel.ActualHeight) };
            }).Where(seed => seed.RelativePosition.X >= 0 && seed.RelativePosition.X <= 1 && seed.RelativePosition.Y >= 0 && seed.RelativePosition.Y <= 1).ToArray();
            timerAreaParent = (Grid)StoryArea.Parent;
            Detach(StoryArea); StoryArea.MinHeight = 0; SwarmGameZone.Children.Add(StoryArea);
            DashboardShell.Visibility = Visibility.Hidden;
            SwarmGameZone.Visibility = Visibility.Visible;
        }
        swarmGame?.Clear(); swarmGame = new PixelSwarmGame(timerPixels: swarmSeeds);
        storyOpen = true; PixelArea.Visibility = Visibility.Collapsed; StoryArea.Visibility = Visibility.Visible;
        StoryButton.Content = "Back to timer"; UpdateClock(); RenderStory();
        swarmFrameClock.Restart(); boredomTicker.Start();
    }
    internal void ExitBoredomMode()
    {
        boredomTicker.Stop(); swarmFrameClock.Reset(); storyOpen = false;
        swarmGame?.Clear(); swarmGame = null; swarmArena = null;
        swarmScoreText = swarmPauseText = null; swarmResult = null;
        StoryArea.Children.Clear(); Detach(StoryArea); timerAreaParent?.Children.Insert(1, StoryArea); timerAreaParent = null;
        StoryArea.MinHeight = 265; SwarmGameZone.Children.Clear(); SwarmGameZone.Visibility = Visibility.Collapsed;
        DashboardShell.Visibility = Visibility.Visible; swarmSeeds = [];
        StoryArea.Visibility = Visibility.Collapsed; PixelArea.Visibility = Visibility.Visible; StoryButton.Content = "I'm bored";
        UpdateClock();
    }
    private static string SurvivalText(double seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";
    internal bool SwarmPointerInside(Point point) => swarmArena != null && point.X >= 0 && point.Y >= 0 && point.X <= swarmArena.ActualWidth && point.Y <= swarmArena.ActualHeight;
    private void UpdateBoredomGame()
    {
        var elapsed = swarmFrameClock.Elapsed.TotalSeconds; swarmFrameClock.Restart();
        if (!storyOpen || swarmArena == null || swarmGame == null || swarmGame.Finished) return;
        var pointer = Mouse.GetPosition(swarmArena);
        // Bounds, rather than IsMouseOver, keep HUD controls and every visible interior point playable.
        var playing = IsActive && swarmArena.IsVisible && SwarmPointerInside(pointer);
        swarmPauseText!.Visibility = playing ? Visibility.Collapsed : Visibility.Visible;
        UpdateSwarmScore();
        if (!playing) return;
        swarmGame.Advance(Math.Min(elapsed, .1), pointer, Mouse.LeftButton == MouseButtonState.Pressed);
        swarmArena.InvalidateVisual(); UpdateSwarmScore();
        if (swarmGame.Finished) ShowSwarmResult();
    }
    private void UpdateSwarmScore()
    {
        if (swarmScoreText == null || swarmGame == null) return;
        var game = swarmGame;
        swarmScoreText.Text = $"Stage {game.Stage}/3 · Score {game.Score:N0} · {SurvivalText(game.Survived)} / 01:30\n" +
            $"{game.Pixels.Count} pixels left · Power dots {game.Collected}/{PixelSwarmGame.RequiredDots} · Timer {Countdown.Text}" +
            (game.PowerRemaining > 0 ? $"\nPOWER · Swarm fleeing · {Math.Ceiling(game.PowerRemaining)}s" : "");
    }
    internal void ShowSwarmResult()
    {
        if (swarmGame == null || swarmResult == null) return;
        if (swarmGame.Won && swarmResult.Visibility != Visibility.Visible) PlayInterfaceSound();
        UpdateSwarmScore(); boredomTicker.Stop(); swarmPauseText!.Visibility = Visibility.Collapsed;
        var stack = new StackPanel { Margin = new Thickness(28) };
        stack.Children.Add(new TextBlock { Text = swarmGame.Won ? "Swarm cleared" : "Game over", Foreground = Brush("TextBrush"),
            FontSize = 30, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = $"Score: {swarmGame.Score:N0}\nTime: {SurvivalText(swarmGame.Survived)}",
            Foreground = Brush("TextBrush"), Margin = new Thickness(0, 12, 0, 18), TextAlignment = TextAlignment.Center, FontSize = 18 });
        var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var retry = new Button { Content = swarmGame.Won ? "Play again" : "Retry", MinWidth = 100, Margin = new Thickness(0, 0, 8, 0) };
        retry.Click += (_, _) => StartPixelSwarm(); controls.Children.Add(retry);
        var back = new Button { Content = "Back to timer" }; back.Click += (_, _) => ExitBoredomMode(); controls.Children.Add(back);
        stack.Children.Add(controls); swarmResult.Child = stack; swarmResult.Visibility = Visibility.Visible;
    }
    private void RenderStory()
    {
        if (swarmGame == null) return;
        StoryArea.Children.Clear();
        var root = new Grid { ClipToBounds = true };
        swarmArena = new SwarmArena(swarmGame, ((SolidColorBrush)Brush("AccentBrush")).Color);
        swarmArena.SizeChanged += (_, _) => UpdateSwarmScore();
        root.Children.Add(swarmArena);
        swarmArena.MouseLeftButtonDown += (_, e) => { swarmGame?.Fire(); e.Handled = true; };
        var hud = Brush("PanelBrush").Clone(); hud.Opacity = .9;
        swarmScoreText = new TextBlock { Foreground = Brush("TextBrush"), FontWeight = FontWeights.SemiBold, FontSize = 13,
            Margin = new Thickness(14), Padding = new Thickness(10, 7, 10, 7), TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Background = hud, IsHitTestVisible = false };
        root.Children.Add(swarmScoreText);
        var back = new Button { Content = "Back to timer", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(14) };
        back.Click += (_, _) => ExitBoredomMode(); root.Children.Add(back);
        var hint = new TextBlock { Text = "Move to dodge · Click or hold to fire at the nearest pixel · Collect white dots · Clear the swarm after 90 seconds",
            Foreground = Brush("TextBrush"), FontSize = 12, Background = hud,
            Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(10), TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
        root.Children.Add(hint);
        swarmPauseText = new TextBlock { Text = "Move inside the window to play\nThe session timer keeps running while the game is paused.",
            Foreground = Brush("TextBrush"), Background = hud, Padding = new Thickness(18),
            TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        root.Children.Add(swarmPauseText);
        swarmResult = new Border { Background = Brush("PanelBrush"), CornerRadius = new CornerRadius(14),
            BorderBrush = Brush("LineBrush"), BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };
        root.Children.Add(swarmResult); StoryArea.Children.Add(root); UpdateSwarmScore();
        if (swarmGame.Finished) ShowSwarmResult();
    }
}
