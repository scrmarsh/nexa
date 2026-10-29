using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;

internal readonly record struct TimerPixelSeed(Point RelativePosition, double Radius)
{
    internal Size PixelSize { get; init; }
}

// This simulation owns all run-only state; the window owns its one frame timer.
internal sealed class PixelSwarmGame
{
    internal sealed class Pixel
    {
        public Point Position, Previous;
        public Vector Direction;
        public double Radius = 7, Width, Height, Scale = 1, Age, Phase, AttackClock, DashClock;
        public int Pattern, Health = 1;
        public bool FromTimer, Elite;
        public SwarmEnemyKind Kind;
        public double HalfWidth => (Width > 0 ? Width / 2 : Radius) * Scale;
        public double HalfHeight => (Height > 0 ? Height / 2 : Radius) * Scale;
    }
    internal sealed class Shot
    {
        public Point Position, Previous;
        public Vector Velocity;
        public double Age, Radius = 3, Explosion;
        public int Damage = 1, Pierces = 1;
        public bool Hostile;
    }
    internal sealed class Pickup
    {
        public Point Position;
        public Vector Velocity;
        public SwarmPickupKind Kind;
        public double Age;
    }
    internal sealed class Particle
    {
        public Point Position;
        public Vector Velocity;
        public double Age, Lifetime = .35;
        public Color Color;
    }
    internal sealed record Upgrade(string Name, string Description, string Key);

    private readonly Random random;
    private readonly TimerPixelSeed[] seeds;
    internal readonly List<Pixel> Pixels = [];
    internal readonly List<Shot> Shots = [];
    internal readonly List<Point> PowerDots = []; // Kept for old save-free callers; moving relief uses Pickups.
    internal readonly List<Pickup> Pickups = [];
    internal readonly List<Particle> Particles = [];
    internal readonly List<Upgrade> UpgradeChoices = [];
    internal Point Player { get; private set; }
    internal Size Bounds { get; private set; }
    internal SwarmPhase Phase { get; private set; } = SwarmPhase.Menu;
    internal double Survived { get; private set; }
    internal int Destroyed { get; private set; }
    internal int Collected { get; private set; }
    internal int Score => (int)Survived * 5 + Destroyed * 50 + Collected * 25 + Math.Max(0, Stage - 1) * 100;
    internal bool GameOver => Phase == SwarmPhase.GameOver;
    internal bool Won => false;
    internal bool Finished => GameOver;
    internal int Stage => Survived < SwarmBalance.StageTwoAt ? 1 : Survived < SwarmBalance.StageThreeAt ? 2 : 3 + (int)((Survived - SwarmBalance.StageThreeAt) / 35);
    internal const double RequiredTime = 90;
    internal const int RequiredDots = 6;
    internal const double PlayerRadius = 6;
    internal int Health { get; private set; } = SwarmBalance.MaxHealth;
    internal int MaxHealth { get; private set; } = SwarmBalance.MaxHealth;
    internal int Level { get; private set; } = 1;
    internal int Experience { get; private set; }
    internal int ExperienceGoal => SwarmBalance.ExperienceNeeded(Level);
    internal double GraceRemaining { get; private set; } = 2.5;
    internal double InvulnerableRemaining { get; private set; }
    internal double PowerRemaining { get; private set; }
    internal double ShieldRemaining { get; private set; }
    internal double RapidRemaining { get; private set; }
    internal SwarmWeaponKind Weapon { get; private set; } = SwarmWeaponKind.PixelBlaster;
    internal int Ammo { get; private set; } = int.MaxValue;
    internal double Speed => SwarmBalance.EnemySpeed(Stage);
    internal double SpawnInterval => SwarmBalance.SpawnInterval(Stage);
    internal double AimAngle { get; private set; }
    internal bool NewBest { get; set; }
    internal Action<NexaSoundEffect>? Sound { get; set; }
    private double spawnClock, reliefClock, weaponClock, multiplyClock, shotCooldown, moveBonus;
    private int damageBonus, extraShots, pierceBonus, healthUpgrade, lastBossStage;
    private Vector aim = new(0, -1);
    internal PixelSwarmGame(int? seed = null, IReadOnlyList<TimerPixelSeed>? timerPixels = null)
    { random = seed.HasValue ? new Random(seed.Value) : new Random(); seeds = timerPixels?.ToArray() ?? []; }

    internal void Resize(Size size)
    {
        if (!double.IsFinite(size.Width) || !double.IsFinite(size.Height) || size.Width < 20 || size.Height < 20 || size == Bounds) return;
        var old = Bounds; Bounds = size;
        if (old.Width == 0) { Player = new Point(size.Width / 2, size.Height / 2); SeedTimerPixels(); return; }
        Point Scale(Point p) => new(p.X * size.Width / old.Width, p.Y * size.Height / old.Height);
        Player = Clamp(Scale(Player));
        foreach (var pixel in Pixels) { pixel.Position = KeepPixelInside(Scale(pixel.Position), pixel); pixel.Previous = pixel.Position; }
        foreach (var shot in Shots) { shot.Position = Scale(shot.Position); shot.Previous = shot.Position; }
        foreach (var pickup in Pickups) pickup.Position = Scale(pickup.Position);
        GraceRemaining = Math.Max(GraceRemaining, .6);
    }
    private void SeedTimerPixels()
    {
        var count = Math.Min(SwarmBalance.EnemyCap(1), seeds.Length);
        for (var i = 0; i < count; i++)
        {
            var seed = seeds[(int)((long)i * seeds.Length / count)];
            Pixels.Add(new Pixel { Position = new(seed.RelativePosition.X * Bounds.Width, seed.RelativePosition.Y * Bounds.Height),
                Radius = seed.Radius, Width = seed.PixelSize.Width, Height = seed.PixelSize.Height,
                Phase = random.NextDouble() * 6, Kind = SwarmEnemyKind.Drifter, FromTimer = true });
        }
        if (Pixels.Count == 0) for (var i = 0; i < 18; i++) Spawn(SwarmEnemyKind.Drifter);
        foreach (var pixel in Pixels) { pixel.Previous = pixel.Position; pixel.Direction = RandomDirection(); }
    }
    internal void Start()
    {
        Clear(); Survived = 0; Destroyed = Collected = Experience = 0; Health = MaxHealth = SwarmBalance.MaxHealth; Level = 1;
        GraceRemaining = 2.5; InvulnerableRemaining = PowerRemaining = ShieldRemaining = RapidRemaining = 0;
        Weapon = SwarmWeaponKind.PixelBlaster; Ammo = int.MaxValue; spawnClock = reliefClock = weaponClock = multiplyClock = shotCooldown = 0;
        moveBonus = 0; damageBonus = extraShots = pierceBonus = healthUpgrade = lastBossStage = 0; NewBest = false;
        Player = new Point(Bounds.Width / 2, Bounds.Height / 2); SeedTimerPixels(); Phase = SwarmPhase.Playing;
    }
    internal void TogglePause() { if (Phase == SwarmPhase.Playing) Phase = SwarmPhase.Paused; else if (Phase == SwarmPhase.Paused) Phase = SwarmPhase.Playing; }
    internal void Fire() => Fire(Player + aim);
    internal void Fire(Point pointer)
    {
        if (Phase != SwarmPhase.Playing || Bounds.Width < 20 || shotCooldown > 0 || Shots.Count >= SwarmBalance.ProjectileCap) return;
        Aim(pointer); var weapon = SwarmBalance.Definition(Weapon); var count = 1 + extraShots;
        for (var i = 0; i < count && Shots.Count < SwarmBalance.ProjectileCap; i++)
        {
            var angle = AimAngle + (i - (count - 1) / 2d) * .15; var vector = new Vector(Math.Cos(angle), Math.Sin(angle));
            Shots.Add(new Shot { Position = Player, Previous = Player, Velocity = vector * weapon.Speed,
                Damage = weapon.Damage + damageBonus, Radius = weapon.Radius, Explosion = weapon.Explosion, Pierces = 1 + pierceBonus });
        }
        shotCooldown = Math.Max(.045, weapon.Cooldown * (RapidRemaining > 0 ? .58 : 1));
        Sound?.Invoke(NexaSoundEffect.Shoot);
        if (Weapon != SwarmWeaponKind.PixelBlaster && --Ammo <= 0) { Weapon = SwarmWeaponKind.PixelBlaster; Ammo = int.MaxValue; }
    }
    private void Aim(Point pointer)
    {
        if (!double.IsFinite(pointer.X) || !double.IsFinite(pointer.Y)) return;
        var direction = pointer - Player; if (direction.Length <= .01) return;
        direction.Normalize(); aim = direction; AimAngle = Math.Atan2(aim.Y, aim.X);
    }
    internal void Advance(double seconds, Point target, bool firing = false) => Advance(seconds, new Vector(), target, firing);
    internal void Advance(double seconds, Vector movement, Point pointer, bool firing = false)
    {
        if (Phase != SwarmPhase.Playing || Bounds.Width < 20 || Bounds.Height < 20 || !double.IsFinite(seconds) || seconds <= 0) return;
        if (!double.IsFinite(movement.X) || !double.IsFinite(movement.Y)) movement = new();
        if (movement.LengthSquared > 1) movement.Normalize(); seconds = Math.Min(seconds, .1);
        while (seconds > 0 && Phase == SwarmPhase.Playing)
        {
            var dt = Math.Min(seconds, 1d / 120); seconds -= dt; var previousPlayer = Player;
            Player = Clamp(Player + movement * (SwarmBalance.PlayerSpeed + moveBonus) * dt); Aim(pointer);
            Survived += dt; GraceRemaining = Math.Max(0, GraceRemaining - dt); InvulnerableRemaining = Math.Max(0, InvulnerableRemaining - dt);
            PowerRemaining = Math.Max(0, PowerRemaining - dt); ShieldRemaining = Math.Max(0, ShieldRemaining - dt);
            RapidRemaining = Math.Max(0, RapidRemaining - dt); shotCooldown = Math.Max(0, shotCooldown - dt);
            if (firing && Weapon == SwarmWeaponKind.MachineGun) Fire(pointer);
            spawnClock += dt; reliefClock += dt; weaponClock += dt; multiplyClock += dt;
            if (spawnClock >= SpawnInterval) { spawnClock = 0; Spawn(ChooseEnemy()); }
            if (Stage >= 2 && reliefClock >= 9) { reliefClock = 0; AddPickup(SwarmPickupKind.Relief); }
            if (Stage >= 3 && weaponClock >= 13) { weaponClock = 0; AddPickup(random.Next(2) == 0 ? SwarmPickupKind.MachineGun : SwarmPickupKind.GrenadeLauncher); }
            if (Stage >= 3 && multiplyClock >= 5)
            {
                multiplyClock = 0;
                if (Pixels.Count < SwarmBalance.EnemyCap(Stage) && Pixels.FirstOrDefault(p => p.Kind == SwarmEnemyKind.Drifter) is { } parent)
                    Pixels.Add(new Pixel { Position = KeepPixelInside(parent.Position + RandomDirection() * 18, parent),
                        Previous = parent.Position, Direction = RandomDirection(), Kind = SwarmEnemyKind.Drifter, Radius = 6 });
            }
            if (Stage >= 5 && Stage % 5 == 0 && lastBossStage != Stage)
            { lastBossStage = Stage; if (Pixels.Count >= SwarmBalance.EnemyCap(Stage)) Pixels.RemoveAt(0); Spawn(SwarmEnemyKind.Boss); }
            MoveEnemies(dt); MoveShots(dt);
            if (Phase != SwarmPhase.Playing) break;
            MovePickups(dt, previousPlayer); MoveParticles(dt);
            if (GraceRemaining <= 0 && InvulnerableRemaining <= 0 && ShieldRemaining <= 0 &&
                Pixels.Any(p => Touches(previousPlayer - p.Previous, Player - p.Position, p.HalfWidth + PlayerRadius, p.HalfHeight + PlayerRadius))) DamagePlayer();
        }
    }
    private void MoveEnemies(double dt)
    {
        foreach (var pixel in Pixels)
        {
            pixel.Previous = pixel.Position; pixel.Age += dt; pixel.AttackClock += dt;
            Vector desired = pixel.Direction;
            if (Stage == 1 || pixel.Kind == SwarmEnemyKind.Drifter)
            { if (pixel.Age > 1.2 + pixel.Phase % .7) { pixel.Age = 0; desired = RandomDirection(); } }
            else desired = PowerRemaining > 0 ? pixel.Position - Player : Player - pixel.Position;
            if (desired.Length > .01) desired.Normalize();
            if (pixel.Kind == SwarmEnemyKind.Dasher && pixel.AttackClock > 2.2) { pixel.DashClock = .28; pixel.AttackClock = 0; }
            if (pixel.Kind == SwarmEnemyKind.Shooter && pixel.AttackClock > 2.4 && Shots.Count < SwarmBalance.ProjectileCap)
            { pixel.AttackClock = 0; Shots.Add(new Shot { Position = pixel.Position, Previous = pixel.Position, Velocity = desired * 230, Hostile = true, Radius = 4 }); }
            pixel.Direction += (desired - pixel.Direction) * Math.Min(1, dt * (Stage == 1 ? 1.2 : 3));
            var speed = Speed * (pixel.Kind == SwarmEnemyKind.Tank ? .55 : pixel.Kind == SwarmEnemyKind.Boss ? .45 : 1) *
                (pixel.DashClock > 0 ? 3 : 1) * (PowerRemaining > 0 ? .18 : 1);
            pixel.DashClock = Math.Max(0, pixel.DashClock - dt);
            pixel.Position = KeepPixelInside(pixel.Position + pixel.Direction * speed * dt, pixel);
        }
    }
    private void MoveShots(double dt)
    {
        for (var i = Shots.Count - 1; i >= 0; i--)
        {
            var shot = Shots[i]; shot.Previous = shot.Position; shot.Position += shot.Velocity * dt; shot.Age += dt;
            if (shot.Hostile)
            {
                if (InvulnerableRemaining <= 0 && ShieldRemaining <= 0 && Touches(shot.Previous - Player, shot.Position - Player, PlayerRadius + shot.Radius, PlayerRadius + shot.Radius))
                { DamagePlayer(); if (GameOver) return; Shots.RemoveAt(i); continue; }
            }
            else for (var j = Pixels.Count - 1; j >= 0 && shot.Pierces > 0; j--)
            {
                var pixel = Pixels[j];
                if (!Touches(shot.Previous - pixel.Previous, shot.Position - pixel.Position, pixel.HalfWidth + shot.Radius, pixel.HalfHeight + shot.Radius)) continue;
                if (shot.Explosion > 0) Explode(shot.Position, shot.Explosion, shot.Damage); else Hit(pixel, shot.Damage);
                shot.Pierces--; if (shot.Explosion > 0) break;
            }
            if (shot.Pierces <= 0 || shot.Age > 2.3 || shot.Position.X < 0 || shot.Position.X > Bounds.Width || shot.Position.Y < 0 || shot.Position.Y > Bounds.Height)
                Shots.RemoveAt(i);
            if (Phase != SwarmPhase.Playing) return;
        }
    }
    private void MovePickups(double dt, Point previousPlayer)
    {
        for (var i = Pickups.Count - 1; i >= 0; i--)
        {
            var pickup = Pickups[i]; pickup.Age += dt;
            if (pickup.Kind != SwarmPickupKind.Relief && (pickup.Position - Player).Length < 85) pickup.Velocity += (Player - pickup.Position) * dt * 2;
            pickup.Position += pickup.Velocity * dt;
            if (pickup.Position.X < 10 || pickup.Position.X > Bounds.Width - 10) pickup.Velocity.X *= -1;
            if (pickup.Position.Y < 10 || pickup.Position.Y > Bounds.Height - 10) pickup.Velocity.Y *= -1;
            pickup.Position = new Point(Math.Clamp(pickup.Position.X, 10, Bounds.Width - 10), Math.Clamp(pickup.Position.Y, 10, Bounds.Height - 10));
            if (Touches(previousPlayer - pickup.Position, Player - pickup.Position, 13, 13))
            {
                Collected++;
                Sound?.Invoke(NexaSoundEffect.PowerUp);
                switch (pickup.Kind)
                {
                    case SwarmPickupKind.Relief: PowerRemaining = SwarmBalance.ReliefSeconds; break;
                    case SwarmPickupKind.Health: Health = Math.Min(MaxHealth, Health + 1); break;
                    case SwarmPickupKind.Shield: ShieldRemaining = 6; break;
                    case SwarmPickupKind.RapidFire: RapidRemaining = 7; break;
                    case SwarmPickupKind.MachineGun: Weapon = SwarmWeaponKind.MachineGun; Ammo = SwarmBalance.Definition(Weapon).Ammo; break;
                    case SwarmPickupKind.GrenadeLauncher: Weapon = SwarmWeaponKind.GrenadeLauncher; Ammo = SwarmBalance.Definition(Weapon).Ammo; break;
                }
                Pickups.RemoveAt(i);
            }
            else if (pickup.Age > SwarmBalance.PickupLifetime) Pickups.RemoveAt(i);
        }
    }
    private void MoveParticles(double dt)
    {
        for (var i = Particles.Count - 1; i >= 0; i--)
        { var particle = Particles[i]; particle.Age += dt; particle.Position += particle.Velocity * dt; if (particle.Age >= particle.Lifetime) Particles.RemoveAt(i); }
    }
    private void Hit(Pixel pixel, int damage)
    {
        pixel.Health -= damage; if (pixel.Health > 0) return;
        Pixels.Remove(pixel); Destroyed++; Experience += pixel.Kind == SwarmEnemyKind.Boss ? 100 : pixel.Elite ? 35 : 22;
        Burst(pixel.Position, pixel.Kind == SwarmEnemyKind.Boss ? Colors.Gold : Colors.IndianRed);
        if (random.NextDouble() < (pixel.Elite ? .35 : .055)) AddPickup(random.Next(3) switch
        { 0 => SwarmPickupKind.Health, 1 => SwarmPickupKind.Shield, _ => SwarmPickupKind.RapidFire }, pixel.Position);
        if (Experience >= ExperienceGoal && Phase == SwarmPhase.Playing) OfferUpgrades();
    }
    private void Explode(Point point, double radius, int damage)
    { foreach (var pixel in Pixels.ToArray()) if ((pixel.Position - point).Length <= radius + pixel.HalfWidth) Hit(pixel, damage); Burst(point, Colors.Orange); }
    private void DamagePlayer()
    {
        if (Phase != SwarmPhase.Playing || GraceRemaining > 0 || InvulnerableRemaining > 0 || ShieldRemaining > 0) return;
        Health--; InvulnerableRemaining = SwarmBalance.InvulnerabilitySeconds; Burst(Player, Colors.White);
        if (Health <= 0) { Phase = SwarmPhase.GameOver; Shots.Clear(); Sound?.Invoke(NexaSoundEffect.Death); }
    }
    private void OfferUpgrades()
    {
        Experience -= ExperienceGoal; Level++; Phase = SwarmPhase.LevelUp; UpgradeChoices.Clear();
        var choices = new List<Upgrade>
        {
            new("Sharper pixels", "+1 projectile damage", "damage"), new("Quick feet", "+30 movement speed", "speed"),
            new("Extra pixel", "+1 projectile per shot", "multi"), new("Piercing shot", "+1 enemy per shot", "pierce"),
            new("Deep breath", "Restore 1 HP", "heal"), new("More heart", "+1 maximum HP", "health"),
            new("Pixel shield", "Block hits for 6 seconds", "shield"), new("Steady hands", "Rapid fire for 7 seconds", "rapid")
        };
        choices.RemoveAll(choice => choice.Key switch
        { "damage" => damageBonus >= 5, "speed" => moveBonus >= 120, "multi" => extraShots >= 3,
          "pierce" => pierceBonus >= 3, "heal" => Health >= MaxHealth, "health" => healthUpgrade >= 3, _ => false });
        foreach (var choice in choices.OrderBy(_ => random.Next()).Take(3)) UpgradeChoices.Add(choice);
    }
    internal void ChooseUpgrade(int index)
    {
        if (Phase != SwarmPhase.LevelUp || index < 0 || index >= UpgradeChoices.Count) return;
        switch (UpgradeChoices[index].Key)
        {
            case "damage": damageBonus++; break; case "speed": moveBonus += 30; break;
            case "multi": extraShots++; break; case "pierce": pierceBonus++; break;
            case "heal": Health = Math.Min(MaxHealth, Health + 1); break;
            case "health": healthUpgrade++; MaxHealth++; Health++; break;
            case "shield": ShieldRemaining = Math.Min(12, ShieldRemaining + 6); break;
            case "rapid": RapidRemaining = Math.Min(14, RapidRemaining + 7); break;
        }
        UpgradeChoices.Clear(); Phase = SwarmPhase.Playing;
    }
    private SwarmEnemyKind ChooseEnemy()
    {
        var n = random.Next(100);
        return Stage == 1 ? SwarmEnemyKind.Drifter : Stage == 2 ? n < 70 ? SwarmEnemyKind.Chaser : SwarmEnemyKind.Drifter :
            n < 35 ? SwarmEnemyKind.Chaser : n < 60 ? SwarmEnemyKind.Dasher : n < 80 ? SwarmEnemyKind.Shooter : SwarmEnemyKind.Tank;
    }
    private void Spawn(SwarmEnemyKind kind)
    {
        if (Pixels.Count >= SwarmBalance.EnemyCap(Stage)) return;
        Point point = new();
        for (var attempt = 0; attempt < 12; attempt++)
        {
            point = random.Next(4) switch
            { 0 => new Point(10, random.NextDouble() * Bounds.Height), 1 => new Point(Bounds.Width - 10, random.NextDouble() * Bounds.Height),
              2 => new Point(random.NextDouble() * Bounds.Width, 10), _ => new Point(random.NextDouble() * Bounds.Width, Bounds.Height - 10) };
            if ((point - Player).Length >= SwarmBalance.SpawnSafeDistance) break;
        }
        var elite = kind != SwarmEnemyKind.Boss && Stage >= 3 && random.NextDouble() < .08;
        var radius = kind == SwarmEnemyKind.Boss ? 22 : kind == SwarmEnemyKind.Tank ? 12 : 7;
        Pixels.Add(new Pixel { Position = point, Previous = point, Direction = RandomDirection(), Kind = kind, Radius = radius,
            Health = kind == SwarmEnemyKind.Boss ? 24 : kind == SwarmEnemyKind.Tank ? 4 : elite ? 3 : 1,
            Elite = elite, Phase = random.NextDouble() * 6 });
    }
    private void AddPickup(SwarmPickupKind kind, Point? at = null)
    {
        if (Pickups.Count >= 12) return;
        var point = at ?? new Point(30 + random.NextDouble() * Math.Max(1, Bounds.Width - 60), 30 + random.NextDouble() * Math.Max(1, Bounds.Height - 60));
        Pickups.Add(new Pickup { Position = point, Velocity = kind == SwarmPickupKind.Relief ? RandomDirection() * 45 : new Vector(), Kind = kind });
    }
    private void Burst(Point point, Color color)
    { for (var i = 0; i < 6 && Particles.Count < SwarmBalance.ParticleCap; i++) Particles.Add(new Particle { Position = point, Velocity = RandomDirection() * (40 + random.NextDouble() * 100), Color = color }); }
    private Vector RandomDirection() { var angle = random.NextDouble() * Math.PI * 2; return new Vector(Math.Cos(angle), Math.Sin(angle)); }
    private Point Clamp(Point p) => new(Math.Clamp(p.X, PlayerRadius, Bounds.Width - PlayerRadius), Math.Clamp(p.Y, PlayerRadius, Bounds.Height - PlayerRadius));
    private Point KeepPixelInside(Point p, Pixel pixel)
    { var x = Math.Min(pixel.HalfWidth, Bounds.Width / 2); var y = Math.Min(pixel.HalfHeight, Bounds.Height / 2); return new(Math.Clamp(p.X, x, Bounds.Width - x), Math.Clamp(p.Y, y, Bounds.Height - y)); }
    private static bool Touches(Vector from, Vector to, double halfWidth, double halfHeight)
    {
        var delta = to - from; var low = 0d; var high = 1d;
        bool Axis(double start, double movement, double extent)
        { if (Math.Abs(movement) < .000001) return Math.Abs(start) <= extent;
          var a = (-extent - start) / movement; var b = (extent - start) / movement;
          low = Math.Max(low, Math.Min(a, b)); high = Math.Min(high, Math.Max(a, b)); return low <= high; }
        return Axis(from.X, delta.X, halfWidth) && Axis(from.Y, delta.Y, halfHeight);
    }
    internal void Clear() { Pixels.Clear(); Shots.Clear(); Pickups.Clear(); Particles.Clear(); PowerDots.Clear(); UpgradeChoices.Clear(); }
}

internal sealed class SwarmArena : FrameworkElement
{
    internal PixelSwarmGame Game { get; }
    private readonly Brush background, foreground, player, playerFlash, hostile, neutral, tank, elite, white, health, grenade;
    internal SwarmArena(PixelSwarmGame game, Color accent, Color? dashboardBackground = null, Color? dashboardText = null, Color? timerPixelColor = null)
    {
        Game = game;
        static Brush Solid(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
        background = Solid(dashboardBackground ?? Color.FromRgb(8, 11, 22)); foreground = Solid(dashboardText ?? Colors.White);
        player = Solid(accent); playerFlash = Solid(Colors.White); hostile = Solid(Color.FromRgb(245, 65, 83));
        neutral = Solid(timerPixelColor ?? Color.FromRgb(131, 177, 221)); tank = Solid(Color.FromRgb(219, 113, 70)); elite = Solid(Colors.Gold);
        white = Solid(Colors.White); health = Solid(Color.FromRgb(88, 220, 147)); grenade = Solid(Color.FromRgb(255, 176, 60));
        ClipToBounds = true; SizeChanged += (_, _) => { Game.Resize(RenderSize); InvalidateVisual(); };
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); dc.DrawRectangle(background, null, new Rect(RenderSize));
        foreach (var pixel in Game.Pixels)
        {
            var brush = pixel.Elite || pixel.Kind == SwarmEnemyKind.Boss ? elite : pixel.Kind == SwarmEnemyKind.Tank ? tank : Game.Stage == 1 ? neutral : hostile;
            dc.DrawRectangle(brush, null, new Rect(Math.Round(pixel.Position.X - pixel.HalfWidth), Math.Round(pixel.Position.Y - pixel.HalfHeight), Math.Max(2, pixel.HalfWidth * 2), Math.Max(2, pixel.HalfHeight * 2)));
        }
        foreach (var pickup in Game.Pickups)
        {
            var brush = pickup.Kind == SwarmPickupKind.Relief ? white : pickup.Kind == SwarmPickupKind.Health ? health : pickup.Kind == SwarmPickupKind.GrenadeLauncher ? grenade : player;
            dc.DrawRectangle(brush, null, new Rect(Math.Round(pickup.Position.X - 6), Math.Round(pickup.Position.Y - 6), 12, 12));
        }
        foreach (var shot in Game.Shots)
            dc.DrawRectangle(shot.Hostile ? hostile : shot.Explosion > 0 ? grenade : foreground, null,
                new Rect(Math.Round(shot.Position.X - shot.Radius), Math.Round(shot.Position.Y - shot.Radius), shot.Radius * 2, shot.Radius * 2));
        foreach (var particle in Game.Particles)
        { var brush = new SolidColorBrush(particle.Color) { Opacity = Math.Max(0, 1 - particle.Age / particle.Lifetime) };
          dc.DrawRectangle(brush, null, new Rect(Math.Round(particle.Position.X), Math.Round(particle.Position.Y), 3, 3)); }
        var current = Game.InvulnerableRemaining > 0 && (int)(Game.InvulnerableRemaining * 10) % 2 == 0 ? playerFlash : player;
        dc.DrawRectangle(current, null, new Rect(Math.Round(Game.Player.X - 6), Math.Round(Game.Player.Y - 6), 12, 12));
        var aim = new Vector(Math.Cos(Game.AimAngle), Math.Sin(Game.AimAngle));
        dc.DrawLine(new Pen(foreground, 2), Game.Player + aim * 9, Game.Player + aim * 22);
        if (Game.ShieldRemaining > 0 || Game.InvulnerableRemaining > 0) dc.DrawEllipse(null, new Pen(white, 1), Game.Player, 15, 15);
    }
}
