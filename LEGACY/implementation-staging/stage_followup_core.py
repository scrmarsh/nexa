from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(name,fn):
 p=r/name;p.write_text(fn(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'PixelTiming.followup.cs',r/'PixelTiming.cs')
shutil.copy2(s/'NexaSelection.cs',r/'NexaSelection.cs')
edit('Widgets.cs',lambda t:t.replace('public DateTimeOffset? TimerStart { get; set; }','public bool ProgressLinkedToSession { get; set; } = true;\n    public DateTimeOffset? TimerStart { get; set; }'))
edit('DarkControls.xaml',lambda t:t.replace('xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"','xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:local="clr-namespace:SessionDashboard"').replace('<Style TargetType="ComboBox">','<Style TargetType="ComboBox">\n        <Setter Property="local:NexaSelection.ControlledWheel" Value="True"/>'))
edit('NexaChrome.cs',lambda t:t.replace('node is ButtonBase or TextBoxBase','node is ComboBoxItem or ListBoxItem or ScrollViewer or ButtonBase or TextBoxBase'))
edit('MainWindow.xaml.cs',lambda t:t.replace('clock.Tick += (_, _) => UpdateClock();','clock.Interval=PixelTiming.UntilNextSecond(DateTimeOffset.UtcNow);\n        clock.Tick += (_, _) => { UpdateClock(); clock.Interval=PixelTiming.UntilNextSecond(DateTimeOffset.UtcNow); };').replace('''        ReconcileSchedule(at ?? DateTimeOffset.UtcNow);
        UpdateWidgetClocks(at ?? DateTimeOffset.UtcNow);
        var session = state.Session;
        var now = at ?? DateTimeOffset.Now;''','''        var now = at ?? DateTimeOffset.UtcNow;
        ReconcileSchedule(now);
        var session = state.Session;''').replace('var progress = reading.SessionProgress;','var progress = reading.VisualFraction;\n        UpdateWidgetClocks(now);').replace('var remainingFraction = reading.RemainingFraction;','var remainingFraction = reading.VisualFraction;').replace('var head = NexaTime.LitPixels(remainingFraction, pixels.Count);','var head = PixelTiming.CompletedPixels(reading, pixels.Count);').replace('DateTime.Today; EndDate.SelectedDate = DateTime.Today.AddDays(p.EndDayOffset);','NexaTime.Local(DateTimeOffset.UtcNow,Options.TimeZoneId).Date; EndDate.SelectedDate = StartDate.SelectedDate.Value.AddDays(p.EndDayOffset);'))
edit('MainWindow.Widgets.cs',lambda t:t.replace('var value = state.Session?.Progress(now) ?? 0; label.Text = NexaTime.Percentage(value); progress.Value = value;','var reading=PixelTiming.ForWidget(widget,state.Session,now); var value=reading.VisualFraction; label.Text=NexaTime.Percentage(value); label.ToolTip=reading.Caption; progress.Value=value;'))
edit('MainWindow.ClassSchedule.cs',lambda t:t.replace('Pixels always deplete in order.','Session pixels fill from the top-left; between classes they clear from the bottom-right.'))
edit('tests/Nexa024Runner.cs',lambda t:t.replace('settings.ShowDismissible();','settings.Owner=null;settings.ShowInTaskbar=true;settings.Show();'))
print('Updated authoritative timing, pixel direction, clock alignment and dropdown input routing.')
