using System.IO;
using System.Windows;

namespace SessionDashboard;

public partial class App : Application
{
    private Mutex? workspaceMutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--verify-startup")
        {
            StartupVerification.Run(this, Path.GetFullPath(e.Args[1]));
            return;
        }
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionDashboard", "state.json");
        var userId = System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
        workspaceMutex = new Mutex(true, "Local\\Nexa.SessionDashboard." + userId, out var firstInstance);
        if (!firstInstance)
        {
            MessageBox.Show("Nexa is already open for this Windows account. Use the existing window to keep your saves consistent.", "Nexa");
            Shutdown(); return;
        }
        try
        {
            var splash = new SplashWindow(); MainWindow = splash;
            splash.ContentRendered += async (_, _) =>
            {
                try { await splash.EnterHome(new StateStore(path)); }
                catch (Exception ex) { MessageBox.Show("Nexa could not open your workspace. No data was overwritten.\n\n" + ex.Message, "Nexa"); Shutdown(1); }
            };
            splash.Show();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or NotSupportedException)
        {
            MessageBox.Show("Your saved workspace could not be opened. No data was overwritten.\n\n" + ex.Message, "Nexa", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
        }
    }
    protected override void OnExit(ExitEventArgs e) { workspaceMutex?.Dispose(); base.OnExit(e); }
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        if (MainWindow is MainWindow window && !window.SaveForClose()) e.Cancel = true;
        base.OnSessionEnding(e);
    }
}
