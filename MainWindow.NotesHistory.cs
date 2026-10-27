using System.Windows;
namespace SessionDashboard;
public partial class MainWindow
{
    internal void ShowNotesHistory() => NotesHistoryClick(this, new RoutedEventArgs());
    private void NotesHistoryClick(object sender, RoutedEventArgs e)
    {
        SaveNow(); // A failed checkpoint must not prevent viewing the last readable archive.
        new SessionHistoryWindow(this, store, this).ShowDismissible();
    }
}
