using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
public partial class MainWindow
{
    private NexaFeedback? feedbackOverlay;
    private void InitializeFeedback()
    {
        feedbackOverlay = new NexaFeedback(() => Options, FeedbackBounds, () => IsLoaded && IsVisible && WindowState != WindowState.Minimized && DashboardShell.IsVisible);
        Panel.SetZIndex(feedbackOverlay, 110); ((Grid)Content).Children.Add(feedbackOverlay);
        if (state.Workspace != null) feedbackOverlay.Observe(Continuity.Current(state.Workspace, store.UserId).Current, state.Productivity.Events);
        PreviewMouseMove += (_, e) =>
        {
            if (bugsOverlay != null) bugsOverlay.SetCursor(e.GetPosition(bugsOverlay));
            if (seasonalArtwork != null) seasonalArtwork.SetCursor(e.GetPosition(WidgetCanvas));
        };
        WidgetScroller.ScrollChanged += (_, _) => seasonalArtwork?.SetViewport(new Rect(WidgetScroller.HorizontalOffset, WidgetScroller.VerticalOffset, Math.Max(1, WidgetScroller.ViewportWidth), Math.Max(1, WidgetScroller.ViewportHeight)));
        Closed += (_, _) => { feedbackOverlay?.Dispose(); seasonalArtwork?.Dispose(); };
    }
    private Rect? FeedbackBounds(Guid? id)
    {
        if (feedbackOverlay == null || !IsLoaded) return null;
        FrameworkElement? element = id is { } value && widgetFrames.TryGetValue(value, out var frame) ? frame : null;
        element ??= state.Widgets.FirstOrDefault(w => w.Kind == "Productivity Score") is { } score && widgetFrames.TryGetValue(score.Id, out var scoreFrame) ? scoreFrame : PixelBannerContainer;
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return null;
        var bounds = element.TransformToVisual(feedbackOverlay).TransformBounds(new Rect(element.RenderSize));
        var viewport = new Rect(0, 52, ActualWidth, Math.Max(0, ActualHeight - 52));
        return bounds.IntersectsWith(viewport) ? bounds : null;
    }
    private void SavedFeedback()
    {
        if (feedbackOverlay != null && state.Workspace != null) feedbackOverlay.Observe(Continuity.Current(state.Workspace, store.UserId).Current, state.Productivity.Events);
    }
    private void UpdateFrenzyFeedback()
    {
        var score = state.Widgets.FirstOrDefault(w => w.Kind == "Productivity Score");
        feedbackOverlay?.Frenzy(state.Productivity.FrenzyUntil, score == null ? null : FeedbackBounds(score.Id));
    }
}
