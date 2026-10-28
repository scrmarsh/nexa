using System.Windows;
using System.Windows.Media;
namespace SessionDashboard;
internal sealed partial class NexaweenDecorations
{
    private readonly List<Rect> vineCorners=[];
    internal static readonly int[] CandyKinds=[5,6,7,8,9,13,15,16,17,18];
    internal int CandyActorCount=>actors.Count(a=>CandyKinds.Contains(a.Kind));
    internal int ContextualActorCount=>actors.Count(a=>a.Contextual);
    internal IReadOnlyList<Rect> AttachmentBounds=>actors.Where(a=>a.Contextual).Select(a=>a.Bounds).ToArray();
    private static void DrawVine(DrawingContext dc,Rect rect,double time=0)
    {
        var x=rect.Right+3;var y=rect.Bottom+3;var sway=0d;
        var path=new StreamGeometry();using(var g=path.Open()){g.BeginFigure(new Point(x-68,y),false,false);g.BezierTo(new(x+15,y+9),new(x-8+sway,y-35),new(x+3,y-80),true,false);g.BezierTo(new(x+15,y-95),new(x+23,y-70),new(x+8,y-72),true,false);}path.Freeze();
        var growth=time<=0?1:Math.Clamp(time/20,.12,1);
        dc.PushClip(new RectangleGeometry(new Rect(x-80*growth,y-110*growth,115,115*growth)));
        dc.DrawGeometry(null,P("#99567D36",4),path);
        for(var branch=0;branch<3;branch++)
        {var line=new StreamGeometry();using(var g=line.Open()){g.BeginFigure(new(x-branch*17,y),false,false);g.BezierTo(new(x-30-branch*22,y+15),new(x-45-branch*12,y-branch*12),new(x-60-branch*17,y-8),true,false);}dc.DrawGeometry(null,P("#7065A30D",1.5),line);}
        for(var i=0;i<3;i++){var p=new Point(x-i*19-12,y+Math.Sin(i)*3);dc.PushTransform(new RotateTransform(-35+i*25,p.X,p.Y));dc.DrawEllipse(B("#8065A30D"),null,p,7,3);dc.Pop();}
        dc.Pop();
    }
    private void AddContextual(DrawingContext dc,IReadOnlyList<Rect> widgets,List<Rect> occupied,int limit)
    {
        var count=0;
        var selection=new Random(102725);
        foreach(var rect in widgets.OrderBy(_=>selection.Next()))
        {
            if(count>=limit)break;var size=76d;
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
            if(kind==1){vineCorners.Add(rect);DrawVine(dc,rect);}count++;
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
                dc.DrawLine(P("#E9D5FF",4),new(33,39),new(42,63));dc.DrawEllipse(B("#F97316"),P("#9A3412",2),new(31,26),21,19);dc.DrawLine(P("#65A30D",5),new(31,8),new(35,3));Shape(dc,"M20,27 L25,20 L29,27 Z M35,27 L40,20 L44,27 Z M20,32 Q31,43 43,32 Z","#84CC16");break;
        }
    }
}
