using System.IO;
using System.Text.Json;
using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckNaturalWeb021()
    {
        var m=Infinite021(510);m.Configure(m.Preferences with{Arcade=m.BattleOptions with{EnableCampaigns=false}});
        for(var i=0;i<4000;i++) {foreach(var b in m.Bugs)b.Protection=1000;m.Advance(.033);}
        foreach(var b in m.Bugs) {b.Position=b.OrderPoint=new Point(1400,850);b.ThinkTimer=b.AttackTimer=10000;b.Protection=1000;b.TargetUnit=null;}
        m.Webs.Clear();var spiders=m.Bugs.Where(b=>b.Species==BugSpecies.Spiders && !b.IsLeader).Take(2).ToArray();
        var builder=spiders[0];var helper=spiders[1];builder.Position=builder.OrderPoint=new Point(500,350);helper.Position=helper.OrderPoint=new Point(520,350);
        Assert(m.TrySharedWeb(builder),"A live spider starts a seeded irregular construction plan");var web=m.Webs[0];
        Assert(web.Strands.Count==2 && web.Plan.Count>=40 && web.Strands.All(s=>(BugManager.VisibleStrandEnd(web,s)-s.A).Length<.01),"A new web starts with unfinished silk instead of a completed large structure");
        for(var i=0;i<125;i++)m.Advance(.08);
        Assert(web.Contributors.Contains(builder.UnitId) && web.Contributors.Contains(helper.UnitId) && web.Strands.Count>10 && web.Strands.Count<web.Plan.Count,"Nearby builders visibly extend one connected structure over time");
        for(var i=0;i<150;i++)m.Advance(.08);
        Assert(web.Strands.Count==web.Plan.Count && web.Plan.Count(s=>s.A==web.Position)<6 && web.Plan.Select(s=>(s.B-web.Position).Length).Distinct().Count()>20,"Completed geometry uses uneven branching connections and asymmetric anchors");
        var silk=web.Strands.First(s=>(BugManager.VisibleStrandEnd(web,s)-s.A).Length>12);var point=silk.A+(BugManager.VisibleStrandEnd(web,silk)-silk.A)*.5;
        var victim=m.Bugs.First(b=>b.Species==BugSpecies.Ants && !b.IsLeader);victim.Protection=0;victim.Position=victim.OrderPoint=point;victim.WebImmunity=0;
        m.Advance(.016);Assert(victim.SlowTimer>2 && victim.StunTimer>0 && victim.WebImmunity>0,"Contact with actually rendered silk applies visible, finite trapping and escape immunity");
        builder.ThinkTimer=0;m.Advance(.016);Assert(builder.TargetUnit==victim,"Nearby spider hunting responds to trapped prey");
        builder.TargetUnit=null;builder.ThinkTimer=10000;builder.Position=builder.OrderPoint=new Point(500,350);
        victim.Position=victim.OrderPoint=new Point(1000,600);victim.ThinkTimer=10000;victim.TargetUnit=null;
        for(var i=0;i<60;i++)m.Advance(.08);Assert(victim.SlowTimer==0 && victim.StunTimer==0,"A bug escapes web control after leaving the silk");
        m.Configure(m.Preferences with{Arcade=m.BattleOptions with{FunctionalWebTraps=false}});victim.Position=victim.OrderPoint=point;victim.WebImmunity=0;m.Advance(.016);
        Assert(victim.SlowTimer==0 && victim.StunTimer==0,"Functional trapping can be disabled without clearing constructed webs");
        victim.Position=victim.OrderPoint=new Point(1000,600);m.Configure(m.Preferences with{Arcade=m.BattleOptions with{FunctionalWebTraps=true}});
        var d=silk.B-silk.A;d.Normalize();var perpendicular=new Vector(-d.Y,d.X)*8;var count=m.CutWebs(point+perpendicular,point-perpendicular);
        Assert(count>0 && web.Strands.Count<web.Plan.Count,"Cutting visible geometry removes its sticky collision segments immediately");
        var damaged=JsonSerializer.Serialize(m.CaptureWorld(),ArcadeWorldStore.JsonOptions);var restored=BugManager.RestoreWorld(JsonSerializer.Deserialize<ArcadeWorldSnapshot>(damaged,ArcadeWorldStore.JsonOptions)!);
        Assert(restored.Webs[0].GeometrySeed==web.GeometrySeed && restored.Webs[0].Strands.Count==web.Strands.Count && restored.Webs[0].Plan.Count==web.Plan.Count,"Save/load preserves irregular web plans, damage, work progress and builder identity");
        for(var i=0;i<110;i++)m.Advance(.08);Assert(web.Strands.Count==web.Plan.Count,"Workers repair missing silk through timed construction work");
        helper.Position=helper.OrderPoint=new Point(780,350);Assert(m.TrySharedWeb(helper),"Spiders can establish a second spaced network structure");var other=m.Webs[1];
        for(var i=0;i<650;i++)m.Advance(.08);
        Assert(web.LinkedWebs.Contains(other.WebId) && other.LinkedWebs.Contains(web.WebId) && m.Webs.Any(w=>w.Strands.Any(s=>s.Anchor?.StartsWith("web:")==true)),"Nearby completed structures form bounded shared network connections");
        var snapshot=m.CaptureWorld();File.WriteAllText(Path.Combine(output,"web-network021.json"),JsonSerializer.Serialize(snapshot,ArcadeWorldStore.JsonOptions));
    }
}
