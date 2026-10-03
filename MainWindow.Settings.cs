namespace SessionDashboard;

public partial class MainWindow
{
    internal DateTimeOffset? lastSuccessfulSaveAt;
    internal void RefreshWidgetPreferences() { SizeTimerPixels(); if (widgetFocusBackdrop != null) widgetFocusBackdrop.Background = FocusBackdropBrush(); if (focusOverlay != null) focusOverlay.Background = FocusBackdropBrush(); UpdateClock(); }
    private SettingsDialog? settingsWindow;
    internal void OpenMainSettings(string section = "Appearance")
    {
        if (settingsWindow is { IsVisible: true }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }
        settingsWindow = new SettingsDialog(this, store, state, this, section);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.ShowDismissible();
    }
    internal void RefreshThemeViews()
    {
        ApplyColors();
        if (storyOpen) RenderStory();
    }
}
