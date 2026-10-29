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
        if((Keyboard.Modifiers&ModifierKeys.Shift)!=0)return;
        var box=(ComboBox)sender;e.Handled=true;
        if(!box.IsDropDownOpen && !box.IsKeyboardFocusWithin)
        {
            box.SetValue(RemainderProperty,0);
            NexaScrolling.ScrollFrom(box,e.Delta);
            return;
        }
        if(box.IsDropDownOpen)
        {
            if(box.Template.FindName("PART_Popup",box) is System.Windows.Controls.Primitives.Popup popup && popup.Child is DependencyObject content)
            {
                ScrollViewer? Find(DependencyObject node){if(node is ScrollViewer scroll)return scroll;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++){var found=Find(VisualTreeHelper.GetChild(node,i));if(found!=null)return found;}return null;}
                if(Find(content) is {} scroll)NexaScrolling.Scroll(scroll,e.Delta);
            }
            return;
        }
        var remainder=Accumulate((int)box.GetValue(RemainderProperty),e.Delta,out var steps);box.SetValue(RemainderProperty,remainder);
        if(steps==0||box.Items.Count==0)return;
        box.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedIndexProperty,Math.Clamp(box.SelectedIndex-steps,0,box.Items.Count-1));
        if(box.ItemContainerGenerator.ContainerFromIndex(box.SelectedIndex) is FrameworkElement item)item.BringIntoView();
    }
}
