using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckAlpha010()
    {
        object? Call(object target, string name, params object?[] args) => target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
        IEnumerable<DependencyObject> Visuals(DependencyObject root)
        {
            yield return root; if (root is not Visual) yield break;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Visuals(VisualTreeHelper.GetChild(root, i))) yield return child;
        }
        void Capture(Window w, string name)
        {
            Pump(TimeSpan.FromMilliseconds(240)); w.UpdateLayout(); var bmp = new RenderTargetBitmap((int)w.ActualWidth, (int)w.ActualHeight, 96, 96, PixelFormats.Pbgra32); bmp.Render(w);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bmp)); using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
        }
        void WaitClosed(Window w) { for (var i = 0; w.IsVisible && i < 30; i++) Pump(TimeSpan.FromMilliseconds(100)); Assert(!w.IsVisible, "Panel dismissal completes"); }
        void Tap(DependencyObject root, string label) => Descendants(root).OfType<Button>().Single(b => Equals(b.Content, label)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        KeyEventArgs Key(Window window, DependencyObject target, System.Windows.Input.Key key, RoutedEvent routed)
        {
            var e = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window)!, 0, key) { RoutedEvent = routed }; ((UIElement)target).RaiseEvent(e); return e;
        }
        void PanelChrome(NexaDialog panel, string name)
        {
            panel.UpdateLayout();
            Assert(panel.IsPanel && panel.WindowStyle == WindowStyle.None && !panel.ShowInTaskbar && !Descendants(panel).OfType<Border>().Any(b => Equals(b.Tag, "WindowCaption")), name + " uses internal panel chrome without a caption");
            Assert(!Descendants(panel).OfType<Button>().Any(b => new[] { "Minimize", "Maximize or restore" }.Contains(b.ToolTip?.ToString())), name + " has no window action buttons");
            var close = Descendants(panel).OfType<Button>().Single(b => Equals(b.Content, "Close")); var point = close.TranslatePoint(new Point(), panel);
            Assert(close.IsVisible && point.X < 30 && point.Y + close.ActualHeight >= panel.ActualHeight - 22, name + " has a persistent bottom-left Close button");
        }
        MouseButtonEventArgs Outside(MainWindow window)
        {
            var e = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Mouse.PreviewMouseDownEvent };
            window.WidgetCanvas.RaiseEvent(e);
            e.RoutedEvent = Mouse.MouseDownEvent; window.WidgetCanvas.RaiseEvent(e); return e;
        }
        Assert(App.AppVersion == "0.1.11" && App.VersionLabel == "Nexa 0.1.11 Alpha", "Shared version displays two-digit Alpha patch correctly");
        var store = new StateStore(Path.Combine(output, "navigation.json"));
        var initial = new AppState { WidgetLayoutInitialized = true, Widgets = [WidgetLibrary.Create("Notes", "notes")], Notes = "Keep my active notes", Session = new Session { Title = "Navigation class", Start = DateTimeOffset.Now.AddMinutes(-1), End = DateTimeOffset.Now.AddMinutes(30), IntervalMinutes = 5 } }; store.Save(initial);
        var first = Continuity.Current(initial.Workspace!, store.UserId); first.Name = "Space 0"; first.Current.LastAccessedAt = DateTimeOffset.Now;
        for (var i = 1; i < 7; i++) { var record = Continuity.NewDashboard("Space " + i, "Class", new DashboardData { Notes = "Other notes " + i }, store.UserId); record.Current.LastAccessedAt = first.Current.LastAccessedAt.AddHours(-i); initial.Workspace!.Dashboards.Add(record); }
        initial.Workspace!.Dashboards.Add(Continuity.NewDashboard("Hidden developer", "Developer mode", new DashboardData(), store.UserId));
        for (var i = 0; i < 12; i++) { var saved = Continuity.Clone(first.Current); saved.SessionId = Guid.NewGuid(); saved.Data.Session!.Title = "Saved lecture " + i; saved.Data.Notes = new string('n', 5000); first.History.Add(saved); }
        store.Save(initial); var profile = store.LoadProfile(); profile.DisplayName = "Saved profile name"; store.SaveProfile(profile);
        var home = new HomeWindow(store) { Width = 1280, Height = 1000 }; home.Show(); Pump(TimeSpan.FromMilliseconds(220));
        Assert(Descendants(home).OfType<Border>().Any(b => Equals(b.Tag, "WindowCaption")), "Home retains its primary window controls");
        Assert(!Descendants(home).OfType<TextBlock>().Any(t => t.Text.StartsWith("Hey,") || t.Text.StartsWith("Welcome back,")) && store.LoadProfile().DisplayName == "Saved profile name", "Home has no greeting while profile name remains intact");
        Assert(Descendants(home).OfType<Button>().Count(b => Equals(b.Content, "Changelog")) == 1, "Home keeps only the footer Changelog link");
        var firstColumn = home.Recent.Children.OfType<StackPanel>().First();
        Assert(firstColumn.Children.OfType<Button>().Select(b => (Guid)b.Tag).SequenceEqual(initial.Workspace.Dashboards.Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).Take(4).Select(d => d.DashboardId)), "Recent Spaces shows exactly four newest non-developer spaces");
        Assert(firstColumn.Children.OfType<Button>().All(b => b.ActualWidth <= 280 && b.ActualHeight <= 84), "Recent cards stay compact");
        Tap(home, ">"); Pump(TimeSpan.FromMilliseconds(100));
        Assert(home.Recent.Children.OfType<StackPanel>().SelectMany(column => column.Children.OfType<Button>()).Count() == 7 && home.Recent.Children.OfType<StackPanel>().Count() == 2, "Chevron expands older spaces into columns of four"); Capture(home, "navigation-home-expanded");
        Tap(home, "<"); Pump(TimeSpan.FromMilliseconds(50)); Assert(firstColumn.Children.OfType<Button>().Count() == 4 && home.Recent.Children.OfType<StackPanel>().Count() == 1, "Chevron collapse restores four recent spaces");
        foreach (var entry in new[] { (Method: "OpenAbout", Name: "About"), (Method: "OpenChangelog", Name: "Changelog"), (Method: "OpenHistory", Name: "History") })
        {
            Call(home, entry.Method); Pump(TimeSpan.FromMilliseconds(180)); var panel = home.OwnedWindows.OfType<NexaDialog>().Single(); PanelChrome(panel, entry.Name);
            foreach (var viewer in Visuals(panel).OfType<ScrollViewer>().Where(s => s.ScrollableHeight > 0).ToArray()) viewer.ScrollToEnd(); Pump(TimeSpan.FromMilliseconds(50)); PanelChrome(panel, entry.Name + " scrolled");
            var bars = Visuals(panel).OfType<ScrollBar>().ToArray(); Assert(bars.All(b => b.ActualWidth == 0 || b.ActualHeight == 0), entry.Name + " hides scrollbar chrome"); Capture(panel, "navigation-" + entry.Name);
            Tap(panel, "Close"); WaitClosed(panel);
        }
        Assert(Math.Abs(((UIElement)home.Content).Opacity - 1) < .001, "Closing panels restores the Home backdrop"); home.Close();
        var window = new MainWindow(store) { Width = 1280, Height = 900 }; window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        var session = window.state.Session!; var start = session.Start; var end = session.End; var sessionId = Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId;
        var widgets = JsonSerializer.Serialize(window.state.Widgets); var notes = window.state.Notes; var remaining = session.Remaining(DateTimeOffset.Now);
        Assert(window.TopNavigation.Children.IndexOf(window.AddWidgetButton) == window.TopNavigation.Children.IndexOf(window.SessionSettingsButton) + 1, "Add Widget is immediately right of Session");
        Assert(!window.WidgetCanvas.Children.OfType<Button>().Any(b => Equals(b.Tag, "AddWidgetShortcut")), "Dashboard plus shortcut and placement bar are gone");
        var size = window.WidgetCanvas.RenderSize; window.AdvanceDashboardTip();
        Assert(window.DashboardTip.Text == DashboardTips.Messages[1] && window.DashboardTip.HorizontalAlignment == HorizontalAlignment.Center && !window.DashboardTip.IsHitTestVisible && window.WidgetCanvas.RenderSize == size && JsonSerializer.Serialize(window.state.Widgets) == widgets, "Centered rotating tips leave layout and pointer input untouched");
        window.AddWidgetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(180)); var picker = window.OwnedWindows.OfType<NexaDialog>().Single(); PanelChrome(picker, "Add Widgets");
        var scroller = Descendants(picker).OfType<ScrollViewer>().Single(); scroller.Focusable = true; scroller.Focus();
        Assert(scroller.ScrollableHeight > 0, "Widget catalog remains scrollable without scrollbar chrome");
        Key(picker, scroller, System.Windows.Input.Key.PageDown, Keyboard.KeyDownEvent); Pump(TimeSpan.FromMilliseconds(80)); Assert(scroller.VerticalOffset > 0, "Keyboard PageDown scrolls the hidden-bar catalog");
        scroller.ScrollToTop(); Pump(TimeSpan.FromMilliseconds(30)); scroller.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.MouseWheelEvent }); Pump(TimeSpan.FromMilliseconds(80)); Assert(scroller.VerticalOffset > 0, "Mouse wheel scrolls the hidden-bar catalog");
        scroller.ScrollToTop(); Pump(TimeSpan.FromMilliseconds(30)); scroller.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -24) { RoutedEvent = Mouse.MouseWheelEvent }); Pump(TimeSpan.FromMilliseconds(80)); Assert(scroller.VerticalOffset > 0, "Small wheel deltas used by trackpads still scroll");
        scroller.ScrollToEnd(); Pump(TimeSpan.FromMilliseconds(50)); PanelChrome(picker, "Add Widgets scrolled"); Capture(picker, "navigation-add-widgets");
        var outside = Outside(window); Assert(outside.Handled && window.OwnedWindows.OfType<NexaDialog>().Count() == 1, "Outside click is consumed without spawning another picker"); WaitClosed(picker); Pump(TimeSpan.FromMilliseconds(100)); Assert(!window.OwnedWindows.OfType<NexaDialog>().Any(), "Outside click does not reopen the picker after dismissal");
        // Raising the blank-space mouse event takes the same routed path as the canvas handler.
        window.WidgetCanvas.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent }); Pump(TimeSpan.FromMilliseconds(180)); picker = window.OwnedWindows.OfType<NexaDialog>().Single();
        Assert(picker.IsVisible, "Clicking blank dashboard space still opens Add Widgets"); Outside(window); WaitClosed(picker); Assert(!window.OwnedWindows.OfType<NexaDialog>().Any(), "Clicking the same blank area closes without reopening");
        Call(window, "HistoryClick", window, new RoutedEventArgs()); Pump(TimeSpan.FromMilliseconds(180)); var history = window.OwnedWindows.OfType<NexaDialog>().Single(); PanelChrome(history, "Dashboard History"); Capture(history, "navigation-dashboard-history");
        Assert(Outside(window).Handled && window.IsEnabled, "Dashboard History uses shared outside-click dismissal instead of blocking its owner"); WaitClosed(history);
        Assert(window.state.Notes == notes && ReferenceEquals(window.state.Session, session) && Continuity.Current(window.state.Workspace!, store.UserId).History.Count == 12, "Dismissing dashboard History preserves current content and archived sessions");
        window.OpenMainSettings("Session"); Pump(TimeSpan.FromMilliseconds(180)); var settings = window.OwnedWindows.OfType<SettingsDialog>().Single(); PanelChrome(settings, "Settings"); Capture(settings, "navigation-settings");
        var layered = new NexaPanel { Owner = settings, Title = "Top panel", Width = 450, Height = 320, Content = new TextBlock { Text = "Topmost only", Margin = new Thickness(24) } }; layered.ShowDismissible(); Pump(TimeSpan.FromMilliseconds(100));
        var escape = Key(window, window, System.Windows.Input.Key.Escape, Keyboard.PreviewKeyDownEvent); Assert(escape.Handled, "Escape targets the topmost panel from its owner"); WaitClosed(layered); Assert(settings.IsVisible, "Escape closes only the top stacked panel");
        var name = Descendants(settings).OfType<TextBox>().Single(t => t.Name == "SessionName"); name.Text = "Saved on Escape"; Key(settings, name, System.Windows.Input.Key.Escape, Keyboard.PreviewKeyDownEvent); WaitClosed(settings);
        Assert(store.Load().Session!.Title == "Saved on Escape" && ReferenceEquals(window.state.Session, session), "Escape flushes Settings autosave without replacing the active session");
        Assert(session.Start == start && session.End == end && session.StoppedAt == null && session.Remaining(DateTimeOffset.Now) < remaining && Continuity.Current(window.state.Workspace!, store.UserId).Current.SessionId == sessionId, "Overlay navigation preserves session identity and advancing timer");
        Assert(window.state.Notes == notes && JsonSerializer.Serialize(window.state.Widgets) == widgets && store.LoadProfile().DisplayName == "Saved profile name", "Panel navigation preserves notes, layout and profile");
        window.OpenMainSettings("Appearance"); Pump(TimeSpan.FromMilliseconds(100)); settings = window.OwnedWindows.OfType<SettingsDialog>().Single(); var combo = Descendants(settings).OfType<ComboBox>().First(); combo.IsDropDownOpen = true;
        Key(settings, combo, System.Windows.Input.Key.Escape, Keyboard.PreviewKeyDownEvent); Assert(!combo.IsDropDownOpen && settings.IsVisible, "First Escape closes a dropdown without closing its panel");
        Key(settings, settings, System.Windows.Input.Key.Escape, Keyboard.PreviewKeyDownEvent); WaitClosed(settings);
        Assert(Math.Abs(((UIElement)window.Content).Opacity - 1) < .001, "Final panel dismissal restores dashboard backdrop");
        Capture(window, "navigation-dashboard"); window.Close();
        var loaded = store.Load(); Assert(loaded.Workspace!.Dashboards.Count == 8 && loaded.Notes == notes && loaded.Session!.Title == "Saved on Escape" && loaded.Session.Start == start && loaded.Session.End == end && loaded.Workspace.Dashboards.Single(d => d.DashboardId == first.DashboardId).History.Count == 12, "Saved spaces, active session and history survive release navigation");
    }
}
