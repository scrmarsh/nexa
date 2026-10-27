using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SessionDashboard;

// Opt-in packaging check. All diagnostic state stays in the supplied output directory.
internal static class StartupVerification
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern nint GetWindowDpiAwarenessContext(nint hwnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool AreDpiAwarenessContextsEqual(nint first,nint second);
    internal static void Run(App app, string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            var store = new StateStore(Path.Combine(directory, "isolated-state.json"));
            var window = new MainWindow(store)
            {
                ShowActivated = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -10000,
                Top = -10000
            };
            app.MainWindow = window;
            var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
            timeout.Tick += (_, _) => { File.WriteAllText(Path.Combine(directory, "startup-error.txt"), "ContentRendered was not raised."); app.Shutdown(1); };
            timeout.Start();
            window.ContentRendered += async (_, _) =>
            {
                try
                {
                    timeout.Stop();
                    var content = (FrameworkElement)window.Content;
                    var bitmap = new RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth + content.Margin.Left + content.Margin.Right), (int)Math.Ceiling(content.ActualHeight + content.Margin.Top + content.Margin.Bottom), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(content);
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (var stream = File.Create(Path.Combine(directory, "startup.png"))) encoder.Save(stream);
                    if (window.PixelGrid.Children.Count == 0 || window.PixelGrid.Children.OfType<System.Windows.Controls.Border>().Any(p => Math.Abs(p.Width - p.Height) > .01) || window.PresetPicker.Items.Count != 4) throw new InvalidOperationException("Startup controls did not initialize.");
                    var bibleVerses = BibleLibrary.Translations.ToDictionary(t => t, t => BibleLibrary.Load(t).Verses.Count);
                    if (bibleVerses.Values.Any(count => count < 30000)) throw new InvalidOperationException("Packaged Bible resources are incomplete.");
                    File.WriteAllText(Path.Combine(directory, "startup.json"), JsonSerializer.Serialize(new { Success = true, AppVersion=App.AppVersion, Release=App.VersionLabel, BugPopulation=window.bugsOverlay?.Manager.Bugs.Count??0, PerMonitorV2 = AreDpiAwarenessContextsEqual(GetWindowDpiAwarenessContext(new System.Windows.Interop.WindowInteropHelper(window).Handle),new nint(-4)), DpiScale = VisualTreeHelper.GetDpi(window).DpiScaleX, window.Title, Pixels = window.PixelGrid.Children.Count, Presets = window.PresetPicker.Items.Count, Width = content.ActualWidth, Height = content.ActualHeight, BibleVerses = bibleVerses, ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
                    app.Shutdown(0);
                }
                catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "startup-error.txt"), ex.ToString()); app.Shutdown(1); }
            };
            window.Show();
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(directory, "startup-error.txt"), ex.ToString()); app.Shutdown(1); }
    }
}