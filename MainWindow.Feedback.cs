using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
public partial class MainWindow
{
    private NexaFeedback? feedbackOverlay;
    private ProductivityStreakOverlay? streakOverlay;
    private readonly System.Windows.Threading.DispatcherTimer streakRefresh = new(System.Windows.Threading.DispatcherPriority.Background) { Interval=TimeSpan.FromMilliseconds(100) };
    private void InitializeFeedback()
    {
        feedbackOverlay = new NexaFeedback(() => Options, FeedbackBounds, () => IsLoaded && IsVisible && WindowState != WindowState.Minimized && DashboardShell.IsVisible);
        Panel.SetZIndex(feedbackOverlay, 110); ((Grid)Content).Children.Add(feedbackOverlay);
        streakOverlay=new ProductivityStreakOverlay();Panel.SetZIndex(streakOverlay,111);((Grid)Content).Children.Add(streakOverlay);
        streakRefresh.Tick+=(_,_)=>{if(IsVisible && WindowState!=WindowState.Minimized && DashboardShell.IsVisible){streakOverlay.SpaceScore=SpaceScores.Total(Continuity.Current(state.Workspace!,store.UserId),state.Productivity);streakOverlay.Ledger=state.Productivity;streakOverlay.Options=Options;streakOverlay.Refresh();}};
        WidgetCanvas.PreviewMouseLeftButtonDown+=(_,e)=>
        {
            if(!ReferenceEquals(e.OriginalSource,WidgetCanvas) || !CanCollectSeasonalCandy)return;
            var id=seasonalArtwork!.CandyAt(e.GetPosition(WidgetCanvas));if(id==null)return;
            if(bugsOverlay?.Manager.Bugs.Any(b=>b.ResourceId==id && b.Activity==BugActivity.Gathering)==true)return;
            var ledger=Continuity.Clone(state.Productivity);
            if(!ProductivityScoring.CollectCandy(ledger,id.Value,DateTimeOffset.UtcNow,Options.StreakMultiplierCap))return;
            if(!seasonalArtwork.TryCollect(id.Value))return;
            state.Productivity=ledger;bugsOverlay?.Manager.BindCandy([]);Changed();SaveNow();e.Handled=true;
        };
        Loaded+=(_,_)=>streakRefresh.Start();
        if (state.Workspace != null) feedbackOverlay.Observe(Continuity.Current(state.Workspace, store.UserId).Current, state.Productivity.Events);
        PreviewMouseMove += (_, e) =>
        {
            if (bugsOverlay != null) bugsOverlay.SetCursor(e.GetPosition(bugsOverlay));
            if (seasonalArtwork != null) seasonalArtwork.SetCursor(e.GetPosition(WidgetCanvas));
        };
        WidgetScroller.ScrollChanged += (_, _) => seasonalArtwork?.SetViewport(new Rect(WidgetScroller.HorizontalOffset, WidgetScroller.VerticalOffset, Math.Max(1, WidgetScroller.ViewportWidth), Math.Max(1, WidgetScroller.ViewportHeight)));
        Closed += (_, _) => { streakRefresh.Stop();streakOverlay?.Dispose();feedbackOverlay?.Dispose(); seasonalArtwork?.Dispose(); };
    }
    internal bool CanCollectSeasonalCandy=>NexaSeason.Active(Options,DateTimeOffset.UtcNow) && Options.SeasonalDecorations && Options.DecorationDensity!="Off" && state.Session?.FinalizedAt==null && seasonalArtwork?.IsVisible==true;
    private Rect? FeedbackBounds(Guid? id)
    {
        if (feedbackOverlay == null || !IsLoaded) return null;
        FrameworkElement? element=id is {} scoreId && scoreAreas.TryGetValue(scoreId,out var scoreArea)?scoreArea:id is {} value && widgetFrames.TryGetValue(value,out var frame)?frame:null;
        element ??= state.Widgets.FirstOrDefault(w => w.Kind is "Productivity" or "Productivity Score") is { } score && widgetFrames.TryGetValue(score.Id, out var scoreFrame) ? scoreFrame : PixelBannerContainer;
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return null;
        var bounds = element.TransformToVisual(feedbackOverlay).TransformBounds(new Rect(element.RenderSize));
        var viewport = new Rect(0, 52, ActualWidth, Math.Max(0, ActualHeight - 52));
        return bounds.IntersectsWith(viewport) ? bounds : null;
    }
    private Guid soundSessionId;
    private readonly HashSet<Guid> soundedEvents = [];
    private void SavedFeedback()
    {
        if (feedbackOverlay != null && state.Workspace != null) feedbackOverlay.Observe(Continuity.Current(state.Workspace, store.UserId).Current, state.Productivity.Events);
        if (state.Workspace == null) return;
        var current = Continuity.Current(state.Workspace, store.UserId).Current;
        if (soundSessionId != current.SessionId) { soundSessionId = current.SessionId; soundedEvents.Clear(); foreach (var item in state.Productivity.Events) soundedEvents.Add(item.Id); return; }
        foreach (var item in state.Productivity.Events)
            if (soundedEvents.Add(item.Id) && state.InterfaceSoundsEnabled && item.BasePoints + item.BonusPoints != 0 && item.Category != "Bug squash")
                InterfaceAudio.PreviewCue(state.InterfaceSoundVolume, item.BasePoints + item.BonusPoints > 0 ? "positive" : "negative",item.BasePoints+item.BonusPoints>0?state.PositiveFeedbackSound:state.NegativeFeedbackSound);
    }
    private void UpdateFrenzyFeedback()
    {
        var score = state.Widgets.FirstOrDefault(w => w.Kind is "Productivity" or "Productivity Score");
        if(streakOverlay!=null){streakOverlay.SpaceScore=SpaceScores.Total(Continuity.Current(state.Workspace!,store.UserId),state.Productivity);streakOverlay.Ledger=state.Productivity;streakOverlay.Options=Options;streakOverlay.Refresh();}
    }
}
