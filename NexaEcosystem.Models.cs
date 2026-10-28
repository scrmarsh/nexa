using System.IO;
using System.Text.Json;
using System.Windows;

namespace SessionDashboard;

internal enum BugBehavior { Cozy = 0, Creepy = 1, Combative = 2, Classic = 3 }
internal enum BugAppearance { Normal = 0, Cozy = 1, Creepy = 2, Halloween = 3 }
internal enum BugAnimationQuality { Performance, Balanced, High }
internal enum BugActivity { Exploring, Gathering, Carrying, Nesting, Socializing, Grooming, Sleeping, Hiding, Hunting, Defending, Raiding }
internal enum BugPersonality { Curious, Shy, Social, Bold }
internal enum BugResourceKind { Candy, Fruit, Leaves, Water, Protein }

internal sealed record EcosystemOptions
{
    public BugBehavior Behavior { get; init; } = BugBehavior.Classic;
    public BugBehavior? NonSeasonalBehavior {get;init;}
    public BugBehavior? SeasonalBehavior {get;init;}
    public bool? LastSeasonalActive {get;init;}
    public BugAppearance Appearance { get; init; } = BugAppearance.Normal;
    public bool MatchAppearance { get; init; } = true;
    public bool Adorable { get; init; } = true;
    public bool AdvancedAnimations { get; init; }
    public double HitboxExpansion {get;init;}=1.5;
    public double Size { get; init; } = 2;
    public BugAnimationQuality Quality { get; init; } = BugAnimationQuality.Balanced;
    public bool Costumes { get; init; } = true;
    [System.Text.Json.Serialization.JsonIgnore] public BugAppearance EffectiveAppearance => !MatchAppearance ? Appearance : Behavior switch { BugBehavior.Cozy => BugAppearance.Cozy, BugBehavior.Creepy => BugAppearance.Creepy, _ => BugAppearance.Normal };
    internal EcosystemOptions ForSeason(bool active)
    {
        if (!MatchAppearance) return this with { LastSeasonalActive = active };
        if(LastSeasonalActive==active)return this;
        var normal=NonSeasonalBehavior;var seasonal=SeasonalBehavior;
        if(LastSeasonalActive==true)seasonal=Behavior;else if(LastSeasonalActive==false)normal=Behavior;
        var selected=active?seasonal ?? BugBehavior.Cozy:normal ?? BugBehavior.Classic;
        return this with {Behavior=selected,NonSeasonalBehavior=normal,SeasonalBehavior=seasonal,LastSeasonalActive=active,Adorable=active && seasonal==null?true:Adorable};
    }
    internal EcosystemOptions ChooseBehavior(BugBehavior behavior,bool seasonal)=>this with {Behavior=behavior,LastSeasonalActive=seasonal,NonSeasonalBehavior=seasonal?NonSeasonalBehavior:behavior,SeasonalBehavior=seasonal?behavior:SeasonalBehavior};
    internal static EcosystemOptions Existing(EcosystemOptions value)=>value.LastSeasonalActive==null && value.NonSeasonalBehavior==null && value.SeasonalBehavior==null?value with {NonSeasonalBehavior=value.Behavior,SeasonalBehavior=value.Behavior}:value;
    internal EcosystemOptions Safe() => this with
    {
        Behavior = Enum.IsDefined(Behavior) ? Behavior : BugBehavior.Classic,
        NonSeasonalBehavior = NonSeasonalBehavior is {} normal && !Enum.IsDefined(normal) ? null : NonSeasonalBehavior,
        SeasonalBehavior = SeasonalBehavior is {} seasonal && !Enum.IsDefined(seasonal) ? null : SeasonalBehavior,
        Appearance = Enum.IsDefined(Appearance) ? Appearance : BugAppearance.Normal,
        Quality = Enum.IsDefined(Quality) ? Quality : BugAnimationQuality.Balanced,
        HitboxExpansion = double.IsFinite(HitboxExpansion)?Math.Clamp(HitboxExpansion,1.4,1.6):1.5,
        Size = double.IsFinite(Size) ? Math.Clamp(Size, .75, 4) : 2
    };
}
internal sealed class EcosystemNest
{
    public BugSpecies Species { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Food { get; set; } = 20;
    public double Construction { get; set; } = .3;
    public int Deliveries { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] internal Point Position => new(X, Y);
}
internal sealed class EcosystemResource
{
    public bool Decoration {get;set;}
    public Guid Id { get; set; } = Guid.NewGuid();
    public BugResourceKind Kind { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Amount { get; set; } = 12;
    public double Regrowth { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] internal Point Position => new(X, Y);
}
internal sealed class SilkStrand
{
    public double FromX {get;set;}
    public double FromY {get;set;}
    public double ToX {get;set;}
    public double ToY {get;set;}
    public double Progress {get;set;}
}
internal sealed class EcosystemWeb
{
    public List<SilkStrand> Strands {get;set;}=[];
    public List<Guid> Helpers {get;set;}=[];
    public string? AnchorId {get;set;}
    public double AnchorOffsetX {get;set;}
    public double AnchorOffsetY {get;set;}
    public Guid Owner { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Construction { get; set; }
    public double Age { get; set; }
}
internal sealed class SavedEcosystemBug
{
    public Guid Id { get; set; }
    public BugSpecies Species { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double TargetX { get; set; }
    public double TargetY { get; set; }
    public double Angle { get; set; }
    public double Speed { get; set; }
    public double Age { get; set; }
    public double FlightHeight {get;set;}
    public double Health { get; set; } = 100;
    public double Energy { get; set; } = 1;
    public int Hits { get; set; }
    public int Kills { get; set; }
    public BugState State { get; set; }
    public BugActivity Activity { get; set; }
    public BugPersonality Personality { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid? EnemyId { get; set; }
    public Guid? PartnerId { get; set; }
    public BugSpecies? RaidTarget { get; set; }
    public double Carrying { get; set; }
    public double Decision { get; set; }
    public double Phase { get; set; }
}
internal sealed class EcosystemSquash
{
    public Guid BugId { get; set; }
    public Guid SessionId { get; set; }
    public BugSpecies Species { get; set; }
    public DateTimeOffset At { get; set; }
}
internal sealed class EcosystemDocument
{
    public int Schema { get; set; } = 1;
    public string UserId { get; set; } = "";
    public Guid WorldId { get; set; } = Guid.NewGuid();
    public int Seed { get; set; } = Random.Shared.Next();
    public DateTimeOffset UpdatedAt { get; set; }
    public double SimulationAge { get; set; }
    public bool Paused { get; set; }
    public long NaturalEncounters { get; set; }
    public long CombatDeaths { get; set; }
    public EcosystemOptions Options { get; set; } = new();
    public List<SavedEcosystemBug> Bugs { get; set; } = [];
    public List<EcosystemSquash> PendingSquashes { get; set; } = [];
    public List<EcosystemNest> Nests { get; set; } = [];
    public List<EcosystemResource> Resources { get; set; } = [];
    public List<EcosystemWeb> Webs { get; set; } = [];

    internal void Validate(string user)
    {
        if (UserId != user) throw new UnauthorizedAccessException("This ecosystem belongs to another Windows user.");
        bool Coordinate(double x, double y) => double.IsFinite(x) && double.IsFinite(y) && x is >= 0 and <= BugManager.WorldWidth && y is >= 0 and <= BugManager.WorldHeight;
        var species = BugManager.AllSpecies;
        if (Schema != 1 || WorldId == Guid.Empty || !double.IsFinite(SimulationAge) || SimulationAge < 0 || CombatDeaths < 0 || NaturalEncounters < 0 || Options == null || Options != Options.Safe() ||
            PendingSquashes == null || PendingSquashes.Count > 500 || PendingSquashes.Any(s=>s==null || s.BugId==Guid.Empty || !species.Contains(s.Species)) || PendingSquashes.Select(s=>s.BugId).Distinct().Count()!=PendingSquashes.Count ||
            Bugs == null || Bugs.Any(b => b == null || b.Id == Guid.Empty || !species.Contains(b.Species) || !Coordinate(b.X, b.Y) || !Coordinate(b.TargetX, b.TargetY) ||
                !double.IsFinite(b.FlightHeight) || b.FlightHeight is < 0 or > 1 || !double.IsFinite(b.Health) || b.Health is < 0 or > 100 || !double.IsFinite(b.Energy) || b.Energy is < 0 or > 1 || !double.IsFinite(b.Speed) || b.Speed is < 0 or > 300 ||
                !double.IsFinite(b.Age) || b.Age < 0 || !double.IsFinite(b.Angle) || !double.IsFinite(b.Decision) || !double.IsFinite(b.Phase) || !double.IsFinite(b.Carrying) || b.Carrying is < 0 or > 10 || b.Hits < 0 || b.Kills < 0 || b.RaidTarget != null && !species.Contains(b.RaidTarget.Value) || !Enum.IsDefined(b.Activity) || !Enum.IsDefined(b.Personality) || !Enum.IsDefined(b.State)) ||
            Bugs.Select(b => b.Id).Distinct().Count() != Bugs.Count || Nests == null || Nests.Count != 6 || Nests.Any(n => n == null) || Nests.Select(n => n.Species).Distinct().Count() != 6 || Nests.Any(n => !species.Contains(n.Species) || !Coordinate(n.X, n.Y) || !double.IsFinite(n.Food) || n.Food is < 0 or > 10000 || !double.IsFinite(n.Construction) || n.Construction is < 0 or > 1 || n.Deliveries < 0) ||
            Resources == null || Resources.Count > 128 || Resources.Any(r => r == null || r.Id == Guid.Empty || !Coordinate(r.X, r.Y) || !Enum.IsDefined(r.Kind) || !double.IsFinite(r.Amount) || r.Amount is < 0 or > 100 || !double.IsFinite(r.Regrowth) || r.Regrowth < 0) || Resources.Select(r => r.Id).Distinct().Count() != Resources.Count ||
            Webs == null || Webs.Count > 24 || Webs.Any(w => w == null || w.Helpers==null || w.Helpers.Count>8 || w.Helpers.Any(id=>id==Guid.Empty) || w.Helpers.Distinct().Count()!=w.Helpers.Count || w.Strands==null || w.Strands.Count>80 || w.Strands.Any(t=>t==null || !double.IsFinite(t.FromX) || !double.IsFinite(t.FromY) || !double.IsFinite(t.ToX) || !double.IsFinite(t.ToY) || !double.IsFinite(t.Progress) || t.FromX is <0 or >1 || t.FromY is <0 or >1 || t.ToX is <0 or >1 || t.ToY is <0 or >1 || t.Progress is <0 or >1) || !Coordinate(w.X, w.Y) || w.AnchorId?.Length>128 || !double.IsFinite(w.AnchorOffsetX) || !double.IsFinite(w.AnchorOffsetY) || Math.Abs(w.AnchorOffsetX)>1600 || Math.Abs(w.AnchorOffsetY)>1000 || !double.IsFinite(w.Age) || w.Age < 0 || !double.IsFinite(w.Construction) || w.Construction is < 0 or > 1))
            throw new InvalidDataException("The ecosystem save contains invalid data. Its original files were preserved.");
    }
}
