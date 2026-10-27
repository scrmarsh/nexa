using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckBattlefield021()
    {
        var directory=Path.Combine(output,"battlefield-ui");Directory.CreateDirectory(directory);var store=new StateStore(Path.Combine(directory,"state.json"));
        var note=WidgetLibrary.Create("Notes");note.Text="My dashboard stays intact";note.CustomSize=true;note.PlacementX=0;note.PlacementY=0;
        store.Save(new AppState {WidgetLayoutInitialized=true,Widgets=[note]});store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade));
        var home=new HomeWindow(store) {Left=-10000,Top=-10000,WindowStartupLocation=WindowStartupLocation.Manual,ShowActivated=false};home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var entry=Descendants(home).OfType<Button>().Single(b=>Equals(b.Content,"I just wanna play with bugs"));
        Assert(entry.FontSize==10 && entry.Opacity<=.6 && entry.ToolTip!=null,"Home offers the exact subtle tiny bug-play button with an accessible description");
        entry.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var board=Application.Current.Windows.OfType<BattlefieldWindow>().Single();Pump(TimeSpan.FromMilliseconds(120));var world=board.World;var id=world.Manager.WorldId;
        Assert(board.Battlefield.Manager==world.Manager && world.Manager.IsInfinite && !Descendants(board).OfType<FrameworkElement>().Any(e=>e.Name=="WidgetCanvas"),"Home opens a widget-free Infinite battlefield using the shared world controller");
        Assert(store.Load().Widgets.Single().Id==note.Id && store.Load().Widgets.Single().Text==note.Text,"Standalone entry leaves dashboard widgets and content intact");
        Assert(home.OpenBattlefield()==board && Application.Current.Windows.OfType<BattlefieldWindow>().Count()==1,"Repeated menu clicks activate the existing battlefield instead of starting a second world");
        var main=new MainWindow(store) {Width=1200,Height=820,Left=-10000,Top=-10000,WindowStartupLocation=WindowStartupLocation.Manual,ShowActivated=false};main.Show();Pump(TimeSpan.FromMilliseconds(120));
        Assert(ReferenceEquals(main.sharedArcade,world) && ReferenceEquals(main.bugsOverlay!.Manager,board.Battlefield.Manager) && world.ActiveSurface==board,"Dashboard and standalone bind the same authoritative manager; standalone owns activity priority");
        world.SetPaused(true);world.SaveNow();var time=world.Manager.MatchAge;
        var unit=world.Manager.Bugs.First(BugManager.Alive);var position=unit.Position;var screen=main.bugsOverlay!.WorldToScreen(position);
        main.WidgetCanvas.Height=2500;main.UpdateLayout();main.WidgetScroller.ScrollToVerticalOffset(200);Pump(TimeSpan.FromMilliseconds(80));
        var movedScreen=main.bugsOverlay.WorldToScreen(position);
        Assert(main.WidgetScroller.VerticalOffset>100 && unit.Position==position && movedScreen.Y<screen.Y-100 && world.Manager.MatchAge==time,"Dashboard scrolling translates bugs with content without changing world coordinates or paused time");
        Assert((main.bugsOverlay.ScreenToWorld(movedScreen)-position).Length<.01,"Scrolled pointer conversion returns exact world coordinates for targeting and cuts");
        var cameraPoint=board.Battlefield.WorldToScreen(position);
        Assert((board.Battlefield.ScreenToWorld(cameraPoint)-position).Length<.01 && board.Battlefield.Camera.M11!=1,"Standalone fit/zoom camera uses the same reversible coordinate transform");
        var selector=Descendants(board).OfType<ComboBox>().Single();selector.SelectedIndex=1;
        var selectedId=(int)((ComboBoxItem)selector.SelectedItem).Tag;var selectedFaction=world.Manager.Colonies.Values.Single(c=>c.Id==selectedId);
        var focused=board.Battlefield.WorldToScreen(selectedFaction.Objective);
        Assert(Math.Abs(focused.X-board.Battlefield.ActualWidth/2)<1 && Math.Abs(focused.Y-board.Battlefield.ActualHeight/2)<1 && ((ComboBoxItem)selector.SelectedItem).Content is string,"Follow selector displays civilization names and centers the selected faction");
        board.CenterCamera();
        var settings=new SettingsDialog(home,store,store.Load(),null,"Add Bugs");settings.Show();Pump(TimeSpan.FromMilliseconds(50));
        Assert(Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Open Battlefield")) && Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Reset Arcade World")),"Add Bugs exposes battlefield, pause/resume, recovery and deliberate reset controls");settings.Close();
        Save018(home,(int)home.ActualWidth,(int)home.ActualHeight,"home-bugs021");Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"battlefield021");
        main.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{Mode=ArcadeRuleSet.ClassicRounds}));
        main.WidgetCanvas.Height=2500;main.UpdateLayout();main.WidgetScroller.ScrollToVerticalOffset(100);Pump(TimeSpan.FromMilliseconds(50));var classic=main.bugsOverlay!.Manager;classic.WorldPaused=true;var classicUnit=classic.Bugs.First();var classicPosition=classicUnit.Position;var beforeScroll=main.bugsOverlay.WorldToScreen(classicPosition);
        main.WidgetScroller.ScrollToVerticalOffset(350);Pump(TimeSpan.FromMilliseconds(60));
        Assert(classicUnit.Position==classicPosition && main.bugsOverlay.WorldToScreen(classicPosition).Y<beforeScroll.Y-80,"Classic Arcade rendering also follows dashboard scrolling without teleporting units");
        Assert(main.sharedArcade==null && !ReferenceEquals(main.bugsOverlay!.Manager,world.Manager) && world.Manager.WorldId==id,"Classic Rounds uses a transient manager without overwriting the persistent Infinite world");
        main.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Arcade));
        Assert(ReferenceEquals(main.bugsOverlay!.Manager,world.Manager) && world.Manager.WorldId==id,"Returning from Classic Rounds rebinds the original saved civilization world");
        board.Close();Pump(TimeSpan.FromMilliseconds(60));
        Assert(world.Manager.WorldId==id && world.Manager.MatchAge==time && world.ActiveSurface==main,"Closing battlefield returns to the same paused dashboard world");
        board=home.OpenBattlefield()!;Pump(TimeSpan.FromMilliseconds(70));
        Assert(board.World.Manager.WorldId==id && board.World.Manager.MatchAge==time && File.Exists(world.SavePath),"Reopening the tiny-menu entry resumes the same saved local Arcade world");
        board.Close();main.Close();home.Close();world.SaveNow();
        var loaded=new ArcadeWorldStore(world.SavePath).Load();Assert(!loaded.Blocked && loaded.World?.WorldId==id,"Orderly view shutdown leaves a valid resumable world save");
    }
}

