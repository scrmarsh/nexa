using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace SessionDashboard;
// Native work-area coordinates are physical pixels, including on secondary/DPI-scaled monitors.
internal static class AevoriaWorkArea
{
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint {internal int X,Y;}
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect {internal int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo {internal int Size;internal NativeRect Monitor,Work;internal int Flags;}
    [StructLayout(LayoutKind.Sequential)] private struct MinMax {internal NativePoint Reserved,MaxSize,MaxPosition,MinTrack,MaxTrack;}
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor,ref MonitorInfo info);
    private static bool Read(nint window,out MonitorInfo info){info=new(){Size=Marshal.SizeOf<MonitorInfo>()};return GetMonitorInfo(MonitorFromWindow(window,2),ref info);}
    internal static Rect AvailablePixels(Window window)=>Read(new WindowInteropHelper(window).Handle,out var info)?new(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top):Rect.Empty;
    internal static void Attach(Window window)
    {
        window.SourceInitialized+=(_,_)=>HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(Hook);
    }
    private static nint Hook(nint window,int message,nint wParam,nint lParam,ref bool handled)
    {
        if(message!=0x24 || !Read(window,out var info))return 0;
        var value=Marshal.PtrToStructure<MinMax>(lParam);value.MaxPosition=new(){X=info.Work.Left-info.Monitor.Left,Y=info.Work.Top-info.Monitor.Top};value.MaxSize=new(){X=info.Work.Right-info.Work.Left,Y=info.Work.Bottom-info.Work.Top};Marshal.StructureToPtr(value,lParam,false);handled=true;return 0;
    }
}
