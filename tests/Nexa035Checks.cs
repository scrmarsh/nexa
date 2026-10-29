using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void Click035(DependencyObject root,string label)=>Descendants(root).OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void Invoke035(object instance,string method)=>instance.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(instance,new object[]{instance,new RoutedEventArgs()});
    private static StateStore Store035(string name,bool seasonal=false)
    {
        var store=new StateStore(Path.Combine(output,"nexa035",name,"state.json"));var saved=store.Load();saved.Workspace=Continuity.Migrate(saved,store.UserId);saved.Workspace.Settings.SeasonalMode=seasonal?"On":"Off";saved.Workspace.Settings.ReduceMotion=true;saved.Workspace.Settings.QuickStartCompleted=true;store.Save(saved);return store;
    }
    private static void Capture035(Window window,string name)
    {
        window.UpdateLayout();var root=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(root.ActualWidth)),Math.Max(1,(int)Math.Ceiling(root.ActualHeight)),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
        var folder=Path.GetFullPath("docs/media/nexa035");Directory.CreateDirectory(folder);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(folder,name+".png"));encoder.Save(stream);
    }
    private static IEnumerable<DependencyObject> Visual035(DependencyObject root)
    {yield return root;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Visual035(VisualTreeHelper.GetChild(root,i)))yield return child;}
    private static void Check035()
    {
        Assert(App.VersionLabel=="nexa v0.0.35 alpha" && typeof(App).Assembly.GetName().Version==new Version(0,0,35,0),".35 label and Windows assembly identity agree");
        var store=Store035("home");var home=new HomeWindow(store);home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var tagline=Descendants(home).OfType<TextBlock>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Home tagline");
        Assert(new System.Windows.Documents.TextRange(tagline.ContentStart,tagline.ContentEnd).Text=="a space for whatever comes next" && tagline.Effect is System.Windows.Media.Effects.DropShadowEffect{ShadowDepth:0,BlurRadius:7},"Exact Classic tagline has a soft glyph-shaped white halo");
        var wall=(Border)typeof(HomeWindow).GetField("homeWallpaper",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(home)!;
        Assert(wall.Background is SolidColorBrush charcoal && charcoal.Color==Color.FromRgb(18,22,32),"Default charcoal is one shade darker");
        Assert(!Visual035(home).OfType<Border>().Any(b=>b.IsVisible && b.BorderThickness.Left>=16 && b.BorderBrush is SolidColorBrush blue && blue.Color==Color.FromRgb(24,95,165)),"Default Home has no thick blue frame");
        home.PreviewHomeWallpaper(store,new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#345678"});Assert(wall.Background is SolidColorBrush solid && solid.Color==Color.FromRgb(52,86,120),"User-selected solid wallpaper color stays intact");home.PreviewHomeWallpaper(store,new());
        var name=new DisplayNameDialog(home,store,_=>{});name.Show();Pump(TimeSpan.FromMilliseconds(40));
        Assert(name.WindowStartupLocation==WindowStartupLocation.CenterOwner && name.NameInput.FontSize==23 && name.NameInput.TextAlignment==TextAlignment.Center,"Name editor is centered with a large centered input");
        var original=store.LoadProfile().DisplayName;Capture035(name,"display-name");name.NameInput.Text="Canceled name";Click035(name,"Cancel");Assert(store.LoadProfile().DisplayName==original,"Cancel leaves the saved display name intact");
        name=new DisplayNameDialog(home,store,_=>{});name.Show();name.NameInput.Text="  ";Click035(name,"Save");Assert(name.IsVisible,"Blank name validation keeps the editor open");name.NameInput.Text="Fixture Person";Click035(name,"Save");Assert(store.LoadProfile().DisplayName=="Fixture Person","Display name is durably saved");
        var settings=new SettingsDialog(home,store,home.Saved,null,"Integrations");settings.Show();Pump(TimeSpan.FromMilliseconds(50));
        Assert(Descendants(settings).OfType<TextBlock>().Count(t=>t.Text=="Hedy")==1 && Descendants(settings).OfType<TextBlock>().Count(t=>t.Text=="Hevy")==1,"Integrations shows one Hedy recording provider and one genuine Hevy workout provider");
        Assert(Descendants(settings).OfType<Button>().Count(b=>Equals(b.Content,"Connect Hevy"))==1,"Hevy retains its separately labeled connection");
        Capture035(settings,"integrations");settings.Close();Capture035(home,"home-classic");home.Close();
        CheckStorage035();CheckDashboard035();CheckTransitions035();CheckSeason035();
    }
    private static void CheckStorage035()
    {
        var root=Path.Combine(output,"migration035");var legacy=Path.Combine(root,"legacy");var docs=Path.Combine(root,"documents");var target=Path.Combine(root,"Nexa");
        Directory.CreateDirectory(Path.Combine(legacy,"LivingWorlds"));var foreign=Path.Combine(legacy,"LivingWorlds","world.json");File.WriteAllText(foreign,"unrelated game bytes");
        var source=new StateStore(Path.Combine(legacy,"state.json"));source.Save(source.Load());var world=NexaEcosystem.For(source);world.Save();var digest=NexaData.Hash(world.FilePath);var gameHash=NexaData.Hash(foreign);
        NexaData.Migrate(legacy,docs,target);NexaData.Migrate(legacy,docs,target);
        Assert(!Directory.Exists(Path.Combine(target,"LivingWorlds")) && NexaData.Hash(foreign)==gameHash,"Upgrade skips the unrelated Living Worlds game and preserves its original bytes");
        var migrated=new StateStore(Path.Combine(target,"state.json"));var next=NexaEcosystem.For(migrated);
        Assert(NexaData.Hash(next.FilePath)==digest && File.Exists(world.FilePath),"Owned ecosystem state migrates without renaming compatibility identifiers or changing originals");
        Assert(!Directory.Exists(Path.Combine(target,"Living Worlds")),"Fresh/upgrade ecosystem opens create no Living Worlds directory");
        world.Dispose();next.Dispose();
    }
    private static void CheckDashboard035()
    {
        var store=Store035("dashboard");var saved=store.Load();saved.Session=new(){Title="Timeline fixture",Start=DateTimeOffset.Now.AddMinutes(-15),End=DateTimeOffset.Now.AddMinutes(45),IntervalMinutes=5};Continuity.CaptureCurrent(saved,store.UserId,DateTimeOffset.UtcNow);store.Save(saved);
        var main=new MainWindow(store);main.Show();Pump(TimeSpan.FromMilliseconds(120));
        foreach(var iconId in new[]{"Graduation cap","Briefcase","Palette"})foreach(var fontSize in new[]{16d,20d,24d})
        {
            main.state.SpaceIcon=iconId;main.TitleInput.FontSize=fontSize;main.TitleInput.Text="A long Space title used to verify header alignment";
            typeof(MainWindow).GetMethod("RefreshDashboardPicker",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(main,null);main.UpdateLayout();
            var icon=(FrameworkElement)main.SpaceIconHost.Content;var art=icon.TransformToVisual(main.DashboardHeader).TransformBounds(new Rect(icon.RenderSize));var title=main.TitleInput.TransformToVisual(main.DashboardHeader).TransformBounds(new Rect(main.TitleInput.RenderSize));
            Assert(icon.Margin==new Thickness(0) && Math.Abs((art.Top+art.Bottom-title.Top-title.Bottom)/2)<1,"Header icon centers with the name at "+fontSize+" DIP: "+iconId);
        }
        main.TitleInput.FontSize=16;main.TitleInput.Text="Timeline fixture";
        Assert(main.TopNavigation.Children.OfType<Button>().Count(b=>b.IsVisible)==3 && main.GlobalSettingsButton.IsVisible && main.SessionSettingsButton.IsVisible,"Primary dashboard navigation has exactly Home, Settings and Session");
        Assert(main.WidgetCanvas.Children.OfType<Button>().Count(b=>System.Windows.Automation.AutomationProperties.GetName(b)=="Add Widget in empty workspace")==1 && !main.AddWidgetButton.IsVisible,"Only the bottom Add Widget action is visible");
        foreach(var viewer in Visual035(main).OfType<ScrollViewer>().Where(v=>v.IsVisible))Assert(viewer.ComputedVerticalScrollBarVisibility!=Visibility.Visible && viewer.ComputedHorizontalScrollBarVisibility!=Visibility.Visible,"Viewer bars hidden: "+viewer.Name+" / "+viewer.Tag+" / "+viewer.VerticalScrollBarVisibility+" / "+viewer.HorizontalScrollBarVisibility);
        var timeline=main.state.Widgets.First(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline");
        foreach(var count in new[]{3,6,10,14})
        {
            main.state.Session!.End=main.state.Session.Start.AddMinutes(count*5);timeline.Height=count<=3?720:count<=6?1320:2400;timeline.CustomSize=true;
            main.RenderWidgets();main.WidgetScroller.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(160));
            var view=Descendants(main).OfType<Border>().Where(b=>Equals(b.Tag,"ResizableTranscript")).Select(b=>b.Child).OfType<ScrollViewer>().First();
            Assert(((StackPanel)view.Content).Children.OfType<Border>().Count()==count,$"Timeline builds all {count} intervals");
            if(count<=10)Assert(view.ScrollableHeight<2,$"Expanded timeline can display {count} intervals without internal scrolling");
            if(count==14){timeline.Height=720;main.RenderWidgets();Pump(TimeSpan.FromMilliseconds(80));}
        }
        Assert(main.SaveNow() && store.Load().Widgets.Single(w=>w.Id==timeline.Id).Height==720,"Resized timeline dimensions persist");
        main.ApplyBugPreferences(new BugPreferences(true,BugSpecies.Spiders,BugDensity.Normal,Ecosystem:new()));main.WidgetScroller.ScrollToTop();Pump(TimeSpan.FromMilliseconds(80));
        Assert(main.bugsOverlay?.ScrollTarget==main.WidgetScroller && NexaScrolling.ScrollFrom(main.bugsOverlay,-120),"Wheel input on the sibling bug overlay routes into the dashboard scroller");Pump(TimeSpan.FromMilliseconds(30));Assert(main.WidgetScroller.VerticalOffset>0,"Scrolling over a bug moves the dashboard");
        Invoke035(main,"HomeClick");var confirmation=main.OwnedWindows.OfType<NexaDialog>().Single(w=>w.Title=="nexa · End session");
        var id=Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId;Click035(confirmation,"Cancel");Assert(main.state.Session!.FinalizedAt==null && Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId==id,"Cancel keeps the same active session");
        Invoke035(main,"HomeClick");confirmation=main.OwnedWindows.OfType<NexaDialog>().Single(w=>w.Title=="nexa · End session");
        var switching=main.CopyWorkspaceState().Workspace!;var other=Continuity.NewDashboard("Switched fixture","Focus",Continuity.Capture(main.state),store.UserId);switching.Dashboards.Add(other);switching.ActiveDashboardId=other.DashboardId;
        Assert(main.CommitWorkspace(switching,sessionLoad:true),"Switching Space with a pending end confirmation commits normally");
        Assert(!confirmation.IsVisible && main.state.Session!.FinalizedAt==null,"A Space switch dismisses the stale confirmation without ending the newly active session");
        Invoke035(main,"HomeClick");Invoke035(main,"HomeClick");Assert(main.OwnedWindows.OfType<Window>().Count(w=>w.Title=="nexa · End session")==1,"Repeated Home clicks open only one confirmation");
        confirmation=main.OwnedWindows.OfType<NexaDialog>().Single(w=>w.Title=="nexa · End session");Click035(confirmation,"End Session");Pump(TimeSpan.FromMilliseconds(80));
        var recap=main.OwnedWindows.OfType<SessionRecapWindow>().Single();Assert(main.state.Session!.FinalizedAt!=null,"End Session finalizes before opening its saved recap");
        var totals=ProfileActivity.Completed(main.state.Workspace!.Dashboards.SelectMany(d=>d.History.Append(d.Current))).Length;
        Capture035(recap,"recap");Click035(recap,"← Return to Dashboard");Pump(TimeSpan.FromMilliseconds(250));
        Assert(main.FinalizeSession(showRecap:false) && ProfileActivity.Completed(main.state.Workspace!.Dashboards.SelectMany(d=>d.History.Append(d.Current))).Length==totals,"Repeated finalization and Return to Dashboard do not duplicate completed sessions");
        var record=Continuity.Current(main.state.Workspace,store.UserId);var before=JsonSerializer.Serialize(record.Current);var compact=SessionRecapWindow.Compact(record.Name,record.Current,main.state.Workspace!.Settings);
        Assert(Descendants(compact).OfType<TextBlock>().Any(t=>t.Text.Contains("meaningful actions")) && before==JsonSerializer.Serialize(record.Current),"Compact History recap uses stored actions without changing the record");
        var history=new SessionHistoryWindow(main,store,main);history.Show();Pump(TimeSpan.FromMilliseconds(300));Capture035(history,"notes-history");history.Close();
        main.state.Workspace.Settings.ShowSystemIndicators=true;main.state.Workspace.Settings.EnableVolumeSlider=true;main.ToggleFullscreen();Pump(TimeSpan.FromMilliseconds(100));
        Assert(main.SystemIndicators.Children.OfType<Slider>().Single().Width==90,"Fullscreen exposes the compact inline Windows volume slider");
        Capture035(main,"fullscreen");main.ToggleFullscreen();
        // Hold only a disposable fixture file: failed persistence must leave the session unfinalized.
        main.state.Session.FinalizedAt=null;record.Current.Recap=null;record.Current.SessionValuesReset=false;main.state.Session.StoppedAt=null;main.SaveNow();
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))Assert(!main.FinalizeSession(showRecap:false) && main.state.Session.FinalizedAt==null,"Failed checkpoint never claims that an active session was finalized");
        main.SaveNow();Assert(main.FinalizeSession(showRecap:false),"Finalization can be retried after the fixture file is unlocked");
        typeof(MainWindow).GetMethod("ShowSessionRecap",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(main,null);
        recap=main.OwnedWindows.OfType<SessionRecapWindow>().Single();Click035(recap,"⌂ Return to Home");
        WaitHandoff(()=>Application.Current.MainWindow is HomeWindow && !NexaNavigation.Busy(main),"Recap Return to Home completes saved navigation");
        ((HomeWindow)Application.Current.MainWindow).Close();
    }
    private static void CheckTransitions035()
    {
        foreach(var seasonal in new[]{false,true})foreach(var reduced in new[]{true,false})
        {
            var store=Store035("transition-"+seasonal+reduced,seasonal);var saved=store.Load();saved.Workspace!.Settings.ReduceMotion=reduced;store.Save(saved);
            for(var repetition=0;repetition<2;repetition++)
            {
                var home=new HomeWindow(store);home.Show();Pump(TimeSpan.FromMilliseconds(60));if(repetition==1)NexaFullscreen.Toggle(home);
                var transition=NexaNavigation.Change(home,()=>Task.FromResult<Window>(new MainWindow(store)),saved.Workspace.Settings);WaitHandoff(()=>transition.IsCompleted,"Prepared Home → Space transition completes");transition.GetAwaiter().GetResult();
                var main=(MainWindow)Application.Current.MainWindow;Assert(main.Opacity==1 && ((FrameworkElement)main.Content).Opacity==1 && main.IsVisible,"Destination stays opaque after reveal");
                Assert(NexaNavigation.LastProfile.ContainsKey("Crossfade")==!reduced,"Reduced motion uses atomic direct presentation; animated entry uses a snapshot crossfade");
                Assert(main.IsFullscreen==(repetition==1),"Fullscreen transfer survives navigation");main.Close();
            }
        }
    }
    private static void CheckSeason035()
    {
        var spider=new HomeSpider();spider.Configure(new Size(1440,940),[]);Assert(spider.HasRoom && spider.Radius>150,"Roomy Home companion approaches the seasonal pumpkin's leg-span size");
        for(var i=0;i<8;i++){var p=SpiderAnatomy.Root(i/2,i%2==0?-1:1);Assert(p.X<4 && Math.Pow((p.X-4)/4.5,2)+Math.Pow(p.Y/4.2,2)<1.01,"Spider leg attaches along the cephalothorax behind the face");}
        var store=Store035("season",true);var home=new HomeWindow(store);home.Show();Pump(TimeSpan.FromMilliseconds(100));var corners=Descendants(home).OfType<SeasonalCorners>().Single();
        bool ContainsImage(Drawing drawing)=>drawing is ImageDrawing || drawing is DrawingGroup group && group.Children.Any(ContainsImage);
        var homeArt=(DrawingGroup)typeof(SeasonalCorners).GetField("staticArt",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(corners)!;
        Assert(ContainsImage(homeArt),"Large Home pumpkin retains the original detailed image artwork");
        Assert(!ContainsImage(NexaweenDecorations.Asset(0)),"Small background pumpkins use the flat vector decoration only");
        foreach(var appearance in new[]{BugAppearance.Normal,BugAppearance.Creepy,BugAppearance.Halloween})
        {
            var options=new EcosystemOptions{MatchAppearance=false,Appearance=appearance,AdvancedAnimations=true};store.SaveBugPreferences(new BugPreferences(Ecosystem:options));home.RefreshCompanionOptions(store,options);Assert(corners.Appearance.EffectiveAppearance==appearance,"Home companion follows saved "+appearance+" appearance without behavior changes");Capture035(home,"home-spider-"+appearance);
        }
        home.Width=800;home.Height=600;Pump(TimeSpan.FromMilliseconds(100));Capture035(home,"home-small-seasonal");Console.WriteLine($"Small companion: radius={corners.Companion.Radius}, point={corners.Companion.Bug.Position}, territory={corners.Companion.Territory}, size={corners.RenderSize}");Assert(!corners.Companion.HasRoom || corners.Companion.Valid(corners.Companion.Bug.Position),"Small Home keeps the complete spider footprint outside reserved controls");home.Close();
        var icons=new StackPanel();icons.Children.Add(new SystemStatusIcon{Kind="battery",Percent=72,Charging=true});icons.Children.Add(new SystemStatusIcon{Connected=true});icons.Children.Add(new SystemStatusIcon{Connected=false});var preview=new NexaDialog{Width=300,Height=200,Content=icons};preview.Show();Capture035(preview,"system-icons");preview.Close();
    }
}
