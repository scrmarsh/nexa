using System.Windows;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    private bool initialViewportPending;
    internal void InitializeInitialViewport()
    {
        WidgetScroller.RequestBringIntoView-=SuppressInitialBringIntoView;
        ContentRendered-=SetInitialViewport;
        initialViewportPending=state.InitialViewPending;
        if(!initialViewportPending)return;
        // TextBox/keyboard focus may request the last canvas child before first layout.
        WidgetScroller.RequestBringIntoView+=SuppressInitialBringIntoView;
        if(IsLoaded)SetInitialViewport(this,EventArgs.Empty);else ContentRendered+=SetInitialViewport;
    }
    private void SuppressInitialBringIntoView(object sender,RequestBringIntoViewEventArgs e)
    {if(initialViewportPending)e.Handled=true;}
    private void SetInitialViewport(object? sender,EventArgs e)
    {
        ContentRendered-=SetInitialViewport;
        var opening=state;
        Dispatcher.BeginInvoke(new Action(()=>{
            if(!initialViewportPending || !ReferenceEquals(opening,state))return;
            WidgetScroller.UpdateLayout();
            WidgetScroller.ScrollToTop();initialViewportPending=false;state.InitialViewPending=false;
            WidgetScroller.RequestBringIntoView-=SuppressInitialBringIntoView;Changed();
        }),DispatcherPriority.ContextIdle);
    }
}
