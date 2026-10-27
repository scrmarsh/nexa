using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
// Render drainage as connected vector edges at distance instead of aliasing one-tile rivers.
internal sealed class LivingHydrologyView
{
    private readonly Geometry geometry;
    private readonly BugOverlay overlay;
    internal LivingHydrologyView(LivingTerrain terrain,BugOverlay overlay)
    {
        this.overlay=overlay;var path=new StreamGeometry();using(var dc=path.Open())foreach(var (from,to) in terrain.Drainage)
        {var a=terrain.DrainagePoint(from);var b=terrain.DrainagePoint(to);dc.BeginFigure(new(a.X*terrain.Info.TileSize,a.Y*terrain.Info.TileSize),false,false);dc.LineTo(new(b.X*terrain.Info.TileSize,b.Y*terrain.Info.TileSize),true,false);}path.Freeze();geometry=path;
    }
    internal void Draw(DrawingContext dc,Rect view)
    {
        var zoom=Math.Max(.005,overlay.Camera.M11);if(zoom>.6 || !geometry.Bounds.IntersectsWith(view))return;
        dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));dc.DrawGeometry(null,new Pen(Brushes.SteelBlue,.7/zoom),geometry);dc.Pop();
    }
}
