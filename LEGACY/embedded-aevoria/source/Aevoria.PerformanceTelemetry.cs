using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;

internal sealed record AevoriaFrameReading(double? Fps,double? MeanMs,double? P95Ms,int Samples,string State);
internal sealed class AevoriaFrameWindow
{
    private readonly Queue<double> times=new();
    private TimeSpan rendering=TimeSpan.MinValue;
    private long frame=-1;
    private double last=-1;
    internal void Reset(){times.Clear();rendering=TimeSpan.MinValue;frame=-1;last=-1;}
    internal bool Record(double now,TimeSpan renderingTime,long drawnFrames)
    {
        if(!double.IsFinite(now) || now<0 || now<=last || renderingTime<=rendering)return false;
        rendering=renderingTime;last=now;
        if(frame<0){frame=drawnFrames;return false;}
        if(drawnFrames<=frame)return false;frame=drawnFrames;times.Enqueue(now);while(times.Count>1024 || times.Count>0 && now-times.Peek()>1)times.Dequeue();return true;
    }
    internal AevoriaFrameReading Read(double now)
    {
        while(times.Count>0 && now-times.Peek()>1)times.Dequeue();
        if(times.Count==0 || now-times.Last()>.5)return new(null,null,null,0,"Idle: no changed world frames");
        if(times.Count<2)return new(null,null,null,times.Count,"Warming up");
        var samples=times.ToArray();var elapsed=samples[^1]-samples[0];if(elapsed<.5)return new(null,null,null,samples.Length,"Warming up");
        var intervals=new double[samples.Length-1];for(var i=1;i<samples.Length;i++)intervals[i-1]=(samples[i]-samples[i-1])*1000;
        Array.Sort(intervals);return new((samples.Length-1)/elapsed,elapsed*1000/(samples.Length-1),intervals.Length>=20?intervals[(int)Math.Ceiling(intervals.Length*.95)-1]:null,samples.Length,"Active");
    }
}
internal sealed record AevoriaPerformanceSnapshot(AevoriaFrameReading Frames,AevoriaResources Resources,double SimulationTicks,double SimulationMs,double DispatcherDelayMs,bool Active);

internal sealed class AevoriaPerformanceTelemetry : IDisposable
{
    private readonly Window window;
    private readonly BugOverlay overlay;
    private readonly ArcadeWorldController world;
    private readonly DispatcherTimer refresh;
    private readonly AevoriaFrameWindow frames=new();
    private readonly Queue<double> steps=new();
    private AevoriaResourceSampler? resources;
    private bool enabled,active,disposed;
    private double lastRefresh,simulationMs;
    private long stepStarted;
    internal bool Enabled=>enabled;
    internal bool Sampling=>active;
    internal AevoriaPerformanceSnapshot Snapshot {get;private set;}=new(new(null,null,null,0,"Hidden"),AevoriaResources.Empty,0,0,0,false);
    internal event Action? Changed;
    internal AevoriaPerformanceTelemetry(Window window,BugOverlay overlay,ArcadeWorldController world)
    {
        this.window=window;this.overlay=overlay;this.world=world;
        refresh=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(250)};refresh.Tick+=Refresh;
        window.Activated+=Activity;window.Deactivated+=Activity;window.StateChanged+=Activity;window.IsVisibleChanged+=VisibilityChanged;window.Closed+=Closed;
    }
    internal void SetEnabled(bool value){if(disposed)return;enabled=value;UpdateActivity();}
    private void Activity(object? sender,EventArgs e)=>UpdateActivity();
    private void VisibilityChanged(object sender,DependencyPropertyChangedEventArgs e)=>UpdateActivity();
    private void Closed(object? sender,EventArgs e)=>Dispose();
    private static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    private void UpdateActivity()
    {
        var should=enabled && window.IsVisible && window.IsActive && window.WindowState!=WindowState.Minimized && !disposed;
        if(should==active)return;active=should;
        if(overlay.TerrainView!=null)overlay.TerrainView.MeasureDrawing=active;
        frames.Reset();steps.Clear();
        if(active)
        {
            lastRefresh=Now;resources=new();CompositionTarget.Rendering+=Rendering;world.BeforeStep+=BeforeStep;world.AfterStep+=AfterStep;refresh.Start();
            Snapshot=new(new(null,null,null,0,"Warming up"),AevoriaResources.Empty,0,0,0,true);
        }
        else
        {
            refresh.Stop();CompositionTarget.Rendering-=Rendering;world.BeforeStep-=BeforeStep;world.AfterStep-=AfterStep;resources?.Dispose();resources=null;
            Snapshot=new(new(null,null,null,0,enabled?"Inactive: sampling suspended":"Hidden"),AevoriaResources.Empty,0,0,0,false);
        }
        Changed?.Invoke();
    }
    private void Rendering(object? sender,EventArgs e)
    {if(active && e is RenderingEventArgs args)frames.Record(Now,args.RenderingTime,overlay.RenderedFrames);}
    private void BeforeStep(BugManager manager)=>stepStarted=Stopwatch.GetTimestamp();
    private void AfterStep(BugManager manager,double dt)
    {simulationMs=Stopwatch.GetElapsedTime(stepStarted).TotalMilliseconds;steps.Enqueue(Now);while(steps.Count>1024)steps.Dequeue();}
    private void Refresh(object? sender,EventArgs e)
    {
        if(!active)return;var now=Now;while(steps.Count>0 && now-steps.Peek()>1)steps.Dequeue();
        Snapshot=new(frames.Read(now),resources?.Latest??AevoriaResources.Empty,steps.Count,world.Manager.WorldPaused?0:simulationMs,Math.Max(0,(now-lastRefresh)*1000-250),true);lastRefresh=now;Changed?.Invoke();
    }
    public void Dispose()
    {
        if(disposed)return;enabled=false;UpdateActivity();disposed=true;refresh.Tick-=Refresh;
        window.Activated-=Activity;window.Deactivated-=Activity;window.StateChanged-=Activity;window.IsVisibleChanged-=VisibilityChanged;window.Closed-=Closed;Changed=null;
    }
}
