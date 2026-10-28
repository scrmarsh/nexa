namespace SessionDashboard;

public partial class MainWindow
{
    internal DateTimeOffset? lastSuccessfulSaveAt;
    internal void RefreshWidgetPreferences() { SizeTimerPixels(); if (widgetFocusBackdrop != null) widgetFocusBackdrop.Background = FocusBackdropBrush(); if (focusOverlay != null) focusOverlay.Background = FocusBackdropBrush(); UpdateClock(); }
    private SettingsDialog? settingsWindow;
    internal void SettingsChanged()
    {
        ApplyReliabilityOptions();
        if (state.Session is { } session)
        {
            TitleInput.Text = session.Title;
            StartDate.SelectedDate = NexaTime.Local(session.Start, Options.TimeZoneId).DateTime.Date; EndDate.SelectedDate = NexaTime.Local(session.End, Options.TimeZoneId).DateTime.Date;
            StartTime.Value = NexaTime.Local(session.Start, Options.TimeZoneId).DateTime.TimeOfDay; EndTime.Value = NexaTime.Local(session.End, Options.TimeZoneId).DateTime.TimeOfDay;
            IntervalInput.Text = session.IntervalMinutes.ToString(); SetSessionDays(session.ScheduledDays);
        }
        AccentInput.Text = state.AccentColor; PixelInput.Text = state.PixelColor;
        ApplyColors(); ApplySpaceBackground(); BuildTimeline(); UpdateClock(); Changed();
    }
    internal void OpenMainSettings(string section = "Appearance")
    {
        if(settingsWindow is {IsVisible:true})
        {
            if(settingsWindow.SessionInterface==(section is "Session" or "Tips")){settingsWindow.SelectSection(section);settingsWindow.Activate();return;}
            if(!settingsWindow.FlushPendingChanges())return;settingsWindow.Close();
        }
        settingsWindow = new SettingsDialog(this, store, state, this, section);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.ShowDismissible();
    }
    internal void RefreshThemeViews()
    {
        HeaderBrandIcon.Source=NexaChrome.BrandIcon;
        // The selected app theme is already live; reloading stored preferences
        // here would undo it during the autosave debounce.
        Theme.ApplyDashboard(FullContent.Resources, state.AccentColor, state.PixelColor);
        PercentLabel.Foreground = DashboardBrush("PixelBrush");
        if (storyOpen) RenderStory();
    }
}
