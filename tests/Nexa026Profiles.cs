using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;
internal static partial class Program
{
    private static void Profile026()
    {
        var results=new List<string>();var store=new StateStore(Path.Combine(output,"profile-navigation.json"));
        var spaces=Enumerable.Range(0,8).Select(i=>Continuity.NewDashboard("Synthetic "+i,"Study",new DashboardData{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Notes","notes"),WidgetLibrary.Create("Task List","tasks"),WidgetLibrary.Create("Productivity")]},store.UserId)).ToList();
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId);model.Workspace!.Settings.ReduceMotion=true;store.Save(model);
        void Wait(Func<bool> finished){var wait=Stopwatch.StartNew();while(!finished() && wait.Elapsed<TimeSpan.FromSeconds(20))Pump(TimeSpan.FromMilliseconds(15));Assert(finished(),"Profiled navigation completes within the diagnostic deadline");}
        var home=new HomeWindow(store){ShowInTaskbar=false,ShowActivated=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-10000,Top=-10000};home.Show();Application.Current.MainWindow=home;Pump(TimeSpan.FromMilliseconds(80));
        var startMemory=Process.GetCurrentProcess().WorkingSet64;
        for(var i=0;i<4;i++)
        {
            var watch=Stopwatch.StartNew();home.OpenDashboard(spaces[i%spaces.Count].DashboardId);Wait(()=>!home.IsVisible);
            results.Add($"Open Space cycle {i}: {watch.Elapsed.TotalMilliseconds:F2} ms; "+string.Join(", ",NexaNavigation.LastProfile.Select(p=>$"{p.Key} {p.Value:F2} ms")));
            var main=(MainWindow)Application.Current.MainWindow;results.Add("Construction: "+string.Join(", ",main.ConstructionProfile.Select(p=>$"{p.Key} {p.Value:F2} ms")));
            watch.Restart();var settings=new SettingsDialog(main,store,main.state,main,"Appearance");settings.Show();Pump(TimeSpan.FromMilliseconds(40));results.Add($"Settings first layout {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");settings.Close();
            watch.Restart();Theme.BeginTransition(main.state.Workspace!.Settings);main.state.Workspace.Settings.SeasonalMode=i%2==0?"On":"Off";main.ApplyReliabilityOptions();Pump(TimeSpan.FromMilliseconds(30));results.Add($"Theme update reduced-motion {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");
            var back=main.TopNavigation.Children.OfType<Button>().First(b=>Descendants(b).OfType<TextBlock>().Any(t=>t.Text=="Home"));watch.Restart();back.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(()=>!main.IsVisible);
            results.Add($"Return Home {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");home=(HomeWindow)Application.Current.MainWindow;
        }
        var dev=Descendants(home).OfType<Button>().Single(b=>Equals(b.Content,"DEV"));var developer=Stopwatch.StartNew();dev.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(()=>!home.IsVisible);results.Add($"Developer first layout: {developer.Elapsed.TotalMilliseconds:F2} ms");
        var dashboard=(MainWindow)Application.Current.MainWindow;results.Add("Deferred offscreen editors at readiness: "+dashboard.DeferredWidgetBodies);results.Add("Developer construction: "+string.Join(", ",dashboard.ConstructionProfile.Select(p=>$"{p.Key} {p.Value:F2} ms")));Capture026(dashboard,"developer-026");dashboard.Close();
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();results.Add($"Working set start {startMemory:N0} bytes; after four cycles plus Developer {Process.GetCurrentProcess().WorkingSet64:N0} bytes. This is a short stress sample, not an extended-session leak certification.");
        var world=CreateWorld(BugBehavior.Cozy);world.Configure(new BugPreferences(true,BugSpecies.AllBugs,BugDensity.Infestation,world.EcosystemSettings));var simulation=Stopwatch.StartNew();Step(world,90);results.Add($"High-density simulation workload: {simulation.Elapsed.TotalMilliseconds:F2} ms for 90 simulated seconds; population {world.Bugs.Count}. This is CPU workload timing, not observed display FPS.");
        File.WriteAllLines(Path.Combine(output,"navigation-profile.txt"),results);foreach(var row in results)Console.WriteLine(row);
    }
}
