using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal static class SpiderAnatomy
{
    internal static Point Root(int index,int side)
    {var x=-9.2+index*2.2;return new(x,side*6.8*Math.Sqrt(Math.Max(0,1-Math.Pow((x+5)/7,2))));}
    internal static void Body(DrawingContext dc,Brush abdomen,Brush front,Pen edge)
    {
        dc.DrawEllipse(abdomen,edge,new(-5,0),7,6.8);
        dc.DrawEllipse(front,edge,new(4,0),4.5,4.2);
        dc.DrawEllipse(BugVisuals.B("#28EBDCF6"),null,new(-6.7,-2.5),3.6,2.1);
        dc.DrawEllipse(BugVisuals.B("#20FFF4E8"),null,new(3,-1.8),2.1,1.1);
    }
    internal static void Legs(DrawingContext dc,Pen pen)
    {
        Span<BugLegPose> pose=stackalloc BugLegPose[8];BugAnimation.FillLegs(BugSpecies.Spiders,0,true,pose);
        foreach(var leg in pose){dc.DrawLine(pen,leg.Root,leg.Knee);dc.DrawLine(pen,leg.Knee,leg.Foot);}
    }
}
