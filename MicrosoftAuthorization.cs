using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

internal static class MicrosoftAuthorization
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(20) };
    private const string Scope = "offline_access Calendars.Read User.Read";
    internal static void Configure(IntegrationVault vault, string clientId)
    {
        if (!Guid.TryParse(clientId, out var id)) throw new IntegrationException("Enter the application (client) ID from a Microsoft public client registration.");
        vault.Write("Microsoft app", new IntegrationCredential { ClientId = id.ToString() });
    }
    internal static async Task Connect(IntegrationVault vault, CancellationToken cancellation)
    {
        var app = vault.Read("Microsoft app") ?? throw new IntegrationException("Set up a Microsoft public client application first. See docs/INTEGRATIONS.md.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromMinutes(3));
        var token = timeout.Token;
        var state = GoogleAuthorization.Base64Url(RandomNumberGenerator.GetBytes(32));
        var verifier = GoogleAuthorization.Base64Url(RandomNumberGenerator.GetBytes(64));
        var challenge = GoogleAuthorization.Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        try
        {
            var redirect = $"http://localhost:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            var fields = new Dictionary<string, string>
            {
                ["client_id"] = app.ClientId, ["response_type"] = "code", ["redirect_uri"] = redirect,
                ["response_mode"] = "query", ["scope"] = Scope, ["state"] = state,
                ["code_challenge"] = challenge, ["code_challenge_method"] = "S256"
            };
            var url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?"
                + string.Join("&", fields.Select(p => IntegrationClient.Encode(p.Key) + "=" + IntegrationClient.Encode(p.Value)));
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            string code = "";
            while (code.Length == 0)
            {
                using var incoming = await listener.AcceptTcpClientAsync(token);
                using var stream = incoming.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(token); readTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                var header = new StringBuilder();
                while (header.Length < 8192 && !header.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
                { var buffer = new char[1]; if (await reader.ReadAsync(buffer, readTimeout.Token) == 0) break; header.Append(buffer[0]); }
                var first = header.ToString().Split('\r')[0].Split(' ');
                Dictionary<string, string> query = [];
                if (first.Length >= 2 && first[0] == "GET" && first[1].StartsWith("/?", StringComparison.Ordinal))
                    try { query = GoogleAuthorization.ParseQuery(first[1][1..]); } catch (ArgumentException) { }
                var valid = query.GetValueOrDefault("state") == state;
                var message = valid ? "Nexa received your response. You can close this tab and return to the app." : "Invalid authorization response.";
                var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {(valid ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\nConnection: close\r\nCache-Control: no-store\r\n\r\n{message}");
                await stream.WriteAsync(response, token);
                if (!valid) continue;
                if (query.ContainsKey("error")) throw new IntegrationException("Microsoft sign-in was canceled or denied.");
                code = query.GetValueOrDefault("code") ?? "";
            }
            var exchange = new Dictionary<string, string>
            {
                ["client_id"] = app.ClientId, ["scope"] = Scope, ["code"] = code,
                ["redirect_uri"] = redirect, ["grant_type"] = "authorization_code", ["code_verifier"] = verifier
            };
            using var result = await Http.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", new FormUrlEncodedContent(exchange), token);
            if (!result.IsSuccessStatusCode) throw new IntegrationException("Microsoft could not authorize Nexa. Check the public client registration and redirect URI.");
            using var document = JsonDocument.Parse(await result.Content.ReadAsStringAsync(token));
            var root = document.RootElement; var access = IntegrationClient.Text(root, "access_token");
            if (access.Length == 0 || !root.TryGetProperty("expires_in", out var expiry) || !expiry.TryGetInt32(out var seconds) || seconds <= 0)
                throw new IntegrationException("Microsoft returned incomplete authorization. Try connecting again.");
            token.ThrowIfCancellationRequested();
            vault.Write("Outlook Calendar", new IntegrationCredential
            {
                ClientId = app.ClientId, AccessToken = access, RefreshToken = IntegrationClient.Text(root, "refresh_token"),
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(seconds)
            });
        }
        catch (HttpRequestException) { throw new IntegrationException("Offline. Connect when an internet connection is available."); }
        catch (OperationCanceledException) { throw new IntegrationException("Connection canceled or timed out. Try again later."); }
        finally { listener.Stop(); }
    }
}
