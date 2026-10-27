using System.Windows;
namespace SessionDashboard;
internal sealed record TerritoryRegion
{
    public int Id {get;set;}
    public int OwnerId {get;set;}
    public int? ChallengerId {get;set;}
    public double Occupation {get;set;}
    public bool Contested {get;set;}
}
internal sealed record ColonySettlement
{
    public int Id {get;set;}
    public int FounderId {get;set;}
    public int OwnerId {get;set;}
    public Point Position {get;set;}
    public double LostAt {get;set;}
}
internal sealed partial class BugManager
{
    internal const int TerritoryColumns=12,TerritoryRows=8;
    internal readonly List<TerritoryRegion> Territories=[];
    internal readonly List<ColonySettlement> Settlements=[];
    internal readonly Dictionary<int,uint> FactionColors=[];
    internal readonly Dictionary<int,BugSpecies> FactionSpecies=[];
    internal int TerritoryRevision;
    private int nextSettlement;
    private double territoryDelay;
    private readonly int[,] regionUnits=new int[TerritoryColumns*TerritoryRows,6];
    private readonly double[,] regionStrength=new double[TerritoryColumns*TerritoryRows,6];
    internal Rect RegionBounds(int id)
    {var r=BattleArena;return new Rect(r.Left+id%TerritoryColumns*r.Width/TerritoryColumns,r.Top+id/TerritoryColumns*r.Height/TerritoryRows,r.Width/TerritoryColumns,r.Height/TerritoryRows);}
    internal int RegionAt(Point point)=>Math.Clamp((int)((point.Y-BattleArena.Top)/BattleArena.Height*TerritoryRows),0,TerritoryRows-1)*TerritoryColumns+Math.Clamp((int)((point.X-BattleArena.Left)/BattleArena.Width*TerritoryColumns),0,TerritoryColumns-1);
    internal int TerritoryOwnerAt(Point point)=>Territories.Count==0 ? 0 : Territories[RegionAt(point)].OwnerId;
    internal string FactionName(int id)=>colonyOrder.FirstOrDefault(c=>c.Id==id)?.Name??factionNames.GetValueOrDefault(id,"Unclaimed");
    private bool OwnsSettlement(BugColony colony)=>!IsInfinite || Settlements.Any(n=>n.OwnerId==colony.Id);
    private bool AdjacentOwned(int region,int owner)
    {
        var x=region%TerritoryColumns;var y=region/TerritoryColumns;
        return (x>0 && Territories[region-1].OwnerId==owner) || (x<TerritoryColumns-1 && Territories[region+1].OwnerId==owner) || (y>0 && Territories[region-TerritoryColumns].OwnerId==owner) || (y<TerritoryRows-1 && Territories[region+TerritoryColumns].OwnerId==owner);
    }
    private bool CanFoundAt(Point point)=> (TerritoryOwnerAt(point)==0 || Faction(TerritoryOwnerAt(point))==null || colonyOrder.Count(c=>!c.Eliminated)<=2) && Settlements.All(n=>(n.Position-point).Length>170);
    private void InitializeTerritories()
    {
        Territories.Clear();Settlements.Clear();nextSettlement=0;territoryDelay=0;
        for(var id=0;id<TerritoryColumns*TerritoryRows;id++)Territories.Add(new(){Id=id});
        foreach(var c in colonyOrder)ClaimHomeland(c);
        TerritoryRevision++;
    }
    private void ClaimHomeland(BugColony colony,bool recordChange=false)
    {
        foreach(var region in Territories)
        {
            var center=RegionBounds(region.Id);var point=new Point(center.X+center.Width/2,center.Y+center.Height/2);
            if((point-colony.Territory).Length<170 && (region.OwnerId==0 || Faction(region.OwnerId)==null))region.OwnerId=colony.Id;
        }
        var home=Territories[RegionAt(colony.Territory)];var previous=home.OwnerId;home.OwnerId=colony.Id;
        if(recordChange && previous>0 && previous!=colony.Id)
        {Record(ColonyEventType.TerritoryCaptured,colony,otherId:previous,regionId:home.Id);var old=colonyOrder.FirstOrDefault(c=>c.Id==previous);if(old!=null)Record(ColonyEventType.TerritoryLost,old,colony,regionId:home.Id);}
        CreateSettlement(colony,colony.Territory,false);TerritoryRevision++;
    }
    private bool CreateSettlement(BugColony c,Point point,bool log=true)
    {
        if(Settlements.Count>=24)
        {
            var abandoned=Settlements.FirstOrDefault(n=>!combatUnits.Any(b=>Alive(b) && (b.Position-n.Position).Length<100) && (Faction(n.OwnerId)==null || Settlements.Count(other=>other.OwnerId==n.OwnerId)>1));
            if(abandoned==null)return false;
            var owner=colonyOrder.FirstOrDefault(x=>x.Id==abandoned.OwnerId);
            if(owner!=null)Record(ColonyEventType.OutpostAbandoned,owner);
            Settlements.Remove(abandoned);
        }
        Settlements.Add(new() {Id=++nextSettlement,FounderId=c.Id,OwnerId=c.Id,Position=point});
        Nations?.Settlement(this,Settlements[^1]);
        if(log)Record(ColonyEventType.SettlementFounded,c);TerritoryRevision++;return true;
    }
    private Point MilitaryObjective(BugColony c)=>!Alive(c.Leader) && c.Alive<4 ? c.Objective : Settlements.FirstOrDefault(n=>n.OwnerId==c.Id)?.Position??c.Objective;
    private void AdvanceTerritories(double dt)
    {
        if(Territories.Count==0)InitializeTerritories();
        territoryDelay-=dt;if(territoryDelay>0)return;territoryDelay+=1;
        Array.Clear(regionUnits);Array.Clear(regionStrength);
        foreach(var b in combatUnits)
        {
            if(!Alive(b))continue;var region=RegionAt(b.Position);var column=colonyOrder.IndexOf(Colonies[b.Species]);
            regionUnits[region,column]++;regionStrength[region,column]+=b.IsLeader ? 1.8 : .5+.5*b.Health/b.MaxHealth;
        }
        foreach(var region in Territories)
        {
            var ownerIndex=colonyOrder.FindIndex(c=>c.Id==region.OwnerId);var defenders=ownerIndex>=0 ? regionStrength[region.Id,ownerIndex] : 0;
            var challenger=-1;var strength=0d;var present=0;
            for(var c=0;c<colonyOrder.Count;c++)
            {
                if(regionUnits[region.Id,c]>0)present++;
                if(colonyOrder[c].Id==region.OwnerId || colonyOrder[c].Eliminated || regionStrength[region.Id,c]<=strength)continue;
                challenger=c;strength=regionStrength[region.Id,c];
            }
            var contested=present>1;if(contested!=region.Contested) {region.Contested=contested;TerritoryRevision++;}
            var isNest=Settlements.Any(n=>RegionAt(n.Position)==region.Id);
            if(challenger<0 || regionUnits[region.Id,challenger]<(isNest ? 4 : 3) || strength<=defenders*1.3)
            {region.Occupation=Math.Max(0,region.Occupation-2);if(region.Occupation==0)region.ChallengerId=null;continue;}
            var attacker=colonyOrder[challenger];
            if(!AdjacentOwned(region.Id,attacker.Id) && !(Offensive(attacker.Strategy) && attacker.TargetFactionId==region.OwnerId))continue;
            if(region.ChallengerId!=attacker.Id) {region.ChallengerId=attacker.Id;region.Occupation=0;}
            region.Occupation++;
            if(region.Occupation<(isNest ? 20 : 10))continue;
            CaptureRegion(region,attacker);
        }
        foreach(var c in colonyOrder.Where(c=>!c.Eliminated && Alive(c.Leader)))
        {
            if(OwnsSettlement(c)) {c.Resources=Math.Min(90,c.Resources+Territories.Count(r=>r.OwnerId==c.Id)*.01);continue;}
            if(c.Resources<35 || TerritoryOwnerAt(c.Territory)!=c.Id || Settlements.Any(n=>(n.Position-c.Territory).Length<120))continue;
            var loss=c.SettlementLostAt;
            if(MatchAge-loss<25)continue;Neighbors(c.Territory,180);if(closeUnits.Any(b=>Alive(b) && b.Species!=c.Species))continue;
            if(CreateSettlement(c,c.Territory))c.Resources-=20;
        }
    }
    private void CaptureRegion(TerritoryRegion region,BugColony winner)
    {
        var previous=region.OwnerId;region.OwnerId=winner.Id;region.Occupation=0;region.ChallengerId=null;TerritoryRevision++;
        Nations?.Conquer(this,region.Id,previous,winner.Id);
        Record(ColonyEventType.TerritoryCaptured,winner,otherId:previous==0 ? null : previous,regionId:region.Id);
        var loser=colonyOrder.FirstOrDefault(c=>c.Id==previous);
        if(loser!=null)Record(ColonyEventType.TerritoryLost,loser,winner,regionId:region.Id);
        foreach(var nest in Settlements.Where(n=>RegionAt(n.Position)==region.Id && n.OwnerId!=winner.Id).ToArray())
        {
            var oldOwner=nest.OwnerId;nest.OwnerId=winner.Id;nest.LostAt=MatchAge;
            Record(nest.FounderId==winner.Id ? ColonyEventType.HomelandReclaimed : ColonyEventType.NestConquered,winner,otherId:oldOwner,regionId:region.Id,settlementId:nest.Id);
            if(loser!=null)
            {
                loser.SettlementLostAt=MatchAge;loser.Resources*=.7;loser.Strategy=ArmyStrategy.Retreating;loser.StrategyUntil=MatchAge+18;loser.TargetFactionId=null;
                var refuge=Settlements.FirstOrDefault(n=>n.OwnerId==loser.Id)?.Position;
                if(refuge==null)
                {
                    var land=Territories.Where(r=>r.OwnerId==loser.Id).OrderBy(r=>(RegionBounds(r.Id).TopLeft-nest.Position).LengthSquared).FirstOrDefault();
                    if(land!=null) {var bounds=RegionBounds(land.Id);refuge=new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2);}
                }
                var away=nest.Position-winner.Territory;if(away.Length<1)away=new Vector(1,0);away.Normalize();
                loser.Territory=ClampArena(refuge??nest.Position+away*230);
                foreach(var b in combatUnits)if(Alive(b) && b.Species==loser.Species) {b.RetreatUntil=MatchAge+8;b.ThinkTimer=0;}
            }
            if(Offensive(winner.Strategy) && winner.TargetFactionId==oldOwner)EndCampaign(winner,true);
        }
    }
}
