using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;
internal sealed class SeasonalCorners:FrameworkElement
{
    private NexaOptions options=new();
    internal NexaOptions Options {get=>options;set{options=value;Reconcile();InvalidateVisual();}}
    internal bool AdvancedAnimations {get;set;}=true;
    internal HomeSpider Companion {get;}=new();
    private readonly DispatcherTimer motion=new(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(50)};
    private readonly System.Diagnostics.Stopwatch clock=new();
    private double previous;
    private DrawingGroup? staticArt;private Size artSize;
    private Window? host;
    internal static double WebSize(Size size)=>Math.Min(725,Math.Min(size.Width*.46,size.Height*.62));
    // Retained geometry helper for old callers; living motion uses HomeSpider state.
    internal static Point SpiderPoint(double time,double size)=>new(75,Math.Min(size*.4,180));
    internal static Rect PumpkinBounds(Size size)
    {var width=Math.Min(520,Math.Min(size.Width*.38,size.Height*.85));return new(size.Width-width-12,size.Height-width/1.5-112,width,width/1.5);}
    internal bool IsAnimating=>motion.IsEnabled;
    internal SeasonalCorners()
    {
        Loaded+=(_,_)=>{host=Window.GetWindow(this);if(host!=null)host.StateChanged+=HostStateChanged;Configure();Reconcile();};
        Unloaded+=(_,_)=>{motion.Stop();clock.Stop();if(host!=null)host.StateChanged-=HostStateChanged;host=null;};
        IsVisibleChanged+=(_,_)=>Reconcile();SizeChanged+=(_,_)=>Configure();
        motion.Tick+=(_,_)=>
        {
            if(!IsVisible || !Motion){Reconcile();return;}
            var now=clock.Elapsed.TotalSeconds;var dt=Math.Clamp(now-previous,0,.1);previous=now;
            if(AdvancedAnimations)Companion.Step(dt,Mouse.GetPosition(this));InvalidateVisual();
        };
    }
    private void HostStateChanged(object? sender,EventArgs args)=>Reconcile();
    private bool Motion=>host?.WindowState!=WindowState.Minimized && Options.SeasonalAnimation && NexaMotion.Allowed(Options) && Theme.SeasonalEnabled && Options.SeasonalDecorations;
    private void Reconcile()
    {if(IsLoaded && IsVisible && Motion){clock.Start();previous=clock.Elapsed.TotalSeconds;motion.Start();}else{motion.Stop();clock.Stop();}}
    private void Configure()
    {
        var blocked=new List<Rect>();var window=Window.GetWindow(this);
        void Visit(DependencyObject node)
        {
            if(node is FrameworkElement button && (button is Button or NexaLogo || button is TextBlock text && !string.IsNullOrWhiteSpace(text.Text)) && button.IsVisible && button.ActualWidth>0)
            {try{var r=button.TransformToVisual(this).TransformBounds(new Rect(button.RenderSize));r.Inflate(48,48);blocked.Add(r);}catch(InvalidOperationException){}}
            for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)Visit(VisualTreeHelper.GetChild(node,i));
        }
        if(window!=null)Visit(window);
        Companion.Configure(RenderSize,blocked);staticArt=null;
    }
    protected override void OnRender(DrawingContext dc)
    {
        if(ActualWidth<=0 || ActualHeight<=0)return;
        if(staticArt==null || artSize!=RenderSize)
        {artSize=RenderSize;staticArt=new DrawingGroup();using(var art=staticArt.Open())DrawStatic(art);staticArt.Freeze();}
        dc.DrawDrawing(staticArt);var time=Motion?clock.Elapsed.TotalSeconds:0;
        NexaPumpkin.DrawLight(dc,PumpkinBounds(RenderSize),time,1.73);
        if(!Companion.HasRoom)return;
        if(AdvancedAnimations && Companion.Candy is {} candy)
        {dc.PushTransform(new TranslateTransform(candy.X-11,candy.Y-11));dc.PushTransform(new ScaleTransform(22/64d,22/64d));dc.DrawDrawing(NexaweenDecorations.Asset(5));dc.Pop();dc.Pop();}
        var bug=Companion.Bug;var point=bug.Position;
        dc.PushTransform(new TranslateTransform(point.X,point.Y));dc.PushTransform(new RotateTransform(bug.Angle));dc.PushTransform(new ScaleTransform(2.7,2.7));
        var appearance=new EcosystemOptions{Appearance=BugAppearance.Halloween,MatchAppearance=false,Adorable=true,AdvancedAnimations=true};
        if(Motion && AdvancedAnimations)
            BugVisuals.Draw(dc,bug,appearance,false,Companion.Age,Companion.Candy??Mouse.GetPosition(this),1,true,false);
        else BugVisuals.Draw(dc,bug,appearance,false);
        dc.Pop();dc.Pop();dc.Pop();
    }
    private void DrawStatic(DrawingContext dc)
    {
        var size=WebSize(RenderSize);var pen=BugVisuals.P("#60FFFFFF",.65);
        for(var i=0;i<9;i++){var a=i*Math.PI/16;dc.DrawLine(pen,new(0,0),new(Math.Cos(a)*size,Math.Sin(a)*size));}
        for(var ring=1;ring<=9;ring++)for(var i=0;i<8;i++)
        {
            var a=i*Math.PI/16;var b=(i+1)*Math.PI/16;var radius=size*ring/10;
            var line=new StreamGeometry();using(var g=line.Open()){g.BeginFigure(new(Math.Cos(a)*radius,Math.Sin(a)*radius),false,false);g.QuadraticBezierTo(new(Math.Cos((a+b)/2)*radius*.96,Math.Sin((a+b)/2)*radius*.96),new(Math.Cos(b)*radius,Math.Sin(b)*radius),true,false);}line.Freeze();dc.DrawGeometry(null,pen,line);
        }
        NexaPumpkin.Draw(dc,PumpkinBounds(RenderSize));
    }
}
