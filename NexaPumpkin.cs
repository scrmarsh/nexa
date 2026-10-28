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
    internal static double Candle(double time,double phase)=>.24+.08*Math.Sin(time*2.13+phase)+.045*Math.Sin(time*5.37+phase*1.7)+.025*Math.Sin(time*9.71+phase*.8);
    private static readonly Brush glow=MakeGlow();
    private static Brush MakeGlow()
    {var brush=new RadialGradientBrush(Color.FromArgb(230,255,235,155),Colors.Transparent);brush.Freeze();return brush;}
    internal static void DrawLight(DrawingContext dc,Rect bounds,double time,double phase=0)
    {
        var r=Fit(bounds);dc.PushOpacity(Candle(time,phase));
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.39,r.Y+r.Height*.44),r.Width*.048,r.Height*.085);
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.55,r.Y+r.Height*.52),r.Width*.055,r.Height*.085);
        dc.DrawEllipse(glow,null,new(r.X+r.Width*.44,r.Y+r.Height*.66),r.Width*.11,r.Height*.065);dc.Pop();
    }
}
