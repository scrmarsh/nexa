using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SessionDashboard;

public sealed class ClockPicker : UserControl
{
    private readonly Button display = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch };
    private readonly ComboBox hours = new() { Width = 64 };
    private readonly ComboBox minutes = new() { Width = 64 };
    private readonly ComboBox period = new() { Width = 64 };
    private readonly Line hand = new() { X1 = 104, Y1 = 104, StrokeThickness = 2, IsHitTestVisible = false };
    private readonly Popup popup;
    internal FrameworkElement ClockMenu => (FrameworkElement)popup.Child;
    public event EventHandler? TimeChanged;
    public TimeSpan Value
    {
        get => new((hours.SelectedIndex + 1) % 12 + (period.SelectedIndex == 1 ? 12 : 0), Math.Max(0, minutes.SelectedIndex), 0);
        set { hours.SelectedIndex = (value.Hours + 11) % 12; minutes.SelectedIndex = value.Minutes; period.SelectedIndex = value.Hours >= 12 ? 1 : 0; Update(); }
    }
    public string Text
    {
        get => DateTime.Today.Add(Value).ToString("HH:mm");
        set { if (DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.NoCurrentDateDefault, out var time)) Value = time.TimeOfDay; }
    }
    public ClockPicker()
    {
        hours.ItemsSource = Enumerable.Range(1, 12).Select(i => i.ToString("00"));
        minutes.ItemsSource = Enumerable.Range(0, 60).Select(i => i.ToString("00"));
        period.ItemsSource = new[] { "AM", "PM" };
        hours.SelectedIndex = 11; minutes.SelectedIndex = 0; period.SelectedIndex = 0;
        var panel = new StackPanel { Width = 228 };
        panel.Children.Add(new TextBlock { Text = "CHOOSE HOUR, MINUTE & AM / PM", FontSize = 10, Margin = new Thickness(0, 0, 0, 12), Foreground = Brushes.White });
        var dial = new Canvas { Width = 208, Height = 208 };
        var face = new Ellipse { Width = 208, Height = 208, Stroke = new SolidColorBrush(Color.FromRgb(62, 82, 71)), StrokeThickness = 1 };
        dial.Children.Add(face);
        hand.SetResourceReference(Shape.StrokeProperty, "AccentBrush"); dial.Children.Add(hand);
        for (var hour = 1; hour <= 12; hour++)
        {
            var selected = hour;
            var angle = hour * Math.PI / 6;
            var button = new Button { Content = hour.ToString(), Width = 32, Height = 32, Padding = new Thickness(0), BorderThickness = new Thickness(0) };
            Canvas.SetLeft(button, 88 + Math.Sin(angle) * 80); Canvas.SetTop(button, 88 - Math.Cos(angle) * 80);
            button.Click += (_, _) => hours.SelectedIndex = selected - 1;
            dial.Children.Add(button);
        }
        panel.Children.Add(dial);
        var selectors = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 12) };
        foreach (var box in new[] { hours, minutes, period }) { box.Margin = new Thickness(4, 0, 4, 0); selectors.Children.Add(box); box.SelectionChanged += (_, _) => Update(); }
        System.Windows.Automation.AutomationProperties.SetName(hours, "Hour");
        System.Windows.Automation.AutomationProperties.SetName(minutes, "Minute");
        System.Windows.Automation.AutomationProperties.SetName(period, "AM or PM");
        panel.Children.Add(selectors);
        var done = new Button { Content = "Set time" }; panel.Children.Add(done);
        popup = new Popup { StaysOpen = false, AllowsTransparency = true, Placement = PlacementMode.Bottom, PlacementTarget = display, Child = new Border { Child = panel, Background = new SolidColorBrush(Color.FromRgb(13, 23, 17)), Padding = new Thickness(14), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12) } };
        done.Click += (_, _) => popup.IsOpen = false;
        display.Click += (_, _) => popup.IsOpen = !popup.IsOpen;
        var root = new Grid(); root.Children.Add(display); root.Children.Add(popup); Content = root;
        Update();
    }
    private void Update()
    {
        display.Content = DateTime.Today.Add(Value).ToString("h:mm tt") + " ▾";
        var angle = (Value.Hours + Value.Minutes / 60.0) * Math.PI / 6;
        hand.X2 = 104 + Math.Sin(angle) * 51; hand.Y2 = 104 - Math.Cos(angle) * 51;
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }
}
