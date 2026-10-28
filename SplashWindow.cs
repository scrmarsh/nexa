using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed class SplashWindow:Window
{
    private readonly NexaOptions options;
    private readonly DispatcherTimer typing=new(){Interval=TimeSpan.FromMilliseconds(45)};
    private double reported;
    internal double DisplayedMilliseconds {get;private set;}
    internal readonly ProgressBar Loading=new(){Height=2,Minimum=0,Maximum=1,Background=Brushes.White,Foreground=BugVisuals.B("#84CC16"),BorderThickness=new Thickness(0),Margin=new Thickness(48,16,48,0)};
    internal readonly TextBlock Stage=new(){FontSize=10,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,0)};
    private readonly TextBlock tagline=new(){FontSize=15,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,0)};
    internal readonly List<string> CompletedStages=[];
    internal SplashWindow(NexaOptions? options=null)
    {
        this.options=options??new();
        Title="nexa";Icon=NexaChrome.Icon;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Width=620;Height=350;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=BugVisuals.B("#100D19");
        var panel=new StackPanel{VerticalAlignment=VerticalAlignment.Center};panel.Children.Add(new NexaLogo{Width=480,Height=152});panel.Children.Add(new TextBlock{Text=App.VersionLabel,FontSize=13,HorizontalAlignment=HorizontalAlignment.Center,Foreground=Brushes.LightGray,Margin=new Thickness(0,6,0,0)});panel.Children.Add(tagline);panel.Children.Add(Loading);panel.Children.Add(Stage);
        var grid=new Grid();grid.Children.Add(panel);grid.Children.Add(new TextBlock{Visibility=Visibility.Collapsed,Text=App.VersionLabel,Foreground=Brushes.SlateGray,FontSize=11,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(14)});Content=new Border{BorderBrush=BugVisuals.B("#2A3145"),BorderThickness=new Thickness(1),Child=grid};
        Loading.Foreground=BugVisuals.B(Theme.SeasonalEnabled?"#84CC16":"#60A5FA");
        var phrase=NexaBranding.Tagline;var position=0;
        if(!NexaMotion.Allowed(this.options))NexaBranding.ApplyTagline(tagline,phrase);
        typing.Tick+=(_,_)=>{if(position<phrase.Length)NexaBranding.ApplyTagline(tagline,phrase[..++position]);else typing.Stop();};
        Loaded+=(_,_)=>{if(NexaMotion.Allowed(this.options))typing.Start();};Closed+=(_,_)=>typing.Stop();
        System.Windows.Automation.AutomationProperties.SetName(Loading,"Completed startup stages");Report(.05,"Configuration location ready");
    }
    internal Task WaitForLogoAsync()=>Task.CompletedTask;
    internal void Report(double progress,string completed)
    {
        reported=Math.Max(reported,Math.Clamp(progress,0,1));
        var current=Loading.Value;Loading.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,null);Loading.Value=reported;
        if(NexaMotion.Allowed(options) && reported<1)Loading.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,new DoubleAnimation(current,reported,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.Stop});
        Stage.Text=completed;CompletedStages.Add(completed);
    }
    internal async Task<HomeWindow> EnterHome(StateStore store,bool diagnostic=false,AppState? initialState=null)
    {
        var watch=Stopwatch.StartNew();
        // Interpolate only to completed real milestones; no fixed logo hold.
        var loaded=await Task.Run(()=>{var state=initialState??store.Load();var mode=store.LoadThemeMode();var theme=store.LoadAppTheme();return(state,mode,theme);});
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
