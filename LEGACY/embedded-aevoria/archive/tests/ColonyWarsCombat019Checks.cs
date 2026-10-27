using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static BugEntity Fighter(BugManager m,BugSpecies species,Point position)
    {
        var stats=m.Stats(species);var b=new BugEntity {Species=species,UnitId=1000+m.Bugs.Count,Health=stats.Health,MaxHealth=stats.Health,Position=position,OrderPoint=position,State=BugState.Crossing,Combat=CombatState.Idle,ThinkTimer=100,Speed=stats.Speed,AbilityTimer=100};
        m.Bugs.Add(b);m.Colonies[species].Alive++;return b;
    }
    private static void CheckCombat019()
    {
        foreach(var species in BugHabitats.Species)
        {
            var m=War();var other=species==BugSpecies.Ants ? BugSpecies.Roaches : BugSpecies.Ants;
            var a=Fighter(m,species,new Point(500,350));var b=Fighter(m,other,new Point(508,350));a.TargetUnit=b;
            var hp=b.Health;m.Advance(.016);Assert(b.Health<hp && b.LastAttacker==a,species+" attacks and visibly damages an enemy through the common damage system");
            m.ApplyBattleDamage(b,a,1);a.AttackTimer=0;a.ThinkTimer=0;hp=b.Health;m.Advance(.016);
            Assert(a.TargetUnit==b && b.Health<hp,species+" retaliates when attacked");
            a.AttackTimer=100;b.AttackTimer=0;b.ThinkTimer=0;hp=a.Health;m.Advance(.016);
            Assert(b.TargetUnit==a && a.Health<hp,other+" immediately retaliates against "+species);
            var friend=Fighter(m,species,new Point(500,350));hp=friend.Health;
            Assert(!m.ApplyBattleDamage(a,friend,20) && friend.Health==hp,species+" cannot damage its own colony");
            var distant=Fighter(m,other,new Point(900,600));hp=distant.Health;a.TargetUnit=distant;a.ThinkTimer=100;a.AttackTimer=0;m.Advance(.016);
            Assert(distant.Health==hp,species+" cannot attack beyond its valid melee range");
        }
        var eliminated=War();var queen=eliminated.Colonies[BugSpecies.Ants].Leader;var spider=eliminated.Colonies[BugSpecies.Spiders].Leader;
        Assert(!eliminated.ApplyBattleDamage(spider,queen,9999),"Initial royal protection prevents immediate elimination");
        queen.Protection=0;eliminated.ApplyBattleDamage(spider,queen,9999);spider.Health=0;
        Assert(eliminated.Colonies[BugSpecies.Ants].Eliminated && eliminated.Announcements.Peek().Text.StartsWith("SPIDERS"),"Final damaging species keeps conquest credit even if its attacker then dies");
        for(var i=0;i<120;i++)eliminated.Advance(.016);
        Assert(eliminated.Bugs.All(b=>b.Species!=BugSpecies.Ants) && eliminated.Colonies[BugSpecies.Ants].Alive==0,"Defeated armies fade out and receive no reinforcements");
        var messages=new System.Collections.Generic.HashSet<string>();
        foreach(var attacker in BugHabitats.Species)foreach(var defender in BugHabitats.Species)
        {
            if(attacker==defender)continue;var message=eliminated.ConquestMessage(attacker,defender);messages.Add(message);
            Assert(message.StartsWith(attacker.ToString().ToUpperInvariant()) && message.Contains(BugManager.LeaderTitle(defender).ToUpperInvariant()),"Conquest credits "+attacker+" versus "+defender);
        }
        Assert(messages.Count==30,"Dynamic conquest messages support all thirty attacker/defender pairings");
        var draw=War();var ants=draw.Colonies[BugSpecies.Ants].Leader;var flies=draw.Colonies[BugSpecies.Flies].Leader;
        foreach(var c in draw.Colonies.Values) {c.Leader.Protection=0;if(c.Species is not (BugSpecies.Ants or BugSpecies.Flies))draw.ApplyBattleDamage(ants,c.Leader,9999);}
        ants.Position=new Point(500,350);flies.Position=new Point(508,350);ants.OrderPoint=ants.Position;flies.OrderPoint=flies.Position;
        ants.Health=flies.Health=1;ants.TargetUnit=flies;flies.TargetUnit=ants;ants.AttackTimer=flies.AttackTimer=0;ants.ThinkTimer=flies.ThinkTimer=100;
        draw.Advance(.016);Assert(draw.Phase==BattlePhase.Victory && draw.Draws==1 && draw.Colonies.Values.All(c=>c.Eliminated),"Simultaneous final attacks produce a draw with no surviving colony");
        for(var i=0;i<2400 && draw.MatchNumber==1;i++)draw.Advance(.016);
        Assert(draw.MatchNumber==2 && draw.Colonies.Count==6 && draw.Colonies.Values.All(c=>!c.Eliminated),"Queued conquests, draw banner and celebration finish before a clean automatic restart");
        var winner=War();var antLeader=winner.Colonies[BugSpecies.Ants].Leader;
        foreach(var c in winner.Colonies.Values)if(c.Species!=BugSpecies.Ants) {c.Leader.Protection=0;winner.ApplyBattleDamage(antLeader,c.Leader,9999);}
        winner.Advance(.016);Assert(winner.Phase==BattlePhase.Victory && winner.Victories.GetValueOrDefault(BugSpecies.Ants)==1,"Last surviving colony wins and increments its running victory counter");
        var count=winner.Bugs.Count;for(var i=0;i<100;i++)winner.Advance(.016);
        Assert(winner.Bugs.Count<=count && winner.Bugs.All(BugManager.Alive) && winner.Projectiles.Count==0,"Victory freezes combat and reinforcements while defeated units finish fading");
        var webs=War();var hunter=webs.Colonies[BugSpecies.Spiders].Leader;hunter.Health=hunter.MaxHealth;hunter.Protection=0;
        var victim=Fighter(webs,BugSpecies.Roaches,new Point(550,350));hunter.Position=new Point(500,350);hunter.OrderPoint=hunter.Position;hunter.TargetUnit=victim;hunter.AbilityTimer=0;hunter.ThinkTimer=100;
        webs.Advance(.016);Assert(webs.Projectiles.Count>0,"Spider fires a visible targeted combat projectile");
        for(var i=0;i<15;i++)webs.Advance(.016);Assert(victim.SlowTimer>0 && victim.StunTimer<.1 && victim.WebImmunity>3,"Roach resists web control and receives anti-stun immunity");
        hunter.AbilityTimer=100;hunter.AttackTimer=100;victim.TargetUnit=null;victim.ThinkTimer=100;
        for(var i=0;i<120;i++)webs.Advance(.016);Assert(victim.SlowTimer==0 && victim.StunTimer==0,"Combat web control expires instead of trapping indefinitely");
        webs.Webs.Clear();hunter.Position=new Point(600,450);
        Assert(webs.TryDecorativeWeb(hunter) && !webs.TryDecorativeWeb(hunter),"A live spider builds decorative webs with spacing checks");
        var stationary=Fighter(webs,BugSpecies.Ants,hunter.Position);stationary.ThinkTimer=100;stationary.Protection=100;
        for(var i=0;i<800;i++)webs.Advance(.016);
        Assert(stationary.State!=BugState.Trapped && webs.Webs.Count<=4,"Protected units resist shared webs and web count remains bounded");
        webs.Configure(new(false));Assert(webs.Bugs.Count==0 && webs.Colonies.Count==0 && webs.Projectiles.Count==0 && webs.Announcements.Count==0,"Disabling Arcade clears units, colony references, projectiles and event queues");
    }
}
