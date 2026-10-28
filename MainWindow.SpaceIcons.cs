using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
public partial class MainWindow
{
    private void EditSpaceIcon()
    {
        var dialog=Dialog("nexa · Space icon",420,300);
        var panel=new StackPanel{Margin=new Thickness(24)};
        panel.Children.Add(new TextBlock{Text="Make this Space yours",FontSize=22,Margin=new Thickness(0,0,0,18)});
        var selected=SpaceIcons.Resolve(state.SpaceIcon,Continuity.Current(state.Workspace!,store.UserId).PresetOrigin);
        panel.Children.Add(SpaceIcons.Picker(selected,value=>selected=value));
        var save=new Button{Content="Save icon",Margin=new Thickness(0,18,0,0)};
        save.Click+=(_,_)=>{var previous=state.SpaceIcon;state.SpaceIcon=selected;Changed();if(SaveNow()){RefreshDashboardPicker();dialog.Close();}else state.SpaceIcon=previous;};panel.Children.Add(save);dialog.Content=panel;dialog.ShowDismissible();
    }
}
