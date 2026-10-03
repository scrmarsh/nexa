using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

internal sealed class SplashWindow : Window
{
    internal const int DisplayMilliseconds = 2400;
    internal double DisplayedMilliseconds { get; private set; }
    internal SplashWindow()
    {
        Title = "Nexa"; Icon = NexaChrome.Icon; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        Width = 620; Height = 330; WindowStartupLocation = WindowStartupLocation.CenterScreen; Background = new SolidColorBrush(Color.FromRgb(10, 14, 23));
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new NexaLogo { Width = 480, Height = 152 });
        panel.Children.Add(new TextBlock { Text = "A space for whatever comes next.", Foreground = new SolidColorBrush(Color.FromRgb(166, 177, 200)), FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 18, 0, 0) });
        var content = new Grid();
        content.Children.Add(panel);
        content.Children.Add(new TextBlock { Text = App.VersionLabel, Foreground = new SolidColorBrush(Color.FromRgb(116, 128, 151)), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 14, 10) });
        Content = new Border { BorderBrush = new SolidColorBrush(Color.FromRgb(42, 49, 69)), BorderThickness = new Thickness(1), Child = content };
        NexaChrome.FadeIn(this);
    }
    internal async Task<HomeWindow> EnterHome(StateStore store, bool diagnostic = false)
    {
        var watch = Stopwatch.StartNew();
        await Task.Delay(DisplayMilliseconds);
        DisplayedMilliseconds = watch.Elapsed.TotalMilliseconds;
        var home = new HomeWindow(store) { Opacity = 0 };
        if (diagnostic) { home.WindowStartupLocation = WindowStartupLocation.Manual; home.Left = -10000; home.Top = -10000; home.ShowActivated = false; home.ShowInTaskbar = false; }
        Application.Current.MainWindow = home; home.Show();
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)));
        home.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300)));
        await Task.Delay(320); Close(); return home;
    }
}
