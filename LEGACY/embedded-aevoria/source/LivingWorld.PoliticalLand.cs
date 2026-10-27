using System.Numerics;
using System.Windows;
namespace SessionDashboard;
// Immutable topology sampled from the existing terrain. It never changes either generator.
internal sealed class PoliticalLand
{
    internal const int CellTiles=8;
    internal readonly LivingWorldInfo Info;
    internal readonly LivingTerrain Terrain;
    internal readonly int Columns,Rows;
    internal readonly ulong[] Land,Ink;
    internal int Count=>Land.Length;
    internal long Bytes=>(long)Count*16;
    internal double BuildMilliseconds {get;}
    internal PoliticalLand(LivingTerrain terrain,CancellationToken cancel=default)
    {
        var began=System.Diagnostics.Stopwatch.GetTimestamp();
        Terrain=terrain;Info=terrain.Info;Columns=(Info.Tiles+7)/8;Rows=(Info.HeightTiles+7)/8;Land=new ulong[Columns*Rows];Ink=new ulong[Land.Length];
        for(var y=0;y<Info.HeightTiles;y++)for(var x=0;x<Info.Tiles;x++)
        {
            if(x==0)cancel.ThrowIfCancellationRequested();
            var biome=terrain.Sample(x,y).Biome;if(biome is TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake)continue;
            var cell=y/8*Columns+x/8;var bit=1UL<<((y%8)*8+x%8);Land[cell]|=bit;if(biome!=TerrainBiome.River)Ink[cell]|=bit;
        }
        BuildMilliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }
    internal int Cell(int x,int y)=>x<0 || y<0 || x>=Info.Tiles || y>=Info.HeightTiles ? -1 : y/8*Columns+x/8;
    internal bool IsLand(int x,int y) {var cell=Cell(x,y);return cell>=0 && (Land[cell]&(1UL<<((y%8)*8+x%8)))!=0;}
    internal int At(Point point)=>Cell((int)Math.Floor(point.X/Info.TileSize),(int)Math.Floor(point.Y/Info.TileSize));
    internal bool IsLand(Point point)=>point.X>=0 && point.Y>=0 && IsLand((int)(point.X/Info.TileSize),(int)(point.Y/Info.TileSize));
    internal bool IsCapital(Point point){var x=(int)Math.Floor(point.X/Info.TileSize);var y=(int)Math.Floor(point.Y/Info.TileSize);var cell=Cell(x,y);return cell>=0 && (Ink[cell]&(1UL<<((y%8)*8+x%8)))!=0;}
    internal Point Center(int cell)
    {
        var mask=Ink[cell]!=0 ? Ink[cell] : Land[cell];if(mask==0)return new();var best=0;var score=double.MaxValue;
        if(mask==ulong.MaxValue)return new((cell%Columns*8+4)*Info.TileSize,(cell/Columns*8+4)*Info.TileSize);
        for(var bit=0;bit<64;bit++)if((mask&(1UL<<bit))!=0) {var x=cell%Columns*8+bit%8;var y=cell/Columns*8+bit/8;var neighbors=(IsLand(x-1,y)?1:0)+(IsLand(x+1,y)?1:0)+(IsLand(x,y-1)?1:0)+(IsLand(x,y+1)?1:0);var d=Math.Pow(bit%8-3.5,2)+Math.Pow(bit/8-3.5,2)-neighbors*100;if(d<score){score=d;best=bit;}}
        return new((cell%Columns*8+best%8+.5)*Info.TileSize,(cell/Columns*8+best/8+.5)*Info.TileSize);
    }
    internal IEnumerable<int> Neighbors(int cell)
    {
        var x=cell%Columns;var y=cell/Columns;
        foreach(var n in new[]{x>0 ? cell-1 : -1,x<Columns-1 ? cell+1 : -1,y>0 ? cell-Columns : -1,y<Rows-1 ? cell+Columns : -1})
            if(n>=0 && Land[n]!=0 && Touches(cell,n))yield return n;
    }
    private bool Touches(int a,int b)
    {
        if(b==a-1 || b==a+1) {var left=b==a+1 ? a : b;var right=b==a+1 ? b : a;for(var y=0;y<8;y++)if((Land[left]&(1UL<<(y*8+7)))!=0 && (Land[right]&(1UL<<(y*8)))!=0)return true;}
        else {var top=Math.Min(a,b);var bottom=Math.Max(a,b);return ((Land[top]>>56)&Land[bottom]&255)!=0;}
        return false;
    }
    internal int Area(int cell)=>BitOperations.PopCount(Land[cell]);
}
