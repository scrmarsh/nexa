using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckFollowupDesign()
    {
        var store=new StateStore(Path.Combine(output,"design.json"));var model=new AppState();store.Save(model);model.Workspace!.Settings.SeasonalMode="Off";store.Save(model);
        var counter=WidgetLibrary.Create("Counter");counter.CounterMode="Distraction";model.Widgets.Add(counter);store.Save(model);model.Workspace.Settings.DistractionPenalty=10;counter.Value=1;store.Save(model);
        Assert(store.Load().Productivity.Events.Any(e=>e.Category=="Distraction" && e.BasePoints==-10),"Configured negative awards survive the real workspace validator and restart payload");
        var initialTotal=store.Load().Workspace!.AllTimeProductivityScore;store.Save(model);Assert(store.Load().Workspace!.AllTimeProductivityScore==initialTotal,"Re-saving a signed award cannot change lifetime totals again");
        var at=NexaTime.Instant(new DateTime(2030,1,7,9,30,0),"America/New_York");var schedule=PixelTiming.WeeklyClass("Class",[DayOfWeek.Monday],TimeSpan.FromHours(9),TimeSpan.FromHours(10),"America/Chicago",at);
        WeeklySchedule.ChangeTimeZone(schedule,"America/New_York");var zoneReading=PixelTiming.Read(schedule,at);
        Assert(zoneReading.CountUp && zoneReading.VisualFraction==.5,"Changing recurring time zone recognizes today's newly active wall-clock class");
        var preset=new Preset{Name="Edited class",Minutes=60,IntervalMinutes=10,StartClockMinutes=9*60,EndClockMinutes=10*60,ScheduledDays=[DayOfWeek.Monday],AutomaticScheduling=true,AccentColor="#7C3AED",PixelColor="#84CC16"};
        var main=new MainWindow(store){Left=-10000,Top=-10000,ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual};main.Show();Pump(TimeSpan.FromMilliseconds(100));
        main.state.Workspace!.Settings.TimeZoneId="America/Chicago";var instant=NexaTime.Instant(new DateTime(2030,1,7,9,30,0),"America/Chicago");var sessionId=Continuity.Current(main.state.Workspace,store.UserId).Current.SessionId;
        Assert(main.ApplyTimingPreset(preset,instant),"Selecting saved preset timing applies through the persisted edit path");main.UpdateClock(instant);
        Assert(main.PercentLabel.Text=="50.000%" && Continuity.Current(main.state.Workspace,store.UserId).Current.SessionId==sessionId,"Preset selection recognizes an active class without replacing its report identity");
        var global=new SettingsDialog(main,store,main.state,main,"Appearance");global.Show();Pump(TimeSpan.FromMilliseconds(80));
        var enable=Descendants(global).OfType<CheckBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Nexaween enabled");enable.IsChecked=true;enable.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));global.FlushPendingChanges();
        Assert(Theme.SeasonalEnabled && store.Load().Workspace!.Settings.SeasonalMode=="On","The separate red/green switch activates and persists Nexaween");
        Descendants(global).OfType<Button>().Single(b=>Equals(b.Content,"Sounds")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        Assert(Descendants(global).OfType<Slider>().Any(s=>s.Minimum==0 && s.Maximum==1),"Sounds remain usable while the seasonal appearance is locked");
        Descendants(global).OfType<Button>().Single(b=>Equals(b.Content,"Appearance")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));Pump(TimeSpan.FromMilliseconds(60));
        Assert(!Application.Current.Windows.OfType<Window>().Any(w => w.Title == "Nexaween appearance"), "Appearance stays accessible in the same Settings window");
        var disable = Descendants(global).OfType<CheckBox>().Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Nexaween enabled");
        disable.IsChecked=false;disable.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));global.FlushPendingChanges();
        Assert(!Theme.SeasonalEnabled && store.Load().Workspace!.Settings.SeasonalMode=="Off", "The top Appearance switch restores normal appearance");
        var enableAgain=Descendants(global).OfType<CheckBox>().Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "Nexaween enabled");
        enableAgain.IsChecked=true;enableAgain.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));global.FlushPendingChanges();
        Assert(Descendants(global).OfType<CheckBox>().Any(c=>Equals(c.Content,"Halloween bug costumes")), "Seasonal controls live directly in Appearance");
        var density=Descendants(global).OfType<ComboBox>().Single(c=>c.Items.OfType<string>().SequenceEqual(new[]{"Off","Low","Medium","High"}));density.SelectedItem="High";global.FlushPendingChanges();
        Assert(store.Load().Workspace!.Settings.DecorationDensity=="High","Direct decoration-density selection persists");
        global.Close();var session=new SettingsDialog(main,store,main.state,main,"Tips");session.Show();session.SelectSection("Tips");Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(session).OfType<ScrollViewer>().Any(s=>s.VerticalOffset>0),"Tips navigation scrolls the Session page to its bottom setting");
        var tips=Descendants(session).OfType<CheckBox>().Single(c=>Equals(c.Content,"Enable Tips"));tips.IsChecked=false;tips.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));session.FlushPendingChanges();
        Assert(!store.Load().Workspace!.Settings.ShowQuickTips,"Tips can be disabled from their new Session location");session.Close();main.Close();
        CheckFollowupArtwork();CheckFollowupStartup(store);
    }
    private static void CheckFollowupArtwork()
    {
        using var candy=new NexaweenDecorations();candy.Update(1600,1500,[],"High");
        Assert(NexaweenDecorations.CandyKinds.Distinct().Count()==10 && candy.CandyActorCount==candy.ActorCount,"Background placement uses ten candy types and no generic environmental decorations");
        var cards=new[]{new Rect(80,80,400,250),new Rect(620,80,400,250),new Rect(80,500,400,250)};candy.Update(1600,1500,cards.ToList(),"High",cards);
        Assert(candy.ContextualActorCount>0 && candy.ActorCount<=32,"Contextual widget-corner decorations share the bounded budget");
        var reserved=new List<Rect>{new Rect(80,80,400,250),new Rect(620,80,400,250)};
        candy.Update(1600,1500,reserved,"High");Assert(candy.ContextualActorCount==0,"Occupied reserved regions never become inferred attachment targets");
        var occupiedCopy=reserved.ToArray();var target=new Rect(80,500,400,250);
        candy.Update(1600,1500,reserved,"High",widgetTargets:[target]);var originalAttachment=candy.AttachmentBounds.Single();
        Assert(reserved.SequenceEqual(occupiedCopy),"Decoration placement does not modify the caller's occupied collision list");
        var resized=new Rect(100,520,500,280);candy.Update(1600,1500,reserved,"High",widgetTargets:[resized]);
        Assert(candy.AttachmentBounds.Single()!=originalAttachment && Math.Abs(candy.AttachmentBounds.Single().X-(resized.Right-16.8))<.001,"Only widget target changes are enough to reposition attached art on resize/move");
        var beforeScroll=candy.AttachmentBounds.Single();candy.SetViewport(new Rect(0,400,1200,800));Assert(candy.AttachmentBounds.Single()==beforeScroll,"Viewport scrolling preserves the decoration's canvas attachment coordinates");
        candy.Update(1600,1500,reserved,"High");Assert(candy.ContextualActorCount==0 && !candy.IsHitTestVisible,"Missing optional targets clear contextual art gracefully and never intercept controls");
        candy.Update(1600,1500,[new Rect(target.Right,target.Bottom,100,100)],"High",widgetTargets:[target]);Assert(candy.ContextualActorCount==0,"Reserved UI collision regions prevent corner attachments even for a real widget target");
        Theme.SetSeasonalEnabled(true);var tagline=new TextBlock();NexaBranding.ApplyTagline(tagline,NexaBranding.Tagline);
        var orange=tagline.Inlines.OfType<Run>().Where(r=>r.Foreground is SolidColorBrush b && b.Color==(Color)ColorConverter.ConvertFromString("#F97316")).ToArray();
        Assert(orange.Length==1 && orange[0].Text=="haunts" && new TextRange(tagline.ContentStart,tagline.ContentEnd).Text=="Nexa — A space for whatever haunts you next.","Only haunts receives Halloween orange in the new seasonal tagline");
        Theme.SetSeasonalEnabled(false);Assert(NexaBranding.Tagline=="a space for whatever comes next.","Disabling Nexaween restores the ordinary tagline");
        var world=CreateWorld(BugBehavior.Cozy);world.SetSurfaces([new BugSurface("card",new Rect(300,300,300,200))],new Size(1600,1000));var owner=world.Bugs.FirstOrDefault(b=>b.Species==BugSpecies.Spiders);if(owner==null){Step(world,20);owner=world.Bugs.First(b=>b.Species==BugSpecies.Spiders);}
        var web=new EcosystemWeb{Owner=owner.InteractionId,AnchorId="card",AnchorOffsetX=12,AnchorOffsetY=-12,X=612,Y=288,Construction=.4};world.Webs.Add(web);
        world.SetSurfaces([new BugSurface("card",new Rect(400,400,300,200))],new Size(1600,1000));Assert(web.X==712 && web.Y==388,"Spider web anchors follow a moved widget without accumulating scroll deltas");
        var endpoints=Enumerable.Range(0,8).Select(i=>(NexaWebGeometry.Spoke(web,i,30)-new Point(web.X,web.Y)).Length).ToArray();Assert(endpoints.Distinct().Count()>3,"Persistent web geometry has irregular spoke lengths");
        var saved=world.CaptureEcosystem("synthetic-owner");var restored=new BugManager();restored.RestoreEcosystem(Continuity.Clone(saved));Assert(restored.Webs.Any(w=>w.Owner==owner.InteractionId && w.AnchorId=="card" && w.Construction==.4),"Partial web construction and widget anchors survive restart");
        var grid=new Grid();grid.Measure(new Size(1200,800));grid.Arrange(new Rect(0,0,1200,800));using var notices=new NexaNotifications(grid);notices.Show("autosave","Autosaving...","🔄",seconds:null);notices.Show("tip","Test tip","💡");
        Assert(Descendants(grid.Children.OfType<ScrollViewer>().Single(v=>v.HorizontalAlignment==HorizontalAlignment.Left)).OfType<TextBlock>().Any(t=>t.Text=="Autosaving..."),"Autosave notifications occupy the left anchor");
        Assert(Descendants(grid.Children.OfType<ScrollViewer>().Single(v=>v.HorizontalAlignment==HorizontalAlignment.Right)).OfType<TextBlock>().Any(t=>t.Text=="Test tip"),"Tips remain at the independent right anchor");
    }
    private static void CheckFollowupStartup(StateStore store)
    {
        var app=Application.Current;var prior=app.MainWindow;var splash=new SplashWindow{Left=-10000,Top=-10000,ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual};splash.Show();
        var task=splash.EnterHome(store,true);var watch=Stopwatch.StartNew();while(!task.IsCompleted && watch.Elapsed<TimeSpan.FromSeconds(20))Pump(TimeSpan.FromMilliseconds(10));
        var home=task.GetAwaiter().GetResult();
        Assert(splash.Loading.Value==1 && home.IsLoaded && splash.CompletedStages.Last()=="Home ready","Real splash stages reach 100% only after the home window renders");
        Assert(splash.CompletedStages.Contains("Workspace loaded and validated") && splash.CompletedStages.IndexOf("Workspace loaded and validated")<splash.CompletedStages.IndexOf("Home ready"),"Startup progress records actual completed data and UI initialization stages");
        File.WriteAllText(Path.Combine(output,"startup-stages.txt"),string.Join(Environment.NewLine,splash.CompletedStages)+$"\nMeasured startup {splash.DisplayedMilliseconds:0} ms; no fixed display delay.");app.MainWindow=prior;home.Close();
    }
}
