using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckLiving024()
    {
        var metrics=new List<object>();
        foreach(var (seed,width,height) in new[]{("kingdoms",1024,576),("old-world",1536,864),("five-seas",2048,1152)})
        {
            var info=new LivingWorldInfo {Name=seed,Seed=seed,Tiles=width,TilesY=height,GeneratorVersion=2};var watch=Stopwatch.StartNew();var terrain=new LivingTerrain(info);watch.Stop();
            var repeat=new LivingTerrain(info);var a=TerrainChunks.Generate(terrain,new(3,2));var b=TerrainChunks.Generate(repeat,new(3,2));
            Assert(a.Colors.SequenceEqual(b.Colors),$"{seed}: version 2 reproduces terrain and lighting");
            const int stride=4;var w=width/stride;var h=height/stride;var cells=new byte[w*h];var landCount=0;
            for(var y=0;y<h;y++)for(var x=0;x<w;x++) {var biome=terrain.Sample(x*stride,y*stride).Biome;cells[y*w+x]=(byte)(biome is TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean ? 0 : 1);landCount+=cells[y*w+x];}
            var regions=Regions024(cells,w,h,1).OrderDescending().ToArray();var oceans=Regions024(cells,w,h,0).OrderDescending().ToArray();var major=regions.Count(n=>n>w*h*.025);
            Console.WriteLine($"{seed}: major continents={major}, land={landCount/(double)(w*h):P1}, ocean connectivity={oceans[0]/(double)(w*h-landCount):P1}, generation={watch.Elapsed.TotalMilliseconds:F0}ms, rivers={terrain.RiverSources}, atlas={terrain.AtlasBytes}");
            Assert(major is >=2 and <=5 && landCount>w*h*.2 && landCount<w*h*.7,$"{seed}: recognizable major landmasses and substantial oceans");
            Assert(oceans[0]>(w*h-landCount)*.9,$"{seed}: ocean forms one substantial connected body");
            Assert(terrain.RiverSources>0 && terrain.DrainageDownhill && terrain.OceanOutlets>0,$"{seed}: elevated catchments drain downhill to ocean outlets");
            var graph=terrain.Drainage.ToDictionary(e=>e.From,e=>e.To);var routes=true;
            foreach(var start in graph.Keys)
            {
                var node=start;var count=0;while(graph.TryGetValue(node,out var next)) {if(++count>graph.Count) {routes=false;break;}node=next;}
                var p=terrain.DrainagePoint(node);routes &= terrain.Sample((int)p.X,(int)p.Y).Height<.04;
            }
            Assert(routes,$"{seed}: all river branches connect to an outlet without cycles or isolated fragments");
            foreach(var level in new[]{-2,0,2})
            {
                var l=TerrainChunks.Generate(terrain,new(3,2,level));var r=TerrainChunks.Generate(terrain,new(4,2,level));
                Assert(Enumerable.Range(0,66).All(y=>l.Colors[y*66+65]==r.Colors[y*66+1]),$"{seed}: river/coast imagery agrees at chunk boundaries at detail level {level}");
            }
            var pixels=new uint[768*432];for(var y=0;y<432;y++)for(var x=0;x<768;x++)pixels[y*768+x]=terrain.ColorAt((x+.5)*width/768,(y+.5)*height/432);
            var image=BitmapSource.Create(768,432,96,96,PixelFormats.Bgra32,null,pixels,768*4);Save024(image,$"geography-{seed}");
            metrics.Add(new{seed,width,height,MajorContinents=major,GenerationMs=watch.Elapsed.TotalMilliseconds,terrain.AtlasBytes,terrain.RiverSources});
        }
        var legacy=new LivingWorldInfo {Seed="living-023",Name="Legacy",Tiles=512};var old=new LivingTerrain(legacy);
        var census=new Dictionary<TerrainBiome,int>();for(var y=0;y<512;y++)for(var x=0;x<512;x++) {var biome=old.Sample(x,y).Biome;census[biome]=census.GetValueOrDefault(biome)+1;}
        Assert(census[TerrainBiome.DeepOcean]==138981 && census[TerrainBiome.Forest]==13670 && census[TerrainBiome.River]==1754,"Legacy generator geography retains its exact pre-update biome census");
        var dir=Path.Combine(output,"wide-session");Directory.CreateDirectory(dir);var store=new StateStore(Path.Combine(dir,"state.json"),gameContext:true);
        var note=WidgetLibrary.Create("Notes");note.Text="Kingdoms keep notes editable";note.CustomSize=true;store.Save(new AppState {WidgetLayoutInitialized=true,Widgets=[note]});
        var colony=ArcadeWorlds.For(store);colony.SetPaused(true);colony.SaveNow();var previous=File.ReadAllBytes(colony.SavePath);
        var session=Complete023(LivingWorlds.Create(store,"Age of Kingdoms","kingdoms",1024,576));session.Controller.SetPaused(true);
        Assert(session.Info.GeneratorVersion==2 && session.Controller.Manager.BattleArena.Size==new Size(6144,3456),"New widescreen worlds use generator v2 and preserve both simulation dimensions");
        Assert(previous.SequenceEqual(File.ReadAllBytes(colony.SavePath)),"Creating a widescreen world leaves Colony Wars save bytes untouched");
        var snap=session.Controller.Manager.CaptureWorld();var restored=BugManager.RestoreWorld(snap);
        Assert(restored.BattleArena==session.Controller.Manager.BattleArena && restored.LivingGeneratorVersion==2,"Rectangular simulation extension survives a validated snapshot round trip");
        snap.LivingGeneratorVersion=0;var rejected=false;try {BugManager.ValidateWorld(snap);}catch(InvalidDataException) {rejected=true;}
        Assert(rejected,"Large dimensions remain rejected for ordinary Colony Wars snapshots");
        session.Info.Quality=TerrainQuality.High;
        var owner=new MainWindow(store) {Width=1200,Height=820};owner.Show();var board=BattlefieldWindow.ShowFor(owner,store);board.Left=30;board.Top=30;board.Activate();Pump(TimeSpan.FromMilliseconds(80));
        board.FitWorld();var corner=board.Battlefield.WorldToScreen(new Point(session.Info.Extent,session.Info.HeightExtent));var startPoint=board.Battlefield.WorldToScreen(new Point());
        Assert(startPoint.X>=0 && startPoint.Y>=0 && corner.X<=board.Battlefield.ActualWidth && corner.Y<=board.Battlefield.ActualHeight,"Fit world displays a complete rectangular map without stretching");
        var anchor=new Point(170,130);var before=board.Battlefield.ScreenToWorld(anchor);board.ZoomAt(anchor,360);
        Assert((board.Battlefield.ScreenToWorld(anchor)-before).Length<.001,"Wide-world zoom remains cursor centered");
        Assert(owner.sharedArcade==session.Controller && board.World==session.Controller,"Both wide-world views use one authoritative simulation");
        var editor=Descendants(owner).OfType<TextBox>().First(t=>t.Text==note.Text);var edits=0;var input=new DispatcherTimer(DispatcherPriority.Input) {Interval=TimeSpan.FromMilliseconds(20)};
        input.Tick+=(_,_)=> {editor.Text="Kingdoms edited "+edits++;};input.Start();session.Controller.SetPaused(false);var frames=board.Battlefield.RenderedFrames;var active=Stopwatch.StartNew();Pump(TimeSpan.FromSeconds(1.5));input.Stop();
        var cadence=(board.Battlefield.RenderedFrames-frames)/active.Elapsed.TotalSeconds;session.Controller.SetPaused(true);
        Console.WriteLine($"Wide-world foreground: {cadence:F1} OnRender/sec, edits={edits}");
        Assert(cadence>=15 && edits>=15,"Widescreen foreground animation and dashboard editing remain responsive");metrics.Add(new{ForegroundOnRenderPerSecond=cadence,AcceptedEdits=edits});
        var followed=session.Controller.Manager.Bugs.First(b=>BugManager.Alive(b) && !b.IsLeader);board.FollowBug(followed);
        Assert(board.FollowedUnitId==followed.UnitId && board.Battlefield.Camera.M11>=1.8,"Follow Bug selects an individual at useful close-up zoom");
        var health=followed.Health;followed.Health=0;session.Controller.SetPaused(true);
        Assert(board.FollowedUnitId==null,"A followed bug's death exits follow mode safely");followed.Health=health;
        board.FitWorld();
        board.Close();owner.Close();
        foreach(var (width,height) in new[]{(1920,1080),(2560,1440),(3840,2160)})
        {
            var overlay=new BugOverlay(session.Controller.Manager) {ShowHud=false};overlay.TerrainView=new LivingTerrainView(session,overlay);
            var dpi=VisualTreeHelper.GetDpi(overlay);var logicalWidth=width/dpi.DpiScaleX;var logicalHeight=height/dpi.DpiScaleY;
            overlay.Measure(new Size(logicalWidth,logicalHeight));overlay.Arrange(new Rect(0,0,logicalWidth,logicalHeight));
            var zoom=Math.Min(logicalWidth/session.Info.Extent,logicalHeight/session.Info.HeightExtent)*.96;
            overlay.Camera=new Matrix(zoom,0,0,zoom,(logicalWidth-session.Info.Extent*zoom)/2,(logicalHeight-session.Info.HeightExtent*zoom)/2);
            var image=new RenderTargetBitmap(width,height,96*dpi.DpiScaleX,96*dpi.DpiScaleY,PixelFormats.Pbgra32);var load=Stopwatch.StartNew();
            void Render() {overlay.RefreshFrame();overlay.UpdateLayout();image.Clear();image.Render(overlay);}
            Render();while(!overlay.TerrainView.DetailReady && load.Elapsed.TotalSeconds<15) {Pump(TimeSpan.FromMilliseconds(100));Render();}
            Assert(overlay.TerrainView.DetailReady,$"{width}x{height}: visible zoom-aware terrain chunks load completely");
            var detail=overlay.TerrainView.DetailPixels!.Value;
            Console.WriteLine($"Capture target {width}x{height}, DPI={dpi.DpiScaleX:F2}, source={detail.Width}x{detail.Height}, quality={session.Info.Quality}");
            Assert(detail.Width>768 && detail.Height>432 && detail.Width<=width+256 && detail.Height<=height+256,$"{width}x{height}: terrain detail follows viewport resolution instead of stretching a small overview");
            var times=new List<double>();for(var i=0;i<7;i++) {var t=Stopwatch.GetTimestamp();Render();if(i>0)times.Add(Stopwatch.GetElapsedTime(t).TotalMilliseconds);}
            Console.WriteLine($"{width}x{height}: source={detail.Width}x{detail.Height}, load={load.Elapsed.TotalMilliseconds:F0}ms, software render p95={Percentile(times,.95):F2}ms, cached={session.Chunks.CachedBytes}");
            Assert(session.Chunks.CachedBytes<=session.Chunks.Budget && session.Chunks.PendingCount<=66,$"{width}x{height}: detail cache and worker queue remain bounded");
            Save024(image,$"wide-{width}");metrics.Add(new{ViewportWidth=width,ViewportHeight=height,SourceWidth=detail.Width,SourceHeight=detail.Height,LoadMs=load.Elapsed.TotalMilliseconds,SoftwareRenderP95Ms=Percentile(times,.95),session.Chunks.CachedBytes});
            if(width==1920)
            {
                var savedSeed=session.Info.Seed;session.Info.Quality=TerrainQuality.Performance;Render();
                Assert(session.Info.Seed==savedSeed && session.Info.GeneratorVersion==2 && overlay.TerrainView.DetailPixels!.Value.Width<detail.Width,"Quality changes detail resolution without regenerating geography");session.Info.Quality=TerrainQuality.High;
            }
            overlay.TerrainView.Dispose();
        }
        session.Info.Quality=TerrainQuality.Balanced;session.SaveMetadata();session.Controller.SaveNow();
        var saved=LivingFiles.Read<LivingWorldInfo>(Path.Combine(LivingWorlds.Root(store),session.Info.WorldId.ToString("N"),"world.json"));
        Assert(saved.TilesY==576 && saved.Quality==TerrainQuality.Balanced && saved.GeneratorVersion==2,"Camera/render preferences and rectangular generator metadata persist atomically");
        File.WriteAllText(Path.Combine(output,"geography024.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static List<int> Regions024(byte[] cells,int width,int height,byte value)
    {
        var seen=new bool[cells.Length];var result=new List<int>();var queue=new Queue<int>();
        for(var i=0;i<cells.Length;i++)
        {
            if(seen[i] || cells[i]!=value)continue;seen[i]=true;queue.Enqueue(i);var count=0;
            while(queue.TryDequeue(out var n))
            {
                count++;var x=n%width;var y=n/width;
                foreach(var p in new[]{x>0 ? n-1 : -1,x<width-1 ? n+1 : -1,y>0 ? n-width : -1,y<height-1 ? n+width : -1})
                    if(p>=0 && !seen[p] && cells[p]==value) {seen[p]=true;queue.Enqueue(p);}
            }
            result.Add(count);
        }
        return result;
    }
    private static void Save024(BitmapSource image,string name)
    {var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(output,name+".png"));encoder.Save(file);}
}
