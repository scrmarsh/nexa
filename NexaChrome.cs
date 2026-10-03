using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Media.Animation;

namespace SessionDashboard;

internal static class NexaChrome
{
    private static readonly HashSet<Window> FadingWindows = [];
    internal static ImageSource Icon => new BitmapImage(new Uri("pack://application:,,,/Nexa;component/assets/nexa-icon-256.png"));
    internal static void Apply(Window window)
    {
        window.UseLayoutRounding = true; window.SnapsToDevicePixels = true; window.Icon = Icon; window.WindowStyle = WindowStyle.None; window.ResizeMode = ResizeMode.CanResize;
        WindowChrome.SetWindowChrome(window, new WindowChrome { CaptionHeight = 44, ResizeBorderThickness = new Thickness(10), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(8), UseAeroCaptionButtons = false });
        FadeIn(window);
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var dark = 1; DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
            var border = 0x00261B17; DwmSetWindowAttribute(hwnd, 34, ref border, sizeof(int));
        };
    }
    internal static void FadeIn(Window window)
    {
        window.Opacity = 0;
        window.Loaded += (_, _) => window.BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
    }
    internal static void FadeOutAndClose(Window window)
    {
        FadeOut(window, window.Close);
    }
    internal static void FadeOut(Window window, Action afterFade)
    {
        if (!FadingWindows.Add(window)) return;
        if (!window.IsVisible) { FadingWindows.Remove(window); afterFade(); return; }
        var fade = new DoubleAnimation(window.Opacity, 0, TimeSpan.FromMilliseconds(160)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
        fade.Completed += (_, _) => { window.BeginAnimation(Window.OpacityProperty, null); FadingWindows.Remove(window); afterFade(); };
        window.BeginAnimation(Window.OpacityProperty, fade);
    }
    internal static void ToggleMaximize(Window window) => window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    internal static Border Caption(Window window)
    {
        var dock = new DockPanel { LastChildFill = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(buttons, Dock.Right); dock.Children.Add(buttons);
        void Add(string glyph, string label, Action action)
        {
            var button = new Button { Content = glyph, ToolTip = label, Style = (Style)Application.Current.FindResource(label == "Close" ? "CaptionCloseButton" : "CaptionControlButton") };
            System.Windows.Automation.AutomationProperties.SetName(button, label); WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            button.Click += (_, _) => action(); buttons.Children.Add(button);
        }
        Add("\uE921", "Minimize", () => SystemCommands.MinimizeWindow(window));
        if (window.ResizeMode != ResizeMode.NoResize) { Add("\uE922", "Maximize or restore", () => ToggleMaximize(window)); var maximize = (Button)buttons.Children[^1]; window.StateChanged += (_, _) => maximize.Content = window.WindowState == WindowState.Maximized ? "\uE923" : "\uE922"; }
        Add("\uE8BB", "Close", () =>
        {
            if (window is MainWindow mainWindow) mainWindow.CloseWithFade();
            else FadeOutAndClose(window);
        });
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 8, 0) };
        brand.Children.Add(new Image { Source = Icon, Width = 22, Height = 22, Margin = new Thickness(0, 0, 10, 0) });
        brand.Children.Add(new TextBlock { Text = window.Title, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold }); dock.Children.Add(brand);
        var caption = new Border { Tag = "WindowCaption", Height = 44, BorderThickness = new Thickness(0, 0, 0, 1), Child = dock }; caption.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); caption.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); return caption;
    }
    internal static void TransferBounds(Window source, Window target)
    {
        var bounds = source.WindowState == WindowState.Normal ? new Rect(source.Left, source.Top, source.ActualWidth, source.ActualHeight) : source.RestoreBounds;
        target.WindowStartupLocation = WindowStartupLocation.Manual; target.MinWidth = Math.Min(target.MinWidth, bounds.Width); target.MinHeight = Math.Min(target.MinHeight, bounds.Height);
        target.Width = bounds.Width; target.Height = bounds.Height; target.Left = bounds.Left; target.Top = bounds.Top;
        target.WindowState = source.WindowState == WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

// Dialogs share the same native resize/caption hit testing as the main window.
internal class NexaDialog : Window
{
    private bool wrapping;
    private bool closingWithFade;
    internal UIElement? Body { get; private set; }
    internal bool IsPanel { get; }
    internal NexaDialog(bool panel = false)
    {
        IsPanel = panel;
        NexaChrome.Apply(this);
        if (panel)
        {
            ShowInTaskbar = false; ResizeMode = ResizeMode.NoResize;
            WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(0), GlassFrameThickness = new Thickness(0), UseAeroCaptionButtons = false });
        }
        SetResourceReference(BackgroundProperty, "BackgroundBrush"); SetResourceReference(ForegroundProperty, "TextBrush");
        Loaded += (_, _) => NexaPanels.Register(this);
        Closed += (_, _) => NexaPanels.Unregister(this);
        PreviewKeyDown += (_, e) => NexaPanels.HandleEscape(this, e);
        SourceInitialized += (_, _) => { if (panel && Owner != null) NexaPanels.FitOwner(this); };
    }
    internal void ShowDismissible()
    {
        NexaPanels.EnableOutsideDismissal(this);
        Show();
        Activate();
    }
    internal void CloseWithFade(bool? dialogResult = null)
    {
        if (closingWithFade) return;
        closingWithFade = true;
        NexaChrome.FadeOut(this, () =>
        {
            if (dialogResult.HasValue) DialogResult = dialogResult.Value;
            else Close();
        });
    }
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel) return;
        // A failed Settings save may cancel dismissal. Allow the same close gesture to retry.
        closingWithFade = false;
        BeginAnimation(OpacityProperty, null); Opacity = 1;
    }
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (wrapping || newContent is not UIElement body) return;
        Body = body;
        wrapping = true;
        Content = null;
        var dock = new DockPanel();
        if (IsPanel)
        {
            var footer = NexaPanels.CloseFooter(this); DockPanel.SetDock(footer, Dock.Bottom); dock.Children.Add(footer);
        }
        else { var caption = NexaChrome.Caption(this); DockPanel.SetDock(caption, Dock.Top); dock.Children.Add(caption); }
        dock.Children.Add(body);
        var frame = new Border { Child = dock, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), SnapsToDevicePixels = true };
        frame.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        Content = frame; wrapping = false;
    }
}
