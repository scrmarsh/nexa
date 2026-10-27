using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SessionDashboard;

public partial class MainWindow
{
    private NexaNotifications? notifications;
    private bool reliabilityReady, reconciling, saveFailureVisible;
    private DateTimeOffset lastReconcile, lastResetWarning;
    private readonly DispatcherTimer saveRetry = new() { Interval = TimeSpan.FromSeconds(5) };
    private NexaOptions Options => state.Workspace?.Settings ?? new();
    private void InitializeReliability()
    {
        notifications = new NexaNotifications((Grid)Content, () => NexaMotion.Allowed(Options));
        InitializeFeedback();
        FooterStatus.Visibility = Visibility.Collapsed; DashboardTip.Visibility = Visibility.Collapsed;
        var feedback = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
        feedback.AddValueChanged(Feedback, FeedbackChanged);
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.TimeChanged += ClockChanged;
        saveRetry.Tick += async (_, _) => { if (!dirty) { saveRetry.Stop(); return; } await SaveInBackground(); };
        Closed += (_, _) =>
        {
            reliabilityReady = false; feedback.RemoveValueChanged(Feedback, FeedbackChanged);
            SystemEvents.TimeChanged -= ClockChanged;
            SystemEvents.PowerModeChanged -= PowerChanged; saveRetry.Stop(); notifications.Dispose();
        };
        reliabilityReady = true; ApplyReliabilityOptions();
    }
    private void FeedbackChanged(object? sender, EventArgs e)
    { if (!string.IsNullOrWhiteSpace(Feedback.Text) && !Feedback.Text.StartsWith("Could not save changes:", StringComparison.Ordinal)) notifications?.Show("feedback", Feedback.Text); }
    private void ClockChanged(object? sender, EventArgs e)
    {
        TimeZoneInfo.ClearCachedData();
        Dispatcher.BeginInvoke(new Action(() => { if (!closing) { lastReconcile = default; ReconcileSchedule(DateTimeOffset.UtcNow); UpdateClock(); } }));
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            Dispatcher.BeginInvoke(new Action(() => { if (!closing) { ReconcileSchedule(DateTimeOffset.UtcNow); UpdateClock(); } }));
        else if (e.Mode == PowerModes.Suspend)
            Dispatcher.BeginInvoke(new Action(() => { if (!closing) SaveNow(); }));
    }
    internal void ApplyReliabilityOptions()
    {
        autosave.Interval = TimeSpan.FromSeconds(Options.EffectiveSaveSeconds);
        ApplySeasonalOptions();
        feedbackOverlay?.ReconcileOptions(); notifications?.ReconcileMotion();
        if (!Options.ShowQuickTips) { notifications?.Dismiss("tip"); tipRotation.Stop(); }
        else if (!closing) tipRotation.Start();
        if(widgetsReady)UpdateWidgetClocks(DateTimeOffset.UtcNow);
        if (!Options.ShowAutosaveNotifications && !saveFailureVisible) notifications?.Dismiss("autosave");
    }
    private void ManualSaveClick(object sender, RoutedEventArgs e)
    {
        if (SaveNow()) { notifications?.Dismiss("autosave"); notifications?.Show("manual-save", "Saved now. Your dashboard is ready to review.", "✓", seconds: 2); }
        else notifications?.Show("autosave", NexaNotifications.FailureMessage, "⚠", 3, null);
    }
    private void ReconcileSchedule(DateTimeOffset now)
    {
        if (!reliabilityReady || reconciling || closing || (now >= lastReconcile && now - lastReconcile < TimeSpan.FromSeconds(1))) return;
        reconciling = true; lastReconcile = now;
        ApplySeasonalOptions(now);
        try
        {
            var count = state.Productivity.Events.Count;
            if (ProductivityScoring.CaptureCompletions(state, now))
            {
                Changed();
                foreach (var completed in state.Productivity.Events.Skip(count).Where(e => e.Key.StartsWith("timer:", StringComparison.Ordinal)))
                    notifications?.Show(completed.Key, completed.Description, "✓", 1, 8);
            }
            if (state.Session is not { } session || state.Workspace == null) return;
            var current = Continuity.Current(state.Workspace, store.UserId).Current;
            if (session.StoppedAt == null && now >= session.End && current.CompletedCheckpointAt == null)
            {
                current.CompletedCheckpointAt = session.End;
                if (SaveNow()) notifications?.Show("session-complete", "Session complete. Your notes and widgets remain available for review.", "✓", 1, 8);
                else current.CompletedCheckpointAt = null;
            }
            if (current.CompletedCheckpointAt is { } checkpoint && checkpoint < session.End.AddMinutes(5) && now >= session.End.AddMinutes(5))
            {
                current.CompletedCheckpointAt = now;
                if (!SaveNow()) current.CompletedCheckpointAt = checkpoint;
            }
            if (session.StoppedAt != null || !session.AutomaticScheduling) return;
            var latest = WeeklySchedule.Latest(session, now);
            var target = latest != null && latest.Start > session.Start && latest.End > now ? latest : WeeklySchedule.Next(session, now);
            if (target == null || target.Start <= session.Start || now < session.End) return;
            var reset = target.Start.AddMinutes(-5);
            if (now >= reset.AddMinutes(-5) && now < reset && lastResetWarning != target.Start && !current.ResetDeferred)
            {
                lastResetWarning = target.Start;
                var localReset = NexaTime.Local(reset, session.TimeZoneId); var localStart = NexaTime.Local(target.Start, session.TimeZoneId);
                notifications?.Show("reset-warning", $"Your dashboard will save and reset at {NexaTime.Clock(reset, Options)} for your {NexaTime.Clock(target.Start, Options)} session.", "⚠", 2, null,
                    () => { current.ResetDeferred = true; current.ResetPostponedFor = target.Start; Changed(); SaveNow(); ShowDeferredReset(); }, "Postpone Reset");
            }
            if (current.ResetDeferred) { if (lastResetWarning != DateTimeOffset.MaxValue) ShowDeferredReset(); return; }
            if (now >= reset) PrepareOccurrence(target, now);
        }
        finally { reconciling = false; }
    }
    private void ShowDeferredReset()
    {
        lastResetWarning = DateTimeOffset.MaxValue;
        notifications?.Show("reset-warning", "Reset postponed. Your current work remains visible.", "ℹ", 2, null, CompleteDeferredReset, "Save and reset now");
    }
    internal void CompleteDeferredReset() => CompleteDeferredResetAt(DateTimeOffset.UtcNow);
    internal void CompleteDeferredResetAt(DateTimeOffset now)
    {
        if (state.Session is not { } session) return;
        var latest = WeeklySchedule.Latest(session, now);
        var target = latest != null && latest.Start > session.Start && latest.End > now ? latest : WeeklySchedule.Next(session, now);
        if (target != null) PrepareOccurrence(target, now);
    }
    private void PrepareOccurrence(SessionOccurrence occurrence, DateTimeOffset now)
    {
        if (!SaveNow()) return;
        var candidate = CopyWorkspaceState().Workspace!;
        var dashboard = Continuity.Current(candidate, store.UserId);
        var seed = Continuity.Clone(dashboard.Current.Data.Session!);
        var data = WeeklySchedule.ResetSessionData(dashboard.Current.Data);
        seed.BeforeSessionCountdownFrom = seed.End;
        seed.Start = occurrence.Start; seed.End = occurrence.End; seed.TimeZoneId = NexaTime.Zone(seed.ScheduleTimeZoneId ?? seed.TimeZoneId).Id; seed.StoppedAt = null;
        seed.ReachedMilestones.Clear(); seed.IntervalNotes.Clear(); seed.HedyIntervalNotes.Clear(); seed.IntervalSummaries.Clear();
        seed.TimelineEntries.Clear(); seed.HedySessionId = ""; seed.HedyTranscript = ""; seed.HedyBaseline = ""; seed.LocalRecordingPath = ""; seed.LocalRecordingSeconds = 0;
        data.Session = seed; Continuity.Archive(dashboard, true);
        dashboard.Current = new DashboardSession { DashboardId = dashboard.DashboardId, UserId = store.UserId, Title = seed.Title, PresetId = dashboard.Current.PresetId, PresetName = dashboard.Current.PresetName, Data = data, ResetForStart = occurrence.Start, OpenedAt = now.ToUniversalTime() };
        if (CommitWorkspace(candidate))
        {
            lastResetWarning = default; notifications?.Dismiss("reset-warning");
            notifications?.Show("reset-complete", "Session saved to Notes & History. Your next session is ready.", "✓", 1);
        }
    }
    private readonly Dictionary<Guid,FrameworkElement> scoreAreas=[];
    private void BuildProductivityWidget(StackPanel panel, WidgetInstance widget, bool score)
    {
        TextBlock? prominent=null,multiplier=null;
        System.Windows.Shapes.Path? lightning=null;bool boltActive=false;
        if(score)
        {
            var center=new StackPanel{HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,8,0,10)};
            var bolt=new System.Windows.Shapes.Path{Data=System.Windows.Media.Geometry.Parse("M12,0 L2,14 L10,14 L7,27 L23,10 L14,10 L18,0 Z"),Width=30,Height=36,Stretch=System.Windows.Media.Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center};bolt.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"PixelBrush");center.Children.Add(bolt);lightning=bolt;bolt.RenderTransformOrigin=new Point(.5,.5);bolt.RenderTransform=new System.Windows.Media.ScaleTransform(1,1);
            bolt.Unloaded+=(_,_)=>{if(bolt.RenderTransform is System.Windows.Media.ScaleTransform scale){scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty,null);}};
            prominent=new TextBlock{FontSize=42,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center};center.Children.Add(prominent);
            multiplier=new TextBlock{FontSize=22,FontWeight=FontWeights.SemiBold,HorizontalAlignment=HorizontalAlignment.Center};center.Children.Add(multiplier);
            var area=new Border{Child=center,Padding=new Thickness(12),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,0,8)};area.SetResourceReference(Border.BackgroundProperty,"SurfaceBrush");panel.Children.Add(area);scoreAreas[widget.Id]=area;
        }
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) };
        panel.Children.Add(text);
        var expanded = new CheckBox { Content = "Expanded statistics", IsChecked = widget.DisplayMode == "Expanded" };
        expanded.Click += (_, _) => { widget.DisplayMode = expanded.IsChecked == true ? "Expanded" : "Compact"; Changed(); };
        panel.Children.Add(expanded);
        var categories = new[] { "Task created", "Task completed", "Task reopened", "Notes", "Water", "Scripture", "Timer", "Timer activity", "Counter", "Widget", "Study", "Integrations", "Bug squash" };
        var filters = new WrapPanel();
        foreach (var category in categories)
        {
            var check = new CheckBox { Content = category, IsChecked = !widget.HiddenProductivityCategories.Contains(category), Margin = new Thickness(0, 0, 12, 8) };
            check.Click += (_, _) => { if (check.IsChecked == true) widget.HiddenProductivityCategories.Remove(category); else if (!widget.HiddenProductivityCategories.Contains(category)) widget.HiddenProductivityCategories.Add(category); Changed(); };
            filters.Children.Add(check);
        }
        panel.Children.Add(new Expander { Header = "Statistics to show", Content = filters, Margin = new Thickness(0, 8, 0, 8) });
        string previous = "";
        widgetTicks.Add(now =>
        {
            var ledger = state.Productivity;
            if(prominent!=null)
            {
                prominent.Text=ledger.Score.ToString("N0");multiplier!.Text=ledger.Multiplier(now).ToString("0.0")+"×";
                var active=ledger.Multiplier(now)>1 && NexaMotion.Allowed(Options) && Options.CelebrationEffects;
                if(active!=boltActive && lightning?.RenderTransform is System.Windows.Media.ScaleTransform scale)
                {
                    boltActive=active;var pulse=active?new System.Windows.Media.Animation.DoubleAnimation(.96,1.06,TimeSpan.FromSeconds(1.2)){AutoReverse=true,RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever}:null;
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty,pulse);scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty,pulse);
                }
            }
            var visible = ledger.Events.Where(e => !widget.HiddenProductivityCategories.Contains(e.Category)).ToArray();
            var value = score ? $"Base: {ledger.Events.Sum(e => e.BasePoints):N0} · Bonus: {ledger.Events.Sum(e => e.BonusPoints):N0}\n" +
                (ledger.FrenzyUntil > now ? $"Streak · {ledger.Multiplier(now):0.0}× · {(ledger.FrenzyUntil.Value - now).TotalSeconds:0}s\n" : "") +
                $"Next milestone: {(ledger.Score / 500 + 1) * 500:N0}" + (expanded.IsChecked == true ? $"\nAll-time score: {state.Workspace?.AllTimeProductivityScore ?? 0:N0}" : "")
                : $"Meaningful actions: {ledger.Events.Count:N0}\nCompleted tasks: {ledger.Events.Count(e => e.Category == "Task completed"):N0}";
            if (expanded.IsChecked == true)
                value += "\n\n" + string.Join("\n", visible.GroupBy(e => e.Category).Select(g => $"{g.Key}: {g.Count()} · {g.Sum(e => e.BasePoints + e.BonusPoints)} pts"));
            value += "\n\n" + string.Join("\n", visible.TakeLast(expanded.IsChecked == true ? 12 : 4).Reverse().Select(e => $"{NexaTime.Local(e.At, Options.TimeZoneId):t} · {e.Description} ({e.BasePoints + e.BonusPoints:+0;-0;0})"));
            if (value == previous) return; previous = value; text.Text = value;
        });
    }
}
