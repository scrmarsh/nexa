using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class BugOverlay
{
    internal Matrix Camera=Matrix.Identity;
    internal Rect? ViewportClip;
    internal bool ShowHud=true;
    internal bool SelectUnits;
    internal event Action<BugEntity>? UnitSelected;
    private Rect[]? widgetRects;
    private Geometry? widgetClip;
    internal Point ScreenToWorld(Point point)
    {
        var inverse=Camera;if(!inverse.HasInverse)return new Point();inverse.Invert();
        return inverse.Transform(point)-(Vector)ContentOrigin;
    }
    internal Point WorldToScreen(Point point)=>Camera.Transform(point+(Vector)ContentOrigin);
    internal void SetViewOrigin(Point origin,Rect? viewport=null)
    {
        ContentOrigin=origin;ViewportClip=viewport;RebuildWidgetClip();RefreshFrame();
    }
    internal void SetWidgetSurfaces(IEnumerable<Rect> surfaces)
    {widgetRects=surfaces.ToArray();RebuildWidgetClip();}
    private void RebuildWidgetClip()
    {
        widgetClip=null;
        if(TerrainView!=null || !Manager.IsInfinite || widgetRects==null || widgetRects.Length==0)return;
        var holes=new GeometryGroup {FillRule=FillRule.Nonzero};
        foreach(var rect in widgetRects)holes.Children.Add(new RectangleGeometry(rect));
        var outer=new RectangleGeometry(new Rect(-200,-200,Manager.BattleArena.Width+400,Manager.BattleArena.Height+400));
        var clip=new CombinedGeometry(GeometryCombineMode.Exclude,outer,holes) {Transform=new TranslateTransform(ContentOrigin.X,ContentOrigin.Y)};
        clip.Freeze();widgetClip=clip;
    }
    internal void BindManager(BugManager manager)
    {
        if(ReferenceEquals(Manager,manager))return;Manager=manager;titles.Clear();battleText.Clear();territoryDrawing=null;RebuildWidgetClip();RefreshFrame();
    }
}
