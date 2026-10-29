using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SessionDashboard;

internal sealed class UpdatePreferences
{
    public bool CheckOnStartup { get; set; } = true; // Legacy field: discovery is always enabled.
    public bool AutomaticUpdates { get; set; } = true;
    public bool UpdateChoiceMade { get; set; }
    public string Channel {get;set;}="Automatic";
    public string LastCheckError {get;set;}="";
    public DateTimeOffset? LastSuccessfulCheckUtc { get; set; }
    public string LastOfferedVersion { get; set; } = "";
}

public sealed partial class StateStore
{
    private string UpdatePreferencesPath => Path.Combine(UserPreferencesDirectory, "update-preferences.json");
    internal UpdatePreferences LoadUpdatePreferences()
    {
        try { return File.Exists(UpdatePreferencesPath) ? JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(UpdatePreferencesPath), Options) ?? new() : new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    internal void SaveUpdatePreferences(UpdatePreferences value)
    {
        Directory.CreateDirectory(UserPreferencesDirectory);
        AtomicWrite(UpdatePreferencesPath, JsonSerializer.Serialize(value, Options), false);
    }
}

internal sealed record AvailableUpdate(Version Version, string Channel, string Name, string Notes, DateTimeOffset? PublishedAt, Uri Url, long Size, string Sha256)
{
    internal Version CanonicalVersion=>NexaReleaseIdentity.Canonicalize(Version,Channel);
    internal string DisplayLabel=>new NexaReleaseIdentity(Version,Channel).Label;
    internal bool Portable => !Path.GetFileName(Url.AbsolutePath).EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase);
}

internal sealed class ExperimentalUpdater(HttpClient client)
{
    internal static readonly Uri ReleasesUri = new("https://api.github.com/repos/scrmarsh/nexa/releases?per_page=30");
    private static readonly Regex Digest = new(@"^sha256:([a-fA-F0-9]{64})$", RegexOptions.CultureInvariant);
    internal static int ChannelRank(string channel)=>NexaReleaseIdentity.Rank(channel);
    internal static bool IsNewer(Version candidate,string channel,Version current,string currentChannel="Alpha")=>NexaReleaseIdentity.Newer(candidate,channel,current,currentChannel);
    private NexaReleaseIdentity? newestRejected;
    internal bool HasEligiblePackage {get;private set;}
    internal AvailableUpdate? LatestEligible {get;private set;}
    internal static string DescribeFailure(Exception error)=>error switch
    {
        HttpRequestException{StatusCode:System.Net.HttpStatusCode.NotFound}=>"GitHub release feed unavailable (404). This does not mean nexa is up to date.",
        HttpRequestException{StatusCode:System.Net.HttpStatusCode.Forbidden or (System.Net.HttpStatusCode)429}=>"GitHub rate limited or restricted this request. Try again later.",
        HttpRequestException{StatusCode:null}=>"No internet connection, DNS failure or GitHub connection unavailable. "+error.Message,
        HttpRequestException=>"GitHub is unavailable. "+error.Message,
        TaskCanceledException=>"The update request timed out or was canceled. Try again.",
        InvalidDataException=>"Invalid or incomplete release metadata: "+error.Message,
        _=>error.Message
    };
    internal async Task<AvailableUpdate?> CheckAsync(Version current, CancellationToken cancellationToken = default, string currentChannel = "Alpha",string? channelPreference=null)
    {
        var latest = await DiscoverAsync(cancellationToken, channelPreference is null or "Automatic"?currentChannel:channelPreference);
        if(newestRejected is {} rejected && IsNewer(rejected.Version,rejected.Stage,current,currentChannel) && (latest==null || IsNewer(rejected.Version,rejected.Stage,latest.Version,latest.Channel)))throw new InvalidDataException("A newer published release is missing a verified compatible nexa asset. Check Settings again after the release package is complete.");
        LatestEligible=latest;
        return latest != null && IsNewer(latest.Version, latest.Channel, current, currentChannel) ? latest : null;
    }
    internal async Task<AvailableUpdate?> DiscoverAsync(CancellationToken cancellationToken = default, string currentChannel = "Alpha")
    {
        AvailableUpdate? latest = null;newestRejected=null;HasEligiblePackage=false;
        for (var page = 1; page <= 10; page++)
        {
        using var request = new HttpRequestMessage(HttpMethod.Get, page == 1 ? ReleasesUri : new Uri(ReleasesUri.AbsoluteUri + "&page=" + page));
        request.Headers.UserAgent.ParseAdd("Nexa-Desktop/"+App.AppVersion);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if ((int)response.StatusCode is 403 or 429) throw new HttpRequestException("GitHub rate limit or access restriction. Try again later; public release checks require no login.",null,response.StatusCode);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw new HttpRequestException("The public nexa releases feed was not found. Check network access to api.github.com/repos/scrmarsh/nexa/releases.",null,response.StatusCode);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Array) throw new InvalidDataException("Unexpected release response.");
        foreach (var release in document.RootElement.EnumerateArray())
        {
            try
            {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
            var tag = release.GetProperty("tag_name").GetString() ?? "";
            var identity=NexaReleaseIdentity.Parse(tag,release.TryGetProperty("prerelease",out var pre) && pre.GetBoolean());
            if(identity==null)continue;var version=identity.Version;var channel=identity.Stage;
            if(ChannelRank(channel)<ChannelRank(currentChannel))continue;
            if(newestRejected==null || IsNewer(version,channel,newestRejected.Version,newestRejected.Stage))newestRejected=identity;
            if(latest!=null && !IsNewer(version,channel,latest.Version,latest.Channel))continue;
            var exactName = $"Nexa-{version.ToString(3)}-{channel}-Setup.exe";
            if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) continue;
            foreach (var asset in assets.EnumerateArray())
            {
                var assetName = asset.GetProperty("name").GetString() ?? "";
                if (assetName != exactName && !assetName.Equals("Nexa.exe", StringComparison.OrdinalIgnoreCase) && !assetName.Equals($"Nexa-{version.ToString(3)}-{channel}.exe", StringComparison.OrdinalIgnoreCase)) continue;
                var digest = Digest.Match(asset.TryGetProperty("digest", out var hash) ? hash.GetString() ?? "" : "");
                if (!digest.Success || !asset.TryGetProperty("size", out var length) || !length.TryGetInt64(out var size) || size is < 1 or > 500_000_000) continue;
                if (!Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var url) || url.Scheme != "https" || url.Host != "github.com" || !url.AbsolutePath.StartsWith("/scrmarsh/nexa/releases/download/", StringComparison.OrdinalIgnoreCase) || !url.AbsolutePath.EndsWith("/" + assetName, StringComparison.Ordinal)) continue;
                DateTimeOffset? published = release.TryGetProperty("published_at", out var date) && DateTimeOffset.TryParse(date.GetString(), out var parsed) ? parsed : null;
                if (latest != null && latest.Version == version && latest.Channel == channel && latest.Portable && assetName.EndsWith("-Setup.exe", StringComparison.OrdinalIgnoreCase)) continue;
                HasEligiblePackage=true;
                latest = new(version, channel, release.GetProperty("name").GetString() ?? $"nexa {version} {channel}", release.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "", published, url, size, digest.Groups[1].Value.ToUpperInvariant());
            }
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException or FormatException) { /* Ignore one malformed release without hiding valid older releases. */ }
        }
        if (!response.Headers.TryGetValues("Link", out var links) || !links.Any(value => value.Contains("rel=\"next\"", StringComparison.Ordinal))) break;
        }
        return latest;
    }

    internal async Task<string> DownloadAsync(AvailableUpdate release, string directory, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!Regex.IsMatch(release.Sha256, "^[a-fA-F0-9]{64}$") || release.Size is < 1 or > 500_000_000 || release.Url.Scheme != "https" || release.Url.Host != "github.com" || !release.Url.AbsolutePath.StartsWith("/scrmarsh/nexa/releases/download/", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Untrusted update descriptor.");
        Directory.CreateDirectory(directory);
        var name = Path.GetFileName(release.Url.AbsolutePath);
        var final = Path.Combine(directory, name);
        var temporary = final + "." + Guid.NewGuid().ToString("N") + ".download";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, release.Url);
            request.Headers.UserAgent.ParseAdd("Nexa-Desktop/"+App.AppVersion);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri is not { Scheme: "https" } actual || actual.Host is not ("github.com" or "release-assets.githubusercontent.com" or "objects.githubusercontent.com") || actual.Host == "github.com" && !actual.AbsolutePath.StartsWith("/scrmarsh/nexa/releases/download/", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Installer redirect is not an official release URL.");
            if (response.Content.Headers.ContentLength is long declared && declared != release.Size) throw new InvalidDataException("Installer size changed.");
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var target = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            using var sha = SHA256.Create();
            var buffer = new byte[81920]; long received = 0;
            while (true)
            {
                var count = await source.ReadAsync(buffer, cancellationToken);
                if (count == 0) break;
                received += count;
                if (received > release.Size) throw new InvalidDataException("Installer exceeds published size.");
                sha.TransformBlock(buffer, 0, count, null, 0);
                await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                progress?.Report((double)received / release.Size);
            }
            sha.TransformFinalBlock([], 0, 0);
            if (received != release.Size || !CryptographicOperations.FixedTimeEquals(Convert.FromHexString(release.Sha256), sha.Hash!)) throw new InvalidDataException("Installer verification failed.");
            await target.FlushAsync(cancellationToken);
            target.Close();
            File.Move(temporary, final, true);
            return final;
        }
        catch { if (File.Exists(temporary)) File.Delete(temporary); throw; }
    }
}
