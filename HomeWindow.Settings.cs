namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private SettingsDialog? settingsWindow;
    private void OpenSettings() => OpenSettings("Appearance");
    private void OpenSettings(string section)
    {
        if (settingsWindow is { IsVisible: true }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }
        settingsWindow = new SettingsDialog(this, store, Saved, null, section);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.ShowDismissible();
    }
}
