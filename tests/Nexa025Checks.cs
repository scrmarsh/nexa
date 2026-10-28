using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using SessionDashboard;
internal static partial class Program
{
    private sealed class AuditHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) { token.ThrowIfCancellationRequested(); var response=reply(request,token); response.RequestMessage ??= request; return Task.FromResult(response); }
    }
    private static string Feed(string name="Nexa.exe", string repository="scrmarsh/nexa", string version="0.1.24", string channel="alpha", string digest="") => JsonSerializer.Serialize(new[]{new { draft=false, tag_name=$"v{version}-{channel}", prerelease=true, name="Published alpha",body="Recorded notes",published_at="2026-10-26T00:00:00Z",assets=new[]{new { name, size=4,digest="sha256:"+(digest==""?new string('A',64):digest), browser_download_url=$"https://github.com/{repository}/releases/download/v{version}-{channel}/{name}"}} }});
    private static void CheckAudit025()
    {
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(Feed())})))
        {
            var updater=new ExperimentalUpdater(client);var release=updater.CheckAsync(new Version(0,1,23)).GetAwaiter().GetResult();
            Assert(release is {Portable:true} && release.Version==new Version(0,1,24),"Actual portable Nexa.exe alpha asset is discoverable from an older version");
            Assert(updater.CheckAsync(new Version(0,1,25)).GetAwaiter().GetResult()==null,"Development candidate never offers the older production release");
            Assert(updater.DiscoverAsync(currentChannel:"Stable").GetAwaiter().GetResult()==null,"Stable installations do not receive alpha packages");
        }
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(Feed(repository:"another/repo"))}))) Assert(new ExperimentalUpdater(client).DiscoverAsync().GetAwaiter().GetResult()==null,"Foreign repository assets are rejected");
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(Feed(digest:"missing"))}))) Assert(new ExperimentalUpdater(client).DiscoverAsync().GetAwaiter().GetResult()==null,"Missing or invalid official digests are rejected");
        var pages=0;
        using(var client=new HttpClient(new AuditHandler((request,_)=>{pages++;var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(pages==1?"[{\"tag_name\":null},{}]":Feed())};if(pages==1)response.Headers.Add("Link","<https://api.github.com/repos/scrmarsh/nexa/releases?page=2>; rel=\"next\"");return response;})))
            Assert(new ExperimentalUpdater(client).DiscoverAsync().GetAwaiter().GetResult()!=null && pages==2,"Malformed entries do not hide compatible releases on later pages");
        foreach(var code in new[]{HttpStatusCode.Forbidden,(HttpStatusCode)429,HttpStatusCode.NotFound})
        {
            using var client=new HttpClient(new AuditHandler((_,_)=>new(code)));var failed=false;try{new ExperimentalUpdater(client).DiscoverAsync().GetAwaiter().GetResult();}catch(HttpRequestException){failed=true;}Assert(failed,"Public feed failure reports a useful error: "+code);
        }
        var bytes=new byte[]{1,2,3,4};var digest=Convert.ToHexString(SHA256.HashData(bytes));var offered=new AvailableUpdate(new(0,1,24),"Alpha","test","",null,new Uri("https://github.com/scrmarsh/nexa/releases/download/v0.1.24-alpha/Nexa.exe"),4,digest);
        var directory=Path.Combine(output,"downloads");
        using(var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)})))
        {
            var updater=new ExperimentalUpdater(client);var downloaded=updater.DownloadAsync(offered,directory).GetAwaiter().GetResult();Assert(File.ReadAllBytes(downloaded).SequenceEqual(bytes),"Matching bytes and digest are atomically staged");
            var failed=false;try{updater.DownloadAsync(offered with {Sha256=new string('0',64)},directory).GetAwaiter().GetResult();}catch(InvalidDataException){failed=true;}
            Assert(failed && !File.Exists(downloaded+".download") && File.ReadAllBytes(downloaded).SequenceEqual(bytes),"Tampered downloads clean partials and preserve prior verified file");
            using var canceled=new CancellationTokenSource();canceled.Cancel();failed=false;try{updater.DownloadAsync(offered,directory,cancellationToken:canceled.Token).GetAwaiter().GetResult();}catch(OperationCanceledException){failed=true;}Assert(failed && !File.Exists(downloaded+".download"),"Canceled download cannot leave an installable partial");
        }
        var store=new StateStore(Path.Combine(output,"backup-source","state.json"));Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);File.WriteAllText(store.FilePath,"preserved");
        var updates=Path.Combine(store.UserPreferencesDirectory,"updates","transaction");Directory.CreateDirectory(updates);File.WriteAllText(Path.Combine(updates,"candidate.exe"),"do not recurse");
        UpdateTransaction.BackupData(store,updates).GetAwaiter().GetResult();
        Assert(File.ReadAllText(Path.Combine(updates,"data-backup","workspace.json"))=="preserved","Updater snapshots the committed workspace through the save gate");
        Assert(Directory.GetFiles(Path.Combine(updates,"data-backup"),"*",SearchOption.AllDirectories).Length==2,"Backup prunes its updates subtree before traversal and cannot recursively copy itself");
    }
    private static void CheckUpdateIntegration(string candidate)
    {
        candidate=Path.GetFullPath(candidate);var root=Path.GetFullPath(Path.Combine(output,"update-integration"));Directory.CreateDirectory(root);
        var target=Path.Combine(root,"Nexa.exe");File.Copy(candidate,target);
        var old=File.ReadAllBytes(target).Concat(new byte[]{42}).ToArray();File.WriteAllBytes(target,old);var before=UpdateTransaction.Hash(target);
        var store=new StateStore(Path.Combine(root,"data","state.json"));Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);File.WriteAllText(store.FilePath,"preserve user data");
        string Prepare()=>UpdateTransaction.Prepare(store,target,candidate,UpdateTransaction.Hash(candidate),"0.1.25").GetAwaiter().GetResult();
        var plan=Prepare();var rejected=false;try{UpdateTransaction.Apply(plan,_=>Task.FromResult(false),waitForParent:false).GetAwaiter().GetResult();}catch(IOException){rejected=true;}
        Assert(rejected && UpdateTransaction.Hash(target)==before,"Failed installed health check restores the actual previous executable");
        Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="RolledBack","Failed update has a durable rollback checkpoint");
        plan=Prepare();UpdateTransaction.Apply(plan,_=>Task.FromResult(true),waitForParent:false).GetAwaiter().GetResult();
        Assert(UpdateTransaction.Hash(target)==UpdateTransaction.Hash(candidate),"Portable transaction replaces only the isolated target");
        Assert(File.ReadAllText(store.FilePath)=="preserve user data","Successful install leaves existing user data unchanged");
        UpdateTransaction.Apply(plan,_=>throw new InvalidOperationException("should not replay"),waitForParent:false).GetAwaiter().GetResult();Assert(true,"Completed update is idempotent");
        File.WriteAllBytes(target,old);plan=Prepare();var interrupted=JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!;interrupted.State="Installing";UpdateTransaction.Save(plan,interrupted);File.Copy(candidate,target,true);
        UpdateTransaction.Apply(plan,_=>Task.FromResult(true),waitForParent:false).GetAwaiter().GetResult();Assert(JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="Installed","Interrupted helper reconciles an already replaced executable");
        File.WriteAllBytes(target,old);plan=Prepare();File.AppendAllText(Path.Combine(Path.GetDirectoryName(plan)!,"candidate.exe"),"tamper");rejected=false;try{UpdateTransaction.Apply(plan,_=>Task.FromResult(true),waitForParent:false).GetAwaiter().GetResult();}catch(InvalidDataException){rejected=true;}Assert(rejected && UpdateTransaction.Hash(target)==before,"Changed staged executable is rejected before replacement");
        var healthy=UpdateTransaction.Health(candidate,Path.Combine(root,"native-startup")).GetAwaiter().GetResult();Assert(healthy,"Published candidate passes a separate real-process startup health check");
    }
}
