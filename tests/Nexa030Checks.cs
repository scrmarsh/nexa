using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Automation;
using System.Diagnostics;
using System.Text.Json;
using SessionDashboard;
internal static partial class Program
{
    private static void Check030()
    {
        Assert(App.AppVersion=="0.0.31" && App.VersionLabel=="nexa v0.0.31 alpha","Current refinement uses shared lowercase Alpha identity");
        Assert(ReleaseNotes.All[0].Title=="Release Integrity & Ecosystem Evolution" && ReleaseNotes.All.Single(n=>n.Version=="0.0.29").Title=="NEXAWEEN: Interface Polish","New active note preserves released .29 history");
        Assert(new EcosystemOptions().HitboxExpansion==1.5 && new EcosystemOptions{HitboxExpansion=double.NaN}.Safe().HitboxExpansion==1.5,"Legacy and invalid hit tolerance safely default to 1.5");
        Assert(new EcosystemOptions{HitboxExpansion=100}.Safe().HitboxExpansion==1.6,"Saved hit tolerance cannot create enormous invisible regions");
        CheckInput030();CheckPersistence030();CheckFullscreen030();CheckSpider030();Preview030();
    }
    private static void CheckInput030()
    {
        var manager=CreateWorld(BugBehavior.Cozy);manager.Bugs.Clear();
        var overlay=new BugOverlay(manager);overlay.Measure(new Size(800,500));overlay.Arrange(new Rect(0,0,800,500));
        overlay.SetWorldMapping(new Point(20,-50),new Size(800,500));overlay.SetViewport(new Rect(0,0,800,500));overlay.SetProtectedWidgets([new Rect(0,0,800,500)]);
        foreach(var species in BugManager.AllSpecies)
        {
            var bug=new BugEntity{Species=species,Position=new(600,500),State=BugState.Crossing,Scale=.75};manager.Bugs.Add(bug);
            var point=overlay.WorldToScreen(bug.Position);
            Assert(overlay.HitAt(point)==bug,species+" receives a hit over a protected widget after scroll/projection");
            bug.Angle=90;
            Assert(overlay.HitAt(point+new Vector(0,4))==bug && overlay.HitAt(point+new Vector(120,0))==null,species+" rotated small silhouette has padding and bounded reach");
            manager.Bugs.Clear();
        }
        var bottom=new BugEntity{Species=BugSpecies.Ants,Position=new(600,500),State=BugState.Crossing};var top=new BugEntity{Species=BugSpecies.Ants,Position=bottom.Position,State=BugState.Crossing};manager.Bugs.Add(bottom);manager.Bugs.Add(top);
        Assert(overlay.HitAt(overlay.WorldToScreen(top.Position))==top,"Last drawn eligible bug wins overlapping hits deterministically");
        top.State=BugState.Hiding;Assert(overlay.HitAt(overlay.WorldToScreen(top.Position))==bottom,"Hidden bug cannot steal a visible bug hit");
        manager.Bugs.Clear();
        var grid=new Grid();var clicks=0;var button=new Button{Content="Underlying widget",Width=250,Height=160};button.Click+=(_,_)=>clicks++;grid.Children.Add(button);grid.Children.Add(overlay);
        var host=OffscreenHandoff(new Window{Width=800,Height=500,Content=grid});host.Show();Pump(TimeSpan.FromMilliseconds(40));
        overlay.SetWorldMapping(new Point(),new Size(800,500));overlay.SetViewport(new Rect(0,0,800,500));
        var fatal=new BugEntity{Species=BugSpecies.Ants,Position=new(800,500),State=BugState.Crossing};manager.Bugs.Add(fatal);var awarded=0;overlay.DirectSquashed+=_=>awarded++;
        var hit=overlay.WorldToScreen(fatal.Position);
        Assert(VisualTreeHelper.HitTest(grid,hit)?.VisualHit==overlay,"Native WPF hit routing chooses bug overlay before underlying button");
        Assert(overlay.BeginPointer(hit) && fatal.State==BugState.Squashed && awarded==1,"Fatal state and recovery registration finish in the same input turn");
        Assert(overlay.ConsumingPointer && Mouse.Captured==overlay,"Valid hit captures pointer through movement and release");
        overlay.EndPointer();Pump(TimeSpan.FromMilliseconds(40));
        Assert(!overlay.ConsumingPointer && Mouse.Captured!=overlay && awarded==1 && clicks==0,"Pointer release is consumed and one death callback is delivered");
        Assert(!overlay.BeginPointer(new Point(10,10)),"Outside all bug hit regions leaves ordinary widgets available");
        Console.WriteLine($"MEASURE fatal pointer handler: {overlay.LastPointerMilliseconds:F3} ms; durable snapshots deferred until dispatcher background turn.");host.Close();
    }
    private static void CheckPersistence030()
    {
        var store=new StateStore(Path.Combine(output,"squash030.json"));var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(50));
        main.state.Workspace!.Settings.ReduceMotion=true;main.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions()));
        var world=NexaEcosystem.For(store);world.Manager.WorldPaused=true;
        var first=new BugEntity{Species=BugSpecies.Ants};var before=main.state.Workspace.AllTimeSquashes;
        var task=world.RecordSquashResponsive(first,main);
        Assert(task.IsCompleted && main.state.Workspace.AllTimeSquashes==before+1,"Input credits a queued kill immediately without a forced save");
        WaitHandoff(()=>task.IsCompleted,"In-memory queued scoring completes");task.GetAwaiter().GetResult();
        Assert(main.SaveNow() && main.state.Workspace.AllTimeSquashes==before+1 && store.Load().Workspace!.AllTimeSquashes==before+1,"Squash count reaches durable workspace exactly once");
        var twice=world.RecordSquashResponsive(first,main);WaitHandoff(()=>twice.IsCompleted,"Repeated squash event persistence completes");twice.GetAwaiter().GetResult();
        Assert(main.state.Workspace.AllTimeSquashes==before+1,"Repeated death event cannot duplicate score or species totals");
        var failed=new BugEntity{Species=BugSpecies.Spiders};
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))
        {
            var retry=world.RecordSquashResponsive(failed,main);WaitHandoff(()=>retry.IsCompleted,"Locked workspace returns without blocking dispatcher or losing pending event");retry.GetAwaiter().GetResult();
            world.Save(); // Explicit checkpoint, not a save on every hit.
            var pending=JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!;
            Assert(pending.PendingSquashes.Any(p=>p.BugId==failed.InteractionId),"Checkpointed ecosystem event survives workspace save failure");
        }
        world.RetryHistory();
        Assert(main.state.Workspace.AllTimeSquashes==before+2 && store.Load().Workspace!.AllTimeSquashes==before+2,"Recovery retries failed event without duplicating already-applied ledger");
        var doc=JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!;
        Assert(doc.PendingSquashes.Count==0,"Successfully committed history clears durable pending events");
        var simultaneous=Enumerable.Range(0,3).Select(_=>world.RecordSquashResponsive(new BugEntity{Species=BugSpecies.Ladybugs},main)).ToArray();
        WaitHandoff(()=>simultaneous.All(t=>t.IsCompleted),"Rapid fatal events serialize background persistence safely");foreach(var write in simultaneous)write.GetAwaiter().GetResult();
        Assert(main.SaveNow() && main.state.Workspace.AllTimeSquashes==before+5 && store.Load().Workspace!.AllTimeSquashes==before+5,"Rapid hits preserve every award and final durable count");
        Console.WriteLine($"MEASURE queued in-memory squash scoring (no per-hit disk writes): {world.LastSquashPersistenceMilliseconds:F3} ms.");main.Close();
    }
    private static void CheckFullscreen030()
    {
        var store=new StateStore(Path.Combine(output,"fullscreen030.json"));var saved=store.Load();saved.Workspace=Continuity.Migrate(saved,store.UserId);saved.Workspace.Settings.ReduceMotion=true;saved.Workspace.Settings.ShowSystemIndicators=false;store.Save(saved);
        var home=OffscreenHandoff(new HomeWindow(store){Width=1220,Height=880});home.Show();Pump(TimeSpan.FromMilliseconds(40));
        var original=new Rect(home.Left,home.Top,home.Width,home.Height);
        void Key(Window w,Key key)=>w.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w)!,0,key){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        home.Saved.Workspace!.Settings.EnableF11=false;Key(home,System.Windows.Input.Key.F11);Assert(!NexaFullscreen.IsFullscreen(home),"Home honors disabled F11 preference");
        home.Saved.Workspace.Settings.EnableF11=true;Key(home,System.Windows.Input.Key.F11);Pump(TimeSpan.FromMilliseconds(40));
        home.Saved.Workspace.Settings.EscExitsFullscreen=false;Key(home,System.Windows.Input.Key.Escape);Assert(NexaFullscreen.IsFullscreen(home),"Home respects disabled Escape exit preference");
        home.Saved.Workspace.Settings.EscExitsFullscreen=true;Key(home,System.Windows.Input.Key.Escape);Assert(!NexaFullscreen.IsFullscreen(home) && home.IsVisible,"Escape exits Home fullscreen without closing Home");Key(home,System.Windows.Input.Key.F11);
        Assert(NexaFullscreen.IsFullscreen(home) && home.ResizeMode==ResizeMode.NoResize,"F11 enters shared fullscreen on Home");Save030(home,"home-fullscreen");
        var preview=new SplashWindow(home.Saved.Workspace.Settings);var bounds=new Rect(home.Left,home.Top,home.Width,home.Height);var task=preview.PreviewIn((Grid)((HomeWindow)home).Body!);
        Pump(TimeSpan.FromMilliseconds(80));Assert(preview.Width==620 && preview.Height==350 && NexaFullscreen.IsFullscreen(home) && new Rect(home.Left,home.Top,home.Width,home.Height)==bounds,"Standalone fullscreen-host preview leaves host size and fullscreen untouched");
        preview.Close();WaitHandoff(()=>task.IsCompleted,"Closing preview restores existing fullscreen Home immediately");task.GetAwaiter().GetResult();
        var change=NexaNavigation.Change(home,()=>Task.FromResult<Window>(new MainWindow(store)),saved.Workspace.Settings);WaitHandoff(()=>change.IsCompleted,"Home to dashboard navigation finishes in fullscreen");change.GetAwaiter().GetResult();
        var main=(MainWindow)Application.Current.MainWindow;
        Assert(main.IsFullscreen && Descendants(main).OfType<Button>().Where(b=>b.Content is NexaCaptionIcon).All(b=>b.Visibility==Visibility.Collapsed),"Dashboard inherits shared fullscreen and hides caption controls");Save030(main,"dashboard-fullscreen");
        change=NexaNavigation.Change(main,()=>Task.FromResult<Window>(new HomeWindow(store)),saved.Workspace.Settings);WaitHandoff(()=>change.IsCompleted,"Dashboard to Home navigation finishes in fullscreen");change.GetAwaiter().GetResult();
        var second=(HomeWindow)Application.Current.MainWindow;Assert(NexaFullscreen.IsFullscreen(second),"Return Home retains fullscreen in same navigation chain");
        change=NexaNavigation.Change(second,()=>Task.FromResult<Window>(new MainWindow(store)),saved.Workspace.Settings);WaitHandoff(()=>change.IsCompleted,"Home can open another dashboard while fullscreen");change.GetAwaiter().GetResult();main=(MainWindow)Application.Current.MainWindow;
        main.ToggleFullscreen();Pump(TimeSpan.FromMilliseconds(40));
        Assert(!main.IsFullscreen && main.WindowState==WindowState.Normal && Math.Abs(main.Width-original.Width)<1 && Math.Abs(main.Height-original.Height)<1 && Math.Abs(main.Left-original.Left)<1 && Math.Abs(main.Top-original.Top)<1,"Leaving fullscreen restores original chain bounds and window state");
        var windowedBounds=new Rect(main.Left,main.Top,main.Width,main.Height);var windowed=new SplashWindow(saved.Workspace.Settings);task=windowed.PreviewIn((Grid)main.Content);Pump(TimeSpan.FromMilliseconds(50));
        Assert(windowed.IsVisible && windowed.Width==620 && windowed.Height==350 && new Rect(main.Left,main.Top,main.Width,main.Height)==windowedBounds && !main.IsFullscreen,"Windowed standalone preview leaves host bounds and fullscreen unchanged");windowed.Close();WaitHandoff(()=>task.IsCompleted,"Windowed preview can close without changing the current session");task.GetAwaiter().GetResult();
        var controls=Descendants(main).OfType<Button>().Where(b=>b.Content is NexaCaptionIcon).ToArray();
        Assert(controls.Length==3 && controls.All(b=>AutomationProperties.GetName(b).Length>0),"Dashboard captions use vector strokes with accessible names");
        controls.Single(b=>((NexaCaptionIcon)b.Content).Kind=="Maximize").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(TimeSpan.FromMilliseconds(30));
        Assert(main.WindowState==WindowState.Maximized && controls.Any(b=>b.Content is NexaCaptionIcon{Kind:"Restore"}),"Maximize changes vector icon to overlapping restore squares");Save030(main,"caption-controls-maximized");
        controls.Single(b=>b.Content is NexaCaptionIcon{Kind:"Restore"}).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));main.Close();
        var restarted=OffscreenHandoff(new HomeWindow(store));restarted.Show();Assert(!NexaFullscreen.IsFullscreen(restarted),"Fresh restart remains windowed by default");restarted.Close();
    }
    private static void CheckSpider030()
    {
        var spider=new HomeSpider();var blocked=new Rect(220,190,110,90);spider.Configure(new Size(1440,940),[blocked]);var valid=true;var activities=new HashSet<string>();
        for(var i=0;i<24000;i++){spider.Step(.05);valid &= spider.Valid(spider.Bug.Position);if(spider.Candy is {} candy)valid &= spider.Valid(candy);activities.Add(spider.Activity);}
        Assert(valid,"Twenty simulated minutes keep spider and candy inside territory and outside controls");
        Assert(spider.Pickups>4 && activities.Contains("Groom") && activities.Contains("Rest") && activities.Contains("Wander"),"Home companion independently wanders, rests, grooms and physically reaches candy");
        Console.WriteLine($"MEASURE Home companion simulation: {spider.Pickups} decorative pickups across 1,200 simulated seconds; no persistent rewards.");
        var min=double.MaxValue;var max=double.MinValue;var jump=0d;var last=NexaPumpkin.Candle(0,1);
        for(var i=0;i<10000;i++){var v=NexaPumpkin.Candle(i*.05,1);min=Math.Min(min,v);max=Math.Max(max,v);jump=Math.Max(jump,Math.Abs(v-last));last=v;}
        Assert(min>.05 && max<.4 && max-min>.2 && jump<.035,"Home candlelight visibly varies smoothly without flashing");
        Assert(NexaPumpkin.Candle(2,1)!=NexaPumpkin.Candle(2,3),"Multiple pumpkins have independent flicker phase");
    }
    private static void Save030(Window window,string name)
    {
        var folder=Path.GetFullPath("docs/media/nexa030");Directory.CreateDirectory(folder);window.UpdateLayout();var root=(FrameworkElement)window.Content;
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));encoder.Save(file);
    }
    private static void Preview030()
    {
        var folder=Path.GetFullPath("docs/media/nexa030");Directory.CreateDirectory(folder);
        var store=new StateStore(Path.Combine(output,"preview030.json"));
        var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);
        state.Workspace.Settings.SeasonalMode="On";state.Workspace.Settings.SeasonalAnimation=true;state.Workspace.Settings.SeasonalDecorations=true;
        store.Save(state);store.SaveBugPreferences(new BugPreferences(Ecosystem:new EcosystemOptions{AdvancedAnimations=true}));
        var home=OffscreenHandoff(new HomeWindow(store));home.Width=1440;home.Height=940;home.Show();Pump(TimeSpan.FromMilliseconds(3200));
        Save(home,"home-seasonal");
        var companion=Descendants(home).OfType<SeasonalCorners>().FirstOrDefault();
        Assert(companion!=null,"Home contains the shared seasonal companion renderer");
        if(companion!=null)
        {
            companion.AdvancedAnimations=false;
            companion.Companion.Bug.Position=new(130,260);companion.InvalidateVisual();Pump(TimeSpan.FromMilliseconds(50));Save(home,"home-spider-center");
            companion.Companion.Bug.Position=companion.Companion.Territory.TopLeft+new Vector(3,3);companion.InvalidateVisual();Save(home,"home-spider-boundary");
            for(var i=0;i<140 && companion.Companion.Candy==null;i++)companion.Companion.Step(.05);companion.AdvancedAnimations=true;companion.InvalidateVisual();Save(home,"home-spider-candy");
            companion.AdvancedAnimations=true;
            Assert(!NexaMotion.Allowed(home.Saved.Workspace!.Settings) || companion.IsAnimating,"Visible seasonal Home starts its limited animation timer");
            home.Saved.Workspace.Settings.ReduceMotion=true;companion.Options=home.Saved.Workspace.Settings;
            Assert(!companion.IsAnimating,"Reduced motion stops the corner animation timer");
            home.Saved.Workspace.Settings.ReduceMotion=false;companion.Options=home.Saved.Workspace.Settings;
            home.Hide();Assert(!companion.IsAnimating,"Hidden Home stops companion and candle timer");home.Show();
        }
        var splash=OffscreenHandoff(new SplashWindow(new NexaOptions{ReduceMotion=true}));splash.Show();Pump(TimeSpan.FromMilliseconds(150));Save(splash,"splash-seasonal");splash.Close();
        home.Width=800;home.Height=600;Pump(TimeSpan.FromMilliseconds(50));Save(home,"home-seasonal-small");
        var footer=Descendants(home).OfType<Button>().Single(b=>Equals(b.Content,"About nexa"));
        var corners=Descendants(home).OfType<SeasonalCorners>().Single();var fruit=SeasonalCorners.PumpkinBounds(corners.RenderSize);
        var footerBounds=footer.TransformToVisual(corners).TransformBounds(new Rect(footer.RenderSize));
        Assert(!fruit.IntersectsWith(footerBounds),"Small Home pumpkin stays above footer controls");
        home.Width=1440;home.Height=940;home.Saved.Workspace!.Settings.SeasonalMode="Off";Theme.SetSeasonalEnabled(false);Pump(TimeSpan.FromMilliseconds(500));Save(home,"home-classic");
        Assert(!corners.IsVisible && !corners.IsAnimating,"Seasonal Off removes artwork and stops its timer immediately");
        Assert(ReferenceEquals(home.Icon,NexaChrome.Icon),"Classic Windows icon remains stable after seasonal toggle");
        splash=OffscreenHandoff(new SplashWindow(new NexaOptions{ReduceMotion=true}));splash.Show();Pump(TimeSpan.FromMilliseconds(50));Save(splash,"splash-classic");
        Assert(Descendants(splash).OfType<TextBlock>().Any(t=>new System.Windows.Documents.TextRange(t.ContentStart,t.ContentEnd).Text=="a space for whatever comes next"),"Classic splash uses exact animated tagline text");splash.Close();home.Close();
        Theme.SetSeasonalEnabled(true);
        var widgets=new AppState{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Counter"),WidgetLibrary.Create("Notes") ]};
        widgets.Workspace=Continuity.Migrate(widgets,store.UserId);widgets.Workspace.Settings.SeasonalMode="On";widgets.Workspace.Settings.ReduceMotion=true;store.Save(widgets);
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(150));Save(main,"widget-pumpkin");
        main.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions{AdvancedAnimations=true}));
        var world=NexaEcosystem.For(store);world.Manager.Bugs.Clear();world.Manager.WorldPaused=true;
        if(main.bugsOverlay is {} overlay)
        {world.Manager.Bugs.Add(new BugEntity{Species=BugSpecies.Spiders,Position=overlay.ScreenToWorld(new Point(320,260)),State=BugState.Crossing,Scale=1.3});overlay.RefreshFrame();Pump(TimeSpan.FromMilliseconds(50));Save(main,"bug-near-widget");}
        main.Close();Console.WriteLine("WPF previews: "+folder);
        void Save(Window window,string name)
        {
            window.UpdateLayout();var root=(FrameworkElement)window.Content;
            var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));encoder.Save(file);
        }
    }
}
