using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class SettingsDialog : NexaDialog
{
    private readonly StateStore store;
    private readonly AppState state;
    private readonly MainWindow? space;
    private readonly ContentControl host = new();
    private readonly Dictionary<string, UIElement> pages = [];
    private readonly Dictionary<string, Button> tabs = [];

    internal SettingsDialog(Window owner, StateStore store, AppState state, MainWindow? space, string initialSection)
    {
        this.store = store; this.state = state; this.space = space;
        Owner = owner; Title = "Nexa"; Width = 900; Height = 680; MinWidth = 700; MinHeight = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(190) }); root.ColumnDefinitions.Add(new());
        var nav = new StackPanel { Margin = new Thickness(18) };
        nav.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 30, Margin = new Thickness(10, 4, 0, 22), ToolTip = "Settings" });
        var sidebar = new Border { Child = nav, BorderThickness = new Thickness(0, 0, 1, 0) };
        sidebar.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); sidebar.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); root.Children.Add(sidebar);
        host.Margin = new Thickness(30, 24, 30, 28); Grid.SetColumn(host, 1); root.Children.Add(host);
        AddPage(nav, "Appearance", BuildAppearance()); AddPage(nav, "Session", BuildSession()); AddPage(nav,"Widgets",BuildWidgetPreferences()); AddPage(nav, "Account", ComingSoon("Account", "Optional sign-in and sync with Microsoft, Google or Apple are Coming Soon. Nexa remains offline-first.")); AddPage(nav, "Data", ComingSoon("Data", "Space browsing and email export tools are Coming Soon. Existing Import Space and Export Space commands remain available."));
        AddPage(nav,"Profile",BuildProfile()); AddPage(nav,"Integrations",BuildIntegrations());
        if(state.Workspace!=null && Continuity.Current(state.Workspace,store.UserId).PresetOrigin=="Developer mode") AddPage(nav,"Developer",BuildSoundTests());
        Content = root; SelectSection(initialSection);
    }

    private Brush Brush(string key) => (Brush)FindResource(key);
    private void AddPage(Panel nav, string name, UIElement page)
    {
        pages[name] = page; var tab = new Button { Content = name, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(0) };
        tab.Click += (_, _) => SelectSection(name); tabs[name] = tab; nav.Children.Add(tab);
    }
    internal void SelectSection(string name)
    {
        var hedy=name=="Hedy"; if(hedy) name="Integrations";
        if(!pages.ContainsKey(name)) name="Appearance";
        if (!pages.TryGetValue(name, out var page)) return;
        host.Content = page;
        foreach (var pair in tabs) { pair.Value.SetResourceReference(Control.BackgroundProperty, pair.Key == name ? "AccentBrush" : "PanelBrush"); pair.Value.SetResourceReference(Control.ForegroundProperty, pair.Key == name ? "BackgroundBrush" : "TextBrush"); }
        if(hedy) Dispatcher.BeginInvoke(new Action(()=> { hedySettingsSection?.BringIntoView(); hedySettingsSection?.Focus(); }));
    }
    private StackPanel Page(string title, string hint)
    {
        var panel = new StackPanel(); panel.Children.Add(new TextBlock { Text = title, FontSize = 26, FontWeight = FontWeights.SemiBold }); panel.Children.Add(new TextBlock { Text = hint, TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 7, 0, 22) }); return panel;
    }
    private void Field(Panel page, string label, UIElement control)
    { page.Children.Add(new TextBlock { Text = label, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 7) }); page.Children.Add(control); }
    private void SaveInteractionOptions()
    {
        if(space!=null) { space.SaveFocusPreferences(); return; }
        try { store.SaveInteractionPreferences(state); }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this,ex.Message,"Could not save preferences"); }
    }
    private UIElement BuildAppearance()
    {
        var page = Page("Appearance", "Choose Nexa’s global light mode and app palette. Space colors and backgrounds remain independent.");
        store.LoadInteractionPreferences(state);
        {
            var sounds = new CheckBox { Content = "Interface sounds", IsChecked = state.InterfaceSoundsEnabled, Margin = new Thickness(0,6,0,8) };
            var nudges = new CheckBox { Content = "Playful focus nudges after 3 minutes away (Class / Study / Focus)", IsChecked = state.FocusNudgesEnabled, Margin = new Thickness(0,6,0,8) };
            var volume = new Slider { Minimum = 0, Maximum = 1, Value = state.InterfaceSoundVolume, Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
            sounds.Click += (_, _) => { state.InterfaceSoundsEnabled = sounds.IsChecked == true; SaveInteractionOptions(); space?.PlayInterfaceSound(); };
            nudges.Click += (_, _) => { state.FocusNudgesEnabled = nudges.IsChecked == true; SaveInteractionOptions(); };
            volume.ValueChanged += (_, _) => { state.InterfaceSoundVolume = volume.Value; SaveInteractionOptions(); };
            page.Children.Add(sounds); Field(page, "Sound volume", volume); page.Children.Add(nudges);
        }
        var mode = new ComboBox { ItemsSource = Enum.GetValues<ThemeMode>(), SelectedItem = store.LoadThemeMode(), MinHeight = 40 }; Field(page, "Light mode", mode);
        var theme = new ComboBox { ItemsSource = Enum.GetValues<AppTheme>(), SelectedItem = store.LoadAppTheme(), MinHeight = 40 }; Field(page, "App theme", theme);
        var note = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 0) }; page.Children.Add(note);
        var save = new Button { Content = "Apply appearance", Style = (Style)FindResource("PrimaryButton"), MinHeight = 42, Margin = new Thickness(0, 20, 0, 0) };
        save.Click += (_, _) => { if (mode.SelectedItem is not ThemeMode m || theme.SelectedItem is not AppTheme t) return; try { store.SavePreferences(m, t); Theme.Apply(Application.Current.Resources, m, t); space?.RefreshThemeViews(); note.Text = "Appearance applied."; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { note.Text = ex.Message; } }; page.Children.Add(save); return page;
    }
    private UIElement BuildSession()
    {
        var page = Page("Session", "Settings here apply only to the current Space and its active session.");
        if (space == null)
        {
            page.Children.Add(new TextBlock { Text = "No active session", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 20, 0, 8) });
            page.Children.Add(new TextBlock { Text = "Start or resume a session to manage session-specific settings.", Foreground = Brush("MutedBrush"), TextWrapping = TextWrapping.Wrap }); return page;
        }
        var session = state.Session;
        var scroll = new StackPanel(); page.Children.Add(scroll);
        var name = new TextBox { Text = session?.Title ?? state.Draft.Title, MinHeight = 40 }; Field(scroll, "Session name", name);
        var dates = new Grid(); dates.ColumnDefinitions.Add(new()); dates.ColumnDefinitions.Add(new() { Width = new GridLength(14) }); dates.ColumnDefinitions.Add(new());
        var startDate = new DarkDatePicker { SelectedDate = session?.Start.LocalDateTime.Date ?? DateTime.Today }; var endDate = new DarkDatePicker { SelectedDate = session?.End.LocalDateTime.Date ?? DateTime.Today };
        var startTime = new ClockPicker { Value = session?.Start.LocalDateTime.TimeOfDay ?? DateTime.Now.TimeOfDay }; var endTime = new ClockPicker { Value = session?.End.LocalDateTime.TimeOfDay ?? DateTime.Now.AddHours(1).TimeOfDay };
        var start = new StackPanel(); Field(start, "Start", startDate); start.Children.Add(startTime); dates.Children.Add(start);
        var end = new StackPanel(); Field(end, "End", endDate); end.Children.Add(endTime); Grid.SetColumn(end, 2); dates.Children.Add(end); scroll.Children.Add(dates);
        var interval = new ComboBox { ItemsSource = new[] { 5, 10, 15 }, SelectedItem = session!=null && new[] { 5, 10, 15 }.Contains(session.IntervalMinutes) ? session.IntervalMinutes : 10, MinHeight = 40 }; Field(scroll, "Summary interval (minutes)", interval);
        var days = new WrapPanel(); var selectedDays = session==null || session.ScheduledDays.Count == 0 ? new List<DayOfWeek> { session?.Start.DayOfWeek ?? DateTime.Today.DayOfWeek } : session.ScheduledDays.ToList();
        foreach (var day in Enum.GetValues<DayOfWeek>().OrderBy(day => day == DayOfWeek.Sunday ? 7 : (int)day)) days.Children.Add(new CheckBox { Content = day.ToString()[..3], Tag = day, IsChecked = selectedDays.Contains(day), Margin = new Thickness(0, 0, 14, 8) }); Field(scroll, "Selected days", days);
        var apply = new Button { Content = "Apply session", Style = (Style)FindResource("PrimaryButton"), MinHeight = 42, Margin = new Thickness(0, 18, 0, 8) };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush") };
        apply.Click += (_, _) =>
        {
            space.TitleInput.Text = name.Text; space.StartDate.SelectedDate = startDate.SelectedDate; space.EndDate.SelectedDate = endDate.SelectedDate; space.StartTime.Value = startTime.Value; space.EndTime.Value = endTime.Value; space.IntervalInput.Text = interval.SelectedItem?.ToString() ?? "10";
            space.SetSessionDays(days.Children.OfType<CheckBox>().Where(box => box.IsChecked == true).Select(box => (DayOfWeek)box.Tag));
            status.Text = space.ApplyInlineSessionTimes() ? "Session updated." : space.Feedback.Text;
        }; scroll.Children.Add(apply); scroll.Children.Add(status);
        scroll.Children.Add(new Border { Height = 1, Background = Brush("LineBrush"), Margin = new Thickness(0, 22, 0, 12) });
        scroll.Children.Add(new TextBlock { Text = "Space appearance", FontSize = 20, FontWeight = FontWeights.SemiBold });
        var mainColor = new ColorPickerButton { SelectedColor = state.AccentColor }; Field(scroll, "Space main color", mainColor);
        var accentColor = new ColorPickerButton { SelectedColor = state.PixelColor }; Field(scroll, "Space accent / pixel color", accentColor);
        var background = Continuity.Clone(state.Background); var mode = new ComboBox { ItemsSource = Enum.GetValues<SpaceBackgroundMode>(), SelectedItem = background.Mode, MinHeight = 40 }; Field(scroll, "Background mode", mode);
        var color = new ColorPickerButton { SelectedColor = InputRules.IsColor(background.SolidColor) ? background.SolidColor : "#080B16" }; Field(scroll, "Solid color", color);
        var imagePath = new TextBox { Text = background.ImagePath, IsReadOnly = true, MinHeight = 40 }; var browse = new Button { Content = "Choose image", Margin = new Thickness(0, 8, 0, 0) }; browse.Click += (_, _) => { var picker = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" }; if (picker.ShowDialog(this) == true) { imagePath.Text = picker.FileName; mode.SelectedItem=SpaceBackgroundMode.CustomImage; } }; Field(scroll, "Custom image", imagePath); scroll.Children.Add(browse);
        var blur = new CheckBox { Content = "Blur background", IsChecked = background.Blur, Margin = new Thickness(0, 14, 0, 0) }; scroll.Children.Add(blur);
        var blurStrength = new Slider { Minimum = 0, Maximum = 40, Value = background.BlurStrength, TickFrequency = 2, IsSnapToTickEnabled = true }; Field(scroll, "Blur strength", blurStrength);
        var overlay = new Slider { Minimum = 0, Maximum = .9, Value = background.Overlay, TickFrequency = .05, IsSnapToTickEnabled = true }; Field(scroll, "Overlay / dimming", overlay);
        var saveBackground = new Button { Content = "Apply Space appearance", MinHeight = 42, Margin = new Thickness(0, 18, 0, 0) };
        saveBackground.Click += (_, _) => { state.AccentColor = mainColor.SelectedColor; state.PixelColor = accentColor.SelectedColor; space.RefreshThemeViews(); var value = new SpaceBackground { Mode = (SpaceBackgroundMode)(mode.SelectedItem ?? SpaceBackgroundMode.Default), SolidColor = color.SelectedColor, ImagePath = imagePath.Text, Blur = blur.IsChecked == true, BlurStrength = blurStrength.Value, Overlay = overlay.Value }; status.Text = space.SaveSpaceBackground(value) ? "Space appearance saved." : "The Space appearance could not be saved."; }; scroll.Children.Add(saveBackground);
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
    private UIElement BuildHedy()
    {
        var page = Page("Hedy", "Hedy is optional. Save its API key securely for this Windows account, or use Local Recording in Session Notes.");
        var region = new ComboBox { ItemsSource = new[] { "US", "EU" }, SelectedItem = state.HedyRegion, MinHeight = 40 }; Field(page, "Region", region);
        var key = new PasswordBox { Password = SecretStorage.Unprotect(state.HedyProtectedKey), MinHeight = 40, Padding = new Thickness(8) }; Field(page, "API key", key);
        var remember = new CheckBox { Content = "Save securely for this Windows account", IsChecked = !string.IsNullOrWhiteSpace(state.HedyProtectedKey), Margin = new Thickness(0, 14, 0, 0) }; page.Children.Add(remember);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 0) }; page.Children.Add(status);
        var save = new Button { Content = "Save Hedy", Style = (Style)FindResource("PrimaryButton"), MinHeight = 42, Margin = new Thickness(0, 18, 0, 0) };
        save.Click += (_, _) => { try { state.Workspace ??= Continuity.Migrate(state, store.UserId); state.HedyRegion = region.SelectedItem?.ToString() ?? "US"; state.HedyProtectedKey = remember.IsChecked == true ? SecretStorage.Protect(key.Password) : ""; store.Save(state); space?.RefreshHedySettings(); status.Text = "Hedy settings saved securely."; } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException) { status.Text = ex.Message; } }; page.Children.Add(save); return page;
    }
    private UIElement ComingSoon(string title, string copy)
    {
        var page = Page(title, copy); page.Children.Add(new Border { Child = new TextBlock { Text = "Coming Soon", FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center }, BorderBrush = Brush("LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(28), Margin = new Thickness(0, 22, 0, 0) }); return page;
    }
}
