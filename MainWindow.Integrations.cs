using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

public partial class MainWindow
{
    private IntegrationClient? integrationClient;
    private IntegrationClient Connections => integrationClient??=new IntegrationClient(store);
    private UIElement BuildIntegrationWidget(WidgetInstance widget)
    {
        var service=widget.Kind=="Calendar" && widget.CalendarService=="Outlook Calendar"?"Outlook Calendar":widget.Kind=="Calendar"?"Google Calendar":widget.Kind;
        var calendar=widget.Kind=="Calendar";
        var client=Connections; var provider=calendar?null:client.TaskProvider(service);
        var panel=new StackPanel();
        if(calendar) panel.Children.Add(new TextBlock { Text=DateTime.Now.ToString("dddd, MMMM d"),FontSize=18,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8) });
        var status=new TextBlock { Text="Not connected. Your local tools are ready.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10) }; panel.Children.Add(status);
        var sources=new ComboBox { ItemsSource=new[] { "Google Calendar","Outlook Calendar" },SelectedItem=service,Margin=new Thickness(0,0,0,8),Visibility=calendar?Visibility.Visible:Visibility.Collapsed };
        panel.Children.Add(sources);
        var lists=new ComboBox { DisplayMemberPath="Name",Margin=new Thickness(0,0,0,8) }; panel.Children.Add(lists);
        var filter=new ComboBox { ItemsSource=new[] { "Today","Upcoming","All" },SelectedItem=widget.IntegrationView,Margin=new Thickness(0,0,0,8),Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(filter);
        var summary=new TextBlock { TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10),Visibility=calendar?Visibility.Visible:Visibility.Collapsed }; panel.Children.Add(summary);
        var content=new StackPanel(); panel.Children.Add(new ScrollViewer { Content=content,MaxHeight=widget.DisplayMode=="Expanded"?360:150,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        var actions=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,0) };
        var refresh=new Button { Content="Refresh",Margin=new Thickness(0,0,8,0) }; var settings=new Button { Content="Connections" }; settings.Click+=(_,_)=>OpenMainSettings("Integrations"); actions.Children.Add(refresh); actions.Children.Add(settings); panel.Children.Add(actions);
        if(calendar) AddDisplayModeControls(panel,widget);
        var input=new TextBox { MaxLength=500,Margin=new Thickness(0,10,0,6),Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(input);
        var add=new Button { Content="Add task",HorizontalAlignment=HorizontalAlignment.Left,Visibility=calendar?Visibility.Collapsed:Visibility.Visible }; panel.Children.Add(add);
        CancellationTokenSource? pending=null; var lifetime=new CancellationTokenSource();
        bool loading=false,initialized=false,sourceReloadPending=false; Guid? loadedConnection=null; List<ExternalTask> tasks=[]; List<CalendarEntry> events=[];
        void ClearData(bool resetLists)
        {
            tasks.Clear(); events.Clear(); content.Children.Clear(); summary.Text="";
            if(resetLists) { initialized=false; lists.ItemsSource=null; loadedConnection=null; }
        }
        bool ConnectionMatches(Guid? expected)
        {
            try { return expected!=null && client.Vault.Read(service)?.ConnectionId==expected; }
            catch(Exception ex) when(ex is IntegrationException or System.IO.IOException or UnauthorizedAccessException) { return false; }
        }
        void CheckConnection(Guid? expected,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if(!ConnectionMatches(expected)) throw new IntegrationException("Connection changed. Refresh to use the current account.");
        }
        void RenderTasks()
        {
            if(!ConnectionMatches(loadedConnection)) { ClearData(true); status.Text="Connection changed. Refresh to reconnect."; return; }
            content.Children.Clear(); var today=DateOnly.FromDateTime(DateTime.Now);
            var visible=tasks.Where(t=>widget.IntegrationView=="All" || widget.IntegrationView=="Upcoming" && t.Due>today || widget.IntegrationView=="Today" && t.Due==today).OrderBy(t=>t.Due);
            foreach(var task in visible)
            {
                var check=new CheckBox { Content=task.Title+(task.Due is {} date?$" ({date:MMM d})":""),Margin=new Thickness(0,6,0,6),IsChecked=false };
                check.Click+=async (_,_)=>
                {
                    check.IsEnabled=false;
                    try { var account=loadedConnection; using var cancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); cancel.CancelAfter(TimeSpan.FromSeconds(20)); CheckConnection(account,cancel.Token); await provider!.Complete(widget.ExternalListId,task.Id,cancel.Token); CheckConnection(account,cancel.Token); tasks.Remove(task); RenderTasks(); PlayInterfaceSound(); }
                    catch(Exception ex) when(ex is IntegrationException or OperationCanceledException or System.IO.IOException or UnauthorizedAccessException) { if(!ConnectionMatches(loadedConnection)) ClearData(true); check.IsChecked=false; check.IsEnabled=true; status.Text=ex is IntegrationException?ex.Message:"Could not complete task. Refresh to check its status."; }
                }; content.Children.Add(check);
            }
            if(content.Children.Count==0) content.Children.Add(new TextBlock { Text="No tasks in this view.",Margin=new Thickness(0,10,0,10) });
        }
        async Task Load()
        {
            if(loading) return; loading=true; refresh.IsEnabled=false; add.IsEnabled=false; lists.IsEnabled=false;
            pending?.Cancel(); pending?.Dispose(); pending=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); var token=pending.Token;
            try
            {
                var account=client.Vault.Read(service)?.ConnectionId;
                if(account==null) { ClearData(true); status.Text="Not connected. Open Connections when you're ready."; return; }
                if(loadedConnection!=account) { ClearData(true); loadedConnection=account; }
                provider=calendar?null:client.TaskProvider(service,account);
                ClearData(false);
                status.Text="Loading...";
                if(!initialized)
                {
                    var available=calendar?await client.CalendarProvider(service,account).Calendars(token):await provider!.Lists(token);
                    if(service=="Todoist") available.Insert(0,new ExternalList("","All projects"));
                    CheckConnection(account,token); lists.ItemsSource=available;
                    lists.SelectedItem=available.FirstOrDefault(l=>l.Id==widget.ExternalListId)??available.FirstOrDefault(); initialized=true;
                }
                if(lists.SelectedItem is not ExternalList chosen) { status.Text="No lists available in this account."; return; }
                if(chosen.Id!=widget.ExternalListId) { widget.ExternalListId=chosen.Id; Changed(); }
                if(calendar)
                {
                    var schedule=await client.CalendarProvider(service,account).Schedule(chosen.Id,DateTimeOffset.Now,token); CheckConnection(account,token); events=schedule; content.Children.Clear();
                    foreach(var item in events.Take(widget.DisplayMode=="Expanded"?30:3))
                        content.Children.Add(new TextBlock { Text=(item.AllDay?"All day":item.Start.LocalDateTime.ToString("h:mm tt")+"–"+item.End.LocalDateTime.ToString("h:mm tt"))+"  "+item.Title
                            +(widget.DisplayMode=="Expanded"?"  ·  "+chosen.Name+" ("+service+")":""),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,6) });
                    if(events.Count==0) content.Children.Add(new TextBlock { Text="Nothing scheduled today." });
                }
                else { var result=await provider!.Tasks(chosen.Id,token); CheckConnection(account,token); tasks=result; RenderTasks(); }
                status.Text="Updated "+DateTime.Now.ToString("h:mm tt");
            }
            catch(Exception ex) when(ex is IntegrationException or System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException or FormatException or OperationCanceledException)
            { ClearData(true); if(!token.IsCancellationRequested) status.Text=ex is IntegrationException?ex.Message:"Could not load this service. Refresh to try again."; }
            finally
            {
                loading=false; refresh.IsEnabled=true; lists.IsEnabled=true; add.IsEnabled=client.Vault.Connected(service) && lists.SelectedItem!=null;
                if(sourceReloadPending) { sourceReloadPending=false; await Load(); }
            }
        }
        refresh.Click+=async (_,_)=>await Load();
        sources.SelectionChanged+=async (_,_)=>
        {
            if(!calendar || sources.SelectedItem is not string selected || selected==service) return;
            pending?.Cancel(); service=selected; widget.CalendarService=selected; widget.ExternalListId=""; ClearData(true); Changed(); SaveNow();
            if(loading) sourceReloadPending=true; else await Load();
        };
        lists.SelectionChanged+=async (_,_)=> { if(!loading && initialized) await Load(); };
        filter.SelectionChanged+=(_,_)=> { widget.IntegrationView=(string?)filter.SelectedItem??"Today"; Changed(); RenderTasks(); };
        add.Click+=async (_,_)=>
        {
            if(string.IsNullOrWhiteSpace(input.Text)) return;
            add.IsEnabled=false;
            try { var account=loadedConnection; using var cancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); cancel.CancelAfter(TimeSpan.FromSeconds(20)); CheckConnection(account,cancel.Token); await provider!.Add(widget.ExternalListId,input.Text.Trim(),cancel.Token); CheckConnection(account,cancel.Token); input.Clear(); filter.SelectedItem="All"; await Load(); }
            catch(Exception ex) when(ex is IntegrationException or System.IO.IOException or UnauthorizedAccessException or OperationCanceledException) { if(!ConnectionMatches(loadedConnection)) ClearData(true); status.Text=ex is IntegrationException?ex.Message:"Could not add the task. Refresh to check before trying again."; }
            finally { add.IsEnabled=ConnectionMatches(loadedConnection); }
        };
        panel.Loaded+=async (_,_)=> { if(lifetime.IsCancellationRequested) { lifetime.Dispose(); lifetime=new CancellationTokenSource(); } await Load(); };
        panel.Unloaded+=(_,_)=> { lifetime.Cancel(); pending?.Cancel(); ClearData(true); };
        if(calendar) widgetTicks.Add(now=>
        {
            if(events.Count==0) return;
            if(!ConnectionMatches(loadedConnection)) { ClearData(true); status.Text="Connection changed. Refresh to reconnect."; return; }
            var current=events.FirstOrDefault(e=>!e.AllDay && e.Start<=now && e.End>now); var next=events.FirstOrDefault(e=>!e.AllDay && e.Start>now);
            summary.Text=(current==null?"No event right now.":"Now: "+current.Title)+"\n"+(next==null?"No more timed events today.":$"Next: {next.Title} in {Math.Ceiling((next.Start-now).TotalMinutes):0} minutes");
        });
        return panel;
    }
}
