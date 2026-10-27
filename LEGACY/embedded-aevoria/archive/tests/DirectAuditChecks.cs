using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckDirectAudit()
    {
        void Tap(DependencyObject root,string label)=>Descendants(root).OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        void MouseTap(UIElement target)=>target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left) { RoutedEvent=UIElement.MouseLeftButtonUpEvent });
        IEnumerable<DependencyObject> Visuals(DependencyObject root)
        {
            yield return root;
            for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) foreach(var child in Visuals(VisualTreeHelper.GetChild(root,i))) yield return child;
        }
        var store=new StateStore(Path.Combine(output,"direct-audit.json")); var now=DateTimeOffset.Now;
        store.Save(new AppState { WidgetLayoutInitialized=true,Notes="Preserve class notes",Session=new Session { Title="Direct editing",Start=now,End=now.AddMinutes(25),IntervalMinutes=5,ConfiguredDurationMinutes=25,HedyTranscript="Preserve local transcript" },Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),WidgetLibrary.Create("Notes","notes"),WidgetLibrary.Create("Clock")] });
        var window=new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var current=window.state.Session!; var sessionId=Continuity.Current(window.state.Workspace!,store.UserId).Current.SessionId;
        MouseTap(window.Countdown); Pump(TimeSpan.FromMilliseconds(100));
        Assert(window.OwnedWindows.OfType<Window>().Any(w=>w.Title.EndsWith("Edit session")) && window.StartDate.IsVisible,"Clicking the banner countdown opens direct time editing");
        window.StartDate.SelectedDate=window.EndDate.SelectedDate=DateTime.Today.AddDays(1); window.StartTime.Text="10:00 AM"; window.EndTime.Text="10:40 AM";
        Tap(window.InlineSessionHost,"Apply times"); Pump(TimeSpan.FromMilliseconds(100));
        Assert(ReferenceEquals(current,window.state.Session) && current.End-current.Start==TimeSpan.FromMinutes(40) && Continuity.Current(window.state.Workspace!,store.UserId).Current.SessionId==sessionId,"Direct time edits keep the session identity and selected duration");
        Assert(window.state.Notes=="Preserve class notes" && current.HedyTranscript=="Preserve local transcript","Direct time edits preserve notes and local transcript");
        window.SessionResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(100));
        var reset=window.OwnedWindows.OfType<Window>().Single(w=>w.Title.Contains("Reset timer")); Tap(reset,"Reset"); Pump(TimeSpan.FromMilliseconds(220));
        Assert(current.IsRunning(DateTimeOffset.Now) && current.End-current.Start==TimeSpan.FromMinutes(40) && window.state.Notes=="Preserve class notes","Session Reset restores edited duration without clearing data");
        var notes=window.state.Widgets.Single(w=>w.Kind=="Notes");
        var notesFrame=window.WidgetCanvas.Children.OfType<Border>().Single(b=>Equals(b.Tag,notes.Id));
        Descendants(notesFrame).OfType<TextBox>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t).StartsWith("Widget title")).Text="Lecture notes"; window.SaveNow();
        Assert(store.Load().Widgets.Single(w=>w.Id==notes.Id).Title=="Lecture notes","Direct widget title editing saves");
        var clock=window.state.Widgets.Single(w=>w.Kind=="Clock"); var clockFrame=window.WidgetCanvas.Children.OfType<Border>().Single(b=>Equals(b.Tag,clock.Id));
        var clockText=Descendants(clockFrame).OfType<TextBlock>().Single(t=>Equals(t.ToolTip,"Click to configure clock")); MouseTap(clockText); Pump(TimeSpan.FromMilliseconds(80));
        var menu=PresentationSource.CurrentSources.Cast<PresentationSource>().Where(s=>s.RootVisual!=null).SelectMany(s=>Visuals(s.RootVisual!)).OfType<ContextMenu>().Single(m=>m.IsOpen);
        Assert(menu.Items.OfType<MenuItem>().Any(i=>Equals(i.Header,"Show seconds")),"Clicking the clock opens its small settings menu");
        var seconds=menu.Items.OfType<MenuItem>().Single(i=>Equals(i.Header,"Show seconds")); seconds.IsChecked=false; seconds.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); menu.IsOpen=false;
        Assert(!clock.ShowSeconds && !store.Load().Widgets.Single(w=>w.Id==clock.Id).ShowSeconds,"Clock's direct seconds toggle persists");
        store.SaveInteractionPreferences(new DashboardData { InterfaceSoundsEnabled=true,FocusNudgesEnabled=true,InterfaceSoundVolume=.15 });
        var candidate=window.CopyWorkspaceState().Workspace!; var another=Continuity.NewDashboard("Other space","Study",new DashboardData(),store.UserId); candidate.Dashboards.Add(another); candidate.ActiveDashboardId=another.DashboardId;
        Assert(window.CommitWorkspace(candidate) && window.state.InterfaceSoundsEnabled && window.state.FocusNudgesEnabled && Math.Abs(window.state.InterfaceSoundVolume-.15)<.001,"Space switching retains global sound, volume and nudge preferences");
        candidate=window.CopyWorkspaceState().Workspace!; candidate.ActiveDashboardId=candidate.Dashboards.Single(d=>d.DashboardId!=another.DashboardId).DashboardId; Assert(window.CommitWorkspace(candidate),"Switching back preserves original space");
        window.Close(); var reopened=new MainWindow(store); reopened.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(reopened.state.Session!.End-reopened.state.Session.Start==TimeSpan.FromMinutes(40) && reopened.state.Notes=="Preserve class notes" && !reopened.state.Widgets.Single(w=>w.Id==clock.Id).ShowSeconds,"Direct timer and clock changes survive reopen"); reopened.Close();
    }
}
