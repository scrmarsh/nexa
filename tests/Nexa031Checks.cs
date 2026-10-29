using System.IO;
using System.Net;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Reflection;
using System.Text.Json;
using SessionDashboard;
internal static partial class Program
{
    private static void Check031()
    {
        Assert(App.VersionLabel=="nexa v0.0.34 alpha" && NexaReleaseIdentity.Current.Tag=="v0.0.34-alpha","Shared release model owns current label/tag");
        Assert(NexaReleaseIdentity.Canonicalize(new(0,1,26),"Alpha")==new Version(0,0,26) && NexaReleaseIdentity.Canonicalize(new(0,1,26),"Beta")==new Version(0,1,26),"Explicit legacy Alpha map never rewrites genuine Beta");
        Assert(NexaReleaseIdentity.Canonicalize(new(0,1,99),"Alpha")==new Version(0,1,99),"Unconfirmed legacy-looking versions are not silently remapped");
        Assert(NexaReleaseIdentity.Parse("v0.1.0-alpha",true)==null && NexaReleaseIdentity.Parse("v0.1.0-beta",true)?.Stage=="Beta","First Beta numbering cannot masquerade as an Alpha release");
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(File.ReadAllText("docs/NEXA_031_RELEASE_SNAPSHOT.json").Replace("\"tag\":","\"tag_name\":").Replace("\"title\":","\"name\":").Replace("\"url\":","\"browser_download_url\":"))})))
        {
            var release=new ExperimentalUpdater(client).CheckAsync(new(0,1,26)).GetAwaiter().GetResult();
            Assert(release?.CanonicalVersion==new Version(0,0,29),"Captured real unsorted GitHub releases offer .29 to a legacy .26 installation using corrected comparator");
        }
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(Feed(digest:"missing",version:"0.0.32"))})))
        {
            var failed=false;try{new ExperimentalUpdater(client).CheckAsync(new(0,0,31)).GetAwaiter().GetResult();}catch(InvalidDataException){failed=true;}
            Assert(failed,"Incomplete newer package cannot become a false up-to-date result");
        }
        Assert(NexaReleaseIdentity.Parse("v0.1.4",true)?.Stage=="Beta","An unsuffixed prerelease in the Beta series is never reinterpreted as historical Alpha");
        CheckHome031();CheckInterface031();CheckReset031();CheckSilk031();
    }
    private static void Capture031(Window window,string name)
    {
        var folder=Path.GetFullPath("docs/media/nexa034/continuity031");Directory.CreateDirectory(folder);window.UpdateLayout();var root=(FrameworkElement)window.Content;
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(root);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));encoder.Save(file);
    }
    private static void CheckHome031()
    {
        var store=new StateStore(Path.Combine(output,"home031.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="On";state.Workspace.Settings.ReduceMotion=true;store.Save(state);
        var home=OffscreenHandoff(new HomeWindow(store));
        home.Width=1280;home.Height=900;home.Show();Pump(TimeSpan.FromMilliseconds(80));Capture031(home,"home-normal");
        home.WindowState=WindowState.Maximized;Pump(TimeSpan.FromMilliseconds(120));Capture031(home,"home-maximized");
        var corners=Descendants(home).OfType<SeasonalCorners>().Single();
        Console.WriteLine($"HOME maximize window={home.ActualWidth}x{home.ActualHeight}; corners={corners.ActualWidth}x{corners.ActualHeight}; visible={corners.IsVisible}; fruit={SeasonalCorners.PumpkinBounds(corners.RenderSize)}");
        Assert(corners.IsVisible && corners.ActualWidth>0 && corners.ActualHeight>0,"Maximized Home retains a visible seasonal pumpkin surface");
        var title=Descendants(home.Recent).OfType<TextBlock>().Single(t=>t.FontSize==19);
        var card=Descendants(home.Recent).OfType<Button>().First(b=>b.Tag is Guid);
        var bounds=title.TransformToVisual(card).TransformBounds(new Rect(title.RenderSize));
        Assert(Math.Abs(bounds.X+bounds.Width/2-card.ActualWidth/2)<2,"Space name is independently centered across its card despite left icon");
        Assert(Descendants(card).OfType<System.Windows.Shapes.Path>().Single().Width==38,"Recent Space uses larger left-side icon");
        home.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(50));Capture031(home,"home-restored");
        NexaFullscreen.Toggle(home);Pump(TimeSpan.FromMilliseconds(40));Capture031(home,"home-fullscreen");Assert(corners.IsVisible && !SeasonalCorners.PumpkinBounds(corners.RenderSize).IsEmpty,"Pumpkin remains present in separate F11 fullscreen mode");NexaFullscreen.Toggle(home);home.Close();
    }
    private static void CheckInterface031()
    {
        var store=new StateStore(Path.Combine(output,"interface031.json"));var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(70));main.state.Workspace!.Settings.ReduceMotion=true;
        var settings=new SettingsDialog(main,store,main.state,main,"Session");settings.Show();Pump(TimeSpan.FromMilliseconds(40));
        Assert(Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Change Icon")),"Session Settings exposes Space Appearance → Change Icon");Capture031(settings,"session-settings");settings.Close();
        var before=main.state.SpaceIcon;main.EditSpaceIcon();Pump(TimeSpan.FromMilliseconds(30));var picker=Application.Current.Windows.OfType<SpaceIconChooser>().Single();
        Descendants(picker).OfType<Button>().Single(b=>Equals(b.Tag,"Cap and gown")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(main.state.SpaceIcon==before && picker.Selected=="Cap and gown","Icon choice previews without altering saved Space before Save");Capture031(picker,"icon-picker");
        Descendants(picker).OfType<Button>().Single(b=>Equals(b.Content,"Cancel")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(main.state.SpaceIcon==before,"Cancel preserves original Space icon");
        main.EditSpaceIcon();picker=Application.Current.Windows.OfType<SpaceIconChooser>().Single();Descendants(picker).OfType<Button>().Single(b=>Equals(b.Tag,"Cap and gown")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Descendants(picker).OfType<Button>().Single(b=>Equals(b.Content,"Save icon")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(main.state.SpaceIcon=="Cap and gown" && store.Load().SpaceIcon=="Cap and gown","Cap and gown saves through existing persistence and header refresh");
        settings=new SettingsDialog(main,store,main.state,main,"Updates");settings.Show();Pump(TimeSpan.FromMilliseconds(30));
        Assert(!Descendants(settings).OfType<TextBlock>().Any(t=>t.Text.Contains("Experimental update",StringComparison.OrdinalIgnoreCase)),"Permanent Updates page has no Experimental subtitle");Capture031(settings,"updates");settings.Close();
        settings=new SettingsDialog(main,store,main.state,main,"Sounds");settings.Show();Pump(TimeSpan.FromMilliseconds(30));
        var style=Descendants(settings).OfType<ComboBox>().Single(c=>c.Items.OfType<string>().Contains("Bell garden"));var sound=Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Test Sound"));
        Assert(style.Parent==sound.Parent && style.Parent is Panel panel && panel.Children.IndexOf(sound)==panel.Children.IndexOf(style)+1,"Test Sound is directly underneath the Sound Style dropdown");
        style.SelectedIndex=2;Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Apply preset to both feedback sounds")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(main.state.PositiveFeedbackSound==InterfaceAudio.PositiveChoices[2],"Sound style selection controls the previewed positive cue");Capture031(settings,"sounds");settings.Close();
        var status=(TextBlock)main.FindName("ElapsedLabel");main.UpdateClock();main.UpdateLayout();
        Assert(status.Text=="Ready for a fresh session" && status.TextWrapping==TextWrapping.Wrap && status.TextAlignment==TextAlignment.Center,"Pixel Timer fresh-session status wraps and centers without scheduling changes");Capture031(main,"pixel-status");main.Close();
    }
    private static void CheckReset031()
    {
        var store=new StateStore(Path.Combine(output,"reset031.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.ReduceMotion=true;state.Notes="Preserve notes";store.Save(state);store.SaveBugPreferences(new(true,BugSpecies.Spiders,BugDensity.Low,new EcosystemOptions{AdvancedAnimations=true}));
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(50));var world=NexaEcosystem.For(store);world.Manager.WorldPaused=true;
        var web=new EcosystemWeb{Owner=Guid.NewGuid(),X=500,Y=300,Construction=1};BugManager.EnsureSilk(web);world.Manager.Webs.Add(web);world.Save();var id=world.Manager.WorldId;
        main.Close();main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(30));
        Assert(world.Manager.Webs.Count==0,"Every Space entry resets transient live web state, including restart");
        var originalSession=Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId;var originalScore=main.state.Workspace!.AllTimeSquashes;
        var eventBug=new BugEntity{Species=BugSpecies.Spiders};var pending=world.RecordSquashResponsive(eventBug,main);
        var next=Continuity.Clone(main.state.Workspace);next.LiveEcosystemGeneration=Guid.NewGuid();
        Assert(main.CommitWorkspace(next,sessionLoad:true),"Explicit same-session load commits a fresh ecosystem generation");
        Assert(world.Manager.Webs.Count==0 && world.Manager.Bugs.Count==0 && world.Manager.WorldId==id,"Session load clears bugs/webs while retaining world identity");
        Assert(main.state.Notes=="Preserve notes" && Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId==originalSession,"Live reset does not erase notes or rewrite session identity");
        WaitHandoff(()=>pending.IsCompleted,"An in-flight original-session award survives the live reset");pending.GetAwaiter().GetResult();
        Assert(main.state.Workspace!.AllTimeSquashes==originalScore+1,"Pending direct-squash award is committed once through reset");
        world.Save();var saved=JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!;
        Assert(saved.SessionGeneration==main.state.Workspace.LiveEcosystemGeneration && saved.Webs.Count==0,"Delayed persistence cannot resurrect previous-generation web geometry");
        var same=world.Manager.SessionGeneration;world.ReconcileGeneration(same);Assert(world.Manager.SessionGeneration==same,"Reconciling an already-current generation is idempotent");main.Close();
    }
    private static void CheckSilk031()
    {
        Span<BugLegPose> poses=stackalloc BugLegPose[8];BugAnimation.FillLegs(BugSpecies.Spiders,.7,false,poses);
        Assert(poses.Length==8 && poses.ToArray().Count(p=>p.Root.Y<0)==4 && poses.ToArray().Count(p=>p.Root.Y>0)==4,"Shared spider has four front-body leg roots per side");
        Assert(poses.ToArray().Select(p=>p.Root).Distinct().Count()==8 && poses.ToArray().All(p=>Math.Pow((p.Root.X-4)/4.6,2)+Math.Pow(p.Root.Y/3.7,2)<1.01),"Eight evenly spaced roots follow the rounded front-body boundary");
        var manager=CreateWorld(BugBehavior.Creepy);manager.Bugs.Clear();manager.Webs.Clear();manager.Resources.Clear();
        var bug=new BugEntity{Species=BugSpecies.Spiders,Position=new(510,310),Activity=BugActivity.Nesting,State=BugState.Crossing,Speed=70,Decision=10000,Scale=1};manager.Bugs.Add(bug);
        var web=new EcosystemWeb{Owner=bug.InteractionId,X=600,Y=400};BugManager.EnsureSilk(web);manager.Webs.Add(web);
        Assert(web.Orb && web.Strands.Count==72 && web.Strands.All(s=>s.Progress==0),"Fresh orb contains empty supports/radials/spiral, never a precompleted picture");
        var previous=bug.Position;var maxStep=0d;for(var i=0;i<4000 && web.Construction<.2;i++){manager.Advance(.05);maxStep=Math.Max(maxStep,(bug.Position-previous).Length);previous=bug.Position;bug.Decision=10000;}
        Assert(web.Construction>0 && web.Construction<1 && web.Strands.Any(s=>s.Progress>0),"Physical spider movement gradually lays actual silk progress");
        Assert(maxStep<10,"Spider construction movement does not teleport between attachment points");
        var progress=web.Construction;manager.Bugs.Clear();Step(manager,5);Assert(web.Construction==progress,"A nonexistent spider cannot continue weaving its web");
        RenderSpiders031();
        CaptureSilk031(manager,web,"orb-partial");
        foreach(var strand in web.Strands)strand.Progress=1;web.Construction=1;CaptureSilk031(manager,web,"orb-structure");
    }
    private static void CaptureSilk031(BugManager manager,EcosystemWeb web,string name)
    {
        // The complete-structure fixture is explicitly synthetic; progression is tested above.
        manager.Webs.RemoveAll(w=>w.Owner!=web.Owner);manager.Bugs.Clear();manager.WorldPaused=true;manager.WorldSpawnDelay=1000;
        var overlay=new BugOverlay(manager){Width=900,Height=600};overlay.SetWorldMapping(new Point(),new Size(900,600));overlay.SetViewport(new Rect(0,0,900,600));
        var window=OffscreenHandoff(new Window{Width=920,Height=640,Background=BugVisuals.B("#100D19"),Content=new Grid{Background=BugVisuals.B("#100D19"),Children={overlay}}});window.Show();Pump(TimeSpan.FromMilliseconds(30));Capture031(window,name);window.Close();
    }
    private static void RenderSpiders031()
    {
        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())
        {
            dc.DrawRectangle(BugVisuals.B("#100D19"),null,new Rect(0,0,1200,280));var index=0;
            foreach(var angle in new[]{0d,90,180,-45,135,42})
            {var bug=new BugEntity{Species=BugSpecies.Spiders,Angle=angle,Position=new(0,0),CrawlPhase=index*.6};dc.PushTransform(new TranslateTransform(95+index*200,130));dc.PushTransform(new RotateTransform(angle));dc.PushTransform(new ScaleTransform(4,4));BugVisuals.Draw(dc,bug,new EcosystemOptions{Adorable=true,AdvancedAnimations=true},false,1.3,new(100,30),1);dc.Pop();dc.Pop();dc.Pop();index++;}
        }
        var bitmap=new RenderTargetBitmap(1200,280,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));Directory.CreateDirectory("docs/media/nexa034/continuity031");using var file=File.Create("docs/media/nexa034/continuity031/spider-directions.png");encoder.Save(file);
    }
    private static void CheckPackaged031(string candidate)
    {
        candidate=Path.GetFullPath(candidate);var baseline=Path.GetFullPath("Builds/Testing/nexa029-prerelease/Nexa.exe");
        const string published="BADBDB24FC0345CC6ED00C27CC873BB92068F27194CA23F25452CBA0F57CD9CD";
        Assert(UpdateTransaction.Hash(baseline)==published,"Genuine cached .29 executable matches the live GitHub asset digest");
        var root=Path.Combine(output,"packaged031");Directory.CreateDirectory(root);var target=Path.Combine(root,"Nexa.exe");File.Copy(baseline,target);
        var store=new StateStore(Path.Combine(root,"data","state.json"));var state=new AppState{Notes="Keep actual upgrade data"};store.Save(state);state.Workspace!.AllTimeSquashes=73;store.Save(state);var before=File.ReadAllText(store.FilePath);
        string Prepare()=>UpdateTransaction.Prepare(store,target,candidate,UpdateTransaction.Hash(candidate),App.AppVersion).GetAwaiter().GetResult();
        var failed=Prepare();var rejected=false;try{UpdateTransaction.Apply(failed,_=>Task.FromResult(false),waitForParent:false).GetAwaiter().GetResult();}catch(IOException){rejected=true;}
        Assert(rejected && UpdateTransaction.Hash(target)==published,"Failed .31 startup rolls the genuine .29 executable back byte-for-byte");
        Assert(File.ReadAllText(store.FilePath)==before,"Rollback preserves committed workspace bytes");
        var plan=Prepare();UpdateTransaction.Apply(plan,path=>UpdateTransaction.Health(path,Path.Combine(root,"installed-health")),waitForParent:false).GetAwaiter().GetResult();
        Assert(UpdateTransaction.Hash(target)==UpdateTransaction.Hash(candidate),"Genuine packaged .29 installation upgrades to the verified .31 executable");
        Assert(File.ReadAllText(store.FilePath)==before && store.Load().Workspace!.AllTimeSquashes==73,"Real packaged upgrade preserves notes, permanent scores and workspace bytes");
        Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="Installed","Successful packaged upgrade records its durable installed checkpoint");
        Assert(UpdateTransaction.Hash(baseline)==published,"Approved cached release was read only and remains unchanged");
        CheckUpdateIntegration(candidate);
    }
}
