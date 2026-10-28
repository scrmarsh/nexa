using System.Windows;
namespace SessionDashboard;
public partial class MainWindow
{
    internal void EditSpaceIcon()
    {
        var selected=SpaceIcons.Resolve(state.SpaceIcon,Continuity.Current(state.Workspace!,store.UserId).PresetOrigin);
        new SpaceIconChooser(this,selected,value=>
        {
            var previous=state.SpaceIcon;state.SpaceIcon=value;Changed();
            if(!SaveNow()){state.SpaceIcon=previous;return false;}
            RefreshDashboardPicker();
            foreach(var home in Application.Current.Windows.OfType<HomeWindow>())home.RefreshSpaceCards(store);
            return true;
        }).ShowDismissible();
    }
}
