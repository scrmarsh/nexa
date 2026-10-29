using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SessionDashboard;

internal static partial class NexaWindowBounds
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Window,Action> safetyUpdates=new();
    internal static void RefreshSafety(Window window){if(safetyUpdates.TryGetValue(window,out var apply))apply();}
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rectangle rectangle);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);

    // Native rectangles remain in physical pixels, including negative monitor origins.
    internal static Rect FitToWorkArea(Rect requested, Rect work)
    {
        if (work.IsEmpty || work.Width <= 0 || work.Height <= 0) return requested;
        var width = Math.Clamp(double.IsFinite(requested.Width) ? requested.Width : work.Width, 1, work.Width);
        var height = Math.Clamp(double.IsFinite(requested.Height) ? requested.Height : work.Height, 1, work.Height);
        var x = double.IsFinite(requested.X) ? requested.X : work.X;
        var y = double.IsFinite(requested.Y) ? requested.Y : work.Y;
        return new(Math.Clamp(x, work.Left, work.Right - width), Math.Clamp(y, work.Top, work.Bottom - height), width, height);
    }

    private static void AttachSafety(Window window)
    {
        HwndSource? source = null;
        var queued = false; var applying = false; var closed = false; var initialized = false;
        double minWidth = 0, minHeight = 0, maxWidth = double.PositiveInfinity, maxHeight = double.PositiveInfinity;
        void Apply()
        {
            queued = false;
            if (closed || applying || !initialized || window.WindowState == WindowState.Minimized) return;
            var hwnd = new WindowInteropHelper(window).Handle;
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (hwnd == 0 || !GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) return;
            var full = NexaFullscreen.IsFullscreen(window);
            var area = full ? info.Monitor : info.Work;
            var dpi = Math.Max(96, GetDpiForWindow(hwnd)) / 96d;
            var width = (area.Right - area.Left) / dpi;
            var height = (area.Bottom - area.Top) / dpi;
            if (width <= 0 || height <= 0) return;
            applying = true;
            try
            {
                window.MinWidth = Math.Min(minWidth, width); window.MinHeight = Math.Min(minHeight, height);
                window.MaxWidth = Math.Min(maxWidth, width); window.MaxHeight = Math.Min(maxHeight, height);
                if (full || window.WindowState != WindowState.Normal || !GetWindowRect(hwnd, out var rect)) return;
                var current = new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                var fitted = FitToWorkArea(current, new Rect(area.Left, area.Top, area.Right - area.Left, area.Bottom - area.Top));
                if (current != fitted)
                    SetWindowPos(hwnd, 0, (int)fitted.X, (int)fitted.Y, (int)fitted.Width, (int)fitted.Height, 0x0014); // NOZORDER | NOACTIVATE
            }
            finally { applying = false; }
        }
        void Queue()
        {
            if (queued || applying || closed || !initialized) return;
            queued = true; window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Apply));
        }
        safetyUpdates.Add(window,Apply);
        nint Hook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
        {
            if (message is 0x02E0 or 0x007E or 0x001A) Queue(); // DPI, display or work-area change.
            return 0;
        }
        window.SourceInitialized += (_, _) =>
        {
            minWidth = window.MinWidth; minHeight = window.MinHeight; maxWidth = window.MaxWidth; maxHeight = window.MaxHeight;
            initialized = true; source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle); source?.AddHook(Hook); Apply();
        };
        window.Loaded += (_, _) => Apply();
        window.LocationChanged += (_, _) => Queue(); window.SizeChanged += (_, _) => Queue(); window.StateChanged += (_, _) => Queue();
        window.Closed += (_, _) => { closed = true; source?.RemoveHook(Hook); source = null; };
    }
}
