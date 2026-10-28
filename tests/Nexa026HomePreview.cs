using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Windows.Threading;
using SessionDashboard;
internal static partial class Program
{
    private static void ShowHandoffUi026(App app)
    {
        var store=new StateStore(Path.Combine(output,"synthetic-ui.json"));
        var spaces=Enumerable.Range(0,14).Select(i=>{
            var data=new DashboardData{SpaceIcon=i%2==0?"Book":"Palette",WidgetLayoutInitialized=true,Session=new Session{Title="Synthetic focus",Start=DateTimeOffset.Now.AddMinutes(-15),End=DateTimeOffset.Now.AddMinutes(45)}};
            foreach(var kind in new[]{"Pixel timer","Productivity","Task List","Water Bottle Counter","Notes","Clock"})data.Widgets.Add(WidgetLibrary.Create(kind,kind=="Pixel timer"?"pixels":kind=="Notes"?"notes":kind=="Task List"?"tasks":""));
            WidgetLibrary.Arrange(data.Widgets,false);return Continuity.NewDashboard(new[]{"Morning focus","Lecture notes","Creative corner","Weekly planning","Reading nook","Personal projects"}[i%6]+(i<6?"":" "+(i+1)),"Study",data,store.UserId);
        }).ToList();
        var state=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId);state.Workspace!.Settings.SeasonalMode="Off";store.Save(state);
        var home=new HomeWindow(store){Title="Nexa - isolated handoff UI",Width=1250,Height=900};app.MainWindow=home;home.Show();
        var life=new DispatcherTimer{Interval=TimeSpan.FromMinutes(20)};life.Tick+=(_,_)=>{life.Stop();app.Shutdown();};life.Start();app.Run();
    }
    // Visible preview uses only this run's synthetic records, never the user's saved Spaces.
    private static void ShowHomeFullscreen026(App app)
    {
        var store=new StateStore(Path.Combine(output,"fullscreen-preview.json"));
        var names=new[]{"Morning focus","Lecture notes","Creative corner","Weekly planning","Reading nook","Personal projects"};
        var spaces=names.Select((name,i)=>Continuity.NewDashboard(name,"Study",new DashboardData{SpaceIcon=i%2==0?"Book":"Palette",WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Notes","notes")]},store.UserId)).ToList();
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId);
        model.Workspace!.Settings.SeasonalMode="On";model.Workspace.Settings.ReduceMotion=true;store.Save(model);
        var home=new HomeWindow(store){Title="nexa - fullscreen testing preview",WindowStartupLocation=WindowStartupLocation.Manual,Left=0,Top=0,Width=SystemParameters.PrimaryScreenWidth,Height=SystemParameters.PrimaryScreenHeight,ResizeMode=ResizeMode.NoResize,WindowStyle=WindowStyle.None};
        WindowChrome.SetWindowChrome(home,null);
        app.MainWindow=home;home.Show();
        home.PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape)home.Close();};
        var lifetime=new DispatcherTimer{Interval=TimeSpan.FromMinutes(10)};
        lifetime.Tick+=(_,_)=>home.Close();home.Closed+=(_,_)=>{lifetime.Stop();app.Shutdown();};lifetime.Start();
        File.WriteAllText(Path.Combine(output,"preview-window.txt"),$"{home.ActualWidth} x {home.ActualHeight}; synthetic data; Escape closes preview.");
        app.Run();
    }
}
