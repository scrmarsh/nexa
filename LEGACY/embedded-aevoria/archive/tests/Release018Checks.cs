using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static readonly BugSpecies[] Species018=[BugSpecies.Ants,BugSpecies.Roaches,BugSpecies.Flies,BugSpecies.Spiders,BugSpecies.Ladybugs,BugSpecies.Centipedes,BugSpecies.AllBugs];
    private static void CheckRelease018()
    {
        Assert(App.VersionLabel=="Nexa 0.1.18 alpha" && ReleaseNotes.All[0].Title=="Bug Fixes","Current version and update tag are 0.1.18 alpha — Bug Fixes");
        CheckStartupAndMenu018();
        CheckCalm018();
        CheckHabitats018();
    }
    private static void CheckStartupAndMenu018()
    {
        var store=new StateStore(Path.Combine(output,"startup018.json"));
        var note=WidgetLibrary.Create("Notes");
        store.Save(new AppState { WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),note] });
        store.SaveBugPreferences(new(true,BugSpecies.AllThree,BugDensity.Infestation));
        Assert(store.LoadBugPreferences().Species==BugSpecies.AllBugs,"Saved All Three preferences migrate to All Bugs");
        var w=new MainWindow(store) { Width=1280,Height=850,Left=-10000,Top=-10000,ShowActivated=false };
        w.Show(); Pump(TimeSpan.FromMilliseconds(150));
        Assert(w.WidgetCanvas.Children.Count>0 && !w.BugsRunning && w.bugsOverlay?.Manager.Bugs.Count==0,"Dashboard widgets render before bug activity starts");
        var editor=Descendants(w.WidgetCanvas).OfType<TextBox>().First(t=>t.AcceptsReturn);
        editor.AppendText("Startup remains editable.");
        Pump(TimeSpan.FromMilliseconds(2200));
        Assert(!w.BugsRunning && w.bugsOverlay!.Manager.Bugs.Count==0 && editor.Text.Contains("editable"),"Startup delay leaves controls usable for the first few seconds");
        Pump(TimeSpan.FromMilliseconds(1100));
        Assert(w.BugsRunning && w.bugsOverlay!.Manager.Bugs.Count is >0 and <30 && w.BugFrameInterval.TotalMilliseconds<=33,"Bug activity starts after three seconds with a gradual ramp at a 30 FPS target");
        var settings=new SettingsDialog(w,store,w.state,w,"Add Bugs");settings.Show();Pump(TimeSpan.FromMilliseconds(80));
        var choices=Descendants(settings).OfType<RadioButton>().ToArray();
        var species=choices.Where(c=>Species018.Select(s=>s==BugSpecies.AllBugs ? "All Bugs" : s.ToString()).Contains(c.Content?.ToString())).ToArray();
        Assert(species.Select(c=>c.Content?.ToString()).SequenceEqual(new[] { "Ants","Roaches","Flies","Spiders","Ladybugs","Centipedes","All Bugs" }) && !choices.Any(c=>Equals(c.Content,"All Three")),"Add Bugs has exactly the seven requested choices in the requested order");
        foreach(var choice in species)
        {
            choice.IsChecked=true;
            Assert(w.bugsOverlay!.Manager.Preferences.Species==Species018[Array.IndexOf(species,choice)],$"{choice.Content} applies immediately");
        }
        settings.Close();w.SaveNow();
        w.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(60));Assert(!w.BugsRunning,"Minimizing pauses decorative activity");
        w.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(60));Assert(w.BugsRunning,"Restoring resumes without another startup delay");
        w.Close();Assert(store.Load().Widgets.Single(v=>v.Id==note.Id).Text.Contains("editable"),"Startup edits persist independently of bug activity");
    }
    private static void CheckCounterVessels018()
    {
        var old=new WidgetInstance { Kind="Beer Counter",Title="Beer Counter",Value=3,BeerGoal=6 };
        var custom=new WidgetInstance { Kind="Beer Counter",Title="My count",Value=2 };
        var data=new AppState { WidgetLayoutInitialized=true,Widgets=[old,custom] };WidgetLibrary.PrepareLayout(data);
        Assert(old.Kind=="Alcoholic Beverage" && old.Title=="Alcoholic Beverage" && old.Value==3 && old.BeerGoal==6 && custom.Title=="My count","Saved Beer Counters migrate without losing counts, goals or custom titles");
        Assert(WidgetLibrary.Definitions.Count(d=>d.Name=="Alcoholic Beverage")==1 && !WidgetLibrary.Definitions.Any(d=>d.Name=="Beer Counter"),"Widget menu has one Alcoholic Beverage entry");
        var store=new StateStore(Path.Combine(output,"vessels018.json"));store.Save(data);
        var w=new MainWindow(store) { Width=1150,Height=850,Left=-10000,Top=-10000,ShowActivated=false };w.Show();Pump(TimeSpan.FromMilliseconds(100));
        var frame=w.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,old.Id));
        var art=Descendants(frame).OfType<DrinkArtwork>().Single();
        var selector=Descendants(frame).OfType<ComboBox>().Single(c=>Equals(c.ToolTip,"Beverage vessel"));
        Assert(selector.Items.Cast<string>().SequenceEqual(new[] { "Wine Glass","Shot Glass","Beer Bottle" }),"Alcoholic Beverage exposes all three vessel options");
        foreach(var vessel in selector.Items.Cast<string>().ToArray())
        {
            selector.SelectedItem=vessel;Pump(TimeSpan.FromMilliseconds(320));
            Assert(art.Vessel==vessel && Math.Abs(art.FillFraction-.5)<.01,$"{vessel} reflects the counter's existing progress");
        }
        selector.SelectedItem="Wine Glass";w.SaveNow();w.Close();
        Assert(store.Load().Widgets.Single(v=>v.Id==old.Id).BeverageVessel=="Wine Glass","Selected vessel persists with the independent counter");
        var sheet=new Grid { Width=760,Height=510,Background=new SolidColorBrush(Color.FromRgb(15,20,30)) };
        for(var col=0;col<4;col++)sheet.ColumnDefinitions.Add(new ColumnDefinition());
        for(var row=0;row<3;row++)sheet.RowDefinitions.Add(new RowDefinition());
        var vessels=new[] { "Water Bottle","Wine Glass","Shot Glass","Beer Bottle" };
        for(var col=0;col<4;col++)for(var row=0;row<3;row++)
        {
            var a=new DrinkArtwork(col>0,false) { Vessel=vessels[col],FillFraction=row*.5,Width=110,Height=150,VerticalAlignment=VerticalAlignment.Top };
            var cell=new Grid();cell.Children.Add(a);
            cell.Children.Add(new TextBlock { Text=vessels[col]+" "+(row*50)+"%",Foreground=Brushes.White,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Bottom,FontSize=12 });
            Grid.SetColumn(cell,col);Grid.SetRow(cell,row);sheet.Children.Add(cell);
        }
        sheet.Measure(new Size(760,510));sheet.Arrange(new Rect(0,0,760,510));
        Save018(sheet,760,510,"vessel-progress018");
        Assert(DrinkArtwork.WaterBottleShape.Bounds.Height>80 && DrinkArtwork.BottleShape.Bounds.Height/DrinkArtwork.BottleShape.Bounds.Width>2.5,"Water has a full-height bottle silhouette and beer is tall and slim");
    }
    private static void Save018(Visual visual,int width,int height,string name)
    {
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(output,name+".png"));png.Save(stream);
    }
    private static void CheckSharedNest018()
    {
        var m=new BugManager(18);m.Configure(new(true,BugSpecies.Spiders,BugDensity.Low));m.SetSurfaces([],new Size(1000,600));
        var web=new BugWeb { Position=new Point(500,300),Construction=1 };m.Webs.Add(web);
        var spiders=new[] {
            new BugEntity { Species=BugSpecies.Spiders,State=BugState.Paused,Position=new Point(150,200),Speed=90,Decision=100 },
            new BugEntity { Species=BugSpecies.Spiders,State=BugState.Paused,Position=new Point(850,200),Speed=90,Decision=100 }
        };
        var prey=new[] {
            new BugEntity { Species=BugSpecies.Ants,State=BugState.Paused,Position=new Point(480,290),Decision=100 },
            new BugEntity { Species=BugSpecies.Ladybugs,State=BugState.Paused,Position=new Point(530,310),Decision=100 }
        };
        m.Bugs.AddRange(spiders);m.Bugs.AddRange(prey);m.Advance(.08);
        Assert(prey.All(p=>p.State==BugState.Trapped) && web.Captives.Count==2,"The large shared nest captures multiple bug types");
        var positions=prey.Select(p=>p.Position).ToArray();for(var i=0;i<5;i++)m.Advance(.08);
        Assert(spiders.All(s=>s.State==BugState.ApproachingWeb) && spiders.Select(s=>s.Prey).Distinct().Count()==2,"Spiders cooperate and approach separate captives");
        Assert(prey.Select(p=>p.Position).SequenceEqual(positions) && prey.All(p=>!m.Squash(p)),"Captured bugs remain fixed and cannot escape or interrupt feeding");
        var fed=new HashSet<BugEntity>();for(var i=0;i<150;i++) { m.Advance(.08);foreach(var s in spiders)if(s.State==BugState.Feeding)fed.Add(s); }
        Assert(fed.Count==2 && prey.All(p=>!m.Bugs.Contains(p)) && m.Webs.Count==1 && ReferenceEquals(m.Webs[0],web),"Both spiders visibly feed, remove eaten bugs and retain the shared nest");
        var next=new BugEntity { Species=BugSpecies.Roaches,State=BugState.Paused,Position=web.Position,Decision=100 };m.Bugs.Add(next);
        for(var i=0;i<200;i++)m.Advance(.08);
        Assert(!m.Bugs.Contains(next) && m.Webs.Count==1,"The same nest supports repeated capture and feeding");
        var build=new BugManager(81);build.Configure(new(true,BugSpecies.Spiders,BugDensity.Normal));build.SetSurfaces([],new Size(1000,600));
        for(var i=0;i<600;i++)build.Advance(.08);
        Assert(build.Webs.Count==1 && build.Webs[0].Construction==1,"Spiders naturally build and maintain one large shared nest");
        build.Configure(new(false));Assert(build.Webs.Count==0 && build.Bugs.Count==0,"Disabling clears the nest and all runtime state");
    }
    private static void CheckGrowthVariants018()
    {
        foreach(var species in Species018.Where(s=>s!=BugSpecies.AllBugs))
        {
            var m=new BugManager(18);m.Configure(new(true,species,BugDensity.Low));m.SetSurfaces([],new Size(1000,600));
            var b=new BugEntity { Species=species,State=BugState.Paused,Position=new Point(200,200),Decision=1000 };m.Bugs.Add(b);
            var start=b.SizeScale;for(var i=0;i<750;i++)m.Advance(.08);
            Assert(b.SizeScale>start && b.SizeScale<1.65,$"{species} grow gradually with bounded, proportional scaling");
        }
        var titles=new HashSet<string>();var overlay=new BugOverlay { UseGpuSprites=false };
        var examples=new[] { (BugSpecies.Roaches,BugVariant.QueenRoach),(BugSpecies.Flies,BugVariant.QueenFly),(BugSpecies.Ladybugs,BugVariant.QueenLadybug),(BugSpecies.Spiders,BugVariant.MamaSpider),(BugSpecies.Centipedes,BugVariant.KingCentipede) };
        foreach(var (species,variant) in examples)
        {
            var ordinary=new BugEntity { Species=species,Age=180 };
            var special=new BugEntity { Species=species,Variant=variant,Position=new Point(100+titles.Count*175,150),Age=180 };
            titles.Add(special.SpecialTitle!);overlay.Manager.Bugs.Add(special);
            Assert(special.SizeScale>ordinary.SizeScale*2 && !ReferenceEquals(BugArtwork.Source(species,0,false,variant).Image,BugArtwork.Source(species,0,false).Image),$"{special.SpecialTitle} stays larger than grown ordinary bugs and has distinct markings");
        }
        Assert(titles.SetEquals(new[] { "Queen Roach","Queen Fly","Queen Ladybug","Mama Spider","King Centipede" }) && overlay.Manager.Bugs.Last().SizeScale>5,"All five titles are exact and King Centipede is substantially larger");
        overlay.Measure(new Size(950,300));overlay.Arrange(new Rect(0,0,950,300));overlay.RefreshFrame();Save018(overlay,950,300,"special-titles018");
        var king=overlay.Manager.Bugs.Last();Assert(overlay.Manager.Hit(king.Position+new Vector(60,0))==king,"Larger bug hitboxes grow with their visible bodies");
        var found=new HashSet<BugVariant>();
        for(var seed=0;seed<40;seed++)
        {
            var m=new BugManager(seed);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Infestation));m.SetSurfaces([],new Size(1280,720));
            for(var i=0;i<900;i++) { m.Advance(.08);foreach(var b in m.Bugs)if(b.Variant!=BugVariant.None)found.Add(b.Variant); }
            if(found.Count==5)break;
        }
        Assert(found.Count==5,"All five rare variants occur naturally in mixed activity");
    }
    private static void CheckBugInteractions018()
    {
        var events=0;var sizes=new HashSet<int>();
        for(var seed=0;seed<2000;seed++)
        {
            var m=new BugManager(seed);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Low));m.SetSurfaces([],new Size(1000,600));
            var lady=new BugEntity { Species=BugSpecies.Ladybugs,State=BugState.Crossing,Position=new Point(400,300) };m.Bugs.Add(lady);
            Assert018(!m.Squash(lady) && lady.Hits==1 && m.LastBurstCount==0,"First ladybug hit must not spawn.");
            Assert018(m.Squash(lady) && !m.Squash(lady),"Second hit squashes only once.");
            if(m.LastBurstCount>0)
            {
                events++;sizes.Add(m.LastBurstCount);
                Assert018(m.Bugs.Where(b=>b!=lady).All(b=>b.State==BugState.Bursting && b.Position==lady.Position),"Burst must originate at squash.");
            }
        }
        Assert(events is >440 and <560 && sizes.Count==13,"Ladybugs preserve two-hit feedback and the inclusive 4–16 infestation burst at 25%");
        var slow=BugTestManager(18,BugSpecies.AllBugs,BugDensity.Arcade);var fast=BugTestManager(18,BugSpecies.AllBugs,BugDensity.Arcade);
        for(var i=0;i<1200;i++)
        {
            slow.Advance(.033);fast.Advance(.033);
            if(i%5==0 && fast.Bugs.FirstOrDefault(b=>b.State is not (BugState.Squashed or BugState.Trapped or BugState.Hiding)) is { } b)fast.Squash(b);
        }
        Assert(fast.ArcadeInfestation && fast.ArcadeProgress>slow.ArcadeProgress && fast.Cap==600,"Aggressive clicking still escalates Arcade automatically to infestation");
        Assert(fast.Bugs.Count<=BugManager.MaximumPopulation && fast.Webs.Count<=1,"Chained bursts and the shared nest stay bounded");
    }
    private static void Assert018(bool condition,string message) { if(!condition)throw new Exception(message); }
    private static void BenchmarkMatrix018()
    {
        var overlay=new BugOverlay { UseGpuSprites=false };var host=new Grid { Background=Brushes.Black };host.Children.Add(overlay);
        var w=new Window { Content=host,Width=1280,Height=720,Left=-10000,Top=-10000,ShowActivated=false };w.Show();Pump(TimeSpan.FromMilliseconds(60));
        var bitmap=new RenderTargetBitmap(1280,720,96,96,PixelFormats.Pbgra32);
        var report=new List<object>();
        foreach(var species in Species018)foreach(var density in Enum.GetValues<BugDensity>())
        {
            var m=overlay.Manager;m.Configure(new(true,species,density));m.SetSurfaces([],new Size(1280,720));
            if(density==BugDensity.Arcade)for(var i=0;i<2400;i++)m.Advance(.08);
            for(var i=0;i<900;i++)m.Advance(.08);
            // Include full, mature populations to avoid a slow spawn rate hiding render cost.
            while(m.Bugs.Count<m.Cap)
            {
                var individual=species==BugSpecies.AllBugs ? Species018[m.Bugs.Count%6] : species;
                m.Bugs.Add(new BugEntity { Species=individual,State=BugState.Crossing,Position=new Point(40+m.Bugs.Count*47%1180,40+m.Bugs.Count*31%610),Target=new Point(600,350),Decision=10,Speed=30,Age=180 });
            }
            foreach(var b in m.Bugs)b.Age=180;
            var cpu=new List<double>();var render=new List<double>();
            for(var i=0;i<50;i++)
            {
                var start=Stopwatch.GetTimestamp();m.Advance(1d/30);cpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
                start=Stopwatch.GetTimestamp();overlay.RefreshFrame();overlay.UpdateLayout();bitmap.Clear();bitmap.Render(host);
                if(i>=5)render.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
            var c95=Percentile(cpu,.95);var r95=Percentile(render,.95);
            Assert(c95+r95<33.333,$"{species}/{density}: mature population {m.Bugs.Count}, CPU p95 {c95:F2} ms, full software render p95 {r95:F2} ms meets 30 FPS");
            report.Add(new { Species=species.ToString(),Density=density.ToString(),Population=m.Bugs.Count,CpuP95Ms=c95,SoftwareRenderP95Ms=r95,FpsBudget=1000/(c95+r95) });
        }
        var manager=overlay.Manager;manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Infestation));manager.SetSurfaces([],new Size(1280,720));
        for(var i=0;i<768;i++)manager.Bugs.Add(new BugEntity { Species=Species018[i%6],Age=180,State=BugState.Crossing,Position=new Point(40+i*47%1180,40+i*31%610),Target=new Point(600,350),Decision=10,Speed=30 });
        var latency=new List<double>();var timer=new DispatcherTimer(DispatcherPriority.Background) { Interval=TimeSpan.FromMilliseconds(32) };
        timer.Tick+=(_,_)=> { var start=Stopwatch.GetTimestamp();manager.Advance(1d/30);overlay.RefreshFrame();w.Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=>latency.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds))); };
        timer.Start();Pump(TimeSpan.FromSeconds(1.2));timer.Stop();
        Assert(latency.Count>=20 && Percentile(latency,.95)<33.333,$"Input remains responsive at the 768-bug burst safety limit (p95 {Percentile(latency,.95):F2} ms)");
        var gpuTimes=new List<double>();overlay.UseGpuSprites=true;
        for(var i=0;i<40;i++) { var start=Stopwatch.GetTimestamp();overlay.RefreshFrame();overlay.UpdateLayout();bitmap.Clear();bitmap.Render(host);if(i>=5)gpuTimes.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
        Console.WriteLine($"GPU evaluation: WPF tier {RenderCapability.Tier>>16}; sprite path under forced software capture p95 {Percentile(gpuTimes,.95):F2} ms.");
        overlay.UseGpuSprites=false;overlay.SetPerformancePressure(true);overlay.RefreshFrame();
        Assert(overlay.ResolutionScale==.45,"Performance pressure reduces decorative resolution while retaining the animation target");
        File.WriteAllText(Path.Combine(output,"performance018.json"),System.Text.Json.JsonSerializer.Serialize(new { WpfRenderTier=RenderCapability.Tier>>16,Viewport="1280x720",Matrix=report,InputP95Ms=Percentile(latency,.95),GpuSpriteSoftwareCaptureP95Ms=Percentile(gpuTimes,.95) },new System.Text.Json.JsonSerializerOptions { WriteIndented=true }));
        w.Close();
    }
    private static void CheckLiveCadence018()
    {
        var store=new StateStore(Path.Combine(output,"live018.json"));var note=WidgetLibrary.Create("Notes");
        store.Save(new AppState { WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),note] });
        store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Infestation));
        var w=new MainWindow(store) { Width=1280,Height=850,Left=-10000,Top=-10000,ShowActivated=false };w.Show();Pump(TimeSpan.FromMilliseconds(3300));
        var overlay=w.bugsOverlay!;var m=overlay.Manager;
        while(m.Bugs.Count<600)m.Bugs.Add(new BugEntity { Species=Species018[m.Bugs.Count%6],Age=180,State=BugState.Crossing,Position=new Point(80+m.Bugs.Count*47%1100,80+m.Bugs.Count*31%500),Target=new Point(600,300),Speed=30,Decision=5 });
        var editor=Descendants(w.WidgetCanvas).OfType<TextBox>().First(t=>t.AcceptsReturn);
        var liveReport=new List<object>();
        foreach(var gpu in new[] { false,true })
        {
            overlay.UseGpuSprites=gpu;Pump(TimeSpan.FromMilliseconds(250));
            var frames=overlay.RenderedFrames;var began=Stopwatch.GetTimestamp();
            var timer=new DispatcherTimer(DispatcherPriority.Input) { Interval=TimeSpan.FromMilliseconds(60) };
            timer.Tick+=(_,_)=>editor.AppendText("x");timer.Start();Pump(TimeSpan.FromSeconds(2.4));timer.Stop();
            var elapsed=Stopwatch.GetElapsedTime(began).TotalSeconds;var fps=(overlay.RenderedFrames-frames)/elapsed;
            Assert(fps>=30,$"Live dashboard {(gpu ? "GPU sprites" : "software bitmap")} reaches {fps:F1} rendered FPS with 600 mature bugs while controls accept edits");
            liveReport.Add(new { Backend=gpu ? "GPU sprites" : "Software bitmap",RenderedFps=fps,WorkMs=w.BugUpdateMilliseconds });
        }
        w.SaveNow();w.Close();Assert(store.Load().Widgets.Single(v=>v.Id==note.Id).Text.Length>=50,"Live high-count animation preserves responsive note edits and saves");
        File.WriteAllText(Path.Combine(output,"live-cadence018.json"),System.Text.Json.JsonSerializer.Serialize(liveReport,new System.Text.Json.JsonSerializerOptions { WriteIndented=true }));
    }
}
