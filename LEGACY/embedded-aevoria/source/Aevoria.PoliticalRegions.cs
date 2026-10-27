using System.Diagnostics;
using System.Windows;
namespace SessionDashboard;
internal sealed partial class LivingNationWorld
{
    private int coarseRevision=-1;
    private void SynchronizeFrontierContests(BugManager manager)
    {
        if(coarseRevision==manager.TerritoryRevision)return;coarseRevision=manager.TerritoryRevision;
        foreach(var cell in conflicts.Keys.ToArray())Contest(cell,null);
        // Only contested coarse rectangles are visited, and only when their revision changes.
        foreach(var coarse in manager.Territories.Where(t=>t.Contested && t.ChallengerId!=null))
        {
            var challenger=nations.FirstOrDefault(n=>n.FactionId==coarse.ChallengerId && n.Status==NationExistence.Active);if(challenger==null)continue;
            var bounds=manager.RegionBounds(coarse.Id);var unit=Land.Info.TileSize*8;
            var left=Math.Clamp((int)(bounds.Left/unit),0,Land.Columns-1);var right=Math.Clamp((int)(bounds.Right/unit),0,Land.Columns-1);
            var top=Math.Clamp((int)(bounds.Top/unit),0,Land.Rows-1);var bottom=Math.Clamp((int)(bounds.Bottom/unit),0,Land.Rows-1);
            for(var y=top;y<=bottom;y++)for(var x=left;x<=right;x++){var cell=y*Land.Columns+x;if(Owner(cell)!=null && Owner(cell)!=challenger.Id && bounds.Contains(Land.Center(cell)))Contest(cell,challenger.Id);}
        }
    }
    private List<NationComponent> RegionalComponents(Guid id)
    {
        var revision=nationRevisions.GetValueOrDefault(id);if(components.TryGetValue(id,out var cached) && cached.Revision==revision)return cached.Components;
        var start=Stopwatch.GetTimestamp();AnalysisCount++;var remaining=Regions(id).ToHashSet();var queue=new Queue<int>();var result=new List<NationComponent>();
        while(remaining.Count>0)
        {
            var first=remaining.Min();remaining.Remove(first);queue.Enqueue(first);var cells=new List<int>();
            while(queue.TryDequeue(out var cell)){cells.Add(cell);foreach(var n in Land.Neighbors(cell))if(remaining.Remove(n))queue.Enqueue(n);}
            var members=cells.ToHashSet();var depths=new Dictionary<int,int>();
            foreach(var cell in cells.Order())if(Land.Neighbors(cell).Count(n=>members.Contains(n))<4){depths[cell]=0;queue.Enqueue(cell);}
            var anchor=first;var deepest=-1;var center=new Point(cells.Average(c=>c%Land.Columns),cells.Average(c=>c/Land.Columns));var tie=double.MaxValue;
            while(queue.TryDequeue(out var cell))
            {
                var depth=depths[cell];var distance=(new Point(cell%Land.Columns,cell/Land.Columns)-center).LengthSquared;
                if(Land.IsCapital(Land.Center(cell)) && (depth>deepest || depth==deepest && (distance<tie || distance==tie && cell<anchor))){anchor=cell;deepest=depth;tie=distance;}
                foreach(var n in Land.Neighbors(cell))if(members.Contains(n) && depths.TryAdd(n,depth+1))queue.Enqueue(n);
            }
            cells.Sort();var unit=8*Land.Info.TileSize;var minX=cells.Min(c=>c%Land.Columns);var minY=cells.Min(c=>c/Land.Columns);
            result.Add(new(first,Land.Center(anchor),new Rect(minX*unit,minY*unit,(cells.Max(c=>c%Land.Columns)-minX+1)*unit,(cells.Max(c=>c/Land.Columns)-minY+1)*unit),[],cells.ToArray(),cells.Sum(Land.Area)));
        }
        result=result.OrderByDescending(c=>c.AreaTiles).ThenBy(c=>c.Key).ToList();components[id]=(revision,result);AnalysisMilliseconds+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;return result;
    }
}
