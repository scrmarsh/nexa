using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SessionDashboard;

public sealed class ColorPickerButton : Button
{
    private static readonly (string Name, string Hex)[] Palette =
    [
        ("nexa blue", "#5B7CFF"), ("nexa violet", "#8B5CF6"), ("Electric indigo", "#6C7AEF"),
        ("Soft lilac", "#A855F7"), ("Sky blue", "#69ACFF"), ("Azure", "#3B82F6"),
        ("Mint", "#6AE58F"), ("Teal", "#14B8A6"), ("Amber", "#F59E0B"),
        ("Coral", "#F97370"), ("Rose", "#EC4899"), ("Slate", "#94A3B8")
    ];

    private string selectedColor = "#5B7CFF";
    private readonly Border swatch = new() { Width = 25, Height = 22, CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 9, 0) };
    private readonly TextBlock label = new() { VerticalAlignment = VerticalAlignment.Center };

    public string SelectedColor
    {
        get => selectedColor;
        set
        {
            if (!InputRules.IsColor(value)) return;
            selectedColor = value;
            UpdateDisplay();
        }
    }

    public event EventHandler? ColorChanged;

    public ColorPickerButton()
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(swatch);
        content.Children.Add(label);
        content.Children.Add(new TextBlock { Text = "▾", Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Gray });
        Content = content;
        Padding = new Thickness(10, 7, 10, 7);
        HorizontalContentAlignment = HorizontalAlignment.Left;
        SetResourceReference(BackgroundProperty, "SurfaceBrush");
        SetResourceReference(ForegroundProperty, "TextBrush");
        SetResourceReference(BorderBrushProperty, "LineBrush");
        ToolTip = "Choose a color";
        Click += OpenPicker;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        swatch.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor));
        swatch.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
        label.Text = Palette.FirstOrDefault(item => item.Hex.Equals(selectedColor, StringComparison.OrdinalIgnoreCase)).Name ?? "Custom color";
    }

    private void OpenPicker(object sender, RoutedEventArgs e)
    {
        var dialog = new ColorPickerDialog(selectedColor) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        SelectedColor = dialog.SelectedColor;
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ColorPickerDialog : NexaDialog
    {
        private readonly Slider hue = new() { Minimum = 0, Maximum = 360, Value = 220, Margin = new Thickness(0, 8, 0, 10) };
        private readonly Border preview = new() { Width = 36, Height = 30, CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 10, 0) };
        private readonly TextBlock previewName = new() { VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
        private readonly Grid colorField = new() { Width = 300, Height = 176, ClipToBounds = true, Margin = new Thickness(0, 8, 0, 4) };
        private readonly Canvas selectorLayer = new();
        private readonly Ellipse selector = new() { Width = 16, Height = 16, Stroke = Brushes.White, StrokeThickness = 2, Fill = Brushes.Transparent, IsHitTestVisible = false };
        private double saturation = 0.72;
        private double brightness = 1;

        public string SelectedColor { get; private set; }

        public ColorPickerDialog(string initialColor)
        {
            SelectedColor = initialColor;
            Title = "Choose a color"; Width = 410; Height = 550; MinWidth = 390; MinHeight = 500;
            var panel = new StackPanel { Margin = new Thickness(24) };
            panel.Children.Add(new TextBlock { Text = "CHOOSE A COLOR", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("MutedBrush") });
            var selectedRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 8) };
            selectedRow.Children.Add(preview); selectedRow.Children.Add(previewName); panel.Children.Add(selectedRow);

            var swatches = new UniformGrid { Columns = 6, Margin = new Thickness(0, 2, 0, 8) };
            foreach (var item in Palette)
            {
                var color = (Color)ColorConverter.ConvertFromString(item.Hex);
                var chip = new Button { Width = 34, Height = 30, Padding = new Thickness(0), Margin = new Thickness(3), Background = new SolidColorBrush(color), BorderBrush = Brushes.White, BorderThickness = new Thickness(1), ToolTip = item.Name };
                chip.Click += (_, _) => SetFromColor(color);
                swatches.Children.Add(chip);
            }
            panel.Children.Add(swatches);

            colorField.Children.Add(new Rectangle { Fill = new LinearGradientBrush(Colors.White, Colors.Red, 0), IsHitTestVisible = false });
            colorField.Children.Add(new Rectangle { Fill = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1), GradientStops = [new(Colors.Transparent, 0), new(Colors.Black, 1)] }, IsHitTestVisible = false });
            selectorLayer.Background = Brushes.Transparent;
            selectorLayer.Children.Add(selector);
            selectorLayer.MouseLeftButtonDown += FieldMouse;
            selectorLayer.MouseMove += HandlePointerMove;
            selectorLayer.MouseLeftButtonUp += (_, _) => selectorLayer.ReleaseMouseCapture();
            colorField.Children.Add(selectorLayer);
            colorField.Loaded += (_, _) => RefreshSelectedColor();
            panel.Children.Add(colorField);

            panel.Children.Add(new TextBlock { Text = "Hue", Margin = new Thickness(0, 4, 0, 0), Foreground = (Brush)FindResource("MutedBrush") });
            hue.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5),
                GradientStops = [new(Colors.Red, 0), new(Colors.Yellow, 1d / 6), new(Colors.Lime, 2d / 6), new(Colors.Cyan, 3d / 6), new(Colors.Blue, 4d / 6), new(Colors.Magenta, 5d / 6), new(Colors.Red, 1)]
            };
            hue.ValueChanged += (_, _) => RefreshColorField();
            panel.Children.Add(hue);

            var actions = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0) };
            cancel.Click += (_, _) => CloseWithFade(false);
            DockPanel.SetDock(cancel, Dock.Right); actions.Children.Add(cancel);
            var choose = new Button { Content = "Save color", Style = (Style)FindResource("PrimaryButton") };
            choose.Click += (_, _) => CloseWithFade(true); DockPanel.SetDock(choose, Dock.Right); actions.Children.Add(choose);
            var root = new DockPanel(); actions.Margin = new Thickness(24, 12, 24, 20); DockPanel.SetDock(actions, Dock.Bottom); root.Children.Add(actions);
            root.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
            Content = root;

            SetFromColor((Color)ColorConverter.ConvertFromString(initialColor));
        }

        private void FieldMouse(object sender, MouseButtonEventArgs e)
        {
            UpdateSelectionFromPointer(e.GetPosition(colorField));
            selectorLayer.CaptureMouse();
        }

        private void HandlePointerMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            UpdateSelectionFromPointer(e.GetPosition(colorField));
        }

        private void UpdateSelectionFromPointer(Point point)
        {
            saturation = Math.Clamp(point.X / colorField.ActualWidth, 0, 1);
            brightness = 1 - Math.Clamp(point.Y / colorField.ActualHeight, 0, 1);
            RefreshSelectedColor();
        }

        private void SetFromColor(Color color)
        {
            var (h, s, v) = ToHsv(color);
            hue.Value = h;
            saturation = s;
            brightness = v;
            RefreshSelectedColor();
        }

        private void RefreshColorField()
        {
            var hueColor = FromHsv(hue.Value, 1, 1);
            colorField.Children.OfType<Rectangle>().First().Fill = new LinearGradientBrush(Colors.White, hueColor, 0);
            RefreshSelectedColor();
        }

        private void RefreshSelectedColor()
        {
            var selected = FromHsv(hue.Value, saturation, brightness);
            SelectedColor = $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
            preview.Background = new SolidColorBrush(selected);
            previewName.Text = Palette.FirstOrDefault(item => item.Hex.Equals(SelectedColor, StringComparison.OrdinalIgnoreCase)).Name ?? "Custom color";
            Canvas.SetLeft(selector, Math.Clamp(saturation * colorField.ActualWidth - selector.Width / 2, 0, Math.Max(0, colorField.ActualWidth - selector.Width)));
            Canvas.SetTop(selector, Math.Clamp((1 - brightness) * colorField.ActualHeight - selector.Height / 2, 0, Math.Max(0, colorField.ActualHeight - selector.Height)));
        }

        private static (double Hue, double Saturation, double Value) ToHsv(Color color)
        {
            var r = color.R / 255d; var g = color.G / 255d; var b = color.B / 255d;
            var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var delta = max - min;
            var h = delta == 0 ? 0 : max == r ? 60 * (((g - b) / delta) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
            if (h < 0) h += 360;
            return (h, max == 0 ? 0 : delta / max, max);
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            var chroma = value * saturation;
            var x = chroma * (1 - Math.Abs((hue / 60 % 2) - 1));
            var m = value - chroma;
            var (r, g, b) = hue switch
            {
                < 60 => (chroma, x, 0d), < 120 => (x, chroma, 0d), < 180 => (0d, chroma, x),
                < 240 => (0d, x, chroma), < 300 => (x, 0d, chroma), _ => (chroma, 0d, x)
            };
            return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
        }
    }
}
