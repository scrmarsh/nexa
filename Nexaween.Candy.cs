using System.Windows;
namespace SessionDashboard;
internal sealed partial class NexaweenDecorations
{
    internal IReadOnlyList<(Guid Id, Point Position)> AvailableCandy => actors.Where(a=>!a.Contextual && a.ConsumedAt<0 && CandyKinds.Contains(a.Kind) && (elapsed.Elapsed.TotalSeconds<a.BusyUntil || !animationOptions.SeasonalAnimation || !NexaMotion.Allowed(animationOptions) || elapsed.Elapsed.TotalSeconds-a.Born is >=1 && elapsed.Elapsed.TotalSeconds-a.Born<=a.Hold+1)).Select(a=>(a.Id,new Point(a.Bounds.X+a.Bounds.Width/2,a.Bounds.Y+a.Bounds.Height/2))).ToArray();
    internal Guid? CandyAt(Point point)
        =>actors.LastOrDefault(a=>AvailableCandy.Any(c=>c.Id==a.Id) && a.Bounds.Contains(point) && a.BusyUntil<=elapsed.Elapsed.TotalSeconds && a.CandyAmount>.1)?.Id;
    internal bool TryCollect(Guid id)
    {
        var actor=actors.FirstOrDefault(a=>a.Id==id);
        if(actor==null || actor.ConsumedAt>=0 || actor.BusyUntil>elapsed.Elapsed.TotalSeconds || actor.CandyAmount<=.1)return false;
        actor.CandyAmount=0;actor.ConsumedAt=elapsed.Elapsed.TotalSeconds;candyRespawn.Start();InvalidateVisual();return true;
    }
    internal void ObserveCandy(Guid id,double amount,bool eating)
    {
        var actor=actors.FirstOrDefault(a=>a.Id==id);if(actor==null || actor.ConsumedAt>=0)return;
        actor.CandyAmount=Math.Clamp(amount,0,1);
        if(eating) {actor.BusyUntil=elapsed.Elapsed.TotalSeconds+2;actor.Born=elapsed.Elapsed.TotalSeconds-1;}
        if(amount<=.1 && actor.ConsumedAt<0){actor.ConsumedAt=elapsed.Elapsed.TotalSeconds;candyRespawn.Start();}
        InvalidateVisual();
    }
    internal void ReconcileCandy() {RecycleCandyActors();InvalidateVisual();}
}
internal sealed partial class BugManager
{
    internal bool DecorationCandyActive {get;set;}
    internal void BindCandy(IReadOnlyList<(Guid Id,Point Position)> sites)
    {
        var ids=sites.Select(s=>s.Id).ToHashSet();Resources.RemoveAll(r=>r.Decoration && !ids.Contains(r.Id));
        foreach(var site in sites)
        {
            var resource=Resources.FirstOrDefault(r=>r.Decoration && r.Id==site.Id);
            if(resource==null)Resources.Add(new EcosystemResource{Id=site.Id,Kind=BugResourceKind.Candy,X=site.Position.X,Y=site.Position.Y,Amount=1,Decoration=true});
            else {resource.X=site.Position.X;resource.Y=site.Position.Y;}
        }
    }
}
public partial class MainWindow
{
    private long lastCandySync;
    private void ReconcileVisibleCandy()
    {
        if(bugsOverlay==null || seasonalArtwork==null || WidgetCanvas.ActualWidth<=0 || WidgetCanvas.ActualHeight<=0 || ecosystem?.ViewCount>1 && !IsActive)return;
        if(Environment.TickCount64-lastCandySync<300)return;lastCandySync=Environment.TickCount64;
        var manager=bugsOverlay.Manager;
        manager.DecorationCandyActive=seasonalArtwork.IsVisible && Theme.SeasonalEnabled && manager.EcosystemSettings.Behavior==BugBehavior.Cozy;
        if(!manager.DecorationCandyActive){manager.BindCandy([]);return;}
        var x=BugManager.WorldWidth/WidgetCanvas.ActualWidth;var y=BugManager.WorldHeight/WidgetCanvas.ActualHeight;
        foreach(var resource in manager.Resources.Where(r=>r.Decoration))
        {
            var eating=manager.Bugs.Any(b=>b.ResourceId==resource.Id && b.Activity==BugActivity.Gathering && (b.Position-resource.Position).Length<55);
            seasonalArtwork.ObserveCandy(resource.Id,resource.Amount,eating);
        }
        seasonalArtwork.ReconcileCandy();
        manager.BindCandy(seasonalArtwork.AvailableCandy.Select(s=>(s.Id,new Point(s.Position.X*x,s.Position.Y*y))).ToArray());
    }
}
