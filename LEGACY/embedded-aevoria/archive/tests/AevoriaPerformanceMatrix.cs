using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;
using Aevoria.Desktop;
internal static partial class Program
{
    // The same probe-only resource collector can instrument frozen pre-D DLLs through an isolated load context.
    // No instrumented replacement of those product DLLs is necessary.
    private sealed class MatrixResources : IDisposable
    {
        private readonly AssemblyLoadContext? context;
        private readonly object? sampler;
        private readonly PropertyInfo? latest;
        internal MatrixResources()
        {
            var path=Environment.GetEnvironmentVariable("AEVORIA_PROBE_TELEMETRY");
            if(string.IsNullOrWhiteSpace(path))return;
            context=new AssemblyLoadContext("Isolated telemetry probe",true);var assembly=context.LoadFromAssemblyPath(Path.GetFullPath(path));
            var type=assembly.GetType("SessionDashboard.AevoriaResourceSampler")!;sampler=type.GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single().Invoke(new object?[]{null});latest=type.GetProperty("Latest",BindingFlags.Instance|BindingFlags.NonPublic);
        }
        internal object? Read()=>latest?.GetValue(sampler);
        public void Dispose(){(sampler as IDisposable)?.Dispose();if(sampler!=null)(sampler.GetType().GetProperty("Completion",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(sampler) as Task)?.Wait(TimeSpan.FromSeconds(5));context?.Unload();}
    }
    private static void CheckDMatrix(bool native,bool focused=false)
    {
        using var resources=new MatrixResources();var rows=new List<object>();var environment=new
        {
            Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,Environment.ProcessorCount,
            Nexa=typeof(BugOverlay).Assembly.Location,Aevoria=typeof(GameMenu).Assembly.Location,TelemetryProbe=Environment.GetEnvironmentVariable("AEVORIA_PROBE_TELEMETRY"),
            NativeRequested=native,FocusedLocal=focused,FixtureSource=Environment.GetEnvironmentVariable("AEVORIA_MATRIX_FIXTURES"),Graphics=RenderOptions.ProcessRenderMode.ToString(),Tier=RenderCapability.Tier>>16,
            CapturePolicy="8 warm-up captures, then 3 preplanned trials of 8 captures per DPI. p99 omitted below 100 samples. Physical monitor DPI is distinct from capture DPI.",
            Seed="living-landscape",PopulationBudget=120,Quality="Balanced",Viewport="1200x800 DIPs",State="Paused initial world; native callback sample uses deterministic camera motion, not simulation FPS.",
            OtherProcesses=Process.GetProcessesByName("Aevoria").Select(p=>{using(p)return new{p.Id,p.ProcessName};}).ToArray()
        };
        File.WriteAllText(Path.Combine(output,"environment.json"),JsonSerializer.Serialize(environment,new JsonSerializerOptions{WriteIndented=true}));
        foreach(var size in GrandGeneration.Sizes.Where(s=>s.Width is 1024 or 2048 or 4096 or 6144))
        {
            var library=new AevoriaLibrary(Path.Combine(output,"matrix-"+size.Width));LivingWorldSession world;
            var fixtures=Environment.GetEnvironmentVariable("AEVORIA_MATRIX_FIXTURES");
            if(string.IsNullOrWhiteSpace(fixtures))world=GrandWait(library.Create("Performance matrix","living-landscape",size.Width,size.Height,new(){PopulationBudget=120}));
            else
            {
                var sourceRoot=Path.GetFullPath(fixtures);if(!sourceRoot.StartsWith(Path.GetFullPath("test-output")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Benchmark fixtures must be isolated under test-output.");
                var source=new AevoriaLibrary(Path.Combine(sourceRoot,"matrix-"+size.Width));var id=source.Worlds().Single().WorldId;
                var imported=GrandWait(library.Import(Path.Combine(LivingWorlds.Root(source.Store),id.ToString("N"))));world=library.Open(imported);Until023(()=>world.PoliticsReady.IsCompleted,"Copied benchmark politics loads");world.PoliticsReady.GetAwaiter().GetResult();
            }
            world.Controller.SetPaused(true);world.Info.Quality=TerrainQuality.Balanced;
            var manager=world.Controller.Manager;var initial=JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions);
            File.WriteAllText(Path.Combine(output,"initial-"+size.Width+".json"),initial);var initialHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(initial)));
            var core=BugManager.RestoreWorld(manager.CaptureWorld());core.WorldPaused=false;var simulation=new List<double>();for(var i=0;i<500;i++){var at=Stopwatch.GetTimestamp();core.Advance(1d/60);if(i>=50)simulation.Add(Stopwatch.GetElapsedTime(at).TotalMilliseconds);}
            var overlay=new BugOverlay(manager){ShowHud=false,StrategicMap=true};overlay.SetWidgetSurfaces([]);overlay.TerrainView=new LivingTerrainView(world,overlay);
            var profiling=typeof(LivingTerrainView).GetField("MeasureDrawing",BindingFlags.Instance|BindingFlags.NonPublic);profiling?.SetValue(overlay.TerrainView,true);
            var stages=typeof(LivingTerrainView).GetField("LastDrawingTimes",BindingFlags.Instance|BindingFlags.NonPublic);
            var host=new Grid{Background=Brushes.Black};host.Children.Add(overlay);var window=new Window{Content=host,Width=1200,Height=800,WindowStyle=WindowStyle.None,ShowActivated=native,Left=native?20:-10000,Top=native?20:-10000,Title="Aevoria isolated performance matrix"};window.Show();if(native)window.Activate();Pump(TimeSpan.FromMilliseconds(200));
            foreach(var lod in focused?new[]{"Local"}:new[]{"World","Regional","Local","Inspection"})
            {
                var fit=Math.Min(1200/world.Info.Extent,800/world.Info.HeightExtent);var zoom=lod switch{"World"=>Math.Min(.08,fit*.96),"Regional"=>.3,"Local"=>.8,_=>1.8};var center=world.Nations!.Nations[0].Capital;
                var camera=new Matrix(zoom,0,0,zoom,600-center.X*zoom,400-center.Y*zoom);overlay.Camera=camera;overlay.RefreshFrame();overlay.UpdateLayout();new RenderTargetBitmap(1200,800,96,96,PixelFormats.Pbgra32).Render(host);
                Until023(()=>overlay.TerrainView.DetailReady,"Matrix complete current camera: "+size.Width+" "+lod);Assert(overlay.MapDetail.ToString()==lod,"Matrix uses matching actual LOD: "+size.Width+" "+lod);
                var nativeIntervals=new List<double>();var nativeDrawing=new List<double>();var delays=new List<double>();var nativeFrames=0;var foregroundFrames=0;var nativeSeconds=0d;bool foreground=false;var hwnd=new WindowInteropHelper(window).Handle;
                if(native)
                {
                    var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(16)};var motion=0;var clock=Stopwatch.StartNew();var lastTick=clock.Elapsed.TotalMilliseconds;
                    timer.Tick+=(_,_)=>{var now=clock.Elapsed.TotalMilliseconds;delays.Add(Math.Max(0,now-lastTick-16));lastTick=now;motion++;var matrix=camera;matrix.Translate(Math.Sin(motion*.05)*30,Math.Cos(motion*.05)*20);overlay.Camera=matrix;overlay.RefreshFrame();};
                    var count=overlay.RenderedFrames;TimeSpan rendering=TimeSpan.MinValue;double previous=0;
                    EventHandler callback=(_,e)=>{if(e is not RenderingEventArgs args || rendering==args.RenderingTime || overlay.RenderedFrames==count)return;rendering=args.RenderingTime;count=overlay.RenderedFrames;nativeFrames++;if(GetForegroundWindow()==hwnd)foregroundFrames++;var at=clock.Elapsed.TotalMilliseconds;if(previous>0)nativeIntervals.Add(at-previous);previous=at;nativeDrawing.Add(overlay.LastRenderMilliseconds);};
                    timer.Start();Pump(TimeSpan.FromSeconds(1));delays.Clear();clock.Restart();lastTick=0;CompositionTarget.Rendering+=callback;Pump(TimeSpan.FromSeconds(3));nativeSeconds=clock.Elapsed.TotalSeconds;CompositionTarget.Rendering-=callback;timer.Stop();foreground=GetForegroundWindow()==new WindowInteropHelper(window).Handle;
                    overlay.Camera=camera;overlay.RefreshFrame();overlay.UpdateLayout();new RenderTargetBitmap(1200,800,96,96,PixelFormats.Pbgra32).Render(host);Until023(()=>overlay.TerrainView.DetailReady,"Matrix detail settles after native motion");
                }
                foreach(var dpi in new[]{1d,1.5,2d})
                {
                    var image=new RenderTargetBitmap((int)(1200*dpi),(int)(800*dpi),96*dpi,96*dpi,PixelFormats.Pbgra32);
                    void Capture(){overlay.RefreshFrame();overlay.UpdateLayout();image.Clear();image.Render(host);}
                    var cold=new List<double>();for(var i=0;i<8;i++){var at=Stopwatch.GetTimestamp();Capture();cold.Add(Stopwatch.GetElapsedTime(at).TotalMilliseconds);}
                    var samples=new List<double>();var drawing=new List<double>();var trialRows=new List<object>();var rasterizations=overlay.SpriteRasterizations;using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;var wall=Stopwatch.StartNew();var bytes=GC.GetTotalAllocatedBytes();var gen0=GC.CollectionCount(0);var gen1=GC.CollectionCount(1);var gen2=GC.CollectionCount(2);
                    for(var trial=0;trial<3;trial++)
                    {
                        var trialSamples=new List<double>();for(var i=0;i<8;i++){var at=Stopwatch.GetTimestamp();Capture();var ms=Stopwatch.GetElapsedTime(at).TotalMilliseconds;trialSamples.Add(ms);samples.Add(ms);drawing.Add(overlay.LastRenderMilliseconds);}trialRows.Add(PerfStats(trialSamples));Pump(TimeSpan.FromMilliseconds(30));
                    }
                    process.Refresh();Assert(overlay.TerrainView.DetailReady,"Matrix captures fully loaded terrain at "+size.Width+" "+lod+" "+dpi);
                    rows.Add(new{size.Width,size.Height,Lod=lod,Zoom=zoom,Units=manager.Bugs.Count,InitialStateHash=initialHash,CaptureDpi=dpi,PhysicalDpi=VisualTreeHelper.GetDpi(window).DpiScaleX,ActualViewport=new{overlay.ActualWidth,overlay.ActualHeight},CaptureWarmup=PerfStats(cold),Warm=PerfStats(samples),Trials=trialRows,DrawingSubmission=PerfStats(drawing),LastTerrainStages=stages?.GetValue(overlay.TerrainView)?.ToString(),CoreAdvanceOnly=PerfStats(simulation),SpriteCacheMisses=overlay.SpriteRasterizations-rasterizations,NativeFrames=nativeFrames,NativeSeconds=nativeSeconds,NativeForegroundFraction=nativeFrames>0?foregroundFrames/(double)nativeFrames:(double?)null,NativeCallbacksPerSecond=nativeSeconds>0?nativeFrames/nativeSeconds:(double?)null,NativeIntervals=PerfStats(nativeIntervals),NativeDrawing=PerfStats(nativeDrawing),DispatcherDelay=PerfStats(delays),NativeForegroundAtEnd=foreground,window.IsActive,ProcessCpu=AevoriaResourceSamplerCpu((process.TotalProcessorTime-cpu).TotalSeconds,wall.Elapsed.TotalSeconds),process.WorkingSet64,process.PrivateMemorySize64,ManagedBytes=GC.GetTotalMemory(false),AllocatedBytes=GC.GetTotalAllocatedBytes()-bytes,Gen0=GC.CollectionCount(0)-gen0,Gen1=GC.CollectionCount(1)-gen1,Gen2=GC.CollectionCount(2)-gen2,ResourceTelemetry=resources.Read(),TerrainCacheBytes=world.Chunks.CachedBytes,Minimap="Not installed in raw renderer fixture; covered separately by F3 and D tests."});
                    File.WriteAllText(Path.Combine(output,"matrix.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"Matrix {size.Width} {lod} {dpi}: median={Percentile(samples,.5):F2}ms p95={Percentile(samples,.95):F2}ms");
                }
            }
            Assert(JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions)==initial,"Matrix rendering leaves original simulation state unchanged");overlay.TerrainView.Dispose();window.Close();
        }
        Console.WriteLine("Matrix artifacts: "+output);
    }
    private static double AevoriaResourceSamplerCpu(double seconds,double elapsed)=>elapsed>0?Math.Clamp(seconds/elapsed/Environment.ProcessorCount*100,0,100):0;
    private static void CheckDScheduler()
    {
        var world=GrandWait(new AevoriaLibrary(Path.Combine(output,"scheduler")).Create("Scheduler probe","living-landscape",1024,576,new(){PopulationBudget=120}));world.Controller.SetPaused(true);
        var board=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Title="Aevoria isolated timer scheduling probe";board.Show();board.Activate();board.ZoomAt(new Point(600,400),600);Pump(TimeSpan.FromSeconds(1));
        var rows=new List<object>();var center=board.CameraCenter;using var resources=new AevoriaResourceSampler();var hwnd=new WindowInteropHelper(board).Handle;
        for(var repeat=0;repeat<2;repeat++)foreach(var request in new[]{false,true,true,false})
        {
            var acquired=request && OperatingSystem.IsWindowsVersionAtLeast(10,0,19041) && timeBeginPeriod(1)==0;
            try
            {
                var ticks=new List<double>();var frames=new List<double>();var tick=0;var foreground=0;var clock=Stopwatch.StartNew();var lastTick=0d;var lastFrame=0d;TimeSpan rendering=TimeSpan.MinValue;var count=board.Battlefield.RenderedFrames;
                var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(16)};
                timer.Tick+=(_,_)=>{var now=clock.Elapsed.TotalMilliseconds;if(lastTick>0)ticks.Add(now-lastTick);lastTick=now;tick++;if(GetForegroundWindow()==hwnd)foreground++;board.NavigateTo(center+new Vector(Math.Sin(tick*.05)*80,Math.Cos(tick*.05)*60));};
                EventHandler callback=(_,e)=>{if(e is not RenderingEventArgs args || args.RenderingTime==rendering || count==board.Battlefield.RenderedFrames)return;rendering=args.RenderingTime;count=board.Battlefield.RenderedFrames;var now=clock.Elapsed.TotalMilliseconds;if(lastFrame>0)frames.Add(now-lastFrame);lastFrame=now;};
                timer.Start();Pump(TimeSpan.FromMilliseconds(500));ticks.Clear();tick=foreground=0;lastTick=lastFrame=0;clock.Restart();CompositionTarget.Rendering+=callback;Pump(TimeSpan.FromSeconds(3));CompositionTarget.Rendering-=callback;timer.Stop();
                rows.Add(new{repeat,Requested1Ms=request,Acquired=acquired,Os=Environment.OSVersion.VersionString,ForegroundTickFraction=tick>0?foreground/(double)tick:0,TickIntervals=PerfStats(ticks),WpfChangedFrameIntervals=PerfStats(frames),Resources=resources.Latest});
            }
            finally{if(acquired)timeEndPeriod(1);}
        }
        File.WriteAllText(Path.Combine(output,"scheduler.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));board.Close();Console.WriteLine("Scheduler artifacts: "+output);
    }
    [DllImport("winmm.dll")]private static extern uint timeBeginPeriod(uint milliseconds);
    [DllImport("winmm.dll")]private static extern uint timeEndPeriod(uint milliseconds);
    [DllImport("user32.dll")]private static extern IntPtr GetForegroundWindow();
}
