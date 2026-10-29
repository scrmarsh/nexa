using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SessionDashboard;

public partial class MainWindow
{
    private void BuildSessionTimeline(StackPanel panel, WidgetInstance widget)
    {
        panel.Children.Add(new TextBlock { Text = state.Session?.Title ?? "Your session notes", FontWeight = FontWeights.SemiBold });
        var summaryInterval=widget.SummaryIntervalMinutes??state.Session?.IntervalMinutes??10;
        var interval=new ComboBox{ItemsSource=new[]{5,10,15,20,30,summaryInterval}.Distinct().Order().ToArray(),SelectedItem=summaryInterval,ToolTip="Summary interval in minutes"};
        panel.Children.Add(new TextBlock{Text="Summary interval (minutes)",Margin=new Thickness(0,8,0,4)});panel.Children.Add(interval);
        interval.SelectionChanged+=(_,_)=>{if(interval.SelectedItem is int minutes){foreach(var timeline in state.Widgets.Where(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline"))timeline.SummaryIntervalMinutes=minutes;if(state.Session is {} session)session.IntervalMinutes=minutes;IntervalInput.Text=minutes.ToString();BuildTimeline();Changed();SaveNow();}};
        var shortcuts = new WrapPanel { Margin = new Thickness(0, 8, 0, 10) };
        var recorder = new Button { Content = "●", Foreground=System.Windows.Media.Brushes.OrangeRed,Width=36,Height=36,Padding=new Thickness(0),ToolTip="Record · choose source", Margin = new Thickness(0, 0, 8, 5) };
        var sources=new ContextMenu();var local=new MenuItem{Header="Local Recording"};var remote=new MenuItem{Header="Hedy"};sources.Items.Add(local);sources.Items.Add(remote);recorder.ContextMenu=sources;
        var shortcutStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush") };
        void OpenTool(string kind)
        {
            if (kind == "Hedy Notes" && string.IsNullOrWhiteSpace(state.HedyProtectedKey))
            { shortcutStatus.Text = "Hedy Notes is not configured. Connect Hedy in Integrations first."; OpenMainSettings("Integrations"); return; }
            var target = state.Widgets.FirstOrDefault(w => w.Kind == kind) ?? AddWidget(kind);
            ArrangeWidgetFrames();
            if (widgetFrames.TryGetValue(target.Id, out var frame)) WidgetScroller.ScrollToVerticalOffset(Math.Max(0, Canvas.GetTop(frame) - 40));
            FocusWidget(target.Id);
        }
        local.Click+=(_,_)=>{OpenTool("Audio Recorder");StartLocalRecordingClick(recorder,new RoutedEventArgs());};
        remote.Click+=(_,_)=>{OpenTool("Hedy Notes");shortcutStatus.Text="Link notes in Hedy. Remote recording must be started explicitly in Hedy; nexa does not control the provider microphone.";};
        recorder.Click+=(_,_)=>{sources.PlacementTarget=recorder;sources.IsOpen=true;};
        shortcuts.Children.Add(recorder); panel.Children.Add(shortcuts); panel.Children.Add(shortcutStatus);
        // A standalone manual notes area also works before a timed session starts.
        var note = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 64, MaxLength = 100000, ToolTip = "Add a timeline note" };
        var add = new Button { Content = "Add note", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 10) };
        var entries = new StackPanel(); panel.Children.Add(note); panel.Children.Add(add); panel.Children.Add(ResizableTranscriptArea(panel,new ScrollViewer{Content=entries,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled},120));
        string? lastContent = null;
        void Refresh()
        {
            var session = state.Session;
            var rows = new List<string>();
            if (!string.IsNullOrWhiteSpace(widget.Text)) rows.Add(widget.Text);
            rows.AddRange(widget.Items.Select(i => i.Text));
            if (session != null)
            {
                rows.AddRange(session.IntervalNotes.OrderBy(p => p.Key).Select(p => $"Interval {p.Key + 1} · Manual\n{p.Value}"));
                rows.AddRange(session.HedyIntervalNotes.OrderBy(p => p.Key).Select(p => $"Interval {p.Key + 1} · Hedy\n{p.Value}"));
                rows.AddRange(session.IntervalSummaries.OrderBy(p => p.Key).Select(p => $"Interval {p.Key + 1} · Summary\n{p.Value}"));
                rows.AddRange(session.TimelineEntries.OrderBy(e => e.CreatedAt).Select(e => $"{NexaTime.Local(e.CreatedAt, Options.TimeZoneId):t} · {e.Source}\n{e.Text}"));
            }
            if (rows.Count == 0 && session != null) rows.Add(string.IsNullOrEmpty(session.HedySessionId) ? "Session notes\nAdd a timestamped note or open the recorder or Hedy Notes." : "Hedy notes\nWaiting for notes and interval summaries…");
            var content = string.Join("\n\n", rows); if (content == lastContent) return; lastContent = content; entries.Children.Clear();
            if (session != null)
            {
                var count=Math.Min(120,(int)Math.Ceiling((session.End-session.Start).TotalMinutes/session.IntervalMinutes));
                for(var i=0;i<count;i++)
                {
                    var start=session.Start.AddMinutes(i*session.IntervalMinutes);var end=start.AddMinutes(session.IntervalMinutes);if(end>session.End)end=session.End;
                    var summary=session.IntervalSummaries.GetValueOrDefault(i);
                    var source=session.HedyIntervalNotes.GetValueOrDefault(i)??session.IntervalNotes.GetValueOrDefault(i);
                    var excerpt=summary??(string.IsNullOrWhiteSpace(source)?"No transcript available for this interval.":source.Length>160?source[..160]+"…":source);
                    var card=new StackPanel();card.Children.Add(new TextBlock{Text=$"{NexaTime.Clock(start,Options)}–{NexaTime.Clock(end,Options)}",FontWeight=FontWeights.SemiBold,FontSize=12});
                    card.Children.Add(new TextBlock{Text=excerpt,TextWrapping=TextWrapping.Wrap,FontSize=12,Margin=new Thickness(0,5,0,5)});
                    var detail=(source??"No transcript available for this interval.")+"\n\n"+(string.IsNullOrWhiteSpace(source)?"Local audio transcription is not connected.":"Source excerpt; no connected abstractive summarization provider.");
                    card.Children.Add(new Expander{Header=string.IsNullOrWhiteSpace(source)?"No transcript · Details":"Saved text · Expand",Content=new TextBlock{Text=detail,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(8)}});
                    var frame=new Border{Child=card,Padding=new Thickness(10),CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,8)};frame.SetResourceReference(Border.BorderBrushProperty,"LineBrush");entries.Children.Add(frame);

                }
            }
            foreach (var row in rows)
            {
                var split = row.IndexOf('\n');
                var heading = split < 0 ? "Session note" : row[..split];
                var body = split < 0 ? row : row[(split + 1)..];
                entries.Children.Add(new Expander { Header = heading, IsExpanded = rows.Count <= 3,
                    Content = new TextBox { Text = body, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, BorderThickness = new Thickness(0), Margin = new Thickness(0,8,0,12), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 400 } });
            }
        }
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(note.Text)) return;
            if (state.Session is { } session) session.TimelineEntries.Add(new() { Text = note.Text.Trim() });
            else widget.Items.Add(new() { Text = note.Text.Trim() });
            note.Clear(); Changed(); SaveNow(); Refresh(); ArrangeWidgetFrames();
        };
        widgetTicks.Add(_ => Refresh()); Refresh();
    }
    private void BuildHedyNotes(StackPanel panel, WidgetInstance widget)
    {
        // Credentials stay exclusively in Settings / Integrations. Shared live controls have one owner.
        var connect = new Button { Content = "Connect Hedy", HorizontalAlignment = HorizontalAlignment.Left };
        connect.Click += ConnectHedyClick; panel.Children.Add(connect);
        if (state.Widgets.FirstOrDefault(w => w.Kind == "Hedy Notes")?.Id == widget.Id)
        {
            Detach(HedyControls); HedyControls.Visibility = Visibility.Visible; panel.Children.Add(HedyControls);
            Detach(HedyStatus); panel.Children.Add(HedyStatus);
        }
        else
        {
            var settings = new Button { Content = "Hedy settings" }; settings.Click += HedySettingsClick; panel.Children.Add(settings);
        }
        if (!string.IsNullOrEmpty(widget.Text)) panel.Children.Add(new TextBlock { Text = widget.Text, TextWrapping = TextWrapping.Wrap });
        var transcript = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(ResizableTranscriptArea(panel,transcript,110)); widgetTicks.Add(_ => { var value = state.Session?.HedyTranscript ?? "Connect and link a Hedy session to see live notes."; if (transcript.Text != value) transcript.Text = value; });
    }
    private static Border ResizableTranscriptArea(StackPanel panel,FrameworkElement content,double minimum)
    {
        var host=new Border{Child=content,Height=minimum,Tag="ResizableTranscript",MinHeight=minimum};
        return host;
    }
    private void BuildRecorderWidget(StackPanel panel)
    {
        var controls = new WrapPanel();
        Button Add(string label, RoutedEventHandler handler) { var button = new Button { Content = label, Margin = new Thickness(0, 0, 6, 6) }; button.Click += (sender, e) => { handler(sender, e); UpdateRecordingStatus(); UpdateWidgetClocks(DateTimeOffset.Now); }; controls.Children.Add(button); return button; }
        var record = Add("Record", StartLocalRecordingClick); var pause = Add("Pause", PauseLocalRecordingClick); var stop = Add("Stop", StopLocalRecordingClick);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 8) };
        var elapsed = new TextBlock { FontSize = 26 };
        panel.Children.Add(controls); panel.Children.Add(elapsed); panel.Children.Add(status);
        panel.Children.Add(new TextBlock { Text = "Audio stays on this PC. Saved recordings appear in Session Timeline; transcription is not connected yet.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var open = Add("Open recording", (_, _) => { var path = state.Session?.LocalRecordingPath; if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path)) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true }); } catch (System.ComponentModel.Win32Exception) { RecordingStatus.Text = "Could not open the recording."; } } });
        widgetTicks.Add(_ => { record.IsEnabled = !recordingActive; pause.IsEnabled = stop.IsEnabled = recordingActive; pause.Content = recordingPaused ? "Resume" : "Pause"; open.IsEnabled = !string.IsNullOrEmpty(state.Session?.LocalRecordingPath); elapsed.Text = RecordingElapsed.Text; status.Text = RecordingStatus.Text.Length > 0 ? RecordingStatus.Text : "Ready · Start a session, then Record."; });
    }
    private void BuildCalculator(StackPanel panel, WidgetInstance widget)
    {
        var input = new TextBox { Text = widget.Text, MaxLength = 256, FontSize = 22, ToolTip = "Type a calculation; Enter to calculate" };
        var result = new TextBlock { FontSize = 20, Margin = new Thickness(0, 6, 0, 8), TextWrapping = TextWrapping.Wrap };
        var buttons = new UniformGrid { Columns = 4 }; var history = new StackPanel();
        panel.Children.Add(input); panel.Children.Add(result); panel.Children.Add(buttons); panel.Children.Add(new ScrollViewer { Content = history, Height = 72, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        void History() { history.Children.Clear(); foreach (var line in widget.CalculationHistory) history.Children.Add(new TextBlock { Text = line, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) }); }
        void Calculate()
        {
            try
            {
                var value = Calculator.Evaluate(input.Text).ToString("G29", CultureInfo.InvariantCulture); result.Text = value;
                widget.CalculationHistory.Insert(0, input.Text + " = " + value);
                if (widget.CalculationHistory.Count > 8) widget.CalculationHistory.RemoveRange(8, widget.CalculationHistory.Count - 8);
                History(); Changed(); SaveNow(); ArrangeWidgetFrames();
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or DivideByZeroException) { result.Text = ex is DivideByZeroException ? "Cannot divide by zero." : ex is OverflowException ? "Number is too large." : ex.Message; }
        }
        foreach (var label in new[] { "C", "⌫", "%", "/", "7", "8", "9", "*", "4", "5", "6", "-", "1", "2", "3", "+", "0", ".", "=", "Copy" })
        {
            var key = new Button { Content = label, Padding = new Thickness(6), Margin = new Thickness(2) };
            key.Click += (_, e) =>
            {
                e.Handled = true;
                if (label == "=") Calculate();
                else if (label == "C") { input.Clear(); result.Text = ""; }
                else if (label == "Copy") { if (result.Text.Length > 0) { try { Clipboard.SetText(result.Text); } catch (System.Runtime.InteropServices.COMException) { result.Text = "Clipboard is busy. Try again."; } } }
                else if (label == "⌫") { if (input.SelectionLength > 0) input.SelectedText = ""; else if (input.CaretIndex > 0) { var index = input.CaretIndex; input.Text = input.Text.Remove(index - 1, 1); input.CaretIndex = index - 1; } }
                else { var index = input.SelectionStart; input.SelectedText = label; input.CaretIndex = index + label.Length; }
                input.Focus();
            }; buttons.Children.Add(key);
        }
        input.TextChanged += (_, _) => { widget.Text = input.Text; Changed(); };
        input.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Calculate(); e.Handled = true; } else if (e.Key == Key.Escape) { input.Clear(); e.Handled = true; } };
        History();
    }
}
