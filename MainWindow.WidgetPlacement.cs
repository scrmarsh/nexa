using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly List<Border> dropGhosts = [];
    private sealed record SharedEdge(WidgetInstance First, WidgetInstance Second, Rect FirstBounds, Rect SecondBounds,
        bool Horizontal, bool ActiveIsFirst, int FirstMinimum, int SecondMinimum);

    private SharedEdge? FindSharedEdge(WidgetInstance active, Rect bounds, ResizeEdge edge)
    {
        var horizontal = edge is ResizeEdge.Left or ResizeEdge.Right;
        var vertical = edge is ResizeEdge.Top or ResizeEdge.Bottom;
        if (!horizontal && !vertical) return null;
        var firstIsActive = edge is ResizeEdge.Right or ResizeEdge.Bottom;
        var candidates = state.Widgets.Where(w => w.Id != active.Id && widgetFrames.ContainsKey(w.Id))
            .Select(w => (Widget: w, Bounds: FrameRect(widgetFrames[w.Id])))
            .ToList();
        if (state.Widgets.FirstOrDefault(w => w.Kind == "Pixel timer" && w.Id != active.Id) is { } pixel && PixelObstacle is { } pixelBounds)
            candidates.Add((pixel, pixelBounds));
        var nearby = candidates.Select(candidate =>
        {
            var before = firstIsActive ? bounds : candidate.Bounds;
            var after = firstIsActive ? candidate.Bounds : bounds;
            var gap = horizontal ? after.Left - before.Right : after.Top - before.Bottom;
            var overlap = horizontal
                ? Math.Min(before.Bottom, after.Bottom) - Math.Max(before.Top, after.Top)
                : Math.Min(before.Right, after.Right) - Math.Max(before.Left, after.Left);
            var required = .6 * (horizontal ? Math.Min(before.Height, after.Height) : Math.Min(before.Width, after.Width));
            return (candidate.Widget, candidate.Bounds, Gap: gap, Overlap: overlap, Required: required);
        }).Where(c => c.Gap >= WidgetGap - 2 &&
            c.Gap <= (horizontal ? GridUnit : RowUnit) + WidgetGap + 1 && c.Overlap >= c.Required)
          .OrderBy(c => c.Gap).FirstOrDefault();
        if (nearby.Widget == null) return null;
        var first = firstIsActive ? active : nearby.Widget;
        var second = firstIsActive ? nearby.Widget : active;
        // The timer is anchored at the origin; it can only be the first card.
        if (second.Kind == "Pixel timer") return null;
        return new SharedEdge(first, second, firstIsActive ? bounds : nearby.Bounds,
            firstIsActive ? nearby.Bounds : bounds, horizontal, firstIsActive,
            first.Kind == "Pixel timer" ? horizontal ? (int)Math.Ceiling((450 + WidgetGap) / GridUnit) : 312 / 24 : horizontal ? MinimumWidthSteps(first) : (int)Math.Ceiling(MinimumWidgetHeight(first) / RowUnit),
            horizontal ? MinimumWidthSteps(second) : (int)Math.Ceiling(MinimumWidgetHeight(second) / RowUnit));
    }

    private int SharedStep(SharedEdge edge, double drag)
    {
        var unit = edge.Horizontal ? GridUnit : RowUnit;
        var first = (int)Math.Round(((edge.Horizontal ? edge.FirstBounds.Width : edge.FirstBounds.Height) + (edge.Horizontal ? WidgetGap : 0)) / unit);
        var second = (int)Math.Round(((edge.Horizontal ? edge.SecondBounds.Width : edge.SecondBounds.Height) + (edge.Horizontal ? WidgetGap : 0)) / unit);
        return Math.Clamp((int)Math.Round(drag / unit), edge.FirstMinimum - first, second - edge.SecondMinimum);
    }
    private (Rect First, Rect Second) SharedBounds(SharedEdge edge, int steps)
    {
        var delta = steps * (edge.Horizontal ? GridUnit : RowUnit);
        var first = edge.FirstBounds; var second = edge.SecondBounds;
        if (edge.Horizontal) { first.Width += delta; second.X += delta; second.Width -= delta; }
        else { first.Height += delta; second.Y += delta; second.Height -= delta; }
        return (first, second);
    }
    private void ShowSharedResize(SharedEdge edge, int steps)
    {
        var (first, second) = SharedBounds(edge, steps);
        void Set(WidgetInstance widget, Rect rect)
        {
            if (widget.Kind == "Pixel timer")
            {
                if (edge.Horizontal) PixelBannerContainer.Width = rect.Width;
                else { PixelBannerContainer.Height = rect.Height; PixelArea.Height = Math.Max(260, rect.Height - 34); }
                return;
            }
            var frame = widgetFrames[widget.Id];
            Canvas.SetLeft(frame, rect.X); Canvas.SetTop(frame, rect.Y); frame.Width = rect.Width; frame.Height = rect.Height;
            FitWidgetBody(frame, widget); PositionResizeHandles(widget.Id);
        }
        Set(edge.First, first); Set(edge.Second, second);
        var active = edge.ActiveIsFirst ? first : second;
        if (placementPreview != null)
        {
            Canvas.SetLeft(placementPreview, active.X); Canvas.SetTop(placementPreview, active.Y);
            placementPreview.Width = active.Width; placementPreview.Height = active.Height;
        }
    }
    private void CommitSharedResize(SharedEdge edge, int steps)
    {
        var (first, second) = SharedBounds(edge, steps);
        if (edge.Horizontal)
        {
            var firstSpan = (int)Math.Round((first.Width + WidgetGap) / GridUnit);
            if (edge.First.Kind == "Pixel timer") edge.First.BannerSpan = firstSpan;
            else { edge.First.Span = firstSpan; edge.First.CustomSize = true; }
            edge.Second.Span = (int)Math.Round((second.Width + WidgetGap) / GridUnit);
        }
        else
        {
            if (edge.First.Kind == "Pixel timer") edge.First.BannerHeight = (int)first.Height;
            else { edge.First.Height = (int)first.Height; edge.First.CustomSize = true; }
            edge.Second.Height = (int)second.Height;
        }
        edge.Second.CustomSize = true;
        var positions = new Dictionary<Guid, Rect> { [edge.Second.Id] = second };
        if (edge.First.Kind != "Pixel timer") positions[edge.First.Id] = first;
        StoreWidgetPositions(positions);
        Changed(); SaveNow();
    }

    // A row insertion is planned without moving live widgets. The plan is committed only on release.
    private bool TryPlanRowInsertion(WidgetInstance moved, double x, double y, out Dictionary<Guid, Rect> plan, out Dictionary<Guid, int> spans)
    {
        plan = []; spans = [];
        if (WidgetCanvas.ActualWidth < 640 + WidgetGap || !widgetFrames.TryGetValue(moved.Id, out var frame)) return false;
        var center = y + frame.Height / 2;
        var rows = state.Widgets.Where(w => w.Id != moved.Id && widgetFrames.ContainsKey(w.Id))
            .GroupBy(w => Math.Round(Canvas.GetTop(widgetFrames[w.Id]) / RowUnit))
            .Select(group => group.OrderBy(w => Canvas.GetLeft(widgetFrames[w.Id])).ToArray())
            .Where(row => row.Length is > 0 and < 3)
            .OrderBy(row => Math.Abs(center - (Canvas.GetTop(widgetFrames[row[0].Id]) + row.Max(w => widgetFrames[w.Id].Height) / 2)))
            .FirstOrDefault();
        if (rows == null) return false;
        var top = Canvas.GetTop(widgetFrames[rows[0].Id]);
        var rowHeight = rows.Max(w => widgetFrames[w.Id].Height);
        if (Math.Abs(center - (top + rowHeight / 2)) > Math.Max(75, rowHeight / 3)) return false;
        var ordered = rows.ToList();
        // Use the pointer's requested left edge for insertion. A resized, wide card
        // must still be able to land in the first column when dropped at x = 0.
        var insertion = ordered.FindIndex(w => x < Canvas.GetLeft(widgetFrames[w.Id]) + widgetFrames[w.Id].Width / 2);
        ordered.Insert(insertion < 0 ? ordered.Count : insertion, moved);
        var minima = ordered.ToDictionary(w => w.Id, MinimumWidthSteps);
        if (minima.Values.Sum() > 20) return false;
        var desired = ordered.ToDictionary(w => w.Id, DisplaySpan);
        var widths = ordered.ToDictionary(w => w.Id, w => minima[w.Id]);
        spans = widths;
        var remaining = Math.Min(20 - widths.Values.Sum(), Math.Max(0, desired.Values.Sum() - widths.Values.Sum()));
        while (remaining-- > 0)
        {
            var next = ordered.OrderByDescending(w => desired[w.Id] - widths[w.Id]).First();
            widths[next.Id]++;
        }
        var column = 0;
        foreach (var widget in ordered)
        {
            var height = widgetFrames[widget.Id].Height;
            plan[widget.Id] = new Rect(column * GridUnit, top, widths[widget.Id] * GridUnit - WidgetGap, height);
            column += widths[widget.Id];
        }
        if (PixelObstacle is { } pixel && plan.Values.Any(rect => PlacementOverlaps(pixel, rect))) { plan.Clear(); spans.Clear(); return false; }
        var planned = plan;
        if (state.Widgets.Where(w => !ordered.Contains(w) && widgetFrames.ContainsKey(w.Id))
            .Any(w => planned.Values.Any(rect => PlacementOverlaps(FrameRect(widgetFrames[w.Id]), rect))))
        { plan.Clear(); spans.Clear(); return false; }
        return true;
    }

    private void UpdateWidgetDropPreview(WidgetInstance moved, double x, double y)
    {
        if (placementPreview == null) return;
        Rect landing; Dictionary<Guid, Rect> plan;
        if (TryPlanRowInsertion(moved, x, y, out plan, out _)) landing = plan[moved.Id];
        else
        {
            var frame = widgetFrames[moved.Id]; var span = DisplaySpan(moved);
            landing = new Rect(Math.Clamp(Math.Round(x / GridUnit), 0, 20 - span) * GridUnit,
                Math.Max(0, Math.Round(y / RowUnit) * RowUnit), frame.Width, frame.Height);
            plan = StableDropPlan(moved, landing);
        }
        var neighbors = plan.Where(pair => pair.Key != moved.Id &&
            (pair.Value.Location - FrameRect(widgetFrames[pair.Key]).Location).Length > 1 ||
            pair.Key != moved.Id && Math.Abs(pair.Value.Width - widgetFrames[pair.Key].Width) > 1).ToArray();
        while (dropGhosts.Count < neighbors.Length)
        {
            var ghost = new Border { Background = new SolidColorBrush(Color.FromArgb(24, 91, 124, 255)), BorderBrush = DashboardBrush("AccentBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), IsHitTestVisible = false, Opacity = .6 };
            WidgetCanvas.Children.Add(ghost); Panel.SetZIndex(ghost, 1800); dropGhosts.Add(ghost);
        }
        for (var i = 0; i < dropGhosts.Count; i++)
        {
            dropGhosts[i].Visibility = i < neighbors.Length ? Visibility.Visible : Visibility.Collapsed;
            if (i >= neighbors.Length) continue;
            var rect = neighbors[i].Value; dropGhosts[i].Width = rect.Width; dropGhosts[i].Height = rect.Height;
            Canvas.SetLeft(dropGhosts[i], rect.X); Canvas.SetTop(dropGhosts[i], rect.Y);
        }
        placementPreview.Width = landing.Width; placementPreview.Height = landing.Height;
        Canvas.SetLeft(placementPreview, landing.X); Canvas.SetTop(placementPreview, landing.Y);
    }

    private Dictionary<Guid, Rect> StableDropPlan(WidgetInstance moved, Rect destination)
    {
        if (PixelObstacle is { } pixel && PlacementOverlaps(pixel, destination))
        {
            // A card may fit to the right of the visible timer; only a real
            // intersection moves it below the anchored banner.
            destination.Y = Math.Ceiling((pixel.Bottom + WidgetGap) / RowUnit) * RowUnit;
        }
        var packed = new Dictionary<Guid, Rect> { [moved.Id] = destination };
        var others = state.Widgets.Where(w => w.Id != moved.Id && widgetFrames.ContainsKey(w.Id))
            .OrderBy(w => Canvas.GetTop(widgetFrames[w.Id])).ThenBy(w => Canvas.GetLeft(widgetFrames[w.Id]));
        return PackWidgetFrames(others, packed);
    }

    private void ClearWidgetDropPreview()
    {
        foreach (var ghost in dropGhosts) WidgetCanvas.Children.Remove(ghost);
        dropGhosts.Clear();
    }
    private Dictionary<Guid, Point> WidgetVisualPositions() => widgetFrames.ToDictionary(
        pair => pair.Key, pair => pair.Value.TranslatePoint(new Point(), WidgetCanvas));

    private static bool PlacementOverlaps(Rect a, Rect b) =>
        a.Left < b.Right + WidgetGap - .01 && a.Right + WidgetGap > b.Left + .01 &&
        a.Top < b.Bottom + WidgetGap - .01 && a.Bottom + WidgetGap > b.Top + .01;

    private void CommitWidgetDrop(WidgetInstance moved, double x, double y, bool preserveAnchor=false)
    {
        if (!preserveAnchor && TryPlanRowInsertion(moved, x, y, out var insertion, out var spans))
        {
            foreach (var widget in state.Widgets.Where(w => spans.ContainsKey(w.Id))) widget.Span = spans[widget.Id];
            StoreWidgetPositions(insertion); Changed(); SaveNow(); return;
        }
        var frame = widgetFrames[moved.Id];
        var span = DisplaySpan(moved);
        var column = Math.Clamp((int)Math.Round(x / GridUnit), 0, 20 - span);
        var destination = new Rect(column * GridUnit, Math.Clamp(preserveAnchor ? y : Math.Round(y / RowUnit) * RowUnit, 0, 720000), frame.Width, frame.Height);
        var packed = StableDropPlan(moved, destination);
        StoreWidgetPositions(packed);
        Changed(); SaveNow();
    }

    private void CompactWidgetLayout()
    {
        if (widgetGestureActive || widgetFrames.Count == 0) return;
        var ordered = state.Widgets.Where(w => widgetFrames.ContainsKey(w.Id))
            .OrderBy(w => Canvas.GetTop(widgetFrames[w.Id])).ThenBy(w => Canvas.GetLeft(widgetFrames[w.Id]));
        StoreWidgetPositions(PackWidgetFrames(ordered, []));
        Changed(); ArrangeWidgetFrames(); SaveNow();
    }

    private Dictionary<Guid,Rect> PackWidgetFrames(IEnumerable<WidgetInstance> widgets,Dictionary<Guid,Rect> positions)
    {
        foreach(var widget in widgets)
        {
            var current=FrameRect(widgetFrames[widget.Id]);
            var maxColumn=20-DisplaySpan(widget);
            var preferred=Math.Clamp((int)Math.Round(current.X/GridUnit),0,maxColumn);
            var columns=Enumerable.Range(0,maxColumn+1).OrderBy(c=>Math.Abs(c-preferred)).ThenBy(c=>c).ToArray();
            var lastRow=Math.Ceiling((Math.Max(positions.Values.Select(r=>r.Bottom).DefaultIfEmpty(0).Max(),PixelObstacle?.Bottom ?? 0)
                + widgets.Sum(w=>widgetFrames[w.Id].Height+WidgetGap)+WidgetGap)/RowUnit);
            Rect? slot=null;
            for(var row=0;row<=lastRow && slot==null;row++)
                foreach(var col in columns)
                {
                    var candidate=new Rect(col*GridUnit,row*RowUnit,current.Width,current.Height);
                    if(positions.Values.Any(r=>PlacementOverlaps(r,candidate)) || PixelObstacle is { } pixel && PlacementOverlaps(pixel,candidate)) continue;
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
        var occupied=new List<Rect> { anchor }; if (PixelObstacle is { } pixel) occupied.Add(pixel);
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
        if (widget.Kind == "Notes" && body.Content is StackPanel notesPanel && notesPanel.Children.OfType<TextBox>().FirstOrDefault() is { } notes)
            notes.Height = Math.Max(110, body.Height - notesPanel.Margin.Top - notesPanel.Margin.Bottom - 2);
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
