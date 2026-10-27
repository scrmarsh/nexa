using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed record NationHit(Guid NationId,int ComponentKey,bool MainLabel);
internal sealed class LivingNationView
{
    private sealed record MapInk(Guid Owner,Geometry Fill,Geometry Border,Geometry Coast,Geometry Conflict);
    private sealed record CachedChunk(long Revision,List<MapInk> Drawings);
    private sealed record NameInk(DrawingGroup Drawing,double Width,double Height);
    private readonly LivingWorldSession session;
    private readonly BugOverlay overlay;
    private LivingNationWorld? current;
    private readonly Dictionary<int,CachedChunk> cache=[];
    private readonly Dictionary<(string,double,double),Geometry> text=[];
    private readonly Dictionary<uint,SolidColorBrush> brushes=[];
    private readonly Dictionary<(uint,double,bool),Pen> pens=[];
    private readonly Dictionary<(string,double),NameInk> names=[];
    private readonly List<(Rect Bounds,NationHit Hit)> labels=[];
    private readonly List<(Rect Bounds,NationHit Hit)> letters=[];
    internal IEnumerable<Rect> LabelBounds=>labels.Select(l=>l.Bounds);
    internal NationHit? HitLetter(Point screen)=>labelCamera==overlay.Camera ? letters.Where(l=>l.Bounds.Contains(screen)).Select(l=>l.Hit).FirstOrDefault() : null;
    private Matrix labelCamera;
    internal Guid? Selected;
    internal bool Enabled=true;
    internal int Rebuilds {get;private set;}
    internal double RebuildMilliseconds {get;private set;}
    internal LivingNationView(LivingWorldSession session,BugOverlay overlay) {this.session=session;this.overlay=overlay;}
    private SolidColorBrush Brush(uint color)
    {if(!brushes.TryGetValue(color,out var b)){b=new SolidColorBrush(Color.FromArgb((byte)(color>>24),(byte)(color>>16),(byte)(color>>8),(byte)color));b.Freeze();brushes[color]=b;}return b;}
    private SolidColorBrush Tint(uint color,double alpha)=>Brush((color&0xffffff)|((uint)(byte)(alpha*255)<<24));
    private Pen Pen(uint color,double width,bool dashed=false)
    {var key=(color,width,dashed);if(!pens.TryGetValue(key,out var pen)){pen=new Pen(Brush(color),width);if(dashed)pen.DashStyle=DashStyles.Dash;pen.Freeze();if(pens.Count>=512)pens.Clear();pens[key]=pen;}return pen;}
    private static void Rectangle(StreamGeometryContext dc,Rect r)
    {dc.BeginFigure(r.TopLeft,true,true);dc.LineTo(r.TopRight,true,false);dc.LineTo(r.BottomRight,true,false);dc.LineTo(r.BottomLeft,true,false);}
    private static void Segment(StreamGeometryContext dc,Point a,Point b) {dc.BeginFigure(a,false,false);dc.LineTo(b,true,false);}
    private CachedChunk Build(LivingNationWorld world,int chunkX,int chunkY,long revision)
    {
        var start=Stopwatch.GetTimestamp();var land=world.Land;var cells=new Dictionary<Guid,List<int>>();
        for(var y=chunkY*16;y<Math.Min(land.Rows,(chunkY+1)*16);y++)for(var x=chunkX*16;x<Math.Min(land.Columns,(chunkX+1)*16);x++)
        {var cell=y*land.Columns+x;if(world.Owner(cell) is {} id){if(!cells.TryGetValue(id,out var list))cells[id]=list=[];list.Add(cell);}}
        var drawings=new List<MapInk>();var unit=land.Info.TileSize;
        foreach(var pair in cells)
        {
            var fill=new StreamGeometry();var border=new StreamGeometry();var coast=new StreamGeometry();var conflict=new StreamGeometry();
            using(var f=fill.Open())using(var b=border.Open())using(var c=coast.Open())using(var contested=conflict.Open())
            foreach(var cell in pair.Value)
            {
                var tx=cell%land.Columns*8;var ty=cell/land.Columns*8;var mask=land.Ink[cell];
                if(mask==ulong.MaxValue) {var rect=new Rect(tx*unit,ty*unit,8*unit,8*unit);Rectangle(f,rect);if(world.IsContested(cell))Rectangle(contested,rect);}
                else for(var y=0;y<8;y++)for(var x=0;x<8;)
                {if((mask&(1UL<<(y*8+x)))==0){x++;continue;}var left=x;while(x<8 && (mask&(1UL<<(y*8+x)))!=0)x++;var rect=new Rect((tx+left)*unit,(ty+y)*unit,(x-left)*unit,unit);Rectangle(f,rect);if(world.IsContested(cell))Rectangle(contested,rect);}
                for(var y=0;y<8;y++)for(var x=0;x<8;x++)
                {
                    if((land.Land[cell]&(1UL<<(y*8+x)))==0)continue;
                    var gx=tx+x;var gy=ty+y;
                    for(var edge=0;edge<4;edge++)
                    {
                        var nx=gx+(edge==0 ? -1 : edge==1 ? 1 : 0);var ny=gy+(edge==2 ? -1 : edge==3 ? 1 : 0);var isLand=land.IsLand(nx,ny);
                        if(isLand && world.Owner(land.Cell(nx,ny))==pair.Key)continue;
                        var a=edge switch {0=>new Point(gx*unit,gy*unit),1=>new Point((gx+1)*unit,gy*unit),2=>new Point(gx*unit,gy*unit),_=>new Point(gx*unit,(gy+1)*unit)};
                        var end=edge<2 ? a+new Vector(0,unit) : a+new Vector(unit,0);Segment(isLand ? b : c,a,end);
                    }
                }
            }
            fill.Freeze();border.Freeze();coast.Freeze();conflict.Freeze();drawings.Add(new(pair.Key,fill,border,coast,conflict));
        }
        Rebuilds++;RebuildMilliseconds+=Stopwatch.GetElapsedTime(start).TotalMilliseconds;return new(revision,drawings);
    }
    internal void Draw(DrawingContext dc,Rect view)
    {
        var world=session.Nations;labels.Clear();letters.Clear();labelCamera=overlay.Camera;if(world==null || !Enabled)return;
        if(!ReferenceEquals(world,current)){cache.Clear();text.Clear();names.Clear();current=world;}
        var land=world.Land;var chunkUnit=128*land.Info.TileSize;var cols=(land.Columns+15)/16;var rows=(land.Rows+15)/16;
        var zoom=Math.Max(.005,overlay.Camera.M11);var opacity=zoom<.35 ? .24 : zoom<1 ? .14 : .065;var options=overlay.Manager.BattleOptions;
        dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));
        for(var y=Math.Clamp((int)(view.Top/chunkUnit),0,rows-1);y<=Math.Clamp((int)(view.Bottom/chunkUnit),0,rows-1);y++)for(var x=Math.Clamp((int)(view.Left/chunkUnit),0,cols-1);x<=Math.Clamp((int)(view.Right/chunkUnit),0,cols-1);x++)
        {
            long rev=0;for(var cy=y*16;cy<Math.Min(land.Rows,(y+1)*16);cy++)for(var cx=x*16;cx<Math.Min(land.Columns,(x+1)*16);cx++)rev=Math.Max(rev,world.CellRevisions[cy*land.Columns+cx]);
            var key=y*cols+x;if(!cache.TryGetValue(key,out var chunk) || chunk.Revision!=rev)cache[key]=chunk=Build(world,x,y,rev);
            foreach(var ink in chunk.Drawings)
            {
                var nation=world.Find(ink.Owner);if(nation==null)continue;var color=Brush(nation.Color);
                if(options.ShowTerritories)dc.DrawGeometry(Tint(nation.Color,opacity),null,ink.Fill);
                var selected=Selected==ink.Owner;var pen=Pen((selected ? 0xF2ffffffu : (nation.Color&0xffffff)|0xb2000000u),(selected ? 2 : zoom>1 ? .45 : 1)/zoom);
                if(options.ShowTerritoryBorders){dc.DrawGeometry(null,pen,ink.Border);if(selected)dc.DrawGeometry(null,Pen((nation.Color&0xffffff)|0x7f000000u,1.2/zoom),ink.Coast);}
                if(options.HighlightContested)dc.DrawGeometry(null,Pen(nation.Color,.7/zoom,true),ink.Conflict);
            }
        }
        dc.Pop();if(zoom>1.15 || !options.ShowFactionLabels)return;
        var inverse=overlay.Camera;inverse.Invert();dc.PushTransform(new MatrixTransform(inverse));
        var candidates=world.Nations.Where(n=>n.Status==NationExistence.Active).OrderByDescending(n=>n.Id==Selected).ThenByDescending(n=>world.Area(n.Id));
        foreach(var n in candidates)
        {
            var component=world.Components(n.Id).FirstOrDefault();if(component==null)continue;var point=overlay.WorldToScreen(component.Anchor);var size=zoom<.35 ? 29d : 24d;
            if(overlay.StrategicMap && !new Rect(-100,-80,overlay.ActualWidth+200,overlay.ActualHeight+160).Contains(point))continue;
            var letter=Text(LivingSpecies.Letter(n.Species),size);var name=Name(n.Name);
            if(overlay.StrategicMap && world.Expansion==null){var half=Math.Max(letter.Bounds.Width,name.Width)/2+5;point.X=Math.Clamp(point.X,half,Math.Max(half,overlay.ActualWidth-half));point.Y=Math.Clamp(point.Y,size/2+5,Math.Max(size/2+5,overlay.ActualHeight-size/2-name.Height-5));}
            var bounds=new Rect(point.X-Math.Max(letter.Bounds.Width,name.Width)/2-4,point.Y-size/2-4,Math.Max(letter.Bounds.Width,name.Width)+8,size+name.Height+8);
            if(!new Rect(0,0,overlay.ActualWidth,overlay.ActualHeight).IntersectsWith(bounds) || labels.Any(l=>l.Bounds.IntersectsWith(bounds)))continue;
            var hit=new NationHit(n.Id,component.Key,true);labels.Add((bounds,hit));
            letters.Add((new Rect(point.X-letter.Bounds.Width/2+letter.Bounds.X,point.Y-size/2+letter.Bounds.Y,letter.Bounds.Width,letter.Bounds.Height),hit));
            DrawText(letter,new Point(point.X-letter.Bounds.Width/2,point.Y-size/2),Brush(n.Color));
            dc.PushTransform(new TranslateTransform(point.X-name.Width/2,point.Y+size/2));dc.DrawDrawing(name.Drawing);dc.Pop();
        }
        dc.Pop();
        void DrawText(Geometry geometry,Point p,Brush fill){dc.PushTransform(new TranslateTransform(p.X,p.Y));dc.DrawGeometry(null,Pen(0xff000000,3),geometry);dc.DrawGeometry(fill,null,geometry);dc.Pop();}
    }
    private NameInk Name(string value)
    {
        var dpi=VisualTreeHelper.GetDpi(overlay).PixelsPerDip;var key=(value,dpi);if(names.TryGetValue(key,out var cached))return cached;
        FormattedText Make(Brush brush)=>new(value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,brush,dpi){MaxTextWidth=190,MaxLineCount=1,Trimming=TextTrimming.CharacterEllipsis};
        var black=Make(Brushes.Black);var white=Make(Brushes.White);var group=new DrawingGroup();
        using(var dc=group.Open()){dc.DrawText(black,new Point(-1,0));dc.DrawText(black,new Point(1,0));dc.DrawText(black,new Point(0,-1));dc.DrawText(black,new Point(0,1));dc.DrawText(white,new Point());}group.Freeze();
        if(names.Count>=1024)names.Clear();return names[key]=new(group,white.Width,white.Height);
    }
    private Geometry Text(string value,double size)
    {
        var dpi=VisualTreeHelper.GetDpi(overlay).PixelsPerDip;var key=(value,size,dpi);
        if(!text.TryGetValue(key,out var geometry))
        {var t=new FormattedText(value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.Bold,FontStretches.Normal),size,Brushes.White,dpi){MaxTextWidth=190,MaxLineCount=1,Trimming=TextTrimming.CharacterEllipsis};geometry=t.BuildGeometry(new Point());geometry.Freeze();if(text.Count>2048)text.Clear();text[key]=geometry;}
        return geometry;
    }
    internal NationHit? Hit(Point screen)
    {
        if(labelCamera==overlay.Camera)foreach(var label in labels)if(label.Bounds.Contains(screen))return label.Hit;
        var world=session.Nations;if(world==null)return null;var point=overlay.ScreenToWorld(screen);var id=world.OwnerAt(point);if(id==null)return null;
        var tile=(int)(point.Y/world.Land.Info.TileSize)*world.Land.Info.Tiles+(int)(point.X/world.Land.Info.TileSize);
        var c=world.Components(id.Value).FirstOrDefault(c=>c.ContainsTile(tile,world.Land.Info.Tiles,world.Land.Columns));return c==null ? null : new(id.Value,c.Key,false);
    }
    internal bool FillContains(Point world)=>cache.Values.SelectMany(c=>c.Drawings).Any(d=>d.Fill.FillContains(world));
    internal bool BorderContains(Point world)=>cache.Values.SelectMany(c=>c.Drawings).Any(d=>d.Border.StrokeContains(new Pen(Brushes.Black,.2),world));
}
