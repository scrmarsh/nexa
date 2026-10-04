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
    Ember
}

public static class Theme
{
    private static ThemeMode currentMode = ThemeMode.System;
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
        _ => ("#5B7CFF", "#8B5CF6")
    };

    public static void Apply(ResourceDictionary resources, ThemeMode mode, AppTheme theme = AppTheme.Nexa)
    {
        currentMode = mode;
        currentTheme = theme;
        (currentAccent, currentPixel) = AppPalette(theme);
        ApplyPalette(resources, mode, currentAccent, currentPixel);
        if (Application.Current != null)
            foreach (Window window in Application.Current.Windows) Invalidate(window);
    }
    private static void Invalidate(DependencyObject root)
    {
        if (root is UIElement element) element.InvalidateVisual();
        var count = root is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetChildrenCount(root) : 0;
        for (var i = 0; i < count; i++) Invalidate(VisualTreeHelper.GetChild(root, i));
    }
    public static void ApplyDashboard(ResourceDictionary resources, string accent, string pixel)
    {
        var palette = new ResourceDictionary(); ApplyPalette(palette, currentMode, accent, pixel);
        foreach (string key in palette.Keys)
        {
            var name = "Dashboard" + key;
            if (resources[name] is SolidColorBrush existing && !existing.IsFrozen && palette[key] is SolidColorBrush next) existing.Color = next.Color;
            else resources[name] = palette[key];
        }
    }

    private static void SystemPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (currentMode != ThemeMode.System || Application.Current is not { } application) return;
        application.Dispatcher.BeginInvoke(new Action(() => Apply(application.Resources, currentMode, currentTheme)));
    }

    private static void ApplyPalette(ResourceDictionary resources, ThemeMode mode, string accentHex, string pixelHex)
    {
        void Set(string key, Color color)
        {
            if (resources[key] is SolidColorBrush existing && !existing.IsFrozen) existing.Color = color;
            else resources[key] = new SolidColorBrush(color);
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
