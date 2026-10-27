using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

public static class NexaSeason
{
    public static bool Active(NexaOptions options, DateTimeOffset now)
    {
        if (options.SeasonalMode == "On") return true;
        if (options.SeasonalMode == "Off") return false;
        var date = NexaTime.Local(now, options.TimeZoneId);
        return date.Month == 10 || date.Month == 11 && date.Day <= 2;
    }
}
public partial class MainWindow
{
    private NexaweenDecorations? seasonalArtwork;
    private Button? workspaceAdd;
    private void ApplySeasonalOptions(DateTimeOffset? at = null)
    {
        var active = NexaSeason.Active(Options, at ?? DateTimeOffset.UtcNow);
        Theme.SetSeasonalEnabled(active);
        NexaEcosystem.UpdateSeason(store, Options);
        seasonalArtwork?.ConfigureAnimation(Options,new Rect(WidgetScroller.HorizontalOffset,WidgetScroller.VerticalOffset,Math.Max(1,WidgetScroller.ViewportWidth),Math.Max(1,WidgetScroller.ViewportHeight)));
        if (seasonalArtwork != null) seasonalArtwork.Visibility = active && Options.SeasonalDecorations && Options.DecorationDensity != "Off" ? Visibility.Visible : Visibility.Collapsed;
    }
    internal void RefreshSeasonalWorkspace()
    {
        if (!widgetsReady) return;
        var bottom = Math.Max(PixelObstacle?.Bottom ?? 0, widgetFrames.Values.Select(f => Canvas.GetTop(f) + f.Height).DefaultIfEmpty(0).Max());
        if (workspaceAdd == null)
        {
            var label = new StackPanel(); label.Children.Add(new TextBlock { Text = "Add Widget", TextAlignment = TextAlignment.Center }); label.Children.Add(new TextBlock { Text = "+", FontSize = 40, TextAlignment = TextAlignment.Center });
            workspaceAdd = new Button { Content = label, Opacity = .4, BorderThickness = new Thickness(0), Background = Brushes.Transparent, Padding = new Thickness(12, 7, 12, 7), ToolTip = "Place a widget in the empty workspace" };
            System.Windows.Automation.AutomationProperties.SetName(workspaceAdd, "Add Widget in empty workspace");
            workspaceAdd.MouseEnter += (_, _) => workspaceAdd.Opacity = 1;
            workspaceAdd.MouseLeave += (_, _) => workspaceAdd.Opacity = .55;
            workspaceAdd.Click += (_, e) => { addWidgetLocation = new Point(16, Canvas.GetTop(workspaceAdd)); AddWidgetClick(workspaceAdd, e); };
        }
        if (!WidgetCanvas.Children.Contains(workspaceAdd)) WidgetCanvas.Children.Add(workspaceAdd);
        Canvas.SetTop(workspaceAdd, bottom + 30); Canvas.SetLeft(workspaceAdd, Math.Max(16, (WidgetCanvas.ActualWidth - Math.Max(130, workspaceAdd.ActualWidth)) / 2));
        if (seasonalArtwork == null) seasonalArtwork = new NexaweenDecorations { IsHitTestVisible = false };
        if (!WidgetCanvas.Children.Contains(seasonalArtwork)) { Panel.SetZIndex(seasonalArtwork, -1); WidgetCanvas.Children.Add(seasonalArtwork); }
        var widgetTargets=widgetFrames.Values.Select(f=>f.TransformToVisual(WidgetCanvas).TransformBounds(new Rect(0,0,f.Width,f.Height))).ToArray();
        var occupied=widgetTargets.ToList();
        if (PixelObstacle is { } pixels) occupied.Add(pixels);
        occupied.Add(new Rect(0, bottom + 20, 190, 60));
        seasonalArtwork.ConfigureAnimation(Options,new Rect(WidgetScroller.HorizontalOffset,WidgetScroller.VerticalOffset,Math.Max(1,WidgetScroller.ViewportWidth),Math.Max(1,WidgetScroller.ViewportHeight)));
        seasonalArtwork.Update(Math.Max(1, WidgetCanvas.ActualWidth), Math.Max(1, WidgetCanvas.Height), occupied, Options.DecorationDensity,widgetTargets:widgetTargets);
        seasonalArtwork.Visibility = Theme.SeasonalEnabled && Options.SeasonalDecorations && Options.DecorationDensity != "Off" ? Visibility.Visible : Visibility.Collapsed;
    }
}
internal sealed partial class SettingsDialog
{
    private UIElement BuildSeasonalEvents()
    {
        var page = Page("Nexaween", "A space for whatever haunts you next. Control the seasonal palette, candy, costumes and effects.");

        var decorations = new CheckBox { Content = "Show seasonal decorations", IsChecked = ReliabilityOptions.SeasonalDecorations, Margin = new Thickness(0, 16, 0, 12) }; page.Children.Add(decorations);
        var density = new ComboBox { ItemsSource = new[] { "Off", "Low", "Medium", "High" }, SelectedItem = ReliabilityOptions.DecorationDensity }; Field(page, "Decoration density", density);
        var animation = new CheckBox { Content = "Seasonal animation", IsChecked = ReliabilityOptions.SeasonalAnimation, Margin = new Thickness(0,12,0,8) };
        var surprises = new CheckBox { Content = "Random spooky surprises", IsChecked = ReliabilityOptions.SpookySurprises, Margin = new Thickness(0,0,0,8) };
        var sounds = new CheckBox { Content = "Seasonal sounds", IsChecked = ReliabilityOptions.SeasonalSounds, Margin = new Thickness(0,0,0,8) };
        var costumes = new CheckBox { Content = "Halloween bug costumes", IsChecked = ReliabilityOptions.HalloweenBugCostumes, Margin = new Thickness(0,0,0,8) };
        foreach(var toggle in new[]{animation,surprises,sounds,costumes})page.Children.Add(toggle);
        void Update()
        {

            ReliabilityOptions.SeasonalDecorations = decorations.IsChecked == true;
            ReliabilityOptions.DecorationDensity = density.SelectedItem?.ToString() ?? "Medium";
            ReliabilityOptions.SeasonalAnimation = animation.IsChecked == true; ReliabilityOptions.SpookySurprises = surprises.IsChecked == true;
            ReliabilityOptions.SeasonalSounds = sounds.IsChecked == true; ReliabilityOptions.HalloweenBugCostumes = costumes.IsChecked == true;
            NexaEcosystem.UpdateSeason(store, ReliabilityOptions);
            Theme.SetSeasonalEnabled(NexaSeason.Active(ReliabilityOptions, DateTimeOffset.UtcNow));
            space?.ApplyReliabilityOptions(); space?.RefreshSeasonalWorkspace();
            QueueSave("seasonal", SaveState);
        }
        foreach(var toggle in new[]{animation,surprises,sounds,costumes})toggle.Click+=(_,_)=>Update();
        density.SelectionChanged += (_, _) => Update(); decorations.Click += (_, _) => Update();
        page.Children.Add(new TextBlock { Text = "Decorations appear in open workspace areas and keep your notes and controls clear.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0) });
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
internal sealed partial class NexaweenDecorations : FrameworkElement
{
    private DrawingGroup drawing = new();
    private int signature;
    internal void Update(double width, double height, List<Rect> occupied, string density, IReadOnlyList<Rect>? widgetTargets = null)
    {
        Width = width; Height = height;
        var hash = new HashCode(); hash.Add(width); hash.Add(height); hash.Add(density);
        foreach (var rect in occupied) hash.Add(rect);
        hash.Add(widgetTargets?.Count??-1);if(widgetTargets!=null)foreach(var target in widgetTargets)hash.Add(target);
        var next = hash.ToHashCode(); if (next == signature) return; signature = next;
        var collisionAreas=occupied.ToList();
        var result = new DrawingGroup(); actors.Clear();vineCorners.Clear();
        var budget = density switch { "Off" => 0, "Low" => 8, "High" => 32, _ => 18 };
        var random = new Random(102624);
        using (var dc = result.Open())
        {
            var reserve=density=="High"?8:density=="Low"?2:4;
            if(budget>0 && widgetTargets!=null)AddContextual(dc,widgetTargets,collisionAreas,Math.Min(reserve,budget));
            var count = 0;var candyBudget=budget-actors.Count;
            for (var attempt = 0; attempt < 300 && count < candyBudget; attempt++)
            {
                var size = 38 + random.Next(25);
                var x = random.NextDouble() * Math.Max(0, width - size);
                var y = random.NextDouble() * Math.Max(0, height - size);
                var bounds = new Rect(x, y, size, size); var safe = bounds; safe.Inflate(28, 28);
                if (collisionAreas.Any(r => r.IntersectsWith(safe))) continue;
                dc.PushTransform(new TranslateTransform(x, y)); dc.PushTransform(new ScaleTransform(size / 64d, size / 64d));
                var kind=CandyKinds[count%CandyKinds.Length];var asset=Asset(kind);dc.DrawDrawing(asset);actors.Add(new(bounds,kind,asset,count)); dc.Pop(); dc.Pop(); collisionAreas.Add(bounds); count++;
            }
        }
        result.Freeze(); drawing = result; ReconcileAnimation(); InvalidateVisual();
    }
    private static readonly Dictionary<string, SolidColorBrush> brushes = [];
    private static SolidColorBrush B(string hex)
    { if (!brushes.TryGetValue(hex, out var b)) { b = new((Color)ColorConverter.ConvertFromString(hex)); b.Freeze(); brushes[hex] = b; } return b; }
    private static Pen P(string hex, double width = 2) { var pen = new Pen(B(hex), width); pen.Freeze(); return pen; }
    private static void Shape(DrawingContext dc, string path, string color, string? stroke = null) =>
        dc.DrawGeometry(B(color), stroke == null ? null : P(stroke), Geometry.Parse(path));
    private static void Draw(DrawingContext dc, int kind)
    {
        if(kind>=15){DrawExtraCandy(dc,kind);return;}
        switch (kind)
        {
            case 0: case 1: // Pumpkin / jack-o-lantern
                dc.DrawEllipse(B("#C45115"), null, new(32,36), 27,23);
                dc.DrawEllipse(B("#F97316"), P("#9A3412"), new(23,36), 16,22);
                dc.DrawEllipse(B("#FB923C"), P("#C45115"), new(38,36), 15,22);
                Shape(dc,"M29,17 L30,5 Q40,1 39,8 L35,18 Z","#567D36");
                if (kind == 1) { Shape(dc,"M15,34 L22,24 L28,34 Z M36,34 L42,24 L49,34 Z","#D9F99D"); Shape(dc,"M16,43 L24,47 L30,43 L37,48 L48,42 Q35,60 16,43 Z","#84CC16"); }
                break;
            case 2: // Ghost
                Shape(dc,"M9,58 L10,25 Q13,3 32,5 Q53,4 55,26 L56,58 L47,51 L39,59 L31,52 L21,59 L16,52 Z","#D8D1F0");
                dc.DrawEllipse(B("#2C183F"), null, new(24,28), 4,6); dc.DrawEllipse(B("#2C183F"), null, new(42,28), 4,6);
                dc.DrawEllipse(B("#A855F7"), null, new(33,42), 4,5); break;
            case 3: // Witch on broom
                dc.DrawLine(P("#A87939",4),new(3,50),new(61,38));
                Shape(dc,"M3,46 L17,45 L13,60 Z","#D5A451");
                Shape(dc,"M18,44 Q38,20 43,48 Z","#A855F7");
                dc.DrawEllipse(B("#D9F99D"),null,new(36,24),8,9);
                Shape(dc,"M19,19 L32,2 L43,18 L54,22 L14,24 Z","#24152F","#A855F7");
                dc.DrawLine(P("#F97316",3),new(29,14),new(42,17)); break;
            case 4: // Iron cauldron
                dc.DrawEllipse(null,P("#647080",4),new(9,35),7,9); dc.DrawEllipse(null,P("#647080",4),new(55,35),7,9);
                Shape(dc,"M10,25 Q6,60 31,60 Q59,61 54,25 Z","#242832","#555966");
                dc.DrawEllipse(B("#3F4550"),P("#6F7480",3),new(32,26),24,8);
                dc.DrawEllipse(B("#365314"),null,new(32,25),21,6);
                dc.DrawEllipse(B("#84CC16"),null,new(32,23),20,4);
                Shape(dc,"M14,24 Q17,46 22,35 L24,25 Z","#84CC16");
                dc.DrawEllipse(B("#D9F99D"),null,new(28,17),4,4); dc.DrawEllipse(B("#84CC16"),null,new(43,11),3,3);
                dc.DrawLine(P("#4D535F",4),new(18,53),new(13,63)); dc.DrawLine(P("#4D535F",4),new(45,53),new(50,63)); break;
            case 5: // Candy corn
                Shape(dc,"M8,55 L28,7 Q32,0 36,7 L57,55 Q33,65 8,55 Z","#FACC15");
                Shape(dc,"M16,37 L26,12 L39,12 L49,37 Z","#F97316");
                Shape(dc,"M25,16 L29,6 Q32,1 35,6 L40,16 Z","#FFF7ED"); break;
            case 6: // Wrapped candy
                Shape(dc,"M5,16 L23,27 L21,40 L3,48 L8,32 Z M59,16 L43,27 L44,40 L61,48 L56,32 Z","#A855F7");
                dc.DrawRoundedRectangle(B("#F97316"),P("#D9F99D"),new Rect(18,20,29,25),9,9);
                dc.DrawLine(P("#FACC15",4),new(24,21),new(35,44)); break;
            case 7: // Lollipop
                dc.DrawLine(P("#E9D5FF",4),new(36,33),new(43,62));
                dc.DrawEllipse(B("#F97316"),P("#FACC15",3),new(29,24),20,20);
                dc.DrawEllipse(null,P("#E9D5FF",4),new(29,24),12,12); dc.DrawEllipse(B("#A855F7"),null,new(29,24),5,5); break;
            case 8: // Chocolate
                dc.PushTransform(new RotateTransform(-14,32,32));
                dc.DrawRoundedRectangle(B("#54321F"),P("#9B6343"),new Rect(10,5,43,52),5,5);
                for(var i=0;i<2;i++) for(var j=0;j<3;j++) dc.DrawRoundedRectangle(B("#79482D"),P("#3D2419"),new Rect(14+i*18,10+j*14,15,11),2,2);
                Shape(dc,"M8,42 L54,38 L59,60 L9,61 Z","#A855F7"); dc.Pop(); break;
            case 9: // Gummy candy
                dc.DrawEllipse(B("#84CC16"),null,new(21,16),8,8); dc.DrawEllipse(B("#84CC16"),null,new(43,16),8,8);
                dc.DrawEllipse(B("#65A30D"),null,new(32,24),18,15); dc.DrawEllipse(B("#84CC16"),null,new(32,43),18,17);
                dc.DrawEllipse(B("#365314"),null,new(26,24),2,3); dc.DrawEllipse(B("#365314"),null,new(39,24),2,3); break;
            case 10: // Autumn leaf
                Shape(dc,"M30,58 L11,43 L16,35 L4,23 L23,26 L23,5 L35,20 L49,6 L46,29 L61,31 L45,45 Z","#F97316");
                dc.DrawLine(P("#9A3412"),new(31,61),new(33,22)); break;
            case 11: // Tiny skull
                dc.DrawEllipse(B("#E9D5FF"),null,new(32,27),24,23);
                dc.DrawRoundedRectangle(B("#E9D5FF"),null,new Rect(19,34,26,23),4,4);
                dc.DrawEllipse(B("#24152F"),null,new(23,27),6,7); dc.DrawEllipse(B("#24152F"),null,new(42,27),6,7);
                Shape(dc,"M28,41 L33,34 L37,41 Z","#24152F");
                for(var i=0;i<3;i++) dc.DrawLine(P("#6B4C7E"),new(26+i*6,48),new(26+i*6,57)); break;
            case 13: // Candy bucket
                dc.DrawEllipse(null, P("#A855F7", 4), new(32, 24), 20, 20);
                Shape(dc, "M10,25 L54,25 L49,60 L15,60 Z", "#F97316", "#9A3412");
                dc.DrawEllipse(B("#24152F"), null, new(32,25), 22,6);
                for(var i=0;i<5;i++) {dc.PushTransform(new RotateTransform(i*21-40,32,20));dc.DrawRoundedRectangle(B(i%2==0?"#84CC16":"#A855F7"),P("#D9F99D",1),new Rect(20+i*3,10,8,16),2,2);dc.Pop();}
                Shape(dc, "M20,39 L25,34 L28,41 Z M37,41 L41,34 L46,39 Z M21,49 Q33,59 45,48 Z", "#24152F"); break;
            case 14: // Standalone green slime
                Shape(dc,"M4,52 Q9,41 18,48 Q22,31 30,44 Q37,37 41,48 Q56,39 61,53 Q61,63 32,61 Q4,63 4,52 Z","#65A30D");
                Shape(dc,"M10,50 Q19,46 24,51 Q31,42 36,50 Q46,46 54,53 Q29,58 10,50 Z","#84CC16");
                dc.DrawEllipse(B("#D9F99D"),null,new(27,30),4,4);dc.DrawEllipse(B("#84CC16"),null,new(42,22),3,3); break;
            default: // Web
                for(var i=0;i<7;i++) {var angle=i*Math.PI/3;dc.DrawLine(P("#CBB5DD",1),new(32,32),new(32+Math.Cos(angle)*30,32+Math.Sin(angle)*30));}
                for(var r=9;r<=27;r+=9) dc.DrawEllipse(null,P("#A78ABF",1),new(32,32),r,r); break;
        }
    }
}
