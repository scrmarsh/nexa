using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private sealed class IntegrationHandler : HttpMessageHandler
    {
        internal readonly List<(string Method,string Path,string Body)> Requests=[];
        internal Func<HttpRequestMessage,string> Reply = _=>"{}";
        internal HttpStatusCode Status=HttpStatusCode.OK;
        internal bool Offline;
        internal Func<HttpRequestMessage,CancellationToken,Task<string>>? AsyncReply;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            if(Offline) throw new HttpRequestException("test offline");
            var body=request.Content==null?"":await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method.Method,request.RequestUri!.PathAndQuery,body));
            var reply=AsyncReply==null?Reply(request):await AsyncReply(request,cancellationToken);
            return new HttpResponseMessage(Status) { Content=new StringContent(reply,Encoding.UTF8,"application/json") };
        }
    }
    private static void CheckOnboarding106()
    {
        var root=Path.Combine(output,"profiles106"); Directory.CreateDirectory(root);
        var a=new StateStore(Path.Combine(root,"state.json"),"user-a"); var b=new StateStore(Path.Combine(root,"state.json"),"user-b");
        Assert(!a.LoadProfile().OnboardingCompleted,"Fresh installation selects welcome flow");
        var profile=a.LoadProfile(); profile.DisplayName="Student"; profile.UseCases=["Studying / Focus"]; profile.PreferredDashboard="Study"; profile.PreferredWidgets=["Pixel timer","Notes","Focus Reset"]; profile.OnboardingCompleted=true; profile.Theme=AppTheme.Violet; profile.Sounds=true; profile.FocusNudges=true;
        a.SaveProfile(profile);
        Assert(a.LoadProfile().DisplayName=="Student" && a.LoadProfile().OnboardingCompleted,"Profile and completed flag persist");
        Assert(a.LoadAppTheme()==AppTheme.Violet && a.LoadProfile().Sounds && a.LoadProfile().FocusNudges,"Theme and sound / nudge choices persist");
        Assert(b.LoadProfile().DisplayName=="" && !b.LoadProfile().Sounds && b.LoadAppTheme()==AppTheme.Nexa,"Users do not share profile or appearance preferences");
        foreach(var type in GuidedSetup.Types)
        {
            profile.PreferredDashboard=type;
            var recommended=GuidedSetup.Recommendations(type); var record=GuidedSetup.Create(profile,type+" space",recommended,"user-a");
            Assert(record.Current.Data.Widgets.Count<=4 && record.Current.Data.Widgets.Select(w=>w.Kind).Distinct().Count()==record.Current.Data.Widgets.Count,type+" gets small unique recommendations");
            Assert(record.Current.Data.Session?.IsRunning(DateTimeOffset.Now)==true,type+" is ready with configured timing"); WidgetLibrary.Validate(record.Current.Data);
        }
        profile.TimedSessions=false;
        Assert(GuidedSetup.Create(profile,"Untimed",["Notes"],"user-a").Current.Data.Session==null,"Untimed preference affects guided creation");
        var legacyRoot=Path.Combine(output,"legacy106"); Directory.CreateDirectory(legacyRoot);
        var legacy=new StateStore(Path.Combine(legacyRoot,"state.json"),"legacy-user"); var prior=new AppState { Notes="Keep notes" }; legacy.Save(prior);
        File.WriteAllText(Path.Combine(legacyRoot,"preferences.json"),"{\"ThemeMode\":\"Dark\",\"AppTheme\":\"Ember\"}");
        Assert(legacy.LoadProfile().OnboardingCompleted && legacy.LoadAppTheme()==AppTheme.Ember,"Existing user bypasses onboarding and retains legacy theme");
        Assert(legacy.Load().Notes=="Keep notes" && File.Exists(Path.Combine(legacyRoot,"preferences.json")),"Migration retains data and original preferences");
        var vault=new IntegrationVault(a); vault.Write("Todoist",new IntegrationCredential { AccessToken="fixture-token-private" });
        Assert(vault.Read("Todoist")!.AccessToken=="fixture-token-private" && !new IntegrationVault(b).Connected("Todoist"),"DPAPI credentials round-trip only in the owning user's scope");
        var encrypted=Directory.GetFiles(Path.Combine(a.UserPreferencesDirectory,"credentials"))[0];
        Assert(!Encoding.UTF8.GetString(File.ReadAllBytes(encrypted)).Contains("fixture-token-private"),"Credential file does not contain plaintext token");
        var bCredentials=Path.Combine(b.UserPreferencesDirectory,"credentials"); Directory.CreateDirectory(bCredentials); File.Copy(encrypted,Path.Combine(bCredentials,Path.GetFileName(encrypted)));
        Throws<IntegrationException>(()=>new IntegrationVault(b).Read("Todoist"),"Copied credentials cannot be decrypted in another Nexa user scope");
        new IntegrationVault(b).Disconnect("Todoist");
        var recordExport=GuidedSetup.Create(profile,"Export",["Todoist","Calendar"],"user-a");
        var export=Continuity.Export(recordExport,false);
        Assert(!export.Contains("AccessToken") && !export.Contains("RefreshToken") && !export.Contains("fixture-token-private"),"Dashboard exports exclude integration credentials");

        var handler=new IntegrationHandler(); using var http=new HttpClient(handler); var client=new IntegrationClient(a,http);
        handler.Reply=request=>request.RequestUri!.AbsolutePath.Contains("projects")?"{\"results\":[{\"id\":\"p1\",\"name\":\"School\"}],\"next_cursor\":null}":request.Method==HttpMethod.Get?"{\"results\":[{\"id\":\"t1\",\"content\":\"Read chapter\",\"due\":{\"date\":\"2026-10-02\"}}],\"next_cursor\":null}":"{}";
        var tasks=client.TaskProvider("Todoist");
        client.ConnectTodoist("fixture-token-private",default).GetAwaiter().GetResult();
        Assert(client.Vault.Connected("Todoist"),"Todoist connect validates and saves authorization");
        Assert(tasks.Lists(default).GetAwaiter().GetResult().Single().Name=="School","Todoist projects parse");
        Assert(tasks.Tasks("p1",default).GetAwaiter().GetResult().Single().Due==new DateOnly(2026,10,2),"Todoist dated tasks parse");
        tasks.Complete("p1","t1",default).GetAwaiter().GetResult(); tasks.Add("p1","New task",default).GetAwaiter().GetResult();
        Assert(handler.Requests.Any(r=>r.Method=="POST" && r.Path.EndsWith("/tasks/t1/close")) && handler.Requests.Any(r=>r.Body.Contains("project_id")),"Todoist completion and creation use intended API requests");
        var paged=0; handler.Reply=_=>++paged==1?"{\"results\":[{\"id\":\"p1\",\"name\":\"One\"}],\"next_cursor\":\"second-page\"}":"{\"results\":[{\"id\":\"p2\",\"name\":\"Two\"}],\"next_cursor\":null}";
        Assert(tasks.Lists(default).GetAwaiter().GetResult().Count==2 && handler.Requests.Last().Path.Contains("cursor=second-page"),"Provider pagination includes later projects");
        Throws<IntegrationException>(()=>client.Send("Todoist",HttpMethod.Get,"https://www.googleapis.com/calendar/v3/calendars",null,default).GetAwaiter().GetResult(),"Bearer credentials cannot be sent to another provider host");
        handler.Status=HttpStatusCode.Unauthorized; Throws<IntegrationException>(()=>tasks.Lists(default).GetAwaiter().GetResult(),"Authentication failure gives clean reconnect state"); handler.Status=HttpStatusCode.OK;
        handler.Offline=true; Throws<IntegrationException>(()=>tasks.Lists(default).GetAwaiter().GetResult(),"No internet gives clean offline state"); handler.Offline=false;
        vault.Disconnect("Todoist"); Throws<IntegrationException>(()=>tasks.Lists(default).GetAwaiter().GetResult(),"Disconnect prevents further authorized requests");
        foreach(var service in new[] { "Google Tasks","Google Calendar" }) vault.Write(service,new IntegrationCredential { AccessToken="google-fixture",ExpiresAt=DateTimeOffset.UtcNow.AddHours(1) });
        handler.Reply=request=>request.RequestUri!.AbsolutePath.EndsWith("/users/@me/lists")?"{\"items\":[{\"id\":\"l1\",\"title\":\"Classes\"}]}":request.Method==HttpMethod.Get?"{\"items\":[{\"id\":\"t1\",\"title\":\"Assignment\",\"due\":\"2026-10-02T00:00:00Z\"}]}":"{}";
        var googleTasks=client.TaskProvider("Google Tasks");
        Assert(googleTasks.Lists(default).GetAwaiter().GetResult().Single().Name=="Classes" && googleTasks.Tasks("l1",default).GetAwaiter().GetResult().Single().Title=="Assignment","Google Tasks lists and tasks parse");
        googleTasks.Complete("l1","t1",default).GetAwaiter().GetResult(); googleTasks.Add("l1","Essay",default).GetAwaiter().GetResult();
        Assert(handler.Requests.Any(r=>r.Method=="PATCH" && r.Body.Contains("completed")),"Google completion uses status patch");
        handler.Reply=request=>request.RequestUri!.AbsolutePath.EndsWith("calendarList")?"{\"items\":[{\"id\":\"primary\",\"summary\":\"Classes\"}]}":"{\"items\":[{\"summary\":\"Lecture\",\"start\":{\"dateTime\":\"2026-10-02T10:00:00-05:00\"},\"end\":{\"dateTime\":\"2026-10-02T11:00:00-05:00\"}},{\"summary\":\"Reading day\",\"start\":{\"date\":\"2026-10-02\"},\"end\":{\"date\":\"2026-10-03\"}}]}";
        var calendar=client.CalendarProvider(); var requestCount=handler.Requests.Count;
        Assert(calendar.Calendars(default).GetAwaiter().GetResult().Single().Id=="primary" && calendar.Schedule("primary",DateTimeOffset.Now,default).GetAwaiter().GetResult().Count==2,"Calendar lists, timed events and all-day events parse");
        Assert(handler.Requests.Skip(requestCount).All(r=>r.Method=="GET"),"Calendar never issues mutation requests");
        vault.Write("Google Tasks",new IntegrationCredential { AccessToken="expired",RefreshToken="refresh-fixture",ClientId="test.apps.googleusercontent.com",ExpiresAt=DateTimeOffset.MinValue });
        handler.Reply=request=>request.RequestUri!.Host=="oauth2.googleapis.com"?"{\"access_token\":\"renewed-fixture\",\"expires_in\":3600}":"{\"items\":[]}";
        googleTasks.Lists(default).GetAwaiter().GetResult();
        Assert(vault.Read("Google Tasks")!.AccessToken=="renewed-fixture","Expired Google token refreshes securely");
        vault.Write("Google Tasks",new IntegrationCredential { AccessToken="expired",RefreshToken="old-refresh",ClientId="test.apps.googleusercontent.com",ExpiresAt=DateTimeOffset.MinValue });
        handler.Reply=request=>
        {
            if(request.RequestUri!.Host=="oauth2.googleapis.com") vault.Write("Google Tasks",new IntegrationCredential { AccessToken="new-account",ExpiresAt=DateTimeOffset.UtcNow.AddHours(1) });
            return "{\"access_token\":\"old-result\",\"expires_in\":3600}";
        };
        Throws<IntegrationException>(()=>googleTasks.Lists(default).GetAwaiter().GetResult(),"Old refresh cannot replace a newly connected account");
        Assert(vault.Read("Google Tasks")!.AccessToken=="new-account","New account authorization survives stale refresh response");
        Assert(GoogleAuthorization.Base64Url([255,254])=="__4" && GoogleAuthorization.ParseQuery("?state=test&code=a%2Bb")["code"]=="a+b","OAuth PKCE encoding and callback decoding are correct");
        var config=Path.Combine(output,"desktop-fixture.json"); File.WriteAllText(config,"{\"installed\":{\"client_id\":\"fixture.apps.googleusercontent.com\",\"client_secret\":\"fixture-client-value\"}}");
        GoogleAuthorization.ImportConfiguration(vault,config);
        Assert(vault.Read("Google app")!.ClientId=="fixture.apps.googleusercontent.com","Desktop OAuth configuration imports into secure storage");
        vault.Disconnect("Google Tasks"); vault.Disconnect("Google Calendar");
        Assert(!vault.Connected("Google Tasks") && !vault.Connected("Google Calendar"),"Google services disconnect independently");

        var fresh=new StateStore(Path.Combine(output,"wizard106","state.json"));
        var welcome=new OnboardingWindow(fresh); var timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(140) }; var screens=0;
        timer.Tick+=(_,_)=>
        {
            if(screens==0) { Assert(welcome.Step==0,"Welcome starts at first step"); RenderPreview((FrameworkElement)welcome.Content,"onboarding106-welcome"); }
            if(screens==1) welcome.Profile.UseCases=["Studying / Focus"];
            if(screens==3) Assert(Descendants(welcome).OfType<CheckBox>().Count()<12,"Onboarding shows a curated tool list");
            if(screens==4) RenderPreview((FrameworkElement)welcome.Content,"onboarding106-theme");
            if(screens==5) timer.Stop(); screens++; welcome.Advance();
        };
        welcome.Loaded+=(_,_)=>timer.Start(); var accepted=welcome.ShowDialog(); timer.Stop();
        Assert(accepted==true && screens==6 && welcome.Result!=null,"Six-step welcome produces first dashboard");
        Assert(welcome.Result!.PresetOrigin=="Study" && welcome.Result.Current.Data.Widgets.Any(w=>w.Kind=="Focus Reset"),"Selected use case changes recommended space and tools");
        var workspace=new WorkspaceDocument { UserId=fresh.UserId,ActiveDashboardId=welcome.Result!.DashboardId,Dashboards=[welcome.Result] }; fresh.Save(Continuity.Activate(workspace,fresh.UserId)); fresh.SaveProfile(welcome.Profile);
        var window=new MainWindow(fresh); window.Show(); Pump(TimeSpan.FromMilliseconds(150));
        Assert(window.PixelBannerHost.Content==window.PixelPanel && window.state.Widgets.Count<=4,"First dashboard opens as a complete local space");
        var settings=new SettingsDialog(window,fresh,window.state,window,"Profile"); settings.Show(); Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(settings).OfType<TextBlock>().Any(t=>t.Text=="Profile"),"Settings exposes local Profile"); settings.SelectSection("Integrations"); Pump(TimeSpan.FromMilliseconds(50));
        Assert(Descendants(settings).OfType<TextBlock>().Any(t=>t.Text=="Google Calendar"),"Settings has dedicated integration controls"); settings.Close();
        window.state.Widgets.Add(WidgetLibrary.Create("Calendar")); window.RenderWidgets(); Pump(TimeSpan.FromMilliseconds(120));
        Assert(Descendants(window.WidgetCanvas).OfType<TextBlock>().Any(t=>t.Text.Contains("Not connected")),"Disconnected Calendar remains useful without network"); window.Close();
        var repeat=GuidedSetup.Create(welcome.Profile,"Another space",["Notes"],fresh.UserId); var retained=fresh.Load(); var oldCount=retained.Workspace!.Dashboards.Count; retained.Workspace.Dashboards.Add(repeat); retained.Workspace.ActiveDashboardId=repeat.DashboardId; fresh.Save(Continuity.Activate(retained.Workspace,fresh.UserId));
        Assert(fresh.Load().Workspace!.Dashboards.Count==oldCount+1 && fresh.LoadProfile().OnboardingCompleted,"Repeat setup appends a space and keeps previous dashboards");
        CheckContinuityModels();
    }
}
