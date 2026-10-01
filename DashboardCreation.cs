using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed class DashboardCreation : NexaDialog
{
    internal DashboardRecord? Result { get; private set; }
    internal readonly ComboBox TemplatePicker = new() { ItemsSource = DashboardTemplates.All, DisplayMemberPath = "Name", SelectedIndex = 0 };
    internal readonly TextBox NameInput = new() { Text = "Class", MaxLength = 100 };
    internal readonly TextBox AccentInput = new() { Text = "#69ACFF" };
    internal readonly ClockPicker Start = new() { Value = DateTime.Now.TimeOfDay };
    internal readonly ClockPicker End = new() { Value = DateTime.Now.AddHours(1).TimeOfDay };
    internal readonly CheckBox Schedule = new() { Content = "Schedule a session (end before start means tomorrow)" };
    internal DashboardCreation(Window owner, string userId) : base()
    {
        Owner = owner; Title = "Nexa · Create dashboard"; Width = 620; Height = 680; MinHeight = 480; MinWidth = 460; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "Create dashboard", FontSize = 28, FontWeight = FontWeights.SemiBold });
        var description = new TextBlock { Text = DashboardTemplates.All[0].Description, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 18) }; panel.Children.Add(description);
        void Field(string label, UIElement control) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 6) }); panel.Children.Add(control); }
        Field("Start from a preset", TemplatePicker); Field("Dashboard name", NameInput); Field("Accent color (#RRGGBB)", AccentInput);
        var preview = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 14), Text = "Pixel timer · Session controls · Notes · Tasks · Timeline · Hedy · Questions" }; panel.Children.Add(preview);
        TemplatePicker.SelectionChanged += (_, _) =>
        {
            var selected = (DashboardTemplate)TemplatePicker.SelectedItem; NameInput.Text = selected.Name == "Blank" ? "My dashboard" : selected.Name;
            AccentInput.Text = selected.Accent; description.Text = selected.Description; preview.Text = selected.Widgets.Length == 0 ? "An empty canvas, ready for any widget." : string.Join(" · ", selected.Widgets);
        };
        panel.Children.Add(Schedule);
        Field("Start time", Start); Field("End time", End);
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) }; panel.Children.Add(error);
        var create = new Button { Content = "Create dashboard", Margin = new Thickness(0, 20, 0, 0), Style = (Style)FindResource("PrimaryButton") }; panel.Children.Add(create);
        create.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text) || !InputRules.IsColor(AccentInput.Text)) { error.Text = "Enter a name and a six-digit hex accent color."; return; }
            var template = (DashboardTemplate)TemplatePicker.SelectedItem;
            var data = DashboardTemplates.Build(template); data.AccentColor = AccentInput.Text; data.PixelColor = AccentInput.Text;
            data.Draft = new DashboardDraft { Initialized = true, Title = NameInput.Text.Trim(), StartMinutes = (int)Start.Value.TotalMinutes, EndMinutes = (int)End.Value.TotalMinutes };
            if (Schedule.IsChecked == true)
            {
                var start = DateTime.Today + Start.Value; var end = DateTime.Today + End.Value; if (end <= start) end = end.AddDays(1);
                if (TimeZoneInfo.Local.IsInvalidTime(start) || TimeZoneInfo.Local.IsInvalidTime(end) || end <= DateTime.Now) { error.Text = "Choose valid times with an end in the future."; return; }
                data.Session = new Session { Title = NameInput.Text.Trim(), Start = new DateTimeOffset(start), End = new DateTimeOffset(end) };
                data.Draft.EndDate = end.Date;
            }
            Result = Continuity.NewDashboard(NameInput.Text.Trim(), template.Name, data, userId); DialogResult = true;
        };
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}

