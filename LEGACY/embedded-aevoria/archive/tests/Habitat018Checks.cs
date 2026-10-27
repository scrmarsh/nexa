using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckHabitats018()
    {
        var m=new BugManager(118);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade));m.SetSurfaces([],new Size(1400,1000));
        for(var i=0;i<150;i++)m.Advance(.08);
        Assert(m.Bugs.Count==3 && m.Bugs.Any(b=>b.Species==BugSpecies.Centipedes),"Slow opening spawns include a centipede within five seconds");
        Assert(m.Cap==240 && m.PopulationLimit==240 && m.SpawnInterval==5,"Arcade cap is 240 with one spawn every five seconds");
        m.SpawnPressure=true;Assert(m.SpawnInterval==10,"Load slows spawning without reducing resolution");
        var ants=new BugManager(1);ants.Configure(new(true,BugSpecies.Ants,BugDensity.Arcade));ants.SetSurfaces([],new Size(1400,1000));
        for(var i=0;i<17000;i++)ants.Advance(.08);
        Assert(ants.Bugs.Count==240,"Arcade can fill to the requested 240-bug ceiling without exceeding it");
        var spiders=new BugManager(2);spiders.Configure(new(true,BugSpecies.Spiders,BugDensity.Arcade));spiders.SetSurfaces([],new Size(1400,1000));
        for(var i=0;i<20;i++)spiders.BuildWeb(new BugEntity {Species=BugSpecies.Spiders,Position=new Point(500,500)});
        Assert(spiders.Webs.Count==18 && spiders.Webs.All(w=>w.Strands.Count>40),"Eighteen organic webs can coexist with multiple strand rings");
        Assert(spiders.Webs.Any(w=>w.Strands.Max(s=>s.B.X)-w.Strands.Min(s=>s.B.X)>350),"Large webs span substantially more than the previous diameter");
        var store=new StateStore(System.IO.Path.Combine(output,"habitats018.json"));var note=WidgetLibrary.Create("Notes");
        store.Save(new AppState { WidgetLayoutInitialized=true,Widgets=[note] });store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade));
        var w=new MainWindow(store) {Width=1250,Height=900,Left=-10000,Top=-10000,ShowActivated=false};w.Show();Pump(TimeSpan.FromMilliseconds(3400));
        var homes=w.state.Widgets.Where(v=>BugHabitats.KindSpecies.ContainsKey(v.Kind)).ToArray();
        Assert(homes.Length==6 && homes.All(v=>v.Title==v.Kind),"All six species get labeled real habitat widgets automatically");
        var frames=w.WidgetCanvas.Children.OfType<Border>().Where(f=>f.Tag is Guid).ToArray();
        Rect Bounds(Border f)=>f.TransformToVisual(w.WidgetCanvas).TransformBounds(new Rect(f.RenderSize));
        Assert(frames.All(a=>frames.All(b=>a==b || !Bounds(a).IntersectsWith(Bounds(b)))),"Habitat widgets occupy blank slots without overlapping existing widgets");
        Assert(Descendants(w.WidgetCanvas).OfType<BugHabitatArtwork>().Count()==6 && homes.All(v=>v.Height>=216),"Every habitat has a large illustration and normal widget size");
        Save018(w,1250,900,"habitat-widgets-initial018");
        var antBed=homes.Single(v=>v.Kind=="Ant Bed");var frame=frames.Single(f=>Equals(f.Tag,antBed.Id));
        var previous=w.bugsOverlay!.Manager.Home(BugSpecies.Ants);
        var grip=Descendants(frame).OfType<Thumb>().First();
        grip.RaiseEvent(new DragStartedEventArgs(0,0) {RoutedEvent=Thumb.DragStartedEvent});
        grip.RaiseEvent(new DragDeltaEventArgs(0,1600) {RoutedEvent=Thumb.DragDeltaEvent});
        grip.RaiseEvent(new DragCompletedEventArgs(0,1600,false) {RoutedEvent=Thumb.DragCompletedEvent});Pump(TimeSpan.FromMilliseconds(150));
        Assert((w.bugsOverlay.Manager.Home(BugSpecies.Ants)-previous).Length>100,"Dragging the ant-bed widget moves the live scavenging destination");
        var movedY=antBed.PlacementY;w.SaveNow();
        w.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Normal));Pump(TimeSpan.FromMilliseconds(80));
        Assert(!Descendants(w.WidgetCanvas).OfType<BugHabitatArtwork>().Any() && w.bugsOverlay.Manager.Homes.Count==0,"Habitat widgets are hidden outside Arcade without deleting saved placements");
        w.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade));Pump(TimeSpan.FromMilliseconds(100));
        Assert(w.state.Widgets.Count(v=>BugHabitats.KindSpecies.ContainsKey(v.Kind))==6 && antBed.PlacementY==movedY,"Re-enabling Arcade restores habitats without duplicates or position loss");
        w.WidgetScroller.ScrollToTop();w.UpdateLayout();Save018(w,1250,900,"habitat-widgets018");w.SaveNow();w.Close();
        Assert(store.Load().Widgets.Single(v=>v.Id==antBed.Id).PlacementY==movedY,"Habitat positions persist in the existing Space save format");
    }
}
