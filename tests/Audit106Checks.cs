using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckAudit106()
    {
        void Tap(DependencyObject root,string label)=>Descendants(root).OfType<Button>().Single(b=>Equals(b.Content,label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var store=new StateStore(Path.Combine(output,"audit106","state.json"));
        var welcome=new OnboardingWindow(store); var timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(80) }; var stage=0;
        timer.Tick+=(_,_)=>
        {
            switch(stage++)
            {
                case 0: Descendants(welcome).OfType<TextBox>().Single().Text="Audit student"; Tap(welcome,"Continue"); break;
                case 1: welcome.Profile.UseCases=["Classes"]; Tap(welcome,"Continue"); break;
                case 2: Tap(welcome,"Continue"); break;
                case 3:
                    var clock=Descendants(welcome).OfType<CheckBox>().Single(c=>Equals(c.Content,"Clock")); clock.IsChecked=false; clock.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
                    var focus=Descendants(welcome).OfType<CheckBox>().Single(c=>Equals(c.Content,"Focus Reset")); focus.IsChecked=true; focus.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent)); Tap(welcome,"Continue"); break;
                case 4: Tap(welcome,"Back"); Assert(welcome.Step==3 && Descendants(welcome).OfType<CheckBox>().Single(c=>Equals(c.Content,"Focus Reset")).IsChecked==true,"Back retains custom recommended tool selections"); Tap(welcome,"Continue"); break;
                case 5: welcome.Profile.Theme=AppTheme.Aurora; welcome.Profile.Sounds=false; Tap(welcome,"Continue"); break;
                case 6: timer.Stop(); Tap(welcome,"Create my space"); break;
            }
        };
        welcome.Loaded+=(_,_)=>timer.Start(); Assert(welcome.ShowDialog()==true,"Back navigation still creates the selected space"); timer.Stop();
        Assert(welcome.Result!.Current.Data.Widgets.Any(w=>w.Kind=="Focus Reset") && welcome.Result.Current.Data.Widgets.All(w=>w.Kind!="Clock"),"User can add and remove recommendations");
        var workspace=new WorkspaceDocument { UserId=store.UserId,ActiveDashboardId=welcome.Result.DashboardId,Dashboards=[welcome.Result] };
        store.Save(Continuity.Activate(workspace,store.UserId)); store.SaveProfile(welcome.Profile);
        Assert(store.LoadProfile().DisplayName=="Audit student" && store.LoadAppTheme()==AppTheme.Aurora && !store.LoadProfile().Sounds,"Wizard name, theme and sounds persist");
        var window=new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(120));
        var settings=new SettingsDialog(window,store,window.state,window,"Profile"); settings.Show(); Pump(TimeSpan.FromMilliseconds(80));
        var boxes=Descendants(settings).OfType<TextBox>().ToArray(); boxes.Single(b=>b.MaxLength==80).Text="Changed student"; boxes.Single(b=>b.MaxLength==5).Text="40";
        Descendants(settings).OfType<ComboBox>().Single(c=>c.ItemsSource==GuidedSetup.Types).SelectedItem="Study";
        var timed=Descendants(settings).OfType<CheckBox>().Single(c=>Equals(c.Content,"Start guided spaces with a timed session")); timed.IsChecked=false;
        Tap(settings,"Save profile"); settings.Close();
        var changed=store.LoadProfile(); Assert(changed.DisplayName=="Changed student" && changed.DefaultMinutes==40 && changed.PreferredDashboard=="Study" && !changed.TimedSessions,"Profile controls save and reload editable preferences");
        Assert(window.state.Notes=="" && store.Load().Workspace!.Dashboards.Count==1,"Profile changes preserve existing dashboard"); window.Close();
        var skipStore=new StateStore(Path.Combine(output,"audit-skip","state.json")); var skip=new OnboardingWindow(skipStore);
        var skipTimer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(80) }; skipTimer.Tick+=(_,_)=> { skipTimer.Stop(); Tap(skip,"Skip setup for now"); }; skip.Loaded+=(_,_)=>skipTimer.Start();
        Assert(skip.ShowDialog()==false && skipStore.LoadProfile().OnboardingCompleted && !File.Exists(skipStore.FilePath),"Skipping persists completion without creating or replacing a workspace");

        var handler=new IntegrationHandler(); using var http=new HttpClient(handler); var client=new IntegrationClient(store,http); var vault=client.Vault;
        vault.Write("Todoist",new IntegrationCredential { AccessToken="synthetic-audit-token" });
        var today=DateOnly.FromDateTime(DateTime.Now);
        string TaskReply(string title)=>JsonSerializer.Serialize(new { results=new[] { new { id="today",content=title,due=new { date=today.ToString("yyyy-MM-dd") } }, new { id="future",content="Future task",due=new { date=today.AddDays(1).ToString("yyyy-MM-dd") } } } });
        handler.Reply=r=>r.RequestUri!.AbsolutePath.EndsWith("projects")?"{\"results\":[{\"id\":\"p1\",\"name\":\"First account\"}]}":r.Method==HttpMethod.Get?TaskReply("Today task"):"{}";
        var externalState=store.Load(); externalState.Widgets=[WidgetLibrary.Create("Todoist")]; externalState.WidgetLayoutInitialized=true; store.Save(externalState);
        var external=new MainWindow(store); typeof(MainWindow).GetField("integrationClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(external,client); external.RenderWidgets(); external.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var frame=external.WidgetCanvas.Children.OfType<Border>().Single(b=>b.Tag is Guid);
        ComboBox Filter()=>Descendants(frame).OfType<ComboBox>().Single(c=>c.DisplayMemberPath!="Name");
        ComboBox Lists()=>Descendants(frame).OfType<ComboBox>().Single(c=>c.DisplayMemberPath=="Name");
        bool TaskVisible(string label)=>Descendants(frame).OfType<CheckBox>().Any(c=>c.Content.ToString()!.StartsWith(label));
        Assert(TaskVisible("Today task") && !TaskVisible("Future task"),"Task widget Today view filters dates");
        Filter().SelectedItem="Upcoming"; Assert(!TaskVisible("Today task") && TaskVisible("Future task"),"Task widget Upcoming view filters future tasks");
        Filter().SelectedItem="All"; Assert(TaskVisible("Today task") && TaskVisible("Future task"),"All view includes both dates");
        handler.Offline=true; Tap(frame,"Refresh"); Pump(TimeSpan.FromMilliseconds(80)); Filter().SelectedItem="Today";
        Assert(!TaskVisible("Today task") && Lists().Items.Count==0,"Failed refresh and filter changes cannot resurrect cached tasks or account lists"); handler.Offline=false;
        Tap(frame,"Refresh"); Pump(TimeSpan.FromMilliseconds(80));
        var delayed=new TaskCompletionSource<string>(); handler.AsyncReply=(r,_)=>r.RequestUri!.AbsolutePath.EndsWith("/tasks")?delayed.Task:Task.FromResult(handler.Reply(r));
        Tap(frame,"Refresh"); Pump(TimeSpan.FromMilliseconds(50));
        vault.Write("Todoist",new IntegrationCredential { AccessToken="synthetic-new-account" }); delayed.SetResult(TaskReply("Old private task")); Pump(TimeSpan.FromMilliseconds(100));
        Assert(!TaskVisible("Old private task") && Lists().Items.Count==0,"In-flight response from replaced account is rejected and cached lists cleared");
        handler.AsyncReply=null; handler.Reply=r=>r.RequestUri!.AbsolutePath.EndsWith("projects")?"{\"results\":[{\"id\":\"p2\",\"name\":\"New account\"}]}":TaskReply("New task");
        Tap(frame,"Refresh"); Pump(TimeSpan.FromMilliseconds(80));
        Assert(Lists().Items.OfType<ExternalList>().Any(l=>l.Id=="p2") && TaskVisible("New task"),"Refresh after account replacement loads new account lists and tasks");
        var bound=client.TaskProvider("Todoist",vault.Read("Todoist")!.ConnectionId); var beforeRequests=handler.Requests.Count;
        vault.Write("Todoist",new IntegrationCredential { AccessToken="synthetic-third-account" });
        Throws<IntegrationException>(()=>bound.Complete("p2","today",default).GetAwaiter().GetResult(),"Task mutation from an old account cannot use replacement credentials");
        Assert(handler.Requests.Count==beforeRequests,"Rejected old-account mutation makes no external request");
        vault.Disconnect("Todoist"); Filter().SelectedItem="All";
        Assert(!TaskVisible("New task") && Lists().Items.Count==0,"Disconnect clears task data even before widget rerender"); external.Close();

        handler.Reply=_=>"{\"results\":[null]}"; vault.Write("Todoist",new IntegrationCredential { AccessToken="synthetic-token" });
        Throws<IntegrationException>(()=>client.TaskProvider("Todoist").Tasks("",default).GetAwaiter().GetResult(),"Malformed provider rows return a recoverable error");
        Throws<IntegrationException>(()=>client.ConnectTodoist("bad\r\ntoken",default).GetAwaiter().GetResult(),"Malformed pasted authorization cannot crash HTTP header creation");
        var malformed=Path.Combine(output,"malformed-google.json"); File.WriteAllText(malformed,"{\"installed\":null}");
        Throws<IntegrationException>(()=>GoogleAuthorization.ImportConfiguration(vault,malformed),"Malformed desktop configuration returns a clean error");
        File.WriteAllText(malformed,"[]"); Throws<IntegrationException>(()=>GoogleAuthorization.ImportConfiguration(vault,malformed),"Non-object desktop configuration returns a clean error");
        vault.Write("Google Tasks",new IntegrationCredential { AccessToken="expired",RefreshToken="synthetic-refresh",ExpiresAt=DateTimeOffset.MinValue }); handler.Reply=_=>"{\"access_token\":\"synthetic-renewed\",\"expires_in\":\"bad\"}";
        Throws<IntegrationException>(()=>client.TaskProvider("Google Tasks").Lists(default).GetAwaiter().GetResult(),"Malformed Google refresh expiry returns a clean error");

        vault.Write("Google Calendar",new IntegrationCredential { AccessToken="synthetic-calendar",ExpiresAt=DateTimeOffset.UtcNow.AddHours(1) });
        var now=DateTimeOffset.Now;
        var schedule=JsonSerializer.Serialize(new { items=new[] {
            new { summary="Current class",start=new { dateTime=now.AddMinutes(-10).ToString("o") },end=new { dateTime=now.AddMinutes(10).ToString("o") } },
            new { summary="Next class",start=new { dateTime=now.AddMinutes(25).ToString("o") },end=new { dateTime=now.AddMinutes(55).ToString("o") } }
        } });
        handler.Reply=r=>r.RequestUri!.AbsolutePath.EndsWith("calendarList")?"{\"items\":[{\"id\":\"primary\",\"summary\":\"School\"}]}":schedule;
        var calendarState=store.Load(); calendarState.Widgets=[WidgetLibrary.Create("Calendar")]; store.Save(calendarState);
        var calendarWindow=new MainWindow(store); typeof(MainWindow).GetField("integrationClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(calendarWindow,client); calendarWindow.RenderWidgets(); calendarWindow.Show(); Pump(TimeSpan.FromMilliseconds(120)); calendarWindow.UpdateClock(now);
        var calendarFrame=calendarWindow.WidgetCanvas.Children.OfType<Border>().Single(b=>b.Tag is Guid);
        Assert(Descendants(calendarFrame).OfType<TextBlock>().Any(t=>t.Text.Contains("Now: Current class") && t.Text.Contains("Next: Next class in 25 minutes")),"Calendar displays current event, next event and countdown");
        var calendarDelayed=new TaskCompletionSource<string>(); handler.AsyncReply=(r,_)=>r.RequestUri!.AbsolutePath.EndsWith("/events")?calendarDelayed.Task:Task.FromResult(handler.Reply(r));
        Tap(calendarFrame,"Refresh"); Pump(TimeSpan.FromMilliseconds(50)); vault.Disconnect("Google Calendar"); calendarDelayed.SetResult(schedule); Pump(TimeSpan.FromMilliseconds(100)); calendarWindow.UpdateClock(now);
        Assert(!Descendants(calendarFrame).OfType<TextBlock>().Any(t=>t.Text.Contains("Current class") || t.Text.Contains("Next class")),"Calendar cannot restore event data after in-flight disconnect"); calendarWindow.Close();
    }
}
