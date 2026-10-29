using System.IO;
using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckPass026()
    {
        var now=DateTimeOffset.UtcNow;var first=Guid.NewGuid();var ledger=new ProductivityLedger();
        Assert(ProductivityScoring.CollectCandy(ledger,first,now),"Candy can be collected");
        Assert(ledger.Events.Single().BasePoints==1 && ledger.Multiplier(now)==1.5 && ledger.Score==2,"Candy increments first, then awards one base point with rounded multiplier bonus");
        Assert(!ProductivityScoring.CollectCandy(ledger,first,now) && ledger.Score==2,"Duplicate candy cannot score twice");
        ProductivityScoring.DirectSquash(ledger,"fixture",now.AddSeconds(1));ProductivityScoring.CollectCandy(ledger,Guid.NewGuid(),now.AddSeconds(2));
        Assert(ledger.Multiplier(now.AddSeconds(2))==2.5,"Candy and direct squashes share half-step streak increments");
        Assert(ledger.Multiplier(now.AddMinutes(3))==1 && ledger.Score>0,"Expiry retains earned score");
        var record=Continuity.NewDashboard("Test","Study",new DashboardData{Productivity=ledger,Session=new Session{Start=now.AddMinutes(-30),End=now.AddMinutes(30)}}, "fixture");
        SpaceScores.Reconcile(record);var total=record.SpaceProductivityScore;SpaceScores.Reconcile(record);
        Assert(record.SpaceProductivityScore==total,"Repeated reconciliation cannot duplicate cumulative awards");
        var copy=Continuity.Clone(record.Current);copy.SessionId=Guid.NewGuid();record.History.Add(copy);SpaceScores.Reconcile(record);
        Assert(record.SpaceProductivityScore==total,"Copied archive events cannot duplicate cumulative awards");
        var recap=SessionFinalization.Complete(record,now);Assert(recap.ActualSeconds==1800 && recap.ScheduledEnd==now.AddMinutes(30) && record.Current.Data.Session!.End==recap.ScheduledEnd,"Early completion preserves actual duration and scheduled end independently");
        Assert(ReferenceEquals(recap,SessionFinalization.Complete(record,now.AddMinutes(1))),"Finalization is idempotent");
        Assert(PixelTiming.Read(record.Current.Data.Session,now).VisualFraction==1 && record.Current.Data.Session!.Progress(now)==.5,"Visual completion does not falsify actual elapsed progress");
        var stale=Continuity.NewDashboard("Missed","Study",new DashboardData{Session=new Session{Start=now.AddDays(-14),End=now.AddDays(-14).AddHours(1),AutomaticScheduling=true,ScheduledDays=[now.DayOfWeek]}}, "fixture");
        var missed=SessionFinalization.Complete(stale,now);Assert(missed.StartedAt==now.AddDays(-14) && missed.EndedAt==now.AddDays(-14).AddHours(1),"Hibernate recovery never invents a recap for an unrecorded later occurrence");
        Assert(ProductivityRing.Fraction(750)==.5 && ProductivityRing.Fraction(-5)==0,"Activity ring uses current milestone interval and handles signed scores");
        using(var candy=new NexaweenDecorations())
        {
            candy.ConfigureAnimation(new NexaOptions{SeasonalAnimation=false},new Rect(0,0,1500,1000));candy.Update(1500,1000,[],"High");
            var oldIds=candy.AvailableCandy.Select(c=>c.Id).ToHashSet();var site=candy.AvailableCandy.First();Assert(candy.TryCollect(site.Id) && !candy.TryCollect(site.Id),"Collected candy is atomically consumed once");
            candy.ObserveCandy(site.Id,1,true);Assert(!candy.AvailableCandy.Any(c=>c.Id==site.Id),"Bug observation cannot resurrect consumed candy");
            var respawn=System.Diagnostics.Stopwatch.StartNew();while(!candy.AvailableCandy.Any(c=>!oldIds.Contains(c.Id)) && respawn.Elapsed<TimeSpan.FromSeconds(5.5))Pump(TimeSpan.FromMilliseconds(25));Assert(candy.AvailableCandy.Any(c=>!oldIds.Contains(c.Id)),"Collected candy respawns independently when bugs and seasonal animation are disabled");
            var busy=candy.AvailableCandy.First();candy.ObserveCandy(busy.Id,.5,true);Assert(!candy.TryCollect(busy.Id),"Candy being eaten is not collectible");
        }
        var store=new StateStore(Path.Combine(output,"pass026.json"));
        var space=Continuity.NewDashboard("Saved recap","Study",new DashboardData{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Notes","notes")],Session=new Session{Title="Study",Start=now.AddMinutes(-10),End=now.AddMinutes(10)}},store.UserId);
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=space.DashboardId,Dashboards=[space]},store.UserId);model.Workspace!.Settings.ReduceMotion=true;store.Save(model);
        var main=new MainWindow(store){ShowInTaskbar=false,ShowActivated=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-10000,Top=-10000};main.Show();Pump(TimeSpan.FromMilliseconds(100));
        Assert(main.FinalizeSession(now,false),"Real dashboard finalizes through save-before-recap commit");
        var saved=store.Load();var current=Continuity.Current(saved.Workspace!,store.UserId).Current;Assert(current.Recap!=null && !current.IsActive && current.Recap.ActualSeconds==600,"Saved recap and completion survive restart");
        Assert(main.FinalizeSession(now.AddMinutes(1),false) && store.Load().Workspace!.Dashboards.Single().Current.SessionId==current.SessionId,"Duplicate End Session retains the same record identity");
        var recapWindow=new SessionRecapWindow(main,"Saved recap","Book",current,model.Workspace.Settings);recapWindow.Show();Pump(TimeSpan.FromMilliseconds(100));Capture026(recapWindow,"recap-026");recapWindow.Close();
        main.StartQuickStart();Assert(main.TutorialActive,"Guide can be replayed on the real dashboard");main.AdvanceGuide();
        var added=main.AddWidget("Counter");Assert(main.state.Workspace!.Settings.QuickStartStep==2 && main.state.Workspace.Settings.QuickStartWidget==added.Id,"Adding a real widget advances its spotlight guide");
        main.Close();main=new MainWindow(store){ShowInTaskbar=false,ShowActivated=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-10000,Top=-10000};main.Show();Pump(TimeSpan.FromMilliseconds(100));
        Assert(main.TutorialActive && main.state.Workspace!.Settings.QuickStartStep==2,"Incomplete guide resumes its real widget step after restart");
        added=main.state.Widgets.Single(w=>w.Id==added.Id);
        var grip=Descendants(main).OfType<System.Windows.Controls.Primitives.Thumb>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Move "+added.Title);
        grip.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0,0){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragStartedEvent});
        grip.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(0,400){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragDeltaEvent});
        grip.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(0,400,false){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragCompletedEvent});
        Assert(main.state.Workspace!.Settings.QuickStartStep==3,"Moving the real widget advances the guide");Pump(TimeSpan.FromMilliseconds(140));Capture026(main,"guide-resize-026");
        var resize=Descendants(main).OfType<System.Windows.Controls.Primitives.Thumb>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Resize "+added.Title);
        resize.RaiseEvent(new System.Windows.Controls.Primitives.DragStartedEventArgs(0,0){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragStartedEvent});
        resize.RaiseEvent(new System.Windows.Controls.Primitives.DragDeltaEventArgs(60,80){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragDeltaEvent});
        resize.RaiseEvent(new System.Windows.Controls.Primitives.DragCompletedEventArgs(60,80,false){RoutedEvent=System.Windows.Controls.Primitives.Thumb.DragCompletedEvent});
        Assert(main.state.Workspace!.Settings.QuickStartStep==4,"Resizing the real widget advances the guide");
        var title=Descendants(main).OfType<System.Windows.Controls.TextBox>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Widget title "+added.Title);
        title.Text="My wins";Assert(main.state.Workspace!.Settings.QuickStartStep==5,"Renaming the actual widget advances the guide");
        main.AdvanceGuide();Assert(main.state.Workspace!.Settings.QuickStartStep==6,"Optional integrations lead to completion");
        var finish=Descendants(main).OfType<System.Windows.Controls.Button>().Single(b=>Equals(b.Content,"Get Started"));finish.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert(!main.TutorialActive && store.Load().Workspace!.Settings.QuickStartCompleted,"Tutorial completion removes the overlay and persists");
        main.ShowWeeklySchedule();Pump(TimeSpan.FromMilliseconds(50));
        var timerEditor=Application.Current.Windows.OfType<Window>().Single(w=>w is SettingsDialog{SessionInterface:true});
        var mode=Descendants(timerEditor).OfType<System.Windows.Controls.ComboBox>().Single(b=>System.Windows.Automation.AutomationProperties.GetName(b)=="Pixel Timer operating mode");
        var clocks=Descendants(timerEditor).OfType<ClockPicker>().ToArray();clocks[0].Value=TimeSpan.FromHours(9);mode.SelectedIndex=1;clocks[0].Value=TimeSpan.FromHours(13);mode.SelectedIndex=0;
        Assert(clocks[0].Value==TimeSpan.FromHours(9),"Switching modes restores the independent Manual time draft");mode.SelectedIndex=1;
        Assert(clocks[0].Value==TimeSpan.FromHours(13),"Switching back restores the Schedule time draft");timerEditor.Close();
        Assert(store.Load().ManualTimerDraft!=null && store.Load().ScheduleTimerDraft!=null,"Both timer drafts persist through the existing state store");
        main.Close();
        var blockedStore=new StateStore(Path.Combine(output,"blocked-completion.json"));blockedStore.Save(new AppState{Session=new Session{Title="Blocked",Start=now.AddMinutes(-5),End=now.AddMinutes(5)}});
        var blocked=new MainWindow(blockedStore){ShowInTaskbar=false,ShowActivated=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-10000,Top=-10000};blocked.Show();Pump(TimeSpan.FromMilliseconds(40));
        using(var locked=new FileStream(blockedStore.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))
            Assert(!blocked.FinalizeSession(now,false) && blocked.state.Session!.FinalizedAt==null && blocked.HasUnsavedChanges,"Persistence failure prevents completion and retains pending data");
        Assert(blocked.FinalizeSession(now,false),"Completion succeeds after the persistence error resolves");blocked.Close();
        var companion=new HomeSpider();companion.Configure(new Size(1440,940),[]);for(var i=0;i<2000;i++)companion.Step(.05);Assert(companion.Valid(companion.Bug.Position),"Living Home spider remains bounded inside its web territory");
        var world=CreateWorld(BugBehavior.Cozy);world.SetSurfaces([new BugSurface("square",new Rect(100,100,700,500))],new Size(1600,1000));world.WebCoordinateAspect=.25;var bounds=world.WebBounds(new EcosystemWeb{AnchorId="square"});
        Assert(bounds.Width*.25/bounds.Height>=.799 && bounds.Width*.25/bounds.Height<=1.301,"Web geometry compensates for tall Canvas projection");
        Assert(typeof(App).Assembly.GetType("SessionDashboard.HevyConnection")==null,"Retired duplicate provider has no exposed connection implementation");

    }
    private sealed class HevyFixtureHandler:System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token)
        {
            Assert(request.RequestUri!.Host=="api.hevyapp.com" && request.Headers.Contains("api-key") && request.Headers.Authorization==null,"Hevy uses its own API-key header and endpoint, separate from Hedy");
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent("{\"workout_count\":7}")});
        }
    }
}
