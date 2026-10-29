using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private Border? hedySettingsSection;
    private UIElement BuildWidgetPreferences()
    {
        var page=Page("Widgets","Global widget behavior. Click widget titles or content to edit each Space directly.");
        if(space!=null){var guide=new Button{Content="Replay Quick Start",HorizontalAlignment=HorizontalAlignment.Left};guide.Click+=(_,_)=>{Close();space.StartQuickStart();};page.Children.Add(guide);}
        page.Children.Add(new TextBlock { Text="Pixel Timer",FontSize=20,FontWeight=FontWeights.SemiBold });
        var shape=new ComboBox { ItemsSource=PixelShapes.Names,SelectedItem=state.PixelShape,MinHeight=40 };
        Field(page,"Pixel Timer Shapes",shape);
        var shapePreview=new ContentControl{Content=PixelShapes.Preview(state.PixelShape),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,12,0,0)};page.Children.Add(shapePreview);
        shape.SelectionChanged+=(_,_)=>{state.PixelShape=PixelShapes.Normalize(shape.SelectedItem?.ToString());shapePreview.Content=PixelShapes.Preview(state.PixelShape);space?.RefreshWidgetPreferences();QueueSave("shape",SaveInteractionOptions);};
        page.Children.Add(new TextBlock { Text="Focus",FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,24,0,8) });
        var pulse=new CheckBox { Content="Gently pulse the focus backdrop",IsChecked=state.FocusPulseEnabled }; page.Children.Add(pulse);
        var intensity=new Slider { Minimum=.5,Maximum=.95,Value=state.FocusPulseIntensity };
        Field(page,"Backdrop intensity",intensity);
        pulse.Click+=(_,_)=> QueueSave("pulse", () => { state.FocusPulseEnabled=pulse.IsChecked==true; SaveInteractionOptions(); space?.RefreshWidgetPreferences(); });
        intensity.ValueChanged+=(_,_)=> QueueSave("intensity", () => { state.FocusPulseIntensity=intensity.Value; SaveInteractionOptions(); space?.RefreshWidgetPreferences(); });
        page.Children.Add(new TextBlock { Text="Distraction Counter",FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,24,0,8) });
        var escalation=new CheckBox { Content="Playful escalation messages",IsChecked=state.DistractionEscalationEnabled }; page.Children.Add(escalation);
        escalation.Click+=(_,_)=> QueueSave("escalation", () => { state.DistractionEscalationEnabled=escalation.IsChecked==true; SaveInteractionOptions(); space?.UpdateClock(); });
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
}
