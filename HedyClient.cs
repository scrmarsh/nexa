using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

public sealed record HedySessionInfo(string Id, string Title)
{
    public override string ToString() => Title;
}

public sealed class HedyClient : IDisposable
{
    private readonly HttpClient client;
    public HedyClient(string key, string region, HttpMessageHandler? handler = null)
    {
        client = handler == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) : new HttpClient(handler);
        client.BaseAddress = new Uri(region == "EU" ? "https://eu-api.hedy.bot/" : "https://api.hedy.bot/");
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
    }
    private async Task<JsonElement> Get(string path, CancellationToken token)
    {
        using var response = await client.GetAsync(path, token);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Hedy returned HTTP {(int)response.StatusCode}. Check your API key, region, and cloud sync.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        var root = document.RootElement;
        return (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) ? data : root).Clone();
    }
    public async Task<List<HedySessionInfo>> Sessions(CancellationToken token)
    {
        var data = await Get("sessions?limit=50", token);
        if (data.ValueKind != JsonValueKind.Array) throw new JsonException("Hedy session list was not an array.");
        var result = new List<HedySessionInfo>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("sessionId", out var id) || id.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(id.GetString())) continue;
            var title = item.TryGetProperty("title", out var name) && name.ValueKind == JsonValueKind.String ? name.GetString() : null;
            result.Add(new HedySessionInfo(id.GetString()!, string.IsNullOrWhiteSpace(title) ? "Untitled session" : title));
        }
        return result;
    }
    public async Task<string> Transcript(string id, CancellationToken token)
    {
        var data = await Get("sessions/" + Uri.EscapeDataString(id), token);
        if (data.ValueKind != JsonValueKind.Object) throw new JsonException("Hedy session detail was not an object.");
        return data.TryGetProperty("transcript", out var transcript) && transcript.ValueKind == JsonValueKind.String ? transcript.GetString() ?? "" : "";
    }
    public void Dispose() => client.Dispose();
}

public static class HedyNotes
{
    public static string Apply(Session session, string transcript, DateTimeOffset received)
    {
        if (transcript == session.HedyTranscript) return "No new transcript text";
        var previous = session.HedyTranscript;
        session.HedyTranscript = transcript;
        if (previous.Length == 0)
        {
            session.HedyBaseline = transcript;
            return "Existing transcript loaded separately; new text will go into live intervals";
        }
        if (!session.IsRunning(received)) return "Transcript updated; no active interval";
        if (!transcript.StartsWith(previous, StringComparison.Ordinal)) return "Hedy revised its transcript; latest full text updated (interval notes preserved)";
        var delta = transcript[previous.Length..].Trim();
        if (delta.Length == 0) return "No new transcript text";
        var index = (int)((received - session.Start).TotalMinutes / session.IntervalMinutes);
        session.HedyIntervalNotes.TryGetValue(index, out var old);
        session.HedyIntervalNotes[index] = (old ?? "") + $"[{session.HedySourceTitle} · {received.LocalDateTime:h:mm:ss tt} · received]\n{delta}\n\n";
        return $"New text added to interval {index + 1}";
    }
}

public static class SecretStorage
{
    public static string Protect(string key) => Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser));
    public static string Unprotect(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return "";
        try { return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser)); }
        catch (Exception ex) when (ex is CryptographicException or FormatException) { return ""; }
    }
}
