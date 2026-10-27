using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal static class BugHabitatVisuals
{
    private static readonly Dictionary<BugSpecies,Drawing> cache=[];
    internal static Drawing For(BugSpecies species)
    {
        if(cache.TryGetValue(species,out var art))return art;
        var group=new DrawingGroup();using(var dc=group.Open())
        {
            switch(species)
            {
                case BugSpecies.Ants:
                    dc.DrawGeometry(BugVisuals.B("#61442D"),BugVisuals.P("#A17C51",.7),Geometry.Parse("M-16,7 Q-10,-7 0,-9 Q13,-5 17,7 Z"));
                    dc.DrawEllipse(BugVisuals.B("#20191A"),null,new(0,3),5,3);break;
                case BugSpecies.Roaches:
                    dc.DrawGeometry(BugVisuals.B("#49442B"),BugVisuals.P("#71674A",.8),Geometry.Parse("M-18,5 Q-12,-13 5,-9 L18,5 Q0,1 -18,5 Z"));
                    dc.DrawLine(BugVisuals.P("#8E8055",1),new(-13,2),new(10,-6));dc.DrawEllipse(BugVisuals.B("#17171B"),null,new(1,4),10,2.5);break;
                case BugSpecies.Flies:
                    dc.DrawEllipse(BugVisuals.B("#773849"),BugVisuals.P("#B77B84",.7),new(-3,0),11,9);
                    dc.DrawEllipse(BugVisuals.B("#B4A06D"),null,new(2,2),6,6);dc.DrawLine(BugVisuals.P("#6E7E46",2),new(-2,-7),new(3,-14));break;
                case BugSpecies.Spiders:
                    dc.DrawLine(BugVisuals.P("#655147",2),new(-15,10),new(-13,-13));dc.DrawLine(BugVisuals.P("#655147",2),new(-13,-13),new(16,-11));
                    for(var i=0;i<8;i++){var a=i*Math.PI/4;dc.DrawLine(BugVisuals.P("#9A8CA9",.5),new(0,0),new(Math.Cos(a)*14,Math.Sin(a)*14));}
                    for(var r=5;r<=13;r+=4)dc.DrawEllipse(null,BugVisuals.P("#9A8CA9",.4),new(),r,r);break;
                case BugSpecies.Ladybugs:
                    dc.DrawLine(BugVisuals.P("#7E9251",2),new(-5,12),new(5,-12));
                    dc.DrawGeometry(BugVisuals.B("#607F45"),BugVisuals.P("#A7B878",.7),Geometry.Parse("M0,1 Q-20,-13 -14,0 Q-9,8 0,1 Z M1,0 Q17,-13 16,-1 Q11,8 1,0 Z"));break;
                case BugSpecies.Centipedes:
                    dc.DrawRoundedRectangle(BugVisuals.B("#4D3D30"),BugVisuals.P("#8F7251",1),new Rect(-19,-7,38,16),4,4);
                    for(var i=0;i<4;i++)dc.DrawLine(BugVisuals.P("#6E523A",.8),new(-15,-4+i*3),new(14,-3+i*3));
                    dc.DrawEllipse(BugVisuals.B("#17151A"),null,new(0,6),11,2.8);break;
            }
        }
        group.Freeze();cache[species]=group;return group;
    }
}
