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
    private readonly TaskCompletionSource taglineReady=new(TaskCreationOptions.RunContinuationsAsynchronously);
    private double reported;
    private readonly Stopwatch displayClock = new();
    private readonly CancellationTokenSource lifetime=new();
    internal static readonly TimeSpan MinimumDisplay = TimeSpan.FromSeconds(5);
    internal bool TaglineComplete=>taglineReady.Task.IsCompletedSuccessfully;
    internal double ReportedProgress=>reported;
    internal double DisplayedMilliseconds {get;private set;}
    internal readonly ProgressBar Loading=new(){Visibility=Visibility.Collapsed,Height=2,Minimum=0,Maximum=1,Background=Brushes.White,BorderThickness=new Thickness(0),Margin=new Thickness(48,16,48,0)};
    internal readonly TextBlock Stage=new(){Visibility=Visibility.Collapsed,FontSize=10,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,0)};
    private readonly TextBlock tagline=new(){FontSize=15,Foreground=Brushes.LightGray,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,12,0,0)};
    internal readonly List<string> CompletedStages=[];
    private Action beginSequence=()=>{};
    internal SplashWindow(NexaOptions? options=null)
    {
        this.options=options??new();Title=App.VersionLabel;Icon=NexaChrome.Icon;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Width=620;Height=350;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        SetResourceReference(BackgroundProperty,"BackgroundBrush");
        var panel=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
        panel.Children.Add(new NexaWordmark(480){HorizontalAlignment=HorizontalAlignment.Center});panel.Children.Add(tagline);panel.Children.Add(Loading);panel.Children.Add(Stage);
        Content=new Border{BorderBrush=BugVisuals.B("#2A3145"),BorderThickness=new Thickness(1),Child=panel};
        Loading.Foreground=BugVisuals.B(Theme.SeasonalEnabled?"#84CC16":"#60A5FA");
        var phrase=Theme.SeasonalEnabled?NexaBranding.Tagline:"a space for whatever comes next";var position=0;
        if(!Theme.SeasonalEnabled)tagline.Foreground=BugVisuals.B("#A5B4FC");
        void CompleteTagline()
        {
            typing.Stop();NexaBranding.ApplyTagline(tagline,phrase);tagline.Visibility=phrase.Length==0?Visibility.Collapsed:Visibility.Visible;
            taglineReady.TrySetResult();Loading.Visibility=Stage.Visibility=Visibility.Collapsed;ShowProgress();
        }
        typing.Tick+=(_,_)=>{if(position<phrase.Length)NexaBranding.ApplyTagline(tagline,phrase[..++position]);if(position>=phrase.Length)CompleteTagline();};
        beginSequence=()=>{if(!NexaMotion.Allowed(this.options)||phrase.Length==0)CompleteTagline();else typing.Start();};
        ContentRendered+=(_,_)=>{displayClock.Start();beginSequence();};
        Closed+=(_,_)=>{typing.Stop();displayClock.Stop();lifetime.Cancel();if(!TaglineComplete)taglineReady.TrySetCanceled();};
        System.Windows.Automation.AutomationProperties.SetName(Loading,"Completed startup stages");Report(.05,"Configuration location ready");
    }
    internal Task WaitForLogoAsync()=>taglineReady.Task;
    internal async Task WaitForMinimumDisplayAsync()
    {
        // TimeSpan delays may truncate fractional milliseconds. Recheck the
        // monotonic clock so reduced motion cannot finish just before 3,000ms.
        while (displayClock.Elapsed < MinimumDisplay)
            await Task.Delay(Math.Max(1, (int)Math.Ceiling((MinimumDisplay - displayClock.Elapsed).TotalMilliseconds)),lifetime.Token);
        DisplayedMilliseconds = displayClock.Elapsed.TotalMilliseconds;
    }
    internal async Task PreviewIn(Grid host)
    {
        var owner=Window.GetWindow(host);
        Owner=owner;ShowInTaskbar=false;WindowStartupLocation=owner==null?WindowStartupLocation.CenterScreen:WindowStartupLocation.CenterOwner;
        var closed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Closed+=(_,_)=>closed.TrySetResult();
        Show();
        try
        {
            var presentation=Present();
            await Task.WhenAny(presentation,closed.Task);
            if(!closed.Task.IsCompleted){await presentation;await Dispatcher.InvokeAsync(()=>NexaMotion.Fade(this,0,options)).Task.Unwrap();await Dispatcher.InvokeAsync(Close);}
        }
        catch(TaskCanceledException) when(closed.Task.IsCompleted){}
        async Task Present(){await WaitForLogoAsync();await WaitForMinimumDisplayAsync();}
    }
    internal void Report(double progress,string completed)
    {
        reported=Math.Max(reported,Math.Clamp(progress,0,1));Stage.Text=completed;CompletedStages.Add(completed);
        if(TaglineComplete)ShowProgress();
    }
    private void ShowProgress()
    {
        var current=Loading.Value;Loading.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,null);Loading.Value=reported;
        if(NexaMotion.Allowed(options) && current<reported)Loading.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,new DoubleAnimation(current,reported,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.Stop});
    }
    internal Task<HomeWindow> EnterHome(StateStore store,bool diagnostic=false,AppState? initialState=null)
        =>Dispatcher.InvokeAsync(()=>EnterHomeCore(store,diagnostic,initialState)).Task.Unwrap();
    private async Task<HomeWindow> EnterHomeCore(StateStore store,bool diagnostic,AppState? initialState)
    {
        var loaded=await Task.Run(()=>{var state=initialState??store.Load();return(state,mode:store.LoadThemeMode(),theme:store.LoadAppTheme());});
        Report(.6,"Workspace loaded and validated");
        await WaitForLogoAsync();
        Theme.SetSeasonalEnabled(NexaSeason.Active(loaded.state.Workspace?.Settings??new(),DateTimeOffset.UtcNow));Theme.Apply(Application.Current.Resources,loaded.mode,loaded.theme);Icon=NexaChrome.Icon;
        Report(.75,"Saved appearance and session data ready");await Dispatcher.Yield(DispatcherPriority.Background);
        var home=new HomeWindow(store,loaded.state){Opacity=0};Report(.9,"Home controls initialized");
        if(diagnostic){home.WindowStartupLocation=WindowStartupLocation.Manual;home.Left=-10000;home.Top=-10000;home.ShowActivated=false;home.ShowInTaskbar=false;}
        var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        home.Loaded+=(_,_)=>{home.BeginAnimation(OpacityProperty,null);home.Opacity=0;};
        home.ContentRendered+=(_,_)=>ready.TrySetResult();Application.Current.MainWindow=home;home.Show();
        if(await Task.WhenAny(ready.Task,Task.Delay(TimeSpan.FromSeconds(15)))!=ready.Task){home.Close();throw new TimeoutException("nexa's home window did not finish rendering.");}
        Report(1,"Home ready");
        await WaitForMinimumDisplayAsync();
        home.Opacity=1;await NexaMotion.Fade(this,0,options);Close();return home;
    }
}
