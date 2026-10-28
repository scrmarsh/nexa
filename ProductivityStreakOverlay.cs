using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SessionDashboard;
namespace SessionDashboard;
// One viewport layer, driven by saved scoring timestamps. Visual decay never edits the ledger.
internal sealed class ProductivityStreakOverlay : FrameworkElement, IDisposable
{
    internal static double DisplayMultiplier(ProductivityLedger ledger, DateTimeOffset now)
    {
        if (ledger.FrenzyUntil is not {} until || ledger.FrenzyStartedAt>now) return 0;
        if (now < until) return ledger.Multiplier(now);
        return Math.Max(0, Math.Clamp(ledger.FrenzyMultiplier, 1.5, 10) - .5 * (1 + Math.Floor((now-until).TotalSeconds/.35)));
    }
    internal ProductivityLedger? Ledger {get;set;}
    internal NexaOptions Options {get;set;} = new();
    internal ProductivityStreakOverlay() { IsHitTestVisible=false; }
    private bool displayed;
    internal void Refresh()
    {
        var active=Ledger is {} ledger && DisplayMultiplier(ledger,DateTimeOffset.UtcNow)>0;
        if(active || displayed)InvalidateVisual();displayed=active;
    }
    internal static Rect LayoutFrame(Size size,Rect? workArea=null)
    {
        var safe=workArea??new Rect(size);safe.Intersect(new Rect(size));if(safe.IsEmpty)safe=new Rect(size);
        var width=Math.Max(1,Math.Min(320,safe.Width-32));return new Rect(safe.Left+(safe.Width-width)/2,Math.Max(safe.Top+52,safe.Bottom-156-18),width,156);
    }
    protected override void OnRender(DrawingContext dc)
    {
        if (Ledger is not {} ledger || !IsVisible || ActualWidth<80 || ActualHeight<140) return;
        var now=DateTimeOffset.UtcNow;var multiplier=DisplayMultiplier(ledger,now);if(multiplier<=0)return;
        var frame=LayoutFrame(RenderSize,NexaWindowBounds.TryWorkArea(this,out var work)?work:null);var width=frame.Width;var height=frame.Height;var x=frame.X;var y=frame.Y;var active=ledger.FrenzyUntil>now;
        if(!active)dc.PushOpacity(Math.Clamp(multiplier/Math.Max(1,ledger.FrenzyMultiplier),.08,1));
        dc.DrawRoundedRectangle(BugVisuals.B(Theme.SeasonalEnabled?"#F21C102A":"#F2171B26"),BugVisuals.P(Theme.SeasonalEnabled?"#A855F7":"#7C6CF0",1.5),frame,16,16);
        void Text(string value,double size,double top,string color)
        {
            var text=new FormattedText(value,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),size,BugVisuals.B(color),VisualTreeHelper.GetDpi(this).PixelsPerDip){MaxTextWidth=width-24,Trimming=TextTrimming.CharacterEllipsis};
            dc.DrawText(text,new(x+(width-text.WidthIncludingTrailingWhitespace)/2,y+top));
        }
        Text("PRODUCTIVITY",12,12,"#E9D5FF");Text(ledger.Score.ToString("N0"),32,31,"#FACC15");
        dc.PushTransform(new TranslateTransform(x+width/2-66,y+38));dc.PushTransform(new ScaleTransform(.75,.75));dc.DrawGeometry(BugVisuals.B("#FACC15"),null,Geometry.Parse("M12,0 L2,14 L10,14 L7,27 L23,10 L14,10 L18,0 Z"));dc.Pop();dc.Pop();Text(multiplier.ToString("0.0")+"×"+(active?" streak":" cooling down"),19,76,Theme.SeasonalEnabled?"#84CC16":"#C4B5FD");
        Text(string.Join(" · ",ledger.Events.TakeLast(2).Select(e=>$"{e.BasePoints+e.BonusPoints:+0;-0;0}")),12,112,"#E9D5FF");
        if(NexaMotion.Allowed(Options) && Options.CelebrationEffects)
        {
            var time=now.ToUnixTimeMilliseconds()/1000d;
            for(var i=0;i<10;i++)
            {
                var px=x+18+i*(width-36)/9;var bottom=y+height-4;var h=8+3*Math.Sin(time*3+i);
                var g=new StreamGeometry();using(var c=g.Open()){c.BeginFigure(new(px-3,bottom),true,true);c.BezierTo(new(px-8,bottom-5),new(px+3,bottom-h-5),new(px,bottom-h),true,false);c.BezierTo(new(px+7,bottom-4),new(px+5,bottom),new(px-3,bottom),true,false);}g.Freeze();
                dc.DrawGeometry(BugVisuals.B(Theme.SeasonalEnabled?"#84CC16":"#F97316"),null,g);
            }
        }
        if(!active)dc.Pop();
    }
    public void Dispose() { Ledger=null; }
}
