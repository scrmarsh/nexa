using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed partial class NexaweenDecorations : IDisposable
{
    private sealed record Actor(Rect Bounds, int Kind, Drawing Art, int Seed, bool Contextual=false)
    {
        internal Guid Id {get;init;}=Guid.NewGuid();
        internal double CandyAmount {get;set;}=1;
        internal double BusyUntil {get;set;}
        internal double ConsumedAt {get;set;}=-1;
        internal double Born {get;set;} = -Seed%7;
        internal double Hold {get;set;} = 5+Seed%6;
    }
    private readonly Random lifecycleRandom = new(102725);
    private List<Rect> backgroundAvoidance = [];
    internal static readonly int[] BackgroundKinds=[5,6,8,13,16,5,6,8,16,6,1,4,20,2,21];
    private readonly List<Actor> actors = [];
    private static readonly Dictionary<int, Drawing> assets = [];
    internal static void DrawPumpkinLight(DrawingContext dc,Rect bounds,double time)
    {
        NexaPumpkin.DrawLight(dc,bounds,time,bounds.X*.013+bounds.Y*.019);
    }
    private readonly DispatcherTimer animation = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly DispatcherTimer candyRespawn=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private double revealStarted=double.NegativeInfinity;
    internal void BeginReveal(){revealStarted=animationOptions.SeasonalAnimation && NexaMotion.Allowed(animationOptions)?elapsed.Elapsed.TotalSeconds:double.NegativeInfinity;InvalidateVisual();}
    private double RevealOpacity(Actor actor)=>!animationOptions.SeasonalAnimation || !NexaMotion.Allowed(animationOptions) || double.IsNegativeInfinity(revealStarted)?1:Math.Clamp((elapsed.Elapsed.TotalSeconds-revealStarted-(actor.Contextual?0:actor.Seed%15*.07))/.5,0,1);
    private NexaOptions animationOptions = new();
    private Rect visibleArea;
    private Window? host;
    private int lastSurprise = -1;
    private Point? cursor;
    internal void SetCursor(Point point){cursor=point;}
    private bool disposed;
    internal int ActorCount => actors.Count;
    internal bool IsAnimating => animation.IsEnabled;
    internal IEnumerable<Point> Anchors => actors.Select(a => new Point(a.Bounds.X + a.Bounds.Width / 2, a.Bounds.Y + a.Bounds.Height / 2));
    internal NexaweenDecorations()
    {
        IsHitTestVisible=false;
        candyRespawn.Tick+=(_,_)=>{ReconcileCandy();if(!actors.Any(a=>a.ConsumedAt>=0))candyRespawn.Stop();};
        IsVisibleChanged+=(_,_)=>{if(IsVisible && actors.Any(a=>a.ConsumedAt>=0))candyRespawn.Start();else if(!IsVisible)candyRespawn.Stop();};
        Unloaded+=(_,_)=>candyRespawn.Stop();
        Loaded+=(_,_)=>{if(actors.Any(a=>a.ConsumedAt>=0))candyRespawn.Start();};
        animation.Tick += Animate;
        IsVisibleChanged += (_, _) => ReconcileAnimation();
        Loaded += (_, _) => { host = Window.GetWindow(this); if (host != null) host.StateChanged += HostStateChanged; ReconcileAnimation(); };
        Unloaded += (_, _) => { animation.Stop(); if (host != null) host.StateChanged -= HostStateChanged; host = null; };
    }
    private void HostStateChanged(object? sender, EventArgs e) => ReconcileAnimation();
    internal static Drawing Asset(int kind)
    {
        if (assets.TryGetValue(kind, out var art)) return art;
        var group = new DrawingGroup(); using (var dc = group.Open()) Draw(dc, kind); group.Freeze(); assets[kind] = group; return group;
    }
    internal void ConfigureAnimation(NexaOptions options, Rect viewport)
    { animationOptions = options; visibleArea = viewport; ReconcileAnimation(); }
    internal void SetViewport(Rect viewport) { visibleArea = viewport; ReconcileAnimation(); }
    private void ReconcileAnimation()
    {
        var active = !disposed && IsLoaded && IsVisible && host?.WindowState != WindowState.Minimized && animationOptions.SeasonalAnimation && NexaMotion.Allowed(animationOptions) && actors.Any(a => visibleArea.IsEmpty || a.Bounds.IntersectsWith(visibleArea));
        if (active) animation.Start(); else animation.Stop(); InvalidateVisual();
    }
    private void Animate(object? sender, EventArgs e)
    {
        if (disposed || !IsVisible || host?.WindowState == WindowState.Minimized || !animationOptions.SeasonalAnimation || !NexaMotion.Allowed(animationOptions)) { animation.Stop(); return; }
        var surprise = (int)(elapsed.Elapsed.TotalSeconds / 53);
        if (surprise != lastSurprise)
        {
            if (lastSurprise >= 0 && animationOptions.SeasonalSounds && host?.IsActive == true) InterfaceAudio.PreviewSeasonal(host is MainWindow main ? main.SettingsState.InterfaceSoundVolume : host is HomeWindow home ? home.Saved.InterfaceSoundVolume : .25);
            lastSurprise = surprise;
        }
        RecycleCandyActors();
        InvalidateVisual();
    }
    private void RecycleCandyActors()
    {
        for(var i=0;i<actors.Count;i++)
        {
            var actor=actors[i];if(actor.Kind is 0 or 1)continue;if(actor.ConsumedAt<0 && (!animationOptions.SeasonalAnimation || !NexaMotion.Allowed(animationOptions)))continue;if(actor.Contextual || elapsed.Elapsed.TotalSeconds<actor.BusyUntil || actor.ConsumedAt>=0 && elapsed.Elapsed.TotalSeconds-actor.ConsumedAt<3 || actor.ConsumedAt<0 && elapsed.Elapsed.TotalSeconds-actor.Born<actor.Hold+2)continue;
            for(var attempt=0;attempt<30;attempt++)
            {
                var size=actor.Bounds.Width;var next=new Rect(lifecycleRandom.NextDouble()*Math.Max(0,Width-size),lifecycleRandom.NextDouble()*Math.Max(0,Height-size),size,size);var safe=next;safe.Inflate(20,20);
                if(backgroundAvoidance.Any(r=>r.IntersectsWith(safe)) || actors.Where((_,index)=>index!=i).Any(other=>other.Bounds.IntersectsWith(safe)))continue;
                actors[i]=actor with {Bounds=next,Id=Guid.NewGuid(),CandyAmount=1,ConsumedAt=-1,BusyUntil=0};actors[i].Born=elapsed.Elapsed.TotalSeconds;actors[i].Hold=5+lifecycleRandom.NextDouble()*5;break;
            }
        }
    }
    private void RenderAnimated(DrawingContext dc)
    {
        var moving=animationOptions.SeasonalAnimation && NexaMotion.Allowed(animationOptions);var time = moving?elapsed.Elapsed.TotalSeconds:0; var particles = 0;
        foreach(var corner in vineCorners)DrawVine(dc,corner.Widget,time,corner.Pumpkin);
        foreach (var actor in actors)
        {
            if(actor.ConsumedAt>=0)
            {
                var age=elapsed.Elapsed.TotalSeconds-actor.ConsumedAt;
                if(age<.3 && animationOptions.SeasonalAnimation && NexaMotion.Allowed(animationOptions))
                {
                    var factor=1-age/.3;dc.PushOpacity(factor);dc.PushTransform(new TranslateTransform(actor.Bounds.X,actor.Bounds.Y-age*24));dc.PushTransform(new ScaleTransform(actor.Bounds.Width/64*factor,actor.Bounds.Height/64*factor));dc.DrawDrawing(actor.Art);dc.Pop();dc.Pop();dc.Pop();
                }
                continue;
            }
            var expanded = actor.Bounds; expanded.Inflate(18, 18); if (!visibleArea.IsEmpty && !expanded.IntersectsWith(visibleArea)) continue;
            var phase = time + actor.Seed * .7; var x = actor.Bounds.X; var y = actor.Bounds.Y;
            var lifecycleAge=time-actor.Born;
            var opacity=!moving || actor.Contextual || actor.Kind is 0 or 1 || elapsed.Elapsed.TotalSeconds<actor.BusyUntil?1:lifecycleAge<1?Math.Clamp(lifecycleAge,0,1):lifecycleAge<=actor.Hold+1?1:Math.Clamp(actor.Hold+2-lifecycleAge,0,1);
            dc.PushOpacity(opacity*RevealOpacity(actor));
            dc.PushTransform(new TranslateTransform(x, y)); dc.PushTransform(new ScaleTransform(actor.Bounds.Width / 64, actor.Bounds.Height / 64));
            var rotation = 0d;
            dc.PushTransform(new RotateTransform(rotation, 32, 42));
            if (actor.Kind == 2) dc.PushOpacity(.78 + Math.Sin(phase * .6) * .08);
            dc.PushTransform(new ScaleTransform(Math.Max(.25,Math.Sqrt(actor.CandyAmount)),Math.Max(.25,Math.Sqrt(actor.CandyAmount)),32,32));dc.DrawDrawing(actor.Art);dc.Pop();
            if (actor.Kind == 2) dc.Pop();
            if (actor.Kind == 1)
            {
                NexaPumpkin.DrawLight(dc,new Rect(0,0,64,64),moving?time:0,actor.Seed*.73);
            }
            if (actor.Kind is 4 or 14)
            {
                dc.PushOpacity(.12 + Math.Sin(phase * 1.4) * .025); dc.DrawEllipse(B("#84CC16"), null, new(32, actor.Kind == 4 ? 25 : 51), 28, 11); dc.Pop();
                for (var i = 0; i < 3 && particles < 32; i++, particles++)
                {
                    var rise = (phase * .35 + i * .31) % 1; var center = new Point(22 + i * 9 + Math.Sin(phase + i), (actor.Kind == 4 ? 23 : 48) - rise * 24);
                    dc.PushOpacity((1 - rise) * .7); dc.DrawEllipse(null, P("#D9F99D", .8), center, 1.7 + rise, 1.7 + rise); dc.Pop();
                    if (rise > .88) { dc.PushOpacity((1 - rise) * 2); dc.DrawEllipse(null, P("#84CC16", .5), center, 3 + (rise - .88) * 12, 1.2); dc.Pop(); }
                }
                if (actor.Kind == 4)
                    for (var i = 0; i < 2; i++) dc.DrawEllipse(B("#65A30D"), null, new(16 + i * 30, 30 + (Math.Sin(phase * .7 + i) + 1) * 5), 1.6, 3);
            }
            if (actor.Kind == 3)
                for (var i = 0; i < 4 && particles < 32; i++, particles++)
                { var age = (phase * .4 + i * .2) % 1; dc.PushOpacity((1 - age) * .45); dc.DrawEllipse(B(i % 2 == 0 ? "#A855F7" : "#84CC16"), null, new(5 - age * 10, 47 + Math.Sin(phase + i) * 3), 1.1, 1.1); dc.Pop(); }
            if (actor.Kind == 2 && animationOptions.SpookySurprises && ((int)(time / 53) + actor.Seed) % 7 == 0)
            {
                dc.PushOpacity(.12 + (Math.Sin(phase * .7) + 1) * .06); dc.DrawEllipse(B("#D9F99D"), null, new(32, 31), 27, 27); dc.Pop();
            }
            dc.Pop(); dc.Pop(); dc.Pop(); dc.Pop();
        }
    }
    protected override void OnRender(DrawingContext dc)
    { RenderAnimated(dc); }
    public void Dispose()
    { if (disposed) return; disposed = true; candyRespawn.Stop(); animation.Stop(); animation.Tick -= Animate; elapsed.Stop(); if (host != null) host.StateChanged -= HostStateChanged; host = null; actors.Clear(); }
}
