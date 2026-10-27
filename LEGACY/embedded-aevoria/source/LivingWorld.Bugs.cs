using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    private readonly Dictionary<int,Point> terrainPrevious=[];
    private readonly Dictionary<(int,int),TerrainBiome> terrainWalkCache=[];
    internal void SeedLivingColonies(LivingTerrain terrain)
    {
        var size=terrain.Info.Extent;var index=0;
        foreach(var c in colonyOrder)
        {
            var angle=index++*Math.PI/3;
            c.Territory=terrain.FindLand(new Point(size*.5+Math.Cos(angle)*size*.23,terrain.Info.HeightExtent*(.5+Math.Sin(angle)*.23)));
            c.SurvivorCenter=c.CampaignRally=c.Territory;c.Leader.Position=c.Leader.OrderPoint=c.Territory;
        }
        InitializeTerritories();
    }
    internal void PrepareLivingStep()
    {terrainPrevious.Clear();foreach(var b in Bugs)if(Alive(b))terrainPrevious[b.UnitId]=b.Position;}
    internal void ResolveLivingStep(LivingTerrain terrain,double dt)
    {
        TerrainBiome Biome(Point p)
        {
            var key=((int)Math.Floor(p.X/terrain.Info.TileSize),(int)Math.Floor(p.Y/terrain.Info.TileSize));
            if(terrainWalkCache.TryGetValue(key,out var b))return b;
            if(terrainWalkCache.Count>=8192)terrainWalkCache.Clear();return terrainWalkCache[key]=terrain.Sample(key.Item1,key.Item2).Biome;
        }
        bool Pass(Point p)=>Biome(p) is not (TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake);
        foreach(var c in colonyOrder)
        {
            if(LivingTerrain.Land(Biome(c.Territory)))continue;
            var old=c.Territory;c.Territory=terrain.FindLand(old);c.CampaignRally=c.Territory;
            foreach(var nest in Settlements.Where(n=>n.OwnerId==c.Id && (n.Position-old).Length<100))nest.Position=c.Territory;
        }
        foreach(var b in Bugs)
        {
            if(!Alive(b) || b.Species==BugSpecies.Flies || Pass(b.Position))continue;
            if(!terrainPrevious.TryGetValue(b.UnitId,out var previous) || !Pass(previous)) {b.Position=terrain.FindLand(b.Position);b.OrderPoint=b.Position;continue;}
            var direction=b.Position-previous;var distance=Math.Max(1,direction.Length);direction/=distance;
            var step=Math.Max(distance,b.Speed*dt);
            var left=ClampArena(previous+new Vector(-direction.Y,direction.X)*step);
            var right=ClampArena(previous+new Vector(direction.Y,-direction.X)*step);
            b.Position=Pass(left) ? left : Pass(right) ? right : previous;
        }
    }
}
