using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private UIElement BuildProfile()
    {
        var profile=store.LoadProfile(); var page=Page("Profile","Your preferences stay on this PC. No Nexa account is needed.");
        var overview=new TextBlock { TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12) };
        overview.Loaded+=(_,_)=> { var current=store.LoadProfile(); overview.Text=$"Theme: {current.Theme} / {current.ThemeMode}\nSounds: {(current.Sounds?"On":"Off")} / volume {current.Volume:P0}\nFocus reminders: {(current.FocusNudges?"On":"Off")}\nConnections: {(current.ConnectedIntegrations.Count==0?"None":string.Join(", ",current.ConnectedIntegrations))}"; }; page.Children.Add(overview);
        var appearance=new Button { Content="Change theme, sounds or reminders",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12) }; appearance.Click+=(_,_)=>SelectSection("Appearance"); page.Children.Add(appearance);
        var name=new TextBox { Text=profile.DisplayName,MaxLength=80 }; Field(page,"Display name (optional)",name);
        var uses=new WrapPanel();
        foreach(var use in GuidedSetup.UseCases) uses.Children.Add(new CheckBox { Content=use,IsChecked=profile.UseCases.Contains(use),Margin=new Thickness(0,8,12,8) }); Field(page,"I use Nexa for",uses);
        var type=new ComboBox { ItemsSource=GuidedSetup.Types,SelectedItem=profile.PreferredDashboard }; Field(page,"Preferred space type",type);
        var timed=new CheckBox { Content="Start guided spaces with a timed session",IsChecked=profile.TimedSessions,Margin=new Thickness(0,12,0,8) }; page.Children.Add(timed);
        var minutes=new TextBox { Text=profile.DefaultMinutes.ToString(),MaxLength=5 }; Field(page,"Default session minutes",minutes);
        var tools=new WrapPanel();
        foreach(var tool in new[] { "Pixel timer","Notes","Clock","Tasks","Focus Reset","Bible","Scripture pins","Agenda","Calendar","Flashcards","Session timeline" }) tools.Children.Add(new CheckBox { Content=tool,IsChecked=profile.PreferredWidgets.Contains(tool),Margin=new Thickness(0,8,12,8) }); Field(page,"Preferred tools",tools);
        var status=new TextBlock { Text="Setup: "+(profile.OnboardingCompleted?"Completed":"Not completed"),Margin=new Thickness(0,12,0,12),TextWrapping=TextWrapping.Wrap }; page.Children.Add(status);
        var save=new Button { Content="Save profile",HorizontalAlignment=HorizontalAlignment.Left }; save.Click+=(_,_)=>
        {
            if(!int.TryParse(minutes.Text,out var duration) || duration is <1 or >10080) { status.Text="Choose a duration from 1 to 10080 minutes."; return; }
            profile.DisplayName=name.Text.Trim(); profile.UseCases=uses.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(string)c.Content).ToList(); profile.PreferredDashboard=(string)type.SelectedItem;
            profile.PreferredWidgets=tools.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(string)c.Content).ToList(); profile.TimedSessions=timed.IsChecked==true; profile.DefaultMinutes=duration;
            profile.Theme=store.LoadAppTheme(); profile.ThemeMode=store.LoadThemeMode(); store.LoadInteractionPreferences(state); profile.Sounds=state.InterfaceSoundsEnabled; profile.Volume=state.InterfaceSoundVolume; profile.FocusNudges=state.FocusNudgesEnabled;
            try { store.SaveProfile(profile); status.Text="Profile saved. Sounds and theme can be changed under Appearance."; } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { status.Text="Profile could not be saved. Try again."; }
        }; page.Children.Add(save);
        var setup=new Button { Content="Run welcome setup again",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,14,0,0) }; setup.Click+=(_,_)=> { var owner=Owner; Close(); try { SetupNavigation.Run(owner,store,space); } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { MessageBox.Show(owner,"Setup could not be saved. Existing spaces have been retained.","Nexa setup"); } }; page.Children.Add(setup);
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
    private UIElement BuildIntegrations()
    {
        var page=Page("Integrations","Optional connections. Your local spaces always work without them.");
        var client=new IntegrationClient(store);
        foreach(var service in new[] { "Todoist","Google Tasks","Google Calendar" })
        {
            var section=new StackPanel { Margin=new Thickness(0,0,0,24) }; page.Children.Add(section);
            section.Children.Add(new TextBlock { Text=service,FontSize=20,FontWeight=FontWeights.SemiBold });
            var status=new TextBlock { Text=client.Vault.Connected(service)?"Connected":"Not connected",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8) }; section.Children.Add(status);
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
                    else await GoogleAuthorization.Connect(client.Vault,service,pending.Token);
                    status.Text="Connected. Select a project or list in the widget."; disconnect.IsEnabled=true; space?.RenderWidgets();
                }
                catch(Exception ex) when(ex is IntegrationException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException) { status.Text=ex is IntegrationException?ex.Message:"Connection could not be saved. Try again."; }
                catch(OperationCanceledException) { status.Text="Connection canceled."; }
                finally { connect.IsEnabled=true; pending.Dispose(); pending=null; }
            };
            disconnect.Click+=(_,_)=> { pending?.Cancel(); try { client.Vault.Disconnect(service); status.Text="Not connected"; disconnect.IsEnabled=false; space?.RenderWidgets(); } catch(IOException) { status.Text="Could not disconnect. Try again."; } };
        }
        var googleSetup=new Button { Content="Set up Google connection...",HorizontalAlignment=HorizontalAlignment.Left };
        googleSetup.Click+=(_,_)=>
        {
            var picker=new OpenFileDialog { Title="Google desktop OAuth credentials",Filter="Google desktop configuration|*.json" };
            if(picker.ShowDialog(this)!=true) return;
            try { GoogleAuthorization.ImportConfiguration(client.Vault,picker.FileName); googleSetup.Content="Google connection configured"; }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or IntegrationException) { MessageBox.Show(this,ex is IntegrationException?ex.Message:"Could not read the configuration file.","Google connection"); }
        }; page.Children.Add(googleSetup);
        page.Children.Add(new TextBlock { Text="Google requires a registered desktop app with Tasks / Calendar APIs enabled. See INTEGRATIONS.md. Calendar access is read-only. Disconnect removes local authorization; Google account settings can revoke the app's permission.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0) });
        return new ScrollViewer { Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
}
