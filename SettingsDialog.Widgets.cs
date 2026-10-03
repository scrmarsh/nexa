using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private Border? hedySettingsSection;
    private UIElement BuildWidgetPreferences()
    {
        var page=Page("Widgets","Global widget behavior. Click widget titles or content to edit each Space directly.");
        page.Children.Add(new TextBlock { Text="Pixel Timer",FontSize=20,FontWeight=FontWeights.SemiBold });
        var shape=new ComboBox { ItemsSource=new[] { "Square","Circle" },SelectedItem=state.PixelShape,MinHeight=40 };
        Field(page,"Pixel shape",shape);
        shape.SelectionChanged+=(_,_)=> { state.PixelShape=shape.SelectedItem?.ToString()??"Square"; SaveInteractionOptions(); space?.RefreshWidgetPreferences(); };
        page.Children.Add(new TextBlock { Text="Focus",FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,24,0,8) });
        var pulse=new CheckBox { Content="Gently pulse the focus backdrop",IsChecked=state.FocusPulseEnabled }; page.Children.Add(pulse);
        var intensity=new Slider { Minimum=.5,Maximum=.95,Value=state.FocusPulseIntensity };
        Field(page,"Backdrop intensity",intensity);
        pulse.Click+=(_,_)=> { state.FocusPulseEnabled=pulse.IsChecked==true; SaveInteractionOptions(); space?.RefreshWidgetPreferences(); };
        intensity.ValueChanged+=(_,_)=> { state.FocusPulseIntensity=intensity.Value; SaveInteractionOptions(); space?.RefreshWidgetPreferences(); };
        page.Children.Add(new TextBlock { Text="Distraction Counter",FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,24,0,8) });
        var escalation=new CheckBox { Content="Playful escalation messages",IsChecked=state.DistractionEscalationEnabled }; page.Children.Add(escalation);
        escalation.Click+=(_,_)=> { state.DistractionEscalationEnabled=escalation.IsChecked==true; SaveInteractionOptions(); space?.UpdateClock(); };
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
    private UIElement BuildSoundTests()
    {
        var page=Page("Developer","Test the existing Nexa completion chime without changing dashboard content.");
        page.Children.Add(new TextBlock { Text="Task completion, widget added, Focus Reset and session completion share this built-in soft chime. Master sounds and volume still apply.",TextWrapping=TextWrapping.Wrap });
        var test=new Button { Content="Play completion chime",Margin=new Thickness(0,18,0,0) };
        test.Click+=(_,_)=>space?.PlayInterfaceSound(); page.Children.Add(test); return page;
    }
}
