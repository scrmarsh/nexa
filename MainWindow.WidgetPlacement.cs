using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

public partial class MainWindow
{
    private Dictionary<Guid, Point> WidgetVisualPositions() => widgetFrames.ToDictionary(
        pair => pair.Key, pair => pair.Value.TranslatePoint(new Point(), WidgetCanvas));

    private static bool PlacementOverlaps(Rect a, Rect b) =>
        a.Left < b.Right + WidgetGap - .01 && a.Right + WidgetGap > b.Left + .01 &&
        a.Top < b.Bottom + WidgetGap - .01 && a.Bottom + WidgetGap > b.Top + .01;

    private void CommitWidgetDrop(WidgetInstance moved, double x, double y)
    {
        var frame = widgetFrames[moved.Id];
        var span = DisplaySpan(moved);
        var column = Math.Clamp((int)Math.Round(x / GridUnit), 0, 20 - span);
        var destination = new Rect(column * GridUnit, Math.Clamp(Math.Round(y / RowUnit) * RowUnit, 0, 720000), frame.Width, frame.Height);
        var others = state.Widgets.Where(w => w.Id != moved.Id && widgetFrames.ContainsKey(w.Id))
            .OrderBy(w => Canvas.GetTop(widgetFrames[w.Id])).ThenBy(w => Canvas.GetLeft(widgetFrames[w.Id])).ToList();

        Dictionary<Guid, Rect> Pack(Rect anchor)
        {
            var positions = new Dictionary<Guid, Rect> { [moved.Id] = anchor };
            foreach (var widget in others)
            {
                var current = FrameRect(widgetFrames[widget.Id]);
                var maxColumn = 20 - DisplaySpan(widget);
                var preferredColumn = Math.Clamp((int)Math.Round(current.X / GridUnit), 0, maxColumn);
                var columns = Enumerable.Range(0, maxColumn + 1).OrderBy(c => Math.Abs(c - preferredColumn)).ThenBy(c => c).ToArray();
                // First available row fills holes; prefer the previous column when that row has room.
                // Reserve the physical drop destination before placing any surrounding card.
                var lastRow = Math.Ceiling((positions.Values.Max(r => r.Bottom) + WidgetGap) / RowUnit);
                Rect? slot = null;
                for (var row = 0; row <= lastRow && slot == null; row++)
                    foreach (var col in columns)
                    {
                        var candidate = new Rect(col * GridUnit, row * RowUnit, current.Width, current.Height);
                        if (positions.Values.Any(r => PlacementOverlaps(r, candidate))) continue;
                        slot = candidate; break;
                    }
                positions[widget.Id] = slot!.Value;
            }
            return positions;
        }

        var packed = Pack(destination);
        // Remove empty space above the selected card without passing cards in its chosen column.
        var above = packed.Where(p => p.Key != moved.Id).Select(p => p.Value)
            .Where(r => r.Top < destination.Top && r.Left < destination.Right + WidgetGap - .01 && r.Right + WidgetGap > destination.Left + .01);
        destination.Y = above.Any() ? Math.Ceiling((above.Max(r => r.Bottom) + WidgetGap) / RowUnit) * RowUnit : 0;
        packed = Pack(destination);
        // A hole in another column can still fit this card: keep its requested column,
        // but let neighbors shift sideways instead of leaving a needless extra row.
        var otherRects = packed.Where(p => p.Key != moved.Id).Select(p => p.Value).ToArray();
        var bottom = packed.Values.Max(r => r.Bottom);
        for (var row = 0d; row * RowUnit < destination.Y; row++)
        {
            var hole = Enumerable.Range(0, 21 - span).Any(col =>
                otherRects.All(r => !PlacementOverlaps(r, new Rect(col * GridUnit, row * RowUnit, destination.Width, destination.Height))));
            if (!hole) continue;
            var anchor = destination; anchor.Y = row * RowUnit;
            var compacted = Pack(anchor);
            if (compacted.Values.Max(r => r.Bottom) > bottom + .01) continue;
            packed = compacted; break;
        }
        foreach (var widget in state.Widgets.Where(w => packed.ContainsKey(w.Id)))
        {
            var rect = packed[widget.Id];
            // Reuse the existing normalized position fields; exports and old layouts need no migration.
            widget.PlacementX = rect.X / Math.Max(1, WidgetCanvas.ActualWidth);
            widget.PlacementY = rect.Y;
            widget.Column = Math.Clamp((int)Math.Round(rect.X / GridUnit), 0, 20 - widget.Span);
            widget.Row = Math.Clamp((int)Math.Round(rect.Y / RowUnit), 0, 30000);
        }
        Changed(); SaveNow();
    }

    private void AnimateWidgetSettle(Dictionary<Guid, Point> before)
    {
        foreach (var (id, frame) in widgetFrames)
        {
            if (!before.TryGetValue(id, out var start)) continue;
            var offset = start - new Point(Canvas.GetLeft(frame), Canvas.GetTop(frame));
            if (offset.Length < .5) { frame.RenderTransform = Transform.Identity; continue; }
            var transform = new TranslateTransform(offset.X, offset.Y);
            frame.RenderTransform = transform;
            var duration = TimeSpan.FromMilliseconds(180);
            var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
            transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, duration) { EasingFunction = easing });
            var vertical = new DoubleAnimation(0, duration) { EasingFunction = easing };
            vertical.Completed += (_, _) => { if (ReferenceEquals(frame.RenderTransform, transform)) frame.RenderTransform = Transform.Identity; };
            transform.BeginAnimation(TranslateTransform.YProperty, vertical);
        }
    }
}
