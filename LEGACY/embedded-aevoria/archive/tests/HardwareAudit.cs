using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    // Opt-in investigation only. No production preferences, save or graphics settings change.
    private static void CheckHardwareAudit(bool softwareOnly,bool noWebs,bool noTerritory,bool foreground,bool linearScaling,bool timerPaced,bool dashboard,bool classic)
    {
        var warnings=new List<string>();
        object Memory()
        {
            using var process=Process.GetCurrentProcess();
            var gc=GC.GetGCMemoryInfo();
            return new {WorkingSetBytes=process.WorkingSet64,PrivateBytes=process.PrivateMemorySize64,ManagedBytes=GC.GetTotalMemory(false),gc.HeapSizeBytes,gc.FragmentedBytes,gc.TotalAvailableMemoryBytes,AllocatedBytes=GC.GetTotalAllocatedBytes(),Gen0=GC.CollectionCount(0),Gen1=GC.CollectionCount(1),Gen2=GC.CollectionCount(2)};
        }
        var physical=new AuditNative.MemoryStatus {Length=(uint)Marshal.SizeOf<AuditNative.MemoryStatus>()};
        var hasPhysical=AuditNative.GlobalMemoryStatusEx(ref physical);
        var initial=Memory();var adaptersBefore=AuditNative.Adapters(warnings);
        var manager=Infinite021(621);var table=new Dictionary<BugSpecies,SpeciesCombatStats>(manager.BattleOptions.Species!);
        foreach(var key in table.Keys.ToArray())table[key]=table[key] with{MaximumUnits=30};
        manager.Configure(manager.Preferences with{Arcade=manager.BattleOptions with{Species=table,EnableCampaigns=false}});
        for(var i=0;i<5000;i++) {foreach(var b in manager.Bugs)b.Protection=1000;manager.Advance(.033);}
        if(noWebs)manager.Webs.Clear();
        if(noTerritory)manager.Configure(manager.Preferences with{Arcade=manager.BattleOptions with{ShowTerritories=false,ShowTerritoryBorders=false,ShowFactionLabels=false,HighlightContested=false}});
        var overlay=new BugOverlay(manager) {ShowHud=false,Camera=new Matrix(.72,0,0,.72,12,12)};
        if(linearScaling)RenderOptions.SetBitmapScalingMode(overlay,BitmapScalingMode.Linear);
        var otherNexa=Process.GetProcessesByName("Nexa").Select(p=> {using(p)return new {p.Id,CpuSeconds=p.TotalProcessorTime.TotalSeconds,p.WorkingSet64,p.PrivateMemorySize64,Window=p.MainWindowHandle.ToInt64()};}).ToArray();
        var host=new Grid {Background=new SolidColorBrush(Color.FromRgb(8,12,20))};host.Children.Add(overlay);
        var window=new Window {Title="Nexa hardware audit — closes automatically",Content=host,Width=1200,Height=800,Left=30,Top=30,ShowActivated=foreground,ShowInTaskbar=false};
        if(dashboard)
        {
            var store=new StateStore(Path.Combine(output,"audit-dashboard","state.json"));
            store.Save(new AppState {WidgetLayoutInitialized=true,Widgets=[]});store.SaveBugPreferences(classic ? manager.Preferences with{Arcade=manager.BattleOptions with{Mode=ArcadeRuleSet.ClassicRounds}} : manager.Preferences);
            window=new MainWindow(store) {Width=1200,Height=800,Left=30,Top=30,ShowActivated=foreground,ShowInTaskbar=false};
            window.Show();manager=((MainWindow)window).bugsOverlay!.Manager;overlay=((MainWindow)window).bugsOverlay!;
            for(var i=0;i<5000;i++) {foreach(var b in manager.Bugs)b.Protection=1000;manager.Advance(.033);}
        }
        else window.Show();
        if(foreground)window.Activate();
        var source=(HwndSource)PresentationSource.FromVisual(window)!;
        if(softwareOnly)source.CompositionTarget.RenderMode=RenderMode.SoftwareOnly;
        Pump(TimeSpan.FromMilliseconds(700));
        var mainFrames=(window as MainWindow)?.BugFrameCount;
        var rasterizationsAt=overlay.SpriteRasterizations;
        var warmed=Memory();var allocatedAt=GC.GetTotalAllocatedBytes();
        var intervals=new List<double>();var simulation=new List<double>();var submission=new List<double>();
        var start=Stopwatch.StartNew();var previous=0d;var frames=overlay.RenderedFrames;
        TimeSpan lastRendering=TimeSpan.MinValue;var renderCallbacks=0;var previousRender=0d;
        void Update()
        {
            var now=start.Elapsed.TotalMilliseconds;
            var began=Stopwatch.GetTimestamp();manager.Advance(Math.Min(.08,Math.Max(.001,(now-previous)/1000)));simulation.Add(Stopwatch.GetElapsedTime(began).TotalMilliseconds);
            if(noWebs)manager.Webs.Clear();previous=now;overlay.RefreshFrame();
        }
        EventHandler render=(_,args)=>
        {
            var rendering=((RenderingEventArgs)args).RenderingTime;if(rendering==lastRendering)return;lastRendering=rendering;renderCallbacks++;
            var now=start.Elapsed.TotalMilliseconds;if(previousRender>0)intervals.Add(now-previousRender);previousRender=now;
            if(overlay.RenderedFrames>frames)submission.Add(overlay.LastRenderMilliseconds);
            if(!timerPaced && !dashboard)Update();
        };
        var timer=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background) {Interval=TimeSpan.FromMilliseconds(16)};
        timer.Tick+=(_,_)=>Update();
        try {CompositionTarget.Rendering+=render;if(timerPaced && !dashboard)timer.Start();Pump(TimeSpan.FromSeconds(5));}
        finally {timer.Stop();CompositionTarget.Rendering-=render;}
        var duration=start.Elapsed.TotalSeconds;var allocated=GC.GetTotalAllocatedBytes()-allocatedAt;
        var after=Memory();var adaptersDuring=AuditNative.Adapters(warnings);
        // Snapshot costs remain on the dispatcher today, even though the subsequent file write is asynchronous.
        var captureColdBegan=Stopwatch.GetTimestamp();ArcadeWorldSnapshot? snapshot=manager.IsInfinite ? manager.CaptureWorld() : null;var captureColdMs=Stopwatch.GetElapsedTime(captureColdBegan).TotalMilliseconds;
        var captureTimes=new List<double>();var captureAllocatedAt=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<100 && manager.IsInfinite;i++) {var began=Stopwatch.GetTimestamp();snapshot=manager.CaptureWorld();captureTimes.Add(Stopwatch.GetElapsedTime(began).TotalMilliseconds);}
        var captureAllocated=(GC.GetAllocatedBytesForCurrentThread()-captureAllocatedAt)/100;
        var serialized=JsonSerializer.SerializeToUtf8Bytes(snapshot,ArcadeWorldStore.JsonOptions).Length;
        var cache=(System.Collections.IDictionary)typeof(BugOverlay).GetField("battleSprites",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(overlay)!;
        long spriteBytes=0;foreach(var value in cache.Values)if(value is BugArtwork.Sprite sprite)spriteBytes+=(long)sprite.Image.PixelWidth*sprite.Image.PixelHeight*4;
        var result=new
        {
            ForegroundRequested=foreground,WindowIsActive=window.IsActive,WindowVisible=window.IsVisible,RemoteSession=AuditNative.GetSystemMetrics(0x1000)!=0,UserInteractive=Environment.UserInteractive,Runtime=RuntimeInformation.FrameworkDescription,Architecture=RuntimeInformation.ProcessArchitecture.ToString(),Environment.ProcessorCount,
            PhysicalMemoryAvailable=hasPhysical,PhysicalTotalBytes=physical.TotalPhysical,PhysicalAvailableBytes=physical.AvailablePhysical,
            WpfTier=RenderCapability.Tier>>16,ProcessRenderMode=RenderOptions.ProcessRenderMode.ToString(),WindowRenderMode=source.CompositionTarget.RenderMode.ToString(),
            RenderCapability.MaxHardwareTextureSize,SoftwareComparisonRequested=softwareOnly,WebsRemoved=noWebs,TerritoryOverlaysDisabled=noTerritory,
            Dpi=VisualTreeHelper.GetDpi(window).DpiScaleX,Viewport=new {overlay.ActualWidth,overlay.ActualHeight},
            Units=manager.Bugs.Count(BugManager.Alive),Webs=manager.Webs.Count,Regions=manager.Territories.Count,
            DurationSeconds=duration,RenderingCallbacks=renderCallbacks,RenderingCallbacksPerSecond=renderCallbacks/duration,UpdateCount=simulation.Count,Dashboard=dashboard,RuleSet=manager.BattleOptions.Mode.ToString(),MainWindowFrameCount=(window as MainWindow)?.BugFrameCount-mainFrames,FrameSource=dashboard ? "MainWindow" : timerPaced ? "DispatcherTimer16ms" : "CompositionTarget.Rendering",BitmapScaling=RenderOptions.GetBitmapScalingMode(overlay).ToString(),OtherNexaProcesses=otherNexa,OverlayRenderCalls=overlay.RenderedFrames-frames,
            CallbackIntervalP95Ms=intervals.Count>0 ? Percentile(intervals,.95) : (double?)null,
            SimulationP95Ms=simulation.Count>0 ? Percentile(simulation,.95) : (double?)null,
            DrawingSubmissionP95Ms=submission.Count>0 ? Percentile(submission,.95) : (double?)null,
            AllocatedBytesDuringSample=allocated,AllocatedBytesPerSecond=allocated/duration,
            SpriteRasterizationsDuringSample=overlay.SpriteRasterizations-rasterizationsAt,CachedSpriteCount=cache.Count,CachedSpriteRawPixelBytes=spriteBytes,
            SnapshotCaptureColdMs=captureColdMs,SnapshotCaptureWarmSamples=captureTimes.Count,SnapshotCaptureP95Ms=captureTimes.Count>0 ? Percentile(captureTimes,.95) : (double?)null,SnapshotCaptureAllocatedBytesPerCapture=captureAllocated,SerializedSnapshotBytes=serialized,
            InitialMemory=initial,WarmedMemory=warmed,AfterMemory=after,AdaptersBefore=adaptersBefore,AdaptersDuring=adaptersDuring,Warnings=warnings,
            MeasurementLimits="Composition callbacks and DrawingContext submission do not measure GPU execution or displayed/presented frames. Adapter usage is this diagnostic process, not other Nexa instances. No GPU timing or ETW trace collected. Native/cache allocations are not all represented by managed GC counts."
        };
        window.Close();var file=Path.Combine(output,softwareOnly ? "hardware-software.json" : "hardware-default.json");
        File.WriteAllText(file,JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
        Console.WriteLine(File.ReadAllText(file));
    }
}

internal static class AuditNative
{
    [StructLayout(LayoutKind.Sequential)] internal struct MemoryStatus
    {
        public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtendedVirtual;
    }
    [DllImport("user32.dll")] internal static extern int GetSystemMetrics(int index);
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)] internal static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    [DllImport("dxgi.dll",ExactSpelling=true)] private static extern int CreateDXGIFactory1(ref Guid iid,out IntPtr factory);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int EnumAdapter(IntPtr self,uint index,out IntPtr adapter);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int GetDescription(IntPtr self,out AdapterDescription description);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int QueryMemory(IntPtr self,uint node,int segment,out VideoMemory memory);
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] private struct AdapterDescription
    {
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=128)] public string Description;
        public uint VendorId,DeviceId,SubSysId,Revision;public UIntPtr DedicatedVideoMemory,DedicatedSystemMemory,SharedSystemMemory;public uint LuidLow;public int LuidHigh;public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] private struct VideoMemory {public ulong Budget,CurrentUsage,AvailableForReservation,CurrentReservation;}
    private static T Method<T>(IntPtr instance,int slot) where T:Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance),slot*IntPtr.Size));
    internal static List<object> Adapters(List<string> warnings)
    {
        var result=new List<object>();IntPtr factory=IntPtr.Zero;
        try
        {
            var iid=new Guid("770aae78-f26f-4dba-a829-253c83d1b387");Marshal.ThrowExceptionForHR(CreateDXGIFactory1(ref iid,out factory));
            for(uint index=0;index<16;index++)
            {
                var hr=Method<EnumAdapter>(factory,12)(factory,index,out var adapter);if(hr==unchecked((int)0x887A0002))break;Marshal.ThrowExceptionForHR(hr);
                try
                {
                    Marshal.ThrowExceptionForHR(Method<GetDescription>(adapter,10)(adapter,out var desc));
                    object? local=null,nonLocal=null;var adapter3Id=new Guid("645967a4-1392-4310-a798-8053ce3e93fd");
                    if(Marshal.QueryInterface(adapter,in adapter3Id,out var adapter3)>=0)
                    {
                        try
                        {
                            object? Read(int segment) {if(Method<QueryMemory>(adapter3,14)(adapter3,0,segment,out var memory)<0)return null;return new {memory.Budget,memory.CurrentUsage,memory.AvailableForReservation,memory.CurrentReservation};}
                            local=Read(0);nonLocal=Read(1);
                        }
                        finally {Marshal.Release(adapter3);}
                    }
                    result.Add(new {Index=index,desc.Description,desc.VendorId,desc.DeviceId,DedicatedVideoMemoryBytes=desc.DedicatedVideoMemory.ToUInt64(),DedicatedSystemMemoryBytes=desc.DedicatedSystemMemory.ToUInt64(),SharedSystemMemoryBytes=desc.SharedSystemMemory.ToUInt64(),SoftwareAdapter=(desc.Flags&2)!=0,Luid=$"{desc.LuidHigh:X8}:{desc.LuidLow:X8}",LocalMemory=local,NonLocalMemory=nonLocal});
                }
                finally {Marshal.Release(adapter);}
            }
        }
        catch(Exception ex) when(ex is ExternalException or DllNotFoundException or EntryPointNotFoundException) {warnings.Add("DXGI inventory unavailable: "+ex.Message);}
        finally {if(factory!=IntPtr.Zero)Marshal.Release(factory);}
        return result;
    }
}


