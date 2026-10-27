using System.Windows;

namespace SessionDashboard;
internal enum CombatState { Idle, Patrol, Detect, Pursue, Attack, Defend, Retreat, Regroup, Defeated }
internal enum ArmyStrategy { Peace, Scouting, Preparing, Mobilizing, Raiding, Invading, Defending, Retreating, Recovering }
internal enum ArmyRole { HomeGuard, BorderPatrol, Scout, Expeditionary, Reserve }
internal enum ArcadeRuleSet { InfiniteWarfare, ClassicRounds }
internal enum BattlePhase { Mustering, Fighting, Victory }
internal sealed record SpeciesCombatStats(double Health, double Damage, double AttackCooldown, double Speed,
    double DetectionRange, double AttackRange, double AbilityCooldown, double LeaderHealth, double ReinforcementRate, int MaximumUnits)
{
    internal SpeciesCombatStats Safe(SpeciesCombatStats fallback) => new(
        Finite(Health,5,150,fallback.Health),Finite(Damage,1,30,fallback.Damage),Finite(AttackCooldown,.25,4,fallback.AttackCooldown),
        Finite(Speed,25,240,fallback.Speed),Finite(DetectionRange,60,400,fallback.DetectionRange),Finite(AttackRange,8,45,fallback.AttackRange),
        Finite(AbilityCooldown,2,20,fallback.AbilityCooldown),Finite(LeaderHealth,150,1200,fallback.LeaderHealth),
        Finite(ReinforcementRate,.5,3,fallback.ReinforcementRate),Math.Clamp(MaximumUnits,6,35));
    internal static double Finite(double value,double min,double max,double fallback)=>double.IsFinite(value) ? Math.Clamp(value,min,max) : fallback;
}
internal sealed record ArcadeSettings
{
    public ArcadeRuleSet Mode { get; init; }=ArcadeRuleSet.InfiniteWarfare;
    public int MaximumSharedWebs {get;init;}=4;
    public double WebConstructionRate {get;init;}=1;
    public bool FunctionalWebTraps {get;init;}=true;
    public bool ShowTerritories {get;init;}=true;
    public bool ShowTerritoryBorders {get;init;}=true;
    public bool ShowFactionLabels {get;init;}=true;
    public bool HighlightContested {get;init;}=true;
    public double TerritoryOpacity {get;init;}=.1;
    public bool EnableCampaigns { get; init; }=true;
    public double Aggression { get; init; }=1;
    public double WarFrequency { get; init; }=1;
    public bool AllowNewCivilizations { get; init; }=true;
    public int SoftCap { get; init; }=150;
    public int HardCap { get; init; }=180;
    public double SpawnInterval { get; init; }=.5;
    public int GroupSize { get; init; }=4;
    public int TargetFps { get; init; }=60;
    public bool ShowScoreboard { get; init; }=true;
    public Dictionary<BugSpecies,SpeciesCombatStats>? Species { get; init; }
    internal static readonly Dictionary<BugSpecies,SpeciesCombatStats> Defaults=new()
    {
        [BugSpecies.Ants]=new(32,5.5,.65,76,185,16,6,390,.8,28),
        [BugSpecies.Spiders]=new(44,7,.95,86,220,23,5.5,400,1.1,24),
        [BugSpecies.Roaches]=new(56,7.5,1.0,64,180,22,6,400,1,26),
        [BugSpecies.Ladybugs]=new(44,6,.85,66,175,20,6,400,1,26),
        [BugSpecies.Centipedes]=new(56,12,1.05,112,230,26,4.5,410,1.3,24),
        [BugSpecies.Flies]=new(30,6,.7,158,210,21,4,390,.85,24)
    };
    internal ArcadeSettings Safe()
    {
        var hard=Math.Clamp(HardCap,36,240);
        var stats=new Dictionary<BugSpecies,SpeciesCombatStats>();
        foreach(var pair in Defaults)stats[pair.Key]=(Species?.GetValueOrDefault(pair.Key)??pair.Value).Safe(pair.Value);
        return this with {Mode=Enum.IsDefined(Mode) ? Mode : ArcadeRuleSet.InfiniteWarfare,MaximumSharedWebs=Math.Clamp(MaximumSharedWebs,1,8),WebConstructionRate=SpeciesCombatStats.Finite(WebConstructionRate,.25,2,1),TerritoryOpacity=SpeciesCombatStats.Finite(TerritoryOpacity,.03,.25,.1),Aggression=SpeciesCombatStats.Finite(Aggression,.25,2,1),WarFrequency=SpeciesCombatStats.Finite(WarFrequency,.25,2,1),HardCap=hard,SoftCap=Math.Clamp(SoftCap,24,hard-1),SpawnInterval=SpeciesCombatStats.Finite(SpawnInterval,.3,2,.5),GroupSize=Math.Clamp(GroupSize,3,6),TargetFps=TargetFps>=45 ? 60 : 30,Species=stats};
    }
}
internal sealed class BugColony(BugSpecies species)
{
    internal readonly BugSpecies Species=species;
    internal Point Territory,SurvivorCenter,CampaignRally;
    internal ArmyStrategy Strategy;
    internal int? TargetFactionId;
    internal double StrategyUntil,NextCampaign,CampaignStarted,Strength;
    internal int InitialForce,CampaignKills;
    internal bool MajorCampaign;
    internal uint ColorArgb;
    internal readonly Dictionary<int,double> Rivalries=[];
    internal readonly Dictionary<int,int> KillsAgainst=[];
    internal int Id;
    internal string Name="";
    internal bool Regrouping;
    internal double Resources=90,RecoveryTime,DefeatTime,SettlementLostAt;
    internal int Recoveries;
    internal Point Objective => BugManager.Alive(Leader) ? Leader.Position : SurvivorCenter;
    internal Point Rally => BugManager.Alive(Leader) ? Leader.Position : Territory;
    internal BugEntity Leader=null!;
    internal bool Eliminated;
    internal int Alive,Kills;
    internal double NextWave;
}
internal sealed class CombatWebProjectile
{
    internal Point Position,Previous;
    internal BugEntity Target=null!;
    internal BugSpecies Species;
    internal BugEntity? Attacker;
    internal double Age;
}
internal sealed record BattleAnnouncement(string Text,BugSpecies? Accent,bool Victory=false);
internal readonly record struct CombatHit(BugEntity Target,BugEntity Attacker,double Amount);

internal enum ColonyEventType { Founded, LeaderKilled, Retreat, Recovery, Extinction, Victory, LegendaryWarrior, Emerged, Mobilized, InvasionLaunched, BattleWon, CampaignWithdrawn, MajorDefeat, TerritoryCaptured, TerritoryLost, NestConquered, HomelandReclaimed, SettlementFounded, OutpostAbandoned }
internal sealed record ColonyEvent(double Timestamp,int Match,ColonyEventType Type,int FactionId,int? OtherFactionId,int? UnitId=null,int? RegionId=null,int? SettlementId=null);
