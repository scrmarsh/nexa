using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace SessionDashboard;

internal static class NexaChrome
{
    internal static ImageSource Icon => new BitmapImage(new Uri("pack://application:,,,/Nexa;component/assets/nexa-icon-256.png"));
    internal static void Apply(Window window)
    {
        window.Icon = Icon; window.WindowStyle = WindowStyle.None;
        WindowChrome.SetWindowChrome(window, new WindowChrome { CaptionHeight = 44, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(8), UseAeroCaptionButtons = false });
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var dark = 1; DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
            var border = 0x00261B17; DwmSetWindowAttribute(hwnd, 34, ref border, sizeof(int));
        };
    }
    internal static void ToggleMaximize(Window window) => window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    internal static Border Caption(Window window)
    {
        var dock = new DockPanel { LastChildFill = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(buttons, Dock.Right); dock.Children.Add(buttons);
        void Add(string glyph, string label, Action action)
        {
            var button = new Button { Content = glyph, ToolTip = label, Width = 44, Height = 36, Padding = new Thickness(0), Margin = new Thickness(2, 4, 2, 4), Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            System.Windows.Automation.AutomationProperties.SetName(button, label); WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            button.Click += (_, _) => action(); buttons.Children.Add(button);
        }
        Add("−", "Minimize", () => SystemCommands.MinimizeWindow(window));
        if (window.ResizeMode != ResizeMode.NoResize) Add("□", "Maximize or restore", () => ToggleMaximize(window));
        Add("×", "Close", window.Close);
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 8, 0) };
        brand.Children.Add(new Image { Source = Icon, Width = 22, Height = 22, Margin = new Thickness(0, 0, 10, 0) });
        brand.Children.Add(new TextBlock { Text = window.Title, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold }); dock.Children.Add(brand);
        return new Border { Height = 44, Background = new SolidColorBrush(Color.FromRgb(13, 17, 25)), BorderBrush = new SolidColorBrush(Color.FromRgb(37, 44, 59)), BorderThickness = new Thickness(0, 0, 0, 1), Child = dock };
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

// Dialogs share the same native resize/caption hit testing as the main window.
internal class NexaDialog : Window
{
    private bool wrapping;
    internal UIElement? Body { get; private set; }
    internal NexaDialog()
    {
        NexaChrome.Apply(this);
        SetResourceReference(BackgroundProperty, "BackgroundBrush"); SetResourceReference(ForegroundProperty, "TextBrush");
    }
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        if (wrapping || newContent is not UIElement body) return;
        Body = body;
        wrapping = true;
        Content = null;
        var dock = new DockPanel(); var caption = NexaChrome.Caption(this); DockPanel.SetDock(caption, Dock.Top); dock.Children.Add(caption); dock.Children.Add(body);
        Content = dock; wrapping = false;
    }
}
