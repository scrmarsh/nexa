using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

internal sealed partial class SettingsDialog : NexaPanel
{
    private readonly StateStore store;
    private readonly AppState standaloneState;
    private readonly AppState boundState;
    private AppState state => space?.SettingsState ?? standaloneState;
    private readonly MainWindow? space;
    private readonly ContentControl host = new();
    private readonly Dictionary<string, UIElement> pages = [];
    private readonly Dictionary<string, Button> tabs = [];
    private readonly System.Windows.Threading.DispatcherTimer sessionStatusTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool sessionWasActive;
    private bool HasCurrentSession => state.Session is { StoppedAt: null } session && session.End > DateTimeOffset.Now;

    internal SettingsDialog(Window owner, StateStore store, AppState state, MainWindow? space, string initialSection)
    {
        this.store = store; standaloneState = state;
        var candidate = space ?? owner as MainWindow ?? owner.Owner as MainWindow;
        this.space = candidate != null && candidate.SettingsStore.UserId == store.UserId &&
            string.Equals(Path.GetFullPath(candidate.SettingsStore.FilePath), Path.GetFullPath(store.FilePath), StringComparison.OrdinalIgnoreCase) ? candidate : null;
        boundState = this.state;
        Owner = owner; Title = "Nexa"; Width = 900; Height = 680; MinWidth = 700; MinHeight = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new Grid(); root.ColumnDefinitions.Add(new() { Width = new GridLength(190) }); root.ColumnDefinitions.Add(new());
        var nav = new StackPanel { Margin = new Thickness(18) };
        nav.Children.Add(new TextBlock { Text = "\uE713", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 30, Margin = new Thickness(10, 4, 0, 22), ToolTip = "Settings" });
        var sidebar = new Border { Child = nav, BorderThickness = new Thickness(0, 0, 1, 0) };
        sidebar.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); sidebar.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); root.Children.Add(sidebar);
        host.Margin = new Thickness(30, 24, 30, 28); Grid.SetColumn(host, 1); root.Children.Add(host);
        sessionWasActive = HasCurrentSession;
        AddPage(nav, "Appearance", BuildAppearance()); AddPage(nav, "Session", BuildSession()); AddPage(nav, "Widgets", BuildWidgetPreferences()); AddPage(nav, "Profile", BuildProfile());
        nav.Children.Add(new Border { Height = 1, Margin = new Thickness(8, 15, 8, 18), Background = Brush("LineBrush") });
        nav.Children.Add(new TextBlock { Text = "Experimental Features", Foreground = Brush("MutedBrush"), FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(12, 0, 0, 12) });
        AddPage(nav, "Account & Data", ComingSoon("Account & Data", "Optional account and data tools are in development. Import and export commands remain available from a Space."));
        AddPage(nav, "Integrations", BuildIntegrations());
        Content = root; SelectSection(initialSection);
        InitializeAutosave();
        sessionStatusTimer.Tick += (_, _) =>
        {
            if (sessionWasActive == HasCurrentSession) return;
            sessionWasActive = HasCurrentSession; pages["Session"] = BuildSession();
            if (ReferenceEquals(host.Content, sessionStatusPage)) host.Content = pages["Session"];
            sessionStatusPage = pages["Session"];
        };
        Loaded += (_, _) => sessionStatusTimer.Start(); Closed += (_, _) => sessionStatusTimer.Stop();
    }
    private UIElement? sessionStatusPage;

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
        if (name == "Session") sessionStatusPage = page;
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
        store.SaveInteractionPreferences(state);
        if(space!=null) SaveState();
    }
    private UIElement BuildAppearance()
    {
        var page = Page("Appearance", "Choose Nexa’s global light mode and app palette. Space colors and backgrounds remain independent.");
        store.LoadInteractionPreferences(state);
        {
            var sounds = new CheckBox { Content = "Interface sounds", IsChecked = state.InterfaceSoundsEnabled, Margin = new Thickness(0,6,0,8) };
            var nudges = new CheckBox { Content = "Playful focus nudges after 3 minutes away (Class / Study / Focus)", IsChecked = state.FocusNudgesEnabled, Margin = new Thickness(0,6,0,8) };
            var volume = new Slider { Minimum = 0, Maximum = 1, Value = state.InterfaceSoundVolume, Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
            sounds.Click += (_, _) => QueueSave("sounds", () => { state.InterfaceSoundsEnabled = sounds.IsChecked == true; SaveInteractionOptions(); });
            nudges.Click += (_, _) => QueueSave("nudges", () => { state.FocusNudgesEnabled = nudges.IsChecked == true; SaveInteractionOptions(); });
            volume.ValueChanged += (_, _) => QueueSave("volume", () => { state.InterfaceSoundVolume = volume.Value; SaveInteractionOptions(); });
            page.Children.Add(sounds); Field(page, "Sound volume", volume); page.Children.Add(nudges);
        }
        var mode = new ComboBox { ItemsSource = Enum.GetValues<ThemeMode>(), SelectedItem = store.LoadThemeMode(), MinHeight = 40 }; Field(page, "Light mode", mode);
        var theme = new ComboBox { ItemsSource = Enum.GetValues<AppTheme>(), SelectedItem = store.LoadAppTheme(), MinHeight = 40 }; Field(page, "App theme", theme);
        var note = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 0) }; page.Children.Add(note);
        void SaveAppearance() => QueueSave("appearance", () =>
        {
            if (mode.SelectedItem is not ThemeMode m || theme.SelectedItem is not AppTheme t) return;
            store.SavePreferences(m, t); Theme.Apply(Application.Current.Resources, m, t); space?.RefreshThemeViews(); note.Text = "Appearance saved.";
        });
        mode.SelectionChanged += (_, _) => SaveAppearance(); theme.SelectionChanged += (_, _) => SaveAppearance();
        return page;
    }
    private UIElement BuildHedy()
    {
        var page = Page("Hedy", "Hedy is optional. Save its API key securely for this Windows account, or use Local Recording in Session Notes.");
        var region = new ComboBox { ItemsSource = new[] { "US", "EU" }, SelectedItem = state.HedyRegion, MinHeight = 40 }; Field(page, "Region", region);
        var key = new PasswordBox { Password = SecretStorage.Unprotect(state.HedyProtectedKey), MinHeight = 40, Padding = new Thickness(8) }; Field(page, "API key", key);
        var remember = new CheckBox { Content = "Save securely for this Windows account", IsChecked = !string.IsNullOrWhiteSpace(state.HedyProtectedKey), Margin = new Thickness(0, 14, 0, 0) }; page.Children.Add(remember);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 0) }; page.Children.Add(status);
        var keyEdited = false;
        void SaveHedy() => QueueSave("hedy", () =>
        {
            state.HedyRegion = region.SelectedItem?.ToString() ?? "US";
            if (keyEdited) state.HedyProtectedKey = remember.IsChecked == true ? SecretStorage.Protect(key.Password) : "";
            SaveState(); space?.RefreshHedySettings(keyEdited ? key.Password : null); keyEdited = false;
            status.Text = "Hedy settings saved. Keys are stored only when secure saving is enabled.";
        });
        region.SelectionChanged += (_, _) => SaveHedy();
        key.PasswordChanged += (_, _) => { keyEdited = true; SaveHedy(); };
        remember.Click += (_, _) => { keyEdited = true; SaveHedy(); };
        return page;
    }
    private UIElement ComingSoon(string title, string copy)
    {
        var page = Page(title, copy); page.Children.Add(new Border { Child = new TextBlock { Text = "Coming Soon", FontSize = 20, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center }, BorderBrush = Brush("LineBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Padding = new Thickness(28), Margin = new Thickness(0, 22, 0, 0) }); return page;
    }
}
