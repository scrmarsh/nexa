using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void Check034()
    {
        Assert(App.AppVersion=="0.0.34" && NexaReleaseIdentity.Current.Stage=="Alpha",".34 assembly and Alpha identity agree");
        CheckData034();CheckProfile034();CheckUi034();
    }
    private static void CheckProfile034()
    {
        var now=DateTimeOffset.UtcNow;
        DashboardRecord Space()=>Continuity.NewDashboard("Fixture","Focus",new(){Session=new(){Title="Same-time completed session",Start=now.AddMinutes(-10),End=now}},"profile-fixture");
        var first=Space();var second=Space();var a=SessionFinalization.ArchiveAndReset(first,now);var b=SessionFinalization.Complete(second,now);
        Assert(a.CompletionId!=Guid.Empty && b.CompletionId!=Guid.Empty && a.CompletionId!=b.CompletionId,"Independent completed sessions have stable distinct recap identities even at identical times");
        var activity=ProfileActivity.Completed(first.History.Append(first.Current).Concat(second.History.Append(second.Current)));
        Assert(activity.Length==2 && activity.Sum(r=>r.ActualSeconds)==1200,"Profile counts simultaneous completed sessions separately and excludes the reset copy");
        foreach(var session in first.History.Append(first.Current))session.Recap!.CompletionId=Guid.Empty;
        Assert(ProfileActivity.Completed(first.History.Append(first.Current)).Length==1,"Legacy recaps use reset provenance to avoid duplicate completed-session statistics");
    }
    private sealed class InlineProgress034(Action<string> action):IProgress<string>{public void Report(string value)=>action(value);}
    private static void CheckData034()
    {
        var root=Path.Combine(output,"data034");var legacy=Path.Combine(root,"SessionDashboard");var docs=Path.Combine(root,"Documents","nexa");var target=Path.Combine(root,"Nexa");
        Directory.CreateDirectory(Path.Combine(legacy,"Recordings"));Directory.CreateDirectory(Path.Combine(docs,"History"));
        File.WriteAllText(Path.Combine(legacy,"Recordings","session.wav"),"fixture audio");
        var source=new StateStore(Path.Combine(legacy,"state.json"));var state=source.Load();state.Notes="Preserve private session notes";source.Save(state);
        var protectedMigration=false;var connection=new IntegrationCredential{AccessToken="fixture-only-todoist-secret"};
        try
        {
            state.HedyProtectedKey=SecretStorage.Protect("fixture-only-hedy-secret");source.Save(state);new IntegrationVault(source).Write("Todoist",connection);protectedMigration=true;
        }
        catch(Exception ex)when(ex is System.Security.Cryptography.CryptographicException or UnauthorizedAccessException or PlatformNotSupportedException or IntegrationException)
        {Console.WriteLine("UNVERIFIED: This token does not support the isolated DPAPI migration fixture.");}
        File.WriteAllText(Path.Combine(legacy,"links.json"),JsonSerializer.Serialize(new{AudioPath=Path.Combine(legacy,"Recordings","session.wav"),HedyProtectedKey="test-managed-secret",Nested=new{AccessToken="private-access",RefreshToken="private-refresh",ClientSecret="private-client"}}));
        File.WriteAllText(Path.Combine(docs,"History","session.md"),"Readable session history");var original=NexaData.Hash(source.FilePath);
        var first=NexaData.Migrate(legacy,docs,target);
        Assert(first.Copied>=4 && first.Verified>=4,"Migration copies and hashes private saves, recordings and readable history");
        Assert(NexaData.Hash(source.FilePath)==original,"Migration leaves original private save bytes intact");
        Assert(File.ReadAllText(Path.Combine(target,"links.json")).Contains(Path.Combine(target,"Recordings","session.wav").Replace("\\","\\\\")),"Migrated structured recording links point to the copied recording");
        Assert(new StateStore(Path.Combine(target,"state.json")).Load().Notes=="Preserve private session notes","Migrated actual workspace can be loaded");
        if(protectedMigration)
        {
            var migrated=new StateStore(Path.Combine(target,"state.json"));
            Assert(SecretStorage.Unprotect(migrated.Load().HedyProtectedKey)=="fixture-only-hedy-secret","Hedy's Windows-protected fixture key remains readable after migration");
            var restoredCredential=new IntegrationVault(migrated).Read("Todoist");Assert(restoredCredential?.AccessToken==connection.AccessToken && restoredCredential.ConnectionId==connection.ConnectionId,"DPAPI-protected integration token and connection identity survive the root change");
        }
        var second=NexaData.Migrate(legacy,docs,target);Assert(second.Copied==0,"Repeating migration adds no duplicate files");
        File.WriteAllText(Path.Combine(target,"History","session.md"),"Newer readable report");var conflict=NexaData.Migrate(legacy,docs,target);
        Assert(conflict.Conflicts>=1 && File.ReadAllText(Path.Combine(target,"History","session.md"))=="Newer readable report","Mixed-root conflicts preserve the new file and retain verified originals");
        Assert(File.Exists(Path.Combine(target,"Backups","migration-inventory.json")),"Migration writes its versioned inventory and hashes");
        Directory.CreateDirectory(Path.Combine(target,"profiles","fixture","credentials"));File.WriteAllText(Path.Combine(target,"profiles","fixture","credentials","encrypted.bin"),"dpapi fixture");File.WriteAllText(Path.Combine(target,"skip.exe"),"binary fixture");
        var zip=Path.Combine(root,"Nexa-Backup.zip");var stages=new List<string>();var inspectedCheckpoint=false;
        var count=NexaData.Export(target,zip,CancellationToken.None,new InlineProgress034(stage=>
        {
            stages.Add(stage);
            if(!inspectedCheckpoint && stage.StartsWith("Archiving",StringComparison.Ordinal))
            {
                inspectedCheckpoint=true;Assert(!System.Threading.Monitor.IsEntered(NexaData.SnapshotGate),"Compression runs after releasing the application save lock");
                var checkpoint=Directory.GetDirectories(root,".nexa-backup-stage-*").Single();
                Assert(!File.ReadAllText(Path.Combine(checkpoint,"links.json")).Contains("test-managed-secret") && !Directory.Exists(Path.Combine(checkpoint,"profiles","fixture","credentials")),"Temporary checkpoint files exclude credentials before ZIP compression");
            }
        }));
        using(var archive=ZipFile.OpenRead(zip))
        {
            Assert(count>0 && archive.GetEntry("nexa-backup-manifest.json")!=null,"Backup creates a real ZIP with its recovery manifest");
            Assert(archive.Entries.All(e=>!e.FullName.Contains("credentials")&&!e.FullName.StartsWith("Backups/")&&!e.FullName.EndsWith(".exe")),"ZIP excludes credentials, migration originals and binaries");
            using var reader=new StreamReader(archive.GetEntry("links.json")!.Open());var text=reader.ReadToEnd();
            Assert(!text.Contains("test-managed-secret")&&!text.Contains("private-access")&&!text.Contains("private-refresh")&&!text.Contains("private-client"),"Nested managed secret fields are removed from structured exports");
            Assert(archive.GetEntry("Recordings/session.wav")!=null && archive.GetEntry("History/session.md")!=null,"Nonsecret recordings and readable history are included");
            using var manifest=JsonDocument.Parse(archive.GetEntry("nexa-backup-manifest.json")!.Open());Assert(manifest.RootElement.GetProperty("Files").GetArrayLength()==count,"Manifest accounts for every archived data file");
            foreach(var expected in manifest.RootElement.GetProperty("Files").EnumerateArray())
            {
                var item=archive.GetEntry(expected.GetProperty("Path").GetString()!)!;using var content=item.Open();
                Assert(item.Length==expected.GetProperty("Size").GetInt64() && Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(content))==expected.GetProperty("SHA256").GetString(),"Independent ZIP reader verifies manifest bytes: "+item.FullName);
            }
        }
        Assert(stages.Any(s=>s.StartsWith("Archiving"))&&stages.Any(s=>s.StartsWith("Verifying")),"Backup progress describes actual inventory, copy and verification stages");
        Assert(!Directory.GetDirectories(root,".nexa-backup-stage-*").Any(),"Successful export removes its sanitized staging folder");
        using(var cancel=new CancellationTokenSource())
        {
            var canceled=Path.Combine(root,"canceled.zip");
            try{NexaData.Export(target,canceled,cancel.Token,new InlineProgress034(_=>cancel.Cancel()));Assert(false,"Canceled export must stop");}catch(OperationCanceledException){Assert(!File.Exists(canceled)&&!Directory.GetFiles(root,"canceled.zip.*.tmp").Any(),"Cancellation removes its temporary archive and publishes no final ZIP");}
        }
        try{NexaData.Export(target,zip,CancellationToken.None);Assert(false,"An existing backup must be preserved");}catch(IOException){Assert(File.Exists(zip),"Export refuses to overwrite an existing backup");}
        using(var held=new FileStream(Path.Combine(target,"History","session.md"),FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        {
            var locked=Path.Combine(root,"locked.zip");try{NexaData.Export(target,locked,CancellationToken.None);Assert(false,"Locked input must fail");}catch(IOException){Assert(!File.Exists(locked),"Locked file failure never publishes an incomplete backup");}
        }
        try{NexaData.Inside(target,"../unrelated.txt");Assert(false,"Traversal must be rejected");}catch(IOException){Assert(true,"Owned path validation rejects traversal");}
        var linkedTarget=Path.Combine(root,"unrelated-folder");Directory.CreateDirectory(linkedTarget);File.WriteAllText(Path.Combine(linkedTarget,"keep.txt"),"Unrelated fixture bytes");
        var link=Path.Combine(target,"linked-folder");
        var create=new System.Diagnostics.ProcessStartInfo("powershell.exe"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden};
        foreach(var argument in new[]{"-NoProfile","-NonInteractive","-Command","New-Item -ItemType Junction -Path $env:NEXA_TEST_LINK -Target $env:NEXA_TEST_TARGET -ErrorAction Stop | Out-Null"})create.ArgumentList.Add(argument);
        create.Environment["NEXA_TEST_LINK"]=link;create.Environment["NEXA_TEST_TARGET"]=linkedTarget;
        using(var process=System.Diagnostics.Process.Start(create)!)
        {
            if(!process.WaitForExit(15000)){process.Kill();throw new TimeoutException("Isolated junction fixture creation timed out.");}
            if(process.ExitCode==0)
            {
                try
                {
                    try{NexaData.Export(target,Path.Combine(root,"linked.zip"),CancellationToken.None);Assert(false,"Linked inputs must be rejected");}catch(IOException){Assert(!File.Exists(Path.Combine(root,"linked.zip")),"Export rejects directory junctions without publishing an archive");}
                    try{NexaData.Delete(target,true);Assert(false,"Linked roots must not be deleted");}catch(IOException){Assert(File.Exists(Path.Combine(target,"state.json")) && File.ReadAllText(Path.Combine(linkedTarget,"keep.txt"))=="Unrelated fixture bytes","Deletion rejects a junction before changing owned or unrelated files");}
                }
                finally{Directory.Delete(link);}
            }
            else Console.WriteLine("UNVERIFIED: This token cannot create the isolated NTFS junction fixture.");
        }
        File.WriteAllText(Path.Combine(root,"unavailable-parent"),"This is a file, not a destination directory.");
        try{NexaData.Export(target,Path.Combine(root,"unavailable-parent","backup.zip"),CancellationToken.None);Assert(false,"Unavailable destination must fail");}catch(IOException){Assert(true,"Destination failure publishes no backup");}
        try{NexaData.Delete(target);Assert(false,"Noncanonical UI deletion root must be refused");}catch(IOException){Assert(File.Exists(Path.Combine(target,"state.json")),"Production deletion refuses an arbitrary fixture root");}
        var at=DateTimeOffset.UtcNow;var consent=new NexaDeletionConsent();Assert(!consent.Begin("confirm",at)&&!consent.Authorize(at,true),"Only exact uppercase CONFIRM can arm deletion");
        Assert(consent.Begin("CONFIRM",at)&&!consent.Authorize(at.AddSeconds(29)),"Deletion cannot precede its 30-second deadline");consent.Cancel();Assert(!consent.Authorize(at.AddMinutes(1),true),"Canceled confirmation cannot delete even after its deadline");
        var elapsed=new NexaDeletionConsent();elapsed.Begin("CONFIRM",at);Assert(elapsed.Authorize(at.AddSeconds(30))&&!elapsed.Authorize(at.AddSeconds(31)),"Countdown authorizes one deletion exactly once");
        var immediate=new NexaDeletionConsent();immediate.Begin("CONFIRM",at);Assert(immediate.Authorize(at,true),"Delete Now requires prior typed confirmation");
        var untouched=Path.Combine(root,"unrelated.txt");File.WriteAllText(untouched,"keep");
        Assert(NexaData.Delete(target,true).Length==0 && !File.Exists(Path.Combine(target,"state.json")),"Isolated deletion removes owned saves successfully");
        Assert(File.ReadAllText(untouched)=="keep" && File.Exists(source.FilePath),"Deletion preserves unrelated siblings and legacy originals");
        Assert(File.Exists(Path.Combine(target,"skip.exe")),"Data deletion preserves cached application and installer binaries");
        Assert(File.Exists(Path.Combine(target,"Settings","legacy-import-disabled")),"Reset records a guard against silently reimporting deleted legacy data");
        try{StateStore.AtomicWrite(Path.Combine(target,"state.json"),"{}",false);Assert(false,"Frozen store must not recreate deleted saves");}catch(IOException){Assert(!File.Exists(Path.Combine(target,"state.json")),"Late saves cannot recreate deleted workspace data");}
        var partial=Path.Combine(root,"partial-reset");Directory.CreateDirectory(partial);var partialStore=new StateStore(Path.Combine(partial,"state.json"));partialStore.Save(partialStore.Load());
        var heldPath=Path.Combine(partial,"locked-recording.wav");File.WriteAllText(heldPath,"keep locked bytes");
        using(var held=new FileStream(heldPath,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        {var errors=NexaData.Delete(partial,true);Assert(errors.Length==1 && File.Exists(heldPath),"Partial deletion truthfully reports and preserves a locked file");}
    }
    private static void CheckUi034()
    {
        var store=new StateStore(Path.Combine(output,"ui034","state.json"));Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="Off";store.Save(state);
        var home=new HomeWindow(store);home.Show();var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");settings.Show();Pump(TimeSpan.FromMilliseconds(220));
        Assert(NexaBranding.Tagline=="a space for whatever comes next","Classic tagline uses the exact requested text");
        var wallpaper=Descendants(settings).OfType<StackPanel>().Single(p=>Equals(p.Tag,"HomeWallpaperControls"));
        Assert(new[]{"nexa Default","Solid Color","Custom Image"}.All(label=>Descendants(wallpaper).OfType<Button>().Any(b=>Equals(b.Content,label))),"Home offers all three direct background choices");
        Assert(!Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Restore default theme color")),"Redundant restore theme action is removed");
        Assert(Descendants(settings).OfType<RadioButton>().Count(r=>r.Tag is AppTheme)==4,"The original four official palette choices remain");
        var wallpaperField=typeof(HomeWindow).GetField("homeWallpaper",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        foreach(var mode in new[]{SessionDashboard.ThemeMode.Light,SessionDashboard.ThemeMode.Dark})foreach(var palette in Theme.OfficialThemes)
        {
            store.SavePreferences(mode,palette);Theme.Apply(Application.Current.Resources,mode,palette);home.RefreshWallpaper();
            Assert(((Border)wallpaperField.GetValue(home)!).Background is SolidColorBrush brush && brush.Color==Color.FromRgb(23,27,38),"Default Home stays charcoal under "+mode+" / "+palette);
        }
        Assert(home.FindResource("TextBrush") is SolidColorBrush textBrush && textBrush.Color==Color.FromRgb(240,242,255),"The fixed charcoal Home retains readable text independently of the application mode");
        store.SavePreferences(SessionDashboard.ThemeMode.System,AppTheme.Nexa);Theme.Apply(Application.Current.Resources,SessionDashboard.ThemeMode.System,AppTheme.Nexa);
        Assert(Descendants(settings).OfType<Slider>().All(s=>s.IsMoveToPointEnabled),"Appearance sliders support click-to-point as well as dragging");
        Capture034(settings,"appearance");settings.Close();
        foreach(var section in new[]{"Profile","Sounds","Add Bugs","Files & sharing","Updates"})
        {
            settings=new SettingsDialog(home,store,home.Saved,null,section);settings.Show();Pump(TimeSpan.FromMilliseconds(40));
            if(section is "Profile" or "Sounds" or "Add Bugs")Assert(Descendants(settings).OfType<ScrollViewer>().Any(s=>Equals(s.Tag,"HiddenPageScroll") && s.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden),section+" hides its scroll bar while retaining scrolling");
            if(section=="Add Bugs")Assert(Descendants(settings).OfType<ComboBox>().Single(c=>c.Items.OfType<BugBehavior>().Count()==3).Items.Cast<BugBehavior>().SequenceEqual(new[]{BugBehavior.Classic,BugBehavior.Cozy,BugBehavior.Combative}),"One bug behavior selector exposes exactly Classic, Cozy and Combative");
            if(section=="Updates")Assert(!Descendants(settings).OfType<ComboBox>().Any(c=>c.Items.Contains("Alpha")),"Updates has no release channel selector");
            Capture034(settings,section.Replace(" & ","-").Replace(" ","-").ToLowerInvariant());settings.Close();
        }
        Capture034(home,"home-classic");home.Close();
        var main=new MainWindow(store);main.Show();Pump(TimeSpan.FromMilliseconds(40));
        var pixel=main.state.Widgets.Single(w=>w.Kind=="Pixel timer");main.RemoveWidgetConfirmed(pixel);Assert(main.state.Widgets.Any(w=>w.Id==pixel.Id),"The primary Pixel Timer cannot be removed");
        var world=NexaEcosystem.For(store);var generation=world.Manager.SessionGeneration;var candidate=Continuity.Clone(main.state.Workspace!);candidate.LiveEcosystemGeneration=Guid.NewGuid();world.ReconcileGeneration(candidate.LiveEcosystemGeneration);
        Assert(world.Manager.SessionGeneration!=generation && world.Manager.Webs.Count==0,"Changing generation clears transient web state");
        world.Manager.WebCoordinateAspect=1.8;var bounds=world.Manager.WebBounds(new(){X=800,Y=500});Assert(Math.Abs(bounds.Width*1.8/bounds.Height-1)<.01,"Large webs remain circular after viewport coordinate scaling");
        Assert(bounds.Width*bounds.Height>=BugManager.WorldWidth*BugManager.WorldHeight*.2,"Communal web uses approximately a quarter of a suitable dashboard");
        var communal=new EcosystemWeb{Owner=Guid.NewGuid(),X=800,Y=500,Helpers=Enumerable.Range(0,BugManager.MaximumWebHelpers).Select(_=>Guid.NewGuid()).ToList()};BugManager.EnsureSilk(communal);world.Manager.Webs.Add(communal);
        var serialized=JsonSerializer.Serialize(world.Manager.CaptureEcosystem(store.UserId));var restored=JsonSerializer.Deserialize<EcosystemDocument>(serialized)!;restored.Validate(store.UserId);
        Assert(restored.Webs.Single().Helpers.Count==BugManager.MaximumWebHelpers,"A communal web with more than eight helpers survives serialization and validation");
        Capture034(main,"pixel-timer");main.Close();
        Assert(new NexaOptions().Use24HourClock==false,"New accounts default to 12-hour clock");
    }
    private static void Capture034(Window window,string name)
    {
        var directory=Path.GetFullPath("docs/media/nexa034");Directory.CreateDirectory(directory);window.UpdateLayout();
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(Math.Max(1,(int)window.ActualWidth),Math.Max(1,(int)window.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(window);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(directory,name+".png"));encoder.Save(file);
    }
    private static void InstallerArtwork034()
    {
        Theme.SetSeasonalEnabled(false);
        var logo=(DrawingImage)typeof(NexaLogo).GetMethod("Load",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.Invoke(null,new object[]{"nexa-logo-full-dark.svg",false})!;
        foreach(var small in new[]{false,true})
        {
            var width=small?55:164;var height=small?55:314;var visual=new DrawingVisual();
            using(var dc=visual.RenderOpen())
            {
                dc.DrawRectangle(BugVisuals.B("#171B26"),null,new Rect(0,0,width,height));
                if(small)dc.DrawImage(new System.Windows.Media.Imaging.BitmapImage(new Uri(Path.GetFullPath("assets/nexa-icon-256.png"))),new Rect(3,3,49,49));
                else
                {
                    dc.DrawRectangle(BugVisuals.B("#185FA5"),null,new Rect(0,0,164,10));dc.DrawRectangle(BugVisuals.B("#7F77DD"),null,new Rect(0,304,164,10));
                    dc.DrawImage(logo,new Rect(10,102,144,144*logo.Height/logo.Width));
                    void Text(string text,double y,Brush brush,double size)=>dc.DrawText(new FormattedText(text,System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),size,brush,1){MaxTextWidth=144,TextAlignment=TextAlignment.Center},new Point(10,y));
                    Text("a space for whatever",162,BugVisuals.B("#7F77DD"),12);Text("comes next",180,BugVisuals.B("#7F77DD"),12);Text("Setup",247,Brushes.White,17);
                }
            }
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
            var converted=new System.Windows.Media.Imaging.FormatConvertedBitmap(bitmap,PixelFormats.Bgr24,null,0);var encoder=new System.Windows.Media.Imaging.BmpBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(converted));
            using var file=File.Create(small?"assets/nexa-setup-small.bmp":"assets/nexa-setup-classic.bmp");encoder.Save(file);
        }
    }
}
