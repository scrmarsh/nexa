using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    internal ArcadeWorldSnapshot CaptureWorld()
    {
        if(!IsInfinite || pendingDamage.Count!=0)throw new InvalidOperationException("Capture the Infinite world between simulation steps.");
        var entities=Bugs.Concat(colonyOrder.Select(c=>c.Leader)).Distinct().ToArray();var known=entities.Select(b=>b.UnitId).ToHashSet();
        return new ArcadeWorldSnapshot
        {
            WorldId=WorldId,LivingGeneratorVersion=LivingGeneratorVersion,LivingPolitics=Nations?.CaptureJson()??PendingPolitics,LivingCivilization=Civilization?.CaptureJson()??PendingCivilization,Width=BattleArena.Width,Height=BattleArena.Height,Time=MatchAge,Paused=WorldPaused,RandomState=random.State,
            Settings=BattleOptions.Safe(),NextWeb=nextWeb,NextSettlement=nextSettlement,TerritoryDelay=territoryDelay,Territories=Territories.Select(r=>r with{}).ToArray(),Settlements=Settlements.Select(n=>n with{}).ToArray(),FactionColors=new(FactionColors),FactionSpecies=new(FactionSpecies),SpawnPressure=SpawnPressure,NextFaction=nextFaction,NextUnit=nextUnit,MatchNumber=MatchNumber,Phase=Phase,Attacks=BattleAttacks,
            StrategyDelay=strategyDelay,NextWarAt=nextWarAt,NextEmergence=nextEmergence,LastDominant=lastDominantFaction,WaveDelay=waveDelay,DripDelay=dripDelay,WaveIndex=waveIndex,
            PendingSoldiers=pendingSoldiers,PendingColonyId=pendingColony?.Id,AnnouncementAge=AnnouncementAge,Announcement=CurrentAnnouncement,
            Announcements=Announcements.ToArray(),Victories=new(Victories),FactionNames=new(factionNames),History=History.ToArray(),
            Units=entities.Select(b=>new ArcadeUnitState {InScene=Bugs.Contains(b),TargetId=b.TargetUnit is {} target && known.Contains(target.UnitId) ? target.UnitId : null,
                LastAttackerId=b.LastAttacker is {} attacker && known.Contains(attacker.UnitId) ? attacker.UnitId : null,
                Species=b.Species,
                UnitId=b.UnitId,
                ConfirmedKills=b.ConfirmedKills,
                Direction=b.Direction,
                Hits=b.Hits,
                Meals=b.Meals,
                IsLeader=b.IsLeader,
                CanReproduce=b.CanReproduce,
                Name=b.Name,
                LegendaryTitle=b.LegendaryTitle,
                Combat=b.Combat,Role=b.Role,CampaignDeployed=b.CampaignDeployed,
                State=b.State,
                Position=b.Position,
                Target=b.Target,
                OrderPoint=b.OrderPoint,
                Health=b.Health,
                MaxHealth=b.MaxHealth,
                Protection=b.Protection,
                AttackTimer=b.AttackTimer,
                AbilityTimer=b.AbilityTimer,
                ThinkTimer=b.ThinkTimer,
                SlowTimer=b.SlowTimer,
                StunTimer=b.StunTimer,
                DefeatAge=b.DefeatAge,
                AttackFlash=b.AttackFlash,
                HealFlash=b.HealFlash,
                RevengeTimer=b.RevengeTimer,
                RetreatUntil=b.RetreatUntil,
                NextRetreat=b.NextRetreat,
                Angle=b.Angle,
                Speed=b.Speed,
                Age=b.Age,
                CrawlPhase=b.CrawlPhase,
                HitFlash=b.HitFlash,
                WebImmunity=b.WebImmunity,
            }).ToArray(),
            Colonies=colonyOrder.Select(c=>new ArcadeColonyState {Species=c.Species,Id=c.Id,Name=c.Name,Territory=c.Territory,SurvivorCenter=c.SurvivorCenter,
                Strategy=c.Strategy,TargetFactionId=c.TargetFactionId,CampaignRally=c.CampaignRally,StrategyUntil=c.StrategyUntil,NextCampaign=c.NextCampaign,CampaignStarted=c.CampaignStarted,Strength=c.Strength,InitialForce=c.InitialForce,MajorCampaign=c.MajorCampaign,CampaignKills=c.CampaignKills,ColorArgb=c.ColorArgb,KillsAgainst=new(c.KillsAgainst),Rivalries=new(c.Rivalries),LeaderId=c.Leader.UnitId,Eliminated=c.Eliminated,Regrouping=c.Regrouping,Alive=c.Alive,Kills=c.Kills,Recoveries=c.Recoveries,
                Resources=c.Resources,RecoveryTime=c.RecoveryTime,SettlementLostAt=c.SettlementLostAt,DefeatTime=c.DefeatTime}).ToArray(),
            Webs=Webs.Select(w=>new ArcadeWebState {WebId=w.WebId,FactionId=w.FactionId,GeometrySeed=w.GeometrySeed,TotalWork=w.TotalWork,Plan=w.Plan.ToArray(),LinkedWebs=w.LinkedWebs.ToArray(),Position=w.Position,OwnerId=w.Owner is {} owner && known.Contains(owner.UnitId) ? owner.UnitId : null,
                Age=w.Age,Construction=w.Construction,Rings=w.Rings,BuildWork=w.BuildWork,Strands=w.Strands.ToArray(),Contributors=w.Contributors.ToArray()}).ToArray(),
            Projectiles=Projectiles.Select(p=>new ArcadeProjectileState {Position=p.Position,Previous=p.Previous,Species=p.Species,TargetId=p.Target.UnitId,
                AttackerId=p.Attacker is {} shooter && known.Contains(shooter.UnitId) ? shooter.UnitId : null,Age=p.Age}).ToArray()
        };
    }
    internal static BugManager RestoreWorld(ArcadeWorldSnapshot s)
    {
        ValidateWorld(s);
        var m=new BugManager(1);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,s.Settings));
        m.WorldId=s.WorldId;m.LivingGeneratorVersion=s.LivingGeneratorVersion;m.PendingPolitics=s.LivingPolitics?.Clone();m.PendingCivilization=s.LivingCivilization?.Clone();m.FixedWorldBounds=new Rect(0,0,s.Width,s.Height);m.Bounds=m.FixedWorldBounds.Value.Size;m.VisibleBounds=m.FixedWorldBounds.Value;m.previousArena=m.BattleArena;
        m.MatchAge=s.Time;m.WorldPaused=s.Paused;m.random.State=s.RandomState;m.nextFaction=s.NextFaction;m.nextUnit=s.NextUnit;m.MatchNumber=s.MatchNumber;m.Phase=s.Phase;m.BattleAttacks=s.Attacks;
        m.strategyDelay=s.StrategyDelay;m.nextWarAt=s.NextWarAt;m.nextEmergence=s.NextEmergence;m.lastDominantFaction=s.LastDominant;m.waveDelay=s.WaveDelay;m.dripDelay=s.DripDelay;m.waveIndex=s.WaveIndex;m.pendingSoldiers=s.PendingSoldiers;
        var units=new Dictionary<int,BugEntity>();
        foreach(var u in s.Units)
        {
            var b=new BugEntity {
                Species=u.Species,
                UnitId=u.UnitId,
                ConfirmedKills=u.ConfirmedKills,
                Direction=u.Direction,
                Hits=u.Hits,
                Meals=u.Meals,
                IsLeader=u.IsLeader,
                CanReproduce=u.CanReproduce,
                Name=u.Name,
                LegendaryTitle=u.LegendaryTitle,
                Combat=u.Combat,Role=u.Role,CampaignDeployed=u.CampaignDeployed,
                State=u.State,
                Position=u.Position,
                Target=u.Target,
                OrderPoint=u.OrderPoint,
                Health=u.Health,
                MaxHealth=u.MaxHealth,
                Protection=u.Protection,
                AttackTimer=u.AttackTimer,
                AbilityTimer=u.AbilityTimer,
                ThinkTimer=u.ThinkTimer,
                SlowTimer=u.SlowTimer,
                StunTimer=u.StunTimer,
                DefeatAge=u.DefeatAge,
                AttackFlash=u.AttackFlash,
                HealFlash=u.HealFlash,
                RevengeTimer=u.RevengeTimer,
                RetreatUntil=u.RetreatUntil,
                NextRetreat=u.NextRetreat,
                Angle=u.Angle,
                Speed=u.Speed,
                Age=u.Age,
                CrawlPhase=u.CrawlPhase,
                HitFlash=u.HitFlash,
                WebImmunity=u.WebImmunity,
            };units.Add(b.UnitId,b);if(u.InScene)m.Bugs.Add(b);
        }
        foreach(var u in s.Units) {units[u.UnitId].TargetUnit=u.TargetId is {} target ? units[target] : null;units[u.UnitId].LastAttacker=u.LastAttackerId is {} attacker ? units[attacker] : null;}
        foreach(var c in s.Colonies)
        {
            var colony=new BugColony(c.Species) {Id=c.Id,Name=c.Name,Territory=c.Territory,SurvivorCenter=c.SurvivorCenter,Leader=units[c.LeaderId],Eliminated=c.Eliminated,
                Strategy=c.Strategy,TargetFactionId=c.TargetFactionId,CampaignRally=c.CampaignRally,StrategyUntil=c.StrategyUntil,NextCampaign=c.NextCampaign,CampaignStarted=c.CampaignStarted,Strength=c.Strength,InitialForce=c.InitialForce,MajorCampaign=c.MajorCampaign,CampaignKills=c.CampaignKills,ColorArgb=c.ColorArgb,
                Regrouping=c.Regrouping,Alive=c.Alive,Kills=c.Kills,Recoveries=c.Recoveries,Resources=c.Resources,RecoveryTime=c.RecoveryTime,SettlementLostAt=c.SettlementLostAt,DefeatTime=c.DefeatTime};
            foreach(var rival in c.Rivalries)colony.Rivalries.Add(rival.Key,rival.Value);
            foreach(var kills in c.KillsAgainst)colony.KillsAgainst.Add(kills.Key,kills.Value);
            m.Colonies.Add(c.Species,colony);m.colonyOrder.Add(colony);
        }
        m.pendingColony=s.PendingColonyId is {} pending ? m.colonyOrder.Single(c=>c.Id==pending) : null;
        foreach(var w in s.Webs)
        {
            var web=new BugWeb {WebId=w.WebId,FactionId=w.FactionId,GeometrySeed=w.GeometrySeed,TotalWork=w.TotalWork,Position=w.Position,Owner=w.OwnerId is {} owner ? units[owner] : null,Age=w.Age,Construction=w.Construction,Rings=w.Rings,BuildWork=w.BuildWork};
            web.Plan.AddRange(w.Plan);web.LinkedWebs.UnionWith(w.LinkedWebs);web.Strands.AddRange(w.Strands);web.Contributors.UnionWith(w.Contributors);m.Webs.Add(web);
        }
        foreach(var p in s.Projectiles)m.Projectiles.Add(new CombatWebProjectile {Position=p.Position,Previous=p.Previous,Species=p.Species,Target=units[p.TargetId],Attacker=p.AttackerId is {} shooter ? units[shooter] : null,Age=p.Age});
        m.SpawnPressure=s.SpawnPressure;foreach(var kind in s.FactionSpecies)m.FactionSpecies.Add(kind.Key,kind.Value);m.nextWeb=s.NextWeb;m.nextSettlement=s.NextSettlement;m.territoryDelay=s.TerritoryDelay;m.Territories.AddRange(s.Territories.Select(r=>r with{}));m.Settlements.AddRange(s.Settlements.Select(n=>n with{}));m.TerritoryRevision++;foreach(var color in s.FactionColors)m.FactionColors.Add(color.Key,color.Value);
        m.History.AddRange(s.History);foreach(var pair in s.FactionNames)m.factionNames.Add(pair.Key,pair.Value);
        foreach(var pair in s.Victories)m.Victories.Add(pair.Key,pair.Value);
        m.CurrentAnnouncement=s.Announcement;m.AnnouncementAge=s.AnnouncementAge;foreach(var item in s.Announcements)m.Announcements.Enqueue(item);
        return m;
    }
    internal static void ValidateWorld(ArcadeWorldSnapshot s)
    {
        static void Need([DoesNotReturnIf(false)] bool condition,string message) {if(!condition)throw new InvalidDataException("Invalid Arcade save: "+message);}
        static bool Finite(double x)=>double.IsFinite(x) && Math.Abs(x)<1e12;
        bool PointOk(Point p)=>Finite(p.X) && Finite(p.Y) && p.X>=-200 && p.Y>=-200 && p.X<=s.Width+200 && p.Y<=s.Height+200;
        Need(s!=null && s.SchemaVersion==1,"unsupported schema");
        Need(s!.LivingGeneratorVersion is 0 or 2 or 3,"Living World generator extension");
        var wide=s.LivingGeneratorVersion==2 && (s.Width,s.Height) is (6144,3456) or (9216,5184) or (12288,6912);
        if(s.LivingGeneratorVersion==3)wide=GrandGeneration.Sizes.Any(size=>s.Width==size.Width*6d && s.Height==size.Height*6d);
        Need(s.WorldId!=Guid.Empty && (wide || s.Width>=640 && s.Width<=4000 && s.Height>=480 && s.Height<=4000) && Finite(s.Time) && s.Time>=0 && s.RandomState!=0,"world identity, dimensions or clock");
        Need(s.Settings!=null && s.Settings.Mode==ArcadeRuleSet.InfiniteWarfare && Enum.IsDefined(s.Phase) && s.Phase!=BattlePhase.Victory,"world rules");
        Need(s.Units!=null && s.Units.Length<=512 && s.Colonies!=null && s.Colonies.Length==6 && s.History!=null && s.History.Length<=2048 && s.Webs!=null && s.Webs.Length<=18 && s.Projectiles!=null && s.Projectiles.Length<=48,"collection limits");
        Need(s.FactionNames!=null && s.FactionNames.Count<=8192 && s.Announcements!=null && s.Announcements.Length<=32 && s.Victories!=null,"history metadata");
        Need(s.Units!.Select(u=>u.UnitId).Distinct().Count()==s.Units.Length && s.Colonies!.Select(c=>c.Id).Distinct().Count()==6 && s.Colonies.Select(c=>c.Species).Distinct().Count()==6,"duplicate identities");
        Need(s.Settings.Species!=null && s.Settings.Species.Count==6 && ArcadeSettings.Defaults.All(pair=>s.Settings.Species.TryGetValue(pair.Key,out var value) && value!=null && value.Safe(pair.Value)==value),"species balance");
        var safe=s.Settings.Safe();Need(safe.HardCap==s.Settings.HardCap && safe.SoftCap==s.Settings.SoftCap && safe.SpawnInterval==s.Settings.SpawnInterval && safe.GroupSize==s.Settings.GroupSize && safe.TargetFps==s.Settings.TargetFps && safe.WebConstructionRate==s.Settings.WebConstructionRate && safe.MaximumSharedWebs==s.Settings.MaximumSharedWebs && safe.TerritoryOpacity==s.Settings.TerritoryOpacity && safe.Aggression==s.Settings.Aggression && safe.WarFrequency==s.Settings.WarFrequency,"unsafe world settings");
        var known=s.Units.ToDictionary(u=>u.UnitId);var species=s.Colonies!.Select(c=>c.Species).ToHashSet();
        foreach(var u in s.Units)
        {
            Need(u.UnitId>0 && species.Contains(u.Species) && Enum.IsDefined(u.Role) && Enum.IsDefined(u.Combat) && Enum.IsDefined(u.State) && PointOk(u.Position) && PointOk(u.Target) && PointOk(u.OrderPoint),"unit identity or coordinates");
            Need(typeof(ArcadeUnitState).GetProperties().Where(p=>p.PropertyType==typeof(double)).All(p=>Finite((double)p.GetValue(u)!)),"unit numeric data");
            Need(u.MaxHealth>0 && u.MaxHealth<=1500 && u.Health>=0 && u.Health<=u.MaxHealth && (u.Name?.Length??0)<=100 && (u.LegendaryTitle?.Length??0)<=100,"unit health or name");
            Need((u.TargetId==null || known.ContainsKey(u.TargetId.Value)) && (u.LastAttackerId==null || known.ContainsKey(u.LastAttackerId.Value)),"unit references");
        }
        foreach(var c in s.Colonies)
        {
            Need(BugHabitats.Species.Contains(c.Species) && c.Id>0 && c.Name.Length is >0 and <=100 && PointOk(c.Territory) && PointOk(c.SurvivorCenter),"faction data");
            Need(known.TryGetValue(c.LeaderId,out var leader) && leader.Species==c.Species && leader.IsLeader,"leader identity");
            Need(Enum.IsDefined(c.Strategy) && PointOk(c.CampaignRally) && c.Rivalries!=null && c.Rivalries.Count<=8192 && c.Rivalries.All(p=>p.Key>0 && Finite(p.Value)) && c.KillsAgainst!=null && c.KillsAgainst.Count<=8192 && c.KillsAgainst.All(p=>p.Key>0 && p.Value>=0) && new[]{c.StrategyUntil,c.NextCampaign,c.CampaignStarted,c.Strength}.All(Finite),"strategy");
            Need(c.Alive==s.Units.Count(u=>u.InScene && u.Species==c.Species && u.Health>0 && u.DefeatAge<=0 && u.Combat!=CombatState.Defeated) && c.Eliminated==(c.Alive==0),"population consistency");
            Need(Finite(c.Resources) && c.Resources>=0 && c.Resources<=90 && Finite(c.RecoveryTime) && Finite(c.DefeatTime) && Finite(c.SettlementLostAt) && c.Kills>=0 && c.Recoveries is >=0 and <=1,"colony reserves");
        }
        Need(s.NextFaction>=s.Colonies.Max(c=>c.Id) && s.NextUnit>=s.Units.Max(u=>u.UnitId) && s.MatchNumber>=1 && s.WaveIndex>=0 && s.PendingSoldiers is >=0 and <=6,"simulation counters");
        Need(s.PendingColonyId==null || s.Colonies.Any(c=>c.Id==s.PendingColonyId),"reinforcement reference");
        Need(new[]{s.StrategyDelay,s.NextWarAt,s.NextEmergence,s.WaveDelay,s.DripDelay,s.AnnouncementAge}.All(Finite),"simulation timers");
        foreach(var w in s.Webs!)Need(PointOk(w.Position) && Finite(w.Age) && w.Age>=0 && w.Construction is >=0 and <=1 && w.Rings is >=1 and <=4 && Finite(w.BuildWork) && w.Strands!=null && w.Strands.Length<=160 && w.Strands.All(t=>PointOk(t.A) && PointOk(t.B) && Finite(t.BuiltAt) && (t.BuiltAt<0 || t.BuiltAt<=w.TotalWork)) && w.Contributors!=null && w.Contributors.Length<=512 && w.Plan!=null && w.Plan.Length<=64 && w.Plan.All(p=>PointOk(p.A) && PointOk(p.B)) && w.LinkedWebs!=null && w.LinkedWebs.Length<=8 && Finite(w.TotalWork) && (w.OwnerId==null || known.ContainsKey(w.OwnerId.Value)),"web geometry");
        foreach(var p in s.Projectiles!)Need(PointOk(p.Position) && PointOk(p.Previous) && known.ContainsKey(p.TargetId) && (p.AttackerId==null || known.ContainsKey(p.AttackerId.Value)) && species.Contains(p.Species) && Finite(p.Age) && p.Age>=0,"projectile");
        Need(s.Territories!=null && s.Territories.Length==TerritoryColumns*TerritoryRows && s.Territories.Select(r=>r.Id).SequenceEqual(Enumerable.Range(0,TerritoryColumns*TerritoryRows)),"territory grid");
        Need(s.Settlements!=null && s.Settlements.Length<=24 && s.Settlements.Select(n=>n.Id).Distinct().Count()==s.Settlements.Length && s.FactionColors!=null && s.FactionColors.Count<=8192 && s.FactionSpecies!=null && s.FactionSpecies.Count<=8192 && s.FactionSpecies.All(p=>p.Key>0 && BugHabitats.Species.Contains(p.Value)) && s.Colonies.All(c=>s.FactionColors.ContainsKey(c.Id) && s.FactionSpecies.GetValueOrDefault(c.Id)==c.Species),"settlements and colors");
        foreach(var region in s.Territories)Need(region.OwnerId>=0 && (region.OwnerId==0 || s.FactionNames.ContainsKey(region.OwnerId)) && (region.ChallengerId==null || s.FactionNames.ContainsKey(region.ChallengerId.Value)) && Finite(region.Occupation) && region.Occupation>=0,"region control");
        foreach(var nest in s.Settlements)Need(nest.Id>0 && nest.Id<=s.NextSettlement && s.FactionNames.ContainsKey(nest.OwnerId) && s.FactionNames.ContainsKey(nest.FounderId) && PointOk(nest.Position) && Finite(nest.LostAt),"settlement ownership");
        foreach(var e in s.History!)Need(Finite(e.Timestamp) && e.Timestamp>=0 && e.Timestamp<=s.Time && Enum.IsDefined(e.Type) && e.FactionId>0 && e.Match==s.MatchNumber && (e.RegionId==null || e.RegionId is >=0 and <96) && (e.SettlementId==null || e.SettlementId>0 && e.SettlementId<=s.NextSettlement),"historical record");
    }
}
