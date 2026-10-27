using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
namespace SessionDashboard;
internal sealed record NationComponent(int Key,Point Anchor,Rect Bounds,int[] Tiles,int[]? Cells=null,int AreaTiles=0)
{
    internal bool ContainsTile(int tile,int width,int columns)=>Cells is {} cells ? Array.BinarySearch(cells,tile/width/8*columns+tile%width/8)>=0 : Array.BinarySearch(Tiles,tile)>=0;
}
internal sealed partial class LivingNationWorld
{
    internal readonly PoliticalLand Land;
    internal Guid WorldId {get;}
    private readonly List<LivingNation> nations=[];
    private readonly int[] owners;
    private readonly Dictionary<int,int> conflicts=[];
    private readonly List<OwnershipFact> changes=[];
    private readonly Dictionary<Guid,(long Revision,List<NationComponent> Components)> components=[];
    private readonly Dictionary<Guid,long> nationRevisions=[];
    private readonly Dictionary<Guid,int> areas=[];
    private int writer=Environment.CurrentManagedThreadId;
    internal readonly long[] CellRevisions;
    internal IReadOnlyList<LivingNation> Nations=>nations;
    internal IReadOnlyList<OwnershipFact> RecentChanges=>changes;
    internal long Revision {get;private set;}
    internal long Sequence {get;private set;}
    internal int AnalysisCount {get;private set;}
    internal double AnalysisMilliseconds {get;private set;}
    internal string? Diagnostic {get;private set;}
    internal event Action<OwnershipFact>? OwnershipChanged;
    internal LivingNationWorld(PoliticalLand land,Guid worldId) {Land=land;WorldId=worldId;owners=new int[land.Count];CellRevisions=new long[land.Count];}
    internal void BindWriter()=>writer=Environment.CurrentManagedThreadId;
    private void Write() {if(writer!=Environment.CurrentManagedThreadId)throw new InvalidOperationException("Only the owning simulation thread may change political state.");}
    internal LivingNation? Find(Guid id)=>nations.FirstOrDefault(n=>n.Id==id);
    internal Guid? Owner(int region)=>region<0 || region>=owners.Length || owners[region]==0 ? null : nations[owners[region]-1].Id;
    internal Guid? OwnerAt(Point point)=>Land.IsLand(point) ? Owner(Land.At(point)) : null;
    internal int[] Regions(Guid id)=>Enumerable.Range(0,owners.Length).Where(i=>Owner(i)==id).ToArray();
    internal int Area(Guid id)=>areas.GetValueOrDefault(id);
    internal bool IsContested(int region)=>conflicts.ContainsKey(region);
    internal static Guid Identity(Guid world,int faction)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(world.ToString("N")+"/nation/"+faction))[..16]);
    private static uint Color(int index)
    {
        uint[] palette=[0xff56b4e9,0xffe69f00,0xff009e73,0xffcc79a7,0xfff0e442,0xffd55e00,0xffb69aff,0xff8cd4b8];
        return palette[index%palette.Length]^(uint)((index/palette.Length*37)&63);
    }
    internal LivingNation AddNation(LivingNation nation)
    {
        Write();if(nations.Count>=512 || nation.Id==Guid.Empty || Find(nation.Id)!=null || !ValidNation(nation,Land))throw new InvalidDataException("Invalid or duplicate nation.");
        if(nations.Any(n=>n.Color==nation.Color))throw new InvalidDataException("Nation colors must remain distinguishable.");
        nations.Add(nation);areas[nation.Id]=0;nationRevisions[nation.Id]=0;Revision++;return nation;
    }
    internal void Rename(Guid id,string name)
    {Write();if(string.IsNullOrWhiteSpace(name) || name.Length>100)throw new ArgumentException("Invalid nation name.");var i=nations.FindIndex(n=>n.Id==id);if(i<0)throw new ArgumentException("Unknown nation.");nations[i]=nations[i] with{Name=name.Trim()};Revision++;}
    internal void AssignCulture(Guid id,string civilization,string government)
    {Write();var i=nations.FindIndex(n=>n.Id==id);if(i<0 || !HistoricalAtlas.Default.Entities.ContainsKey(civilization))throw new ArgumentException("Unknown nation/civilization.");nations[i]=nations[i] with{CivilizationId=civilization,GovernmentId=government};Revision++;}
    internal void RelocateCapital(Guid id,Point point)
    {Write();var i=nations.FindIndex(n=>n.Id==id);if(i<0 || OwnerAt(point)!=id || !Land.IsCapital(point))throw new ArgumentException("Capital must be owned dry land.");nations[i]=nations[i] with{Capital=point};Revision++;}
    internal bool Commit(int region,Guid? previous,Guid? next,double time,OwnershipReason reason,int? settlement=null)
    {
        Write();if(region<0 || region>=owners.Length || Land.Land[region]==0 || Owner(region)!=previous || !double.IsFinite(time) || time<0 || !Enum.IsDefined(reason) || next is {} id && Find(id) is not {Status:NationExistence.Active})throw new InvalidDataException("Invalid political ownership event.");
        if(previous==next)return false;
        if(reason==OwnershipReason.Founding && previous!=null)throw new InvalidDataException("Founding cannot overwrite a claim.");
        var index=next==null ? 0 : nations.FindIndex(n=>n.Id==next)+1;owners[region]=index;conflicts.Remove(region);Revision++;
        if(previous is {} old) {areas[old]-=Land.Area(region);nationRevisions[old]=Revision;}
        if(next is {} target) {areas[target]+=Land.Area(region);nationRevisions[target]=Revision;}
        CellRevisions[region]=Revision;
        // The current snapshot keeps recent facts; a permanent History Book is a separate milestone.
        var fact=new OwnershipFact(++Sequence,region,previous,next,time,reason,settlement);changes.Add(fact);if(changes.Count>4096)changes.RemoveRange(0,256);
        OwnershipChanged?.Invoke(fact);return true;
    }
    internal void Contest(int region,Guid? challenger)
    {
        Write();if(region<0 || region>=owners.Length || Land.Land[region]==0)throw new InvalidDataException("Invalid contested region.");
        var index=challenger==null ? 0 : nations.FindIndex(n=>n.Id==challenger)+1;if(challenger!=null && (index==0 || challenger==Owner(region)))throw new InvalidDataException("Invalid challenger.");
        if(conflicts.GetValueOrDefault(region)==index)return;if(index==0)conflicts.Remove(region);else conflicts[region]=index;CellRevisions[region]=++Revision;
    }
    private static bool ValidNation(LivingNation n,PoliticalLand land)=>n!=null && n.Id!=Guid.Empty && !string.IsNullOrWhiteSpace(n.Name) && n.Name.Length<=100 && !string.IsNullOrWhiteSpace(n.Species) && n.Species.Length<=50 && (n.Color>>24)==255 && (n.Color&0xffffff)!=0 && land.IsCapital(n.Capital) && (n.FoundedAt==null || double.IsFinite(n.FoundedAt.Value) && n.FoundedAt>=0) && Enum.IsDefined(n.Status) && (n.FactionId==null || n.FactionId>0) && (n.RulerUnitId==null || n.RulerUnitId>0) && (n.CivilizationId==null || HistoricalAtlas.Default.Entities.ContainsKey(n.CivilizationId));
    internal LivingPoliticsSnapshot Capture()
    {
        Write();return new(){SchemaVersion=Expansion==null?1:2,WorldId=WorldId,Columns=Land.Columns,Rows=Land.Rows,Nations=nations.ToArray(),Owners=owners.ToArray(),Conflicts=conflicts.OrderBy(p=>p.Key).Select(p=>new NationConflict(p.Key,p.Value)).ToArray(),Revision=Revision,Sequence=Sequence,RecentChanges=changes.ToArray(),NextSyncAt=nextSync,Diagnostic=Diagnostic,Expansion=Expansion?.State};
    }
    internal JsonElement CaptureJson()=>JsonSerializer.SerializeToElement(Capture(),ArcadeWorldStore.JsonOptions);
    internal static LivingNationWorld Restore(PoliticalLand land,Guid id,JsonElement raw)
    {
        LivingPoliticsSnapshot s;try {s=raw.Deserialize<LivingPoliticsSnapshot>(ArcadeWorldStore.JsonOptions)??throw new InvalidDataException("Empty politics.");}catch(JsonException e){throw new InvalidDataException("Malformed optional territory data.",e);}
        if(!(s.SchemaVersion==1 && s.Expansion==null || s.SchemaVersion==2 && s.Expansion!=null) || s.WorldId!=id || s.CellTiles!=8 || s.Columns!=land.Columns || s.Rows!=land.Rows || s.Nations==null || s.Nations.Length>512 || s.Owners==null || s.Owners.Length!=land.Count || s.Conflicts==null || s.Conflicts.Length>land.Count || s.RecentChanges==null || s.RecentChanges.Length>4096 || s.Revision<0 || s.Sequence<0 || s.Revision<s.Sequence)throw new InvalidDataException("Unsupported or inconsistent optional territory data.");
        var result=new LivingNationWorld(land,id);
        if(!double.IsFinite(s.NextSyncAt) || s.NextSyncAt<0 || s.Diagnostic?.Length>300)throw new InvalidDataException("Invalid political clock or diagnostic.");result.nextSync=s.NextSyncAt;result.Diagnostic=s.Diagnostic;
        foreach(var n in s.Nations)result.AddNation(n);
        if(s.Nations.Where(n=>n.FactionId!=null).GroupBy(n=>n.FactionId).Any(g=>g.Count()>1))throw new InvalidDataException("Duplicate faction bindings.");
        for(var i=0;i<s.Owners.Length;i++)
        {var owner=s.Owners[i];if(owner<0 || owner>s.Nations.Length || owner>0 && land.Land[i]==0)throw new InvalidDataException("Invalid political owner.");result.owners[i]=owner;if(owner>0)result.areas[s.Nations[owner-1].Id]+=land.Area(i);}
        foreach(var c in s.Conflicts)
        {if(c==null || c.Region<0 || c.Region>=land.Count || land.Land[c.Region]==0 || c.Challenger<=0 || c.Challenger>s.Nations.Length || c.Challenger==s.Owners[c.Region] || !result.conflicts.TryAdd(c.Region,c.Challenger))throw new InvalidDataException("Invalid conflict data.");}
        long seq=0;foreach(var e in s.RecentChanges)
        {if(e==null || e.Sequence<=seq || e.Sequence>s.Sequence || e.Region<0 || e.Region>=land.Count || !double.IsFinite(e.Time) || e.Time<0 || !Enum.IsDefined(e.Reason) || e.PreviousOwner is {} before && result.Find(before)==null || e.NewOwner is {} after && result.Find(after)==null)throw new InvalidDataException("Invalid ownership facts.");seq=e.Sequence;}
        if(seq!=s.Sequence || s.RecentChanges.GroupBy(e=>e.Region).Any(g=>g.Last().NewOwner!=result.Owner(g.Key)))throw new InvalidDataException("Ownership facts do not match the saved map.");
        result.changes.AddRange(s.RecentChanges);result.Revision=s.Revision;result.Sequence=s.Sequence;Array.Fill(result.CellRevisions,s.Revision);if(s.Expansion!=null)result.EnableExpansion(s.Expansion);return result;
    }
    internal static LivingNationWorld Initialize(PoliticalLand land,BugManager manager,bool fresh,bool kingdoms=false)
    {
        var result=new LivingNationWorld(land,manager.WorldId);var available=Enumerable.Range(0,land.Count).Where(i=>System.Numerics.BitOperations.PopCount(land.Ink[i])>=32 && (!kingdoms || KingdomExpansion.SingleComponent(land.Land[i]))).ToArray();var chosen=new List<Point>();
        foreach(var c in manager.Colonies.Values.OrderBy(c=>c.Id))
        {
            var candidates=available.Where(i=>result.Owner(i)==null && land.Neighbors(i).Count()>=2).ToArray();
            if(candidates.Length==0){result.Diagnostic="Some founding nations could not be placed on navigable land.";if(fresh){c.Eliminated=true;c.Leader.Health=0;c.Alive=0;}continue;}
            int cell;
            if(!fresh)cell=candidates.MinBy(i=>(land.Center(i)-c.Territory).LengthSquared);
            else cell=candidates.MaxBy(i=>chosen.Count==0 ? -(land.Center(i)-c.Territory).LengthSquared : chosen.Min(p=>Math.Pow((p.X-land.Center(i).X)/land.Info.Extent,2)+Math.Pow((p.Y-land.Center(i).Y)/land.Info.HeightExtent,2)));
            var capital=!fresh && land.IsCapital(c.Territory) && result.Owner(land.At(c.Territory))==null ? c.Territory : land.Center(cell);cell=land.At(capital);chosen.Add(capital);
            var suffix=LivingSpecies.Map.GetValueOrDefault(c.Species.ToString()).Title??c.Species+" Realm";
            var adjectives=new[]{"Amber","Silver","Eastern","Verdant","Northern","Sunlit"};var name=fresh ? adjectives[BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(land.Info.Seed+c.Id)),0)%6]+" "+suffix : c.Name;
            var founded=manager.History.FirstOrDefault(e=>e.FactionId==c.Id && e.Type is ColonyEventType.Founded or ColonyEventType.Emerged)?.Timestamp;
            var nation=result.AddNation(new(Identity(manager.WorldId,c.Id),name,c.Species.ToString(),Color(result.nations.Count),capital,c.Id,fresh ? 0 : founded,c.Leader.UnitId,Status:c.Eliminated ? NationExistence.Extinct : NationExistence.Active));
            if(c.Eliminated || !fresh)continue;
            var queue=new Queue<int>();var seen=new HashSet<int>();queue.Enqueue(cell);var count=0;
            while(queue.TryDequeue(out var next) && count<9)
            {if(!seen.Add(next) || result.Owner(next)!=null || kingdoms && !KingdomExpansion.SingleComponent(land.Land[next]))continue;result.Commit(next,null,nation.Id,manager.MatchAge,OwnershipReason.Founding);count++;foreach(var n in land.Neighbors(next))queue.Enqueue(n);}
        }
        if(!fresh)
        {
            // Preserve the old committed coarse claims, clipped to terrain, without touching units.
            foreach(var cell in Enumerable.Range(0,land.Count).Where(i=>land.Land[i]!=0))
            {
                var coarse=manager.RegionAt(land.Center(cell));var old=manager.Territories.FirstOrDefault(r=>r.Id==coarse)?.OwnerId;
                var nation=result.nations.FirstOrDefault(n=>n.FactionId==old && n.Status==NationExistence.Active);
                if(nation!=null)result.Commit(cell,null,nation.Id,manager.MatchAge,OwnershipReason.CompatibilityInitialization);
            }
            foreach(var n in result.nations.Where(n=>n.Status==NationExistence.Active))
            {var cell=land.At(n.Capital);if(result.Owner(cell)==null)result.Commit(cell,null,n.Id,manager.MatchAge,OwnershipReason.CompatibilityInitialization);}
        }
        return result;
    }
    internal List<NationComponent> Components(Guid id)
    {
        if(Expansion!=null)return RegionalComponents(id);
        var revision=nationRevisions.GetValueOrDefault(id);if(components.TryGetValue(id,out var cached) && cached.Revision==revision)return cached.Components;
        var began=System.Diagnostics.Stopwatch.GetTimestamp();AnalysisCount++;var remaining=new HashSet<int>();var width=Land.Info.Tiles;
        foreach(var cell in Regions(id))for(var b=0;b<64;b++)if((Land.Land[cell]&(1UL<<b))!=0)remaining.Add((cell/Land.Columns*8+b/8)*width+cell%Land.Columns*8+b%8);
        var result=new List<NationComponent>();var queue=new Queue<int>();
        IEnumerable<int> Adj(int p) {var x=p%width;var y=p/width;if(x>0)yield return p-1;if(x<width-1)yield return p+1;if(y>0)yield return p-width;if(y<Land.Info.HeightTiles-1)yield return p+width;}
        while(remaining.Count>0)
        {
            var first=remaining.First();remaining.Remove(first);queue.Enqueue(first);var pixels=new List<int>();
            while(queue.TryDequeue(out var p)) {pixels.Add(p);foreach(var n in Adj(p))if(remaining.Remove(n))queue.Enqueue(n);}
            var members=pixels.ToHashSet();var distance=new Dictionary<int,int>();
            foreach(var p in pixels)if(Adj(p).Any(n=>!members.Contains(n)) || p%width==0 || p%width==width-1 || p/width==0 || p/width==Land.Info.HeightTiles-1){distance[p]=0;queue.Enqueue(p);}
            var meanX=pixels.Average(p=>p%width);var meanY=pixels.Average(p=>p/width);var anchor=first;var max=-1;var tie=double.MaxValue;
            while(queue.TryDequeue(out var p))
            {var d=distance[p];var t=Math.Pow(p%width-meanX,2)+Math.Pow(p/width-meanY,2);if(Land.IsCapital(new Point((p%width+.5)*Land.Info.TileSize,(p/width+.5)*Land.Info.TileSize)) && (d>max || d==max && (t<tie || t==tie && p<anchor))){anchor=p;max=d;tie=t;}foreach(var n in Adj(p))if(members.Contains(n) && distance.TryAdd(n,d+1))queue.Enqueue(n);}
            var size=Land.Info.TileSize;var minX=pixels.Min(p=>p%width);var minY=pixels.Min(p=>p/width);pixels.Sort();
            result.Add(new(pixels[0],new((anchor%width+.5)*size,(anchor/width+.5)*size),new(minX*size,minY*size,(pixels.Max(p=>p%width)-minX+1)*size,(pixels.Max(p=>p/width)-minY+1)*size),pixels.ToArray()));
        }
        result=result.OrderByDescending(c=>c.Tiles.Length).ThenBy(c=>c.Key).ToList();components[id]=(revision,result);AnalysisMilliseconds+=System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds;return result;
    }
}
