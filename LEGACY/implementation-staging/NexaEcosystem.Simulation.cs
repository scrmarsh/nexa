using System.Windows;

namespace SessionDashboard;

internal sealed partial class BugManager
{
    internal const double WorldWidth = 1600, WorldHeight = 1000;
    internal static readonly BugSpecies[] AllSpecies = [BugSpecies.Ants, BugSpecies.Roaches, BugSpecies.Flies, BugSpecies.Spiders, BugSpecies.Ladybugs, BugSpecies.Centipedes];
    internal bool EcosystemActive { get; private set; }
    internal EcosystemOptions EcosystemSettings => (Preferences.Ecosystem ?? new()).Safe();
    internal readonly List<EcosystemNest> Nests = [];
    internal readonly List<EcosystemResource> Resources = [];
    internal readonly List<EcosystemWeb> Webs = [];
    internal Guid WorldId { get; private set; }
    internal int WorldSeed { get; private set; }
    internal double SimulationAge { get; private set; }
    internal long CombatDeaths { get; private set; }
    internal long NaturalEncounters { get; private set; }
    internal bool WorldPaused { get; set; }
    internal bool SeasonalCostumes { get; set; }
    internal bool SeasonalCandy { get; set; }
    internal int EcosystemCap => Preferences.Density switch { BugDensity.Low => 12, BugDensity.Infestation => 96, _ => 36 };
    internal double WorldSpawnDelay;
    internal void NewEcosystem(string user, EcosystemOptions options, int? seed = null)
    {
        var doc = new EcosystemDocument { UserId = user, Seed = seed ?? Random.Shared.Next(), Options = options.Safe() };
        var seeded = new Random(doc.Seed);
        for (var i = 0; i < AllSpecies.Length; i++)
        {
            var angle = i * Math.PI / 3;
            doc.Nests.Add(new() { Species = AllSpecies[i], X = WorldWidth / 2 + Math.Cos(angle) * WorldWidth * .36, Y = WorldHeight / 2 + Math.Sin(angle) * WorldHeight * .35 });
        }
        for (var i = 0; i < 30; i++) doc.Resources.Add(new() { Kind = (BugResourceKind)(i % 5), X = 90 + seeded.NextDouble() * (WorldWidth - 180), Y = 90 + seeded.NextDouble() * (WorldHeight - 180), Amount = 8 + seeded.Next(12) });
        RestoreEcosystem(doc);
    }
    internal void RestoreEcosystem(EcosystemDocument doc)
    {
        EcosystemActive = true; WorldId = doc.WorldId; WorldSeed = doc.Seed; SimulationAge = doc.SimulationAge; WorldPaused = doc.Paused; CombatDeaths = doc.CombatDeaths; NaturalEncounters = doc.NaturalEncounters;
        Preferences = Preferences with { Ecosystem = doc.Options.Safe() };
        Bounds = new Size(WorldWidth, WorldHeight); VisibleBounds = new Rect(Bounds);
        Bugs.Clear(); Splats.Clear(); Nests.Clear(); Resources.Clear(); Webs.Clear();
        Nests.AddRange(Continuity.Clone(doc.Nests)); Resources.AddRange(Continuity.Clone(doc.Resources)); Webs.AddRange(Continuity.Clone(doc.Webs));
        foreach (var b in doc.Bugs) Bugs.Add(new()
        {
            InteractionId = b.Id, Species = b.Species, Position = new(b.X, b.Y), Target = new(b.TargetX, b.TargetY), Angle = b.Angle, Speed = b.Speed, Age = b.Age,
            Health = b.Health, Energy = b.Energy, Hits = b.Hits, Kills = b.Kills, State = b.State, Activity = b.Activity, Personality = b.Personality,
            ResourceId = b.ResourceId, EnemyId = b.EnemyId, Carrying = b.Carrying, Decision = b.Decision, CrawlPhase = b.Phase, Scale = EcosystemSettings.Size
        });
    }
    internal EcosystemDocument CaptureEcosystem(string user) => new()
    {
        UserId = user, WorldId = WorldId, Seed = WorldSeed, SimulationAge = SimulationAge, Paused = WorldPaused, CombatDeaths = CombatDeaths, NaturalEncounters = NaturalEncounters,
        Options = EcosystemSettings, UpdatedAt = DateTimeOffset.UtcNow, Nests = Continuity.Clone(Nests), Resources = Continuity.Clone(Resources), Webs = Continuity.Clone(Webs),
        Bugs = Bugs.Select(b => new SavedEcosystemBug
        {
            Id = b.InteractionId, Species = b.Species, X = b.Position.X, Y = b.Position.Y, TargetX = b.Target.X, TargetY = b.Target.Y,
            Angle = b.Angle, Speed = b.Speed, Age = b.Age, Health = b.Health, Energy = b.Energy, Hits = b.Hits, Kills = b.Kills, State = b.State,
            Activity = b.Activity, Personality = b.Personality, ResourceId = b.ResourceId, EnemyId = b.EnemyId, Carrying = b.Carrying, Decision = b.Decision, Phase = b.CrawlPhase
        }).ToList()
    };
    private EcosystemNest Nest(BugSpecies species) => Nests.First(n => n.Species == species);
    private bool Edible(BugEntity bug, EcosystemResource resource) => resource.Amount > .1 && resource.Kind switch
    {
        BugResourceKind.Candy => SeasonalCandy && bug.Species is BugSpecies.Ants or BugSpecies.Roaches,
        BugResourceKind.Fruit => bug.Species is BugSpecies.Ants or BugSpecies.Roaches or BugSpecies.Flies,
        BugResourceKind.Leaves => bug.Species == BugSpecies.Ladybugs,
        BugResourceKind.Water => bug.Species is BugSpecies.Flies or BugSpecies.Ladybugs,
        BugResourceKind.Protein => bug.Species is BugSpecies.Spiders or BugSpecies.Centipedes,
        _ => false
    };
    internal void AdvanceEcosystem(double dt)
    {
        if (WorldPaused) return;
        dt = Math.Clamp(dt, 0, .1); SimulationAge += dt;
        foreach (var r in Resources)
        {
            if (r.Amount < 12) { r.Regrowth += dt; if (r.Regrowth >= 90) { r.Amount = Math.Min(12, r.Amount + 1); r.Regrowth = 0; } }
        }
        for (var i = Webs.Count - 1; i >= 0; i--) { Webs[i].Age += dt; Webs[i].Construction = Math.Min(1, Webs[i].Construction + dt * .025); if (Webs[i].Age > 900) Webs.RemoveAt(i); }
        WorldSpawnDelay -= dt;
        if (Bugs.Count < EcosystemCap && WorldSpawnDelay <= 0)
        {
            var bug = Spawn(); bug.Scale = EcosystemSettings.Size; bug.Personality = (BugPersonality)random.Next(4); bug.Decision = 0;
            var nest = Nest(bug.Species); bug.Position = nest.Position + new Vector(Range(-35, 35), Range(-35, 35)); bug.Surface = null;
            if (Bugs.Count <= 6 && Preferences.Species == BugSpecies.AllBugs) bug.Species = AllSpecies[(Bugs.Count - 1) % 6];
            WorldSpawnDelay = Bugs.Count < 12 ? .15 : nest.Food >= 1 ? 2 : 8;
            nest.Food = Math.Max(0, nest.Food - .2);
        }
        for (var i = Splats.Count - 1; i >= 0; i--) { Splats[i].Age += dt; if (Splats[i].Age > 4) Splats.RemoveAt(i); }
        for (var i = Bugs.Count - 1; i >= 0; i--)
        {
            var bug = Bugs[i];
            if (bug.State == BugState.Squashed || bug.Health <= 0)
            {
                bug.SquashAge += dt;
                if (bug.SquashAge > .4) { if (Splats.Count >= 60) Splats.RemoveAt(0); Splats.Add(new() { Position = bug.Position, Species = bug.Species }); Bugs.RemoveAt(i); }
                continue;
            }
            var step = dt;
            if (!VisibleBounds.Contains(bug.Position)) { bug.OffscreenTime += dt; if (bug.OffscreenTime < .3) continue; step = bug.OffscreenTime; bug.OffscreenTime = 0; }
            bug.Age += step; bug.HitFlash = Math.Max(0, bug.HitFlash - step); bug.Scale = EcosystemSettings.Size;
            bug.Energy = Math.Clamp(bug.Energy - step * .0008, 0, 1); bug.Decision -= step;
            if (bug.Decision <= 0) ChooseActivity(bug);
            var nest = Nest(bug.Species);
            if (bug.Activity is BugActivity.Sleeping or BugActivity.Grooming or BugActivity.Socializing)
            {
                bug.State = BugState.Paused; bug.Energy = Math.Min(1, bug.Energy + step * .015); bug.Health = Math.Min(100, bug.Health + step * .3); bug.CrawlPhase += step * .15; continue;
            }
            if (bug.Activity == BugActivity.Hiding) { bug.State = BugState.Hiding; continue; }
            bug.State = bug.Species == BugSpecies.Flies ? BugState.Flying : BugState.Crossing;
            if (bug.Activity == BugActivity.Carrying) bug.Target = nest.Position;
            if (bug.Activity == BugActivity.Gathering && Resources.FirstOrDefault(r => r.Id == bug.ResourceId) is { } resource)
            {
                bug.Target = resource.Position;
                if ((bug.Position - resource.Position).Length < 18)
                {
                    // Ants recruit a nearby partner for large candy loads. Other species take small, suitable resources.
                    var amount = bug.Species == BugSpecies.Ants && resource.Kind == BugResourceKind.Candy && Bugs.Any(b => b != bug && b.Species == BugSpecies.Ants && (b.Position - bug.Position).Length < 100) ? 2 : .5;
                    bug.Carrying = Math.Min(amount, resource.Amount); resource.Amount -= bug.Carrying; bug.Activity = BugActivity.Carrying; bug.Target = nest.Position; bug.Decision = 30;
                }
            }
            if (bug.Activity == BugActivity.Carrying && (bug.Position - nest.Position).Length < 24)
            {
                nest.Food = Math.Min(10000, nest.Food + bug.Carrying); nest.Deliveries++; nest.Construction = Math.Min(1, nest.Construction + .005);
                bug.Carrying = 0; bug.Activity = BugActivity.Nesting; bug.Decision = 4; bug.Energy = Math.Min(1, bug.Energy + .1);
            }
            if (bug.Activity == BugActivity.Nesting) { nest.Construction = Math.Min(1, nest.Construction + step * .001); bug.Target = nest.Position; }
            if (bug.Activity is BugActivity.Hunting or BugActivity.Defending or BugActivity.Raiding) Encounter(bug, step);
            var speed = bug.Speed * .55 * (bug.Activity == BugActivity.Carrying ? .65 : 1);
            if (EcosystemSettings.Behavior == BugBehavior.Creepy && bug.Species == BugSpecies.Roaches && bug.Personality != BugPersonality.Social) speed *= 1.8;
            var delta = bug.Target - bug.Position;
            if (delta.Length > 2)
            {
                var movement = Math.Min(delta.Length, speed * step); delta.Normalize(); bug.Position += delta * movement;
                bug.Angle = Math.Atan2(delta.Y, delta.X) * 180 / Math.PI; bug.CrawlPhase += movement * .35;
            }
            else if (bug.Activity == BugActivity.Exploring) bug.Decision = 0;
            bug.Position = new(Math.Clamp(bug.Position.X, 12, WorldWidth - 12), Math.Clamp(bug.Position.Y, 12, WorldHeight - 12));
        }
    }
    private void ChooseActivity(BugEntity bug)
    {
        bug.State = BugState.Crossing; bug.ResourceId = null; bug.EnemyId = null; bug.Decision = Range(5, 16);
        if (bug.Carrying > 0) { bug.Activity = BugActivity.Carrying; bug.Target = Nest(bug.Species).Position; return; }
        var mode = EcosystemSettings.Behavior; var roll = random.NextDouble();
        if (mode == BugBehavior.Combative)
        {
            var enemy = Bugs.Where(b => b != bug && b.Species != bug.Species && b.Health > 0 && b.State != BugState.Squashed).MinBy(b => (b.Position - bug.Position).LengthSquared);
            if (enemy != null && ((enemy.Position - Nest(bug.Species).Position).Length < 180 || roll < .6))
            { bug.Activity = (enemy.Position - Nest(bug.Species).Position).Length < 180 ? BugActivity.Defending : BugActivity.Raiding; bug.EnemyId = enemy.InteractionId; bug.Target = enemy.Position; bug.Decision = Range(2, 5); return; }
        }
        if (mode == BugBehavior.Creepy && bug.Species is BugSpecies.Spiders or BugSpecies.Centipedes && roll < .13)
        {
            var prey = Bugs.Where(b => b.Species is BugSpecies.Flies or BugSpecies.Ants && b.State != BugState.Squashed).MinBy(b => (b.Position - bug.Position).LengthSquared);
            if (prey != null && (prey.Position - bug.Position).Length < 180) { bug.Activity = BugActivity.Hunting; bug.EnemyId = prey.InteractionId; bug.Target = prey.Position; bug.Decision = 3; return; }
        }
        if (bug.Energy < .3 || roll < (mode == BugBehavior.Cozy ? .12 : .04)) { bug.Activity = BugActivity.Sleeping; bug.Decision = Range(5, 12); return; }
        if (mode == BugBehavior.Cozy && roll < .24) { bug.Activity = BugActivity.Socializing; bug.Target = Bugs.FirstOrDefault(b => b != bug && b.Species == bug.Species)?.Position ?? Nest(bug.Species).Position; return; }
        if (roll < .31) { bug.Activity = mode == BugBehavior.Creepy ? BugActivity.Hiding : BugActivity.Grooming; bug.Decision = Range(2, 5); return; }
        if (bug.Species == BugSpecies.Spiders && Webs.Count < 24 && Webs.All(w => w.Owner != bug.InteractionId) && roll < .5)
        { Webs.Add(new() { Owner = bug.InteractionId, X = bug.Position.X, Y = bug.Position.Y }); bug.Activity = BugActivity.Nesting; bug.Decision = 8; return; }
        if (roll < .8)
        {
            var resource = Resources.Where(r => Edible(bug, r)).MinBy(r => (r.Position - bug.Position).LengthSquared);
            if (resource != null) { bug.Activity = BugActivity.Gathering; bug.ResourceId = resource.Id; bug.Target = resource.Position; return; }
        }
        bug.Activity = BugActivity.Exploring;
        bug.Target = new(Range(25, WorldWidth - 25), Range(25, WorldHeight - 25));
        if (mode == BugBehavior.Creepy && Surfaces.Count > 0 && roll > .85) bug.Target = EdgePoint(Surfaces.Values.ElementAt(random.Next(Surfaces.Count)), random.NextDouble());
    }
    private void Encounter(BugEntity bug, double dt)
    {
        var enemy = Bugs.FirstOrDefault(b => b.InteractionId == bug.EnemyId && b.Health > 0 && b.State != BugState.Squashed);
        if (enemy == null) { bug.Decision = 0; return; }
        bug.Target = enemy.Position;
        if ((enemy.Position - bug.Position).Length > 24 * EcosystemSettings.Size) return;
        var damage = (bug.Species switch { BugSpecies.Ants => 8, BugSpecies.Flies => 6, BugSpecies.Roaches => 9, BugSpecies.Spiders => 10, BugSpecies.Ladybugs => 8, _ => 10 }) * dt;
        if (EcosystemSettings.Behavior == BugBehavior.Creepy) { damage *= .25; NaturalEncounters++; }
        var defended = (enemy.Position - Nest(enemy.Species).Position).Length < 100;
        enemy.Health = Math.Max(0, enemy.Health - damage * (defended ? .75 : 1)); enemy.HitFlash = .12;
        if (enemy.Health <= 0) { CombatDeaths++; bug.Kills++; enemy.State = BugState.Squashed; enemy.SquashAge = 0; bug.Decision = 0; }
    }
}
