using System.Windows;
namespace SessionDashboard;
internal static class NexaWebGeometry
{
    internal static Point Spoke(EcosystemWeb web,int index,double radius)
    {
        var seed=(uint)web.Owner.GetHashCode();var angle=index*Math.PI/4+Math.Sin(index*2.1+seed%23)*.12;
        var length=radius*(.86+((seed>>(index*3%24))&7)/28d);return new(web.X+Math.Cos(angle)*length,web.Y+Math.Sin(angle)*length);
    }
}
