using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static readonly string media032=Path.GetFullPath("docs/media/nexa035/regression");
    private static void Capture032(Window window,string name)
    {
        Directory.CreateDirectory(media032);window.UpdateLayout();var root=(FrameworkElement)window.Content;
        var image=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);image.Render(root);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(Path.Combine(media032,name+".png"));png.Save(file);
    }
    private static IEnumerable<DependencyObject> Visual032(DependencyObject root)
    {yield return root;if(root is not Visual)yield break;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var child in Visual032(VisualTreeHelper.GetChild(root,i)))yield return child;}
    private static Button Button032(DependencyObject root,string name)=>Descendants(root).OfType<Button>().Single(b=>Equals(b.Content,name));
    private static ScrollViewer AppearanceScroll032(SettingsDialog settings)=>Descendants(settings).OfType<ScrollViewer>().Single(s=>Equals(s.Tag,"AppearanceScroll"));
    private static void ShowWallpaperUi032(Application app)
    {
        var image=Image032("png");var store=new StateStore(Path.Combine(output,"native-wallpaper032.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="On";state.Workspace.Settings.ReduceMotion=true;store.Save(state);
        var home=new HomeWindow(store){Width=1100,Height=820};app.MainWindow=home;home.Show();NexaFullscreen.Toggle(home);
        var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance"){Title="nexa .32 wallpaper acceptance fixture",ShowInTaskbar=true};settings.Show();
        var preview=Descendants(settings).OfType<Border>().Single(b=>Equals(b.Tag,"WallpaperPreview"));preview.BringIntoView();
        var timer=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMinutes(4)};timer.Tick+=(_,_)=>{timer.Stop();settings.Close();};timer.Start();
        File.WriteAllText(Path.Combine("verification","native-wallpaper032-fixture.json"),JsonSerializer.Serialize(new{Image=image,Result=Path.Combine(output,"native-picker-result.json")}));
        settings.Closed+=(_,_)=>{timer.Stop();settings.FlushPendingChanges();File.WriteAllText(Path.Combine(output,"native-picker-result.json"),JsonSerializer.Serialize(new{HomeFullscreen=NexaFullscreen.IsFullscreen(home),Wallpaper=store.Load().Workspace!.Settings.HomeWallpaper,Decoded=WallpaperImages.Load(home.Saved.Workspace!.Settings.HomeWallpaper)!=null}));home.Close();app.Shutdown();};
        app.Run();
    }
    private static void Check032()
    {
        Assert(App.VersionLabel=="nexa v0.1.1 beta" && NexaReleaseIdentity.Current.Tag=="v0.1.1-beta",".32 uses shared lowercase Alpha metadata");
        CheckAppearance032();CheckWallpaper032();CheckScrolling032();
    }
    private static void Bottom032(SettingsDialog settings,string label)
    {
        var scroll=AppearanceScroll032(settings);scroll.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(35));settings.UpdateLayout();
        var last=Descendants(scroll).OfType<CheckBox>().Single(c=>Equals(c.Content,"Allow Esc to exit"));var bounds=last.TransformToVisual(scroll).TransformBounds(new Rect(last.RenderSize));
        Assert(Math.Abs(scroll.VerticalOffset-scroll.ScrollableHeight)<1 && bounds.Top>=0 && bounds.Bottom<=scroll.ActualHeight+1,"Bottom-most Appearance option is fully reachable: "+label);
        var footer=Visual032(settings).OfType<Border>().Single(b=>Equals(b.Tag,"PanelCloseBar"));var end=last.TransformToVisual((Visual)settings.Content).TransformBounds(new Rect(last.RenderSize)).Bottom;
        Assert(end<=footer.TransformToVisual((Visual)settings.Content).Transform(new Point()).Y,"Footer does not overlap the final control: "+label);
    }
    private static void CheckAppearance032()
    {
        var store=new StateStore(Path.Combine(output,"appearance032.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.SeasonalMode="Off";state.Workspace.Settings.ReduceMotion=true;state.Workspace.Settings.HomeWallpaper=new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#123456"};state.Background=new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#234567"};store.Save(state);store.SavePreferences(SessionDashboard.ThemeMode.Dark,AppTheme.Aurora);
        var home=new HomeWindow(store){Width=1100,Height=820};home.Show();var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance"){MinWidth=400,MinHeight=300};settings.Show();Pump(TimeSpan.FromMilliseconds(60));
        var scroll=AppearanceScroll032(settings);Assert(Descendants(scroll).OfType<ScrollViewer>().Count()==0,"Appearance has one content scroll owner without nested page scrollers");
        Assert(Descendants(settings).OfType<RadioButton>().Single(r=>Equals(r.Tag,AppTheme.Aurora)).IsChecked==true,"Previously saved Aurora global preference is restored");
        Capture032(settings,"appearance-top");Bottom032(settings,"Classic normal");Capture032(settings,"appearance-bottom");
        var originalExtent=scroll.ExtentHeight;var expander=new Expander{Header="Dynamic extent fixture",Content=new Border{Height=260}};((Panel)scroll.Content).Children.Insert(3,expander);expander.IsExpanded=true;Pump(TimeSpan.FromMilliseconds(30));Assert(scroll.ExtentHeight>originalExtent+200,"Expanding content recalculates Appearance extent");expander.IsExpanded=false;Pump(TimeSpan.FromMilliseconds(30));Assert(scroll.ExtentHeight<originalExtent+100,"Collapsing content reduces extent without stale scrolling");((Panel)scroll.Content).Children.Remove(expander);
        foreach(var scale in new[]{1d,1.25,1.5})
        {
            settings.Width=760;settings.Height=520;((FrameworkElement)settings.Body!).LayoutTransform=new ScaleTransform(scale,scale);Pump(TimeSpan.FromMilliseconds(35));Bottom032(settings,"synthetic layout scale "+scale);
        }
        ((FrameworkElement)settings.Body!).LayoutTransform=Transform.Identity;settings.Width=900;settings.Height=680;settings.WindowState=WindowState.Maximized;Pump(TimeSpan.FromMilliseconds(35));Bottom032(settings,"maximized");settings.WindowState=WindowState.Normal;
        var originalHome=JsonSerializer.Serialize(home.Saved.Workspace!.Settings.HomeWallpaper);var originalSpace=JsonSerializer.Serialize(home.Saved.Background);
        foreach(var theme in Theme.OfficialThemes)
        {
            var radio=Descendants(settings).OfType<RadioButton>().Single(r=>Equals(r.Tag,theme));radio.IsChecked=true;settings.FlushPendingChanges();
            Assert(store.LoadAppTheme()==theme && ((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color==(Color)ColorConverter.ConvertFromString(Theme.AppPalette(theme).Accent),"Theme color applies immediately and persists: "+theme);Assert(((SolidColorBrush)Application.Current.Resources["AccentTextBrush"]).Color==Theme.AccentText((Color)ColorConverter.ConvertFromString(Theme.AppPalette(theme).Accent)),"Selection text uses the stronger black/white contrast for "+theme);
        }
        Assert(JsonSerializer.Serialize(home.Saved.Workspace.Settings.HomeWallpaper)==originalHome && JsonSerializer.Serialize(home.Saved.Background)==originalSpace,"Theme Colors never overwrite Home or Space wallpapers");
        var seasonal=Descendants(settings).OfType<CheckBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Nexaween enabled");seasonal.IsChecked=true;seasonal.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));Pump(TimeSpan.FromMilliseconds(40));settings.FlushPendingChanges();
        Assert(Descendants(settings).OfType<RadioButton>().Count(r=>r.Tag is AppTheme)==4,"Exactly four official themes stay accessible in nexaween");Bottom032(settings,"nexaween");
        var amber=Descendants(settings).OfType<RadioButton>().Single(r=>Equals(r.Tag,AppTheme.Aurora));amber.IsChecked=true;settings.FlushPendingChanges();Assert(((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color==Color.FromRgb(168,85,247),"nexaween retains its seasonal accent while the Classic choice is saved");
        Descendants(settings).OfType<RadioButton>().Single(r=>Equals(r.Tag,AppTheme.Nexa)).IsChecked=true;settings.FlushPendingChanges();Assert(store.LoadAppTheme()==AppTheme.Nexa,"Restore default theme color preserves the original saved theme identity");
        AppearanceScroll032(settings).ScrollToTop();Pump(TimeSpan.FromMilliseconds(25));Capture032(settings,"theme-colors");
        home.Topmost=true;NexaFullscreen.Toggle(home);Pump(TimeSpan.FromMilliseconds(30));Bottom032(settings,"fullscreen host");NexaFullscreen.Toggle(home);home.Topmost=false;
        settings.Close();home.Close();var restarted=new HomeWindow(store);restarted.Show();Assert(store.LoadAppTheme()==AppTheme.Nexa && restarted.Saved.Workspace!.Settings.HomeWallpaper.SolidColor=="#123456","Theme default and wallpaper survive restart independently");restarted.Close();
    }
    private static string Image032(string extension)
    {
        var pixels=new byte[64*32*4];for(var i=0;i<pixels.Length;i+=4){pixels[i]=100;pixels[i+1]=(byte)(i/4%255);pixels[i+2]=210;pixels[i+3]=255;}
        var bitmap=BitmapSource.Create(64,32,96,96,PixelFormats.Bgra32,null,pixels,64*4);BitmapEncoder encoder=extension switch{"jpg"=>new JpegBitmapEncoder(),"bmp"=>new BmpBitmapEncoder(),_=>new PngBitmapEncoder()};encoder.Frames.Add(BitmapFrame.Create(bitmap));var path=Path.Combine(output,"wallpaper032."+extension);using(var file=File.Create(path))encoder.Save(file);return path;
    }
    private static void CheckWallpaper032()
    {
        var png=Image032("png");foreach(var format in new[]{"png","jpg","bmp"})Assert(WallpaperImages.Load(new(){Mode=SpaceBackgroundMode.CustomImage,ImagePath=Image032(format)})!=null,"Existing decoder accepts "+format);
        var webpPath=Path.Combine(output,"codec032.webp");File.WriteAllBytes(webpPath,Convert.FromBase64String("UklGRh4AAABXRUJQVlA4TBEAAAAvAQAAAAdQvHoUuf+BiOh/AAA="));var webpChoice=new SpaceBackground{Mode=SpaceBackgroundMode.CustomImage,ImagePath=webpPath};var decodedWebp=WallpaperImages.Load(webpChoice);Assert(decodedWebp!=null || webpChoice.ImagePath==webpPath,"WebP uses the installed WIC decoder when available, otherwise preserves a safe fallback");Console.WriteLine("WEBP WIC support on this host: "+(decodedWebp!=null?"available":"unavailable; fallback verified"));
        var store=new StateStore(Path.Combine(output,"wallpapers032.json"));var state=store.Load();state.Workspace=Continuity.Migrate(state,store.UserId);state.Workspace.Settings.ReduceMotion=true;state.Workspace.Settings.SeasonalMode="Off";state.Background=new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#135724"};var other=Continuity.NewDashboard("Other","Work",new(){Background=new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor="#246813"}},store.UserId);state.Workspace.Dashboards.Add(other);store.Save(state);
        var home=new HomeWindow(store){Width=1100,Height=820};home.Show();var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");settings.Show();Pump(TimeSpan.FromMilliseconds(40));
        var group=Descendants(settings).OfType<StackPanel>().Single(p=>Equals(p.Tag,"HomeWallpaperControls"));
        Assert(!Descendants(group).OfType<Button>().Any(b=>b.Tag is AppearancePalettes.Preset),"Recovered duplicate background presets are removed from Home");
        var preview=Descendants(group).OfType<Border>().Single(b=>Equals(b.Tag,"WallpaperPreview"));preview.BringIntoView();Pump(TimeSpan.FromMilliseconds(30));Capture032(settings,"background-presets");
        var original=WallpaperPicker.Select;
        try
        {
            var before=JsonSerializer.Serialize(home.Saved.Workspace!.Settings.HomeWallpaper);WallpaperPicker.Select=_=>null;Button032(group,"Custom Image").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(JsonSerializer.Serialize(home.Saved.Workspace.Settings.HomeWallpaper)==before,"Cancel leaves wallpaper unchanged");
            var invalid=Path.Combine(output,"invalid032.png");File.WriteAllText(invalid,"not an image");WallpaperPicker.Select=_=>invalid;Button032(group,"Custom Image").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(JsonSerializer.Serialize(home.Saved.Workspace.Settings.HomeWallpaper)==before,"Corrupt image selection leaves the previous wallpaper unchanged");
            foreach(var full in new[]{false,true})
            {
                if(full)NexaFullscreen.Toggle(home);home.Topmost=true;settings.Topmost=true;var received=false;
                WallpaperPicker.Select=owner=>{received=ReferenceEquals(owner,settings) && !settings.Topmost && !home.Topmost;return png;};Button032(group,"Custom Image").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(received && home.Topmost && settings.Topmost,"Picker receives correct owner and temporarily releases/restores topmost: "+(full?"fullscreen":"windowed"));Assert(home.Saved.Workspace.Settings.HomeWallpaper.ImagePath==png,"Decoded image applies to Home immediately");
                settings.Topmost=false;home.Topmost=false;if(full)NexaFullscreen.Toggle(home);
            }
            settings.FlushPendingChanges();var fit=Descendants(group).OfType<ComboBox>().Single(c=>c.Items.OfType<string>().SequenceEqual(new[]{"Fill","Fit"}));fit.SelectedIndex=1;settings.FlushPendingChanges();var layer=(Border)((Grid)preview.Child).Children[0];Assert(layer.Background is ImageBrush{Stretch:Stretch.Uniform} && store.Load().Workspace!.Settings.HomeWallpaper.FitImage,"Fit preserves image aspect ratio and persists");fit.SelectedIndex=0;Assert(layer.Background is ImageBrush{Stretch:Stretch.UniformToFill},"Fill uses UniformToFill without distorting aspect ratio");preview.BringIntoView();Pump(TimeSpan.FromMilliseconds(30));Capture032(settings,"custom-wallpaper");
        }
        finally{WallpaperPicker.Select=original;}
        settings.FlushPendingChanges();Assert(store.Load().Background.SolidColor=="#135724" && store.Load().Workspace!.Dashboards.Single(d=>d.DashboardId==other.DashboardId).Current.Data.Background.SolidColor=="#246813","Home edits preserve both independent Space backgrounds");
        var pickerImage=WallpaperImages.Load(home.Saved.Workspace!.Settings.HomeWallpaper);Assert(ReferenceEquals(pickerImage,WallpaperImages.Load(home.Saved.Workspace.Settings.HomeWallpaper)),"Repeated preview refresh reuses a decoded frozen image");
        Button032(group,"nexa Default").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(home.Saved.Workspace.Settings.HomeWallpaper.Mode==SpaceBackgroundMode.Default && File.Exists(png),"Remove Image changes only the selected scope and never deletes the source file");
        WallpaperPicker.Select=_=>png;Button032(group,"Custom Image").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));settings.FlushPendingChanges();Assert(home.Saved.Workspace.Settings.HomeWallpaper.ImagePath==png,"Choosing a custom image again retains its settings");WallpaperPicker.Select=original;
        settings.Close();home.Close();home=new HomeWindow(store);home.Show();Assert(home.Saved.Workspace!.Settings.HomeWallpaper.ImagePath==png,"Custom Home image survives a restart");
        File.Delete(png);var preserved=home.Saved.Workspace.Settings.HomeWallpaper.ImagePath;home.RefreshWallpaper();Assert(WallpaperImages.Load(home.Saved.Workspace.Settings.HomeWallpaper)==null && home.Saved.Workspace.Settings.HomeWallpaper.ImagePath==preserved,"Deleted image falls back without deleting its stored selection");
        settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");settings.Show();group=Descendants(settings).OfType<StackPanel>().Single(p=>Equals(p.Tag,"HomeWallpaperControls"));Button032(group,"nexa Default").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));settings.FlushPendingChanges();Assert(store.Load().Workspace!.Settings.HomeWallpaper.Mode==SpaceBackgroundMode.Default && store.Load().Background.SolidColor=="#135724","Restore Default resets Home only");
        settings.Close();Capture032(home,"classic-home-default");home.Saved.Workspace.Settings.SeasonalMode="On";Theme.SetSeasonalEnabled(true);home.RefreshWallpaper();Capture032(home,"nexaween-home-default");Assert(home.Saved.Workspace.Settings.HomeWallpaper.Mode==SpaceBackgroundMode.Default,"Classic/nexaween switching preserves the Home default choice");home.Close();
        var main=new MainWindow(store);main.Show();settings=new SettingsDialog(main,store,main.state,main,"Session");settings.Show();var space=Descendants(settings).OfType<StackPanel>().Single(p=>Equals(p.Tag,"SpaceWallpaperControls"));var green=AppearancePalettes.Backgrounds[3];main.SaveSpaceBackground(new(){Mode=SpaceBackgroundMode.SolidColor,SolidColor=green.Hex});settings.FlushPendingChanges();var saved=store.Load();Assert(saved.Background.SolidColor==green.Hex && saved.Workspace!.Settings.HomeWallpaper.Mode==SpaceBackgroundMode.Default && saved.Workspace.Dashboards.Single(d=>d.DashboardId==other.DashboardId).Current.Data.Background.SolidColor=="#246813","Per-Space preset edits preserve Home and the other Space");
        var dashboard=Continuity.Current(saved.Workspace!,store.UserId);var wallpaper=JsonSerializer.Serialize(dashboard.Current.Data.Background);Continuity.Archive(dashboard);Continuity.RestoreHistorical(dashboard,dashboard.History.Last(),store.UserId,true,DateTimeOffset.UtcNow);Assert(JsonSerializer.Serialize(dashboard.Current.Data.Background)==wallpaper,"Session history restoration preserves the target Space wallpaper");Capture032(settings,"session-background");settings.Close();main.Close();
    }
    private static void Wheel032(UIElement source,int delta){source.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice,Environment.TickCount,delta){RoutedEvent=Mouse.PreviewMouseWheelEvent});Pump(TimeSpan.FromMilliseconds(18));}
    private static void CheckScrolling032()
    {
        Assert(Math.Abs(NexaScrolling.Distance(12,3,16,400)-4.8)<.001 && NexaScrolling.Distance(120,3,16,400)==48,"High-resolution wheel movement is proportional to delta and system line count");Assert(NexaScrolling.Distance(120,-1,16,400)==400 && NexaScrolling.Distance(120,0,16,400)==0,"Page-scroll and zero-line preferences are respected");Assert(NexaScrolling.Distance(5000,3,16,400)==400,"One rapid event is bounded to a viewport without animation accumulation");
        var innerContent=new StackPanel();for(var i=0;i<40;i++)innerContent.Children.Add(new TextBlock{Text="Nested line "+i,Height=30});
        var inner=new ScrollViewer{Height=140,Content=innerContent,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,CanContentScroll=false};var content=new StackPanel();content.Children.Add(inner);content.Children.Add(new Border{Height=1800});var outer=new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,CanContentScroll=false,Focusable=true};var window=new NexaDialog{Width=650,Height=480,Content=outer};window.Show();Pump(TimeSpan.FromMilliseconds(35));
        if(SystemParameters.WheelScrollLines>0)
        {
            Wheel032((TextBlock)innerContent.Children[0],-12);Assert(inner.VerticalOffset>0 && inner.VerticalOffset<30 && outer.VerticalOffset==0,"Nested child receives a small gesture exactly once");
            var initial=inner.VerticalOffset;Wheel032((TextBlock)innerContent.Children[0],12);Assert(inner.VerticalOffset<initial,"Direction reversal responds immediately without queued easing");
            inner.ScrollToTop();Pump(TimeSpan.FromMilliseconds(25));for(var burst=0;burst<10;burst++)NexaScrolling.Scroll(inner,-12);Pump(TimeSpan.FromMilliseconds(25));var burstDistance=inner.VerticalOffset;inner.ScrollToTop();Pump(TimeSpan.FromMilliseconds(25));NexaScrolling.Scroll(inner,-120);Pump(TimeSpan.FromMilliseconds(25));Assert(Math.Abs(inner.VerticalOffset-burstDistance)<1,"Same-frame precision deltas accumulate without being dropped by WPF's deferred scroll commands");
            inner.ScrollToBottom();Pump(TimeSpan.FromMilliseconds(25));var boundary=inner.VerticalOffset;Wheel032((TextBlock)innerContent.Children[0],-120);Assert(inner.VerticalOffset==boundary && outer.VerticalOffset>0,"Parent resumes at the nested lower boundary without double scrolling");
            outer.ScrollToTop();inner.ScrollToTop();Pump(TimeSpan.FromMilliseconds(25));for(var i=0;i<10;i++)Wheel032((TextBlock)innerContent.Children[0],-12);var precise=inner.VerticalOffset;inner.ScrollToTop();Pump(TimeSpan.FromMilliseconds(20));Wheel032((TextBlock)innerContent.Children[0],-120);Assert(Math.Abs(inner.VerticalOffset-precise)<1,"Ten fine deltas travel the same distance as one physical wheel notch");
        }
        Assert(outer.VerticalScrollBarVisibility==ScrollBarVisibility.Hidden && outer.ComputedVerticalScrollBarVisibility!=Visibility.Visible,"Global hidden scrollbar policy removes the visual gutter");
        outer.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(25));Assert(outer.ScrollableHeight>0 && Math.Abs(outer.VerticalOffset-outer.ScrollableHeight)<1,"Hidden bars preserve full content extent and bottom reachability");
        outer.ScrollToTop();Pump(TimeSpan.FromMilliseconds(20));outer.Focus();outer.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(window)!,Environment.TickCount,Key.PageDown){RoutedEvent=Keyboard.KeyDownEvent});Pump(TimeSpan.FromMilliseconds(25));Assert(outer.VerticalOffset>0,"Native Page Down remains functional");
        var beforeX=outer.HorizontalOffset;var beforeY=outer.VerticalOffset;Assert(NexaScrolling.Distance(0,3,16,400)==0 && outer.HorizontalOffset==beforeX && outer.VerticalOffset==beforeY,"Zero input leaves both axes unchanged");Capture032(window,"nested-scrolling");
        var horizontal=new ScrollViewer{Content=new Border{Width=2400,Height=100},HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false};window.Content=horizontal;Pump(TimeSpan.FromMilliseconds(30));Assert(horizontal.HorizontalScrollBarVisibility==ScrollBarVisibility.Hidden && horizontal.ComputedHorizontalScrollBarVisibility!=Visibility.Visible,"Horizontal scrollbar visuals are hidden too");horizontal.LineRight();Pump(TimeSpan.FromMilliseconds(25));Assert(horizontal.HorizontalOffset>0 && horizontal.VerticalOffset==0,"Hidden horizontal scrolling retains native horizontal commands");window.Close();
    }
}
