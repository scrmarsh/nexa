using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckMasterCore()
    {
        var zone = "America/Chicago";
        DateTimeOffset At(int year, int month, int day, int hour, int minute = 0) => NexaTime.Instant(new DateTime(year, month, day, hour, minute, 0), zone);
        var monday = At(2030, 1, 7, 13);
        var schedule = PixelTiming.WeeklyClass("Biology", new[] { DayOfWeek.Monday, DayOfWeek.Wednesday }, TimeSpan.FromHours(13), TimeSpan.FromMinutes(855), zone, monday.AddHours(-1));
        Assert(schedule.Start == monday && schedule.End == monday.AddMinutes(75), "Weekly editor selects the upcoming Monday class");
        var before = PixelTiming.Read(schedule, monday.AddMinutes(-30));
        Assert(before.Time == TimeSpan.FromMinutes(30) && !before.CountUp && before.RemainingFraction == .5, "Before-class countdown uses its saved cycle baseline");
        var start = PixelTiming.Read(schedule, monday);
        Assert(start.Time == TimeSpan.Zero && start.CountUp && start.VisualFraction == 0, "At class start the number counts up from zero and pixels start empty");
        var during = PixelTiming.Read(schedule, monday.AddMinutes(20));
        Assert(during.Time == TimeSpan.FromMinutes(20) && during.CountUp && Math.Abs(during.VisualFraction - 20d / 75) < 1e-10, "During class elapsed digits count up while pixels fill upward");
        var between = PixelTiming.Read(schedule, schedule.End);
        Assert(between.Time == TimeSpan.FromHours(46.75) && !between.CountUp && between.RemainingFraction == 1, "Monday completion begins the full 46h45 next-class countdown");
        var late = PixelTiming.Read(schedule, At(2030, 1, 9, 12, 59));
        Assert(late.Time == TimeSpan.FromMinutes(1) && late.RemainingFraction < .001, "Between-class pixels reach the final contiguous prefix before Wednesday");
        var missedStart = PixelTiming.Read(schedule, NexaTime.Instant(new DateTime(2030, 1, 9, 13, 20, 0), zone));
        Assert(missedStart.CountUp && missedStart.Time == TimeSpan.FromMinutes(20) && Math.Abs(missedStart.SessionProgress - 20d / 75) < 1e-10, "Timer reading reconciles a missed class start even while a reset is blocked");
        var wednesday = PixelTiming.WeeklyClass("Biology", schedule.ScheduledDays, TimeSpan.FromHours(13), TimeSpan.FromMinutes(855), zone, At(2030, 1, 9, 13, 10));
        Assert(wednesday.Start == At(2030, 1, 9, 13), "Opening a schedule mid-class selects the current occurrence");
        Assert(PixelTiming.Read(wednesday, wednesday.End).Time == TimeSpan.FromHours(118.75), "Wednesday completion counts down 118h45 to Monday");
        var overnight = PixelTiming.WeeklyClass("Night lab", new[] { DayOfWeek.Monday }, TimeSpan.FromHours(23), TimeSpan.FromHours(1), zone, At(2030, 1, 8, 0, 20));
        Assert(overnight.Start == At(2030, 1, 7, 23) && overnight.End == At(2030, 1, 8, 1), "An overnight class remains current after midnight");
        var spring = PixelTiming.WeeklyClass("Sunday lab", new[] { DayOfWeek.Sunday }, TimeSpan.FromHours(1), TimeSpan.FromHours(4), zone, At(2026, 3, 8, 0));
        Assert(spring.End - spring.Start == TimeSpan.FromHours(2), "Spring DST class duration uses actual UTC instants");
        var autumn = PixelTiming.WeeklyClass("Sunday lab", new[] { DayOfWeek.Sunday }, TimeSpan.FromHours(0), TimeSpan.FromHours(3), zone, At(2026, 11, 1, 0));
        Assert(autumn.End - autumn.Start == TimeSpan.FromHours(4), "Fall DST class duration includes the repeated hour once");
        Throws<ArgumentException>(() => PixelTiming.WeeklyClass("Class", Array.Empty<DayOfWeek>(), TimeSpan.FromHours(1), TimeSpan.FromHours(2), zone, monday), "Empty weekday selection cannot overwrite a valid schedule");
        Throws<ArgumentException>(() => PixelTiming.WeeklyClass("Class", schedule.ScheduledDays, TimeSpan.FromHours(1), TimeSpan.FromHours(1), zone, monday), "Equal class start and end times are rejected");

        var store = new StateStore(Path.Combine(output, "weekly-editor.json"));
        var notes = WidgetLibrary.Create("Notes"); notes.Text = "Keep these class notes"; notes.PlacementY = 960;
        var state = new AppState { WidgetLayoutInitialized = true, Widgets = new() { WidgetLibrary.Create("Pixel timer", "pixels"), notes, WidgetLibrary.Create("Productivity Meter"), WidgetLibrary.Create("Productivity Score") }, Notes = "Current report notes" };
        store.Save(state); var id = Continuity.Current(state.Workspace!, store.UserId).Current.SessionId;
        var window = new MainWindow(store) { Width = 1450, Height = 950, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); Pump(TimeSpan.FromMilliseconds(220));
        Assert(window.ApplyClassSchedule(schedule, true), "Weekly schedule applies through a detached persisted workspace");
        Assert(Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == id && window.state.Notes == "Current report notes" && window.state.Widgets.Single(w => w.Id == notes.Id).Text == notes.Text, "Editing a weekly schedule preserves report identity and note content");
        var saved = store.Load(); var preset = saved.Presets.Single(p => p.Name == "Biology");
        Assert(preset.AutomaticScheduling && preset.ScheduledDays.SequenceEqual(schedule.ScheduledDays) && preset.StartClockMinutes == 780 && preset.EndClockMinutes == 855, "Weekdays, times and automatic recurrence persist on the reusable preset");
        Assert(saved.Session!.AutomaticScheduling && saved.Session.TimeZoneId == zone && saved.Widgets.Single(w => w.Id == notes.Id).PlacementY == 960, "Restart payload preserves recurrence, selected zone and widget coordinates");
        window.UpdateClock(monday.AddMinutes(20));
        Assert(window.Countdown.Text == "00:55:00" && window.CountdownCaption.Text == "TIME REMAINING" && window.ElapsedLabel.Text == "00:20:00", "The actual Pixel Timer counts down while its secondary timer shows elapsed time");
        var lit = window.PixelGrid.Children.OfType<Border>().Select(p => Equals(p.Background, window.FullContent.Resources["DashboardPixelBrush"])).ToArray();
        Assert(lit.TakeWhile(x => x).Count() == lit.Count(x => x) && lit.Any(x => x) && lit.Any(x => !x), "Rendered pixels remain a contiguous partially filled prefix");
        Assert(Descendants(window).OfType<ProductivityRing>().Count()==2 && !Descendants(window).OfType<TextBlock>().Any(t=>t.Text.Contains("Meaningful actions:") || t.Text.Contains("Next milestone:")), "Both legacy productivity placements render compact rings without default activity summaries");
        window.ShowWeeklySchedule(); Pump(TimeSpan.FromMilliseconds(60));
        var editor = Application.Current.Windows.OfType<Window>().Single(w => w is SettingsDialog{SessionInterface:true});
        Assert(Descendants(editor).OfType<Button>().Any(b => Equals(b.Content, "M/W")) && Descendants(editor).OfType<Button>().Any(b => Equals(b.Content, "Apply timer settings")), "The timer's real editor exposes weekday shortcuts and a save action");
        editor.Close();
        Assert(WidgetLibrary.Definitions.Count(d => d.Name == "Productivity") == 1 && WidgetLibrary.Definition("Productivity Meter").Name == "Productivity" && WidgetLibrary.Definition("Productivity Score").Name == "Productivity", "One combined Productivity entry accepts both legacy widget kinds");
        Snapshot(window, "weekly-productivity-dashboard", 1450, 950);
        window.Close(); Pump(TimeSpan.FromMilliseconds(80));
    }
}
