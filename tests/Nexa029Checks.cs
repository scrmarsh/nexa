using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void Check029()
    {
        Assert(App.AppVersion=="0.0.29" && App.VersionLabel=="nexa v0.0.29 alpha","Current candidate has lowercase Alpha identity");
        var old=JsonSerializer.Deserialize<NexaOptions>("{}")!;
        Assert(old.EnableF11 && old.FullscreenNotification && old.ShowSystemIndicators && old.EnableVolumeSlider && old.EscExitsFullscreen,"Old settings acquire all five display defaults");
        Assert(old.HomeWallpaper.Mode==SpaceBackgroundMode.Default && !new SpaceBackground().FitImage,"Old wallpaper saves preserve default fill behavior");
        Assert(SplashWindow.MinimumDisplay==TimeSpan.FromSeconds(5),"Splash has a real five-second minimum");
        Assert(new NexaWordmark(300).Version.HorizontalAlignment==HorizontalAlignment.Left,"Version label sits below left branding");
        Assert(SpaceIcons.Paths.ContainsKey("Graduation cap"),"Graduation-cap icon has a persistent portable identity");
        for(var i=0;i<=20;i++)Assert(!string.IsNullOrWhiteSpace(MainWindow.DistractionMessage(i)),"Distraction phrase "+i+" is defined");
        Assert(MainWindow.DistractionMessage(0)=="Locked in. Keep it going." && MainWindow.DistractionMessage(10)=="TEN?! Be serious." && MainWindow.DistractionMessage(999)==MainWindow.DistractionMessage(20),"Distraction boundaries retain requested exact playful messages");
        CheckCompletion029();CheckRestore029();CheckViews029();CheckClosingAnimation029();CheckSystemAndBugs029();CheckWallpaperPersistence029();
    }
    private static void CheckCompletion029()
    {
        var store=new StateStore(Path.Combine(output,"lifecycle029.json"));var at=DateTimeOffset.UtcNow;
        var counter=WidgetLibrary.Create("Counter");counter.Value=17;
        var water=WidgetLibrary.Create("Water Bottle Counter");water.Value=4;
        var timer=WidgetLibrary.Create("Stopwatch");timer.StopwatchElapsedSeconds=123;
        var list=WidgetLibrary.Create("Task List");list.Items=[new(){Text="Reusable",Permanent=false}];
        var pinned=WidgetLibrary.Create("Counter");pinned.SessionScoped=false;pinned.Value=9;
        var data=new DashboardData{Notes="Archive before reset",WidgetLayoutInitialized=true,Widgets=[counter,water,timer,list,pinned],Tasks=[new(){Text="Retain local task"}],Session=new Session{Title="Lifecycle fixture",Start=at.AddMinutes(-20),End=at.AddMinutes(20),HedyTranscript="Original transcript",LocalRecordingPath="synthetic-audio.wav",LocalRecordingSeconds=11}};
        var record=Continuity.NewDashboard("Fixture","Study",data,store.UserId);
        record.Current.Data.Productivity.BugsSquashed=3;var id=record.Current.SessionId;
        var workspace=new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=record.DashboardId,Dashboards=[record],Settings=new(){ReduceMotion=true},AllTimeSquashes=77};
        store.Save(Continuity.Activate(workspace,store.UserId));
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(60));main.NotesBox.Text="Archive before reset";
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))Assert(!main.FinalizeSession(at,false),"Failed save rejects completion before reset");
        Assert(main.state.Notes=="Archive before reset" && main.state.Widgets.Single(w=>w.Id==counter.Id).Value==17 && Continuity.Current(main.state.Workspace!,store.UserId).History.Count==0,"Save failure retains notes/counters and creates no archive");
        Assert(main.FinalizeSession(at,false),"Completion commits successfully after lock release");
        var done=Continuity.Current(main.state.Workspace!,store.UserId);
        Assert(done.History.Count==1 && done.History[0].SessionId==id && done.History[0].Data.Notes=="Archive before reset" && done.History[0].Data.Widgets.Single(w=>w.Id==counter.Id).Value==17,"Original session identity, notes and counters are immutable in History");
        Assert(done.History[0].Data.Session!.HedyTranscript=="Original transcript" && done.History[0].Data.Session!.LocalRecordingPath=="synthetic-audio.wav","Archived recording reference and transcript survive reset");
        Assert(main.state.Notes=="" && main.state.Widgets.Single(w=>w.Id==counter.Id).Value==0 && main.state.Widgets.Single(w=>w.Id==water.Id).Value==0 && main.state.Widgets.Single(w=>w.Id==timer.Id).StopwatchElapsedSeconds==0,"Session notes/counters/water/stopwatch reset together");
        Assert(main.state.Tasks.Single().Text=="Retain local task" && main.state.Widgets.Single(w=>w.Id==list.Id).Items.Single().Text=="Reusable" && main.state.Widgets.Single(w=>w.Id==pinned.Id).Value==9,"All local tasks and explicit persistent widget state survive");
        Assert(main.state.Workspace!.AllTimeSquashes==77 && main.state.Productivity.BugsSquashed==0,"Session squashes reset without touching all-time totals");
        Assert(done.Current.SessionValuesReset && done.Current.SessionId!=id && done.Current.Recap!=null,"Fresh workspace identity retains the previous recap and persistent reset checkpoint");
        Assert(main.FinalizeSession(at,false) && done.History.Count==1,"Repeated completion is idempotent");
        main.Close();var restarted=new StateStore(store.FilePath).Load();
        Assert(Continuity.Current(restarted.Workspace!,store.UserId).History.Count==1 && restarted.Notes=="" && Continuity.Current(restarted.Workspace!,store.UserId).Current.SessionValuesReset,"Restart sees complete archive/reset transaction without animation dependency");
        var source=done.History[0];var serialized=JsonSerializer.Serialize(source);
        done.Current.Data.Notes="Work after completion";Continuity.Archive(done);
        Assert(done.History.Count==2 && done.History[1].Data.Notes=="Work after completion" && done.History[1].Recap==null && JsonSerializer.Serialize(source)==serialized,"Work entered after completion archives separately without overwriting the original recap");
        var next=WeeklySchedule.ResetSessionData(source.Data);
        Assert(next.Widgets.Select(w=>w.Id).SequenceEqual(source.Data.Widgets.Select(w=>w.Id)) && next.Background.ImagePath==source.Data.Background.ImagePath,"Reset preserves widget identities and Space wallpaper");
    }
    private static void CheckRestore029()
    {
        var at=DateTimeOffset.UtcNow;const string user="synthetic-user";
        var source=Continuity.NewDashboard("Archive","Study",new DashboardData{Notes="Original notes",SpaceIcon="Book",Session=new Session{Start=at.AddMinutes(-20),End=at.AddMinutes(20),StoppedAt=at,HedySessionId="provider-id",LocalRecordingPath="saved.wav"}},user);
        var counter=WidgetLibrary.Create("Counter");counter.Value=8;source.Current.Data.Widgets=[counter];
        ProductivityScoring.Add(source.Current.Data.Productivity,"old-award","Notes","Saved award",10,at);SessionFinalization.Complete(source,at);Continuity.Archive(source);
        var archived=source.History[0];var unchanged=JsonSerializer.Serialize(archived);
        var target=Continuity.NewDashboard("Target","Work",new DashboardData{Notes="Current unsaved fixture",SpaceIcon="Briefcase",Background=new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath="target.png"},Widgets=[Continuity.Clone(counter)],Tasks=[new(){Text="Keep target task"}]},user);target.Current.Data.Widgets[0].Value=2;
        Continuity.RestoreHistorical(target,archived,user,false,at);
        Assert(target.Current.ContinuedFrom==archived.SessionId && target.Current.Data.Notes=="Original notes" && target.History.Single().Data.Notes=="Current unsaved fixture","Resume copies original notes with provenance and archives the replaced current content");
        Assert(target.Current.Data.SpaceIcon=="Briefcase" && target.Current.Data.Background.ImagePath=="target.png" && target.Current.Data.Tasks.Single().Text=="Keep target task","Restore retains target Space identity, wallpaper and tasks");
        Assert(target.Current.Data.Session!.StoppedAt==null && target.Current.Data.Session!.FinalizedAt==null && target.Current.Data.Session!.HedySessionId=="" && target.Current.Data.Session!.LocalRecordingPath=="","Resume never restarts remote/local recording or reuses provider session links");
        SpaceScores.Reconcile(target);Assert(target.SpaceProductivityScore==0,"Restored historical score events are not credited to the target again");
        Assert(target.Current.Data.Widgets.Single().Value==8 && target.Current.Data.Widgets.Single().Id==counter.Id,"Restorable counter values reuse existing target widget identity");
        Continuity.RestoreHistorical(target,archived,user,true,at.AddMinutes(1));
        Assert(target.Current.ContinuedFrom==null && target.Current.Data.Productivity.Events.Count==0 && target.Current.Data.Session!.Start==at.AddMinutes(1),"Load-as-new has a fresh clock and no imported scoring awards");
        Assert(JsonSerializer.Serialize(archived)==unchanged,"Resume and new-session load leave the original history record unchanged");
    }
    private static void CheckViews029()
    {
        var store=new StateStore(Path.Combine(output,"views029.json"));var state=new AppState{Session=new Session{Title="View fixture",Start=DateTimeOffset.UtcNow.AddMinutes(-10),End=DateTimeOffset.UtcNow.AddMinutes(30)},WidgetLayoutInitialized=true};
        var hedy=WidgetLibrary.Create("Hedy Notes");hedy.Height=600;hedy.CustomSize=true;hedy.PlacementX=0;hedy.PlacementY=0;
        state.Widgets=[hedy];state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings=new(){ReduceMotion=true,SeasonalMode="Off",ShowSystemIndicators=false};store.Save(state);
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(70));
        var notes=main.state.Notes;var sessionId=Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId;var before=new Rect(main.Left,main.Top,main.Width,main.Height);
        main.ToggleFullscreen();Pump(TimeSpan.FromMilliseconds(100));
        Assert(main.IsFullscreen && main.DashboardHeader.Height==44 && main.WorkspaceOuter.Margin.Left==8 && main.ExitButton.Visibility==Visibility.Collapsed,"Fullscreen has compact flush-top navigation and removes caption controls");
        var headerPosition=main.DashboardHeader.TranslatePoint(new Point(),(UIElement)main.Content);
        Assert(headerPosition.Y==0 && Descendants(main).OfType<TextBlock>().Any(t=>t.Text=="Fullscreen mode — Press F11 to exit"),"Fullscreen notice overlays at top without shifting navigation");
        var root=(Grid)main.Content;
        var notices=(NexaNotifications)typeof(MainWindow).GetField("notifications",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(main)!;
        notices.Dismiss("feedback");notices.Dismiss("autosave");notices.Dismiss("tip");
        notices.Show("autosave","Fullscreen edge autosave",seconds:null);notices.Show("tip","Fullscreen edge tip",seconds:null);
        main.UpdateLayout();
        Rect NoticeBounds(string message)
        {
            var label=Descendants(root).OfType<TextBlock>().Single(t=>t.Text==message);
            var card=(Border)((Grid)((StackPanel)label.Parent).Parent).Parent;
            return card.TransformToVisual(root).TransformBounds(new Rect(card.RenderSize));
        }
        var saveBounds=NoticeBounds("Fullscreen edge autosave");var tipBounds=NoticeBounds("Fullscreen edge tip");
        Assert(Math.Abs(saveBounds.Left-8)<.5 && Math.Abs(root.ActualHeight-saveBounds.Bottom-8)<.5,"Fullscreen autosave has matching tight eight-DIP side and bottom gaps");
        Assert(Math.Abs(root.ActualWidth-tipBounds.Right-8)<.5 && Math.Abs(root.ActualHeight-tipBounds.Bottom-8)<.5,"Fullscreen tips use the same eight-DIP right and bottom gaps");
        Assert(NexaWindowBounds.TryNotificationArea(root,out var notificationArea) && notificationArea==new Rect(root.RenderSize),"Fullscreen notification area includes the taskbar strip");
        var scoreFrame=ProductivityStreakOverlay.LayoutFrame(root.RenderSize,notificationArea,NexaWindowBounds.NotificationInset(root));
        Assert(Math.Abs(root.ActualHeight-scoreFrame.Bottom-8)<.5,"Productivity score banner shares the tight fullscreen bottom baseline");
        SnapshotHome029(main,"nexa029-fullscreen-notification-edges");
        notices.Dismiss("tip");notices.Show("edge-alert","Fullscreen edge alert",seconds:null);main.UpdateLayout();
        var alertBounds=NoticeBounds("Fullscreen edge alert");
        Assert(Math.Abs(root.ActualWidth-alertBounds.Right-8)<.5 && Math.Abs(root.ActualHeight-alertBounds.Bottom-8)<.5,"Other fullscreen notification cards share the same edge spacing");
        notices.Dismiss("edge-alert");notices.Dismiss("autosave");
        SnapshotHome029(main,"nexa029-fullscreen");main.ToggleFullscreen();Pump(TimeSpan.FromMilliseconds(50));
        Assert(NexaWindowBounds.NotificationInset(root)==18,"Leaving fullscreen restores the standard windowed notification inset");
        Assert(!main.IsFullscreen && Math.Abs(main.Width-before.Width)<1 && Math.Abs(main.Height-before.Height)<1 && main.ExitButton.Visibility==Visibility.Visible,"Fullscreen restores dimensions and caption controls");
        Assert(main.state.Notes==notes && Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId==sessionId,"Fullscreen leaves session identity/data intact");
        var transcript=Descendants(main).OfType<TextBox>().First(t=>t.IsReadOnly && t.TextWrapping==TextWrapping.Wrap && t.VerticalScrollBarVisibility==ScrollBarVisibility.Auto && t.Parent is Border{Tag:"ResizableTranscript"});
        var small=transcript.ActualHeight;main.state.Widgets.Single().Height=960;
        typeof(MainWindow).GetMethod("ArrangeWidgetFrames",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(main,null);Pump(TimeSpan.FromMilliseconds(50));
        Assert(transcript.ActualHeight>small+100,"Hedy transcript viewport grows substantially with widget resize");
        var settings=new SettingsDialog(main,store,main.state,main,"Session");settings.Show();Pump(TimeSpan.FromMilliseconds(30));
        Assert(Descendants(settings).OfType<ComboBox>().Any(c=>c.Items.OfType<string>().Contains("Manual Mode")) && !Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Edit Pixel Timer schedule")),"Session Settings directly embeds Manual/Scheduled controls");settings.Close();
        main.Close();
        var home=new HomeWindow(store);home.Show();Pump(TimeSpan.FromMilliseconds(40));
        SnapshotHome029(home,"nexa029-home-classic");
        foreach(var active in new[]{true,false,true,false}){Theme.SetSeasonalEnabled(active);Assert(Theme.SeasonalEnabled==active,"Repeated theme toggle refreshes current visual state");}
        Assert(WallpaperImages.Load(new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=Path.Combine(output,"missing.png")})==null,"Deleted wallpaper falls back without throwing");
        File.WriteAllText(Path.Combine(output,"damaged.png"),"broken image");Assert(WallpaperImages.Load(new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=Path.Combine(output,"damaged.png")})==null,"Damaged wallpaper falls back without throwing");
        home.Close();
    }
    private static void CheckClosingAnimation029()
    {
        var store=new StateStore(Path.Combine(output,"animation029.json"));var at=DateTimeOffset.UtcNow;
        var counter=WidgetLibrary.Create("Counter");counter.Value=15;counter.CustomSize=true;counter.Height=240;
        var water=WidgetLibrary.Create("Water Bottle Counter");water.Value=3;
        var state=new AppState{WidgetLayoutInitialized=true,Widgets=[counter,water],Notes="Closing snapshot",Session=new Session{Title="Animated fixture",Start=at.AddMinutes(-5),End=at.AddMinutes(20)}};
        state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings=new(){SeasonalMode="Off",ReduceMotion=false};store.Save(state);
        var window=OffscreenHandoff(new MainWindow(store));window.Show();Pump(TimeSpan.FromMilliseconds(60));
        Assert(window.FinalizeSession(at,true),"Animated completion commits before any presentation ticks");
        var committed=store.Load();Assert(committed.Widgets.Single(w=>w.Id==counter.Id).Value==0 && Continuity.Current(committed.Workspace!,store.UserId).History.Single().Data.Widgets.Single(w=>w.Id==counter.Id).Value==15,"On-disk reset and original archive are already correct during closing animation");
        if(NexaMotion.Allowed(window.state.Workspace!.Settings))
        {
            Assert(!window.FinalizeSession(at,true),"Repeated End Session input is blocked throughout closing animation");
            Pump(TimeSpan.FromMilliseconds(250));SnapshotHome029(window,"nexa029-closing-animation");
            Assert(typeof(MainWindow).GetField("completionLayer",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!=null,"Counters and vessels share a transient visual layer after successful archive");
            Pump(TimeSpan.FromMilliseconds(1200));
            Assert(typeof(MainWindow).GetField("completionLayer",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)==null && Application.Current.Windows.OfType<SessionRecapWindow>().Any(),"Closing presentation finishes and opens the existing recap");
        }
        foreach(var recap in Application.Current.Windows.OfType<SessionRecapWindow>().ToArray())recap.Close();window.Close();
        var recovered=new StateStore(store.FilePath).Load();Assert(Continuity.Current(recovered.Workspace!,store.UserId).History.Count==1 && recovered.Widgets.Single(w=>w.Id==counter.Id).Value==0,"Closing animation cannot add duplicate History or restore old counters");
        var linked=WidgetLibrary.Create("Notes","notes");linked.SessionScoped=false;
        Assert(WeeklySchedule.ResetSessionData(new(){Notes="Persistent linked notes",Widgets=[linked]}).Notes=="Persistent linked notes","Explicit linked Notes reset opt-out remains respected");
    }
    private static void CheckSystemAndBugs029()
    {
        var network=MainWindow.ReadWindowsNetworkStatus();File.WriteAllText(Path.Combine(output,"windows-network029.txt"),network);
        Assert(!string.IsNullOrWhiteSpace(network),"Read-only Windows connectivity query returns an explicit state on this host");
        using(var volume=new WindowsOutputVolume()){volume.Refresh();File.WriteAllText(Path.Combine(output,"windows-output029.txt"),$"Available={volume.Available}; Level={volume.Level}; Muted={volume.Muted}");Assert(!volume.Available || volume.Level is >=0 and <=1,"Read-only default output query is bounded without changing Windows volume");}
        var world=new BugManager(29);var bug=new BugEntity{Species=BugSpecies.Roaches,Health=100};world.Bugs.Add(bug);
        Assert(!world.Squash(bug) && bug.Health==80 && bug.HitFlash>0,"Direct nonfatal click updates actual health and preserves red hit feedback");
        world.Squash(bug);world.Squash(bug);world.Squash(bug);Assert(world.Squash(bug) && bug.Health==0 && bug.State==BugState.Squashed,"Full-health roach keeps five-click resilience and ends at zero health");
        var friendly=CreateWorld(BugBehavior.Cozy);var attacker=new BugEntity{Species=BugSpecies.Ants,Position=new Point(350,230),Target=new Point(350,230)};var victim=new BugEntity{Species=BugSpecies.Roaches,Position=new Point(350,230),Target=new Point(350,230),Decision=20};friendly.Bugs.AddRange([attacker,victim]);var health=victim.Health;
        attacker.EnemyId=victim.InteractionId;attacker.Activity=BugActivity.Raiding;attacker.Decision=20;attacker.Position=victim.Position;friendly.Advance(.1);
        Assert(victim.Health>=health && attacker.EnemyId==null,"Friendly mode cancels a stale advanced combat order before damage");
    }
    private static void CheckWallpaperPersistence029()
    {
        var path=Path.Combine(output,"wallpaper029.png");var bitmap=new RenderTargetBitmap(160,90,96,96,PixelFormats.Pbgra32);var art=new DrawingVisual();using(var dc=art.RenderOpen())dc.DrawRectangle(Brushes.MidnightBlue,null,new Rect(0,0,160,90));bitmap.Render(art);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(path))png.Save(file);
        Assert(WallpaperImages.Load(new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=path})!=null,"A real synthetic PNG decodes through the application wallpaper loader");
        var store=new StateStore(Path.Combine(output,"wallpaper-persistence029.json"));
        var first=Continuity.NewDashboard("Study","Study",new DashboardData(),store.UserId);var second=Continuity.NewDashboard("Work","Work",new DashboardData(),store.UserId);
        var workspace=new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=first.DashboardId,Dashboards=[first,second],Settings=new(){SeasonalMode="On",ReduceMotion=true,EnableF11=false,FullscreenNotification=false,ShowSystemIndicators=false,EnableVolumeSlider=false,EscExitsFullscreen=false,HomeWallpaper=new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=path,FitImage=true}}};
        store.Save(Continuity.Activate(workspace,store.UserId));var window=OffscreenHandoff(new MainWindow(store));window.Show();Pump(TimeSpan.FromMilliseconds(40));
        var layout=window.state.Widgets.Select(w=>(w.Id,w.Column,w.Row,w.Height,w.Span,w.PlacementX,w.PlacementY)).ToArray();
        Assert(window.SaveSpaceBackground(new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=path,FitImage=true}),"Space wallpaper saves through the current Space persistence path");
        Assert(layout.SequenceEqual(window.state.Widgets.Select(w=>(w.Id,w.Column,w.Row,w.Height,w.Span,w.PlacementX,w.PlacementY))),"Wallpaper changes preserve exact stored widget positions and sizes");
        window.Close();var loaded=store.Load();var options=loaded.Workspace!.Settings;
        Assert(loaded.Background.ImagePath==path && loaded.Background.FitImage && options.HomeWallpaper.ImagePath==path && options.HomeWallpaper.FitImage,"Home and Space image/fit choices persist independently through restart");
        Assert(loaded.Workspace.Dashboards.Single(d=>d.DashboardId==second.DashboardId).Current.Data.Background.Mode==SpaceBackgroundMode.Default,"Study wallpaper does not alter the other Space");
        Assert(!options.EnableF11 && !options.FullscreenNotification && !options.ShowSystemIndicators && !options.EnableVolumeSlider && !options.EscExitsFullscreen,"All five fullscreen preferences persist when disabled");
        options.SeasonalMode="Off";store.Save(loaded);Assert(store.Load().Background.ImagePath==path,"Disabling seasonal appearance preserves the custom wallpaper");
    }
    private static void SnapshotHome029(Window window,string name)
    {
        var content=(FrameworkElement)window.Content;content.UpdateLayout();var width=(int)content.ActualWidth;var height=(int)content.ActualHeight;
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(output,name+".png"));encoder.Save(file);
    }
}

