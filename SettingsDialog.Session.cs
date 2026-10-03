using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private UIElement BuildSession()
    {
        var page = Page("Session", "Edit the current Space and its session.");
        var session = state.Session;
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush") };
        if (session == null || !HasCurrentSession)
        {
            page.Children.Add(new TextBlock { Text = "No active session", FontSize = 20, FontWeight = FontWeights.SemiBold });
            page.Children.Add(new TextBlock { Text = "Start a session to change session-specific settings. Space appearance remains available below.", TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            page.Children.Add(new TextBlock { Text = session.StoppedAt == null ? "Current session: " + session.Title : "Stopped session: " + session.Title, FontWeight = FontWeights.SemiBold });
            var name = new TextBox { Text = session.Title, MinHeight = 40, Name = "SessionName" }; Field(page, "Session name", name);
            var dates = new Grid(); dates.ColumnDefinitions.Add(new()); dates.ColumnDefinitions.Add(new() { Width = new GridLength(14) }); dates.ColumnDefinitions.Add(new());
            var startDate = new DarkDatePicker { SelectedDate = session.Start.LocalDateTime.Date }; var endDate = new DarkDatePicker { SelectedDate = session.End.LocalDateTime.Date };
            var startTime = new ClockPicker { Value = session.Start.LocalDateTime.TimeOfDay }; var endTime = new ClockPicker { Value = session.End.LocalDateTime.TimeOfDay };
            var start = new StackPanel(); Field(start, "Start", startDate); start.Children.Add(startTime); dates.Children.Add(start);
            var end = new StackPanel(); Field(end, "End", endDate); end.Children.Add(endTime); Grid.SetColumn(end, 2); dates.Children.Add(end); page.Children.Add(dates);
            var interval = new ComboBox { ItemsSource = new[] { 5, 10, 15, session.IntervalMinutes }.Distinct().Order().ToArray(), SelectedItem = session.IntervalMinutes, MinHeight = 40 }; Field(page, "Summary interval (minutes)", interval);
            var days = new WrapPanel(); var selectedDays = session.ScheduledDays.Count == 0 ? new List<DayOfWeek> { session.Start.DayOfWeek } : session.ScheduledDays.ToList();
            foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)) days.Children.Add(new CheckBox { Content = day.ToString()[..3], Tag = day, IsChecked = selectedDays.Contains(day), Margin = new Thickness(0, 0, 14, 8) }); Field(page, "Selected days", days);
            var startEdited = false; var endEdited = false; var daysEdited = false;
            void SaveSession() => QueueSave("session", () =>
            {
                if (!ReferenceEquals(state.Session, session)) { status.Text = "The session changed. Reopen Settings."; return; }
                // Minute-based controls must not round saved seconds unless the user edits that time.
                var startLocal = startDate.SelectedDate?.Date + startTime.Value;
                var endLocal = endDate.SelectedDate?.Date + endTime.Value;
                if (string.IsNullOrWhiteSpace(name.Text) || startLocal == null || endLocal == null ||
                    startEdited && TimeZoneInfo.Local.IsInvalidTime(startLocal.Value) || endEdited && TimeZoneInfo.Local.IsInvalidTime(endLocal.Value))
                { status.Text = "Enter a name and valid dates/times. Invalid changes are not saved."; return; }
                var proposedStart = startEdited ? new DateTimeOffset(startLocal.Value) : session.Start;
                var proposedEnd = endEdited ? new DateTimeOffset(endLocal.Value) : session.End;
                var proposedDays = daysEdited ? days.Children.OfType<CheckBox>().Where(box => box.IsChecked == true).Select(box => (DayOfWeek)box.Tag).ToList() : session.ScheduledDays;
                if (proposedEnd <= proposedStart || (proposedEnd - proposedStart).TotalDays > 7 || daysEdited && proposedDays.Count == 0)
                { status.Text = "Choose an end after start, within 7 days, and at least one day. Invalid changes are not saved."; return; }
                var timingChanged = proposedStart != session.Start || proposedEnd != session.End;
                session.Title = name.Text.Trim(); session.Start = proposedStart; session.End = proposedEnd;
                session.IntervalMinutes = (int)interval.SelectedItem; session.ScheduledDays = proposedDays;
                if (timingChanged) session.ConfiguredDurationMinutes = (session.End - session.Start).TotalMinutes;
                // Preserve StoppedAt, reached milestones, notes, recording, Hedy and session identity.
                SaveState(); startEdited = endEdited = daysEdited = false; status.Text = "Session saved.";
            });
            name.TextChanged += (_, _) => SaveSession(); interval.SelectionChanged += (_, _) => SaveSession();
            startDate.SelectedDateChanged += (_, _) => { startEdited = true; SaveSession(); };
            endDate.SelectedDateChanged += (_, _) => { endEdited = true; SaveSession(); };
            startTime.TimeChanged += (_, _) => { startEdited = true; SaveSession(); };
            endTime.TimeChanged += (_, _) => { endEdited = true; SaveSession(); };
            foreach (var box in days.Children.OfType<CheckBox>()) box.Click += (_, _) => { daysEdited = true; SaveSession(); };
        }
        page.Children.Add(status);
        page.Children.Add(new Border { Height = 1, Background = Brush("LineBrush"), Margin = new Thickness(0, 22, 0, 12) });
        page.Children.Add(new TextBlock { Text = "Space appearance", FontSize = 20, FontWeight = FontWeights.SemiBold });
        var mainColor = new ColorPickerButton { SelectedColor = state.AccentColor }; Field(page, "Space main color", mainColor);
        var accentColor = new ColorPickerButton { SelectedColor = state.PixelColor }; Field(page, "Space accent / pixel color", accentColor);
        var background = Continuity.Clone(state.Background); var mode = new ComboBox { ItemsSource = Enum.GetValues<SpaceBackgroundMode>(), SelectedItem = background.Mode, MinHeight = 40 }; Field(page, "Background mode", mode);
        var color = new ColorPickerButton { SelectedColor = InputRules.IsColor(background.SolidColor) ? background.SolidColor : "#080B16" }; Field(page, "Solid color", color);
        var imagePath = new TextBox { Text = background.ImagePath, IsReadOnly = true, MinHeight = 40 }; var browse = new Button { Content = "Choose image", Margin = new Thickness(0, 8, 0, 0) };
        Field(page, "Custom image", imagePath); page.Children.Add(browse);
        var blur = new CheckBox { Content = "Blur background", IsChecked = background.Blur, Margin = new Thickness(0, 14, 0, 0) }; page.Children.Add(blur);
        var blurStrength = new Slider { Minimum = 0, Maximum = 40, Value = background.BlurStrength, TickFrequency = 2, IsSnapToTickEnabled = true }; Field(page, "Blur strength", blurStrength);
        var overlay = new Slider { Minimum = 0, Maximum = .9, Value = background.Overlay, TickFrequency = .05, IsSnapToTickEnabled = true }; Field(page, "Overlay / dimming", overlay);
        void SaveBackground() => QueueSave("background", () =>
        {
            state.AccentColor = mainColor.SelectedColor; state.PixelColor = accentColor.SelectedColor;
            state.Background = new SpaceBackground { Mode = (SpaceBackgroundMode)(mode.SelectedItem ?? SpaceBackgroundMode.Default), SolidColor = color.SelectedColor, ImagePath = imagePath.Text, Blur = blur.IsChecked == true, BlurStrength = blurStrength.Value, Overlay = overlay.Value };
            SaveState(); status.Text = "Space appearance saved.";
        });
        browse.Click += (_, _) => { var picker = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" }; if (picker.ShowDialog(this) == true) { imagePath.Text = picker.FileName; mode.SelectedItem = SpaceBackgroundMode.CustomImage; SaveBackground(); } };
        mainColor.ColorChanged += (_, _) => SaveBackground(); accentColor.ColorChanged += (_, _) => SaveBackground(); color.ColorChanged += (_, _) => SaveBackground();
        mode.SelectionChanged += (_, _) => SaveBackground(); blur.Click += (_, _) => SaveBackground(); blurStrength.ValueChanged += (_, _) => SaveBackground(); overlay.ValueChanged += (_, _) => SaveBackground();
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
