using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SessionDashboard;

// Frozen drawings keep the render loop to one sprite per bug. Crawl poses are shared.
internal static class BugArtwork
{
    internal static readonly Brush Ant = Solid("#654B37"), Roach = Solid("#39251B"), Fly = Solid("#424B50"), Wing = Solid("#BCCED9");
    private static readonly Brush Lady = Solid("#DA433D"), HurtLady = Solid("#9E3932"), Centipede = Solid("#4F3B30");
    private static readonly Pen Outline = Ink("#E1D1A7",.6), Legs = Ink("#806D56",1), Shell = Ink("#242933",.8), Crack = Ink("#FFE7A7",1.3);
    internal static readonly Pen HitRing = Ink("#FFE7A7",1.3);
    private static readonly Drawing[] Bodies = MakeBodies();
    private static readonly Drawing HurtBody = Make(BugSpecies.Ladybugs,0,true);
    private static readonly Drawing[] Crawl = Enumerable.Range(0,8).Select(frame=>Make(BugSpecies.Centipedes,frame,false)).ToArray();
    internal static readonly Drawing Web = MakeWeb();
    internal readonly record struct Sprite(BitmapSource Image, Rect Bounds);
    private static readonly Sprite[] BodySprites = Bodies.Select(Rasterize).ToArray();
    private static readonly Sprite HurtSprite = Rasterize(HurtBody);
    private static readonly Sprite[] CrawlSprites = Crawl.Select(Rasterize).ToArray();
    internal static readonly Sprite WebSprite = Rasterize(Web);
    private static readonly Dictionary<(BugSpecies,int,bool,BugVariant),Sprite> Specials=[];
    internal static Sprite Source(BugSpecies species,int pose,bool hurt,BugVariant variant)
    {
        if(variant==BugVariant.None)return Source(species,pose,hurt);
        var key=(species,pose,hurt,variant);
        if(Specials.TryGetValue(key,out var sprite))return sprite;
        var group=new DrawingGroup();
        using(var dc=group.Open())
        {
            dc.DrawDrawing(species==BugSpecies.Centipedes ? Crawl[pose&7] : species==BugSpecies.Ladybugs && hurt ? HurtBody : Bodies[(int)species]);
            var royal=Ink(variant==BugVariant.KingCentipede ? "#EB6656" : variant==BugVariant.MamaSpider ? "#B69AFF" : "#FFD774",1);
            if(variant==BugVariant.KingCentipede)
            {
                for(var x=-12;x<12;x+=4)
                { dc.DrawLine(royal,new Point(x,-2),new Point(x-2,-5)); dc.DrawLine(royal,new Point(x,2),new Point(x-2,5)); }
                dc.DrawLine(royal,new Point(15,-1),new Point(20,-3)); dc.DrawLine(royal,new Point(15,1),new Point(20,3));
            }
            else
            {
                dc.DrawEllipse(null,royal,new Point(-2,0),species==BugSpecies.Roaches ? 4 : 3,2);
                for(var x=-3;x<=3;x+=3)dc.DrawLine(royal,new Point(x,-3),new Point(x+1,-6));
            }
            dc.DrawEllipse(royal.Brush,null,new Point(species==BugSpecies.Centipedes ? 14 : 4,-1.2),.8,.8);
        }
        group.Freeze(); sprite=Rasterize(group); Specials.Add(key,sprite); return sprite;
    }
    internal static Sprite Source(BugSpecies species,int pose,bool hurt) => species == BugSpecies.Centipedes ? CrawlSprites[pose & 7] : species == BugSpecies.Ladybugs && hurt ? HurtSprite : BodySprites[(int)species];
    internal static Sprite SpriteFor(BugEntity bug) => bug.Species == BugSpecies.Centipedes ? CrawlSprites[(int)bug.CrawlPhase & 7]
        : bug.Species == BugSpecies.Ladybugs && bug.Hits>0 ? HurtSprite : BodySprites[(int)bug.Species];
    private static Sprite Rasterize(Drawing drawing)
    {
        var bounds=drawing.Bounds; bounds.Inflate(1,1);
        var visual=new DrawingVisual();
        using(var dc=visual.RenderOpen())
        { dc.PushTransform(new ScaleTransform(2,2)); dc.PushTransform(new TranslateTransform(-bounds.X,-bounds.Y)); dc.DrawDrawing(drawing); dc.Pop(); dc.Pop(); }
        var image=new RenderTargetBitmap((int)Math.Ceiling(bounds.Width*2),(int)Math.Ceiling(bounds.Height*2),96,96,PixelFormats.Pbgra32);
        image.Render(visual); image.Freeze(); return new(image,bounds);
    }
    private static Brush Solid(string hex) { var b=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); return b; }
    private static Pen Ink(string hex,double width) { var p=new Pen(Solid(hex),width); p.Freeze(); return p; }
    private static Drawing[] MakeBodies() => Enum.GetValues<BugSpecies>().Select(species=>Make(species,0,false)).ToArray();
    internal static Drawing For(BugEntity bug) => bug.Species == BugSpecies.Centipedes ? Crawl[(int)bug.CrawlPhase & 7]
        : bug.Species == BugSpecies.Ladybugs && bug.Hits>0 ? HurtBody : Bodies[(int)bug.Species];
    private static Drawing Make(BugSpecies species,int frame,bool hurt,bool limbs=true)
    {
        var group=new DrawingGroup();
        using(var dc=group.Open())
        {
            if(species==BugSpecies.Spiders)
            {
                if(limbs)SpiderAnatomy.Legs(dc,Legs);
                SpiderAnatomy.Body(dc,Solid("#312B38"),Solid("#51445E"),Shell);
            }
            else if(species == BugSpecies.Centipedes)
            {
                for(var segment=0;segment<15;segment++)
                {
                    var x=-15+segment*2.15;
                    var wave=Math.Sin(frame*Math.PI/4-segment*.7);
                    var y=wave*.9;
                    if(limbs) for(var side=-1;side<=1;side+=2)
                    {
                        var stride=Math.Sin(frame*Math.PI/4-segment*.8+ (side==1 ? Math.PI : 0));
                        var joint=new Point(x+stride*1.7,y+side*4.5);
                        dc.DrawLine(Legs,new Point(x,y),joint);
                        dc.DrawLine(Legs,joint,new Point(x+stride*2.8-1,y+side*7));
                    }
                    dc.DrawEllipse(Centipede,Outline,new Point(x,y),1.7,2.5);
                }
                dc.DrawEllipse(Roach,Shell,new Point(17,0),2.8,3);
                if(limbs) dc.DrawLine(Legs,new Point(18,-1),new Point(24,-5));
                if(limbs) dc.DrawLine(Legs,new Point(18,1),new Point(24,5));
                dc.DrawEllipse(Fly,null,new Point(14,-1.2),.6,.6); dc.DrawEllipse(Fly,null,new Point(14,1.2),.6,.6);
            }
            else
            {
                var length=species == BugSpecies.Roaches ? 11 : 5;
                if(species == BugSpecies.Spiders)
                {
                    if(limbs) for(var leg=0;leg<4;leg++) for(var side=-1;side<=1;side+=2)
                    { var x=1+leg*1.55; dc.DrawLine(Shell,new Point(x,side*2),new Point(x-2,side*6)); dc.DrawLine(Shell,new Point(x-2,side*6),new Point(x+1,side*12)); }
                }
                else if(limbs) for(var leg=-1;leg<=1;leg++)
                { dc.DrawLine(Legs,new Point(leg*3,-2),new Point(leg*4-2,-5)); dc.DrawLine(Legs,new Point(leg*3,2),new Point(leg*4-2,5)); }
                if(species == BugSpecies.Flies && limbs)
                { dc.DrawEllipse(Wing,Outline,new Point(-1,-4),4,2.5); dc.DrawEllipse(Wing,Outline,new Point(-1,4),4,2.5); }
                if(species == BugSpecies.Ladybugs)
                {
                    dc.DrawEllipse(hurt ? HurtLady : Lady,Shell,new Point(-1,0),6.5,5.5);
                    dc.DrawLine(Shell,new Point(-7,0),new Point(5,0));
                    foreach(var x in new[] { -4d,0d,3d })
                    { dc.DrawEllipse(Fly,null,new Point(x,-2.7),1,1); dc.DrawEllipse(Fly,null,new Point(x,2.7),1,1); }
                    if(hurt)
                    { dc.DrawLine(Crack,new Point(-5,-4),new Point(-1,-1)); dc.DrawLine(Crack,new Point(-1,-1),new Point(-3,1)); dc.DrawLine(Crack,new Point(-3,1),new Point(1,4)); }
                }
                else dc.DrawEllipse(species==BugSpecies.Ants ? Ant : species==BugSpecies.Roaches ? Roach : species==BugSpecies.Spiders ? Brushes.Black : Fly,Outline,new Point(-2,0),length*.7,species is BugSpecies.Roaches or BugSpecies.Spiders ? 4 : 2.5);
                dc.DrawEllipse(Fly,Outline,new Point(length-2,0),2,2);
                if(limbs) {dc.DrawLine(Legs,new Point(length,-1),new Point(length+(species==BugSpecies.Spiders?1.5:3),species==BugSpecies.Spiders?-2:-4)); dc.DrawLine(Legs,new Point(length,1),new Point(length+(species==BugSpecies.Spiders?1.5:3),species==BugSpecies.Spiders?2:4));}
            }
        }
        group.Freeze(); return group;
    }
    private static readonly Dictionary<(BugSpecies,bool),Drawing> bodyOnly=[];
    internal static Drawing BodyOnly(BugEntity bug)
    {
        var key=(bug.Species,bug.Species==BugSpecies.Ladybugs && bug.Hits>0);
        if(!bodyOnly.TryGetValue(key,out var art)){art=Make(key.Item1,0,key.Item2,false);bodyOnly[key]=art;}return art;
    }
    private static Drawing MakeWeb()
    {
        var group=new DrawingGroup(); var pen=Ink("#B4BCCB",.6);
        using(var dc=group.Open())
        {
            for(var spoke=0;spoke<12;spoke++)
            { var a=spoke*Math.PI/6; dc.DrawLine(pen,new Point(),new Point(Math.Cos(a)*240,Math.Sin(a)*240)); }
            for(var ring=1;ring<=9;ring++) for(var side=0;side<12;side++)
            { var a=side*Math.PI/6;var b=(side+1)*Math.PI/6;var r=ring*240/9d;dc.DrawLine(pen,new Point(Math.Cos(a)*r,Math.Sin(a)*r),new Point(Math.Cos(b)*r,Math.Sin(b)*r)); }
            dc.DrawEllipse(null,pen,new Point(-12,7),20,15);
            dc.DrawEllipse(null,pen,new Point(-8,4),13,10);
        }
        group.Freeze();return group;
    }
}
