using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed partial class NexaweenDecorations : IDisposable
{
    private sealed record Actor(Rect Bounds, int Kind, Drawing Art, int Seed);
    private readonly List<Actor> actors = [];
    private static readonly Dictionary<int, Drawing> assets = [];
    private readonly DispatcherTimer animation = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
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
        animation.Tick += Animate;
        IsVisibleChanged += (_, _) => ReconcileAnimation();
        Loaded += (_, _) => { host = Window.GetWindow(this); if (host != null) host.StateChanged += HostStateChanged; ReconcileAnimation(); };
        Unloaded += (_, _) => { animation.Stop(); if (host != null) host.StateChanged -= HostStateChanged; host = null; };
    }
    private void HostStateChanged(object? sender, EventArgs e) => ReconcileAnimation();
    private static Drawing Asset(int kind)
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
        InvalidateVisual();
    }
    private void RenderAnimated(DrawingContext dc)
    {
        var time = elapsed.Elapsed.TotalSeconds; var particles = 0;
        foreach(var corner in vineCorners)DrawVine(dc,corner,time);
        foreach (var actor in actors)
        {
            var expanded = actor.Bounds; expanded.Inflate(18, 18); if (!visibleArea.IsEmpty && !expanded.IntersectsWith(visibleArea)) continue;
            var phase = time + actor.Seed * .7; var x = actor.Bounds.X; var y = actor.Bounds.Y;
            if (actor.Kind == 2) { x += Math.Sin(phase * .4) * 3; y += Math.Sin(phase * .8) * 5; }
            else if (actor.Kind == 3) { x += Math.Sin(phase * .35) * 11; y += Math.Cos(phase * .7) * 4; }
            else if (actor.Kind == 10) { x += Math.Sin(phase * .3) * 6; y += Math.Cos(phase * .4) * 3; }
            if(actor.Kind==2 && cursor is {} pointer){var away=new Point(x+actor.Bounds.Width/2,y+actor.Bounds.Height/2)-pointer;if(away.Length>1 && away.Length<110){away.Normalize();x+=away.X*5;y+=away.Y*5;}}
            dc.PushTransform(new TranslateTransform(x, y)); dc.PushTransform(new ScaleTransform(actor.Bounds.Width / 64, actor.Bounds.Height / 64));
            var rotation = actor.Kind is 0 or 1 ? Math.Sin(phase * .65) * 1.5 : actor.Kind == 10 ? Math.Sin(phase * .4) * 7 : 0;
            dc.PushTransform(new RotateTransform(rotation, 32, 42));
            if (actor.Kind == 2) dc.PushOpacity(.78 + Math.Sin(phase * .6) * .08);
            dc.DrawDrawing(actor.Art);
            if (actor.Kind == 2) dc.Pop();
            if (actor.Kind == 1)
            {
                dc.PushOpacity(.2 + Math.Sin(phase * 2) * .05);
                dc.DrawEllipse(B("#BEF264"), null, new(32, 37), 17, 13); dc.Pop();
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
            dc.Pop(); dc.Pop(); dc.Pop();
        }
    }
    protected override void OnRender(DrawingContext dc)
    { if (animationOptions.SeasonalAnimation && NexaMotion.Allowed(animationOptions) && IsVisible) RenderAnimated(dc); else dc.DrawDrawing(drawing); }
    public void Dispose()
    { if (disposed) return; disposed = true; animation.Stop(); animation.Tick -= Animate; elapsed.Stop(); if (host != null) host.StateChanged -= HostStateChanged; host = null; actors.Clear(); }
}
