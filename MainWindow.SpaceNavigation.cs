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
        Closed+=ClosedDuringChange;root.IsEnabled=false;
        System.Windows.Controls.Image? cover=null;
        try
        {
        if(root is System.Windows.Controls.Grid grid && root.ActualWidth>0 && root.ActualHeight>0)
        {
            var dpi=VisualTreeHelper.GetDpi(root);var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(root.ActualWidth*dpi.DpiScaleX)),Math.Max(1,(int)Math.Ceiling(root.ActualHeight*dpi.DpiScaleY)),dpi.PixelsPerInchX,dpi.PixelsPerInchY,PixelFormats.Pbgra32);
            bitmap.Render(root);bitmap.Freeze();cover=new System.Windows.Controls.Image{Source=bitmap,Stretch=Stretch.Fill,IsHitTestVisible=false};
            System.Windows.Controls.Grid.SetRowSpan(cover,Math.Max(1,grid.RowDefinitions.Count));System.Windows.Controls.Grid.SetColumnSpan(cover,Math.Max(1,grid.ColumnDefinitions.Count));System.Windows.Controls.Panel.SetZIndex(cover,int.MaxValue);grid.Children.Add(cover);
        }
            if(closed)return false;
            if(!CommitWorkspace(candidate,sessionLoad:true))return false;
            root.UpdateLayout();await Dispatcher.Yield(DispatcherPriority.ContextIdle);if(closed)return false;
            return true;
        }
        finally
        {
            if(cover!=null && root is System.Windows.Controls.Grid coverGrid)coverGrid.Children.Remove(cover);
            Closed-=ClosedDuringChange;root.BeginAnimation(OpacityProperty,null);root.Opacity=1;root.IsEnabled=enabled;Background=background;changingSpace=false;
        }
    }
}
