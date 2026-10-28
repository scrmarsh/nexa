using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly string RecorderAlias = "NexaLocal_" + Guid.NewGuid().ToString("N");
    private readonly DispatcherTimer recordingTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTimeOffset recordingStarted;
    private double recordedBeforePause;
    private bool recordingActive, recordingPaused, microphoneConsentGranted;
    private Session? recordingSession;
    internal Func<string, int> RecordingCommand = Command;
    private string RecordingFolder => Path.Combine(Path.GetDirectoryName(store.FilePath)!, "Recordings", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(store.UserId)))[..16]);
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)] private static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr callback);

    private void InitializeLocalRecording()
    {
        NotesSourcePicker.SelectedIndex = state.Session?.NotesSource == "Local Recording" ? 1 : 0;
        recordingTimer.Tick += (_, _) => UpdateRecordingStatus(); UpdateRecordingSource();
    }
    private void NotesSourceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return; if (state.Session is { } session) { session.NotesSource = NotesSourcePicker.SelectedIndex == 1 ? "Local Recording" : "Hedy"; Changed(); }
        UpdateRecordingSource();
    }
    private void UpdateRecordingSource()
    {
        var local = NotesSourcePicker.SelectedIndex == 1; HedyControls.Visibility = Visibility.Visible; LocalRecordingControls.Visibility = local ? Visibility.Visible : Visibility.Collapsed;
        if (!recordingActive) RecordingStatus.Text = state.Session is { } session && !string.IsNullOrWhiteSpace(session.LocalRecordingPath) ? "Saved locally · " + Path.GetFileName(session.LocalRecordingPath) : "Ready · Start a session, then Record.";
    }
    private void StartLocalRecordingClick(object sender, RoutedEventArgs e)
    {
        if (state.Session is not {FinalizedAt:null} session) { RecordingStatus.Text = "Start or resume a session before recording."; return; }
        if (recordingPaused) { if (RecordingCommand($"record {RecorderAlias}") == 0) { recordingPaused = false; recordingStarted = DateTimeOffset.Now; recordingTimer.Start(); PauseRecordingButton.Content = "Pause"; RecordingStatus.Text = "Microphone active · Recording"; MicrophoneIndicator.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "AccentBrush"); } return; }
        if (recordingActive) return;
        if (!microphoneConsentGranted)
        {
            var dialog = Dialog("Nexa · Microphone access", 480, 300);
            var body = new StackPanel { Margin = new Thickness(24) };
            body.Children.Add(new TextBlock { Text = "Allow microphone recording?", FontSize = 22, FontWeight = FontWeights.SemiBold });
            body.Children.Add(new TextBlock { Text = "Nexa will record audio only after you allow it. The recording stays on this PC and is never uploaded. You can stop or delete it at any time.", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 12, 0, 22) });
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = "Cancel", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) }; cancel.Click += (_, _) => dialog.CloseWithFade(); actions.Children.Add(cancel);
            var allow = new Button { Content = "Allow microphone", MinWidth = 140, Style = (Style)FindResource("PrimaryButton") }; allow.Click += (_, _) => { microphoneConsentGranted = true; dialog.CloseWithFade(); BeginLocalRecording(); }; actions.Children.Add(allow);
            body.Children.Add(actions); dialog.Content = body; dialog.ShowDismissible(); return;
        }
        BeginLocalRecording();
    }
    private void BeginLocalRecording()
    {
        if (state.Session == null || recordingActive) return;
        RecordingCommand($"close {RecorderAlias}"); var open = RecordingCommand($"open new type waveaudio alias {RecorderAlias}"); if (open != 0) { RecordingStatus.Text = "Microphone could not be opened. Check Windows microphone permission."; return; }
        if (RecordingCommand($"record {RecorderAlias}") != 0) { RecordingCommand($"close {RecorderAlias}"); RecordingStatus.Text = "Recording could not start. Check Windows microphone permission."; return; }
        recordingSession = state.Session;
        recordingActive = true; recordingPaused = false; recordedBeforePause = 0; recordingStarted = DateTimeOffset.Now; recordingTimer.Start(); RecordButton.IsEnabled = false; PauseRecordingButton.IsEnabled = StopRecordingButton.IsEnabled = true; PauseRecordingButton.Content = "Pause"; RecordingStatus.Text = "Microphone active · Recording"; MicrophoneIndicator.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "AccentBrush");
    }
    private void PauseLocalRecordingClick(object sender, RoutedEventArgs e)
    {
        if (!recordingActive) return;
        if (!recordingPaused)
        {
            if (RecordingCommand($"pause {RecorderAlias}") != 0) return; recordedBeforePause += (DateTimeOffset.Now - recordingStarted).TotalSeconds; recordingPaused = true; recordingTimer.Stop(); PauseRecordingButton.Content = "Resume"; RecordingStatus.Text = "Recording paused"; MicrophoneIndicator.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "MutedBrush");
        }
        else StartLocalRecordingClick(sender, e);
    }
    private void StopLocalRecordingClick(object sender, RoutedEventArgs e) => StopLocalRecording(true);
    private bool StopLocalRecording(bool save)
    {
        if (!recordingActive) return true;
        if (RecordingCommand($"stop {RecorderAlias}") != 0) { RecordingStatus.Text = "Could not stop the recorder. Try again."; return false; }
        if (!recordingPaused) recordedBeforePause += (DateTimeOffset.Now - recordingStarted).TotalSeconds;
        recordingPaused = true; recordingTimer.Stop(); PauseRecordingButton.Content = "Resume";
        if (save && recordingSession is { } session)
        {
            try
            {
                Directory.CreateDirectory(RecordingFolder); var path = Path.Combine(RecordingFolder, Guid.NewGuid() + ".wav");
                if (RecordingCommand($"save {RecorderAlias} \"{path}\"") != 0) { RecordingStatus.Text = "Recording is held in memory. Could not save; try Stop again."; return false; }
                session.LocalRecordingPath = path; session.LocalRecordingSeconds = recordedBeforePause;
                session.TimelineEntries.Add(new() { Source = "recorder", AudioPath = path, Text = $"Local audio · {TimeSpan.FromSeconds(recordedBeforePause):hh\\:mm\\:ss} · {Path.GetFileName(path)}" });
                RecordingStatus.Text = "Saved locally · " + Path.GetFileName(path); Changed(); SaveNow();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { RecordingStatus.Text = "Recording is held in memory. Could not save; try Stop again."; return false; }
        }
        RecordingCommand($"close {RecorderAlias}"); recordingSession = null; recordingActive = recordingPaused = false; RecordButton.IsEnabled = true; PauseRecordingButton.IsEnabled = StopRecordingButton.IsEnabled = false; PauseRecordingButton.Content = "Pause"; MicrophoneIndicator.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "MutedBrush"); UpdateRecordingStatus(); return true;
    }
    private void DeleteLocalRecordingClick(object sender, RoutedEventArgs e)
    {
        if (recordingActive && !StopLocalRecording(false)) return; if (state.Session is not { } session) return;
        try { if (!string.IsNullOrWhiteSpace(session.LocalRecordingPath) && File.Exists(session.LocalRecordingPath)) { var path = Path.GetFullPath(session.LocalRecordingPath); if (!path.StartsWith(Path.GetFullPath(RecordingFolder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) { RecordingStatus.Text = "Manage this legacy recording in its original folder."; return; } File.Delete(path); } session.LocalRecordingPath = ""; session.LocalRecordingSeconds = 0; RecordingStatus.Text = "Recording deleted. Session notes are preserved."; Changed(); SaveNow(); BuildTimeline(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { RecordingStatus.Text = "The local recording could not be deleted: " + ex.Message; }
    }
    private void UpdateRecordingStatus()
    {
        var seconds = recordingActive ? recordedBeforePause + (!recordingPaused ? Math.Max(0, (DateTimeOffset.Now - recordingStarted).TotalSeconds) : 0) : state.Session?.LocalRecordingSeconds ?? 0; RecordingElapsed.Text = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }
    private static int Command(string command) => mciSendString(command, null, 0, IntPtr.Zero);
}
