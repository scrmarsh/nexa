using System.Windows;
using System.Windows.Input;

namespace SessionDashboard;
public partial class MainWindow
{
    private Point? webSweepStart,webSweepLast;
    private bool webSweepMoved;
    private bool TryStartWebSweep(MouseButtonEventArgs e)
    {
        if(bugsOverlay?.Manager is not { IsArcade:true } manager || manager.Webs.Count==0)return false;
        webSweepStart=webSweepLast=bugsOverlay!.ScreenToWorld(e.GetPosition(bugsOverlay!));webSweepMoved=false;
        WidgetCanvas.CaptureMouse();e.Handled=true;return true;
    }
    private void InitializeWebSweep()
    {
        WidgetCanvas.MouseMove+=(_,e)=>
        {
            if(webSweepStart is not {} start || e.LeftButton!=MouseButtonState.Pressed)return;
            var p=bugsOverlay!.ScreenToWorld(e.GetPosition(bugsOverlay!));
            if(!webSweepMoved && (p-start).Length<SystemParameters.MinimumHorizontalDragDistance)return;
            webSweepMoved=true;bugsOverlay?.Manager.CutWebs(webSweepLast??start,p);webSweepLast=p;
            bugsOverlay?.RefreshFrame();e.Handled=true;
        };
        WidgetCanvas.MouseLeftButtonUp+=(_,e)=>
        {
            if(webSweepStart is not {} start)return;
            var moved=webSweepMoved;webSweepStart=webSweepLast=null;WidgetCanvas.ReleaseMouseCapture();e.Handled=true;
            if(!moved) { addWidgetLocation=e.GetPosition(WidgetCanvas);AddWidgetClick(WidgetCanvas,new RoutedEventArgs()); }
        };
        WidgetCanvas.LostMouseCapture+=(_,_)=>webSweepStart=webSweepLast=null;
    }
}
