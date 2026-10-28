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
        CheckCandidateUi025();
    }
    private static void CheckCandidateUi025()
    {
        Theme.SetSeasonalEnabled(true);
        const string phrase="a space for whatever haunts you next";var text=new System.Windows.Controls.TextBlock();var start=phrase.IndexOf("haunts",StringComparison.Ordinal);
        for(var length=1;length<=6;length++)
        {
            NexaBranding.ApplyTagline(text,phrase[..(start+length)]);
            Assert(text.Inlines.OfType<System.Windows.Documents.Run>().Any(r=>r.Text=="haunts"[..length] && r.Foreground is System.Windows.Media.SolidColorBrush b && b.Color==(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F97316")),"Orange is applied during typing, prefix length "+length);
        }
        var notificationRoot=new System.Windows.Controls.Grid();
        using(var notices=new NexaNotifications(notificationRoot))
        {
            var message=string.Join(" ",Enumerable.Repeat("A longer notification must wrap within the content column and keep its close button clear.",3));notices.Show("wrapping",message,"⚠",2,null);
            foreach(var width in new[]{700d,1200d})
            {
                notificationRoot.Measure(new System.Windows.Size(width,700));notificationRoot.Arrange(new System.Windows.Rect(0,0,width,700));notificationRoot.UpdateLayout();
                notificationRoot.Measure(new System.Windows.Size(width,700));notificationRoot.Arrange(new System.Windows.Rect(0,0,width,700));notificationRoot.UpdateLayout();
                var label=Descendants(notificationRoot).OfType<System.Windows.Controls.TextBlock>().Single(t=>t.Text==message);var dismiss=Descendants(notificationRoot).OfType<System.Windows.Controls.Button>().Single(b=>b.Content is string value && value=="×");
                var labelBounds=label.TransformToVisual(notificationRoot).TransformBounds(new System.Windows.Rect(label.RenderSize));var dismissBounds=dismiss.TransformToVisual(notificationRoot).TransformBounds(new System.Windows.Rect(dismiss.RenderSize));
                Assert(labelBounds.Right<=dismissBounds.Left+.1 && label.ActualHeight>30,"Long notifications wrap without overlapping the dismiss control at width "+width);
            }
        }
        var ledger=new ProductivityLedger();var now=DateTimeOffset.UtcNow;ProductivityScoring.DirectSquash(ledger,"one",now,3,"Ants");ProductivityScoring.DirectSquash(ledger,"one",now,3,"Ants");
        Assert(ledger.SquashesBySpecies.GetValueOrDefault("Ants")==1 && ledger.BugsSquashed==1,"Per-species direct squashes are deduplicated with the score event");
        var until=ledger.FrenzyUntil!.Value;var score=ledger.Score;
        Assert(ledger.Multiplier(until)==1 && ProductivityStreakOverlay.DisplayMultiplier(ledger,until)==1,"Bonus scoring stops at expiry while the popup begins its visual half-step decay");
        Assert(ProductivityStreakOverlay.DisplayMultiplier(ledger,until.AddSeconds(.36))==.5 && ProductivityStreakOverlay.DisplayMultiplier(ledger,until.AddSeconds(.8))==0 && ledger.Score==score,"Visual multiplier decays to zero without subtracting earned points");
        var legacyA=new WidgetInstance{Kind="Productivity Meter",Title="My actions",HiddenProductivityCategories=["Counter"],PlacementX=12,PlacementY=300};var legacyB=new WidgetInstance{Kind="Productivity Score",Title="Productivity Score",PlacementX=400,PlacementY=300};
        var state=new AppState{WidgetLayoutInitialized=true,Widgets=[legacyA,legacyB],Productivity=ledger};WidgetLibrary.PrepareLayout(state);
        Assert(state.Widgets.Count==2 && state.Widgets.All(w=>w.Kind=="Productivity") && legacyA.Title=="My actions" && legacyA.HiddenProductivityCategories.Contains("Counter") && legacyA.PlacementX==12 && ledger.Score==score,"Combined view preserves both existing widget identities, custom settings, placements and central scoring");
        Assert(WidgetLibrary.Definitions.Count(d=>d.Name=="Productivity")==1 && !WidgetLibrary.Definitions.Any(d=>d.Name is "Productivity Score" or "Productivity Meter"),"New widget picker exposes one combined Productivity entry");
        var store=new StateStore(Path.Combine(output,"home025.json"));var spaces=Enumerable.Range(0,8).Select(i=>Continuity.NewDashboard("Fixture "+i,"Class",new DashboardData(),store.UserId)).ToList();store.Save(Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId));
        var home=new HomeWindow(store){Left=-10000,Top=-10000,ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=System.Windows.WindowStartupLocation.Manual};home.Show();Pump(TimeSpan.FromMilliseconds(150));var buttons=Descendants(home.Recent).OfType<System.Windows.Controls.Button>().ToArray();
        Assert(buttons.Count(b=>b.Tag is Guid)==6 && Descendants(home.Recent).OfType<System.Windows.Controls.Primitives.UniformGrid>().Single().Columns==2,"Homepage initially displays six Space cards in two columns");
        buttons.Single(b=>b.Content is string value && value=="Show More Spaces").RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
        Assert(Descendants(home.Recent).OfType<System.Windows.Controls.Button>().Count(b=>b.Tag is Guid)==8,"Show More keeps every saved Space accessible");
        var content=(System.Windows.FrameworkElement)home.Content;content.UpdateLayout();var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(content);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));using(var file=File.Create(Path.Combine(output,"home025.png")))encoder.Save(file);home.Close();
        using(var candy=new NexaweenDecorations())
        {
            candy.ConfigureAnimation(new NexaOptions{SeasonalAnimation=false},new System.Windows.Rect(0,0,1400,1200));candy.Update(1400,1200,[],"High");
            var site=candy.AvailableCandy.First();var eatingWorld=CreateWorld(BugBehavior.Cozy);eatingWorld.DecorationCandyActive=true;eatingWorld.BindCandy([(site.Id,new System.Windows.Point(800,500))]);
            var eater=new BugEntity{Species=BugSpecies.Ants,Position=new(800,500),Activity=BugActivity.Gathering,ResourceId=site.Id,Decision=10,Health=100};eatingWorld.Bugs.Add(eater);
            var food=eatingWorld.Resources.Single(r=>r.Id==site.Id);eatingWorld.Advance(.1);
            Assert(food.Amount==1 && eater.State==BugState.Paused,"A visible decoration candy triggers a short eating pause before consumption");
            for(var i=0;i<12;i++)eatingWorld.Advance(.1);
            Assert(food.Amount<1 && eater.Carrying>0,"Cozy gathering consumes the actual decoration resource");
            var idsBefore=candy.AvailableCandy.Select(s=>s.Id).ToHashSet();candy.ObserveCandy(site.Id,0,false);Assert(!candy.AvailableCandy.Any(s=>s.Id==site.Id),"Consumed decoration candy leaves the visible food catalogue");
            Pump(TimeSpan.FromSeconds(3.1));candy.ReconcileCandy();Assert(candy.AvailableCandy.Any(s=>!idsBefore.Contains(s.Id)) && candy.ActorCount>0,"Replacement remains available with seasonal motion disabled");
        }
        var world=CreateWorld(BugBehavior.Cozy);world.Configure(world.Preferences with {Density=BugDensity.High});Assert(world.EcosystemCap==96,"High preserves the previous fixed high population limit");world.Configure(world.Preferences with {Density=BugDensity.Infestation});
        Assert(world.EcosystemCap==int.MaxValue,"Infestation has no fixed population-count limit");world.ObserveFrameCost(100);Assert(!world.AdaptiveSpawnAllowed,"Frame pressure suppresses Infestation spawning");
        world.Bugs.Clear();world.Bugs.Add(new BugEntity{Species=BugSpecies.Ants});world.Bugs.Add(new BugEntity{Species=BugSpecies.Ants});var pair=world.Bugs.Take(2).ToArray();foreach(var bug in pair){bug.State=BugState.Crossing;bug.Position=new(800,500);bug.PartnerId=null;bug.EnemyId=null;}world.SeparateNearby(.1);Assert((pair[0].Position-pair[1].Position).Length>0,"Overlapping ground creatures separate without a global pairwise search");
        store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Infestation));Assert(store.LoadBugPreferences().Density==BugDensity.Infestation,"Adaptive density selection survives preference persistence");
    }
    private static void CheckPublicUpdates025()
    {
        using var client=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};
        var release=Task.Run(()=>new ExperimentalUpdater(client).DiscoverAsync()).GetAwaiter().GetResult();
        Assert(release is {Portable:true} && release.Version>=new Version(0,1,24),"Unauthenticated public GitHub feed discovers a compatible portable alpha");
        Assert(release!.Url.Host=="github.com" && release.Sha256.Length==64 && release.Size>1000000,"Live release descriptor includes canonical URL, size and official digest");
        File.WriteAllText(Path.Combine(output,"public-update.json"),JsonSerializer.Serialize(release,new JsonSerializerOptions{WriteIndented=true}));
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
        File.WriteAllBytes(target,old);plan=Prepare();var external=JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!;external.VerificationOnly=true;external.ParentPid=int.MaxValue;UpdateTransaction.Save(plan,external);
        using(var helper=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(candidate){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,ArgumentList={"--apply-update",plan}})!)
        {if(!helper.WaitForExit(90000)){helper.Kill();throw new TimeoutException("Isolated helper did not exit.");}Assert(helper.ExitCode==0,"A separate real helper installs and validates the isolated portable target");}
        var restarted=Path.Combine(Path.GetDirectoryName(plan)!,"restart-health","startup.json");var watch=System.Diagnostics.Stopwatch.StartNew();while(!File.Exists(restarted)&&watch.Elapsed<TimeSpan.FromSeconds(45))Thread.Sleep(100);
        Assert(File.Exists(restarted) && JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(plan))!.State=="Installed","Helper restart opens a separate verified process with isolated data");
        var healthy=UpdateTransaction.Health(candidate,Path.Combine(root,"native-startup")).GetAwaiter().GetResult();Assert(healthy,"Published candidate passes a separate real-process startup health check");
    }
}
