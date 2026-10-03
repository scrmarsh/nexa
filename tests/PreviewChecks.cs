using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void RenderPreview(FrameworkElement content, string name)
    {
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth), (int)Math.Ceiling(content.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(output, name + ".png")); encoder.Save(file);
    }
    private static void CheckPreview()
    {
        var now = DateTimeOffset.Now;
        foreach (var kind in PreviewDashboards.Scenarios)
        {
            var workspace = PreviewDashboards.Generate("preview-test", random: new Random(17), at: now, scenario: kind);
            Continuity.ValidateWorkspace(workspace, "preview-test");
            var dashboard = workspace.Dashboards.Single();
            Assert(dashboard.Current.Data.Notes.Length > 100 && dashboard.Current.Data.Tasks.Any(t => t.Done) &&
                dashboard.Current.Data.Tasks.Any(t => !t.Done) && dashboard.Current.Data.Widgets.Count >= 5, kind + " preview has coherent notes, completed/incomplete tasks and varied widgets");
            Assert(dashboard.Current.Data.Session!.IsRunning(now) && dashboard.Current.Data.Session.Progress(now) > .1 &&
                dashboard.History.Count == 2 && dashboard.History.All(h => !h.IsActive && h.Data.Session!.End < now), kind + " preview has a live timer, interval notes and populated history");
            if (kind == "Argo Pantry")
                Assert(dashboard.Current.Data.Widgets.Single(w => w.Kind == "Record tracker").Records.All(r => r.Values.ContainsKey("Student ID")) &&
                    dashboard.Current.Data.Widgets.Any(w => w.Kind == "Statistics"), "Pantry preview supplies records and live totals");
            if (kind == "Church")
                Assert(dashboard.Current.Data.Widgets.Single(w => w.Kind == "Bible").Text.Length > 0 &&
                    dashboard.Current.Data.Widgets.Single(w => w.Kind == "Prayer requests").Items.Count > 0, "Church preview supplies passage references and prayer requests");
            var next = PreviewDashboards.Generate("preview-test", dashboard.PresetOrigin, new Random(17), now);
            Assert(next.Dashboards.Single().PresetOrigin != dashboard.PresetOrigin, "Another preview avoids repeating " + kind);
        }
        var homeStore = new StateStore(Path.Combine(output, "home-state.json"));
        homeStore.Save(new AppState { Notes = "My personal notes must stay unchanged." });
        homeStore.SaveThemeMode(SessionDashboard.ThemeMode.Dark);
        var original = File.ReadAllBytes(homeStore.FilePath);
        var preferences = File.ReadAllBytes(Path.Combine(output, "preferences.json"));
        var home = new HomeWindow(homeStore) { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual };
        home.Show(); Pump(TimeSpan.FromMilliseconds(250));
        home.Width = 1040; home.Height = 720; Pump(TimeSpan.FromMilliseconds(100)); var homeWidth = home.ActualWidth; var homeHeight = home.ActualHeight;
        Assert(!Descendants(home).OfType<Button>().Any(b => Equals(b.Content, "Preview Nexa")) && Descendants(home).OfType<Button>().Any(b => Equals(b.Content, "About Nexa")), "Preview moves from Home into About Nexa");
        Assert(App.AppVersion == "1.0.4", "App metadata reports release 1.0.4");
        var historyButton = Descendants(home).OfType<Button>().Single(b => Equals(b.Content, "View history"));
        Assert(historyButton.TranslatePoint(new Point(0, 0), home).Y > home.ActualHeight * .8, "History stays at the bottom of Home");
        var tagline = Descendants(home).OfType<TextBlock>().Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Home tagline");
        Pump(TimeSpan.FromSeconds(2.2));
        Assert(tagline.Text == "a space for whatever comes next" && tagline.Opacity > .9, "Home tagline types out beside the logo");
        RenderPreview((FrameworkElement)home.Content, "home-tagline-typed"); Pump(TimeSpan.FromSeconds(1.8));
        Assert(tagline.Opacity < .1, "Typed tagline fades away without shifting Home layout");
        Pump(TimeSpan.FromSeconds(2));
        Assert(tagline.Opacity > .9 && tagline.Text.Length > 8, "Tagline restarts after fading instead of remaining missing");
        void InspectInfo(string buttonName, string title, Action<Window> inspect)
        {
            var inspected = false; var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (_, _) => { var dialog = home.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title.EndsWith(title)); if (dialog == null) return; timer.Stop(); inspect(dialog); inspected = true; dialog.Close(); };
            timer.Start(); Descendants(home).OfType<Button>().Last(b => Equals(b.Content, buttonName)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(220)); Assert(inspected, title + " opens as a themed menu");
        }
        InspectInfo("About Nexa", "About Nexa", dialog =>
        {
            Assert(dialog is NexaDialog && Descendants(dialog).OfType<Button>().Any(b => Equals(b.Content, "Preview Nexa") && b.IsVisible), "Themed About contains Preview Nexa");
            Assert(Descendants(dialog).OfType<TextBlock>().Any(t => t.Text.Contains("classroom lessons")), "About describes the updated session use cases"); RenderPreview((FrameworkElement)dialog.Content, "about-nexa-103");
        });
        InspectInfo("Changelog", "Changelog", dialog =>
        {
            Assert(Descendants(dialog).OfType<TextBlock>().Any(t => t.Text == "v1.0.3") && Descendants(dialog).OfType<System.Windows.Shapes.Ellipse>().Count() >= 8, "Changelog presents versioned release cards with polished bullets"); RenderPreview((FrameworkElement)dialog.Content, "changelog-103");
        });
        var settingsButton = Descendants(home).OfType<Button>().Single(b => System.Windows.Automation.AutomationProperties.GetName(b) == "Settings");
        settingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(120));
        var settingsDialog = home.OwnedWindows.OfType<NexaDialog>().Single();
        Assert(settingsDialog.Title == "Nexa" && Descendants(settingsDialog).OfType<Button>().Any(b => Equals(b.Content, "Appearance")) && Descendants(settingsDialog).OfType<Button>().Any(b => Equals(b.Content, "Hedy")), "Cog opens the two-pane Appearance and Hedy menu without a Settings title");
        Assert(Descendants(settingsDialog).OfType<ComboBox>().Any(combo => combo.ItemsSource is AppTheme[]), "Appearance offers full app theme choices separate from dashboard colors");
        home.RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, Environment.TickCount, System.Windows.Input.MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseDownEvent }); Pump(TimeSpan.FromMilliseconds(220));
        Assert(!settingsDialog.IsVisible, "Clicking outside dismisses the Settings menu");
        RenderPreview((FrameworkElement)home.Content, "preview-home");
        var window = home.OpenPreview(Path.Combine(output, "samples"))!;
        window.Left = -10000; window.Top = -10000;
        Pump(TimeSpan.FromMilliseconds(350));
        Assert(window.ActualWidth == homeWidth && window.ActualHeight == homeHeight, "Home-to-dashboard transition preserves window size");
        Assert(!Descendants(window.TopNavigation).OfType<Button>().Any(b => Equals(b.Content, "View history")) && Descendants(home).OfType<Button>().Any(b => Equals(b.Content, "View history")), "History navigation belongs to Home");
        Assert(window.IsPreview && window.PreviewBanner.IsVisible && window.HedyKeyInput.Password.Length == 0,
            "Preview is labeled, isolated and does not load real Hedy credentials");
        var before = window.Countdown.Text;
        Pump(TimeSpan.FromSeconds(1.2));
        Assert(window.Countdown.Text != before, "Preview countdown runs live");
        window.NotesBox.Text = "Changed sample notes";
        Pump(TimeSpan.FromSeconds(1.2));
        Assert(File.ReadAllBytes(homeStore.FilePath).SequenceEqual(original), "Editing and autosaving preview do not alter personal dashboards");
        var origin = window.state.Workspace!.Dashboards.Single().PresetOrigin;
        window.Width = 1060; window.Height = 730; Pump(TimeSpan.FromMilliseconds(150));
        var previewWidth = window.ActualWidth; var previewHeight = window.ActualHeight;
        Click(window, "Another preview"); Pump(TimeSpan.FromMilliseconds(300));
        Assert(Math.Abs(window.ActualWidth - previewWidth) < 1 && Math.Abs(window.ActualHeight - previewHeight) < 1, "Another preview preserves the resized window dimensions");
        Assert(window.state.Workspace!.Dashboards.Single().PresetOrigin != origin && window.state.Notes != "Changed sample notes",
            "Another preview generates a new populated sample in the same window");
        Assert(File.ReadAllBytes(Path.Combine(output, "preferences.json")).SequenceEqual(preferences), "Preview theme and palette changes leave personal preferences intact");
        RenderPreview((FrameworkElement)window.Content, "preview-dashboard");
        var pickerChecked = false;
        var inspectPicker = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        inspectPicker.Tick += (_, _) =>
        {
            var dialog = window.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title.Contains("Add widget"));
            if (dialog == null) return;
            inspectPicker.Stop();
            var choices = Descendants(dialog).OfType<Button>().Where(b => b.Tag is string && b.Content is Grid).ToList();
            Assert(choices.Count > 10 && choices.All(b => b.HorizontalContentAlignment == HorizontalAlignment.Stretch && ((Grid)b.Content).ColumnDefinitions[0].Width.Value == 42), "Widget picker uses a compact icon column and stretched text rows");
            RenderPreview((FrameworkElement)dialog.Content, "add-widget-icons"); pickerChecked = true; dialog.Close();
        };
        inspectPicker.Start(); window.WidgetCanvas.Children.OfType<Button>().Single(b => Equals(b.Tag, "AddWidgetShortcut")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(220)); Assert(pickerChecked, "Dashboard plus opens the widget picker");
        window.Width = 960; window.Height = 660; Pump(TimeSpan.FromMilliseconds(250));
        RenderPreview((FrameworkElement)window.Content, "preview-dashboard-small");
        Assert(window.PreviewBanner.IsVisible && Descendants(window.PreviewBanner).OfType<Button>().Single().IsVisible,
            "Preview navigation stays visible in a small window");
        Click(window, "Home"); Pump(TimeSpan.FromMilliseconds(350));
        var returned = (HomeWindow)Application.Current.MainWindow;
        Assert(returned.ActualWidth == window.ActualWidth && returned.ActualHeight == window.ActualHeight, "Dashboard-to-Home transition preserves window size");
        var captionNames = new[] { "Minimize", "Maximize or restore", "Close" };
        var captionControls = Descendants(returned).OfType<Button>().Where(b => captionNames.Contains(System.Windows.Automation.AutomationProperties.GetName(b))).ToList();
        Assert(captionControls.Count == 3 && captionControls.All(b => b.Width == 40 && b.FontSize == window.FullscreenButton.FontSize), "Home uses the same caption icon sizes as the dashboard");
        Assert(returned.Saved.Notes == "My personal notes must stay unchanged." && File.ReadAllBytes(homeStore.FilePath).SequenceEqual(original),
            "Home returns from preview to the untouched personal workspace");
        returned.Close();
        foreach (var kind in PreviewDashboards.Scenarios)
        {
            var sampleStore = new StateStore(Path.Combine(output, "render-" + kind, "state.json"));
            sampleStore.Save(Continuity.Activate(PreviewDashboards.Generate(sampleStore.UserId, random: new Random(5), scenario: kind), sampleStore.UserId));
            var sample = new MainWindow(sampleStore, homeStore) { Left = -10000, Top = -10000, WindowStartupLocation = WindowStartupLocation.Manual };
            sample.Show(); Pump(TimeSpan.FromMilliseconds(250));
            RenderPreview((FrameworkElement)sample.Content, "preview-" + kind.Replace(" ", "-"));
            sample.Close();
        }
    }
}
