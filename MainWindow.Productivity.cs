using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

public partial class MainWindow
{
    private void TrackDirectSquash(BugEntity bug)
    {
        if (ProductivityScoring.DirectSquash(state.Productivity, bug.InteractionId.ToString(), DateTimeOffset.UtcNow,Options.StreakMultiplierCap))
        { if (state.Workspace != null) state.Workspace.AllTimeSquashes++; Changed(); }
    }
    internal void AddSquashesWidget() => AddWidget("Bugs Squashed", null);
    private void AttachTaskPermanence(CheckBox checkbox, TaskItem item)
    {
        checkbox.Checked += (_, _) => NexaMotion.Pop(checkbox, Options);
        var menu = new ContextMenu();
        var keep = new MenuItem { Header = "Permanent task", IsCheckable = true, IsChecked = item.Permanent };
        keep.Click += (_, _) => { item.Permanent = keep.IsChecked; Changed(); };
        menu.Items.Add(keep); checkbox.ContextMenu = menu;
    }
}
