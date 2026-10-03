namespace SessionDashboard;

public partial class MainWindow
{
    internal DateTimeOffset? lastSuccessfulSaveAt;
    internal void RefreshWidgetPreferences() { SizeTimerPixels(); if (widgetFocusBackdrop != null) widgetFocusBackdrop.Background = FocusBackdropBrush(); if (focusOverlay != null) focusOverlay.Background = FocusBackdropBrush(); UpdateClock(); }
    private SettingsDialog? settingsWindow;
    internal void SettingsChanged()
    {
        if (state.Session is { } session)
        {
            TitleInput.Text = session.Title;
            StartDate.SelectedDate = session.Start.LocalDateTime.Date; EndDate.SelectedDate = session.End.LocalDateTime.Date;
            StartTime.Value = session.Start.LocalDateTime.TimeOfDay; EndTime.Value = session.End.LocalDateTime.TimeOfDay;
            IntervalInput.Text = session.IntervalMinutes.ToString(); SetSessionDays(session.ScheduledDays);
        }
        AccentInput.Text = state.AccentColor; PixelInput.Text = state.PixelColor;
        ApplyColors(); ApplySpaceBackground(); BuildTimeline(); UpdateClock(); Changed();
    }
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
