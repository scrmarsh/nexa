using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;
using Aevoria.Desktop;

internal static partial class Program
{
    private static void CheckDPerformanceLive()
    {
        var library=new AevoriaLibrary(Path.Combine(output,"interactive-f3"));var world=GrandWait(library.Create("F3 isolated inspection","living-landscape",1024,576,new(){PopulationBudget=120}));
        var board=new BattlefieldWindow(world.Controller,world,strategic:true){Title="Aevoria F3 inspection — isolated test world"};board.Show();board.Activate();
        File.WriteAllText(Path.Combine("test-output","aevoria-d-perf","f3-live-ready.json"),JsonSerializer.Serialize(new{Output=output,World=world.Info.WorldId}));
        Pump(TimeSpan.FromSeconds(60));CaptureB(board,"f3-live",1);File.WriteAllText(Path.Combine(output,"f3-live.json"),JsonSerializer.Serialize(board.Performance!.Snapshot,new JsonSerializerOptions{WriteIndented=true}));board.Close();
    }
    private sealed class FailingGpu : IAevoriaGpuSource
    {public bool Disposed;public AevoriaGpuReading Read()=>throw new InvalidOperationException("Injected unavailable counter");public void Dispose()=>Disposed=true;}
    private static void CheckDPerformance()
    {
        var frames=new AevoriaFrameWindow();
        Assert(frames.Read(0).Fps==null,"F3 empty frame window has no fabricated FPS or division by zero");
        for(var i=0;i<=60;i++)frames.Record(i/60d,TimeSpan.FromSeconds(i/60d),i);
        var read=frames.Read(1);
        Assert(Math.Abs(read.Fps!.Value-60)<1e-6 && Math.Abs(read.MeanMs!.Value-1000d/60)<1e-6,"F3 rolling FPS and mean interval use real event timestamps");
        Assert(Math.Abs(read.P95Ms!.Value-1000d/60)<1e-6,"F3 p95 frame interval is correct after enough intervals");
        Assert(!frames.Record(.5,TimeSpan.FromSeconds(.5),100) && !frames.Record(1,TimeSpan.FromSeconds(1),100),"F3 duplicate/backward timestamps are ignored");
        Assert(!frames.Record(1.01,TimeSpan.FromSeconds(1.01),60),"F3 compositor callbacks without changed gameplay drawings are excluded");
        Assert(frames.Read(2).Fps==null,"F3 stale paused/idle FPS expires");frames.Reset();
        for(var i=0;i<20;i++)frames.Record(i*.01,TimeSpan.FromSeconds(i*.01),i);
        Assert(frames.Read(.2).Fps==null,"F3 does not publish frame statistics from a tiny startup window");
        Assert(AevoriaResourceSampler.CpuPercent(1,1,8)==12.5 && AevoriaResourceSampler.CpuPercent(8,1,8)==100,"F3 CPU normalizes actual process time across logical processors");
        Assert(AevoriaResourceSampler.CpuPercent(-1,1,8)==0 && AevoriaResourceSampler.CpuPercent(999,1,8)==100 && AevoriaResourceSampler.CpuPercent(1,0,8)==null,"F3 CPU bounds and initialization handle invalid intervals");
        var stats=JsonSerializer.SerializeToElement(PerfStats(new[]{1d,2,3,4}));
        Assert(stats.GetProperty("Median").GetDouble()==2.5 && stats.GetProperty("P95").GetDouble()==4 && stats.GetProperty("Variance").GetDouble()==1.25 && stats.GetProperty("P99").ValueKind==JsonValueKind.Null,"Performance statistics use true median, nearest-rank percentiles and sample-count limits");
        var gpu=AevoriaGpuSource.Aggregate(new[]{("pid_1_luid_a_phys_0_eng_0_engtype_3D",60d),("pid_1_luid_a_phys_0_eng_1_engtype_Copy",50d),("pid_1_luid_b_phys_0_eng_0_engtype_3D",70d)});
        Assert(gpu.Percent==70 && gpu.Adapters==2 && gpu.Engines==3,"F3 multiple GPU engines/adapters use the peak, not an impossible summed percentage");
        Assert(AevoriaGpuSource.Aggregate([]).Percent==null && AevoriaGpuSource.Aggregate(new[]{("missing-adapter",80d)}).Percent==null,"F3 missing adapters/counters display unavailable");
        var failure=new FailingGpu();using(var sampler=new AevoriaResourceSampler(()=>failure))
        {
            Until023(()=>sampler.Latest.Cpu!=null,"F3 independent resource sampler initializes");var r=sampler.Latest;using var process=Process.GetCurrentProcess();process.Refresh();
            Assert(r.WorkingSet>0 && Math.Abs(r.WorkingSet-process.WorkingSet64)<64L*1024*1024,"F3 RAM is an actual process working-set measurement");
            Assert(r.Cpu is >=0 and <=100 && r.ManagedBytes>0,"F3 CPU and managed-memory samples are measured and bounded");
            Assert(r.Gpu.Percent==null && r.Gpu.Reason.Contains("failed"),"F3 GPU polling failures do not crash or become fake utilization");
            sampler.Dispose();Assert(sampler.Completion.Wait(TimeSpan.FromSeconds(5)) && failure.Disposed,"F3 sampler cancellation releases GPU resources");
        }
        using var startupSampler=new AevoriaResourceSampler();Until023(()=>startupSampler.Latest.CapturedAt>0,"F3 cold GPU sampler produces a sample before a graphics window exists");var startupReading=startupSampler.Latest;
        var library=new AevoriaLibrary(Path.Combine(output,"perf-library"));var world=GrandWait(library.Create("F3 verification","living-landscape",1024,576,new(){PopulationBudget=120}));world.Controller.SetPaused(true);
        var board=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Show();board.Activate();Pump(TimeSpan.FromMilliseconds(400));
        Assert(!board.PerformanceVisible && board.Performance is {Sampling:false},"F3 begins hidden with sampling disabled");
        void Key(Key key){board.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(board),Environment.TickCount,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});}
        Key(System.Windows.Input.Key.F3);Pump(TimeSpan.FromMilliseconds(50));Assert(board.PerformanceVisible,"F3 actual keyboard route opens overlay");
        Key(System.Windows.Input.Key.F3);Assert(!board.PerformanceVisible && !board.Performance!.Sampling,"F3 second key press closes overlay and sampling");
        Key(System.Windows.Input.Key.F3);Assert(board.PerformanceVisible,"F3 subsequent presses alternate");
        board.OpenGameMenu();Assert(board.GameMenuOpen && board.PerformanceVisible,"F3 does not replace or break Escape pause menu");board.CloseGameMenu();
        var unrelated=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(board),Environment.TickCount,System.Windows.Input.Key.A){RoutedEvent=Keyboard.PreviewKeyDownEvent};board.RaiseEvent(unrelated);Assert(!unrelated.Handled,"F3 leaves unrelated typing events alone");
        board.Activate();Until023(()=>board.Performance!.Snapshot.Resources.CapturedAt>0,"F3 live overlay receives real resource samples");
        Pump(TimeSpan.FromMilliseconds(1200));
        if(board.Performance!.Snapshot.Resources.Gpu.Percent!=null)Until023(()=>startupSampler.Latest.Gpu.Percent!=null,"F3 GPU sampler started before graphics recovers after GPU instances appear");
        File.WriteAllText(Path.Combine(output,"gpu-startup.json"),JsonSerializer.Serialize(new{BeforeWindow=startupReading,AfterWindow=startupSampler.Latest,Overlay=board.Performance.Snapshot.Resources},new JsonSerializerOptions{WriteIndented=true}));
        startupSampler.Dispose();Assert(startupSampler.Completion.Wait(TimeSpan.FromSeconds(5)),"F3 startup probe releases native counter resources");
        var title=Descendants(board).OfType<TextBlock>().Single(t=>t.Text=="AEVORIA — PERFORMANCE");var panel=(Border)((FrameworkElement)title.Parent).Parent;
        Rect Bounds(FrameworkElement element)=>new(element.TranslatePoint(new Point(),board),element.RenderSize);
        Assert(!Bounds(panel).IntersectsWith(Bounds(board.Minimap!)),"F3 upper-left panel does not obscure minimap");
        board.Width=900;board.Height=600;Pump(TimeSpan.FromMilliseconds(100));Assert(Bounds(panel).Left>=0 && Bounds(panel).Right<=board.ActualWidth && !Bounds(panel).IntersectsWith(Bounds(board.Minimap!)),"F3 remains inside viewport and clear of minimap after resize");
        board.Width=1200;board.Height=800;board.WindowState=WindowState.Maximized;Pump(TimeSpan.FromMilliseconds(100));Assert(board.WindowStyle==WindowStyle.None && board.PerformanceVisible,"F3 works in borderless maximized gameplay");
        board.WindowState=WindowState.Normal;board.Activate();Pump(TimeSpan.FromMilliseconds(100));
        foreach(var dpi in new[]{1d,1.5,2d}){CaptureB(board,"f3-"+dpi,dpi);Assert(panel.ActualWidth>250 && Bounds(panel).Bottom<=board.ActualHeight,"F3 layout remains readable in "+dpi+" capture DPI");}
        var label=Descendants(panel).OfType<TextBlock>().Single(t=>t.Text.Contains("Render FPS"));
        Assert(label.Text.Contains("WPF callbacks") && label.Text.Contains("CPU (Aevoria)") && label.Text.Contains("GPU (Aevoria, peak engine)") && label.Text.Contains("working set"),"F3 source labels distinguish process metrics and do not claim confirmed GPU presents");
        board.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(100));Assert(!board.Performance!.Sampling && board.Performance.Snapshot.Frames.Fps==null,"F3 minimized window suspends monitoring and clears stale FPS");
        board.WindowState=WindowState.Normal;board.Activate();Pump(TimeSpan.FromMilliseconds(200));
        world.Controller.SetPaused(false);Pump(TimeSpan.FromMilliseconds(1400));Assert(board.Performance!.Snapshot.SimulationTicks>0 && board.Performance.Snapshot.SimulationMs>=0,"F3 simulation metrics come from actual controller steps");world.Controller.SetPaused(true);
        var before=JsonSerializer.Serialize(world.Controller.Manager.CaptureWorld(),ArcadeWorldStore.JsonOptions);
        MeasureF3Overhead(board,world);
        Until023(()=>board.Battlefield.TerrainView!.DetailReady,"F3 paused scene finishes background terrain work");Pump(TimeSpan.FromMilliseconds(1400));
        Assert(board.Performance!.Snapshot.Frames.Fps==null,"F3 text refresh does not manufacture FPS in an unchanged paused scene");
        Assert(JsonSerializer.Serialize(world.Controller.Manager.CaptureWorld(),ArcadeWorldStore.JsonOptions)==before,"F3 and camera measurement do not mutate paused simulation, finances or history");
        var telemetry=board.Performance;board.Close();Assert(!telemetry.Enabled && !telemetry.Sampling,"F3 closing gameplay detaches subscriptions");
        var classic=new BattlefieldWindow(world.Controller,world);Assert(classic.Performance==null,"F3 is not installed in traditional Nexa gameplay");classic.Close();
        var weak=ClosedPerformanceWindow(world);for(var i=0;i<3 && weak.IsAlive;i++){Pump(TimeSpan.FromMilliseconds(100));GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();}
        Assert(!weak.IsAlive,"F3 sampling does not retain a closed game window");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]private static WeakReference ClosedPerformanceWindow(LivingWorldSession world)
    {var board=new BattlefieldWindow(world.Controller,world,strategic:true);board.Show();board.Activate();board.TogglePerformance();Pump(TimeSpan.FromMilliseconds(100));var weak=new WeakReference(board);board.Close();if(ReferenceEquals(Application.Current.MainWindow,board))Application.Current.MainWindow=null;return weak;}
    private static object PerfStats(IEnumerable<double> input)
    {
        var samples=input.Order().ToList();if(samples.Count==0)return new{Count=0};var mean=samples.Average();
        double Quantile(double p)=>samples[Math.Clamp((int)Math.Ceiling(samples.Count*p)-1,0,samples.Count-1)];
        var middle=samples.Count/2;return new{Count=samples.Count,Median=samples.Count%2==0?(samples[middle-1]+samples[middle])/2:samples[middle],Mean=mean,P95=Quantile(.95),P99=samples.Count>=100?(double?)Quantile(.99):null,Maximum=samples[^1],Variance=samples.Average(s=>(s-mean)*(s-mean))};
    }
    private static void MeasureF3Overhead(BattlefieldWindow board,LivingWorldSession world)
    {
        var rows=new List<object>();var center=new Point(world.Info.Extent*.5,world.Info.HeightExtent*.5);board.NavigateTo(center);board.ZoomAt(new Point(600,400),600);var camera=board.Battlefield.Camera;
        for(var repeat=0;repeat<2;repeat++)foreach(var visible in new[]{false,true,true,false})
        {
            if(board.PerformanceVisible!=visible)board.TogglePerformance();board.Activate();board.Battlefield.Camera=camera;board.Battlefield.RefreshFrame();Pump(TimeSpan.FromMilliseconds(1200));
            var times=new List<double>();var drawing=new List<double>();var previous=0d;var count=board.Battlefield.RenderedFrames;var watch=Stopwatch.StartNew();TimeSpan stamp=TimeSpan.MinValue;
            EventHandler callback=(_,e)=>{if(e is not RenderingEventArgs args || args.RenderingTime==stamp || board.Battlefield.RenderedFrames==count)return;stamp=args.RenderingTime;count=board.Battlefield.RenderedFrames;var now=watch.Elapsed.TotalMilliseconds;if(previous>0)times.Add(now-previous);previous=now;drawing.Add(board.Battlefield.LastRenderMilliseconds);};
            var step=0;var motion=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(16)};motion.Tick+=(_,_)=>{step++;board.NavigateTo(center+new Vector(Math.Sin(step*.05)*80,Math.Cos(step*.05)*60));};
            var bytes=GC.GetTotalAllocatedBytes();var collections=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)};using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
            CompositionTarget.Rendering+=callback;motion.Start();Pump(TimeSpan.FromSeconds(3));motion.Stop();CompositionTarget.Rendering-=callback;process.Refresh();
            rows.Add(new{repeat,F3Visible=visible,Active=board.IsActive,Seconds=watch.Elapsed.TotalSeconds,Intervals=PerfStats(times),DrawingSubmission=PerfStats(drawing),AllocatedBytes=GC.GetTotalAllocatedBytes()-bytes,Gen0=GC.CollectionCount(0)-collections[0],Gen1=GC.CollectionCount(1)-collections[1],Gen2=GC.CollectionCount(2)-collections[2],CpuPercent=AevoriaResourceSampler.CpuPercent((process.TotalProcessorTime-cpu).TotalSeconds,watch.Elapsed.TotalSeconds,Environment.ProcessorCount),process.WorkingSet64,Telemetry=board.Performance!.Snapshot});
        }
        File.WriteAllText(Path.Combine(output,"f3-overhead.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
        Assert(rows.Count==8,"F3 hidden/visible overhead measured in two preplanned ABBA blocks");
    }
}
