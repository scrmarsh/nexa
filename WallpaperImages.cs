using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
namespace SessionDashboard;
internal static class WallpaperImages
{
    private static readonly Dictionary<string,(long Length,DateTime Written,ImageSource Image)> cache=new(StringComparer.OrdinalIgnoreCase);
    internal static ImageSource? Load(SpaceBackground? settings)
    {
        if(settings?.Mode!=SpaceBackgroundMode.CustomImage || string.IsNullOrWhiteSpace(settings.ImagePath))return null;
        try
        {
            var path=Path.GetFullPath(settings.ImagePath);var info=new FileInfo(path);
            if(!info.Exists || info.Length==0 || info.Length>80_000_000)return null;
            if(cache.TryGetValue(path,out var known) && known.Length==info.Length && known.Written==info.LastWriteTimeUtc)return known.Image;
            var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=2560;
            image.UriSource=new Uri(path);image.EndInit();image.Freeze();
            if(cache.Count>=8)cache.Remove(cache.Keys.First());cache[path]=(info.Length,info.LastWriteTimeUtc,image);return image;
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException or InvalidOperationException or FormatException){return null;}
    }
    internal static double Shade(SpaceBackground? settings,bool home,bool loaded)=>!loaded?0:home?Math.Clamp(settings?.Overlay??.42,.2,.9):settings?.Mode==SpaceBackgroundMode.CustomImage?Math.Clamp(settings.Overlay,0,.9):0;
    internal static void Apply(Border layer,SpaceBackground? settings)
    {
        layer.Effect=null;
        layer.Background=settings?.Mode==SpaceBackgroundMode.SolidColor && InputRules.IsColor(settings.SolidColor)?new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.SolidColor)):Load(settings) is {} image?new ImageBrush(image){Stretch=settings!.FitImage?Stretch.Uniform:Stretch.UniformToFill}:null;
        if(layer.Background is ImageBrush && settings?.Blur==true)layer.Effect=new System.Windows.Media.Effects.BlurEffect{Radius=Math.Clamp(settings.BlurStrength,0,40),KernelType=System.Windows.Media.Effects.KernelType.Gaussian};
    }
}
internal sealed partial class HomeWindow
{
    private readonly Border homeWallpaper=new(){IsHitTestVisible=false};
    private readonly Border homeWallpaperShade=new(){IsHitTestVisible=false,Background=Brushes.Black};
    internal void PreviewHomeWallpaper(StateStore source,SpaceBackground settings)
    {if(source.FilePath!=store.FilePath || Saved.Workspace==null)return;Saved.Workspace.Settings.HomeWallpaper=Continuity.Clone(settings);RefreshWallpaper();}
    internal void RefreshWallpaper()
    {
        var settings=Saved.Workspace?.Settings.HomeWallpaper;
        WallpaperImages.Apply(homeWallpaper,settings);
        homeWallpaperShade.Opacity=WallpaperImages.Shade(settings,true,homeWallpaper.Background!=null);
    }
}
internal sealed partial class SettingsDialog
{
    private UIElement BuildWallpaperControls(SpaceBackground source,bool home)
    {
        var settings=Continuity.Clone(source??new());var syncing=false;
        var page=new StackPanel{Margin=new Thickness(0,18,0,0),Tag=home?"HomeWallpaperControls":"SpaceWallpaperControls"};
        page.Children.Add(new TextBlock{Text=home?"Home Background Presets":"Space Background Presets",FontSize=20,FontWeight=FontWeights.SemiBold});
        var tiles=new WrapPanel();page.Children.Add(tiles);var swatches=new List<(Button Button,AppearancePalettes.Preset Preset)>();
        var modes=new ComboBox{ItemsSource=new[]{"Default","Custom image","Solid color"},Margin=new Thickness(0,10,0,8)};page.Children.Add(modes);
        var layer=new Border{IsHitTestVisible=false};var shadeLayer=new Border{Background=Brushes.Black,IsHitTestVisible=false};
        var preview=new Border{Height=125,CornerRadius=new CornerRadius(8),Margin=new Thickness(0,0,0,8),ClipToBounds=true,Tag="WallpaperPreview",Child=new Grid{Children={layer,shadeLayer}}};page.Children.Add(preview);
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};page.Children.Add(status);
        var fit=new ComboBox{ItemsSource=new[]{"Fill","Fit"}};Field(page,"Image sizing",fit);
        var actions=new WrapPanel{Margin=new Thickness(0,8,0,8)};page.Children.Add(actions);
        var color=new ColorPickerButton{SelectedColor=settings.SolidColor};Field(page,"Custom solid color",color);
        var blur=new CheckBox{Content="Blur background",IsChecked=settings.Blur,Margin=new Thickness(0,10,0,0)};page.Children.Add(blur);
        var strength=new Slider{Minimum=0,Maximum=40,Value=settings.BlurStrength};Field(page,"Blur strength",strength);
        var shade=new Slider{Minimum=.2,Maximum=.9,Value=Math.Clamp(settings.Overlay,.2,.9)};Field(page,"Contrast overlay",shade);
        void Refresh()
        {
            syncing=true;
            try
            {
                modes.SelectedIndex=settings.Mode==SpaceBackgroundMode.CustomImage?1:settings.Mode==SpaceBackgroundMode.SolidColor?2:0;fit.SelectedIndex=settings.FitImage?1:0;color.SelectedColor=settings.SolidColor;
                blur.IsChecked=settings.Blur;strength.Value=Math.Clamp(settings.BlurStrength,0,40);shade.Value=Math.Clamp(settings.Overlay,.2,.9);
                WallpaperImages.Apply(layer,settings);shadeLayer.Opacity=WallpaperImages.Shade(settings,home,layer.Background!=null);
                if(layer.Background==null)layer.SetResourceReference(Border.BackgroundProperty,"BackgroundBrush");
                status.Text=settings.Mode==SpaceBackgroundMode.CustomImage?(layer.Background is not ImageBrush?"Image unavailable. Your stored choice is kept; the theme default is shown.":Path.GetFileName(settings.ImagePath)):settings.Mode==SpaceBackgroundMode.SolidColor?"Color background: "+settings.SolidColor:"Theme default background";
                foreach(var (button,preset) in swatches){var selected=settings.Mode==SpaceBackgroundMode.SolidColor && settings.SolidColor.Equals(preset.Hex,StringComparison.OrdinalIgnoreCase);button.BorderThickness=new Thickness(selected?3:1);button.ToolTip=selected?"Selected — "+preset.Hex:preset.Hex;}
            }
            finally{syncing=false;}
        }
        void Save()
        {
            if(syncing)return;
            var captured=Continuity.Clone(settings);
            if(home)
            {
                ReliabilityOptions.HomeWallpaper=captured;
                foreach(var window in Application.Current.Windows.OfType<HomeWindow>())window.PreviewHomeWallpaper(store,captured);
            }
            else {state.Background=captured;space?.ApplySpaceBackground();}
            QueueSave(home?"home-wallpaper":"space-wallpaper",SaveState);Refresh();
        }
        foreach(var preset in AppearancePalettes.Backgrounds)
        {
            var content=new StackPanel();content.Children.Add(new Border{Width=90,Height=30,CornerRadius=new CornerRadius(5),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Hex)),Margin=new Thickness(0,0,0,6)});content.Children.Add(new TextBlock{Text=preset.Name,TextAlignment=TextAlignment.Center});
            var button=new Button{Content=content,Tag=preset,Margin=new Thickness(0,10,8,4),Padding=new Thickness(8)};System.Windows.Automation.AutomationProperties.SetName(button,preset.Name+" background");button.Click+=(_,_)=>{settings.Mode=SpaceBackgroundMode.SolidColor;settings.SolidColor=preset.Hex;Save();};swatches.Add((button,preset));tiles.Children.Add(button);
        }
        var choose=new Button{Content="Choose Image",Margin=new Thickness(0,0,8,0)};actions.Children.Add(choose);
        choose.Click+=(_,_)=>
        {
            string? path;
            try{path=WallpaperPicker.Choose(this);}
            catch(Exception ex)when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception){status.Text="The image picker could not open. Your background is unchanged: "+ex.Message;return;}
            if(path==null)return;
            var candidate=Continuity.Clone(settings);candidate.ImagePath=path;candidate.Mode=SpaceBackgroundMode.CustomImage;
            if(WallpaperImages.Load(candidate)==null){status.Text="This image could not be decoded. Your previous background is unchanged.";return;}
            settings=candidate;Save();
        };
        var remove=new Button{Content="Remove Image",Margin=new Thickness(0,0,8,0)};actions.Children.Add(remove);remove.Click+=(_,_)=>{settings.Mode=SpaceBackgroundMode.Default;Save();};
        var restore=new Button{Content="Restore Default",Margin=new Thickness(0,0,8,0)};actions.Children.Add(restore);restore.Click+=(_,_)=>{settings=new();Save();};
        modes.SelectionChanged+=(_,_)=>{if(syncing)return;settings.Mode=modes.SelectedIndex==1?SpaceBackgroundMode.CustomImage:modes.SelectedIndex==2?SpaceBackgroundMode.SolidColor:SpaceBackgroundMode.Default;Save();};
        fit.SelectionChanged+=(_,_)=>{if(syncing)return;settings.FitImage=fit.SelectedIndex==1;Save();};
        color.ColorChanged+=(_,_)=>{if(syncing)return;settings.SolidColor=color.SelectedColor;settings.Mode=SpaceBackgroundMode.SolidColor;Save();};
        blur.Click+=(_,_)=>{settings.Blur=blur.IsChecked==true;Save();};strength.ValueChanged+=(_,_)=>{if(syncing)return;settings.BlurStrength=strength.Value;Save();};shade.ValueChanged+=(_,_)=>{if(syncing)return;settings.Overlay=shade.Value;Save();};
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
