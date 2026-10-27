using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SessionDashboard;

// Combobox wheel events use one discrete step per notch. Unfocused controls give
// the gesture to their surrounding page, rather than silently editing a setting.
public static class NexaSelection
{
    public static readonly DependencyProperty ControlledWheelProperty=DependencyProperty.RegisterAttached("ControlledWheel",typeof(bool),typeof(NexaSelection),new PropertyMetadata(false,Changed));
    private static readonly DependencyProperty RemainderProperty=DependencyProperty.RegisterAttached("Remainder",typeof(int),typeof(NexaSelection),new PropertyMetadata(0));
    public static void SetControlledWheel(DependencyObject target,bool value)=>target.SetValue(ControlledWheelProperty,value);
    public static bool GetControlledWheel(DependencyObject target)=>(bool)target.GetValue(ControlledWheelProperty);
    private static void Changed(DependencyObject target,DependencyPropertyChangedEventArgs e)
    {if(target is ComboBox box){if((bool)e.OldValue)box.PreviewMouseWheel-=Wheel;if((bool)e.NewValue)box.PreviewMouseWheel+=Wheel;}}
    internal static int Accumulate(int remainder,int delta,out int steps)
    {var total=remainder+delta;steps=Math.Clamp(total/120,-1,1);return Math.Abs(total)>=120?total%120:total;}
    private static void Wheel(object sender,MouseWheelEventArgs e)
    {
        var box=(ComboBox)sender;e.Handled=true;
        if(!box.IsDropDownOpen && !box.IsKeyboardFocusWithin)
        {
            box.SetValue(RemainderProperty,0);
            for(DependencyObject? p=box;p!=null;p=p is Visual?VisualTreeHelper.GetParent(p):LogicalTreeHelper.GetParent(p))
                if(p is ScrollViewer scroll){scroll.ScrollToVerticalOffset(scroll.VerticalOffset-e.Delta/120d*36);break;}
            return;
        }
        var remainder=Accumulate((int)box.GetValue(RemainderProperty),e.Delta,out var steps);box.SetValue(RemainderProperty,remainder);
        if(steps==0||box.Items.Count==0)return;
        box.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty,Math.Clamp(box.SelectedIndex-steps,0,box.Items.Count-1));
        if(box.ItemContainerGenerator.ContainerFromIndex(box.SelectedIndex) is FrameworkElement item)item.BringIntoView();
    }
}
