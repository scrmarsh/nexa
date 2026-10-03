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

    private void CommitWidgetDrop(WidgetInstance moved, double x, double y, bool preserveAnchor=false)
    {
        var frame = widgetFrames[moved.Id];
        var span = DisplaySpan(moved);
        var column = Math.Clamp((int)Math.Round(x / GridUnit), 0, 20 - span);
        var destination = new Rect(column * GridUnit, Math.Clamp(preserveAnchor ? y : Math.Round(y / RowUnit) * RowUnit, 0, 720000), frame.Width, frame.Height);
        var others = state.Widgets.Where(w => w.Id != moved.Id && widgetFrames.ContainsKey(w.Id))
            .OrderBy(w => Canvas.GetTop(widgetFrames[w.Id])).ThenBy(w => Canvas.GetLeft(widgetFrames[w.Id])).ToList();

        Dictionary<Guid,Rect> Pack(Rect anchor)=>PackWidgetFrames(others,new() { [moved.Id]=anchor });

        var packed = Pack(destination);
        if(!preserveAnchor)
        {
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
        }
        StoreWidgetPositions(packed);
        Changed(); SaveNow();
    }

    private Dictionary<Guid,Rect> PackWidgetFrames(IEnumerable<WidgetInstance> widgets,Dictionary<Guid,Rect> positions)
    {
        foreach(var widget in widgets)
        {
            var current=FrameRect(widgetFrames[widget.Id]);
            var maxColumn=20-DisplaySpan(widget);
            var preferred=Math.Clamp((int)Math.Round(current.X/GridUnit),0,maxColumn);
            var columns=Enumerable.Range(0,maxColumn+1).OrderBy(c=>Math.Abs(c-preferred)).ThenBy(c=>c).ToArray();
            var lastRow=Math.Ceiling((positions.Values.Select(r=>r.Bottom).DefaultIfEmpty(0).Max()+WidgetGap)/RowUnit);
            Rect? slot=null;
            for(var row=0;row<=lastRow && slot==null;row++)
                foreach(var col in columns)
                {
                    var candidate=new Rect(col*GridUnit,row*RowUnit,current.Width,current.Height);
                    if(positions.Values.Any(r=>PlacementOverlaps(r,candidate))) continue;
                    slot=candidate; break;
                }
            positions[widget.Id]=slot!.Value;
        }
        return positions;
    }

    private void StoreWidgetPositions(Dictionary<Guid,Rect> packed)
    {
        foreach (var widget in state.Widgets.Where(w => packed.ContainsKey(w.Id)))
        {
            var rect = packed[widget.Id];
            // Reuse the existing normalized position fields; exports and old layouts need no migration.
            widget.PlacementX = rect.X / Math.Max(1, WidgetCanvas.ActualWidth);
            widget.PlacementY = rect.Y;
            widget.Column = Math.Clamp((int)Math.Round(rect.X / GridUnit), 0, 20 - widget.Span);
            widget.Row = Math.Clamp((int)Math.Round(rect.Y / RowUnit), 0, 30000);
        }
    }

    private void ResizeWidgetVisual(WidgetInstance active, Rect anchor, Dictionary<Guid, Rect> original)
    {
        var occupied=new List<Rect> { anchor };
        var frame=widgetFrames[active.Id]; Canvas.SetLeft(frame,anchor.X); Canvas.SetTop(frame,anchor.Y); frame.Width=anchor.Width; frame.Height=anchor.Height;
        FitWidgetBody(frame,active);
        foreach(var pair in original.Where(p=>p.Key!=active.Id).OrderBy(p=>p.Value.Y).ThenBy(p=>p.Value.X))
        {
            var rect=pair.Value;
            while(occupied.FirstOrDefault(r=>PlacementOverlaps(r,rect)) is var hit && hit.Width>0) rect.Y=Math.Ceiling((hit.Bottom+WidgetGap)/RowUnit)*RowUnit;
            var other=widgetFrames[pair.Key]; Canvas.SetLeft(other,rect.X); Canvas.SetTop(other,rect.Y); occupied.Add(rect);
        }
        WidgetCanvas.Height=Math.Max(WidgetScroller.ViewportHeight,occupied.Max(r=>r.Bottom)+WidgetGap);
    }

    private void FitWidgetBody(Border frame,WidgetInstance widget)
    {
        if(frame.Child is not StackPanel stack || stack.Children.OfType<ScrollViewer>().FirstOrDefault() is not {} body) return;
        if(body.Content is FrameworkElement content) content.MaxWidth=Math.Max(1,frame.Width-2-content.Margin.Left-content.Margin.Right);
        body.Height=Math.Max(0,frame.Height-stack.Children.OfType<FrameworkElement>().Where(c=>c!=body).Sum(c=>c.DesiredSize.Height)-2);
        if(widget.Kind=="Bible" && body.Content is StackPanel biblePanel && biblePanel.Children.OfType<BibleReader>().FirstOrDefault() is {} reader)
            reader.Height=Math.Max(1,body.Height-biblePanel.Margin.Top-biblePanel.Margin.Bottom);
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
