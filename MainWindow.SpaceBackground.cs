using System.IO;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace SessionDashboard;

public partial class MainWindow
{
    internal StateStore SettingsStore => store;
    internal AppState SettingsState => state;

    internal void ApplySpaceBackground()
    {
        state.Background ??= new();
        SpaceBackgroundLayer.Effect = null;
        SpaceBackgroundLayer.Background = null;
        SpaceBackgroundOverlay.Opacity = 0;
        if (state.Background.Mode == SpaceBackgroundMode.SolidColor && InputRules.IsColor(state.Background.SolidColor))
            SpaceBackgroundLayer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(state.Background.SolidColor));
        else if (state.Background.Mode == SpaceBackgroundMode.CustomImage && File.Exists(state.Background.ImagePath))
        {
            try
            {
                var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(Path.GetFullPath(state.Background.ImagePath)); bitmap.EndInit(); bitmap.Freeze();
                SpaceBackgroundLayer.Background = new ImageBrush(bitmap) { Stretch = state.Background.FitImage?Stretch.Uniform:Stretch.UniformToFill };
                if (state.Background.Blur) SpaceBackgroundLayer.Effect = new BlurEffect { Radius = Math.Clamp(state.Background.BlurStrength, 0, 40), KernelType = KernelType.Gaussian };
                SpaceBackgroundOverlay.Opacity = Math.Clamp(state.Background.Overlay, 0, .9);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException or InvalidOperationException)
            { Feedback.Text = "The Space background image could not be loaded."; }
        }
        // Wallpaper changes do not change widget geometry or saved placement.
    }

    internal bool SaveSpaceBackground(SpaceBackground value)
    {
        state.Background = value; ApplySpaceBackground(); Changed("Space background updated."); return SaveNow();
    }
}
