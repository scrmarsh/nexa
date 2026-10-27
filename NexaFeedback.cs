using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed class NexaFeedback : FrameworkElement, IDisposable
{
    private sealed record FeedbackEffect(Rect Bounds, double Started, string Kind, FormattedText? Text, int Seed);
    private readonly List<FeedbackEffect> effects = [];
    private readonly HashSet<Guid> observed = [];
    private readonly DispatcherTimer tick = new(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1d / 30) };
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Func<NexaOptions> options;
    private readonly Func<Guid?, Rect?> locate;
    private readonly Func<bool> visible;
    private Guid sessionId;
    private DateTimeOffset? frenzyUntil;
    private Rect? frenzyBounds;
    private bool disposed;
    private double edgeStarted=-10;
    private bool edgeNegative;
    internal int EffectCount => effects.Count;
    internal int ParticleCount => Math.Min(72, effects.Count * 9);
    internal bool IsAnimating => tick.IsEnabled;
    internal NexaFeedback(Func<NexaOptions> options, Func<Guid?, Rect?> locate, Func<bool> visible)
    {
        this.options = options; this.locate = locate; this.visible = visible; IsHitTestVisible = false; ClipToBounds = true;
        tick.Tick += Tick;
    }
    internal void Observe(DashboardSession session, IEnumerable<ProductivityEvent> events)
    {
        if (sessionId != session.SessionId) { sessionId = session.SessionId; observed.Clear(); foreach (var e in events) observed.Add(e.Id); return; }
        foreach (var item in events)
        {
            if (!observed.Add(item.Id)) continue;
            if (item.BasePoints + item.BonusPoints == 0) continue;
            edgeNegative=item.BasePoints+item.BonusPoints<0;edgeStarted=clock.Elapsed.TotalSeconds;
            var kind = edgeNegative?"negative":item.Category == "Task completed" ? "task" : item.Category == "Timer" ? "timer" : item.Category == "Notes" ? "note" : "points";
            Show(kind, locate(item.WidgetId) ?? new Rect(Math.Max(0, ActualWidth - 240), Math.Max(0, ActualHeight - 180), 180, 60), (item.BasePoints+item.BonusPoints>0?"+":"") + (item.BasePoints + item.BonusPoints));
        }
    }
    internal void Show(string kind, Rect bounds, string? label = null)
    {
        if (disposed || !NexaMotion.Allowed(options()) || !options().CelebrationEffects || !visible()) return;
        var text = label == null ? null : new FormattedText(label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), 17, BugVisuals.B(kind=="negative"?"#FCA5A5":Theme.SeasonalEnabled ? "#D9F99D" : "#C7F5DC"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        if (effects.Count >= 8) effects.RemoveAt(0);
        effects.Add(new(bounds, clock.Elapsed.TotalSeconds, kind, text, effects.Count * 31 + kind.Length)); Start();
    }
    internal void Frenzy(DateTimeOffset? until, Rect? bounds)
    { frenzyUntil = until; frenzyBounds = bounds; if (until > DateTimeOffset.UtcNow && bounds != null) Start(); }
    internal void ReconcileOptions()
    { if (!NexaMotion.Allowed(options()) || !options().CelebrationEffects) { effects.Clear(); tick.Stop(); InvalidateVisual(); } }
    private void Start()
    { if (!disposed && NexaMotion.Allowed(options()) && options().CelebrationEffects && visible()) { tick.Start(); InvalidateVisual(); } }
    private void Tick(object? sender, EventArgs e)
    {
        effects.RemoveAll(x => clock.Elapsed.TotalSeconds - x.Started > 1.25);
        if (!visible() || !NexaMotion.Allowed(options()) || !options().CelebrationEffects) { effects.Clear(); tick.Stop(); InvalidateVisual(); return; }
        if (effects.Count == 0 && clock.Elapsed.TotalSeconds-edgeStarted>1.2 && !(frenzyUntil > DateTimeOffset.UtcNow && frenzyBounds != null)) tick.Stop();
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        if (!NexaMotion.Allowed(options()) || !options().CelebrationEffects) return;
        var time = clock.Elapsed.TotalSeconds; var green = Theme.SeasonalEnabled ? "#84CC16" : "#FB923C";
        var ageOfEdge=time-edgeStarted;
        if(ageOfEdge>=0 && ageOfEdge<1.2 && ActualWidth>16 && ActualHeight>16)
        {
            var opacity=Math.Sin(ageOfEdge/1.2*Math.PI)*.55;dc.PushOpacity(opacity);
            var edge=edgeNegative?"#EF4444":"#22C55E";var perimeter=new Rect(5,5,ActualWidth-10,ActualHeight-10);
            dc.DrawRoundedRectangle(null,BugVisuals.P(edge,6),perimeter,9,9);dc.Pop();
        }
        var particles = 0;
        foreach (var effect in effects)
        {
            var age = Math.Clamp((time - effect.Started) / 1.25, 0, 1); var center = new Point(effect.Bounds.Right - 24, effect.Bounds.Top + 28);
            var opacity = (1 - age) * .6; dc.PushOpacity(opacity);
            dc.DrawRoundedRectangle(null, BugVisuals.P(effect.Kind=="negative"?"#EF4444":green, 1.5), effect.Bounds, 12, 12);
            if (effect.Kind is "task" or "timer" or "milestone")
            {
                for (var i = 0; i < 9 && particles < 72; i++, particles++)
                {
                    var angle = i * Math.PI * 2 / 9 + effect.Seed;
                    var p = center + new Vector(Math.Cos(angle) * age * 38, Math.Sin(angle) * age * 28 + age * age * 20);
                    dc.DrawEllipse(BugVisuals.B(i % 2 == 0 ? green : Theme.SeasonalEnabled ? "#A855F7" : "#E5C6FF"), null, p, 1.5 * (1 - age), 1.5 * (1 - age));
                }
            }
            if (effect.Text != null) dc.DrawText(effect.Text, center + new Vector(-effect.Text.Width / 2, -12 - age * 22));
            dc.Pop();
        }
        if (frenzyUntil > DateTimeOffset.UtcNow && frenzyBounds is { } frame)
        {
            dc.PushOpacity(.25 + Math.Sin(time * 2) * .05); dc.DrawRoundedRectangle(null, BugVisuals.P(green, 2), frame, 12, 12); dc.Pop();
            for (var i = 0; i < 16; i++)
            {
                var x=i<8?frame.Left+16+i*Math.Max(1,(frame.Width-32)/7):i<12?frame.Left+8:frame.Right-8;
                var y=i<8?frame.Bottom-10:frame.Top+25+(i%4)*Math.Max(1,(frame.Height-45)/3);
                var height = 9 + Math.Sin(time * 3 + i * 1.4) * 3;
                var shape = new StreamGeometry(); using (var g = shape.Open()) { g.BeginFigure(new(x - 3, y), true, true); g.BezierTo(new(x - 7, y - 4), new(x - 1, y - height), new(x + 1, y - height - 2), true, false); g.BezierTo(new(x, y - 5), new(x + 8, y - 3), new(x + 3, y), true, false); }
                dc.PushOpacity(.55); dc.DrawGeometry(BugVisuals.B(Theme.SeasonalEnabled && i%4==0?"#F97316":green), null, shape); dc.Pop();
            }
        }
    }
    public void Dispose()
    { if (disposed) return; disposed = true; tick.Stop(); tick.Tick -= Tick; effects.Clear(); observed.Clear(); clock.Stop(); }
}
