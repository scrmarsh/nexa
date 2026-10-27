using System.Diagnostics;
using System.Numerics;
using System.Windows;
namespace SessionDashboard;
internal sealed class KingdomExpansion
{
    private readonly LivingNationWorld world;
    internal KingdomExpansionState State {get;}
    private sealed class Frontier
    {
        internal readonly SortedSet<(double Score,int Cell)> Queue=[];
        internal readonly Dictionary<int,double> Scores=[];
        internal CivicSettlement[] Sources=[];
        internal string Signature="";
        internal readonly HashSet<int> Reachable=[];
        internal bool ReachDirty=true;
        internal int Cells;
    }
    private readonly Dictionary<Guid,Frontier> frontiers=[];
    private readonly byte[] costs;
    private readonly byte[] connected;
    private readonly double scale;
    internal long Evaluations {get;private set;}
    internal long Claims {get;private set;}
    internal double LastMilliseconds {get;private set;}
    internal double MaximumMilliseconds {get;private set;}
    internal string? CapacityWarning {get;private set;}
    // Conservative collection estimate; excludes shared topology/ownership and CLR/GC overhead.
    internal long RegionBytes=>(long)costs.Length*2+frontiers.Values.Sum(f=>(long)f.Scores.Count*112+(long)f.Reachable.Count*24);
    internal KingdomExpansion(LivingNationWorld world,KingdomExpansionState state)
    {
        this.world=world;State=state;costs=new byte[world.Land.Count];connected=new byte[costs.Length];scale=Math.Pow(world.Land.Info.Tiles/1024d,.65);
        foreach(var n in world.Nations)frontiers[n.Id]=new();
        // One load-time pass. All later frontier maintenance is local to a changed cell.
        for(var cell=0;cell<world.Land.Count;cell++)if(world.Owner(cell) is {} id)frontiers[id].Cells++;
        world.OwnershipChanged+=Changed;
    }
    internal double TerrainCost(int cell)
    {
        if(cell<0 || cell>=costs.Length)return double.PositiveInfinity;
        if(costs[cell]!=0)return costs[cell]/10d;
        var land=world.Land;var p=land.Center(cell);var biome=land.Terrain.Sample((int)(p.X/6),(int)(p.Y/6)).Biome;
        return (costs[cell]=(byte)(biome switch{TerrainBiome.Mountain=>38,TerrainBiome.Snow=>32,TerrainBiome.Hills=>22,TerrainBiome.Forest=>17,TerrainBiome.River=>19,TerrainBiome.Desert=>21,_=>10}))/10d;
    }
    // A mixed coastal cell cannot provide a hidden bridge across a sea channel.
    internal bool ConnectedCell(int cell)
    {
        if(cell<0 || cell>=connected.Length || world.Land.Land[cell]==0)return false;
        if(connected[cell]!=0)return connected[cell]==1;
        var result=SingleComponent(world.Land.Land[cell]);connected[cell]=(byte)(result?1:2);return result;
    }
    internal static bool SingleComponent(ulong mask)
    {
        if(mask==0)return false;var reached=1UL<<BitOperations.TrailingZeroCount(mask);
        const ulong left=0x0101010101010101,right=0x8080808080808080;
        while(true){var next=reached|(((reached&~left)>>1)|((reached&~right)<<1)|(reached>>8)|(reached<<8))&mask;if(next==reached)break;reached=next;}
        return reached==mask;
    }
    private double SupportScore(Guid nation,int cell,Frontier f)
    {
        if(f.Sources.Length==0)return double.PositiveInfinity;
        var point=world.Land.Center(cell);var distance=f.Sources.Min(s=>(point-s.Position).Length/(48*scale*(1+(int)s.Stage*.35+(world.Find(nation)?.Capital==s.Position ? .25 : 0))));
        var same=world.Land.Neighbors(cell).Count(n=>world.Owner(n)==nation);
        return distance+TerrainCost(cell)*State.Tuning.TerrainWeight-same*State.Tuning.CompactnessWeight;
    }
    private void Refresh(Guid id,int cell,Frontier f)
    {
        if(f.Scores.Remove(cell,out var old))f.Queue.Remove((old,cell));
        if(world.Owner(cell)!=null || !ConnectedCell(cell) || !world.Land.Neighbors(cell).Any(n=>world.Owner(n)==id && f.Reachable.Contains(n) && ConnectedCell(n)))return;
        var score=SupportScore(id,cell,f);f.Scores[cell]=score;f.Queue.Add((score,cell));
    }
    private void Changed(OwnershipFact fact)
    {
        if(fact.PreviousOwner is {} old && frontiers.TryGetValue(old,out var previous)){previous.Cells--;previous.ReachDirty=true;previous.Reachable.Remove(fact.Region);}
        if(fact.NewOwner is {} next){if(!frontiers.TryGetValue(next,out var f))frontiers[next]=f=new();f.Cells++;if(world.Land.Neighbors(fact.Region).Any(f.Reachable.Contains))f.Reachable.Add(fact.Region);else f.ReachDirty=true;}
        var adjacent=world.Land.Neighbors(fact.Region).ToArray();
        var affected=adjacent.Select(world.Owner).Append(fact.PreviousOwner).Append(fact.NewOwner).Where(id=>id!=null).Select(id=>id!.Value).Distinct();
        foreach(var id in affected)
        {if(!frontiers.TryGetValue(id,out var f))continue;Refresh(id,fact.Region,f);foreach(var neighbor in adjacent)Refresh(id,neighbor,f);}
    }
    internal double Influence(Guid nation,int cell,LivingCivilization civic)
    {
        var p=world.Land.Center(cell);
        return civic.State.Settlements.Where(s=>s.Active && s.OwnerId==nation && s.Population>0 && s.Food>0 && world.OwnerAt(s.Position)==nation)
            .Select(s=>Math.Max(0,1-(p-s.Position).Length/Reach(s,civic))*(1+(int)s.Stage*.4)).DefaultIfEmpty(0).Max();
    }
    internal double AdministrativeReach(CivicSettlement s,LivingCivilization civic)=>Reach(s,civic);
    private double Reach(CivicSettlement s,LivingCivilization civic)=>48*scale*(22+s.Population*2+(int)s.Stage*16+civic.Completed(s,CivicPurpose.Infrastructure)*12+civic.Completed(s,CivicPurpose.Government)*16+(world.Find(s.OwnerId)?.Capital==s.Position?12:0));
    private int Capacity(Frontier f,LivingCivilization civic)=>(int)(scale*scale*f.Sources.Sum(s=>(180+s.Population*85+(int)s.Stage*450+civic.Completed(s,CivicPurpose.Infrastructure)*400+civic.Completed(s,CivicPurpose.Government)*500)*Math.Min(1,s.Food/4)));
    private void Sources(Guid id,Frontier f,LivingCivilization civic)
    {
        var sources=civic.State.Settlements.Where(s=>s.Active && s.OwnerId==id && s.Population>0 && s.Food>0 && world.OwnerAt(s.Position)==id).OrderBy(s=>s.Id).ToArray();
        var signature=world.Find(id)?.Capital.ToString(System.Globalization.CultureInfo.InvariantCulture)+"/"+string.Join(";",sources.Select(s=>$"{s.Id}:{s.Position.X:R}:{s.Position.Y:R}:{s.Stage}:{civic.Completed(s,CivicPurpose.Infrastructure)}:{civic.Completed(s,CivicPurpose.Government)}"));
        f.Sources=sources;if(signature==f.Signature && !f.ReachDirty)return;f.Signature=signature;f.ReachDirty=false;f.Queue.Clear();f.Scores.Clear();f.Reachable.Clear();
        var queue=new Queue<int>();foreach(var source in sources){var cell=world.Land.At(source.Position);if(ConnectedCell(cell) && f.Reachable.Add(cell))queue.Enqueue(cell);}
        while(queue.TryDequeue(out var cell))foreach(var n in world.Land.Neighbors(cell))if(world.Owner(n)==id && ConnectedCell(n) && f.Reachable.Add(n))queue.Enqueue(n);
        foreach(var owned in f.Reachable)foreach(var n in world.Land.Neighbors(owned))Refresh(id,n,f);
    }
    internal void Tick(BugManager manager,LivingCivilization civic)
    {
        if(manager.WorldPaused || manager.MatchAge-State.LastTick<State.Tuning.Interval)return;
        var began=Stopwatch.GetTimestamp();State.LastTick=manager.MatchAge;State.Turn++;
        var active=world.Nations.Where(n=>n.Status==NationExistence.Active).OrderBy(n=>n.FactionId??int.MaxValue).ThenBy(n=>n.Id).ToArray();
        // Rotating first mover prevents a fixed enumeration order owning every contested opening.
        for(var turn=0;turn<active.Length;turn++)
        {
            var nation=active[(turn+(int)(State.Turn%Math.Max(1,active.Length)))%active.Length];
            if(!frontiers.TryGetValue(nation.Id,out var f))frontiers[nation.Id]=f=new();Sources(nation.Id,f,civic);
            var capacity=Capacity(f,civic);var budget=Math.Min(State.Tuning.ClaimsPerNation,8+f.Sources.Sum(s=>s.Population)*2+f.Sources.Sum(s=>civic.Completed(s,CivicPurpose.Infrastructure)*6));
            var rejected=new List<(double,int)>();var claims=0;
            for(var checks=0;checks<State.Tuning.EvaluationsPerNation && claims<budget && f.Cells<capacity && f.Queue.Count>0;checks++)
            {
                var candidate=f.Queue.Min;f.Queue.Remove(candidate);f.Scores.Remove(candidate.Cell);Evaluations++;
                if(world.Owner(candidate.Cell)!=null || !ConnectedCell(candidate.Cell))continue;
                var p=world.Land.Center(candidate.Cell);var sponsor=f.Sources.Where(s=>s.Materials>=State.Tuning.MaterialsPerCell*TerrainCost(candidate.Cell) && s.Food>=1+State.Tuning.FoodPerCell && (p-s.Position).Length<=Reach(s,civic)).OrderBy(s=>(s.Position-p).LengthSquared).ThenBy(s=>s.Id).FirstOrDefault();
                if(sponsor==null){rejected.Add(candidate);continue;}
                var cost=State.Tuning.MaterialsPerCell*TerrainCost(candidate.Cell);
                if(world.Commit(candidate.Cell,null,nation.Id,manager.MatchAge,OwnershipReason.PeacefulExpansion))
                {sponsor.Materials-=cost;sponsor.Food-=State.Tuning.FoodPerCell;claims++;Claims++;}
            }
            foreach(var candidate in rejected)Refresh(nation.Id,candidate.Item2,f);
            FoundSupport(nation,f,civic,manager.MatchAge);
            var level=f.Cells>=8192?6:f.Cells>=4096?5:f.Cells>=1024?4:f.Cells>=256?3:f.Cells>=64?2:f.Cells>=16?1:0;
            var known=State.Milestones.FirstOrDefault(m=>m.NationId==nation.Id)?.Level??0;
            if(level>known){State.Milestones=State.Milestones.Where(m=>m.NationId!=nation.Id).Append(new(nation.Id,level)).ToArray();civic.TerritorialEvent(CivicEventKind.TerritorialMilestone,nation.Id,manager.MatchAge,$"Kingdom now controls {world.Area(nation.Id)} land tiles across {f.Cells} political regions.");}
        }
        foreach(var border in world.Frontiers.Relationships)
        {
            var pair=new KingdomBorderFact(border.A,border.B);if(State.KnownBorders.Contains(pair) || State.KnownBorders.Length>=4096)continue;
            State.KnownBorders=State.KnownBorders.Append(pair).ToArray();civic.TerritorialEvent(CivicEventKind.FirstSharedBorder,border.A,manager.MatchAge,$"First shared land frontier with {world.Find(border.B)?.Name} ({border.Length:F0} world units).");
        }
        LastMilliseconds=Stopwatch.GetElapsedTime(began).TotalMilliseconds;MaximumMilliseconds=Math.Max(MaximumMilliseconds,LastMilliseconds);
    }
    private void FoundSupport(LivingNation nation,Frontier f,LivingCivilization civic,double time)
    {
        if(f.Sources.Length==0 || f.Cells<150*scale || f.Sources.Length>=3)return;
        if(civic.State.Settlements.Length>=128){CapacityWarning="Civic settlement capacity (128 records) reached; expansion uses remaining support.";return;}
        var polity=civic.State.Nations.FirstOrDefault(n=>n.NationId==nation.Id);var capital=f.Sources.FirstOrDefault(s=>s.Id==polity?.CapitalSettlementId);
        if(polity==null || capital==null || capital.Population<10 || capital.Materials<36 || capital.Food<8 || capital.Production<8)return;
        // Only inspect a bounded sample of actual frontier-adjacent owned regions.
        var candidate=f.Scores.Keys.Order().Take(128).SelectMany(world.Land.Neighbors).Distinct().Where(c=>world.Owner(c)==nation.Id && ConnectedCell(c)).OrderByDescending(c=>f.Sources.Min(s=>(s.Position-world.Land.Center(c)).LengthSquared)).FirstOrDefault(-1);
        if(candidate<0)return;var p=world.Land.Center(candidate);if(f.Sources.Any(s=>(s.Position-p).Length<48*scale*10))return;
        if(civic.Found(polity,p,time) is {} founded){capital.Materials-=12;capital.Food-=3;capital.Production-=8;founded.Food=3;founded.Materials=0;}
    }
}
