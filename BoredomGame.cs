using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;

internal readonly record struct TimerPixelSeed(Point RelativePosition, double Radius)
{
    internal Size PixelSize { get; init; }
}

// All entities belong to this disposable run, never to a saved dashboard.
internal sealed class PixelSwarmGame
{
    internal sealed class Pixel
    {
        public Point Position, Previous;
        public Vector Direction;
        public double Radius, Age, Phase;
        public double Width, Height, Scale = 1;
        public int Pattern;
        public bool FromTimer;
        public double HalfWidth => (Width > 0 ? Width / 2 : Radius) * Scale;
        public double HalfHeight => (Height > 0 ? Height / 2 : Radius) * Scale;
    }
    internal sealed class Shot
    {
        public Point Position;
        public Vector Velocity;
        public double Age;
        public int Pierces = 1;
    }
    private readonly Random random;
    private readonly TimerPixelSeed[] timerPixels;
    internal readonly List<Pixel> Pixels = [];
    internal readonly List<Shot> Shots = [];
    internal readonly List<Point> PowerDots = [];
    internal Point Player { get; private set; }
    internal Size Bounds { get; private set; }
    internal double Survived { get; private set; }
    internal int Destroyed { get; private set; }
    internal int Collected { get; private set; }
    internal int Score => (int)Math.Floor(Survived * 10 + .000001) + Destroyed * 50 + Collected * 100;
    internal bool GameOver { get; private set; }
    internal bool Won { get; private set; }
    internal bool Finished => GameOver || Won;
    internal int Stage => Survived < 25 ? 1 : Survived < 55 ? 2 : 3;
    internal const double RequiredTime = 90;
    internal const int RequiredDots = 6;
    internal const double PlayerRadius = 5;
    internal double GraceRemaining { get; private set; } = 2.5;
    internal double PowerRemaining { get; private set; }
    internal double Speed => Stage switch { 1 => 26 + Survived * .3, 2 => 39 + (Survived - 25) * .35, _ => Math.Min(76, 56 + (Survived - 55) * .3) };
    internal double SpawnInterval => double.PositiveInfinity; // A finite swarm can be cleared.
    private double shotCooldown;
    private Vector aim = new(0, -1);
    internal PixelSwarmGame(int? seed = null, IReadOnlyList<TimerPixelSeed>? timerPixels = null)
    {
        random = seed.HasValue ? new Random(seed.Value) : new Random();
        this.timerPixels = timerPixels?.ToArray() ?? [];
    }
    internal void Resize(Size size)
    {
        if (size.Width < 20 || size.Height < 20 || size == Bounds) return;
        var old = Bounds; Bounds = size;
        if (old.Width == 0)
        {
            Player = new Point(size.Width / 2, size.Height * .8);
            foreach (var seed in timerPixels)
                Pixels.Add(new Pixel { Position = new(seed.RelativePosition.X * size.Width, seed.RelativePosition.Y * size.Height),
                    Radius = seed.Radius, Width = seed.PixelSize.Width, Height = seed.PixelSize.Height,
                    Phase = random.NextDouble() * Math.PI * 2, Pattern = random.Next(3), FromTimer = true });
            // An empty timer still offers a finite run, using timer-shaped edge pixels.
            if (Pixels.Count == 0)
                for (var i = 0; i < 24; i++)
                    Pixels.Add(new Pixel { Position = new(i % 2 == 0 ? 15 : size.Width - 15, 20 + (i / 2) * (size.Height - 40) / 12),
                        Radius = 7, Width = 16, Height = 14, Phase = random.NextDouble() * Math.PI * 2, Pattern = i % 3 });
            foreach (var pixel in Pixels)
            {
                // Outward drift starts from each pixel's exact on-screen position.
                var outward = pixel.Position - new Point(size.Width / 2, size.Height / 2);
                if (outward.Length < .01) outward = new Vector(Math.Cos(pixel.Phase), Math.Sin(pixel.Phase));
                outward.Normalize(); pixel.Direction = outward; pixel.Previous = pixel.Position;
            }
            for (var i = 0; i < RequiredDots; i++)
                PowerDots.Add(new Point(size.Width * (.1 + (i % 3) * .4), size.Height * (.24 + (i / 3) * .53)));
            return;
        }
        Point Scale(Point p) => new(p.X * size.Width / old.Width, p.Y * size.Height / old.Height);
        Player = Clamp(Scale(Player));
        foreach (var pixel in Pixels) { pixel.Position = KeepPixelInside(Scale(pixel.Position), pixel); pixel.Previous = pixel.Position; }
        foreach (var shot in Shots) shot.Position = Scale(shot.Position);
        for (var i = 0; i < PowerDots.Count; i++) PowerDots[i] = Scale(PowerDots[i]);
        GraceRemaining = Math.Max(GraceRemaining, .9);
    }
    private Point Clamp(Point p) => new(Math.Clamp(p.X, PlayerRadius, Bounds.Width - PlayerRadius), Math.Clamp(p.Y, PlayerRadius, Bounds.Height - PlayerRadius));
    private Point KeepPixelInside(Point p, Pixel pixel)
    {
        var x = Math.Min(pixel.HalfWidth, Bounds.Width / 2); var y = Math.Min(pixel.HalfHeight, Bounds.Height / 2);
        return new(Math.Clamp(p.X, x, Bounds.Width - x), Math.Clamp(p.Y, y, Bounds.Height - y));
    }
    internal void Fire()
    {
        if (Finished || Bounds.Width < 20 || shotCooldown > 0 || Shots.Count >= 80) return;
        if (Pixels.Count > 0)
        {
            var nearest = Pixels.MinBy(p => (p.Position - Player).LengthSquared)!;
            var direction = nearest.Position - Player;
            if (direction.Length > .01) { direction.Normalize(); aim = direction; }
        }
        Shots.Add(new Shot { Position = Player, Velocity = aim * 680, Pierces = PowerRemaining > 0 ? 3 : 1 });
        shotCooldown = PowerRemaining > 0 ? .065 : .09;
    }
    internal void Advance(double seconds, Point target, bool firing = false)
    {
        if (Finished || Bounds.Width < 20 || Bounds.Height < 20 || seconds <= 0) return;
        target = Clamp(target);
        while (seconds > 0 && !Finished)
        {
            var dt = Math.Min(seconds, .016); seconds -= dt;
            var previousPlayer = Player;
            Player += (target - Player) * (1 - Math.Exp(-28 * dt));
            if ((target - Player).Length < .1) Player = target;
            Survived += dt; GraceRemaining = Math.Max(0, GraceRemaining - dt);
            PowerRemaining = Math.Max(0, PowerRemaining - dt); shotCooldown = Math.Max(0, shotCooldown - dt);
            for (var i = PowerDots.Count - 1; i >= 0; i--)
                if (Touches(previousPlayer - PowerDots[i], Player - PowerDots[i], 12, 12))
                { PowerDots.RemoveAt(i); Collected++; PowerRemaining = 7; }
            if (firing) Fire();
            foreach (var pixel in Pixels)
            {
                pixel.Previous = pixel.Position; pixel.Age += dt;
                pixel.Scale = Stage == 3 ? 1 + .13 * Math.Sin(pixel.Age * 2.1 + pixel.Phase) : Stage == 2 ? 1 + .04 * Math.Sin(pixel.Age + pixel.Phase) : 1;
                var desired = PowerRemaining > 0 ? pixel.Position - Player : Player - pixel.Position;
                if (desired.Length > .01) desired.Normalize();
                if (Survived < 2.2) desired = pixel.Direction;
                else if (Stage >= 2 && PowerRemaining <= 0)
                {
                    var weave = Math.Sin(pixel.Age * (Stage == 3 ? 3.2 : 1.8) + pixel.Phase) * (Stage == 3 ? .75 : .35);
                    desired += new Vector(-desired.Y, desired.X) * weave;
                }
                pixel.Direction += (desired - pixel.Direction) * (1 - Math.Exp(-(Stage + (PowerRemaining > 0 ? 4 : 1.2)) * dt));
                var velocity = pixel.Direction * Speed * (.88 + .18 * Math.Sin(pixel.Phase)) * (PowerRemaining > 0 ? 1.15 : 1);
                pixel.Position = KeepPixelInside(pixel.Position + velocity * dt, pixel);
            }
            for (var i = Shots.Count - 1; i >= 0; i--)
            {
                var shot = Shots[i]; var previous = shot.Position; shot.Position += shot.Velocity * dt; shot.Age += dt;
                for (var j = Pixels.Count - 1; j >= 0 && shot.Pierces > 0; j--)
                {
                    var pixel = Pixels[j];
                    if (!Touches(previous - pixel.Previous, shot.Position - pixel.Position, pixel.HalfWidth + 3, pixel.HalfHeight + 3)) continue;
                    Pixels.RemoveAt(j); Destroyed++; shot.Pierces--;
                }
                if (shot.Pierces <= 0 || shot.Age > 2 || shot.Position.X < 0 || shot.Position.X > Bounds.Width || shot.Position.Y < 0 || shot.Position.Y > Bounds.Height) Shots.RemoveAt(i);
            }
            if (GraceRemaining <= 0)
                for (var i = Pixels.Count - 1; i >= 0; i--)
                {
                    var pixel = Pixels[i];
                    if (!Touches(previousPlayer - pixel.Previous, Player - pixel.Position, pixel.HalfWidth + PlayerRadius, pixel.HalfHeight + PlayerRadius)) continue;
                    if (PowerRemaining > 0) { Pixels.RemoveAt(i); Destroyed++; }
                    else { GameOver = true; break; }
                }
            if (!GameOver && Survived >= RequiredTime && Collected == RequiredDots && Pixels.Count == 0) Won = true;
        }
    }
    // Swept rectangle collision avoids tunneling through pixels between frames.
    private static bool Touches(Vector from, Vector to, double halfWidth, double halfHeight)
    {
        var delta = to - from; var low = 0d; var high = 1d;
        bool Axis(double start, double movement, double extent)
        {
            if (Math.Abs(movement) < .000001) return Math.Abs(start) <= extent;
            var a = (-extent - start) / movement; var b = (extent - start) / movement;
            low = Math.Max(low, Math.Min(a, b)); high = Math.Min(high, Math.Max(a, b)); return low <= high;
        }
        return Axis(from.X, delta.X, halfWidth) && Axis(from.Y, delta.Y, halfHeight);
    }
    internal void Clear() { Pixels.Clear(); Shots.Clear(); PowerDots.Clear(); }
}

// A single lightweight visual draws every pixel, projectile and power dot.
internal sealed class SwarmArena : FrameworkElement
{
    internal PixelSwarmGame Game { get; }
    private readonly Brush[] enemies = [Frozen(Color.FromRgb(172, 101, 109)), Frozen(Color.FromRgb(221, 60, 77)), Frozen(Color.FromRgb(255, 30, 58))];
    private readonly Brush background;
    private readonly Brush vulnerable = Frozen(Color.FromRgb(136, 188, 242));
    private readonly Brush whiteGlow, themeText;
    private readonly Brush player, glow;
    private readonly Pen outline;
    private static Brush Frozen(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
    internal SwarmArena(PixelSwarmGame game, Color accent)
    {
        Game = game; player = Frozen(accent); glow = Frozen(Color.FromArgb(45, accent.R, accent.G, accent.B));
        var backgroundColor = ((SolidColorBrush)Application.Current.FindResource("BackgroundBrush")).Color;
        var textColor = ((SolidColorBrush)Application.Current.FindResource("TextBrush")).Color;
        background = Frozen(backgroundColor); themeText = Frozen(textColor); whiteGlow = Frozen(Color.FromArgb(45, textColor.R, textColor.G, textColor.B)); outline = new Pen(themeText, 1);
        ClipToBounds = true; outline.Freeze();
        SizeChanged += (_, _) => { Game.Resize(RenderSize); InvalidateVisual(); };
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); dc.DrawRectangle(background, null, new Rect(RenderSize));
        var enemy = Game.PowerRemaining > 0 ? vulnerable : enemies[Game.Stage - 1];
        foreach (var pixel in Game.Pixels)
            dc.DrawRoundedRectangle(enemy, null, new Rect(pixel.Position.X - pixel.HalfWidth, pixel.Position.Y - pixel.HalfHeight, pixel.HalfWidth * 2, pixel.HalfHeight * 2), 2, 2);
        foreach (var dot in Game.PowerDots)
        {
            dc.DrawEllipse(whiteGlow, null, dot, 13, 13);
            dc.DrawRoundedRectangle(themeText, null, new Rect(dot.X - 4, dot.Y - 4, 8, 8), 2, 2);
        }
        foreach (var shot in Game.Shots) dc.DrawRectangle(themeText, null, new Rect(shot.Position.X - 2, shot.Position.Y - 2, 4, 4));
        dc.DrawEllipse(Game.PowerRemaining > 0 ? whiteGlow : glow, null, Game.Player, Game.PowerRemaining > 0 ? 22 : 12, Game.PowerRemaining > 0 ? 22 : 12);
        dc.DrawRoundedRectangle(player, outline, new Rect(Game.Player.X - 5, Game.Player.Y - 5, 10, 10), 2, 2);
        if (Game.GraceRemaining > 0 || Game.PowerRemaining > 0) dc.DrawEllipse(null, outline, Game.Player, 17, 17);
    }
}
