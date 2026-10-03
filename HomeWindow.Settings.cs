namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private SettingsDialog? settingsWindow;
    private bool FlushSettingsForNavigation()
    {
        if (settingsWindow?.FlushPendingChanges() == false) return false;
        settingsWindow?.Close();
        // History commands may have updated the store since Home was constructed.
        Saved = store.Load();
        return true;
    }
    private void OpenSettings() => OpenSettings("Appearance");
    private void OpenSettings(string section)
    {
        if (settingsWindow is { IsVisible: true }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }
        Saved = store.Load();
        settingsWindow = new SettingsDialog(this, store, Saved, null, section);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.ShowDismissible();
    }
}
