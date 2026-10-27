using System.Diagnostics;
using System.Runtime.InteropServices;
namespace SessionDashboard;

internal sealed record AevoriaGpuReading(double? Percent,string Reason,int Adapters=0,int Engines=0);
internal interface IAevoriaGpuSource : IDisposable { AevoriaGpuReading Read(); }

// Windows PDH, scoped to this process. Engines/adapters run concurrently: never add their percentages.
internal sealed class AevoriaGpuSource : IAevoriaGpuSource
{
    private IntPtr query,counter,buffer;
    private uint capacity;
    private readonly string prefix=$"pid_{Environment.ProcessId}_";
    private readonly string? unavailable;
    internal AevoriaGpuSource()
    {
        try
        {
            var status=PdhOpenQueryW(null,UIntPtr.Zero,out query);
            if(status==0)status=PdhAddEnglishCounterW(query,@"\GPU Engine("+prefix+@"*)\Utilization Percentage",UIntPtr.Zero,out counter);
            if(status==0){var prime=PdhCollectQueryData(query);if(prime!=0x800007D5)status=prime;} // NO_DATA before WPF creates a GPU instance is transient.
            if(status!=0)unavailable=$"Windows GPU Engine counters unavailable (0x{status:X8}).";
        }
        catch(Exception ex) when(ex is DllNotFoundException or EntryPointNotFoundException or ExternalException){unavailable="Windows GPU counters unavailable: "+ex.GetType().Name;}
    }
    public AevoriaGpuReading Read()
    {
        if(unavailable!=null)return new(null,unavailable);
        var status=PdhCollectQueryData(query);if(status!=0)return new(null,status==0x800007D5?"No process GPU instances yet; sampling will retry.":$"GPU counter collection: 0x{status:X8}");
        uint bytes=0;status=PdhGetFormattedCounterArrayW(counter,0x200,ref bytes,out _,IntPtr.Zero);
        if(status!=0x800007D2 || bytes==0 || bytes>4*1024*1024)return new(null,$"GPU counter data unavailable (0x{status:X8}).");
        if(bytes>capacity){if(buffer!=IntPtr.Zero)Marshal.FreeHGlobal(buffer);buffer=Marshal.AllocHGlobal((int)bytes);capacity=bytes;}
        bytes=capacity;status=PdhGetFormattedCounterArrayW(counter,0x200,ref bytes,out var count,buffer);
        if(status!=0 || count>16384 || (long)count*Marshal.SizeOf<Item>()>bytes)return new(null,$"GPU counter array unavailable (0x{status:X8}).");
        var engines=new List<(string Name,double Value)>();var size=Marshal.SizeOf<Item>();
        for(var i=0;i<count;i++)
        {
            var item=Marshal.PtrToStructure<Item>(buffer+(int)i*size);if(item.Value.Status>1 || !double.IsFinite(item.Value.Number))continue;
            var name=Marshal.PtrToStringUni(item.Name);if(name?.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)==true)engines.Add((name,item.Value.Number));
        }
        return Aggregate(engines);
    }
    internal static AevoriaGpuReading Aggregate(IEnumerable<(string Name,double Value)> engines)
    {
        double? peak=null;var adapters=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var count=0;
        foreach(var (name,value) in engines)
        {
            var end=name.IndexOf("_eng_",StringComparison.OrdinalIgnoreCase);var luid=name.IndexOf("_luid_",StringComparison.OrdinalIgnoreCase);
            if(luid<0 || end<=luid || !double.IsFinite(value) || value<0)continue;
            adapters.Add(name[luid..end]);count++;peak=Math.Max(peak??0,Math.Clamp(value,0,100));
        }
        return peak==null?new(null,"No valid GPU Engine instances for this process; adapter/driver counters may be unavailable."):
            new(peak,"Windows PDH process GPU Engine utilization: busiest valid engine across all observed adapters; not a sum, not whole-system GPU usage.",adapters.Count,count);
    }
    public void Dispose(){if(query!=IntPtr.Zero){PdhCloseQuery(query);query=IntPtr.Zero;}if(buffer!=IntPtr.Zero){Marshal.FreeHGlobal(buffer);buffer=IntPtr.Zero;}counter=IntPtr.Zero;capacity=0;}
    [StructLayout(LayoutKind.Explicit,Size=16)] private struct Value { [FieldOffset(0)]public uint Status;[FieldOffset(8)]public double Number; }
    [StructLayout(LayoutKind.Sequential)] private struct Item { public IntPtr Name;public Value Value; }
    [DllImport("pdh.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint PdhOpenQueryW(string? source,UIntPtr data,out IntPtr query);
    [DllImport("pdh.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint PdhAddEnglishCounterW(IntPtr query,string path,UIntPtr data,out IntPtr counter);
    [DllImport("pdh.dll",ExactSpelling=true)]private static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll",CharSet=CharSet.Unicode,ExactSpelling=true)]private static extern uint PdhGetFormattedCounterArrayW(IntPtr counter,uint format,ref uint bytes,out uint count,IntPtr buffer);
    [DllImport("pdh.dll",ExactSpelling=true)]private static extern uint PdhCloseQuery(IntPtr query);
}

internal sealed record AevoriaResources(double? Cpu,long WorkingSet,long PrivateBytes,long ManagedBytes,double? SystemRam,
    AevoriaGpuReading Gpu,double SampleMilliseconds,long AllocatedBytes,int Gen0,int Gen1,int Gen2,double CapturedAt)
{
    internal static readonly AevoriaResources Empty=new(null,0,0,0,null,new(null,"Sampling has not started."),0,0,0,0,0,0);
}

// No Window/Dispatcher/game references cross into the sampling task. PDH can block; it never runs on the UI thread.
internal sealed class AevoriaResourceSampler : IDisposable
{
    private readonly CancellationTokenSource stopping=new();
    private AevoriaResources latest=AevoriaResources.Empty;
    internal AevoriaResources Latest=>Volatile.Read(ref latest);
    internal Task Completion {get;}
    internal AevoriaResourceSampler(Func<IAevoriaGpuSource>? source=null)=>Completion=Task.Run(()=>Run(source??(()=>new AevoriaGpuSource())));
    private async Task Run(Func<IAevoriaGpuSource> source)
    {
        IAevoriaGpuSource? gpu=null;
        try
        {
            using var process=Process.GetCurrentProcess();TimeSpan? previousCpu=null;double previousTime=0;
            try{gpu=source();}catch(Exception ex) when(ex is not OutOfMemoryException){Volatile.Write(ref latest,latest with{Gpu=new(null,"GPU source initialization failed: "+ex.GetType().Name)});}
            while(!stopping.IsCancellationRequested)
            {
                var started=Stopwatch.GetTimestamp();var at=started/(double)Stopwatch.Frequency;process.Refresh();var cpu=process.TotalProcessorTime;
                AevoriaGpuReading reading;
                try{reading=gpu?.Read()??new(null,"GPU source unavailable.");}catch(Exception ex) when(ex is not OutOfMemoryException){reading=new(null,"GPU polling failed: "+ex.GetType().Name);}
                var memory=new MemoryStatus{Length=(uint)Marshal.SizeOf<MemoryStatus>()};double? system=GlobalMemoryStatusEx(ref memory)?memory.Load:null;
                var result=new AevoriaResources(previousCpu is {} prior?CpuPercent((cpu-prior).TotalSeconds,at-previousTime,Environment.ProcessorCount):null,
                    process.WorkingSet64,process.PrivateMemorySize64,GC.GetTotalMemory(false),system,reading,Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                    GC.GetTotalAllocatedBytes(false),GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2),at);
                Volatile.Write(ref latest,result);previousCpu=cpu;previousTime=at;
                await Task.Delay(1000,stopping.Token).ConfigureAwait(false);
            }
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) when(ex is not OutOfMemoryException){Volatile.Write(ref latest,Latest with{Cpu=null,Gpu=new(null,"Resource sampling stopped: "+ex.GetType().Name)});}
        finally{gpu?.Dispose();stopping.Dispose();}
    }
    internal static double? CpuPercent(double cpuSeconds,double elapsed,int logicalProcessors)=>elapsed<=0 || logicalProcessors<=0 || !double.IsFinite(cpuSeconds) || !double.IsFinite(elapsed)?null:Math.Clamp(cpuSeconds/elapsed/logicalProcessors*100,0,100);
    public void Dispose(){try{stopping.Cancel();}catch(ObjectDisposedException){} }
    [StructLayout(LayoutKind.Sequential)]private struct MemoryStatus{public uint Length,Load;public ulong TotalPhysical,AvailablePhysical,TotalPageFile,AvailablePageFile,TotalVirtual,AvailableVirtual,AvailableExtendedVirtual;}
    [DllImport("kernel32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);
}
