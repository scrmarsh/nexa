using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckFollowup()
    {
        var zone="America/Chicago";
        DateTimeOffset At(int year,int month,int day,int hour,int minute=0)=>NexaTime.Instant(new DateTime(year,month,day,hour,minute,0),zone);
        var start=At(2030,1,7,9);var classSchedule=PixelTiming.WeeklyClass("Class",[DayOfWeek.Monday,DayOfWeek.Wednesday],TimeSpan.FromHours(9),TimeSpan.FromHours(10),zone,start);
        var zero=PixelTiming.Read(classSchedule,start);var half=PixelTiming.Read(classSchedule,start.AddMinutes(30));
        Assert(zero.CountUp && zero.VisualFraction==0 && PixelTiming.CompletedPixels(zero,100)==0,"Class begins with zero completed pixels");
        Assert(half.VisualFraction==.5 && half.Time==TimeSpan.FromMinutes(30) && PixelTiming.CompletedPixels(half,100)==50,"Active 9–10 class immediately reads 50% at 9:30");
        var after=PixelTiming.Read(classSchedule,classSchedule.End);var next=after.PhaseEnd!.Value;
        Assert(after.VisualFraction==1 && !after.CountUp && after.Time==TimeSpan.FromHours(47),"Class completion immediately begins the full intersession countdown");
        Assert(PixelTiming.CompletedPixels(PixelTiming.Read(classSchedule,next.AddTicks(-1)),100)==1 && PixelTiming.CompletedPixels(PixelTiming.Read(classSchedule,next),100)==0,"The last bottom-right-clearing prefix empties exactly at the next start");
        var missed=PixelTiming.Read(classSchedule,At(2030,1,16,10));
        Assert(missed.PhaseStart==At(2030,1,16,10) && missed.VisualFraction==1,"A missed week's countdown uses its latest occurrence end");
        var edited=Continuity.Clone(classSchedule);edited.Start=start.AddMinutes(15);edited.End=start.AddMinutes(75);edited.RecurringStartMinutes=9*60+15;edited.RecurringEndMinutes=10*60+15;
        Assert(PixelTiming.Read(edited,start.AddMinutes(30)).VisualFraction==.25,"Edited start/end change authoritative progress immediately");
        var linked=WidgetLibrary.Create("Progress bar");
        Assert(PixelTiming.ForWidget(linked,classSchedule,start.AddMinutes(30)).VisualFraction==half.VisualFraction,"Session-linked Progress Bar shares Pixel Timer progress");
        linked.ProgressLinkedToSession=false;linked.TimerStart=start;linked.TimerEnd=start.AddHours(2);
        Assert(PixelTiming.ForWidget(linked,classSchedule,start.AddMinutes(30)).VisualFraction==.25,"An independent Progress Bar keeps its own timestamps");
        Assert(PixelTiming.UntilNextSecond(start.AddMilliseconds(750))==TimeSpan.FromMilliseconds(250) && PixelTiming.UntilNextSecond(start)==TimeSpan.FromSeconds(1),"Refresh scheduling aligns to actual UTC second boundaries");
        var utc=At(2026,7,1,9);Assert(NexaTime.Local(utc,"America/New_York").Hour==10 && NexaTime.Local(utc,zone).Hour==9,"Central and Eastern displays represent the same UTC instant without a one-hour conversion error");
        int steps;Assert(NexaSelection.Accumulate(0,120,out steps)==0 && steps==1,"One wheel notch produces one controlled selection increment");
        var rest=NexaSelection.Accumulate(0,40,out steps);Assert(steps==0 && NexaSelection.Accumulate(rest,80,out steps)==0 && steps==1,"Trackpad sub-notch deltas accumulate without jumping values");
        NexaSelection.Accumulate(0,480,out steps);Assert(steps==1,"A large wheel event cannot skip several time values");
        Assert(!NexaDialog.BlankPanelDragTarget(new ComboBoxItem(),new Grid()),"Clicking a dropdown item cannot initiate panel dragging");
        Assert(!NexaDialog.BlankPanelDragTarget(new ListBoxItem(),new Grid()),"Clicking a list item cannot initiate panel dragging");
        Assert(ExperimentalUpdater.ReleasesUri.AbsolutePath=="/repos/scrmarsh/nexa/releases","The optional updater uses the canonical Nexa GitHub repository");
        using(var client=new System.Net.Http.HttpClient(new FollowupReleaseHandler("scrmarsh/nexa")))Assert(new ExperimentalUpdater(client).CheckAsync(new Version(0,1,24)).GetAwaiter().GetResult()?.Version==new Version(0,1,25),"Release URL validation accepts the canonical Nexa repository");
        using(var client=new System.Net.Http.HttpClient(new FollowupReleaseHandler("someone/else")))
        {var rejected=false;try{new ExperimentalUpdater(client).CheckAsync(new Version(0,1,24)).GetAwaiter().GetResult();}catch(InvalidDataException){rejected=true;}Assert(rejected,"Foreign repository assets are rejected with an explicit invalid-package result");}
        CheckFollowupScore(start);CheckFollowupBugs();CheckFollowupUi(classSchedule,start.AddMinutes(30));
    }
    private static void CheckFollowupScore(DateTimeOffset now)
    {
        var ledger=new ProductivityLedger();ProductivityScoring.DirectSquash(ledger,"a",now);
        Assert(ledger.Multiplier(now)==1.5 && ledger.FrenzyUntil==now.AddMinutes(2),"First direct squash starts a two-minute 1.5× streak");
        ProductivityScoring.DirectSquash(ledger,"b",now.AddSeconds(90));
        Assert(ledger.Multiplier(now.AddSeconds(90))==2 && ledger.FrenzyUntil==now.AddSeconds(210),"A valid later squash adds 0.5 and refreshes two minutes");
        for(var i=0;i<10;i++)ProductivityScoring.DirectSquash(ledger,"cap"+i,now.AddSeconds(91+i),2.5);
        Assert(ledger.Multiplier(now.AddSeconds(101))==2.5,"Configured multiplier cap bounds a long streak");
        ProductivityScoring.Add(ledger,"task","Task completed","Task",100,now.AddSeconds(101));
        Assert(ledger.Events.Last().BonusPoints==150,"Central scoring uses the actual capped multiplier");
        var until=ledger.FrenzyUntil;ProductivityScoring.DirectSquash(ledger,"late-history",now.AddSeconds(5));
        Assert(ledger.FrenzyUntil==until,"A delayed historical squash cannot overwrite newer streak timestamps");
        Assert(ledger.Multiplier(until!.Value)==1 && ledger.Multiplier(now.AddSeconds(-1))==1,"Elapsed real time ends a streak after resume, and actions before it have no bonus");
        var count=ledger.BugsSquashed;ProductivityScoring.DirectSquash(ledger,"b",now.AddSeconds(110));Assert(ledger.BugsSquashed==count,"Replaying a squash cannot extend or farm a streak");
        var before=new DashboardData();var counter=WidgetLibrary.Create("Counter");counter.CounterMode="Distraction";before.Widgets=[counter];
        var after=Continuity.Clone(before);after.Widgets[0].Value=1;ProductivityScoring.Observe(before,after,now,new NexaOptions{DistractionPenalty=10});
        Assert(after.Productivity.Score==-10 && after.Productivity.Events.Last().Category=="Distraction","Only configured negative categories deduct points");
        var positive=Continuity.Clone(before);positive.Widgets[0].Value=1;ProductivityScoring.Observe(before,positive,now,new NexaOptions());
        Assert(positive.Productivity.Score==0,"A distraction has no negative score unless the user enables a rule");
    }
    private static void CheckFollowupBugs()
    {
        var fresh=new EcosystemOptions();Assert(fresh.ForSeason(false).Behavior==BugBehavior.Classic,"Fresh nonseasonal setup defaults to Classic");
        var seasonal=fresh.ForSeason(true);Assert(seasonal.Behavior==BugBehavior.Classic && seasonal.Adorable,"Nexaween retains the single authoritative behavior preference");
        seasonal=seasonal.ChooseBehavior(BugBehavior.Creepy,true);var normal=seasonal.ForSeason(false).ChooseBehavior(BugBehavior.Combative,false);
        Assert(normal.ForSeason(true).Behavior==BugBehavior.Combative && normal.ForSeason(true).ForSeason(false).Behavior==BugBehavior.Combative,"Season toggling keeps the single authoritative behavior");
        foreach(var mode in Enum.GetValues<BugBehavior>())
        {
            var option=new EcosystemOptions{Behavior=mode,Appearance=BugAppearance.Creepy,MatchAppearance=false};
            Assert(option.EffectiveAppearance==BugAppearance.Creepy,mode+" preserves the independently selected Creepy appearance");
        }
        var world=CreateWorld(BugBehavior.Cozy);Step(world,100);var id=world.WorldId;
        world.Configure(new(true,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions{Behavior=BugBehavior.Cozy}));
        Assert(world.Bugs.Count==12 && world.WorldId==id,"Lowering density immediately trims the maintained population without resetting world identity");
        world.Configure(new(true,BugSpecies.Spiders,BugDensity.Normal,new EcosystemOptions{Behavior=BugBehavior.Creepy}));Step(world,30);
        Assert(world.Bugs.Count>0 && world.Bugs.All(b=>b.Species==BugSpecies.Spiders),"Selecting a species immediately replaces incompatible population and spawns the selected type");
        world.SetSurfaces([new BugSurface("widget",new Rect(200,200,200,200))],new Size(1600,1000));var waypoint=world.VisibleWaypoint(new Point(100,300),new Point(600,300));
        Assert(waypoint!=new Point(600,300) && !new Rect(200,200,200,200).Contains(waypoint),"Ground paths route around opaque widget surfaces");
        Assert(!new Rect(200,200,200,200).Contains(world.VisibleWaypoint(new Point(300,300),new Point(600,300))),"A widget moved over a creature provides a free-edge recovery target");
    }
    private static void CheckFollowupUi(Session schedule,DateTimeOffset now)
    {
        var store=new StateStore(Path.Combine(output,"followup-ui.json"));var model=new AppState{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),WidgetLibrary.Create("Progress bar"),WidgetLibrary.Create("Productivity Score"),WidgetLibrary.Create("Session timeline")]};
        store.Save(model);var main=new MainWindow(store){Left=-10000,Top=-10000,ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual};main.Show();Pump(TimeSpan.FromMilliseconds(100));main.ApplyClassSchedule(schedule,false);main.UpdateClock(now);
        Assert(main.PercentLabel.Text=="50.000%" && main.SessionProgressBar.Value==50,"Pixel Timer label and embedded bar immediately share 50% elapsed");
        Assert(Descendants(main).OfType<TextBlock>().Count(t=>t.Text=="50.000%")>=1,"Permanent Pixel Timer reflects the same captured instant; redundant standalone bar stays hidden");
        var global=new SettingsDialog(main,store,main.state,main,"Add Bugs");global.Show();Pump(TimeSpan.FromMilliseconds(80));
        var globalButtons=Descendants(global).OfType<Button>().Where(b=>b.Content is string).Select(b=>b.Content.ToString()).ToArray();
        Assert(globalButtons.Contains("Sounds") && !globalButtons.Contains("Session") && !globalButtons.Contains("Seasonal Events") && !globalButtons.Contains("Appearance & Effects"),"Global navigation has one Appearance and Sounds, with no duplicate Session/Seasonal tabs");
        Assert(!Descendants(global).OfType<CheckBox>().Any(c=>Equals(c.Content,"Match Appearance to Behavior")) && !Descendants(global).OfType<TextBlock>().Any(t=>t.Text=="Appearance override"),"Redundant appearance override controls are absent");
        Assert(Descendants(global).OfType<ComboBox>().Any(c=>c.Items.OfType<BugBehavior>().Count()==3),"Classic, Cozy and Combative are available with independent appearance");
        Assert(Equals(Descendants(global).OfType<ComboBox>().Single(c=>c.Items.OfType<BugBehavior>().Count()==3).SelectedItem,NexaEcosystem.For(store).Manager.EcosystemSettings.Behavior),"The first-open behavior selector reflects the effective seasonal world mode");
        global.Close();var session=new SettingsDialog(main,store,main.state,main,"Session");session.Show();Pump(TimeSpan.FromMilliseconds(80));
        Assert(Descendants(session).OfType<Button>().Any(b=>Equals(b.Content,"Notes & History")) && Descendants(session).OfType<CheckBox>().Any(c=>Equals(c.Content,"Enable Tips")),"The Session interface contains history and the Tips setting");
        Assert(!Descendants(session).OfType<TextBlock>().Any(t=>t.Text is "Selected days" or "Summary interval (minutes)"),"Session interface has no duplicate weekday or summary-interval selection");
        session.Close();Snapshot(main,"followup-dashboard",1220,880);main.Close();
    }
    private sealed class FollowupReleaseHandler(string repository):System.Net.Http.HttpMessageHandler
    {
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token)
        {
            var json=System.Text.Json.JsonSerializer.Serialize(new[]{new{tag_name="v0.1.25-alpha",name="Synthetic release",prerelease=true,draft=false,assets=new[]{new{name="Nexa-0.1.25-Alpha-Setup.exe",size=123,digest="sha256:"+new string('a',64),browser_download_url="https://github.com/"+repository+"/releases/download/v0.1.25-alpha/Nexa-0.1.25-Alpha-Setup.exe"}}}});
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent(json)});
        }
    }

}
