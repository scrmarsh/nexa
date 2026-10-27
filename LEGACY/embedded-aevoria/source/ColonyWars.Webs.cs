using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    private int nextWeb;
    private static double Noise(BugWeb web,int n)
    {var x=unchecked((uint)web.GeometrySeed+(uint)n*747796405u);x=(x^(x>>16))*2246822519u;x=(x^(x>>13))*3266489917u;return (x^(x>>16))/(double)uint.MaxValue;}
    private static double SharedRadius(BugWeb web)=>web.GeometrySeed==0 ? 45+web.Rings*28 : 135+Noise(web,90)*35;
    internal static Point VisibleStrandEnd(BugWeb web,WebStrand strand)
    {
        var progress=web.GeometrySeed==0 || strand.BuiltAt<0 ? web.Construction : Math.Clamp((web.TotalWork-strand.BuiltAt)/.6,0,1);
        return strand.A+(strand.B-strand.A)*progress;
    }
    internal bool TrySharedWeb(BugEntity spider)
    {
        if(!IsArcade || !Alive(spider) || spider.Species!=BugSpecies.Spiders || Webs.Count>=BattleOptions.MaximumSharedWebs)return false;
        var site=ClampArena(spider.Position,175);
        if(Webs.Any(w=>(w.Position-site).Length<240))return false;
        var web=new BugWeb {WebId=++nextWeb,GeometrySeed=random.Next(1,int.MaxValue),FactionId=Colonies[spider.Species].Id,Position=site,Owner=spider,Construction=.15};
        BuildNaturalPlan(web);web.Contributors.Add(spider.UnitId);
        web.Strands.AddRange(web.Plan.Take(2).Select(s=>s with{BuiltAt=0}));Webs.Add(web);return true;
    }
    private void BuildNaturalPlan(BugWeb web)
    {
        var center=web.Position;var radius=SharedRadius(web);
        var cores=new[]{center,center+new Vector(-18-Noise(web,1)*13,-8-Noise(web,2)*8),center+new Vector(14+Noise(web,3)*10,-14-Noise(web,4)*11),center+new Vector(Noise(web,5)*12,17+Noise(web,6)*13)};
        for(var i=0;i<4;i++)for(var j=i+1;j<4;j++)web.Plan.Add(new(cores[i],cores[j]));
        var points=new Point[9];var anchors=new Point[9];
        for(var i=0;i<9;i++)
        {
            var angle=i*Math.PI*2/9+(Noise(web,i+11)-.5)*.6;
            var tip=ClampArena(center+new Vector(Math.Cos(angle),Math.Sin(angle))*radius*(.68+Noise(web,i+23)*.32),8);
            var structure=Settlements.OrderBy(n=>(n.Position-tip).LengthSquared).FirstOrDefault();
            if(structure!=null && (structure.Position-tip).Length<55)tip=structure.Position;
            anchors[i]=tip;points[i]=center+(tip-center)*(.28+Noise(web,i+35)*.38)+new Vector((Noise(web,i+48)-.5)*15,(Noise(web,i+61)-.5)*15);
        }
        for(var i=0;i<9;i++)
        {
            var nearest=Enumerable.Range(0,4).OrderBy(n=>(cores[n]-points[i]).LengthSquared).First();
            web.Plan.Add(new(cores[nearest],points[i]));web.Plan.Add(new(points[i],anchors[i]));
            web.Plan.Add(new(points[i],points[(i+1)%9]));web.Plan.Add(new(points[i],cores[(nearest+1)%4]));
            if(i%3==0)web.Plan.Add(new(points[i],anchors[(i+2)%9]));
            if(i%2==0)web.Plan.Add(new(anchors[i],anchors[(i+1)%9]));
        }
    }
    private bool OrderWebPatrol(BugEntity spider)
    {
        var faction=Colonies[spider.Species];BugWeb? nearest=null;var distance=double.PositiveInfinity;
        foreach(var web in Webs.Where(w=>w.FactionId==faction.Id))
        {var d=(web.Position-spider.Position).LengthSquared;if(d<distance) {distance=d;nearest=web;}}
        if(nearest==null || distance>300*300)
        {
            var center=new Point(BattleArena.Left+BattleArena.Width/2,BattleArena.Top+BattleArena.Height/2);var inward=center-faction.Rally;if(inward.Length>1)inward.Normalize();
            spider.OrderPoint=ClampArena(faction.Rally+inward*75,175);spider.Combat=CombatState.Patrol;
            if((spider.Position-spider.OrderPoint).Length<45 && TrySharedWeb(spider))nearest=Webs[^1];else if(nearest==null)return true;
        }
        if(nearest!=null)
        {spider.AssignedWeb=nearest;var angle=spider.UnitId*2.399+MatchAge*.18;spider.OrderPoint=ClampArena(nearest.Position+new Vector(Math.Cos(angle),Math.Sin(angle))*SharedRadius(nearest)*.55);}
        spider.Combat=CombatState.Patrol;return true;
    }
    private static bool SameSilk(WebStrand a,WebStrand b)=>a.A==b.A && a.B==b.B && a.Anchor==b.Anchor;
    private void JoinNearbyWeb(BugWeb web)
    {
        if(web.Plan.Count>=56)return;
        foreach(var other in Webs)
        {
            if(other==web || other.Strands.Count<other.Plan.Count || other.FactionId!=web.FactionId || web.LinkedWebs.Contains(other.WebId) || (other.Position-web.Position).Length>370)continue;
            var pair=web.Plan.SelectMany(a=>other.Plan.Select(b=>(A:a.B,B:b.B))).OrderBy(p=>(p.A-p.B).LengthSquared).FirstOrDefault();
            if((pair.A-pair.B).Length<1)continue;
            web.Plan.Add(new(pair.A,pair.B,"web:"+other.WebId));web.LinkedWebs.Add(other.WebId);other.LinkedWebs.Add(web.WebId);break;
        }
    }
    private void CleanupSharedLinks()
    {
        var ids=Webs.Select(w=>w.WebId).ToHashSet();
        foreach(var w in Webs)
        {
            var missing=w.LinkedWebs.Where(id=>!ids.Contains(id)).ToArray();
            foreach(var id in missing) {w.LinkedWebs.Remove(id);w.Plan.RemoveAll(s=>s.Anchor=="web:"+id);w.Strands.RemoveAll(s=>s.Anchor=="web:"+id);}
        }
    }
    private void AdvanceSharedWebs(double dt)
    {
        CleanupSharedLinks();
        for(var i=Webs.Count-1;i>=0;i--)
        {
            var web=Webs[i];web.Age+=dt;var workers=0;
            foreach(var spider in combatUnits)
                if(Alive(spider) && spider.Species==BugSpecies.Spiders && Colonies[spider.Species].Id==web.FactionId && spider.TargetUnit==null && spider.Combat!=CombatState.Retreat && (spider.Position-web.Position).Length<SharedRadius(web)+35)
                {
                    workers++;if(!web.Contributors.Contains(spider.UnitId) && web.Contributors.Count>=128)web.Contributors.Remove(web.Contributors.Min());web.Contributors.Add(spider.UnitId);
                }
            if(workers>0)
            {
                web.Age=Math.Max(0,web.Age-dt*2);var work=dt*Math.Min(3,workers)*.3*BattleOptions.WebConstructionRate;
                web.TotalWork+=work;web.BuildWork+=work;web.Construction=Math.Min(1,web.TotalWork/3);
                if(web.BuildWork>=.2 && web.Strands.Count<web.Plan.Count)
                {
                    var missing=web.Plan.FirstOrDefault(s=>!web.Strands.Any(b=>SameSilk(s,b)));
                    if(missing!=null) {web.Strands.Add(missing with{BuiltAt=web.TotalWork});web.BuildWork-=.2;}
                    web.Rings=Math.Min(4,1+web.Strands.Count/12);
                }
                else if(web.BuildWork>=2.5) {web.BuildWork=0;JoinNearbyWeb(web);}
            }
            if(web.Age>90) {Webs.RemoveAt(i);continue;}
            if(!BattleOptions.FunctionalWebTraps)continue;
            Neighbors(web.Position,SharedRadius(web)+35);
            foreach(var enemy in closeUnits)
            {
                if(!Alive(enemy) || enemy.Species==BugSpecies.Spiders || enemy.Protection>0 || enemy.WebImmunity>0)continue;
                var touching=false;
                foreach(var strand in web.Strands)
                {
                    var end=VisibleStrandEnd(web,strand);if((end-strand.A).Length<1)continue;
                    if(DistanceToSegment(enemy.Position,strand.A,end)<7 || Crosses(enemy.PreviousCombatPosition,enemy.Position,strand.A,end)) {touching=true;break;}
                }
                if(!touching)continue;
                enemy.SlowTimer=enemy.Species==BugSpecies.Roaches ? .7 : 2.1;enemy.StunTimer=enemy.Species==BugSpecies.Roaches ? .08 : .35;enemy.WebImmunity=4;enemy.ThinkTimer=0;
            }
        }
    }
}
