using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow
{
    private HedyClient? hedy;
    private CancellationTokenSource hedyCancellation = new();
    private readonly DispatcherTimer hedyTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private bool hedyBusy;
    private int hedyGeneration;
    private int hedyFailures;
    private readonly Dictionary<(Session, int), Window> noteWindows = [];

    private void InitializeHedy()
    {
        HedyRegionPicker.SelectedIndex = state.HedyRegion == "EU" ? 1 : 0;
        HedyKeyInput.Password = SecretStorage.Unprotect(state.HedyProtectedKey);
        RememberHedyKey.IsChecked = HedyKeyInput.Password.Length > 0;
        if (HedyKeyInput.Password.Length == 0) HedyKeyInput.Password = Environment.GetEnvironmentVariable("HEDY_API_KEY") ?? "";
        HedyTranscriptBox.Text = state.Session?.HedyTranscript ?? "";
        hedyTimer.Tick += async (_, _) => await SyncHedy();
    }
    private void PauseHedySync()
    {
        hedyTimer.Stop(); hedyCancellation.Cancel(); hedyCancellation.Dispose(); hedyCancellation = new();
        hedyGeneration++; hedyBusy = false;
        HedyStatus.Text = "Live sync paused. Link a Hedy session to this timer.";
        HedyTranscriptBox.Clear();
    }
    private void StopHedy()
    {
        PauseHedySync(); hedy?.Dispose(); hedy = null;
    }
    private async void ConnectHedyClick(object sender, RoutedEventArgs e)
    {
        StopHedy();
        var key = HedyKeyInput.Password.Trim();
        if (key.Length == 0) { HedyStatus.Text = "Enter your Hedy API key here, or set HEDY_API_KEY before launching the app."; return; }
        var generation = hedyGeneration;
        HedySessionPicker.ItemsSource = null;
        HedyConnectButton.IsEnabled = false;
        HedyStatus.Text = "Connecting to Hedy…";
        try
        {
            state.HedyRegion = HedyRegionPicker.SelectedIndex == 1 ? "EU" : "US";
            state.HedyProtectedKey = RememberHedyKey.IsChecked == true ? SecretStorage.Protect(key) : "";
            Changed();
            hedy = new HedyClient(key, state.HedyRegion);
            var sessions = await hedy.Sessions(hedyCancellation.Token);
            if (generation != hedyGeneration) return;
            HedySessionPicker.ItemsSource = sessions;
            HedySessionPicker.SelectedItem = sessions.FirstOrDefault(s => s.Id == state.Session?.HedySessionId) ?? sessions.FirstOrDefault();
            HedyStatus.Text = sessions.Count == 0 ? "Connected; no synced Hedy sessions found." : "Connected. Select a session, then link live notes.";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or CryptographicException or FormatException)
        {
            if (generation == hedyGeneration) HedyStatus.Text = "Could not connect. Check your key, region, internet connection, and Hedy cloud sync.";
        }
        finally { HedyConnectButton.IsEnabled = true; }
    }
    private async void LinkHedyClick(object sender, RoutedEventArgs e)
    {
        if (hedy == null || HedySessionPicker.SelectedItem is not HedySessionInfo selected) { HedyStatus.Text = "Connect and select a Hedy session first."; return; }
        if (state.Session is not { } session || session.StoppedAt != null || session.End <= DateTimeOffset.Now) { HedyStatus.Text = "Start or schedule a dashboard session first."; return; }
        PauseHedySync();
        var generation = hedyGeneration;
        if (session.HedySessionId != selected.Id) { session.HedyTranscript = ""; session.HedyBaseline = ""; }
        session.HedySessionId = selected.Id;
        session.HedySourceTitle = selected.Title;
        Changed(); hedyFailures = 0; hedyTimer.Interval = TimeSpan.FromSeconds(15);
        await SyncHedy();
        if (generation == hedyGeneration && hedy != null && state.Session == session) hedyTimer.Start();
    }
    private async Task SyncHedy()
    {
        if (hedyBusy || hedy == null || state.Session is not { } session || string.IsNullOrEmpty(session.HedySessionId)) return;
        if (session.StoppedAt != null || DateTimeOffset.Now >= session.End) { hedyTimer.Stop(); HedyStatus.Text = "Session ended. Saved interval notes are available."; return; }
        hedyBusy = true;
        var generation = hedyGeneration;
        try
        {
            var transcript = await hedy.Transcript(session.HedySessionId, hedyCancellation.Token);
            if (generation != hedyGeneration || state.Session != session) return;
            hedyFailures = 0; hedyTimer.Interval = TimeSpan.FromSeconds(15);
            if (transcript.Length == 0) { HedyStatus.Text = "Connected; waiting for Hedy to sync transcript text."; return; }
            var previous = session.HedyTranscript;
            var result = HedyNotes.Apply(session, transcript, DateTimeOffset.Now);
            HedyTranscriptBox.Text = transcript;
            HedyStatus.Text = $"{DateTime.Now:t} · {result}";
            hedyFailures = 0; hedyTimer.Interval = TimeSpan.FromSeconds(15);
            if (previous != transcript) { BuildTimeline(); UpdateClock(); Changed(); SaveNow(); RefreshOpenHedyNotes(session); }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (generation != hedyGeneration) return;
            hedyFailures++;
            hedyTimer.Interval = TimeSpan.FromSeconds(Math.Min(300, 15 * Math.Pow(2, Math.Min(hedyFailures, 5))));
            HedyStatus.Text = $"Hedy unavailable. Notes are safe; retrying in {hedyTimer.Interval.TotalSeconds:0}s.";
        }
        finally { if (generation == hedyGeneration) hedyBusy = false; }
    }
    private void DisconnectHedyClick(object sender, RoutedEventArgs e)
    {
        StopHedy(); HedyStatus.Text = "Disconnected. Imported notes remain saved.";
    }
    private void OpenIntervalNotes(Session session, int first, int count)
    {
        if (noteWindows.TryGetValue((session, first), out var existing)) { existing.Activate(); return; }
        var editor = new NexaDialog { Title = count == 1 ? $"Nexa · Interval {first + 1} · Notes" : $"Nexa · Intervals {first + 1}–{first + count} · Notes", Width = 640, Height = 620, MinWidth = 400, MinHeight = 350, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brush("BackgroundBrush"), Foreground = Brush("TextBrush") };
        var panel = new StackPanel { Margin = new Thickness(22) };
        for (var i = first; i < first + count; i++)
        {
            var index = i;
            var start = session.Start.AddMinutes(i * session.IntervalMinutes);
            panel.Children.Add(new TextBlock { Text = $"Interval {i + 1} · {start.LocalDateTime:MMM d, h:mm tt}", FontSize = 17, Margin = new Thickness(0, 12, 0, 10) });
            var notes = new TextBox { Text = session.IntervalNotes.GetValueOrDefault(i) ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 130, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            System.Windows.Automation.AutomationProperties.SetName(notes, $"Your notes for interval {i + 1}");
            notes.TextChanged += (_, _) => { session.IntervalNotes[index] = notes.Text; Changed(); };
            panel.Children.Add(notes);
            panel.Children.Add(new TextBlock { Text = "HEDY · FILED BY RECEIPT TIME", FontSize = 11, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 16, 0, 8) });
            var live = new TextBox { Text = session.HedyIntervalNotes.GetValueOrDefault(i) ?? "No new Hedy text received during this interval.", IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 160, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Tag = index };
            panel.Children.Add(live);
        }
        var close = new Button { Content = "Done", Margin = new Thickness(0, 18, 0, 0) }; close.Click += (_, _) => editor.Close(); panel.Children.Add(close);
        editor.Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        editor.Closed += (_, _) => { noteWindows.Remove((session, first)); if (state.Session == session) { BuildTimeline(); UpdateClock(); } };
        noteWindows[(session, first)] = editor; editor.Show();
    }
    private void RefreshOpenHedyNotes(Session session)
    {
        foreach (var pair in noteWindows.Where(pair => pair.Key.Item1 == session))
        {
            var panel = (StackPanel)((ScrollViewer)((NexaDialog)pair.Value).Body!).Content;
            foreach (var text in panel.Children.OfType<TextBox>().Where(t => t.IsReadOnly && t.Tag is int))
                text.Text = session.HedyIntervalNotes.GetValueOrDefault((int)text.Tag) ?? "No new Hedy text received during this interval.";
        }
    }
}
