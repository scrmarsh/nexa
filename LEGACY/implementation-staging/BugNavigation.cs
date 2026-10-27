using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    internal Point VisibleWaypoint(Point from,Point target,double margin=20)
    {
        if(Surfaces.Count==0)return target;
        bool Clear(Point p)=>p.X>=12 && p.Y>=12 && p.X<=WorldWidth-12 && p.Y<=WorldHeight-12 && !Surfaces.Values.Any(r=>Expanded(r,margin).Contains(p));
        foreach(var surface in Surfaces.Values)
        {
            var rect=Expanded(surface,margin);
            if(!rect.Contains(from)&&!Crosses(from,target,rect))continue;
            var candidates=new[]{new Point(rect.Left-1,Math.Clamp(from.Y,rect.Top,rect.Bottom)),new Point(rect.Right+1,Math.Clamp(from.Y,rect.Top,rect.Bottom)),new Point(Math.Clamp(from.X,rect.Left,rect.Right),rect.Top-1),new Point(Math.Clamp(from.X,rect.Left,rect.Right),rect.Bottom+1),new Point(rect.Left-1,rect.Top-1),new Point(rect.Right+1,rect.Top-1),new Point(rect.Left-1,rect.Bottom+1),new Point(rect.Right+1,rect.Bottom+1)};
            var valid=candidates.Where(Clear).Where(p=>rect.Contains(from)||!Crosses(from,p,rect)).OrderBy(p=>(p-from).Length+(p-target).Length).ToArray();
            if(valid.Length>0)return valid[0];
        }
        return target;
    }
    private static Rect Expanded(Rect rect,double margin){rect.Inflate(margin,margin);return rect;}
    private static bool Crosses(Point a,Point b,Rect rect)
    {
        var delta=b-a;var low=0d;var high=1d;
        bool Slab(double origin,double direction,double min,double max)
        {if(Math.Abs(direction)<.00001)return origin>min && origin<max;var t1=(min-origin)/direction;var t2=(max-origin)/direction;if(t1>t2)(t1,t2)=(t2,t1);low=Math.Max(low,t1);high=Math.Min(high,t2);return low<high;}
        return Slab(a.X,delta.X,rect.Left,rect.Right)&&Slab(a.Y,delta.Y,rect.Top,rect.Bottom)&&high>.001&&low<.999;
    }
}
