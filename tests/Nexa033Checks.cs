using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void Check033()
    {
        Assert(App.AppVersion=="0.0.34" && App.VersionLabel=="nexa v0.0.34 alpha",".33 application and canonical identity agree");
        Assert(Theme.OfficialThemes.SequenceEqual(new[]{AppTheme.Nexa,AppTheme.Violet,AppTheme.Aurora,AppTheme.Ember}),"Exactly the four original global palettes are officially selectable");
        Assert(Theme.AppPalette(AppTheme.Nexa)==("#5B7CFF","#8B5CF6") && Theme.AppPalette(AppTheme.Violet)==("#A78BFA","#7C3AED") && Theme.AppPalette(AppTheme.Aurora)==("#38BDF8","#14B8A6") && Theme.AppPalette(AppTheme.Ember)==("#FB7185","#F59E0B"),"All four original global palette pairs remain unchanged");
        Assert(Theme.OfficialTheme(AppTheme.Amber)==AppTheme.Ember && Theme.OfficialTheme(AppTheme.Green)==AppTheme.Aurora,"Unpublished legacy theme enum values have explicit compatible official choices");
        CheckThemes033();CheckAssets033();CheckMigration033();
    }
    private static void CheckThemes033()
    {
        var store=new StateStore(Path.Combine(output,"themes033","state.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="Off";state.Workspace.Settings.ReduceMotion=true;state.Workspace.Settings.HomeWallpaper=new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#123456"};store.Save(state);store.SavePreferences(SessionDashboard.ThemeMode.Dark,AppTheme.Violet);Theme.SetSeasonalEnabled(false);Theme.Apply(Application.Current.Resources,SessionDashboard.ThemeMode.Dark,AppTheme.Violet);
        var home=new HomeWindow(store);home.Show();var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");settings.Show();Pump(TimeSpan.FromMilliseconds(35));
        var scroll=AppearanceScroll032(settings);Assert(scroll.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden && scroll.ScrollableHeight>0,"Only Appearance hides its bar while retaining its scroll extent");Bottom032(settings,".33 hidden scrollbar");
        Assert(Descendants(settings).OfType<RadioButton>().Where(r=>r.Tag is AppTheme).Select(r=>System.Windows.Automation.AutomationProperties.GetName(r)).SequenceEqual(new[]{"nexa","Violet","Aurora","Ember"}),"Official swatches use the requested four visible labels");
        Assert(Descendants(settings).OfType<TextBlock>().Single(t=>Equals(t.Tag,"SeasonalThemeOverride")).Visibility==Visibility.Collapsed,"Seasonal override note is hidden in Classic");
        var wallpaper=typeof(HomeWindow).GetField("homeWallpaper",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        Assert(((Border)wallpaper.GetValue(home)!).Background is SolidColorBrush {Color:var color} && color==Color.FromRgb(18,52,86),"Classic applies the saved Home wallpaper");
        void Season(bool enabled){var toggle=Descendants(settings).OfType<CheckBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Nexaween enabled");toggle.IsChecked=enabled;toggle.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));settings.FlushPendingChanges();Pump(TimeSpan.FromMilliseconds(30));}
        Season(true);
        Assert(Descendants(AppearanceScroll032(settings)).OfType<ScrollViewer>().Count()==0,"Seasonal Appearance retains one scroll owner without nested competing scrollers");
        var note=Descendants(settings).OfType<TextBlock>().Single(t=>Equals(t.Tag,"SeasonalThemeOverride"));Assert(note.Visibility==Visibility.Visible && note.Text=="Nexaween overrides the theme color.","Seasonal mode displays the exact required subtle override message");
        Assert(((Border)wallpaper.GetValue(home)!).Background==null,"Seasonal Home does not display the saved Classic wallpaper");
        var keys=new[]{"AccentBrush","PixelBrush","BackgroundBrush","PanelBrush","SurfaceBrush","TextBrush","AccentTextBrush"};var before=keys.Select(k=>((SolidColorBrush)Application.Current.Resources[k]).Color).ToArray();
        Descendants(settings).OfType<RadioButton>().Single(r=>Equals(r.Tag,AppTheme.Aurora)).IsChecked=true;settings.FlushPendingChanges();
        Assert(keys.Select(k=>((SolidColorBrush)Application.Current.Resources[k]).Color).SequenceEqual(before),"Selecting a Classic palette leaves active seasonal colors unchanged");
        Assert(store.LoadAppTheme()==AppTheme.Aurora && home.Saved.Workspace!.Settings.HomeWallpaper.SolidColor=="#123456","Classic choices persist independently while seasonal mode is active");
        Capture032(settings,"seasonal-override");Season(false);
        Assert(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color==Color.FromRgb(56,189,248),"Returning to Classic applies the palette chosen during nexaween");
        Assert(((Border)wallpaper.GetValue(home)!).Background is SolidColorBrush {Color:var restored} && restored==Color.FromRgb(18,52,86),"Returning to Classic restores its wallpaper");
        Season(true);settings.Close();home.Close();home=new HomeWindow(store);home.Show();
        Assert(store.LoadAppTheme()==AppTheme.Aurora && home.Saved.Workspace!.Settings.SeasonalMode=="On" && home.Saved.Workspace.Settings.HomeWallpaper.SolidColor=="#123456","Seasonal state and saved Classic appearance survive restart independently");home.Close();
        var main=new MainWindow(store);main.Show();main.SaveSpaceBackground(new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#654321"});Assert(main.SpaceBackgroundLayer.Background==null,"Seasonal Space keeps its Classic background saved but hidden");main.SettingsState.Workspace!.Settings.SeasonalMode="Off";Theme.SetSeasonalEnabled(false);Assert(main.SpaceBackgroundLayer.Background is SolidColorBrush {Color:var spaceColor} && spaceColor==Color.FromRgb(101,67,33),"Switching a Space back to Classic restores its independent wallpaper immediately");main.SettingsState.Workspace.Settings.SeasonalMode="On";Theme.SetSeasonalEnabled(true);Assert(main.SpaceBackgroundLayer.Background==null && main.SettingsState.Background.SolidColor=="#654321","Returning a Space to seasonal mode preserves its saved Classic background");main.Close();
    }
    private static void CheckAssets033()
    {
        var digest=new string('A',64);object Asset(string name)=>new{name,size=100,digest="sha256:"+digest,browser_download_url="https://github.com/scrmarsh/nexa/releases/download/v0.0.34-alpha/"+name};
        foreach(var assets in new[]{new[]{Asset("Nexa.exe"),Asset("NexaSetup.exe"),Asset("nexa-portable.exe")},new[]{Asset("NexaSetup.exe"),Asset("Nexa.exe"),Asset("nexa-portable.exe")}})
        {
            var feed=JsonSerializer.Serialize(new[]{new{draft=false,tag_name="v0.0.34-alpha",prerelease=true,name="nexa",body="fixture",assets}});
            using var client=new HttpClient(new AuditHandler((_,_)=>new(HttpStatusCode.OK){Content=new StringContent(feed)}));var release=new ExperimentalUpdater(client).CheckAsync(new(0,0,33)).GetAwaiter().GetResult();
            Assert(release is {Portable:false} && Path.GetFileName(release.Url.AbsolutePath)=="NexaSetup.exe","Official installer wins independent of GitHub asset ordering; standalone portable is not an update package");
        }
        Assert(AvailableUpdate.IsInstallerName("NexaSetup.exe") && AvailableUpdate.IsInstallerName("Nexa-0.0.34-Alpha-Setup.exe") && !AvailableUpdate.IsInstallerName("nexa-portable.exe"),"New and historical installer naming classify explicitly");
    }
    private static void CheckMigration033()
    {
        var root=Path.Combine(output,"migration033");var legacy=Path.Combine(root,"legacy");Directory.CreateDirectory(legacy);var old=Path.Combine(legacy,"Nexa (2).exe");File.Copy("Builds/Testing/nexa029-prerelease/Nexa.exe",old);
        var source=new StateStore(Path.Combine(legacy,"state.json"));var sourceState=source.Load();sourceState.Notes="Portable private note";sourceState.Workspace=Continuity.Migrate(sourceState,source.UserId);var dashboard=sourceState.Workspace.Dashboards.Single();var historical=Continuity.Clone(dashboard.Current);historical.SessionId=Guid.NewGuid();historical.IsActive=false;dashboard.History.Add(historical);source.Save(sourceState);var sourceBytes=File.ReadAllText(source.FilePath);
        var target=new StateStore(Path.Combine(root,"private","state.json"));target.Save(new AppState{Notes="Existing installed note"});var initialId=target.Load().Workspace!.ActiveDashboardId;
        var install=Path.Combine(root,"app");Directory.CreateDirectory(install);File.Copy(Path.Combine(Path.GetDirectoryName(typeof(App).Assembly.Location)!,"Nexa.exe"),Path.Combine(install,"Nexa.exe"));
        var impostor=Path.Combine(legacy,"nexa-unrelated.exe");File.WriteAllText(impostor,"Unrelated file; do not execute or remove.");
        var copies=InstallerMaintenance.Discover(new[]{legacy},install).ToArray();Assert(copies.Length==1 && copies[0].VerifiedRelease && copies[0].Path==old,"Discovery recognizes a real published duplicate-named portable and rejects filename-only impostors");
        var planPath=Path.Combine(root,"plan.json");var plan=InstallerMaintenance.Prepare(planPath,NexaFiles.Data(target),install,new[]{legacy}).GetAwaiter().GetResult();
        Assert(File.Exists(Path.Combine(plan.Backup,"old-executables",copies[0].Hash+".exe")) && File.Exists(Path.Combine(plan.Backup,"data-backup","workspace.json")),"Verified executable and private saves are backed up before cleanup");
        var merged=target.Load().Workspace!;Assert(merged.ActiveDashboardId==initialId && merged.Dashboards.Count==2 && merged.Dashboards.Any(d=>d.Current.Data.Notes=="Portable private note") && merged.Dashboards.Any(d=>d.Current.Data.Notes=="Existing installed note"),"Adjacent portable Spaces merge without overwriting the installed Space or selection");
        Assert(merged.Dashboards.Sum(d=>d.History.Count)==1 && File.ReadAllText(source.FilePath)==sourceBytes,"Adjacent session history and original source save remain intact");
        Assert(UpdateTransaction.IsWithin(NexaFiles.Data(target),NexaFiles.History(target)),"Fixture documents remain under isolated test data");
        var count=NexaFiles.WriteHistory(target);Assert(count>=3 && Directory.GetFiles(NexaFiles.History(target),"*.md").Length==count,"Readable history documents are accessible without becoming a competing save database");
        var before=File.ReadAllText(target.FilePath);NexaFiles.WriteHistory(target);Assert(File.ReadAllText(target.FilePath)==before,"Refreshing history documents does not alter the authoritative saves");
        InstallerMaintenance.Complete(planPath,true,_=>true).GetAwaiter().GetResult();Assert(!File.Exists(old) && File.Exists(impostor) && File.Exists(source.FilePath),"Cleanup removes only a backed-up verified old executable, preserving data and unrelated files");
        Assert(File.Exists(Path.Combine(plan.Backup,"old-executables",copies[0].Hash+".exe")),"Removed portable executable remains recoverable from its verified backup");
    }
}
