using System.Diagnostics;
using System.IO;
using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckArcade020()
    {
        Assert(App.AppVersion=="0.1.23","Target version is 0.1.23 Alpha");
        foreach(var species in BugHabitats.Species)
        {
            var m=War();foreach(var reserve in m.Colonies.Values)reserve.Resources=0;
            var enemy=species==BugSpecies.Ants ? BugSpecies.Roaches : BugSpecies.Ants;
            var a=Fighter(m,species,new Point(500,350));var b=Fighter(m,enemy,new Point(510,350));
            a.ThinkTimer=b.ThinkTimer=0;var hp=b.Health;m.Advance(.016);
            Assert(a.TargetUnit==b && b.Health<hp,species+" independently detects and attacks a nearby enemy");
            Assert(b.LastAttacker==a && a.LastAttacker==b,species+" exchanges actual damage and retaliation");
            hp=b.Health;m.Advance(.016);Assert(b.Health==hp,species+" respects its attack cooldown");
            var protectedUnit=Fighter(m,enemy,new Point(510,350));protectedUnit.Protection=2;
            Assert(!m.ApplyBattleDamage(a,protectedUnit,4) && protectedUnit.LastAttacker==null,"Blocked damage does not fabricate retaliation");
            a.AttackTimer=0;a.ThinkTimer=0;b.Health=0;b.Combat=CombatState.Defeated;m.Advance(.016);
            Assert(a.TargetUnit!=b,species+" discards a dead target");
            var chase=War();foreach(var c in chase.Colonies.Values)c.Resources=0;
            var hunter=Fighter(chase,species,new Point(500,350));var threat=Fighter(chase,enemy,new Point(560,350));
            hunter.ThinkTimer=0;var distance=(threat.Position-hunter.Position).Length;chase.Advance(.08);
            Assert(hunter.TargetUnit==threat && (threat.Position-hunter.Position).Length<distance,species+" chases detected enemies");
            hunter.Health=hunter.MaxHealth*.1;hunter.ThinkTimer=0;chase.Advance(.016);
            Assert(hunter.Combat==CombatState.Retreat,species+" retreats when badly injured");
            var defense=War();foreach(var c in defense.Colonies.Values)c.Resources=-1000;
            var leader=defense.Colonies[species].Leader;leader.Position=new Point(500,350);leader.Protection=0;
            var guard=Fighter(defense,species,new Point(520,350));var invader=Fighter(defense,enemy,new Point(540,350));
            defense.ApplyBattleDamage(invader,leader,1);guard.ThinkTimer=0;defense.Advance(.016);
            Assert(guard.TargetUnit==invader,species+" defends its attacked reproductive leader");
        }
        var duel=War();foreach(var c in duel.Colonies.Values) {c.Resources=-1000;c.Leader.ThinkTimer=1000;c.Leader.Position=c.Leader.OrderPoint=new Point(3000,3000);}
        var duelist=Fighter(duel,BugSpecies.Centipedes,new Point(500,350));var armored=Fighter(duel,BugSpecies.Roaches,new Point(510,350));duelist.ThinkTimer=armored.ThinkTimer=0;
        for(var i=0;i<900 && BugManager.Alive(duelist) && BugManager.Alive(armored);i++)duel.Advance(.016);
        Console.WriteLine($"Duel: centipede hp={duelist.Health:F1}, roach hp={armored.Health:F1}");Assert(BugManager.Alive(duelist) && !BugManager.Alive(armored),"Centipede's individual combat advantage defeats an isolated armored roach");
        var recovery=War();foreach(var c in recovery.Colonies.Values)c.Resources=0;
        var roaches=recovery.Colonies[BugSpecies.Roaches];roaches.Resources=40;
        var heir=Fighter(recovery,BugSpecies.Roaches,roaches.Territory);heir.CanReproduce=true;heir.Name="Cassius";
        var queen=roaches.Leader;queen.Protection=0;var killer=recovery.Colonies[BugSpecies.Ants].Leader;
        var survivors=roaches.Alive;recovery.ApplyBattleDamage(killer,queen,9999);
        Assert(BugManager.Alive(heir) && roaches.Alive==survivors-1 && !roaches.Eliminated && roaches.Regrouping,"Leader death preserves surviving adults and reserves");
        Assert(recovery.History.Any(e=>e.Type==ColonyEventType.Retreat && e.FactionId==roaches.Id && e.OtherFactionId==recovery.Colonies[killer.Species].Id),"Leader-loss retreat records both participating factions");
        var leaderEvents=recovery.History.Count(e=>e.Type==ColonyEventType.LeaderKilled);
        Assert(!recovery.ApplyBattleDamage(killer,queen,9999) && recovery.History.Count(e=>e.Type==ColonyEventType.LeaderKilled)==leaderEvents,"Leader death and credit are recorded once");
        var birthCount=recovery.Bugs.Count;
        for(var i=0;i<100;i++)recovery.Advance(.08);
        Assert(recovery.Bugs.Count(b=>b.Species==BugSpecies.Roaches && BugManager.Alive(b))==1 && !heir.IsLeader,"No births before valid recovery completes");
        for(var i=0;i<60;i++)recovery.Advance(.08);
        Assert(heir.IsLeader && roaches.Leader==heir && heir.Name=="Cassius" && roaches.Recoveries==1 && roaches.Resources<20,"Safe reproductive adult rebuilds once by spending reserves and preserving identity");
        Assert(recovery.History.Any(e=>e.Type==ColonyEventType.Recovery && e.FactionId==roaches.Id && e.UnitId==heir.UnitId),"Recovery records actual faction and unit IDs");
        foreach(var species in new[]{BugSpecies.Ants,BugSpecies.Flies})
        {
            var m=War();foreach(var reserve in m.Colonies.Values)reserve.Resources=0;
            var c=m.Colonies[species];var adult=Fighter(m,species,c.Territory);adult.CanReproduce=species!=BugSpecies.Ants;adult.Protection=1000;
            c.Resources=species==BugSpecies.Ants ? 50 : 24;c.Leader.Protection=0;m.ApplyBattleDamage(null,c.Leader,9999);
            for(var i=0;i<170;i++)m.Advance(.08);
            Assert(!adult.IsLeader && c.Regrouping,"Recovery requires both reproductive capability and sufficient reserves: "+species);
            adult.Protection=0;m.ApplyBattleDamage(null,adult,9999);Assert(c.Eliminated,"Last survivor death causes true extinction");
        }
        var names=War();Assert(names.Colonies.Values.Select(c=>c.Name).Distinct().Count()==6,"Active army names are unique");
        var warrior=Fighter(names,BugSpecies.Centipedes,new Point(500,350));var identity=warrior.Name="Belisarius";
        for(var i=0;i<5;i++)
        {var prey=Fighter(names,BugSpecies.Ants,new Point(510,350));names.ApplyBattleDamage(warrior,prey,9999);names.ApplyBattleDamage(warrior,prey,9999);}
        Assert(warrior.ConfirmedKills==5 && warrior.Name==identity && warrior.LegendaryTitle!=null,"Confirmed kills award a title without changing identity or counting deaths twice");
        var founded=names.History[0];var description=names.DescribeEvent(founded);names.StartBattle();
        Assert(names.DescribeEvent(founded)==description && names.Colonies.Values.All(c=>c.Id>founded.FactionId),"Faction IDs and event names survive automatic match restarts");
        CheckSharedWeb020();CheckPopulation020();
        CheckColonyUi019();CheckBattlePerformance019();
    }
    private static void CheckSharedWeb020()
    {
        var m=War();foreach(var reserve in m.Colonies.Values) {reserve.Resources=-1000;reserve.Leader.ThinkTimer=1000;}
        var spider=Fighter(m,BugSpecies.Spiders,new Point(500,350));spider.ThinkTimer=0;
        Assert(m.TrySharedWeb(spider),"A live spider starts a connected shared web");var web=m.Webs[0];
        var helper=Fighter(m,BugSpecies.Spiders,new Point(530,350));helper.ThinkTimer=0;
        // Keep workers at a safe construction site, away from enemy leaders.
        spider.ThinkTimer=helper.ThinkTimer=100;spider.OrderPoint=spider.Position;helper.OrderPoint=helper.Position;
        for(var i=0;i<350;i++)m.Advance(.08);
        Assert(web.Contributors.Contains(spider.UnitId) && web.Contributors.Contains(helper.UnitId) && web.Rings==4 && web.Construction==1,"Nearby spiders cooperate over time to complete one bounded shared web");
        Assert(web.Strands.Count==50 && web.Strands.Any(s=>s.A==web.Position),"Bounded irregular geometry remains connected to its construction site");
        var strand=web.Strands[0];var victim=Fighter(m,BugSpecies.Ants,strand.A+(strand.B-strand.A)*.6);victim.ThinkTimer=100;victim.OrderPoint=victim.Position;
        m.Advance(.016);Assert(victim.SlowTimer>0 && victim.StunTimer>0 && victim.WebImmunity>0,"Contact with built strands temporarily traps enemies");
        spider.ThinkTimer=0;spider.AbilityTimer=100;m.Advance(.016);Assert(spider.TargetUnit==victim,"Spider detects trapped prey and pursues it");
        victim.Position=new Point(900,600);victim.OrderPoint=victim.Position;victim.ThinkTimer=100;
        for(var i=0;i<30;i++)m.Advance(.08);
        Assert(victim.SlowTimer==0 && victim.StunTimer==0,"Web control expires after escaping contact");
        var cut=m.CutWebs(new Point(535,320),new Point(535,380));var reduced=web.Strands.Count;
        Assert(cut>0 && reduced<50,"Existing strand cutting damages shared structures");
        spider.TargetUnit=helper.TargetUnit=null;spider.Position=web.Position;helper.Position=web.Position+new Vector(20,0);
        spider.OrderPoint=spider.Position;helper.OrderPoint=helper.Position;spider.ThinkTimer=helper.ThinkTimer=100;
        for(var i=0;i<130;i++)m.Advance(.08);
        Assert(web.Strands.Count==50,"Workers repair cut strands after spending construction time");
        Assert(!m.TrySharedWeb(helper),"Nearby spiders share structures instead of stacking webs");
        SaveSharedWeb020(m);
    }
    private static void SaveSharedWeb020(BugManager source)
    {
        var overlay=new BugOverlay();var m=overlay.Manager;m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade));m.SetSurfaces([],new Size(1100,700));m.Advance(.016);
        m.Webs.Clear();m.Webs.AddRange(source.Webs);overlay.Measure(new Size(1100,700));overlay.Arrange(new Rect(0,0,1100,700));overlay.RefreshFrame();overlay.UpdateLayout();Save018(overlay,1100,700,"shared-web020");
    }
    private static void CheckPopulation020()
    {
        var m=War();foreach(var c in m.Colonies.Values)c.Leader.Protection=1000;
        for(var i=0;i<4000;i++)
        {foreach(var b in m.Bugs)b.Protection=1000;m.Advance(.033);}
        Assert(m.Colonies.Values.All(c=>c.Alive>=20 && c.Alive<=m.Stats(c.Species).MaximumUnits),"All six established colonies reach 20-28 visible units");
        Assert(m.Bugs.Count(BugManager.Alive)<=m.BattleOptions.HardCap,"Default rendered population obeys global budget");
        var watch=Stopwatch.StartNew();var cpu=new List<double>();
        for(var i=0;i<1000;i++) {var start=Stopwatch.GetTimestamp();m.Advance(.016);cpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);}
        Console.WriteLine($"Larger armies: units={m.Bugs.Count(BugManager.Alive)}, CPU p95={Percentile(cpu,.95):F2}ms, 1000 frames={watch.Elapsed.TotalSeconds:F2}s");
        Assert(Percentile(cpu,.95)<5,"Larger-army simulation fits the existing CPU frame budget");
        var capped=new BugManager(20);capped.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{HardCap=42,SoftCap=36}));capped.SetSurfaces([],new Size(1100,700));
        for(var i=0;i<2500;i++) {foreach(var b in capped.Bugs)b.Protection=1000;capped.Advance(.033);if(capped.Bugs.Count(BugManager.Alive)>42)throw new Exception("Configured hard cap exceeded");}
        var outcomes=new Dictionary<BugSpecies,int>();
        for(var seed=0;seed<6;seed++)
        {
            var battle=War(seed);for(var i=0;i<18000 && battle.CompletedMatches<2;i++)battle.Advance(.08);
            Console.WriteLine($"020 seed {seed}: completed={battle.CompletedMatches}, age={battle.MatchAge:F1}, alive={battle.Bugs.Count(BugManager.Alive)}, attacks={battle.BattleAttacks}");
            if(battle.CompletedMatches<2)foreach(var c in battle.Colonies.Values.Where(c=>!c.Eliminated))Console.WriteLine($"STALL {c.Species}: units={c.Alive} hp={c.Leader.Health} leader={c.Leader.Position} live={BugManager.Alive(c.Leader)} regroup={c.Regrouping} states={string.Join(',',battle.Bugs.Where(b=>b.Species==c.Species && BugManager.Alive(b)).Select(b=>$"{b.Combat}:{b.Position}:{b.TargetUnit?.Species}"))}");
            Assert(battle.CompletedMatches>=2,"Seed "+seed+" completes consecutive autonomous matches");
            foreach(var pair in battle.Victories)outcomes[pair.Key]=outcomes.GetValueOrDefault(pair.Key)+pair.Value;
        }
        Console.WriteLine("020 victories: "+string.Join(", ",outcomes.Select(p=>$"{p.Key}={p.Value}")));
        CheckColonySoak019();
    }
}



