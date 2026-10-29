using System.Runtime.InteropServices;
namespace SessionDashboard;
// Core Audio endpoint follows the active Windows default output. No audio is captured.
internal sealed class WindowsOutputVolume : IDisposable
{
    private IMMDeviceEnumerator? enumerator;
    private IAudioEndpointVolume? endpoint;
    private readonly VolumeCallback callback;
    private readonly DeviceCallback devices;
    internal event Action? Changed;
    internal float Level {get;private set;}
    internal bool Muted {get;private set;}
    internal bool Available=>endpoint!=null;
    internal WindowsOutputVolume()
    {
        callback=new VolumeCallback(()=>Changed?.Invoke());devices=new DeviceCallback(()=>Changed?.Invoke());
        try{enumerator=(IMMDeviceEnumerator)new MMDeviceEnumerator();enumerator.RegisterEndpointNotificationCallback(devices);Refresh();}
        catch(Exception ex) when(ex is COMException or InvalidCastException){Dispose();}
    }
    internal void Refresh()
    {
        if(enumerator==null)return;
        IAudioEndpointVolume? next=null;IMMDevice? device=null;object? activated=null;
        try
        {
            if(enumerator.GetDefaultAudioEndpoint(0,1,out device)!=0){if(endpoint!=null){endpoint.UnregisterControlChangeNotify(callback);Marshal.ReleaseComObject(endpoint);endpoint=null;}Level=0;Muted=false;return;}
            var id=typeof(IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref id,23,IntPtr.Zero,out activated));next=(IAudioEndpointVolume)activated;
            if(endpoint!=null){endpoint.UnregisterControlChangeNotify(callback);Marshal.ReleaseComObject(endpoint);}
            endpoint=next;endpoint.RegisterControlChangeNotify(callback);endpoint.GetMasterVolumeLevelScalar(out var level);endpoint.GetMute(out var muted);Level=level;Muted=muted;
        }
        catch(COMException){if(next!=null && next!=endpoint)Marshal.ReleaseComObject(next);}
        finally{if(device!=null)Marshal.ReleaseComObject(device);}
    }
    internal void Set(float level){if(endpoint!=null){var context=Guid.Empty;if(endpoint.SetMasterVolumeLevelScalar(Math.Clamp(level,0,1),ref context)==0)Level=Math.Clamp(level,0,1);else Refresh();}}
    internal void ToggleMute(){if(endpoint!=null){var context=Guid.Empty;if(endpoint.SetMute(!Muted,ref context)==0)Muted=!Muted;else Refresh();}}
    public void Dispose()
    {
        if(endpoint!=null){endpoint.UnregisterControlChangeNotify(callback);Marshal.ReleaseComObject(endpoint);endpoint=null;}
        if(enumerator!=null){enumerator.UnregisterEndpointNotificationCallback(devices);Marshal.ReleaseComObject(enumerator);enumerator=null;}
    }
    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] private class MMDeviceEnumerator {}
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IMMDeviceEnumerator
    {
        [PreserveSig]int EnumAudioEndpoints(int flow,int state,out IntPtr collection);
        [PreserveSig]int GetDefaultAudioEndpoint(int flow,int role,out IMMDevice device);
        [PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IMMDevice device);
        [PreserveSig]int RegisterEndpointNotificationCallback(IMMNotificationClient client);
        [PreserveSig]int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IMMDevice
    {
        [PreserveSig]int Activate(ref Guid iid,int context,IntPtr parameters,[MarshalAs(UnmanagedType.IUnknown)]out object result);
    }
    [ComImport,Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IAudioEndpointVolume
    {
        [PreserveSig]int RegisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
        [PreserveSig]int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback callback);
        [PreserveSig]int GetChannelCount(out uint count);
        [PreserveSig]int SetMasterVolumeLevel(float level,ref Guid context);
        [PreserveSig]int SetMasterVolumeLevelScalar(float level,ref Guid context);
        [PreserveSig]int GetMasterVolumeLevel(out float level);
        [PreserveSig]int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig]int SetChannelVolumeLevel(uint channel,float level,ref Guid context);
        [PreserveSig]int SetChannelVolumeLevelScalar(uint channel,float level,ref Guid context);
        [PreserveSig]int GetChannelVolumeLevel(uint channel,out float level);
        [PreserveSig]int GetChannelVolumeLevelScalar(uint channel,out float level);
        [PreserveSig]int SetMute([MarshalAs(UnmanagedType.Bool)]bool mute,ref Guid context);
        [PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool mute);
    }
    [Guid("657804FA-D6AD-4496-8A60-352752AF4F89"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IAudioEndpointVolumeCallback{[PreserveSig]int OnNotify(IntPtr data);}
    [ComVisible(true),ClassInterface(ClassInterfaceType.None)] private sealed class VolumeCallback(Action changed):IAudioEndpointVolumeCallback{public int OnNotify(IntPtr data){changed();return 0;}}
    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] private interface IMMNotificationClient
    {
        [PreserveSig]int OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)]string id,uint state);
        [PreserveSig]int OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)]string id);
        [PreserveSig]int OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)]string id);
        [PreserveSig]int OnDefaultDeviceChanged(int flow,int role,[MarshalAs(UnmanagedType.LPWStr)]string? id);
        [PreserveSig]int OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)]string id,PropertyKey key);
    }
    [StructLayout(LayoutKind.Sequential)] private struct PropertyKey{public Guid Format;public uint Id;}
    [ComVisible(true),ClassInterface(ClassInterfaceType.None)] private sealed class DeviceCallback(Action changed):IMMNotificationClient
    {
        public int OnDeviceStateChanged(string id,uint state){changed();return 0;}
        public int OnDeviceAdded(string id){changed();return 0;}
        public int OnDeviceRemoved(string id){changed();return 0;}
        public int OnDefaultDeviceChanged(int flow,int role,string? id){if(flow==0)changed();return 0;}
        public int OnPropertyValueChanged(string id,PropertyKey key)=>0;
    }
}
