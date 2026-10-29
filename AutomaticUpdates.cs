using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SessionDashboard;

internal sealed class AutomaticUpdates
{
    private static readonly Dictionary<string,AutomaticUpdates> services=new(StringComparer.OrdinalIgnoreCase);
    private readonly StateStore store;
    private readonly DispatcherTimer heartbeat=new(DispatcherPriority.Background){Interval=TimeSpan.FromSeconds(1)};
    private DateTimeOffset nextCheck=DateTimeOffset.UtcNow.AddHours(4);
    private bool started,choosing;
    private Grid? currentRoot;
    private readonly Border notice;
    private readonly TextBlock text=new(){TextWrapping=TextWrapping.Wrap,MaxWidth=360,Margin=new Thickness(0,0,0,8)};
    private readonly Button action=new(){Content="Install update",Margin=new Thickness(0,0,8,0)};
    internal UpdateFlow Flow {get;}
    internal static AutomaticUpdates For(StateStore store)
    {
        var key=Path.GetFullPath(store.FilePath);
        if(!services.TryGetValue(key,out var service))services[key]=service=new(store);
        return service;
    }
    private AutomaticUpdates(StateStore store)
    {
        this.store=store;
        Flow=new(store.LoadUpdatePreferences,store.SaveUpdatePreferences,Check,Prepare,Restart);
        var buttons=new WrapPanel();buttons.Children.Add(action);
        var later=new Button{Content="Later"};buttons.Children.Add(later);later.Click+=(_,_)=>Flow.Later();
        action.Click+=async(_,_)=>{if(Flow.Prepared!=null)await Flow.RestartNowAsync();else await Flow.InstallAsync();};
        var content=new StackPanel();content.Children.Add(new TextBlock{Text="nexa update",FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,6)});content.Children.Add(text);content.Children.Add(buttons);
        notice=new Border{Child=content,Padding=new Thickness(16),CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(18),Visibility=Visibility.Collapsed,MaxWidth=400};
        notice.SetResourceReference(Border.BackgroundProperty,"PanelBrush");notice.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");Panel.SetZIndex(notice,180);
        Flow.Changed+=Refresh;
        heartbeat.Tick+=async(_,_)=>
        {
            AttachNotice();await Flow.TickAsync();
            if(DateTimeOffset.UtcNow>=nextCheck && store.LoadUpdatePreferences().UpdateChoiceMade)
            {nextCheck=DateTimeOffset.UtcNow.AddHours(4);await Flow.CheckAsync();}
        };
        Application.Current.Exit+=(_,_)=>{heartbeat.Stop();Flow.Cancel();};
    }
    internal async Task StartAsync(Window owner)
    {
        if(started)return;started=true;heartbeat.Start();
        try
        {
            if(!store.LoadUpdatePreferences().UpdateChoiceMade)
            {
                choosing=true;var choice=BuildChoice(owner,store);
                choice.ShowDialog();choosing=false;
            }
            await Flow.CheckAsync();
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {text.Text="Update preferences could not be saved: "+ex.Message;choosing=false;}
        finally{Refresh();}
    }
    internal static NexaDialog BuildChoice(Window owner,StateStore store)
    {
        var dialog=new NexaDialog{Owner=owner,Title="nexa updates",Width=540,Height=420,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};
        var body=new StackPanel{Margin=new Thickness(24)};
        body.Children.Add(new TextBlock{Text="Keep nexa up to date",FontSize=24,Margin=new Thickness(0,0,0,12)});
        body.Children.Add(new TextBlock{Text="Automatic updates download and verify new releases, then save your work and restart after a 30-second countdown. You can restart now or postpone. You will still receive update notices if you turn automatic installation off.",TextWrapping=TextWrapping.Wrap});
        var automatic=new CheckBox{Content="Keep automatic updates enabled",IsChecked=true,Margin=new Thickness(0,18,0,16)};body.Children.Add(automatic);
        var error=new TextBlock{TextWrapping=TextWrapping.Wrap};body.Children.Add(error);
        var save=new Button{Content="Save update preference",HorizontalAlignment=HorizontalAlignment.Right};body.Children.Add(save);
        save.Click+=(_,_)=>
        {
            try{var prefs=store.LoadUpdatePreferences();prefs.AutomaticUpdates=automatic.IsChecked==true;prefs.UpdateChoiceMade=true;prefs.CheckOnStartup=true;store.SaveUpdatePreferences(prefs);dialog.DialogResult=true;}
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){error.Text="Could not save your choice: "+ex.Message;}
        };
        // Closing without choosing leaves consent unset; notices still work, no auto install.
        dialog.Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};return dialog;
    }
    private async Task<AvailableUpdate?> Check(UpdatePreferences prefs,CancellationToken cancellation)
    {
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(20)};
        var updater=new ExperimentalUpdater(client);
        var release=await updater.CheckAsync(new Version(App.AppVersion),cancellation,NexaReleaseIdentity.Current.Stage,prefs.Channel);
        if(release==null && updater.LatestEligible==null)throw new InvalidDataException("No eligible published package was found. This does not confirm that the installed version is current.");
        return release;
    }
    private async Task<string> Prepare(AvailableUpdate release,IProgress<double> progress,CancellationToken cancellation)
    {
        var target=Environment.ProcessPath??throw new IOException("Application path unavailable.");
        if(!Path.GetFileName(target).Equals("Nexa.exe",StringComparison.OrdinalIgnoreCase))throw new IOException("Automatic installation is available in the portable Nexa.exe release, not this development host.");
        using var client=new HttpClient{Timeout=TimeSpan.FromMinutes(10)};
        var candidate=await new ExperimentalUpdater(client).DownloadAsync(release,Path.Combine(store.UserPreferencesDirectory,"updates","downloads"),progress,cancellation);
        cancellation.ThrowIfCancellationRequested();
        return await Task.Run(()=>UpdateTransaction.Prepare(store,target,candidate,release.Sha256,release.Version.ToString(3)),cancellation);
    }
    private async Task<bool> Restart(string prepared)
    {
        // Flush all editors and this store's live windows at the final countdown boundary.
        foreach(var settings in Application.Current.Windows.OfType<SettingsDialog>().ToArray())if(!settings.FlushPendingChanges())return false;
        foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>w.SettingsStore.FilePath==store.FilePath).ToArray())
            if(!window.PrepareForUpdateRestart())return false;
        NexaEcosystem.CheckpointAll();
        await UpdateTransaction.BackupData(store,Path.GetDirectoryName(prepared)!);
        UpdateTransaction.StartHelper(prepared);Application.Current.Shutdown();return true;
    }
    internal static Grid? NoticeRoot(Window? window)=>window is NexaDialog {Body:Grid body}?body:window?.Content as Grid;
    private void AttachNotice()
    {
        var host=Application.Current.Windows.OfType<Window>().LastOrDefault(w=>w.IsVisible && w.WindowState!=WindowState.Minimized && (w is HomeWindow h && h.SettingsStore.FilePath==store.FilePath || w is MainWindow m && m.SettingsStore.FilePath==store.FilePath));
        var root=NoticeRoot(host);
        if(root==currentRoot)return;
        currentRoot?.Children.Remove(notice);currentRoot=root;currentRoot?.Children.Add(notice);Refresh();
    }
    private void Refresh()
    {
        if(!started)return;
        text.Text=Flow.Status;
        notice.Visibility=Flow.NoticeVisible && !choosing?Visibility.Visible:Visibility.Collapsed;
        action.Content=Flow.Prepared!=null?"Restart now":"Install update";
        action.IsEnabled=!Flow.Busy && !Flow.Restarting;
        var host=currentRoot==null?null:Window.GetWindow(currentRoot);
        var inset=host!=null && NexaFullscreen.IsFullscreen(host)?12:18;notice.Margin=new Thickness(inset);
    }
}

public partial class MainWindow
{
    internal bool PrepareForUpdateRestart()
    {
        if(storyOpen)
        {
            SaveSwarmRun();
            if(swarmStats!=null)
                try{store.SaveSwarmStats(swarmStats);}
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Feedback.Text="The game score could not be saved. Restart is paused: "+ex.Message;return false;}
        }
        return SaveForClose();
    }
}
