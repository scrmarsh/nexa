using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal enum MapDetail { World, Regional, Local, Inspection }
internal sealed partial class BugOverlay
{
    internal bool StrategicMap;
    private MapDetail mapDetail;
    internal MapDetail MapDetail
    {
        get
        {
            if(!StrategicMap || TerrainView==null)return MapDetail.Inspection;
            var fit=Math.Min(ActualWidth/Manager.BattleArena.Width,ActualHeight/Manager.BattleArena.Height);
            var worldBoundary=Math.Max(.12,fit*1.15);
            double[] boundaries=[worldBoundary,Math.Max(.48,worldBoundary*1.6),Math.Max(1.4,worldBoundary*3)];
            while(mapDetail<MapDetail.Inspection && Camera.M11>boundaries[(int)mapDetail]*1.1)mapDetail++;
            while(mapDetail>MapDetail.World && Camera.M11<boundaries[(int)mapDetail-1]*.9)mapDetail--;
            return mapDetail;
        }
    }
    internal int DrawnIndividuals {get;private set;}
}
internal sealed record StrategicArmy(string Id,Guid NationId,int FactionId,string Nation,uint Color,Point Position,Point Objective,string Orders,bool Fighting,int[] Units);
internal sealed class LivingArmyView(LivingWorldSession session,BugOverlay overlay)
{
    private StrategicArmy[] groups=[];
    private double sampled=-1;
    private readonly List<(Rect Bounds,StrategicArmy Army)> hits=[];
    private Matrix hitCamera;
    private readonly Dictionary<(string,double),FormattedText> text=[];
    internal int Rebuilds {get;private set;}
    internal int VisibleGroups {get;private set;}
    internal IReadOnlyList<StrategicArmy> Groups {get{Update();return groups;}}
    private void Update()
    {
        var manager=session.Controller.Manager;if(sampled>=0 && manager.MatchAge>=sampled && manager.MatchAge-sampled<.2)return;
        sampled=manager.MatchAge;var result=new List<StrategicArmy>();
        foreach(var colony in manager.Colonies.Values.Where(c=>!c.Eliminated))
        {
            var nation=session.Nations?.Nations.SingleOrDefault(n=>n.FactionId==colony.Id && n.Status==NationExistence.Active);if(nation==null)continue;
            // Existing military units only. Sector groups are presentation, not new armies/orders.
            foreach(var sector in manager.Bugs.Where(b=>BugManager.Alive(b) && !b.IsLeader && b.Role!=ArmyRole.Reserve && b.Species==colony.Species).GroupBy(b=>((int)(b.Position.X/800),(int)(b.Position.Y/800),b.CampaignDeployed)))
            {
                var units=sector.ToArray();var point=new Point(units.Average(b=>b.Position.X),units.Average(b=>b.Position.Y));var objective=new Point(units.Average(b=>b.OrderPoint.X),units.Average(b=>b.OrderPoint.Y));
                var target=manager.Colonies.Values.FirstOrDefault(c=>c.Id==colony.TargetFactionId)?.Name;
                result.Add(new($"F{colony.Id}/{sector.Key.Item1}:{sector.Key.Item2}/{(sector.Key.CampaignDeployed?"deployed":"home")}",nation.Id,colony.Id,nation.Name,nation.Color,point,objective,colony.Strategy+(target==null?"":" → "+target),units.Any(b=>b.Combat is CombatState.Attack or CombatState.Defend),units.Select(b=>b.UnitId).Order().ToArray()));
            }
        }
        groups=result.ToArray();Rebuilds++;
    }
    internal void Draw(DrawingContext dc)
    {
        hits.Clear();VisibleGroups=0;hitCamera=overlay.Camera;if(!overlay.StrategicMap || overlay.MapDetail>=MapDetail.Local)return;Update();
        var inverse=overlay.Camera;inverse.Invert();dc.PushTransform(new MatrixTransform(inverse));var screen=new Rect(-25,-25,overlay.ActualWidth+50,overlay.ActualHeight+50);
        foreach(var army in groups)
        {
            var point=overlay.WorldToScreen(army.Position);if(!screen.Contains(point))continue;VisibleGroups++;
            var brush=new SolidColorBrush(Color.FromArgb(255,(byte)(army.Color>>16),(byte)(army.Color>>8),(byte)army.Color));brush.Freeze();var box=new Rect(point.X-10,point.Y-8,20,16);
            dc.DrawRoundedRectangle(brush,new Pen(army.Fighting?Brushes.OrangeRed:Brushes.Black,2),box,2,2);dc.DrawLine(new Pen(Brushes.White,1),box.TopLeft,box.BottomRight);dc.DrawLine(new Pen(Brushes.White,1),box.TopRight,box.BottomLeft);
            var direction=army.Objective-army.Position;if(direction.Length>1){direction.Normalize();dc.DrawLine(new Pen(brush,2),point,point+direction*23);}
            var key=(army.Units.Length.ToString(CultureInfo.InvariantCulture),VisualTreeHelper.GetDpi(overlay).PixelsPerDip);
            if(!text.TryGetValue(key,out var label)){if(text.Count>512)text.Clear();text[key]=label=new FormattedText(key.Item1,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.White,key.Item2);}
            dc.DrawRoundedRectangle(Brushes.Black,null,new Rect(point.X-8,point.Y+10,label.Width+6,label.Height),2,2);dc.DrawText(label,new(point.X-5,point.Y+10));hits.Add((new Rect(point.X-13,point.Y-12,30,40),army));
        }
        dc.Pop();
    }
    internal StrategicArmy? Hit(Point screen)=>hitCamera==overlay.Camera ? hits.LastOrDefault(h=>h.Bounds.Contains(screen)).Army : null;
}
