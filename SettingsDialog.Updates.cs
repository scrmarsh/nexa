using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
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
        var download = new Button { Content = "Download and install", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 12, 0, 0), Visibility = Visibility.Collapsed };
        page.Children.Add(download);
        AvailableUpdate? available = null;
        check.Click += async (_, _) =>
        {
            check.IsEnabled = false; download.Visibility = Visibility.Collapsed; notes.Visibility = Visibility.Collapsed;
            status.Text = "Checking published releases…";
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
                available = await new ExperimentalUpdater(client).CheckAsync(new Version(App.AppVersion));
                preferences.LastSuccessfulCheckUtc = DateTimeOffset.UtcNow;
                preferences.LastOfferedVersion = available == null ? "" : available.Version.ToString(3) + " " + available.Channel;
                store.SaveUpdatePreferences(preferences);
                if (available == null) { status.Text = "You're up to date. No newer verified setup release is available."; return; }
                status.Text = $"Nexa {available.Version.ToString(3)} {available.Channel} is available ({available.Size / 1024d / 1024d:0.0} MB).";
                notes.Text = $"{available.Name}\nPublished: {(available.PublishedAt?.ToLocalTime().ToString("D") ?? "Date unavailable")}\n\n" + (string.IsNullOrWhiteSpace(available.Notes) ? "No release notes were provided." : available.Notes);
                notes.Visibility = download.Visibility = Visibility.Visible;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or IOException or KeyNotFoundException or InvalidOperationException or FormatException)
            { available = null; status.Text = "Update check unavailable: " + ex.Message; }
            finally { check.IsEnabled = true; }
        };
        download.Click += async (_, _) =>
        {
            if (available is not { } release) return;
            download.IsEnabled = false;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                var progress = new Progress<double>(value => status.Text = $"Downloading verified installer… {value:P0}");
                var directory = Path.Combine(store.UserPreferencesDirectory, "updates");
                var path = await new ExperimentalUpdater(client).DownloadAsync(release, directory, progress);
                status.Text = "Installer verified and ready.";
                if (new NexaConfirmation(this, "Install Nexa update?", $"Nexa {release.Version.ToString(3)} {release.Channel} is ready to install. Nexa will close while the installer runs.", "Install Update").ShowDialog() != true)
                { File.Delete(path); status.Text = "Installation canceled. Nexa is unchanged."; download.IsEnabled = true; return; }
                status.Text = "Launching installer…";
                using var launched = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }) ?? throw new IOException("Windows did not start the installer.");
                Application.Current.Shutdown();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidDataException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            { status.Text = "Update was not installed: " + ex.Message; download.IsEnabled = true; }
        };
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
