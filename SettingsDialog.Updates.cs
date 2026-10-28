using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private CancellationTokenSource usingCancellation = new();
    private UIElement BuildUpdates()
    {
        var page = Page("Updates", "Experimental update checks use published Nexa releases. Checks never install anything automatically.");
        page.Children.Add(new TextBlock { Text = "Current version: " + App.VersionLabel, FontSize = 16, Margin = new Thickness(0, 2, 0, 14) });
        var preferences = store.LoadUpdatePreferences();
        var startup = new CheckBox { Content = "Check when Nexa starts", IsChecked = preferences.CheckOnStartup, Margin = new Thickness(0, 0, 0, 14) };
        startup.Click += (_, _) => { preferences.CheckOnStartup = startup.IsChecked == true; store.SaveUpdatePreferences(preferences); };
        page.Children.Add(startup);
        var check = new Button { Content = "Check for updates", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 8, 14, 8) };
        page.Children.Add(check);
        var status = new TextBlock { Text = "Startup checks are off by default.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 10) };
        page.Children.Add(status);
        var notes = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 180, Visibility = Visibility.Collapsed };
        page.Children.Add(notes);
        var download = new Button { Content = "Download update", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 12, 0, 0), Visibility = Visibility.Collapsed };
        page.Children.Add(download);
        var cancel = new Button { Content = "Cancel download", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) }; page.Children.Add(cancel);
        var restart = new Button { Content = "Restart Nexa", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 0) }; page.Children.Add(restart);
        usingCancellation = new CancellationTokenSource();
        Closed += (_, _) => usingCancellation.Cancel();
        CancellationTokenSource? downloadCancellation = null;
        cancel.Click += (_, _) => downloadCancellation?.Cancel();
        string? prepared = null;
        restart.Click += (_, _) =>
        {
            if (prepared == null || !FlushPendingChanges() || space != null && !space.SaveForClose()) { status.Text = "Save your current changes before restarting."; return; }
            try { UpdateTransaction.StartHelper(prepared); Application.Current.Shutdown(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { status.Text = "Could not restart: " + ex.Message; }
        };
        AvailableUpdate? available = null;
        check.Click += async (_, _) =>
        {
            check.IsEnabled = false; download.Visibility = Visibility.Collapsed; notes.Visibility = Visibility.Collapsed;
            status.Text = "Checking published releases…";
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                available = await new ExperimentalUpdater(client).CheckAsync(new Version(App.AppVersion), usingCancellation.Token);
                preferences.LastSuccessfulCheckUtc = DateTimeOffset.UtcNow;
                preferences.LastOfferedVersion = available == null ? "" : available.Version.ToString(3) + " " + available.Channel;
                store.SaveUpdatePreferences(preferences);
                if (available == null) { status.Text = "You're up to date. No newer compatible release is available."; return; }
                status.Text = $"Nexa {available.Version.ToString(3)} {available.Channel} is available ({available.Size / 1024d / 1024d:0.0} MB).";
                notes.Text = $"{available.Name}\nPublished: {(available.PublishedAt?.ToLocalTime().ToString("D") ?? "Date unavailable")}\n\n" + (string.IsNullOrWhiteSpace(available.Notes) ? "No release notes were provided." : available.Notes);
                notes.Visibility = Visibility.Visible; download.Visibility = available.Portable ? Visibility.Visible : Visibility.Collapsed; if (!available.Portable) status.Text += " This release uses the legacy installer; install it from the official Releases page.";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or IOException or KeyNotFoundException or InvalidOperationException or FormatException)
            { available = null; status.Text = "Update check unavailable: " + ex.Message; }
            finally { check.IsEnabled = true; }
        };
        download.Click += async (_, _) =>
        {
            if (available is not { } release) return;
            download.IsEnabled = check.IsEnabled = false; cancel.Visibility = Visibility.Visible;
            downloadCancellation = CancellationTokenSource.CreateLinkedTokenSource(usingCancellation.Token);
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                var progress = new Progress<double>(value => Dispatcher.InvokeAsync(() => status.Text = $"Downloading update… {value:P0}"));
                var directory = Path.Combine(store.UserPreferencesDirectory, "updates", "downloads");
                var path = await new ExperimentalUpdater(client).DownloadAsync(release, directory, progress, downloadCancellation.Token);
                await Dispatcher.InvokeAsync(async () =>
                {
                    if (usingCancellation.IsCancellationRequested) return;
                    status.Text = "Download verified against the official GitHub asset digest.";
                    if (new NexaConfirmation(this, "Prepare Nexa update?", $"Nexa {release.Version.ToString(3)} {release.Channel} is ready. Preparing backs up your local data and stages a verified executable. You choose when to restart and install.", "Prepare Update").ShowDialog() != true)
                    { status.Text = "Installation canceled. Nexa is unchanged."; return; }
                    if (!FlushPendingChanges() || space != null && !space.SaveForClose()) throw new IOException("Your current changes could not be saved. Nothing will be replaced.");
                    var target = Environment.ProcessPath ?? throw new IOException("Application path unavailable.");
                    if (!Path.GetFileName(target).Equals("Nexa.exe", StringComparison.OrdinalIgnoreCase)) throw new IOException("Portable updates require the published Nexa.exe, not a development host.");
                    prepared = await UpdateTransaction.Prepare(store, target, path, release.Sha256, release.Version.ToString(3));
                    await Dispatcher.InvokeAsync(() => { status.Text = "Backup complete. Restart to install; failed startup restores the previous executable."; restart.Visibility = Visibility.Visible; download.Visibility = Visibility.Collapsed; });
                }).Task.Unwrap();
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or InvalidDataException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            { await Dispatcher.InvokeAsync(() => status.Text = "Update was not installed: " + (ex is OperationCanceledException ? "Download canceled." : ex.Message)); }
            finally
            {
                downloadCancellation.Dispose(); downloadCancellation = null;
                await Dispatcher.InvokeAsync(() => { download.IsEnabled = check.IsEnabled = true; cancel.Visibility = Visibility.Collapsed; });
            }
        };
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
