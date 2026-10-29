using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

// WPF wheel events do not reliably identify a precision pad. Preserve their delta
// magnitude instead of turning every small event into a complete wheel notch.
internal static class NexaScrolling
{
    private static bool initialized;
    private sealed class Pending {internal double Target;internal int Direction;internal bool Queued;}
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<DependencyObject,Pending> pending=new();
    private static readonly DependencyProperty RemainderProperty=DependencyProperty.RegisterAttached("WheelRemainder",typeof(double),typeof(NexaScrolling),new PropertyMetadata(0d));
    internal static void Initialize()
    {
        if(initialized)return;initialized=true;
        // Coerce visible bars at the viewer, including viewers inside control templates.
        // Hidden retains the scroll extent, keyboard, wheel and panning behavior.
        object Hide(DependencyObject owner,object value)=>value is ScrollBarVisibility.Auto or ScrollBarVisibility.Visible?ScrollBarVisibility.Hidden:value;
        ScrollViewer.VerticalScrollBarVisibilityProperty.OverrideMetadata(typeof(ScrollViewer),new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden,null,Hide));
        ScrollViewer.HorizontalScrollBarVisibilityProperty.OverrideMetadata(typeof(ScrollViewer),new FrameworkPropertyMetadata(ScrollBarVisibility.Hidden,null,Hide));
        EventManager.RegisterClassHandler(typeof(Window),Mouse.PreviewMouseWheelEvent,new MouseWheelEventHandler(Wheel));
        EventManager.RegisterClassHandler(typeof(ScrollViewer),Mouse.PreviewMouseWheelEvent,new MouseWheelEventHandler(Wheel));
    }
    private static void Wheel(object sender,MouseWheelEventArgs e)
    {
        if(e.Handled || (Keyboard.Modifiers&ModifierKeys.Shift)!=0)return;
        if(ScrollFrom(e.OriginalSource as DependencyObject,e.Delta))e.Handled=true;
    }
    internal static DependencyObject? Parent(DependencyObject node)=>node switch
    {
        ContentElement content=>ContentOperations.GetParent(content)??(content as FrameworkContentElement)?.Parent,
        Visual or System.Windows.Media.Media3D.Visual3D=>VisualTreeHelper.GetParent(node),
        _=>LogicalTreeHelper.GetParent(node)
    };
    internal static double Distance(int delta,int lines,double lineHeight,double viewport)
    {
        if(delta==0 || lines==0 || viewport<=0)return 0;
        var step=lines<0?viewport:lines*Math.Max(1,lineHeight);
        return Math.Clamp(delta/120d*step,-viewport,viewport);
    }
    private static double Starting(DependencyObject owner,double actual,int delta)
    {
        var state=pending.GetOrCreateValue(owner);
        return state.Queued && state.Direction==Math.Sign(delta)?state.Target:actual;
    }
    private static void Request(DependencyObject owner,double target,int delta,Action<double> scroll)
    {
        var state=pending.GetOrCreateValue(owner);state.Target=target;state.Direction=Math.Sign(delta);
        scroll(target);
        if(state.Queued)return;
        state.Queued=true;
        // Merge only commands waiting on WPF's next layout pass. One callback per
        // batch, no animation, momentum or per-event dispatcher queue.
        owner.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(()=>state.Queued=false));
    }
    internal static bool ScrollFrom(DependencyObject? source,int delta)
    {
        // Keep focused, closed selectors' discrete value stepping. Open popup lists
        // own their scrolling; scrolling a closed unfocused selector moves its page.
        for(var node=source;node!=null;node=Parent(node))
            if(node is ComboBox box && box.IsKeyboardFocusWithin && !box.IsDropDownOpen)return false;
        for(var node=source;node!=null;node=Parent(node))
        {
            if(node is BugOverlay {ScrollTarget:{} target} && Scroll(target,delta))return true;
            if(node is ScrollViewer scroll && Scroll(scroll,delta))return true;
            if(node is TextBoxBase editor && (delta<0?editor.VerticalOffset+editor.ViewportHeight<editor.ExtentHeight-.01:editor.VerticalOffset>.01))
            {Request(editor,Math.Clamp(Starting(editor,editor.VerticalOffset,delta)-Distance(delta,SystemParameters.WheelScrollLines,editor.FontSize*1.2,editor.ViewportHeight),0,Math.Max(0,editor.ExtentHeight-editor.ViewportHeight)),delta,editor.ScrollToVerticalOffset);return true;}
        }
        return false;
    }
    private static bool Logical(ScrollViewer scroll)
    {
        if(!scroll.CanContentScroll)return false;
        if(scroll.Content is StackPanel or VirtualizingStackPanel)return true;
        if(scroll.Content is ItemsPresenter items && VisualTreeHelper.GetChildrenCount(items)>0)
            return VisualTreeHelper.GetChild(items,0) is StackPanel or VirtualizingStackPanel;
        return false;
    }
    internal static bool Scroll(ScrollViewer scroll,int delta)
    {
        if(delta==0 || scroll.VerticalScrollBarVisibility==ScrollBarVisibility.Disabled || scroll.ScrollableHeight<=0)return false;
        var start=Starting(scroll,scroll.VerticalOffset,delta);
        if(delta<0?start>=scroll.ScrollableHeight-.01:start<=.01)
        {scroll.SetValue(RemainderProperty,0d);return false;}
        var font=TextElement.GetFontSize(scroll);
        var logical=Logical(scroll);
        var amount=Distance(delta,SystemParameters.WheelScrollLines,logical?1:font*1.2,scroll.ViewportHeight);
        if(amount==0)return true; // The system's zero-line setting means no wheel movement.
        if(logical)
        {
            var remainder=(double)scroll.GetValue(RemainderProperty);
            if(Math.Sign(amount)!=Math.Sign(remainder))remainder=0;
            var total=remainder+amount;amount=Math.Truncate(total);scroll.SetValue(RemainderProperty,total-amount);
            if(amount==0)return true;
        }
        Request(scroll,Math.Clamp(start-amount,0,scroll.ScrollableHeight),delta,scroll.ScrollToVerticalOffset);
        return true;
    }
}
