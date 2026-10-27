using System.Windows;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckTerritory021()
    {
        var m=Infinite021(410);m.Configure(m.Preferences with{Arcade=m.BattleOptions with{EnableCampaigns=false}});
        Assert(m.Territories.Count==96 && m.Settlements.Count==6 && m.Colonies.Values.All(c=>m.TerritoryOwnerAt(c.Territory)==c.Id),"Every faction starts with a real owned homeland and settlement on the persistent grid");
        Assert(m.Colonies.Values.Select(c=>c.ColorArgb).Distinct().Count()==6,"Starting faction identities have distinct persistent colors");
        var defender=m.Colonies[BugSpecies.Ants];var attacker=m.Colonies[BugSpecies.Centipedes];var nest=m.Settlements.Single(n=>n.OwnerId==defender.Id);var region=m.Territories[m.RegionAt(nest.Position)];
        foreach(var c in m.Colonies.Values)c.Resources=-1000;
        // Park the original armies away from this controlled occupation test.
        foreach(var b in m.Bugs) {b.Position=b.OrderPoint=new Point(800,500);b.ThinkTimer=10000;b.AttackTimer=10000;b.Protection=1000;}
        var invaders=Enumerable.Range(0,4).Select(i=>Fighter(m,attacker.Species,nest.Position+new Vector(i*2,0))).ToArray();
        foreach(var b in invaders) {b.ThinkTimer=10000;b.AttackTimer=10000;b.Protection=1000;}
        attacker.Strategy=ArmyStrategy.Invading;attacker.TargetFactionId=defender.Id;attacker.StrategyUntil=10000;attacker.CampaignStarted=10000;
        // A single enemy and brief visits cannot conquer a settlement.
        foreach(var b in invaders.Skip(1))b.Position=b.OrderPoint=new Point(800,500);
        for(var i=0;i<100;i++)m.Advance(.033);
        Assert(region.OwnerId==defender.Id && nest.OwnerId==defender.Id,"A lone trespasser cannot capture territory or a nest");
        foreach(var b in invaders)b.Position=b.OrderPoint=nest.Position+new Vector((b.UnitId%4)*2,0);
        // Ensure a valid adjacent foothold while the strategy test parks campaign movement.
        var neighbor=region.Id%12>0 ? region.Id-1 : region.Id+1;m.Territories[neighbor].OwnerId=attacker.Id;
        for(var i=0;i<150;i++)m.Advance(.033);
        Assert(region.OwnerId==defender.Id && region.Occupation>0,"A superior force must maintain occupation rather than flipping ownership on contact");
        for(var i=0;i<650;i++)m.Advance(.033);
        Assert(region.OwnerId==attacker.Id && nest.OwnerId==attacker.Id && m.History.Any(e=>e.Type==ColonyEventType.NestConquered && e.FactionId==attacker.Id && e.OtherFactionId==defender.Id),"Sustained military occupation changes actual nest ownership and records both participants");
        Assert(BugManager.Alive(defender.Leader) && defender.Alive>0,"Nest conquest preserves surviving leaders and soldiers");
        // Move conquerors out and let the original owner reclaim the same structure.
        foreach(var b in invaders)b.Position=b.OrderPoint=new Point(800,500);
        var liberators=Enumerable.Range(0,4).Select(i=>Fighter(m,defender.Species,nest.Position+new Vector(i*2,0))).ToArray();
        foreach(var b in liberators) {b.ThinkTimer=10000;b.AttackTimer=10000;b.Protection=1000;}
        m.Territories[neighbor].OwnerId=defender.Id;
        for(var i=0;i<760;i++)m.Advance(.033);
        Assert(nest.OwnerId==defender.Id && m.History.Any(e=>e.Type==ColonyEventType.HomelandReclaimed && e.FactionId==defender.Id),"An original civilization can reclaim its lost homeland through sustained control");
        var guarded=Infinite021(411);guarded.Configure(guarded.Preferences with{Arcade=guarded.BattleOptions with{EnableCampaigns=false}});
        foreach(var c in guarded.Colonies.Values)c.Resources=-1000;
        var own=guarded.Colonies[BugSpecies.Ants];var ownNest=guarded.Settlements.Single(n=>n.OwnerId==own.Id);var cell=guarded.Territories[guarded.RegionAt(ownNest.Position)];var invader=guarded.Colonies[BugSpecies.Roaches];
        foreach(var b in guarded.Bugs) {b.Position=b.OrderPoint=new Point(800,500);b.ThinkTimer=10000;b.AttackTimer=10000;b.Protection=1000;}
        for(var i=0;i<4;i++) {var b=Fighter(guarded,invader.Species,ownNest.Position+new Vector(i*2,0));b.Protection=1000;b.AttackTimer=b.ThinkTimer=10000;}
        for(var i=0;i<6;i++) {var b=Fighter(guarded,own.Species,ownNest.Position+new Vector(0,i*2));b.Protection=1000;b.AttackTimer=b.ThinkTimer=10000;}
        guarded.Territories[cell.Id%12>0 ? cell.Id-1 : cell.Id+1].OwnerId=invader.Id;
        for(var i=0;i<1000;i++)guarded.Advance(.033);
        Assert(cell.OwnerId==own.Id && cell.Contested && cell.Occupation==0,"Present defenders contest a region and stop an inferior occupation force");
    }
}
