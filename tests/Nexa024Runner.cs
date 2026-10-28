using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;
internal static partial class Program
{
    private static int passed;
    private static string output="";
    [STAThread] private static int Main(string[] args)
    {
        output=Path.GetFullPath(Path.Combine("verification",DateTime.Now.ToString("yyyyMMdd-HHmmss")));Directory.CreateDirectory(output);
        try
        {
            var app=new App{ShutdownMode=ShutdownMode.OnExplicitShutdown,SuppressNormalStartup=true};app.InitializeComponent();
            Theme.Apply(app.Resources,SessionDashboard.ThemeMode.Dark,AppTheme.Nexa);
            if(args.Contains("--ui-controls"))
            {
                var store=new StateStore(Path.Combine(output,"isolated-controls.json"));
                var main=new MainWindow(store);app.MainWindow=main;main.Show();
                var settings=new SettingsDialog(main,store,main.state,main,"Add Bugs");settings.Owner=null;settings.ShowInTaskbar=true;settings.WindowStartupLocation=WindowStartupLocation.Manual;settings.Left=100;settings.Top=100;settings.Show();
                var lifetime=new DispatcherTimer{Interval=TimeSpan.FromSeconds(120)};lifetime.Tick+=(_,_)=>{lifetime.Stop();settings.Close();main.Close();app.Shutdown();};lifetime.Start();
                settings.Closed+=(_,_)=>{main.Close();lifetime.Stop();app.Shutdown();};main.Closed+=(_,_)=>app.Shutdown();app.Run();return 0;
            }
            if(args.Contains("--showcase"))GenerateShowcase(args[Array.IndexOf(args,"--showcase")+1]);
            else if(args.Contains("--existing-data"))CheckExistingData(args[Array.IndexOf(args,"--existing-data")+1]);
            else if(args.Contains("--public-updates"))CheckPublicUpdates025();
            else if(args.Contains("--arrivals"))CheckArrivalTweaks();
            else if(args.Contains("--audit025"))CheckAudit025();
            else if(args.Contains("--update-integration"))CheckUpdateIntegration(args[Array.IndexOf(args,"--update-integration")+1]);
            else if(args.Contains("--final"))CheckFinalRelease();
            else if(args.Contains("--followup-design"))CheckFollowupDesign();
            else if(args.Contains("--followup"))CheckFollowup();
            else if(args.Contains("--animation"))CheckAnimation();
            else if(args.Contains("--ecosystem"))CheckEcosystem();
            else if(args.Contains("--reliability"))CheckMasterReliability();
            else if(args.Contains("--core"))CheckNexaweenCore();
            else if(args.Contains("--continuity")){CheckContinuityModels();CheckContinuityUi();}
            else if(args.Contains("--shared"))CheckShared012();
            else if(args.Contains("--menus"))CheckMenuRepair();
            else CheckMasterCore();
            app.Shutdown();File.WriteAllText(Path.Combine(output,"result.txt"),$"PASS: {passed} checks");Console.WriteLine($"PASS: {passed} checks. Artifacts: {output}");return 0;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());Console.Error.WriteLine(ex);return 1;}
    }
    private static void Assert(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        passed++; Console.WriteLine("PASS: " + description);
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static void Pump(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static void Snapshot(MainWindow window, string name, double width, double height)
    {
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, name + ".png")); encoder.Save(stream);
    }
}
