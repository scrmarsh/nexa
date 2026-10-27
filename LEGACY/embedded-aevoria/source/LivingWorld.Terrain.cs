using System.Text;
using System.Windows;

namespace SessionDashboard;
internal readonly record struct TerrainTile(TerrainBiome Biome,double Height);
internal readonly record struct ChunkKey(int X,int Y,int Level=0)
{
    // Eighth-octave detail bands keep sampling within 9% of the requested physical density.
    internal double Step=>Math.Pow(2,Math.Clamp(Level,-64,64)/8d);
}
internal sealed partial class LivingTerrain
{
    internal readonly LivingWorldInfo Info;
    private readonly ulong seed;
    private readonly Dictionary<(int,int),TerrainBiome> edits;
    internal LivingTerrain(LivingWorldInfo info)
    {
        info.Validate();Info=info;seed=14695981039346656037UL;
        foreach(var b in Encoding.UTF8.GetBytes(info.Seed))seed=unchecked((seed^b)*1099511628211UL);
        edits=info.Edits.GroupBy(e=>(e.X,e.Y)).ToDictionary(g=>g.Key,g=>g.Last().Biome);
        if(info.GeneratorVersion==2)BuildContinents();else if(info.GeneratorVersion==3)BuildKingdoms();
    }
    internal static int FloorDiv(int value,int divisor)=>(int)Math.Floor(value/(double)divisor);
    private static ulong Mix(ulong v) {v=unchecked((v^(v>>30))*0xbf58476d1ce4e5b9UL);v=unchecked((v^(v>>27))*0x94d049bb133111ebUL);return v^(v>>31);}
    private double Hash(int x,int y,int domain)=>((Mix(seed^unchecked((ulong)(long)x*0x9e3779b97f4a7c15UL)^unchecked((ulong)(long)y*0xd1b54a32d192ed03UL)^((ulong)domain*7919))>>11)*(1d/(1UL<<53)))*2-1;
    private double Noise(double x,double y,int domain)
    {
        var ix=(int)Math.Floor(x);var iy=(int)Math.Floor(y);var fx=x-ix;var fy=y-iy;
        fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
        var a=Hash(ix,iy,domain);var b=Hash(ix+1,iy,domain);var c=Hash(ix,iy+1,domain);var d=Hash(ix+1,iy+1,domain);
        return (a+(b-a)*fx)*(1-fy)+(c+(d-c)*fx)*fy;
    }
    internal TerrainTile Sample(int x,int y)
    {
        if(Info.GeneratorVersion>=2)return SampleContinents(x+.5,y+.5);
        if(x<0 || y<0 || x>=Info.Tiles || y>=Info.Tiles)return new(TerrainBiome.DeepOcean,-1);
        var scale=Info.Tiles/512d;var xx=x/scale;var yy=y/scale;
        var wx=Noise(xx/125,yy/125,1)*32;var wy=Noise(xx/125,yy/125,2)*32;
        var nx=(x+.5)/Info.Tiles*2-1;var ny=(y+.5)/Info.Tiles*2-1;var distance=Math.Sqrt(nx*nx+ny*ny);
        var h=Noise((xx+wx)/145,(yy+wy)/145,3)*.64+Noise(xx/55,yy/55,4)*.22+Noise(xx/18,yy/18,5)*.09+.24-Math.Pow(distance,3)*.52;
        var moisture=Noise(xx/82,yy/82,6)*.8+Noise(xx/25,yy/25,7)*.2;
        var temperature=1-Math.Abs(ny)*.85+Noise(xx/110,yy/110,8)*.3-h*.65;
        TerrainBiome biome;
        if(h<-.18)biome=TerrainBiome.DeepOcean;
        else if(h<-.035)biome=TerrainBiome.ShallowOcean;
        else if(h<.027)biome=TerrainBiome.Beach;
        else if(h>.56)biome=TerrainBiome.Snow;
        else if(h>.46)biome=TerrainBiome.Mountain;
        else if(h>.32)biome=TerrainBiome.Hills;
        else if(h<.24 && Noise(xx/17,yy/17,9)>.69 && moisture>-.05)biome=TerrainBiome.Lake;
        else if(h<.4 && Math.Abs(Noise((xx+wx*.35)/37,(yy+wy*.35)/37,10))<.023)biome=TerrainBiome.River;
        else if(temperature>.48 && moisture<-.18)biome=TerrainBiome.Desert;
        else if(moisture>.06)biome=TerrainBiome.Forest;
        else biome=TerrainBiome.Grassland;
        if(edits.TryGetValue((x,y),out var changed))biome=changed;
        return new(biome,h);
    }
    internal TerrainTile At(Point p)=>Sample((int)Math.Floor(p.X/Info.TileSize),(int)Math.Floor(p.Y/Info.TileSize));
    internal static bool Land(TerrainBiome b)=>b is not (TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.Lake or TerrainBiome.River);
    internal Point FindLand(Point near)
    {
        var tx=(int)(near.X/Info.TileSize);var ty=(int)(near.Y/Info.TileSize);
        for(var radius=0;radius<Math.Max(Info.Tiles,Info.HeightTiles);radius+=2)
            for(var n=0;n<(radius==0 ? 1 : 24);n++)
            {
                var angle=n*Math.PI*2/24;var x=Math.Clamp(tx+(int)(Math.Cos(angle)*radius),4,Info.Tiles-5);var y=Math.Clamp(ty+(int)(Math.Sin(angle)*radius),4,Info.HeightTiles-5);
                if(Land(Sample(x,y).Biome))return new((x+.5)*Info.TileSize,(y+.5)*Info.TileSize);
            }
        throw new InvalidOperationException("This seed has no accessible land. Choose another seed.");
    }
    internal static uint Color(TerrainTile tile)
    {
        uint color=tile.Biome switch {TerrainBiome.DeepOcean=>0xff123c70,TerrainBiome.ShallowOcean=>0xff2687ad,TerrainBiome.River=>0xff439ec0,TerrainBiome.Lake=>0xff287fba,TerrainBiome.Beach=>0xffedcf83,TerrainBiome.Grassland=>0xff7fae56,TerrainBiome.Forest=>0xff286b4b,TerrainBiome.Desert=>0xffd9b776,TerrainBiome.Hills=>0xff93905e,TerrainBiome.Mountain=>0xff929a9c,_=>0xffeef3eb};
        var light=1+Math.Clamp(tile.Height,-1,1)*.11;
        return 0xff000000u|((uint)Math.Clamp((int)(((color>>16)&255)*light),0,255)<<16)|((uint)Math.Clamp((int)(((color>>8)&255)*light),0,255)<<8)|(uint)Math.Clamp((int)((color&255)*light),0,255);
    }
}
