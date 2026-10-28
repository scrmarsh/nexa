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
            NexaMotion.SetShown(art,enabled,settings,.35);NexaMotion.SetShown(corners,enabled,settings);corners.Options=settings;
            var viewport=new Rect(0,0,Math.Max(1,root.ActualWidth),Math.Max(1,root.ActualHeight));art.ConfigureAnimation(settings,viewport);art.Update(viewport.Width,viewport.Height,[],settings.DecorationDensity);
        }
        root.SizeChanged+=(_,_)=>Refresh();root.Loaded+=(_,_)=>Refresh();root.IsVisibleChanged+=(_,_)=>Refresh();return root;
    }
}
internal sealed class SeasonalCorners:FrameworkElement
{
    internal NexaOptions Options {get;set;}=new();
    private readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    private readonly System.Windows.Threading.DispatcherTimer motion=new(){Interval=TimeSpan.FromMilliseconds(100)};
    private DrawingGroup? staticArt;private Size artSize;
    internal static double WebSize(Size size)=>Math.Min(725,Math.Min(size.Width*.46,size.Height*.62));
    internal static Point SpiderPoint(double time,double size)
    {
        var angle=time*.06+.4*Math.Sin(time*.15);return new(26+6*Math.Cos(angle),Math.Min(size*.24,120)+14*Math.Sin(angle));
    }
    internal SeasonalCorners()
    {
        motion.Tick+=(_,_)=>{if(IsVisible && Options.SeasonalAnimation && NexaMotion.Allowed(Options))InvalidateVisual();};
        Loaded+=(_,_)=>motion.Start();Unloaded+=(_,_)=>motion.Stop();
        IsVisibleChanged+=(_,_)=>{if(IsVisible && IsLoaded)motion.Start();else motion.Stop();};
    }
    protected override void OnRender(DrawingContext dc)
    {
        if(ActualWidth<=0 || ActualHeight<=0)return;
        if(staticArt==null || artSize!=RenderSize){artSize=RenderSize;staticArt=new DrawingGroup();using(var art=staticArt.Open())DrawStatic(art);staticArt.Freeze();}
        dc.DrawDrawing(staticArt);
        var time=Options.SeasonalAnimation && NexaMotion.Allowed(Options)?clock.Elapsed.TotalSeconds:0;
        var point=SpiderPoint(time,WebSize(RenderSize));dc.PushTransform(new TranslateTransform(point.X,point.Y));dc.PushTransform(new ScaleTransform(1.7,1.7));dc.PushTransform(new RotateTransform(Math.Sin(time*.06)*35));
        BugVisuals.Draw(dc,new BugEntity{Species=BugSpecies.Spiders},new EcosystemOptions{Appearance=BugAppearance.Halloween,MatchAppearance=false,Adorable=true},true);dc.Pop();dc.Pop();dc.Pop();
    }
    private void DrawStatic(DrawingContext dc)
    {
        var size=WebSize(RenderSize);var pen=BugVisuals.P("#609D8CAE",.75);
        for(var i=0;i<9;i++){var a=i*Math.PI/16;dc.DrawLine(pen,new(0,0),new(Math.Cos(a)*size,Math.Sin(a)*size));}
        for(var ring=1;ring<=9;ring++)for(var i=0;i<8;i++)
        {
            var a=i*Math.PI/16;var b=(i+1)*Math.PI/16;var radius=size*ring/10;
            var line=new StreamGeometry();using(var g=line.Open()){g.BeginFigure(new(Math.Cos(a)*radius,Math.Sin(a)*radius),false,false);g.QuadraticBezierTo(new(Math.Cos((a+b)/2)*radius*.96,Math.Sin((a+b)/2)*radius*.96),new(Math.Cos(b)*radius,Math.Sin(b)*radius),true,false);}line.Freeze();dc.DrawGeometry(null,pen,line);
        }
        var pumpkin=Math.Min(225,ActualWidth*.28);var x=ActualWidth-pumpkin;var y=ActualHeight-pumpkin;var stem=new Point(x+pumpkin*.5,y+pumpkin*.2);
        var ends=new[]{new Point(12,ActualHeight-16),new Point(ActualWidth-16,14),new Point(ActualWidth*.48,ActualHeight-65),new Point(ActualWidth-65,ActualHeight*.38),new Point(ActualWidth*.7,ActualHeight-100),new Point(ActualWidth-115,ActualHeight*.58)};
        var random=new Random(102726);
        for(var vine=0;vine<ends.Length;vine++)
        {
            var end=ends[vine];Point one,two;
            if(vine==0){one=new(stem.X-60,ActualHeight+20);two=new(ActualWidth*.35,ActualHeight+5);}
            else if(vine==1){one=new(ActualWidth+12,ActualHeight*.7);two=new(ActualWidth-42,ActualHeight*.25);}
            else{one=new(stem.X-70-random.Next(60),stem.Y+random.Next(-70,110));two=new(end.X+random.Next(-75,76),end.Y+random.Next(-65,66));}
            var curve=new StreamGeometry();using(var g=curve.Open()){g.BeginFigure(stem,false,false);g.BezierTo(one,two,end,true,false);}curve.Freeze();dc.DrawGeometry(null,BugVisuals.P(vine<2?"#AD4D7C0F":"#8565A30D",vine<2?5:3),curve);
            for(var leaf=1;leaf<=5;leaf++)
            {
                var t=leaf/6d;var u=1-t;var at=new Point(u*u*u*stem.X+3*u*u*t*one.X+3*u*t*t*two.X+t*t*t*end.X,u*u*u*stem.Y+3*u*u*t*one.Y+3*u*t*t*two.Y+t*t*t*end.Y);
                dc.PushTransform(new TranslateTransform(at.X,at.Y));dc.PushTransform(new RotateTransform(random.Next(-165,166)));
                dc.DrawGeometry(null,BugVisuals.P("#9065A30D",1.4),Geometry.Parse("M0,0 C-18,-35 32,-40 23,-16 C18,-4 2,-20 15,-23"));
                dc.DrawGeometry(BugVisuals.B("#A365A30D"),BugVisuals.P("#80365314",.6),Geometry.Parse("M4,-4 Q8,-23 25,-15 Q23,3 4,-4 Z"));dc.Pop();dc.Pop();
            }
        }
        dc.PushTransform(new TranslateTransform(x,y));dc.PushTransform(new ScaleTransform(pumpkin/64,pumpkin/64));dc.DrawDrawing(NexaweenDecorations.Asset(1));dc.Pop();dc.Pop();
    }
}
