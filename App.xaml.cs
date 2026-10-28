using System.IO;
using System.Windows;

namespace SessionDashboard;

public partial class App : Application
{
    static App()
    {
        FrameworkElement.UseLayoutRoundingProperty.OverrideMetadata(typeof(Window),new FrameworkPropertyMetadata(true));
        UIElement.SnapsToDevicePixelsProperty.OverrideMetadata(typeof(Window),new FrameworkPropertyMetadata(true));
    }
    internal const string BrandWordmark = "nexa";
    internal static string AppVersion => typeof(App).Assembly.GetName().Version?.ToString(3) ?? "Unknown";
    internal static string VersionLabel => ReleaseLabel(AppVersion);
    internal static string ReleaseLabel(string version) => $"nexa v{version} alpha";
    private Mutex? workspaceMutex;
    internal bool SuppressNormalStartup { get; init; }
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (SuppressNormalStartup) return;
        if (e.Args.Length == 2 && e.Args[0] == "--verify-startup")
        {
            StartupVerification.Run(this, Path.GetFullPath(e.Args[1]));
            return;
        }
        if (e.Args.Length == 2 && e.Args[0] == "--apply-update")
        {
            ApplyUpdateHelper(Path.GetFullPath(e.Args[1])); return;
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
            var store = new StateStore(path);
            var loaded=await Task.Run(()=>(state:store.Load(),mode:store.LoadThemeMode(),theme:store.LoadAppTheme()));
            var options=loaded.state.Workspace?.Settings??new NexaOptions{SeasonalMode="On"};
            Theme.SetSeasonalEnabled(NexaSeason.Active(options,DateTimeOffset.UtcNow));Theme.Apply(Resources,loaded.mode,loaded.theme);
            var splash = new SplashWindow(options); MainWindow = splash;
            splash.ContentRendered += async (_, _) =>
            {
                try
                {
                    var profile=await Task.Run(()=>store.LoadProfile());splash.Report(.2,"Profile settings loaded");
                    if(profile.OnboardingCompleted) { var home = await splash.EnterHome(store,initialState:loaded.state); _ = CheckStartupUpdatesAsync(store, home); return; }
                    await splash.WaitForLogoAsync();
                    await splash.WaitForMinimumDisplayAsync();
                    var welcome=new OnboardingWindow(store) { Owner=splash };
                    splash.Hide();
                    var accepted=welcome.ShowDialog();
                    Window next;
                    if(accepted==true && welcome.Result!=null)
                    {
                        var workspace=new WorkspaceDocument { UserId=store.UserId, Settings=welcome.Options, ActiveDashboardId=welcome.Result.DashboardId, Dashboards=[welcome.Result] };
                        store.Save(Continuity.Activate(workspace,store.UserId)); store.SaveProfile(welcome.Profile); Theme.Apply(Resources,welcome.Profile.ThemeMode,welcome.Profile.Theme); next=new MainWindow(store);
                    }
                    else next=new HomeWindow(store);
                    MainWindow=next; next.Show(); splash.Close();
                }
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
    private async void ApplyUpdateHelper(string path)
    {
        try
        {
            var plan=System.Text.Json.JsonSerializer.Deserialize<UpdatePlan>(File.ReadAllText(path))??throw new InvalidDataException("Missing update plan.");
            await UpdateTransaction.Apply(path, target => UpdateTransaction.Health(target, Path.Combine(Path.GetDirectoryName(path)!, "installed-health")), target =>
            {
                var restart=new System.Diagnostics.ProcessStartInfo(target){UseShellExecute=false,CreateNoWindow=plan.VerificationOnly,WindowStyle=plan.VerificationOnly?System.Diagnostics.ProcessWindowStyle.Hidden:System.Diagnostics.ProcessWindowStyle.Normal};
                // Verification transactions restart into a fresh isolated store, never the user's workspace.
                if(plan.VerificationOnly){restart.ArgumentList.Add("--verify-startup");restart.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(path)!,"restart-health"));}
                System.Diagnostics.Process.Start(restart)?.Dispose();
            });Shutdown(0);
        }
        catch (Exception ex) when (HistoryArchive.Recoverable(ex) || ex is TimeoutException or System.ComponentModel.Win32Exception) { Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { NexaEcosystem.FlushAll(); workspaceMutex?.Dispose(); base.OnExit(e); }
    private static async Task CheckStartupUpdatesAsync(StateStore store, Window home)
    {
        if (!store.LoadUpdatePreferences().CheckOnStartup) return;
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            var release = await new ExperimentalUpdater(client).CheckAsync(new Version(AppVersion));
            var preferences = store.LoadUpdatePreferences();
            var offered = release == null ? "" : release.Version.ToString(3) + " " + release.Channel;
            var alreadyOffered = preferences.LastOfferedVersion == offered;
            preferences.LastSuccessfulCheckUtc = DateTimeOffset.UtcNow;
            preferences.LastOfferedVersion = offered;
            store.SaveUpdatePreferences(preferences);
            if (release != null && !alreadyOffered && home.IsVisible) home.Title += $" · {release.Name} available in Settings → Updates";
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidDataException or UnauthorizedAccessException or IOException or KeyNotFoundException or InvalidOperationException or FormatException) { }
    }
    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        if (MainWindow is MainWindow window && !window.SaveForClose()) e.Cancel = true;
        base.OnSessionEnding(e);
    }
}
