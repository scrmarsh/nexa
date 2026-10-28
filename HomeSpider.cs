using System.Windows;
namespace SessionDashboard;
// Decorative Home companion: candy never changes the saved ecosystem or global balance.
internal sealed class HomeSpider
{
    private readonly Random random=new(301028);
    internal BugEntity Bug {get;}=new(){Species=BugSpecies.Spiders};
    internal Rect Territory {get;private set;}
    internal Point? Candy {get;private set;}
    internal string Activity {get;private set;}="Rest";
    internal int Pickups {get;private set;}
    internal bool HasRoom {get;private set;}=true;
    internal double Age {get;private set;}
    private Point target;
    private double until,nextCandy=6,detourUntil;
    private IReadOnlyList<Rect> reserved=[];
    internal void Configure(Size size,IReadOnlyList<Rect> obstacles)
    {
        Territory=new Rect(48,80,Math.Max(45,Math.Min(300,size.Width*.24)-48),Math.Max(45,Math.Min(370,size.Height*.53)-80));
        reserved=obstacles;
        if(!Valid(Bug.Position)){Bug.Position=FindPoint();target=Bug.Position;until=Age+1;}
        HasRoom=Valid(Bug.Position);
        if(Candy is {} candy && !Valid(candy))Candy=null;
        if(!Valid(target))target=Bug.Position;
    }
    internal bool Valid(Point point)=>Territory.Contains(point) && !reserved.Any(r=>r.Contains(point));
    private Point FindPoint()
    {
        for(var i=0;i<60;i++){var p=new Point(Territory.X+random.NextDouble()*Territory.Width,Territory.Y+random.NextDouble()*Territory.Height);if(Valid(p))return p;}
        return Territory.TopLeft;
    }
    internal void Step(double dt,Point? cursor=null)
    {
        if(!HasRoom)return;
        dt=Math.Clamp(dt,0,.1);Age+=dt;Bug.Age=Age;
        if(Age>=nextCandy && Candy==null){Candy=FindPoint();nextCandy=Age+18+random.Next(15);}
        if(Candy is {} snack && Activity!="Pickup" && Age>=detourUntil) {target=snack;Activity="Candy";}
        else if(Age>=until && Activity!="Candy")
        {
            var choice=random.Next(7);
            Activity=choice<3?"Wander":choice==3?"Groom":choice==4?"Look":"Rest";
            target=Activity=="Wander"?FindPoint():Bug.Position;until=Age+1.4+random.NextDouble()*3;
        }
        var delta=target-Bug.Position;
        if(delta.Length>2 && Activity is "Wander" or "Candy")
        {
            var direction=Math.Atan2(delta.Y,delta.X)*180/Math.PI;
            Bug.Angle=BugAnimation.SmoothAngle(Bug.Angle,direction,dt,125);
            var distance=Math.Min(delta.Length,dt*24);var turn=((direction-Bug.Angle+540)%360)-180;
            distance*=Math.Max(0,Math.Cos(turn*Math.PI/180));delta.Normalize();var next=Bug.Position+delta*distance;
            if(Valid(next)){Bug.Position=next;Bug.CrawlPhase+=distance;Bug.State=BugState.Crossing;Bug.Activity=BugActivity.Exploring;}
            else {target=FindPoint();until=detourUntil=Age+3;Activity="Wander";}
        }
        else
        {
            Bug.State=BugState.Paused;Bug.Activity=Activity is "Groom" or "Pickup"?BugActivity.Grooming:BugActivity.Sleeping;
            if(Candy is {} treat && (treat-Bug.Position).Length<5)
            {Candy=null;Pickups++;Activity="Pickup";until=Age+1.2;}
            if(cursor is {} look && (look-Bug.Position).Length<150)
                Bug.Angle=BugAnimation.SmoothAngle(Bug.Angle,Math.Atan2(look.Y-Bug.Position.Y,look.X-Bug.Position.X)*180/Math.PI,dt,60);
        }
    }
}
