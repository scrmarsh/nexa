using System.Diagnostics;
using System.Globalization;
namespace SessionDashboard;
internal sealed class LivingEconomy
{
    internal const long CurrencyLimit=1_000_000_000_000;
    internal CivicEconomyState State {get;}
    private readonly LivingCivilization civic;
    private readonly LivingNationWorld politics;
    internal double LastMilliseconds {get;private set;}
    internal LivingEconomy(CivicEconomyState state,LivingCivilization civic,LivingNationWorld politics){State=state;this.civic=civic;this.politics=politics;}
    internal static string Money(long cents)=>"¤"+(cents/100m).ToString("N2",CultureInfo.InvariantCulture);
    internal NationalTreasury? Nation(Guid id)=>State.Nations.FirstOrDefault(n=>n.NationId==id);
    internal SettlementFinance? Settlement(Guid id)=>State.Settlements.FirstOrDefault(s=>s.SettlementId==id);
    private NationalTreasury Account(Guid id)
    {
        var found=Nation(id);if(found!=null)return found;
        if(!civic.State.Nations.Any(n=>n.NationId==id) || State.Nations.Length>=512)throw new InvalidOperationException("Unknown nation or treasury capacity reached.");
        var added=new NationalTreasury{NationId=id};State.Nations=State.Nations.Append(added).ToArray();return added;
    }
    private void Fact(Guid id,TreasuryCategory category,long amount,double time,string detail)
    {State.Recent=State.Recent.Append(new TreasuryFact(++State.Sequence,time,id,category,amount,detail)).TakeLast(64).ToArray();}
    internal bool Spend(Guid id,long amount,TreasuryCategory category,double time,string detail)
    {
        civic.RequireWriter();if(amount<0 || amount>CurrencyLimit || !double.IsFinite(time) || time<0 || category is not (TreasuryCategory.Construction or TreasuryCategory.Foundation or TreasuryCategory.Maintenance or TreasuryCategory.MilitaryUpkeep))return false;
        var n=Account(id);if(n.Balance<amount || n.Spending>CurrencyLimit-amount){n.Pressure="Treasury cannot fund "+detail;return false;}if(amount==0)return true;n.Balance-=amount;
        switch(category){case TreasuryCategory.Construction:n.Construction+=amount;n.LastInvestmentAt=time;break;case TreasuryCategory.Foundation:n.Foundation+=amount;n.LastInvestmentAt=time;break;case TreasuryCategory.Maintenance:n.Maintenance+=amount;break;case TreasuryCategory.MilitaryUpkeep:n.MilitaryUpkeep+=amount;break;}
        Fact(id,category,amount,time,detail);return true;
    }
    internal bool Transfer(Guid from,Guid to,long amount,long command,double time)
    {
        civic.RequireWriter();if(from==to || amount<=0 || amount>CurrencyLimit || command!=State.LastTransferCommand+1 || !double.IsFinite(time) || time<0 || !civic.State.Nations.Any(n=>n.NationId==from) || !civic.State.Nations.Any(n=>n.NationId==to))return false;
        var source=Account(from);var destination=Account(to);if(source.Balance<amount || destination.Balance>CurrencyLimit-amount || source.TransferOut>CurrencyLimit-amount || destination.TransferIn>CurrencyLimit-amount)return false;
        source.Balance-=amount;source.TransferOut+=amount;destination.Balance+=amount;destination.TransferIn+=amount;State.LastTransferCommand=command;Fact(from,TreasuryCategory.TransferOut,amount,time,"Validated transfer");Fact(to,TreasuryCategory.TransferIn,amount,time,"Validated transfer");return true;
    }
    internal void AssetLoss(Guid owner,Guid settlement,long value,double time,string detail)
    {
        if(value<=0 || Nation(owner) is not {} nation)return;civic.RequireWriter();value=Math.Min(value,CurrencyLimit-nation.LostAssets);nation.LostAssets+=value;Fact(owner,TreasuryCategory.AssetLoss,value,time,detail);
        if(Settlement(settlement) is {} local)local.LastDamageAt=time;
    }
    internal void Tick(BugManager manager)
    {
        civic.RequireWriter();if(manager.WorldPaused || manager.MatchAge-State.LastTick<5)return;var watch=Stopwatch.GetTimestamp();var elapsed=Math.Min(5,manager.MatchAge-State.LastTick);State.LastTick=manager.MatchAge;
        foreach(var polity in civic.State.Nations)
        {
            var nation=Account(polity.NationId);nation.Pressure="No immediate financial pressure";var towns=civic.State.Settlements.Where(s=>s.Active && s.OwnerId==polity.NationId && politics.OwnerAt(s.Position)==s.OwnerId).ToArray();
            foreach(var town in towns)
            {
                // Real production is exchanged for fixed-point local currency; no trade route is invented.
                var amount=(long)Math.Floor(Math.Min(town.Production,elapsed*town.Workforce*.08)*100+1e-8);amount=Math.Min(amount,Math.Min(CurrencyLimit-nation.Balance,CurrencyLimit-nation.Revenue));
                if(amount>0){town.Production-=amount/100d;nation.Balance+=amount;nation.Revenue+=amount;Fact(nation.NationId,TreasuryCategory.ProductionSales,amount,manager.MatchAge,"Existing local production sold");}
            }
            var buildings=civic.State.Buildings.Where(b=>b.OwnerId==nation.NationId && b.Completed && !b.Cancelled && b.Condition>0 && towns.Any(t=>t.Id==b.SettlementId)).ToArray();
            var due=(long)Math.Ceiling(buildings.Length*elapsed*.4);var paid=Math.Min(due,nation.Balance);Spend(nation.NationId,paid,TreasuryCategory.Maintenance,manager.MatchAge,"Civic infrastructure maintenance");
            if(paid<due){nation.Pressure="Unfunded building maintenance";foreach(var b in buildings)civic.Damage(b.Id,elapsed*.0006,manager.MatchAge);}
            var units=politics.Statistics(nation.NationId,manager).Military;var upkeep=(long)Math.Floor(units*elapsed*.1);var funded=Math.Min(upkeep,nation.Balance);Spend(nation.NationId,funded,TreasuryCategory.MilitaryUpkeep,manager.MatchAge,"Existing living military upkeep");if(funded<upkeep)nation.Pressure="Military upkeep unfunded; combat rules unchanged";
            if(towns.Any(t=>t.Food<Math.Max(2,t.Population*.25)))nation.Pressure="Food reserves are low; prioritize food infrastructure";
            nation.PeriodIncome=nation.Revenue-nation.PreviousRevenue;nation.PeriodSpending=nation.Spending-nation.PreviousSpending;nation.PreviousRevenue=nation.Revenue;nation.PreviousSpending=nation.Spending;
            foreach(var town in towns)Prosperity(town,nation,elapsed,manager.MatchAge);
        }
        LastMilliseconds=Stopwatch.GetElapsedTime(watch).TotalMilliseconds;
    }
    internal double ProsperityTarget(CivicSettlement s,NationalTreasury n,double now)
    {
        var buildings=civic.State.Buildings.Where(b=>b.SettlementId==s.Id && b.Completed && !b.Cancelled && b.Condition>0).ToArray();
        var food=Math.Clamp(s.Food/(Math.Max(1,s.Population)*.8+2),0,1)*30;var stock=Math.Clamp(s.Materials/30,0,1)*10+Math.Clamp(s.Production/30,0,1)*10;
        var infrastructure=Math.Min(12,(civic.Completed(s,CivicPurpose.Infrastructure)+civic.Completed(s,CivicPurpose.Government))*6);var condition=buildings.Length==0?0:buildings.Average(b=>b.Condition)*12;
        var security=politics.IsContested(politics.Land.At(s.Position))?-10:8;var administration=politics.Expansion?.Influence(s.OwnerId,politics.Land.At(s.Position),civic)>0?5:0;
        var investment=now-n.LastInvestmentAt<120?Math.Min(10,n.Construction/100d):0;var damage=Settlement(s.Id) is {} local && now-local.LastDamageAt<120?15:0;
        return Math.Clamp(food+stock+infrastructure+condition+security+administration+investment-damage-(s.Food<=0?20:0),0,100);
    }
    private void Prosperity(CivicSettlement s,NationalTreasury n,double elapsed,double now)
    {
        var local=Settlement(s.Id);if(local==null){if(State.Settlements.Length>=128)return;local=new(){SettlementId=s.Id,TierSince=now};State.Settlements=State.Settlements.Append(local).ToArray();}
        local.Score+=(ProsperityTarget(s,n,now)-local.Score)*(1-Math.Exp(-elapsed/30));double[] boundaries=[15,35,55,75,90];var previous=local.Tier;
        while(local.Tier<ProsperityTier.Opulent && local.Score>=boundaries[(int)local.Tier]+3)local.Tier++;
        while(local.Tier>ProsperityTier.Destitute && local.Score<boundaries[(int)local.Tier-1]-3)local.Tier--;
        if(local.Tier!=previous){local.TierSince=now;s.VisualRevision++;civic.State.VisualRevision++;}
        if(local.Tier>local.HighestAnnounced && local.Tier>=ProsperityTier.Prosperous && now-local.TierSince>=30){local.HighestAnnounced=local.Tier;civic.EconomicEvent(CivicEventKind.SustainedProsperity,s,now,s.Name+" sustained "+local.Tier+" conditions.");}
        if(local.Tier<=ProsperityTier.Poor){local.HardshipSince??=now;if(!local.HardshipAnnounced && now-local.HardshipSince>=60){local.HardshipAnnounced=true;civic.EconomicEvent(CivicEventKind.EconomicHardship,s,now,s.Name+" experienced prolonged hardship.");}}
        else{if(local.HardshipAnnounced && now-local.TierSince>=30){local.HardshipAnnounced=false;civic.EconomicEvent(CivicEventKind.EconomicRecovery,s,now,s.Name+" recovered to stable living conditions.");}local.HardshipSince=null;}
    }
}
internal sealed partial class LivingCivilization
{
    internal LivingEconomy? Economy {get;private set;}
    internal void EconomicEvent(CivicEventKind kind,CivicSettlement settlement,double time,string detail)=>Emit(kind,settlement.OwnerId,time,detail,settlement:settlement.Id);
    internal void RequireWriter()=>Write();
    internal void EnableEconomy(double time)
    {
        Write();if(Economy!=null)return;if(!double.IsFinite(time) || time<0 || time<State.LastTick)throw new ArgumentException("Invalid economy starting clock.");
        State.Economy=new(){LastTick=time,Nations=State.Nations.Select(n=>new NationalTreasury{NationId=n.NationId}).ToArray(),Settlements=State.Settlements.Select(s=>new SettlementFinance{SettlementId=s.Id,TierSince=time}).ToArray()};State.LastTick=time;State.SchemaVersion=2;Economy=new(State.Economy,this,politics);
    }
    private CivicPurpose EconomicPurpose(CivicSettlement s,CivicPurpose fallback)
    {
        if(s.Food<Math.Max(5,s.Population*.5) && Completed(s,CivicPurpose.Food)<2)return CivicPurpose.Food;
        if(s.Population>=Capacity(s)-2 && Completed(s,CivicPurpose.Housing)<3)return CivicPurpose.Housing;
        if(Completed(s,CivicPurpose.Infrastructure)==0 && Completed(s,CivicPurpose.Housing)>0)return CivicPurpose.Infrastructure;
        if(politics.Frontiers.Settlements(this).Any(t=>t.SettlementId==s.Id) && Completed(s,CivicPurpose.Defense)==0)return CivicPurpose.Defense;
        return fallback;
    }
}
internal sealed partial class BattlefieldWindow
{
    private static string EconomySummary(NationalTreasury n)=>$"\nTreasury: {LivingEconomy.Money(n.Balance)}\nLatest economic tick: +{LivingEconomy.Money(n.PeriodIncome)} / −{LivingEconomy.Money(n.PeriodSpending)}\nTotal production sales: {LivingEconomy.Money(n.Revenue)}\nConstruction / foundation: {LivingEconomy.Money(n.Construction)} / {LivingEconomy.Money(n.Foundation)}\nMaintenance / military: {LivingEconomy.Money(n.Maintenance)} / {LivingEconomy.Money(n.MilitaryUpkeep)}\nLost invested assets: {LivingEconomy.Money(n.LostAssets)}\n{n.Pressure}";
}
