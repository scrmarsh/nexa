using System.Windows;
namespace SessionDashboard;
internal sealed partial class LivingCivilization
{
    internal int Capacity(CivicSettlement settlement)=>6+Completed(settlement,CivicPurpose.Housing)*8+Completed(settlement,CivicPurpose.Infrastructure)*2;
    internal int Completed(CivicSettlement s,CivicPurpose purpose)=>State.Buildings.Count(b=>b.SettlementId==s.Id && b.Purpose==purpose && b.Completed && !b.Cancelled && b.Condition>0);
    internal bool CanReproduce(int faction,int? actualPopulation=null)
    {
        var nation=politics.Nations.FirstOrDefault(n=>n.FactionId==faction);if(nation==null)return true;
        var settlements=State.Settlements.Where(s=>s.Active && s.OwnerId==nation.Id).ToArray();
        return settlements.Any(s=>s.Food>0) && (actualPopulation??settlements.Sum(s=>s.Population))<settlements.Sum(Capacity);
    }
    private void Capital(CivicNation nation,CivicSettlement settlement,double time)
    {
        nation.CapitalSettlementId=settlement.Id;politics.RelocateCapital(nation.NationId,settlement.Position);State.VisualRevision++;settlement.VisualRevision++;
        Emit(CivicEventKind.CapitalEstablished,nation.NationId,time,settlement.Name,settlement:settlement.Id);
    }
    internal CivicSettlement? Found(CivicNation nation,Point point,double time,int? core=null,bool initial=false)
    {
        Write();if(State.Settlements.Length>=128 || !politics.Land.IsCapital(point) || politics.OwnerAt(point)!=nation.NationId || State.Settlements.Any(s=>s.Active && (s.Position-point).Length<60))return null;
        var home=State.Settlements.FirstOrDefault(s=>s.Id==nation.CapitalSettlementId);
        if(!initial && (home==null || home.Materials<12 || home.Population<4))return null;
        if(!initial && Economy!=null && !Economy.Spend(nation.NationId,600,TreasuryCategory.Foundation,time,"Settlement foundation"))return null;
        if(home!=null && !initial)home.Materials-=12;
        var id=Identity(State.WorldId,"settlement/"+(State.Settlements.Length+1));var profile=cultures.Profiles[nation.ProfileId];
        var name=LivingCultures.Name(profile.SettlementNames,id,profile.Id+"/settlement");if(State.Settlements.Any(s=>s.Name==name))name+=" "+(State.Settlements.Length+1);
        var settlement=new CivicSettlement{Id=id,CoreSettlementId=core,Name=name,OwnerId=nation.NationId,StyleId=profile.Id,Position=point,FoundedAt=time};State.Settlements=State.Settlements.Append(settlement).ToArray();State.VisualRevision++;
        Emit(CivicEventKind.SettlementFounded,nation.NationId,time,name,settlement:id);
        if(nation.CapitalSettlementId==null)Capital(nation,settlement,time);return settlement;
    }
    private void SynchronizeSettlements(BugManager manager)
    {
        foreach(var s in State.Settlements.Where(s=>s.Active))
        {
            var owner=politics.OwnerAt(s.Position);
            if(owner==s.OwnerId)continue;
            Economy?.AssetLoss(s.OwnerId,s.Id,State.Buildings.Where(b=>b.SettlementId==s.Id && b.Condition>0).Sum(b=>b.InvestmentCents),manager.MatchAge,"Settlement lost; invested assets written down");
            foreach(var b in State.Buildings.Where(b=>b.SettlementId==s.Id && !b.Completed)){b.Cancelled=true;}
            if(owner is {} id && State.Nations.Any(n=>n.NationId==id))
            {
                s.OwnerId=id;foreach(var b in State.Buildings.Where(b=>b.SettlementId==s.Id))b.OwnerId=id;
                Emit(CivicEventKind.SettlementCaptured,id,manager.MatchAge,s.Name,settlement:s.Id);
            }
            else {s.Active=false;Emit(CivicEventKind.SettlementAbandoned,s.OwnerId,manager.MatchAge,s.Name,settlement:s.Id);}
            s.VisualRevision++;State.VisualRevision++;
        }
        foreach(var core in manager.Settlements)
        {
            if(State.Settlements.Any(s=>s.CoreSettlementId==core.Id))continue;
            var owner=politics.OwnerAt(core.Position);var nation=State.Nations.FirstOrDefault(n=>n.NationId==owner);if(nation!=null)Found(nation,core.Position,manager.MatchAge,core.Id);
        }
    }
    private void AssignPopulation(BugManager manager)
    {
        foreach(var s in State.Settlements){s.Population=0;s.Workforce=0;}var tasks=new List<CivicTask>();
        foreach(var nation in State.Nations)
        {
            var faction=politics.Find(nation.NationId)?.FactionId;var colony=manager.Colonies.Values.FirstOrDefault(c=>c.Id==faction);if(colony==null)continue;
            var towns=State.Settlements.Where(s=>s.Active && s.OwnerId==nation.NationId).ToArray();if(towns.Length==0)continue;
            foreach(var bug in manager.Bugs.Where(b=>BugManager.Alive(b) && b.Species==colony.Species))
            {
                var town=towns.MinBy(s=>(s.Position-bug.Position).LengthSquared)!;town.Population++;
                if(bug.IsLeader || bug.CampaignDeployed || bug.Combat is CombatState.Attack or CombatState.Pursue or CombatState.Retreat || (town.Position-bug.Position).Length>120)continue;
                // A capped founding population can contain no home-guard role slots.
                // One idle local adult can bootstrap work without changing military or reproductive identity.
                if(bug.Role is not (ArmyRole.HomeGuard or ArmyRole.Reserve) && town.Workforce>0)continue;
                town.Workforce++;tasks.Add(new(bug.UnitId,town.Id,(town.Workforce%4) switch{0=>"Maintain",1=>"Gather food",2=>"Gather materials",_=>"Construct"}));
            }
        }
        State.Tasks=tasks.Take(240).ToArray();
    }
    private void Develop(CivicSettlement s,BugManager manager,double dt)
    {
        var secure=!politics.IsContested(politics.Land.At(s.Position));
        var jobs=State.Tasks.Where(t=>t.SettlementId==s.Id).ToArray();var foodWorkers=jobs.Count(t=>t.Work=="Gather food");var materialWorkers=jobs.Count(t=>t.Work=="Gather materials");
        s.Food=Math.Clamp(s.Food+dt*(foodWorkers*(1+Completed(s,CivicPurpose.Food)*.35)-s.Population*.025),0,200);
        s.Materials=Math.Min(200,s.Materials+dt*materialWorkers*.28);
        s.Production=Math.Min(200,s.Production+dt*s.Workforce*.12*(1+Completed(s,CivicPurpose.Workshop)*.25));
        s.DefensiveSupplies=Math.Min(100,s.DefensiveSupplies+dt*Completed(s,CivicPurpose.Defense)*.08);
        foreach(var b in State.Buildings.Where(b=>b.SettlementId==s.Id && !b.Cancelled && b.Condition>0))
        {
            if(!secure && b.Completed){Damage(b.Id,dt*.005,manager.MatchAge);continue;}
            if(b.Completed)
            {if(b.Condition<1 && s.Materials>dt*.03 && jobs.Any(t=>t.Work=="Maintain")){s.Materials-=dt*.03;b.Condition=Math.Min(1,b.Condition+dt*.005);s.VisualRevision++;}continue;}
            if(!secure || s.Food<=0 || s.Workforce==0)continue;
            var old=(int)(b.Progress*4);b.Progress=Math.Min(1,b.Progress+dt*s.Workforce*(1+Completed(s,CivicPurpose.Workshop)*.2)/80);
            if(old!=(int)(b.Progress*4)){s.VisualRevision++;State.VisualRevision++;}
            if(b.Progress>=1){b.Completed=true;Emit(CivicEventKind.BuildingCompleted,s.OwnerId,manager.MatchAge,b.Archetype,settlement:s.Id);var ruler=State.Royals.FirstOrDefault(r=>r.Id==State.Nations.First(n=>n.NationId==s.OwnerId).CurrentRulerId);if(ruler!=null)ruler.Achievements=ruler.Achievements.Append("Completed "+b.Archetype+" at "+s.Name).TakeLast(16).ToArray();}
        }
        if(secure && s.Food>0 && s.Workforce>0 && !State.Buildings.Any(b=>b.SettlementId==s.Id && !b.Completed && !b.Cancelled && b.Condition>0))QueueNext(s);
        var requiredPopulation=new[]{5,10,16,22};var requiredBuildings=new[]{2,4,6,8};var requiredProduction=new[]{10d,25,50,80};var index=(int)s.Stage;
        var polity=State.Nations.First(n=>n.NationId==s.OwnerId);var tech=1+(Completed(s,CivicPurpose.Workshop)>0 ? 1:0)+(Completed(s,CivicPurpose.Infrastructure)>0 ? 1:0)+(Completed(s,CivicPurpose.Government)>0 ? 1:0);
        if(index<4 && secure && s.Food>0 && s.Population>=requiredPopulation[index] && State.Buildings.Count(b=>b.SettlementId==s.Id && b.Completed && b.Condition>0)>=requiredBuildings[index] && s.Production>=requiredProduction[index] && s.Materials>=8*(index+1) && tech>=index+1 && (index<3 || polity.CapitalSettlementId==s.Id))
        {s.Materials-=8*(index+1);s.Production-=requiredProduction[index];s.Stage++;s.VisualRevision++;State.VisualRevision++;Emit(CivicEventKind.SettlementUpgraded,s.OwnerId,manager.MatchAge,s.Stage.ToString(),settlement:s.Id);}
        var political=politics.Find(s.OwnerId);var colony=manager.Colonies.Values.FirstOrDefault(c=>c.Id==political?.FactionId);
        if(colony!=null)
        {
            if(s.Food>1 && s.Workforce>0)colony.Resources=Math.Min(90,colony.Resources+dt*.04*s.Workforce);
            if(!secure && s.DefensiveSupplies>dt*.02 && Completed(s,CivicPurpose.Defense)>0)
            {var region=manager.Territories[manager.RegionAt(s.Position)];if(region.OwnerId==colony.Id){s.DefensiveSupplies-=dt*.02;region.Occupation=Math.Max(0,region.Occupation-dt*.04*Completed(s,CivicPurpose.Defense));}}
        }
        // A genuine colonization decision costs materials and claims just one adjacent
        // neutral land cell. It does not invent units or expand an entire continent.
        if(politics.Expansion==null && secure && s.Stage>=SettlementStage.Town && s.Population>=10 && s.Materials>=45 && State.Settlements.Count(c=>c.Active && c.OwnerId==s.OwnerId)<3)
        {
            var treasury=State.Settlements.FirstOrDefault(c=>c.Id==polity.CapitalSettlementId && c.Active && c.OwnerId==s.OwnerId);
            // Reserve both the colonization cost and the administrative founding cost
            // before committing land; a poor/empty capital must not create a phantom outpost claim.
            if(treasury==null || treasury.Population<4 || treasury.Materials<12+(treasury.Id==s.Id ? 30:0))return;
            var candidate=politics.Regions(s.OwnerId).SelectMany(politics.Land.Neighbors).Distinct().Where(c=>politics.Owner(c)==null && politics.Land.IsCapital(politics.Land.Center(c)) && State.Settlements.All(t=>!t.Active || (t.Position-politics.Land.Center(c)).Length>=90)).OrderBy(c=>(politics.Land.Center(c)-s.Position).LengthSquared).FirstOrDefault(-1);
            if(candidate>=0){var point=politics.Land.Center(candidate);if(State.Settlements.All(c=>!c.Active || (c.Position-point).Length>=90) && State.Settlements.Length<128){s.Materials-=30;if(politics.Commit(candidate,null,s.OwnerId,manager.MatchAge,OwnershipReason.Settlement))Found(polity,point,manager.MatchAge);}}
        }
    }
    private void QueueNext(CivicSettlement s)
    {
        if(State.Buildings.Length>=2048 || State.Buildings.Count(b=>b.SettlementId==s.Id && !b.Cancelled && b.Condition>0)>=12)return;
        var order=new[]{CivicPurpose.Housing,CivicPurpose.Food,CivicPurpose.Workshop,CivicPurpose.Defense,CivicPurpose.Infrastructure,CivicPurpose.Government,CivicPurpose.Housing,CivicPurpose.Monument};
        var count=State.Buildings.Count(b=>b.SettlementId==s.Id && !b.Cancelled && b.Condition>0);var purpose=order[Math.Min(count,order.Length-1)];
        if(count>=order.Length)return;if(Economy!=null)purpose=EconomicPurpose(s,purpose);var rule=cultures.Profiles[s.StyleId].Construction.Single(r=>r.Purpose==purpose);
        if(purpose==CivicPurpose.Monument && (!State.Nations.Any(n=>n.CapitalSettlementId==s.Id) || s.Population<22))return;
        if((int)s.Stage<rule.MinimumStage || s.Materials<rule.Materials)return;
        Point? location=null;
        for(var i=0;i<64;i++){var angle=i*2.399963;var radius=12+Math.Sqrt(i)*5;var candidate=s.Position+new Vector(Math.Cos(angle)*radius,Math.Sin(angle)*radius);candidate=new Point(Math.Floor(candidate.X/6)*6+3,Math.Floor(candidate.Y/6)*6+3);if(politics.Land.IsCapital(candidate) && politics.OwnerAt(candidate)==s.OwnerId && State.Buildings.All(b=>(b.Position-candidate).Length>10)){location=candidate;break;}}
        if(location==null)return;var investment=Economy==null?0:(long)Math.Ceiling(rule.Materials*20);
        if(Economy!=null && !Economy.Spend(s.OwnerId,investment,TreasuryCategory.Construction,State.LastTick,rule.Archetype))return;s.Materials-=rule.Materials;
        State.Buildings=State.Buildings.Append(new CivicBuilding{Id=Identity(State.WorldId,"building/"+(State.Buildings.Length+1)),SettlementId=s.Id,OwnerId=s.OwnerId,StyleId=s.StyleId,Archetype=rule.Archetype,Purpose=purpose,Position=location.Value,MaterialCost=rule.Materials,InvestmentCents=investment}).ToArray();s.VisualRevision++;State.VisualRevision++;
    }
    internal void Damage(Guid id,double amount,double time)
    {
        Write();if(!double.IsFinite(amount) || amount<=0)throw new ArgumentException("Damage must be positive and finite.");var b=State.Buildings.FirstOrDefault(b=>b.Id==id);if(b==null || b.Condition<=0)return;
        b.Condition=Math.Max(0,b.Condition-amount);var settlement=State.Settlements.First(s=>s.Id==b.SettlementId);settlement.VisualRevision++;State.VisualRevision++;
        if(b.Condition==0){Economy?.AssetLoss(b.OwnerId,b.SettlementId,b.InvestmentCents,time,b.Archetype+" destroyed");if(!b.Completed)b.Cancelled=true;Emit(CivicEventKind.BuildingDestroyed,b.OwnerId,time,b.Archetype,settlement:b.SettlementId);}
    }
}
