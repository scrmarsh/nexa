using System.Windows;

namespace SessionDashboard;

internal sealed partial class BugManager
{
    internal const double WorldWidth = 1600, WorldHeight = 1000;
    internal static readonly BugSpecies[] AllSpecies = [BugSpecies.Ants, BugSpecies.Roaches, BugSpecies.Flies, BugSpecies.Spiders, BugSpecies.Ladybugs, BugSpecies.Centipedes];
    internal bool EcosystemActive { get; private set; }
    private static readonly EcosystemOptions defaultEcosystemOptions = new();
    internal EcosystemOptions EcosystemSettings => Preferences.Ecosystem ?? defaultEcosystemOptions;
    internal readonly List<EcosystemNest> Nests = [];
    internal readonly List<EcosystemResource> Resources = [];
    internal readonly List<EcosystemWeb> Webs = [];
    internal Guid SessionGeneration {get;private set;}
    internal Guid WorldId { get; private set; }
    internal int WorldSeed { get; private set; }
    internal double SimulationAge { get; private set; }
    internal long CombatDeaths { get; private set; }
    internal long NaturalEncounters { get; private set; }
    internal bool WorldPaused { get; set; }
    internal bool SeasonalCostumes { get; set; }
    internal bool SeasonalCandy { get; set; }
    internal NexaOptions VisualOptions { get; set; } = new();
    internal int EcosystemCap => Preferences.Density switch { BugDensity.Low => 12, BugDensity.High => 96, BugDensity.Infestation => int.MaxValue, _ => 36 };
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
        EcosystemActive = true;SessionGeneration=doc.SessionGeneration; WorldId = doc.WorldId; WorldSeed = doc.Seed; SimulationAge = doc.SimulationAge; WorldPaused = doc.Paused; CombatDeaths = doc.CombatDeaths; NaturalEncounters = doc.NaturalEncounters;
        Preferences = Preferences with { Ecosystem = doc.Options.Safe() };
        Bounds = new Size(WorldWidth, WorldHeight); VisibleBounds = new Rect(Bounds);
        arrivals.Clear();WorldSpawnDelay=1;
        Bugs.Clear(); Splats.Clear(); Nests.Clear(); Resources.Clear(); Webs.Clear();
        Nests.AddRange(Continuity.Clone(doc.Nests)); Resources.AddRange(Continuity.Clone(doc.Resources)); Webs.AddRange(Continuity.Clone(doc.Webs));
        foreach (var b in doc.Bugs) Bugs.Add(new()
        {
            InteractionId = b.Id, Species = b.Species, Position = new(b.X, b.Y), Target = new(b.TargetX, b.TargetY), Angle = b.Angle, Speed = b.Speed, Age = b.Age,
            FlightHeight=b.FlightHeight, Health = b.Health, Energy = b.Energy, Hits = b.Hits, Kills = b.Kills, State = b.State, Activity = b.Activity, Personality = b.Personality,
            ResourceId = b.ResourceId, EnemyId = b.EnemyId, PartnerId = b.PartnerId, RaidTarget = b.RaidTarget, Carrying = b.Carrying, Decision = b.Decision, CrawlPhase = b.Phase, Scale = EcosystemSettings.Size
        });
    }
    internal EcosystemDocument CaptureEcosystem(string user) => new()
    {
        UserId = user, SessionGeneration=SessionGeneration, WorldId = WorldId, Seed = WorldSeed, SimulationAge = SimulationAge, Paused = WorldPaused, CombatDeaths = CombatDeaths, NaturalEncounters = NaturalEncounters,
        Options = EcosystemSettings, UpdatedAt = DateTimeOffset.UtcNow, Nests = Continuity.Clone(Nests), Resources = Continuity.Clone(Resources), Webs = Continuity.Clone(Webs),
        Bugs = Bugs.Select(b => new SavedEcosystemBug
        {
            Id = b.InteractionId, Species = b.Species, X = b.Position.X, Y = b.Position.Y, TargetX = b.Target.X, TargetY = b.Target.Y,
            Angle = b.Angle, Speed = b.Speed, Age = b.Age, FlightHeight=b.FlightHeight, Health = b.Health, Energy = b.Energy, Hits = b.Hits, Kills = b.Kills, State = b.State,
            Activity = b.Activity, Personality = b.Personality, ResourceId = b.ResourceId, EnemyId = b.EnemyId, PartnerId = b.PartnerId, RaidTarget = b.RaidTarget, Carrying = b.Carrying, Decision = b.Decision, Phase = b.CrawlPhase
        }).ToList()
    };
    internal double FrameCostAverage {get;private set;}
    private double pressureUntil;
    internal long PersistenceBytes {get;set;}
    internal bool AdaptiveSpawnAllowed => Environment.TickCount64/1000d >= pressureUntil && PersistenceBytes < 6_000_000 && FrameCostAverage < 32 && Environment.WorkingSet < 768L*1024*1024 && GC.GetTotalMemory(false) < 384L*1024*1024;
    internal void ObserveFrameCost(double milliseconds)
    {
        if(!double.IsFinite(milliseconds) || milliseconds<0)return;
        FrameCostAverage=FrameCostAverage==0?milliseconds:FrameCostAverage*.9+milliseconds*.1;
        if(milliseconds>45)pressureUntil=Environment.TickCount64/1000d+5;
    }
    internal void SeparateNearby(double dt)
    {
        const double cell=64;
        var buckets=new Dictionary<(int,int),List<BugEntity>>();
        foreach(var bug in Bugs.Where(b=>IsActive(b) && b.State is not (BugState.Squashed or BugState.Hiding or BugState.Flying)))
        {var key=((int)(bug.Position.X/cell),(int)(bug.Position.Y/cell));if(!buckets.TryGetValue(key,out var list))buckets[key]=list=[];list.Add(bug);}
        foreach(var pair in buckets)foreach(var bug in pair.Value)
        {
            var offset=new Vector();var examined=0;
            for(var x=-1;x<=1;x++)for(var y=-1;y<=1;y++)if(buckets.TryGetValue((pair.Key.Item1+x,pair.Key.Item2+y),out var neighbors))
            foreach(var other in neighbors)
            {
                if(examined++>32)break;
                if(other==bug || other.InteractionId==bug.PartnerId || other.InteractionId==bug.EnemyId)continue;
                var away=bug.Position-other.Position;var distance=away.Length;var space=14*Math.Min(3,EcosystemSettings.Size);
                if(distance>=space)continue;
                if(distance<.01)away=bug.InteractionId.CompareTo(other.InteractionId)<0?new Vector(1,0):new Vector(-1,0);else away/=distance;
                offset+=away*(space-distance)/space;
            }
            if(offset.Length>1)offset.Normalize();var next=bug.Position+offset*(18*dt);
            bug.Position=new(Math.Clamp(next.X,8,WorldWidth-8),Math.Clamp(next.Y,8,WorldHeight-8));
        }
    }
    private EcosystemNest Nest(BugSpecies species) => Nests.First(n => n.Species == species);
    private bool Edible(BugEntity bug, EcosystemResource resource) => resource.Amount > .1 && resource.Kind switch
    {
        BugResourceKind.Candy => SeasonalCandy && (!DecorationCandyActive || resource.Decoration) && bug.Species is BugSpecies.Ants or BugSpecies.Roaches,
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
            if (!r.Decoration && r.Amount < 12) { r.Regrowth += dt; if (r.Regrowth >= 90) { r.Amount = Math.Min(12, r.Amount + 1); r.Regrowth = 0; } }
        }
        for (var i = Webs.Count - 1; i >= 0; i--) { Webs[i].Age += dt; if (Webs[i].Age > 180 && !Bugs.Any(b=>b.InteractionId==Webs[i].Owner || Webs[i].Helpers.Contains(b.InteractionId))) Webs.RemoveAt(i); }
        AdvanceArrivals();
        SeparateNearby(dt);
        WorldSpawnDelay -= dt;
        if (Bugs.Count < EcosystemCap && arrivals.Count==0 && WorldSpawnDelay <= 0 && ArrivalBudgetAvailable && (Preferences.Density != BugDensity.Infestation || AdaptiveSpawnAllowed))
        {
            var bug = Spawn(); bug.Scale = EcosystemSettings.Size; bug.Personality = (BugPersonality)random.Next(4); bug.Decision = 0;
            if (Preferences.Species == BugSpecies.AllBugs) bug.Species = AllSpecies.OrderBy(species => Bugs.Count(other => !ReferenceEquals(other, bug) && other.Species == species)).First();
            bug.Speed = bug.Species switch { BugSpecies.Ants => Range(18,34), BugSpecies.Roaches => Range(75,125), BugSpecies.Ladybugs => Range(22,38), BugSpecies.Centipedes => Range(38,58), _ => Range(50,90) };
            var nest = Nest(bug.Species); bug.Position=ArrivalPoint();bug.Target=ExplorationPoint(bug.Position);bug.Surface=null;
            bug.Activity=BugActivity.Exploring;bug.ExploreUntil=SimulationAge+Range(16,28);bug.Decision=Range(3,6);
            WorldSpawnDelay=ArrivalInterval;
            nest.Food = Math.Max(0, nest.Food - .2);
        }
        for (var i = Splats.Count - 1; i >= 0; i--) { Splats[i].Age += dt; if (Splats[i].Age > 4) Splats.RemoveAt(i); }
        for (var i = Bugs.Count - 1; i >= 0; i--)
        {
            var bug = Bugs[i];
            if(!IsActive(bug))continue;
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
            if (bug.Activity is BugActivity.Sleeping or BugActivity.Grooming)
            {
                bug.State = BugState.Paused; bug.FlightHeight=Math.Max(0,bug.FlightHeight-step*3); bug.Energy = Math.Min(1, bug.Energy + step * .015); bug.Health = Math.Min(100, bug.Health + step * .3); bug.CrawlPhase += step * .15; continue;
            }
            if (bug.Activity == BugActivity.Socializing && (bug.Target-bug.Position).Length < 30) { bug.Energy=Math.Min(1,bug.Energy+step*.01); bug.State=BugState.Paused; bug.FlightHeight=Math.Max(0,bug.FlightHeight-step*3); continue; }
            if((bug.Activity==BugActivity.Hiding || bug.State==BugState.Hiding) && bug.Species!=BugSpecies.Roaches){bug.Activity=BugActivity.Exploring;bug.State=BugState.Crossing;bug.Decision=0;}
            if(bug.Activity==BugActivity.Hiding && Surfaces.Values.Any(rect=>rect.Contains(bug.Position))){bug.State=BugState.Hiding;continue;}
            if(MarchAnt(bug,step))continue;
            var flyPhase=bug.Age%11;
            if(bug.Species==BugSpecies.Flies && bug.Activity==BugActivity.Exploring && flyPhase>=9)
            {
                var landing=Surfaces.Count==0?bug.Position:Surfaces.Values.Select(rect=>new Point(Math.Clamp(bug.Position.X,rect.Left,rect.Right),Math.Clamp(bug.Position.Y,rect.Top,rect.Bottom))).MinBy(p=>(p-bug.Position).LengthSquared);
                if((landing-bug.Position).Length>10)bug.Target=landing;
                else {bug.Activity=BugActivity.Grooming;bug.Decision=1.6;bug.State=BugState.Landed;bug.FlightHeight=Math.Max(0,bug.FlightHeight-step*3);continue;}
            }
            var flight = bug.Species == BugSpecies.Flies || bug.Species == BugSpecies.Ladybugs && bug.Activity == BugActivity.Exploring && bug.Age % 45 < 10;
            bug.State = flight ? BugState.Flying : BugState.Crossing;
            bug.FlightHeight = Math.Clamp(bug.FlightHeight + step * (flight ? 1.8 : -3), 0, 1);
            if (bug.Activity == BugActivity.Carrying)
            {
                bug.Target=nest.Position;
                if(bug.PartnerId is {} id)
                {
                    var partner=Bugs.FirstOrDefault(b=>b.InteractionId==id && b.State!=BugState.Squashed && b.Health>0 && b.Carrying>0);
                    if(partner==null)bug.PartnerId=null;
                    else bug.Target+=new Vector(0,bug.InteractionId.CompareTo(id)<0?-6:6);
                }
            }
            if (bug.Activity == BugActivity.Raiding && bug.RaidTarget is { } raided)
            {
                var targetNest = Nest(raided); bug.Target = targetNest.Position;
                if ((bug.Position-targetNest.Position).Length < 28)
                {
                    bug.Carrying = Math.Min(2,targetNest.Food); targetNest.Food -= bug.Carrying;
                    bug.RaidTarget = null; bug.EnemyId = null; bug.Activity = BugActivity.Carrying; bug.Target = nest.Position; bug.Decision = 30;
                }
                else if (bug.EnemyId == null)
                    bug.EnemyId = Bugs.FirstOrDefault(b=>b.Species==raided && b.Health>0 && (b.Position-bug.Position).Length<65)?.InteractionId;
            }
            if (bug.Activity == BugActivity.Gathering && Resources.FirstOrDefault(r => r.Id == bug.ResourceId) is { } resource)
            {
                bug.Target = resource.Position;
                if ((bug.Position - resource.Position).Length < 18)
                {
                    if (bug.Species is BugSpecies.Flies or BugSpecies.Ladybugs or BugSpecies.Spiders or BugSpecies.Centipedes)
                    {
                        resource.Amount = Math.Max(0, resource.Amount - .2); bug.Energy = Math.Min(1,bug.Energy+.2); bug.Health=Math.Min(100,bug.Health+3);
                        bug.Carrying=0;bug.Activity=BugActivity.Grooming;bug.State=BugState.Paused;bug.Decision=4;continue;
                    }
                    if(resource.Decoration)
                    {
                        if(bug.FeedingResource!=resource.Id){bug.FeedingResource=resource.Id;bug.FeedingUntil=SimulationAge+.7;}
                        if(SimulationAge<bug.FeedingUntil){bug.State=BugState.Paused;bug.FlightHeight=0;continue;}
                        bug.FeedingResource=null;
                    }
                    var partner = bug.Species==BugSpecies.Ants && resource.Kind==BugResourceKind.Candy ? Bugs.FirstOrDefault(b=>IsActive(b) && b!=bug && b.Species==BugSpecies.Ants && b.State!=BugState.Squashed && b.Health>0 && b.Carrying<=0 && (b.Position-bug.Position).Length<35) : null;
                    var amount=Math.Min(resource.Amount,partner!=null?2:.5);resource.Amount-=amount;
                    bug.Carrying=partner==null?amount:amount/2;bug.Activity=BugActivity.Carrying;bug.Target=nest.Position;bug.Decision=30;
                    if(partner!=null){bug.PartnerId=partner.InteractionId;partner.PartnerId=bug.InteractionId;partner.Carrying=amount/2;partner.Activity=BugActivity.Carrying;partner.Target=nest.Position;partner.Decision=30;partner.CrawlPhase=bug.CrawlPhase;}

                }
            }
            if (bug.Activity == BugActivity.Carrying && (bug.Position - nest.Position).Length < 24)
            {
                nest.Food = Math.Min(10000, nest.Food + bug.Carrying); nest.Deliveries++; nest.Construction = Math.Min(1, nest.Construction + .005);
                bug.Carrying = 0; bug.PartnerId=null; bug.Activity = BugActivity.Nesting; bug.Decision = 4; bug.Energy = Math.Min(1, bug.Energy + .1);
            }
            if (bug.Activity == BugActivity.Nesting)
            {
                if(bug.Species==BugSpecies.Spiders && Webs.FirstOrDefault(w=>w.Owner==bug.InteractionId || w.Helpers.Contains(bug.InteractionId)) is {} web)
                {if(!PrepareSilk(bug,web)){bug.State=BugState.Paused;continue;}}
                else {nest.Construction=Math.Min(1,nest.Construction+step*.001);bug.Target=nest.Position;}
            }
            if (bug.Activity is BugActivity.Hunting or BugActivity.Defending or BugActivity.Raiding) Encounter(bug, step);
            var speed = bug.Speed * .55 * (bug.Activity == BugActivity.Carrying ? .65 : 1);
            if(bug.PartnerId is {} partnerId && Bugs.FirstOrDefault(b=>b.InteractionId==partnerId) is {} carrier)speed=(bug.Speed+carrier.Speed)*.5*.55*.65;
            if(bug.Species==BugSpecies.Flies && flight && flyPhase<3)speed*=.14;
            if (EcosystemSettings.Behavior == BugBehavior.Creepy && bug.Species == BugSpecies.Roaches && bug.Personality != BugPersonality.Social) speed *= 1.8;
            var navigationTarget=(bug.Species==BugSpecies.Spiders && bug.Activity==BugActivity.Nesting || bug.Species==BugSpecies.Roaches && bug.Activity==BugActivity.Hiding || bug.Species==BugSpecies.Flies)?bug.Target:VisibleWaypoint(bug.Position,bug.Target);
            // A moved/resized widget can engulf a creature; place it on a free edge once.
            if(!(bug.Species==BugSpecies.Spiders && bug.Activity==BugActivity.Nesting) && Surfaces.Values.Any(rect=>rect.Contains(bug.Position)) && !Surfaces.Values.Any(rect=>rect.Contains(navigationTarget)))bug.Position=navigationTarget;
            var delta = navigationTarget - bug.Position;
            if (delta.Length > 2)
            {
                var movement = Math.Min(delta.Length, speed * step); delta.Normalize();
                var direction=Math.Atan2(delta.Y,delta.X)*180/Math.PI;
                bug.Angle = BugAnimation.SmoothAngle(bug.Angle,direction,step,bug.Species==BugSpecies.Roaches?420:280);
                if(bug.Species==BugSpecies.Spiders)movement*=Math.Max(0,Math.Cos((direction-bug.Angle)*Math.PI/180));
                bug.Position += delta * movement;bug.CrawlPhase += movement * .35;
            }
            else if (bug.Activity == BugActivity.Exploring) bug.Decision = 0;
            bug.Position = new(Math.Clamp(bug.Position.X, 12, WorldWidth - 12), Math.Clamp(bug.Position.Y, 12, WorldHeight - 12));
            if(bug.Species==BugSpecies.Spiders && bug.Activity==BugActivity.Nesting)LaySilk(bug);
        }
    }
    private void ChooseActivity(BugEntity bug)
    {
        bug.State = BugState.Crossing; bug.ResourceId = null; bug.EnemyId = null; bug.RaidTarget = null; bug.Decision = Range(5, 16);
        if(EcosystemSettings.Behavior==BugBehavior.Cozy && SimulationAge<bug.ExploreUntil && bug.Carrying<=0)
        {bug.Activity=BugActivity.Exploring;bug.Target=ExplorationPoint(bug.Position);bug.Decision=Range(3,6);return;}
        if(bug.Species==BugSpecies.Spiders && Webs.FirstOrDefault(w=>w.Owner==bug.InteractionId && w.Construction<1) is {} unfinished)
        {bug.Activity=BugActivity.Nesting;bug.Target=new(unfinished.X,unfinished.Y);bug.Decision=8;return;}
        if (bug.Carrying > 0) { bug.Activity = BugActivity.Carrying; bug.Target = Nest(bug.Species).Position; return; }
        if(bug.Species==BugSpecies.Spiders && ChooseWebActivity(bug))return;
        bug.SilkAttached=false;bug.WeavingSegment=-1;
        var mode = EcosystemSettings.Behavior; var roll = random.NextDouble();
        if(bug.Species==BugSpecies.Roaches && mode!=BugBehavior.Combative && roll<.22 && Surfaces.Count>0)
        {var cover=Surfaces.Values.ElementAt(random.Next(Surfaces.Count));bug.Activity=BugActivity.Hiding;bug.Target=new(cover.Left+cover.Width*.25,cover.Top+cover.Height*.35);bug.Decision=Range(3,7);return;}
        if(mode==BugBehavior.Classic)
        {
            bug.Activity=roll<.12?BugActivity.Grooming:BugActivity.Exploring;
            bug.Target=Surfaces.Count>0 && roll<.65?EdgePoint(Surfaces.Values.ElementAt(random.Next(Surfaces.Count)),random.NextDouble()):new Point(Range(25,WorldWidth-25),Range(25,WorldHeight-25));
            if(bug.Species==BugSpecies.Flies)bug.Target=new Point(Math.Clamp(bug.Position.X+Range(-180,180),25,WorldWidth-25),Math.Clamp(bug.Position.Y+Range(-120,120),25,WorldHeight-25));
            bug.Decision=Range(2,7);return;
        } if (bug.Personality == BugPersonality.Social && mode == BugBehavior.Cozy) roll *= .75;
        if (mode == BugBehavior.Combative)
        {
            if (Nest(bug.Species).Food < 15 && roll < .35)
            {
                var targetNest = Nests.Where(n=>n.Species!=bug.Species && n.Food>1).OrderByDescending(n=>n.Food).FirstOrDefault();
                if(targetNest!=null)
                {
                    bug.Activity=BugActivity.Raiding;bug.RaidTarget=targetNest.Species;bug.Target=targetNest.Position;bug.Decision=30;
                    foreach(var ally in Bugs.Where(b=>IsActive(b) && b!=bug && b.Species==bug.Species && b.Health>0 && (b.Position-bug.Position).Length<180).Take(2))
                    {ally.Activity=BugActivity.Raiding;ally.RaidTarget=targetNest.Species;ally.EnemyId=null;ally.Target=targetNest.Position;ally.Decision=30;}
                    return;
                }
            }
            var enemy = Bugs.Where(b => IsActive(b) && b != bug && b.Species != bug.Species && b.Health > 0 && b.State != BugState.Squashed).MinBy(b => (b.Position - bug.Position).LengthSquared);
            if (enemy != null && ((enemy.Position - Nest(bug.Species).Position).Length < 180 || roll < .6))
            { bug.Activity = (enemy.Position - Nest(bug.Species).Position).Length < 180 ? BugActivity.Defending : BugActivity.Raiding; bug.EnemyId = enemy.InteractionId; bug.Target = enemy.Position; bug.Decision = Range(2, 5);
                foreach(var ally in Bugs.Where(b=>IsActive(b) && b!=bug && b.Species==bug.Species && b.Health>0 && b.Activity!=BugActivity.Defending && (b.Position-bug.Position).Length<200).Take(2))
                {ally.Activity=BugActivity.Raiding;ally.EnemyId=enemy.InteractionId;ally.Target=enemy.Position;ally.Decision=4;}
                return; }
        }
        if (mode == BugBehavior.Creepy && bug.Species is BugSpecies.Spiders or BugSpecies.Centipedes && roll < .13)
        {
            var prey = Bugs.Where(b => IsActive(b) && b.Species is BugSpecies.Flies or BugSpecies.Ants && b.State != BugState.Squashed).MinBy(b => (b.Position - bug.Position).LengthSquared);
            if (prey != null && (prey.Position - bug.Position).Length < 180) { NaturalEncounters++; bug.Activity = BugActivity.Hunting; bug.EnemyId = prey.InteractionId; bug.Target = prey.Position; bug.Decision = 3; return; }
        }
        if (bug.Energy < .3 || roll < (mode == BugBehavior.Cozy ? .12 : .04)) { bug.Activity = BugActivity.Sleeping; bug.Decision = Range(5, 12); return; }
        if (mode == BugBehavior.Cozy && roll < .24) { bug.Activity = BugActivity.Socializing; bug.Target = Bugs.FirstOrDefault(b => IsActive(b) && b != bug && b.Species == bug.Species)?.Position ?? Nest(bug.Species).Position; return; }
        if (roll < .31) { bug.Activity = mode == BugBehavior.Creepy ? BugActivity.Hiding : BugActivity.Grooming; bug.Decision = Range(2, 5); return; }
        if (roll < .8)
        {
            var resource = Resources.Where(r => Edible(bug, r)).MinBy(r => (r.Position - bug.Position).LengthSquared);
            if (resource != null) { bug.Activity = BugActivity.Gathering; bug.ResourceId = resource.Id; bug.Target = resource.Position; return; }
        }
        bug.Activity = BugActivity.Exploring;
        bug.Target = bug.Species==BugSpecies.Flies?new Point(Math.Clamp(bug.Position.X+Range(-180,180),25,WorldWidth-25),Math.Clamp(bug.Position.Y+Range(-120,120),25,WorldHeight-25)):new Point(Range(25,WorldWidth-25),Range(25,WorldHeight-25));
        if (mode == BugBehavior.Creepy && Surfaces.Count > 0 && roll > .85) bug.Target = EdgePoint(Surfaces.Values.ElementAt(random.Next(Surfaces.Count)), random.NextDouble());
    }
    private void Encounter(BugEntity bug, double dt)
    {
        if(EcosystemSettings.Behavior is BugBehavior.Classic or BugBehavior.Cozy){bug.EnemyId=null;bug.RaidTarget=null;bug.Activity=BugActivity.Exploring;bug.Decision=0;return;}
        var enemy = Bugs.FirstOrDefault(b => IsActive(b) && b.InteractionId == bug.EnemyId && b.Health > 0 && b.State != BugState.Squashed);
        if (enemy == null) { bug.EnemyId = null; if (bug.RaidTarget == null) bug.Decision = 0; return; }
        bug.Target = enemy.Position;
        if ((enemy.Position - bug.Position).Length > 24 * EcosystemSettings.Size) return;
        var damage = (bug.Species switch { BugSpecies.Ants => 8, BugSpecies.Flies => 6, BugSpecies.Roaches => 9, BugSpecies.Spiders => 10, BugSpecies.Ladybugs => 8, _ => 10 }) * dt;
        if (EcosystemSettings.Behavior == BugBehavior.Creepy) { damage *= .25; }
        var defended = (enemy.Position - Nest(enemy.Species).Position).Length < 100;
        enemy.Health = Math.Max(0, enemy.Health - damage * (defended ? .75 : 1)); enemy.HitFlash = .12;
        if (enemy.Health > 0 && EcosystemSettings.Behavior == BugBehavior.Combative && enemy.EnemyId == null)
        {
            enemy.EnemyId = bug.InteractionId; enemy.Activity = BugActivity.Defending;
            enemy.Target = bug.Position; enemy.Decision = Math.Max(enemy.Decision, 3);
        }
        if (enemy.Health <= 0) { CombatDeaths++; bug.Kills++; enemy.State = BugState.Squashed; enemy.SquashAge = 0; bug.Decision = 0; }
    }
}
