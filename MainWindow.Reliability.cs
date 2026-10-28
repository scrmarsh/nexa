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
        if(bugsOverlay!=null)bugsOverlay.ReduceMotion=Options.ReduceMotion;
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
                if(session.FinalizedAt==null){if(!FinalizeSession(now,false))return;session=state.Session!;current=Continuity.Current(state.Workspace!,store.UserId).Current;}
                current.CompletedCheckpointAt = session.End;
                if (SaveNow()) notifications?.Show("session-complete", "Session complete. Your notes and widgets remain available for review.", "✓", 1, 8);
                else current.CompletedCheckpointAt = null;
            }
            if (current.CompletedCheckpointAt is { } checkpoint && checkpoint < session.End.AddMinutes(5) && now >= session.End.AddMinutes(5))
            {
                current.CompletedCheckpointAt = now;
                if (!SaveNow()) current.CompletedCheckpointAt = checkpoint;
            }
            if (session.StoppedAt != null && session.FinalizedAt==null || !session.AutomaticScheduling) return;
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
        seed.Start = occurrence.Start; seed.End = occurrence.End; seed.TimeZoneId = NexaTime.Zone(seed.ScheduleTimeZoneId ?? seed.TimeZoneId).Id; seed.StoppedAt = null; seed.FinalizedAt=null;
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
        var row=new Grid();row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var ring=new ProductivityRing {Width=108,Height=108,HorizontalAlignment=HorizontalAlignment.Center};
        row.Children.Add(ring);scoreAreas[widget.Id]=ring;
        var side=new StackPanel {VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8,0,0,0)};
        var multiplier=new TextBlock {FontSize=20,FontWeight=FontWeights.SemiBold,HorizontalAlignment=HorizontalAlignment.Center};
        var badge=new Border{Child=multiplier,CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1),Padding=new Thickness(7,4,7,4),Margin=new Thickness(0,0,0,8)};
        badge.SetResourceReference(Border.BorderBrushProperty,"LineBrush");side.Children.Add(badge);
        var details=new Button {Content="···",ToolTip="Productivity details",FontSize=18,Padding=new Thickness(8,0,8,0),MinHeight=26};
        System.Windows.Automation.AutomationProperties.SetName(details,"Productivity details");
        details.Click+=(_,_)=>ShowProductivityDetails(widget);side.Children.Add(details);Grid.SetColumn(side,1);row.Children.Add(side);panel.Children.Add(row);
        widgetTicks.Add(now=>{
            ring.Update(SpaceScores.Total(Continuity.Current(state.Workspace!,store.UserId),state.Productivity),Options);
            var value=state.Productivity.Multiplier(now);badge.Visibility=value>1?Visibility.Visible:Visibility.Collapsed;
            multiplier.Text=value.ToString("0.0")+"×";
        });
    }
    private void ShowProductivityDetails(WidgetInstance widget)
    {
        var dialog=new NexaPanel {Owner=this,Title="nexa · Productivity details",Width=660,Height=560,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var panel=new StackPanel{Margin=new Thickness(22)};
        var ledger=state.Productivity;
        panel.Children.Add(new TextBlock{Text="Your productivity",FontSize=26,FontWeight=FontWeights.SemiBold});
        var statistics=new WrapPanel();
        foreach(var item in new[]{("Actions",(long)ledger.Events.Count),("Tasks",(long)ledger.Events.Count(e=>e.Category=="Task completed")),("Base points",ledger.Events.Sum(e=>(long)e.BasePoints)),("Bonus points",ledger.Events.Sum(e=>(long)e.BonusPoints))})
        {
            var text=new StackPanel();text.Children.Add(new TextBlock{Text=item.Item2.ToString("N0"),FontSize=27,FontWeight=FontWeights.Bold});text.Children.Add(new TextBlock{Text=item.Item1,FontSize=13});
            var card=new Border{Child=text,Padding=new Thickness(16),CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1),Margin=new Thickness(0,16,10,10)};card.SetResourceReference(Border.BorderBrushProperty,"LineBrush");statistics.Children.Add(card);
        }
        panel.Children.Add(statistics);
        panel.Children.Add(new TextBlock{Text="Recent recorded activity",FontSize=18,Margin=new Thickness(0,14,0,8)});
        foreach(var item in ledger.Events.Where(e=>!widget.HiddenProductivityCategories.Contains(e.Category)).TakeLast(100).Reverse())
            panel.Children.Add(new TextBlock{Text=$"{NexaTime.Local(item.At,Options.TimeZoneId):t} · {item.Description} ({item.BasePoints+item.BonusPoints:+0;-0;0})",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,4)});
        dialog.Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};dialog.ShowDismissible();
    }
}
