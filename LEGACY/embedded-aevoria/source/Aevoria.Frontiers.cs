using System.Diagnostics;
using System.Numerics;
using System.Windows;
namespace SessionDashboard;
internal sealed record PoliticalEdge(int CellA,int CellB,double Length,Point Location,double Difficulty);
internal sealed record PoliticalFrontier(Guid A,Guid B,PoliticalEdge[] Edges)
{
    internal double Length=>Edges.Sum(e=>e.Length);
    internal Point Location=>new(Edges.Average(e=>e.Location.X),Edges.Average(e=>e.Location.Y));
}
internal sealed record BorderSettlement(Guid SettlementId,Guid NationId,Guid NeighborId,double Distance);
internal sealed class PoliticalFrontiers
{
    private readonly LivingNationWorld world;
    private readonly Dictionary<(int,int),(Guid A,Guid B,PoliticalEdge Edge)> edges=[];
    private PoliticalFrontier[] relationships=[];
    private bool dirty=true;
    internal long Revision {get;private set;}
    internal int Updates {get;private set;}
    internal double UpdateMilliseconds {get;private set;}
    internal PoliticalFrontiers(LivingNationWorld world)
    {
        this.world=world;
        for(var cell=0;cell<world.Land.Count;cell++)if(world.Owner(cell)!=null)foreach(var n in world.Land.Neighbors(cell))if(n>cell)Update(cell,n);
        world.OwnershipChanged+=fact=>{var began=Stopwatch.GetTimestamp();foreach(var n in world.Land.Neighbors(fact.Region))Update(fact.Region,n);Revision++;Updates++;UpdateMilliseconds+=Stopwatch.GetElapsedTime(began).TotalMilliseconds;};
    }
    private void Update(int x,int y)
    {
        var key=(Math.Min(x,y),Math.Max(x,y));edges.Remove(key);dirty=true;
        if(world.Owner(x) is not {} a || world.Owner(y) is not {} b || a==b)return;
        var land=world.Land;ulong overlap;
        if(key.Item2==key.Item1+1)
        {overlap=0;for(var row=0;row<8;row++)if((land.Land[key.Item1]&(1UL<<(row*8+7)))!=0 && (land.Land[key.Item2]&(1UL<<(row*8)))!=0)overlap|=1UL<<row;}
        else overlap=(land.Land[key.Item1]>>56)&land.Land[key.Item2]&255;
        if(overlap==0)return;if(a.CompareTo(b)>0)(a,b)=(b,a);
        var middle=(Point)(((System.Windows.Vector)land.Center(x)+(System.Windows.Vector)land.Center(y))/2);
        var difficulty=world.Expansion is {} expansion?(expansion.TerrainCost(x)+expansion.TerrainCost(y))/2:1;
        edges[key]=(a,b,new(key.Item1,key.Item2,BitOperations.PopCount(overlap)*land.Info.TileSize,middle,difficulty));
    }
    internal IReadOnlyList<PoliticalFrontier> Relationships
    {
        get{if(dirty){relationships=edges.Values.GroupBy(e=>(e.A,e.B)).OrderBy(g=>g.Key.A).ThenBy(g=>g.Key.B).Select(g=>new PoliticalFrontier(g.Key.A,g.Key.B,g.Select(e=>e.Edge).OrderBy(e=>e.CellA).ThenBy(e=>e.CellB).ToArray())).ToArray();dirty=false;}return relationships;}
    }
    internal IEnumerable<Guid> Neighbors(Guid nation)=>Relationships.Where(r=>r.A==nation || r.B==nation).Select(r=>r.A==nation?r.B:r.A);
    internal BorderSettlement[] Settlements(LivingCivilization civic,double distance=288)=>Relationships.SelectMany(r=>civic.State.Settlements.Where(s=>s.Active && (s.OwnerId==r.A || s.OwnerId==r.B)).Select(s=>new BorderSettlement(s.Id,s.OwnerId,s.OwnerId==r.A?r.B:r.A,r.Edges.Min(e=>(e.Location-s.Position).Length))).Where(s=>s.Distance<=distance)).ToArray();
    internal bool Disputed(PoliticalFrontier border)=>border.Edges.Any(e=>world.IsContested(e.CellA) || world.IsContested(e.CellB));
    internal string Status(PoliticalFrontier border,BugManager manager)
    {var a=world.Find(border.A)?.FactionId;var b=world.Find(border.B)?.FactionId;return Disputed(border)?"Disputed":manager.Colonies.Values.Any(c=>c.Id==a && c.TargetFactionId==b || c.Id==b && c.TargetFactionId==a)?"Military target":"No diplomatic status recorded";}
    internal CivicSettlement? ClosestSettlement(Guid nation,Guid neighbor,LivingCivilization civic)=>world.Find(nation) is {} polity ? civic.State.Settlements.Where(s=>s.Active && s.OwnerId==neighbor).OrderBy(s=>(s.Position-polity.Capital).LengthSquared).FirstOrDefault():null;
    internal int[] FriendlyRoute(Guid nation,int from,int to,int budget=8192)
    {
        if(budget is <1 or >65536 || world.Owner(from)!=nation || world.Owner(to)!=nation)return [];
        var previous=new Dictionary<int,int>{{from,-1}};var queue=new Queue<int>();queue.Enqueue(from);
        while(queue.TryDequeue(out var cell) && previous.Count<=budget)
        {if(cell==to){var result=new List<int>();for(var c=to;c>=0;c=previous[c])result.Add(c);result.Reverse();return result.ToArray();}foreach(var n in world.Land.Neighbors(cell))if(world.Owner(n)==nation && previous.TryAdd(n,cell))queue.Enqueue(n);}
        return []; // Bounded query; no route within this search budget, not proof of isolation.
    }
}
internal sealed partial class LivingNationWorld
{
    private PoliticalFrontiers? frontiers;
    internal PoliticalFrontiers Frontiers=>frontiers??=new(this);
    internal Point? FrontierFocus(Guid a,Guid b)=>Frontiers.Relationships.FirstOrDefault(r=>r.A==a && r.B==b || r.A==b && r.B==a)?.Location;
    internal (Guid? Owner,bool Land,bool Contested) InspectRegion(int cell)=>(Owner(cell),cell>=0 && cell<Land.Count && Land.Land[cell]!=0,cell>=0 && cell<Land.Count && IsContested(cell));
    // Future interventions have their own reason and retain normal thread/geography/event validation.
    internal bool ClaimRegion(int cell,Guid nation,double time)
    {Write();if(Find(nation) is not {Status:NationExistence.Active} || !double.IsFinite(time) || time<0 || Expansion is not {} expansion || cell<0 || cell>=Land.Count || Owner(cell)!=null || !expansion.ConnectedCell(cell) || !Land.Neighbors(cell).Any(n=>Owner(n)==nation && expansion.ConnectedCell(n)))return false;return Commit(cell,null,nation,time,OwnershipReason.Intervention);}
    internal bool TransferRegion(int cell,Guid expected,Guid next,double time)
    {Write();if(Find(next) is not {Status:NationExistence.Active} || !double.IsFinite(time) || time<0 || Owner(cell)!=expected)return false;return Commit(cell,expected,next,time,OwnershipReason.Intervention);}
    internal bool ReleaseRegion(int cell,Guid expected,double time)
    {Write();if(!double.IsFinite(time) || time<0 || Owner(cell)!=expected)return false;return Commit(cell,expected,null,time,OwnershipReason.Intervention);}
}
