using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;
internal static partial class Program
{
    private static void Measure026()
    {
        var results=new List<string>();
        for(var i=0;i<3;i++)
        {
            var store=new StateStore(Path.Combine(output,$"perf-{i}.json"));
            var spaces=Enumerable.Range(0,12).Select(n=>Continuity.NewDashboard("Example "+n,"Study",new DashboardData{WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Notes","notes"),WidgetLibrary.Create("Task List","tasks"),WidgetLibrary.Create("Clock")]},store.UserId)).ToList();
            store.Save(Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId));
            var watch=Stopwatch.StartNew();var home=new HomeWindow(store);results.Add($"Home constructor {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");
            home.ShowActivated=false;home.ShowInTaskbar=false;home.WindowStartupLocation=WindowStartupLocation.Manual;home.Left=-10000;home.Top=-10000;home.Show();Pump(TimeSpan.FromMilliseconds(80));watch.Restart();var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");results.Add($"Settings constructor {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");settings.Close();home.Close();
            watch.Restart();var main=new MainWindow(store);results.Add($"Space constructor {i}: {watch.Elapsed.TotalMilliseconds:F2} ms");main.Close();
        }
        File.WriteAllLines(Path.Combine(output,"performance.txt"),results);foreach(var row in results)Console.WriteLine(row);
    }
    private static void Capture026(Window window,string name)
    {
        window.UpdateLayout();var element=(FrameworkElement)window.Content;
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),(int)Math.Ceiling(element.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(element);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(output,name+".png"));encoder.Save(stream);
    }
    private static void Check026()
    {
        Assert(App.VersionLabel=="Nexa v0.1.26 alpha","Current alpha version comes from assembly metadata");
        var store=new StateStore(Path.Combine(output,"alpha026.json"));
        var spaces=Enumerable.Range(0,10).Select(i=>Continuity.NewDashboard("Space "+i,"Study",new DashboardData{SpaceIcon="Palette",WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Notes","notes")]},store.UserId)).ToList();
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=spaces[0].DashboardId,Dashboards=spaces},store.UserId);model.Workspace!.Settings.ReduceMotion=true;store.Save(model);
        var file=Continuity.ReadExport(Continuity.Export(spaces[0],false));Assert(file.Data.SpaceIcon=="Palette" && file.Data.Notes=="","Configuration export preserves icon without carrying private notes");
        Assert(store.Load().SpaceIcon=="Palette" && SpaceIcons.Resolve("unknown","Church")=="Church" && SpaceIcons.Resolve(null,"Class")=="Book","Icons survive persistence and legacy preset fallback");
        foreach(var icon in SpaceIcons.Paths.Keys)Assert(SpaceIcons.Create(icon).Width==20,"Vector geometry parses: "+icon);
        var home=new HomeWindow(store){ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual,Left=-10000,Top=-10000};home.Show();Pump(TimeSpan.FromMilliseconds(100));
        var initial=Descendants(home.Recent).OfType<Button>().Where(b=>b.Tag is Guid).ToArray();Assert(initial.Length==6,"Home displays exactly six initial cards");
        var more=Descendants(home.Recent).OfType<Button>().Single(b=>Equals(b.Content,"↓ Show More Spaces"));more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Descendants(home.Recent).OfType<Button>().Count(b=>b.Tag is Guid)==10 && initial.All(b=>Descendants(home.Recent).Contains(b)),"Expansion shows all additional Spaces and retains original six controls");
        more.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(TimeSpan.FromMilliseconds(40));Assert(Descendants(home.Recent).OfType<Button>().Count(b=>b.Tag is Guid)==6,"Hide More restores six cards");
        var settings=new SettingsDialog(home,store,home.Saved,null,"Appearance");settings.Show();Pump(TimeSpan.FromMilliseconds(80));
        Assert(!Descendants(settings).OfType<Expander>().Any() && Descendants(settings).OfType<Button>().Count(b=>Equals(b.Content,"Experimental Features"))==1,"Experimental Features is a standalone tab without a collapsible arrow");
        Assert(!Descendants(settings).OfType<TextBlock>().Any(t=>t.Text=="API token from Todoist / Settings / Integrations / Developer"),"Hidden integrations are not constructed with Appearance");
        var bounds=new Rect(settings.Left,settings.Top,settings.Width,settings.Height);settings.SelectSection("Sounds");settings.SelectSection("Appearance");
        Assert(bounds==new Rect(settings.Left,settings.Top,settings.Width,settings.Height),"Settings tab navigation preserves geometry");settings.Close();
        var motion=new Border{Opacity=1};var fade=NexaMotion.Fade(motion,0,new NexaOptions{ReduceMotion=true});Assert(fade.IsCompleted && motion.Opacity==0,"Reduced motion completes fades immediately");
        var splash=new SplashWindow(new NexaOptions{ReduceMotion=true});Assert(splash.Loading.Visibility==Visibility.Collapsed && Descendants(splash).OfType<TextBlock>().Any(t=>t.Text==App.VersionLabel && t.Visibility==Visibility.Visible),"Splash version precedes the progress track");splash.Show();Pump(TimeSpan.FromMilliseconds(30));Assert(splash.TaglineComplete && splash.Loading.Visibility==Visibility.Visible,"Reduced motion completes the tagline before revealing progress");splash.Report(.6,"Fixture ready");splash.Report(.3,"Another milestone");Assert(splash.Loading.Value==.6,"Startup progress cannot move backward");splash.Close();
        var wizard=new OnboardingWindow(store){Owner=home};Assert(wizard.Options.SeasonalMode==model.Workspace.Settings.SeasonalMode,"Replayed setup preserves an existing seasonal preference");
        wizard.Advance();wizard.Advance();Assert(wizard.Step==3,"Onboarding skips the redundant activity page");wizard.Advance();wizard.Advance();Assert(wizard.Step==5,"Setup retains tools, customization and the final naming screen");wizard.Close();
        Capture026(home,"home-026");
        var oldWindow=Application.Current.MainWindow;
        var navigation=NexaNavigation.Change(home,()=>Task.FromException<Window>(new IOException("Synthetic read failure")),model.Workspace.Settings);while(!navigation.IsCompleted)Pump(TimeSpan.FromMilliseconds(10));
        Assert(navigation.IsFaulted && home.IsVisible && ((FrameworkElement)home.Content).Opacity==1 && !NexaNavigation.Busy(home),"Failed navigation restores visible Home and releases the navigation guard");Application.Current.MainWindow=oldWindow;
        home.Close();
    }
}
