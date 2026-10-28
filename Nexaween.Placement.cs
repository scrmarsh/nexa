using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class NexaweenDecorations
{
    private readonly List<(Rect Widget,Rect Pumpkin)> vineCorners=[];
    internal static readonly int[] CandyKinds=[5,6,7,8,9,13,15,16,17,18];
    internal int CandyActorCount=>actors.Count(a=>CandyKinds.Contains(a.Kind));
    internal int ContextualActorCount=>actors.Count(a=>a.Contextual);
    internal IReadOnlyList<Rect> AttachmentBounds=>actors.Where(a=>a.Contextual).Select(a=>a.Bounds).ToArray();
    private static void DrawVine(DrawingContext dc,Rect rect,double time=0,Rect? pumpkin=null)
    {
        var fruit=pumpkin??new Rect(rect.Right-30,rect.Bottom-15,88,88);var stem=PumpkinStalk(fruit);
        var left=fruit.Left+fruit.Width/2<rect.Left+rect.Width/2;var top=fruit.Top+fruit.Height/2<rect.Top+rect.Height/2;
        var x=left?rect.Left-4:rect.Right+4;var y=top?rect.Top-4:rect.Bottom+4;var dx=left?1d:-1d;var dy=top?1d:-1d;
        var growth=time<=0?1:Math.Clamp(time/20,.12,1);dc.PushClip(new RectangleGeometry(new Rect(stem.X-240*growth,stem.Y-240*growth,480*growth,480*growth)));
        for(var branch=0;branch<2;branch++)
        {
            var sway=time>0?Math.Sin(time*.32+branch)*1.3:0;
            var end=branch==0?new Point(x+dx*Math.Min(160,rect.Width*.55),y+sway):new Point(x+sway,y+dy*Math.Min(140,rect.Height*.55));
            var one=new Point(stem.X+dx*24,stem.Y-22);var two=new Point(end.X-dx*36,end.Y-dy*26);
            var path=new StreamGeometry();using(var g=path.Open()){g.BeginFigure(stem,false,false);g.BezierTo(one,two,end,true,false);g.BezierTo(new(end.X+dx*24,end.Y-dy*25),new(end.X+dx*43,end.Y+dy*20),new(end.X+dx*13,end.Y+dy*17),true,false);g.BezierTo(new(end.X-dx*2,end.Y+dy*14),new(end.X+dx*7,end.Y+dy*2),new(end.X+dx*14,end.Y+dy*8),true,false);}path.Freeze();
            dc.DrawGeometry(null,BugVisuals.P("#C0354B2F",4),path);dc.DrawGeometry(null,BugVisuals.P("#704F6C39",1.3),path);
            dc.PushTransform(new TranslateTransform(end.X,end.Y));dc.PushTransform(new RotateTransform(branch*65));
            dc.DrawGeometry(B("#B04F6540"),P("#8033462D",.5),Geometry.Parse("M0,0 Q-15,-5 -18,-19 Q-5,-24 4,-12 Q12,-24 23,-16 Q18,-2 0,0 Z"));dc.Pop();dc.Pop();
        }
        dc.Pop();
    }

    private void AddContextual(DrawingContext dc,IReadOnlyList<Rect> widgets,List<Rect> occupied,int limit)
    {
        var count=0;
        var selection=new Random(102725);
        foreach(var rect in widgets.OrderBy(_=>selection.Next()))
        {
            if(count>=limit)break;var size=88d;
            var candidates=new[]{new Rect(Math.Clamp(rect.Right-size*.4,0,Math.Max(0,Width-size)),rect.Bottom-size*.18,size,size),new Rect(Math.Clamp(rect.Left-size*.6,0,Math.Max(0,Width-size)),rect.Bottom-size*.18,size,size),new Rect(Math.Clamp(rect.Left-size*.3,0,Math.Max(0,Width-size)),rect.Top-size*.85,size,size)};
            var bounds=Rect.Empty;
            foreach(var candidate in candidates)
            {
                var safe=candidate;safe.Inflate(5,5);
                if(candidate.Top<0 || candidate.Bottom>Height || occupied.Any(other=>other!=rect && other.IntersectsWith(safe)))continue;
                bounds=candidate;break;
            }
            if(bounds.IsEmpty)continue;
            var kind=1;var asset=Asset(kind);dc.PushTransform(new TranslateTransform(bounds.X,bounds.Y));dc.PushTransform(new ScaleTransform(size/64,size/64));dc.DrawDrawing(asset);dc.Pop();dc.Pop();actors.Add(new(bounds,kind,asset,100+count,true));occupied.Add(bounds);
            if(kind==1){vineCorners.Add((rect,bounds));DrawVine(dc,rect,0,bounds);}count++;
        }
    }
    private static void DrawSeasonalExtra(DrawingContext dc,int kind)
    {
        if(kind==20)
        {
            Shape(dc,"M10,48 L27,6 Q38,18 46,45 L60,52 Q29,63 3,52 Z","#24152F","#A855F7");
            Shape(dc,"M16,39 Q30,46 45,39 L48,47 Q29,54 13,47 Z","#F97316");dc.DrawRectangle(B("#FACC15"),null,new Rect(28,42,8,7));dc.DrawRectangle(B("#24152F"),null,new Rect(30,43,4,4));return;
        }
        var eye=new RadialGradientBrush(Colors.White,(Color)ColorConverter.ConvertFromString("#A8A8BE")){GradientOrigin=new Point(.35,.25)};eye.Freeze();dc.DrawEllipse(eye,P("#766878",1),new(32,32),26,24);
        for(var side=-1;side<=1;side+=2)for(var i=0;i<3;i++)
        {var y=18+i*12;var vein=new StreamGeometry();using(var g=vein.Open()){g.BeginFigure(new(32+side*24,y),false,false);g.BezierTo(new(32+side*18,y+4),new(32+side*19,32),new(32+side*12,32),true,false);}dc.DrawGeometry(null,P("#CCEF4444",.6),vein);}
        dc.DrawEllipse(B("#84CC16"),P("#365314",1),new(32,32),12,14);dc.DrawEllipse(B("#24152F"),null,new(32,32),5,8);dc.DrawEllipse(B("#FFFFFF"),null,new(28,27),3,3);
    }
    private static void DrawExtraCandy(DrawingContext dc,int kind)
    {
        switch(kind)
        {
            case 15:
                Shape(dc,"M3,11 L22,22 L19,41 L3,52 L9,32 Z M61,11 L43,22 L44,41 L61,52 L54,32 Z","#84CC16");
                Shape(dc,"M18,21 Q31,12 47,22 L45,43 Q31,53 18,42 Z","#A855F7","#D9F99D");dc.DrawEllipse(null,P("#FACC15",2),new(32,32),7,8);break;
            case 16:
                for(var i=0;i<10;i++){var p=new Point(8+i*5,32+Math.Sin(i*.65)*15);dc.DrawEllipse(B(i<5?"#84CC16":"#F97316"),P("#C9DA88",.6),p,5,6);}dc.DrawEllipse(B("#24152F"),null,new(9,29),1,1);break;
            case 17:
                dc.DrawEllipse(B("#365314"),P("#D9F99D",2),new(32,32),23,23);dc.DrawEllipse(B("#84CC16"),null,new(29,29),19,20);Shape(dc,"M32,12 L36,25 L49,26 L39,34 L41,47 L31,40 L19,47 L23,34 L14,25 L27,25 Z","#D9F99D");break;
            case 18:
                dc.DrawLine(P("#E9D5FF",4),new(33,39),new(42,63));NexaPumpkin.Draw(dc,new Rect(2,0,60,54));break;
        }
    }
}
