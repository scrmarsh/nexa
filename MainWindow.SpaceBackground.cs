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
        WallpaperImages.Apply(SpaceBackgroundLayer,state.Background);
        SpaceBackgroundOverlay.Opacity=WallpaperImages.Shade(state.Background,false,SpaceBackgroundLayer.Background!=null);
        // Wallpaper changes do not change widget geometry or saved placement.
    }

    internal bool SaveSpaceBackground(SpaceBackground value)
    {
        state.Background = value; ApplySpaceBackground(); Changed("Space background updated."); return SaveNow();
    }
}
