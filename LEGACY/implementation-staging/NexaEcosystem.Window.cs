using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed class EcosystemWindow : Window
{
    private static readonly Dictionary<string, EcosystemWindow> open = new(StringComparer.OrdinalIgnoreCase);
    private readonly NexaEcosystem world;
    private readonly BugOverlay surface;
    private readonly Border viewport;
    private readonly TextBlock status = new() { Margin = new Thickness(16, 8, 16, 8), TextWrapping = TextWrapping.Wrap };
    private readonly Button pause = new() { Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(6) };
    private readonly DispatcherTimer info = new() { Interval = TimeSpan.FromSeconds(1) };
    private double zoom = 1;
    private Vector pan;
    private Point? drag;
    internal bool IsFullscreen { get; private set; }
    internal NexaEcosystem World => world;
    internal static EcosystemWindow ShowFor(Window owner, StateStore store)
    {
        var key = System.IO.Path.GetFullPath(store.FilePath);
        if (open.TryGetValue(key, out var existing)) { existing.WindowState = WindowState.Maximized; existing.Activate(); return existing; }
        var window = new EcosystemWindow(owner, store); open[key] = window;
        window.Closed += (_, _) => open.Remove(key); window.Show(); return window;
    }
    internal EcosystemWindow(Window owner, StateStore store)
    {
        Owner = owner; Title = "nexa · Bug Ecosystem"; Width = 1280; Height = 850; MinWidth = 800; MinHeight = 550;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SetResourceReference(BackgroundProperty, "BackgroundBrush"); SetResourceReference(ForegroundProperty, "TextBrush");
        NexaChrome.Apply(this); world = NexaEcosystem.For(store);
        var root = new DockPanel();
        var header = new DockPanel { LastChildFill = true };
        var caption = NexaChrome.Caption(this); DockPanel.SetDock(caption, Dock.Top); root.Children.Add(caption);
        var actions = new WrapPanel { Margin = new Thickness(10, 6, 10, 4) };
        void Add(string text, Action callback)
        { var button = new Button { Content = text, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(6) }; button.Click += (_, _) => callback(); actions.Children.Add(button); }
        actions.Children.Add(pause); pause.Click += (_, _) => { world.TogglePause(); RefreshStatus(); };
        Add("Restore Backup", () =>
        {
            if (MessageBox.Show(this, "Restore the previous ecosystem backup? The current state is preserved in a recovery copy.", "Restore ecosystem", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) world.RestoreBackup();
            RefreshStatus();
        });
        Add("Reset Ecosystem", () =>
        {
            if (MessageBox.Show(this, "Start a new ecosystem? This replaces the live world and preserves a recovery copy. Your dashboard and session history are unchanged.", "Reset ecosystem", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) world.Reset();
            RefreshStatus();
        });
        Add("Fit view", () => { zoom = 1; pan = new(); Map(); }); Add("Full screen (F11)", ToggleFullscreen); Add("Return to Nexa", Close);
        DockPanel.SetDock(actions, Dock.Top); root.Children.Add(actions);
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        surface = new BugOverlay(world.Manager) { DrawWorld = true };
        surface.DirectSquashed += bug => world.RecordSquash(bug, owner as MainWindow);
        viewport = new Border { Child = surface, ClipToBounds = true }; viewport.SetResourceReference(BackgroundProperty, "BackgroundBrush"); root.Children.Add(viewport); Content = root;
        viewport.SizeChanged += (_, _) => Map();
        surface.MouseWheel += (_, e) =>
        {
            var pointer = e.GetPosition(surface); var before = surface.ScreenToWorld(pointer);
            zoom = Math.Clamp(zoom * (e.Delta > 0 ? 1.12 : 1 / 1.12), .4, 3); Map();
            var after = surface.WorldToScreen(before); pan += pointer - after; Map(); e.Handled = true;
        };
        surface.MouseDown += (_, e) => { if (e.ChangedButton is MouseButton.Right or MouseButton.Middle) { drag = e.GetPosition(surface); surface.CaptureMouse(); e.Handled = true; } };
        surface.MouseMove += (_, e) => { if (drag is { } from) { var to = e.GetPosition(surface); pan += to - from; drag = to; Map(); } };
        surface.MouseUp += (_, e) => { if (drag != null) { drag = null; surface.ReleaseMouseCapture(); e.Handled = true; } };
        surface.LostMouseCapture += (_, _) => drag = null;
        PreviewKeyDown += (_, e) => { if (e.Key == Key.F11 || e.Key == Key.Escape && IsFullscreen) { ToggleFullscreen(); e.Handled = true; } };
        world.Attach(this, () => IsVisible && WindowState != WindowState.Minimized);
        world.Changed += RenderWorld;
        info.Tick += (_, _) => RefreshStatus(); Loaded += (_, _) => { info.Start(); Map(); RefreshStatus(); };
        Closed += (_, _) => { info.Stop(); world.Changed -= RenderWorld; world.Detach(this); };
        WindowState = WindowState.Maximized;
    }
    private void Map()
    {
        if (viewport.ActualWidth <= 0 || viewport.ActualHeight <= 0) return;
        var scale = Math.Min(viewport.ActualWidth / BugManager.WorldWidth, viewport.ActualHeight / BugManager.WorldHeight) * zoom;
        surface.SetWorldMapping(new Point((viewport.ActualWidth - BugManager.WorldWidth * scale) / 2 + pan.X, (viewport.ActualHeight - BugManager.WorldHeight * scale) / 2 + pan.Y), new Size(BugManager.WorldWidth * scale, BugManager.WorldHeight * scale));
        if (IsActive) world.Manager.VisibleBounds = new Rect(surface.ScreenToWorld(new Point()), surface.ScreenToWorld(new Point(viewport.ActualWidth, viewport.ActualHeight)));
    }
    private void RenderWorld() { surface.RefreshFrame(); }
    private void RefreshStatus()
    {
        pause.Content = world.Manager.WorldPaused ? "Resume World" : "Pause World";
        status.Text = world.Status ?? $"{world.Manager.EcosystemSettings.Behavior} · {world.Manager.EcosystemSettings.EffectiveAppearance} appearance · {world.Manager.Bugs.Count} creatures · {world.Manager.Nests.Sum(n => n.Deliveries)} deliveries · {world.Manager.Webs.Count} webs · Pan with right/middle drag; use the wheel to zoom.";
    }
    private void ToggleFullscreen()
    { WindowState = WindowState.Normal; IsFullscreen = !IsFullscreen; Topmost = IsFullscreen; ResizeMode = IsFullscreen ? ResizeMode.NoResize : ResizeMode.CanResize; WindowState = WindowState.Maximized; }
}
