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
        if (now < until) return ledger.EffectiveMultiplier(now);
        return Math.Max(0, Math.Clamp(ledger.FrenzyMultiplier, 1.5, 10) - .5 * (1 + Math.Floor((now-until).TotalSeconds/.35)));
    }
    internal long? SpaceScore {get;set;}
    internal ProductivityLedger? Ledger {get;set;}
    internal NexaOptions Options {get;set;} = new();
    internal ProductivityStreakOverlay() { IsHitTestVisible=false; }
    private bool displayed;
    internal void Refresh()
    {
        var active=Ledger is {} ledger && DisplayMultiplier(ledger,DateTimeOffset.UtcNow)>0;
        if(active || displayed)InvalidateVisual();displayed=active;
    }
    internal static Rect LayoutFrame(Size size,Rect? workArea=null,double inset=18)
    {
        var safe=workArea??new Rect(size);safe.Intersect(new Rect(size));if(safe.IsEmpty)safe=new Rect(size);
        var width=Math.Max(1,Math.Min(340,safe.Width-inset*2));return new Rect(safe.Left+(safe.Width-width)/2,Math.Max(safe.Top+52,safe.Bottom-64-inset),width,64);
    }
    protected override void OnRender(DrawingContext dc)
    {
        if (Ledger is not {} ledger || !IsVisible || ActualWidth<80 || ActualHeight<140) return;
        var now=DateTimeOffset.UtcNow;var multiplier=DisplayMultiplier(ledger,now);if(multiplier<=0)return;
        var frame=LayoutFrame(RenderSize,NexaWindowBounds.TryNotificationArea(this,out var work)?work:null,NexaWindowBounds.NotificationInset(this));var width=frame.Width;var height=frame.Height;var x=frame.X;var y=frame.Y;var active=ledger.FrenzyUntil>now;
        var fadeIn=ledger.FrenzyStartedAt is {} began?Math.Clamp((now-began).TotalSeconds/.18,0,1):1;
        var fadeOut=!active && ledger.FrenzyUntil is {} until?Math.Clamp(1-(now-until).TotalSeconds/(Math.Max(1,ledger.FrenzyMultiplier)*.7),0,1):1;
        dc.PushOpacity(NexaMotion.Allowed(Options)?Math.Min(fadeIn,fadeOut):1);
        dc.DrawRoundedRectangle(BugVisuals.B(Theme.SeasonalEnabled?"#F21C102A":"#F2171B26"),BugVisuals.P(Theme.SeasonalEnabled?"#A855F7":"#7C6CF0",1.5),frame,16,16);
        void Text(string value,double size,double left,double available,string color)
        {
            var text=new FormattedText(value,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),size,BugVisuals.B(color),VisualTreeHelper.GetDpi(this).PixelsPerDip){MaxTextWidth=Math.Max(1,available),Trimming=TextTrimming.CharacterEllipsis};
            dc.DrawText(text,new(x+left,y+(height-text.Height)/2));
        }
        dc.PushTransform(new TranslateTransform(x+16,y+18));dc.DrawGeometry(ProductivityRing.LightningBrush,null,Geometry.Parse("M12,0 L2,14 L10,14 L7,27 L23,10 L14,10 L18,0 Z"));dc.Pop();
        Text((SpaceScore??ledger.Score).ToString("N0"),26,50,width*.46-42,"#FACC15");
        Text("Streak "+multiplier.ToString("0.0")+"×",17,width*.50,width*.50-14,Theme.SeasonalEnabled?"#84CC16":"#C4B5FD");
        dc.Pop();
    }
    public void Dispose() { Ledger=null; }
}
