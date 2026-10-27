using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SessionDashboard;
internal static partial class Program
{
    private static void Until023(Func<bool> condition,string label)
    {
        var watch=Stopwatch.StartNew();while(!condition() && watch.Elapsed.TotalSeconds<12)Pump(TimeSpan.FromMilliseconds(12));
        Assert(condition(),label);
    }
    private static T Complete023<T>(Task<T> task)
    {Until023(()=>task.IsCompleted,"Asynchronous world operation completes without a blocked dispatcher");return task.GetAwaiter().GetResult();}
    private static void CheckLiving023()
    {
        var malformed=Path.Combine(output,"malformed-living.json");File.WriteAllText(malformed,"not json");var rejected=false;
        try {_=LivingFiles.Read<LivingWorldInfo>(malformed);}catch(InvalidDataException) {rejected=true;}
        Assert(rejected && File.ReadAllText(malformed)=="not json","Malformed Living World files produce a recoverable data error and remain untouched");
        var info=new LivingWorldInfo {Name="Verdant Sea",Seed="living-023",Tiles=512};var map=new LivingTerrain(info);var other=new LivingTerrain(info with{WorldId=Guid.NewGuid()});
        var keys=new[]{new ChunkKey(1,2),new(-1,-2),new(0,0),new(3,4)};
        var first=keys.Select(k=>TerrainChunks.Generate(map,k)).ToArray();var reordered=keys.Reverse().Select(k=>TerrainChunks.Generate(other,k)).Reverse().ToArray();
        Assert(first.Zip(reordered).All(p=>p.First.Biomes.SequenceEqual(p.Second.Biomes)),"Seed, version and coordinates reproduce positive/negative chunks independently of generation order");
        Assert(LivingTerrain.FloorDiv(-1,64)==-1 && LivingTerrain.FloorDiv(-64,64)==-1 && LivingTerrain.FloorDiv(-65,64)==-2,"Negative chunk coordinates use floor division");
        var different=TerrainChunks.Generate(new LivingTerrain(info with{Seed="another-world"}),new(3,4));
        Assert(!first[3].Biomes.SequenceEqual(different.Biomes),"Different seeds produce different geography");
        var left=TerrainChunks.Generate(map,new(3,3));var right=TerrainChunks.Generate(map,new(4,3));var lp=new uint[66*66];var rp=new uint[66*66];left.Image.CopyPixels(lp,66*4,0);right.Image.CopyPixels(rp,66*4,0);
        Assert(Enumerable.Range(0,66).All(y=>lp[y*66+65]==rp[y*66+1] && lp[y*66+64]==rp[y*66]),"Neighboring chunk image halos match exactly across their shared boundary");
        var modified=new LivingTerrain(info with{Edits=[new TerrainEdit(10,10,TerrainBiome.Desert)]});
        Assert(modified.Sample(10,10).Biome==TerrainBiome.Desert,"Versioned terrain edit records override the regenerated baseline");
        var unsupported=false;try {_=new LivingTerrain(info with{GeneratorVersion=999});}catch(InvalidDataException) {unsupported=true;}
        Assert(unsupported,"Unsupported generator versions are rejected instead of silently regenerating different geography");
        var biomes=new Dictionary<TerrainBiome,int>();var generation=Stopwatch.StartNew();var highest=-1d;
        for(var y=0;y<512;y++)for(var x=0;x<512;x++) {var tile=map.Sample(x,y);highest=Math.Max(highest,tile.Height);var b=tile.Biome;biomes[b]=biomes.GetValueOrDefault(b)+1;}
        Console.WriteLine("Biome census: "+string.Join(", ",biomes.Select(p=>$"{p.Key}={p.Value}"))+ $"; highest={highest:F3}; full analytical census {generation.Elapsed.TotalMilliseconds:F0}ms (not run on UI in production)");
        Assert(biomes.ContainsKey(TerrainBiome.DeepOcean) && biomes.ContainsKey(TerrainBiome.ShallowOcean) && biomes.ContainsKey(TerrainBiome.Beach) && biomes.ContainsKey(TerrainBiome.River) && biomes.ContainsKey(TerrainBiome.Forest) && biomes.ContainsKey(TerrainBiome.Grassland),"Generated map contains substantial oceans, coastlines, rivers, forest and grassland");
        using(var cache=new TerrainChunks(map,96*1024))
        {
            var view=new object();
            for(var i=0;i<28;i++) {var key=new ChunkKey(i%8,i/8);cache.Want(view,[key]);Until023(()=>cache.Get(key)!=null,"Camera-streamed chunk becomes available");if(cache.CachedBytes>cache.Budget)throw new Exception("Chunk cache exceeded budget");}
            Assert(cache.CachedCount<=3 && cache.CachedBytes<=cache.Budget && cache.PendingCount<=66,"Chunk residency and generation queue remain bounded during repeated camera movement");cache.Want(view,Enumerable.Range(0,64).Select(i=>new ChunkKey(i%8,i/8)).ToArray());cache.Want(view,[]);Until023(()=>cache.PendingCount==0,"Obsolete queued chunk requests drain without publishing unwanted terrain");cache.Release(view);
        }
        var directory=Path.Combine(output,"living-session");Directory.CreateDirectory(directory);var store=new StateStore(Path.Combine(directory,"state.json"),gameContext:true);
        var note=WidgetLibrary.Create("Notes");note.Text="Keep my widgets";note.CustomSize=true;store.Save(new AppState {WidgetLayoutInitialized=true,Widgets=[note]});
        var colony=ArcadeWorlds.For(store);colony.SetPaused(true);colony.SaveNow();var colonyFile=File.ReadAllBytes(colony.SavePath);var colonyId=colony.Manager.WorldId;
        var edits=0;var inputTimer=new DispatcherTimer(DispatcherPriority.Input) {Interval=TimeSpan.FromMilliseconds(10)};inputTimer.Tick+=(_,_)=>edits++;inputTimer.Start();
        var world=Complete023(LivingWorlds.Create(store,"Verdant Sea","living-023",512,generatorVersion:1));Until023(()=>world.Chunks.Overview!=null,"Coarse world overview generates asynchronously");inputTimer.Stop();
        Assert(edits>0,"Dispatcher input timers remain responsive during generation and file creation");
        Assert(File.ReadAllBytes(colony.SavePath).SequenceEqual(colonyFile) && world.Controller.SavePath!=colony.SavePath,"New Living World never overwrites the Colony Wars save");
        var originalSimulation=File.ReadAllBytes(world.Controller.SavePath);var second=Complete023(LivingWorlds.Create(store,"Amber Coast","another-world",256,generatorVersion:1));
        Assert(second.Info.WorldId!=world.Info.WorldId && LivingWorlds.List(store).Count==2 && originalSimulation.SequenceEqual(File.ReadAllBytes(world.Controller.SavePath)),"Generating another world creates a unique save and preserves previous worlds");
        LivingWorlds.Select(store,new(ArcadeGame.LivingWorlds,world.Info.WorldId));world.Controller.SetPaused(true);
        Assert(ReferenceEquals(LivingWorlds.Current(store),world) && world.Controller.Manager.Colonies.Values.All(c=>LivingTerrain.Land(map.At(c.Territory).Biome)),"Loading resumes the same world controller with six land-based colonies");
        var manager=world.Controller.Manager;manager.WorldPaused=false;var clock=Stopwatch.StartNew();
        for(var i=0;i<450;i++) {manager.PrepareLivingStep();manager.Advance(.033);manager.ResolveLivingStep(world.Terrain,.033);}
        manager.WorldPaused=true;clock.Stop();
        Assert(manager.Bugs.Where(b=>BugManager.Alive(b) && b.Species!=BugSpecies.Flies).All(b=>world.Terrain.At(b.Position).Biome is not (TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake)),"Ground units spawn and remain on valid terrain during existing combat movement");
        var walker=manager.Bugs.First(b=>BugManager.Alive(b) && b.Species!=BugSpecies.Flies);var fly=manager.Bugs.First(b=>BugManager.Alive(b) && b.Species==BugSpecies.Flies);
        manager.PrepareLivingStep();walker.Position=new Point(6,6);fly.Position=new Point(6,6);manager.ResolveLivingStep(world.Terrain,.033);
        Assert(world.Terrain.At(walker.Position).Biome!=TerrainBiome.DeepOcean && world.Terrain.At(fly.Position).Biome==TerrainBiome.DeepOcean,"Ocean correction returns ground units to valid terrain while allowing flying units over water");
        Console.WriteLine($"Living simulation: {manager.Bugs.Count} entities, 450 steps {clock.Elapsed.TotalMilliseconds:F0}ms");
        var main=new MainWindow(store) {Width=1200,Height=820,Left=-10000,Top=-10000,ShowActivated=false};main.Show();Pump(TimeSpan.FromMilliseconds(180));
        Assert(main.livingWorld==world && ReferenceEquals(main.bugsOverlay!.Manager,manager) && main.bugsOverlay.TerrainView!=null && main.WidgetCanvas.Background==Brushes.Transparent && !main.bugsOverlay.IsHitTestVisible && Panel.GetZIndex(main.bugsOverlay)<Panel.GetZIndex(main.FullContent),"Living dashboard shares the simulation and renders beneath interactive widgets");
        var editor=Descendants(main).OfType<TextBox>().First(t=>t.Text=="Keep my widgets");editor.Text="Widgets remain editable";
        var point=manager.Bugs.First(BugManager.Alive).Position;var before=main.bugsOverlay!.WorldToScreen(point);main.WidgetCanvas.Height=2500;main.UpdateLayout();main.WidgetScroller.ScrollToVerticalOffset(240);Pump(TimeSpan.FromMilliseconds(80));var after=main.bugsOverlay.WorldToScreen(point);
        Assert(main.WidgetScroller.VerticalOffset>150 && after.Y<before.Y-150 && (main.bugsOverlay.ScreenToWorld(after)-point).Length<.01,"Dashboard scrolling translates terrain/bugs and preserves reversible world coordinates");
        var dialog=new LivingWorldDialog(main,store);dialog.Show();Pump(TimeSpan.FromMilliseconds(50));
        Assert(new[]{"COLONY WARS","LIVING WORLDS","New World","Load World","Generate","Open Battlefield","Return to COLONY WARS"}.All(text=>Descendants(dialog).OfType<Button>().Any(b=>Equals(b.Content,text))),"Arcade interface provides both modes and all Living World creation/loading controls");dialog.Close();
        var settings=new SettingsDialog(main,store,store.Load(),main,"Add Bugs");settings.Show();world.Controller.SetPaused(false);colony.SetPaused(false);
        Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Pause world")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(colony.Manager.WorldPaused && !world.Controller.Manager.WorldPaused,"Traditional settings pause Colony Wars without changing the game world's clock");
        Assert(!Descendants(settings).OfType<Button>().Any(b=>b.Content?.ToString()?.Contains("LIVING WORLDS")==true),"Traditional settings expose no historical-game navigation");settings.Close();
        var board=BattlefieldWindow.ShowFor(main,store);Pump(TimeSpan.FromMilliseconds(120));
        Assert(ReferenceEquals(board.World,world.Controller) && ReferenceEquals(board.Battlefield.Manager,main.bugsOverlay.Manager) && ReferenceEquals(BattlefieldWindow.ShowFor(main,store),board),"Standalone and dashboard reuse one authoritative Living World simulation");
        board.FitWorld();var origin=board.Battlefield.WorldToScreen(new Point());var corner=board.Battlefield.WorldToScreen(new Point(world.Info.Extent,world.Info.Extent));
        Assert(origin.X>=0 && origin.Y>=0 && corner.X<=board.Battlefield.ActualWidth+1 && corner.Y<=board.Battlefield.ActualHeight+1,"Fit world overview reveals the entire finite map");
        var cursor=new Point(160,140);var anchor=board.Battlefield.ScreenToWorld(cursor);board.ZoomAt(cursor,240);
        Assert((board.Battlefield.ScreenToWorld(cursor)-anchor).Length<.01,"Standalone wheel zoom preserves its cursor anchor");var oldScreen=board.Battlefield.WorldToScreen(point);board.Pan(new Vector(41,-23));
        Assert((board.Battlefield.WorldToScreen(point)-oldScreen-new Vector(41,-23)).Length<.01,"Standalone pan translates the camera without moving simulation entities");
        board.Left=30;board.Top=30;board.Activate();world.Controller.SetPaused(false);Pump(TimeSpan.FromMilliseconds(150));
        var liveFrames=board.Battlefield.RenderedFrames;var liveEdits=0;var liveClock=Stopwatch.StartNew();
        var editTimer=new DispatcherTimer(DispatcherPriority.Input) {Interval=TimeSpan.FromMilliseconds(20)};editTimer.Tick+=(_,_)=> {liveEdits++;editor.Text="Widgets remain editable"+(liveEdits%2==0 ? "." : "");};editTimer.Start();Pump(TimeSpan.FromSeconds(1.2));editTimer.Stop();
        var cadence=(board.Battlefield.RenderedFrames-liveFrames)/liveClock.Elapsed.TotalSeconds;world.Controller.SetPaused(true);editor.Text="Widgets remain editable";
        Console.WriteLine($"Living foreground draw cadence: {cadence:F1} OnRender calls/sec; accepted edits={liveEdits}");
        Assert(cadence>=20 && liveEdits>=20,"Foreground terrain animation and normal widget editing remain responsive together");
        board.CenterCamera();var ocean=new Point(64*world.Info.TileSize,10*world.Info.TileSize);var oceanScreen=board.Battlefield.WorldToScreen(ocean);
        var samplePoint=new Point(board.Battlefield.ActualWidth/2+.37,board.Battlefield.ActualHeight/2);board.Pan(samplePoint-oceanScreen);
        Until023(()=>world.Chunks.Get(new(0,0))!=null && world.Chunks.Get(new(1,0))!=null,"Both ocean-edge chunks load for the rendered seam check");Pump(TimeSpan.FromMilliseconds(90));
        var seam=new System.Windows.Media.Imaging.RenderTargetBitmap((int)board.Battlefield.ActualWidth,(int)board.Battlefield.ActualHeight,96,96,PixelFormats.Pbgra32);seam.Render(board.Battlefield);var seamPixels=new uint[3];seam.CopyPixels(new Int32Rect((int)samplePoint.X-1,(int)samplePoint.Y,3,1),seamPixels,12,0);
        Assert(seamPixels.All(p=>(p>>24)==255) && seamPixels.Max(p=>p&255)-seamPixels.Min(p=>p&255)<4,"Fractionally scaled adjacent terrain chunks render without transparent/dark seams");
        board.CenterCamera();Until023(()=>world.Chunks.CachedCount>0,"High-detail visible chunks load for the battlefield");Pump(TimeSpan.FromMilliseconds(120));Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"living-battlefield023");
        main.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(80));
        var renderImage=new System.Windows.Media.Imaging.RenderTargetBitmap((int)board.ActualWidth,(int)board.ActualHeight,96,96,PixelFormats.Pbgra32);var renderTimes=new List<double>();
        for(var frame=0;frame<24;frame++) {board.Battlefield.RefreshFrame();board.Battlefield.UpdateLayout();var began=Stopwatch.GetTimestamp();renderImage.Clear();renderImage.Render(board.Battlefield);if(frame>3)renderTimes.Add(Stopwatch.GetElapsedTime(began).TotalMilliseconds);}
        Console.WriteLine($"Living cached terrain software render p95: {Percentile(renderTimes,.95):F2}ms; resident chunks={world.Chunks.CachedCount}; cache bytes={world.Chunks.CachedBytes}");
        Assert(Percentile(renderTimes,.95)<50 && world.Chunks.CachedBytes<=world.Chunks.Budget,"Cached terrain and insects remain within a bounded responsive software-render budget");main.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(60));board.FitWorld();Pump(TimeSpan.FromMilliseconds(90));Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"living-overview023");main.WidgetScroller.ScrollToVerticalOffset(0);Pump(TimeSpan.FromMilliseconds(80));Save018(main,(int)main.ActualWidth,(int)main.ActualHeight,"living-dashboard023");
        var zoom=world.Info.CameraZoom;board.Close();world.Controller.SaveNow();world.SaveMetadata();
        var saved=LivingFiles.Read<LivingWorldInfo>(Path.Combine(LivingWorlds.Root(store),world.Info.WorldId.ToString("N"),"world.json"));var restoredMap=new LivingTerrain(saved);var restored=new ArcadeWorldStore(world.Controller.SavePath).Load();
        Assert(saved.Seed==world.Info.Seed && saved.CameraZoom==zoom && restored.World?.WorldId==saved.WorldId && TerrainChunks.Generate(restoredMap,new(3,4)).Biomes.SequenceEqual(first[3].Biomes),"Save/reload retains world identity, seed, camera, simulation and regenerated geography");
        var replayA=manager;var replayB=BugManager.RestoreWorld(restored.World!);
        // Match the session's optional-layer restore before replaying committed conquest hooks.
        if(replayB.PendingPolitics is {} politics){replayB.Nations=LivingNationWorld.Restore(new PoliticalLand(restoredMap),saved.WorldId,politics);replayB.Nations.BindWriter();replayB.PendingPolitics=null;}
        replayA.WorldPaused=replayB.WorldPaused=false;
        for(var step=0;step<120;step++)foreach(var replay in new[]{replayA,replayB}) {replay.PrepareLivingStep();replay.Advance(.033);replay.ResolveLivingStep(restoredMap,.033);}
        replayA.WorldPaused=replayB.WorldPaused=true;
        Assert(JsonSerializer.Serialize(replayA.CaptureWorld(),ArcadeWorldStore.JsonOptions)==JsonSerializer.Serialize(replayB.CaptureWorld(),ArcadeWorldStore.JsonOptions),"Restored terrain constraints and combat replay deterministically for 120 steps");
        LivingWorlds.Select(store,new());Assert(main.livingWorld==null && main.sharedArcade==colony && colony.Manager.WorldId==colonyId,"Returning to Colony Wars restores its original saved simulation");
        main.SaveNow();main.Close();Assert(store.Load().Widgets.Single().Text=="Widgets remain editable","Normal dashboard edits persist independently of Living Worlds");
        File.WriteAllText(Path.Combine(output,"living023.json"),JsonSerializer.Serialize(new {BiomeCounts=biomes,Seed=info.Seed,SimEntities=manager.Bugs.Count,Simulation450StepsMs=clock.Elapsed.TotalMilliseconds,DispatcherInputTicks=edits},new JsonSerializerOptions {WriteIndented=true}));
    }
}
