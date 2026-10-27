using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed class BugOverlay : FrameworkElement
{
    internal event Action<BugEntity>? DirectSquashed;
    internal BugManager Manager { get; }
    internal Point ContentOrigin;
    private double scaleX = 1, scaleY = 1;
    private Rect[] protectedWidgets = [];
    internal bool DrawWorld { get; set; }
    internal double LastRenderMilliseconds { get; private set; }
    internal long RenderedFrames { get; private set; }
    internal BugOverlay(BugManager? manager = null) { Manager = manager ?? new(); ClipToBounds = true; Focusable = false; }
    internal void SetViewOrigin(Point origin) { ContentOrigin = origin; RefreshFrame(); }
    internal void SetWorldMapping(Point origin, Size size)
    { ContentOrigin = origin; scaleX = size.Width / BugManager.WorldWidth; scaleY = size.Height / BugManager.WorldHeight; RefreshFrame(); }
    internal void SetProtectedWidgets(IEnumerable<Rect> rectangles) => protectedWidgets = rectangles.ToArray();
    internal Point ScreenToWorld(Point point) => new((point.X - ContentOrigin.X) / Math.Max(.001, scaleX), (point.Y - ContentOrigin.Y) / Math.Max(.001, scaleY));
    internal Point WorldToScreen(Point point) => new(point.X * scaleX + ContentOrigin.X, point.Y * scaleY + ContentOrigin.Y);
    private double ViewScale => DrawWorld ? Math.Min(scaleX, scaleY) : 1;
    private bool Protected(Point point) => protectedWidgets.Any(r => r.Contains(new Point(point.X - ContentOrigin.X, point.Y - ContentOrigin.Y)));
    private IEnumerable<BugEntity> VisibleBugs => Manager.Bugs.Where(b => Manager.Preferences.Species is BugSpecies.AllBugs or BugSpecies.AllThree || b.Species == Manager.Preferences.Species).Take(Manager.EcosystemActive ? Manager.EcosystemCap : 600);
    internal BugEntity? HitAt(Point point)
    {
        if (Protected(point)) return null;
        return VisibleBugs.Reverse().FirstOrDefault(b => b.State is not (BugState.Hiding or BugState.Squashed) && b.Health > 0 && (point - WorldToScreen(b.Position)).Length <= BugVisuals.HitRadius(b) * b.SizeScale * ViewScale);
    }
    protected override HitTestResult? HitTestCore(PointHitTestParameters p) => DrawWorld || HitAt(p.HitPoint) != null ? new PointHitTestResult(this, p.HitPoint) : null;
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (HitAt(e.GetPosition(this)) is { } bug) { if (Manager.Squash(bug)) DirectSquashed?.Invoke(bug); e.Handled = true; RefreshFrame(); }
    }
    internal void RefreshFrame() => InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        var began = System.Diagnostics.Stopwatch.GetTimestamp(); RenderedFrames++;
        if (protectedWidgets.Length > 0)
        {
            var holes = new GeometryGroup();
            foreach (var r in protectedWidgets) { var rect = r; rect.Offset(ContentOrigin.X, ContentOrigin.Y); rect.Inflate(-5, -5); if (rect.Width > 0 && rect.Height > 0) holes.Children.Add(new RectangleGeometry(rect, 6, 6)); }
            var clip = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(RenderSize)), holes); dc.PushClip(clip);
        }
        if (Manager.EcosystemActive) DrawFeatures(dc);
        foreach (var splat in Manager.Splats)
        { var p = WorldToScreen(splat.Position); dc.PushOpacity(.45 * Math.Clamp((4 - splat.Age) / 2, 0, 1)); dc.DrawEllipse(BugArtwork.Ant, null, p, 4 * ViewScale, 2 * ViewScale); dc.Pop(); }
        var options = Manager.EcosystemSettings;
        foreach (var bug in VisibleBugs)
        {
            if (bug.State == BugState.Hiding) continue;
            var p = WorldToScreen(bug.Position); var radius = BugVisuals.HitRadius(bug) * bug.SizeScale * ViewScale;
            if (p.X + radius < 0 || p.Y + radius < 0 || p.X - radius > ActualWidth || p.Y - radius > ActualHeight) continue;
            var sx = bug.SizeScale * ViewScale * (bug.State == BugState.Squashed ? 1 + bug.SquashAge * 2 : 1);
            var sy = bug.SizeScale * ViewScale * (bug.State == BugState.Squashed ? Math.Max(.15, 1 - bug.SquashAge * 5) : 1);
            var angle = bug.Angle * Math.PI / 180;
            dc.PushTransform(new MatrixTransform(new Matrix(sx, 0, 0, sy, 0, 0) * new Matrix(Math.Cos(angle), Math.Sin(angle), -Math.Sin(angle), Math.Cos(angle), p.X, p.Y)));
            BugVisuals.Draw(dc, bug, options, Manager.SeasonalCostumes); dc.Pop();
            if (bug.HitFlash > 0) dc.DrawEllipse(null, BugArtwork.HitRing, p, radius * .5, radius * .4);
        }
        if (protectedWidgets.Length > 0) dc.Pop();
        LastRenderMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }
    private void DrawFeatures(DrawingContext dc)
    {
        foreach (var nest in Manager.Nests)
        {
            var p = WorldToScreen(nest.Position);
            var colors = new[] { "#B58A5A", "#A3A36B", "#8AB6C5", "#B594D9", "#E5878A", "#BDA366" };
            var color = colors[Array.IndexOf(BugManager.AllSpecies, nest.Species)];
            if (DrawWorld)
            {
                dc.PushOpacity(.075); dc.DrawEllipse(BugVisuals.B(color), null, p, 145 * scaleX, 145 * scaleY); dc.Pop();
                dc.DrawEllipse(BugVisuals.B("#413327"), BugVisuals.P(color, 1), p, (12 + nest.Construction * 12) * ViewScale, 9 * ViewScale);
                dc.DrawEllipse(BugVisuals.B("#18131A"), null, p, 7 * ViewScale, 4 * ViewScale);
            }
        }
        foreach (var resource in Manager.Resources)
        {
            if (resource.Amount <= .1 || !DrawWorld && (resource.Kind != BugResourceKind.Candy || !Manager.SeasonalCandy)) continue;
            if (resource.Kind == BugResourceKind.Candy && !Manager.SeasonalCandy) continue;
            var p = WorldToScreen(resource.Position); if (p.X < -20 || p.Y < -20 || p.X > ActualWidth + 20 || p.Y > ActualHeight + 20) continue;
            var size = Math.Clamp(2 + resource.Amount / 6, 3, 6) * ViewScale;
            var color = resource.Kind switch { BugResourceKind.Candy => "#F97316", BugResourceKind.Fruit => "#D88191", BugResourceKind.Leaves => "#84A85A", BugResourceKind.Water => "#7DD3FC", _ => "#BDA77A" };
            dc.DrawEllipse(BugVisuals.B(color), BugVisuals.P("#D9C9DD", .5), p, size, size * .7);
            if (resource.Kind == BugResourceKind.Candy) { dc.DrawLine(BugVisuals.P("#A855F7", 2 * ViewScale), p + new Vector(-size * 1.4, -size * .5), p + new Vector(size * 1.4, size * .5)); dc.DrawLine(BugVisuals.P("#FACC15", ViewScale), p + new Vector(0, -size * .6), p + new Vector(0, size * .6)); }
        }
        foreach (var web in Manager.Webs)
        {
            var p = WorldToScreen(new(web.X, web.Y)); var radius = (12 + 30 * web.Construction) * ViewScale;
            for (var spoke = 0; spoke < 8; spoke++) { var angle = spoke * Math.PI / 4; dc.DrawLine(BugVisuals.P(Manager.SeasonalCostumes ? "#AB98C7" : "#79818B", .5), p, p + new Vector(Math.Cos(angle) * radius, Math.Sin(angle) * radius)); }
            for (var ring = 1; ring <= 3; ring++) dc.DrawEllipse(null, BugVisuals.P(Manager.SeasonalCostumes ? "#84CC16" : "#79818B", .4), p, radius * ring / 3, radius * ring / 3);
        }
    }
}
