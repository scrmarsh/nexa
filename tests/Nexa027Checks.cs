using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using SessionDashboard;

internal static partial class Program
{
    private static void Check027()
    {
        CheckBranding027();
        Assert(App.AppVersion == "0.0.29" && App.VersionLabel == "nexa v0.0.29 alpha", "Shared current identity uses the Alpha convention");
        Assert(typeof(App).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version == "0.0.29.0", "Windows file version is numeric and current");
        Assert(typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.StartsWith("0.0.29-alpha") == true, "Product version retains the Alpha stage");
        Assert(ReleaseNotes.All[0].Version == "0.0.29" && ReleaseNotes.All[0].Title == "NEXAWEEN: Final Polish & Workspace Evolution" && ReleaseNotes.All.Single(r=>r.Version=="0.0.27").Title=="NEXAWEEN" && ReleaseNotes.All.Single(r => r.Version == "0.1.26").Title == "Spaces in Motion", "New changelog retains historical release identity");
        Assert(ExperimentalUpdater.IsNewer(new(0,0,27), "Alpha", new(0,1,26)), "New Alpha can update the legacy Alpha revision");
        Assert(!ExperimentalUpdater.IsNewer(new(0,1,26), "Alpha", new(0,0,27)), "Legacy Alpha cannot downgrade new Alpha");
        Assert(!ExperimentalUpdater.IsNewer(new(0,1,27), "alpha", new(0,0,27)), "Equivalent legacy identity is not a new package");
        Assert(!ExperimentalUpdater.IsNewer(new(0,0,27), "Alpha", new(0,1,27)), "Equivalent new identity is not a new package");
        Assert(ExperimentalUpdater.IsNewer(new(0,0,28), "Alpha", new(0,0,27)), "Next Alpha revision is newer");
        Assert(ExperimentalUpdater.IsNewer(new(0,1,0), "Beta", new(0,1,99), "Alpha"), "First Beta follows even a higher legacy Alpha patch");
        Assert(!ExperimentalUpdater.IsNewer(new(0,1,99), "Alpha", new(0,1,0), "Beta"), "Legacy Alpha cannot downgrade Beta");
        Assert(ExperimentalUpdater.IsNewer(new(1,0,0), "Stable", new(0,1,0), "Beta"), "Stable release follows Beta");
        Assert(ExperimentalUpdater.IsNewer(new(0,1,2), "Beta", new(0,1,1), "Beta"), "Beta numbering is not normalized as legacy Alpha");
        foreach (var feed in new[] { "[]", "[{\"tag_name\":\"v0.0.28-alpha\",\"assets\":[]}]" })
        {
            using var client = new HttpClient(new AuditHandler((_,_) => new(HttpStatusCode.OK) { Content = new StringContent(feed) }));
            Assert(new ExperimentalUpdater(client).CheckAsync(new(0,0,27)).GetAwaiter().GetResult() == null, "Empty or assetless feed offers no update");
        }
        var mixed = "[" + Feed(version:"0.1.26")[1..^1] + "," + Feed(version:"0.0.28")[1..^1] + "]";
        using (var client = new HttpClient(new AuditHandler((_,_) => new(HttpStatusCode.OK) { Content = new StringContent(mixed) })))
        {
            var updater = new ExperimentalUpdater(client);
            Assert(updater.CheckAsync(new(0,0,27)).GetAwaiter().GetResult()?.Version == new Version(0,0,28), "Release discovery orders mixed Alpha conventions correctly");
            Assert(updater.CheckAsync(new(0,1,26)).GetAwaiter().GetResult()?.Version == new Version(0,0,28), "Legacy installation discovers the new Alpha convention");
            Assert(updater.CheckAsync(new(0,1,0), currentChannel:"Beta").GetAwaiter().GetResult() == null, "Beta feed filtering excludes all Alpha packages");
        }
        var old = JsonSerializer.Deserialize<EcosystemOptions>("{\"Adorable\":false,\"Size\":1.25,\"Costumes\":false,\"Quality\":2}")!;
        Assert(!old.Adorable && !old.AdvancedAnimations && old.Size == 1.25 && !old.Costumes && old.Quality == BugAnimationQuality.High, "Old preferences remain readable and retain their choices");
        Assert(new EcosystemOptions().Adorable && !new EcosystemOptions().AdvancedAnimations, "Fresh settings enable Adorable and disable Advanced Animations");
        var store = new StateStore(Path.Combine(output,"nexaween027.json"));
        store.SaveBugPreferences(new BugPreferences(true,BugSpecies.Ants,BugDensity.Low,old));
        var main = OffscreenHandoff(new MainWindow(store)); main.Show(); Pump(TimeSpan.FromMilliseconds(60));
        Assert(main.Title == App.VersionLabel, "Dashboard window uses shared current identity");
        var world = NexaEcosystem.For(store);
        var settings = new SettingsDialog(main,store,main.state,main,"Add Bugs"); settings.Show();
        CheckBox Toggle(string text) => Descendants(settings).OfType<CheckBox>().Single(c => Equals(c.Content,text));
        var adorable = Toggle("Adorable Mode"); var advanced = Toggle("Advanced Animations"); var costumes = Toggle("Halloween costumes");
        Assert(LogicalTreeHelper.GetParent(adorable) == LogicalTreeHelper.GetParent(advanced) && LogicalTreeHelper.GetParent(advanced) is WrapPanel, "Adorable and Advanced Animations are adjacent and wrap at narrow widths");
        Assert(adorable.IsChecked == false && advanced.IsChecked == false && costumes.IsChecked == false, "Settings load existing preferences without resetting them");
        advanced.IsChecked = true; advanced.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); settings.FlushPendingChanges();
        var persisted = store.LoadBugPreferences().Ecosystem!;
        Assert(persisted.AdvancedAnimations && !persisted.Adorable && !persisted.Costumes && persisted.Size == 1.25 && persisted.MatchAppearance == old.MatchAppearance && persisted.Appearance == old.Appearance, "Advanced animation toggle persists without replacing unrelated choices or automatic appearance");
        var appearance = Descendants(settings).OfType<ComboBox>().Single(c => c.Items.OfType<string>().SequenceEqual(new[] {"Classic","Creepy","Halloween"}));
        appearance.SelectedItem = "Halloween"; settings.FlushPendingChanges();
        Assert(!world.Manager.SeasonalCostumes, "Explicit Halloween appearance respects costume-off choice");
        costumes.IsChecked = true; costumes.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); settings.FlushPendingChanges();
        Assert(world.Manager.SeasonalCostumes && store.LoadBugPreferences().Ecosystem!.Costumes, "Costumes enable independently and persist");
        settings.Close();
        settings = new SettingsDialog(main,store,main.state,main,"Add Bugs"); settings.Show();
        Assert(Toggle("Advanced Animations").IsChecked == true && Toggle("Halloween costumes").IsChecked == true, "Reopened settings restore new toggles");
        Pump(TimeSpan.FromMilliseconds(300)); Capture026(settings,"nexaween027-bug-settings");
        settings.Close();
        var watch = Stopwatch.StartNew(); var splash = new SplashWindow(new NexaOptions { ReduceMotion = true });
        var preview = splash.PreviewIn((Grid)main.Content);
        Pump(TimeSpan.FromMilliseconds(100));
        Assert(!preview.IsCompleted && splash.TaglineComplete && splash.ReportedProgress == 1, "Reduced-motion preview completes stages but remains for minimum display");
        Capture026(main,"nexaween027-splash-preview");
        WaitHandoff(() => preview.IsCompleted,"Five-second compact in-window preview completes"); preview.GetAwaiter().GetResult();
        Assert(watch.ElapsedMilliseconds >= 5000 && splash.DisplayedMilliseconds >= 5000, "Splash minimum duration is measured from display and applies with reduced motion");
        main.Close();
        var home = OffscreenHandoff(new HomeWindow(store)); home.Show();
        Assert(home.Title == App.VersionLabel && Descendants(home).OfType<TextBlock>().Any(t => t.Text == App.VersionLabel), "Home title and shared wordmark show current Alpha");
        Pump(TimeSpan.FromMilliseconds(450));
        Capture026(home,"nexaween027-home"); home.Close();
    }

    private static void CheckBranding027()
    {
        var assets = Path.Combine(Directory.GetCurrentDirectory(),"assets");
        Assert(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(assets,"references","nexa-halloween-icon-1024.png")))) == "F08A21EC47C7814BC0A5112A480C002094DB3BE1753B32E8404026C8365BD6BF", "Authoritative seasonal raster is preserved byte for byte");
        BitmapSource Read(string name) => BitmapDecoder.Create(new Uri(Path.Combine(assets,name)),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
        Color Pixel(BitmapSource image,int x,int y)
        {
            var converted = new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
            var bytes = new byte[4]; converted.CopyPixels(new Int32Rect(x,y,1,1),bytes,4,0);
            return Color.FromArgb(bytes[3],bytes[2],bytes[1],bytes[0]);
        }
        foreach(var size in new[]{16,32,48,64,128,256,1024})
        {
            var image = Read($"nexa-icon-seasonal-{size}.png");
            Assert(image.PixelWidth == size && image.PixelHeight == size && Pixel(image,0,0).A == 0, $"Seasonal icon is {size}px with transparent rounded corners");
        }
        var icon = Read("nexa-icon-seasonal-1024.png");
        Assert(Pixel(icon,500,100) == Color.FromRgb(132,204,22) && Pixel(icon,900,800) == Color.FromRgb(147,51,234), "Reference green/purple diagonal colors are exact");
        Assert(Pixel(icon,250,500) == Color.FromRgb(249,115,22) && Pixel(icon,600,400) == Color.FromRgb(253,186,116), "Orange body and lighter diagonal retain exact reference colors");
        Assert(Pixel(icon,512,1) == Colors.Black && Pixel(icon,216,500) == Colors.Black, "Outer perimeter and hourglass silhouette both have black borders");
        Assert(Pixel(icon,512,512) == Color.FromRgb(249,115,22) && Pixel(icon,530,490) == Color.FromRgb(249,115,22), "Layer crossing has no unwanted internal black seam");
        var frames = BitmapDecoder.Create(new Uri(Path.Combine(assets,"nexa-icon-seasonal.ico")),BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames;
        Assert(frames.Select(f => f.PixelWidth).Order().SequenceEqual(new[]{16,32,48,64,128,256}), "Windows ICO contains all six native icon sizes");
        foreach(var name in new[]{"nexa-logo-full-dark-seasonal.svg","nexa-logo-full-seasonal.svg"})
        {
            var svg = XDocument.Load(Path.Combine(assets,name));
            var groups = svg.Root!.Elements().Where(e => e.Name.LocalName == "g").ToArray();
            Assert(groups[0].Elements().Count(e => (string?)e.Attribute("stroke") == "#000000" && (string?)e.Attribute("stroke-width") == "24") == 2, name + " uses the selected quadruple black borders only on the icon perimeter and union silhouette");
            var letters = groups[1].Elements().ToArray();
            Assert(letters.All(e => (string?)e.Attribute("stroke") != "#000000") && letters.Length == 6, name + " keeps all wordmark letters borderless and geometry intact");
            var classicName = name == "nexa-logo-full-dark-seasonal.svg" ? "nexa-logo-full-dark.svg" : "nexa-logo-full.svg";
            var classic = XDocument.Load(Path.Combine(assets,classicName)).Root!.Elements().Last(e => e.Name.LocalName == "g");
            var expectedLetters = new XElement(classic);
            expectedLetters.Elements().Single(e => (string?)e.Attribute("d") == "M875 112 L755 250").SetAttributeValue("stroke","#FDBA74");
            Assert(XNode.DeepEquals(expectedLetters,groups[1]), name + " changes only the classic purple X stroke to light orange; every other letter, stroke and layer is unchanged");
        }
        Theme.SetSeasonalEnabled(true); Theme.Apply(Application.Current.Resources,SessionDashboard.ThemeMode.Dark,AppTheme.Nexa);
        var logo = new NexaLogo {Width=1140,Height=360}; logo.Measure(new Size(1140,360)); logo.Arrange(new Rect(0,0,1140,360));
        var rendered = new RenderTargetBitmap(1140,360,96,96,PixelFormats.Pbgra32); rendered.Render(logo);
        Assert(Pixel(rendered,815,181) == Colors.White && Pixel(rendered,785,216) == Color.FromRgb(253,186,116), "Actual shared WPF logo renderer displays the classic white/light-orange X contrast");
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(rendered));
        using var stream = File.Create(Path.Combine(output,"nexaween027-logo.png")); encoder.Save(stream);
    }
}
