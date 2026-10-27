using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SessionDashboard;

internal static class NexaWindowBounds
{
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rectangle { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMax { public Point Reserved, MaxSize, MaxPosition, MinTrack, MaxTrack; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rectangle Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    internal static void Attach(Window window)
    {
        HwndSource? source = null;
        nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
        {
            if (message != 0x0024) return nint.Zero; // WM_GETMINMAXINFO uses physical monitor pixels.
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) return nint.Zero;
            var full = window is MainWindow main && main.IsFullscreen || window is EcosystemWindow ecosystem && ecosystem.IsFullscreen;
            var area = full ? info.Monitor : info.Work;
            var size = Marshal.PtrToStructure<MinMax>(lParam);
            size.MaxPosition = new Point { X = area.Left - info.Monitor.Left, Y = area.Top - info.Monitor.Top };
            size.MaxSize = new Point { X = area.Right - area.Left, Y = area.Bottom - area.Top };
            Marshal.StructureToPtr(size, lParam, false);
            handled = true;
            return nint.Zero;
        }
        window.SourceInitialized += (_, _) => { source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle); source?.AddHook(Hook); };
        window.Closed += (_, _) => { source?.RemoveHook(Hook); source = null; };
    }
}
