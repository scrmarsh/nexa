using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
// Decoration only. The painterly gameplay/costume asset remains available to its callers.
internal static class FlatPumpkin
{
    internal static void Draw(DrawingContext dc,Rect bounds)
    {
        var r=NexaPumpkin.Fit(bounds);dc.PushTransform(new TranslateTransform(r.X,r.Y));dc.PushTransform(new ScaleTransform(r.Width/96,r.Height/64));
        var dark=BugVisuals.B("#E77825");var light=BugVisuals.B("#F99B38");
        dc.DrawRoundedRectangle(BugVisuals.B("#4C803C"),null,new Rect(44,2,9,21),4,4);
        dc.DrawEllipse(dark,null,new Point(31,39),26,24);dc.DrawEllipse(dark,null,new Point(65,39),26,24);
        dc.DrawEllipse(light,null,new Point(48,39),21,25);dc.Pop();dc.Pop();
    }
}
