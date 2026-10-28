using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed class SplashWindow:Window
{
    private readonly Stopwatch logoClock = new();
    internal double DisplayedMilliseconds {get;private set;}
    internal readonly ProgressBar Loading=new(){Height=2,Minimum=0,Maximum=1,Background=Brushes.White,Foreground=BugVisuals.B("#84CC16"),BorderThickness=new Thickness(0),Margin=new Thickness(48,16,48,0)};
    internal readonly TextBlock Stage=new(){FontSize=10,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,0)};
    private readonly TextBlock tagline=new(){FontSize=15,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,0)};
    internal readonly List<string> CompletedStages=[];
    internal SplashWindow()
    {
        Title="nexa";Icon=NexaChrome.Icon;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Width=620;Height=350;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=BugVisuals.B("#100D19");
        var panel=new StackPanel{VerticalAlignment=VerticalAlignment.Center};panel.Children.Add(new NexaLogo{Width=480,Height=152});NexaBranding.ApplyTagline(tagline,NexaBranding.Tagline);panel.Children.Add(tagline);panel.Children.Add(Loading);panel.Children.Add(Stage);
        var grid=new Grid();grid.Children.Add(panel);grid.Children.Add(new TextBlock{Visibility=Visibility.Collapsed,Text=App.VersionLabel,Foreground=Brushes.SlateGray,FontSize=11,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(14)});Content=new Border{BorderBrush=BugVisuals.B("#2A3145"),BorderThickness=new Thickness(1),Child=grid};
        Loading.Visibility=Stage.Visibility=tagline.Visibility=Visibility.Hidden;
        ContentRendered+=(_,_)=>{if(!logoClock.IsRunning)logoClock.Start();};
        System.Windows.Automation.AutomationProperties.SetName(Loading,"Completed startup stages");Report(.05,"Configuration location ready");
    }
    internal async Task WaitForLogoAsync()
    {
        if(!logoClock.IsRunning)logoClock.Start();
        var remaining=TimeSpan.FromSeconds(3)-logoClock.Elapsed;
        if(remaining>TimeSpan.Zero)await Task.Delay(remaining);
        await Dispatcher.InvokeAsync(()=>{Loading.Visibility=Stage.Visibility=tagline.Visibility=Visibility.Visible;Loading.Foreground=BugVisuals.B(Theme.SeasonalEnabled?"#84CC16":"#60A5FA");});
    }
    internal void Report(double progress,string completed)
    {Loading.Value=Math.Max(Loading.Value,Math.Clamp(progress,0,1));Stage.Text=completed+" · stage progress";CompletedStages.Add(completed);}
    internal async Task<HomeWindow> EnterHome(StateStore store,bool diagnostic=false)
    {
        var watch=Stopwatch.StartNew();
        // Disk reads proceed during the logo-only hold; reported stages reflect completed work.
        var loaded=await Task.Run(()=>{var state=store.Load();var mode=store.LoadThemeMode();var theme=store.LoadAppTheme();return(state,mode,theme);});
        return await Dispatcher.InvokeAsync(async ()=>
        {
        Report(.6,"Workspace loaded and validated");
        Theme.SetSeasonalEnabled(NexaSeason.Active(loaded.state.Workspace?.Settings??new(),DateTimeOffset.UtcNow));Theme.Apply(Application.Current.Resources,loaded.mode,loaded.theme);Icon=NexaChrome.Icon;NexaBranding.ApplyTagline(tagline,NexaBranding.Tagline);
        await WaitForLogoAsync();
        Report(.75,"Saved appearance and session data ready");await Dispatcher.Yield(DispatcherPriority.Background);
        var home=new HomeWindow(store,loaded.state){Opacity=0};Report(.9,"Home controls initialized");
        if(diagnostic){home.WindowStartupLocation=WindowStartupLocation.Manual;home.Left=-10000;home.Top=-10000;home.ShowActivated=false;home.ShowInTaskbar=false;}
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);home.ContentRendered+=(_,_)=>ready.TrySetResult();Application.Current.MainWindow=home;home.Show();
        if(await Task.WhenAny(ready.Task,Task.Delay(TimeSpan.FromSeconds(15)))!=ready.Task){home.Close();throw new TimeoutException("Nexa's home window did not finish rendering.");}
        Report(1,"Home ready");DisplayedMilliseconds=watch.Elapsed.TotalMilliseconds;home.Opacity=1;Close();return home;
        }).Task.Unwrap();
    }
}
