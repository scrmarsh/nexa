using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckAlpha011()
    {
        object? Call(object target, string method, params object?[] args) => target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
        Button FindButton(DependencyObject parent, string label) => Descendants(parent).OfType<Button>().Single(b => Equals(b.Content, label));
        void Tap(DependencyObject parent, string label) => FindButton(parent, label).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(App.AppVersion == "0.1.11" && App.VersionLabel == "Nexa 0.1.11 Alpha" && ReleaseNotes.All[0].Title.Contains("Pixel Timer Performance"), "Release version and history are current");
        foreach (var scale in new[] { 1d, 1.25, 1.5, 2d })
        {
            var grid = MainWindow.PixelMetrics(1000, 260, scale);
            Assert(Math.Abs(grid.Size * scale - Math.Round(grid.Size * scale)) < .001 && Math.Abs(grid.Gap * scale - Math.Round(grid.Gap * scale)) < .001, "Pixel size and spacing align to device pixels at " + scale);
            Assert(MainWindow.PixelMetrics(1001, 260, scale).Columns == grid.Columns, "Subcell width changes preserve grid columns at " + scale);
        }
        var store = new StateStore(Path.Combine(output, "alpha011.json"));
        var initial = new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Notes", "notes")], Notes = "Keep notes" }; store.Save(initial);
        var first = Continuity.Current(initial.Workspace!, store.UserId); first.Name = "Newest Space"; first.Current.LastAccessedAt = DateTimeOffset.Now;
        for (var i = 1; i < 10; i++)
        {
            var record = Continuity.NewDashboard("Space " + i, "Class", new DashboardData { Notes = "Saved " + i }, store.UserId);
            record.Current.LastAccessedAt = first.Current.LastAccessedAt.AddMinutes(-i); initial.Workspace!.Dashboards.Add(record);
        }
        initial.Workspace!.Dashboards.Add(Continuity.NewDashboard("Developer Space", "Developer mode", new DashboardData(), store.UserId));
        for (var i = 0; i < 3; i++)
        {
            var session = Continuity.Clone(first.Current); session.SessionId = Guid.NewGuid(); session.Data.Session = new Session { Title = "Archived " + i, Start = DateTimeOffset.Now.AddHours(-2), End = DateTimeOffset.Now.AddHours(-1) };
            first.History.Add(session);
        }
        store.Save(initial);
        var home = new HomeWindow(store) { Width = 1280, Height = 960 }; home.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(home.Title == "nexa" && !Descendants(home).OfType<TextBlock>().Any(t => t.Text == "Nexa"), "Home uses lowercase top-bar branding");
        var firstColumn = home.Recent.Children.OfType<StackPanel>().Single();
        Assert(firstColumn.Children.OfType<Button>().Count() == 4 && home.Recent.Children.OfType<Button>().Single().Content?.ToString() == ">", "Recent Spaces defaults to four cards in one column with a side chevron");
        Assert(!Descendants(home).OfType<Button>().Any(b => Equals(b.Content, "More")), "Recent Spaces has no More button");
        var originalTimes = initial.Workspace.Dashboards.Select(d => d.Current.LastAccessedAt).ToArray();
        Tap(home, ">"); Pump(TimeSpan.FromMilliseconds(90));
        var columns = home.Recent.Children.OfType<StackPanel>().ToArray();
        Assert(columns.Length == 3 && columns.Select(c => c.Children.OfType<Button>().Count()).SequenceEqual([4, 4, 2]), "Older Spaces expand horizontally in columns of up to four");
        Assert(columns.SelectMany(c => c.Children.OfType<Button>()).Select(b => (Guid)b.Tag).SequenceEqual(initial.Workspace.Dashboards.Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).Select(d => d.DashboardId)), "Expanded Space order remains newest to oldest");
        Assert(store.Load().Workspace!.Dashboards.Select(d => d.Current.LastAccessedAt).SequenceEqual(originalTimes), "Expanding Recent Spaces does not update last-used timestamps");
        Tap(home, "<"); Assert(home.Recent.Children.OfType<StackPanel>().Single().Children.OfType<Button>().Count() == 4, "Chevron collapses Recent Spaces");
        Call(home, "OpenSettings", "Session"); Pump(TimeSpan.FromMilliseconds(140)); var settings = home.OwnedWindows.OfType<SettingsDialog>().Single();
        var nav = (StackPanel)((Border)((Grid)settings.Body!).Children[0]).Child;
        Assert(nav.Children.OfType<Button>().Select(b => b.Content?.ToString()).SequenceEqual(["Appearance", "Session", "Widgets", "Profile", "Account & Data", "Integrations"]), "Settings categories follow the requested order without Developer");
        Assert(nav.Children.OfType<TextBlock>().Any(t => t.Text == "Experimental Features"), "Experimental Features separates secondary Settings sections");
        Assert(Descendants(settings).OfType<TextBlock>().Any(t => t.Text == "No active session") && !Descendants(settings).OfType<TextBox>().Any(t => t.Name == "SessionName"), "Home Session tab has a calm empty state without stale controls");
        var closeBar = Descendants(settings).OfType<Border>().Single(b => Equals(b.Tag, "PanelCloseBar"));
        Assert(Descendants(closeBar).OfType<TextBlock>().Any(t => t.Text == "Changes save automatically") && !Descendants(nav).OfType<TextBlock>().Any(t => t.Text.Contains("Changes save automatically")), "Autosave message stays in the fixed Settings footer");
        settings.Close(); home.Close();

        var loaded = store.Load(); var a = WidgetLibrary.Create("Notes", "notes"); var b = WidgetLibrary.Create("Clock"); var c = WidgetLibrary.Create("Tasks", "tasks");
        foreach (var widget in new[] { a, b, c }) { widget.GridColumns = 20; widget.Span = 10; widget.Height = 288; widget.CustomSize = true; }
        a.Column = 0; a.Row = 0; b.Column = 10; b.Row = 0; c.Column = 0; c.Row = 13;
        loaded.Widgets = [WidgetLibrary.Create("Pixel timer", "pixels"), a, b, c]; loaded.WidgetLayoutInitialized = true;
        loaded.Session = new Session { Title = "Class", Start = DateTimeOffset.Now.AddMinutes(-2), End = DateTimeOffset.Now.AddMinutes(25) };
        store.Save(loaded);
        var window = new MainWindow(store) { Width = 1280, Height = 950 }; window.Show(); Pump(TimeSpan.FromMilliseconds(230));
        Border Frame(WidgetInstance widget) => window.WidgetCanvas.Children.OfType<Border>().Single(f => Equals(f.Tag, widget.Id));
        Rect Bounds(WidgetInstance widget) { var frame = Frame(widget); return new Rect(Canvas.GetLeft(frame), Canvas.GetTop(frame), frame.Width, frame.Height); }
        var activeSession = window.state.Session!; var start = activeSession.Start; var end = activeSession.End;
        var originalPixels = window.PixelGrid.Children.OfType<Border>().ToArray(); var cellSize = originalPixels[0].Width;
        Assert(originalPixels.Length > 0 && originalPixels.All(p => Math.Abs(p.Width - cellSize) < .001 && Math.Abs(p.Height - cellSize) < .001), "Visible Pixel Timer cells keep a fixed square size");
        var firstRow = originalPixels.Where(p => Canvas.GetTop(p) == 0).Take(8).ToArray();
        Assert(firstRow.Skip(1).Select((p, i) => Canvas.GetLeft(p) - Canvas.GetLeft(firstRow[i])).Distinct().Count() == 1, "Pixel spacing is uniform without periodic gaps");
        var baseChanges = window.PixelLayoutChanges; var baseCreated = window.PixelVisualsCreated; var baseWidth = window.Width;
        for (var i = 0; i < 16; i++) { window.Width = baseWidth + 1; Pump(TimeSpan.FromMilliseconds(8)); window.Width = baseWidth; Pump(TimeSpan.FromMilliseconds(8)); }
        Pump(TimeSpan.FromMilliseconds(130));
        Assert(window.PixelLayoutChanges == baseChanges && window.PixelVisualsCreated == baseCreated, "Many subcell resize events cause no grid rebuild or pixel creation");
        window.Width = baseWidth + 240; Pump(TimeSpan.FromMilliseconds(180));
        Assert(window.PixelLayoutChanges == baseChanges + 1 && window.PixelVisualsCreated > baseCreated, "One settled width change adds only needed pixels");
        var peakCreated = window.PixelVisualsCreated; window.Width = baseWidth; Pump(TimeSpan.FromMilliseconds(180)); window.Width = baseWidth + 240; Pump(TimeSpan.FromMilliseconds(180));
        Assert(window.PixelVisualsCreated == peakCreated && window.PixelGrid.Children.Count > 0, "Growing again reuses pixel visuals without unbounded creation");
        var stamp = DateTimeOffset.Now; window.UpdateClock(stamp);
        Assert(window.Countdown.Text == MainWindow.ClockText(activeSession.Remaining(stamp)) && activeSession.Start == start && activeSession.End == end, "Resizing preserves the exact running session timer");
        window.Width = baseWidth; Pump(TimeSpan.FromMilliseconds(180));
        var bounds = new Rect(window.Left, window.Top, window.Width, window.Height);
        window.ToggleFullscreen(); Pump(TimeSpan.FromMilliseconds(330)); window.ToggleFullscreen(); Pump(TimeSpan.FromMilliseconds(330));
        Assert(window.WindowState == WindowState.Normal && Math.Abs(window.Width - bounds.Width) < 2 && Math.Abs(window.Height - bounds.Height) < 2 && Math.Abs(window.Left - bounds.Left) < 2, "Windowed fullscreen roundtrip restores custom bounds");
        window.WindowState = WindowState.Maximized; Pump(TimeSpan.FromMilliseconds(80)); window.ToggleFullscreen(); Pump(TimeSpan.FromMilliseconds(300)); window.ToggleFullscreen(); Pump(TimeSpan.FromMilliseconds(300));
        Assert(window.WindowState == WindowState.Maximized, "Maximized fullscreen roundtrip restores maximized state");
        window.WindowState = WindowState.Normal; Pump(TimeSpan.FromMilliseconds(150));
        var cards = window.state.Widgets.Where(w => w.Kind is "Notes" or "Clock" or "Tasks").ToArray();
        Assert(cards.Length == 3 && Bounds(cards[0]).Y == Bounds(cards[1]).Y && Bounds(cards[2]).Y > Bounds(cards[0]).Y, "Fixture starts with two widgets above a third");
        var grip = Descendants(Frame(cards[2])).OfType<Thumb>().Single(); var beforeA = Bounds(cards[0]); var beforeB = Bounds(cards[1]); var from = Bounds(cards[2]);
        grip.RaiseEvent(new DragStartedEventArgs(0, 0)); grip.RaiseEvent(new DragDeltaEventArgs(window.WidgetCanvas.ActualWidth / 3 - from.X, -from.Y));
        var preview = (Border)typeof(MainWindow).GetField("placementPreview", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Assert(preview != null && preview.BorderThickness.Left == 2 && preview.Width < from.Width && Canvas.GetTop(preview) == beforeA.Top, "Blue insertion preview appears before mouse release");
        Assert(Bounds(cards[0]) == beforeA && Bounds(cards[1]) == beforeB, "Neighbor cards remain still during drag preview");
        grip.RaiseEvent(new DragCompletedEventArgs(0, 0, false)); Pump(TimeSpan.FromMilliseconds(260));
        Assert(cards.All(w => Bounds(w).Top == 0) && Bounds(cards[0]).Right <= Bounds(cards[2]).Left && Bounds(cards[2]).Right <= Bounds(cards[1]).Left, "Drop inserts the third widget between the existing row cards");
        Assert(cards.Sum(w => w.Span) <= 20 && cards.All(w => Bounds(w).Width >= WidgetLibrary.Definition(w.Kind).MinWidth - 1), "Three-across compression respects widget minimum widths");
        Assert(typeof(MainWindow).GetField("placementPreview", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window) == null, "Drop indicator disappears after release");
        var savedWidgets = JsonSerializer.Serialize(window.state.Widgets); window.Close();
        Assert(JsonSerializer.Serialize(store.Load().Widgets) == savedWidgets && store.Load().Session!.Start == start, "Inserted widget layout and session survive close and reload");

        var homeAgain = new HomeWindow(store) { Width = 1120, Height = 850 }; homeAgain.Show(); Pump(TimeSpan.FromMilliseconds(160));
        var spaceCount = store.Load().Workspace!.Dashboards.Count;
        var create = new DashboardCreation(homeAgain, store.UserId);
        Assert(create.IsPanel && create.Body is ScrollViewer && !Descendants(create).OfType<Border>().Any(b => Equals(b.Tag, "WindowCaption")), "Create Space uses a scrollable internal panel without old window controls");
        Assert(!Descendants(create).OfType<Button>().Any(b => b.Tag is DashboardTemplate t && t.Name == "Argo Pantry"), "Create Space omits Argo Pantry without removing existing data");
        create.NameInput.Text = "Unfinished space";
        create.Dispatcher.BeginInvoke(new Action(() =>
        {
            var footer = Descendants(create).OfType<Border>().Single(b => Equals(b.Tag, "PanelCloseBar"));
            var viewer = (ScrollViewer)create.Body!; viewer.ScrollToEnd(); Pump(TimeSpan.FromMilliseconds(60));
            Assert(Descendants(footer).OfType<Button>().Any(b => Equals(b.Content, "Back")) && Descendants(footer).OfType<Button>().Any(b => Equals(b.Content, "Close")), "Create Space keeps Back and Close in the fixed footer while content scrolls");
            Tap(footer, "Back");
        }), DispatcherPriority.ApplicationIdle);
        Assert(create.ShowDialog() != true && homeAgain.IsVisible && create.NameInput.Text == "Unfinished space", "Back returns to Home without creating a Space or losing entered text before dismissal");
        Assert(store.Load().Workspace!.Dashboards.Count == spaceCount, "Canceled Create Space preserves existing Spaces");

        Call(homeAgain, "OpenHistory"); Pump(TimeSpan.FromMilliseconds(120)); var history = homeAgain.OwnedWindows.OfType<NexaDialog>().Single();
        var clear = FindButton(history, "Clear History");
        Assert(ReferenceEquals(clear.Style, Application.Current.FindResource("DestructiveButton")), "History shows a distinct red Clear History action");
        void ChooseHistoryConfirmation(string action)
        {
            history.Dispatcher.BeginInvoke(new Action(() =>
            {
                var confirmation = history.OwnedWindows.OfType<NexaConfirmation>().Single();
                Assert(confirmation.IsPanel && Descendants(confirmation).OfType<TextBlock>().Any(t => t.Text == "Clear all history?"), "Clear History opens a themed Nexa confirmation panel");
                Tap(confirmation, action);
            }), DispatcherPriority.Background);
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
        ChooseHistoryConfirmation("Cancel");
        Assert(store.Load().Workspace!.Dashboards.Sum(d => d.History.Count) == 3, "Cancel preserves every archived session");
        ChooseHistoryConfirmation("Clear History");
        Assert(store.Load().Workspace!.Dashboards.Sum(d => d.History.Count) == 0 && store.Load().Workspace!.Dashboards.Count == spaceCount, "Explicit confirmation clears history only and preserves Spaces");
        Assert(Descendants(history).OfType<TextBox>().Any(t => t.IsReadOnly && t.Text == "No session history yet."), "History refreshes its empty state immediately");
        history.Close(); homeAgain.Close();

        var developerStore = new StateStore(Path.Combine(output, "alpha011-developer.json"));
        var developerState = new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Pixel timer", "pixels")],
            Session = new Session { Title = "Developer mode", Start = DateTimeOffset.Now.AddMinutes(-1), End = DateTimeOffset.Now.AddMinutes(20) } };
        developerStore.Save(developerState);
        Continuity.Current(developerState.Workspace!, developerStore.UserId).PresetOrigin = "Developer mode"; developerStore.Save(developerState);
        var developer = new MainWindow(developerStore) { Width = 1160, Height = 820 }; developer.Show(); Pump(TimeSpan.FromMilliseconds(200));
        Assert(developer.DeveloperSplashButton.IsVisible && developer.DeveloperSoundButton.IsVisible && developer.TopNavigation.Children.IndexOf(developer.DeveloperSoundButton) == developer.TopNavigation.Children.IndexOf(developer.DeveloperSplashButton) + 1, "Developer Play Sound sits beside Preview Splash");
        var normalBounds = new Rect(developer.Left, developer.Top, developer.Width, developer.Height);
        Call(developer, "ResetDeveloperConfirmed"); Pump(TimeSpan.FromMilliseconds(160));
        Assert(developer.WindowState == WindowState.Normal && Math.Abs(developer.Width - normalBounds.Width) < 2 && Math.Abs(developer.Height - normalBounds.Height) < 2 && Math.Abs(developer.Left - normalBounds.Left) < 2, "Reset Dev preserves custom size and position");
        developer.ToggleFullscreen(); Pump(TimeSpan.FromMilliseconds(320)); Call(developer, "ResetDeveloperConfirmed"); Pump(TimeSpan.FromMilliseconds(180));
        Assert(developer.WindowState == WindowState.Maximized && Equals(developer.FullscreenButton.ToolTip, "Exit fullscreen (Esc)"), "Reset Dev preserves fullscreen");
        developer.ToggleFullscreen(); developer.WindowState = WindowState.Maximized; Pump(TimeSpan.FromMilliseconds(120)); Call(developer, "ResetDeveloperConfirmed");
        Assert(developer.WindowState == WindowState.Maximized, "Reset Dev preserves maximize state");
        developer.WindowState = WindowState.Normal; developer.OpenMainSettings("Session"); Pump(TimeSpan.FromMilliseconds(120));
        var activeSettings = developer.OwnedWindows.OfType<SettingsDialog>().Single(); var sameSession = developer.state.Session;
        Assert(Descendants(activeSettings).OfType<TextBox>().Any(t => t.Name == "SessionName") && ReferenceEquals(sameSession, developer.state.Session), "Settings recognizes the live session without creating another");
        sameSession!.StoppedAt = DateTimeOffset.Now; Pump(TimeSpan.FromSeconds(1.2));
        Assert(Descendants(activeSettings).OfType<TextBlock>().Any(t => t.Text == "No active session") && !Descendants(activeSettings).OfType<TextBox>().Any(t => t.Name == "SessionName"), "Session tab becomes an empty state when the active session ends");
        activeSettings.Close(); developer.Close();
    }
}
