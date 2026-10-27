using System.Windows;
namespace SessionDashboard;
internal sealed class ArcadeWorldSnapshot
{
    public int SchemaVersion {get;set;}=1;
    public Guid WorldId {get;set;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int LivingGeneratorVersion {get;set;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public System.Text.Json.JsonElement? LivingPolitics {get;set;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public System.Text.Json.JsonElement? LivingCivilization {get;set;}
    public double Width {get;set;}
    public double Height {get;set;}
    public double Time {get;set;}
    public bool Paused {get;set;}
    public ulong RandomState {get;set;}
    public ArcadeSettings Settings {get;set;}=new();
    public int NextWeb {get;set;}
    public int NextSettlement {get;set;}
    public double TerritoryDelay {get;set;}
    public TerritoryRegion[] Territories {get;set;}=[];
    public ColonySettlement[] Settlements {get;set;}=[];
    public bool SpawnPressure {get;set;}
    public Dictionary<int,BugSpecies> FactionSpecies {get;set;}=[];
    public Dictionary<int,uint> FactionColors {get;set;}=[];
    public int NextFaction {get;set;}
    public int NextUnit {get;set;}
    public int MatchNumber {get;set;}
    public BattlePhase Phase {get;set;}
    public double StrategyDelay {get;set;}
    public double NextWarAt {get;set;}
    public long Attacks {get;set;}
    public double NextEmergence {get;set;}
    public int LastDominant {get;set;}
    public double WaveDelay {get;set;}
    public double DripDelay {get;set;}
    public int WaveIndex {get;set;}
    public int PendingSoldiers {get;set;}
    public int? PendingColonyId {get;set;}
    public double AnnouncementAge {get;set;}
    public BattleAnnouncement? Announcement {get;set;}
    public BattleAnnouncement[] Announcements {get;set;}=[];
    public Dictionary<BugSpecies,int> Victories {get;set;}=[];
    public Dictionary<int,string> FactionNames {get;set;}=[];
    public ColonyEvent[] History {get;set;}=[];
    public ArcadeUnitState[] Units {get;set;}=[];
    public ArcadeColonyState[] Colonies {get;set;}=[];
    public ArcadeWebState[] Webs {get;set;}=[];
    public ArcadeProjectileState[] Projectiles {get;set;}=[];
}
internal sealed class ArcadeUnitState
{
    public bool InScene {get;set;}
    public int? TargetId {get;set;}
    public int? LastAttackerId {get;set;}
    public BugSpecies Species {get;set;}
    public int UnitId {get;set;}
    public int ConfirmedKills {get;set;}
    public int Direction {get;set;}
    public int Hits {get;set;}
    public int Meals {get;set;}
    public bool IsLeader {get;set;}
    public bool CanReproduce {get;set;}
    public string? Name {get;set;}
    public string? LegendaryTitle {get;set;}
    public bool CampaignDeployed {get;set;}
    public ArmyRole Role {get;set;}
    public CombatState Combat {get;set;}
    public BugState State {get;set;}
    public Point Position {get;set;}
    public Point Target {get;set;}
    public Point OrderPoint {get;set;}
    public double Health {get;set;}
    public double MaxHealth {get;set;}
    public double Protection {get;set;}
    public double AttackTimer {get;set;}
    public double AbilityTimer {get;set;}
    public double ThinkTimer {get;set;}
    public double SlowTimer {get;set;}
    public double StunTimer {get;set;}
    public double DefeatAge {get;set;}
    public double AttackFlash {get;set;}
    public double HealFlash {get;set;}
    public double RevengeTimer {get;set;}
    public double RetreatUntil {get;set;}
    public double NextRetreat {get;set;}
    public double Angle {get;set;}
    public double Speed {get;set;}
    public double Age {get;set;}
    public double CrawlPhase {get;set;}
    public double HitFlash {get;set;}
    public double WebImmunity {get;set;}
}
internal sealed class ArcadeColonyState
{
    public BugSpecies Species {get;set;}
    public int Id {get;set;}
    public string Name {get;set;}="";
    public Point Territory {get;set;}
    public Point SurvivorCenter {get;set;}
    public ArmyStrategy Strategy {get;set;}
    public int? TargetFactionId {get;set;}
    public Point CampaignRally {get;set;}
    public double StrategyUntil {get;set;}
    public double NextCampaign {get;set;}
    public double CampaignStarted {get;set;}
    public double Strength {get;set;}
    public bool MajorCampaign {get;set;}
    public int InitialForce {get;set;}
    public int CampaignKills {get;set;}
    public uint ColorArgb {get;set;}
    public Dictionary<int,int> KillsAgainst {get;set;}=[];
    public Dictionary<int,double> Rivalries {get;set;}=[];
    public int LeaderId {get;set;}
    public bool Eliminated {get;set;}
    public bool Regrouping {get;set;}
    public int Alive {get;set;}
    public int Kills {get;set;}
    public int Recoveries {get;set;}
    public double Resources {get;set;}
    public double RecoveryTime {get;set;}
    public double SettlementLostAt {get;set;}
    public double DefeatTime {get;set;}
}
internal sealed class ArcadeWebState
{
    public int WebId {get;set;}
    public int FactionId {get;set;}
    public int GeometrySeed {get;set;}
    public double TotalWork {get;set;}
    public WebStrand[] Plan {get;set;}=[];
    public int[] LinkedWebs {get;set;}=[];
    public Point Position {get;set;}
    public int? OwnerId {get;set;}
    public double Age {get;set;}
    public double Construction {get;set;}
    public int Rings {get;set;}
    public double BuildWork {get;set;}
    public WebStrand[] Strands {get;set;}=[];
    public int[] Contributors {get;set;}=[];
}
internal sealed class ArcadeProjectileState
{
    public Point Position {get;set;}
    public Point Previous {get;set;}
    public BugSpecies Species {get;set;}
    public int TargetId {get;set;}
    public int? AttackerId {get;set;}
    public double Age {get;set;}
}
