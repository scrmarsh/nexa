using System.IO;
using System.Numerics;
namespace SessionDashboard;
internal enum ProsperityTier { Destitute,Poor,Stable,Prosperous,Wealthy,Opulent }
internal enum TreasuryCategory { ProductionSales,Construction,Foundation,Maintenance,MilitaryUpkeep,TransferIn,TransferOut,AssetLoss }
internal sealed record NationalTreasury
{
    public Guid NationId {get;init;}
    public long Balance {get;set;}
    public long Revenue {get;set;}
    public long Construction {get;set;}
    public long Foundation {get;set;}
    public long Maintenance {get;set;}
    public long MilitaryUpkeep {get;set;}
    public long TransferIn {get;set;}
    public long TransferOut {get;set;}
    public long LostAssets {get;set;}
    public long PeriodIncome {get;set;}
    public long PeriodSpending {get;set;}
    public long PreviousRevenue {get;set;}
    public long PreviousSpending {get;set;}
    public double LastInvestmentAt {get;set;}
    public string Pressure {get;set;}="Building a productive economy";
    internal long Spending=>Construction+Foundation+Maintenance+MilitaryUpkeep;
}
internal sealed record SettlementFinance
{
    public Guid SettlementId {get;init;}
    public double Score {get;set;}=40;
    public ProsperityTier Tier {get;set;}=ProsperityTier.Stable;
    public double TierSince {get;set;}
    public ProsperityTier HighestAnnounced {get;set;}=ProsperityTier.Stable;
    public double? HardshipSince {get;set;}
    public bool HardshipAnnounced {get;set;}
    public double LastDamageAt {get;set;}=-1000;
}
internal sealed record TreasuryFact(long Sequence,double Time,Guid NationId,TreasuryCategory Category,long Amount,string Detail);
internal sealed record CivicEconomyState
{
    public int SchemaVersion {get;init;}=1;
    public double LastTick {get;set;}
    public long Sequence {get;set;}
    public long LastTransferCommand {get;set;}
    public NationalTreasury[] Nations {get;set;}=[];
    public SettlementFinance[] Settlements {get;set;}=[];
    public TreasuryFact[] Recent {get;set;}=[];
    internal void Validate(CivicSnapshot civic)
    {
        static void Need([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool value){if(!value)throw new InvalidDataException("Invalid optional financial state; preserve the original world and history.");}
        Need(SchemaVersion==1 && double.IsFinite(LastTick) && LastTick>=0 && LastTick<=civic.LastTick+1e-6 && Sequence>=0 && LastTransferCommand>=0 && Nations!=null && Nations.Length<=512 && Settlements!=null && Settlements.Length<=128 && Recent!=null && Recent.Length<=64);
        Need(Nations!.All(n=>n!=null && civic.Nations.Any(c=>c.NationId==n.NationId) && new[]{n.Balance,n.Revenue,n.Construction,n.Foundation,n.Maintenance,n.MilitaryUpkeep,n.TransferIn,n.TransferOut,n.LostAssets,n.PeriodIncome,n.PeriodSpending,n.PreviousRevenue,n.PreviousSpending}.All(v=>v>=0 && v<=LivingEconomy.CurrencyLimit) && n.PreviousRevenue<=n.Revenue && n.PreviousSpending<=n.Spending && double.IsFinite(n.LastInvestmentAt) && n.LastInvestmentAt>=0 && n.Pressure!=null && n.Pressure.Length<=200 && new BigInteger(n.Balance)==new BigInteger(n.Revenue)+n.TransferIn-n.TransferOut-n.Spending));
        Need(Nations.Select(n=>n.NationId).Distinct().Count()==Nations.Length);
        Need(Settlements!.All(s=>s!=null && civic.Settlements.Any(c=>c.Id==s.SettlementId) && double.IsFinite(s.Score) && s.Score is >=0 and <=100 && Enum.IsDefined(s.Tier) && Enum.IsDefined(s.HighestAnnounced) && double.IsFinite(s.TierSince) && s.TierSince>=0 && s.TierSince<=civic.LastTick+1e-6 && (s.HardshipSince==null || double.IsFinite(s.HardshipSince.Value) && s.HardshipSince>=0) && double.IsFinite(s.LastDamageAt)));
        Need(Settlements.Select(s=>s.SettlementId).Distinct().Count()==Settlements.Length);
        // Destruction may occur between civic ticks, just like existing civic facts.
        long previous=0;foreach(var fact in Recent!){Need(fact!=null && fact.Sequence>previous && fact.Sequence<=Sequence && double.IsFinite(fact.Time) && fact.Time>=0 && civic.Nations.Any(n=>n.NationId==fact.NationId) && Enum.IsDefined(fact.Category) && fact.Amount>=0 && fact.Amount<=LivingEconomy.CurrencyLimit && fact.Detail!=null && fact.Detail.Length<=200);previous=fact!.Sequence;}
        Need(previous==Sequence);
    }
}
