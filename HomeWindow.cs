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
    internal StateStore SettingsStore=>store;
    internal AppState Saved { get; private set; }
    internal readonly StackPanel Recent = new() { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Button recentChevron = new() { Content = ">", Width = 27, Height = 54, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0), Margin = new Thickness(0, 0, 7, 0), Opacity = .55, ToolTip = "Show older spaces" };
    private readonly TextBlock feedback = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0) };
    internal HomeWindow(StateStore store,AppState? loadedState=null)
    {
        this.store = store; Saved = loadedState ?? store.Load(); store.LoadInteractionPreferences(Saved);
        Theme.SetSeasonalEnabled(NexaSeason.Active(Saved.Workspace?.Settings ?? new NexaOptions(), DateTimeOffset.UtcNow));
        Closing += (_, e) => { if (settingsWindow?.FlushPendingChanges() == false) e.Cancel = true; };
        Theme.Apply(Application.Current.Resources, store.LoadThemeMode(), store.LoadAppTheme());
        if (Saved.Workspace == null && File.Exists(store.FilePath)) Saved.Workspace = Continuity.Migrate(Saved, store.UserId);
        Title = App.VersionLabel; Width = Saved.WindowWidth; Height = Saved.WindowHeight; MinWidth = 740; MinHeight = 550; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(0, 36, 0, 20) };
        BuildHomeHeader(panel);
        var create = new Button { Content = "+ Create New Space", HorizontalAlignment = HorizontalAlignment.Center, MinWidth = 260, Margin = new Thickness(0, 28, 0, 20) };
        create.FontSize=17;create.FontWeight=FontWeights.Bold;create.MinHeight=48;
        var primary=new Grid();primary.ColumnDefinitions.Add(new());primary.ColumnDefinitions.Add(new());
        create.Click += (_, _) => NewDashboard();primary.Children.Add(create);
        var import=new Button {Content="↓ Import Space",FontSize=17,FontWeight=FontWeights.Bold,MinHeight=48,MinWidth=210,Margin=new Thickness(12,28,0,20)};
        import.Click+=(_,_)=>ImportDashboard();Grid.SetColumn(import,1);primary.Children.Add(import);create.HorizontalAlignment=HorizontalAlignment.Stretch;import.HorizontalAlignment=HorizontalAlignment.Stretch;create.MinWidth=import.MinWidth=0;create.Margin=new Thickness(4,28,4,20);import.Margin=new Thickness(4,28,4,20);panel.Children.Add(primary);
        panel.Children.Add(new TextBlock { Text = "Recent Spaces", FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 22, 0, 12) });
        recentChevron.MouseEnter += (_, _) => recentChevron.Opacity = 1;
        recentChevron.MouseLeave += (_, _) => recentChevron.Opacity = .55;
        recentChevron.Click += (_, _) => ToggleRecentSpaces();
        panel.Children.Add(Recent); RefreshRecentSpaces();
        if (!string.IsNullOrWhiteSpace(store.Warning)) { feedback.Text = store.Warning; panel.Children.Add(feedback); var hide = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6) }; hide.Tick += (_, _) => { hide.Stop(); feedback.Visibility = Visibility.Collapsed; }; Loaded += (_, _) => hide.Start(); Closed += (_, _) => hide.Stop(); }
        var footer = new Grid { Margin = new Thickness(0, 10, 0, 30) }; footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        var left = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var gearGraphic = (FrameworkElement)NexaIcons.Create("Settings"); gearGraphic.RenderTransformOrigin = new Point(.5, .5); gearGraphic.RenderTransform = new RotateTransform();
        var gear = new Button { Content = gearGraphic, Width = 42, Height = 42, Padding = new Thickness(0), ToolTip = "Settings", HorizontalContentAlignment=HorizontalAlignment.Center, VerticalContentAlignment=VerticalAlignment.Center };
        System.Windows.Automation.AutomationProperties.SetName(gear, "Settings"); gear.Click += (_, _) => OpenSettings();
        void TurnGear(double angle) => ((RotateTransform)gearGraphic.RenderTransform).BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(angle,TimeSpan.FromMilliseconds(260)) { EasingFunction=new QuadraticEase { EasingMode=EasingMode.EaseInOut } });
        gear.MouseEnter += (_, _) => TurnGear(90); gear.MouseLeave += (_, _) => TurnGear(0);
        left.Children.Add(gear);
        footer.Children.Add(left);
        var utilities = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right };
        Button Utility(string text, Action action) { var button = new Button { Content = text == "View History" ? NexaIcons.Label("History") : text, Padding = new Thickness(14, 9, 14, 9), Margin = new Thickness(8, 0, 0, 0) }; button.Click += (_, _) => action(); utilities.Children.Add(button); return button; }
        Utility("View History", OpenHistory); Utility("About nexa", OpenAbout); var developer = Utility("DEV", OpenDeveloperDashboard); developer.Opacity = .7;
        Grid.SetColumn(utilities, 2); footer.Children.Add(utilities);
        var homeLayout = new Grid { Margin = new Thickness(60, 0, 60, 20), MaxWidth = 960 };
        homeLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        homeLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        homeLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        homeLayout.Children.Add(new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Grid.SetRow(footer, 1); homeLayout.Children.Add(footer);
        var backdrop=new Grid();backdrop.Children.Add(homeWallpaper);backdrop.Children.Add(homeWallpaperShade);backdrop.Children.Add(SeasonalBackdrop.Wrap(homeLayout,()=>Saved.Workspace?.Settings??new(),permanentCorners:true,advanced:()=>store.LoadBugPreferences().Ecosystem?.AdvancedAnimations??false));Content=backdrop;RefreshWallpaper();
    }

    internal void RefreshCompanionOptions(StateStore source,EcosystemOptions options)
    {
        if(source.FilePath!=store.FilePath)return;
        void Visit(DependencyObject node)
        {
            if(node is SeasonalCorners corners){corners.AdvancedAnimations=options.AdvancedAnimations;corners.InvalidateVisual();}
            for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)Visit(VisualTreeHelper.GetChild(node,i));
        }
        Visit(this);
    }

    private void OpenSettingsOld()
    {
        var dialog = new NexaDialog { Owner = this, Title = "nexa", Width = 760, Height = 560, MinWidth = 620, MinHeight = 460, WindowStartupLocation = WindowStartupLocation.CenterOwner };
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
        var dialog = new NexaDialog { Owner = this, Title = "nexa · Settings", Width = 440, Height = 300, MinWidth = 380, MinHeight = 260, WindowStartupLocation = WindowStartupLocation.CenterOwner };
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
        if (!FlushSettingsForNavigation()) return null;
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
    internal async void OpenDashboard(Guid id)
    {
        if (!FlushSettingsForNavigation(refresh:false)) return;
        await NavigateDashboard(()=>
        {
            var latest=store.Load();var workspace=latest.Workspace??throw new InvalidOperationException("Missing workspace");workspace.ActiveDashboardId=id;
            Continuity.Access(Continuity.Current(workspace,store.UserId));
            var next=Continuity.Activate(workspace,store.UserId,latest.HedyRegion,latest.HedyProtectedKey);return next;
        });
    }
    private async void ShowDashboard()=>await NavigateDashboard(()=>store.Load());
    private async Task NavigateDashboard(Func<AppState> prepare)
    {
        try { await NexaNavigation.Change(this,async ()=>{
            var preparation=await Task.Run(()=>{var watch=System.Diagnostics.Stopwatch.StartNew();var next=prepare();var model=watch.Elapsed.TotalMilliseconds;var load=store.LastLoadMilliseconds;watch.Restart();next.Workspace!.CleanShutdown=false;next.Workspace.LiveEcosystemGeneration=Guid.NewGuid();store.Save(next);return(next,model,load,save:watch.Elapsed.TotalMilliseconds);});
            var window=new MainWindow(store,loadedState:preparation.next,entryAlreadyPersisted:true);
            window.ConstructionProfile["Data loading"]=preparation.load;window.ConstructionProfile["Space model preparation"]=Math.Max(0,preparation.model-preparation.load);window.ConstructionProfile["Entry persistence"]=preparation.save;return window;
        },Saved.Workspace?.Settings??new()); }
        catch(OperationCanceledException) when(Dispatcher.HasShutdownStarted) { }
        catch(Exception ex) when(HistoryArchive.Recoverable(ex)) { await Dispatcher.InvokeAsync(()=>feedback.Text="Could not open Space: "+ex.Message); }
    }
    private void NewDashboard()
    {
        if (!FlushSettingsForNavigation()) return;
        var dialog = new DashboardCreation(this, store.UserId, store.LoadProfile());
        if (dialog.ShowDialog() != true || dialog.Result == null) return;
        try
        {
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            workspace.Dashboards.Add(dialog.Result); workspace.ActiveDashboardId = dialog.Result.DashboardId;
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { feedback.Text = "Could not create Space: " + ex.Message; }
    }
    private async void OpenDeveloperDashboard()
    {
        if (!FlushSettingsForNavigation(refresh:false)) return;
        await NavigateDashboard(()=>
        {
            var latest=store.Load();
            var workspace = latest.Workspace ?? new WorkspaceDocument { UserId = store.UserId };
            var record = workspace.Dashboards.FirstOrDefault(d => d.PresetOrigin == "Developer mode");
            var created=record==null;
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
            // Older Developer Spaces may contain Tasks/Agenda and other retired kinds.
            // Normalize them before checking which current widgets are missing so a
            // second linked tasks panel is never created.
            WidgetLibrary.PrepareLayout(data);
            foreach (var definition in WidgetLibrary.Definitions.Where(definition => definition.Name is not ("Session controls" or "Hedy")))
            {
                if (data.Widgets.Any(widget => widget.Kind == definition.Name)) continue;
                var panelKey = definition.Name switch
                {
                    "Pixel timer" => "pixels",
                    "Session timeline" => "timeline",
                    "Notes" => "notes",
                    "Tasks" or "Task List" => "tasks",
                    "Hedy" => "hedy",
                    _ => ""
                };
                data.Widgets.Add(WidgetLibrary.Create(definition.Name, panelKey));
            }
            if(created) WidgetLibrary.Arrange(data.Widgets, false);
            if(created)
            {

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
            }
            data.WidgetLayoutInitialized = true;
            workspace.ActiveDashboardId = record.DashboardId;
            Continuity.Access(record);
            var next=Continuity.Activate(workspace, store.UserId, latest.HedyRegion, latest.HedyProtectedKey);return next;
        });
    }
    private void ImportDashboard()
    {
        if (!FlushSettingsForNavigation()) return;
        var picker = new OpenFileDialog { Filter = "nexa Space (*.nexa)|*.nexa" }; if (picker.ShowDialog(this) != true) return;
        try
        {
            if (new FileInfo(picker.FileName).Length > 10_000_000) throw new InvalidDataException("Space file exceeds the 10 MB limit.");
            var file = Continuity.ReadExport(File.ReadAllText(picker.FileName));
            if (file.IncludesSessionData && MessageBox.Show(this, "This file includes private session data. Import that content into your Windows account?", "Import Space", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
            var workspace = Saved.Workspace == null ? new WorkspaceDocument { UserId = store.UserId } : Continuity.Clone(Saved.Workspace);
            Continuity.Import(workspace, file, store.UserId, file.IncludesSessionData);
            store.Save(Continuity.Activate(workspace, store.UserId, Saved.HedyRegion, Saved.HedyProtectedKey)); ShowDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidOperationException) { feedback.Text = "Import failed: " + ex.Message; }
    }
}
