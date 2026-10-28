using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed class HevyConnection(StateStore store,HttpClient? transport=null)
{
    private static readonly HttpClient shared=new(new HttpClientHandler{AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(15)};
    private readonly IntegrationVault vault=new(store);
    internal async Task<int> Count(string key,CancellationToken token)
    {
        if(!Guid.TryParse(key,out _))throw new IntegrationException("Enter the API key from your Hevy Pro developer settings.");
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.hevyapp.com/v1/workouts/count");
        request.Headers.Add("api-key",key);
        using var response=await (transport??shared).SendAsync(request,token);
        if(!response.IsSuccessStatusCode)throw new IntegrationException("Hevy could not verify this key. Check your connection and Hevy Pro developer key.");
        if(response.Content.Headers.ContentLength>1048576)throw new IntegrationException("Unexpected Hevy response.");
        using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        if(!json.RootElement.TryGetProperty("workout_count",out var count)||!count.TryGetInt32(out var value)||value<0)throw new IntegrationException("Hevy returned an unsupported response.");
        return value;
    }
    internal async Task<int> Connect(string key,CancellationToken token)
    {
        var total=await Count(key.Trim(),token);token.ThrowIfCancellationRequested();
        vault.Write("Hevy",new IntegrationCredential{AccessToken=key.Trim()});return total;
    }
}
internal sealed partial class SettingsDialog
{
    private UIElement BuildHevyConnection()
    {
        var section=new StackPanel{Margin=new Thickness(0,8,0,24)};
        section.Children.Add(new TextBlock{Text="Hevy",FontSize=20,FontWeight=FontWeights.SemiBold});
        section.Children.Add(new TextBlock{Text="Optional workout-account connection. Use your Hevy Pro developer API key. The key stays encrypted for this Windows account. Hedy recording notes are separate.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
        var vault=new IntegrationVault(store);var key=new PasswordBox();section.Children.Add(key);
        var status=new TextBlock{Text=vault.Connected("Hevy")?"Connected":"Not connected",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,6,0,8)};section.Children.Add(status);
        var buttons=new WrapPanel();var connect=new Button{Content="Connect Hevy",Margin=new Thickness(0,0,8,0)};var disconnect=new Button{Content="Disconnect",IsEnabled=vault.Connected("Hevy")};buttons.Children.Add(connect);buttons.Children.Add(disconnect);section.Children.Add(buttons);
        CancellationTokenSource? pending=null;Closed+=(_,_)=>pending?.Cancel();
        connect.Click+=async(_,_)=>{
            connect.IsEnabled=false;pending=new();var secret=key.Password;key.Clear();
            try{var count=await new HevyConnection(store).Connect(secret,pending.Token);status.Text=$"Connected - {count:N0} recorded workouts";disconnect.IsEnabled=true;}
            catch(OperationCanceledException){status.Text="Connection canceled.";}
            catch(Exception ex) when(ex is IntegrationException or System.Net.Http.HttpRequestException or IOException or UnauthorizedAccessException or System.Text.Json.JsonException){status.Text=ex is IntegrationException?ex.Message:"Hevy could not connect. Try again when online.";}
            finally{connect.IsEnabled=true;pending.Dispose();pending=null;}
        };
        disconnect.Click+=(_,_)=>{pending?.Cancel();try{vault.Disconnect("Hevy");status.Text="Not connected";disconnect.IsEnabled=false;}catch(IOException){status.Text="Could not disconnect. Try again.";}};return section;
    }
    private UIElement BuildFutureIntegrations()
    {
        var page=Page("Future Integrations","Google Tasks, Google Calendar and Outlook Calendar are not offered as new connections in this alpha interface.");
        foreach(var name in new[]{"Google Tasks","Google Calendar","Outlook Calendar"})page.Children.Add(new TextBlock{Text=name+" - Coming soon",FontSize=17,Margin=new Thickness(0,10,0,10)});
        page.Children.Add(new Expander{Header="Existing provider configuration",Content=BuildIntegrations(true),Margin=new Thickness(0,18,0,0)});
        return new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
