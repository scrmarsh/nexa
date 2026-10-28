using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    private readonly Queue<BugEntity> arrivals=[];
    private double nextArrival;
    internal bool IsActive(BugEntity bug) => !bug.ArrivalPending;
    internal int ActivePopulation => Bugs.Count(IsActive);
    private bool ArrivalBudgetAvailable => Environment.TickCount64/1000d>=pressureUntil && FrameCostAverage<32;
    private double ArrivalInterval => (Preferences.Density switch {BugDensity.Low=>3.2,BugDensity.Normal=>2.1,BugDensity.High=>1.7,_=>1.4})*Range(.9,1.2);
    // Presentation/simulation ramp only: every dormant creature remains in authoritative saves.
    internal void BeginDashboardArrival()
    {
        arrivals.Clear();nextArrival=SimulationAge+1.2;WorldSpawnDelay=Math.Max(WorldSpawnDelay,1.2);
        foreach(var bug in Bugs.OrderBy(_=>random.Next()))
        {bug.ArrivalPending=true;arrivals.Enqueue(bug);}
    }
    private void AdvanceArrivals()
    {
        if(arrivals.Count==0 || SimulationAge<nextArrival || !ArrivalBudgetAvailable)return;
        while(arrivals.TryDequeue(out var bug))
        {
            if(!Bugs.Contains(bug))continue;
            bug.ArrivalPending=false;
            // Idle explorers can enter throughout the current viewport; retain active jobs, webs and loads.
            if(bug.Carrying<=0 && bug.PartnerId==null && bug.Activity==BugActivity.Exploring && !Webs.Any(w=>w.Owner==bug.InteractionId))
            {
                bug.Position=ArrivalPoint();bug.Target=ExplorationPoint(bug.Position);
                bug.ExploreUntil=SimulationAge+Range(16,28);bug.Decision=Range(3,6);bug.Surface=null;
            }
            nextArrival=SimulationAge+ArrivalInterval;WorldSpawnDelay=Math.Max(WorldSpawnDelay,nextArrival-SimulationAge);break;
        }
    }
    private Point ArrivalPoint()
    {
        var area=VisibleBounds;area.Intersect(new Rect(24,24,WorldWidth-48,WorldHeight-48));
        if(area.IsEmpty || area.Width<10 || area.Height<10)area=new Rect(24,24,WorldWidth-48,WorldHeight-48);
        var neighbors=Bugs.Where(b=>IsActive(b) && b.State!=BugState.Squashed && area.Contains(b.Position)).TakeLast(256).Select(b=>b.Position).ToArray();
        var best=new Point(area.X+area.Width/2,area.Y+area.Height/2);var bestScore=double.NegativeInfinity;
        for(var attempt=0;attempt<32;attempt++)
        {
            var point=new Point(area.X+random.NextDouble()*area.Width,area.Y+random.NextDouble()*area.Height);
            var distance=neighbors.Length==0?160:Math.Min(160,neighbors.Min(p=>(p-point).Length));
            var score=distance-(Surfaces.Values.Any(rect=>rect.Contains(point))?100:0);
            if(score>bestScore){best=point;bestScore=score;}
            if(score>=120)break;
        }
        return best;
    }
    private Point ExplorationPoint(Point position)
    {
        var area=VisibleBounds;area.Intersect(new Rect(24,24,WorldWidth-48,WorldHeight-48));
        if(area.IsEmpty || area.Width<24 || area.Height<24)area=new Rect(24,24,WorldWidth-48,WorldHeight-48);
        return new(Math.Clamp(position.X+Range(-130,130),area.Left+8,area.Right-8),Math.Clamp(position.Y+Range(-95,95),area.Top+8,area.Bottom-8));
    }
}
