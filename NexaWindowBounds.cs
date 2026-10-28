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

    internal static bool IsFullscreen(FrameworkElement element) => NexaFullscreen.IsFullscreen(Window.GetWindow(element));
    internal static double NotificationInset(FrameworkElement element) => IsFullscreen(element) ? 12 : 18;
    internal static bool TryNotificationArea(FrameworkElement element, out Rect area)
    {
        if (!IsFullscreen(element)) return TryWorkArea(element, out area);
        // Fullscreen covers the taskbar, so its reserved work area is not an inset.
        area = new Rect(0, 0, Math.Max(0, element.ActualWidth), Math.Max(0, element.ActualHeight));
        return area.Width > 0 && area.Height > 0;
    }

    internal static bool TryWorkArea(FrameworkElement element, out Rect area)
    {
        area = Rect.Empty;
        var window = Window.GetWindow(element);
        if (window == null) return false;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == nint.Zero) return false;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return false;
        try
        {
            var first = element.PointFromScreen(new System.Windows.Point(info.Work.Left, info.Work.Top));
            var last = element.PointFromScreen(new System.Windows.Point(info.Work.Right, info.Work.Bottom));
            area = Rect.Intersect(new Rect(0, 0, Math.Max(0, element.ActualWidth), Math.Max(0, element.ActualHeight)), new Rect(first, last));
            return !area.IsEmpty;
        }
        catch (InvalidOperationException) { return false; }
    }
    internal static void Attach(Window window)
    {
        HwndSource? source = null;
        nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
        {
            if (message != 0x0024) return nint.Zero; // WM_GETMINMAXINFO uses physical monitor pixels.
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) return nint.Zero;
            var full = NexaFullscreen.IsFullscreen(window);
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
