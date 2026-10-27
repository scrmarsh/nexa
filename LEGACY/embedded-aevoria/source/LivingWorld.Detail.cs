using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed class LivingDetailView : IDisposable
{
    private readonly LivingWorldSession session;
    private readonly BugOverlay overlay;
    private readonly DispatcherTimer arrivals;
    private (int Left,int Top,int Right,int Bottom,int Level)? requested;
    private long revision=-1;
    private long lastBuildAt;
    private (int Left,int Top,int Right,int Bottom,int Level)? baked;
    private TerrainQuality bakedQuality;
    internal bool DeferUpdates;
    private DrawingGroup? drawing;
    private uint[] pixels=[];
    internal int PixelWidth {get;private set;}
    internal int PixelHeight {get;private set;}
    internal bool Complete=>requested is {} r && baked==requested && revision==session.Chunks.Revision && Enumerable.Range(r.Top,r.Bottom-r.Top+1).All(y=>Enumerable.Range(r.Left,r.Right-r.Left+1).All(x=>session.Chunks.Get(new(x,y,r.Level))!=null));
    internal LivingDetailView(LivingWorldSession session,BugOverlay overlay)
    {
        this.session=session;this.overlay=overlay;
        arrivals=new DispatcherTimer(DispatcherPriority.Background) {Interval=TimeSpan.FromMilliseconds(90)};
        arrivals.Tick+=(_,_)=> {arrivals.Stop();lastBuildAt=0;overlay.RefreshFrame();};session.Chunks.Changed+=Ready;
    }
    private void Ready() {if(!arrivals.IsEnabled)arrivals.Start();}
    internal void Suspend()
    {
        if(requested==null)return;
        arrivals.Stop();session.Chunks.Release(this);requested=null;baked=null;drawing=null;pixels=[];
    }
    internal void Draw(DrawingContext dc,Rect view,bool paint=true)
    {
        if(DeferUpdates && drawing!=null){if(paint)Paint(dc);return;}
        var info=session.Info;var size=info.ChunkSize;var dpi=VisualTreeHelper.GetDpi(overlay);
        var physicalWidth=Math.Max(1,overlay.ActualWidth*dpi.DpiScaleX);var physicalHeight=Math.Max(1,overlay.ActualHeight*dpi.DpiScaleY);
        var target=info.Quality switch {TerrainQuality.Performance=>1024d*576,TerrainQuality.Balanced=>1920d*1080,_=>3840d*2160};
        var scale=Math.Min(1,Math.Sqrt(target/(physicalWidth*physicalHeight)));
        var level=Math.Clamp((int)Math.Ceiling(Math.Log2(1/(Math.Max(.001,overlay.Camera.M11)*info.TileSize*dpi.DpiScaleX*scale))*8),-64,64);
        var step=new ChunkKey(0,0,level).Step;var unit=size*step*info.TileSize;
        var columns=(int)Math.Ceiling(info.Tiles/(size*step));var rows=(int)Math.Ceiling(info.HeightTiles/(size*step));
        var bounds=new Rect(0,0,info.Extent,info.HeightExtent);
        if(!view.IntersectsWith(bounds)) {if(requested!=null)session.Chunks.Want(this,[]);requested=null;drawing=null;return;}
        var left=Math.Clamp((int)Math.Floor(view.Left/unit),0,columns-1);var top=Math.Clamp((int)Math.Floor(view.Top/unit),0,rows-1);
        var right=Math.Clamp((int)Math.Floor(view.Right/unit),0,columns-1);var bottom=Math.Clamp((int)Math.Floor(view.Bottom/unit),0,rows-1);
        var next=(left,top,right,bottom,level);
        if(requested!=next)
        {
            requested=next;var keys=new List<ChunkKey>();
            for(var y=top;y<=bottom;y++)for(var x=left;x<=right;x++)keys.Add(new(x,y,level));
            // Visible chunks first; at most a one-chunk prefetch ring.
            for(var y=Math.Max(0,top-1);y<=Math.Min(rows-1,bottom+1);y++)for(var x=Math.Max(0,left-1);x<=Math.Min(columns-1,right+1);x++)
                if(x<left || x>right || y<top || y>bottom)keys.Add(new(x,y,level));
            session.Chunks.Want(this,keys.ToArray());
        }
        if(drawing==null || bakedQuality!=info.Quality || (baked!=requested || revision!=session.Chunks.Revision) && System.Diagnostics.Stopwatch.GetElapsedTime(lastBuildAt).TotalMilliseconds>=75)
        {
            revision=session.Chunks.Revision;baked=requested;bakedQuality=info.Quality;lastBuildAt=System.Diagnostics.Stopwatch.GetTimestamp();PixelWidth=(right-left+1)*size+2;PixelHeight=(bottom-top+1)*size+2;
            if(pixels.Length<PixelWidth*PixelHeight)pixels=new uint[PixelWidth*PixelHeight];
            var sources=new TerrainChunk?[right-left+3,bottom-top+3];
            for(var y=top-1;y<=bottom+1;y++)for(var x=left-1;x<=right+1;x++)sources[x-left+1,y-top+1]=session.Chunks.Get(new(x,y,level));
            var overview=session.Chunks.Overview;var colors=session.Chunks.OverviewColors;
            for(var y=0;y<PixelHeight;y++)for(var x=0;x<PixelWidth;x++)
            {
                var gx=left*size+x-1;var gy=top*size+y-1;var cx=LivingTerrain.FloorDiv(gx,size);var cy=LivingTerrain.FloorDiv(gy,size);
                var source=sources[cx-left+1,cy-top+1];uint color;
                if(source!=null)color=source.Colors[(gy-cy*size+1)*(size+2)+gx-cx*size+1];
                else if(overview!=null && colors!=null && gx*step>=0 && gy*step>=0 && gx*step<info.Tiles && gy*step<info.HeightTiles)
                    color=colors[(int)(gy*step*overview.PixelHeight/info.HeightTiles)*overview.PixelWidth+(int)(gx*step*overview.PixelWidth/info.Tiles)];
                else color=0xff29485e;
                pixels[y*PixelWidth+x]=color;
            }
            var image=BitmapSource.Create(PixelWidth,PixelHeight,96,96,PixelFormats.Bgra32,null,pixels,PixelWidth*4);image.Freeze();
            drawing=new DrawingGroup();RenderOptions.SetBitmapScalingMode(drawing,BitmapScalingMode.Linear);
            using(var map=drawing.Open())map.DrawImage(image,new Rect((left*size-1)*step*info.TileSize,(top*size-1)*step*info.TileSize,PixelWidth*step*info.TileSize,PixelHeight*step*info.TileSize));
            drawing.Freeze();
        }
        if(paint)Paint(dc);
    }
    private void Paint(DrawingContext dc)
    {dc.PushClip(new RectangleGeometry(new Rect(overlay.ContentOrigin,new Size(session.Info.Extent,session.Info.HeightExtent))));dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));dc.DrawDrawing(drawing!);dc.Pop();dc.Pop();}
    public void Dispose() {session.Chunks.Changed-=Ready;arrivals.Stop();session.Chunks.Release(this);pixels=[];drawing=null;}
}
