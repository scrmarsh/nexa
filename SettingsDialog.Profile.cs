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
        var name=new Button{Content=string.IsNullOrWhiteSpace(profile.DisplayName)?"Set display name":profile.DisplayName,HorizontalAlignment=HorizontalAlignment.Left,ToolTip="Edit display name"};name.FontSize=24;name.FontWeight=FontWeights.SemiBold;name.HorizontalAlignment=HorizontalAlignment.Center;
        var editor=new StackPanel{MaxWidth=360,HorizontalAlignment=HorizontalAlignment.Center,Visibility=Visibility.Collapsed};
        var input=new TextBox{Text=profile.DisplayName,MaxLength=80,FontSize=21,TextAlignment=TextAlignment.Center,Tag="InlineProfileName"};
        System.Windows.Automation.AutomationProperties.SetName(input,"Profile display name");
        var validation=new TextBlock{TextWrapping=TextWrapping.Wrap,MinHeight=24,Margin=new Thickness(0,6,0,6)};
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};
        var saveName=new Button{Content="Save",MinWidth=90,Margin=new Thickness(0,0,8,0)};
        var cancelName=new Button{Content="Cancel",MinWidth=90};actions.Children.Add(saveName);actions.Children.Add(cancelName);
        editor.Children.Add(input);editor.Children.Add(validation);editor.Children.Add(actions);
        var nameArea=new Grid{MinHeight=112,Margin=new Thickness(12)};nameArea.Children.Add(editor);name.VerticalAlignment=VerticalAlignment.Center;nameArea.Children.Add(name);var nameCard=new Border{Child=nameArea,CornerRadius=new CornerRadius(10),BorderThickness=new Thickness(1)};nameCard.SetResourceReference(Border.BackgroundProperty,"PanelBrush");nameCard.SetResourceReference(Border.BorderBrushProperty,"LineBrush");page.Children.Add(nameCard);
        void CancelName(){input.Text=store.LoadProfile().DisplayName;validation.Text="";editor.Visibility=Visibility.Collapsed;name.Visibility=Visibility.Visible;name.IsEnabled=true;name.Focus();}
        void SaveName()
        {
            var value=input.Text.Trim();
            if(value.Length==0 || value.Length>80 || value.Any(char.IsControl)){validation.Text="Enter a name of 1–80 characters without control characters.";input.Focus();return;}
            try{var current=store.LoadProfile();current.DisplayName=value;store.SaveProfile(current);name.Content=value;CancelName();}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){validation.Text="Your name could not be saved. Try again.";}
        }
        name.Click+=(_,_)=>{input.Text=store.LoadProfile().DisplayName;name.Visibility=Visibility.Collapsed;name.IsEnabled=false;editor.Visibility=Visibility.Visible;input.Focus();input.SelectAll();};
        saveName.Click+=(_,_)=>SaveName();cancelName.Click+=(_,_)=>CancelName();
        input.Tag=(Action)CancelName;
        input.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter){SaveName();e.Handled=true;}};
        var setup=new Button { Content="Rerun Setup",HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,20,0,0) };
        setup.Click+=(_,_)=>RerunSetup();page.Children.Add(setup);
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

        return new ScrollViewer { Tag="HiddenPageScroll",Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden,PanningMode=PanningMode.VerticalOnly };
    }
    internal void RerunSetup()
    {
        if(!FlushPendingChanges())return;
        var owner=Owner;Close();SetupNavigation.TryRun(owner,store,space);
    }
    private UIElement BuildIntegrations(bool future=false)
    {
        var page=Page("Integrations","Optional connections. Your local spaces always work without them.");
        var client=new IntegrationClient(store);
        foreach(var service in (future?new[] { "Google Tasks","Google Calendar","Outlook Calendar" }:new[] { "Todoist" }))
        {
            var section=new StackPanel { Margin=new Thickness(0,0,0,24) }; page.Children.Add(section);
            section.Children.Add(new TextBlock { Text=service,FontSize=20,FontWeight=FontWeights.SemiBold });
            var status=new TextBlock { Text=client.Vault.Connected(service)?"Saved connection · not verified this session":"Not connected",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8) }; section.Children.Add(status);
            PasswordBox? secret=null;
            if(service=="Todoist") { section.Children.Add(new TextBlock { Text="API token from Todoist / Settings / Integrations / Developer",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,6) }); secret=new PasswordBox(); section.Children.Add(secret); }
            var buttons=new WrapPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,10,0,0) }; section.Children.Add(buttons);
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
