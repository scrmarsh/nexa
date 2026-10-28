using System.Windows;
namespace SessionDashboard;
internal sealed partial class BugManager
{
    internal double? WebCoordinateAspect {get;set;}
    internal Rect WebBounds(EcosystemWeb web)
    {
        if(web.AnchorId is {} id && Surfaces.TryGetValue(id,out var card))
        {
            var width=Math.Max(12,Math.Min(180,card.Width*.55));var height=Math.Max(12,Math.Min(150,card.Height*.5));
            if(WebCoordinateAspect is {} aspect && aspect>0)
            {
                var ratio=width*aspect/height;
                if(ratio>1.3)width=height*1.3/aspect;
                else if(ratio<.8)height=width*aspect/.8;
            }
            return new Rect(Math.Clamp(card.Right-width*.25+web.AnchorOffsetX,12,WorldWidth-width-12),Math.Clamp(card.Top-height*.75+web.AnchorOffsetY,12,WorldHeight-height-12),width,height);
        }
        return new Rect(Math.Clamp(web.X-160,12,WorldWidth-332),Math.Clamp(web.Y-115,12,WorldHeight-242),320,230);
    }
    internal static void EnsureSilk(EcosystemWeb web)
    {
        if(web.Strands.Count>0)return;
        web.Orb=true;
        var rng=new Random(web.Owner.GetHashCode());
        void Add(Point a,Point b)=>web.Strands.Add(new SilkStrand{FromX=a.X,FromY=a.Y,ToX=b.X,ToY=b.Y});
        var center=new Point(.5,.5);
        var rim=Enumerable.Range(0,12).Select(i=>{var angle=i*Math.PI/6;var radius=.44+rng.NextDouble()*.025;return center+new Vector(Math.Cos(angle)*radius,Math.Sin(angle)*radius);}).ToArray();
        // Supports, then radials, then a continuous outward spiral. Every strand starts empty.
        for(var i=0;i<12;i++)Add(rim[i],rim[(i+1)%12]);
        foreach(var anchor in rim)Add(anchor,center);
        Point Spiral(int i){var angle=i*Math.PI/6;var radius=.075+i/48d*.36;return center+new Vector(Math.Cos(angle)*radius,Math.Sin(angle)*radius);}
        for(var i=0;i<48;i++)Add(Spiral(i),Spiral(i+1));
        var completed=Math.Clamp(web.Construction,0,1)*web.Strands.Count;
        for(var i=0;i<web.Strands.Count;i++)web.Strands[i].Progress=Math.Clamp(completed-i,0,1);
    }
    internal Point SilkPoint(EcosystemWeb web,double x,double y){var box=WebBounds(web);return new(box.Left+x*box.Width,box.Top+y*box.Height);}
    private bool ChooseWebActivity(BugEntity bug)
    {
        var home=Webs.FirstOrDefault(w=>w.Owner==bug.InteractionId || w.Helpers.Contains(bug.InteractionId));
        if(home!=null && (home.Construction<1 || random.NextDouble()<.78))
        {bug.Activity=BugActivity.Nesting;bug.Target=WebBounds(home).Location;bug.Decision=Range(5,10);return true;}
        if(home!=null)return false;
        var shared=Webs.Where(w=>w.Construction<1 && w.Helpers.Count<8).OrderBy(w=>(WebBounds(w).Location-bug.Position).LengthSquared).FirstOrDefault();
        if(shared!=null && (WebBounds(shared).Location-bug.Position).Length<600)
        {shared.Helpers.Add(bug.InteractionId);bug.Activity=BugActivity.Nesting;bug.Target=WebBounds(shared).Location;bug.Decision=8;return true;}
        if(Webs.Count>=24 || random.NextDouble()>.45)return false;
        var anchor=Surfaces.Count>0?Surfaces.MinBy(p=>(p.Value.Location-bug.Position).LengthSquared):default;
        var web=new EcosystemWeb{Owner=bug.InteractionId,X=Math.Clamp(Surfaces.Count>0?anchor.Value.Right+12:bug.Position.X,12,WorldWidth-12),Y=Math.Clamp(Surfaces.Count>0?anchor.Value.Top-12:bug.Position.Y,12,WorldHeight-12),AnchorId=Surfaces.Count>0?anchor.Key:null,AnchorOffsetX=12,AnchorOffsetY=-12};
        EnsureSilk(web);Webs.Add(web);bug.Activity=BugActivity.Nesting;bug.Target=WebBounds(web).Location;bug.Decision=8;return true;
    }
    private bool PrepareSilk(BugEntity bug,EcosystemWeb web)
    {
        EnsureSilk(web);
        if(web.Construction>=1)
        {
            bug.SilkAttached=false;bug.WeavingSegment=-1;
            if(bug.WeavingOwner!=web.Owner || !WebBounds(web).Contains(bug.Target))bug.Target=SilkPoint(web,Range(.15,.85),Range(.15,.85));
            bug.WeavingOwner=web.Owner;return (bug.Target-bug.Position).Length>10;
        }
        if(bug.WeavingOwner!=web.Owner || bug.WeavingSegment<0 || bug.WeavingSegment>=web.Strands.Count || web.Strands[bug.WeavingSegment].Progress>=1)
        {
            bug.WeavingOwner=web.Owner;bug.WeavingSegment=-1;bug.SilkAttached=false;
            var claimed=Bugs.Where(b=>b!=bug && IsActive(b) && b.WeavingOwner==web.Owner && b.Activity==BugActivity.Nesting).Select(b=>b.WeavingSegment).ToHashSet();
            for(var i=0;i<web.Strands.Count;i++)if(web.Strands[i].Progress<1 && !claimed.Contains(i)){bug.WeavingSegment=i;break;}
            if(bug.WeavingSegment<0)return false;
        }
        var strand=web.Strands[bug.WeavingSegment];var from=SilkPoint(web,strand.FromX,strand.FromY);var to=SilkPoint(web,strand.ToX,strand.ToY);var tip=from+(to-from)*strand.Progress;
        if(!bug.SilkAttached && (bug.Position-tip).Length<9)bug.SilkAttached=true;
        var direction=to-from;if(direction.Length>.001)direction.Normalize();
        bug.Target=bug.SilkAttached?to+direction*(12*bug.SizeScale):tip;return true;
    }
    private void LaySilk(BugEntity bug)
    {
        if(!bug.SilkAttached || bug.WeavingSegment<0 || Webs.FirstOrDefault(w=>w.Owner==bug.WeavingOwner) is not {} web)return;
        var strand=web.Strands[bug.WeavingSegment];var from=SilkPoint(web,strand.FromX,strand.FromY);var to=SilkPoint(web,strand.ToX,strand.ToY);var direction=to-from;
        var spinneret=bug.Position-new Vector(Math.Cos(bug.Angle*Math.PI/180),Math.Sin(bug.Angle*Math.PI/180))*(12*bug.SizeScale);
        var progress=direction.LengthSquared<.01?1:Math.Clamp(Vector.Multiply(spinneret-from,direction)/direction.LengthSquared,0,1);
        strand.Progress=Math.Max(strand.Progress,(spinneret-to).Length<8?1:progress);web.Construction=web.Strands.Average(s=>s.Progress);
        if(strand.Progress>=1){bug.SilkAttached=false;bug.WeavingSegment=-1;}
    }
    private bool MarchAnt(BugEntity bug,double step)
    {
        if(bug.Species!=BugSpecies.Ants || bug.Activity!=BugActivity.Exploring || Surfaces.Count==0)return false;
        if(bug.Surface==null || !Surfaces.ContainsKey(bug.Surface))
        {
            var nearest=Surfaces.MinBy(p=>Enumerable.Range(0,32).Min(i=>(EdgePoint(p.Value,i/32d)-bug.Position).LengthSquared));bug.Surface=nearest.Key;
            bug.EdgeProgress=Enumerable.Range(0,64).MinBy(i=>(EdgePoint(nearest.Value,i/64d)-bug.Position).LengthSquared)/64d;
        }
        var rect=Surfaces[bug.Surface];var edge=EdgePoint(rect,bug.EdgeProgress);
        if((bug.Position-edge).Length>14){bug.Target=edge;return false;}
        var prior=bug.Position;bug.EdgeProgress+=Math.Max(12,bug.Speed*.55)*step/Math.Max(1,2*(rect.Width+rect.Height));bug.Position=EdgePoint(rect,bug.EdgeProgress);bug.Target=EdgePoint(rect,bug.EdgeProgress+.02);bug.State=BugState.Edge;
        var delta=bug.Target-bug.Position;bug.Angle=BugAnimation.SmoothAngle(bug.Angle,Math.Atan2(delta.Y,delta.X)*180/Math.PI,step,280);bug.CrawlPhase+=(bug.Position-prior).Length*.35;return true;
    }
}
