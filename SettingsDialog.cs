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
    private readonly Dictionary<string, Func<UIElement>> pageFactories = [];
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
        var navigationDock=new DockPanel();
        var sidebar = new Border { Child = navigationDock, BorderThickness = new Thickness(0, 0, 1, 0) };
        sidebar.SetResourceReference(Border.BackgroundProperty, "PanelBrush"); sidebar.SetResourceReference(Border.BorderBrushProperty, "LineBrush"); root.Children.Add(sidebar);
        host.Margin = new Thickness(30, 24, 30, 28); Grid.SetColumn(host, 1); root.Children.Add(host);
        sessionWasActive = HasCurrentSession;
        SessionInterface=initialSection is "Session" or "Tips";
        if(SessionInterface)
        {
            sidebar.Visibility = Visibility.Collapsed; root.ColumnDefinitions[0].Width = new GridLength(0);
            pages["Session"] = BuildSession();
        }
        else
        {
            nav.Children.Add(new TextBlock { Text = "General", FontWeight = FontWeights.Bold, Margin = new Thickness(10, 0, 0, 12) });
            AddPage(nav,"Appearance",BuildAppearancePage);
            AddPage(nav,"Profile",BuildProfile);
            AddPage(nav,"Sounds",BuildSounds);AddPage(nav,"Time Zone & Clock",BuildTimeZoneClock);
            AddPage(nav,"Widgets",BuildWidgetPreferences);
            AddPage(nav,"Integrations",()=>BuildIntegrations());AddPage(nav,"Updates",BuildUpdates);
            AddPage(nav,"Add Bugs",BuildBugs);
            var bottom=new StackPanel {Margin=new Thickness(18,8,18,18)};DockPanel.SetDock(bottom,Dock.Bottom);navigationDock.Children.Add(bottom);
            AddPage(bottom,"Experimental Features",BuildExperimental);

        }
        navigationDock.Children.Add(new ScrollViewer {Content=nav,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Content = root; SelectSection(initialSection);
        InitializeAutosave();
        sessionStatusTimer.Tick += (_, _) =>
        {
            if (!pages.ContainsKey("Session")) return;
            if (sessionWasActive == HasCurrentSession) return;
            sessionWasActive = HasCurrentSession; pages["Session"] = BuildSession();
            if (ReferenceEquals(host.Content, sessionStatusPage)) host.Content = pages["Session"];
            sessionStatusPage = pages["Session"];
        };
        Loaded += (_, _) => sessionStatusTimer.Start(); Closed += (_, _) => sessionStatusTimer.Stop();
    }
    private UIElement? sessionStatusPage;

    internal void RefreshAppearance(Dictionary<Brush, Brush> replacements)
    {
        foreach (var page in pages.Values) Theme.Retarget(page, replacements);
    }

    private Brush Brush(string key) => (Brush)FindResource(key);
    private void AddPage(Panel nav, string name, Func<UIElement> factory)
    {
        pageFactories[name] = factory; var tab = new Button { Content = name, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 8), BorderThickness = new Thickness(0) };
        tab.Click += (_, _) => SelectSection(name); tabs[name] = tab; nav.Children.Add(tab);
    }
    internal void SelectSection(string name)
    {
        if (!FlushPendingChanges()) return;
        var tips=name=="Tips";if(tips)name="Session";
        var hedy=name=="Hedy"; if(hedy) name="Integrations";
        if(!pages.ContainsKey(name) && !pageFactories.ContainsKey(name)) name=SessionInterface?"Session":"Appearance";
        if(!pages.ContainsKey(name) && pageFactories.TryGetValue(name,out var factory)) pages[name]=factory();
        if (!pages.TryGetValue(name, out var page)) return;
        host.Content = page; NexaMotion.Entrance(host,ReliabilityOptions);
        if (name == "Session") sessionStatusPage = page;
        foreach (var pair in tabs) { pair.Value.SetResourceReference(Control.BackgroundProperty, pair.Key == name ? "AccentBrush" : "PanelBrush"); pair.Value.SetResourceReference(Control.ForegroundProperty, pair.Key == name ? "BackgroundBrush" : "TextBrush"); }
        if(tips)FocusTips();
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
        var selectedMode = store.LoadThemeMode(); var selectedTheme = store.LoadAppTheme();
        var modes = new WrapPanel(); var themes = new WrapPanel();
        Field(page, "Light mode", modes); Field(page, "App theme", themes);
        var note = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("MutedBrush"), Margin = new Thickness(0, 12, 0, 0) }; page.Children.Add(note);
        void SaveAppearance()
        {
            Theme.Apply(Application.Current.Resources, selectedMode, selectedTheme); space?.RefreshThemeViews();
            var modeToSave = selectedMode; var themeToSave = selectedTheme;
            QueueSave("appearance", () => { store.SavePreferences(modeToSave, themeToSave); note.Text = "Appearance saved."; });
        }
        void Choice(Panel group, string label, bool selected, Action choose)
        {
            var option = new RadioButton { Content = label, IsChecked = selected, Margin = new Thickness(0, 0, 18, 8),
                Padding = new Thickness(4, 8, 4, 8), VerticalContentAlignment = VerticalAlignment.Center };
            option.SetResourceReference(Control.ForegroundProperty, "TextBrush");
            option.Checked += (_, _) => { choose(); SaveAppearance(); };
            group.Children.Add(option);
        }
        foreach (var value in Enum.GetValues<ThemeMode>()) Choice(modes, value.ToString(), value == selectedMode, () => selectedMode = value);
        foreach (var value in Enum.GetValues<AppTheme>()) Choice(themes, value.ToString(), value == selectedTheme, () => selectedTheme = value);
        var effects=(StackPanel)BuildEffects();foreach(var child in effects.Children.Cast<UIElement>().ToArray()){effects.Children.Remove(child);page.Children.Add(child);}
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
