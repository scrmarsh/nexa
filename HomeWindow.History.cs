namespace SessionDashboard;
internal sealed partial class HomeWindow
{
    private void OpenHistory()
    {
        if (!FlushSettingsForNavigation()) return;
        new SessionHistoryWindow(this, store, openSpace: OpenDashboard).ShowDismissible();
    }
}
