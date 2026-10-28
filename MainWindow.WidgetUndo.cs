using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow
{
    private sealed record RemovedWidget(WidgetInstance Widget,int Index,Guid DashboardId);
    private readonly Stack<RemovedWidget> widgetUndo=[];
    private readonly Stack<RemovedWidget> widgetRedo=[];
    private readonly DispatcherTimer widgetToastTimer=new() { Interval=TimeSpan.FromSeconds(10) };
    private Border? widgetToast;
    private void ShowWidgetRemovedToast()
    {
        if(Content is not Grid root) return;
        widgetToastTimer.Stop();
        if(widgetToast!=null) root.Children.Remove(widgetToast);
        var row=new StackPanel { Orientation=Orientation.Horizontal };
        row.Children.Add(new TextBlock { Text="Widget removed",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,16,0) });
        var undo=new Button { Content="Undo" }; undo.Click+=(_,_)=>UndoWidgetRemoval(); row.Children.Add(undo);
        var content=new StackPanel(); content.Children.Add(row); content.Children.Add(new TextBlock { Text="Ctrl+Z",FontSize=9,Opacity=.55,Margin=new Thickness(0,3,0,0) });
        widgetToast=new Border { Child=content,Background=DashboardBrush("SurfaceBrush"),BorderBrush=DashboardBrush("LineBrush"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Padding=new Thickness(14,10,14,8),Margin=fullscreen?new Thickness(8):new Thickness(24,0,0,36),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom };
        root.Children.Add(widgetToast); Panel.SetZIndex(widgetToast,3500);
        widgetToastTimer.Tick-=FadeWidgetToast; widgetToastTimer.Tick+=FadeWidgetToast; widgetToastTimer.Start();
    }
    private void FadeWidgetToast(object? sender,EventArgs e)
    {
        widgetToastTimer.Stop(); if(widgetToast==null || Content is not Grid root) return;
        var toast=widgetToast; var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(450));
        fade.Completed+=(_,_)=> { root.Children.Remove(toast); if(ReferenceEquals(widgetToast,toast)) widgetToast=null; };
        toast.BeginAnimation(OpacityProperty,fade);
    }
    internal bool UndoWidgetRemoval()
    {
        if(!widgetUndo.TryPeek(out var action) || action.DashboardId!=state.Workspace?.ActiveDashboardId || state.Widgets.Any(w=>w.Id==action.Widget.Id)) return false;
        var restored=Continuity.Clone(action.Widget);
        if(restored.PanelKey.Length>0 && state.Widgets.Any(w=>w.PanelKey==restored.PanelKey))
        {
            if(restored.PanelKey=="notes") restored.Text=state.Notes;
            if(restored.PanelKey=="tasks") restored.Items=Continuity.Clone(state.Tasks);
            restored.PanelKey="";
        }
        state.Widgets.Insert(Math.Clamp(action.Index,0,state.Widgets.Count),restored); Changed(); RenderWidgets();
        if(!SaveNow()) { state.Widgets.Remove(restored); RenderWidgets(); return false; }
        widgetUndo.Pop(); widgetRedo.Push(action); FadeWidgetToast(null,EventArgs.Empty); return true;
    }
    internal bool RedoWidgetRemoval()
    {
        if(!widgetRedo.TryPeek(out var action) || action.DashboardId!=state.Workspace?.ActiveDashboardId) return false;
        var widget=state.Widgets.SingleOrDefault(w=>w.Id==action.Widget.Id); if(widget==null) return false;
        if (widget.Kind == "Audio Recorder" && state.Widgets.Count(w => w.Kind == "Audio Recorder") == 1 && !StopLocalRecording(true)) return false;
        var index=state.Widgets.IndexOf(widget); state.Widgets.Remove(widget); Changed(); RenderWidgets();
        if(!SaveNow()) { state.Widgets.Insert(index,widget); RenderWidgets(); return false; }
        widgetRedo.Pop(); widgetUndo.Push(action with { Widget=Continuity.Clone(widget),Index=index }); ShowWidgetRemovedToast(); return true;
    }
    private bool HandleWidgetUndoKey(KeyEventArgs e)
    {
        var handled = HandleWidgetShortcut(e.Key, Keyboard.Modifiers, Keyboard.FocusedElement);
        if(handled) e.Handled=true; return handled;
    }
    internal bool HandleWidgetShortcut(Key key, ModifierKeys modifiers, IInputElement? focused)
    {
        if(key != Key.Z || !modifiers.HasFlag(ModifierKeys.Control) || modifiers.HasFlag(ModifierKeys.Alt)) return false;
        var redo = modifiers.HasFlag(ModifierKeys.Shift);
        if(focused is TextBoxBase text && (redo ? text.CanRedo : text.CanUndo)) return false;
        return redo ? RedoWidgetRemoval() : UndoWidgetRemoval();
    }
}
