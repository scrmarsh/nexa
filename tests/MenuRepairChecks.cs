using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckMenuRepair()
    {
        var store = new StateStore(Path.Combine(output, "menu-repair.json"));
        var legacyTasks = WidgetLibrary.Create("Tasks", "tasks"); legacyTasks.Kind = "Tasks";
        legacyTasks.Items.Add(new TaskItem { Text = "Keep developer task" });
        var data = new DashboardData { WidgetLayoutInitialized = true, Widgets = [legacyTasks], Tasks = [new TaskItem { Text = "Keep developer task" }] };
        var developer = Continuity.NewDashboard("Developer mode", "Developer mode", data, store.UserId);
        var workspace = new WorkspaceDocument { UserId = store.UserId, ActiveDashboardId = developer.DashboardId, Dashboards = [developer] };
        store.Save(Continuity.Activate(workspace, store.UserId));
        var home = new HomeWindow(store); home.Show(); Pump(TimeSpan.FromMilliseconds(160));
        var settings = new SettingsDialog(home, store, home.Saved, null, "Appearance"); settings.ShowDismissible(); Pump(TimeSpan.FromMilliseconds(100));
        var click = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
        home.RaiseEvent(click);
        Assert(click.Handled, "Click outside a menu is consumed by the owner");
        Pump(TimeSpan.FromMilliseconds(230));
        Assert(!settings.IsVisible, "Click outside closes the menu");
        var second = new SettingsDialog(home, store, home.Saved, null, "Appearance"); second.ShowDismissible(); Pump(TimeSpan.FromMilliseconds(100));
        typeof(Window).GetMethod("OnActivated", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(home, [EventArgs.Empty]);
        Pump(TimeSpan.FromMilliseconds(230));
        Assert(!second.IsVisible, "Owner activation closes a menu when Windows consumes the first click");
        var parent = new SettingsDialog(home, store, home.Saved, null, "Appearance"); parent.ShowDismissible(); Pump(TimeSpan.FromMilliseconds(100));
        var child = new NexaPanel { Owner = parent, Title = "Nested menu", Width = 400, Height = 280, Content = new TextBlock { Text = "Nested" } };
        child.ShowDismissible(); Pump(TimeSpan.FromMilliseconds(100));
        var nestedClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
        home.RaiseEvent(nestedClick); Pump(TimeSpan.FromMilliseconds(230));
        Assert(nestedClick.Handled && !child.IsVisible && parent.IsVisible, "Outside click closes only the topmost nested menu");
        home.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
        Pump(TimeSpan.FromMilliseconds(230)); Assert(!parent.IsVisible, "Next outside click closes the remaining menu");
        var dev = Descendants(home).OfType<Button>().Single(b => Equals(b.Content, "DEV"));
        dev.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(350));
        var reloaded = store.Load();
        var savedDeveloper = reloaded.Workspace!.Dashboards.Single(d => d.DashboardId == developer.DashboardId);
        Assert(savedDeveloper.Current.Data.Widgets.Count(w => w.PanelKey == "tasks") == 1 && savedDeveloper.Current.Data.Widgets.Any(w => w.Kind == "Task List" && w.Items.Any(i => i.Text == "Keep developer task")), "DEV opens legacy Space without duplicate linked tasks or lost content");
        Assert(Application.Current.MainWindow is MainWindow { IsVisible: true }, "DEV opens its dashboard successfully");
        Application.Current.MainWindow?.Close();
    }
}
