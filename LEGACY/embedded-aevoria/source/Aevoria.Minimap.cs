using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace SessionDashboard;
internal sealed class AevoriaMinimap : FrameworkElement,IDisposable
{
    private readonly LivingWorldSession world;
    private readonly BattlefieldWindow camera;
    private readonly DispatcherTimer updates;
    private BitmapSource? image;
    private uint[]? terrainPixels;
    private long revision=-1;
    private bool dragging,collapsed;
    internal int Rebuilds {get;private set;}
    internal double BuildMilliseconds {get;private set;}
    internal long CachedBytes=>(image==null?0:(long)image.PixelWidth*image.PixelHeight*4)+(terrainPixels?.LongLength??0)*4;
    internal AevoriaMinimap(LivingWorldSession world,BattlefieldWindow camera)
    {
        this.world=world;this.camera=camera;Focusable=true;ToolTip="Click or drag to navigate · double-click to fit · right-click to collapse";
        updates=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(250)};updates.Tick+=(_,_)=>{if(IsVisible && !collapsed && revision!=(world.Nations?.Revision??0)){RefreshOverview();InvalidateVisual();}};Loaded+=(_,_)=>updates.Start();Unloaded+=(_,_)=>updates.Stop();
        MouseLeftButtonDown+=(_,e)=>{Focus();if(collapsed){collapsed=false;Height=160;InvalidateVisual();e.Handled=true;return;}if(e.ClickCount==2){camera.FitWorld();e.Handled=true;return;}dragging=true;CaptureMouse();Navigate(e.GetPosition(this));e.Handled=true;};
        MouseMove+=(_,e)=>{if(dragging){Navigate(e.GetPosition(this));e.Handled=true;}};MouseLeftButtonUp+=(_,e)=>{dragging=false;ReleaseMouseCapture();e.Handled=true;};LostMouseCapture+=(_,_)=>dragging=false;
        MouseRightButtonDown+=(_,e)=>{collapsed=!collapsed;Height=collapsed?30:160;InvalidateVisual();e.Handled=true;};KeyDown+=(_,e)=>{if(e.Key==Key.Home){camera.FitWorld();e.Handled=true;}};
    }
    internal Rect MapBounds
    {
        get{var ratio=world.Info.Extent/world.Info.HeightExtent;var width=Math.Max(1,ActualWidth-12);var height=Math.Max(1,ActualHeight-26);if(width/height>ratio)width=height*ratio;else height=width/ratio;return new Rect((ActualWidth-width)/2,20+(ActualHeight-26-height)/2,width,height);}
    }
    internal Point MapToWorld(Point p){var b=MapBounds;return new(Math.Clamp((p.X-b.Left)/b.Width,0,1)*world.Info.Extent,Math.Clamp((p.Y-b.Top)/b.Height,0,1)*world.Info.HeightExtent);}
    internal Point WorldToMap(Point p){var b=MapBounds;return new(b.Left+p.X/world.Info.Extent*b.Width,b.Top+p.Y/world.Info.HeightExtent*b.Height);}
    internal Rect ViewportRectangle
    {
        get{var view=camera.VisibleWorldBounds;view.Intersect(new Rect(0,0,world.Info.Extent,world.Info.HeightExtent));return view.IsEmpty?Rect.Empty:new Rect(WorldToMap(view.TopLeft),WorldToMap(view.BottomRight));}
    }
    internal void Navigate(Point p)=>camera.NavigateTo(MapToWorld(p));
    internal void RefreshOverview()
    {
        var start=Stopwatch.GetTimestamp();const int width=256;var height=Math.Max(1,(int)(width*world.Info.HeightExtent/world.Info.Extent));var pixels=new uint[width*height];var owners=new Guid?[pixels.Length];
        if(terrainPixels==null){terrainPixels=new uint[pixels.Length];for(var y=0;y<height;y++)for(var x=0;x<width;x++)terrainPixels[y*width+x]=world.Terrain.ColorAt((x+.5)*world.Info.Tiles/width,(y+.5)*world.Info.HeightTiles/height);}
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {
            var tx=(x+.5)*world.Info.Tiles/width;var ty=(y+.5)*world.Info.HeightTiles/height;var color=terrainPixels[y*width+x];var owner=world.Nations?.OwnerAt(new Point(tx*6,ty*6));owners[y*width+x]=owner;
            if(owner is {} id && world.Nations!.Find(id) is {} nation)color=Blend(color,nation.Color,.55);pixels[y*width+x]=color;
        }
        for(var y=1;y<height;y++)for(var x=1;x<width;x++){var i=y*width+x;if(owners[i]!=null && (owners[i]!=owners[i-1] || owners[i]!=owners[i-width]))pixels[i]=0xfff0d28e;}
        image=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,width*4);image.Freeze();revision=world.Nations?.Revision??0;Rebuilds++;BuildMilliseconds+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
    private static uint Blend(uint a,uint b,double f)=>0xff000000|(uint)((byte)((a>>16&255)*(1-f)+(b>>16&255)*f))<<16|(uint)((byte)((a>>8&255)*(1-f)+(b>>8&255)*f))<<8|(byte)((a&255)*(1-f)+(b&255)*f);
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(235,12,20,29)),new Pen(Brushes.SlateGray,1),new Rect(RenderSize),7,7);
        var text=new FormattedText(collapsed?"World overview ▸":"World overview · right-click to fold",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),10,Brushes.Wheat,VisualTreeHelper.GetDpi(this).PixelsPerDip);dc.DrawText(text,new Point(8,5));if(collapsed)return;
        if(image==null)RefreshOverview();dc.DrawImage(image,MapBounds);var rect=ViewportRectangle;if(!rect.IsEmpty)dc.DrawRectangle(null,new Pen(Brushes.White,1.5),rect);var point=WorldToMap(camera.CameraCenter);if(MapBounds.Contains(point))dc.DrawEllipse(Brushes.Gold,new Pen(Brushes.Black,1),point,2.5,2.5);
    }
    public void Dispose(){updates.Stop();image=null;}
}
