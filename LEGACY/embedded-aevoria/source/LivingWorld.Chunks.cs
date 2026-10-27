using System.Threading.Channels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed record TerrainChunk(ChunkKey Key,byte[] Biomes,BitmapSource Image,uint[] Colors)
{
    internal long Bytes=>Biomes.Length+(long)Image.PixelWidth*Image.PixelHeight*8+256;
}
internal sealed class TerrainChunks : IDisposable
{
    private readonly LivingTerrain terrain;
    private readonly object gate=new();
    private readonly Dictionary<ChunkKey,(TerrainChunk Chunk,long Used)> cache=[];
    private readonly Dictionary<object,ChunkKey[]> views=[];
    private readonly HashSet<ChunkKey> pending=[];
    private readonly Channel<ChunkKey> queue=Channel.CreateBounded<ChunkKey>(new BoundedChannelOptions(64){SingleReader=false,SingleWriter=false});
    private readonly CancellationTokenSource cancellation=new();
    private readonly Dispatcher dispatcher=Dispatcher.CurrentDispatcher;
    private long clock,bytes;
    private int notification;
    private bool disposed;
    internal readonly long Budget;
    internal long Revision {get;private set;}
    internal BitmapSource? Overview {get;private set;}
    internal uint[]? OverviewColors {get;private set;}
    internal event Action? Changed;
    internal long CachedBytes {get {lock(gate)return bytes;}}
    internal int CachedCount {get {lock(gate)return cache.Count;}}
    internal int PendingCount {get {lock(gate)return pending.Count;}}
    internal TerrainChunks(LivingTerrain terrain,long budget=8*1024*1024)
    {
        this.terrain=terrain;Budget=Math.Max(64*1024,budget);
        // Two bounded chunk workers plus one short-lived overview job; never one Task per chunk.
        for(var i=0;i<2;i++)_ = Task.Run(Worker);
        _=Task.Run(()=>
        {
            try
            {
                var size=terrain.Info.GeneratorVersion==1 ? 192 : 512;var height=Math.Max(1,size*terrain.Info.HeightTiles/terrain.Info.Tiles);var pixels=new uint[size*height];
                for(var y=0;y<height;y++) {cancellation.Token.ThrowIfCancellationRequested();for(var x=0;x<size;x++)pixels[y*size+x]=terrain.ColorAt((x+.5)*terrain.Info.Tiles/size,(y+.5)*terrain.Info.HeightTiles/height);}
                var image=BitmapSource.Create(size,height,96,96,PixelFormats.Bgra32,null,pixels,size*4);image.Freeze();
                lock(gate) {if(disposed)return;Overview=image;OverviewColors=pixels;Revision++;}Notify();
            }
            catch(OperationCanceledException) { }
        });
    }
    internal static TerrainChunk Generate(LivingTerrain terrain,ChunkKey key,CancellationToken token=default)
    {
        var size=terrain.Info.ChunkSize;var width=size+2;var pixels=new uint[width*width];var biomes=new byte[size*size];var step=key.Step;
        // A one-tile halo makes interpolation at the chunk edge match its neighbor.
        for(var y=-1;y<=size;y++)
        {
            token.ThrowIfCancellationRequested();
            for(var x=-1;x<=size;x++)
            {
                var gx=(key.X*size+x+.5)*step;var gy=(key.Y*size+y+.5)*step;
                var tile=terrain.Sample((int)Math.Floor(gx),(int)Math.Floor(gy));pixels[(y+1)*width+x+1]=terrain.ColorAt(gx,gy);
                if(x>=0 && y>=0 && x<size && y<size)biomes[y*size+x]=(byte)tile.Biome;
            }
        }
        var image=BitmapSource.Create(width,width,96,96,PixelFormats.Bgra32,null,pixels,width*4);image.Freeze();return new(key,biomes,image,pixels);
    }
    internal void Want(object view,ChunkKey[] keys)
    {
        lock(gate)
        {
            if(disposed)return;views[view]=keys;
            foreach(var key in keys)if(cache.TryGetValue(key,out var entry))cache[key]=(entry.Chunk,++clock);
            Schedule();
        }
    }
    internal void Release(object view) {lock(gate) {views.Remove(view);if(views.Count==0) {cache.Clear();bytes=0;}}}
    private void Schedule()
    {
        foreach(var key in views.Values.SelectMany(v=>v).Distinct())
        {
            if(cache.ContainsKey(key) || pending.Contains(key))continue;
            if(!queue.Writer.TryWrite(key))break;pending.Add(key);
        }
    }
    internal TerrainChunk? Get(ChunkKey key)
    {lock(gate) {if(!cache.TryGetValue(key,out var entry))return null;cache[key]=(entry.Chunk,++clock);return entry.Chunk;}}
    private async Task Worker()
    {
        try
        {
            await foreach(var key in queue.Reader.ReadAllAsync(cancellation.Token))
            {
                lock(gate)
                {
                    if(disposed)return;
                    if(!views.Values.Any(v=>Array.IndexOf(v,key)>=0)) {pending.Remove(key);Schedule();continue;}
                }
                var chunk=Generate(terrain,key,cancellation.Token);
                lock(gate)
                {
                    if(disposed)return;pending.Remove(key);
                    if(views.Values.Any(v=>Array.IndexOf(v,key)>=0)) {cache[key]=(chunk,++clock);bytes+=chunk.Bytes;}
                    while(bytes>Budget && cache.Count>0)
                    {
                        var old=cache.OrderBy(p=>p.Value.Used).First();bytes-=old.Value.Chunk.Bytes;cache.Remove(old.Key);
                    }
                    Revision++;Schedule();
                }
                Notify();
            }
        }
        catch(OperationCanceledException) { }
    }
    private void Notify()
    {
        if(Interlocked.Exchange(ref notification,1)!=0 || dispatcher.HasShutdownStarted)return;
        dispatcher.BeginInvoke(DispatcherPriority.Background,()=> {Interlocked.Exchange(ref notification,0);if(!disposed)Changed?.Invoke();});
    }
    public void Dispose()
    {
        lock(gate) {if(disposed)return;disposed=true;cancellation.Cancel();queue.Writer.TryComplete();views.Clear();cache.Clear();pending.Clear();bytes=0;Overview=null;OverviewColors=null;}
    }
}
internal sealed class LivingTerrainView : IDisposable
{
    private readonly TerrainChunks chunks;
    private readonly LivingTerrain terrain;
    private readonly BugOverlay overlay;
    private (int,int,int,int,bool)? requested;
    private DrawingGroup? drawing;
    private uint[]? mosaicPixels;
    private ((int,int,int,int,bool),long) drawn;
    private readonly LivingDetailView? detail;
    internal readonly LivingNationView NationView;
    internal readonly LivingSettlementView SettlementView;
    internal LivingArmyView ArmyView {get;}
    private readonly LivingHydrologyView? hydrology;
    internal AevoriaLandscape? Landscape {get;}
    internal bool MeasureDrawing;
    internal (double Base,double Landscape,double Water,double Politics,double Settlements,double Armies) LastDrawingTimes;
    internal void DeferDetailUpdates(bool defer){if(detail!=null)detail.DeferUpdates=defer;}
    internal (int Width,int Height)? DetailPixels=>detail==null ? null : (detail.PixelWidth,detail.PixelHeight);
    internal bool DetailReady=>Landscape is {HasOverview:true} ? Landscape.Ready : detail?.Complete??false;
    internal LivingTerrainView(LivingWorldSession session,BugOverlay overlay)
    {terrain=session.Terrain;chunks=session.Chunks;this.overlay=overlay;NationView=new(session,overlay);SettlementView=new(session,overlay);ArmyView=new(session,overlay);if(overlay.StrategicMap)Landscape=new(session,overlay);if(terrain.Info.GeneratorVersion==3)hydrology=new(terrain,overlay);if(terrain.Info.GeneratorVersion>=2)detail=new(session,overlay);else chunks.Changed+=Ready;}
    private void Ready()=>overlay.RefreshFrame();
    internal void Draw(DrawingContext dc,Rect view)
    {
        if(detail!=null)
        {
            if(!MeasureDrawing){if(Landscape is {HasOverview:true})detail.Suspend();else detail.Draw(dc,view);Landscape?.Draw(dc,view);hydrology?.Draw(dc,view);NationView.Draw(dc,view);SettlementView.Draw(dc,view);ArmyView.Draw(dc);return;}
            var stamp=System.Diagnostics.Stopwatch.GetTimestamp();
            double Lap(){var next=System.Diagnostics.Stopwatch.GetTimestamp();var ms=System.Diagnostics.Stopwatch.GetElapsedTime(stamp,next).TotalMilliseconds;stamp=next;return ms;}
            if(Landscape is {HasOverview:true})detail.Suspend();else detail.Draw(dc,view);var baseline=Lap();
            Landscape?.Draw(dc,view);var landscape=Lap();hydrology?.Draw(dc,view);var water=Lap();
            NationView.Draw(dc,view);var politics=Lap();SettlementView.Draw(dc,view);var settlements=Lap();ArmyView.Draw(dc);
            LastDrawingTimes=(baseline,landscape,water,politics,settlements,Lap());return;
        }
        var extent=terrain.Info.Extent;var bounds=new Rect(new Point(),new Size(extent,extent));
        var unit=terrain.Info.ChunkSize*terrain.Info.TileSize;var count=terrain.Info.Tiles/terrain.Info.ChunkSize;
        var left=Math.Clamp((int)Math.Floor(view.Left/unit),0,count-1);var top=Math.Clamp((int)Math.Floor(view.Top/unit),0,count-1);
        var right=Math.Clamp((int)Math.Floor(view.Right/unit),0,count-1);var bottom=Math.Clamp((int)Math.Floor(view.Bottom/unit),0,count-1);
        var coarse=overlay.Camera.M11<.48 || !view.IntersectsWith(new Rect(0,0,extent,extent));
        var wanted=(left,top,right,bottom,coarse);
        if(requested!=wanted)
        {
            requested=wanted;var keys=new List<ChunkKey>();
            if(!coarse)
            {
                for(var y=top;y<=bottom;y++)for(var x=left;x<=right;x++)keys.Add(new(x,y));
                for(var y=Math.Max(0,top-1);y<=Math.Min(count-1,bottom+1);y++)for(var x=Math.Max(0,left-1);x<=Math.Min(count-1,right+1);x++)if(x<left || x>right || y<top || y>bottom)keys.Add(new(x,y));
            }
            chunks.Want(this,keys.ToArray());
        }
        var style=(wanted,chunks.Revision);
        if(drawing==null || drawn!=style)
        {
            drawn=style;drawing=new DrawingGroup();
            // Bilinear terrain sampling avoids expensive Fant enlargement; bug artwork keeps HighQuality.
            RenderOptions.SetBitmapScalingMode(drawing,BitmapScalingMode.Linear);
            using(var map=drawing.Open())
            {
                if(coarse)
                {if(chunks.Overview is {} overview)map.DrawImage(overview,bounds);else map.DrawRectangle(Brushes.MidnightBlue,null,bounds);}
                if(!coarse)
                {
                    var size=terrain.Info.ChunkSize;var width=(right-left+1)*size+2;var height=(bottom-top+1)*size+2;
                    if(mosaicPixels==null || mosaicPixels.Length<width*height)mosaicPixels=new uint[width*height];
                    var sources=new TerrainChunk?[right-left+3,bottom-top+3];
                    for(var cy=top-1;cy<=bottom+1;cy++)for(var cx=left-1;cx<=right+1;cx++)sources[cx-left+1,cy-top+1]=chunks.Get(new(cx,cy));
                    for(var y=0;y<height;y++)for(var x=0;x<width;x++)
                    {
                        var gx=left*size+x-1;var gy=top*size+y-1;var cx=LivingTerrain.FloorDiv(gx,size);var cy=LivingTerrain.FloorDiv(gy,size);
                        var source=sources[cx-left+1,cy-top+1];uint color;
                        if(source!=null)color=source.Colors[(gy-cy*size+1)*(size+2)+gx-cx*size+1];
                        else if(chunks.OverviewColors is {} colors && gx>=0 && gy>=0 && gx<terrain.Info.Tiles && gy<terrain.Info.Tiles)color=colors[(gy*192/terrain.Info.Tiles)*192+gx*192/terrain.Info.Tiles];
                        else color=LivingTerrain.Color(new(TerrainBiome.DeepOcean,-1));
                        mosaicPixels[y*width+x]=color;
                    }
                    var image=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,mosaicPixels,width*4);image.Freeze();
                    map.DrawImage(image,new Rect((left*size-1)*terrain.Info.TileSize,(top*size-1)*terrain.Info.TileSize,width*terrain.Info.TileSize,height*terrain.Info.TileSize));
                }
            }
            drawing.Freeze();
        }
        dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));dc.DrawDrawing(drawing);dc.Pop();
        Landscape?.Draw(dc,view);NationView.Draw(dc,view);
        SettlementView.Draw(dc,view);
        ArmyView.Draw(dc);
    }
    public void Dispose() {detail?.Dispose();Landscape?.Dispose();chunks.Changed-=Ready;chunks.Release(this);drawing=null;mosaicPixels=null;}
}
