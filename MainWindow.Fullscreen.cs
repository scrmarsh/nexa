using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
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
    private Popup? systemVolumePopup;
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
        volumeStatus.Click+=(_,_)=>ShowVolumePopup();
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
        if(!statusListening || statusBusy)return;
        outputVolume?.Refresh();
        if(volumeStatus!=null){volumeStatus.Content=outputVolume?.Available==true?$"{(outputVolume.Muted?"Muted":"Vol")} {outputVolume.Level*100:0}%":"No output";volumeStatus.IsEnabled=Options.EnableVolumeSlider && outputVolume?.Available==true;}
        if(systemVolumeSlider!=null && outputVolume!=null){updatingVolume=true;systemVolumeSlider.Value=outputVolume.Level*100;updatingVolume=false;}
        if(GetSystemPowerStatus(out var power) && batteryStatus!=null)
        {batteryStatus.Visibility=power.BatteryFlag==128 || power.BatteryFlag==255 || power.Percent==255?Visibility.Collapsed:Visibility.Visible;batteryStatus.Text=$"{power.Percent}%{((power.BatteryFlag&8)!=0?" charging":power.AC==1?" · AC":" battery")}";}
        var generation=statusGeneration;statusBusy=true;
        try
        {
            var network=await Task.Run(ReadWindowsNetworkStatus);
            await Dispatcher.InvokeAsync(()=>{if(statusListening && generation==statusGeneration && networkStatus!=null){networkStatus.Text=network;networkStatus.ToolTip=network;}});
        }
        finally{statusBusy=false;}
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
    private void ShowVolumePopup()
    {
        if(!Options.EnableVolumeSlider || outputVolume?.Available!=true)return;
        if(systemVolumePopup!=null){systemVolumePopup.IsOpen=false;systemVolumePopup=null;return;}
        var panel=new StackPanel{Width=190,Margin=new Thickness(12)};panel.Children.Add(new TextBlock{Text="Windows output volume"});
        systemVolumeSlider=new Slider{Minimum=0,Maximum=100,Value=outputVolume.Level*100,Margin=new Thickness(0,12,0,8)};
        systemVolumeSlider.ValueChanged+=(_,_)=>{if(!updatingVolume)outputVolume?.Set((float)systemVolumeSlider.Value/100);};panel.Children.Add(systemVolumeSlider);
        var mute=new Button{Content=outputVolume.Muted?"Unmute":"Mute"};mute.Click+=(_,_)=>{outputVolume?.ToggleMute();mute.Content=outputVolume?.Muted==true?"Unmute":"Mute";RefreshSystemStatus();};panel.Children.Add(mute);
        var border=new Border{Child=panel,CornerRadius=new CornerRadius(8)};border.SetResourceReference(Border.BackgroundProperty,"PanelBrush");
        systemVolumePopup=new Popup{Child=border,PlacementTarget=volumeStatus,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true};systemVolumePopup.Closed+=(_,_)=>{systemVolumePopup=null;systemVolumeSlider=null;};systemVolumePopup.IsOpen=true;
    }
    private void DisposeSystemIndicators()
    {
        if(!statusListening)return;statusListening=false;statusGeneration++;
        systemStatusTimer.Stop();systemStatusTimer.Tick-=StatusTimerTick;
        NetworkChange.NetworkAddressChanged-=NetworkStatusChanged;NetworkChange.NetworkAvailabilityChanged-=NetworkAvailabilityChanged;SystemEvents.PowerModeChanged-=StatusPowerChanged;
        if(outputVolume!=null){outputVolume.Changed-=QueueSystemStatus;outputVolume.Dispose();outputVolume=null;}
        if(systemVolumePopup!=null)systemVolumePopup.IsOpen=false;
        SystemIndicators.Children.Clear();
    }
    private void FullscreenFocusChanged(object? sender,EventArgs e){if(fullscreen)Topmost=IsActive;}
    private void DisposeFullscreenStatus(){DismissFullscreenNotice();DisposeSystemIndicators();}
    [StructLayout(LayoutKind.Sequential)] private struct PowerStatus{public byte AC,BatteryFlag,Percent,Reserved;public uint LifeTime,FullLifeTime;}
    [DllImport("kernel32.dll")] [return:MarshalAs(UnmanagedType.Bool)]private static extern bool GetSystemPowerStatus(out PowerStatus status);
}
