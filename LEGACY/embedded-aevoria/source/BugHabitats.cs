using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal static class BugHabitats
{
    internal static readonly Dictionary<string,BugSpecies> KindSpecies=new()
    {
        ["Ant Bed"]=BugSpecies.Ants, ["Centipede Den"]=BugSpecies.Centipedes,
        ["Ladybug Shelter"]=BugSpecies.Ladybugs, ["Fly Feeding Ground"]=BugSpecies.Flies,
        ["Roach Hideout"]=BugSpecies.Roaches, ["Spider Retreat"]=BugSpecies.Spiders
    };
    internal static readonly BugSpecies[] Species=KindSpecies.Values.ToArray();
    internal static bool Includes(BugPreferences p,BugSpecies species)=>p.Enabled && p.Density==BugDensity.Arcade && (p.Species is BugSpecies.AllBugs or BugSpecies.AllThree || p.Species==species);
}
internal sealed class BugHabitatArtwork(BugSpecies species) : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        var scale=Math.Min(ActualWidth/240,ActualHeight/140);
        dc.PushTransform(new TranslateTransform(ActualWidth/2,ActualHeight/2));
        dc.PushTransform(new ScaleTransform(scale,scale));
        var ground=new SolidColorBrush(Color.FromRgb(47,40,30));
        dc.DrawEllipse(ground,null,new Point(0,32),108,29);
        switch(species)
        {
            case BugSpecies.Ants:
                dc.DrawEllipse(Brushes.SaddleBrown,new Pen(Brushes.Peru,2),new Point(0,6),88,42);
                for(var i=0;i<23;i++)dc.DrawEllipse(Brushes.Peru,null,new Point((i*37)%151-75,(i*19)%45-10),2,1.4);
                dc.DrawEllipse(Brushes.Black,new Pen(Brushes.Sienna,5),new Point(0,-5),23,13);
                break;
            case BugSpecies.Ladybugs:
                for(var i=0;i<7;i++)
                {
                    dc.PushTransform(new RotateTransform(-45+i*15));
                    dc.DrawEllipse(i%2==0 ? Brushes.DarkOliveGreen : Brushes.OliveDrab,new Pen(Brushes.Olive,1),new Point(0,-18),24,55);
                    dc.DrawLine(new Pen(Brushes.YellowGreen,1),new Point(0,-60),new Point(0,25));dc.Pop();
                }
                dc.DrawEllipse(Brushes.Black,null,new Point(0,22),27,11);break;
            case BugSpecies.Flies:
                dc.DrawRoundedRectangle(Brushes.Peru,new Pen(Brushes.BurlyWood,3),new Rect(-80,-25,88,52),12,12);
                for(var i=0;i<5;i++)dc.DrawEllipse(Brushes.SaddleBrown,null,new Point(-65+i*14,-6+i%2*13),3,3);
                dc.DrawEllipse(Brushes.DarkOliveGreen,new Pen(Brushes.Olive,3),new Point(45,3),35,26);
                dc.DrawLine(new Pen(Brushes.Khaki,7),new Point(15,-28),new Point(73,-41));break;
            case BugSpecies.Roaches:
                dc.DrawRoundedRectangle(Brushes.SaddleBrown,new Pen(Brushes.Peru,2),new Rect(-94,-38,188,72),13,13);
                dc.DrawEllipse(Brushes.Black,new Pen(Brushes.Sienna,5),new Point(0,12),63,19);
                for(var i=0;i<5;i++)dc.DrawLine(new Pen(Brushes.Sienna,2),new Point(-80+i*38,-30),new Point(-72+i*38,-4));break;
            case BugSpecies.Centipedes:
                dc.DrawEllipse(Brushes.Black,null,new Point(0,13),67,25);
                for(var i=0;i<7;i++) { var a=Math.PI+i*Math.PI/6;dc.DrawEllipse(i%2==0 ? Brushes.SlateGray : Brushes.DimGray,new Pen(Brushes.LightSlateGray,1.5),new Point(Math.Cos(a)*72,Math.Sin(a)*39+12),25,21); }
                break;
            default:
                dc.DrawEllipse(Brushes.DarkSlateGray,new Pen(Brushes.SlateGray,3),new Point(0,0),86,48);
                dc.DrawEllipse(Brushes.Black,null,new Point(0,6),54,30);
                // The shelter itself contains no web: all strands are built by live spiders.
                break;
        }
        dc.Pop();dc.Pop();
    }
}
internal sealed partial class BugManager
{
    internal bool HasHabitatWidgets;
    internal void SetHabitatHomes(IEnumerable<KeyValuePair<BugSpecies,Point>> homes)
    {
        Homes.Clear();foreach(var pair in homes)Homes[pair.Key]=pair.Value;
        HasHabitatWidgets=Homes.Count>0;
    }
}
public partial class MainWindow
{
    private BugPreferences? currentBugPreferences;
    private bool HabitatVisible(string kind)=>!BugHabitats.KindSpecies.TryGetValue(kind,out var species) || BugHabitats.Includes(currentBugPreferences??store.LoadBugPreferences(),species);
    private void SyncHabitatWidgets()
    {
        if(widgetsReady)RenderWidgets();
    }
    private void SyncHabitatPositions()
    {
        if(bugsOverlay is not {} overlay)return;
        var homes=new Dictionary<BugSpecies,Point>();
        foreach(var widget in state.Widgets)
        {
            if(!BugHabitats.KindSpecies.TryGetValue(widget.Kind,out var species) || !widgetFrames.TryGetValue(widget.Id,out var frame) || homes.ContainsKey(species))continue;
            var art=FindHabitatArt(frame);
            if(art!=null)homes[species]=art.TransformToVisual(WidgetCanvas).Transform(new Point(art.ActualWidth/2,art.ActualHeight/2));
        }
        overlay.Manager.SetHabitatHomes(homes);
    }
    private static BugHabitatArtwork? FindHabitatArt(DependencyObject parent)
    {
        if(parent is BugHabitatArtwork art)return art;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
            if(FindHabitatArt(VisualTreeHelper.GetChild(parent,i)) is {} found)return found;
        return null;
    }
}
