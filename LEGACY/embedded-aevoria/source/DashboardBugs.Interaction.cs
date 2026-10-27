using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;
internal sealed partial class BugOverlay
{
    private Point? sliceStart, sliceLast;
    private bool sliced;
    private bool OnStrand(Point p)=>Manager.IsArcade && Manager.Webs.Any(w=>w.Strands.Any(s=>(BugManager.VisibleStrandEnd(w,s)-s.A).Length>1 && BugManager.DistanceToSegment(p,s.A,BugManager.VisibleStrandEnd(w,s))<5));
    private bool OverWidget(Point p)=>(widgetRects??Manager.Surfaces.Values.ToArray()).Any(r=>r.Contains(p));
    private void DrawCageBody(DrawingContext dc,BugEntity bug)
    {
        // A curved chain of body segments surrounds the captive; legs make the cage readable.
        for(var i=0;i<15;i++)
        {
            var angle=-Math.PI/2-i*Math.PI*1.8/14;
            var p=new Point(Math.Cos(angle)*13,13+Math.Sin(angle)*13);
            dc.DrawLine(new Pen(Brushes.SaddleBrown,.7),p,p+new Vector(Math.Cos(angle),Math.Sin(angle))*5);
            dc.DrawEllipse(BugArtwork.Roach,null,p,2.8,2.8);
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if(sliceStart is not {} start || e.LeftButton!=MouseButtonState.Pressed)return;
        var p=ScreenToWorld(e.GetPosition(this));
        if(!sliced && (p-start).Length<SystemParameters.MinimumHorizontalDragDistance)return;
        sliced=true;Manager.CutWebs(sliceLast??start,p);sliceLast=p;RefreshFrame();e.Handled=true;
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if(sliceStart==null)return;
        sliceStart=sliceLast=null;ReleaseMouseCapture();e.Handled=true;RefreshFrame();
    }
    protected override void OnLostMouseCapture(MouseEventArgs e)
    { sliceStart=sliceLast=null;base.OnLostMouseCapture(e); }
}
