using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckScrollWeb018()
    {
        var store=new StateStore(System.IO.Path.Combine(output,"scroll-web018.json"));
        var note=WidgetLibrary.Create("Notes");note.Height=1200;note.CustomSize=true;
        store.Save(new AppState { WidgetLayoutInitialized=true,Widgets=[note] });
        store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade));
        var w=new MainWindow(store) { Width=1100,Height=700,Left=-10000,Top=-10000,ShowActivated=false };w.Show();Pump(TimeSpan.FromMilliseconds(3400));
        var overlay=w.bugsOverlay!;var m=overlay.Manager;var id=note.Id.ToString();
        var r=m.Surfaces[id];var spider=new BugEntity {Species=BugSpecies.Spiders,Position=r.TopLeft-new Vector(15,0),State=BugState.Paused,Decision=1000};m.Bugs.Add(spider);m.BuildWeb(spider);
        var web=m.Webs.Last();web.Strands.Clear();web.Strands.Add(new(r.TopLeft-new Vector(30,30),r.TopLeft,id));web.Construction=1;
        var before=overlay.ContentOrigin;w.WidgetScroller.ScrollToVerticalOffset(180);w.UpdateLayout();Pump(TimeSpan.FromMilliseconds(60));
        Assert(w.WidgetScroller.VerticalOffset>0 && overlay.ContentOrigin.Y<before.Y && web.Strands.Count==1,"Live scroll moves content immediately and preserves web anchor");
        var frame=w.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,note.Id));
        var grip=Descendants(frame).OfType<Thumb>().First();
        grip.RaiseEvent(new DragStartedEventArgs(0,0) {RoutedEvent=Thumb.DragStartedEvent});
        grip.RaiseEvent(new DragDeltaEventArgs(35,20) {RoutedEvent=Thumb.DragDeltaEvent});
        Assert(web.Strands.Count==0,"Actual widget drag routing breaks its attached strands");
        grip.RaiseEvent(new DragCompletedEventArgs(35,20,false) {RoutedEvent=Thumb.DragCompletedEvent});
        var editor=Descendants(frame).OfType<TextBox>().First(t=>t.AcceptsReturn);editor.AppendText("Web interaction preserves editing.");
        Assert(editor.Text.Contains("preserves"),"Widget editing remains usable after scrolling and web break");
        foreach(var scale in new[]{1d,1.5,2d})
        {
            var bitmap=new RenderTargetBitmap((int)(w.ActualWidth*scale),(int)(w.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);bitmap.Render(w);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=System.IO.File.Create(System.IO.Path.Combine(output,$"dashboard-{scale*100:0}dpi.png"));encoder.Save(file);
            Assert(bitmap.PixelWidth==(int)(w.ActualWidth*scale),"Dashboard renders at "+scale*100+"% pixel density");
        }
        w.Close();
    }
}
