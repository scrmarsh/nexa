using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal sealed record WebStrand(Point A, Point B, string? Anchor = null) {public double BuiltAt {get;init;}=-1;}
internal sealed partial class BugManager
{
    internal const int MaximumWebs=18;
    internal bool IsArcade => Preferences.Density==BugDensity.Arcade;
    internal readonly Dictionary<BugSpecies,Point> Homes=[];
    internal static readonly string[] Names = "Joe Pete Zack Ada Bea Ben Bo Cal Cam Cleo Coco Daisy Dex Dot Eli Eve Finn Flo Gus Hal Hazel Iris Ivy Jade Jay Jem Kai Kit Leo Lou Luna Max Mia Milo Mo Ned Nico Olive Oscar Pip Ray Remy Rio Rose Rue Sam Sky Theo Uma Zoe".Split(' ');
    internal Point Home(BugSpecies species)
    {
        if(Homes.TryGetValue(species,out var point))return point;
        var index=(int)species;
        var surfaces=Surfaces.Values.ToArray();
        if(surfaces.Length>0) { var r=surfaces[index%surfaces.Length]; point=new Point(r.Left+24+(index*37)%Math.Max(25,r.Width-48),r.Bottom+12); }
        else point=new Point(45+(index*137)%Math.Max(50,Bounds.Width-90),60+(index*89)%Math.Max(50,Bounds.Height-120));
        point=new Point(Math.Clamp(point.X,20,Math.Max(20,Bounds.Width-20)),Math.Clamp(point.Y,20,Math.Max(20,Bounds.Height-20)));
        Homes[species]=point;return point;
    }
    internal void BuildWeb(BugEntity spider)
    {
        if(!IsArcade || spider.Species!=BugSpecies.Spiders || Webs.Count>=MaximumWebs)return;
        var web=new BugWeb { Position=spider.Position,Owner=spider };
        var tips=new List<Point>();
        for(var i=0;i<11;i++)
        {
            var angle=i*Math.PI*2/11+Range(-.2,.2);
            var tip=spider.Position+new Vector(Math.Cos(angle),Math.Sin(angle))*Range(125,BugWeb.Radius);
            string? anchor=null;
            foreach(var pair in Surfaces)
            {
                var edge=EdgePoint(pair.Value,random.NextDouble());
                if((edge-tip).Length<65) { tip=edge;anchor=pair.Key;break; }
            }
            tips.Add(tip);web.Strands.Add(new(spider.Position,tip,anchor));
            if(i>0)web.Strands.Add(new(tips[i-1],tip,anchor));
            if(i>0) for(var ring=1;ring<=3;ring++) web.Strands.Add(new(spider.Position+(tips[i-1]-spider.Position)*(ring*.22),spider.Position+(tip-spider.Position)*(ring*.22+.025)));
        }
        Webs.Add(web);
    }
    internal static double DistanceToSegment(Point p,Point a,Point b)
    {
        var d=b-a;var t=d.LengthSquared<.001 ? 0 : Math.Clamp(Vector.Multiply(p-a,d)/d.LengthSquared,0,1);
        return (p-(a+d*t)).Length;
    }
    internal int CutWebs(Point a,Point b)
    {
        if(!IsArcade || WorldPaused)return 0;
        var count=0;
        foreach(var w in Webs)
        {
            count+=w.Strands.RemoveAll(s=>(VisibleStrandEnd(w,s)-s.A).Length>1 && Crosses(a,b,s.A,VisibleStrandEnd(w,s)));
            ReleaseCaptives(w);
        }
        Webs.RemoveAll(w=>w.Strands.Count==0);CleanupSharedLinks();return count;
    }
    private static bool Crosses(Point a,Point b,Point c,Point d)
    {
        static double Cross(Vector u,Vector v)=>u.X*v.Y-u.Y*v.X;
        var ab=b-a;var cd=d-c;var den=Cross(ab,cd);
        if(Math.Abs(den)<.001)return DistanceToSegment(a,c,d)<2 || DistanceToSegment(b,c,d)<2;
        var t=Cross(c-a,cd)/den;var u=Cross(c-a,ab)/den;
        return t>=0 && t<=1 && u>=0 && u<=1;
    }
    internal void BreakAnchors(string id)
    {
        foreach(var w in Webs)
        {
            var ends=w.Strands.Where(s=>s.Anchor==id).Select(s=>s.B).ToHashSet();
            w.Strands.RemoveAll(s=>s.Anchor==id || ends.Contains(s.A) || ends.Contains(s.B));ReleaseCaptives(w);
        }
        Webs.RemoveAll(w=>w.Strands.Count==0);
    }
    private void ReleaseCaptives(BugWeb web)
    {
        foreach(var b in web.Captives.ToArray())
        {
            if(web.Strands.Any(s=>DistanceToSegment(b.Position,s.A,s.B)<5*b.SizeScale))continue;
            if(b.Feeder is {} feeder) { feeder.Prey=null;feeder.AssignedWeb=null;Detach(feeder); }
            b.Feeder=null;b.WebImmunity=3;Detach(b);web.Captives.Remove(b);
        }
    }
    private bool UpdateEncounter(BugEntity bug,double dt)
    {
        bug.Cooldown=Math.Max(0,bug.Cooldown-dt);
        if(bug.State is BugState.Squashed or BugState.Trapped or BugState.Feeding or BugState.ApproachingWeb)return false;
        if(bug.Cargo is {} cargo)
        {
            var delta=Home(bug.Species)-bug.Position;
            if(delta.Length<6) { Splats.Remove(cargo);bug.Cargo=null;bug.Cooldown=8; }
            else { delta.Normalize();bug.Position+=delta*22*dt;cargo.Position=bug.Position+new Vector(5,0);cargo.Age=0;bug.Angle=Math.Atan2(delta.Y,delta.X)*180/Math.PI; }
            return true;
        }
        if(bug.Prey is {} prey)
        {
            if(!Bugs.Contains(prey) || prey.State==BugState.Squashed) { bug.Prey=null;return false; }
            bug.CarryTime+=dt;
            if(bug.Species==BugSpecies.Centipedes)
            {
                var angle=bug.CarryTime*2;bug.Position=prey.Position+new Vector(Math.Cos(angle),Math.Sin(angle))*18;
                bug.Angle=angle*180/Math.PI+90;
                if(bug.CarryTime>5) { prey.State=BugState.Squashed;bug.Meals++;bug.Prey=null;bug.Cooldown=14; }
            }
            else
            {
                var d=Home(bug.Species)-bug.Position;
                if(d.Length>2) { d.Normalize();bug.Position+=d*24*dt; }
                prey.Position=bug.Position+new Vector(0,8);
                if(bug.CarryTime>5) { Detach(prey);prey.WebImmunity=5;bug.Prey=null;bug.Cooldown=12; }
            }
            return true;
        }
        if(bug.Cooldown>0)return false;
        if(bug.Species is BugSpecies.Ants or BugSpecies.Flies)
        {
            var remains=Splats.FirstOrDefault(s=>(s.Position-bug.Position).Length<45 && !Bugs.Any(b=>b.Cargo==s));
            if(remains!=null) { bug.Surface=null;bug.Cargo=remains;return true; }
        }
        if(bug.Species is not (BugSpecies.Ladybugs or BugSpecies.Flies or BugSpecies.Centipedes))return false;
        // Only two simultaneous carries/cages; encounters require close contact.
        if(Bugs.Count(b=>b.Prey!=null)>=2)return false;
        var opponent=Bugs.FirstOrDefault(b=>b!=bug && b.Species!=bug.Species && b.WebImmunity<=0 && b.State is BugState.Crossing or BugState.Edge or BugState.Paused && (b.Position-bug.Position).Length<22);
        if(opponent==null)return false;
        bug.Cooldown=12;
        if(opponent.Species==BugSpecies.Roaches && ++opponent.Hits<5) { opponent.HitFlash=.35;return false; }
        if(bug.Species==BugSpecies.Ladybugs) { opponent.State=BugState.Squashed;opponent.SquashAge=0;bug.HitFlash=.3;return false; }
        bug.Surface=null;opponent.Surface=null;opponent.State=BugState.Trapped;bug.Prey=opponent;bug.CarryTime=0;
        return true;
    }
}
internal sealed partial class BugOverlay
{
    protected override void OnDpiChanged(DpiScale oldDpi,DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi,newDpi);titles.Clear();RefreshFrame();
    }
    private void DrawHabitat(DrawingContext dc)
    {
        if(!Manager.IsArcade || Manager.HasHabitatWidgets)return;
        foreach(var pair in Manager.Homes)
        {
            var p=pair.Value+(Vector)ContentOrigin;
            dc.PushOpacity(.65);
            switch(pair.Key)
            {
                case BugSpecies.Ants:
                    dc.DrawEllipse(Brushes.SaddleBrown,null,p,18,7);dc.DrawEllipse(Brushes.Black,null,p,4,2);break;
                case BugSpecies.Ladybugs:
                    for(var i=0;i<3;i++)dc.DrawEllipse(Brushes.DarkOliveGreen,null,p+new Vector(i*8-8,-i*2),10,4);break;
                case BugSpecies.Flies:
                    dc.DrawRoundedRectangle(Brushes.Peru,null,new Rect(p.X-10,p.Y-3,12,7),2,2);
                    dc.DrawEllipse(Brushes.Olive,null,p+new Vector(8,2),6,3);break;
                case BugSpecies.Roaches:
                    dc.DrawRoundedRectangle(Brushes.SaddleBrown,null,new Rect(p.X-17,p.Y-4,34,9),3,3);dc.DrawLine(new Pen(Brushes.Black,2),p-new Vector(12,0),p+new Vector(12,0));break;
                case BugSpecies.Centipedes:
                    for(var i=0;i<4;i++)dc.DrawEllipse(Brushes.DimGray,null,p+new Vector(i*8-12,0),7,4);break;
                default: dc.DrawEllipse(null,new Pen(Brushes.DimGray,2),p,13,5);break;
            }
            dc.Pop();
        }
    }
}
