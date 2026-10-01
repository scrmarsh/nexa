using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SessionDashboard;

public sealed class DarkDatePicker : UserControl
{
    private readonly Button display = new() { HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(9, 8, 9, 8) };
    private readonly Popup popup;
    private readonly StackPanel calendar = new() { Width = 280 };
    private DateTime month = DateTime.Today;
    private DateTime? selected;
    public event EventHandler? SelectedDateChanged;
    public DateTime? SelectedDate
    {
        get => selected;
        set { if (selected == value?.Date) return; selected = value?.Date; month = selected ?? month; Refresh(); SelectedDateChanged?.Invoke(this, EventArgs.Empty); }
    }
    internal FrameworkElement CalendarPanel => calendar;
    internal void OpenCalendar() { Refresh(); popup.IsOpen = true; }
    public DarkDatePicker()
    {
        var border = new Border { Child = calendar, Padding = new Thickness(14), CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1) };
        border.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); border.SetResourceReference(Border.BorderBrushProperty, "LineBrush");
        popup = new Popup { StaysOpen = false, AllowsTransparency = true, Placement = PlacementMode.Bottom, PlacementTarget = display, Child = border };
        display.Click += (_, _) => OpenCalendar();
        var root = new Grid(); root.Children.Add(display); root.Children.Add(popup); Content = root; Refresh();
    }
    private void Refresh()
    {
        display.Content = selected?.ToString("MMM d, yyyy") ?? "Select date"; calendar.Children.Clear();
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        var previous = new Button { Content = "‹", Width = 32, Padding = new Thickness(4) };
        var next = new Button { Content = "›", Width = 32, Padding = new Thickness(4) }; DockPanel.SetDock(next, Dock.Right);
        previous.Click += (_, _) => { if (month.Year > 1 || month.Month > 1) { month = month.AddMonths(-1); Refresh(); } };
        next.Click += (_, _) => { if (month.Year < 9999 || month.Month < 12) { month = month.AddMonths(1); Refresh(); } };
        header.Children.Add(previous); header.Children.Add(next); header.Children.Add(new TextBlock { Text = month.ToString("MMMM yyyy"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Foreground = (Brush)FindResource("TextBrush") }); calendar.Children.Add(header);
        var grid = new UniformGrid { Columns = 7 };
        foreach (var day in CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames) grid.Children.Add(new TextBlock { Text = day, TextAlignment = TextAlignment.Center, FontSize = 10, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 0, 0, 8) });
        var first = new DateTime(month.Year, month.Month, 1);
        for (var i = 0; i < (int)first.DayOfWeek; i++) grid.Children.Add(new Border());
        for (var i = 1; i <= DateTime.DaysInMonth(month.Year, month.Month); i++)
        {
            var date = new DateTime(month.Year, month.Month, i);
            var button = new Button { Content = i, Padding = new Thickness(4, 8, 4, 8), Margin = new Thickness(1), Background = (Brush)FindResource(date == selected ? "AccentBrush" : "SurfaceBrush") };
            button.Click += (_, _) => { SelectedDate = date; popup.IsOpen = false; }; grid.Children.Add(button);
        }
        calendar.Children.Add(grid);
        var footer = new UniformGrid { Columns = 2, Margin = new Thickness(0, 10, 0, 0) };
        var today = new Button { Content = "Today" }; today.Click += (_, _) => { SelectedDate = DateTime.Today; popup.IsOpen = false; };
        var clear = new Button { Content = "Clear" }; clear.Click += (_, _) => { SelectedDate = null; popup.IsOpen = false; };
        footer.Children.Add(today); footer.Children.Add(clear); calendar.Children.Add(footer);
    }
}
