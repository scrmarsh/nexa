using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    private bool changingSpace;
    internal Task<bool> ChangeSpace(WorkspaceDocument candidate)
        =>Dispatcher.InvokeAsync(()=>ChangeSpaceCore(candidate)).Task.Unwrap();
    private async Task<bool> ChangeSpaceCore(WorkspaceDocument candidate)
    {
        if(changingSpace)return false;changingSpace=true;
        var root=(FrameworkElement)Content;var enabled=root.IsEnabled;var background=Background;var closed=false;
        void ClosedDuringChange(object? sender,EventArgs e)=>closed=true;
        Closed+=ClosedDuringChange;root.IsEnabled=false;Background=Brushes.Black;
        try
        {
            await NexaMotion.Fade(root,0,Options);if(closed)return false;
            if(!CommitWorkspace(candidate))return false;
            root.UpdateLayout();await Dispatcher.Yield(DispatcherPriority.ContextIdle);if(closed)return false;
            AnimateWidgetEntrance();await NexaMotion.Fade(root,1,Options);return true;
        }
        finally
        {
            Closed-=ClosedDuringChange;root.BeginAnimation(OpacityProperty,null);root.Opacity=1;root.IsEnabled=enabled;Background=background;changingSpace=false;
        }
    }
}
