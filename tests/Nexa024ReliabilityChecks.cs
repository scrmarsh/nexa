using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SessionDashboard;

internal static partial class Program
{
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeMinMax { public NativePoint Reserved, Size, Position, MinTrack, MaxTrack; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeMonitor { public int Size; public NativeRect Bounds, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, int message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(nint monitor, ref NativeMonitor info);
    private static void CheckMasterReliability()
    {
        var zone = "America/Chicago";
        var start = NexaTime.Instant(new DateTime(2030, 1, 7, 13, 0, 0), zone);
        var seed = new Session { Title = "Class", Start = start, End = start.AddMinutes(75), AutomaticScheduling = true, TimeZoneId = zone, ScheduledDays = [DayOfWeek.Monday, DayOfWeek.Wednesday] };
        WeeklySchedule.ChangeTimeZone(seed, "America/New_York");
        var next = WeeklySchedule.Next(seed, seed.End)!;
        Assert(seed.Start == start && seed.End == start.AddMinutes(75) && seed.TimeZoneId == zone, "Changing recurrence zone preserves factual current-session instants and recorded zone");
        Assert(NexaTime.Local(next.Start, "America/New_York").Hour == 13 && next.Start == NexaTime.Instant(new DateTime(2030, 1, 9, 13, 0, 0), "America/New_York"), "Future recurrence keeps the class wall clock in the newly selected zone");
        var pins = WidgetLibrary.Create("Scripture pins");
        pins.ScripturePins = [new() { Text = "Permanent verse", Permanent = true }, new() { Text = "Session verse" }];
        var tasks = WidgetLibrary.Create("Task List"); tasks.Items = [new() { Text = "Keep task", Permanent = true }, new() { Text = "Clear task" }];
        var calendar = WidgetLibrary.Create("Calendar");
        var now = start;
        Assert(WidgetActivity.Capture(calendar, "Google Calendar", "Classes", [new() { Title = "Chemistry", Status = "Scheduled", Start = start, End = start.AddMinutes(75) }], now), "A provider snapshot captures meaningful observed calendar state");
        Assert(!WidgetActivity.Capture(calendar, "Google Calendar", "Classes", [new() { Title = "Chemistry", Status = "Scheduled", Start = start, End = start.AddMinutes(75) }], now.AddMinutes(1)), "Unchanged provider refreshes do not duplicate historical snapshots");
        var data = new DashboardData { Widgets = [pins, tasks, calendar] };
        var reset = WeeklySchedule.ResetSessionData(data);
        Assert(reset.Widgets[0].ScripturePins.Single().Permanent && reset.Widgets[1].Items.Count==2 && reset.Widgets[1].Items.Any(t=>t.Permanent), "Session reset keeps individual permanent Scripture pins and tasks");
        Assert(reset.Widgets[2].IntegrationSnapshots.Count == 0 && data.Widgets[2].IntegrationSnapshots.Count == 1, "Session reset clears live integration history only after preserving the original record");
        Assert(Continuity.ConfigurationOnly(data).Widgets[2].IntegrationSnapshots.Count == 0, "Configuration-only export strips private provider snapshots");
        for (var i = 0; i < 20; i++) WidgetActivity.Capture(calendar, "Google Calendar", "Classes", [new() { Title = "Class " + i }], now.AddMinutes(i + 2));
        Assert(calendar.IntegrationSnapshots.Count == 12, "Provider snapshots remain bounded to twelve changed observations per widget");
        var deck = WidgetLibrary.Create("Flashcards"); deck.FlashcardDecks = [new() { Name = "Biology", Cards = [new() { Front = "Cell", Back = "Basic unit of life" }] }];
        var before = new DashboardData { Widgets = [deck] }; var after = Continuity.Clone(before);
        after.Widgets[0].FlashcardDecks[0].Cards[0].StudyState = "Know it";
        ProductivityScoring.Observe(before, after, now);
        Assert(after.Productivity.Events.Single().Category == "Study" && after.Productivity.Score == 0, "Study results enter meaningful history without farming productivity points");
        ProductivityScoring.Observe(before, after, now.AddMinutes(1));
        Assert(after.Productivity.Events.Count == 1, "Repeated observations do not duplicate a study result");
        var report = MainWindow.PreviewSession(new DashboardSession { TimeZoneId = zone, Data = data, UpdatedAt = now });
        Assert(report.Contains("Permanent") && report.Contains("Google Calendar") && report.Contains("Class 19"), "Readable history includes pin scope and observed provider contents");

        var store = new StateStore(Path.Combine(output, "reliability-extensions.json"));
        var meter = WidgetLibrary.Create("Productivity Meter"); meter.HiddenProductivityCategories = ["Counter"];
        store.Save(new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), meter] });
        Assert(store.Load().Widgets.Single(w => w.Id == meter.Id).HiddenProductivityCategories.SequenceEqual(new[] { "Counter" }), "Productivity statistic selections survive restart");
        var window = new MainWindow(store) { Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
        window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var hwnd = new WindowInteropHelper(window).Handle;
        var monitor = new NativeMonitor { Size = Marshal.SizeOf<NativeMonitor>() };
        Assert(GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref monitor), "Native monitor work area is available for window-bound verification");
        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMinMax>());
        try
        {
            Marshal.StructureToPtr(new NativeMinMax(), pointer, false); SendMessage(hwnd, 0x24, nint.Zero, pointer);
            var bounds = Marshal.PtrToStructure<NativeMinMax>(pointer);
            Assert(bounds.Size.X == monitor.Work.Right - monitor.Work.Left && bounds.Size.Y == monitor.Work.Bottom - monitor.Work.Top, "Maximized Nexa uses the monitor work area above the taskbar");
        }
        finally { Marshal.FreeHGlobal(pointer); }
        var grid = (Grid)window.Content;
        using (var notices = new NexaNotifications(grid))
        {
            var existingText = Descendants(grid).OfType<TextBlock>().ToHashSet();
            notices.Show("probe-save", NexaNotifications.SavingMessage, "🔄", seconds: null);
            var probeSaving = Descendants(grid).OfType<TextBlock>().Single(t => !existingText.Contains(t) && t.Text == NexaNotifications.SavingMessage);
            notices.Show("probe-save", "Autosave complete", "✓", seconds: 2);
            var messages = Descendants(grid).OfType<TextBlock>().Count(t => t.Text == "Autosave complete");
            Assert(messages >= 1 && !Descendants(grid).OfType<TextBlock>().Contains(probeSaving), "A notification key transitions without retaining the old save state");
            notices.Show("probe-error", "Autosave failed — retrying…", "⚠", 3, null);
            Pump(TimeSpan.FromMilliseconds(2100));
            Assert(Descendants(grid).OfType<TextBlock>().Any(t => t.Text == "Autosave failed — retrying…"), "An unresolved save error remains visible after routine success expires");
            var card = Descendants(grid).OfType<TextBlock>().First(t => t.Text == "Autosave failed — retrying…");
            Assert(!card.Focusable, "Save feedback does not take keyboard focus");
            notices.Dismiss("probe-error");
            Assert(!Descendants(grid).OfType<TextBlock>().Any(t => t.Text == "Autosave failed — retrying…"), "Error acknowledgement dismisses the warning");
        }
        window.state.Workspace!.Settings.ShowQuickTips = false; window.ApplyReliabilityOptions(); window.AdvanceDashboardTip();
        Assert(!Descendants(window).OfType<TextBlock>().Any(t => DashboardTips.Messages.Contains(t.Text) && t.IsVisible), "Disabling tips immediately suppresses automatic tip notifications");
        Snapshot(window, "notification-work-area", 1220, 880);
        window.Close(); Pump(TimeSpan.FromMilliseconds(60));
    }
}
