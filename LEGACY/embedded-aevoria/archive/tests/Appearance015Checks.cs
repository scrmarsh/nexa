using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SessionDashboard;
using ThemeMode = SessionDashboard.ThemeMode;

internal static partial class Program
{
    private static void CheckAppearance015()
    {
        var path = Path.Combine(output, "appearance015.json");
        var store = new StateStore(path);
        store.Save(new AppState { Notes = "Preserve recovery notes", AccentColor = "#987654", PixelColor = "#123456" });
        var bugs = new BugPreferences(true, BugSpecies.AllThree, BugDensity.Low);
        store.SaveBugPreferences(bugs); store.SavePreferences(ThemeMode.Dark, AppTheme.Nexa);
        Theme.Apply(Application.Current.Resources, ThemeMode.Dark, AppTheme.Nexa);
        var home = new HomeWindow(store) { Left = -10000, Top = -10000, ShowActivated = false };
        var space = new MainWindow(store) { Left = -10000, Top = -10000, ShowActivated = false };
        home.Show(); space.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var bugManager = space.bugsOverlay!.Manager;
        var windowsLight = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) is int value && value != 0;
        var sequence = 0;
        foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark, ThemeMode.System })
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            var fromHome = sequence++ % 2 == 0;
            var settings = new SettingsDialog(fromHome ? home : space, store, fromHome ? home.Saved : space.state, fromHome ? null : space, "Appearance");
            settings.Show(); Pump(TimeSpan.FromMilliseconds(30));
            void Select(string label)
            {
                var choice = Descendants(settings).OfType<RadioButton>().Single(c => Equals(c.Content, label));
                ((System.Windows.Automation.Provider.ISelectionItemProvider)new System.Windows.Automation.Peers.RadioButtonAutomationPeer(choice)
                    .GetPattern(System.Windows.Automation.Peers.PatternInterface.SelectionItem)).Select();
            }
            Select(mode.ToString()); Select(theme.ToString());
            Pump(TimeSpan.FromMilliseconds(30));
            var expectedLight = mode == ThemeMode.Light || mode == ThemeMode.System && windowsLight;
            var expectedAccent = (Color)ColorConverter.ConvertFromString(Theme.AppPalette(theme).Accent);
            var app = Application.Current.Resources;
            var background = ((SolidColorBrush)app["BackgroundBrush"]).Color;
            Assert((((SolidColorBrush)space.DashboardHeader.Background).Color == Colors.White) == expectedLight &&
                ((SolidColorBrush)space.Background).Color == background && ((SolidColorBrush)home.Background).Color == background &&
                ((SolidColorBrush)settings.Background).Color == background && ((SolidColorBrush)app["AccentBrush"]).Color == expectedAccent,
                $"{mode}/{theme} switches Home, Space and Settings live from {(fromHome ? "Home" : "Space")} Settings");
            settings.SelectSection("Profile"); Pump(TimeSpan.FromMilliseconds(20));
            Assert(Descendants(settings).OfType<TextBlock>().Where(t => t.Text == "Profile").Any(t =>
                t.Foreground is SolidColorBrush foreground && foreground.Color == ((SolidColorBrush)app["TextBrush"]).Color),
                $"{mode}/{theme} refreshes the previously detached Profile page");
            Assert(settings.FlushPendingChanges(), "Appearance autosave succeeds"); settings.Close();
            var reloaded = new StateStore(path);
            Assert(reloaded.LoadThemeMode() == mode && reloaded.LoadAppTheme() == theme && reloaded.LoadBugPreferences() == bugs,
                $"{mode}/{theme} persists independently while preserving Add Bugs preferences");
        }
        Assert(ReferenceEquals(space.bugsOverlay!.Manager, bugManager) && space.BugsRunning &&
            space.state.Notes == "Preserve recovery notes" && space.state.AccentColor == "#987654" && space.state.PixelColor == "#123456",
            "Appearance switching preserves the running bug manager, notes and per-Space colors");
        space.Close(); home.Close();
        Theme.Apply(Application.Current.Resources, ThemeMode.Light, AppTheme.Nexa);
        var restored = new HomeWindow(new StateStore(path)) { Left = -10000, Top = -10000, ShowActivated = false };
        restored.Show(); Pump(TimeSpan.FromMilliseconds(40));
        Assert(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color == (Color)ColorConverter.ConvertFromString(Theme.AppPalette(AppTheme.Ember).Accent) &&
            ((((SolidColorBrush)Application.Current.Resources["PanelBrush"]).Color == Colors.White) == windowsLight),
            "Home startup restores saved System appearance and Ember accent together");
        restored.Close();
    }
}
