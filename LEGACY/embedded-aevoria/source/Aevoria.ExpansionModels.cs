using System.IO;
using System.Text.Json.Serialization;
namespace SessionDashboard;
// Optional, explicit opt-in. Absent in pre-C worlds; terrain and base save versions stay unchanged.
internal sealed record KingdomTuning
{
    public double Interval {get;init;}=2;
    public int ClaimsPerNation {get;init;}=48;
    public int EvaluationsPerNation {get;init;}=384;
    public double MaterialsPerCell {get;init;}=.008;
    public double FoodPerCell {get;init;}=.006;
    public double TerrainWeight {get;init;}=.45;
    public double CompactnessWeight {get;init;}=.4;
    internal void Validate()
    {
        if(!double.IsFinite(Interval) || Interval is <1 or >30 || ClaimsPerNation is <1 or >96 || EvaluationsPerNation<ClaimsPerNation || EvaluationsPerNation>1024 ||
            !new[]{MaterialsPerCell,FoodPerCell,TerrainWeight,CompactnessWeight}.All(double.IsFinite) || MaterialsPerCell is <.001 or >1 || FoodPerCell is <.001 or >1 || TerrainWeight is <0 or >4 || CompactnessWeight is <0 or >4)
            throw new InvalidDataException("Unsupported kingdom expansion tuning.");
    }
}
internal sealed record KingdomMilestone(Guid NationId,int Level);
internal sealed record KingdomBorderFact(Guid A,Guid B);
internal sealed record KingdomExpansionState
{
    public int SchemaVersion {get;init;}=1;
    public KingdomTuning Tuning {get;init;}=new();
    public double LastTick {get;set;}
    public long Turn {get;set;}
    public KingdomMilestone[] Milestones {get;set;}=[];
    public KingdomBorderFact[] KnownBorders {get;set;}=[];
    internal void Validate(LivingNationWorld world)
    {
        if(SchemaVersion!=1 || Tuning==null || !double.IsFinite(LastTick) || LastTick<0 || Turn<0 || Milestones==null || KnownBorders==null || Milestones.Length>512 || KnownBorders.Length>4096 ||
            Milestones.Any(m=>m==null || world.Find(m.NationId)==null || m.Level is <0 or >8) || Milestones.Select(m=>m.NationId).Distinct().Count()!=Milestones.Length ||
            KnownBorders.Any(b=>b==null || b.A.CompareTo(b.B)>=0 || world.Find(b.A)==null || world.Find(b.B)==null) || KnownBorders.Distinct().Count()!=KnownBorders.Length)
            throw new InvalidDataException("Unsupported kingdom expansion extension; preserve the original save.");
        Tuning.Validate();
    }
}
internal sealed partial class LivingNationWorld
{
    internal KingdomExpansion? Expansion {get;private set;}
    internal void EnableExpansion(KingdomExpansionState? state=null)
    {Write();if(Expansion!=null)throw new InvalidOperationException("Expansion already initialized.");state??=new();state.Validate(this);Expansion=new(this,state);}
}
