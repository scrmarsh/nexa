using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;

// Resolution-independent, two-color illustrations rendered entirely by WPF.
public sealed class StoryArtwork(string scene, Color accent, Color pixel) : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.PushTransform(new ScaleTransform(ActualWidth / 600, ActualHeight / 180));
        var ink = new SolidColorBrush(accent);
        var secondary = new SolidColorBrush(pixel);
        var line = new Pen(ink, 1.6);
        var faint = new SolidColorBrush(Color.FromArgb(28, accent.R, accent.G, accent.B));
        dc.DrawRoundedRectangle(faint, null, new Rect(0, 0, 600, 180), 12, 12);
        dc.DrawEllipse(faint, null, new Point(490, 45), 29, 29);
        dc.DrawEllipse(ink, null, new Point(490, 45), 13, 13);
        for (var i = 0; i < 17; i++) dc.DrawRectangle(i % 3 == 0 ? secondary : ink, null, new Rect(25 + i * 33, 18 + i % 4 * 11, 2, 2));
        dc.DrawLine(new Pen(ink, .7), new Point(24, 151), new Point(576, 151));
        void Path(Point[] points, bool fill = false)
        {
            var geometry = new StreamGeometry();
            using (var context = geometry.Open()) { context.BeginFigure(points[0], fill, fill); context.PolyLineTo(points.Skip(1).ToArray(), true, false); }
            dc.DrawGeometry(fill ? faint : null, line, geometry);
        }
        void Greenhouse(double x)
        {
            Path([new(x, 150), new(x, 95), new(x + 70, 52), new(x + 140, 95), new(x + 140, 150)], true);
            dc.DrawLine(line, new Point(x, 95), new Point(x + 140, 95));
            for (var i = 1; i < 4; i++) dc.DrawLine(line, new Point(x + i * 35, 95), new Point(x + i * 35, 150));
            for (var i = 0; i < 4; i++)
            {
                var stem = x + 18 + i * 35;
                dc.DrawLine(line, new Point(stem, 143), new Point(stem, 119));
                dc.DrawEllipse(secondary, null, new Point(stem - 5, 123), 6, 3);
                dc.DrawEllipse(ink, null, new Point(stem + 5, 131), 6, 3);
            }
        }
        if (new[] { "archive", "map", "echo", "memory" }.Contains(scene))
        {
            for (var shelf = 0; shelf < 2; shelf++)
            {
                dc.DrawLine(line, new Point(120, 100 + shelf * 45), new Point(450, 100 + shelf * 45));
                for (var book = 0; book < 12; book++) dc.DrawRoundedRectangle(book % 3 == 0 ? faint : null, new Pen(book % 3 == 0 ? secondary : ink, 1), new Rect(127 + book * 26, 66 + shelf * 45, 17, 34), 1, 1);
            }
        }
        else if (new[] { "platform", "passengers", "engine", "departure", "quiet" }.Contains(scene))
        {
            dc.DrawRoundedRectangle(faint, line, new Rect(95, 80, 350, 57), 14, 14);
            for (var i = 0; i < 6; i++) dc.DrawRoundedRectangle(i == 5 ? secondary : faint, line, new Rect(115 + i * 50, 93, 32, 20), 3, 3);
            foreach (var x in new[] { 135, 185, 345, 395 }) dc.DrawEllipse(secondary, line, new Point(x, 142), 7, 7);
            dc.DrawLine(line, new Point(70, 158), new Point(520, 158));
        }
        else if (new[] { "roof", "weather", "relay", "beacon" }.Contains(scene))
        {
            Path([new(220, 150), new(245, 70), new(270, 150)]);
            dc.DrawLine(line, new Point(245, 70), new Point(245, 38));
            dc.DrawEllipse(secondary, null, new Point(245, 38), 5, 5);
            for (var i = 1; i < 4; i++) dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(180 - i * 38), accent.R, accent.G, accent.B)), 1), new Point(245, 38), i * 15, i * 10);
            Greenhouse(340);
        }
        else
        {
            Greenhouse(235);
            for (var i = 0; i < 5; i++) { var x = 64 + i * 26; dc.DrawRectangle(faint, line, new Rect(x, 115 - i % 2 * 12, 19, 35 + i % 2 * 12)); }
            Path([new(375, 150), new(410, 131), new(445, 150)]);
        }
        dc.Pop();
    }
}
