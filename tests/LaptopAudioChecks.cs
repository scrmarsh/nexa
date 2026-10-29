using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)] private struct ScreenRectCheck { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorCheck { public int Size; public ScreenRectCheck Monitor, Work; public uint Flags; }
    [DllImport("user32.dll", EntryPoint="GetWindowRect")] private static extern bool GetWindowRectCheck(nint hwnd, out ScreenRectCheck rect);
    [DllImport("user32.dll", EntryPoint="MonitorFromWindow")] private static extern nint MonitorCheckFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll", EntryPoint="GetMonitorInfoW")] private static extern bool GetMonitorCheck(nint monitor, ref MonitorCheck info);

    private static void CheckLaptopAudio()
    {
        CheckLaptopBounds(); CheckFatalSwarmShot(); CheckEffectsAudio(); CheckBatchedKills(); CheckStationaryHome();
    }
    private static void CheckLaptopBounds()
    {
        foreach(var work in new[]{new Rect(0,0,1366,728),new Rect(0,40,1280,680),new Rect(-1920,0,1920,1040),new Rect(0,-900,1600,860),new Rect(0,0,640,440)})
        {
            var result=NexaWindowBounds.FitToWorkArea(new Rect(work.X-900,work.Y-1200,4000,3000),work);
            Assert(work.Contains(result) && result.Top>=work.Top && result.Width<=work.Width && result.Height<=work.Height,"Oversized/offscreen startup fits the target work area: "+work);
        }
        var unchanged=new Rect(-1800,100,800,600);Assert(NexaWindowBounds.FitToWorkArea(unchanged,new Rect(-1920,0,1920,1040))==unchanged,"A reachable window on a negative-origin monitor keeps its position");
        var window=new NexaDialog{Title="Screen safety fixture",Width=9000,Height=7000,MinWidth=5000,MinHeight=4000,WindowStartupLocation=WindowStartupLocation.Manual,Left=-400,Top=-900,Content=new TextBlock{Text="Reachable caption"}};
        NexaWindowBounds.RequestInitialPlacement(window,true);
        window.Show();Pump(TimeSpan.FromMilliseconds(150));
        var hwnd=new WindowInteropHelper(window).Handle;var monitor=new MonitorCheck{Size=Marshal.SizeOf<MonitorCheck>()};
        Assert(GetWindowRectCheck(hwnd,out var rect) && GetMonitorCheck(MonitorCheckFromWindow(hwnd,2),ref monitor),"Read actual native monitor/window rectangles");
        Assert(rect.Left>=monitor.Work.Left && rect.Top>=monitor.Work.Top && rect.Right<=monitor.Work.Right+1 && rect.Bottom<=monitor.Work.Bottom+1,"Actual oversized native window opens wholly inside its monitor work area");
        Assert(window.MinWidth<=window.MaxWidth && window.MinHeight<=window.MaxHeight,"Small displays override oversized minimums so the caption stays reachable");window.Close();
    }
    private static void AdvanceSafe(PixelSwarmGame game,double seconds)
    {
        for(var t=0d;t<seconds;t+=.05){game.Pixels.Clear();game.Advance(.05,new Vector(),game.Player);}
    }
    private static void CheckFatalSwarmShot()
    {
        var cues=new List<NexaSoundEffect>();var game=new PixelSwarmGame(19){Sound=cues.Add};game.Resize(new Size(640,480));game.Start();AdvanceSafe(game,2.7);
        for(var hit=0;hit<3;hit++)
        {
            game.Shots.Clear();game.Shots.Add(new(){Position=game.Player,Previous=game.Player,Hostile=true});
            game.Advance(.01,new Vector(),game.Player);
            if(hit<2)AdvanceSafe(game,1.3);
        }
        Assert(game.GameOver && game.Health==0 && game.Shots.Count==0,"Fatal hostile shot clears bullets without removing from an already-cleared list or locking the game");
        Assert(cues.Count(c=>c==NexaSoundEffect.Death)==1,"Death emits exactly one effect");
        var time=game.Survived;game.Advance(100,new Vector(),game.Player);Assert(game.Survived==time,"Game-over simulation stays stopped");
        game.Start();game.Pixels.Clear();game.Fire(new Point(300,0));game.Fire(new Point(300,0));Assert(cues.Count(c=>c==NexaSoundEffect.Shoot)==1,"Successful fire emits a shot cue, cooldown attempts do not");
        game.Pickups.Add(new(){Position=game.Player,Kind=SwarmPickupKind.Shield});game.Advance(.01,new Vector(),game.Player);Assert(game.Collected==1 && cues.Contains(NexaSoundEffect.PowerUp),"Collecting a real pickup applies it and emits its effect");
        var original=game.Bounds;game.Resize(new Size(double.PositiveInfinity,480));Assert(game.Bounds==original,"Invalid render size cannot poison the game geometry");
        time=game.Survived;game.Advance(double.NaN,new Vector(),game.Player);game.Advance(double.PositiveInfinity,new Vector(),game.Player);Assert(game.Survived==time,"Non-finite frame durations cannot enter the simulation loop");
        game.Advance(.05,new Vector(double.NaN,0),new Point(double.NaN,double.PositiveInfinity));Assert(double.IsFinite(game.Player.X) && double.IsFinite(game.Player.Y) && double.IsFinite(game.AimAngle),"Malformed movement and pointer input cannot reach rendering as NaN");
        var random=new Random(21);
        for(var run=0;run<40;run++)
        {
            game.Resize(new Size(20+random.Next(1500),20+random.Next(900)));game.Start();
            for(var frame=0;frame<600;frame++)
            {
                if(game.Phase==SwarmPhase.LevelUp)game.ChooseUpgrade(0);
                game.Fire(new Point(random.NextDouble()*game.Bounds.Width,random.NextDouble()*game.Bounds.Height));
                game.Advance(.03,new Vector(random.Next(-1,2),random.Next(-1,2)),game.Player,true);
                if(game.GameOver)break;
            }
            Assert(game.Shots.Count<=SwarmBalance.ProjectileCap && game.Particles.Count<=SwarmBalance.ParticleCap && game.Pixels.Count<=SwarmBalance.EnemyCap(game.Stage),"Repeated game run respects simulation caps: "+run);
        }
    }
    private static void CheckEffectsAudio()
    {
        var hashes=new HashSet<string>();
        foreach(var effect in Enum.GetValues<NexaSoundEffect>())
        {
            using var stream=NexaEffectsAudio.Create(effect,.5);var bytes=stream.ToArray();
            Assert(System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF" && BitConverter.ToInt32(bytes,40)==bytes.Length-44 && bytes.Length<30000,"Bounded valid PCM WAV: "+effect);
            hashes.Add(Convert.ToHexString(SHA256.HashData(bytes)));File.WriteAllBytes(Path.Combine(output,effect+".wav"),bytes);
            using var silent=NexaEffectsAudio.Create(effect,0);Assert(silent.ToArray().Skip(44).All(b=>b==0),"Zero-volume cue is silent: "+effect);
        }
        Assert(hashes.Count==9,"All nine game, crawl and Halloween effects have distinct waveforms");
        var state=new AppState{InterfaceSoundsEnabled=true,InterfaceSoundVolume=.4};var options=new NexaOptions{SeasonalMode="On",SeasonalSounds=true};
        Assert(Enum.GetValues<NexaSoundEffect>().All(e=>NexaEffectsAudio.Allowed(e,state,options,true)),"Enabled effects are eligible in an active seasonal view");
        Assert(Enum.GetValues<NexaSoundEffect>().All(e=>!NexaEffectsAudio.Allowed(e,state,options,false)),"Hidden, inactive and minimized views remain silent");
        state.InterfaceSoundsEnabled=false;Assert(Enum.GetValues<NexaSoundEffect>().All(e=>!NexaEffectsAudio.Allowed(e,state,options,true)),"Master sound preference gates every effect");state.InterfaceSoundsEnabled=true;
        state.GameSoundsEnabled=false;state.BugSoundsEnabled=false;Assert(!NexaEffectsAudio.Allowed(NexaSoundEffect.Shoot,state,options,true) && !NexaEffectsAudio.Allowed(NexaSoundEffect.Crawl,state,options,true),"Independent game/crawl switches silence their categories");
        options.SeasonalMode="Off";Assert(!NexaEffectsAudio.Allowed(NexaSoundEffect.Ghost,state,options,true),"Halloween effects stop outside nexaween");
        var store=new StateStore(Path.Combine(output,"sound-settings.json"));store.SaveInteractionPreferences(state);var loaded=new AppState();store.LoadInteractionPreferences(loaded);Assert(!loaded.GameSoundsEnabled && !loaded.BugSoundsEnabled && loaded.InterfaceSoundsEnabled,"New sound preferences round-trip in the existing per-user preference flow");
    }
    private static void CheckBatchedKills()
    {
        var store=new StateStore(Path.Combine(output,"batch-kills.json"));var main=new MainWindow(store);main.Show();Pump(TimeSpan.FromMilliseconds(850));main.state.Workspace!.Settings.ReduceMotion=true;
        main.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions()));var world=NexaEcosystem.For(store);world.Manager.WorldPaused=true;main.SaveNow();world.Save();
        var before=File.ReadAllBytes(store.FilePath);var worldBefore=File.ReadAllBytes(world.FilePath);var count=main.state.Workspace.AllTimeSquashes;
        var bugs=Enumerable.Range(0,20).Select(_=>new BugEntity{Species=BugSpecies.Ants}).ToArray();
        foreach(var bug in bugs)world.RecordSquashResponsive(bug,main).GetAwaiter().GetResult();Pump(TimeSpan.FromMilliseconds(900));
        Assert(main.state.Workspace.AllTimeSquashes==count+20 && main.HasUnsavedChanges,"Rapid kills credit immediately and mark the workspace dirty");
        Assert(File.ReadAllBytes(store.FilePath).SequenceEqual(before) && File.ReadAllBytes(world.FilePath).SequenceEqual(worldBefore),"Killing bugs never triggers per-hit workspace autosaves or ecosystem writes");
        world.RecordSquashResponsive(bugs[0],main).GetAwaiter().GetResult();Assert(main.state.Workspace.AllTimeSquashes==count+20,"An unsaved repeated kill remains deduplicated");
        var save=main.SaveInBackground();WaitHandoff(()=>save.IsCompleted,"Normal autosave persists the kill batch");save.GetAwaiter().GetResult();
        Assert(store.Load().Workspace!.AllTimeSquashes==count+20 && world.PendingSquashCount==0,"Ordinary autosave persists every award once and acknowledges only saved events");
        Pump(TimeSpan.FromMilliseconds(180));
        var extra=new BugEntity{Species=BugSpecies.Spiders};world.RecordSquashResponsive(extra,main).GetAwaiter().GetResult();world.Save();
        using(var file=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))Assert(!main.SaveNow() && world.PendingSquashCount==1,"Failed batch save retains its pending event for retry");
        world.RetryHistory();Assert(store.Load().Workspace!.AllTimeSquashes==count+21 && world.PendingSquashCount==0,"Explicit recovery retries the batch without duplicate credit");main.Close();
    }
    private static void CheckStationaryHome()
    {
        var store=new StateStore(Path.Combine(output,"stationary-home.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="On";store.Save(state);
        var home=new HomeWindow(store){Width=1000,Height=740};home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var logo=Descendants(home).OfType<NexaWordmark>().First();var tagline=Descendants(home).OfType<TextBlock>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Home tagline");
        var location=logo.TransformToAncestor(home).Transform(new Point());var width=home.ActualWidth;var left=location.X;var top=location.Y;var tagLeft=tagline.TransformToAncestor(home).Transform(new Point()).X;
        foreach(var phrase in new[]{"a","a space for whatever haunts you next",""})
        {
            NexaBranding.ApplyTagline(tagline,phrase);home.UpdateLayout();var position=logo.TransformToAncestor(home).Transform(new Point());
            Assert(Math.Abs(position.X-left)<.1 && Math.Abs(position.Y-top)<.1 && home.ActualWidth==width,"Logo stays fixed through tagline type/fade/reset layout: "+phrase.Length);
        }
        Assert(tagLeft>left+logo.ActualWidth*.8,"Tagline occupies the right-hand header region");
        NexaBranding.ApplyTagline(tagline,"a space for whatever haunts you next");home.UpdateLayout();
        var root=(FrameworkElement)home.Content;var bitmap=new RenderTargetBitmap((int)root.ActualWidth,(int)root.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(root);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(output,"stationary-home.png")))png.Save(file);home.Close();
    }
}
