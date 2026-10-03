namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private SettingsDialog? settingsWindow;
    private void OpenSettings() => OpenSettings("Appearance");
    private void OpenSettings(string section)
    {
        if (settingsWindow is { IsVisible: true }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }
        settingsWindow = new SettingsDialog(this, store, Saved, null, section);
        settingsWindow.Closed += (_, _) => { settingsWindow = null; RefreshProfileGreeting(); };
        settingsWindow.ShowDismissible();
    }
    private void RefreshProfileGreeting()
    {
        var name = store.LoadProfile().DisplayName.Trim();
        profileGreeting.Text = name.Length == 0 ? "" : "Welcome back, " + name;
        profileGreeting.Visibility = name.Length == 0 ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    }
}
