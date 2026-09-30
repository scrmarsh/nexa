using System.IO;
using System.Windows;

namespace SessionDashboard;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--verify-startup")
        {
            StartupVerification.Run(this, Path.GetFullPath(e.Args[1]));
            return;
        }
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SessionDashboard", "state.json");
        var window = new MainWindow(new StateStore(path));
        MainWindow = window;
        window.Show();
    }
}
