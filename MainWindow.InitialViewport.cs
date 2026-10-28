using System.Windows;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    private bool initialViewportPending;
    internal void InitializeInitialViewport()
    {
        initialViewportPending=state.InitialViewPending;
        if(!initialViewportPending)return;
        // TextBox/keyboard focus may request the last canvas child before first layout.
        WidgetScroller.RequestBringIntoView+=SuppressInitialBringIntoView;
        ContentRendered+=SetInitialViewport;
    }
    private void SuppressInitialBringIntoView(object sender,RequestBringIntoViewEventArgs e)
    {if(initialViewportPending)e.Handled=true;}
    private void SetInitialViewport(object? sender,EventArgs e)
    {
        ContentRendered-=SetInitialViewport;
        Dispatcher.BeginInvoke(new Action(()=>{
            if(!initialViewportPending)return;
            WidgetScroller.ScrollToTop();initialViewportPending=false;state.InitialViewPending=false;
            WidgetScroller.RequestBringIntoView-=SuppressInitialBringIntoView;Changed();
        }),DispatcherPriority.ContextIdle);
    }
}
