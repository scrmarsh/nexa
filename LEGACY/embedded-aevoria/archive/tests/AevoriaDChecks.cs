using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;
using Aevoria.Desktop;
internal static partial class Program
{
    private static void CheckDCameraLag()
    {
        var world=GrandWait(new AevoriaLibrary(Path.Combine(output,"camera")).Create("Camera lag probe","living-landscape",3072,1728,new(){PopulationBudget=120}));world.Controller.SetPaused(true);
        var overlay=new BugOverlay(world.Controller.Manager){ShowHud=false,StrategicMap=true};overlay.SetWidgetSurfaces([]);overlay.TerrainView=new LivingTerrainView(world,overlay);
        var host=new Grid{Background=Brushes.Black};host.Children.Add(overlay);var window=new Window{Content=host,Width=1200,Height=800,WindowStyle=WindowStyle.None,ShowActivated=false,Left=-10000,Top=-10000};window.Show();Pump(TimeSpan.FromMilliseconds(500));
        var target=new RenderTargetBitmap(1200,800,96,96,PixelFormats.Pbgra32);var rows=new List<object>();var submission=new List<double>();var captures=new List<double>();
        for(var step=0;step<32;step++)
        {
            var zoom=step<16?.65:1.7;var center=new Point(world.Info.Extent*(.1+.8*(step%8)/7),world.Info.HeightExtent*(.15+.7*(step/8)/3));
            var before=overlay.RenderedFrames;var watch=Stopwatch.StartNew();overlay.Camera=new Matrix(zoom,0,0,zoom,600-center.X*zoom,400-center.Y*zoom);overlay.RefreshFrame();overlay.UpdateLayout();target.Clear();target.Render(host);captures.Add(watch.Elapsed.TotalMilliseconds);submission.Add(overlay.LastRenderMilliseconds);
            Assert(overlay.RenderedFrames>before,"Cold camera change submits new drawing "+step);
            rows.Add(new{step,zoom,SubmissionMs=overlay.LastRenderMilliseconds,CaptureMs=captures[^1],Ready=overlay.TerrainView.Landscape?.Ready});Pump(TimeSpan.FromMilliseconds(16));
        }
        Until023(()=>overlay.TerrainView.Landscape is {Ready:true},"Camera detail settles after input stops");
        File.WriteAllText(Path.Combine(output,"camera-lag.json"),JsonSerializer.Serialize(new{SubmissionP95Ms=Percentile(submission,.95),SubmissionMaxMs=submission.Max(),CaptureP95Ms=Percentile(captures,.95),Rows=rows},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Camera submission p95={Percentile(submission,.95):F2}ms, max={submission.Max():F2}ms; capture p95={Percentile(captures,.95):F2}ms; artifacts: {output}");overlay.TerrainView.Dispose();window.Close();
    }
    private static void CheckDLive(bool baseline)
    {
        var rows=new List<object>();
        foreach(var size in GrandGeneration.Sizes.Where(s=>s.Width is 1024 or 6144))
        {
            var watch=Stopwatch.StartNew();var library=new AevoriaLibrary(Path.Combine(output,"live-"+size.Width));var world=GrandWait(library.Create("D foreground verification","living-landscape",size.Width,size.Height,new(){PopulationBudget=120}));var menu=new GameMenu(library);menu.Show();menu.Play(world.Info.WorldId);var board=menu.Game!;board.WindowState=WindowState.Normal;board.Width=1200;board.Height=800;board.Activate();Pump(TimeSpan.FromMilliseconds(200));board.Width+=1200-board.Battlefield.ActualWidth;board.Height+=800-board.Battlefield.ActualHeight;Pump(TimeSpan.FromMilliseconds(600));var first=watch.Elapsed.TotalMilliseconds;
            foreach(var lod in new[]{"World","Regional","Local","Inspection"})
            {
                board.FitWorld();if(lod!="World"){var fit=Math.Min(board.Battlefield.ActualWidth/world.Info.Extent,board.Battlefield.ActualHeight/world.Info.HeightExtent);var target=lod switch{"Regional"=>Math.Max(.3,fit*1.5),"Local"=>Math.Max(.55,fit*2.5),_=>Math.Max(1.8,fit*4.5)};board.ZoomAt(new Point(board.Battlefield.ActualWidth/2,board.Battlefield.ActualHeight/2),(int)(Math.Log(target/board.Battlefield.Camera.M11)/Math.Log(1.12)*120));}Pump(TimeSpan.FromMilliseconds(350));
                var callbacks=0;EventHandler rendering=(_,_)=>callbacks++;CompositionTarget.Rendering+=rendering;var before=world.Controller.Manager.MatchAge;var timer=Stopwatch.StartNew();Pump(TimeSpan.FromSeconds(2));CompositionTarget.Rendering-=rendering;
                rows.Add(new{Baseline=baseline,size.Width,Lod=lod,ActualLod=board.Battlefield.MapDetail.ToString(),CallbacksPerSecond=callbacks/timer.Elapsed.TotalSeconds,SimulatedSeconds=world.Controller.Manager.MatchAge-before,StartupToFirstWindowMs=first,ViewportWidth=board.Battlefield.ActualWidth,ViewportHeight=board.Battlefield.ActualHeight,Dpi=VisualTreeHelper.GetDpi(board).DpiScaleX,RenderTier=RenderCapability.Tier>>16,WorkingSetBytes=Process.GetCurrentProcess().WorkingSet64});
                Assert(callbacks>0 && world.Controller.Manager.MatchAge>before,"D foreground compositor and simulation advance: "+size.Name+" "+lod);CaptureB(board,"foreground-"+size.Width+"-"+lod,1);
            }
            world.Controller.SetPaused(true);File.WriteAllText(Path.Combine(output,"foreground-metrics.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine("test-output","aevoria-d",baseline?"foreground-before-output.txt":"foreground-output.txt"),output);
            // Leave the Standard window briefly available for direct keyboard/UI verification.
            if(!baseline && size.Width==1024)HoldDLive(board,world);board.Close();menu.Close();
        }
    }
    private static void HoldDLive(BattlefieldWindow board,LivingWorldSession world)
    {
        world.Controller.Manager.WorldPaused=false;for(var i=0;i<7500;i++)CStep(world);world.Controller.SetPaused(true);
        board.WindowState=WindowState.Maximized;board.SelectSettlement(world.Civilization!.State.Settlements.OrderByDescending(s=>world.Civilization.Economy!.Settlement(s.Id)?.Score??0).First(s=>s.Active).Id);Pump(TimeSpan.FromMilliseconds(500));board.OpenGameMenu();
        File.WriteAllText(Path.Combine("test-output","aevoria-d","foreground-ready.txt"),output);Pump(TimeSpan.FromSeconds(90));CaptureB(board,"foreground-economy",1);
    }
    private static void CheckAevoriaD()
    {
        var atomic=Path.Combine(output,"atomic-contention.txt");File.WriteAllText(atomic,"old");var reader=new FileStream(atomic,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);var release=Task.Run(()=>{Thread.Sleep(50);reader.Dispose();});StateStore.AtomicWrite(atomic,"new",false);release.GetAwaiter().GetResult();Assert(File.ReadAllText(atomic)=="new","D save replacement retries brief sharing contention");
        using(var held=new FileStream(atomic,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)){var denied=false;try{StateStore.AtomicWrite(atomic,"discard",false);}catch(IOException){denied=true;}Assert(denied && File.ReadAllText(atomic)=="new","D save replacement remains bounded and preserves original on persistent sharing contention");}
        var history=DevelopmentChronicle.Data;
        Assert(history.Milestones.Select(m=>m.Version).SequenceEqual(new[]{"0.0.1","0.0.2","0.0.3","0.0.4"}),"D01 numeric milestone versions preserve sequence");
        Assert(AevoriaBuild.Version==typeof(GameMenu).Assembly.GetName().Version!.ToString(3),"D02 current version derives from assembly metadata");
        Assert(AevoriaBuild.Label==AevoriaBuild.Version+" Alpha","D03 public version has no milestone-letter suffix");
        var library=new AevoriaLibrary(Path.Combine(output,"d-library"));var menu=new GameMenu(library);menu.Show();
        var chronicle=Descendants(menu).OfType<Button>().Single(b=>Equals(b.Content,"What's New / Changelog"));chronicle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Descendants(menu).OfType<TextBlock>().Any(t=>t.Text=="Development Chronicle"),"D04 actual main menu opens Chronicle");
        Assert(history.OriginalEntries.Length>=26 && history.SourceDocuments.Length>=12 && history.Eras.Length==5,"D05 all five historical eras and full source reports preserved");
        Assert(history.Milestones.Where(m=>m.Retrospective).All(m=>m.OriginalExecutableVersion=="0.0.1") && history.Milestones.All(m=>!m.Verified || DateOnly.Parse(m.VerifiedOn!)<=DateOnly.FromDateTime(DateTime.Now)),"D06 retrospective labels and completion dates are truthful");
        Assert(typeof(GameMenu).Assembly.GetManifestResourceStream("Aevoria.DevelopmentChronicle")!=null,"D07 complete Chronicle embedded offline");
        Assert(App.AppVersion=="0.1.23","D08 Nexa version unchanged");menu.Close();
        var world=GrandWait(library.Create("D regression","living-landscape",1024,576,new(){PopulationBudget=120}));world.Controller.SetPaused(true);var m=world.Controller.Manager;var civic=world.Civilization!;var economy=civic.Economy!;
        var board=new BattlefieldWindow(world.Controller,world,strategic:true);board.Show();Pump(TimeSpan.FromMilliseconds(180));
        Assert(!Descendants(board).OfType<TextBlock>().Any(t=>t.Text=="NEXA" || t.Text=="Nexa"),"D09 no Nexa gameplay caption/logo");
        Assert(board.WindowStyle==WindowStyle.None && Math.Abs(board.Battlefield.ActualWidth-board.ActualWidth)<2,"D10 borderless viewport fills game window");
        var available=AevoriaWorkArea.AvailablePixels(board);var scale=VisualTreeHelper.GetDpi(board);Assert(!available.IsEmpty && board.ActualHeight*scale.DpiScaleY<=available.Height+2 && board.ActualWidth*scale.DpiScaleX<=available.Width+2,"D10 maximized game respects the monitor work area and taskbar");
        board.OpenGameMenu();Assert(board.GameMenuOpen && m.WorldPaused,"D11 pause menu pauses simulation");board.CloseGameMenu();Assert(m.WorldPaused,"D11 resume preserves pre-menu pause");
        board.WindowState=WindowState.Normal;board.Width=1200;board.Height=800;Pump(TimeSpan.FromMilliseconds(100));board.CenterCamera();board.NavigateTo(new(world.Info.Extent/2,world.Info.HeightExtent/2));var center=board.CameraCenter;
        board.WindowState=WindowState.Maximized;Pump(TimeSpan.FromMilliseconds(100));Assert((center-board.CameraCenter).Length<.01,"D13 maximizing preserves camera center");
        board.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(100));Assert((center-board.CameraCenter).Length<.01,"D14 restoring preserves camera center");
        board.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(100));Assert(!world.Controller.Running,"D12 minimized world does not tick");board.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(100));Assert((center-board.CameraCenter).Length<.01,"D15 reopening minimized window preserves position");
        board.Width=1450;board.Height=650;Pump(TimeSpan.FromMilliseconds(100));Assert((center-board.CameraCenter).Length<.01,"D16 aspect-ratio change preserves center");board.FitWorld();Assert((board.CameraCenter-new Point(world.Info.Extent/2,world.Info.HeightExtent/2)).Length<.01,"D17 rectangular world fits centered");
        board.CenterCamera();board.NavigateTo(new(world.Info.Extent/2,world.Info.HeightExtent/2));var cursor=new Point(500,300);var anchor=board.Battlefield.ScreenToWorld(cursor);var zoom=board.Battlefield.Camera.M11;for(var i=0;i<3;i++)board.SmoothZoomAt(cursor,120);Pump(TimeSpan.FromMilliseconds(300));
        Assert((anchor-board.Battlefield.ScreenToWorld(cursor)).Length<.1,"D18 cursor world anchor retained during smooth zoom");Assert(Math.Abs(board.Battlefield.Camera.M11-zoom*Math.Pow(1.12,9))<.001,"D19 rapid wheel events accumulate sensitivity-adjusted targets");
        library.SavePreferences(new(ZoomSensitivity:2.5,ShowMinimap:false));Assert(library.Preferences().ZoomSensitivity==2.5 && !library.Preferences().ShowMinimap,"D20 sensitivity and minimap preferences persist");
        var landscape=board.Battlefield.TerrainView!.Landscape!;var decorations=Enumerable.Range(0,3000).Select(i=>landscape.Decoration(i%100,i/100)).Where(d=>d!=null).Select(d=>d!.Value).ToArray();
        Assert(decorations.Length>50 && Enumerable.Range(0,3000).All(i=>landscape.Decoration(i%100,i/100)==landscape.Decoration(i%100,i/100)),"D21 decoration is deterministic");
        Assert(decorations.All(d=>d.Biome is not (TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.River or TerrainBiome.Lake or TerrainBiome.Beach)),"D22 vegetation excludes water and beaches");
        Assert(Enumerable.Range(0,100).Any(i=>landscape.ReliefColor(i*7,200)!=world.Terrain.ColorAt(i*7,200)),"D23 elevation-based relief changes presentation colors");
        Assert(Descendants(board.Battlefield).Count()<10,"D25 trees are batched drawings, not WPF controls");
        var repeat=new LivingTerrain(world.Info);Assert(TerrainChunks.Generate(world.Terrain,new(2,2)).Colors.SequenceEqual(TerrainChunks.Generate(repeat,new(2,2)).Colors),"D26/D54 visual decoration leaves generator-3 geography deterministic");
        Assert(landscape.CacheBytes<=AevoriaLandscape.Budget,"D27 landscape cache stays bounded");
        Until023(()=>landscape.HasOverview,"D landscape overview arrives from background worker");
        board.Battlefield.RefreshFrame();Pump(TimeSpan.FromMilliseconds(150));
        Assert(world.Chunks.CachedBytes==0 && world.Chunks.PendingCount==0,"D opaque landscape releases covered base terrain work and cache");
        var mini=board.Minimap!;mini.RefreshOverview();Assert(Math.Abs(mini.MapBounds.Width/mini.MapBounds.Height-world.Info.Extent/world.Info.HeightExtent)<.001,"D28/D36 minimap respects rectangular geometry");
        Assert((mini.MapToWorld(mini.WorldToMap(board.CameraCenter))-board.CameraCenter).Length<.001,"D29 minimap camera center roundtrips");Assert(!mini.ViewportRectangle.IsEmpty && mini.MapBounds.Contains(mini.ViewportRectangle),"D30 viewport rectangle clipped to real world");
        var owners=world.Nations!.Capture().Owners.ToArray();var destination=new Point(world.Info.Extent*.55,world.Info.HeightExtent*.55);zoom=board.Battlefield.Camera.M11;mini.Navigate(mini.WorldToMap(destination));Assert((board.CameraCenter-destination).Length<.01 && board.Battlefield.Camera.M11==zoom,"D31 click navigation preserves zoom");
        mini.Navigate(mini.WorldToMap(center));mini.Navigate(mini.WorldToMap(destination));Assert(owners.SequenceEqual(world.Nations.Capture().Owners),"D32 minimap drag navigation never edits ownership");var rect=mini.ViewportRectangle;board.ZoomAt(cursor,120);Assert(mini.ViewportRectangle.Width<rect.Width,"D33 zoom updates minimap viewport");
        board.Width=1100;Pump(TimeSpan.FromMilliseconds(100));Assert((mini.MapToWorld(mini.WorldToMap(board.CameraCenter))-board.CameraCenter).Length<.01,"D34 resize keeps minimap synchronized");var builds=mini.Rebuilds;mini.RefreshOverview();Assert(mini.Rebuilds==builds+1 && mini.CachedBytes<400000,"D35/D37 bounded minimap rebuild and memory");
        var polity=world.Nations.Nations.First();var cell=world.Nations.Regions(polity.Id).SelectMany(world.Nations.Land.Neighbors).First(c=>world.Nations.Owner(c)==null && world.Nations.Expansion!.ConnectedCell(c));builds=mini.Rebuilds;world.Nations.Commit(cell,null,polity.Id,m.MatchAge,OwnershipReason.Intervention);Pump(TimeSpan.FromMilliseconds(350));Assert(mini.Rebuilds>builds,"D35 real political revision schedules minimap refresh");world.Nations.Commit(cell,polity.Id,null,m.MatchAge,OwnershipReason.Intervention);
        Assert(economy.State.Nations.Length==6 && economy.State.Nations.All(n=>n.Balance==0),"D38 treasuries initialize without free money");
        var town=civic.State.Settlements.First(s=>s.Population>0);var account=economy.Nation(town.OwnerId)!;town.Workforce=Math.Min(3,town.Population);town.Production=20;var production=town.Production;m.WorldPaused=false;m.MatchAge+=5;civic.State.LastTick=m.MatchAge;economy.Tick(m);
        Assert(account.Revenue>0 && town.Production<production && Math.Abs(production-town.Production-account.Revenue/100d)<.001,"D39 income consumes actual production");
        var balance=account.Balance;Assert(economy.Spend(town.OwnerId,10,TreasuryCategory.Construction,m.MatchAge,"Test investment") && account.Balance==balance-10,"D40 spending deducted exactly once");
        var other=economy.State.Nations.First(n=>n.NationId!=town.OwnerId);var total=economy.State.Nations.Sum(n=>n.Balance);Assert(economy.Transfer(town.OwnerId,other.NationId,5,1,m.MatchAge) && !economy.Transfer(town.OwnerId,other.NationId,5,1,m.MatchAge) && economy.State.Nations.Sum(n=>n.Balance)==total,"D41 transfers conserve currency and reject duplicate commands");
        Assert(!economy.Spend(town.OwnerId,balance+10000,TreasuryCategory.Construction,m.MatchAge,"Unaffordable"),"D46 unaffordable construction refused");
        var poor=economy.ProsperityTarget(town,account,m.MatchAge);town.Food=100;town.Materials=100;town.Production=100;var better=economy.ProsperityTarget(town,account,m.MatchAge);Assert(better>poor,"D43 prosperity responds to real stocks");var score=better;account.Balance+=100;account.Revenue+=100;Assert(economy.ProsperityTarget(town,account,m.MatchAge)==score,"D44 treasury alone does not determine prosperity");
        var local=economy.Settlement(town.Id)!;m.MatchAge+=40;civic.State.LastTick=m.MatchAge;local.Score=65;local.Tier=ProsperityTier.Prosperous;local.TierSince=m.MatchAge-31;economy.Tick(m);
        Assert(civic.State.RecentEvents.Any(e=>e.Kind==CivicEventKind.SustainedProsperity && e.SettlementId==town.Id) && local.HighestAnnounced>=ProsperityTier.Prosperous,"D50 sustained prosperity emits a factual settlement-specific event");
        m.WorldPaused=true;var before=JsonSerializer.Serialize(economy.State);m.MatchAge+=10;economy.Tick(m);Assert(JsonSerializer.Serialize(economy.State)==before,"D48 pause stops financial progression");civic.State.LastTick=m.MatchAge;
        board.FitWorld();board.CenterCamera();Assert(JsonSerializer.Serialize(economy.State)==before,"D49 camera cannot modify economy");
        civic.FlushHistory();var raw=civic.CaptureJson();var restored=LivingCivilization.Restore(raw,world.Nations,Path.GetDirectoryName(world.Controller.SavePath)!);Assert(JsonSerializer.Serialize(restored.State.Economy)==before,"D47 save/load preserves all financial state");
        m.WorldPaused=false;restored.Economy!.Tick(m);var once=restored.Economy.State.Sequence;restored.Economy.Tick(m);Assert(restored.Economy.State.Sequence==once,"D47 same-tick income not replayed after load");m.WorldPaused=true;
        var bad=civic.State with{Economy=civic.State.Economy! with{SchemaVersion=99}};var rejected=false;try{LivingCivilization.Validate(bad,world.Nations);}catch(InvalidDataException){rejected=true;}Assert(rejected,"D47 unsupported optional economy schema rejected without migration");
        Assert(world.Nations.Expansion!=null && world.Nations.Nations.All(n=>world.Nations.Components(n.Id).All(c=>world.Nations.OwnerAt(c.Anchor)==n.Id)),"D51 political expansion ownership remains valid");
        Assert(library.Worlds().Single().WorldId==world.Info.WorldId,"D52 world library identity remains intact");
        Assert(typeof(GameMenu).Assembly.GetName().Name=="Aevoria" && !Application.Current.Windows.OfType<MainWindow>().Any(),"D57 standalone game needs no Nexa dashboard");Assert(File.Exists(AevoriaLauncher.Resolve(Path.GetFullPath("bin/Release/net10.0-windows"))),"D58 Nexa launcher still resolves standalone game");
        Assert(restored.State.JournalHead==civic.State.JournalHead,"D59 existing factual history remains valid");
        board.Close();Assert(!world.Controller.Running,"D12 detached world stops ticking");
        CheckDEconomicDevelopment(world);
        CheckDLegacy(library);
        // Run the 48-case benchmark in a separate fresh process using --aevoria-d-baseline.
    }
    private static void CheckDLegacy(AevoriaLibrary library)
    {
        foreach(var version in new[]{1,2})
        {
            var old=GrandWait(LivingWorlds.Create(library.Store,"Legacy "+version,"legacy-d",512,generatorVersion:version,civilizations:true));var oldRaw=old.Civilization!.CaptureJson();
            Assert(old.Civilization.State.SchemaVersion==1 && old.Civilization.Economy==null && !oldRaw.TryGetProperty("Economy",out _),$"D53 generator {version} retains legacy civic save/rules");
            var colors=TerrainChunks.Generate(old.Terrain,new(1,1)).Colors.ToArray();var backup=GrandWait(library.UpgradeEconomy(old.Info.WorldId));var upgraded=library.Open(old.Info.WorldId);
            Assert(File.Exists(backup) && upgraded.Civilization?.Economy!=null && colors.SequenceEqual(TerrainChunks.Generate(upgraded.Terrain,new(1,1)).Colors),$"D52/D53 generator {version} explicit economy upgrade backs up history and preserves terrain");
        }
    }
    private static void CheckDEconomicDevelopment(LivingWorldSession world)
    {
        var m=world.Controller.Manager;var civic=world.Civilization!;var economy=civic.Economy!;m.WorldPaused=false;var startBuildings=civic.State.Buildings.Length;var startMaterials=civic.State.Settlements.Sum(s=>s.Materials);var ticks=new List<double>();
        for(var i=0;i<7500;i++){var clock=Stopwatch.GetTimestamp();CStep(world);ticks.Add(Stopwatch.GetElapsedTime(clock).TotalMilliseconds);}
        Assert(civic.State.Buildings.Length>startBuildings && civic.State.Buildings.Any(b=>b.InvestmentCents>0) && economy.State.Nations.Sum(n=>n.Construction)>0,"D42 real production funds material-consuming construction");
        Assert(civic.State.Buildings.Where(b=>b.Completed).All(b=>b.Progress==1) && civic.State.Buildings.Any(b=>b.Completed),"D45 architecture uses actual completed buildings");
        var building=civic.State.Buildings.First(b=>b.InvestmentCents>0 && b.Condition>0);var account=economy.Nation(building.OwnerId)!;var losses=account.LostAssets;civic.Damage(building.Id,2,m.MatchAge);civic.Damage(building.Id,2,m.MatchAge);Assert(account.LostAssets==losses+building.InvestmentCents,"D40 actual destruction writes down invested assets once");
        LivingCivilization.Validate(civic.State,world.Nations!);Assert(economy.State.Nations.All(n=>n.Balance>=0) && civic.State.Settlements.All(s=>s.Food>=0 && s.Materials>=0),"D46 financial and physical resources never negative");
        Assert(civic.State.RecentEvents.GroupBy(e=>e.Id).All(g=>g.Count()==1),"D50 durable economic/civic fact identities never duplicate");
        var before=civic.State.Sequence;var raw=civic.CaptureJson();var restored=LivingCivilization.Restore(raw,world.Nations!,Path.GetDirectoryName(world.Controller.SavePath)!);Assert(restored.State.Sequence==before,"D50/D59 history archive reload does not replay facts");
        m.WorldPaused=true;world.Controller.SaveNow();File.WriteAllText(Path.Combine(output,"economy-metrics.json"),JsonSerializer.Serialize(new{SimulationSeconds=m.MatchAge,StepP95Ms=Percentile(ticks,.95),EconomyLastMs=economy.LastMilliseconds,Buildings=civic.State.Buildings.Length,Completed=civic.State.Buildings.Count(b=>b.Completed),Revenue=economy.State.Nations.Sum(n=>n.Revenue),Construction=economy.State.Nations.Sum(n=>n.Construction)}));
    }
    private static void CheckAevoriaDBaseline()
    {
        var rows=new List<object>();
        foreach(var size in GrandGeneration.Sizes.Where(s=>s.Width is 1024 or 2048 or 4096 or 6144))
        {
            var clock=Stopwatch.StartNew();var world=GrandWait(new AevoriaLibrary(Path.Combine(output,"visual-"+size.Width)).Create("Visual benchmark","living-landscape",size.Width,size.Height,new(){PopulationBudget=120}));clock.Stop();world.Controller.SetPaused(true);
            BenchmarkDWorld(world,rows,clock.Elapsed.TotalMilliseconds);
            var board=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Show();Pump(TimeSpan.FromMilliseconds(80));board.FitWorld();board.ZoomAt(new Point(board.Battlefield.ActualWidth/2,board.Battlefield.ActualHeight/2),240);var center=board.Battlefield.ScreenToWorld(new Point(board.Battlefield.ActualWidth/2,board.Battlefield.ActualHeight/2));var watch=Stopwatch.StartNew();board.Width=1400;board.Height=700;Pump(TimeSpan.FromMilliseconds(120));var after=board.Battlefield.ScreenToWorld(new Point(board.Battlefield.ActualWidth/2,board.Battlefield.ActualHeight/2));rows.Add(new{Kind="CameraResize",size.Width,CenterDrift=(center-after).Length,LatencyMs=watch.Elapsed.TotalMilliseconds});board.Close();
        }
        File.WriteAllText(Path.Combine(output,"visual-metrics.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Visual benchmark artifacts: "+output);
    }
    private static void BenchmarkDWorld(LivingWorldSession world,List<object> rows,double generationMs)
    {
        var overlay=new BugOverlay(world.Controller.Manager){ShowHud=false,StrategicMap=true};overlay.SetWidgetSurfaces([]);overlay.TerrainView=new LivingTerrainView(world,overlay);var host=new Grid{Background=Brushes.Black};host.Children.Add(overlay);var window=new Window{Content=host,Width=1200,Height=800,WindowStyle=WindowStyle.None,ShowActivated=false,Left=-10000,Top=-10000};window.Show();Pump(TimeSpan.FromMilliseconds(80));
        foreach(var lod in new[]{"World","Regional","Local","Inspection"})
        {
            var zoom=lod switch{"World"=>Math.Min(1200/world.Info.Extent,800/world.Info.HeightExtent)*.96,"Regional"=>.3,"Local"=>.8,_=>1.8};var center=world.Nations!.Nations[0].Capital;overlay.Camera=new Matrix(zoom,0,0,zoom,600-center.X*zoom,400-center.Y*zoom);overlay.RefreshFrame();
            // Force the new view to render before checking readiness; the previous LOD may still report ready.
            overlay.UpdateLayout();new RenderTargetBitmap(1200,800,96,96,PixelFormats.Pbgra32).Render(host);
            var wait=Stopwatch.StartNew();while((!overlay.TerrainView.DetailReady || overlay.TerrainView.Landscape is {Ready:false}) && wait.Elapsed.TotalSeconds<4)Pump(TimeSpan.FromMilliseconds(30));
            Assert(overlay.MapDetail.ToString()==lod && overlay.TerrainView.Landscape is {Ready:true},"D24 complete landscape at "+world.Info.Tiles+" "+lod);
            foreach(var dpi in new[]{1d,1.5,2d})
            {
                var image=new RenderTargetBitmap((int)(1200*dpi),(int)(800*dpi),96*dpi,96*dpi,PixelFormats.Pbgra32);var samples=new List<double>();for(var frame=0;frame<8;frame++){var timer=Stopwatch.StartNew();overlay.RefreshFrame();overlay.UpdateLayout();image.Clear();image.Render(host);if(frame>=2)samples.Add(timer.Elapsed.TotalMilliseconds);}
                Assert(overlay.TerrainView.Landscape is {Ready:true},"D benchmark captures complete landscape at "+world.Info.Tiles+" "+lod+" "+dpi);
                rows.Add(new{Kind="Render",world.Info.Tiles,world.Info.HeightTiles,Lod=lod,Dpi=dpi,SoftwareP95Ms=Percentile(samples,.95),SoftwareMaxMs=samples.Max(),GenerationMs=generationMs,LandscapeReady=overlay.TerrainView.Landscape?.Ready,LandscapeBytes=overlay.TerrainView.Landscape?.CacheBytes,DecorationBuildMs=overlay.TerrainView.Landscape?.BuildMilliseconds,DrawnDecorations=overlay.TerrainView.Landscape?.DrawnDecorations,ChunkBytes=world.Chunks.CachedBytes,ManagedBytes=GC.GetTotalMemory(false),WorkingSetBytes=Process.GetCurrentProcess().WorkingSet64});Console.WriteLine(JsonSerializer.Serialize(rows[^1]));
                if(world.Info.Tiles==1024 && dpi==1){var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(output,"landscape-"+lod+".png"));encoder.Save(file);}
            }
        }
        overlay.TerrainView.Dispose();window.Close();
    }
}
