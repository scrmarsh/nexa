using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Aevoria.Desktop;
using SessionDashboard;
internal static partial class Program
{
    private static T GrandWait<T>(Task<T> task)
    {var watch=Stopwatch.StartNew();while(!task.IsCompleted && watch.Elapsed.TotalSeconds<90)Pump(TimeSpan.FromMilliseconds(15));Assert(task.IsCompleted,"Grand-world operation completes within its bounded test timeout");return task.GetAwaiter().GetResult();}
    private static void CaptureB(FrameworkElement visual,string name,double scale)
    {var bitmap=new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth*scale),(int)Math.Ceiling(visual.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(output,name+".png"));encoder.Save(file);}
    private static void CheckAevoriaB()
    {
        var metrics=new List<object>();var menu=new GameMenu(new AevoriaLibrary(Path.Combine(output,"menu"))){Width=740,Height=560};menu.Show();Pump(TimeSpan.FromMilliseconds(80));
        Assert(AevoriaBuild.Label==AevoriaBuild.Version+" Alpha" && AevoriaBuild.Version==typeof(GameMenu).Assembly.GetName().Version!.ToString(3),"Aevoria display derives its numeric alpha version from its own assembly metadata");
        Assert(App.AppVersion=="0.1.23","Nexa retains its independent version");
        foreach(var scale in new[]{1d,1.5,2d})
        {var labels=Descendants(menu).OfType<TextBlock>().Where(t=>t.Text==AevoriaBuild.Label || t.Text==AevoriaBuild.Status).ToArray();Assert(labels.Length==2 && labels.All(t=>{var rect=t.TransformToAncestor(menu).TransformBounds(new Rect(t.RenderSize));return rect.Top>0 && rect.Bottom<menu.ActualHeight && t.FontSize*scale>=12;}),$"Version/development status remain visible at minimum size and {scale:P0} capture DPI");CaptureB(menu,"menu-b-"+scale.ToString(System.Globalization.CultureInfo.InvariantCulture),scale);}
        menu.Close();Assert(File.Exists(AevoriaLauncher.Resolve(Path.GetFullPath("bin/Release/net10.0-windows"))),"Nexa launcher resolves the independent Aevoria host");
        var formLibrary=new AevoriaLibrary(Path.Combine(output,"creation-form"));var form=new GameMenu(formLibrary);form.Show();form.ShowNew();Pump(TimeSpan.FromMilliseconds(60));
        var sizeChoice=Descendants(form).OfType<ComboBox>().First(c=>c.Items.Count>0 && c.Items[0] is ComboBoxItem);sizeChoice.SelectedIndex=0;
        var profileChoice=Descendants(form).OfType<ComboBox>().Single(c=>c.Items.Count==4 && Equals(c.Items[0],"Continental Kingdoms"));profileChoice.SelectedIndex=3;Descendants(form).OfType<Slider>().Single().Value=65;
        var play=Descendants(form).OfType<Button>().Single(b=>Equals(b.Content,"Create & Play"));play.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));play.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Until023(()=>form.Game!=null,"Create & Play completes through the actual form");Pump(TimeSpan.FromMilliseconds(100));
        Assert(formLibrary.Worlds().Length==1,"Repeated creation clicks produce exactly one world");Assert(form.Game!.Living!.Info.Generation is {Profile:GeographyProfile.Custom,LandTarget:.65},"The creation form persists selected geography/custom land settings");
        var initialFit=Math.Min(form.Game.Battlefield.ActualWidth/form.Game.Living.Info.Extent,form.Game.Battlefield.ActualHeight/form.Game.Living.Info.HeightExtent);
        Assert(form.Game.Battlefield.MapDetail==MapDetail.World && form.Game.Battlefield.Camera.M11>=initialFit*.9 && form.Game.Battlefield.Camera.M11<=initialFit*1.1,"The actual New World flow opens a fitted full-world presentation");form.Game.Close();Pump(TimeSpan.FromMilliseconds(60));Assert(Descendants(form).OfType<Button>().Any(b=>Equals(b.Content,"Copy Seed")) && !string.IsNullOrWhiteSpace(formLibrary.Worlds().Single().Seed),"Returning to the menu exposes the persistent seed and copy action");form.Close();
        foreach(var seed in new[]{"kingdoms","old-world","five-seas"})
        {
            var info=new LivingWorldInfo{Name=seed,Seed=seed,GeneratorVersion=3,Generation=new(),Tiles=1024,TilesY=576};var watch=Stopwatch.StartNew();var terrain=new LivingTerrain(info);watch.Stop();var repeat=new LivingTerrain(info);var cells=new byte[512*288];var traversable=0;
            for(var y=0;y<288;y++)for(var x=0;x<512;x++){var tile=terrain.Sample(x*2,y*2);cells[y*512+x]=(byte)(tile.Biome is TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake?0:1);if(LivingTerrain.Land(tile.Biome))traversable++;}
            var regions=Regions024(cells,512,288,1).OrderDescending().ToArray();var total=cells.Count(b=>b==1);var fraction=traversable/(double)cells.Length;var connected=regions[0]/(double)total;
            Console.WriteLine($"{seed}: traversable={fraction:P2}, largest land component={connected:P2}, significant islands={regions.Count(n=>n>cells.Length*.001)-1}, atlas={terrain.AtlasBytes}, generation={watch.ElapsedMilliseconds}ms");
            Assert(fraction is >=.70 and <=.85,$"{seed}: Continental Kingdoms meets traversable land bounds");Assert(connected>=.90 && regions.Count(n=>n>cells.Length*.001)<=8,$"{seed}: one dominant mainland, occasional islands");
            Assert(terrain.DrainageDownhill && terrain.RiverSources>0 && terrain.OceanOutlets>0,$"{seed}: downhill river trees connect to ocean outlets");
            var biomes=new Dictionary<TerrainBiome,int>();for(var y=0;y<576;y+=4)for(var x=0;x<1024;x+=4){var biome=terrain.Sample(x,y).Biome;biomes[biome]=biomes.GetValueOrDefault(biome)+1;}
            Assert(biomes.GetValueOrDefault(TerrainBiome.Grassland)>100 && biomes.GetValueOrDefault(TerrainBiome.Forest)>100 && biomes.GetValueOrDefault(TerrainBiome.Mountain)+biomes.GetValueOrDefault(TerrainBiome.Hills)>100,$"{seed}: broad plains/forest biomes and coherent relief coexist");
            Assert(TerrainChunks.Generate(terrain,new(4,2)).Colors.SequenceEqual(TerrainChunks.Generate(repeat,new(4,2)).Colors),$"{seed}: identical settings reproduce terrain/lighting");
            var colors=new uint[cells.Length];for(var y=0;y<288;y++)for(var x=0;x<512;x++)colors[y*512+x]=terrain.ColorAt(x*2,y*2);var image=BitmapSource.Create(512,288,96,96,PixelFormats.Bgra32,null,colors,512*4);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(Path.Combine(output,"grand-"+seed+".png")))encoder.Save(file);
            metrics.Add(new{Seed=seed,Land=fraction,Mainland=connected,GenerationMs=watch.Elapsed.TotalMilliseconds,terrain.AtlasBytes});
        }
        var fractions=new List<double>();foreach(var profile in Enum.GetValues<GeographyProfile>())
        {var info=new LivingWorldInfo{Name="Profiles",Seed="profiles",Tiles=512,TilesY=512,GeneratorVersion=3,Generation=new(){Profile=profile,LandTarget=.65}};var terrain=new LivingTerrain(info);var land=0;for(var y=0;y<512;y+=4)for(var x=0;x<512;x+=4)if(LivingTerrain.Land(terrain.Sample(x,y).Biome))land++;fractions.Add(land/16384d);Assert(Math.Abs(fractions[^1]-info.Generation.LandFraction)<.06,$"{profile}: configured land distribution is meaningful");}
        Assert(fractions[0]>fractions[1]+.15 && fractions[1]>fractions[2]+.12,"Geography presets produce distinct land/water balances");
        bool bad=false;try{new LivingWorldInfo{Name="Bad",Seed="bad",Tiles=int.MaxValue,TilesY=int.MaxValue,GeneratorVersion=3,Generation=new()}.Validate();}catch(InvalidDataException){bad=true;}Assert(bad,"Unbounded dimensions are rejected before allocation");
        var cancelled=new AevoriaLibrary(Path.Combine(output,"cancelled"));using(var cts=new CancellationTokenSource())
        {cts.Cancel();bool stopped=false;try{GrandWait(cancelled.Create("Cancelled","",512,512,cancel:cts.Token));}catch(OperationCanceledException){stopped=true;}Assert(stopped && cancelled.Worlds().Length==0,"Cancelled generation does not create a selectable world");}
        using(var cts=new CancellationTokenSource())
        {bool stopped=false;var progress=new Progress<string>(phase=>{if(phase.StartsWith("Indexing"))cts.Cancel();});try{GrandWait(cancelled.Create("Cancelled during work","cancel",2048,1152,progress:progress,cancel:cts.Token));}catch(OperationCanceledException){stopped=true;}Assert(stopped && cancelled.Worlds().Length==0,"Cancellation during generation preserves the selectable library");}
        foreach(var size in GrandGeneration.Sizes.Where(s=>s.Width>=1024))CheckGrandSize(size.Name,size.Width,size.Height,metrics);
        File.WriteAllText(Path.Combine(output,"grand-metrics.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static void CheckGrandSize(string label,int width,int height,List<object> metrics)
    {
        GC.Collect();var before=GC.GetTotalMemory(true);var library=new AevoriaLibrary(Path.Combine(output,"size-"+width));var edits=0;var pulse=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(20)};pulse.Tick+=(_,_)=>edits++;pulse.Start();var watch=Stopwatch.StartNew();var world=GrandWait(library.Create(label,"",width,height,new(){PopulationBudget=120}));watch.Stop();pulse.Stop();
        Assert(edits>0 && world.Info.GeneratorVersion==3 && world.Civilization!=null,$"{label}: asynchronous generation keeps dispatcher responsive and initializes civilizations");Assert(world.Controller.Manager.BattleOptions.HardCap==120,$"{label}: population budget is independent of area");
        var controller=world.Controller;var manager=controller.Manager;controller.SetPaused(true);manager.WorldPaused=false;var ticks=new List<double>();for(var i=0;i<2400;i++){var start=Stopwatch.GetTimestamp();manager.PrepareLivingStep();manager.Advance(.033);manager.ResolveLivingStep(world.Terrain,.033);manager.Nations!.Tick(manager);manager.Civilization!.Tick(manager);ticks.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}manager.WorldPaused=true;
        var board=new BattlefieldWindow(controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Show();Pump(TimeSpan.FromMilliseconds(80));board.FitWorld();Pump(TimeSpan.FromMilliseconds(60));var overlay=board.Battlefield;
        Assert(!NexaDialog.BlankPanelDragTarget(overlay,board),$"{label}: left clicks reach the map instead of starting a window drag");
        Until023(()=>overlay.TerrainView!.DetailReady,$"{label}: bounded viewport detail becomes available");
        var snapshot=JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions);var armies=overlay.TerrainView!.ArmyView.Groups;
        Assert(overlay.MapDetail==MapDetail.World && overlay.DrawnIndividuals==0,$"{label}: full-world view hides individual creatures/effects");
        Assert(overlay.TerrainView.SettlementView.VisibleSettlements>0 && overlay.TerrainView.NationView.LabelBounds.Any(),$"{label}: full-world settlement/nation markers are visible");
        Assert(armies.SelectMany(a=>a.Units).Distinct().Count()==armies.Sum(a=>a.Units.Length) && armies.Sum(a=>a.Units.Length)==manager.Bugs.Count(b=>BugManager.Alive(b)&&!b.IsLeader&&b.Role!=ArmyRole.Reserve),$"{label}: each actual military unit appears in exactly one read-only group");
        var built=overlay.TerrainView.NationView.Rebuilds;CaptureB(overlay,"world-"+width,1);Assert(overlay.TerrainView.NationView.Rebuilds==built,$"{label}: political geometry is reused without ownership changes");
        var render=new List<double>();foreach(var scale in new[]{1d,1.5,2d}){var values=new List<double>();for(var i=0;i<4;i++){var target=new RenderTargetBitmap((int)(overlay.ActualWidth*scale),(int)(overlay.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);var start=Stopwatch.GetTimestamp();target.Render(overlay);values.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}render.Add(Percentile(values,.95));}
        if(width==1024)
        {
            var comparison=new List<object>();foreach(var strategic in new[]{false,true})
            {overlay.StrategicMap=strategic;overlay.RefreshFrame();Pump(TimeSpan.FromMilliseconds(60));var times=new List<double>();for(var i=0;i<8;i++){var target=new RenderTargetBitmap((int)overlay.ActualWidth,(int)overlay.ActualHeight,96,96,PixelFormats.Pbgra32);var start=Stopwatch.GetTimestamp();target.Render(overlay);times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}comparison.Add(new{Strategic=strategic,Population=manager.Bugs.Count(BugManager.Alive),Individuals=overlay.DrawnIndividuals,SoftwareP95Ms=Percentile(times,.95)});}
            File.WriteAllText(Path.Combine(output,"strategic-comparison.json"),JsonSerializer.Serialize(comparison,new JsonSerializerOptions{WriteIndented=true}));Assert(overlay.DrawnIndividuals==0,"Equivalent paused-world comparison restores strategic presentation without changing units");
        }
        var anchor=new Point(500,300);var wp=overlay.ScreenToWorld(anchor);board.ZoomAt(anchor,120);Assert((overlay.ScreenToWorld(anchor)-wp).Length<.001,$"{label}: wheel zoom keeps its cursor anchor");
        var smoothAnchor=overlay.ScreenToWorld(anchor);board.SmoothZoomAt(anchor,120);Pump(TimeSpan.FromMilliseconds(200));Assert((overlay.ScreenToWorld(anchor)-smoothAnchor).Length<.001 && !board.FocusingNation,$"{label}: smooth wheel interpolation preserves the anchor and completes");
        overlay.Camera=new Matrix(.3,0,0,.3,0,0);Assert(overlay.MapDetail==MapDetail.Regional,$"{label}: regional presentation is available");overlay.Camera=new Matrix(.49,0,0,.49,0,0);Assert(overlay.MapDetail==MapDetail.Regional,$"{label}: hysteresis prevents upward boundary flicker");overlay.Camera=new Matrix(.7,0,0,.7,0,0);Assert(overlay.MapDetail==MapDetail.Local,$"{label}: local presentation is available");overlay.Camera=new Matrix(.47,0,0,.47,0,0);Assert(overlay.MapDetail==MapDetail.Local,$"{label}: hysteresis prevents downward boundary flicker");board.FitWorld();
        if(armies.Count>0){board.SelectArmy(armies[0]);board.FocusArmy();Assert(board.SelectedArmy?.Id==armies[0].Id && board.FocusingNation,$"{label}: real military groups can be selected and focused");board.ZoomAt(anchor,0);Assert(!board.FocusingNation,$"{label}: manual zoom cancels army focus");}
        board.FitWorld();Pump(TimeSpan.FromMilliseconds(60));foreach(var scale in new[]{1d,1.5,2d}){var city=world.Civilization!.State.Settlements.First(s=>s.Active);var physical=overlay.WorldToScreen(city.Position);physical=new Point(physical.X*scale,physical.Y*scale);Assert(overlay.TerrainView.SettlementView.Hit(new Point(physical.X/scale,physical.Y/scale))!=null,$"{label}: settlement hit coordinates align in {scale:P0} capture space");}
        overlay.Camera=new Matrix(.7,0,0,.7,-1e6,-1e6);overlay.RefreshFrame();Pump(TimeSpan.FromMilliseconds(60));Assert(overlay.TerrainView.SettlementView.VisibleSettlements==0 && overlay.TerrainView.NationView.LabelBounds.Count()==0 && overlay.DrawnIndividuals==0,$"{label}: fully offscreen settlements, nation labels and creatures are culled");board.FitWorld();
        board.ZoomAt(anchor,10000);Pump(TimeSpan.FromMilliseconds(50));Assert(overlay.MapDetail==MapDetail.Inspection && overlay.TerrainView.ArmyView.VisibleGroups==0,$"{label}: close inspection switches from groups to individual rendering");
        board.FitWorld();var nation=world.Nations!.Nations.First();var component=world.Nations.Components(nation.Id).First();board.SelectNation(new(nation.Id,component.Key,false));Assert(board.FocusingNation && board.FocusDestination is {} focus && world.Nations.Land.IsLand(focus),$"{label}: nation focus targets a connected land region");board.Pan(new Vector(2,3));Assert(!board.FocusingNation,$"{label}: manual pan cancels automated focus");
        var settlement=world.Civilization!.State.Settlements.First(s=>s.Active);board.SelectSettlement(settlement.Id);Assert(board.SelectedSettlementId==settlement.Id && board.FocusDestination==settlement.Position,$"{label}: settlement selection focuses its actual position");board.Pan(new Vector(1,1));
        Assert(JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions)==snapshot,$"{label}: camera/aggregation/rendering leave simulation/RNG unchanged");
        Assert(world.Chunks.CachedBytes<=world.Chunks.Budget && world.Chunks.PendingCount<=66,$"{label}: terrain residency and worker queue remain bounded");
        var cacheBytes=world.Chunks.CachedBytes;var pending=world.Chunks.PendingCount;var seed=world.Info.Seed;board.Close();controller.SaveNow();library.Backup(world.Info.WorldId,Path.Combine(library.Root,"world.zip"));var reopened=library.Open(world.Info.WorldId);Until023(()=>reopened.PoliticsReady.IsCompleted,$"{label}: reopened politics/civilizations finish loading");Assert(reopened.Civilization!=null && reopened.Info.Seed==seed && reopened.Controller.Manager.BattleArena.Width==width*6d,$"{label}: large-world snapshot, seed and optional history reload");
        Assert(GC.GetTotalMemory(false)-before<512L*1024*1024,$"{label}: retained managed allocation stays below the 512 MiB acceptance cap");
        var bytes=GC.GetTotalMemory(false)-before;var process=Process.GetCurrentProcess();Console.WriteLine($"{label} {width}x{height}: generation={watch.Elapsed.TotalMilliseconds:F0}ms, retainedDelta={bytes}, workingSet={process.WorkingSet64}, CPU p95={Percentile(ticks,.95):F3}ms, software p95=[{string.Join(",",render.Select(v=>v.ToString("F2")))}], cache={world.Chunks.CachedBytes}, atlas={world.Terrain.AtlasBytes}");
        metrics.Add(new{Size=label,Width=width,Height=height,GenerationMs=watch.Elapsed.TotalMilliseconds,ManagedDeltaBytes=bytes,WorkingSetBytes=process.WorkingSet64,CpuP95Ms=Percentile(ticks,.95),SoftwareRenderP95Ms=render,AtlasBytes=world.Terrain.AtlasBytes,TopologyBytes=world.Nations.Land.Bytes,CacheBytes=cacheBytes,PendingChunks=pending,SnapshotBytes=new FileInfo(controller.SavePath).Length});LivingWorlds.Forget(library.Store,world.Info.WorldId);
    }
}
