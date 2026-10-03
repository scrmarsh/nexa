using System.Windows;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly StateStore? previewHomeStore;
    internal bool IsPreview => previewHomeStore != null;
    private void AnotherPreviewClick(object sender, RoutedEventArgs e)
    {
        if (!IsPreview || !SaveNow()) return;
        var previous = Continuity.Current(state.Workspace!, store.UserId).PresetOrigin;
        CommitWorkspace(PreviewDashboards.Generate(store.UserId, previous), preserveWindow: true);
    }
}
