using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private void BuildHomeHeader(StackPanel panel)
    {
        var header = new Grid(); header.ColumnDefinitions.Add(new() { Width = new GridLength(300) }); header.ColumnDefinitions.Add(new() { Width = new GridLength(32) }); header.ColumnDefinitions.Add(new());
        var brand = new StackPanel(); header.Children.Add(brand);
        var profile=store.LoadProfile();
        if(!string.IsNullOrWhiteSpace(profile.DisplayName)) brand.Children.Add(new TextBlock { Text="Hey, "+profile.DisplayName+".",FontSize=16,Margin=new Thickness(0,0,0,8) });
        brand.Children.Add(new NexaLogo { Width = 290, Height = 92, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-10, 0, 0, 0) });
        brand.Children.Add(new TextBlock { Text = App.VersionLabel, Foreground = (Brush)FindResource("MutedBrush"), FontSize = 13, Margin = new Thickness(0, 8, 0, 12) });
        var changelog = new Button { Content = "Changelog", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 8, 16, 8) }; changelog.Click += (_, _) => OpenChangelog(); brand.Children.Add(changelog);
        var tagline = new TextBlock { Text = "", FontSize = 24, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("TextBrush"), VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 25, 0, 0), MinHeight = 64 };
        System.Windows.Automation.AutomationProperties.SetName(tagline, "Home tagline"); Grid.SetColumn(tagline, 2); header.Children.Add(tagline); panel.Children.Add(header);
        const string phrase = "a space for whatever comes next";
        var position = 0; var hold = 0; var restartDelay = 0; var fading = false; var typing = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(55) };
        typing.Tick += (_, _) =>
        {
            if (fading) return;
            if (restartDelay > 0)
            {
                if (--restartDelay > 0) return;
                tagline.BeginAnimation(OpacityProperty, null); tagline.Opacity = 1; tagline.Text = ""; position = hold = 0;
            }
            if (position < phrase.Length) { tagline.Text = phrase[..++position]; return; }
            if (++hold < 28) return;
            fading = true;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(650));
            fade.Completed += (_, _) => { fading = false; restartDelay = 14; };
            tagline.BeginAnimation(OpacityProperty, fade);
        };
        Loaded += (_, _) => typing.Start(); Closed += (_, _) => typing.Stop();
    }

    private NexaDialog InfoDialog(string title, double width = 720, double height = 620) => new() { Title = "Nexa · " + title, Owner = this, Width = width, Height = height, MinWidth = 540, MinHeight = 440, WindowStartupLocation = WindowStartupLocation.CenterOwner };
    private TextBlock Copy(string text, double size = 14) => new() { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 0, 0, 16), LineHeight = size * 1.55 };

    private void OpenAbout()
    {
        var dialog = InfoDialog("About Nexa"); var root = new DockPanel();
        var footer = new Grid { Margin = new Thickness(28, 16, 28, 24) }; footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = new GridLength(12) }); footer.ColumnDefinitions.Add(new());
        var preview = new Button { Content = "Preview Nexa", MinHeight = 42, Style = (Style)FindResource("PrimaryButton") }; preview.Click += (_, _) => { dialog.Close(); OpenPreview(); }; footer.Children.Add(preview);
        var changes = new Button { Content = "Changelog", MinHeight = 42 }; changes.Click += (_, _) => { dialog.Close(); OpenChangelog(); }; Grid.SetColumn(changes, 2); footer.Children.Add(changes); DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var body = new StackPanel { Margin = new Thickness(28) };
        body.Children.Add(new NexaLogo { Width = 260, Height = 82, HorizontalAlignment = HorizontalAlignment.Left });
        body.Children.Add(Copy(App.VersionLabel, 13));
        body.Children.Add(new TextBlock { Text = "A space for whatever comes next", FontSize = 24, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 16) });
        body.Children.Add(Copy(ReleaseNotes.Description));
        body.Children.Add(new TextBlock { Text = "Made for your sessions", FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 8, 0, 12) });
        body.Children.Add(Copy(ReleaseNotes.UseCases));
        body.Children.Add(Copy("Your work stays on this PC under your Windows account. Autosave and session history help you return to notes, tasks and data. Preview uses a separate sample workspace."));
        body.Children.Add(Copy("Web app version coming soon. The Windows desktop app is the current release.", 12));
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); dialog.Content = root; dialog.ShowDismissible();
    }

    private void OpenChangelog()
    {
        var dialog = InfoDialog("Changelog", 800, 720); var body = new StackPanel { Margin = new Thickness(28) };
        body.Children.Add(new TextBlock { Text = "What’s new in Nexa", FontSize = 27, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        body.Children.Add(Copy("Release history · Windows desktop", 13));
        foreach (var release in ReleaseNotes.All)
        {
            var section = new StackPanel();
            var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var date = Copy(release.Date, 12); date.Margin = new Thickness(12, 4, 0, 0); DockPanel.SetDock(date, Dock.Right); heading.Children.Add(date);
            heading.Children.Add(new TextBlock { Text = App.ReleaseLabel(release.Version) + " \u2014 " + release.Title, TextWrapping = TextWrapping.Wrap, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("AccentBrush") }); section.Children.Add(heading);
            section.Children.Add(Copy(release.Summary, 15));
            foreach (var update in release.Updates)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 14) }; row.ColumnDefinitions.Add(new() { Width = new GridLength(22) }); row.ColumnDefinitions.Add(new());
                row.Children.Add(new System.Windows.Shapes.Ellipse { Width = 5, Height = 5, Fill = (Brush)FindResource("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(2, 8, 0, 0) });
                var text = Copy(update); text.Margin = new Thickness(0); Grid.SetColumn(text, 1); row.Children.Add(text); section.Children.Add(row);
            }
            body.Children.Add(new Border { Child = section, Background = (Brush)FindResource("PanelBrush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(22), Margin = new Thickness(0, 8, 0, 16) });
        }
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDismissible();
    }
}
