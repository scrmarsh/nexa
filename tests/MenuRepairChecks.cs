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
        var originalStart=DateTimeOffset.UtcNow.AddMinutes(-2);var originalEnd=originalStart.AddHours(1);
        var data = new DashboardData { Session=new Session{Title="Existing DEV session",Start=originalStart,End=originalEnd}, WidgetLayoutInitialized = true, Widgets = [legacyTasks], Tasks = [new TaskItem { Text = "Keep developer task" }] };
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
        Console.WriteLine($"Nested menu before outside click: parent visible={parent.IsVisible}, child visible={child.IsVisible}");
        var nestedClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent };
        home.RaiseEvent(nestedClick); Pump(TimeSpan.FromMilliseconds(230));
        Console.WriteLine($"Nested menu after outside click: handled={nestedClick.Handled}, parent visible={parent.IsVisible}, child visible={child.IsVisible}, parent closing={parent.DismissalInProgress}, child closing={child.DismissalInProgress}");
        Assert(nestedClick.Handled && !child.IsVisible && parent.IsVisible, "Outside click closes only the topmost nested menu");
        Console.WriteLine("Closing remaining parent; physical button state="+Mouse.LeftButton);
        home.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
        Console.WriteLine("Remaining parent outside-click handler returned");
        Pump(TimeSpan.FromMilliseconds(230)); Assert(!parent.IsVisible, "Next outside click closes the remaining menu");
        var originalBounds=new Rect(home.Left,home.Top,home.ActualWidth,home.ActualHeight);
        var dev = Descendants(home).OfType<Button>().Single(b => Equals(b.Content, "DEV"));
        dev.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var navigationWatch=System.Diagnostics.Stopwatch.StartNew();while(Application.Current.MainWindow is not MainWindow && navigationWatch.Elapsed<TimeSpan.FromSeconds(10))Pump(TimeSpan.FromMilliseconds(20));
        var reloaded = store.Load();
        var savedDeveloper = reloaded.Workspace!.Dashboards.Single(d => d.DashboardId == developer.DashboardId);
        Assert(savedDeveloper.Current.Data.Widgets.Count(w => w.PanelKey == "tasks") == 1 && savedDeveloper.Current.Data.Widgets.Any(w => w.Kind == "Task List" && w.Items.Any(i => i.Text == "Keep developer task")), "DEV opens legacy Space without duplicate linked tasks or lost content");
        Assert(Application.Current.MainWindow is MainWindow { IsVisible: true }, "DEV opens its dashboard successfully");
        Assert(savedDeveloper.Current.Data.Session?.Start==originalStart && savedDeveloper.Current.Data.Session?.End==originalEnd,"DEV navigation preserves its existing scheduled timer");
        var opened=Application.Current.MainWindow!;Assert(Math.Abs(opened.ActualWidth-originalBounds.Width)<2 && Math.Abs(opened.ActualHeight-originalBounds.Height)<2 && Math.Abs(opened.Left-originalBounds.Left)<2 && Math.Abs(opened.Top-originalBounds.Top)<2,"DEV navigation preserves outer window bounds");
        Application.Current.MainWindow?.Close();
    }
}
