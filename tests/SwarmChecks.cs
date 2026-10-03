using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using SessionDashboard;

internal static partial class Program
{
    private static void SwarmSnapshot(MainWindow window, string name, double width, double height)
    {
        window.Width = width; window.Height = height; Pump(TimeSpan.FromMilliseconds(150));
        var content = (FrameworkElement)window.Content; Snapshot(window, name, content.ActualWidth, content.ActualHeight);
    }
    private static void CheckSwarmStandalone()
    {
        var store = new StateStore(System.IO.Path.Combine(output, "swarm-ui.json"));
        var window = new MainWindow(store) { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); window.DurationInput.Text = "45"; Click(window, "Start now");
        var now = DateTimeOffset.Now;
        window.state.Session!.Start = now.AddMinutes(-5); window.state.Session.End = now.AddMinutes(5);
        Milestones.Collect(window.state.Session, now); window.UpdateClock(); window.UpdateLayout();
        var sourcePixel = (Border)window.PixelGrid.Children[0];
        var sourceSize = new Size(sourcePixel.ActualWidth, sourcePixel.ActualHeight);
        var sourcePosition = sourcePixel.TranslatePoint(new Point(sourceSize.Width / 2, sourceSize.Height / 2), (FrameworkElement)window.Content);
        Click(window, "I'm bored"); window.UpdateLayout();
        var first = window.swarmGame!.Pixels.First();
        Assert(first.Width == sourceSize.Width && first.Height == sourceSize.Height && (first.Position - sourcePosition).Length < .01,
            "Game preserves the timer pixel's exact initial screen position and rectangular dimensions");
        var before = window.Countdown.Text; Pump(TimeSpan.FromSeconds(1.3));
        Assert(before != window.Countdown.Text, "Real session timer continues while the game is open");
        CheckSwarmUi(window, store);
        var deadline = DateTime.UtcNow.AddSeconds(31); var edits = 0;
        while (DateTime.UtcNow < deadline) { window.NotesBox.Text = "Swarm continuous edit " + edits++; Pump(TimeSpan.FromMilliseconds(200)); }
        Assert(store.Load().Notes.StartsWith("Swarm continuous edit"), "Thirty-second safety autosave works during Swarm");
        Click(window, "Back to timer"); window.Close();
        var restored = new MainWindow(store); Click(restored, "I'm bored");
        Assert(restored.swarmGame!.Score == 0 && restored.state.Notes.StartsWith("Swarm continuous edit"), "Restart preserves notes and starts a fresh run"); restored.Close();
    }
    private static void CheckSwarmModels()
    {
        var seeds = Enumerable.Range(0, 270).Select(i => new TimerPixelSeed(new Point(.1 + i % 36 * .02, .25 + i / 36 * .025), 7)
            { PixelSize = new Size(24, 14) }).ToArray();
        var seeded = new PixelSwarmGame(4, seeds); seeded.Resize(new Size(1200, 800));
        Assert(seeded.Pixels.Count == 270 && seeded.Pixels.All(p => p.FromTimer && p.Width == 24 && p.Height == 14), "Captured timer pixels keep their original rectangular size");
        var initial = seeded.Pixels.Select(p => p.Position).ToArray(); seeded.Advance(.4, seeded.Player);
        Assert(seeded.Pixels.Count == 270 && seeded.Pixels.Where((p,i) => p.Position != initial[i]).Any(), "Pixels detach and spread from their original formation");
        var game = new PixelSwarmGame(42); game.Resize(new Size(1200, 800));
        game.Advance(1, game.Player);
        Assert(game.Score == 10 && game.Pixels.Count == 24 && !game.GameOver, "Empty timers start a finite swarm with safe initial grace");
        var point = game.Player; game.Advance(.016, new Point(1900, -100));
        Assert(game.Player != point && game.Player.X < 1195 && game.Player.Y >= 5, "Player follows the mouse smoothly up to the visible edge");
        game.Pixels.Clear(); var speed = game.Speed;
        game.Advance(25, game.Player); Assert(game.Stage == 2 && game.Speed > speed, "Stage 2 begins after 25 seconds with faster enemies");
        speed = game.Speed; game.Advance(30, game.Player); Assert(game.Stage == 3 && game.Speed > speed, "Stage 3 begins after 55 seconds with stronger pursuit");
        game.Advance(40, game.Player); Assert(!game.Won, "Time and empty swarm alone cannot complete the run without all power dots");
        game.PowerDots.Clear(); game.PowerDots.AddRange(Enumerable.Repeat(game.Player, PixelSwarmGame.RequiredDots - game.Collected)); game.Advance(.016, game.Player);
        Assert(game.Won && game.Collected == 6 && game.Pixels.Count == 0, "Win requires final-stage survival, all six dots and an empty swarm");
        var score = game.Score; game.Advance(10, game.Player); Assert(score == game.Score, "Completion freezes score and survival time");
        var early = new PixelSwarmGame(8); early.Resize(new Size(1200, 800)); early.Pixels.Clear();
        early.PowerDots.Clear(); early.PowerDots.AddRange(Enumerable.Repeat(early.Player, 6)); early.Advance(.016, early.Player);
        Assert(!early.Won, "Clearing enemies and dots early still requires surviving the final stage");
        var collision = new PixelSwarmGame(7); collision.Resize(new Size(600, 400)); collision.Pixels.Clear(); collision.PowerDots.Clear(); collision.Advance(3, collision.Player);
        collision.Pixels.Add(new() { Position = collision.Player + new Vector(35, 0), Radius = 5 });
        collision.Advance(.016, collision.Player + new Vector(200, 0));
        Assert(collision.GameOver, "Swept collision catches fast cursor movement through an enemy");
        score = collision.Score; collision.Advance(10, collision.Player); Assert(collision.Score == score, "Losing freezes score and survival time");
        var shooting = new PixelSwarmGame(2); shooting.Resize(new Size(1200, 800)); shooting.Pixels.Clear();
        shooting.Pixels.Add(new() { Position = shooting.Player + new Vector(70, 0), Radius = 7, Width = 20, Height = 14 });
        shooting.Fire(); Assert(shooting.Shots.Count == 1, "One click creates one light-pixel projectile");
        shooting.Advance(.2, shooting.Player); Assert(shooting.Destroyed == 1 && shooting.Pixels.Count == 0 && shooting.Score >= 50, "A projectile destroys its enemy and awards points");
        shooting.Advance(.5, shooting.Player, true); Assert(shooting.Shots.Count >= 4, "Holding fire produces a continuous stream of projectiles");
        var power = new PixelSwarmGame(3); power.Resize(new Size(1200, 800)); power.Pixels.Clear(); power.PowerDots.Clear(); power.Advance(3, power.Player);
        power.Pixels.Add(new() { Position = power.Player + new Vector(100, 0), Radius = 6, Direction = new Vector(-1,0) });
        power.PowerDots.Add(power.Player); power.Advance(.016, power.Player);
        var distance = (power.Pixels[0].Position - power.Player).Length; power.Advance(1.5, power.Player);
        Assert(power.PowerRemaining > 0 && power.Collected == 1 && (power.Pixels[0].Position - power.Player).Length > distance, "White power dot makes enemies flee and starts a visible timed effect");
        power.Advance(6, power.Player); Assert(power.PowerRemaining == 0, "Power effect expires and normal pursuit resumes");
        var edges = new PixelSwarmGame(3); edges.Resize(new Size(720, 600)); edges.Pixels.Clear(); edges.PowerDots.Clear();
        edges.Advance(1, new Point(2000, 2000)); Assert(edges.Player == new Point(715, 595), "Player reaches the bottom/right boundary without an invisible inset");
        seeded.Resize(new Size(960, 660)); Assert(seeded.Pixels.All(p => p.Width == 24 && p.Height == 14) && seeded.GraceRemaining >= .9, "Window resize preserves pixel dimensions and grants reaction time");
        var performance = new PixelSwarmGame(13, seeds); performance.Resize(new Size(1200,800));
        var watch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 180; i++)
        {
            performance.Resize(new Size(1200 + i % 2, 800)); // maintain grace while measuring a full active swarm
            performance.Advance(1d/60, new Point(50,50), true);
        }
        watch.Stop(); Console.WriteLine($"Swarm model: 180 frames / 270 initial pixels in {watch.ElapsedMilliseconds} ms");
        Assert(performance.Shots.Count <= 80 && performance.Pixels.Count <= 270, "Entity counts stay bounded without an external game engine");
    }
    private static void CheckSwarmUi(MainWindow window, StateStore store)
    {
        if (!window.IsVisible) window.Show(); Pump(TimeSpan.FromMilliseconds(100));
        var session = window.state.Session!; var start = session.Start; var end = session.End;
        var widgets = JsonSerializer.Serialize(window.state.Widgets); var notes = window.state.Notes;
        var history = JsonSerializer.Serialize(Continuity.Current(window.state.Workspace!, store.UserId).History);
        var arena = Descendants(window.StoryArea).OfType<SwarmArena>().Single(); var content = (FrameworkElement)window.Content;
        Assert(window.SwarmGameZone.Visibility == Visibility.Visible && window.DashboardShell.Visibility == Visibility.Hidden &&
            Math.Abs(arena.ActualWidth - content.ActualWidth) < 1 && Math.Abs(arena.ActualHeight - content.ActualHeight) < 1,
            "Swarm fills the entire usable window, including former dashboard margins");
        Assert(window.SwarmPointerInside(new Point(1,1)) && window.SwarmPointerInside(new Point(arena.ActualWidth-1,arena.ActualHeight-1)) &&
            !window.SwarmPointerInside(new Point(-1,50)), "Only points outside the visible window are out of bounds");
        var capturedCount = arena.Game.Pixels.Count;
        arena.Game.Pixels.Clear(); arena.Game.PowerDots.Clear(); arena.Game.Advance(3, arena.Game.Player);
        arena.Game.Pixels.Add(new() { Position = arena.Game.Player, Radius = 5 }); arena.Game.Advance(.016, arena.Game.Player); window.ShowSwarmResult();
        Assert(Descendants(window.StoryArea).OfType<TextBlock>().Any(t => t.Text == "Game over"), "Collision displays Game over, score and retry controls");
        SwarmSnapshot(window, "swarm-game-over", 1220, 880); Click(window, "Retry"); window.UpdateLayout();
        Assert(window.swarmGame!.Score == 0 && window.swarmGame.Pixels.Count == capturedCount && !window.swarmGame.Finished, "Retry restores the captured formation with fresh run state");
        Assert(window.state.Session == session && session.Start == start && session.End == end && window.state.Notes == notes, "Retry leaves the real timer and notes intact");
        SwarmSnapshot(window, "swarm-full-window", 1220, 880); SwarmSnapshot(window, "swarm-small", 960, 660);
        var surface = Descendants(window.StoryArea).OfType<SwarmArena>().Single();
        Assert(surface.Game.Bounds == surface.RenderSize && surface.RenderSize == ((FrameworkElement)window.Content).RenderSize, "Small-window visible bounds and physics bounds match exactly");
        window.WindowState = WindowState.Maximized; Pump(TimeSpan.FromMilliseconds(150));
        Assert(surface.RenderSize == ((FrameworkElement)window.Content).RenderSize, "Maximized arena still reaches every usable edge");
        window.WindowState = WindowState.Normal; window.FullscreenButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(150));
        Assert(surface.RenderSize == ((FrameworkElement)window.Content).RenderSize, "Fullscreen arena uses the actual window dimensions");
        window.FullscreenButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(150));
        surface.Game.Pixels.Clear(); surface.Game.PowerDots.Clear(); surface.Game.Advance(90, surface.Game.Player);
        surface.Game.PowerDots.AddRange(Enumerable.Repeat(surface.Game.Player,6 - surface.Game.Collected)); surface.Game.Advance(.05,surface.Game.Player); window.ShowSwarmResult();
        Assert(surface.Game.Won && Descendants(window.StoryArea).OfType<TextBlock>().Any(t => t.Text == "Swarm cleared"), "Completion displays Swarm cleared and Play again");
        SwarmSnapshot(window, "swarm-cleared", 1220, 880); Click(window,"Play again"); window.UpdateLayout();
        var run = window.swarmGame; Click(window,"Back to timer");
        Assert(window.swarmGame == null && run!.Pixels.Count == 0 && run.Shots.Count == 0 && run.PowerDots.Count == 0 && window.DashboardShell.Visibility == Visibility.Visible, "Returning clears all temporary entities and restores the dashboard");
        Assert(JsonSerializer.Serialize(window.state.Widgets) == widgets && JsonSerializer.Serialize(Continuity.Current(window.state.Workspace!,store.UserId).History) == history &&
            session.Start == start && session.End == end && window.state.Notes == notes, "Play, retry, resize and completion leave widget data/layout, history, notes and timer timestamps unchanged");
        Assert(window.Countdown.Text == MainWindow.ClockText(session.Remaining(DateTimeOffset.Now)), "Returning shows the elapsed real session time");
        Click(window,"I'm bored"); window.NotesBox.Text = "Autosaved while playing Swarm"; Pump(TimeSpan.FromSeconds(1.2));
        Assert(store.Load().Notes == window.NotesBox.Text, "Debounced autosave continues during the overlay");
    }
}
