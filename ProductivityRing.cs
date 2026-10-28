using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace SessionDashboard;
internal sealed class ProductivityRing:FrameworkElement
{
    internal static readonly Brush LightningBrush=BugVisuals.B("#FACC15");
    private long target=long.MinValue;
    internal static double Fraction(long score)=>Math.Max(0,score)%500/500d;
    private static readonly DependencyProperty DisplayScoreProperty=DependencyProperty.Register("DisplayScore",typeof(double),typeof(ProductivityRing),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    private double DisplayScore {get=>(double)GetValue(DisplayScoreProperty);set=>SetValue(DisplayScoreProperty,value);}
    internal ProductivityRing(){Width=190;Height=190;IsHitTestVisible=false;Unloaded+=(_,_)=>BeginAnimation(DisplayScoreProperty,null);}
    internal void Update(long value,NexaOptions options)
    {
        if(target==value)return;
        var initial=target==long.MinValue;target=value;var from=DisplayScore;BeginAnimation(DisplayScoreProperty,null);DisplayScore=value;
        if(!initial && NexaMotion.Allowed(options))
        {
            BeginAnimation(DisplayScoreProperty,new DoubleAnimation(from,value,TimeSpan.FromMilliseconds(320)){FillBehavior=FillBehavior.Stop,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
            NexaMotion.Pop(this,options);
        }
        System.Windows.Automation.AutomationProperties.SetName(this,"Productivity score "+value);
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        var center=new Point(ActualWidth/2,ActualHeight/2);var radius=Math.Min(ActualWidth,ActualHeight)/2-9;
        var accent=TryFindResource("PixelBrush") as Brush??Brushes.MediumPurple;
        if(radius<=0)return;
        dc.DrawEllipse(null,new Pen(TryFindResource("LineBrush") as Brush??Brushes.DimGray,4),center,radius,radius);
        var fraction=Math.Max(0,DisplayScore)%500/500d;
        if(fraction>.0001)
        {
            var angle=fraction*Math.PI*2;var finish=new Point(center.X+Math.Sin(angle)*radius,center.Y-Math.Cos(angle)*radius);
            var geometry=new StreamGeometry();using(var c=geometry.Open()){c.BeginFigure(new(center.X,center.Y-radius),false,false);c.ArcTo(finish,new Size(radius,radius),0,fraction>.5,SweepDirection.Clockwise,true,false);}
            geometry.Freeze();dc.PushOpacity(.10);dc.DrawGeometry(null,new Pen(accent,8),geometry);dc.Pop();dc.DrawGeometry(null,new Pen(accent,4){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},geometry);
        }
        dc.PushTransform(new TranslateTransform(center.X-10,center.Y-48));dc.DrawGeometry(LightningBrush,null,Geometry.Parse("M12,0 L2,14 L10,14 L7,27 L23,10 L14,10 L18,0 Z"));dc.Pop();
        var label=new FormattedText(Math.Round(DisplayScore).ToString("N0"),CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),29,TryFindResource("TextBrush") as Brush??Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var fit=Math.Min(1,Math.Max(1,radius*2-12)/Math.Max(1,label.Width));
        dc.PushTransform(new TranslateTransform(center.X,center.Y-8));dc.PushTransform(new ScaleTransform(fit,fit));
        dc.DrawText(label,new Point(-label.Width/2,0));dc.Pop();dc.Pop();
    }
}
