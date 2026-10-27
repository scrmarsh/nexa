using System.Windows;

namespace SessionDashboard;

// Pre-rotated premultiplied-alpha sprites let the overlay submit one bitmap per frame.
internal sealed class BugSpriteAtlas
{
    internal sealed record Sprite(int Width,int Height,int X,int Y,int[] Pixels);
    private readonly Dictionary<(BugSpecies,int,int,BugVariant),Sprite> sprites = [];
    private readonly Dictionary<(Sprite,int),Sprite> sized = [];
    internal Sprite Sized(BugEntity bug)
    {
        var sprite=For(bug); var size=(int)Math.Round(bug.SizeScale*8);
        if(size==8)return sprite;
        var key=(sprite,size);
        if(sized.TryGetValue(key,out var scaled))return scaled;
        var scale=size/8d; var x=(int)Math.Round(sprite.X*scale); var y=(int)Math.Round(sprite.Y*scale);
        var width=Math.Max(1,(int)Math.Round(sprite.Width*scale)); var height=Math.Max(1,(int)Math.Round(sprite.Height*scale));
        var pixels=new int[width*height]; Blit(sprite,pixels,width,height,-x,-y,255,scale,scale);
        scaled=new Sprite(width,height,x,y,pixels);
        if(sized.Count>=4096)sized.Clear();
        sized.Add(key,scaled); return scaled;
    }
    internal readonly Sprite Web;
    private readonly Sprite smallSplat,largeSplat,longSplat,hitRing;
    internal readonly double Scale;
    internal BugSpriteAtlas(double scale)
    {
        Scale=scale;
        foreach(var species in Enum.GetValues<BugSpecies>())
        {
            if(species is BugSpecies.AllThree or BugSpecies.AllBugs)continue;
            var poses=species == BugSpecies.Centipedes ? 8 : species == BugSpecies.Ladybugs ? 2 : 1;
            for(var pose=0;pose<poses;pose++)
            {
                var source=BugArtwork.Source(species,pose,species == BugSpecies.Ladybugs && pose==1);
                var pixels=new int[source.Image.PixelWidth*source.Image.PixelHeight];source.Image.CopyPixels(pixels,source.Image.PixelWidth*4,0);
                for(var angle=0;angle<32;angle++)sprites[(species,pose,angle,BugVariant.None)]=Rotate(source,pixels,angle);
            }
        }
        var web=BugArtwork.WebSprite;var webPixels=new int[web.Image.PixelWidth*web.Image.PixelHeight];web.Image.CopyPixels(webPixels,web.Image.PixelWidth*4,0);Web=Rotate(web,webPixels,0);
        smallSplat=Ellipse(3,1.8,0xFF654B37);largeSplat=Ellipse(6,3.6,0xFF986542);longSplat=Ellipse(9,5.4,0xFF654B37);hitRing=Ellipse(10,9,0xFFFFE7A7,true);
    }
    internal Sprite For(BugEntity bug)
    {
        var pose=bug.Species == BugSpecies.Centipedes ? (int)bug.CrawlPhase & 7 : bug.Species == BugSpecies.Ladybugs && bug.Hits>0 ? 1 : 0;
        var angle=((int)Math.Round(bug.Angle/11.25)%32+32)%32;
        var key=(bug.Species,pose,angle,bug.Variant);
        if(sprites.TryGetValue(key,out var sprite))return sprite;
        var source=BugArtwork.Source(bug.Species,pose,bug.Species==BugSpecies.Ladybugs && pose==1,bug.Variant);
        var pixels=new int[source.Image.PixelWidth*source.Image.PixelHeight]; source.Image.CopyPixels(pixels,source.Image.PixelWidth*4,0);
        sprite=Rotate(source,pixels,angle); sprites.Add(key,sprite); return sprite;
    }
    internal Sprite Splat(BugSpecies species) => species == BugSpecies.Centipedes ? longSplat : species == BugSpecies.Roaches ? largeSplat : smallSplat;
    internal Sprite HitRing => hitRing;
    private Sprite Rotate(BugArtwork.Sprite source,int[] pixels,int direction)
    {
        var angle=direction*Math.PI/16;var cos=Math.Cos(angle);var sin=Math.Sin(angle);var b=source.Bounds;
        var left=double.PositiveInfinity;var top=left;var right=double.NegativeInfinity;var bottom=right;
        foreach(var p in new[] { b.TopLeft,b.TopRight,b.BottomLeft,b.BottomRight })
        { var x=p.X*cos-p.Y*sin;var y=p.X*sin+p.Y*cos;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y); }
        var ox=(int)Math.Floor(left*Scale);var oy=(int)Math.Floor(top*Scale);
        var width=Math.Max(1,(int)Math.Ceiling(right*Scale)-ox);var height=Math.Max(1,(int)Math.Ceiling(bottom*Scale)-oy);var result=new int[width*height];
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {
            var dx=(x+ox+.5)/Scale;var dy=(y+oy+.5)/Scale;
            var sx=dx*cos+dy*sin;var sy=-dx*sin+dy*cos;
            var ix=(int)Math.Floor((sx-b.Left)/b.Width*source.Image.PixelWidth);var iy=(int)Math.Floor((sy-b.Top)/b.Height*source.Image.PixelHeight);
            if(ix>=0 && iy>=0 && ix<source.Image.PixelWidth && iy<source.Image.PixelHeight)result[y*width+x]=pixels[iy*source.Image.PixelWidth+ix];
        }
        return new(width,height,ox,oy,result);
    }
    private Sprite Ellipse(double rx,double ry,uint color,bool ring=false)
    {
        var ox=-(int)Math.Ceiling((rx+1)*Scale);var oy=-(int)Math.Ceiling((ry+1)*Scale);var width=-ox*2;var height=-oy*2;var pixels=new int[width*height];
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        { var dx=(x+ox+.5)/Scale/rx;var dy=(y+oy+.5)/Scale/ry;var radius=dx*dx+dy*dy;if(radius<=1 && (!ring || radius>=.72))pixels[y*width+x]=unchecked((int)color); }
        return new(width,height,ox,oy,pixels);
    }
    internal static void Blit(Sprite sprite,int[] buffer,int width,int height,int x,int y,int opacity=255,double scaleX=1,double scaleY=1)
    {
        var left=x+(int)Math.Round(sprite.X*scaleX);var top=y+(int)Math.Round(sprite.Y*scaleY);
        var drawWidth=Math.Max(1,(int)Math.Round(sprite.Width*scaleX));var drawHeight=Math.Max(1,(int)Math.Round(sprite.Height*scaleY));
        var x0=Math.Max(0,-left);var x1=Math.Min(drawWidth,width-left);var y0=Math.Max(0,-top);var y1=Math.Min(drawHeight,height-top);
        if(x0>=x1 || y0>=y1)return;
        var unscaled=scaleX==1 && scaleY==1;
        for(var row=y0;row<y1;row++)
        {
            var sy=unscaled ? row : Math.Min(sprite.Height-1,(int)(row/scaleY));var source=sy*sprite.Width;var dest=(top+row)*width+left;
            for(var col=x0;col<x1;col++)
            {
                var sx=unscaled ? col : Math.Min(sprite.Width-1,(int)(col/scaleX));
                var s=unchecked((uint)sprite.Pixels[source+sx]);var a=(int)(s>>24);if(a==0)continue;
                if(opacity<255)
                {
                    a=a*opacity/255;
                    s=(uint)(a<<24 | ((int)(s>>16)&255)*opacity/255<<16 | ((int)(s>>8)&255)*opacity/255<<8 | ((int)s&255)*opacity/255);
                }
                if(a==255) { buffer[dest+col]=unchecked((int)s);continue; }
                var d=unchecked((uint)buffer[dest+col]);var inverse=255-a;
                var blue=(int)(s&255)+((int)(d&255)*inverse+127)/255;
                var green=(int)(s>>8&255)+((int)(d>>8&255)*inverse+127)/255;
                var red=(int)(s>>16&255)+((int)(d>>16&255)*inverse+127)/255;
                var alpha=a+((int)(d>>24)*inverse+127)/255;
                buffer[dest+col]=alpha<<24 | red<<16 | green<<8 | blue;
            }
        }
    }
}
