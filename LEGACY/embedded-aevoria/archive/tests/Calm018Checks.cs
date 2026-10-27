using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckCalm018()
    {
        BugManager Make(BugDensity density=BugDensity.Arcade) { var m=new BugManager(118);m.Configure(new(true,BugSpecies.AllBugs,density));m.SetSurfaces([new("widget",new Rect(100,100,300,180))],new Size(1100,750));return m; }
        BugEntity Bug(BugSpecies species,double x=500,double y=350)=>new() { Species=species,Position=new(x,y),Target=new(x,y),State=BugState.Paused,Decision=1000,Speed=25,Cooldown=0 };
        foreach(var density in new[] { BugDensity.Low,BugDensity.Normal,BugDensity.Infestation })
        {
            var m=Make(density);for(var i=0;i<2400;i++)m.Advance(.08);
            Assert(m.Webs.Count==0 && m.Homes.Count==0 && m.Bugs.All(b=>b.Name==null && b.Prey==null && b.Meals==0),"Decorative "+density+" excludes arcade features");
        }
        var calm=Make();var max=0;
        for(var i=0;i<22500;i++) { calm.Advance(.08);max=Math.Max(max,calm.Bugs.Count);if(i%400==0 && calm.Bugs.FirstOrDefault(b=>b.State==BugState.Crossing) is {} b)calm.Squash(b); }
        Assert(max<=240 && calm.Cap==240 && calm.Webs.Count<=18 && calm.Splats.Count<=60,"Thirty minute arcade simulation stays bounded under repeated interaction");
        Assert(calm.Bugs.All(b=>b.SizeScale<3) && calm.Homes.Count>0,"Feeding growth and home populations stay modest");
        Assert(BugManager.Names.Length==50 && BugManager.Names.Distinct().Count()==50 && new[]{"Joe","Pete","Zack"}.All(BugManager.Names.Contains),"Fifty unique names include Joe, Pete and Zack");
        int named=0,total=0;
        for(var seed=0;seed<300;seed++) { var m=new BugManager(seed);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade));m.SetSurfaces([],new Size(1100,750));for(var i=0;i<750;i++)m.Advance(.08);named+=m.Bugs.Count(b=>b.Name!=null);total+=m.Bugs.Count; }
        Assert((double)named/total is >.012 and <.03,"Named bug frequency is approximately two percent");
        var webManager=Make();var spider=Bug(BugSpecies.Spiders);webManager.Bugs.Add(spider);webManager.BuildWeb(spider);
        var web=webManager.Webs.Single();
        Assert(web.Owner==spider && web.Position==spider.Position && web.Strands.Count>10,"A spider builds organic strands at its actual position");
        web.Strands.Clear();web.Strands.Add(new(new(100,100),new(300,100),"widget"));web.Strands.Add(new(new(500,300),new(600,300)));
        var edge=Bug(BugSpecies.Roaches,150,100);edge.Surface="widget";edge.State=BugState.Edge;webManager.Bugs.Add(edge);
        webManager.SetSurfaces([new("widget",new Rect(100,100,300,180))],new Size(1100,750));
        Assert(web.Strands.Count==2 && edge.Position==new Point(150,100),"Scrolling with unchanged content geometry preserves attachments");
        webManager.SetSurfaces([new("widget",new Rect(140,130,300,180))],new Size(1100,750));
        Assert(edge.Position==new Point(190,130) && edge.Surface=="widget","Attached roach follows its widget without detaching");
        webManager.BreakAnchors("widget");Assert(web.Strands.Count==1,"Dragging breaks affected anchors and preserves unrelated strands");
        Assert(webManager.CutWebs(new(550,280),new(550,320))==1 && webManager.Webs.Count==0,"Cursor sweep slices a crossed strand");
        var ants=Make();var ant=Bug(BugSpecies.Ants);ants.Bugs.Add(ant);var remains=new BugSplat {Position=ant.Position,Species=BugSpecies.Flies};ants.Splats.Add(remains);
        ants.Advance(.08);Assert(ant.Cargo==remains,"Ant picks up encounter remains");
        ant.Position=ants.Home(BugSpecies.Ants);ants.Advance(.08);Assert(ant.Cargo==null && !ants.Splats.Contains(remains),"Ant delivers remains to its ant bed");
        var flies=Make();var fly=Bug(BugSpecies.Flies);var prey=Bug(BugSpecies.Ants,510);flies.Bugs.AddRange([fly,prey]);flies.Advance(.08);
        Assert(fly.Prey==prey && prey.State==BugState.Trapped,"Fly picks up a nearby bug");
        for(var i=0;i<70;i++)flies.Advance(.08);Assert(fly.Prey==null && prey.State!=BugState.Trapped,"Fly releases its passenger after a bounded carry");
        var cent=Make();var centipede=Bug(BugSpecies.Centipedes);var meal=Bug(BugSpecies.Ants,510);cent.Bugs.AddRange([centipede,meal]);cent.Advance(.08);
        Assert(centipede.Prey==meal,"Centipede cages a nearby opponent");
        for(var i=0;i<70;i++)cent.Advance(.08);Assert(centipede.Meals==1 && centipede.Growth>1,"Centipede grows by feeding");
        var ladyManager=Make();var lady=Bug(BugSpecies.Ladybugs);var roach=Bug(BugSpecies.Roaches,510);ladyManager.Bugs.AddRange([lady,roach]);ladyManager.Advance(.08);
        Assert(roach.State!=BugState.Squashed && roach.Hits==1,"Roach resists ladybug attack");
        var ordinary=Bug(BugSpecies.Ants,510);ladyManager.Bugs.Remove(roach);ladyManager.Bugs.Add(ordinary);lady.Cooldown=0;ladyManager.Advance(.08);
        Assert(ordinary.State==BugState.Squashed,"Ladybug squashes an unarmored opponent");
        var overlay=new BugOverlay();overlay.SetPerformancePressure(true);Assert(overlay.ResolutionScale==1 && overlay.UseGpuSprites,"Rendering pressure never lowers native resolution");
        foreach(var species in new[]{BugSpecies.Ants,BugSpecies.Flies,BugSpecies.Ladybugs,BugSpecies.Roaches,BugSpecies.Spiders,BugSpecies.Centipedes})
        { var b=Bug(species,100+overlay.Manager.Bugs.Count*110,100);overlay.Manager.Bugs.Add(b); }
        overlay.Manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade));overlay.Manager.SetSurfaces([],new Size(900,500));
        for(var i=0;i<750;i++)overlay.Manager.Advance(.08);
        overlay.Measure(new Size(900,500));overlay.Arrange(new Rect(0,0,900,500));overlay.RefreshFrame();Save018(overlay,900,500,"calm-arcade018");
        CheckScrollWeb018();
        CheckCounterVessels018();
    }
}
