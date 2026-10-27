using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace SessionDashboard;
internal readonly record struct LandscapeDecoration(Point Position,TerrainBiome Biome,int Kind,double Scale);
// Presentation only: no navigation edits, save objects, entities or simulation RNG.
internal sealed class AevoriaLandscape : IDisposable
{
    internal const long Budget=32L*1024*1024;
    private readonly LivingWorldSession world;
    private readonly BugOverlay overlay;
    private readonly Dictionary<(int,int,int),(DrawingGroup Drawing,long Used,int Count)> cache=[];
    private long clock;
    private BitmapSource? overview;
    private DrawingGroup? overviewDrawing;
    private volatile bool disposed;
    private bool building;
    private readonly ulong seedHash;
    internal bool Ready {get;private set;}
    internal bool HasOverview=>overview!=null;
    internal int DrawnDecorations {get;private set;}
    internal int Builds {get;private set;}
    internal double BuildMilliseconds {get;private set;}
    internal long CacheBytes=>(overview==null?0:(long)overview.PixelWidth*overview.PixelHeight*4)+cache.Values.Sum(c=>24576L+c.Count*640L);
    internal AevoriaLandscape(LivingWorldSession world,BugOverlay overlay)
    {
        this.world=world;this.overlay=overlay;ulong hash=14695981039346656037;foreach(var c in world.Info.Seed)hash=unchecked((hash^c)*1099511628211);seedHash=hash;
        _=Task.Run(()=>
        {
            var start=Stopwatch.GetTimestamp();const int width=512;var height=width*world.Info.HeightTiles/world.Info.Tiles;var pixels=new uint[width*height];
            for(var y=0;y<height && !disposed;y++)for(var x=0;x<width;x++)pixels[y*width+x]=ReliefColor((x+.5)*world.Info.Tiles/width,(y+.5)*world.Info.HeightTiles/height);
            if(disposed)return;var image=BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,width*4);image.Freeze();var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if(!overlay.Dispatcher.HasShutdownStarted)overlay.Dispatcher.BeginInvoke(()=>
            {
                if(disposed)return;overview=image;overviewDrawing=new DrawingGroup();
                RenderOptions.SetBitmapScalingMode(overviewDrawing,BitmapScalingMode.Linear);
                using(var dc=overviewDrawing.Open())dc.DrawImage(image,new Rect(0,0,world.Info.Extent,world.Info.HeightExtent));
                overviewDrawing.Freeze();BuildMilliseconds+=ms;overlay.RefreshFrame();
            });
        });
    }
    private double Noise(int x,int y,int channel)
    {
        ulong h=seedHash;
        h^=unchecked((ulong)(long)x*0x9e3779b97f4a7c15UL^(ulong)(long)y*0xbf58476d1ce4e5b9UL^(ulong)channel*7919);h=unchecked((h^(h>>30))*0xbf58476d1ce4e5b9UL);h=unchecked((h^(h>>27))*0x94d049bb133111ebUL);return ((h^(h>>31))>>11)*(1d/(1UL<<53));
    }
    internal LandscapeDecoration? Decoration(int x,int y)
    {
        var point=new Point((x+.15+Noise(x,y,1)*.7)*36,(y+.15+Noise(x,y,2)*.7)*36);if(point.X<0 || point.Y<0 || point.X>=world.Info.Extent || point.Y>=world.Info.HeightExtent)return null;
        var tile=world.Terrain.Sample((int)(point.X/6),(int)(point.Y/6));var n=Noise(x,y,3);
        if(tile.Biome is TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake or TerrainBiome.River or TerrainBiome.Beach)return null;
        var density=tile.Biome switch{TerrainBiome.Forest=>.80,TerrainBiome.Mountain or TerrainBiome.Hills or TerrainBiome.Snow=>.48,TerrainBiome.Desert=>.14,_=>.24};if(n>density || Noise(x/3,y/3,7)>.86)return null;
        var latitude=Math.Abs(point.Y/world.Info.HeightExtent-.5)*2;
        var kind=tile.Biome switch{TerrainBiome.Forest=>latitude>.62?1:latitude<.26 && tile.Height<.35?2:0,TerrainBiome.Mountain or TerrainBiome.Snow=>3,TerrainBiome.Hills=>n<.2?3:4,TerrainBiome.Desert=>4,_=>n<.1?0:5};
        return new(point,tile.Biome,kind,.7+Noise(x,y,4)*.6);
    }
    internal uint ReliefColor(double x,double y)
    {
        var tile=world.Terrain.Sample((int)x,(int)y);var color=world.Terrain.ColorAt(x,y);if(tile.Biome is TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake or TerrainBiome.River)return color;
        var left=world.Terrain.Sample(Math.Max(0,(int)x-2),(int)y).Height;var right=world.Terrain.Sample(Math.Min(world.Info.Tiles-1,(int)x+2),(int)y).Height;
        var top=world.Terrain.Sample((int)x,Math.Max(0,(int)y-2)).Height;var bottom=world.Terrain.Sample((int)x,Math.Min(world.Info.HeightTiles-1,(int)y+2)).Height;
        var relief=Math.Clamp((left-right)*24+(top-bottom)*18,-.35,.35);var grain=(Noise((int)x/2,(int)y/2,11)-.5)*.07;var forest=tile.Biome==TerrainBiome.Forest?(Noise((int)x/3,(int)y/3,12)-.5)*.16:0;var factor=1+relief+grain+forest;
        byte Shade(int shift)=>(byte)Math.Clamp((color>>shift&255)*factor,0,255);
        return 0xff000000|(uint)Shade(16)<<16|(uint)Shade(8)<<8|Shade(0);
    }
    internal void Draw(DrawingContext dc,Rect view)
    {
        Ready=false;DrawnDecorations=0;dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));dc.PushClip(new RectangleGeometry(new Rect(0,0,world.Info.Extent,world.Info.HeightExtent)));
        // The overview is also the fallback while close-up chunks arrive. No second terrain pipeline is needed.
        if(overviewDrawing!=null && (overlay.MapDetail<=MapDetail.Regional || !Covers(view)))dc.DrawDrawing(overviewDrawing);
        if(overlay.MapDetail<=MapDetail.Regional)
        {
            Ready=overview!=null;dc.Pop();dc.Pop();return;
        }
        const int unit=360;var quality=(int)world.Info.Quality;var left=Math.Max(0,(int)(view.Left/unit));var right=Math.Min((int)(world.Info.Extent/unit),(int)(view.Right/unit));var top=Math.Max(0,(int)(view.Top/unit));var bottom=Math.Min((int)(world.Info.HeightExtent/unit),(int)(view.Bottom/unit));
        var missing=new List<(int,int,int)>(4);var complete=true;
        for(var y=top;y<=bottom;y++)for(var x=left;x<=right;x++)
        {
            var key=(x,y,quality);if(!cache.TryGetValue(key,out var chunk))
            {complete=false;if(missing.Count<4)missing.Add(key);continue;}
            else cache[key]=chunk=(chunk.Drawing,++clock,chunk.Count);
            dc.DrawDrawing(chunk.Drawing);DrawnDecorations+=chunk.Count;
        }
        while(cache.Count>512 || CacheBytes>Budget){var old=cache.MinBy(c=>c.Value.Used);cache.Remove(old.Key);}dc.Pop();dc.Pop();Ready=complete;
        if(!building && missing.Count>0)
        {
            building=true;
            // One bounded batch at a time; only frozen WPF resources cross the dispatcher boundary.
            _=Task.Run(()=>
            {
                var start=Stopwatch.GetTimestamp();var results=new List<((int,int,int) Key,DrawingGroup Drawing,int Count)>();
                foreach(var key in missing){if(disposed)return;var result=Build(key.Item1,key.Item2,key.Item3,out var count);results.Add((key,result,count));}
                var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if(disposed || overlay.Dispatcher.HasShutdownStarted)return;
                overlay.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,()=>
                {
                    building=false;if(disposed)return;
                    foreach(var result in results){cache[result.Key]=(result.Drawing,++clock,result.Count);Builds++;}
                    BuildMilliseconds+=ms;
                    while(cache.Count>512 || CacheBytes>Budget){var old=cache.MinBy(c=>c.Value.Used);cache.Remove(old.Key);}
                    overlay.RefreshFrame();
                });
            });
        }
    }
    internal bool Covers(Rect view)
    {
        if(overlay.MapDetail<=MapDetail.Regional)return overview!=null;
        const int unit=360;var quality=(int)world.Info.Quality;
        for(var y=Math.Max(0,(int)(view.Top/unit));y<=Math.Min((int)(world.Info.HeightExtent/unit),(int)(view.Bottom/unit));y++)
        for(var x=Math.Max(0,(int)(view.Left/unit));x<=Math.Min((int)(world.Info.Extent/unit),(int)(view.Right/unit));x++)if(!cache.ContainsKey((x,y,quality)))return false;
        return true;
    }
    private DrawingGroup Build(int cx,int cy,int quality,out int count)
    {
        var group=new DrawingGroup();RenderOptions.SetBitmapScalingMode(group,BitmapScalingMode.Linear);count=0;using(var dc=group.Open())
        {
        var pixels=new uint[60*60];for(var py=0;py<60;py++)for(var px=0;px<60;px++)pixels[py*60+px]=ReliefColor(cx*60+px,cy*60+py);
        var image=BitmapSource.Create(60,60,96,96,PixelFormats.Bgra32,null,pixels,60*4);image.Freeze();dc.DrawImage(image,new Rect(cx*360,cy*360,360,360));
        DrawContours(dc,cx,cy);
        for(var y=cy*10;y<cy*10+10;y++)for(var x=cx*10;x<cx*10+10;x++)
        {
            if(quality==0 && Noise(x,y,9)>.5 || Decoration(x,y) is not {} d)continue;count++;var p=d.Position;var s=d.Scale;var shadow=new SolidColorBrush(Color.FromArgb(65,12,24,18));shadow.Freeze();dc.DrawEllipse(shadow,null,p+new Vector(2*s,3*s),7*s,3*s);
            var trunk=new Pen(Brushes.SaddleBrown,1.3*s);dc.DrawLine(trunk,p+new Vector(0,2*s),p-new Vector(0,8*s));
            if(d.Kind==1){Triangle(dc,Brushes.DarkOliveGreen,p+new Vector(-7*s,-2*s),p+new Vector(0,-19*s),p+new Vector(7*s,-2*s));Triangle(dc,Brushes.OliveDrab,p+new Vector(-5*s,-8*s),p+new Vector(0,-22*s),p+new Vector(3*s,-8*s));}
            else if(d.Kind==2){for(var i=0;i<5;i++){var a=i*Math.PI*.4;dc.DrawLine(new Pen(Brushes.OliveDrab,2*s),p-new Vector(0,12*s),p+new Vector(Math.Cos(a)*10*s,-12*s+Math.Sin(a)*5*s));}}
            else if(d.Kind==0){dc.DrawEllipse(Brushes.DarkOliveGreen,null,p-new Vector(0,10*s),8*s,9*s);dc.DrawEllipse(Brushes.OliveDrab,null,p+new Vector(-2*s,-13*s),6*s,6*s);dc.DrawEllipse(Brushes.DarkSeaGreen,null,p+new Vector(-3*s,-15*s),2.5*s,2*s);}
            else if(d.Kind==3){Triangle(dc,Brushes.SlateGray,p+new Vector(-7*s,2*s),p+new Vector(-2*s,-8*s),p+new Vector(6*s,2*s));Triangle(dc,d.Biome==TerrainBiome.Snow?Brushes.WhiteSmoke:Brushes.DarkGray,p+new Vector(-3*s,1*s),p+new Vector(-2*s,-8*s),p+new Vector(6*s,2*s));}
            else if(d.Kind==4){dc.DrawEllipse(Brushes.OliveDrab,null,p,4*s,3*s);dc.DrawEllipse(Brushes.DarkOliveGreen,null,p+new Vector(3*s,0),3*s,2*s);}
            else{for(var i=-1;i<=1;i++)dc.DrawLine(new Pen(Brushes.DarkOliveGreen,.6),p+new Vector(i*2,2),p+new Vector(i*3,-3-Noise(x,y,i+20)*3));if(Noise(x,y,31)<.25)dc.DrawLine(new Pen(Brushes.Sienna,.8),p+new Vector(-5,1),p+new Vector(5,3));}
        }
        }
        group.Freeze();return group;
    }
    private void DrawContours(DrawingContext dc,int cx,int cy)
    {
        var geometry=new StreamGeometry();using(var path=geometry.Open())
        for(var y=0;y<12;y++)for(var x=0;x<12;x++)
        {
            var tx=cx*60+x*5;var ty=cy*60+y*5;var tile=world.Terrain.Sample(tx,ty);if(tile.Biome is not (TerrainBiome.Mountain or TerrainBiome.Hills or TerrainBiome.Snow))continue;
            double[] heights=[tile.Height,world.Terrain.Sample(tx+5,ty).Height,world.Terrain.Sample(tx+5,ty+5).Height,world.Terrain.Sample(tx,ty+5).Height];if(heights.Max()-heights.Min()<.012)continue;
            Point[] points=[new(tx*6,ty*6),new((tx+5)*6,ty*6),new((tx+5)*6,(ty+5)*6),new(tx*6,(ty+5)*6)];
            for(var band=Math.Ceiling(heights.Min()*10)/10;band<heights.Max();band+=.1)
            {
                var crossings=new List<Point>(4);for(var edge=0;edge<4;edge++){var next=(edge+1)%4;if((heights[edge]<band)==(heights[next]<band))continue;var t=(band-heights[edge])/(heights[next]-heights[edge]);crossings.Add(points[edge]+(points[next]-points[edge])*t);}
                for(var i=0;i+1<crossings.Count;i+=2){path.BeginFigure(crossings[i],false,false);path.LineTo(crossings[i+1],true,false);}
            }
        }
        geometry.Freeze();var pen=new Pen(new SolidColorBrush(Color.FromArgb(75,62,48,31)),.65);pen.Freeze();dc.DrawGeometry(null,pen,geometry);
    }
    private static void Triangle(DrawingContext dc,Brush fill,Point a,Point b,Point c){var g=new StreamGeometry();using(var path=g.Open()){path.BeginFigure(a,true,true);path.LineTo(b,true,false);path.LineTo(c,true,false);}g.Freeze();dc.DrawGeometry(fill,null,g);}
    public void Dispose(){disposed=true;cache.Clear();overview=null;overviewDrawing=null;}
}
