using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed class SystemStatusIcon:FrameworkElement
{
    internal string Kind {get;set;}="wifi";
    internal bool Connected {get;set;}
    internal bool Charging {get;set;}
    internal int Percent {get;set;}=-1;
    internal SystemStatusIcon(){Width=26;Height=20;VerticalAlignment=VerticalAlignment.Center;IsHitTestVisible=false;}
    protected override void OnRender(DrawingContext dc)
    {
        var ink=TryFindResource("TextBrush") as Brush??Brushes.White;var pen=new Pen(ink,1.5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
        if(Kind=="battery")
        {
            dc.DrawRoundedRectangle(null,pen,new Rect(1,4,20,12),2,2);dc.DrawRectangle(ink,null,new Rect(23,8,2,4));
            if(Percent>=0)dc.DrawRoundedRectangle(ink,null,new Rect(3,6,16*Math.Clamp(Percent/100d,0,1),8),1,1);
            if(Charging){var bolt=Geometry.Parse("M13,1 L7,11 L12,11 L9,20 L18,8 L13,8 Z");dc.DrawGeometry(BugVisuals.B("#FACC15"),new Pen(BugVisuals.B("#222222"),.7),bolt);}
        }
        else
        {
            foreach(var radius in new[]{5d,9d,13d})
            {
                var arc=new StreamGeometry();using(var g=arc.Open()){g.BeginFigure(new Point(13-radius*.8,19-radius*.6),false,false);g.ArcTo(new Point(13+radius*.8,19-radius*.6),new Size(radius,radius),0,false,SweepDirection.Clockwise,true,false);}dc.DrawGeometry(null,pen,arc);
            }
            dc.DrawEllipse(ink,null,new Point(13,18),1.5,1.5);
            if(!Connected){var x=new Pen(ink,2);dc.DrawLine(x,new Point(17,12),new Point(25,20));dc.DrawLine(x,new Point(25,12),new Point(17,20));}
        }
    }
}
