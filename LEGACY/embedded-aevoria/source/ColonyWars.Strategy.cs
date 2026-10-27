using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    private double strategyDelay,nextWarAt=25;
    private static readonly uint[] FactionPalette=[0xffe3b341,0xff64b5f6,0xffd985b5,0xff7dc694,0xffc19af0,0xffed9862,0xff75c9cd,0xffd4cc80,0xffb69b80,0xff9bb4e3,0xffa9d66d,0xffe98291];
    private uint NewFactionColor()
    {
        for(var i=0;i<FactionPalette.Length;i++) {var color=FactionPalette[(nextFaction+i)%FactionPalette.Length];if(!Colonies.Values.Any(c=>!c.Eliminated && c.ColorArgb==color))return color;}
        return FactionPalette[nextFaction%FactionPalette.Length];
    }
    private void InitializeColonyStrategy(BugColony c)
    {c.Strategy=ArmyStrategy.Peace;c.StrategyUntil=MatchAge+10;c.NextCampaign=MatchAge+25+random.Next(10,35);c.CampaignRally=c.Territory;}
    private static ArmyRole AssignRole(BugEntity b)
    {
        if(b.IsLeader)return ArmyRole.HomeGuard;
        var slot=b.UnitId%10;
        if(slot<2)return ArmyRole.HomeGuard;if(slot<4)return ArmyRole.BorderPatrol;if(slot==4)return ArmyRole.Scout;
        if(slot==5 && b.Species!=BugSpecies.Centipedes)return ArmyRole.Reserve;
        return ArmyRole.Expeditionary;
    }
    private static bool Offensive(ArmyStrategy state)=>state is ArmyStrategy.Raiding or ArmyStrategy.Invading;
    private static bool Campaign(ArmyStrategy state)=>state is ArmyStrategy.Preparing or ArmyStrategy.Mobilizing or ArmyStrategy.Raiding or ArmyStrategy.Invading;
    private BugColony? Faction(int id)=>colonyOrder.FirstOrDefault(c=>c.Id==id && !c.Eliminated);
    private bool CanEngage(BugEntity b,BugEntity other)
    {
        if(other.Species==b.Species || !Alive(other))return false;
        var home=Colonies[b.Species];var enemy=Colonies[other.Species];
        if(b.RevengeTimer>0 && b.LastAttacker==other)return true;
        if(other.TargetUnit==b || (other.AttackFlash>0 && other.TargetUnit?.Species==b.Species))return true;
        if(home.Leader.RevengeTimer>0 && home.Leader.LastAttacker==other)return true;
        if(TerritoryOwnerAt(other.Position)==home.Id || (other.Position-home.Territory).Length<190)return true;
        if(b.Species==BugSpecies.Spiders && other.SlowTimer>0 && Webs.Any(w=>w.FactionId==home.Id && (w.Position-other.Position).Length<180))return true;
        return Offensive(home.Strategy) && home.TargetFactionId==enemy.Id && (b.Role==ArmyRole.Expeditionary || b.CampaignDeployed);
    }
    private void AdvanceStrategy(double dt)
    {
        strategyDelay-=dt;if(strategyDelay>0)return;strategyDelay+=1;
        foreach(var c in colonyOrder)
        {
            c.Strength=0;
            foreach(var b in combatUnits)if(Alive(b) && b.Species==c.Species)c.Strength+=(b.IsLeader ? .5 : b.Health/b.MaxHealth)*Stats(c.Species).Damage/Stats(c.Species).AttackCooldown;
        }
        foreach(var c in colonyOrder)
        {
            if(c.Eliminated)continue;
            if(c.Regrouping) {c.Strategy=ArmyStrategy.Recovering;c.TargetFactionId=null;continue;}
            if(!Alive(c.Leader) && c.Alive<14)foreach(var b in combatUnits)if(Alive(b) && b.Species==c.Species)b.Role=ArmyRole.Expeditionary;
            Neighbors(c.Territory,185);var threats=closeUnits.Where(b=>Alive(b) && b.Species!=c.Species).ToArray();
            if(threats.Length>0 && !Campaign(c.Strategy))
            {c.Strategy=ArmyStrategy.Defending;c.TargetFactionId=Colonies[threats[0].Species].Id;c.StrategyUntil=MatchAge+8;continue;}
            if(c.Strategy==ArmyStrategy.Defending)
            {if(MatchAge<c.StrategyUntil)continue;c.Strategy=ArmyStrategy.Recovering;c.StrategyUntil=MatchAge+12;c.TargetFactionId=null;continue;}
            if(c.Strategy is ArmyStrategy.Recovering or ArmyStrategy.Retreating)
            {if(MatchAge<c.StrategyUntil)continue;c.Strategy=ArmyStrategy.Peace;c.NextCampaign=Math.Max(c.NextCampaign,MatchAge+20);c.StrategyUntil=MatchAge+8;}
            if(c.Strategy==ArmyStrategy.Peace)
            {if(MatchAge>=c.StrategyUntil)c.Strategy=ArmyStrategy.Scouting;continue;}
            if(c.Strategy==ArmyStrategy.Scouting)
            {
                if(!BattleOptions.EnableCampaigns || c.Alive<(Alive(c.Leader) ? 14 : 1) || (Alive(c.Leader) && c.Resources<18) || MatchAge<c.NextCampaign || MatchAge<nextWarAt)continue;
                var otherWars=colonyOrder.Count(x=>Campaign(x.Strategy));
                if(otherWars>0 && (otherWars>=2 || MatchAge<180 || c.Resources<60 || random.NextDouble()>.08))continue;
                BugColony? target=null;var best=double.NegativeInfinity;
                foreach(var enemy in colonyOrder)
                {
                    if(otherWars>0 && c.Strength<enemy.Strength*1.65)continue;
                    if(enemy==c || enemy.Eliminated || c.Strength*BattleOptions.Aggression<enemy.Strength*.8)continue;
                    var desirability=2*c.Strength/Math.Max(1,enemy.Strength)+500/Math.Max(150,(enemy.Territory-c.Territory).Length)+c.Rivalries.GetValueOrDefault(enemy.Id)*.025;
                    if(desirability>best) {best=desirability;target=enemy;}
                }
                if(target==null) {c.NextCampaign=MatchAge+15;continue;}
                c.TargetFactionId=target.Id;c.Strategy=ArmyStrategy.Preparing;c.StrategyUntil=MatchAge+8;
                var direction=target.Territory-c.Territory;direction.Normalize();c.CampaignRally=ClampArena(c.Territory+direction*95);
                c.MajorCampaign=Alive(c.Leader) && c.Alive>=22 && c.Resources>=45 && otherWars==0 && random.NextDouble()<.32*BattleOptions.Aggression;
                foreach(var b in combatUnits)if(Alive(b) && b.Species==c.Species)b.CampaignDeployed=!b.IsLeader && c.MajorCampaign && (b.Role is ArmyRole.BorderPatrol or ArmyRole.Reserve or ArmyRole.Scout || (b.Role==ArmyRole.HomeGuard && !b.IsLeader && b.UnitId%3==0));
                c.Resources-=Math.Min(5,c.Resources);c.CampaignStarted=MatchAge;c.CampaignKills=c.KillsAgainst.GetValueOrDefault(target.Id);c.InitialForce=ExpeditionCount(c);
                nextWarAt=MatchAge+35/BattleOptions.WarFrequency;
                continue;
            }
            var opponent=c.TargetFactionId is {} id ? Faction(id) : null;
            if(Campaign(c.Strategy) && opponent==null) {EndCampaign(c,false);continue;}
            if(c.Strategy==ArmyStrategy.Preparing && MatchAge>=c.StrategyUntil)
            {c.Strategy=ArmyStrategy.Mobilizing;c.StrategyUntil=MatchAge+12;Record(ColonyEventType.Mobilized,c,opponent);continue;}
            if(c.Strategy==ArmyStrategy.Mobilizing)
            {
                var together=combatUnits.Count(b=>Alive(b) && b.Species==c.Species && (b.Role==ArmyRole.Expeditionary || b.CampaignDeployed) && (b.Position-c.CampaignRally).Length<120);
                if(together>=Math.Max(Math.Min(3,c.InitialForce),c.InitialForce*.6) || MatchAge>=c.StrategyUntil)
                {c.Strategy=c.MajorCampaign ? ArmyStrategy.Invading : ArmyStrategy.Raiding;c.CampaignStarted=MatchAge;Record(ColonyEventType.InvasionLaunched,c,opponent);}
                continue;
            }
            if(Offensive(c.Strategy) && opponent!=null)
            {
                if(!c.MajorCampaign && c.KillsAgainst.GetValueOrDefault(opponent.Id)-c.CampaignKills>=4) {EndCampaign(c,true);continue;}
                if(ExpeditionCount(c)<Math.Max(1,c.InitialForce*.5) || (Alive(c.Leader) && (c.Resources<5 || c.Leader.Health<c.Leader.MaxHealth*.35)) || MatchAge-c.CampaignStarted>90)
                {EndCampaign(c,false);continue;}
                var delta=MilitaryObjective(opponent)-c.CampaignRally;
                var force=combatUnits.Where(b=>Alive(b) && b.Species==c.Species && (b.Role==ArmyRole.Expeditionary || b.CampaignDeployed)).ToArray();
                var cohesive=force.Count(b=>(b.Position-c.CampaignRally).Length<135)>=Math.Max(2,force.Length*.55);
                if(cohesive && delta.Length>20) {delta.Normalize();c.CampaignRally=ClampArena(c.CampaignRally+delta*Stats(c.Species).Speed*.7);}
            }
        }
    }
    private bool OrderOccupation(BugEntity unit,BugEntity? threat,double attackRange)
    {
        // Hold a nest district while the rest of the army pursues retreating defenders.
        // Immediate threats and injured retreats still take priority.
        if(unit.UnitId%3!=0)return false;
        var colony=Colonies[unit.Species];
        var nest=Settlements.FirstOrDefault(n=>n.OwnerId==colony.TargetFactionId);
        if(nest==null || (unit.Position-nest.Position).Length>240 ||
            (threat!=null && (threat.Position-unit.Position).Length<Math.Max(35,attackRange*1.6)))return false;
        var district=RegionBounds(RegionAt(nest.Position));
        var center=new Point(district.X+district.Width/2,district.Y+district.Height/2);
        unit.TargetUnit=null;unit.Combat=CombatState.Defend;
        unit.OrderPoint=center+new Vector(Math.Cos(unit.UnitId*2.399),Math.Sin(unit.UnitId*2.399))*25;
        return true;
    }
    private int ExpeditionCount(BugColony c)=>combatUnits.Count(b=>Alive(b) && b.Species==c.Species && (b.Role==ArmyRole.Expeditionary || b.CampaignDeployed));
    private void EndCampaign(BugColony c,bool victory)
    {
        var opponent=c.TargetFactionId is {} id ? colonyOrder.FirstOrDefault(x=>x.Id==id) : null;
        Record(victory ? ColonyEventType.BattleWon : ColonyEventType.CampaignWithdrawn,c,opponent,otherId:c.TargetFactionId);
        if(!victory && ExpeditionCount(c)<c.InitialForce*.5)Record(ColonyEventType.MajorDefeat,c,opponent,otherId:c.TargetFactionId);
        foreach(var b in combatUnits)if(b.Species==c.Species)b.CampaignDeployed=false;
        c.Strategy=ArmyStrategy.Retreating;c.StrategyUntil=MatchAge+18;c.NextCampaign=MatchAge+45/BattleOptions.WarFrequency;c.TargetFactionId=null;
    }
    private void OrderStrategicUnit(BugEntity b)
    {
        var c=Colonies[b.Species];var angle=b.UnitId*2.399+MatchAge*.12;
        if(b.IsLeader)
        {
            if(b.Health<b.MaxHealth*.35 && b.TargetUnit is {} threat)
            {var away=b.Position-threat.Position;if(away.Length<1)away=new Vector(1,0);away.Normalize();b.Combat=CombatState.Retreat;b.OrderPoint=ClampArena(b.Position+away*110,48);return;}
            b.Combat=b.TargetUnit==null ? CombatState.Idle : CombatState.Defend;b.OrderPoint=ClampArena(c.Territory+new Vector(Math.Cos(angle),Math.Sin(angle))*22);return;}
        if(b.Species==BugSpecies.Spiders && b.Role!=ArmyRole.HomeGuard && b.TargetUnit==null && (!Campaign(c.Strategy) || !(b.Role==ArmyRole.Expeditionary || b.CampaignDeployed)))
        {OrderWebPatrol(b);return;}
        var radius=b.Role switch {ArmyRole.HomeGuard=>55,ArmyRole.BorderPatrol=>145,ArmyRole.Scout=>220,ArmyRole.Reserve=>85,_=>105};
        var point=c.Territory+new Vector(Math.Cos(angle),Math.Sin(angle))*radius;
        b.Combat=CombatState.Patrol;
        if(c.Strategy is ArmyStrategy.Retreating or ArmyStrategy.Recovering) {point=c.Rally+new Vector(Math.Cos(angle),Math.Sin(angle))*55;b.Combat=CombatState.Regroup;}
        else if((b.Role==ArmyRole.Expeditionary || b.CampaignDeployed) && Campaign(c.Strategy) && MatchAge-c.CampaignStarted>(b.UnitId%4)*.6)
        {point=c.CampaignRally+new Vector(Math.Cos(b.UnitId*2.399),Math.Sin(b.UnitId*2.399))*(b.Species==BugSpecies.Ants ? 30 : b.Species==BugSpecies.Centipedes ? 65 : 45);b.Combat=CombatState.Regroup;}
        b.OrderPoint=ClampArena(point);
    }
}
