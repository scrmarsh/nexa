using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class BugOverlay
{
    private static readonly SolidColorBrush ContestedInk=new(Color.FromArgb(128,119,136,153));
    private readonly Dictionary<uint,SolidColorBrush> factionBrushes=[];
    private DrawingGroup? territoryDrawing;
    private (int Revision,bool Fill,bool Borders,bool Labels,bool Contested,double Opacity) territoryStyle;
    private SolidColorBrush FactionBrush(int id,double opacity=1)
    {
        var argb=(Manager.FactionColors.GetValueOrDefault(id,0xff8896a8)&0x00ffffff)|((uint)(byte)(Math.Clamp(opacity,0,1)*255)<<24);
        if(!factionBrushes.TryGetValue(argb,out var brush))
        {brush=new SolidColorBrush(Color.FromArgb((byte)(argb>>24),(byte)(argb>>16),(byte)(argb>>8),(byte)argb));brush.Freeze();factionBrushes[argb]=brush;}
        return brush;
    }
    private void DrawTerritoryMap(DrawingContext dc)
    {
        if(!Manager.IsInfinite)return;var options=Manager.BattleOptions;
        var key=(Manager.TerritoryRevision,options.ShowTerritories,options.ShowTerritoryBorders,options.ShowFactionLabels,options.HighlightContested,options.TerritoryOpacity);
        if(territoryDrawing==null || key!=territoryStyle)
        {
            territoryStyle=key;territoryDrawing=new DrawingGroup();using(var map=territoryDrawing.Open())
            {
                foreach(var region in Manager.Territories)
                {
                    if(region.OwnerId==0)continue;var bounds=Manager.RegionBounds(region.Id);var color=FactionBrush(region.OwnerId);
                    if(options.ShowTerritories)map.DrawRectangle(FactionBrush(region.OwnerId,options.TerritoryOpacity),null,bounds);
                    if(options.ShowTerritoryBorders)
                    {
                        var pen=new Pen(FactionBrush(region.OwnerId,.5),.7);var x=region.Id%BugManager.TerritoryColumns;var y=region.Id/BugManager.TerritoryColumns;

                        if(x==0 || Manager.Territories[region.Id-1].OwnerId!=region.OwnerId)map.DrawLine(pen,bounds.TopLeft,bounds.BottomLeft);
                        if(x==BugManager.TerritoryColumns-1 || Manager.Territories[region.Id+1].OwnerId!=region.OwnerId)map.DrawLine(pen,bounds.TopRight,bounds.BottomRight);
                        if(y==0 || Manager.Territories[region.Id-BugManager.TerritoryColumns].OwnerId!=region.OwnerId)map.DrawLine(pen,bounds.TopLeft,bounds.TopRight);
                        if(y==BugManager.TerritoryRows-1 || Manager.Territories[region.Id+BugManager.TerritoryColumns].OwnerId!=region.OwnerId)map.DrawLine(pen,bounds.BottomLeft,bounds.BottomRight);

                    }
                    if(options.HighlightContested && region.Contested)
                    {var inset=bounds;inset.Inflate(-4,-4);map.DrawRectangle(null,new Pen(ContestedInk,.8) {DashStyle=DashStyles.Dash},inset);}
                }
                if(options.ShowFactionLabels)
                {
                    foreach(var group in Manager.Territories.Where(r=>r.OwnerId!=0).GroupBy(r=>r.OwnerId))
                    {
                        if(group.Count()<3)continue;
                        var xs=group.Average(r=>Manager.RegionBounds(r.Id).X);var ys=group.Average(r=>Manager.RegionBounds(r.Id).Y);
                        var closest=group.OrderBy(r=>{var b=Manager.RegionBounds(r.Id);return (b.X-xs)*(b.X-xs)+(b.Y-ys)*(b.Y-ys);}).First();
                        var bounds=Manager.RegionBounds(closest.Id);var label=BattleText(Manager.FactionName(group.Key),12,FactionBrush(group.Key,.65));
                        label.MaxTextWidth=bounds.Width-8;label.MaxLineCount=1;label.Trimming=TextTrimming.CharacterEllipsis;
                        map.DrawText(label,new Point(bounds.X+8,bounds.Y+10));
                    }
                }
                foreach(var nest in Manager.Settlements)
                {
                    var color=FactionBrush(nest.OwnerId);var p=nest.Position;
                    map.DrawEllipse(HudBackground,new Pen(color,1.5),p,15,10);
                    map.DrawLine(new Pen(color,1),p+new Vector(-9,0),p+new Vector(9,0));
                    map.DrawEllipse(color,null,p+new Vector(-7,-4),2,2);map.DrawEllipse(color,null,p+new Vector(7,-4),2,2);
                }
            }
            territoryDrawing.Freeze();
        }
        dc.PushTransform(new TranslateTransform(ContentOrigin.X,ContentOrigin.Y));dc.DrawDrawing(territoryDrawing);dc.Pop();
    }
}
