using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed class LivingSettlementView(LivingWorldSession session,BugOverlay overlay)
{
    private readonly Dictionary<Guid,(long Revision,DrawingGroup Drawing)> cache=[];
    private readonly Dictionary<(string,double),FormattedText> text=[];
    private readonly List<(Rect Bounds,Guid Id)> hits=[];
    private Matrix camera;
    internal int VisibleSettlements {get;private set;}
    internal int GeometryBuilds {get;private set;}
    private static Brush Ink(Color color){var brush=new SolidColorBrush(color);brush.Freeze();return brush;}
    private DrawingGroup Build(CivicSettlement s,LivingCivilization world)
    {
        var group=new DrawingGroup();var style=s.StyleId;
        var material=Ink(style switch{"theme:roman"=>Color.FromRgb(205,179,143),"theme:greek"=>Color.FromRgb(225,225,211),"theme:french"=>Color.FromRgb(151,163,174),"theme:mongol"=>Color.FromRgb(214,183,122),"theme:japanese"=>Color.FromRgb(138,93,61),_=>Color.FromRgb(224,189,103)});
        var line=new Pen(Brushes.Black,.65);line.Freeze();var roof=Ink(style=="theme:greek" ? Color.FromRgb(85,134,173) : style=="theme:japanese" ? Color.FromRgb(137,48,43) : Color.FromRgb(149,80,54));
        using(var dc=group.Open())foreach(var b in world.State.Buildings.Where(b=>b.SettlementId==s.Id && !b.Cancelled && b.Condition>0))
        {
            var p=b.Position;var x=p.X;var y=p.Y;var size=b.Purpose is CivicPurpose.Government or CivicPurpose.Monument ? 9d:6d;
            if(!b.Completed){dc.DrawRectangle(null,new Pen(Brushes.Wheat,.8),new Rect(x-size,y-size,size*2,size*2));dc.DrawRectangle(Brushes.Goldenrod,null,new Rect(x-size,y+size,size*2*((int)(b.Progress*4)/4d),1.5));continue;}
            if(overlay.StrategicMap && world.Economy?.Settlement(s.Id) is {} finance && b.InvestmentCents>0)
            {
                if(b.Purpose==CivicPurpose.Infrastructure){dc.DrawLine(new Pen(Brushes.Tan,3),s.Position,p);dc.DrawLine(new Pen(Brushes.Wheat,.5),s.Position,p);}
                if(finance.Tier>=ProsperityTier.Wealthy && b.Condition>.75){dc.DrawRectangle(Brushes.Wheat,null,new Rect(x-size-2,y+size,size*2+4,2));dc.DrawLine(new Pen(Brushes.Gold,1),new Point(x+size-1,y-size),new Point(x+size-1,y-size-5));dc.DrawRectangle(roof,null,new Rect(x+size-1,y-size-5,4,2));}
            }
            if(b.Purpose==CivicPurpose.Infrastructure && style=="theme:roman"){dc.DrawRectangle(material,line,new Rect(x-11,y-2,22,4));for(var i=-8;i<9;i+=4)dc.DrawLine(line,new Point(x+i,y-2),new Point(x+i,y+2));continue;}
            if(style=="theme:mongol")
            {dc.DrawEllipse(material,line,p,size,size*.65);Triangle(dc,roof,line,new(x-size,y),new(x,y-size),new(x+size,y));dc.DrawLine(line,new Point(x,y-2),new Point(x,y+size*.6));}
            else if(style=="theme:japanese")
            {dc.DrawRectangle(material,line,new Rect(x-size*.65,y-size*.5,size*1.3,size*1.5));for(var i=0;i<3;i++)Triangle(dc,roof,line,new(x-size*(1-i*.17),y-i*3),new(x,y-5-i*3),new(x+size*(1-i*.17),y-i*3));}
            else if(style=="theme:egyptian" && b.Purpose==CivicPurpose.Monument)Triangle(dc,material,line,new(x-size,y+size),new(x,y-size*1.6),new(x+size,y+size));
            else if(style=="theme:french" && b.Purpose is CivicPurpose.Defense or CivicPurpose.Government)
            {dc.DrawRectangle(material,line,new Rect(x-size,y-size,size*2,size*2));for(var i=-1;i<=1;i++)dc.DrawRectangle(material,line,new Rect(x+i*size-size*.2,y-size*1.4,size*.4,size*.65));}
            else
            {dc.DrawRectangle(material,line,new Rect(x-size,y-size*.3,size*2,size*1.3));Triangle(dc,roof,line,new(x-size*1.1,y-size*.3),new(x,y-size),new(x+size*1.1,y-size*.3));if(style is "theme:roman" or "theme:greek" or "theme:egyptian")for(var i=-1;i<=1;i++)dc.DrawLine(new Pen(Brushes.Ivory,1.4),new Point(x+i*size*.65,y),new Point(x+i*size*.65,y+size*.8));}
            if(b.Condition<.5)dc.DrawLine(new Pen(Brushes.DarkRed,1.3),new Point(x-size,y-size),new Point(x+size,y+size));
        }
        group.Freeze();GeometryBuilds++;return group;
    }
    private static void Triangle(DrawingContext dc,Brush fill,Pen pen,Point a,Point b,Point c)
    {var g=new StreamGeometry();using(var s=g.Open()){s.BeginFigure(a,true,true);s.LineTo(b,true,false);s.LineTo(c,true,false);}g.Freeze();dc.DrawGeometry(fill,pen,g);}
    internal void Draw(DrawingContext dc,Rect view)
    {
        hits.Clear();VisibleSettlements=0;var world=session.Civilization;if(world==null)return;camera=overlay.Camera;var zoom=Math.Max(.001,overlay.Camera.M11);
        var visible=world.State.Settlements.Where(s=>s.Active && new Rect(s.Position.X-80,s.Position.Y-80,160,160).IntersectsWith(view)).ToArray();VisibleSettlements=visible.Length;
        dc.PushTransform(new TranslateTransform(overlay.ContentOrigin.X,overlay.ContentOrigin.Y));
        var architecture=overlay.StrategicMap ? overlay.MapDetail>=MapDetail.Local : zoom>=.45;
        if(architecture)foreach(var s in visible){if(!cache.TryGetValue(s.Id,out var cached) || cached.Revision!=s.VisualRevision)cache[s.Id]=cached=(s.VisualRevision,Build(s,world));dc.DrawDrawing(cached.Drawing);}
        dc.Pop();var inverse=overlay.Camera;inverse.Invert();dc.PushTransform(new MatrixTransform(inverse));var boxes=overlay.TerrainView?.NationView.LabelBounds.ToList()??[];
        var labelArea=new Rect(overlay.ContentOrigin,new Size(session.Info.Extent,session.Info.HeightExtent));labelArea.Transform(overlay.Camera);labelArea.Intersect(new Rect(0,0,overlay.ActualWidth,overlay.ActualHeight));
        if(labelArea.IsEmpty){dc.Pop();return;}
        foreach(var s in visible.OrderByDescending(s=>world.State.Nations.Any(n=>n.CapitalSettlementId==s.Id)).ThenByDescending(s=>s.Stage))
        {
            var capital=world.State.Nations.Any(n=>n.CapitalSettlementId==s.Id);var point=overlay.WorldToScreen(s.Position);
            if(!new Rect(-10,-10,overlay.ActualWidth+20,overlay.ActualHeight+20).Contains(point))continue;
            if(!architecture){dc.DrawRectangle(capital ? Brushes.Gold : Brushes.Wheat,new Pen(Brushes.Black,1),new Rect(point.X-3,point.Y+8,6,capital ? 8:5));}
            hits.Add((new Rect(point.X-10,point.Y-10,20,30),s.Id));
            if(overlay.StrategicMap ? overlay.MapDetail==MapDetail.World && !capital && s.Stage<SettlementStage.City : zoom<.45 && !capital && s.Stage<SettlementStage.City || zoom<1 && !capital && s.Stage<SettlementStage.Town)continue;
            var dpi=VisualTreeHelper.GetDpi(overlay).PixelsPerDip;var key=((capital ? "* ":"")+s.Name,dpi);
            if(!text.TryGetValue(key,out var label)){if(text.Count>=512)text.Clear();label=new FormattedText((capital ? "* ":"")+s.Name,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.White,dpi);text[key]=label;}
            var rect=new Rect(Math.Clamp(point.X-label.Width/2,labelArea.Left,Math.Max(labelArea.Left,labelArea.Right-label.Width-6)),Math.Clamp(point.Y+(zoom<.45 ? 45:20),labelArea.Top,Math.Max(labelArea.Top,labelArea.Bottom-label.Height-3)),label.Width+6,label.Height+3);
            if(boxes.Any(b=>b.IntersectsWith(rect))){rect.Y=Math.Clamp(point.Y-55,labelArea.Top,Math.Max(labelArea.Top,labelArea.Bottom-label.Height-3));if(boxes.Any(b=>b.IntersectsWith(rect)))continue;}
            boxes.Add(rect);dc.DrawRoundedRectangle(Brushes.Black,null,rect,2,2);dc.DrawText(label,new Point(rect.X+3,rect.Y+1));hits.Add((rect,s.Id));
        }
        dc.Pop();
    }
    internal Guid? Hit(Point screen)
    {
        if(camera==overlay.Camera)foreach(var hit in hits)if(hit.Bounds.Contains(screen))return hit.Id;
        var world=session.Civilization;if(world==null)return null;return world.State.Settlements.Where(s=>s.Active && (overlay.WorldToScreen(s.Position)-screen).Length<12).OrderBy(s=>(overlay.WorldToScreen(s.Position)-screen).LengthSquared).Select(s=>(Guid?)s.Id).FirstOrDefault();
    }
}
