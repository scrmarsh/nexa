using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
public partial class MainWindow
{
    internal BugOverlay? bugsOverlay;
    private NexaEcosystem? ecosystem;
    private bool bugGeometryDirty = true;
    private string? ecosystemWarning;
    internal double BugUpdateMilliseconds => ecosystem?.LastStepMilliseconds ?? 0;
    internal TimeSpan BugFrameInterval => ecosystem?.FrameInterval ?? TimeSpan.FromSeconds(1d / 30);
    internal long BugFrameCount { get; private set; }
    internal bool BugsRunning => ecosystem != null && bugsOverlay?.IsVisible == true && !ecosystem.Manager.WorldPaused;
    private void InitializeBugs()
    {
        Loaded += (_, _) => ApplyBugPreferences(store.LoadBugPreferences());
        IsVisibleChanged += (_, _) => UpdateBugActivity(); StateChanged += (_, _) => UpdateBugActivity();
        DashboardShell.IsVisibleChanged += (_, _) => UpdateBugActivity();
        WidgetCanvas.LayoutUpdated += (_, _) => bugGeometryDirty = true;
        WidgetScroller.ScrollChanged += (_, _) => { bugGeometryDirty = true; UpdateBugGeometry(); bugsOverlay?.RefreshFrame(); };
        Closed += (_, _) => RemoveBugs();
    }
    internal void ApplyBugPreferences(BugPreferences preferences)
    {
        if (!preferences.Enabled) { RemoveBugs(); return; }
        if (bugsOverlay == null)
        {
            ecosystem = NexaEcosystem.For(store);
            bugsOverlay = new BugOverlay(ecosystem.Manager); bugsOverlay.DirectSquashed += DashboardSquashed;
            Panel.SetZIndex(bugsOverlay, 40); ((Grid)Content).Children.Add(bugsOverlay);
            ecosystem.Attach(this, () => IsVisible && WindowState != WindowState.Minimized && DashboardShell.IsVisible);
            ecosystem.Changed += EcosystemChanged;
        }
        ecosystem!.ApplyPreferences(preferences); ecosystem.ApplySeason(Options);
        bugGeometryDirty = true; UpdateBugActivity(); EcosystemChanged();
    }
    private void DashboardSquashed(BugEntity bug) => ecosystem?.RecordSquash(bug, this);
    internal void RecordEcosystemSquash(BugEntity bug) { TrackDirectSquash(bug); SaveNow(); }
    private void UpdateBugActivity()
    {
        if (bugsOverlay == null) return;
        bugsOverlay.Visibility = IsVisible && WindowState != WindowState.Minimized && DashboardShell.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        bugGeometryDirty = true; UpdateBugGeometry();
    }
    private void UpdateBugGeometry()
    {
        if (!bugGeometryDirty || bugsOverlay == null || !IsLoaded || WidgetCanvas.ActualWidth <= 0 || WidgetCanvas.ActualHeight <= 0) return;
        bugGeometryDirty = false;
        var origin = WidgetCanvas.TransformToVisual(bugsOverlay).Transform(new Point());
        bugsOverlay.SetWorldMapping(origin, new Size(WidgetCanvas.ActualWidth, WidgetCanvas.ActualHeight));
        var rectangles = widgetFrames.Select(pair => new Rect(Canvas.GetLeft(pair.Value), Canvas.GetTop(pair.Value), Math.Max(0, pair.Value.ActualWidth), Math.Max(0, pair.Value.ActualHeight))).ToArray();
        if (PixelObstacle is { } pixels) rectangles = rectangles.Append(pixels).ToArray();
        bugsOverlay.SetProtectedWidgets(rectangles);
        if (IsActive || ecosystem?.ViewCount == 1)
        {
            var scaleX = BugManager.WorldWidth / WidgetCanvas.ActualWidth; var scaleY = BugManager.WorldHeight / WidgetCanvas.ActualHeight;
            var surfaces = widgetFrames.Select(pair => new BugSurface(pair.Key.ToString(), new Rect(Canvas.GetLeft(pair.Value) * scaleX, Canvas.GetTop(pair.Value) * scaleY, Math.Max(0, pair.Value.ActualWidth) * scaleX, Math.Max(0, pair.Value.ActualHeight) * scaleY)));
            bugsOverlay.Manager.SetSurfaces(surfaces, new Size(BugManager.WorldWidth, BugManager.WorldHeight));
            var visible = WidgetScroller.TransformToVisual(WidgetCanvas).TransformBounds(new Rect(WidgetScroller.RenderSize));
            bugsOverlay.Manager.VisibleBounds = new Rect(visible.X * scaleX, visible.Y * scaleY, visible.Width * scaleX, visible.Height * scaleY);
        }
    }
    private void EcosystemChanged()
    {
        if (bugsOverlay == null || ecosystem == null) return;
        UpdateBugGeometry(); bugsOverlay.RefreshFrame(); BugFrameCount++;
        if (ecosystem.Status != ecosystemWarning)
        {
            ecosystemWarning = ecosystem.Status;
            if (ecosystemWarning != null) notifications?.Show("ecosystem-save", ecosystemWarning, "⚠", 3, null);
            else notifications?.Dismiss("ecosystem-save");
        }
    }
    private void RemoveBugs()
    {
        if (ecosystem != null) { ecosystem.Changed -= EcosystemChanged; ecosystem.Detach(this); ecosystem = null; }
        if (bugsOverlay == null) return;
        bugsOverlay.DirectSquashed -= DashboardSquashed;
        if (bugsOverlay.Parent is Panel panel) panel.Children.Remove(bugsOverlay);
        bugsOverlay = null;
    }
}
