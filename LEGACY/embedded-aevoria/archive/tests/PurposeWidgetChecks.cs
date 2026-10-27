using System.IO;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckPurposeWidgets()
    {
        var legacyTimer = WidgetLibrary.Create("Timer"); legacyTimer.Kind = legacyTimer.Title = "Pomodoro"; legacyTimer.Text = "Saved timer notes"; legacyTimer.TimerMinutes = 42;
        var legacyData = new DashboardData { Widgets = [legacyTimer], WidgetLayoutInitialized = true };
        WidgetLibrary.Validate(legacyData); WidgetLibrary.PrepareLayout(legacyData);
        Assert(legacyTimer.Kind == "Timer" && legacyTimer.Title == "Timer" && legacyTimer.TimerMinutes == 42 && legacyTimer.Text == "Saved timer notes", "Retired Pomodoro cards load and migrate without losing timer settings or notes");
        var store = new StateStore(Path.Combine(output, "purpose-widgets.json"));
        var data = store.Load();
        var clock = WidgetLibrary.Create("Clock"); clock.Span = 10; clock.Height = 288;
        var bible = WidgetLibrary.Create("Bible"); bible.IsMicro = true;
        var hedy = WidgetLibrary.Create("Hedy", "hedy"); hedy.Text = "Retain legacy widget notes";
        data.Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), clock, bible, WidgetLibrary.Create("Notes"), WidgetLibrary.Create("Counter"), WidgetLibrary.Create("Session timeline", "timeline"), hedy];
        data.WidgetLayoutInitialized = true;
        var start = DateTimeOffset.Now.AddMinutes(-10); var end = DateTimeOffset.Now.AddMinutes(50);
        data.Session = new Session { Title = "Existing session", Start = start, End = end, HedyTranscript = "Saved transcript", HedySessionId = "fixture-session", IntervalNotes = new() { [0] = "Retain interval notes" }, HedyIntervalNotes = new() { [0] = "Retain live notes" } };
        data.Draft = new() { Initialized = true, Title = data.Session.Title, StartDate = start.LocalDateTime.Date, EndDate = end.LocalDateTime.Date, StartMinutes = start.LocalDateTime.Hour * 60 + start.LocalDateTime.Minute, EndMinutes = end.LocalDateTime.Hour * 60 + end.LocalDateTime.Minute, Duration = "60", Interval = "10" };
        data.Notes = "Retain dashboard notes"; store.Save(data);
        var window = new MainWindow(store); window.Show(); Pump(TimeSpan.FromMilliseconds(350));
        Border Frame(string kind) => window.WidgetCanvas.Children.OfType<Border>().Single(b => Equals(b.Tag, window.state.Widgets.Single(w => w.Kind == kind).Id));
        Assert(Frame("Clock").Width < window.WidgetCanvas.ActualWidth / 3 && Frame("Clock").Height < 160, "Legacy clock defaults migrate to a compact card");
        Assert(Frame("Bible").Height >= 480 && !window.state.Widgets.Single(w => w.Kind == "Bible").IsMicro, "Bible stays expanded at a readable height");
        Assert(!Descendants(Frame("Bible")).OfType<Button>().Any(b => (b.ToolTip as string)?.Contains("Minimize widget") == true), "Bible has no minimize action");
        Assert(Frame("Counter").Width < Frame("Notes").Width && Frame("Counter").Height < Frame("Notes").Height, "Counters use smaller cards than notes");
        Assert(window.state.Widgets.All(w => w.Kind != "Hedy") && window.state.Widgets.Single(w => w.Kind == "Session timeline").Text.Contains("Retain legacy widget notes"), "Legacy Hedy widget merges into Session & Hedy without losing its notes");
        Assert(store.Load().Widgets.All(w => w.Kind != "Hedy"), "Widget migration is saved immediately on opening the dashboard");
        Assert(window.HedySessionPanel.IsVisible && !window.HedyKeyInput.IsVisible && !window.HedyRegionPicker.IsVisible, "Session widget shows Hedy tools while credentials stay out of the dashboard");
        Assert(!window.TopNavigation.Children.OfType<Button>().Any(b => Equals(b.Content, "Dashboard setup")), "Dashboard setup button is removed");
        Assert(window.state.Widgets.All(w => Descendants(Frame(w.Kind)).OfType<Button>().Any(b => Equals(b.ToolTip, "Configure"))) && !window.editingDashboard, "Every widget can be configured without editing mode");
        Assert(window.TitleInput.IsVisible && !window.StartDate.IsVisible && !window.EndTime.IsVisible && window.SessionEditButton.IsVisible && Descendants(Frame("Pixel timer")).Contains(window.Countdown), "Name stays in the top bar while countdown and EDIT belong to the pixel timer");
        Assert(window.SessionEditButton.Parent == window.CountdownCaption.Parent, "EDIT sits beside the countdown caption");
        Assert(!window.OverflowButton.IsVisible && !window.DashboardPicker.IsVisible && window.SessionSettingsButton.IsVisible, "Session replaces the overflow button and dashboard dropdown");
        var plus = window.WidgetCanvas.Children.OfType<Button>().Single(b => Equals(b.Tag, "AddWidgetShortcut"));
        Assert(plus.Content is Image && plus.Opacity < .4, "Adding widgets uses a faint asset icon on the canvas");
        Assert(!WidgetLibrary.Definitions.Any(d => d.Name == "Pomodoro") && !DashboardTemplates.All.Any(t => t.Widgets.Contains("Pomodoro")), "Pomodoro is removed from available widgets and templates");
        var inspectSession = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        var sessionShown = false;
        inspectSession.Tick += (_, _) =>
        {
            var dialog = window.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title.EndsWith("Session"));
            if (dialog == null) return;
            inspectSession.Stop();
            Assert(window.StartDate.IsVisible && !window.AccentColorPicker.IsVisible, "Timing page shows only schedule controls");
            RenderPreview((FrameworkElement)dialog.Content, "session-timing");
            Descendants(dialog).OfType<Button>().Single(b => Equals(b.Content, "Colors")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); dialog.UpdateLayout();
            Assert(window.AccentColorPicker.IsVisible && window.PixelColorPicker.IsVisible && !window.StartDate.IsVisible, "Colors has a dedicated page");
            RenderPreview((FrameworkElement)dialog.Content, "session-colors");
            var saveColor = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            saveColor.Tick += (_, _) =>
            {
                var picker = dialog.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title == "Choose a color"); if (picker == null) return;
                saveColor.Stop();
                var saveButton = Descendants(picker).OfType<Button>().Single(b => Equals(b.Content, "Save color"));
                var position = saveButton.TranslatePoint(new Point(0, 0), picker);
                Assert(saveButton.IsVisible && position.Y + saveButton.ActualHeight <= picker.ActualHeight, "Save color stays visible inside the picker");
                Descendants(picker).OfType<Button>().Single(b => Equals(b.ToolTip, "Mint")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                RenderPreview((FrameworkElement)picker.Content, "color-save"); saveButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };
            saveColor.Start(); window.AccentColorPicker.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(window.state.AccentColor == "#6AE58F" && store.Load().AccentColor == "#6AE58F", "Save color applies and persists the choice immediately");
            Assert(((System.Windows.Media.SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color.ToString() == "#FF5B7CFF" && ((System.Windows.Media.SolidColorBrush)window.FullContent.Resources["DashboardAccentBrush"]).Color.ToString() == "#FF6AE58F" && !window.FullContent.Resources.Contains("AccentBrush"), "Custom dashboard colors leave app menus in Nexa blue");
            Descendants(dialog).OfType<Button>().Single(b => Equals(b.Content, "Presets")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); dialog.UpdateLayout();
            sessionShown = window.PresetPicker.IsVisible && !window.AccentColorPicker.IsVisible;
            Descendants(dialog).OfType<Button>().Single(b => Equals(b.Content, "Timing")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); dialog.UpdateLayout();
            Assert(window.StartDate.IsVisible, "Session pages can be revisited without losing their controls"); dialog.Close();
        };
        inspectSession.Start(); window.SessionSettingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Pump(TimeSpan.FromMilliseconds(240));
        Assert(sessionShown && !window.AccentColorPicker.IsVisible && !window.PresetPicker.IsVisible, "Session contains colors and presets and hides them again after closing");
        var notesTitle = Descendants(Frame("Notes")).OfType<TextBox>().Single(b => (System.Windows.Automation.AutomationProperties.GetName(b) ?? "").StartsWith("Widget title"));
        notesTitle.Text = "Research notes";
        Assert(window.state.Widgets.Single(w => w.Kind == "Notes").Title == "Research notes", "Widget titles edit directly on screen");
        var currentSession = window.state.Session!;
        var identity = Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId;
        var historyCount = Continuity.Current(window.state.Workspace!, store.UserId).History.Count;
        window.TitleInput.Focus(); window.TitleInput.Text = "Typed session name";
        Assert(currentSession.Title == "Typed session name", "Typing in the session name updates the active session immediately");
        System.Windows.Input.Keyboard.ClearFocus();
        window.TitleInput.Text = "Updated session"; window.StartDate.SelectedDate = window.EndDate.SelectedDate = DateTime.Today.AddDays(1);
        window.StartTime.Text = "1:00 PM"; window.EndTime.Text = "2:00 PM";
        Assert(window.ApplyInlineSessionTimes(), "Inline time changes apply successfully");
        Assert(ReferenceEquals(currentSession, window.state.Session) && currentSession.Title == "Updated session" && currentSession.End - currentSession.Start == TimeSpan.FromHours(1), "Inline edits update the same session");
        Assert(Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == identity && Continuity.Current(window.state.Workspace!, store.UserId).History.Count == historyCount, "Editing session times preserves identity and does not create an archive");
        Assert(window.state.Notes == "Retain dashboard notes" && currentSession.HedyTranscript == "Saved transcript" && currentSession.HedyIntervalNotes[0] == "Retain live notes" && currentSession.IntervalNotes[0] == "Retain interval notes", "Inline edits preserve notes and Hedy transcripts");
        var validEnd = currentSession.End; window.EndTime.Text = "12:00 PM";
        Assert(!window.ApplyInlineSessionTimes() && currentSession.End == validEnd, "Invalid time edits leave the current session intact");
        window.EndTime.Text = "2:00 PM";
        var sawTimeEditor = false;
        var editTimes = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        editTimes.Tick += (_, _) =>
        {
            if (!window.StartDate.IsVisible || !window.EndTime.IsVisible) return;
            editTimes.Stop(); sawTimeEditor = true; window.EndTime.Text = "3:00 PM";
            Descendants(window.InlineSessionHost).OfType<Button>().Single(b => Equals(b.Content, "Apply times")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
        editTimes.Start(); Click(window, "Edit"); Pump(TimeSpan.FromMilliseconds(240));
        Assert(sawTimeEditor && !window.StartDate.IsVisible && currentSession.End - currentSession.Start == TimeSpan.FromHours(2), "EDIT opens time settings and Apply times updates the session then closes the editor");
        Assert(Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == identity && currentSession.HedyTranscript == "Saved transcript", "Editing through the dialog retains session identity and transcript");
        Assert(currentSession.ScheduledDays.Count > 0 && store.Load().Session!.ScheduledDays.SequenceEqual(currentSession.ScheduledDays), "Selected session days persist with the timer configuration");
        var notesBeforeReset = window.state.Notes; var widgetsBeforeReset = window.state.Widgets.Count; var historyBeforeReset = Continuity.Current(window.state.Workspace!, store.UserId).History.Count;
        var resetTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        resetTimer.Tick += (_, _) => { var resetDialog = window.OwnedWindows.OfType<Window>().FirstOrDefault(w => w.Title.Contains("Reset timer")); if (resetDialog == null) return; resetTimer.Stop(); Descendants(resetDialog).OfType<Button>().Single(b => Equals(b.Content, "Reset")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); };
        resetTimer.Start(); Click(window, "Reset"); Pump(TimeSpan.FromMilliseconds(240));
        Assert(Math.Abs((currentSession.End - currentSession.Start).TotalMinutes - currentSession.ConfiguredDurationMinutes) < .01 && window.state.Notes == notesBeforeReset && window.state.Widgets.Count == widgetsBeforeReset && Continuity.Current(window.state.Workspace!, store.UserId).History.Count == historyBeforeReset, "Reset restores the configured timer length without deleting dashboard data or history");
        var sawSettings = false;
        var closeSettings = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        closeSettings.Tick += (_, _) =>
        {
            var dialog = window.OwnedWindows.OfType<Window>().FirstOrDefault();
            if (dialog == null) return;
            sawSettings = dialog.Title.Contains("Settings") && window.HedyKeyInput.IsVisible;
            closeSettings.Stop(); dialog.Close();
        };
        closeSettings.Start(); Click(window, "Hedy settings"); Pump(TimeSpan.FromMilliseconds(220));
        Assert(sawSettings && !window.HedyKeyInput.IsVisible, "Hedy setup opens API settings and hides credentials again on close");
        var bibleReadyDeadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < bibleReadyDeadline && Descendants(Frame("Bible")).OfType<BibleReader>().Single().Chapter.Items.Count == 0) Pump(TimeSpan.FromMilliseconds(100));
        Assert(Descendants(Frame("Bible")).OfType<BibleReader>().Single().Chapter.Items.Count > 0, "Expanded Bible initializes its chapter selector after widget refresh");
        RenderPreview((FrameworkElement)window.Content, "purpose-widgets-inline-session"); window.SaveNow(); window.Close();
        var saved = store.Load();
        Assert(saved.Session!.Title == "Updated session" && saved.Session.HedyTranscript == "Saved transcript" && saved.Widgets.Single(w => w.Kind == "Notes").Title == "Research notes", "Inline session and widget edits survive reload");
    }
}
