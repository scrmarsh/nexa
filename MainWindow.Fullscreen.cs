using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Windows.Documents;
namespace SessionDashboard;
public partial class MainWindow
{
    private Border? fullscreenNotice;
    private readonly DispatcherTimer fullscreenNoticeTimer=new(){Interval=TimeSpan.FromSeconds(3)};
    private readonly DispatcherTimer systemStatusTimer=new(){Interval=TimeSpan.FromSeconds(30)};
    private WindowsOutputVolume? outputVolume;
    private TextBlock? batteryStatus,networkStatus;
    private Button? volumeStatus;
    private Slider? systemVolumeSlider;
    private bool statusListening,updatingVolume,statusBusy;
    private long statusGeneration;
    private void ShowFullscreenNotice()
    {
        DismissFullscreenNotice();
        fullscreenNotice=new Border{Child=new TextBlock{Text="Fullscreen mode — Press F11 to exit",FontSize=12},Padding=new Thickness(12,7,12,7),CornerRadius=new CornerRadius(7),VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,3,0,0),IsHitTestVisible=false};
        fullscreenNotice.SetResourceReference(Border.BackgroundProperty,"SurfaceBrush");fullscreenNotice.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");fullscreenNotice.BorderThickness=new Thickness(1);
        Panel.SetZIndex(fullscreenNotice,5000);((Grid)Content).Children.Add(fullscreenNotice);
        if(NexaMotion.Allowed(Options))fullscreenNotice.BeginAnimation(OpacityProperty,new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromSeconds(3),KeyFrames={new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)),new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(.15))),new LinearDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.7))),new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3)))}});
        fullscreenNoticeTimer.Tick-=FullscreenNoticeExpired;fullscreenNoticeTimer.Tick+=FullscreenNoticeExpired;fullscreenNoticeTimer.Start();
    }
    private void FullscreenNoticeExpired(object? sender,EventArgs e)=>DismissFullscreenNotice();
    private void DismissFullscreenNotice(){fullscreenNoticeTimer.Stop();if(fullscreenNotice!=null){((Grid)Content).Children.Remove(fullscreenNotice);fullscreenNotice=null;}}
    private void ReconcileFullscreenStatus()
    {
        if(SystemIndicators==null)return;
        var show=fullscreen && Options.ShowSystemIndicators;
        SystemIndicators.Visibility=show?Visibility.Visible:Visibility.Collapsed;
        if(!show){DisposeSystemIndicators();return;}
        if(statusListening){RefreshSystemStatus();return;}
        statusListening=true;SystemIndicators.Children.Clear();
        batteryStatus=new TextBlock{FontSize=11,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(4,0,4,0)};
        networkStatus=new TextBlock{FontSize=11,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(4,0,4,0)};
        volumeStatus=new Button{Padding=new Thickness(5,3,5,3),FontSize=11,ToolTip="System output volume"};
        SystemIndicators.Children.Add(batteryStatus);SystemIndicators.Children.Add(networkStatus);SystemIndicators.Children.Add(volumeStatus);
        outputVolume=new WindowsOutputVolume();outputVolume.Changed+=QueueSystemStatus;
        volumeStatus.Click+=(_,_)=>{if(outputVolume?.Available==true){outputVolume.ToggleMute();RefreshSystemStatus();}};
        systemVolumeSlider=new Slider{Minimum=0,Maximum=100,Width=90,SmallChange=1,LargeChange=10,IsMoveToPointEnabled=true,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(4,0,6,0),ToolTip="Windows output volume"};
        System.Windows.Automation.AutomationProperties.SetName(systemVolumeSlider,"Windows output volume");
        systemVolumeSlider.ValueChanged+=(_,_)=>{if(!updatingVolume && Options.EnableVolumeSlider && outputVolume?.Available==true){outputVolume.Set((float)systemVolumeSlider.Value/100);RefreshSystemStatus();}};SystemIndicators.Children.Add(systemVolumeSlider);
        NetworkChange.NetworkAddressChanged+=NetworkStatusChanged;NetworkChange.NetworkAvailabilityChanged+=NetworkAvailabilityChanged;SystemEvents.PowerModeChanged+=StatusPowerChanged;
        systemStatusTimer.Tick+=StatusTimerTick;systemStatusTimer.Start();RefreshSystemStatus();
    }
    private void QueueSystemStatus(){if(statusListening && !Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(new Action(RefreshSystemStatus));}
    private void NetworkStatusChanged(object? sender,EventArgs e)=>QueueSystemStatus();
    private void NetworkAvailabilityChanged(object? sender,NetworkAvailabilityEventArgs e)=>QueueSystemStatus();
    private void StatusPowerChanged(object sender,PowerModeChangedEventArgs e)=>QueueSystemStatus();
    private void StatusTimerTick(object? sender,EventArgs e)=>RefreshSystemStatus();
    private async void RefreshSystemStatus()
    {
        if(!statusListening)return;
        outputVolume?.Refresh();
        if(volumeStatus!=null){volumeStatus.Content=outputVolume?.Available==true?$"{(outputVolume.Muted?"🔇":"🔊")} {outputVolume.Level*100:0}%":"No output";volumeStatus.ToolTip=outputVolume?.Muted==true?"Unmute Windows output":"Mute Windows output";volumeStatus.IsEnabled=Options.EnableVolumeSlider && outputVolume?.Available==true;System.Windows.Automation.AutomationProperties.SetName(volumeStatus,volumeStatus.ToolTip.ToString());}
        if(systemVolumeSlider!=null){systemVolumeSlider.IsEnabled=Options.EnableVolumeSlider && outputVolume?.Available==true;}
        if(systemVolumeSlider!=null && outputVolume!=null){updatingVolume=true;systemVolumeSlider.Value=outputVolume.Level*100;updatingVolume=false;}
        if(GetSystemPowerStatus(out var power) && batteryStatus!=null)
        {
            batteryStatus.Visibility=power.BatteryFlag==128?Visibility.Collapsed:Visibility.Visible;
            var percent=power.Percent<=100?(int)power.Percent:-1;var charging=power.BatteryFlag!=255 && (power.BatteryFlag&8)!=0 || power.AC==1;
            batteryStatus.Inlines.Clear();batteryStatus.Inlines.Add(new InlineUIContainer(new SystemStatusIcon{Kind="battery",Percent=percent,Charging=charging}));batteryStatus.Inlines.Add(new Run(percent>=0?$" {percent}%":" —"));batteryStatus.ToolTip=charging?"Battery · external power / charging":"Battery";
        }
        else if(batteryStatus!=null)batteryStatus.Visibility=Visibility.Collapsed;
        if(statusBusy)return;
        var generation=statusGeneration;statusBusy=true;
        try
        {
            var network=await Task.Run(ReadWirelessStatus);
            await Dispatcher.InvokeAsync(()=>{if(statusListening && generation==statusGeneration && networkStatus!=null){networkStatus.Inlines.Clear();networkStatus.Inlines.Add(new InlineUIContainer(new SystemStatusIcon{Connected=network.Connected,Opacity=network.Available?1:.45}));networkStatus.ToolTip=network.Description;System.Windows.Automation.AutomationProperties.SetName(networkStatus,network.Description);}});
        }
        finally{statusBusy=false;}
    }
    internal static (bool Available,bool Connected,string Description) ReadWirelessStatus()
    {
        try
        {
            var adapters=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211).ToArray();
            var connected=adapters.Any(n=>n.OperationalStatus==OperationalStatus.Up);
            return(adapters.Length>0,connected,adapters.Length==0?"Wi-Fi hardware unavailable":connected?"Wi-Fi connected · signal strength unavailable":"Wi-Fi disconnected");
        }
        catch(NetworkInformationException){return(false,false,"Wi-Fi status unavailable");}
    }
    internal static string ReadWindowsNetworkStatus()
    {
        try
        {
            var adapters=NetworkInterface.GetAllNetworkInterfaces().Where(n=>n.OperationalStatus==OperationalStatus.Up && n.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)).ToArray();
            if(adapters.Length==0)return "Offline";
            var wifi=adapters.Any(n=>n.NetworkInterfaceType==NetworkInterfaceType.Wireless80211);var wired=adapters.Any(n=>n.NetworkInterfaceType==NetworkInterfaceType.Ethernet);
            var medium=wifi && wired?"Wi-Fi / Ethernet":wifi?"Wi-Fi":wired?"Ethernet":"Network";
            object? manager=null;
            try
            {
                var type=Type.GetTypeFromCLSID(new Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B"));manager=type==null?null:Activator.CreateInstance(type);
                if(manager!=null){dynamic nlm=manager;int flags=(int)nlm.GetConnectivity();return medium+((flags&0x440)!=0?" · Internet":(flags&0x330)!=0?" · Local only":" · Disconnected");}
            }
            catch(Exception ex) when(ex is COMException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException){}
            finally{if(manager!=null && Marshal.IsComObject(manager))Marshal.ReleaseComObject(manager);}
            return medium+" · Internet unverified";
        }
        catch(NetworkInformationException){return "Network unavailable";}
    }
    private void DisposeSystemIndicators()
    {
        if(!statusListening)return;statusListening=false;statusGeneration++;
        systemStatusTimer.Stop();systemStatusTimer.Tick-=StatusTimerTick;
        NetworkChange.NetworkAddressChanged-=NetworkStatusChanged;NetworkChange.NetworkAvailabilityChanged-=NetworkAvailabilityChanged;SystemEvents.PowerModeChanged-=StatusPowerChanged;
        if(outputVolume!=null){outputVolume.Changed-=QueueSystemStatus;outputVolume.Dispose();outputVolume=null;}
        SystemIndicators.Children.Clear();
        systemVolumeSlider=null;volumeStatus=null;networkStatus=null;batteryStatus=null;
    }
    private void FullscreenFocusChanged(object? sender,EventArgs e){if(fullscreen)Topmost=IsActive;}
    private void DisposeFullscreenStatus(){DismissFullscreenNotice();DisposeSystemIndicators();}
    [StructLayout(LayoutKind.Sequential)] private struct PowerStatus{public byte AC,BatteryFlag,Percent,Reserved;public uint LifeTime,FullLifeTime;}
    [DllImport("kernel32.dll")] [return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetSystemPowerStatus(out PowerStatus status);
}
