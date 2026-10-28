using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace SessionDashboard;
internal static class WallpaperImages
{
    internal static ImageSource? Load(SpaceBackground? settings)
    {
        if(settings?.Mode!=SpaceBackgroundMode.CustomImage || string.IsNullOrWhiteSpace(settings.ImagePath))return null;
        try
        {
            var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=2560;
            image.UriSource=new Uri(Path.GetFullPath(settings.ImagePath));image.EndInit();image.Freeze();return image;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException or InvalidOperationException){return null;}
    }
    internal static void Apply(Border layer,SpaceBackground? settings)
    {
        layer.Background=settings?.Mode==SpaceBackgroundMode.SolidColor && InputRules.IsColor(settings.SolidColor)?new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.SolidColor)):Load(settings) is {} image?new ImageBrush(image){Stretch=settings!.FitImage?Stretch.Uniform:Stretch.UniformToFill}:null;
    }
}
internal sealed partial class HomeWindow
{
    private readonly Border homeWallpaper=new(){IsHitTestVisible=false};
    private readonly Border homeWallpaperShade=new(){IsHitTestVisible=false,Background=Brushes.Black};
    internal void RefreshWallpaper()
    {
        var settings=Saved.Workspace?.Settings.HomeWallpaper;
        WallpaperImages.Apply(homeWallpaper,settings);
        homeWallpaperShade.Opacity=homeWallpaper.Background==null?0:Math.Clamp(settings?.Overlay??.42,.2,.9);
    }
}
internal sealed partial class SettingsDialog
{
    private UIElement BuildWallpaperControls(SpaceBackground source,bool home)
    {
        var settings=Continuity.Clone(source??new());
        var page=new StackPanel{Margin=new Thickness(0,18,0,0)};
        page.Children.Add(new TextBlock{Text=home?"Home Screen Wallpaper":"Space wallpaper",FontSize=20,FontWeight=FontWeights.SemiBold});
        var presets=new ComboBox{ItemsSource=home?new[]{"Default","Custom image"}:new[]{"Default","Custom image","Solid color"},SelectedIndex=settings.Mode==SpaceBackgroundMode.CustomImage?1:settings.Mode==SpaceBackgroundMode.SolidColor && !home?2:0,Margin=new Thickness(0,10,0,8)};page.Children.Add(presets);
        var preview=new Border{Height=125,CornerRadius=new CornerRadius(8),Margin=new Thickness(0,0,0,8)};page.Children.Add(preview);
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};page.Children.Add(status);
        var fit=new ComboBox{ItemsSource=new[]{"Fill","Fit"},SelectedIndex=settings.FitImage?1:0};Field(page,"Image sizing",fit);
        var actions=new WrapPanel{Margin=new Thickness(0,8,0,8)};page.Children.Add(actions);
        void Refresh()
        {
            WallpaperImages.Apply(preview,settings);
            status.Text=settings.Mode==SpaceBackgroundMode.CustomImage?(preview.Background==null?"Image unavailable. Default background will be used.":Path.GetFileName(settings.ImagePath)):"Existing default background";
        }
        void Save()
        {
            var captured=Continuity.Clone(settings);
            QueueSave(home?"home-wallpaper":"space-wallpaper",()=>
            {
                if(home)ReliabilityOptions.HomeWallpaper=captured;else state.Background=captured;
                SaveState();if(Owner is HomeWindow window)window.RefreshWallpaper();
            });Refresh();
        }
        var choose=new Button{Content="Choose image",Margin=new Thickness(0,0,8,0)};actions.Children.Add(choose);
        choose.Click+=(_,_)=>
        {
            var picker=new OpenFileDialog{Filter="Images|*.png;*.jpg;*.jpeg;*.bmp;*.webp"};
            if(picker.ShowDialog(this)!=true)return;
            var candidate=Continuity.Clone(settings);candidate.ImagePath=picker.FileName;candidate.Mode=SpaceBackgroundMode.CustomImage;
            if(WallpaperImages.Load(candidate)==null){status.Text="This image could not be decoded. Choose a supported, undamaged image.";return;}
            settings=candidate;presets.SelectedIndex=1;Save();
        };
        foreach(var label in new[]{"Remove custom wallpaper","Restore Default"})
        {
            var button=new Button{Content=label,Margin=new Thickness(0,0,8,0)};actions.Children.Add(button);
            button.Click+=(_,_)=>{settings=new();presets.SelectedIndex=0;fit.SelectedIndex=0;Save();};
        }
        presets.SelectionChanged+=(_,_)=>{settings.Mode=presets.SelectedIndex==1?SpaceBackgroundMode.CustomImage:presets.SelectedIndex==2?SpaceBackgroundMode.SolidColor:SpaceBackgroundMode.Default;Save();};
        fit.SelectionChanged+=(_,_)=>{settings.FitImage=fit.SelectedIndex==1;Save();};
        if(!home)
        {
            var color=new ColorPickerButton{SelectedColor=settings.SolidColor};Field(page,"Solid color",color);color.ColorChanged+=(_,_)=>{settings.SolidColor=color.SelectedColor;Save();};
            var blur=new CheckBox{Content="Blur background",IsChecked=settings.Blur,Margin=new Thickness(0,10,0,0)};page.Children.Add(blur);blur.Click+=(_,_)=>{settings.Blur=blur.IsChecked==true;Save();};
            var strength=new Slider{Minimum=0,Maximum=40,Value=settings.BlurStrength};Field(page,"Blur strength",strength);strength.ValueChanged+=(_,_)=>{settings.BlurStrength=strength.Value;Save();};
        }
        var shade=new Slider{Minimum=.2,Maximum=.9,Value=Math.Clamp(settings.Overlay,.2,.9)};Field(page,"Contrast overlay",shade);shade.ValueChanged+=(_,_)=>{settings.Overlay=shade.Value;Save();};
        Refresh();return page;
    }
    private UIElement BuildDisplaySettings()
    {
        var page=new StackPanel{Margin=new Thickness(0,18,0,0)};
        page.Children.Add(new TextBlock{Text="Display & Fullscreen",FontSize=20,FontWeight=FontWeights.SemiBold});
        void Toggle(string title,bool value,Action<bool> set)
        {
            var toggle=new CheckBox{Content=title,IsChecked=value,Margin=new Thickness(0,10,0,0)};page.Children.Add(toggle);
            toggle.Click+=(_,_)=>{set(toggle.IsChecked==true);space?.ApplyReliabilityOptions();QueueSave("display",SaveState);};
        }
        Toggle("Enable F11 shortcut",ReliabilityOptions.EnableF11,v=>ReliabilityOptions.EnableF11=v);
        Toggle("Show fullscreen notification",ReliabilityOptions.FullscreenNotification,v=>ReliabilityOptions.FullscreenNotification=v);
        Toggle("Show system indicators",ReliabilityOptions.ShowSystemIndicators,v=>ReliabilityOptions.ShowSystemIndicators=v);
        Toggle("Enable volume slider",ReliabilityOptions.EnableVolumeSlider,v=>ReliabilityOptions.EnableVolumeSlider=v);
        Toggle("Allow Esc to exit",ReliabilityOptions.EscExitsFullscreen,v=>ReliabilityOptions.EscExitsFullscreen=v);
        return page;
    }
}
