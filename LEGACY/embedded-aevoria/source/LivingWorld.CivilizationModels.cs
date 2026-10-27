using System.Text.Json;
using System.Windows;
namespace SessionDashboard;
internal enum SettlementStage { Outpost, Village, Town, City, MajorCapital }
internal enum CivicPurpose { Housing, Food, Workshop, Defense, Government, Infrastructure, Monument }
internal enum CivicEventKind { NationFounded, SettlementFounded, SettlementCaptured, SettlementAbandoned, SettlementUpgraded, CapitalEstablished, BuildingCompleted, BuildingDestroyed, RoyalAccession, RoyalDeath, OfficeVacant, Succession, DynastyFounded, DynastyEnded, TerritorialMilestone, FirstSharedBorder, SustainedProsperity, EconomicHardship, EconomicRecovery }
internal sealed record CivicFact(Guid Id,long Sequence,double Time,CivicEventKind Kind,Guid NationId,Guid? RulerId,Guid? SettlementId,string Detail);
internal sealed record RoyalPerson
{
    public Guid Id {get;init;}
    public int UnitId {get;init;}
    public Guid NationId {get;init;}
    public string CultureId {get;init;}="";
    public string Name {get;init;}="";
    public string Office {get;set;}="";
    public Guid DynastyId {get;init;}
    public double CreatedAt {get;init;}
    public double? AccededAt {get;set;}
    public double? DeathAt {get;set;}
    public string? DeathCause {get;set;}
    public Guid? PredecessorId {get;set;}
    public Guid? SuccessorId {get;set;}
    public string[] Achievements {get;set;}=[];
}
internal sealed record RoyalDynasty(Guid Id,Guid NationId,string Name,Guid FounderId,double FoundedAt)
{
    public double? EndedAt {get;set;}
    public Guid[] Rulers {get;set;}=[];
    // No biological genealogy is inferred from political succession.
    public int KnownGenerations {get;set;}=1;
}
internal sealed record CivicNation(Guid NationId,string ProfileId,string CultureId,string CivilizationId,string GovernmentId)
{
    public Guid? CurrentRulerId {get;set;}
    public Guid? DynastyId {get;set;}
    public Guid? CapitalSettlementId {get;set;}
    public bool OfficeVacant {get;set;}
}
internal sealed record CivicBuilding
{
    public Guid Id {get;init;}
    public Guid SettlementId {get;init;}
    public Guid OwnerId {get;set;}
    public string StyleId {get;init;}="";
    public string Archetype {get;init;}="";
    public CivicPurpose Purpose {get;init;}
    public Point Position {get;init;}
    public double Progress {get;set;}
    public double Condition {get;set;}=1;
    public bool Completed {get;set;}
    public bool Cancelled {get;set;}
    public double MaterialCost {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public long InvestmentCents {get;init;}
}
internal sealed record CivicSettlement
{
    public Guid Id {get;init;}
    public int? CoreSettlementId {get;init;}
    public string Name {get;init;}="";
    public Guid OwnerId {get;set;}
    public string StyleId {get;init;}="";
    public Point Position {get;init;}
    public SettlementStage Stage {get;set;}
    public int Population {get;set;}
    public int Workforce {get;set;}
    public double FoundedAt {get;init;}
    public double Food {get;set;}=12;
    public double Materials {get;set;}=18;
    public double Production {get;set;}
    public double DefensiveSupplies {get;set;}=4;
    public bool Active {get;set;}=true;
    public long VisualRevision {get;set;}
}
internal sealed record CivicTask(int UnitId,Guid SettlementId,string Work);
internal sealed record CivicSnapshot
{
    public int SchemaVersion {get;set;}=1;
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public CivicEconomyState? Economy {get;set;}
    public Guid WorldId {get;init;}
    public double LastTick {get;set;}
    public long Sequence {get;set;}
    public long VisualRevision {get;set;}
    public string? JournalHead {get;set;}
    public CivicNation[] Nations {get;set;}=[];
    public RoyalPerson[] Royals {get;set;}=[];
    public RoyalDynasty[] Dynasties {get;set;}=[];
    public CivicSettlement[] Settlements {get;set;}=[];
    public CivicBuilding[] Buildings {get;set;}=[];
    public CivicFact[] RecentEvents {get;set;}=[];
    public CivicTask[] Tasks {get;set;}=[];
}
internal sealed partial class BugManager
{
    internal LivingCivilization? Civilization;
    internal JsonElement? PendingCivilization;
}
