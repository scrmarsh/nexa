using System.Text.Json;
using System.Windows;
namespace SessionDashboard;
internal enum NationExistence { Active, Extinct }
internal enum OwnershipReason { Founding, Occupation, Conquest, Settlement, Abandonment, PoliticalTransfer, CompatibilityInitialization, PeacefulExpansion, Intervention }
internal sealed record LivingNation(Guid Id,string Name,string Species,uint Color,Point Capital,int? FactionId,double? FoundedAt,int? RulerUnitId=null,string? CivilizationId=null,string? GovernmentId=null,NationExistence Status=NationExistence.Active);
internal sealed record OwnershipFact(long Sequence,int Region,Guid? PreviousOwner,Guid? NewOwner,double Time,OwnershipReason Reason,int? SettlementId=null);
internal sealed record NationConflict(int Region,int Challenger);
internal sealed record LivingPoliticsSnapshot
{
    public int SchemaVersion {get;init;}=1;
    public Guid WorldId {get;init;}
    public int Columns {get;init;}
    public int Rows {get;init;}
    public int CellTiles {get;init;}=8;
    public LivingNation[] Nations {get;init;}=[];
    public int[] Owners {get;init;}=[];
    public NationConflict[] Conflicts {get;init;}=[];
    public long Revision {get;init;}
    public long Sequence {get;init;}
    public OwnershipFact[] RecentChanges {get;init;}=[];
    public double NextSyncAt {get;init;}
    public string? Diagnostic {get;init;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public KingdomExpansionState? Expansion {get;init;}
}
internal static class LivingSpecies
{
    internal static readonly IReadOnlyDictionary<string,(BugSpecies Bug,string Letter,string Title)> Map=new Dictionary<string,(BugSpecies,string,string)>
    {
        ["Ants"]=(BugSpecies.Ants,"A","Ant Dominion"),["Spiders"]=(BugSpecies.Spiders,"S","Spider League"),["Ladybugs"]=(BugSpecies.Ladybugs,"L","Ladybug Kingdom"),
        ["Roaches"]=(BugSpecies.Roaches,"R","Roach Confederacy"),["Centipedes"]=(BugSpecies.Centipedes,"C","Centipede Realm"),["Flies"]=(BugSpecies.Flies,"F","Fly Commonwealth")
    };
    internal static string Letter(string species)=>Map.TryGetValue(species,out var info) ? info.Letter : "?";
}
internal sealed partial class BugManager
{
    internal LivingNationWorld? Nations;
    internal JsonElement? PendingPolitics;
    internal void PlacePoliticalHomes(LivingNationWorld politics)
    {
        foreach(var c in colonyOrder)
        {
            var nation=politics.Nations.FirstOrDefault(n=>n.FactionId==c.Id);if(nation==null)continue;
            c.Territory=c.SurvivorCenter=c.CampaignRally=nation.Capital;c.Leader.Position=c.Leader.OrderPoint=nation.Capital;c.Name=nation.Name;factionNames[c.Id]=nation.Name;
        }
        InitializeTerritories();
    }
}
