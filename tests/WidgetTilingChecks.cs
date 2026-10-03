using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckWidgetTiling()
    {
        var store=new StateStore(Path.Combine(output,"tiling105.json"));
        var data=new AppState { WidgetLayoutInitialized=true, Widgets=[WidgetLibrary.Create("Notes"),WidgetLibrary.Create("Clock"),WidgetLibrary.Create("Tasks")] };
        data.Widgets[0].Text="Keep these notes"; WidgetLibrary.Arrange(data.Widgets,false); store.Save(data);
        var window=new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Border Frame(WidgetInstance w)=>window.WidgetCanvas.Children.OfType<Border>().Single(b=>Equals(b.Tag,w.Id));
        Rect Rectangle(WidgetInstance w) { var f=Frame(w); return new Rect(Canvas.GetLeft(f),Canvas.GetTop(f),f.Width,f.Height); }
        void CheckLayout(string phase)
        {
            foreach(var w in window.state.Widgets) { var f=Frame(w); Assert(f.Width>0 && f.Height>=WidgetLibrary.Definition(w.Kind).MinHeight && Rectangle(w).Right<=window.WidgetCanvas.ActualWidth+.01,phase+": useful bounds for "+w.Kind); }
            var cards=window.state.Widgets.Select(Rectangle).ToArray(); for(var i=0;i<cards.Length;i++) for(var j=i+1;j<cards.Length;j++) Assert(!cards[i].IntersectsWith(cards[j]),phase+": cards do not overlap");
        }
        CheckLayout("Initial");
        var notes=window.state.Widgets[0]; var clock=window.state.Widgets[1]; var clockRect=Rectangle(clock);
        var move=Descendants(Frame(notes)).OfType<Thumb>().Single();
        move.RaiseEvent(new DragStartedEventArgs(0,0)); move.RaiseEvent(new DragDeltaEventArgs(140,96));
        Assert(Rectangle(clock)==clockRect,"Neighbors stay stationary during drag preview");
        move.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(80));
        Assert(!Rectangle(clock).IntersectsWith(Rectangle(notes)),"Drop reflows neighbors without overlap"); CheckLayout("Moved");
        var saved=Rectangle(notes); move.RaiseEvent(new DragStartedEventArgs(0,0)); move.RaiseEvent(new DragDeltaEventArgs(300,300)); move.RaiseEvent(new DragCompletedEventArgs(0,0,true));
        Assert(Rectangle(notes)==saved,"Canceled gesture does not change layout");
        var resize=window.WidgetCanvas.Children.OfType<Thumb>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Resize "+notes.Title);
        resize.RaiseEvent(new DragStartedEventArgs(0,0)); resize.RaiseEvent(new DragDeltaEventArgs(60,48)); resize.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(80));
        Assert(notes.CustomSize && notes.Height%24==0,"Resize saves grid dimensions"); CheckLayout("Resized");
        var payload=JsonSerializer.Serialize(window.state.Widgets); window.Close();
        var reopened=new MainWindow(store); reopened.Show(); Pump(TimeSpan.FromMilliseconds(120));
        Assert(JsonSerializer.Serialize(reopened.state.Widgets)==payload && reopened.state.Widgets[0].Text=="Keep these notes","Reopen preserves layout and data"); reopened.Close();
    }
}
