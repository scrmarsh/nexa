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
        Activated += (_, _) => { bugGeometryDirty = true; UpdateBugGeometry(); };
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
            ecosystem.ReconcileGeneration(state.Workspace?.LiveEcosystemGeneration??Guid.Empty);
            bugsOverlay = new BugOverlay(ecosystem.Manager); bugsOverlay.ScrollTarget=WidgetScroller; bugsOverlay.DirectSquashed += DashboardSquashed;
            Panel.SetZIndex(bugsOverlay, 40); ((Grid)Content).Children.Add(bugsOverlay);
            ecosystem.Attach(this, () => IsVisible && WindowState != WindowState.Minimized && DashboardShell.IsVisible);
            ecosystem.Changed += EcosystemChanged;
        }
        bugsOverlay.ReduceMotion=Options.ReduceMotion;
        ecosystem!.Manager.WorldPaused = false; // Add Bugs is now the overlay enable/resume control.
        ecosystem.ApplyPreferences(preferences); ecosystem.ApplySeason(Options);
        bugGeometryDirty = true; UpdateBugActivity(); EcosystemChanged();
    }
    private void DashboardSquashed(BugEntity bug) { if(ecosystem!=null)_=ecosystem.RecordSquashResponsive(bug,this); }
    internal bool RecordEcosystemSquash(BugEntity bug, DateTimeOffset? at = null,bool persist=true)
    {
        if (ProductivityScoring.DirectSquash(state.Productivity, bug.InteractionId.ToString(), at ?? DateTimeOffset.UtcNow,Options.StreakMultiplierCap,bug.Species.ToString()))
        { state.Workspace!.AllTimeSquashes++;RecordSpeciesTotal(bug.Species); Continuity.Current(state.Workspace,store.UserId).Current.Data.Productivity=state.Productivity; Changed(scheduleSave:false); }
        return !persist || SaveNow();
    }
    internal bool ApplyPendingSquash(BugEntity bug,DateTimeOffset at,Guid sessionId)
    {
        if(closing || !IsLoaded || state.Workspace is not {} workspace)return false;
        var current=Continuity.Current(workspace,store.UserId).Current;
        if(sessionId==Guid.Empty || current.SessionId==sessionId)return RecordEcosystemSquash(bug,at,false);
        var target=workspace.Dashboards.SelectMany(d=>d.History.Append(d.Current)).FirstOrDefault(s=>s.SessionId==sessionId);
        if(target==null)return false;
        var ledger=target.Data.Productivity;var score=ledger.Score;
        if(ProductivityScoring.DirectSquash(ledger,bug.InteractionId.ToString(),at,Options.StreakMultiplierCap,bug.Species.ToString()))
        {workspace.AllTimeSquashes++;RecordSpeciesTotal(bug.Species);workspace.AllTimeProductivityScore+=ledger.Score-score;Changed(scheduleSave:false);}
        return true;
    }
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
        bugsOverlay.SetViewport(WidgetScroller.TransformToVisual(bugsOverlay).TransformBounds(new Rect(WidgetScroller.RenderSize)));
        bugsOverlay.SetWorldMapping(origin, new Size(WidgetCanvas.ActualWidth, WidgetCanvas.ActualHeight));
        var rectangles = widgetFrames.Select(pair => new Rect(Canvas.GetLeft(pair.Value), Canvas.GetTop(pair.Value), Math.Max(0, pair.Value.ActualWidth), Math.Max(0, pair.Value.ActualHeight))).ToArray();
        if (PixelObstacle is { } pixels) rectangles = rectangles.Append(pixels).ToArray();
        bugsOverlay.SetProtectedWidgets(rectangles);
        bugsOverlay.DecorationTargets=seasonalArtwork?.Anchors.Select(p=>new Point(p.X*BugManager.WorldWidth/WidgetCanvas.ActualWidth,p.Y*BugManager.WorldHeight/WidgetCanvas.ActualHeight)).ToArray() ?? [];
        if (IsActive || ecosystem?.ViewCount == 1)
        {
            var scaleX = BugManager.WorldWidth / WidgetCanvas.ActualWidth; var scaleY = BugManager.WorldHeight / WidgetCanvas.ActualHeight;
            IEnumerable<BugSurface> surfaces = widgetFrames.Select(pair => new BugSurface(pair.Key.ToString(), new Rect(Canvas.GetLeft(pair.Value) * scaleX, Canvas.GetTop(pair.Value) * scaleY, Math.Max(0, pair.Value.ActualWidth) * scaleX, Math.Max(0, pair.Value.ActualHeight) * scaleY)));
            if(PixelObstacle is {} pixelBounds)surfaces=surfaces.Append(new BugSurface("pixel-timer",new Rect(pixelBounds.X*scaleX,pixelBounds.Y*scaleY,pixelBounds.Width*scaleX,pixelBounds.Height*scaleY)));
            bugsOverlay.Manager.SetSurfaces(surfaces, new Size(BugManager.WorldWidth, BugManager.WorldHeight));
            var visible = WidgetScroller.TransformToVisual(WidgetCanvas).TransformBounds(new Rect(WidgetScroller.RenderSize));
            bugsOverlay.Manager.VisibleBounds = new Rect(visible.X * scaleX, visible.Y * scaleY, visible.Width * scaleX, visible.Height * scaleY);
        }
    }
    private Guid audibleBug;
    private System.Windows.Point? audibleBugPosition;
    private void PlayCrawling()
    {
        var bug=ecosystem?.Manager.Bugs.FirstOrDefault(b=>b.Health>0 && b.State is BugState.Edge or BugState.Crossing or BugState.Landed);
        if(bug==null){audibleBugPosition=null;return;}
        var moved=audibleBug==bug.InteractionId && audibleBugPosition is {} previous && (previous-bug.Position).LengthSquared>.01;
        audibleBug=bug.InteractionId;audibleBugPosition=bug.Position;
        if(moved && NexaEffectsAudio.Allowed(NexaSoundEffect.Crawl,state,Options,IsActive && IsVisible && WindowState!=WindowState.Minimized && DashboardShell.IsVisible && bugsOverlay?.IsVisible==true && ecosystem?.Manager.WorldPaused==false))
            NexaEffectsAudio.Play(NexaSoundEffect.Crawl,state.InterfaceSoundVolume);
    }
    private void EcosystemChanged()
    {
        if (bugsOverlay == null || ecosystem == null) return;
        ReconcileVisibleCandy();UpdateBugGeometry(); bugsOverlay.RefreshFrame(); BugFrameCount++;PlayCrawling();
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
