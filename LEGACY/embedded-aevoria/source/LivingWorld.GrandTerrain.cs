namespace SessionDashboard;
internal sealed partial class LivingTerrain
{
    // Generator 3 owns this field construction. Legacy generator 2 remains unchanged.
    private void BuildKingdoms()
    {
        atlasStep=Math.Max(1,(int)Math.Ceiling(Info.Tiles/512d));
        atlasWidth=(int)Math.Ceiling(Info.Tiles/(double)atlasStep)+1;atlasHeight=(int)Math.Ceiling(Info.HeightTiles/(double)atlasStep)+1;
        elevation=new float[checked(atlasWidth*atlasHeight)];rainfall=new float[elevation.Length];
        var settings=Info.Generation!;var islands=settings.Profile==GeographyProfile.Archipelago;var balanced=settings.Profile==GeographyProfile.BalancedContinents;
        ContinentCount=islands ? 9 : balanced ? 3 : 1;
        for(var y=0;y<atlasHeight;y++)for(var x=0;x<atlasWidth;x++)
        {
            var u=x*atlasStep/(double)Info.Tiles;var v=y*atlasStep/(double)Info.HeightTiles;
            var wx=Noise(u*7,v*7,101)*.045;var wy=Noise(u*7,v*7,102)*.045;
            double h;
            if(!islands && !balanced)
            {
                h=1-Math.Pow(Math.Abs((u-.49+wx)/.49),4)-Math.Pow(Math.Abs((v-.5+wy)/.47),4);
                // Two shelf islands with narrow sea channels, rather than an island quota.
                for(var i=0;i<2;i++){var dx=(u-(i==0?.065:.935))/.035;var dy=(v-(i==0?.10:.90))/.065;var distance=dx*dx+dy*dy;if(distance<2.25)h=Math.Min(h,-1);h=Math.Max(h,.6-distance);}
            }
            else
            {
                h=-10;var columns=islands ? 3:3;var rows=islands ? 3:1;
                for(var i=0;i<ContinentCount;i++){var cx=(i%columns+.5+Hash(i,0,103)*.12)/columns;var cy=(i/columns+.5+Hash(i,0,104)*.12)/rows;var dx=(u+wx-cx)*columns/.44;var dy=(v+wy-cy)*rows/.42;h=Math.Max(h,1-(dx*dx+dy*dy));}
            }
            h+=Noise(u*17,v*17,105)*.12;
            elevation[y*atlasWidth+x]=(float)h;rainfall[y*atlasWidth+x]=(float)(Noise(u*4,v*4,106)*.8+Noise(u*11,v*11,107)*.2);
        }
        var sorted=(float[])elevation.Clone();Array.Sort(sorted);var sea=sorted[(int)((1-settings.LandFraction)*(sorted.Length-1))];
        for(var y=0;y<atlasHeight;y++)for(var x=0;x<atlasWidth;x++)
        {
            var i=y*atlasWidth+x;var h=(elevation[i]-sea)*.35;
            if(h>0)
            {
                var u=x*atlasStep/(double)Info.Tiles;var v=y*atlasStep/(double)Info.HeightTiles;
                var a=.26+.12*Math.Sin(v*6+Hash(0,0,108)*3);var b=.68+.12*Math.Sin(v*5+Hash(1,0,108)*3);
                var ridge=Math.Max(Math.Exp(-Math.Pow((u-a)/.045,2)),.85*Math.Exp(-Math.Pow((u-b)/.055,2)));
                h=.025+Math.Min(.20,h*.35)+ridge*.48;
            }
            if(x==0 || y==0 || x==atlasWidth-1 || y==atlasHeight-1)h=-.28;
            elevation[i]=(float)h;
        }
        RouteRivers();
        // Legacy routing uses wide river raster strokes. Generator 3 retains the exact
        // connected drainage tree but draws one-tile channels to preserve traversable land.
        Array.Clear(rivers);
        foreach(var (from,to) in Drainage)
        {
            var x=from%atlasWidth*atlasStep;var y=from/atlasWidth*atlasStep;var endX=to%atlasWidth*atlasStep;var endY=to/atlasWidth*atlasStep;
            for(;;){if(x>=0 && y>=0 && x<Info.Tiles && y<Info.HeightTiles)rivers[y*Info.Tiles+x]=255;if(x==endX && y==endY)break;x+=Math.Sign(endX-x);y+=Math.Sign(endY-y);}
        }
    }
}
