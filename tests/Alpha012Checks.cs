using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private sealed class FakeReleases(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(respond(request));
    }
    private static void CheckAlpha012()
    {
        Assert(App.VersionLabel == "Nexa 0.1.12 Alpha" && ReleaseNotes.All[0].Title.Contains("Widget System Cleanup"), "Current version and release history");
        var visible = WidgetLibrary.Definitions.Select(d => d.Name).ToList();
        Assert(visible.Contains("Task List") && visible.Contains("Notes") && visible.Contains("Counter") && !visible.Contains("Agenda") && !visible.Contains("Distraction counter"), "Consolidated Add Widget catalog");
        Assert(WidgetLibrary.Definition("Focus Reset").Group == "Mindfulness", "Focus Reset category");
        var legacy = new DashboardData { WidgetLayoutInitialized = true, Notes = "Old note", Tasks = [new TaskItem { Text = "Old task" }] };
        var agenda = WidgetLibrary.Create("Agenda"); agenda.Kind = "Agenda"; agenda.Text = "Review next steps"; agenda.Title = "My agenda"; agenda.Column = 5; agenda.Row = 12;
        var passage = WidgetLibrary.Create("Main passage"); passage.Kind = "Main passage"; passage.Text = "John 3:16";
        var topics = WidgetLibrary.Create("Topics"); topics.Kind = "Topics"; topics.Items.Add(new TaskItem { Text = "Growth", Done = true });
        var counter = WidgetLibrary.Create("Distraction counter"); counter.Kind = "Distraction counter"; counter.Value = 4;
        var notes = WidgetLibrary.Create("Notes", "notes");
        var tasks = WidgetLibrary.Create("Tasks", "tasks"); tasks.Kind = "Tasks";
        legacy.Widgets.AddRange([agenda, passage, topics, counter, notes, tasks]);
        WidgetLibrary.Validate(legacy);
        Assert(WidgetLibrary.PrepareLayout(legacy) && !WidgetLibrary.PrepareLayout(legacy), "Migration accepts old kinds and is idempotent");
        Assert(agenda.Kind == "Task List" && agenda.Title == "My agenda" && agenda.Items.Single().Text == "Review next steps" && agenda.Column == 5 && agenda.Row == 12, "Agenda content, title and placement preserved");
        Assert(passage.Items.Single().Text == "John 3:16" && topics.Kind == "Notes" && topics.Text.Contains("Growth"), "Passage and topics content preserved");
        Assert(counter.Kind == "Counter" && counter.CounterMode == "Distraction" && counter.Value == 4, "Distraction count preserved");
        Assert(notes.Text == "Old note" && tasks.Items.Single().Text == "Old task" && ReferenceEquals(tasks.Items, legacy.Tasks), "Linked legacy notes and tasks preserved");
        var migrationStore = new StateStore(Path.Combine(output, "migration-state.json"));
        var saved = new AppState { WidgetLayoutInitialized = true, Widgets = legacy.Widgets, Notes = legacy.Notes, Tasks = legacy.Tasks };
        migrationStore.Save(saved);
        var reopened = migrationStore.Load();
        Assert(reopened.Widgets.Count == legacy.Widgets.Count && reopened.Widgets.Single(w => w.Id == agenda.Id).Items.Single().Text == "Review next steps" && reopened.Widgets.Single(w => w.Id == counter.Id).CounterMode == "Distraction", "Migrated widgets survive save and reopen");
        var another = WidgetLibrary.Create("Task List"); Assert(another.Id != tasks.Id && another.Items.Count == 0, "Independent task instances");
        var clock = WidgetLibrary.Create("Clock"); Assert(clock.DisplayMode == "Compact" && WidgetLibrary.MinimumHeight(clock) == 96, "Clock compact mode");
        clock.DisplayMode = "Expanded"; Assert(WidgetLibrary.MinimumWidth(clock) >= 340 && WidgetLibrary.MinimumHeight(clock) >= 288, "Clock expanded minimum");
        var timer = WidgetLibrary.Create("Timer"); timer.DisplayMode = "Expanded"; Assert(WidgetLibrary.MinimumHeight(timer) >= 288, "Timer expanded minimum");
        Assert(ExperimentalUpdater.IsNewer(new Version(0, 1, 10), "Alpha", new Version(0, 1, 9)) && !ExperimentalUpdater.IsNewer(new Version(0, 1, 9), "Alpha", new Version(0, 1, 10)), "Numeric version comparison");
        Assert(ExperimentalUpdater.IsNewer(new Version(0, 1, 12), "Beta", new Version(0, 1, 12)) && !ExperimentalUpdater.IsNewer(new Version(0, 1, 12), "Alpha", new Version(0, 1, 12)), "Release channel comparison");
        var bytes = Encoding.UTF8.GetBytes("Synthetic installer payload for verification");
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var name = "Nexa-0.1.13-Alpha-Setup.exe";
        var url = $"https://github.com/scrmarsh/SessionDashboard/releases/download/v0.1.13-alpha/{name}";
        string Json(string digest, string assetUrl = "") => System.Text.Json.JsonSerializer.Serialize(new[] { new { tag_name="v0.1.13-alpha", name="Next Alpha", body="Release notes", published_at="2026-10-04T12:00:00Z", draft=false, prerelease=true, assets=new[] { new { name, size=bytes.Length, digest, browser_download_url=assetUrl.Length == 0 ? url : assetUrl } } } });
        HttpResponseMessage Respond(HttpRequestMessage request) => new(HttpStatusCode.OK) { RequestMessage=request, Content=request.RequestUri!.Host == "api.github.com" ? new StringContent(Json("sha256:"+hash)) : new ByteArrayContent(bytes) };
        using var client = new HttpClient(new FakeReleases(Respond));
        var updater = new ExperimentalUpdater(client);
        var release = Task.Run(() => updater.CheckAsync(new Version(0,1,12))).GetAwaiter().GetResult();
        Assert(release != null && release.Version == new Version(0,1,13) && release.Notes == "Release notes" && release.PublishedAt?.UtcDateTime.Year == 2026, "Published release metadata and exact setup asset");
        var path = Task.Run(() => updater.DownloadAsync(release!, Path.Combine(output,"updates"))).GetAwaiter().GetResult();
        Assert(File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(bytes), "Verified installer download");
        using var badClient = new HttpClient(new FakeReleases(request => new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage=request, Content=new StringContent(Json("sha256:"+hash, "https://evil.example/setup.exe")) }));
        Assert(Task.Run(() => new ExperimentalUpdater(badClient).CheckAsync(new Version(0,1,12))).GetAwaiter().GetResult() == null, "Untrusted asset URL rejected");
        using var betaClient = new HttpClient(new FakeReleases(request => new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage=request, Content=new StringContent(Json("sha256:"+hash).Replace("0.1.13-alpha", "0.2.0-beta", StringComparison.Ordinal).Replace("0.1.13-Alpha", "0.2.0-Beta", StringComparison.Ordinal)) }));
        Assert(Task.Run(() => new ExperimentalUpdater(betaClient).CheckAsync(new Version(0,1,12))).GetAwaiter().GetResult() == null, "Alpha channel does not offer Beta releases");
        using var mismatchClient = new HttpClient(new FakeReleases(request => new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage=request, Content=request.RequestUri!.Host == "api.github.com" ? new StringContent(Json("sha256:"+new string('0',64))) : new ByteArrayContent(bytes) }));
        var mismatch = new ExperimentalUpdater(mismatchClient);
        var badRelease = Task.Run(() => mismatch.CheckAsync(new Version(0,1,12))).GetAwaiter().GetResult();
        var failed = false;
        try { Task.Run(() => mismatch.DownloadAsync(badRelease!, Path.Combine(output,"bad-updates"))).GetAwaiter().GetResult(); } catch (InvalidDataException) { failed = true; }
        Assert(failed && !Directory.GetFiles(Path.Combine(output,"bad-updates")).Any(), "Digest mismatch leaves no installer");
        var store = new StateStore(Path.Combine(output,"update-state.json"));
        Assert(!store.LoadUpdatePreferences().CheckOnStartup, "Startup check defaults off");
        store.SaveUpdatePreferences(new UpdatePreferences { CheckOnStartup = true });
        Assert(store.LoadUpdatePreferences().CheckOnStartup, "Owner-scoped update preference persists");
        var uiStore = new StateStore(Path.Combine(output,"ui-state.json"));
        uiStore.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Clock"), WidgetLibrary.Create("Timer")] });
        var window = new MainWindow(uiStore); window.Show(); Pump(TimeSpan.FromMilliseconds(130));
        Assert(Descendants(window).OfType<Button>().Count(b => Equals(b.Content,"Compact")) >= 2 && Descendants(window).OfType<Button>().Count(b => Equals(b.Content,"Expanded")) >= 2, "Clock and Timer expose direct size modes");
        var settings = new SettingsDialog(window,uiStore,window.state,window,"Updates"); settings.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text.StartsWith("Current version: Nexa 0.1.12 Alpha")) && Descendants(settings).OfType<Button>().Any(b => Equals(b.Content,"Check for updates")), "Experimental Updates is visible in Settings");
        settings.Close(); window.Close();
    }
}
