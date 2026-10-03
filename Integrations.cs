using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

internal sealed class IntegrationCredential
{
    public Guid ConnectionId { get; set; } = Guid.NewGuid();
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
internal sealed class IntegrationVault(StateStore store)
{
    private string PathFor(string service) => Path.Combine(store.UserPreferencesDirectory,"credentials",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(service)))+".protected");
    private byte[] Entropy => SHA256.HashData(Encoding.UTF8.GetBytes("Nexa:"+store.UserId));
    internal IntegrationCredential? Read(string service)
    {
        var path=PathFor(service); if(!File.Exists(path)) return null;
        try { var bytes=ProtectedData.Unprotect(File.ReadAllBytes(path),Entropy,DataProtectionScope.CurrentUser); return JsonSerializer.Deserialize<IntegrationCredential>(bytes); }
        catch(Exception ex) when(ex is CryptographicException or JsonException) { throw new IntegrationException("Saved authorization could not be read. Disconnect and reconnect this service."); }
    }
    internal void Write(string service,IntegrationCredential value)
    {
        var path=PathFor(service); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        byte[] encrypted;
        try { encrypted=ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(value),Entropy,DataProtectionScope.CurrentUser); }
        catch(CryptographicException) { throw new IntegrationException("Windows secure storage is unavailable. Run Nexa in your normal Windows account and try again."); }
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllBytes(temporary,encrypted); File.Move(temporary,path,true); }
        finally { if(File.Exists(temporary)) File.Delete(temporary); }
    }
    internal void Disconnect(string service) { var path=PathFor(service); if(File.Exists(path)) File.Delete(path); }
    internal bool Connected(string service) => File.Exists(PathFor(service));
}
internal sealed class IntegrationException(string message) : Exception(message);
internal sealed record ExternalList(string Id,string Name) { public override string ToString()=>Name; }
internal sealed record ExternalTask(string Id,string Title,DateOnly? Due);
internal sealed record CalendarEntry(string Title,DateTimeOffset Start,DateTimeOffset End,bool AllDay);
internal interface ITaskProvider
{
    Task<List<ExternalList>> Lists(CancellationToken token);
    Task<List<ExternalTask>> Tasks(string list,CancellationToken token);
    Task Complete(string list,string id,CancellationToken token);
    Task Add(string list,string title,CancellationToken token);
}
internal interface ICalendarProvider
{
    Task<List<ExternalList>> Calendars(CancellationToken token);
    Task<List<CalendarEntry>> Schedule(string calendar,DateTimeOffset day,CancellationToken token);
}

internal sealed class IntegrationClient
{
    private static readonly HttpClient Shared = new(new HttpClientHandler { AllowAutoRedirect=false }) { Timeout=TimeSpan.FromSeconds(15) };
    internal readonly IntegrationVault Vault;
    private readonly HttpClient http;
    private readonly SemaphoreSlim refreshLock=new(1);
    internal IntegrationClient(StateStore store,HttpClient? http=null) { Vault=new(store); this.http=http??Shared; }
    internal static string Encode(string value)=>Uri.EscapeDataString(value);
    internal static string Text(JsonElement item,string field)=>item.ValueKind==JsonValueKind.Object && item.TryGetProperty(field,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
    internal async Task<JsonDocument> Send(string service,HttpMethod method,string url,object? body,CancellationToken token,string? overrideToken=null,Guid? expectedConnection=null)
    {
        var uri=new Uri(url);
        var expected=service switch { "Todoist"=>"api.todoist.com","Google Tasks"=>"tasks.googleapis.com","Google Calendar"=>"www.googleapis.com",_=>"" };
        if(uri.Scheme!="https" || uri.Host!=expected) throw new IntegrationException("Invalid integration endpoint.");
        var access=overrideToken??await Access(service,token,expectedConnection);
        if(string.IsNullOrWhiteSpace(access) || access.Any(c=>c<=32 || c>=127)) throw new IntegrationException("Invalid authorization. Reconnect in Settings / Integrations.");
        using var request=new HttpRequestMessage(method,uri); request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",access);
        if(body!=null) request.Content=JsonContent.Create(body);
        try
        {
            using var response=await http.SendAsync(request,token);
            if(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new IntegrationException("Authorization expired or permission was denied. Reconnect in Settings / Integrations.");
            if(response.StatusCode==HttpStatusCode.TooManyRequests) throw new IntegrationException("Service is busy. Wait a little before refreshing.");
            if(!response.IsSuccessStatusCode) throw new IntegrationException("Service could not complete this request. Your local space is unaffected.");
            var content=await response.Content.ReadAsStringAsync(token); return JsonDocument.Parse(string.IsNullOrWhiteSpace(content)?"{}":content);
        }
        catch(HttpRequestException) { throw new IntegrationException("Offline or service unavailable. Local tools still work; refresh when you're connected."); }
        catch(TaskCanceledException) when(!token.IsCancellationRequested) { throw new IntegrationException("Service timed out. Try refreshing later."); }
        catch(JsonException) { throw new IntegrationException("The service returned an unexpected response. Try refreshing later."); }
    }
    private async Task<string> Access(string service,CancellationToken token,Guid? expectedConnection)
    {
        await refreshLock.WaitAsync(token);
        try
        {
            var credential=Vault.Read(service)??throw new IntegrationException("Not connected. Open Settings / Integrations to connect.");
            if(expectedConnection!=null && credential.ConnectionId!=expectedConnection) throw new IntegrationException("Connection changed. Refresh to use the current account.");
            if(service=="Todoist" || credential.ExpiresAt>DateTimeOffset.UtcNow.AddMinutes(1)) return credential.AccessToken;
            if(string.IsNullOrEmpty(credential.RefreshToken)) throw new IntegrationException("Authorization expired. Reconnect in Settings / Integrations.");
            var form=new Dictionary<string,string> { ["client_id"]=credential.ClientId,["refresh_token"]=credential.RefreshToken,["grant_type"]="refresh_token" };
            if(credential.ClientSecret.Length>0) form["client_secret"]=credential.ClientSecret;
            using var response=await http.PostAsync("https://oauth2.googleapis.com/token",new FormUrlEncodedContent(form),token);
            if(!response.IsSuccessStatusCode) throw new IntegrationException("Google authorization expired. Reconnect in Settings / Integrations.");
            using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(token)); credential.AccessToken=Text(doc.RootElement,"access_token");
            if(credential.AccessToken.Length==0) throw new IntegrationException("Google authorization could not be renewed.");
            if(doc.RootElement.ValueKind!=JsonValueKind.Object || !doc.RootElement.TryGetProperty("expires_in",out var expiry) || expiry.ValueKind!=JsonValueKind.Number || !expiry.TryGetInt32(out var seconds) || seconds<=0) throw new IntegrationException("Google authorization could not be renewed.");
            credential.ExpiresAt=DateTimeOffset.UtcNow.AddSeconds(seconds);
            // A disconnect during an in-flight refresh must not reconnect the account.
            if(Vault.Read(service)?.ConnectionId!=credential.ConnectionId) throw new IntegrationException("Connection changed. Refresh to use the current account.");
            Vault.Write(service,credential); return credential.AccessToken;
        }
        catch(HttpRequestException) { throw new IntegrationException("Offline. Refresh when you're connected."); }
        finally { refreshLock.Release(); }
    }
    internal async Task ConnectTodoist(string access,CancellationToken token)
    {
        if(string.IsNullOrWhiteSpace(access)) throw new IntegrationException("Paste your Todoist API token to connect.");
        using var result=await Send("Todoist",HttpMethod.Get,"https://api.todoist.com/api/v1/projects",null,token,access.Trim());
        token.ThrowIfCancellationRequested();
        Vault.Write("Todoist",new IntegrationCredential { AccessToken=access.Trim() });
    }
    internal ITaskProvider TaskProvider(string service,Guid? connection=null)=>new TaskProvider(this,service,connection);
    internal ICalendarProvider CalendarProvider(Guid? connection=null)=>new GoogleCalendarProvider(this,connection);
    internal async Task<List<JsonElement>> Pages(string service,string url,string field,string cursorField,string cursorParameter,CancellationToken token,Guid? expectedConnection=null)
    {
        var all=new List<JsonElement>(); string cursor="";
        for(var page=0;page<20;page++)
        {
            using var doc=await Send(service,HttpMethod.Get,url+(cursor.Length>0?(url.Contains('?')?"&":"?")+cursorParameter+"="+Encode(cursor):""),null,token,expectedConnection:expectedConnection);
            var root=doc.RootElement;
            if(root.ValueKind==JsonValueKind.Array) { AddItems(root); return all; }
            if(root.ValueKind!=JsonValueKind.Object) throw new IntegrationException("Unexpected service response. Refresh to try again.");
            if(root.TryGetProperty(field,out var items)) { if(items.ValueKind!=JsonValueKind.Array) throw new IntegrationException("Unexpected service response. Refresh to try again."); AddItems(items); }
            cursor=Text(root,cursorField); if(cursor.Length==0) return all;
        }
        throw new IntegrationException("This list is too large. Select a smaller project or list.");
        void AddItems(JsonElement items)
        {
            foreach(var item in items.EnumerateArray())
            {
                if(item.ValueKind!=JsonValueKind.Object) throw new IntegrationException("Unexpected service response. Refresh to try again.");
                all.Add(item.Clone());
            }
        }
    }
}

internal sealed class TaskProvider(IntegrationClient client,string service,Guid? connection=null) : ITaskProvider
{
    private bool Todoist=>service=="Todoist";
    private string Base=>Todoist?"https://api.todoist.com/api/v1/":"https://tasks.googleapis.com/tasks/v1/";
    public async Task<List<ExternalList>> Lists(CancellationToken token)
    {
        var items=await client.Pages(service,Base+(Todoist?"projects":"users/@me/lists?maxResults=100"),Todoist?"results":"items",Todoist?"next_cursor":"nextPageToken",Todoist?"cursor":"pageToken",token,connection);
        return items.Select(x=>new ExternalList(IntegrationClient.Text(x,"id"),IntegrationClient.Text(x,Todoist?"name":"title"))).ToList();
    }
    public async Task<List<ExternalTask>> Tasks(string list,CancellationToken token)
    {
        var path=Todoist?"tasks"+(list.Length>0?"?project_id="+IntegrationClient.Encode(list):""):"lists/"+IntegrationClient.Encode(list)+"/tasks?showCompleted=false&showHidden=false&maxResults=100";
        var items=await client.Pages(service,Base+path,Todoist?"results":"items",Todoist?"next_cursor":"nextPageToken",Todoist?"cursor":"pageToken",token,connection);
        return items.Select(x=>
        {
            var due=Todoist && x.TryGetProperty("due",out var d) && d.ValueKind==JsonValueKind.Object?IntegrationClient.Text(d,"date"):IntegrationClient.Text(x,"due");
            DateOnly? date=due.Length>=10 && DateOnly.TryParse(due[..10],System.Globalization.CultureInfo.InvariantCulture,out var value)?value:null;
            return new ExternalTask(IntegrationClient.Text(x,"id"),IntegrationClient.Text(x,Todoist?"content":"title"),date);
        }).ToList();
    }
    public async Task Complete(string list,string id,CancellationToken token)
    {
        using var result=await client.Send(service,Todoist?HttpMethod.Post:HttpMethod.Patch,Base+(Todoist?"tasks/"+IntegrationClient.Encode(id)+"/close":"lists/"+IntegrationClient.Encode(list)+"/tasks/"+IntegrationClient.Encode(id)),Todoist?null:new { status="completed" },token,expectedConnection:connection);
    }
    public async Task Add(string list,string title,CancellationToken token)
    {
        if(string.IsNullOrWhiteSpace(title)) return;
        object body=Todoist?(list.Length>0?new Dictionary<string,string> { ["content"]=title,["project_id"]=list }:new Dictionary<string,string> { ["content"]=title }):new { title };
        using var result=await client.Send(service,HttpMethod.Post,Base+(Todoist?"tasks":"lists/"+IntegrationClient.Encode(list)+"/tasks"),body,token,expectedConnection:connection);
    }
}

internal sealed class GoogleCalendarProvider(IntegrationClient client,Guid? connection=null) : ICalendarProvider
{
    public async Task<List<ExternalList>> Calendars(CancellationToken token)
    {
        var items=await client.Pages("Google Calendar","https://www.googleapis.com/calendar/v3/users/me/calendarList?maxResults=250","items","nextPageToken","pageToken",token,connection);
        return items.Select(x=>new ExternalList(IntegrationClient.Text(x,"id"),IntegrationClient.Text(x,"summary"))).ToList();
    }
    public async Task<List<CalendarEntry>> Schedule(string calendar,DateTimeOffset day,CancellationToken token)
    {
        var start=day.LocalDateTime.Date; var end=start.AddDays(1);
        var url="https://www.googleapis.com/calendar/v3/calendars/"+IntegrationClient.Encode(calendar)+"/events?singleEvents=true&orderBy=startTime&maxResults=250&timeMin="+IntegrationClient.Encode(new DateTimeOffset(start).ToString("o"))+"&timeMax="+IntegrationClient.Encode(new DateTimeOffset(end).ToString("o"));
        var items=await client.Pages("Google Calendar",url,"items","nextPageToken","pageToken",token,connection);
        return items.Where(x=>IntegrationClient.Text(x,"status")!="cancelled").Select(x=>
        {
            if(!x.TryGetProperty("start",out var s) || !x.TryGetProperty("end",out var e) || s.ValueKind!=JsonValueKind.Object || e.ValueKind!=JsonValueKind.Object) throw new IntegrationException("Calendar returned an incomplete event. Refresh to try again.");
            var allDay=s.TryGetProperty("date",out _);
            DateTimeOffset Read(JsonElement value) => DateTimeOffset.Parse(IntegrationClient.Text(value,allDay?"date":"dateTime"),System.Globalization.CultureInfo.InvariantCulture);
            return new CalendarEntry(IntegrationClient.Text(x,"summary"),Read(s),Read(e),allDay);
        }).ToList();
    }
}
