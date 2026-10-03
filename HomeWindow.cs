using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SessionDashboard;

internal sealed partial class HomeWindow : NexaDialog
{
    private readonly StateStore store;
    internal readonly AppState Saved;
    internal readonly StackPanel Recent = new();
    internal readonly Button ContinueButton;
    private readonly TextBlock feedback = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0) };
    internal readonly TextBlock AutosaveStatus = new() { Text = "✓  Autosaved", VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    internal HomeWindow(StateStore store)
    {
        this.store = store; Saved = store.Load();
        Theme.Apply(Application.Current.Resources, store.LoadThemeMode(), store.LoadAppTheme());
        if (Saved.Workspace == null && File.Exists(store.FilePath)) Saved.Workspace = Continuity.Migrate(Saved, store.UserId);
        Title = "Nexa"; Width = Saved.WindowWidth; Height = Saved.WindowHeight; MinWidth = 740; MinHeight = 550; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(60, 36, 60, 40), MaxWidth = 960 };
        BuildHomeHeader(panel);
        var actions = new System.Windows.Controls.Primitives.UniformGrid { Columns = 3, Margin = new Thickness(0, 32, 0, 0) };
        Button IconAction(string label, string asset, Color hoverColor)
        {
            var icon = new System.Windows.Shapes.Rectangle
            {
                Width = 76, Height = 76,
                Fill = (Brush)FindResource("AccentBrush"),
                OpacityMask = new ImageBrush(new BitmapImage(new Uri($"pack://application:,,,/Nexa;component/assets/{asset}"))) { Stretch = Stretch.Uniform },
                RenderTransformOrigin = new Point(.5, .5), RenderTransform = new ScaleTransform(1, 1)
            };
            var button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Height = 156,
                Margin = new Thickness(0, 0, 12, 0),
                Padding = new Thickness(18),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Children = { icon, new TextBlock { Text = label, FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 0) } } }
            };
            System.Windows.Automation.AutomationProperties.SetName(button, label);
            button.MouseEnter += (_, _) =>
            {
                icon.Fill = Brushes.White; var scale = (ScaleTransform)icon.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1, 1.1, TimeSpan.FromMilliseconds(140)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1, 1.1, TimeSpan.FromMilliseconds(140)));
            };
            button.MouseLeave += (_, _) =>
            {
                icon.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "AccentBrush"); var scale = (ScaleTransform)icon.RenderTransform;
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.1, 1, TimeSpan.FromMilliseconds(140)));
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.1, 1, TimeSpan.FromMilliseconds(140)));
            };
            return button;
        }
        var start = IconAction("Create Space", "add.png", Color.FromRgb(73, 151, 255));
        start.Click += (_, _) => NewDashboard();
        actions.Children.Add(start);
        ContinueButton = IconAction("Resume", "continue.png", Color.FromRgb(80, 205, 150));
        ContinueButton.IsEnabled = Saved.Workspace?.Dashboards.Count > 0;
        ContinueButton.Click += (_, _) => { if (Saved.Workspace != null) OpenDashboard(Saved.Workspace.Dashboards.OrderByDescending(d => d.Current.LastAccessedAt).First().DashboardId); };
        actions.Children.Add(ContinueButton);
        var import = IconAction("Import Space", "import.png", Color.FromRgb(255, 176, 75));
        import.Click += (_, _) => ImportDashboard(); actions.Children.Add(import); panel.Children.Add(actions);
        panel.Children.Add(new TextBlock { Text = "Recent Spaces", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 22, 0, 12) });
        foreach (var d in (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt))
        {
            var content = new StackPanel(); content.Children.Add(new TextBlock { Text = d.Name, FontSize = 17, FontWeight = FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text = $"{d.PresetOrigin} · Last opened {d.Current.LastAccessedAt.LocalDateTime:g}", Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 7, 0, 0) });
            var button = new Button { Content = content, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 7) };
            button.Click += (_, _) => OpenDashboard(d.DashboardId); Recent.Children.Add(button);
        }
        if (Recent.Children.Count == 0) Recent.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush") });
        panel.Children.Add(Recent);
        if (!string.IsNullOrWhiteSpace(store.Warning)) { feedback.Text = store.Warning; panel.Children.Add(feedback); var hide = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6) }; hide.Tick += (_, _) => { hide.Stop(); feedback.Visibility = Visibility.Collapsed; }; Loaded += (_, _) => hide.Start(); Closed += (_, _) => hide.Stop(); }
        var footer = new Grid { Margin = new Thickness(0, 22, 0, 0) }; footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var gear = new Button { Content = NexaIcons.Create("Settings"), FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 20, Width = 42, Height = 42, Padding = new Thickness(0), ToolTip = "Settings", RenderTransformOrigin = new Point(.5, .5), RenderTransform = new RotateTransform() };
        System.Windows.Automation.AutomationProperties.SetName(gear, "Settings"); gear.Click += (_, _) => OpenSettings(); gear.MouseEnter += (_, _) => ((RotateTransform)gear.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 90, TimeSpan.FromMilliseconds(260))); gear.MouseLeave += (_, _) => ((RotateTransform)gear.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(90, 0, TimeSpan.FromMilliseconds(260)));
        left.Children.Add(gear); AutosaveStatus.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); AutosaveStatus.Margin = new Thickness(12, 0, 0, 0); left.Children.Add(AutosaveStatus); footer.Children.Add(left);
        var utilities = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        Button Utility(string text, Action action) { var button = new Button { Content = text == "View History" ? NexaIcons.Label("History") : text, Padding = new Thickness(14, 9, 14, 9), Margin = new Thickness(8, 0, 0, 0) }; button.Click += (_, _) => action(); utilities.Children.Add(button); return button; }
        Utility("View History", OpenHistory); Utility("Changelog", OpenChangelog); Utility("About Nexa", OpenAbout); var developer = Utility("DEV", OpenDeveloperDashboard); developer.Opacity = .7;
        Grid.SetColumn(utilities, 2); footer.Children.Add(utilities); panel.Children.Add(footer);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
    private void OpenSettingsOld()
    {
        var dialog = new NexaDialog { Owner = this, Title = "Nexa", Width = 760, Height = 560, MinWidth = 620, MinHeight = 460, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(180) }); root.ColumnDefinitions.Add(new());
        var navigation = new StackPanel { Margin = new Thickness(18) };
        var cog = new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 30, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(10, 4, 0, 24), RenderTransformOrigin = new Point(.5, .5), RenderTransform = new RotateTransform() };
        cog.Loaded += (_, _) => ((RotateTransform)cog.RenderTransform).BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(7)) { RepeatBehavior = RepeatBehavior.Forever });
        navigation.Children.Add(cog);
        root.Children.Add(new Border { Child = navigation, Background = (Brush)FindResource("PanelBrush"), BorderBrush = (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(0, 0, 1, 0) });
        var host = new ContentControl { Margin = new Thickness(30) }; Grid.SetColumn(host, 1); root.Children.Add(host);
        var pages = new Dictionary<string, UIElement>(); var tabs = new Dictionary<string, Button>();
        void Select(string name) { host.Content = pages[name]; foreach (var item in tabs) item.Value.SetResourceReference(Control.BackgroundProperty, item.Key == name ? "AccentBrush" : "PanelBrush"); }
        void Tab(string name) { var b = new Button { Content = name, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(0) }; b.Click += (_, _) => Select(name); tabs[name] = b; navigation.Children.Add(b); }
        var appearance = new StackPanel(); pages["Appearance"] = appearance; Tab("Appearance");
        appearance.Children.Add(new TextBlock { Text = "Appearance", FontSize = 24, FontWeight = FontWeights.SemiBold });
        appearance.Children.Add(new TextBlock { Text = "Light mode", Margin = new Thickness(0, 22, 0, 7) });
        var mode = new ComboBox { ItemsSource = Enum.GetValues<ThemeMode>(), SelectedItem = store.LoadThemeMode(), MinHeight = 38 }; appearance.Children.Add(mode);
        appearance.Children.Add(new TextBlock { Text = "App theme", Margin = new Thickness(0, 18, 0, 7) });
        var appTheme = new ComboBox { ItemsSource = Enum.GetValues<AppTheme>(), SelectedItem = store.LoadAppTheme(), MinHeight = 38 }; appearance.Children.Add(appTheme);
        appearance.Children.Add(new TextBlock { Text = "The app theme updates headers, cards, clocks, timers, banners, buttons, menus and active highlights. Space colors remain specific to each Space.", Foreground = (Brush)FindResource("MutedBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0) });
        var error = new TextBlock { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) }; appearance.Children.Add(error);
        var save = new Button { Content = "Save appearance", Style = (Style)FindResource("PrimaryButton"), Margin = new Thickness(0, 18, 0, 0), MinHeight = 40 };
        save.Click += (_, _) => { if (mode.SelectedItem is not ThemeMode selectedMode || appTheme.SelectedItem is not AppTheme selectedTheme) return; try { store.SavePreferences(selectedMode, selectedTheme); Theme.Apply(Application.Current.Resources, selectedMode, selectedTheme); NexaChrome.FadeOutAndClose(dialog); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { error.Text = "Could not save appearance: " + ex.Message; } };
        appearance.Children.Add(save);
        var hedy = new StackPanel(); pages["Hedy"] = hedy; Tab("Hedy");
        hedy.Children.Add(new TextBlock { Text = "Hedy", FontSize = 24, FontWeight = FontWeights.SemiBold });
        hedy.Children.Add(new TextBlock { Text = "Region", Margin = new Thickness(0, 22, 0, 7) });
        var region = new ComboBox { ItemsSource = new[] { "US", "EU" }, SelectedItem = Saved.HedyRegion, MinHeight = 38 }; hedy.Children.Add(region);
        hedy.Children.Add(new TextBlock { Text = "API key", Margin = new Thickness(0, 18, 0, 7) });
        var key = new PasswordBox { Password = SecretStorage.Unprotect(Saved.HedyProtectedKey), MinHeight = 38, Padding = new Thickness(8) }; hedy.Children.Add(key);
        var remember = new CheckBox { Content = "Save securely for this Windows account", IsChecked = !string.IsNullOrWhiteSpace(Saved.HedyProtectedKey), Margin = new Thickness(0, 14, 0, 0) }; hedy.Children.Add(remember);
        var hedySave = new Button { Content = "Save Hedy", Style = (Style)FindResource("PrimaryButton"), Margin = new Thickness(0, 18, 0, 0), MinHeight = 40 };
        hedySave.Click += (_, _) => { try { Saved.HedyRegion = region.SelectedItem?.ToString() ?? "US"; Saved.HedyProtectedKey = remember.IsChecked == true ? SecretStorage.Protect(key.Password) : ""; store.Save(Saved); NexaChrome.FadeOutAndClose(dialog); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException) { error.Text = "Could not save Hedy: " + ex.Message; } };
        hedy.Children.Add(hedySave);
        dialog.Content = root; Select("Appearance"); dialog.ShowDismissible();
    }
    private void OpenSettingsLegacy()
    {
        var dialog = new NexaDialog { Owner = this, Title = "Nexa · Settings", Width = 440, Height = 300, MinWidth = 380, MinHeight = 260, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = "Appearance", FontSize = 22, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Theme", Margin = new Thickness(0, 18, 0, 6) });
        var mode = new ComboBox { ItemsSource = Enum.GetValues<ThemeMode>(), SelectedItem = store.LoadThemeMode() };
        panel.Children.Add(mode);
        panel.Children.Add(new TextBlock { Text = "System follows your Windows app theme. Light and Dark stay fixed.", Foreground = (Brush)FindResource("MutedBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
        var error = new TextBlock { Foreground = Brushes.IndianRed, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
        panel.Children.Add(error);
        var save = new Button { Content = "Save settings", Style = (Style)FindResource("PrimaryButton"), Margin = new Thickness(0, 18, 0, 0) };
        save.Click += (_, _) =>
        {
            if (mode.SelectedItem is not ThemeMode selectedMode) return;
            try
            {
                store.SaveThemeMode(selectedMode);
                Theme.Apply(Application.Current.Resources, selectedMode, store.LoadAppTheme());
                NexaChrome.FadeOutAndClose(dialog);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { error.Text = "Could not save settings: " + ex.Message; }
        };
        panel.Children.Add(save);
        dialog.Content = panel;
        dialog.ShowDialog();
    }
    internal MainWindow? OpenPreview(string? directory = null)
    {
        try
        {
            var previewStore = PreviewDashboards.CreateStore(store, directory);
            var window = new MainWindow(previewStore, store);
            NexaChrome.TransferBounds(this, window);
            Application.Current.MainWindow = window;
            window.Show();
            NexaChrome.FadeOutAndClose(this);
            return window;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        { feedback.Text = "Could not open preview: " + ex.Message; return null; }
    }
    internal void OpenDashboard(Guid id)
    {
        try
        {
            var workspace = Continuity.Clone(store.Load().Workspace!); workspace.ActiveDashboardId = id;
            Continuity.Access(Continuity.Current(workspace, store.UserId));
            var next = Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey); store.Save(next);
            ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { feedback.Text = "Could not open Space: " + ex.Message; }
    }
    private void ShowDashboard()
    {
        var window = new MainWindow(store); NexaChrome.TransferBounds(this, window); Application.Current.MainWindow = window; window.Show();
        NexaChrome.FadeOutAndClose(this);
    }
    private void NewDashboard()
    {
        var dialog = new DashboardCreation(this, store.UserId);
        if (dialog.ShowDialog() != true || dialog.Result == null) return;
        try
        {
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            workspace.Dashboards.Add(dialog.Result); workspace.ActiveDashboardId = dialog.Result.DashboardId;
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { feedback.Text = "Could not create Space: " + ex.Message; }
    }
    private void OpenDeveloperDashboard()
    {
        try
        {
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            var record = workspace.Dashboards.FirstOrDefault(d => d.PresetOrigin == "Developer mode");
            if (record == null)
            {
                var initialData = new DashboardData
                {
                    AccentColor = "#69ACFF",
                    PixelColor = "#69ACFF",
                    WidgetLayoutInitialized = true,
                    Draft = new DashboardDraft { Initialized = true, Title = "Developer mode" }
                };
                record = Continuity.NewDashboard("Developer mode", "Developer mode", initialData, store.UserId);
                workspace.Dashboards.Add(record);
            }

            var data = record.Current.Data;
            foreach (var definition in WidgetLibrary.Definitions.Where(definition => definition.Name is not ("Session controls" or "Hedy")))
            {
                if (data.Widgets.Any(widget => widget.Kind == definition.Name)) continue;
                var panelKey = definition.Name switch
                {
                    "Pixel timer" => "pixels",
                    "Session timeline" => "timeline",
                    "Notes" => "notes",
                    "Tasks" => "tasks",
                    "Hedy" => "hedy",
                    _ => ""
                };
                data.Widgets.Add(WidgetLibrary.Create(definition.Name, panelKey));
            }
            WidgetLibrary.Arrange(data.Widgets, false);

            var now = DateTimeOffset.Now.AddMinutes(-5);
            var end = now.AddMinutes(10);
            var localStart = now.LocalDateTime;
            var localEnd = end.LocalDateTime;
            data.Session = new Session
            {
                Title = "Developer mode",
                Start = now,
                End = end,
                IntervalMinutes = 5
            };
            // Already-reached milestones must not flash the entire test grid on entry.
            Milestones.Collect(data.Session, now.AddMinutes(5));
            data.Draft = new DashboardDraft
            {
                Initialized = true,
                Title = "Developer mode",
                Duration = "10",
                Interval = "5",
                StartDate = localStart.Date,
                EndDate = localEnd.Date,
                StartMinutes = localStart.Hour * 60 + localStart.Minute,
                EndMinutes = localEnd.Hour * 60 + localEnd.Minute
            };
            data.WidgetLayoutInitialized = true;
            workspace.ActiveDashboardId = record.DashboardId;
            Continuity.Access(record);
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey));
            ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            feedback.Text = "Could not open developer Space: " + ex.Message;
        }
    }
    private void ImportDashboard()
    {
        var picker = new OpenFileDialog { Filter = "Nexa Space (*.nexa)|*.nexa" }; if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 10_000_000) throw new InvalidDataException("Space file exceeds the 10 MB limit.");
            var file = Continuity.ReadExport(File.ReadAllText(picker.FileName));
            if (file.IncludesSessionData && MessageBox.Show(this, "This file includes private session data. Import that content into your Windows account?", "Import Space", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            if (workspace.Dashboards.Count == 0)
            {
                var record = Continuity.NewDashboard(file.DashboardName, file.PresetOrigin, file.Data, store.UserId);
                foreach (var widget in record.Current.Data.Widgets) widget.Id = Guid.NewGuid();
                foreach (var preset in record.Current.Data.Presets) preset.Id = Guid.NewGuid();
                workspace.Dashboards.Add(record); workspace.ActiveDashboardId = record.DashboardId;
            }
            else Continuity.Import(workspace, file, store.UserId, file.IncludesSessionData);
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException) { feedback.Text = "Import failed: " + ex.Message; }
    }
}
