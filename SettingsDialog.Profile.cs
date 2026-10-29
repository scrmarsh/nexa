using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private UIElement BuildProfile()
    {
        var profile=store.LoadProfile();var page=Page("Profile","Your recorded activity across Spaces. Everything stays in your Windows account.");
        var name=new Button{Content=string.IsNullOrWhiteSpace(profile.DisplayName)?"Set display name":profile.DisplayName,HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Edit display name"};Field(page,"Display name",name);
        name.Click+=(_,_)=>new DisplayNameDialog(this,store,value=>name.Content=value).ShowDismissible();
        var sessions=(state.Workspace?.Dashboards??[]).SelectMany(d=>d.History.Append(d.Current)).ToArray();
        var events=sessions.SelectMany(s=>s.Data.Productivity.Events).Concat(state.Productivity.Events).DistinctBy(e=>e.Id).ToArray();
        var completed=ProfileActivity.Completed(sessions);
        var days=completed.Select(s=>s.EndedAt.LocalDateTime.Date).Distinct().OrderDescending().ToArray();var streak=0;var expected=DateTime.Today;
        if(days.FirstOrDefault()==expected.AddDays(-1))expected=expected.AddDays(-1);
        foreach(var day in days){if(day!=expected)break;streak++;expected=expected.AddDays(-1);}
        var tasks=events.Count(e=>e.Category=="Task completed");
        var metrics=new System.Windows.Controls.Primitives.UniformGrid{Columns=2,Margin=new Thickness(0,20,0,16)};
        void Metric(string title,string value,string color)
        {
            var stack=new StackPanel{Margin=new Thickness(14)};stack.Children.Add(new TextBlock{Text=value,FontSize=value.Length>24?18:30,TextWrapping=TextWrapping.Wrap,FontWeight=FontWeights.SemiBold,Foreground=BugVisuals.B(color),TextAlignment=TextAlignment.Center});stack.Children.Add(new TextBlock{Text=title,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap});
            metrics.Children.Add(new Border{Child=stack,BorderBrush=BugVisuals.B(color),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10),Margin=new Thickness(5)});
        }
        Metric("Recorded productivity score",events.Sum(e=>(long)e.BasePoints+e.BonusPoints).ToString("N0"),"#5B7CFF");
        Metric("Completed focus time",TimeSpan.FromSeconds(completed.Sum(s=>s.ActualSeconds)).TotalHours.ToString("0.0")+" h","#7F77DD");
        Metric("Completed sessions",completed.Length.ToString("N0"),"#14B8A6");Metric("Completed-session streak",streak+" days","#F59E0B");
        Metric("Verified task actions",tasks.ToString("N0"),"#69ACFF");
        var achievements=new List<string>();if(completed.Length>0)achievements.Add("First session");if(completed.Length>=10)achievements.Add("10 sessions");if(tasks>=10)achievements.Add("10 task actions");
        Metric("Activity milestones",achievements.Count==0?"None yet":string.Join(" · ",achievements),"#A855F7");page.Children.Add(metrics);
        page.Children.Add(new TextBlock{Text="Score sums unique recorded action awards, including bonuses and penalties. Focus time counts finalized session recaps, not time with nexa open. Streak counts consecutive local days with a completed recap. Older activity without a recap is not estimated. Task actions can include a task completed again after reopening.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,18)});
        var summaries=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};
        foreach(var item in new[]{("Theme",AppearancePalettes.Name(profile.Theme)+" / "+profile.ThemeMode),("Sounds",profile.Sounds?"On · "+profile.Volume.ToString("P0"):"Off"),("Focus reminders",profile.FocusNudges?"On":"Off"),("Connections",profile.ConnectedIntegrations.Count==0?"None":string.Join(", ",profile.ConnectedIntegrations))})
            summaries.Children.Add(new TextBlock{Text=item.Item1+"\n"+item.Item2,FontSize=18,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8,14,8,14)});
        page.Children.Add(summaries);
        var setup=new Button { Content="Run welcome setup again",HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,24,0,0) }; setup.Click+=(_,_)=> { if (!FlushPendingChanges()) return; var owner=Owner; Close(); try { SetupNavigation.Run(owner,store,space); } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { MessageBox.Show(owner,"Setup could not be saved. Existing spaces have been retained.","nexa setup"); } }; page.Children.Add(setup);
        return new ScrollViewer { Tag="HiddenPageScroll",Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,PanningMode=PanningMode.VerticalOnly };
    }
    private UIElement BuildIntegrations(bool future=false)
    {
        var page=Page("Integrations","Optional connections. Your local spaces always work without them.");
        var client=new IntegrationClient(store);
        foreach(var service in (future?new[] { "Google Tasks","Google Calendar","Outlook Calendar" }:new[] { "Todoist","Hevy" }))
        {
            if(service=="Hevy"){page.Children.Add(BuildHevyConnection());continue;}
            var section=new StackPanel { Margin=new Thickness(0,0,0,24) }; page.Children.Add(section);
            section.Children.Add(new TextBlock { Text=service,FontSize=20,FontWeight=FontWeights.SemiBold });
            var status=new TextBlock { Text=client.Vault.Connected(service)?"Saved connection · not verified this session":"Not connected",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8) }; section.Children.Add(status);
            PasswordBox? secret=null;
            if(service=="Todoist") { section.Children.Add(new TextBlock { Text="API token from Todoist / Settings / Integrations / Developer",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,6) }); secret=new PasswordBox(); section.Children.Add(secret); }
            var buttons=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,0) }; section.Children.Add(buttons);
            var connect=new Button { Content="Connect",Margin=new Thickness(0,0,8,0) }; var disconnect=new Button { Content="Disconnect",IsEnabled=client.Vault.Connected(service) }; buttons.Children.Add(connect); buttons.Children.Add(disconnect);
            CancellationTokenSource? pending=null;
            Closed+=(_,_)=>pending?.Cancel();
            connect.Click+=async (_,_)=>
            {
                connect.IsEnabled=false; pending=new CancellationTokenSource(); status.Text="Connecting...";
                try
                {
                    if(service=="Todoist") { var value=secret!.Password; secret.Clear(); await client.ConnectTodoist(value,pending.Token); }
                    else if(service=="Outlook Calendar") await MicrosoftAuthorization.Connect(client.Vault,pending.Token);
                    else await GoogleAuthorization.Connect(client.Vault,service,pending.Token);
                    status.Text="Connected. Select a project or list in the widget."; disconnect.IsEnabled=true; space?.RenderWidgets();
                }
                catch(Exception ex) when(ex is IntegrationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException) { status.Text=ex is IntegrationException?ex.Message:"Connection could not be saved. Try again."; }
                catch(OperationCanceledException) { status.Text="Connection canceled."; }
                finally { connect.IsEnabled=true; pending.Dispose(); pending=null; }
            };
            disconnect.Click+=(_,_)=> { pending?.Cancel(); try { client.Vault.Disconnect(service); status.Text="Not connected"; disconnect.IsEnabled=false; space?.RenderWidgets(); } catch(IOException) { status.Text="Could not disconnect. Try again."; } };
        }
        if(future)
        {
        var googleSetup=new Button { Content="Set up Google connection...",HorizontalAlignment=HorizontalAlignment.Left };
        googleSetup.Click+=(_,_)=>
        {
            var picker=new OpenFileDialog { Title="Google desktop OAuth credentials",Filter="Google desktop configuration|*.json" };
            if(picker.ShowDialog(this)!=true) return;
            try { GoogleAuthorization.ImportConfiguration(client.Vault,picker.FileName); googleSetup.Content="Google connection configured"; }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or IntegrationException) { MessageBox.Show(this,ex is IntegrationException?ex.Message:"Could not read the configuration file.","Google connection"); }
        }; page.Children.Add(googleSetup);
        var microsoftId=new TextBox { MaxLength=80,Margin=new Thickness(0,16,0,6),ToolTip="Your Microsoft application (client) ID" };
        Field(page,"Microsoft public application client ID",microsoftId);
        var microsoftSetup=new Button { Content="Save Microsoft app ID",HorizontalAlignment=HorizontalAlignment.Left };
        var microsoftStatus=new TextBlock { TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,0) };
        microsoftSetup.Click+=(_,_)=> { try { MicrosoftAuthorization.Configure(client.Vault,microsoftId.Text.Trim()); microsoftStatus.Text="Microsoft app configured."; }
            catch(Exception ex) when(ex is IntegrationException or IOException or UnauthorizedAccessException) { microsoftStatus.Text=ex is IntegrationException?ex.Message:"Could not save the Microsoft app ID."; } };
        page.Children.Add(microsoftSetup); page.Children.Add(microsoftStatus);
        page.Children.Add(new TextBlock { Text="Google and Microsoft require registered desktop/public apps. See docs/INTEGRATIONS.md. Calendar access is read-only. Disconnect removes locally saved authorization; revoke permission in the provider account if needed.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0) });
        }
        if(!future){hedySettingsSection=new Border { Child=BuildHedy(),Margin=new Thickness(0,28,0,0),Focusable=true };page.Children.Add(hedySettingsSection);}
        return new ScrollViewer { Tag="HiddenPageScroll",Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,PanningMode=PanningMode.VerticalOnly };
    }
}
