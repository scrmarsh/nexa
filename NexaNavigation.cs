using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
namespace SessionDashboard;
// Window construction stays on its dispatcher; only model/file preparation runs on workers.
internal static class NexaNavigation
{
    private static readonly HashSet<Window> pending=[];
    internal static Dictionary<string,double> LastProfile {get;private set;}=[];
    internal static bool Busy(Window window)=>pending.Contains(window);
    internal static Task Change(Window source,Func<Task<Window>> prepare,NexaOptions options)
    {
        var request=System.Diagnostics.Stopwatch.StartNew();
        return source.Dispatcher.InvokeAsync(()=>ChangeCore(source,prepare,options,request)).Task.Unwrap();
    }
    private static async Task ChangeCore(Window source,Func<Task<Window>> prepare,NexaOptions options,System.Diagnostics.Stopwatch request)
    {
        if(!pending.Add(source))return;
        var phase=System.Diagnostics.Stopwatch.StartNew();var timings=new Dictionary<string,double>();
        timings["Click to transition start"]=request.Elapsed.TotalMilliseconds;
        Window? target=null;var closed=false;var original=source.Background;var root=source.Content as FrameworkElement;
        void OnClosed(object? s,EventArgs e)=>closed=true;
        source.Closed+=OnClosed;
        var enabled=root?.IsEnabled??true;if(root!=null)root.IsEnabled=false;
        try
        {
            // Fade a frozen source image over an already opaque destination, never the HWND.
            BitmapSource? snapshot=null;
            if(source is HomeWindow && root!=null && NexaMotion.Allowed(options) && root.ActualWidth>0 && root.ActualHeight>0)
            {
                try
                {
                    var dpi=VisualTreeHelper.GetDpi(root);var scale=Math.Min(1,2560/(root.ActualWidth*dpi.DpiScaleX));
                    var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(root.ActualWidth*dpi.DpiScaleX*scale)),Math.Max(1,(int)Math.Ceiling(root.ActualHeight*dpi.DpiScaleY*scale)),dpi.PixelsPerInchX*scale,dpi.PixelsPerInchY*scale,PixelFormats.Pbgra32);
                    bitmap.Render(root);bitmap.Freeze();snapshot=bitmap;
                }
                catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException){snapshot=null;}
            }
            // Keep the complete source opaque throughout preparation.
            if(closed)return;
            timings["Fade out"]=phase.Elapsed.TotalMilliseconds;phase.Restart();
            target=await prepare();timings["Prepare and construct"]=phase.Elapsed.TotalMilliseconds;phase.Restart();
            if(closed){target.Close();return;}
            NexaChrome.TransferBounds(source,target);
            // Render invisibly above the still-opaque source; reveal the complete window in one step.
            target.ShowActivated=true;target.BeginAnimation(Window.OpacityProperty,null);target.Opacity=0;
            target.Loaded+=(_,_)=>{target.BeginAnimation(Window.OpacityProperty,null);target.Opacity=0;};
            var targetRoot=target.Content as FrameworkElement;
            var rendered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            target.ContentRendered+=(_,_)=>rendered.TrySetResult();
            if(targetRoot!=null)targetRoot.IsEnabled=false;
            Application.Current.MainWindow=target;target.Show();
            if(await Task.WhenAny(rendered.Task,Task.Delay(15000))!=rendered.Task)throw new TimeoutException("Space did not finish rendering.");
            await target.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ContextIdle);
            timings["First complete render"]=phase.Elapsed.TotalMilliseconds;
            timings["Interactive readiness"]=request.Elapsed.TotalMilliseconds;phase.Restart();
            target.UpdateLayout();
            Image? cover=null;
            if(snapshot!=null && targetRoot is Grid grid)
            {
                cover=new Image{Source=snapshot,Stretch=Stretch.Fill,IsHitTestVisible=false};
                Grid.SetColumnSpan(cover,Math.Max(1,grid.ColumnDefinitions.Count));Grid.SetRowSpan(cover,Math.Max(1,grid.RowDefinitions.Count));Panel.SetZIndex(cover,int.MaxValue);grid.Children.Add(cover);target.UpdateLayout();
            }
            target.BeginAnimation(Window.OpacityProperty,null);target.Opacity=1;
            if(cover!=null && targetRoot is Grid coverGrid)
            {
                var fade=System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await target.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);
                    cover.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(200)){EasingFunction=new QuadraticEase{EasingMode=EasingMode.EaseInOut}});
                    await Task.Delay(210);
                }
                finally{cover.BeginAnimation(UIElement.OpacityProperty,null);coverGrid.Children.Remove(cover);}
                timings["Crossfade"]=fade.Elapsed.TotalMilliseconds;
            }
            if(targetRoot!=null)targetRoot.IsEnabled=true;
            target.Activate();
            await target.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.Render);
            timings["Input enabled"]=request.Elapsed.TotalMilliseconds;
            timings["Fade in"]=0;LastProfile=timings;
            phase.Restart();
            if(source is MainWindow main)main.CloseAfterNavigation();else source.Close();
            timings["Source window teardown"]=phase.Elapsed.TotalMilliseconds;
            await target.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);
            timings["Click to dispatcher idle"]=request.Elapsed.TotalMilliseconds;
            if(target is MainWindow measured)
            {
                timings["Widget layout total"]=measured.WidgetLayoutMilliseconds;
                timings["Widget layout passes"]=measured.WidgetLayoutPasses;
            }
        }
        catch
        {
            target?.Close();if(!closed){if(source is MainWindow main)main.RestoreAfterNavigationFailure();Application.Current.MainWindow=source;if(root!=null){root.BeginAnimation(UIElement.OpacityProperty,null);root.Opacity=1;}}
            throw;
        }
        finally {source.Closed-=OnClosed;source.Background=original;if(root!=null)root.IsEnabled=enabled;pending.Remove(source);}
    }
}
