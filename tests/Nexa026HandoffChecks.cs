using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using SessionDashboard;

internal static partial class Program
{
    private static void WaitHandoff(Func<bool> done,string description,int milliseconds=6000)
    {
        var clock=Stopwatch.StartNew();while(!done() && clock.ElapsedMilliseconds<milliseconds)Pump(TimeSpan.FromMilliseconds(15));
        Assert(done(),description);
    }
    private static T OffscreenHandoff<T>(T window) where T:Window
    {window.ShowActivated=false;window.ShowInTaskbar=false;window.WindowStartupLocation=WindowStartupLocation.Manual;window.Left=-10000;window.Top=-10000;return window;}
    private static void ClickHandoff(DependencyObject root,string text)=>Descendants(root).OfType<Button>().Single(b=>Equals(b.Content,text)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static Button[] CardsHandoff(HomeWindow home)=>Descendants(home.Recent).OfType<Button>().Where(b=>b.Tag is Guid).ToArray();
    private static void CheckHandoff026()
    {
        var store=new StateStore(Path.Combine(output,"handoff.json"));
        var spaces=Enumerable.Range(0,12).Select(i=>Continuity.NewDashboard("Synthetic Space "+i,"Study",new DashboardData{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Productivity"),WidgetLibrary.Create("Notes","notes")]},store.UserId)).ToList();
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId);model.Workspace!.Settings.SeasonalMode="Off";model.Workspace.Settings.ReduceMotion=true;store.Save(model);
        var home=OffscreenHandoff(new HomeWindow(store));home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var art=Descendants(home).OfType<NexaweenDecorations>().Single();var corners=Descendants(home).OfType<SeasonalCorners>().Single();
        Assert(art.Visibility==Visibility.Collapsed && corners.Visibility==Visibility.Collapsed && !art.IsHitTestVisible && !corners.IsHitTestVisible,"Fresh normal Home removes seasonal rendering and hit targets");
        var tagline=Descendants(home).OfType<TextBlock>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Home tagline");
        Assert(tagline.Visibility==Visibility.Collapsed && tagline.Inlines.Count==0,"Normal Home leaves no tagline text run or layout gap");Capture026(home,"handoff-home-normal");
        var initial=CardsHandoff(home);Assert(initial.Length==6,"Initial Home contains exactly six Spaces");
        foreach(var card in initial.Take(1))
        {
            var layout=(Grid)card.Content;var title=Descendants(layout).OfType<TextBlock>().Single(t=>t.FontSize==19);
            Assert(title.TextAlignment==TextAlignment.Center && title.Margin.Left==title.Margin.Right,"Space title uses symmetric whole-card centering independently of its icon");
            Assert(layout.Children.OfType<TextBlock>().Any(t=>t.HorizontalAlignment==HorizontalAlignment.Right && Grid.GetRow(t)==1),"Last-used label occupies the bottom-right corner");
        }
        home.Saved.Workspace!.Settings.ReduceMotion=false;ClickHandoff(home,"↓ Show More Spaces");
        var extra=CardsHandoff(home).Skip(6).ToArray();Assert(initial.All(c=>CardsHandoff(home).Contains(c)),"Expansion preserves original card instances");
        if(NexaMotion.Allowed(home.Saved.Workspace.Settings))
        {
            Assert(extra.Last().Opacity==0,"Delayed entrance stays invisible before its assigned start");Pump(TimeSpan.FromMilliseconds(190));
            Assert(extra[0].Opacity>extra.Last().Opacity,"Additional cards enter visibly in sequence");
        }
        WaitHandoff(()=>Descendants(home.Recent).OfType<Button>().Single(b=>b.Content is string s && s=="↑ Hide More Spaces").IsEnabled,"Expanded layout completes within a bounded deadline");
        home.UpdateLayout();var pageScroller=Descendants(home).OfType<ScrollViewer>().First(s=>s.ScrollableHeight>0);
        initial[0].RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,-120){RoutedEvent=Mouse.MouseWheelEvent});Pump(TimeSpan.FromMilliseconds(35));
        Assert(pageScroller.VerticalOffset>0,"Mouse wheel bubbles from expanded Space cards to the page scroller");pageScroller.ScrollToTop();Pump(TimeSpan.FromMilliseconds(30));Capture026(home,"handoff-home-expanded");
        ClickHandoff(home,"↑ Hide More Spaces");
        if(NexaMotion.Allowed(home.Saved.Workspace.Settings))
        {
            Assert(CardsHandoff(home).Length==12,"Hide More retains visual children during exit");Pump(TimeSpan.FromMilliseconds(160));
            Assert(extra.Last().Opacity<extra.First().Opacity,"Last added Space exits first");
        }
        WaitHandoff(()=>CardsHandoff(home).Length==6,"Hide More restores exactly six Spaces after exit");
        var settings=OffscreenHandoff(new SettingsDialog(home,store,home.Saved,null,"Appearance"));settings.Show();Pump(TimeSpan.FromMilliseconds(50));
        var toggle=Descendants(settings).OfType<CheckBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Nexaween enabled");toggle.IsChecked=true;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));settings.FlushPendingChanges();
        WaitHandoff(()=>art.Visibility==Visibility.Visible && art.Opacity>.3 && corners.Opacity>.9,"Live Home reveals seasonal artwork after toggling on");
        Capture026(home,"handoff-home-seasonal");Capture026(settings,"handoff-settings-appearance");
        toggle=Descendants(settings).OfType<CheckBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Nexaween enabled");toggle.IsChecked=false;toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));settings.FlushPendingChanges();
        Assert(!art.IsHitTestVisible && !corners.IsHitTestVisible,"Seasonal artwork cannot intercept controls during fade-out");
        WaitHandoff(()=>art.Visibility==Visibility.Collapsed && corners.Visibility==Visibility.Collapsed,"Live Home collapses all seasonal layers after fade");
        Assert(store.Load().Workspace!.Settings.SeasonalMode=="Off","Nexaween Off persists through the state store");
        settings.SelectSection("Experimental Features");Pump(TimeSpan.FromMilliseconds(50));
        Assert(Descendants(settings).OfType<TextBlock>().Count(t=>t.Text.EndsWith("· Coming Soon"))==3,"Experimental page marks all three unfinished providers Coming Soon");
        var tab=Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Experimental Features"));
        Assert(tab.Parent is StackPanel p && p.Parent is DockPanel && DockPanel.GetDock(p)==Dock.Bottom,"Experimental tab is pinned through bottom docking");
        var gap=settings.ActualHeight-tab.TranslatePoint(new Point(),settings).Y;settings.Height+=100;settings.UpdateLayout();
        Assert(Math.Abs(gap-(settings.ActualHeight-tab.TranslatePoint(new Point(),settings).Y))<2,"Bottom tab remains anchored during resize");Capture026(settings,"handoff-settings-navigation");
        settings.SelectSection("Time Zone & Clock");
        var search=Descendants(settings).OfType<TextBox>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Search time zones");
        var zones=Descendants(settings).OfType<ComboBox>().Single(c=>c.ItemsSource is IEnumerable<TimeZoneChoice>);
        Assert(!zones.IsEditable && !zones.IsTextSearchEnabled,"Time zone selection field rejects arbitrary text entry");
        Assert(TimeZoneCatalog.Choices.All(c=>c.ToString()==c.Label),"Time zone selections render readable labels through the native ComboBox template");
        var clockLabel=Descendants(settings).OfType<TextBlock>().Single(t=>t.Text.Contains("Daylight saving:"));var clockBefore=clockLabel.Text;
        WaitHandoff(()=>clockLabel.Text!=clockBefore,"Clock preview advances when its Settings page is initialized lazily");
        foreach(var query in new[]{"CHIC","Chicago","Central Standard Time","America Chicago"})Assert(TimeZoneCatalog.Search(query).Any(c=>c.Id=="Central Standard Time"),"Authoritative Central time aliases match "+query);
        search.Text="CHIC";Assert(zones.Items.Cast<TimeZoneChoice>().Any(c=>c.Id=="Central Standard Time"),"Typing CHIC live-filters the real time zone dropdown");Pump(TimeSpan.FromMilliseconds(350));Capture026(settings,"handoff-timezone-search");
        zones.SelectedItem=zones.Items.Cast<TimeZoneChoice>().First(c=>c.Id=="Central Standard Time");ClickHandoff(settings,"Use selected time zone");settings.FlushPendingChanges();
        Assert(store.Load().Workspace!.Settings.TimeZoneId=="Central Standard Time" && search.Text=="","Choosing a result synchronizes and persistently saves the real selection");
        var chicago=TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");Assert(chicago.GetUtcOffset(new DateTime(2026,1,15))!=chicago.GetUtcOffset(new DateTime(2026,7,15)),"Selected authoritative Chicago zone retains daylight saving rules");
        settings.SelectSection("Sounds");var sounds=Descendants(settings).OfType<ComboBox>().ToArray();
        Assert(sounds.Length==3 && sounds.All(c=>c.Items.Count==5),"Sounds exposes five styles and five independent choices for each feedback direction");
        sounds[0].SelectedIndex=3;sounds[1].SelectedIndex=1;sounds[2].SelectedIndex=2;settings.FlushPendingChanges();var audio=store.Load();store.LoadInteractionPreferences(audio);
        Assert(audio.PositiveFeedbackSound=="Bells" && audio.NegativeFeedbackSound=="Wood","Independent sound choices persist without being overwritten by the style selector");
        Assert(InterfaceAudio.PositiveChoices.Select(s=>Convert.ToHexString(SHA256.HashData(InterfaceAudio.CreateCue(.25,"positive",s).ToArray()))).Distinct().Count()==5 && InterfaceAudio.NegativeChoices.Select(s=>Convert.ToHexString(SHA256.HashData(InterfaceAudio.CreateCue(.25,"negative",s).ToArray()))).Distinct().Count()==5,"All five positive and negative original PCM cues are distinct");
        settings.Close();home.Close();
        home=OffscreenHandoff(new HomeWindow(store));home.Show();Pump(TimeSpan.FromMilliseconds(60));Assert(CardsHandoff(home).Length==6 && Descendants(home).OfType<SeasonalCorners>().Single().Visibility==Visibility.Collapsed,"Reopening Home resets expansion and preserves normal appearance");home.Close();
        CheckHandoffDashboard(store);
        CheckHandoffScores();
        CheckHandoffExperience(store);
    }
    private static void CheckHandoffDashboard(StateStore store)
    {
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(120));
        var options=main.state.Workspace!.Settings;options.ReduceMotion=false;
        foreach(var bugs in new[]{false,true})foreach(var seasonal in new[]{false,true})
        {
            main.ApplyBugPreferences(new BugPreferences(bugs,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions()));options.SeasonalMode=seasonal?"On":"Off";main.ApplyReliabilityOptions();main.RefreshSeasonalWorkspace();
            Assert((main.bugsOverlay!=null)==bugs,"Independent Add Bugs survives Nexaween "+seasonal+" with bugs "+bugs);
            Assert(main.CanCollectSeasonalCandy==seasonal,"Candy collection gate follows current seasonal preference, including transition");
        }
        options.SeasonalMode="Off";main.ApplyReliabilityOptions();WaitHandoff(()=>main.WidgetCanvas.Children.OfType<NexaweenDecorations>().Single().Visibility==Visibility.Collapsed,"Dashboard seasonal layer leaves active rendering when Off");
        Assert(((SolidColorBrush)ProductivityRing.LightningBrush).Color==Color.FromRgb(250,204,21),"Productivity lightning uses the shared golden yellow in normal mode");Theme.SetSeasonalEnabled(true);
        Assert(((SolidColorBrush)ProductivityRing.LightningBrush).Color==Color.FromRgb(250,204,21),"Productivity lightning remains golden yellow in seasonal mode");
        var productivity=main.state.Widgets.Single(w=>w.Kind=="Productivity");Assert(WidgetLibrary.MinimumWidth(productivity)==220 && WidgetLibrary.MinimumHeight(productivity)==240,"Productivity supports a 220 by 240 minimum with live breakdown");
        var ring=Descendants(main).OfType<ProductivityRing>().Single();ring.Update(long.MaxValue,new NexaOptions{ReduceMotion=true});Assert(ring.ScoreText==long.MaxValue.ToString("N0"),"Large settled cumulative scores render exact integer digits rather than double rounding");Capture026(main,"handoff-compact-productivity");
        Assert(!Descendants(main).OfType<TextBlock>().Any(t=>t.Text.StartsWith("Meaningful actions:") || t.Text.StartsWith("Next milestone:")),"Default Productivity hides activity summaries behind details");
        options.ReduceMotion=true;var oldVisible=Application.Current.Windows.OfType<Window>().Count(w=>w.IsVisible);var bounds=new Rect(main.Left,main.Top,main.Width,main.Height);
        var preview=new SplashWindow(options).PreviewIn((Grid)main.Content);WaitHandoff(()=>preview.IsCompleted,"Shared in-place Splash Preview completes");preview.GetAwaiter().GetResult();
        Assert(Application.Current.Windows.OfType<Window>().Count(w=>w.IsVisible)==oldVisible && bounds==new Rect(main.Left,main.Top,main.Width,main.Height),"Splash Preview creates no additional visible windows and preserves dashboard bounds");
        main.WidgetScroller.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(30));
        var fresh=Continuity.Clone(main.state.Workspace!);var freshRecord=Continuity.NewDashboard("Synthetic new Space","Class",DashboardTemplates.Build(DashboardTemplates.All.Single(t=>t.Name=="Class")),store.UserId);fresh.Dashboards.Add(freshRecord);fresh.ActiveDashboardId=freshRecord.DashboardId;
        var change=main.ChangeSpace(fresh);WaitHandoff(()=>change.IsCompleted,"In-window Space transition finishes through the shared fade sequence");Assert(change.GetAwaiter().GetResult(),"A newly created Space commits within an already open dashboard");
        WaitHandoff(()=>!main.state.InitialViewPending,"In-window Space creation resets its initial viewport after layout");
        Assert(main.WidgetScroller.VerticalOffset==0,"New Space creation also starts at the top without constructing a new window");
        main.Close();
        foreach(var template in DashboardTemplates.All.Where(t=>t.Name!="Argo Pantry"))foreach(var size in new[]{new Size(900,600),new Size(1450,950)})
        {
            var target=new StateStore(Path.Combine(output,"viewport-"+template.Name+"-"+size.Width+".json"));var data=DashboardTemplates.Build(template);data.Widgets.Add(WidgetLibrary.Create("Notes"));data.Widgets.Last().PlacementY=2000;
            var record=Continuity.NewDashboard("Synthetic "+template.Name,template.Name,data,target.UserId);var model=Continuity.Activate(new WorkspaceDocument{UserId=target.UserId,ActiveDashboardId=record.DashboardId,Dashboards=[record]},target.UserId);model.Workspace!.Settings.ReduceMotion=true;target.Save(model);
            var window=OffscreenHandoff(new MainWindow(target){Width=size.Width,Height=size.Height});window.Loaded+=(_,_)=>window.WidgetScroller.ScrollToEnd();window.Show();
            WaitHandoff(()=>!window.state.InitialViewPending,"New "+template.Name+" finishes its initial viewport reset at "+size);
            Pump(TimeSpan.FromMilliseconds(40));Assert(window.WidgetScroller.VerticalOffset==0,"New "+template.Name+" opens at the top at "+size);window.Close();
        }
        Theme.SetSeasonalEnabled(true);var splash=OffscreenHandoff(new SplashWindow(new NexaOptions{ReduceMotion=false}));splash.Show();
        if(NexaMotion.Allowed(new()))Assert(!splash.TaglineComplete && splash.Loading.Visibility==Visibility.Collapsed,"Animated startup hides progress until the complete tagline is typed");
        splash.Report(.8,"Synthetic initialization complete");WaitHandoff(()=>splash.TaglineComplete,"Full seasonal tagline finishes before progress is revealed");
        Assert(splash.Loading.Visibility==Visibility.Collapsed && splash.ReportedProgress==.8,"Real initialization milestones accumulate while tagline types");
        Assert(Descendants(splash).OfType<TextBlock>().SelectMany(t=>t.Inlines.OfType<Run>()).Any(r=>r.Text=="haunts" && r.Foreground is SolidColorBrush b && b.Color==Color.FromRgb(249,115,22)),"Splash colors only haunts orange after typing");Capture026(splash,"handoff-splash");splash.Close();
    }
    private static void CheckHandoffScores()
    {
        var now=DateTimeOffset.UtcNow;var data=new DashboardData{Session=new Session{Title="Synthetic study",Start=now.AddMinutes(-10),End=now.AddMinutes(10)}};
        var record=Continuity.NewDashboard("Synthetic scores","Study",data,"fixture");record.SpaceScoreInitialized=true;record.SpaceProductivityScore=425;record.Current.ScoreAtStart=425;
        record.Current.Data.Productivity.Events.Add(new ProductivityEvent{Category="Notes",Description="Synthetic note",BasePoints=135});
        var recap=SessionFinalization.Complete(record,now);Assert(recap.ScoreStart==425 && recap.ScoreEnd==560 && recap.ScoreEnd-recap.ScoreStart==135,"Recap preserves the requested 425 to 560 cumulative example");
        var file=Continuity.ReadExport(Continuity.Export(record,true));var workspace=new WorkspaceDocument{UserId="fixture"};var imported=Continuity.Import(workspace,file,"fixture",true);SpaceScores.Reconcile(imported);SpaceScores.Reconcile(imported);
        Assert(imported.SpaceProductivityScore==560 && imported.Current.Recap?.ScoreStart==425 && imported.Current.Recap.ScoreEnd==560,"Private export/import retains cumulative baseline and historical recap without duplicate credit");
        Continuity.Reset(imported);SpaceScores.Reconcile(imported);Assert(imported.SpaceProductivityScore==560 && imported.Current.ScoreAtStart==560 && imported.History.Single().Recap?.ScoreEnd==560,"Starting a fresh session preserves cumulative points and the prior recap snapshot");
        var configuration=Continuity.ReadExport(Continuity.Export(record,false));Assert(configuration.CumulativeScore==null && configuration.Recap==null && configuration.Data.Productivity.Events.Count==0,"Configuration-only exports omit scores, recaps and private activity");
        var legacy=Continuity.ReadExport("{\"Application\":\"Nexa\",\"DashboardName\":\"Legacy\",\"IncludesSessionData\":true,\"Data\":{}}");Assert(legacy.CumulativeScore==null,"Legacy portable exports remain readable without new score metadata");
    }
    private static void CheckHandoffExperience(StateStore store)
    {
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(70));main.state.Workspace!.Settings.ReduceMotion=true;
        var welcome=OffscreenHandoff(new OnboardingWindow(store){Owner=main});welcome.Show();Pump(TimeSpan.FromMilliseconds(40));Capture026(welcome,"handoff-onboarding-welcome");
        welcome.Advance();welcome.Advance();welcome.Advance();
        Assert(!Descendants(welcome).OfType<TextBlock>().Any(t=>t.Text.Contains("Optional accounts can be connected later")),"Appearance onboarding omits unrelated account copy");Capture026(welcome,"handoff-onboarding-appearance");
        welcome.Advance();Assert(welcome.Step==5,"Welcome reaches the single final Space naming step");
        Assert(Descendants(welcome).OfType<TextBox>().Single().TextAlignment==TextAlignment.Center && Descendants(welcome).OfType<Border>().Count(b=>b.CornerRadius==new CornerRadius(10) && b.Child is StackPanel)>=3,"Final naming screen retains centered large input and illustrated selected-tool cards");
        Assert(ReferenceEquals(((System.Windows.Shapes.Path)MainWindow.ToolPreview("Productivity")).Stroke,ProductivityRing.LightningBrush),"Productivity tool previews also use the shared gold lightning");
        Capture026(welcome,"handoff-onboarding-name");welcome.Close();
        main.StartQuickStart();Pump(TimeSpan.FromMilliseconds(40));Capture026(main,"handoff-guide-introduction");
        Assert(Descendants(main).OfType<Button>().Any(b=>Equals(b.Content,"I Know What I'm Doing")) && Descendants(main).OfType<Button>().Any(b=>Equals(b.Content,"Continue")),"Quick Start introduction keeps explicit skip and continue controls");
        main.AdvanceGuide();Pump(TimeSpan.FromMilliseconds(40));Capture026(main,"handoff-guide-spotlight");ClickHandoff(main,"Skip guide");
        Assert(!main.TutorialActive && store.Load().Workspace!.Settings.QuickStartSkipped,"Skipping Quick Start removes the blocking layer and persists");main.StartQuickStart();Assert(main.TutorialActive,"Quick Start remains replayable after skip");ClickHandoff(main,"I Know What I'm Doing");main.Close();
        var now=DateTimeOffset.UtcNow;var data=new DashboardData{Session=new Session{Title="Synthetic accomplishments",Start=now.AddMinutes(-25),End=now.AddMinutes(25)}};var record=Continuity.NewDashboard("Synthetic accomplishments","Study",data,store.UserId);
        foreach(var item in new[]{("Notes",10),("Task completed",10),("Water",10),("Candy",1)})record.Current.Data.Productivity.Events.Add(new ProductivityEvent{At=now,Key=Guid.NewGuid().ToString(),Category=item.Item1,Description="Synthetic recorded action",BasePoints=item.Item2});
        SessionFinalization.Complete(record,now);var owner=OffscreenHandoff(new HomeWindow(store));owner.Show();var recap=OffscreenHandoff(new SessionRecapWindow(owner,record.Name,"Book",record.Current,new NexaOptions{ReduceMotion=true}));recap.Show();Pump(TimeSpan.FromMilliseconds(60));
        Assert(Descendants(recap).OfType<System.Windows.Shapes.Path>().Count()>=4,"Recap renders native illustrated mini-cards for actual recorded actions");Capture026(recap,"handoff-recap-actions");recap.Close();owner.Close();
    }
}
