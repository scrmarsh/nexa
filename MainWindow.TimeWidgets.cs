using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

public partial class MainWindow
{
    private readonly System.Windows.Threading.DispatcherTimer stopwatchRefresh = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly List<Action<DateTimeOffset>> stopwatchTicks = [];
    private void BuildTimeWidget(StackPanel panel, WidgetInstance widget, bool stopwatch)
    {
        var display = new TextBlock { FontFamily = new FontFamily("Consolas"), FontSize = 30, Margin = new Thickness(0, 8, 0, 12) };
        panel.Children.Add(display);
        AnalogClock? analog = null;
        if (stopwatch) analog = new AnalogClock { Height = 144, MinHeight = 120, Margin = new Thickness(0, 8, 0, 8), Visibility = widget.DisplayMode == "Compact" ? Visibility.Collapsed : Visibility.Visible };
        Panel controls = new WrapPanel();
        var laps = new StackPanel();
        var lapList = new ScrollViewer { Content = laps, MaxHeight = 96, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed, Margin = new Thickness(0,4,0,8) };
        System.Windows.Automation.AutomationProperties.SetName(lapList,"Stopwatch laps");
        static string LapText(double seconds)
        {
            var total = (long)Math.Max(0,seconds);
            return $"{total/3600:00}:{total/60%60:00}:{total%60:00}.{(long)(seconds*1000)%1000:000}";
        }
        void RefreshLaps()
        {
            laps.Children.Clear();
            for(var i=0;i<widget.StopwatchLaps.Count;i++)
            {
                var elapsed = widget.StopwatchLaps[i];
                var split = elapsed - (i == 0 ? 0 : widget.StopwatchLaps[i-1]);
                laps.Children.Add(new TextBlock { Text = $"Lap {i+1}   {LapText(split)}   ·   {LapText(elapsed)}", FontFamily = new FontFamily("Consolas"), FontSize = 11, Margin = new Thickness(0,2,0,2) });
            }
            lapList.Visibility = stopwatch && widget.StopwatchLaps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            lapList.ScrollToEnd();
        }
        Button Control(string name, Action action)
        {
            var button = new Button { Content = name, MinWidth = 64, Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 6, 6) };
            button.Click += (_, _) => { action(); Changed(); SaveNow(); Update(DateTimeOffset.Now); };
            controls.Children.Add(button); return button;
        }
        Button? start = null, pause = null, resume = null, lap = null;
        start = Control("Start", () =>
        {
            if (stopwatch) widget.StopwatchStartedAt = DateTimeOffset.Now;
            else { widget.TimerStart = DateTimeOffset.Now; widget.TimerEnd = widget.TimerStart.Value.AddSeconds(widget.DurationSeconds); widget.TimerStopped = null; }
        });
        pause = Control("Pause", () =>
        {
            var now = DateTimeOffset.Now;
            if (stopwatch && widget.StopwatchStartedAt is { } began) { widget.StopwatchElapsedSeconds += Math.Max(0, (now - began).TotalSeconds); widget.StopwatchStartedAt = null; }
            else if (stopwatch) widget.StopwatchStartedAt = now;
            else if (!stopwatch && widget.TimerStart != null && widget.TimerStopped == null) widget.TimerStopped = now;
        });
        if (!stopwatch) resume = Control("Resume", () =>
        {
            if (stopwatch) widget.StopwatchStartedAt = DateTimeOffset.Now;
            else if (widget.TimerStopped is { } stopped) { var paused = DateTimeOffset.Now - stopped; widget.TimerStart += paused; widget.TimerEnd += paused; widget.TimerStopped = null; }
        });
        if (stopwatch) lap = Control("Lap", () =>
        {
            if(widget.StopwatchStartedAt is not { } at || widget.StopwatchLaps.Count >= 10000) return;
            widget.StopwatchLaps.Add(widget.StopwatchElapsedSeconds + Math.Max(0,(DateTimeOffset.Now-at).TotalSeconds));
            RefreshLaps(); ArrangeWidgetFrames();
        });
        Control("Reset", () => { if (stopwatch) { widget.StopwatchElapsedSeconds = 0; widget.StopwatchStartedAt = null; widget.StopwatchLaps.Clear(); RefreshLaps(); ArrangeWidgetFrames(); } else widget.TimerStart = widget.TimerEnd = widget.TimerStopped = null; });
        panel.Children.Add(controls);
        if (stopwatch) { panel.Children.Add(lapList); RefreshLaps(); panel.Children.Add(analog!); AddDisplayModeControls(panel, widget); }
        else AddDisplayModeControls(panel, widget);
        if (!stopwatch)
        {
            var duration = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            TextBox Part(string caption, int value)
            {
                var field = new StackPanel { Margin = new Thickness(0, 0, 6, 6) };
                field.Children.Add(new TextBlock { Text = caption, FontSize = 11 });
                var box = new TextBox { Text = value.ToString(), Width = 46, Padding = new Thickness(4), MaxLength = 3 };
                System.Windows.Automation.AutomationProperties.SetName(box, "Timer " + caption);
                field.Children.Add(box); duration.Children.Add(field); return box;
            }
            var hours = Part("Hours", widget.DurationSeconds / 3600);
            var minutes = Part("Minutes", widget.DurationSeconds / 60 % 60);
            var seconds = Part("Seconds", widget.DurationSeconds % 60);
            var apply = new Button { Content = "Set", Padding = new Thickness(8, 6, 8, 6), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 6) };
            var error = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
            apply.Click += (_, _) =>
            {
                if (!int.TryParse(hours.Text, out var h) || h is < 0 or > 168 || !int.TryParse(minutes.Text, out var m) || m is < 0 or > 59 || !int.TryParse(seconds.Text, out var s) || s is < 0 or > 59 || h * 3600 + m * 60 + s is < 1 or > 604800)
                { error.Text = "Enter 1 second to 7 days; minutes and seconds use 0–59."; return; }
                widget.TimerDurationSeconds = h * 3600 + m * 60 + s;
                widget.TimerStart = widget.TimerEnd = widget.TimerStopped = null;
                error.Text = ""; Changed(); SaveNow(); Update(DateTimeOffset.Now);
            };
            duration.Children.Add(apply); duration.Visibility = widget.DisplayMode == "Compact" ? Visibility.Collapsed : Visibility.Visible;
            panel.Children.Add(duration); panel.Children.Add(error);
            if (widget.DisplayMode == "Compact")
            {
                var editDuration = new Button { Content = "Change duration", Padding = new Thickness(6, 3, 6, 3), HorizontalAlignment = HorizontalAlignment.Left };
                editDuration.Click += (_, _) => { duration.Visibility = Visibility.Visible; editDuration.Visibility = Visibility.Collapsed; };
                panel.Children.Add(editDuration);
            }
        }
        void Update(DateTimeOffset now)
        {
            var running = stopwatch ? widget.StopwatchStartedAt != null : widget.TimerStart != null && widget.TimerStopped == null && widget.TimerEnd > now;
            var begun = stopwatch ? widget.StopwatchStartedAt != null || widget.StopwatchElapsedSeconds > 0 : widget.TimerStart != null;
            var paused = stopwatch ? !running && begun : widget.TimerStopped != null;
            double value;
            if (stopwatch) value = widget.StopwatchElapsedSeconds + (widget.StopwatchStartedAt is { } at ? Math.Max(0, (now - at).TotalSeconds) : 0);
            else value = widget.TimerStart == null || widget.TimerEnd == null ? widget.DurationSeconds : widget.ShowElapsed ? ((widget.TimerStopped ?? now) - widget.TimerStart.Value).TotalSeconds : (widget.TimerEnd.Value - (widget.TimerStopped ?? now)).TotalSeconds;
            var total = (long)Math.Max(0, stopwatch || widget.ShowElapsed ? Math.Floor(value) : Math.Ceiling(value));
            display.Text = total >= 3600 ? $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
            if (stopwatch) { display.Text += "." + ((long)(value * 1000) % 1000).ToString("000"); analog!.SetElapsed(value); }
            if (!stopwatch && !widget.ShowSeconds) display.Text = $"{total / 3600:00}:{total / 60 % 60:00}";
            start!.IsEnabled = !begun; pause!.IsEnabled = stopwatch ? begun : running;
            if (stopwatch) { pause.Content = paused ? "Resume" : "Pause"; lap!.IsEnabled = running && widget.StopwatchLaps.Count < 10000; }
            else resume!.IsEnabled = paused && (value > 0 || widget.ShowElapsed);
        }
        widgetTicks.Add(Update);
        if (stopwatch) stopwatchTicks.Add(now => { if (widget.StopwatchStartedAt != null) Update(now); });
        Update(DateTimeOffset.Now);
    }
}
