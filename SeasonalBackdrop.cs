using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace SessionDashboard;
internal static class SeasonalBackdrop
{
    internal static Grid Wrap(UIElement content,Func<NexaOptions> options,bool permanentCorners=false)
    {
        var root=new Grid{ClipToBounds=true};var art=new NexaweenDecorations{Opacity=.35};root.Children.Add(art);
        var corners=new SeasonalCorners{IsHitTestVisible=false};if(permanentCorners)root.Children.Add(corners);root.Children.Add(content);
        void Refresh()
        {
            var settings=options();var enabled=NexaSeason.Active(settings,DateTimeOffset.UtcNow) && settings.SeasonalDecorations && settings.DecorationDensity!="Off";
            art.Visibility=corners.Visibility=enabled?Visibility.Visible:Visibility.Collapsed;
            var viewport=new Rect(0,0,Math.Max(1,root.ActualWidth),Math.Max(1,root.ActualHeight));art.ConfigureAnimation(settings,viewport);art.Update(viewport.Width,viewport.Height,[],settings.DecorationDensity);
        }
        root.SizeChanged+=(_,_)=>Refresh();root.Loaded+=(_,_)=>Refresh();root.IsVisibleChanged+=(_,_)=>Refresh();return root;
    }
}
internal sealed class SeasonalCorners:FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        // This home-corner spider/web is entirely static and separate from both actors and live webs.
        var pen=BugVisuals.P("#709D8CAE",.7);var origin=new Point(0,0);
        for(var i=0;i<7;i++){var angle=i*Math.PI/12;dc.DrawLine(pen,origin,new(Math.Cos(angle)*145,Math.Sin(angle)*145));}
        for(var ring=1;ring<=5;ring++)for(var i=0;i<6;i++){var a=i*Math.PI/12;var b=(i+1)*Math.PI/12;dc.DrawLine(pen,new(Math.Cos(a)*ring*27,Math.Sin(a)*ring*27),new(Math.Cos(b)*ring*27,Math.Sin(b)*ring*27));}
        dc.PushTransform(new TranslateTransform(74,53));dc.PushTransform(new ScaleTransform(1.7,1.7));BugVisuals.Draw(dc,new BugEntity{Species=BugSpecies.Spiders},new EcosystemOptions{Appearance=BugAppearance.Halloween,MatchAppearance=false,Adorable=true},true);dc.Pop();dc.Pop();
        var size=Math.Min(225,ActualWidth*.28);var x=ActualWidth-size;var y=ActualHeight-size;
        // Long, looping vines stay behind the interactive content and follow the window edges.
        var bottom=ActualHeight-14;var right=ActualWidth-14;var stem=new Point(x+size*.5,y+size*.2);
        var vines=new StreamGeometry();using(var g=vines.Open())
        {
            g.BeginFigure(stem,false,false);g.BezierTo(new(right-80,bottom-140),new(right-140,bottom+5),new(right-210,bottom-8),true,false);
            g.BezierTo(new(ActualWidth*.58,bottom-35),new(ActualWidth*.3,bottom+12),new(12,bottom-2),true,false);
            g.BeginFigure(stem,false,false);g.BezierTo(new(right+20,bottom-170),new(right-30,ActualHeight*.55),new(right-5,ActualHeight*.38),true,false);
            g.BezierTo(new(right+8,ActualHeight*.23),new(right-22,90),new(right-3,16),true,false);
        }
        vines.Freeze();dc.DrawGeometry(null,BugVisuals.P("#AD4D7C0F",6),vines);dc.DrawGeometry(null,BugVisuals.P("#8065A30D",2),vines);
        for(var leafX=55d;leafX<right-80;leafX+=130)
        {
            var curl=new StreamGeometry();using(var g=curl.Open()){g.BeginFigure(new(leafX,bottom-9),false,false);g.BezierTo(new(leafX-28,bottom-75),new(leafX+50,bottom-75),new(leafX+23,bottom-35),true,false);g.BezierTo(new(leafX+3,bottom-18),new(leafX-4,bottom-53),new(leafX+15,bottom-47),true,false);}dc.DrawGeometry(null,BugVisuals.P("#8965A30D",2),curl);Leaf(dc,new(leafX+31,bottom-34),-35);
        }
        for(var leafY=70d;leafY<bottom-130;leafY+=130)
        {
            var curl=new StreamGeometry();using(var g=curl.Open()){g.BeginFigure(new(right-8,leafY+45),false,false);g.BezierTo(new(right-65,leafY+30),new(right-65,leafY-12),new(right-27,leafY),true,false);g.BezierTo(new(right-9,leafY+14),new(right-47,leafY+19),new(right-41,leafY+3),true,false);}dc.DrawGeometry(null,BugVisuals.P("#8965A30D",2),curl);Leaf(dc,new(right-29,leafY-8),-65);
        }
        dc.PushTransform(new TranslateTransform(x,y));dc.PushTransform(new ScaleTransform(size/64,size/64));dc.DrawDrawing(NexaweenDecorations.Asset(1));dc.Pop();dc.Pop();
    }
    private static void Leaf(DrawingContext dc,Point at,double angle)
    {
        dc.PushTransform(new TranslateTransform(at.X,at.Y));dc.PushTransform(new RotateTransform(angle));
        dc.DrawGeometry(BugVisuals.B("#AA65A30D"),BugVisuals.P("#80365314",.6),Geometry.Parse("M-12,0 Q-3,-13 13,-5 Q8,10 -12,0 Z"));dc.DrawLine(BugVisuals.P("#B0A3C95C",.7),new(-10,0),new(10,-4));dc.Pop();dc.Pop();
    }
}
