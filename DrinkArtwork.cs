using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

// Shared artwork with no branding or continuously running animations.
internal sealed class DrinkArtwork : FrameworkElement
{
    internal bool Beer { get; }
    internal string Vessel { get; set; } = "Beer Bottle";
    internal static readonly Geometry GlassShape = Geometry.Parse("M8,7 L68,7 L61,94 L15,94 Z");
    internal static readonly Geometry WaterBottleShape = Geometry.Parse("M29,8 L47,8 L47,19 Q47,24 55,30 Q58,33 58,39 L58,44 L56,46 L58,49 L58,55 L56,57 L58,60 L58,66 L56,68 L58,71 L58,77 L56,79 L58,82 L58,91 Q58,97 51,97 L25,97 Q18,97 18,91 L18,82 L20,79 L18,77 L18,71 L20,68 L18,66 L18,60 L20,57 L18,55 L18,49 L20,46 L18,44 L18,39 Q18,33 21,30 Q29,24 29,19 Z");
    internal static readonly Geometry BottleShape = Geometry.Parse("M32,4 L44,4 L44,32 Q44,38 51,45 Q55,49 55,56 L55,92 Q55,98 50,98 L26,98 Q21,98 21,92 L21,56 Q21,49 25,45 Q32,38 32,32 Z");
    internal static readonly Geometry BottleLiquidShape = Geometry.Parse("M35,10 L41,10 L41,33 Q41,40 48,47 Q51,51 51,57 L51,91 Q51,94 48,94 L28,94 Q25,94 25,91 L25,57 Q25,51 28,47 Q35,40 35,33 Z");
    internal static readonly Geometry WineShape = Geometry.Parse("M18,8 L58,8 Q66,58 38,64 Q10,58 18,8 Z");
    internal static readonly Geometry ShotShape = Geometry.Parse("M15,41 L61,41 L55,94 L21,94 Z");
    internal static readonly Color BeerBottleBrown = Color.FromRgb(91,49,22);
    internal static readonly Color BeerLiquidYellow = Color.FromRgb(255,194,38);
    private static readonly Brush Water=Gradient("#75D4FF","#216BD0"), Ale=Gradient("#C084FC","#7E22CE"), Wine=Gradient("#BEF264","#65A30D"), Spirit=Gradient("#FDBA74","#EA580C");
    private static readonly Brush NormalAle=Gradient("#FFE082","#D99917"),NormalWine=Gradient("#C95072","#7D1838"),NormalSpirit=Gradient("#FFF3C4","#D6B778");
    private static readonly Brush Brown=Gradient("#7E4820","#31180B"), Clear=new SolidColorBrush(Color.FromArgb(24,190,228,245));
    private static readonly Pen ClearEdge=new(new SolidColorBrush(Color.FromRgb(156,201,223)),1.5), BrownEdge=new(new SolidColorBrush(Color.FromRgb(55,27,12)),2);
    private static readonly Pen Shine=new(new SolidColorBrush(Color.FromArgb(145,227,249,255)),1.4);
    private static Brush Gradient(string a,string b)
    { var brush=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(a),(Color)ColorConverter.ConvertFromString(b),90); brush.Freeze(); return brush; }
    public static readonly DependencyProperty FillFractionProperty=DependencyProperty.Register(nameof(FillFraction),typeof(double),typeof(DrinkArtwork),new FrameworkPropertyMetadata(0d,FrameworkPropertyMetadataOptions.AffectsRender));
    public double FillFraction { get=>(double)GetValue(FillFractionProperty); set=>SetValue(FillFractionProperty,value); }
    internal static Brush WaterLiquid => Theme.SeasonalEnabled?Spirit:Water;
    internal DrinkArtwork(bool beer,bool animate=true) { Beer=beer; IsHitTestVisible=false; }
    internal void SetFill(double fraction)
    {
        var current=FillFraction; BeginAnimation(FillFractionProperty,null); FillFraction=fraction;
        BeginAnimation(FillFractionProperty,new DoubleAnimation(current,fraction,TimeSpan.FromMilliseconds(280)));
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var scale=Math.Min(ActualWidth/76,ActualHeight/102);
        dc.PushTransform(new TranslateTransform((ActualWidth-76*scale)/2,(ActualHeight-102*scale)/2));
        dc.PushTransform(new ScaleTransform(scale,scale));
        var bottle=Beer && Vessel=="Beer Bottle"; var wine=Beer && Vessel=="Wine Glass";
        var shape=!Beer ? WaterBottleShape : bottle ? BottleShape : wine ? WineShape : ShotShape;
        var liquid=bottle ? BottleLiquidShape : shape;
        dc.DrawGeometry(bottle ? Brown : Clear,null,shape);
        dc.PushClip(liquid);
        var bounds=liquid.Bounds; var top=bounds.Bottom-bounds.Height*Math.Clamp(FillFraction,0,1);
        dc.DrawRectangle(!Beer ? WaterLiquid : bottle ? (Theme.SeasonalEnabled?Ale:NormalAle) : wine ? (Theme.SeasonalEnabled?Wine:NormalWine) : (Theme.SeasonalEnabled?Spirit:NormalSpirit),null,new Rect(bounds.Left,top,bounds.Width,Math.Max(0,bounds.Bottom-top)));
        if(wine && FillFraction>.01)
        {foreach(var bubble in new[]{new Point(29,45),new Point(43,31),new Point(39,52),new Point(49,43)})if(bubble.Y>top)dc.DrawEllipse(null,Shine,bubble,2,2);}
        if(FillFraction>.01)dc.DrawLine(Shine,new Point(bounds.Left,top),new Point(bounds.Right,top));
        dc.DrawLine(Shine,new Point(bounds.Left+5,Math.Max(bounds.Top+5,35)),new Point(bounds.Left+5,bounds.Bottom-7));
        dc.Pop(); dc.DrawGeometry(null,bottle ? BrownEdge : ClearEdge,shape);
        if(!Beer)
        {
            for(var y=44;y<=88;y+=11)dc.DrawLine(Shine,new Point(21,y),new Point(55,y));
            dc.DrawRoundedRectangle(Theme.SeasonalEnabled?Spirit:new SolidColorBrush(Color.FromRgb(178,212,230)),ClearEdge,new Rect(28,3,20,7),1,1);
        }
        else if(bottle)dc.DrawRoundedRectangle(Brown,BrownEdge,new Rect(31,3,14,6),1,1);
        else if(wine)
        { dc.DrawLine(ClearEdge,new Point(38,64),new Point(38,93)); dc.DrawEllipse(Clear,ClearEdge,new Point(38,95),19,2); }
        else dc.DrawLine(new Pen(ClearEdge.Brush,3),new Point(22,94),new Point(54,94));
        dc.Pop(); dc.Pop();
    }
}
