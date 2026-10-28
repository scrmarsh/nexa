using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;
// Window construction stays on its dispatcher; only model/file preparation runs on workers.
internal static class NexaNavigation
{
    private static readonly HashSet<Window> pending=[];
    internal static bool Busy(Window window)=>pending.Contains(window);
    internal static Task Change(Window source,Func<Task<Window>> prepare,NexaOptions options)
        =>source.Dispatcher.InvokeAsync(()=>ChangeCore(source,prepare,options)).Task.Unwrap();
    private static async Task ChangeCore(Window source,Func<Task<Window>> prepare,NexaOptions options)
    {
        if(!pending.Add(source))return;
        Window? target=null;var closed=false;var original=source.Background;var root=source.Content as FrameworkElement;
        void OnClosed(object? s,EventArgs e)=>closed=true;
        source.Closed+=OnClosed;
        try
        {
            source.Background=Brushes.Black;
            if(root!=null)await NexaMotion.Fade(root,0,options);
            if(closed)return;
            target=await prepare();
            if(closed){target.Close();return;}
            NexaChrome.TransferBounds(source,target);
            // Supersede the generic window entrance. Content stays black until first layout completes.
            target.Loaded+=(_,_)=>{target.BeginAnimation(Window.OpacityProperty,null);target.Opacity=1;};
            var targetRoot=target.Content as FrameworkElement;var background=target.Background;target.Background=Brushes.Black;
            if(targetRoot!=null)targetRoot.Opacity=0;
            Application.Current.MainWindow=target;target.Show();
            await target.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ContextIdle);
            if(targetRoot!=null)await NexaMotion.Fade(targetRoot,1,options);
            target.Background=background;
            source.Close();
        }
        catch
        {
            target?.Close();if(!closed){Application.Current.MainWindow=source;if(root!=null){root.BeginAnimation(UIElement.OpacityProperty,null);root.Opacity=1;}}
            throw;
        }
        finally {source.Closed-=OnClosed;source.Background=original;pending.Remove(source);}
    }
}
