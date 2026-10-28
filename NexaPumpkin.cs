using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace SessionDashboard;
internal static class NexaPumpkin
{
    private static readonly BitmapSource image=Load();
    private static BitmapSource Load()
    {var value=new BitmapImage(new Uri("pack://application:,,,/Nexa;component/assets/nexaween-pumpkin-030.png"));value.Freeze();return value;}
    internal static Rect Fit(Rect bounds)
    {var height=Math.Min(bounds.Height,bounds.Width/1.5);return new(bounds.X+(bounds.Width-height*1.5)/2,bounds.Bottom-height,height*1.5,height);}
    internal static void Draw(DrawingContext dc,Rect bounds)=>dc.DrawImage(image,Fit(bounds));
    internal static double Candle(double time,double phase)=>.055+.016*Math.Sin(time*1.37+phase)+.009*Math.Sin(time*2.71+phase*1.7)+.005*Math.Sin(time*5.13+phase*.8);
    private static readonly Brush glow=MakeGlow();
    private static Brush MakeGlow()
    {var brush=new RadialGradientBrush(Color.FromArgb(170,255,213,100),Colors.Transparent);brush.Freeze();return brush;}
    internal static void DrawLight(DrawingContext dc,Rect bounds,double time,double phase=0)
    {
        var r=Fit(bounds);dc.PushOpacity(Candle(time,phase));
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.4,r.Y+r.Height*.45),r.Width*.075,r.Height*.1);
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.55,r.Y+r.Height*.52),r.Width*.08,r.Height*.1);
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.45,r.Y+r.Height*.68),r.Width*.13,r.Height*.07);dc.Pop();
    }
}
