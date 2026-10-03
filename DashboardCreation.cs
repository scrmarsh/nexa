using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

internal sealed class DashboardCreation : NexaDialog
{
    internal DashboardRecord? Result { get; private set; }
    internal readonly TextBox NameInput = new() { Text = "My Space", MaxLength = 100 };
    internal readonly ColorPickerButton AccentPicker = new() { SelectedColor = "#69ACFF" };
    internal readonly ClockPicker Start = new() { Value = DateTime.Now.TimeOfDay };
    internal readonly ClockPicker End = new() { Value = DateTime.Now.AddHours(1).TimeOfDay };
    internal readonly CheckBox Schedule = new() { Content = "Schedule a session (end before start means tomorrow)" };
    private readonly List<DashboardTemplate> options;
    private DashboardTemplate selectedTemplate;
    private readonly Dictionary<string, CheckBox> widgetChecks = new();
    private readonly WrapPanel templateGrid = new();
    public DashboardCreation(Window owner, string userId) : base()
    {
        Owner = owner; Title = "Nexa · Create Space"; Width = 820; Height = 760; MinHeight = 560; MinWidth = 620; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var custom = new DashboardTemplate("New Space", "Start from a blank canvas and pick what you want.", "#69ACFF", []);
        options = [custom, .. DashboardTemplates.All];
        selectedTemplate = custom;

        var panel = new StackPanel { Margin = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "Create Space", FontSize = 28, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Choose a starting point, then refine the Space details below.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 14), Foreground = (Brush)FindResource("MutedBrush") });

        templateGrid.Margin = new Thickness(0, 0, 0, 18);
        foreach (var template in options)
        {
            var button = new Button { Tag = template, Width = 180, Height = 104, Margin = new Thickness(0, 0, 10, 10), Padding = new Thickness(12), HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Top, Background = Brushes.Transparent, BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(1) };
            button.Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = template.Name, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) },
                    new TextBlock { Text = template.Description, TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = (Brush)FindResource("MutedBrush") }
                }
            };
            button.Click += (_, _) => SelectTemplate(template, button);
            templateGrid.Children.Add(button);
        }
        panel.Children.Add(templateGrid);

        void Field(string label, UIElement control) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 6) }); panel.Children.Add(control); }
        Field("Space name", NameInput);
        Field("Accent color", AccentPicker);
        panel.Children.Add(Schedule);
        Field("Start time", Start); Field("End time", End);

        var widgetPanel = new WrapPanel { Margin = new Thickness(0, 12, 0, 8) };
        foreach (var definition in WidgetLibrary.Definitions.OrderBy(d => d.Group).ThenBy(d => d.Name))
        {
            if (definition.Name is "Session controls" or "Pixel timer" or "Hedy") continue;
            var box = new CheckBox { Content = definition.Name, Margin = new Thickness(0, 0, 12, 8), IsChecked = false };
            widgetChecks[definition.Name] = box;
            widgetPanel.Children.Add(box);
        }
        panel.Children.Add(new TextBlock { Text = "Available widgets", Margin = new Thickness(0, 12, 0, 6) });
        panel.Children.Add(widgetPanel);

        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Foreground = Brushes.IndianRed }; panel.Children.Add(error);
        var create = new Button { Content = "Create Space", Margin = new Thickness(0, 20, 0, 0), Style = (Style)FindResource("PrimaryButton") }; panel.Children.Add(create);
        create.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text)) { error.Text = "Enter a Space name."; return; }
            var chosen = selectedTemplate;
            var selectedWidgets = widgetChecks.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key).ToList();
            if (chosen.Name == "New Space" && selectedWidgets.Count == 0)
            {
                error.Text = "Choose at least one widget for a new Space, or pick a starter template.";
                return;
            }

            var data = new DashboardData { AccentColor = AccentPicker.SelectedColor, PixelColor = AccentPicker.SelectedColor, WidgetLayoutInitialized = true };
            IEnumerable<string> widgetNames = chosen.Name == "New Space" ? selectedWidgets : chosen.Widgets;
            foreach (var kind in widgetNames)
            {
                var key = kind switch { "Pixel timer" => "pixels", "Session controls" => "setup", "Notes" => "notes", "Tasks" => "tasks", "Session timeline" => "timeline", "Hedy" => "hedy", _ => "" };
                var widget = WidgetLibrary.Create(kind, key);
                if (kind == "Pixel timer") widget.Title = "Timer";
                data.Widgets.Add(widget);
            }

            if (data.Widgets.Count == 0) data.Widgets.Add(WidgetLibrary.Create("Notes", "notes"));
            WidgetLibrary.Arrange(data.Widgets, false);
            data.Draft = new DashboardDraft { Initialized = true, Title = NameInput.Text.Trim(), StartMinutes = (int)Start.Value.TotalMinutes, EndMinutes = (int)End.Value.TotalMinutes };
            if (Schedule.IsChecked == true)
            {
                var start = DateTime.Today + Start.Value; var end = DateTime.Today + End.Value; if (end <= start) end = end.AddDays(1);
                if (TimeZoneInfo.Local.IsInvalidTime(start) || TimeZoneInfo.Local.IsInvalidTime(end) || end <= DateTime.Now) { error.Text = "Choose valid times with an end in the future."; return; }
                data.Session = new Session { Title = NameInput.Text.Trim(), Start = new DateTimeOffset(start), End = new DateTimeOffset(end) };
                data.Draft.EndDate = end.Date;
            }

            Result = Continuity.NewDashboard(NameInput.Text.Trim(), chosen.Name, data, userId);
            CloseWithFade(true);
        };

        SelectTemplate(custom, null);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }

    private void SelectTemplate(DashboardTemplate template, Button? button)
    {
        selectedTemplate = template;
        NameInput.Text = template.Name == "New Space" ? "My Space" : template.Name;
        AccentPicker.SelectedColor = template.Accent;

        foreach (var pair in widgetChecks)
        {
            var name = pair.Key;
            var check = pair.Value;
            check.IsChecked = template.Name == "New Space" ? false : template.Widgets.Contains(name);
        }

        foreach (var child in templateGrid.Children.OfType<Button>())
        {
            var isSelected = Equals(child.Tag, template);
            child.Background = isSelected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(template.Accent)) { Opacity = 0.12 } : Brushes.Transparent;
            child.BorderBrush = isSelected ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(template.Accent)) : (Brush)FindResource("LineBrush");
            child.BorderThickness = new Thickness(isSelected ? 2 : 1);
        }
    }
}
