using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed class BugOverlay : FrameworkElement
{
    internal event Action<BugEntity>? DirectSquashed;
    internal BugManager Manager { get; }
    internal Point ContentOrigin;
    private double scaleX = 1, scaleY = 1;
    private Rect[] protectedWidgets = [];
    private Geometry? protectedClip;
    private Point? cursor;
    internal IEnumerable<Point> DecorationTargets {get;set;} = [];
    internal void SetCursor(Point point) => cursor=point;
    internal bool DrawWorld { get; set; }
    internal double LastRenderMilliseconds { get; private set; }
    internal long RenderedFrames { get; private set; }
    internal BugOverlay(BugManager? manager = null) { Manager = manager ?? new(); ClipToBounds = true; Focusable = false; }
    internal void SetViewOrigin(Point origin) { ContentOrigin = origin; RefreshFrame(); }
    internal void SetWorldMapping(Point origin, Size size)
    { if(ContentOrigin==origin && Math.Abs(scaleX-size.Width/BugManager.WorldWidth)<.000001 && Math.Abs(scaleY-size.Height/BugManager.WorldHeight)<.000001)return; ContentOrigin = origin; scaleX = size.Width / BugManager.WorldWidth; scaleY = size.Height / BugManager.WorldHeight;Manager.WebCoordinateAspect=scaleX/Math.Max(.001,scaleY); RebuildProtectedClip(); RefreshFrame(); }
    internal void SetProtectedWidgets(IEnumerable<Rect> rectangles) {var next=rectangles.ToArray();if(protectedWidgets.SequenceEqual(next))return;protectedWidgets=next;RebuildProtectedClip();}
    private Rect viewport = Rect.Empty;
    internal void SetViewport(Rect value) { if (viewport == value) return; viewport = value; RebuildProtectedClip(); RefreshFrame(); }
    private void RebuildProtectedClip()
    {
        protectedClip = null;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        var area = new Rect(RenderSize);
        if (!viewport.IsEmpty) area.Intersect(viewport);
        protectedClip = new RectangleGeometry(area); protectedClip.Freeze();
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {base.OnRenderSizeChanged(sizeInfo);RebuildProtectedClip();}
    internal Point ScreenToWorld(Point point) => new((point.X - ContentOrigin.X) / Math.Max(.001, scaleX), (point.Y - ContentOrigin.Y) / Math.Max(.001, scaleY));
    internal Point WorldToScreen(Point point) => new(point.X * scaleX + ContentOrigin.X, point.Y * scaleY + ContentOrigin.Y);
    private double ViewScale => DrawWorld ? Math.Min(scaleX, scaleY) : 1;
    private bool Protected(Point point) => (!viewport.IsEmpty && !viewport.Contains(point)) || protectedWidgets.Any(r => r.Contains(new Point(point.X - ContentOrigin.X, point.Y - ContentOrigin.Y)));
    private IEnumerable<BugEntity> VisibleBugs => Manager.Bugs.Where(b => Manager.IsActive(b)).Where(b => Manager.Preferences.Species is BugSpecies.AllBugs or BugSpecies.AllThree || b.Species == Manager.Preferences.Species).Take(Manager.EcosystemActive ? Manager.EcosystemCap : 600);
    internal BugEntity? HitAt(Point point)
    {
        if (Protected(point)) return null;
        return VisibleBugs.Reverse().FirstOrDefault(b => b.State is not (BugState.Hiding or BugState.Squashed) && b.Health > 0 && (point - WorldToScreen(b.Position)).Length <= BugVisuals.HitRadius(b) * b.SizeScale * ViewScale);
    }
    protected override HitTestResult? HitTestCore(PointHitTestParameters p) => DrawWorld || HitAt(p.HitPoint) != null ? new PointHitTestResult(this, p.HitPoint) : null;
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (HitAt(e.GetPosition(this)) is { } bug) { if (Manager.Squash(bug)) DirectSquashed?.Invoke(bug); e.Handled = true; RefreshFrame(); }
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);cursor=e.GetPosition(this);}
    internal void RefreshFrame() => InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        var began = System.Diagnostics.Stopwatch.GetTimestamp(); RenderedFrames++;
        if(protectedClip!=null)dc.PushClip(protectedClip);
        if (Manager.EcosystemActive) DrawFeatures(dc);
        foreach (var splat in Manager.Splats)
        { var p = WorldToScreen(splat.Position); dc.PushOpacity(.45 * Math.Clamp((4 - splat.Age) / 2, 0, 1)); dc.DrawEllipse(BugArtwork.Ant, null, p, 4 * ViewScale, 2 * ViewScale); dc.Pop(); }
        var options = Manager.EcosystemSettings;
        foreach (var bug in VisibleBugs)
        {
            if (bug.State == BugState.Hiding) continue;
            var p = WorldToScreen(bug.Position); var radius = BugVisuals.HitRadius(bug) * bug.SizeScale * ViewScale;
            if (p.X + radius < 0 || p.Y + radius < 0 || p.X - radius > ActualWidth || p.Y - radius > ActualHeight) continue;
            var sx = bug.SizeScale * ViewScale * (bug.State == BugState.Squashed ? 1 + bug.SquashAge * 2 : 1);
            var sy = bug.SizeScale * ViewScale * (bug.State == BugState.Squashed ? Math.Max(.15, 1 - bug.SquashAge * 5) : 1);
            var worldAngle=bug.Angle*Math.PI/180;
            var angle = Math.Atan2(Math.Sin(worldAngle)*scaleY,Math.Cos(worldAngle)*scaleX);
            var animated=NexaMotion.Allowed(Manager.VisualOptions);
            if(animated)
            {
                dc.PushTransform(new MatrixTransform(new Matrix(sx,0,0,sy,0,0)*new Matrix(Math.Cos(angle),Math.Sin(angle),-Math.Sin(angle),Math.Cos(angle),p.X,p.Y)));
                BugVisuals.DrawContactShadow(dc,bug,BugAnimation.Airborne(bug));dc.Pop();
            }
            if(animated && bug.FlightHeight>0) p.Y-=Math.Sin(Manager.SimulationAge*3+bug.Age)*1.2*bug.FlightHeight+bug.FlightHeight*12;
            dc.PushTransform(new MatrixTransform(new Matrix(sx, 0, 0, sy, 0, 0) * new Matrix(Math.Cos(angle), Math.Sin(angle), -Math.Sin(angle), Math.Cos(angle), p.X, p.Y)));
            if(animated)
            {
                var look=bug.Target;
                if(cursor is {} pointer && (pointer-p).Length<250)look=ScreenToWorld(pointer);
                else if(bug.ResourceId is {} id && Manager.Resources.FirstOrDefault(r=>r.Id==id) is {} resource)look=resource.Position;
                else if(DecorationTargets.FirstOrDefault(target=>(WorldToScreen(target)-p).Length<130) is var target && target!=default(Point))look=target;
                var motionScale=Math.Sqrt(Math.Pow(scaleX*Math.Cos(worldAngle),2)+Math.Pow(scaleY*Math.Sin(worldAngle),2))/Math.Max(.1,ViewScale);
                BugVisuals.Draw(dc,bug,options,Manager.SeasonalCostumes,Manager.SimulationAge,look,motionScale,Window.GetWindow(this)?.IsActive==true,false);
            }
            else BugVisuals.Draw(dc, bug, options, Manager.SeasonalCostumes);
            dc.Pop();

        }
        foreach(var bug in VisibleBugs.Where(b=>b.PartnerId!=null && b.Carrying>0 && b.State!=BugState.Squashed))
        {
            if(bug.InteractionId.CompareTo(bug.PartnerId!.Value)>=0)continue;
            var partner=Manager.Bugs.FirstOrDefault(b=>Manager.IsActive(b) && b.InteractionId==bug.PartnerId && b.Carrying>0 && b.State!=BugState.Squashed);if(partner==null)continue;
            var a=WorldToScreen(bug.Position);var b=WorldToScreen(partner.Position);if((a-b).Length>100)continue;
            var center=a+(b-a)*.5;var size=3*ViewScale*options.Size;
            dc.DrawLine(BugVisuals.P("#B98D54",.6),a,center);dc.DrawLine(BugVisuals.P("#B98D54",.6),b,center);
            dc.DrawRoundedRectangle(BugVisuals.B(Manager.SeasonalCandy?"#F97316":"#C6A65D"),BugVisuals.P("#E9D5FF",.6),new Rect(center.X-size,center.Y-size*.6,size*2,size*1.2),1.5,1.5);
        }
        if (protectedClip != null) dc.Pop();
        LastRenderMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds;
        Manager.ObserveFrameCost(LastRenderMilliseconds);
    }
    internal bool ReduceMotion {get;set;}
    private readonly Dictionary<Guid,long> webReveals=[];
    internal static double SilkReveal(long started,long now)=>Math.Clamp((now-started)/1500d,0,1);
    private void DrawFeatures(DrawingContext dc)
    {
        foreach (var nest in Manager.Nests)
        {
            var p = WorldToScreen(nest.Position);
            var colors = new[] { "#B58A5A", "#A3A36B", "#8AB6C5", "#B594D9", "#E5878A", "#BDA366" };
            var color = colors[Array.IndexOf(BugManager.AllSpecies, nest.Species)];
            if (DrawWorld)
            {
                dc.PushOpacity(.075); dc.DrawEllipse(BugVisuals.B(color), null, p, 145 * scaleX, 145 * scaleY); dc.Pop();
                dc.PushTransform(new TranslateTransform(p.X,p.Y));dc.PushTransform(new ScaleTransform(ViewScale,ViewScale));dc.DrawDrawing(BugHabitatVisuals.For(nest.Species));dc.Pop();dc.Pop();
            }
        }
        foreach (var resource in Manager.Resources)
        {
            if (resource.Decoration || Manager.DecorationCandyActive && resource.Kind==BugResourceKind.Candy)continue;
            if (resource.Amount <= .1 || !DrawWorld && (resource.Kind != BugResourceKind.Candy || !Manager.SeasonalCandy)) continue;
            if (resource.Kind == BugResourceKind.Candy && !Manager.SeasonalCandy) continue;
            var p = WorldToScreen(resource.Position); if (p.X < -20 || p.Y < -20 || p.X > ActualWidth + 20 || p.Y > ActualHeight + 20) continue;
            var size = Math.Clamp(2 + resource.Amount / 6, 3, 6) * ViewScale;
            var color = resource.Kind switch { BugResourceKind.Candy => "#F97316", BugResourceKind.Fruit => "#D88191", BugResourceKind.Leaves => "#84A85A", BugResourceKind.Water => "#7DD3FC", _ => "#BDA77A" };
            dc.DrawEllipse(BugVisuals.B(color), BugVisuals.P("#D9C9DD", .5), p, size, size * .7);
            if (resource.Kind == BugResourceKind.Candy) { dc.DrawLine(BugVisuals.P("#A855F7", 2 * ViewScale), p + new Vector(-size * 1.4, -size * .5), p + new Vector(size * 1.4, size * .5)); dc.DrawLine(BugVisuals.P("#FACC15", ViewScale), p + new Vector(0, -size * .6), p + new Vector(0, size * .6)); }
        }
        var silk=BugVisuals.P(Manager.SeasonalCostumes?"#7484CC16":"#809D8CAE",.65);
        var builders=Manager.Bugs.Where(b=>Manager.IsActive(b) && b.WeavingOwner!=null && b.WeavingSegment>=0 && b.SilkAttached && b.Activity==BugActivity.Nesting && b.State!=BugState.Squashed).GroupBy(b=>(b.WeavingOwner!.Value,b.WeavingSegment)).ToDictionary(g=>g.Key,g=>g.First());
        foreach(var removed in webReveals.Keys.Where(id=>!Manager.Webs.Any(w=>w.Owner==id)).ToArray())webReveals.Remove(removed);
        foreach(var web in Manager.Webs)
        {
            var owner=Manager.Bugs.FirstOrDefault(b=>b.InteractionId==web.Owner);
            if(owner!=null && !Manager.IsActive(owner))continue;
            if(!webReveals.TryGetValue(web.Owner,out var started))webReveals[web.Owner]=started=Environment.TickCount64;
            var reveal=SystemParameters.ClientAreaAnimation && !ReduceMotion?SilkReveal(started,Environment.TickCount64):1;

            var box=Manager.WebBounds(web);var screenBox=new Rect(WorldToScreen(box.TopLeft),WorldToScreen(box.BottomRight));
            if(!viewport.IsEmpty && !screenBox.IntersectsWith(viewport))continue;
            BugManager.EnsureSilk(web);
            for(var index=0;index<web.Strands.Count;index++)
            {
                var strand=web.Strands[index];if(strand.Progress<=0 || index>=web.Strands.Count*reveal)continue;
                var from=WorldToScreen(Manager.SilkPoint(web,strand.FromX,strand.FromY));var to=WorldToScreen(Manager.SilkPoint(web,strand.ToX,strand.ToY));var end=from+(to-from)*strand.Progress;
                if(builders.TryGetValue((web.Owner,index),out var builder))
                {
                    var direction=builder.Angle*Math.PI/180;var heading=new Vector(Math.Cos(direction)*scaleX,Math.Sin(direction)*scaleY);if(heading.Length>.001)heading.Normalize();
                    end=WorldToScreen(builder.Position)-heading*(9*builder.SizeScale*ViewScale);
                }
                dc.DrawLine(silk,from,end);
            }
        }
    }
}
