using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace SessionDashboard;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public enum AppTheme
{
    Nexa,
    Violet,
    Aurora,
    Ember,
    Amber,
    Green
}

public static class Theme
{
    internal static readonly AppTheme[] OfficialThemes=[AppTheme.Nexa,AppTheme.Violet,AppTheme.Aurora,AppTheme.Ember];
    // Preserve legacy enum storage, while exposing only the original four palettes.
    internal static AppTheme OfficialTheme(AppTheme theme)=>theme switch{AppTheme.Amber=>AppTheme.Ember,AppTheme.Green=>AppTheme.Aurora,_=>OfficialThemes.Contains(theme)?theme:AppTheme.Nexa};
    internal static bool AnimatePalette {get;private set;}
    internal static void BeginTransition(NexaOptions options)
    {
        AnimatePalette=NexaMotion.Allowed(options);
        var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
        timer.Tick+=(_,_)=>{timer.Stop();AnimatePalette=false;};timer.Start();
    }
    private static void SetColor(SolidColorBrush brush,Color color)
    {
        if((Color)brush.GetAnimationBaseValue(SolidColorBrush.ColorProperty)==color)return;
        var from=brush.Color;brush.BeginAnimation(SolidColorBrush.ColorProperty,null);brush.Color=color;
        if(AnimatePalette)brush.BeginAnimation(SolidColorBrush.ColorProperty,new System.Windows.Media.Animation.ColorAnimation(from,color,TimeSpan.FromMilliseconds(280)){FillBehavior=System.Windows.Media.Animation.FillBehavior.Stop,EasingFunction=new System.Windows.Media.Animation.CubicEase{EasingMode=System.Windows.Media.Animation.EasingMode.EaseInOut}});
    }
    internal static event EventHandler? SeasonalChanged;
    internal static bool SeasonalEnabled { get; private set; }
    internal static void SetSeasonalEnabled(bool enabled)
    {
        if (SeasonalEnabled == enabled) return;
        SeasonalEnabled = enabled;
        if (Application.Current != null) Apply(Application.Current.Resources, currentMode, currentTheme);
        SeasonalChanged?.Invoke(null,EventArgs.Empty);
    }
    private static ThemeMode currentMode = ThemeMode.System;
    internal static ThemeMode CurrentMode=>currentMode;
    internal static AppTheme CurrentTheme=>currentTheme;
    internal static bool IsLightMode => currentMode == ThemeMode.Light || (currentMode == ThemeMode.System && IsWindowsAppThemeLight());
    private static string currentAccent = "#5B7CFF";
    private static string currentPixel = "#8B5CF6";
    private static AppTheme currentTheme = AppTheme.Nexa;

    static Theme() => SystemEvents.UserPreferenceChanged += SystemPreferenceChanged;

    public static (string Accent, string Pixel) ForName(string name) => name.ToLowerInvariant() switch
    {
        "lecture" => ("#5B7CFF", "#3B82F6"),
        "focused work" => ("#7C3AED", "#8B5CF6"),
        "meeting" => ("#7C8CF7", "#A855F7"),
        "studying" => ("#C6A5FF", "#A67AF2"),
        _ => ("#5B7CFF", "#8B5CF6")
    };

    public static (string Accent, string Pixel) AppPalette(AppTheme theme) => theme switch
    {
        AppTheme.Violet => ("#A78BFA", "#7C3AED"),
        AppTheme.Aurora => ("#38BDF8", "#14B8A6"),
        AppTheme.Ember => ("#FB7185", "#F59E0B"),
        AppTheme.Amber => ("#FFCA78", "#E6A441"),
        AppTheme.Green => ("#6AE58F", "#1DB954"),
        _ => ("#5B7CFF", "#8B5CF6")
    };

    public static void Apply(ResourceDictionary resources, ThemeMode mode, AppTheme theme = AppTheme.Nexa)
    {
        var old = PaletteBrushes(resources);
        resources["SpaceIconBrush"]=new SolidColorBrush(SeasonalEnabled?Color.FromRgb(249,115,22):mode==ThemeMode.Light || mode==ThemeMode.System && IsWindowsAppThemeLight()?Color.FromRgb(47,57,99):Color.FromRgb(129,140,248));
        currentMode = mode;
        currentTheme = theme = OfficialTheme(theme);
        (currentAccent, currentPixel) = AppPalette(theme);
        ApplyPalette(resources, mode, currentAccent, currentPixel);
        var background=((SolidColorBrush)resources["BackgroundBrush"]).Color;
        resources["WindowFrameBrush"]=new SolidColorBrush(background.R+background.G+background.B>384?Color.FromRgb(186,197,209):Color.FromRgb(48,54,74));
        if (Application.Current != null)
        {
            var replacements = Replacements(old, resources);
            foreach (Window window in Application.Current.Windows)
            {
                NexaChrome.RefreshNativeFrame(window);window.Icon=NexaChrome.Icon;if(window is HomeWindow home){home.RefreshBranding();home.RefreshWallpaper();}
                Retarget(window, replacements);
                if (window is SettingsDialog settings) settings.RefreshAppearance(replacements);
                if (window is MainWindow space){space.RefreshThemeViews();space.ApplySpaceBackground();}
                Invalidate(window);
            }
        }
    }
    private static void Invalidate(DependencyObject root)
    {
        if (root is FrameworkElement element && element.GetType().Namespace == typeof(Theme).Namespace) element.InvalidateVisual();
        if(root is Canvas {Name:"PixelGrid"})return;
        var count = root is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetChildrenCount(root) : 0;
        for (var i = 0; i < count; i++) Invalidate(VisualTreeHelper.GetChild(root, i));
    }
    public static void ApplyDashboard(ResourceDictionary resources, string accent, string pixel)
    {
        var old = PaletteBrushes(resources, "Dashboard");
        var palette = new ResourceDictionary(); ApplyPalette(palette, currentMode, currentAccent, currentPixel);
        palette["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SeasonalEnabled ? "#A855F7" : accent));
        palette["AccentTextBrush"]=new SolidColorBrush(AccentText(((SolidColorBrush)palette["AccentBrush"]).Color));
        palette["PixelBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(SeasonalEnabled ? "#84CC16" : pixel));
        foreach (string key in palette.Keys)
        {
            var name = "Dashboard" + key;
            if (resources[name] is SolidColorBrush existing && !existing.IsFrozen && palette[key] is SolidColorBrush next) SetColor(existing,next.Color);
            else resources[name] = palette[key];
        }
        if (Application.Current != null)
            foreach (Window window in Application.Current.Windows) Retarget(window, Replacements(old, resources));
    }

    private static Dictionary<string, Brush> PaletteBrushes(ResourceDictionary resources, string prefix = "") =>
        new[] { "AccentBrush", "PixelBrush", "BackgroundBrush", "PanelBrush", "LineBrush", "TextBrush", "MutedBrush", "SurfaceBrush", "PixelOffBrush", "InputBrush", "AccentTextBrush", "SpaceIconBrush", "WindowFrameBrush" }
            .Where(key => resources[prefix + key] is Brush)
            .ToDictionary(key => prefix + key, key => (Brush)resources[prefix + key]);
    private static Dictionary<Brush, Brush> Replacements(Dictionary<string, Brush> before, ResourceDictionary resources) =>
        before.Where(pair => resources[pair.Key] is Brush next && !ReferenceEquals(pair.Value, next))
            .GroupBy(pair => pair.Value).ToDictionary(group => group.Key, group => (Brush)resources[group.First().Key]);
    internal static void Retarget(DependencyObject root, Dictionary<Brush, Brush> replacements)
    {
        if (replacements.Count == 0) return;
        var values = root.GetLocalValueEnumerator();
        while (values.MoveNext())
        {
            var entry = values.Current;
            if (entry.Value is Brush brush && replacements.TryGetValue(brush, out var replacement))
                root.SetValue(entry.Property, replacement);
        }
        if (root is not Visual and not System.Windows.Media.Media3D.Visual3D) return;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            Retarget(VisualTreeHelper.GetChild(root, i), replacements);
    }

    private static void SystemPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (currentMode != ThemeMode.System || Application.Current is not { } application) return;
        application.Dispatcher.BeginInvoke(new Action(() => Apply(application.Resources, currentMode, currentTheme)));
    }

    internal static Color AccentText(Color color)
    {
        static double Linear(byte value){var c=value/255d;return c<=.04045?c/12.92:Math.Pow((c+.055)/1.055,2.4);}
        var luminance=.2126*Linear(color.R)+.7152*Linear(color.G)+.0722*Linear(color.B);
        return (luminance+.05)/.05>=1.05/(luminance+.05)?Colors.Black:Colors.White;
    }
    private static void ApplyPalette(ResourceDictionary resources, ThemeMode mode, string accentHex, string pixelHex)
    {
        void Set(string key, Color color)
        {
            if (resources[key] is SolidColorBrush existing && !existing.IsFrozen) SetColor(existing,color);
            else resources[key] = new SolidColorBrush(color);
        }
        Set("AccentTextBrush",AccentText((Color)ColorConverter.ConvertFromString(accentHex)));
        if (SeasonalEnabled)
        {
            foreach (var pair in new Dictionary<string, string> {
                ["BackgroundBrush"]="#100D19", ["PanelBrush"]="#1C102A", ["SurfaceBrush"]="#291938", ["InputBrush"]="#180F24",
                ["LineBrush"]="#794D34", ["TextBrush"]="#E9D5FF", ["MutedBrush"]="#CFBADE", ["AccentBrush"]="#A855F7", ["PixelBrush"]="#84CC16", ["PixelOffBrush"]="#2B2034" })
                Set(pair.Key, (Color)ColorConverter.ConvertFromString(pair.Value));
            Set("AccentTextBrush",AccentText((Color)ColorConverter.ConvertFromString("#A855F7")));
            return;
        }
        var accent = (Color)ColorConverter.ConvertFromString(accentHex);
        var pixel = (Color)ColorConverter.ConvertFromString(pixelHex);
        var light = mode == ThemeMode.Light || (mode == ThemeMode.System && IsWindowsAppThemeLight());
        Set("AccentBrush", accent);
        Set("PixelBrush", pixel);

        if (light)
        {
            Set("BackgroundBrush", Color.FromRgb(239, 243, 248));
            Set("PanelBrush", Colors.White);
            Set("LineBrush", Color.FromRgb(185, 198, 212));
            Set("TextBrush", Color.FromRgb(29, 39, 50));
            Set("MutedBrush", Color.FromRgb(66, 82, 99));
            Set("SurfaceBrush", Color.FromRgb(222, 231, 240));
            Set("PixelOffBrush", Color.FromRgb(222, 229, 236));
            Set("InputBrush", Color.FromRgb(250, 252, 254));
            return;
        }

        Set("BackgroundBrush", Color.FromRgb((byte)(5 + accent.R * .018), (byte)(6 + accent.G * .018), (byte)(7 + accent.B * .018)));
        Set("SurfaceBrush", Color.FromRgb((byte)(14 + accent.R * .07), (byte)(14 + accent.G * .07), (byte)(14 + accent.B * .07)));
        Set("InputBrush", Color.FromRgb((byte)(7 + accent.R * .025), (byte)(7 + accent.G * .025), (byte)(7 + accent.B * .025)));
        Set("PixelOffBrush", Color.FromRgb((byte)(14 + accent.R * .04), (byte)(14 + accent.G * .04), (byte)(14 + accent.B * .04)));
        Set("PanelBrush", Color.FromRgb((byte)(8 + accent.R * .045), (byte)(10 + accent.G * .045), (byte)(10 + accent.B * .045)));
        Set("LineBrush", Color.FromRgb((byte)(24 + accent.R * .12), (byte)(24 + accent.G * .12), (byte)(24 + accent.B * .12)));
        Set("TextBrush", Color.FromRgb(240, 242, 255));
        Set("MutedBrush", Color.FromRgb(164, 172, 199));
    }

    private static bool IsWindowsAppThemeLight()
    {
        var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0);
        return value is int theme && theme != 0;
    }
}

public static class Milestones
{
    public static List<string> Collect(Session session, DateTimeOffset now)
    {
        var result = new List<string>();
        if (!session.IsRunning(now)) return result;
        void Add(string id, bool reached) { if (reached && !session.ReachedMilestones.Contains(id)) { session.ReachedMilestones.Add(id); result.Add(id); } }
        Add("HALFWAY THERE", session.Progress(now) >= .5);
        Add("THREE QUARTERS COMPLETE", session.Progress(now) >= .75);
        Add("5 MINUTES REMAINING", session.Remaining(now) <= TimeSpan.FromMinutes(5));
        return result;
    }
}
