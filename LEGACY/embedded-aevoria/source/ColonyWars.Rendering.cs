using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal sealed partial class BugOverlay
{
    private static readonly Pen WebInk=new(Brushes.SlateGray,.65);
    private static readonly Brush HudBackground=new SolidColorBrush(Color.FromArgb(250,11,16,29));
    private static readonly Brush[] ColonyColors=[Brushes.Goldenrod,Brushes.Peru,Brushes.DeepSkyBlue,Brushes.Gray,Brushes.MediumPurple,Brushes.Gray,Brushes.Salmon,Brushes.MediumSeaGreen];
    private readonly Dictionary<string,FormattedText> battleText=[];
    private readonly Dictionary<(BugSpecies,int,bool,double),BugArtwork.Sprite> battleSprites=[];
    internal long SpriteRasterizations {get;private set;}
    private double battleSpriteDpi;
    private BugArtwork.Sprite BattleSprite(BugEntity bug)
    {
        var dpi=VisualTreeHelper.GetDpi(this).DpiScaleX;
        if(dpi!=battleSpriteDpi) {battleSprites.Clear();battleSpriteDpi=dpi;}
        // Rasterize each shared pose at or above its actual device-pixel size. No downsampling of the overlay.
        var resolution=Math.Ceiling(dpi*bug.SizeScale*2)/2;
        var pose=bug.Species==BugSpecies.Centipedes ? (int)bug.CrawlPhase&7 : 0;
        var key=(bug.Species,pose,bug.Hits>0,resolution);
        if(battleSprites.TryGetValue(key,out var sprite))return sprite;
        SpriteRasterizations++;
        var drawing=BugArtwork.For(bug);var bounds=drawing.Bounds;bounds.Inflate(1,1);
        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())
        {dc.PushTransform(new ScaleTransform(resolution,resolution));dc.PushTransform(new TranslateTransform(-bounds.X,-bounds.Y));dc.DrawDrawing(drawing);dc.Pop();dc.Pop();}
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(bounds.Width*resolution)),Math.Max(1,(int)Math.Ceiling(bounds.Height*resolution)),96,96,PixelFormats.Pbgra32);
        bitmap.Render(visual);bitmap.Freeze();sprite=new BugArtwork.Sprite(bitmap,bounds);
        if(battleSprites.Count>=128)battleSprites.Clear();battleSprites[key]=sprite;return sprite;
    }
    private double battleTextDpi;
    private double BattleOpacity => Manager.Phase==BattlePhase.Victory && Manager.CurrentAnnouncement?.Victory==true ? Math.Clamp((6-Manager.AnnouncementAge)/1.2,0,1) : 1;
    private Brush Accent(BugSpecies species)=>Manager.IsInfinite && Manager.Colonies.TryGetValue(species,out var faction) ? FactionBrush(faction.Id) : ColonyColors[(int)species];
    private FormattedText BattleText(string value,double size,Brush brush)
    {
        var dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if(dpi!=battleTextDpi) {battleText.Clear();battleTextDpi=dpi;}
        var key=value+"|"+size.ToString(CultureInfo.InvariantCulture)+"|"+brush;
        if(!battleText.TryGetValue(key,out var text))
        {
            if(battleText.Count>300)battleText.Clear();
            text=new FormattedText(value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,brush,dpi);battleText[key]=text;
        }
        return text;
    }
    private void DrawCombatUnit(DrawingContext dc,BugEntity b,Point p)
    {
        if(!BugManager.Alive(b))return;
        var color=Accent(b.Species);
        if(b.IsLeader)
        {
            // Crown markings remain vector artwork and scale cleanly with monitor DPI.
            dc.DrawEllipse(null,new Pen(color,1.5),p,12*b.SizeScale,10*b.SizeScale);
            var y=Math.Max(ContentOrigin.Y+19,p.Y-20*b.SizeScale);
            dc.DrawRectangle(HudBackground,null,new Rect(p.X-21,y,42,4));
            dc.DrawRectangle(color,null,new Rect(p.X-21,y,42*Math.Clamp(b.Health/b.MaxHealth,0,1),4));
            var title=BattleText((Manager.Civilization?.Title(b.UnitId)??BugManager.LeaderTitle(b.Species))+(b.Name is {} name ? " \u00b7 "+name : "")+(b.LegendaryTitle is {} honor ? " "+honor : ""),10,color);
            dc.DrawText(title,new Point(Math.Clamp(p.X-title.Width/2,ContentOrigin.X,Math.Max(ContentOrigin.X,ContentOrigin.X+Manager.BattleArena.Width-title.Width)),y-15));
        }
        if(b.Protection>0)dc.DrawEllipse(null,new Pen(Brushes.LightSkyBlue,.7),p,13*b.SizeScale,11*b.SizeScale);
        if(b.SlowTimer>0) {dc.DrawLine(WebInk,p-new Vector(7,7),p+new Vector(7,7));dc.DrawLine(WebInk,p-new Vector(7,-7),p+new Vector(7,-7));}
        if(b.AttackFlash>0 && b.AttackVictim is {} enemy)dc.DrawLine(new Pen(color,1.4),p,enemy.Position+(Vector)ContentOrigin);
        if(b.HealFlash>0)dc.DrawEllipse(null,new Pen(Brushes.LightGreen,1),p,12*b.SizeScale,12*b.SizeScale);
    }
    private void DrawCombatProjectiles(DrawingContext dc)
    {
        foreach(var p in Manager.Projectiles)
        {
            dc.DrawLine(new Pen(Brushes.LightSlateGray,1.2),p.Previous+(Vector)ContentOrigin,p.Position+(Vector)ContentOrigin);
            dc.DrawEllipse(Brushes.GhostWhite,null,p.Position+(Vector)ContentOrigin,2.3,2.3);
        }
    }
    private void DrawBattleHud(DrawingContext dc)
    {
        if(!ShowHud)return;
        var arena=Manager.BattleArena;var origin=ContentOrigin;
        if(Manager.BattleOptions.ShowScoreboard && arena.Width>310)
        {
            var x=origin.X+arena.Width-298;var y=origin.Y+8;
            dc.PushOpacity(.96);dc.DrawRoundedRectangle(HudBackground,new Pen(Brushes.SlateGray,.5),new Rect(x,y,290,153),6,6);
            dc.DrawText(BattleText(Manager.IsInfinite ? $"INFINITE WARFARE \u00b7 {Manager.WorldTimeLabel}" : $"COLONY WARS \u00b7 MATCH {Manager.MatchNumber}",11,Brushes.White),new Point(x+9,y+7));
            var headings=new[]{("Colony",9d),("Alive",134d),("HP",180d),("Kills",226d),("Wins",261d)};
            foreach(var h in headings)dc.DrawText(BattleText(h.Item1,10,Brushes.LightSlateGray),new Point(x+h.Item2,y+27));
            var row=0;
            foreach(var species in BugHabitats.Species)
            {
                if(!Manager.Colonies.TryGetValue(species,out var c))continue;
                var yy=y+44+row++*16;
                var name=BattleText(c.Name,10,Accent(species));name.MaxTextWidth=120;name.MaxLineCount=1;name.Trimming=TextTrimming.CharacterEllipsis;
                dc.DrawText(name,new Point(x+9,yy));
                dc.DrawText(BattleText(c.Eliminated ? "extinct" : c.Alive.ToString(),10,c.Eliminated ? Brushes.Gray : Brushes.White),new Point(x+134,yy));
                if(!c.Eliminated && BugManager.Alive(c.Leader))dc.DrawText(BattleText($"{c.Leader.Health/c.Leader.MaxHealth:P0}",10,Brushes.White),new Point(x+180,yy));
                dc.DrawText(BattleText(c.Kills.ToString(),10,Brushes.White),new Point(x+226,yy));
                dc.DrawText(BattleText(Manager.Victories.GetValueOrDefault(species).ToString(),10,Brushes.White),new Point(x+264,yy));
            }
            dc.Pop();
        }
        if(Manager.CurrentAnnouncement is not {} banner)return;
        var width=Math.Min(arena.Width-24,780);if(width<100)return;
        var t=Manager.AnnouncementAge;var opacity=Math.Clamp(t*5,0,1)*(banner.Victory ? 1 : Math.Clamp((3.5-t)*3,0,1));
        var accent=banner.Accent is {} speciesAccent ? Accent(speciesAccent) : Brushes.LightSlateGray;
        dc.PushOpacity(opacity);
        var bounds=new Rect(origin.X+(arena.Width-width)/2,origin.Y+(banner.Victory ? arena.Height*.36 : 18)+Math.Max(0,1-t*4)*-20,width,banner.Victory ? 85 : 62);
        dc.DrawRoundedRectangle(HudBackground,new Pen(accent,1.3),bounds,7,7);
        var text=BattleText(banner.Text,banner.Victory ? 19 : 14,accent);text.MaxTextWidth=width-30;text.TextAlignment=TextAlignment.Center;text.MaxTextHeight=bounds.Height-10;
        dc.DrawText(text,new Point(bounds.X+15,bounds.Y+(bounds.Height-text.Height)/2));
        dc.Pop();
    }
}
