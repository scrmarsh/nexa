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
        dc.PushTransform(new TranslateTransform(x,y));dc.PushTransform(new ScaleTransform(size/64,size/64));dc.DrawDrawing(NexaweenDecorations.Asset(0));
        for(var i=0;i<5;i++){var curve=new StreamGeometry();using(var g=curve.Open()){g.BeginFigure(new(31,13),false,false);g.BezierTo(new(8-i*8,52),new(-20-i*16,63),new(-35-i*20,60),true,false);}dc.DrawGeometry(null,BugVisuals.P(i<2?"#80567D36":"#5065A30D",Math.Max(.6,2-i*.3)),curve);}
        dc.Pop();dc.Pop();
    }
}
