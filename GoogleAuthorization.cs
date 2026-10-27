using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

internal static class GoogleAuthorization
{
    private static readonly HttpClient Http=new(new HttpClientHandler { AllowAutoRedirect=false }) { Timeout=TimeSpan.FromSeconds(20) };
    internal static string Base64Url(byte[] bytes)=>Convert.ToBase64String(bytes).TrimEnd('=').Replace('+','-').Replace('/','_');
    internal static Dictionary<string,string> ParseQuery(string query)=>query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(part=>part.Split('=',2)).ToDictionary(p=>Uri.UnescapeDataString(p[0]),p=>p.Length>1?Uri.UnescapeDataString(p[1].Replace('+',' ')):"");
    internal static void ImportConfiguration(IntegrationVault vault,string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(path));
        if(doc.RootElement.ValueKind!=JsonValueKind.Object || !doc.RootElement.TryGetProperty("installed",out var installed) || installed.ValueKind!=JsonValueKind.Object) throw new IntegrationException("Choose a Google desktop app credentials file, not a web application file.");
        var id=IntegrationClient.Text(installed,"client_id");
        if(!id.EndsWith(".apps.googleusercontent.com",StringComparison.Ordinal) || id.Length>300) throw new IntegrationException("Invalid Google desktop client configuration.");
        vault.Write("Google app",new IntegrationCredential { ClientId=id,ClientSecret=IntegrationClient.Text(installed,"client_secret") });
    }
    internal static async Task Connect(IntegrationVault vault,string service,CancellationToken cancellation)
    {
        var app=vault.Read("Google app")??throw new IntegrationException("Set up a Google desktop app connection first. See docs/INTEGRATIONS.md.");
        var scope=service=="Google Tasks"?"https://www.googleapis.com/auth/tasks":"https://www.googleapis.com/auth/calendar.readonly";
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromMinutes(3));
        var token=timeout.Token; var state=Base64Url(RandomNumberGenerator.GetBytes(32)); var verifier=Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge=Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var listener=new TcpListener(IPAddress.Loopback,0); listener.Start();
        try
        {
            var redirect=$"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            var fields=new Dictionary<string,string> { ["client_id"]=app.ClientId,["redirect_uri"]=redirect,["response_type"]="code",["scope"]=scope,["state"]=state,["code_challenge"]=challenge,["code_challenge_method"]="S256",["access_type"]="offline",["prompt"]="consent" };
            var url="https://accounts.google.com/o/oauth2/v2/auth?"+string.Join("&",fields.Select(p=>IntegrationClient.Encode(p.Key)+"="+IntegrationClient.Encode(p.Value)));
            Process.Start(new ProcessStartInfo(url) { UseShellExecute=true });
            string code="";
            while(code.Length==0)
            {
                using var incoming=await listener.AcceptTcpClientAsync(token); using var stream=incoming.GetStream();
                using var reader=new StreamReader(stream,Encoding.ASCII,false,1024,true);
                using var readTimeout=CancellationTokenSource.CreateLinkedTokenSource(token); readTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var header=new StringBuilder();
                while(header.Length<8192 && !header.ToString().EndsWith("\r\n\r\n",StringComparison.Ordinal))
                { var buffer=new char[1]; if(await reader.ReadAsync(buffer,readTimeout.Token)==0) break; header.Append(buffer[0]); }
                var first=header.ToString().Split('\r')[0].Split(' '); Dictionary<string,string> query=[];
                if(first.Length>=2 && first[0]=="GET" && first[1].StartsWith("/?",StringComparison.Ordinal))
                    try { query=ParseQuery(first[1][1..]); } catch(ArgumentException) { }
                var valid=query.GetValueOrDefault("state")==state;
                var text=valid?"Nexa received your response. You can close this tab and return to the app.":"Invalid authorization response.";
                var response=Encoding.ASCII.GetBytes($"HTTP/1.1 {(valid?"200 OK":"400 Bad Request")}\r\nContent-Type: text/plain\r\nContent-Length: {text.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n{text}");
                await stream.WriteAsync(response,token);
                if(!valid) continue;
                if(query.ContainsKey("error")) throw new IntegrationException("Google sign-in was canceled or denied.");
                code=query.GetValueOrDefault("code")??"";
            }
            var exchange=new Dictionary<string,string> { ["client_id"]=app.ClientId,["code"]=code,["code_verifier"]=verifier,["redirect_uri"]=redirect,["grant_type"]="authorization_code" };
            if(app.ClientSecret.Length>0) exchange["client_secret"]=app.ClientSecret;
            using var result=await Http.PostAsync("https://oauth2.googleapis.com/token",new FormUrlEncodedContent(exchange),token);
            if(!result.IsSuccessStatusCode) throw new IntegrationException("Google could not authorize Nexa. Check your desktop app configuration and try again.");
            using var document=JsonDocument.Parse(await result.Content.ReadAsStringAsync(token)); var root=document.RootElement;
            var access=IntegrationClient.Text(root,"access_token"); if(access.Length==0) throw new IntegrationException("Google returned no authorization.");
            if(root.ValueKind!=JsonValueKind.Object || !root.TryGetProperty("expires_in",out var expiry) || expiry.ValueKind!=JsonValueKind.Number || !expiry.TryGetInt32(out var seconds) || seconds<=0) throw new IntegrationException("Google returned incomplete authorization. Try connecting again.");
            token.ThrowIfCancellationRequested();
            vault.Write(service,new IntegrationCredential { ClientId=app.ClientId,ClientSecret=app.ClientSecret,AccessToken=access,RefreshToken=IntegrationClient.Text(root,"refresh_token"),ExpiresAt=DateTimeOffset.UtcNow.AddSeconds(seconds) });
        }
        catch(HttpRequestException) { throw new IntegrationException("Offline. Connect when an internet connection is available."); }
        catch(OperationCanceledException) { throw new IntegrationException("Connection canceled or timed out. You can try again later."); }
        finally { listener.Stop(); }
    }
}
