using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;

internal sealed class AnalogClock : FrameworkElement
{
    private DateTime time = DateTime.Now;
    private double? elapsedSeconds;
    internal AnalogClock() { MinHeight = 120; MaxHeight = 300; HorizontalAlignment = HorizontalAlignment.Stretch; }
    internal void SetTime(DateTime value) { elapsedSeconds = null; time = value; InvalidateVisual(); }
    internal void SetElapsed(double seconds) { elapsedSeconds = Math.Max(0, seconds); InvalidateVisual(); }
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = Math.Clamp(Math.Min(double.IsInfinity(availableSize.Width) ? 220 : availableSize.Width, double.IsInfinity(availableSize.Height) ? 220 : availableSize.Height), 120, 260);
        return new Size(size, size);
    }
    protected override void OnRender(DrawingContext dc)
    {
        var accent = (Brush)FindResource("DashboardAccentBrush"); var text = (Brush)FindResource("DashboardTextBrush"); var muted = (Brush)FindResource("DashboardMutedBrush"); var panel = (Brush)FindResource("DashboardSurfaceBrush");
        var size = Math.Min(ActualWidth, ActualHeight); if (size <= 0) return; var center = new Point(ActualWidth / 2, ActualHeight / 2); var radius = size / 2 - 8;
        dc.DrawEllipse(panel, new Pen((Brush)FindResource("DashboardLineBrush"), 1.2), center, radius, radius);
        var full = size >= 190;
        for (var i = 0; i < 60; i++)
        {
            if (!full && i % 5 != 0) continue; var angle = i * Math.PI / 30; var major = i % 5 == 0; var outer = Point(angle, radius - 8); var inner = Point(angle, radius - (major ? 19 : 13)); dc.DrawLine(new Pen(major ? text : muted, major ? 2 : 1), inner, outer);
        }
        var seconds = time.Second + time.Millisecond / 1000.0; var minutes = time.Minute + seconds / 60; var hours = time.Hour % 12 + minutes / 60;
        if (elapsedSeconds is { } elapsed) { seconds = elapsed % 60; minutes = elapsed / 60 % 60; }
        if (elapsedSeconds == null) Hand(hours * Math.PI / 6, radius * .5, 4.5, text);
        Hand(minutes * Math.PI / 30, radius * .72, 3, text); Hand(seconds * Math.PI / 30, radius * .78, 1.5, accent);
        dc.DrawEllipse(accent, null, center, 5, 5);
        Point Point(double angle, double length) => new(center.X + Math.Sin(angle) * length, center.Y - Math.Cos(angle) * length);
        void Hand(double angle, double length, double width, Brush brush) => dc.DrawLine(new Pen(brush, width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, Point(angle + Math.PI, 8), Point(angle, length));
    }
}
