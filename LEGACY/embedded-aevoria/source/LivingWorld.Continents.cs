using System.Windows;

namespace SessionDashboard;
internal sealed partial class LivingTerrain
{
    // Immutable, seed-derived drainage atlas. At most 513 x 513 nodes and 2048 x 1152 river bytes.
    // Built once on the world-creation worker, never during simulation or rendering.
    private int atlasWidth,atlasHeight,atlasStep;
    private float[] elevation=[],rainfall=[];
    private byte[] rivers=[];
    internal int ContinentCount {get;private set;}
    internal int RiverSources {get;private set;}
    internal readonly List<(int From,int To)> Drainage=[];
    internal bool DrainageDownhill {get;private set;}=true;
    internal int OceanOutlets {get;private set;}
    internal long AtlasBytes=>(long)(elevation.Length+rainfall.Length)*4+rivers.Length;

    private void BuildContinents()
    {
        atlasStep=Math.Max(1,(int)Math.Ceiling(Info.Tiles/512d));
        atlasWidth=(int)Math.Ceiling(Info.Tiles/(double)atlasStep)+1;
        atlasHeight=(int)Math.Ceiling(Info.HeightTiles/(double)atlasStep)+1;
        var length=atlasWidth*atlasHeight;elevation=new float[length];rainfall=new float[length];
        ContinentCount=Info.Tiles>=2048 ? 5 : Info.Tiles>=1536 ? 4 : Info.Tiles>=512 ? 3 : 2;
        var columns=ContinentCount<=3 && Info.Tiles>Info.HeightTiles ? ContinentCount : (ContinentCount+1)/2;
        var rows=(int)Math.Ceiling(ContinentCount/(double)columns);
        var centers=new Point[ContinentCount];
        for(var c=0;c<centers.Length;c++)centers[c]=new((c%columns+.5+Hash(c,0,21)*.06)/columns,(c/columns+.5+Hash(c,0,22)*.06)/rows);
        for(var y=0;y<atlasHeight;y++)for(var x=0;x<atlasWidth;x++)
        {
            var u=x*atlasStep/(double)Info.Tiles;var v=y*atlasStep/(double)Info.HeightTiles;
            var warpX=Noise(u*7,v*7,23)*.055/columns;var warpY=Noise(u*7,v*7,24)*.055/rows;
            var h=-1d;
            for(var c=0;c<centers.Length;c++)
            {
                var dx=(u+warpX-centers[c].X)*columns/.40;var dy=(v+warpY-centers[c].Y)*rows/.38;
                var angle=Math.Atan2(dy,dx);var coast=1+.14*Math.Sin(angle*3+Hash(c,1,25)*3)+.09*Math.Sin(angle*5+Hash(c,1,26)*3);
                h=Math.Max(h,(1-Math.Sqrt(dx*dx+dy*dy)/coast)*.48-.045);
                // A small shelf archipelago beside each mainland; no random inland water dots.
                for(var island=0;island<3;island++)
                {
                    var ax=(u-centers[c].X)*columns-.43;var ay=(v-centers[c].Y)*rows-(island-1)*.065;
                    var radius=.027+(Hash(c,island,31)+1)*.008;
                    h=Math.Max(h,(1-Math.Sqrt(ax*ax+ay*ay)/radius)*.2-.035);
                }
            }
            // Broad ridges and coastal inlets; small islands are confined to continental shelves.
            h+=Noise(u*12,v*12,27)*.055;
            var ridge=1-Math.Abs(Noise(u*6,v*6,28));
            if(h>.02)h+=Math.Pow(ridge,7)*.34;
            elevation[y*atlasWidth+x]=(float)h;
            rainfall[y*atlasWidth+x]=(float)(Noise(u*4,v*4,29)*.8+Noise(u*9,v*9,30)*.2);
        }
        RouteRivers();
    }

    private void RouteRivers()
    {
        // Priority flood produces an acyclic downhill drainage tree. Inland depressions are
        // filled to their spill height, so every river has a connected route to an ocean.
        var length=elevation.Length;var parent=new int[length];Array.Fill(parent,-1);
        ReadOnlySpan<int> neighborX=[-1,1,0,0,-1,-1,1,1],neighborY=[0,0,-1,1,-1,1,-1,1];
        var visited=new bool[length];var order=new List<int>(length);var flood=new PriorityQueue<int,(float,int)>();
        for(var i=0;i<length;i++)if(elevation[i]<=0 || i%atlasWidth==0 || i%atlasWidth==atlasWidth-1 || i<atlasWidth || i>=length-atlasWidth)
        {visited[i]=true;flood.Enqueue(i,(elevation[i],i));}
        while(flood.TryDequeue(out var p,out _))
        {
            order.Add(p);var px=p%atlasWidth;var py=p/atlasWidth;
            for(var d=0;d<8;d++)
            {
                var x=px+neighborX[d];var y=py+neighborY[d];
                if(x<0 || y<0 || x>=atlasWidth || y>=atlasHeight)continue;
                var n=y*atlasWidth+x;if(visited[n])continue;visited[n]=true;parent[n]=p;
                elevation[n]=Math.Max(elevation[n],elevation[p]+.00001f);flood.Enqueue(n,(elevation[n],n));
            }
        }
        // Choose the steepest of the eight downhill neighbors after filling depressions.
        // This follows relief instead of drawing long horizontal/vertical flood-tree channels.
        for(var n=0;n<length;n++)
        {
            if(elevation[n]<=0 || parent[n]<0)continue;
            var px=n%atlasWidth;var py=n/atlasWidth;var steepest=0d;
            for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
            {
                var x=px+dx;var y=py+dy;if(dx==0 && dy==0 || x<0 || y<0 || x>=atlasWidth || y>=atlasHeight)continue;
                var p=y*atlasWidth+x;var slope=(elevation[n]-elevation[p])/(dx!=0 && dy!=0 ? 1.41421356237 : 1);
                if(slope>steepest) {steepest=slope;parent[n]=p;}
            }
        }
        var flow=new int[length];Array.Fill(flow,1);
        for(var i=order.Count-1;i>=0;i--) {var n=order[i];if(parent[n]>=0)flow[parent[n]]+=flow[n];}
        rivers=new byte[Info.Tiles*Info.HeightTiles];var routed=new bool[length];
        var threshold=Math.Max(28,length/2200);
        foreach(var source in order.AsEnumerable().Reverse())
        {
            if(elevation[source]<.24 || flow[source]<threshold || routed[source])continue;
            RiverSources++;
            for(var n=source;parent[n]>=0 && elevation[n]>0 && !routed[n];n=parent[n])
            {
                routed[n]=true;var p=parent[n];var x=n%atlasWidth*atlasStep;var y=n/atlasWidth*atlasStep;
                Drainage.Add((n,p));DrainageDownhill &= elevation[n]>=elevation[p];if(elevation[p]<=0)OceanOutlets++;
                var endX=p%atlasWidth*atlasStep;var endY=p/atlasWidth*atlasStep;
                var radius=flow[n]>threshold*14 ? 2 : 1;
                // Thick, connected raster edges remain continuous at chunk boundaries.
                for(;;)
                {
                    for(var yy=Math.Max(0,y-radius);yy<=Math.Min(Info.HeightTiles-1,y+radius);yy++)
                        for(var xx=Math.Max(0,x-radius);xx<=Math.Min(Info.Tiles-1,x+radius);xx++)rivers[yy*Info.Tiles+xx]=255;
                    if(x==endX && y==endY)break;x+=Math.Sign(endX-x);y+=Math.Sign(endY-y);
                }
            }
        }
    }
    internal Point DrainagePoint(int index)=>new(index%atlasWidth*atlasStep,index/atlasWidth*atlasStep);

    private double Field(float[] field,double x,double y)
    {
        var ax=Math.Clamp(x/atlasStep,0,atlasWidth-1.000001);var ay=Math.Clamp(y/atlasStep,0,atlasHeight-1.000001);
        var ix=(int)ax;var iy=(int)ay;var fx=ax-ix;var fy=ay-iy;var i=iy*atlasWidth+ix;
        return (field[i]*(1-fx)+field[i+1]*fx)*(1-fy)+(field[i+atlasWidth]*(1-fx)+field[i+atlasWidth+1]*fx)*fy;
    }
    private double RiverAt(double x,double y)
    {
        x=Math.Clamp(x-.5,0,Info.Tiles-1.000001);y=Math.Clamp(y-.5,0,Info.HeightTiles-1.000001);
        var ix=(int)x;var iy=(int)y;var fx=x-ix;var fy=y-iy;var i=iy*Info.Tiles+ix;
        return (rivers[i]*(1-fx)+rivers[i+1]*fx)*(1-fy)+(rivers[i+Info.Tiles]*(1-fx)+rivers[i+Info.Tiles+1]*fx)*fy;
    }
    private TerrainTile SampleContinents(double x,double y)
    {
        if(x<0 || y<0 || x>=Info.Tiles || y>=Info.HeightTiles)return new(TerrainBiome.DeepOcean,-1);
        var h=Field(elevation,x,y);var rain=Field(rainfall,x,y);var latitude=Math.Abs(y/Info.HeightTiles*2-1);
        var temperature=1-latitude*.9-h*.4;
        var biome=h<-.16 ? TerrainBiome.DeepOcean : h<0 ? TerrainBiome.ShallowOcean : RiverAt(x,y)>90 ? TerrainBiome.River : h<.024 ? TerrainBiome.Beach :
            h>.66 || h>.44 && temperature<.28 ? TerrainBiome.Snow :
            h>.52 ? TerrainBiome.Mountain : h>.34 ? TerrainBiome.Hills :
            temperature>.52 && rain<-.16 ? TerrainBiome.Desert : rain>.04 ? TerrainBiome.Forest : TerrainBiome.Grassland;
        if(edits.TryGetValue(((int)x,(int)y),out var changed))biome=changed;
        return new(biome,h);
    }
    internal uint ColorAt(double x,double y)
    {
        if(Info.GeneratorVersion==1)return Color(Sample((int)Math.Floor(x),(int)Math.Floor(y)));
        var tile=SampleContinents(x,y);
        uint color=tile.Biome switch {TerrainBiome.DeepOcean=>0xff29485e,TerrainBiome.ShallowOcean=>0xff4e7684,TerrainBiome.River=>0xff5c9cae,TerrainBiome.Lake=>0xff5c9cae,TerrainBiome.Beach=>0xffc5bd91,TerrainBiome.Grassland=>0xff8b9c6b,TerrainBiome.Forest=>0xff526f50,TerrainBiome.Desert=>0xffc6ae80,TerrainBiome.Hills=>0xff9b8a6b,TerrainBiome.Mountain=>0xff93938b,_=>0xffe0e3d7};
        // Northwest lighting and slope relief are derived from the same height atlas, at any zoom.
        var slope=Field(elevation,x-atlasStep,y-atlasStep)-Field(elevation,x+atlasStep,y+atlasStep);
        var relief=tile.Biome is TerrainBiome.Hills or TerrainBiome.Mountain or TerrainBiome.Snow;
        var light=1+Math.Clamp(slope*(relief ? 3 : .7),-.25,.25);
        return 0xff000000u|((uint)Math.Clamp((int)(((color>>16)&255)*light),0,255)<<16)|((uint)Math.Clamp((int)(((color>>8)&255)*light),0,255)<<8)|(uint)Math.Clamp((int)((color&255)*light),0,255);
    }
}
