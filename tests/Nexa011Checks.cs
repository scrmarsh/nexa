using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void Check011()
    {
        Assert(App.AppVersion=="0.1.2" && NexaReleaseIdentity.Current.Stage=="Beta","Application identifies as Beta 0.1.2");
        Assert(NexaReleaseIdentity.Newer(new(0,1,1),"Beta",new(0,1,0),"Beta"),"Beta .1.1 is offered after .1.0 without Alpha remapping");
        var store=Store035("beta011");var profile=store.LoadProfile();profile.DisplayName="Before";store.SaveProfile(profile);
        var home=new HomeWindow(store){Width=1000,Height=760};home.Show();Pump(TimeSpan.FromMilliseconds(80));
        var settings=new SettingsDialog(home,store,home.Saved,null,"Profile");settings.Show();Pump(TimeSpan.FromMilliseconds(80));
        var priorWindows=Application.Current.Windows.Count;
        Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Before")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(TimeSpan.FromMilliseconds(30));
        var input=Descendants(settings).OfType<TextBox>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Profile display name");
        Assert(Application.Current.Windows.Count==priorWindows && input.IsVisible,"Profile rename opens inline without creating a window");
        var beforeName=store.LoadProfile().DisplayName;input.Text=" ";Click035(settings,"Save");Assert(store.LoadProfile().DisplayName==beforeName && input.IsVisible,"Invalid inline name leaves the persisted profile unchanged");
        input.Text="Canceled";Click035(settings,"Cancel");Assert(store.LoadProfile().DisplayName=="Before" && !input.IsVisible,"Cancel closes the inline editor without saving");
        Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Before")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));input.Text="  New name  ";Click035(settings,"Save");Assert(store.LoadProfile().DisplayName=="New name" && !input.IsVisible,"Inline Save trims and persists through the existing profile store");
        Assert(Descendants(settings).OfType<Button>().Any(b=>Equals(b.Content,"Rerun Setup")),"Profile exposes Rerun Setup");
        foreach(var size in new[]{new Size(700,520),new Size(900,680)})
        {
            settings.Width=size.Width;settings.Height=size.Height;
            foreach(var page in new[]{"Profile","Integrations","Appearance","Sounds","Time Zone & Clock","Widgets","Updates","Files & sharing","Add Bugs","Experimental Features"})
            {
                settings.SelectSection(page);Pump(TimeSpan.FromMilliseconds(45));settings.UpdateLayout();
                var body=(Visual)settings.Content;
                var clipped=Visual035(settings).OfType<Control>().Where(c=>c.IsVisible && c is Button or TextBox or PasswordBox or ComboBox or CheckBox).Where(c=>
                {var r=c.TransformToAncestor(body).TransformBounds(new Rect(c.RenderSize));return r.Left<-.5 || r.Right>settings.ActualWidth+.5;}).ToArray();
                Assert(clipped.Length==0,$"Settings {page} fits {size.Width} DIP without right-edge control clipping: "+string.Join(",",clipped.Select(c=>c.GetType().Name+":"+(c is Button b?b.Content:c.Name))));
            }
        }
        settings.SelectSection("Integrations");Assert(!Descendants(settings).OfType<TextBlock>().Any(t=>t.Text.Contains("Hevy",StringComparison.OrdinalIgnoreCase)),"Integrations exposes no retired provider tile");
        Assert(Descendants(settings).OfType<TextBlock>().Any(t=>t.Text=="Hedy"),"Hedy settings remain available");
        settings.SelectSection("Profile");Capture011(settings,"profile");
        var restored=store.LoadProfile().DisplayName;
        Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,restored)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        input.Text="Escape cancels";settings.Activate();Keyboard.Focus(input);
        input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(input),Environment.TickCount,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        Assert(!input.IsVisible && settings.IsVisible && store.LoadProfile().DisplayName==restored,"Escape cancels inline rename and keeps Settings open");
        Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,restored)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));input.Text=restored;
        input.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(input),Environment.TickCount,Key.Enter){RoutedEvent=Keyboard.PreviewKeyDownEvent});
        Assert(!input.IsVisible && store.LoadProfile().DisplayName==restored,"Enter safely saves an unchanged display name without duplicating the profile");
        var countBefore=home.Saved.Workspace!.Dashboards.Count;var setupOpened=false;
        var cancelSetup=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(40)};
        cancelSetup.Tick+=(_,_)=>{var wizard=Application.Current.Windows.OfType<OnboardingWindow>().FirstOrDefault();if(wizard!=null){setupOpened=true;cancelSetup.Stop();wizard.Close();}};
        cancelSetup.Start();Click035(settings,"Rerun Setup");cancelSetup.Stop();
        Assert(setupOpened && store.Load().Workspace!.Dashboards.Count==countBefore && store.LoadProfile().DisplayName=="New name","Profile Rerun Setup opens the existing wizard and cancellation preserves profile and Spaces");
        var devState=store.Load();devState.Workspace!.Dashboards[0].PresetOrigin="Developer mode";store.Save(devState);
        var developer=new MainWindow(store);developer.Show();Pump(TimeSpan.FromMilliseconds(50));Assert(developer.DeveloperSetupButton.IsVisible,"Developer Mode exposes the same Rerun Setup action");
        setupOpened=false;cancelSetup.Start();developer.DeveloperSetupButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));cancelSetup.Stop();
        Assert(setupOpened && store.LoadProfile().DisplayName=="New name","Developer Rerun Setup uses the same cancel-safe wizard");developer.Close();
        var freshStore=new StateStore(Path.Combine(output,"fresh011","state.json"));
        var fresh=new HomeWindow(freshStore){Width=4000,Height=3000};fresh.Show();Pump(TimeSpan.FromMilliseconds(120));
        Assert(NexaWindowBounds.TryWorkArea(fresh,out var area) && area.Width>=fresh.ActualWidth-3 && area.Height>=fresh.ActualHeight-3,"Fresh initial placement fits the actual monitor work area and taskbar");
        fresh.Left=-1700;fresh.Top=-950;Pump(TimeSpan.FromMilliseconds(80));Assert(fresh.Left<0 && fresh.Top<0,"Even the first-run safeguard is consumed once and never clamps later movement");fresh.Close();
        home.Left=-1500;home.Top=-900;Pump(TimeSpan.FromMilliseconds(120));
        Assert(home.Left<0 && home.Top<0,"Subsequent user positioning stays outside the screen without repeated clamping");
        home.Left=100;home.Top=100;home.Close();
        for(var frame=0;frame<32;frame++)
        {
            var legs=new BugLegPose[8];Assert(BugAnimation.FillLegs(BugSpecies.Spiders,frame*.31,frame%2==0,legs)==8,"Spider keeps eight legs in gait frame "+frame);
            Assert(legs.All(l=>l.Root.X<0 && Math.Abs(Math.Pow((l.Root.X+5)/7,2)+Math.Pow(l.Root.Y/6.8,2)-1)<.001),"Every leg remains attached to rear-body ellipse in gait frame "+frame);
            Assert(legs.Count(l=>l.Root.Y<0)==4 && legs.Count(l=>l.Root.Y>0)==4,"Spider retains four legs per side in gait frame "+frame);
        }
        CheckCleanup011();Console.WriteLine($"PASS: {passed} Beta011 checks");
    }
    private static void Capture011(Window window,string name)
    {
        window.UpdateLayout();var body=(FrameworkElement)window.Content;var bitmap=new RenderTargetBitmap((int)Math.Ceiling(body.ActualWidth),(int)Math.Ceiling(body.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(body);
        var folder="docs/media/beta011";Directory.CreateDirectory(folder);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(folder,name+".png"));encoder.Save(file);
    }
    private static void CheckCleanup011()
    {
        Assert(LegacyInstallations.IdentifyVersion(new(0,1,26),"0.1.26-alpha","")==new Version(0,0,26),"Legacy Alpha numeric aliases are correctly classified for cleanup");
        Assert(LegacyInstallations.IdentifyVersion(new(0,1,0),"0.1.0-beta","")==new Version(0,1,0),"Genuine Beta versions never use the legacy Alpha map");
        Assert(LegacyInstallations.IdentifyVersion(new(0,1,26),"0.1.26-beta","")==new Version(0,1,26),"A future Beta remains newer and ineligible for obsolete cleanup");
        var root=Path.Combine(output,"cleanup011");Directory.CreateDirectory(root);File.WriteAllText(Path.Combine(root,".nexa-installer-fixture"),"Disposable isolated tests");
        var data=Path.Combine(root,"data");Directory.CreateDirectory(data);var app=Path.Combine(root,"app");Directory.CreateDirectory(app);
        var old=Path.GetFullPath("verification/release010-beta/Application/Nexa.exe");
        foreach(var folder in new[]{"legacy/alpha-copy","custom/old-beta","app"}){Directory.CreateDirectory(Path.Combine(root,folder));File.Copy(old,Path.Combine(root,folder,"Nexa.exe"));File.WriteAllText(Path.Combine(root,folder,"state.json"),"private data "+folder);}
        var unrelated=Path.Combine(root,"legacy/alpha-copy/unrelated.exe");File.WriteAllText(unrelated,"not Nexa");
        var roots=new[]{Path.Combine(root,"legacy"),Path.Combine(root,"custom")};
        var scan=LegacyInstallations.Scan(data,app,"cleanup",roots,true);
        Assert(scan.Candidates.Count==3 && scan.Candidates.Count(c=>c.Eligible)==2,"One scan identifies multiple copies including a nondefault reference and preserves the active installation");
        var progress=new List<string>();LegacyInstallations.Remove(scan,(current,total,text)=>{Assert(current>=1 && current<=total,"Cleanup progress uses real bounded work counts");progress.Add(text);});
        Assert(scan.Removed==2 && scan.Failed==0 && File.Exists(Path.Combine(app,"Nexa.exe")),"Cleanup-only removes both obsolete binaries and preserves the active app");
        Assert(File.ReadAllText(Path.Combine(root,"custom/old-beta/state.json"))=="private data custom/old-beta" && File.Exists(unrelated),"Cleanup leaves adjacent saves and unrelated executables untouched");
        LegacyInstallations.Remove(scan);Assert(scan.Absent==2,"Repeated cleanup is idempotent and reports already absent copies");
        var clean=LegacyInstallations.Scan(data,app,"clean",roots,true);Assert(clean.Candidates.Single().Eligible,"Clean-install mode includes an obsolete unregistered target without an opt-out");
        using(var locked=new FileStream(Path.Combine(app,"Nexa.exe"),FileMode.Open,FileAccess.Read,FileShare.None)){LegacyInstallations.Remove(clean);Assert(clean.Failed==1 && File.Exists(Path.Combine(app,"Nexa.exe")),"A locked obsolete binary produces a verified failure and is not claimed removed");}
        clean=LegacyInstallations.Scan(data,app,"clean",roots,true);clean.Candidates[0].Hash=new string('0',64);LegacyInstallations.Remove(clean);Assert(clean.Failed==1 && File.Exists(Path.Combine(app,"Nexa.exe")),"Changed cleanup identity is rejected before deletion");
        using var canceled=new CancellationTokenSource();canceled.Cancel();var stopped=false;try{LegacyInstallations.Scan(data,app,"clean",roots,true,cancel:canceled.Token);}catch(OperationCanceledException){stopped=true;}Assert(stopped && File.Exists(Path.Combine(app,"Nexa.exe")),"Canceled scanning does not remove binaries");
        clean=LegacyInstallations.Scan(data,app,"clean",roots,true);LegacyInstallations.Remove(clean);Assert(clean.Removed==1 && File.Exists(Path.Combine(app,"state.json")),"Mandatory clean-install cleanup removes the old target while preserving its adjacent save");
        Assert(LegacyInstallations.Report(clean).Contains("Removed: 1"),"Cleanup summary reports actual verified outcomes");
        foreach(var name in new[]{"registered-a","registered-b"})
        {
            var directory=Path.Combine(root,"custom",name);Directory.CreateDirectory(directory);File.Copy(old,Path.Combine(directory,"Nexa.exe"));
            File.WriteAllText(Path.Combine(directory,"installation.json"),System.Text.Json.JsonSerializer.Serialize(new{AppId=NexaInstallation.TestAppId,DataRoot=directory}));
            File.WriteAllText(Path.Combine(directory,"installer-integrity.json"),"{}");File.WriteAllText(Path.Combine(directory,"private.txt"),name);
        }
        var copies=LegacyInstallations.Scan(data,app,"clean",roots,true);
        foreach(var candidate in copies.Candidates)candidate.Registrations.Add(new("CurrentUser","Registry64",@"Software\Microsoft\Windows\CurrentVersion\Uninstall\"+NexaInstallation.TestAppId+"_is1",Path.GetDirectoryName(candidate.Executable)!));
        LegacyInstallations.Remove(copies);
        Assert(copies.Removed==2 && copies.Failed==0,"Same-version installations with different metadata keep independent verified recovery copies");
        Assert(Directory.GetFiles(Path.Combine(data,"installer-cleanup-backups"),"installation.json",SearchOption.AllDirectories).Length==2,"Both distinct installation markers have verified recoverable bytes");
        Assert(File.Exists(Path.Combine(root,"custom/registered-a/private.txt")) && File.Exists(Path.Combine(root,"custom/registered-b/private.txt")),"Metadata cleanup preserves private content beside both installations");
    }
}
