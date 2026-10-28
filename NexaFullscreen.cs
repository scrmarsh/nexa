using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
namespace SessionDashboard;
// A navigation chain shares one restoration record; a fresh application starts windowed.
internal static class NexaFullscreen
{
    private sealed class Session
    {internal Rect Bounds;internal WindowState State;internal bool Topmost;internal bool Active;}
    private sealed class Host {internal Session Session=new();internal WindowChrome? Chrome;internal ResizeMode Resize;}
    private static readonly ConditionalWeakTable<Window,Host> hosts=new();
    internal static bool IsFullscreen(Window? window)=>window!=null && hosts.TryGetValue(window,out var host) && host.Session.Active;
    internal static void SetRestoredTopmost(Window window,bool value)
    {if(hosts.TryGetValue(window,out var host))host.Session.Topmost=value;}
    internal static void Attach(Window window)
    {
        if(window is not (HomeWindow or MainWindow))return;
        hosts.GetOrCreateValue(window);
        window.Activated+=(_,_)=>{if(IsFullscreen(window))window.Topmost=true;};
        window.Deactivated+=(_,_)=>{if(IsFullscreen(window))window.Topmost=false;};
        if(window is HomeWindow home)window.PreviewKeyDown+=(_,e)=>
        {
            var options=home.Saved?.Workspace?.Settings??new();
            if(e.Handled)return;
            if(e.Key==Key.F11 && options.EnableF11){Toggle(window);e.Handled=true;}
            else if(e.Key==Key.Escape && IsFullscreen(window) && options.EscExitsFullscreen && Keyboard.FocusedElement is not (TextBox or ComboBox) && !NexaPanels.HandleEscape(window,e))
            {Toggle(window);e.Handled=true;}
        };
        window.Loaded+=(_,_)=>Refresh(window);
    }
    internal static void Toggle(Window window)
    {
        var host=hosts.GetOrCreateValue(window);var session=host.Session;
        if(!session.Active)
        {
            session.Bounds=window.WindowState==WindowState.Normal?new Rect(window.Left,window.Top,window.Width,window.Height):window.RestoreBounds;
            session.State=window.WindowState;session.Topmost=window.Topmost;session.Active=true;Enter(window,host);
        }
        else
        {
            session.Active=false;window.WindowState=WindowState.Normal;
            WindowChrome.SetWindowChrome(window,host.Chrome);window.ResizeMode=host.Resize;
            window.Left=session.Bounds.Left;window.Top=session.Bounds.Top;window.Width=session.Bounds.Width;window.Height=session.Bounds.Height;
            window.Topmost=session.Topmost;window.WindowState=session.State;
        }
        Refresh(window);
    }
    private static void Enter(Window window,Host host)
    {
        host.Chrome=WindowChrome.GetWindowChrome(window);host.Resize=window.ResizeMode;
        window.WindowState=WindowState.Normal;WindowChrome.SetWindowChrome(window,null);
        window.ResizeMode=ResizeMode.NoResize;window.Topmost=true;window.WindowState=WindowState.Maximized;
    }
    internal static void Transfer(Window source,Window target)
    {
        if(!IsFullscreen(source))return;
        var old=hosts.GetOrCreateValue(source);var host=hosts.GetOrCreateValue(target);host.Session=old.Session;
        target.WindowState=WindowState.Normal;target.Left=old.Session.Bounds.Left;target.Top=old.Session.Bounds.Top;target.Width=old.Session.Bounds.Width;target.Height=old.Session.Bounds.Height;
        Enter(target,host);Refresh(target);
    }
    internal static void Refresh(Window window)
    {
        void Visit(DependencyObject item)
        {
            if(item is FrameworkElement {Tag:"WindowCaption"} caption)caption.Visibility=IsFullscreen(window)?Visibility.Collapsed:Visibility.Visible;
            for(var i=0;i<VisualTreeHelper.GetChildrenCount(item);i++)Visit(VisualTreeHelper.GetChild(item,i));
        }
        Visit(window);
        if(window is MainWindow main)main.FullscreenChanged();
    }
}
