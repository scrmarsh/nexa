using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal enum BugSpecies { Ants, Roaches, Flies, AllThree, Spiders, AllBugs, Ladybugs, Centipedes }
internal enum BugDensity { Low=0, Normal=1, High=2, Infestation=3 }
internal enum BugVariant { None, QueenRoach, QueenFly, QueenLadybug, MamaSpider, KingCentipede }
internal enum BugState { Edge, Crossing, Flying, Landed, Paused, Hiding, Squashed, Bursting, Trapped }
internal sealed record BugPreferences(bool Enabled=false, BugSpecies Species=BugSpecies.Ants, BugDensity Density=BugDensity.Normal, EcosystemOptions? Ecosystem=null);
internal readonly record struct BugSurface(string Id, Rect Bounds);
internal sealed class BugEntity
{
    internal Guid InteractionId=Guid.NewGuid();
    internal double Scale=2, Health=100, Energy=1, Carrying, OffscreenTime, LastEyeTime, FlightHeight;
    internal Vector Gaze;
    internal int Kills;
    internal BugActivity Activity;
    internal BugPersonality Personality;
    internal Guid? ResourceId, EnemyId, PartnerId;
    internal BugSpecies? RaidTarget;
    internal BugSpecies Species;
    internal BugVariant Variant;
    internal BugState State;
    internal Point Position, Target;
    internal Guid? FeedingResource;
    internal double FeedingUntil;
    internal double Angle,Speed,Decision,EdgeProgress,SquashAge,HitFlash,CrawlPhase,Age;
    internal int Direction=1,Hits;
    internal Vector BurstVelocity;
    internal double BurstRemaining,ExcessLifetime;
    internal string? Surface;
    internal double SizeScale=>Scale*(Species switch {BugSpecies.Ants=>.75,BugSpecies.Spiders=>1.65,BugSpecies.Roaches=>1.45,BugSpecies.Centipedes=>1.4,_=>1.25});
}
internal sealed class BugSplat { internal Point Position; internal BugSpecies Species; internal double Age; }
internal sealed partial class BugManager
{
    private readonly Random random;
    internal readonly List<BugEntity> Bugs=[];
    internal readonly List<BugSplat> Splats=[];
    internal readonly Dictionary<string,Rect> Surfaces=[];
    private bool surfaceChoicesDirty=true;
    private BugSurface[] surfaceChoices=[];
    private double replacementDelay;
    internal BugPreferences Preferences {get;private set;}=new();
    internal Size Bounds;
    internal Rect VisibleBounds {get;set;}
    internal bool SpawnPressure;
    internal const int InfestationCap=600;
    internal int Cap=>Preferences.Species==BugSpecies.Centipedes ? Math.Max(1,BaseCap/12) : BaseCap;
    private int BaseCap=>Preferences.Density switch {BugDensity.Low=>9,BugDensity.High=>96,BugDensity.Infestation=>int.MaxValue,_=>24};
    internal double SpawnInterval=>(Preferences.Density==BugDensity.Infestation?.08:.65)*(Preferences.Species==BugSpecies.Centipedes?6:1)*(Preferences.Density==BugDensity.Low?2:1)*(SpawnPressure?3:1);
    internal BugManager(int? seed=null) {random=seed is {} value ? new Random(value) : new Random();}
    internal void Configure(BugPreferences preferences)
    {
        preferences=preferences with {Species=preferences.Species==BugSpecies.AllThree?BugSpecies.AllBugs:preferences.Species};
        preferences=preferences with {Ecosystem=(preferences.Ecosystem ?? new()).Safe()};
        if(Preferences==preferences)return;
        var modeChanged=Preferences.Ecosystem?.Behavior!=preferences.Ecosystem?.Behavior;
        var previousSpecies = Preferences.Species;
        Preferences=preferences; if(!EcosystemActive) Clear();
        if(EcosystemActive)
        {
            if(preferences.Species!=BugSpecies.AllBugs)Bugs.RemoveAll(b=>b.Species!=preferences.Species);
            else if (previousSpecies != BugSpecies.AllBugs && Bugs.All(b => b.Species == previousSpecies))
            {
                // Free slots for newly enabled species without crediting removals as squashes.
                var retainedPerSpecies = Math.Max(1, Math.Min(EcosystemCap, Bugs.Count) / AllSpecies.Length);
                foreach (var group in Bugs.GroupBy(b => b.Species).ToArray())
                    foreach (var excess in group.Skip(retainedPerSpecies).ToArray()) Bugs.Remove(excess);
            }
            if(Bugs.Count>EcosystemCap)Bugs.RemoveRange(EcosystemCap,Bugs.Count-EcosystemCap);
        }
        if(EcosystemActive && modeChanged) foreach(var bug in Bugs){bug.Decision=0;bug.EnemyId=null;bug.RaidTarget=null;bug.Activity=BugActivity.Exploring;}
    }
    internal void Clear(){Bugs.Clear();Splats.Clear();replacementDelay=0;}
    internal void SetSurfaces(IEnumerable<BugSurface> surfaces,Size bounds)
    {
        var next=surfaces.ToArray();if(Bounds==bounds && Surfaces.Count==next.Length && next.All(s=>Surfaces.TryGetValue(s.Id,out var prior)&&prior==s.Bounds))return;
        Bounds=bounds;Surfaces.Clear();foreach(var s in next)Surfaces[s.Id]=s.Bounds;surfaceChoicesDirty=true;
        if(EcosystemActive)
        {
            foreach(var web in Webs)if(web.AnchorId is {} id && Surfaces.TryGetValue(id,out var rect)){web.X=Math.Clamp(rect.Right+web.AnchorOffsetX,12,WorldWidth-12);web.Y=Math.Clamp(rect.Top+web.AnchorOffsetY,12,WorldHeight-12);}
            foreach(var resource in Resources)if(Surfaces.Values.Any(rect=>rect.Contains(resource.Position))){var p=VisibleWaypoint(resource.Position,resource.Position);resource.X=p.X;resource.Y=p.Y;}
            foreach(var nest in Nests)if(Surfaces.Values.Any(rect=>rect.Contains(nest.Position))){var p=VisibleWaypoint(nest.Position,nest.Position);nest.X=p.X;nest.Y=p.Y;}
        }
    }
private double Range(double low, double high) => low + random.NextDouble() * (high - low);
private Point FreePoint()
    {
        Point point = new();
        // Prefer open space; a full dashboard still needs a safe bounded fallback.
        var area = VisibleBounds.Width > 30 && VisibleBounds.Height > 30 ? VisibleBounds : new Rect(Bounds);
        for (var i = 0; i < 12; i++)
        {
            point = new(Range(area.Left + 12, Math.Max(area.Left + 12, area.Right - 12)),
                Range(area.Top + 12, Math.Max(area.Top + 12, area.Bottom - 12)));
            var covered = false;
            foreach (var rect in Surfaces.Values) if (rect.Contains(point)) { covered = true; break; }
            if (!covered) break;
        }
        return point;
    }
private void Detach(BugEntity bug)
    { bug.Surface = null; bug.State = bug.Species == BugSpecies.Flies ? BugState.Flying : BugState.Crossing; bug.Target = FreePoint(); bug.Decision = Range(.4, 1.4); }
private bool PickSurface(BugEntity bug, bool approach)
    {
        if (Surfaces.Count == 0) return false;
        if(surfaceChoicesDirty)
        {
            surfaceChoices = Surfaces.Where(pair => VisibleBounds.Width <= 0 || VisibleBounds.Height <= 0 || pair.Value.IntersectsWith(VisibleBounds))
                .Select(pair => new BugSurface(pair.Key,pair.Value)).ToArray();
            surfaceChoicesDirty = false;
        }
        if (surfaceChoices.Length == 0) return false;
        var surface = surfaceChoices[random.Next(surfaceChoices.Length)]; bug.Surface = surface.Id;
        bug.EdgeProgress = bug.Species == BugSpecies.Flies ? Range(.1, .9) * surface.Bounds.Width / (2 * (surface.Bounds.Width + surface.Bounds.Height)) : random.NextDouble();
        bug.Target = EdgePoint(surface.Bounds, bug.EdgeProgress);
        bug.State = approach ? BugState.Crossing : bug.Species == BugSpecies.Flies ? BugState.Landed : BugState.Edge;
        if (!approach) bug.Position = bug.Target;
        return true;
    }
internal static Point EdgePoint(Rect r, double fraction)
    {
        var d = ((fraction % 1 + 1) % 1) * (r.Width + r.Height) * 2;
        if (d < r.Width) return new(r.Left + d, r.Top);
        d -= r.Width; if (d < r.Height) return new(r.Right, r.Top + d);
        d -= r.Height; if (d < r.Width) return new(r.Right - d, r.Bottom);
        return new(r.Left, r.Bottom - (d - r.Width));
    }
private BugEntity Spawn(Point? burstOrigin = null, double angle = 0)
    {
        var species = Preferences.Species switch
        {
            BugSpecies.AllThree => (BugSpecies)(Bugs.Count % 3),
            BugSpecies.AllBugs => MixedSpecies(),
            _ => Preferences.Species
        };
        var bug = new BugEntity { Species = species, Position = FreePoint(), Target = FreePoint(), Decision = Range(.5, 2.8),
            Speed = species switch { BugSpecies.Ants => Range(18,34), BugSpecies.Roaches => Range(75,125), BugSpecies.Ladybugs => Range(22,38), BugSpecies.Centipedes => Range(38,58), _ => Range(50,90) },
            Direction = random.Next(2) == 0 ? -1 : 1 };
        bug.State = species == BugSpecies.Flies ? BugState.Flying : BugState.Crossing;
        if (burstOrigin is { } origin)
        {
            bug.Position = origin; bug.State = BugState.Bursting; bug.BurstRemaining = .55; bug.ExcessLifetime = 12;
            bug.BurstVelocity = new Vector(Math.Cos(angle),Math.Sin(angle))*Range(220,360);
            bug.Angle = angle*180/Math.PI;
        }
        else if (species != BugSpecies.Flies) PickSurface(bug, false);
        Bugs.Add(bug); return bug;
    }
private BugSpecies MixedSpecies()
    {
        return random.Next(100) switch
        {
            <20 => BugSpecies.Ants, <35 => BugSpecies.Roaches, <53 => BugSpecies.Flies,
            <69 => BugSpecies.Spiders, <85 => BugSpecies.Ladybugs, _ => BugSpecies.Centipedes
        };
    }
private void Decide(BugEntity bug)
    {
        bug.Decision = Range(.8, 3);
        if (bug.State == BugState.Hiding) { PickSurface(bug, false); bug.State = BugState.Edge; bug.Decision = Range(.2, .7); return; }
        if (bug.State is BugState.Landed or BugState.Paused) { Detach(bug); return; }
        var roll = random.NextDouble();
        if (bug.Species == BugSpecies.Flies)
        { if (roll < .35 && PickSurface(bug, true)) return; Detach(bug); return; }
        if (bug.Species is BugSpecies.Ants or BugSpecies.Centipedes)
        { if (roll < .2) bug.Direction *= -1; else if (bug.Surface == null) PickSurface(bug, true); return; }
        if (bug.Species == BugSpecies.Roaches && roll < .65 && PickSurface(bug, true)) return;
        if (roll < .18) { bug.State = bug.Species == BugSpecies.Roaches && bug.Surface != null ? BugState.Hiding : BugState.Paused; bug.Decision = Range(.25, .9); }
        else if (roll < .55) { bug.Direction *= -1; if (bug.State != BugState.Edge) PickSurface(bug, true); }
        else if (bug.Species == BugSpecies.Roaches || roll > .85) { Detach(bug); if (bug.Species == BugSpecies.Ants) PickSurface(bug, true); }
    }
internal BugEntity? Hit(Point point)
    {
        for (var i = Bugs.Count - 1; i >= 0; i--)
        {
            var bug = Bugs[i]; if (bug.State is BugState.Hiding or BugState.Trapped) continue;
            double radius = bug.Species == BugSpecies.Centipedes ? 18 : bug.Species is BugSpecies.Roaches or BugSpecies.Spiders or BugSpecies.Ladybugs ? 10 : 7;
            radius*=bug.SizeScale;
            if ((point - bug.Position).LengthSquared <= radius * radius) return bug;
        }
        return null;
    }
    internal void Advance(double dt)
    {
        if(EcosystemActive){AdvanceEcosystem(dt);return;}
        if(!Preferences.Enabled || Bounds.Width<30 || Bounds.Height<30)return;
        dt=Math.Clamp(dt,0,.08);replacementDelay=Math.Max(-.08,replacementDelay-dt);
        for(var count=0;replacementDelay<=0 && Bugs.Count<Cap && count<2 && (Preferences.Density!=BugDensity.Infestation || AdaptiveSpawnAllowed);count++){Spawn();replacementDelay+=SpawnInterval;}
        for(var i=Splats.Count-1;i>=0;i--){Splats[i].Age+=dt;if(Splats[i].Age>=4)Splats.RemoveAt(i);}
        for(var i=Bugs.Count-1;i>=0;i--)
        {
            var bug=Bugs[i];bug.Age+=dt;
            if(bug.State==BugState.Squashed)
            {
                bug.SquashAge+=dt;
                if(bug.SquashAge>=.16){if(Splats.Count>=60)Splats.RemoveAt(0);Splats.Add(new(){Position=bug.Position,Species=bug.Species});Bugs.RemoveAt(i);replacementDelay=Math.Max(replacementDelay,1.5);}
                continue;
            }
            bug.HitFlash=Math.Max(0,bug.HitFlash-dt);
            if(bug.Surface is {} missing && !Surfaces.ContainsKey(missing))Detach(bug);
            bug.Decision-=dt;if(bug.Decision<=0)Decide(bug);
            var old=bug.Position;
            if(bug.State==BugState.Edge && bug.Surface is {} key && Surfaces.TryGetValue(key,out var rect))
            {bug.EdgeProgress+=bug.Direction*bug.Speed*dt/Math.Max(1,2*(rect.Width+rect.Height));bug.Position=EdgePoint(rect,bug.EdgeProgress);}
            else if(bug.State is BugState.Flying or BugState.Crossing)
            {
                if(bug.Surface is {} id && Surfaces.TryGetValue(id,out var target))bug.Target=EdgePoint(target,bug.EdgeProgress);
                var delta=bug.Target-bug.Position;
                if(delta.Length<=bug.Speed*dt+1)
                {
                    bug.Position=bug.Target;
                    if(bug.Surface!=null){bug.State=bug.Species==BugSpecies.Flies?BugState.Landed:bug.Species==BugSpecies.Roaches?BugState.Hiding:BugState.Edge;bug.Decision=bug.Species==BugSpecies.Roaches?Range(.6,2.2):Range(1,3);}
                    else {bug.Target=FreePoint();bug.Decision=Range(.3,1);}
                }
                else {delta.Normalize();bug.Position+=delta*bug.Speed*dt;}
            }
            var movement=bug.Position-old;
            if(movement.LengthSquared>.001){bug.Angle=Math.Atan2(movement.Y,movement.X)*180/Math.PI;bug.CrawlPhase+=movement.Length*.35;}
            bug.Position=new(Math.Clamp(bug.Position.X,7,Bounds.Width-7),Math.Clamp(bug.Position.Y,7,Bounds.Height-7));
        }
    }
    internal bool Squash(BugEntity bug)
    {
        if(bug.Health<=0||!Bugs.Contains(bug)||bug.State is BugState.Squashed or BugState.Hiding)return false;
        var squash=bug.Hits>=(bug.Species==BugSpecies.Roaches?4:bug.Species==BugSpecies.Ladybugs?1:0);bug.Hits++;
        if(!squash){bug.HitFlash=.35;bug.State=BugState.Paused;bug.Surface=null;bug.Decision=.4;return false;}
        bug.State=BugState.Squashed;bug.Surface=null;
        foreach(var other in Bugs)if(other!=bug && other.State!=BugState.Squashed && (other.Position-bug.Position).LengthSquared<6400)Detach(other);
        return true;
    }
}
