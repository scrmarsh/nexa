using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private UIElement BuildProfile()
    {
        var profile=store.LoadProfile(); var page=Page("Profile","Your preferences stay on this PC. No nexa account is needed.");
        var overview=new TextBlock { TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12) };
        overview.Loaded+=(_,_)=> { var current=store.LoadProfile(); overview.Text=$"Theme: {current.Theme} / {current.ThemeMode}\nSounds: {(current.Sounds?"On":"Off")} / volume {current.Volume:P0}\nFocus reminders: {(current.FocusNudges?"On":"Off")}\nConnections: {(current.ConnectedIntegrations.Count==0?"None":string.Join(", ",current.ConnectedIntegrations))}"; }; page.Children.Add(overview);
        var name=new TextBox { Text=profile.DisplayName,MaxLength=80 }; Field(page,"Display name (optional)",name);
        var uses=new WrapPanel();
        foreach(var use in GuidedSetup.UseCases) uses.Children.Add(new CheckBox { Content=use,IsChecked=profile.UseCases.Contains(use),Margin=new Thickness(0,8,12,8) }); Field(page,"I use nexa for",uses);
        var type=new ComboBox { ItemsSource=GuidedSetup.Types,SelectedItem=profile.PreferredDashboard }; Field(page,"Preferred space type",type);
        var timed=new CheckBox { Content="Start guided spaces with a timed session",IsChecked=profile.TimedSessions,Margin=new Thickness(0,12,0,8) }; page.Children.Add(timed);
        var minutes=new TextBox { Text=profile.DefaultMinutes.ToString(),MaxLength=5 }; Field(page,"Default session minutes",minutes);
        var tools=new WrapPanel();
        foreach(var tool in new[] { "Pixel timer","Notes","Clock","Task List","Focus Reset","Bible","Scripture pins","Calendar","Flashcards","Session timeline" }) tools.Children.Add(new CheckBox { Content=tool,IsChecked=profile.PreferredWidgets.Contains(tool) || (tool=="Task List" && (profile.PreferredWidgets.Contains("Tasks") || profile.PreferredWidgets.Contains("Agenda"))),Margin=new Thickness(0,8,12,8) }); Field(page,"Preferred tools",tools);
        var status=new TextBlock { Text="Setup: "+(profile.OnboardingCompleted?"Completed":"Not completed"),Margin=new Thickness(0,12,0,12),TextWrapping=TextWrapping.Wrap }; page.Children.Add(status);
        void SaveProfile() => QueueSave("profile", () =>
        {
            if(!int.TryParse(minutes.Text,out var duration) || duration is <1 or >10080) { status.Text="Choose a duration from 1 to 10080 minutes."; return; }
            profile.DisplayName=name.Text.Trim(); profile.UseCases=uses.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(string)c.Content).ToList(); profile.PreferredDashboard=(string)type.SelectedItem;
            profile.PreferredWidgets=tools.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(string)c.Content).ToList(); profile.TimedSessions=timed.IsChecked==true; profile.DefaultMinutes=duration;
            profile.Theme=store.LoadAppTheme(); profile.ThemeMode=store.LoadThemeMode(); store.LoadInteractionPreferences(state); profile.Sounds=state.InterfaceSoundsEnabled; profile.Volume=state.InterfaceSoundVolume; profile.FocusNudges=state.FocusNudgesEnabled;
            store.SaveProfile(profile); status.Text="Profile saved. Sounds and theme can be changed under Appearance.";
        });
        name.TextChanged += (_, _) => SaveProfile(); minutes.TextChanged += (_, _) => SaveProfile(); type.SelectionChanged += (_, _) => SaveProfile(); timed.Click += (_, _) => SaveProfile();
        foreach (var box in uses.Children.OfType<CheckBox>().Concat(tools.Children.OfType<CheckBox>())) box.Click += (_, _) => SaveProfile();
        var setup=new Button { Content="Run welcome setup again",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,14,0,0) }; setup.Click+=(_,_)=> { if (!FlushPendingChanges()) return; var owner=Owner; Close(); try { SetupNavigation.Run(owner,store,space); } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { MessageBox.Show(owner,"Setup could not be saved. Existing spaces have been retained.","nexa setup"); } }; page.Children.Add(setup);
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
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
        hedySettingsSection=new Border { Child=BuildHedy(),Margin=new Thickness(0,28,0,0),Focusable=true }; if(!future)page.Children.Add(new Expander{Header="Hedy recording notes",Content=hedySettingsSection});
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
}
