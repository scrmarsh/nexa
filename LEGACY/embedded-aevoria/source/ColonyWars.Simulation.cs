using System.Windows;

namespace SessionDashboard;
// Shared fixed-cadence simulation. The renderer and timer remain the dashboard's existing ones.
internal sealed partial class BugManager
{
    internal ArcadeSettings BattleOptions { get; private set; }=new ArcadeSettings().Safe();
    internal readonly Dictionary<BugSpecies,BugColony> Colonies=[];
    internal readonly List<CombatWebProjectile> Projectiles=[];
    internal readonly Queue<BattleAnnouncement> Announcements=[];
    internal readonly Dictionary<BugSpecies,int> Victories=[];
    internal BattleAnnouncement? CurrentAnnouncement;
    internal double AnnouncementAge,MatchAge,VictoryAge;
    internal int MatchNumber;
    internal BattlePhase Phase;
    internal int CompletedMatches,Draws;
    internal long BattleAttacks;
    private readonly Dictionary<(int,int),List<BugEntity>> nearbyCells=[];
    private readonly List<CombatHit> pendingDamage=new(120);
    private readonly List<BugEntity> closeUnits=new(120);
    private readonly List<BugColony> colonyOrder=new(6);
    private readonly List<BugEntity> combatUnits=new(120);
    private double waveDelay,dripDelay;
    internal readonly List<ColonyEvent> History=[];
    private int nextFaction;
    private BugColony? pendingColony;
    private int pendingSoldiers,waveIndex,nextUnit;
    private Rect previousArena;
    internal static bool Alive(BugEntity b)=>b.Health>0 && b.DefeatAge<=0 && b.Combat!=CombatState.Defeated;
    internal bool IsInfinite => IsArcade && BattleOptions.Mode==ArcadeRuleSet.InfiniteWarfare;
    internal bool WorldPaused;
    internal string WorldTimeLabel=>$"{(long)MatchAge/3600:00}:{(long)MatchAge/60%60:00}:{(long)MatchAge%60:00}";
    internal Guid WorldId=Guid.NewGuid();
    internal Rect? FixedWorldBounds;
    private double nextEmergence=120;
    private int lastDominantFaction;
    internal Rect BattleArena => FixedWorldBounds ?? (VisibleBounds.Width>40 && VisibleBounds.Height>40 ? VisibleBounds : new Rect(Bounds));
    internal static string LeaderTitle(BugSpecies species)=>species switch
    { BugSpecies.Ants=>"Queen Ant",BugSpecies.Spiders=>"Mama Spider",BugSpecies.Roaches=>"King Roach",BugSpecies.Ladybugs=>"Queen Ladybug",BugSpecies.Centipedes=>"King Centipede",_=>"Queen Fly" };
    internal static string ColonyTitle(BugSpecies species)=>species switch
    {BugSpecies.Flies=>"Fly",BugSpecies.Ladybugs=>"Ladybug",BugSpecies.Centipedes=>"Centipede",BugSpecies.Spiders=>"Spider",BugSpecies.Roaches=>"Roach",_=>"Ant"};
    internal SpeciesCombatStats Stats(BugSpecies species)=>BattleOptions.Species![species];
    private void ResetBattleState()
    {
        Colonies.Clear();Projectiles.Clear();Announcements.Clear();CurrentAnnouncement=null;pendingDamage.Clear();closeUnits.Clear();colonyOrder.Clear();combatUnits.Clear();nearbyCells.Clear();
        Territories.Clear();Settlements.Clear();FactionColors.Clear();FactionSpecies.Clear();History.Clear();factionNames.Clear();nextFaction=0;Victories.Clear();MatchNumber=CompletedMatches=Draws=0;BattleAttacks=0;MatchAge=VictoryAge=AnnouncementAge=waveDelay=dripDelay=0;
        pendingColony=null;pendingSoldiers=waveIndex=nextUnit=0;previousArena=Rect.Empty;Phase=BattlePhase.Mustering;
    }
    internal void StartBattle()
    {
        Bugs.Clear();Webs.Clear();Splats.Clear();Projectiles.Clear();Colonies.Clear();colonyOrder.Clear();nearbyCells.Clear();pendingDamage.Clear();
        Announcements.Clear();CurrentAnnouncement=null;AnnouncementAge=MatchAge=VictoryAge=0;Phase=BattlePhase.Mustering;
        pendingColony=null;pendingSoldiers=waveIndex=0;waveDelay=.15;dripDelay=0;MatchNumber++;
        var arena=BattleArena;previousArena=arena;
        var offset=Range(0,1);var center=new Point(arena.Left+arena.Width/2,arena.Top+arena.Height/2);
        // Six separated edge sectors with a new random rotation and species order each match.
        var order=BugHabitats.Species.ToArray();for(var n=order.Length-1;n>0;n--) {var j=random.Next(n+1);(order[n],order[j])=(order[j],order[n]);}
        for(var i=0;i<order.Length;i++)
        {
            var edge=EdgePoint(arena,(offset+i/6d)%1);var inward=center-edge;if(inward.Length>.01)inward.Normalize();
            var colony=new BugColony(order[i]) {Territory=ClampArena(edge+inward*52,48),NextWave=0,Id=++nextFaction,Name=NewArmyName(),ColorArgb=NewFactionColor()};
            InitializeColonyStrategy(colony);Colonies.Add(order[i],colony);colonyOrder.Add(colony);colony.Leader=SpawnSoldier(colony,true);Record(ColonyEventType.Founded,colony);
        }
        if(IsInfinite)InitializeTerritories();
    }
    private BugEntity SpawnSoldier(BugColony colony,bool leader=false)
    {
        var stats=Stats(colony.Species);var id=++nextUnit;
        var angle=id*2.399;var radius=leader ? 0 : 15+(id%4)*7;
        var origin=leader ? colony.Territory : colony.Rally;
        var b=new BugEntity {Species=colony.Species,IsLeader=leader,UnitId=id,
            Position=ClampArena(origin+new Vector(Math.Cos(angle),Math.Sin(angle))*radius),
            Health=leader ? stats.LeaderHealth : stats.Health,MaxHealth=leader ? stats.LeaderHealth : stats.Health,
            Protection=leader ? 5 : .7,State=colony.Species==BugSpecies.Flies ? BugState.Flying : BugState.Crossing,
            Speed=stats.Speed*(leader ? .58 : 1),Combat=leader ? CombatState.Idle : CombatState.Defend,
            AbilityTimer=Range(1,stats.AbilityCooldown),AttackTimer=Range(.1,.5)};
        b.Role=AssignRole(b);b.OrderPoint=b.Position;b.CanReproduce=leader || (colony.Species!=BugSpecies.Ants && id%4==0);
        b.Name=WarriorNames[(id-1)%WarriorNames.Length];
        Bugs.Add(b);colony.Alive++;return b;
    }
    private Point ClampArena(Point p,double inset=12)
    {
        var r=BattleArena;inset=Math.Min(inset,Math.Min(r.Width,r.Height)/3);
        return new Point(Math.Clamp(p.X,r.Left+inset,Math.Max(r.Left+inset,r.Right-inset)),Math.Clamp(p.Y,r.Top+inset,Math.Max(r.Top+inset,r.Bottom-inset)));
    }
    private void RefitArena()
    {
        if(FixedWorldBounds!=null)return;
        var r=BattleArena;if(r==previousArena || previousArena.IsEmpty)return;
        Point Remap(Point p)=>new(r.Left+(p.X-previousArena.Left)/Math.Max(1,previousArena.Width)*r.Width,r.Top+(p.Y-previousArena.Top)/Math.Max(1,previousArena.Height)*r.Height);
        foreach(var b in Bugs)b.Position=ClampArena(Remap(b.Position),b.IsLeader ? 48 : 12);
        foreach(var c in colonyOrder)c.Territory=ClampArena(Remap(c.Territory));
        Webs.Clear();Projectiles.Clear();previousArena=r;
    }
    private void RebuildNearby()
    {
        foreach(var list in nearbyCells.Values)list.Clear();combatUnits.Clear();
        foreach(var b in Bugs)
        {
            if(!Alive(b))continue;combatUnits.Add(b);
            var key=((int)(b.Position.X/128),(int)(b.Position.Y/128));
            if(!nearbyCells.TryGetValue(key,out var list))nearbyCells[key]=list=new List<BugEntity>(16);
            list.Add(b);
        }
    }
    private void Neighbors(Point p,double radius)
    {
        closeUnits.Clear();var x=(int)(p.X/128);var y=(int)(p.Y/128);var cells=(int)Math.Ceiling(radius/128);
        for(var xx=x-cells;xx<=x+cells;xx++)for(var yy=y-cells;yy<=y+cells;yy++)
            if(nearbyCells.TryGetValue((xx,yy),out var list))foreach(var b in list)
                if((b.Position-p).LengthSquared<=radius*radius)closeUnits.Add(b);
    }
    private void Reinforce(double dt)
    {
        waveDelay-=dt;dripDelay-=dt;
        var alive=Bugs.Count; // Include fading bodies in the rendered-unit budget.
        if(alive>=BattleOptions.HardCap) {pendingSoldiers=0;pendingColony=null;return;}
        if(pendingSoldiers>0 && pendingColony is {} pending)
        {
            if(pending.Eliminated || !OwnsSettlement(pending) || pending.Regrouping || !Alive(pending.Leader) || pending.Resources<1 || pending.Alive>=Stats(pending.Species).MaximumUnits || Civilization?.CanReproduce(pending.Id,pending.Alive)==false) {pendingSoldiers=0;pendingColony=null;return;}
            // Drip a wave over several frames, at most two births per update.
            for(var spawned=0;spawned<2 && pendingSoldiers>0 && dripDelay<=0 && alive<BattleOptions.HardCap && pending.Alive<Stats(pending.Species).MaximumUnits && pending.Resources>=1 && Civilization?.CanReproduce(pending.Id,pending.Alive)!=false;spawned++)
            {SpawnSoldier(pending);pending.Resources--;pendingSoldiers--;dripDelay+=.035;alive++;}
            if(pendingSoldiers==0)pendingColony=null;return;
        }
        if(waveDelay>0)return;
        for(var scan=0;scan<colonyOrder.Count;scan++)
        {
            var colony=colonyOrder[waveIndex++%colonyOrder.Count];
            if(colony.Eliminated || !OwnsSettlement(colony) || colony.Regrouping || !Alive(colony.Leader) || colony.Resources<1 || colony.Alive>=Stats(colony.Species).MaximumUnits || Civilization?.CanReproduce(colony.Id,colony.Alive)==false)continue;
            pendingColony=colony;pendingSoldiers=Math.Min(BattleOptions.GroupSize+random.Next(-1,2),Stats(colony.Species).MaximumUnits-colony.Alive);
            pendingSoldiers=Math.Clamp(pendingSoldiers,1,6);dripDelay=0;
            var pressure=Math.Clamp((alive-BattleOptions.SoftCap*.6)/(BattleOptions.HardCap-BattleOptions.SoftCap*.6),0,1);
            waveDelay=BattleOptions.SpawnInterval*(1+pressure*5)*Stats(colony.Species).ReinforcementRate*(SpawnPressure ? 1.5 : 1);return;
        }
        waveDelay=.3;
    }
    internal void AdvanceBattle(double dt)
    {
        if(Colonies.Count==0)StartBattle();RefitArena();MatchAge+=dt;
        AdvanceAnnouncements(dt);
        if(Phase==BattlePhase.Victory)
        {
            VictoryAge+=dt;
            foreach(var b in Bugs) {b.CrawlPhase+=dt*5;b.Angle+=Math.Sin(b.UnitId+VictoryAge*4)*dt*12;if(!Alive(b))b.DefeatAge+=dt;}
            for(var i=Bugs.Count-1;i>=0;i--)if(!Alive(Bugs[i]) && Bugs[i].DefeatAge>1.2)Bugs.RemoveAt(i);
            for(var i=Webs.Count-1;i>=0;i--) {Webs[i].Age+=dt;if(Webs[i].Age>90)Webs.RemoveAt(i);}
            for(var i=Splats.Count-1;i>=0;i--) {Splats[i].Age+=dt;if(Splats[i].Age>2.5)Splats.RemoveAt(i);}
            // Finish queued conquests before showing a full five seconds of victory.
            if(!IsInfinite && VictoryAge>=6 && CurrentAnnouncement?.Victory==true && AnnouncementAge>=5)StartBattle();
            return;
        }
        Phase=MatchAge<5 ? BattlePhase.Mustering : BattlePhase.Fighting;
        RebuildNearby();AdvanceColonies(dt);if(IsInfinite) {AdvanceStrategy(dt);AdvanceTerritories(dt);}
        Reinforce(dt);
        foreach(var b in Bugs)
        {
            if(!Alive(b)) {b.DefeatAge+=dt;b.Position+=new Vector(Math.Cos(b.Angle*Math.PI/180),Math.Sin(b.Angle*Math.PI/180))*dt*32;continue;}
            b.PreviousCombatPosition=b.Position;
            b.Age+=dt;b.Protection=Math.Max(0,b.Protection-dt);b.AttackTimer-=dt;b.AbilityTimer-=dt;b.ThinkTimer-=dt;
            b.HitFlash=Math.Max(0,b.HitFlash-dt);b.AttackFlash=Math.Max(0,b.AttackFlash-dt);b.HealFlash=Math.Max(0,b.HealFlash-dt);
            b.RevengeTimer=Math.Max(0,b.RevengeTimer-dt);b.SlowTimer=Math.Max(0,b.SlowTimer-dt);b.StunTimer=Math.Max(0,b.StunTimer-dt);b.WebImmunity=Math.Max(0,b.WebImmunity-dt);
            if(b.RevengeTimer<=0)b.LastAttacker=null;
            if(b.AttackFlash<=0)b.AttackVictim=null;
            if(b.TargetUnit!=null && !Alive(b.TargetUnit))b.TargetUnit=null;
            if(b.ThinkTimer<=0) {ThinkCombat(b);b.ThinkTimer=.12+(b.UnitId%4)*.015;}
            MoveCombat(b,dt);
        }
        foreach(var b in combatUnits)if(Alive(b))SeparateCombat(b);
        foreach(var b in Bugs)if(Alive(b))AttackCombat(b);
        AdvanceProjectiles(dt);
        // All attacks selected this frame resolve even when their attacker also dies this frame.
        foreach(var hit in pendingDamage)ResolveDamage(hit.Attacker,hit.Target,hit.Amount);
        pendingDamage.Clear();
        for(var i=Bugs.Count-1;i>=0;i--)if(!Alive(Bugs[i]) && Bugs[i].DefeatAge>1.2)Bugs.RemoveAt(i);
        for(var i=Splats.Count-1;i>=0;i--) {Splats[i].Age+=dt;if(Splats[i].Age>2.5)Splats.RemoveAt(i);}
        AdvanceSharedWebs(dt);
        var survivors=0;BugSpecies? winner=null;
        foreach(var c in colonyOrder)if(!c.Eliminated) {survivors++;winner=c.Species;}
        if(IsInfinite)AdvanceInfiniteWorld();
        else if(survivors<=1)FinishBattle(survivors==1 ? winner : null);
    }
    private void ThinkCombat(BugEntity b)
    {
        var stats=Stats(b.Species);var colony=Colonies[b.Species];var leader=colony.Leader;var rally=colony.Rally;
        Neighbors(b.Position,stats.DetectionRange);
        int allies=0,enemies=0;BugEntity? target=null;var score=double.PositiveInfinity;var separation=new Vector();
        foreach(var other in closeUnits)
        {
            if(other==b || !Alive(other))continue;
            var distance=(other.Position-b.Position).LengthSquared;
            if(other.Species==b.Species) {allies++;if(distance<196) {var away=b.Position-other.Position;if(away.Length>.01)separation+=away/Math.Max(1,away.Length)*5;}continue;}
            if(IsInfinite && !CanEngage(b,other))continue;
            enemies++;
            var candidate=distance*(other.IsLeader ? .7 : 1);
            if(b.Species==BugSpecies.Centipedes && !other.IsLeader)candidate*=.8;
            if(candidate<score) {score=candidate;target=other;}
        }
        if(b.RevengeTimer>0 && b.LastAttacker is {} attacker && Alive(attacker) && (attacker.Position-b.Position).Length<stats.DetectionRange*1.2)target=attacker;
        var danger=Alive(leader) ? leader.LastAttacker : null;
        if(leader.RevengeTimer>0 && danger!=null && Alive(danger) && (danger.Position-leader.Position).Length<180 && (b.Position-leader.Position).Length<240) {target=danger;b.Combat=CombatState.Defend;}
        if(b.Species==BugSpecies.Spiders && (target==null || target.SlowTimer<=0))
            foreach(var other in closeUnits)if(Alive(other) && other.Species!=b.Species && other.SlowTimer>0) {target=other;break;}
        var acquired=target!=null && b.TargetUnit!=target;
        b.TargetUnit=target;
        if(b.IsLeader)
        {
            if(IsInfinite) {OrderStrategicUnit(b);return;}
            if(b.Health<b.MaxHealth*.35 || enemies>allies+1)
            {
                b.Combat=CombatState.Retreat;
                var retreatCenter=new Point(BattleArena.Left+BattleArena.Width/2,BattleArena.Top+BattleArena.Height/2);
                b.OrderPoint=colony.Territory+(retreatCenter-colony.Territory)*Math.Clamp((MatchAge-45)/150,0,.97);
            }
            else
            {
                b.Combat=CombatState.Regroup;
                var center=new Point(BattleArena.Left+BattleArena.Width/2,BattleArena.Top+BattleArena.Height/2);
                var advance=Math.Clamp((MatchAge-5)/90,.05,.98);
                b.OrderPoint=colony.Territory+(center-colony.Territory)*advance+new Vector(Math.Sin(MatchAge*.3+b.UnitId)*22,Math.Cos(MatchAge*.23+b.UnitId)*18);
            }
            return;
        }
        var committed=IsInfinite && colony.MajorCampaign && colony.Strategy==ArmyStrategy.Invading && (b.Role==ArmyRole.Expeditionary || b.CampaignDeployed);
        if(colony.Regrouping || b.RetreatUntil>MatchAge ||
            ((b.Health<b.MaxHealth*(b.Species==BugSpecies.Roaches ? .4 : .22) || enemies>(committed ? allies*1.7+4 : allies+3)) && target!=null && MatchAge>=b.NextRetreat))
        {
            if(b.Combat!=CombatState.Retreat)Record(ColonyEventType.Retreat,colony,target==null ? null : Colonies[target.Species],b.UnitId);
            if(b.RetreatUntil<=MatchAge) {b.RetreatUntil=MatchAge+2.5;b.NextRetreat=MatchAge+10;}
            b.Combat=CombatState.Retreat;b.OrderPoint=rally;
            if(target!=null && (b.Position-rally).Length<60)
            {var away=b.Position-target.Position;if(away.Length<1)away=new Vector(1,0);away.Normalize();b.OrderPoint=ClampArena(b.Position+away*100);}
            return;
        }
        if(committed && OrderOccupation(b,target,stats.AttackRange))return;
        if(IsInfinite && target==null) {OrderStrategicUnit(b);return;}
        if(b.Species==BugSpecies.Spiders && target==null && (MatchAge<25 || (MatchAge<90 && b.UnitId%3!=0)) && OrderWebPatrol(b))return;
        var formation=rally+new Vector(Math.Cos(b.UnitId*2.399+MatchAge*.25),Math.Sin(b.UnitId*2.399+MatchAge*.25))*(32+b.UnitId%4*12);
        // Four of five fly slots follow the queen or a forward squad captain; the fifth scouts.
        var follows=b.Species==BugSpecies.Flies && b.UnitId%5!=0;
        if(follows && target==null)
        {
            BugEntity? captain=Alive(leader) ? leader : null;
            foreach(var ally in combatUnits)if(Alive(ally) && ally.Species==b.Species && !ally.IsLeader && ally.UnitId%5==0) {captain=ally;break;}
            if(captain!=null && captain!=b) {b.Combat=CombatState.Regroup;b.OrderPoint=captain.Position+new Vector(Math.Cos(b.UnitId),Math.Sin(b.UnitId))*28+separation;return;}
        }
        if(target!=null)
        {
            if(acquired && b.LastAttacker!=target && b.Combat!=CombatState.Defend) {b.Combat=CombatState.Detect;b.OrderPoint=target.Position;return;}
            b.Combat=(target.Position-b.Position).Length<=stats.AttackRange ? CombatState.Attack : b.Combat==CombatState.Defend ? CombatState.Defend : CombatState.Pursue;
            var offset=b.Species==BugSpecies.Ants ? new Vector(Math.Cos(b.UnitId*2.399),Math.Sin(b.UnitId*2.399))*stats.AttackRange*.65 : new Vector();
            b.OrderPoint=target.Position+offset+separation;
            if(b.Species==BugSpecies.Ladybugs && b.AttackTimer>stats.AttackCooldown*.6 && (target.Position-b.Position).Length<stats.AttackRange*1.6)
            {
                var away=b.Position-target.Position;if(away.Length>.01)away.Normalize();else away=new Vector(1,0);
                var side=new Vector(-away.Y,away.X)*(b.UnitId%2==0 ? 1 : -1);
                b.OrderPoint=ClampArena(b.Position+side*22+away*6);b.Combat=CombatState.Regroup;
            }
            if(b.Species==BugSpecies.Flies && b.AttackTimer>stats.AttackCooldown*.45) {b.Combat=CombatState.Regroup;b.OrderPoint=b.Position+(b.Position-target.Position)*.9+new Vector(Math.Sin(b.Age*3)*20,Math.Cos(b.Age*3)*20);}
            
            return;
        }
        BugColony? enemy=null;var nearest=double.PositiveInfinity;
        foreach(var c in colonyOrder)if(!c.Eliminated && c.Species!=b.Species)
        {var d=(c.Objective-b.Position).LengthSquared;if(d<nearest) {nearest=d;enemy=c;}}
        if(enemy==null) {b.Combat=CombatState.Idle;b.OrderPoint=formation;return;}
        b.Combat=CombatState.Patrol;
        if(MatchAge<4) {b.Combat=CombatState.Defend;b.OrderPoint=formation;return;}
        if(b.Species==BugSpecies.Ladybugs && MatchAge<30) {b.Combat=CombatState.Defend;b.OrderPoint=formation+(enemy.Objective-leader.Position)*.25;return;}
        if((b.Species is BugSpecies.Ants or BugSpecies.Ladybugs) && (b.Position-leader.Position).Length>320 && allies<2)
        {b.Combat=CombatState.Regroup;b.OrderPoint=formation;return;}
        b.OrderPoint=enemy.Objective+new Vector(Math.Sin(b.UnitId)*35,Math.Cos(b.UnitId)*35)+separation;
    }
    private void MoveCombat(BugEntity b,double dt)
    {
        if(b.StunTimer>0)return;
        var delta=b.OrderPoint-b.Position;var speed=b.Speed;
        if(b.SlowTimer>0)speed*=b.Species==BugSpecies.Roaches ? .8 : .55;
        if(b.Combat==CombatState.Retreat)speed*=1.15;
        if(delta.Length<=2) {if(b.TargetUnit is {} facing && Alive(facing))b.Angle=Math.Atan2(facing.Position.Y-b.Position.Y,facing.Position.X-b.Position.X)*180/Math.PI;return;}
        delta.Normalize();
        // A small continuous lateral sway keeps flight smooth without random destinations.
        if(b.Species==BugSpecies.Flies)delta+=new Vector(-delta.Y,delta.X)*Math.Sin(b.Age*5+b.UnitId)*.2;
        var step=delta*Math.Min(speed*dt,(b.OrderPoint-b.Position).Length);
        b.Position=ClampArena(b.Position+step,b.IsLeader ? 48 : 12);b.Angle=Math.Atan2(step.Y,step.X)*180/Math.PI;b.CrawlPhase+=step.Length*.35;
    }
    private void SeparateCombat(BugEntity b)
    {
        Neighbors(b.Position,18);var push=new Vector();
        foreach(var other in closeUnits)
        {
            if(other==b || !Alive(other))continue;var away=b.Position-other.Position;var distance=away.Length;
            var radius=b.IsLeader || other.IsLeader ? 16 : 10;
            if(distance>=radius)continue;
            if(distance<.01)away=new Vector(Math.Sin(b.UnitId),Math.Cos(b.UnitId));else away/=distance;
            push+=away*Math.Min(1.2,(radius-distance)*.2);
        }
        b.Position=ClampArena(b.Position+push,b.IsLeader ? 48 : 12);
    }
    private void AttackCombat(BugEntity b)
    {
        var stats=Stats(b.Species);
        if(b.StunTimer>0)return;
        if(b.Species==BugSpecies.Ladybugs && b.AbilityTimer<=0)
        {
            var healed=0;
            foreach(var ally in Bugs)if(Alive(ally) && ally.Species==b.Species && (ally.Position-b.Position).LengthSquared<10000 && ally.Health<ally.MaxHealth && healed<2)
            {ally.Health=Math.Min(ally.MaxHealth,ally.Health+(ally.IsLeader ? 1.8 : 3));ally.HealFlash=.35;healed++;}
            b.AbilityTimer=stats.AbilityCooldown;
        }
        var target=b.TargetUnit;if(target==null || !Alive(target) || target.Species==b.Species || (IsInfinite && !CanEngage(b,target)))return;
        var distance=(target.Position-b.Position).Length;
        if(b.Species==BugSpecies.Spiders && b.AbilityTimer<=0 && distance<145 && Projectiles.Count<48)
        {Projectiles.Add(new CombatWebProjectile {Position=b.Position,Previous=b.Position,Target=target,Species=b.Species,Attacker=b});b.AbilityTimer=stats.AbilityCooldown;}
        if(b.Species==BugSpecies.Centipedes && !b.IsLeader && b.AbilityTimer<=0 && distance>stats.AttackRange && distance<85)
        {var d=target.Position-b.Position;d.Normalize();b.Position=ClampArena(b.Position+d*Math.Min(32,distance-stats.AttackRange*.75));b.AbilityTimer=stats.AbilityCooldown;distance=(target.Position-b.Position).Length;}
        if(distance>stats.AttackRange || b.AttackTimer>0 || b.Protection>4.5)return;
        var damage=stats.Damage*(b.IsLeader ? 1.15 : 1);
        if(b.Species==BugSpecies.Spiders && target.SlowTimer>0)damage*=1.15;
        if(b.Species==BugSpecies.Ants)
        {
            var supporters=0;Neighbors(b.Position,65);foreach(var ally in closeUnits)if(ally!=b && ally.Species==b.Species)supporters++;
            damage*=1+Math.Min(.24,supporters*.06);
        }
        b.Angle=Math.Atan2(target.Position.Y-b.Position.Y,target.Position.X-b.Position.X)*180/Math.PI;b.Combat=CombatState.Attack;b.AttackTimer=stats.AttackCooldown;b.AttackFlash=.18;b.AttackVictim=target;
        pendingDamage.Add(new CombatHit(target,b,damage));BattleAttacks++;
    }
    private void AdvanceProjectiles(double dt)
    {
        for(var i=Projectiles.Count-1;i>=0;i--)
        {
            var p=Projectiles[i];p.Age+=dt;
            if(p.Age>1.2 || !Alive(p.Target)) {Projectiles.RemoveAt(i);continue;}
            p.Previous=p.Position;var d=p.Target.Position-p.Position;
            if(d.Length<320*dt+5)
            {
                var t=p.Target;
                if(t.Protection<=0 && t.WebImmunity<=0 && t.Species!=p.Species)
                {t.SlowTimer=t.Species==BugSpecies.Roaches ? .8 : 1.6;t.StunTimer=t.Species==BugSpecies.Roaches ? .08 : .35;t.WebImmunity=3.8;t.HitFlash=.3;if(p.Attacker is {} shooter && Alive(shooter)) {t.LastAttacker=shooter;t.RevengeTimer=2;t.ThinkTimer=0;}}
                Projectiles.RemoveAt(i);continue;
            }
            d.Normalize();p.Position+=d*320*dt;
        }
    }
    internal bool ApplyBattleDamage(BugEntity? attacker,BugEntity target,double amount)
    {
        return ResolveDamage(attacker,target,amount);
    }
    private bool ResolveDamage(BugEntity? attacker,BugEntity target,double amount)
    {
        var source=attacker?.Species;
        if(WorldPaused || !Alive(target) || target.Protection>0 || !double.IsFinite(amount) || amount<=0 || source==target.Species || Phase==BattlePhase.Victory)return false;
        if(attacker!=null) {target.LastAttacker=attacker;target.RevengeTimer=2;target.ThinkTimer=0;if(IsInfinite) {var hurt=Colonies[target.Species];var enemy=Colonies[attacker.Species];hurt.Rivalries[enemy.Id]=Math.Min(100,hurt.Rivalries.GetValueOrDefault(enemy.Id)+amount*.05);}}
        if(target.Species==BugSpecies.Roaches)amount*=.82;
        if(target.Species==BugSpecies.Ladybugs)amount*=.86;
        target.Health=Math.Max(0,target.Health-amount);target.HitFlash=.3;
        if(target.Health>0)return true;
        target.Combat=CombatState.Defeated;target.DefeatAge=.001;target.TargetUnit=null;target.LastAttacker=null;target.AttackVictim=null;
        var colony=Colonies[target.Species];colony.Alive=Math.Max(0,colony.Alive-1);
        if(attacker!=null)
        {
            var credited=Colonies[attacker.Species];credited.Kills++;credited.KillsAgainst[colony.Id]=credited.KillsAgainst.GetValueOrDefault(colony.Id)+1;attacker.ConfirmedKills++;
            if(attacker.ConfirmedKills==5)
            {attacker.LegendaryTitle="the Unbroken";Record(ColonyEventType.LegendaryWarrior,Colonies[attacker.Species],colony,attacker.UnitId);}
        }
        if(Splats.Count>=80)Splats.RemoveAt(0);Splats.Add(new BugSplat {Position=target.Position,Species=target.Species});
        if(target.IsLeader)
        {
            colony.Regrouping=true;colony.RecoveryTime=0;colony.DefeatTime=MatchAge;
            Record(ColonyEventType.LeaderKilled,colony,attacker==null ? null : Colonies[attacker.Species],target.UnitId);
            if(colony.Alive>0)Record(ColonyEventType.Retreat,colony,attacker==null ? null : Colonies[attacker.Species]);
            Announcements.Enqueue(new BattleAnnouncement(ConquestMessage(source,target.Species),source));
            foreach(var soldier in Bugs)if(soldier.Species==target.Species && Alive(soldier))
            {soldier.Combat=CombatState.Retreat;soldier.RetreatUntil=MatchAge+12;soldier.ThinkTimer=0;}
        }
        if(colony.Alive==0)Extinguish(colony);
        return true;
    }
    internal string ConquestMessage(BugSpecies? source,BugSpecies defeated)
    {
        if(source==null)return $"THE {ColonyTitle(defeated).ToUpperInvariant()} COLONY HAS FALLEN!";
        var verbs=new[]{"HAVE OVERTHROWN","HAVE DEFEATED"};
        return $"{source.Value.ToString().ToUpperInvariant()} {verbs[random.Next(verbs.Length)]} THE {ColonyTitle(defeated).ToUpperInvariant()} COLONY \u2014 {LeaderTitle(defeated).ToUpperInvariant()} HAS FALLEN!";
    }
    private void FinishBattle(BugSpecies? winner)
    {
        Phase=BattlePhase.Victory;VictoryAge=0;CompletedMatches++;Projectiles.Clear();pendingDamage.Clear();pendingSoldiers=0;pendingColony=null;
        if(winner is {} species) {Record(ColonyEventType.Victory,Colonies[species]);Victories[species]=Victories.GetValueOrDefault(species)+1;Announcements.Enqueue(new BattleAnnouncement($"THE {ColonyTitle(species).ToUpperInvariant()} COLONY HAS CONQUERED THE BATTLEFIELD!",species,true));}
        else {Draws++;Announcements.Enqueue(new BattleAnnouncement("NO COLONY SURVIVES \u2014 THE BATTLE IS A DRAW!",null,true));}
    }
    private void AdvanceAnnouncements(double dt)
    {
        AnnouncementAge+=dt;
        if(CurrentAnnouncement!=null && AnnouncementAge<(CurrentAnnouncement.Victory ? 6 : 3.5))return;
        if(Announcements.Count>0) {CurrentAnnouncement=Announcements.Dequeue();AnnouncementAge=0;}
        else if(CurrentAnnouncement?.Victory!=true)CurrentAnnouncement=null;
    }
    // Compatibility entry point now creates a functional shared web.
    internal bool TryDecorativeWeb(BugEntity spider)=>TrySharedWeb(spider);
}
