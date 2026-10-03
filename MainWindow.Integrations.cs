using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

public partial class MainWindow
{
    private IntegrationClient? integrationClient;
    private IntegrationClient Connections => integrationClient??=new IntegrationClient(store);
    private UIElement BuildIntegrationWidget(WidgetInstance widget)
    {
        var service=widget.Kind=="Calendar"?"Google Calendar":widget.Kind;
        var calendar=widget.Kind=="Calendar";
        var client=Connections; var provider=calendar?null:client.TaskProvider(service);
        var panel=new StackPanel(); var status=new TextBlock { Text="Not connected. Your local tools are ready.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10) }; panel.Children.Add(status);
        var lists=new ComboBox { DisplayMemberPath="Name",Margin=new Thickness(0,0,0,8) }; panel.Children.Add(lists);
        var filter=new ComboBox { ItemsSource=new[] { "Today","Upcoming","All" },SelectedItem=widget.IntegrationView,Margin=new Thickness(0,0,0,8),Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(filter);
        var summary=new TextBlock { TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10),Visibility=calendar?Visibility.Visible:Visibility.Collapsed }; panel.Children.Add(summary);
        var content=new StackPanel(); panel.Children.Add(new ScrollViewer { Content=content,MaxHeight=240,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        var actions=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,0) };
        var refresh=new Button { Content="Refresh",Margin=new Thickness(0,0,8,0) }; var settings=new Button { Content="Connections" }; settings.Click+=(_,_)=>OpenMainSettings("Integrations"); actions.Children.Add(refresh); actions.Children.Add(settings); panel.Children.Add(actions);
        var input=new TextBox { MaxLength=500,Margin=new Thickness(0,10,0,6),Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(input);
        var add=new Button { Content="Add task",HorizontalAlignment=HorizontalAlignment.Left,Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(add);
        CancellationTokenSource? pending=null; bool loading=false,initialized=false; List<ExternalTask> tasks=[]; List<CalendarEntry> events=[];
        void RenderTasks()
        {
            content.Children.Clear(); var today=DateOnly.FromDateTime(DateTime.Now);
            var visible=tasks.Where(t=>widget.IntegrationView=="All" || widget.IntegrationView=="Upcoming" && t.Due>today || widget.IntegrationView=="Today" && t.Due==today).OrderBy(t=>t.Due);
            foreach(var task in visible)
            {
                var check=new CheckBox { Content=task.Title+(task.Due is {} date?$" ({date:MMM d})":""),Margin=new Thickness(0,6,0,6),IsChecked=false };
                check.Click+=async (_,_)=>
                {
                    check.IsEnabled=false;
                    try { using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(20)); await provider!.Complete(widget.ExternalListId,task.Id,cancel.Token); tasks.Remove(task); RenderTasks(); PlayInterfaceSound(); }
                    catch(Exception ex) when(ex is IntegrationException or OperationCanceledException or System.IO.IOException) { check.IsChecked=false; check.IsEnabled=true; status.Text=ex is IntegrationException?ex.Message:"Could not complete task. Refresh to check its status."; }
                }; content.Children.Add(check);
            }
            if(content.Children.Count==0) content.Children.Add(new TextBlock { Text="No tasks in this view.",Margin=new Thickness(0,10,0,10) });
        }
        async Task Load()
        {
            if(loading) return; loading=true; refresh.IsEnabled=false; add.IsEnabled=false; lists.IsEnabled=false;
            pending?.Cancel(); pending?.Dispose(); pending=new CancellationTokenSource(); var token=pending.Token;
            try
            {
                if(!client.Vault.Connected(service)) { content.Children.Clear(); events.Clear(); summary.Text=""; status.Text="Not connected. Open Connections when you're ready."; return; }
                status.Text="Loading...";
                if(!initialized)
                {
                    var available=calendar?await client.CalendarProvider().Calendars(token):await provider!.Lists(token);
                    if(service=="Todoist") available.Insert(0,new ExternalList("","All projects"));
                    token.ThrowIfCancellationRequested(); lists.ItemsSource=available;
                    lists.SelectedItem=available.FirstOrDefault(l=>l.Id==widget.ExternalListId)??available.FirstOrDefault(); initialized=true;
                }
                if(lists.SelectedItem is not ExternalList chosen) { status.Text="No lists available in this account."; return; }
                if(chosen.Id!=widget.ExternalListId) { widget.ExternalListId=chosen.Id; Changed(); }
                if(calendar)
                {
                    events=await client.CalendarProvider().Schedule(chosen.Id,DateTimeOffset.Now,token); token.ThrowIfCancellationRequested(); content.Children.Clear();
                    foreach(var item in events) content.Children.Add(new TextBlock { Text=$"{(item.AllDay?"All day":item.Start.LocalDateTime.ToString("h:mm tt"))}  {item.Title}",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,6) });
                    if(events.Count==0) content.Children.Add(new TextBlock { Text="Nothing scheduled today." });
                }
                else { tasks=await provider!.Tasks(chosen.Id,token); token.ThrowIfCancellationRequested(); RenderTasks(); }
                if(!client.Vault.Connected(service)) { content.Children.Clear(); events.Clear(); status.Text="Disconnected."; return; }
                status.Text="Updated "+DateTime.Now.ToString("h:mm tt");
            }
            catch(Exception ex) when(ex is IntegrationException or System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or FormatException or OperationCanceledException)
            { if(!token.IsCancellationRequested) { content.Children.Clear(); events.Clear(); summary.Text=""; status.Text=ex is IntegrationException?ex.Message:"Could not load this service. Refresh to try again."; } }
            finally { loading=false; refresh.IsEnabled=true; lists.IsEnabled=true; add.IsEnabled=client.Vault.Connected(service) && lists.SelectedItem!=null; }
        }
        refresh.Click+=async (_,_)=>await Load();
        lists.SelectionChanged+=async (_,_)=> { if(!loading) await Load(); };
        filter.SelectionChanged+=(_,_)=> { widget.IntegrationView=(string?)filter.SelectedItem??"Today"; Changed(); RenderTasks(); };
        add.Click+=async (_,_)=>
        {
            if(string.IsNullOrWhiteSpace(input.Text)) return;
            add.IsEnabled=false;
            try { using var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(20)); await provider!.Add(widget.ExternalListId,input.Text.Trim(),cancel.Token); input.Clear(); filter.SelectedItem="All"; await Load(); }
            catch(Exception ex) when(ex is IntegrationException or System.IO.IOException or OperationCanceledException) { status.Text=ex is IntegrationException?ex.Message:"Could not add the task. Refresh to check before trying again."; }
            finally { add.IsEnabled=true; }
        };
        panel.Loaded+=async (_,_)=>await Load(); panel.Unloaded+=(_,_)=> { pending?.Cancel(); tasks.Clear(); events.Clear(); content.Children.Clear(); };
        if(calendar) widgetTicks.Add(now=>
        {
            if(events.Count==0) return;
            var current=events.FirstOrDefault(e=>!e.AllDay && e.Start<=now && e.End>now); var next=events.FirstOrDefault(e=>!e.AllDay && e.Start>now);
            summary.Text=(current==null?"No event right now.":"Now: "+current.Title)+"\n"+(next==null?"No more timed events today.":$"Next: {next.Title} in {Math.Ceiling((next.Start-now).TotalMinutes):0} minutes");
        });
        return panel;
    }
}
