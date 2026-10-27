from pathlib import Path
import shutil,re
root=Path(r'M:\Nexa')
stage=Path(__file__).parent
backup=Path(r'M:\Aevoria-old\Nexa-embedded-20261026\before-master-rebuild')
backup.mkdir(parents=True,exist_ok=True)
def read(name): return (root/name).read_text(encoding='utf-8-sig')
def write(name,text):
    p=root/name; dest=backup/name
    if p.exists() and not dest.exists(): dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text,encoding='utf-8')
def method(text,signature):
    start=text.index(signature);brace=text.index('{',start);depth=1;end=brace+1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]
for name in ['PixelTiming.cs','MainWindow.ClassSchedule.cs','NexaWindowBounds.cs']:write(name,(stage/name).read_text(encoding='utf-8'))
text=read('MainWindow.Session.cs')
text=text.replace('private void SessionTimeEditClick(object sender, RoutedEventArgs e)','private void SessionTimeEditClick(object sender, RoutedEventArgs e) => OpenClassSchedule();\n    private void EditOneTimeTarget(object sender, RoutedEventArgs e)')
write('MainWindow.Session.cs',text)
text=read('MainWindow.xaml.cs')
start=text.index('        var remainingFraction = session == null ? 0 :')
end=text.index('        var head = NexaTime.LitPixels(',start)
text=text[:start]+'''        var reading = PixelTiming.Read(session, now);
        Countdown.Text = ClockText(reading.Time, !reading.CountUp);
        CountdownCaption.Text = reading.Caption; StatusLabel.Text = reading.Status;
        var remainingFraction = reading.RemainingFraction;
'''+text[end:]
text=text.replace('    private bool fullscreen;','    private bool fullscreen;\n    private bool restoreFullscreenTopmost;\n    internal bool IsFullscreen => fullscreen;')
text=text.replace('            fullscreen = true;','            restoreFullscreenTopmost = Topmost;\n            fullscreen = true;\n            WindowState = WindowState.Normal; Topmost = true;')
text=text.replace('            WindowState = restoreWindowState; fullscreen = false;','            fullscreen = false; Topmost = restoreFullscreenTopmost;\n            WindowState = restoreWindowState;')
text=text.replace('        preset.ScheduledDays = SelectedSessionDays();','        preset.ScheduledDays = SelectedSessionDays();\n        preset.AutomaticScheduling = state.Session?.AutomaticScheduling == true || (PresetPicker.SelectedItem as Preset)?.AutomaticScheduling == true;')
text=text.replace('        selected.ScheduledDays = preset.ScheduledDays;','        selected.ScheduledDays = preset.ScheduledDays; selected.AutomaticScheduling = preset.AutomaticScheduling;')
write('MainWindow.xaml.cs',text)
text=read('NexaChrome.cs').replace('        FadeIn(window);','        NexaWindowBounds.Attach(window);\n        FadeIn(window);',1)
text='\n'.join(line for line in text.splitlines() if 'Tag:"AevoriaMapSurface"' not in line)
write('NexaChrome.cs',text)
text=read('MainWindow.Continuity.cs').replace('saving ? "⟳" : "✓"','saving ? "🔄" : "✓"')
write('MainWindow.Continuity.cs',text)
text=read('NexaNotifications.cs').replace('using System.Windows.Threading;','using System.Windows.Threading;\nusing System.Windows.Media.Animation;')
text=text.replace('record Entry(Border Card, DateTimeOffset? Until, int Priority)','record Entry(Border Card, TextBlock Glyph, DateTimeOffset? Until, int Priority)')
text=text.replace('stack.Children.Remove(entry.Card);','{ entry.Glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null); stack.Children.Remove(entry.Card); }')
text=text.replace('        DockPanel.SetDock(glyph, Dock.Left);','        glyph.RenderTransformOrigin = new Point(.5, .5); glyph.RenderTransform = new RotateTransform();\n        if (key == "autosave" && message == "Autosaving…" && SystemParameters.ClientAreaAnimation)\n            glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever });\n        DockPanel.SetDock(glyph, Dock.Left);')
text=text.replace('entries[key] = new(card, seconds','entries[key] = new(card, glyph, seconds')
text=text.replace('owner.Children.Remove(viewport); entries.Clear();','owner.Children.Remove(viewport); foreach (var key in entries.Keys.ToArray()) Dismiss(key);')
write('NexaNotifications.cs',text)
text=read('MainWindow.Reliability.cs').replace('        if (!Options.ShowQuickTips) notifications?.Dismiss("tip");','        if (!Options.ShowQuickTips) { notifications?.Dismiss("tip"); tipRotation.Stop(); }\n        else if (!closing) tipRotation.Start();')
write('MainWindow.Reliability.cs',text)
text=read('SettingsDialog.Session.cs')
needle='        var session = state.Session;'
text=text.replace(needle,'''        var weekly = new Button { Content = "Edit weekly class schedule", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 16) };
        weekly.Click += (_, _) => { Close(); space?.ShowWeeklySchedule(); }; page.Children.Add(weekly);
'''+needle)
write('SettingsDialog.Session.cs',text)
text=read('MainWindow.ClassSchedule.cs').replace('    private void OpenClassSchedule()','    internal void ShowWeeklySchedule() => OpenClassSchedule();\n    private void OpenClassSchedule()')
write('MainWindow.ClassSchedule.cs',text)
# A focused project keeps valid Nexa tests runnable without the archived game project.
old=read('tests/Program.cs')
helpers=[method(old,sig) for sig in ['    private static void Assert(', '    private static IEnumerable<DependencyObject> Descendants(', '    private static void Pump(', '    private static void Snapshot(']]
runner='''using System.IO;
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
            if(args.Contains("--core"))CheckNexaweenCore();
            else if(args.Contains("--continuity")){CheckContinuityModels();CheckContinuityUi();}
            else if(args.Contains("--shared"))CheckShared012();
            else if(args.Contains("--menus"))CheckMenuRepair();
            else CheckMasterCore();
            app.Shutdown();File.WriteAllText(Path.Combine(output,"result.txt"),$"PASS: {passed} checks");Console.WriteLine($"PASS: {passed} checks. Artifacts: {output}");return 0;
        }
        catch(Exception ex){File.WriteAllText(Path.Combine(output,"failure.txt"),ex.ToString());Console.Error.WriteLine(ex);return 1;}
    }
'''
runner+='\n'.join(helpers)+'\n}\n'
write('tests/Nexa024Runner.cs',runner)
write('tests/Nexa024.Checks.csproj','''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0-windows</TargetFramework><UseWPF>true</UseWPF><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><AssemblyName>SessionDashboard.Checks</AssemblyName><EnableDefaultCompileItems>false</EnableDefaultCompileItems><BaseOutputPath>bin/Nexa024/</BaseOutputPath></PropertyGroup>
  <ItemGroup><ProjectReference Include="../SessionDashboard.csproj" /><Compile Include="Nexa024Runner.cs;Nexa024CoreChecks.cs;NexaweenCoreChecks.cs;NexaweenMoreChecks.cs;ContinuityChecks.cs;Shared012Checks.cs;MenuRepairChecks.cs" /></ItemGroup>
</Project>''')
text=read('tests/NexaweenMoreChecks.cs').replace('App.VersionLabel.Contains("NEXAWEEN")','App.VersionLabel == "nexa v0.1.24 alpha"')
write('tests/NexaweenMoreChecks.cs',text)
print('Applied weekly timer editor/count-up reading, monitor work-area bounds, notification icon animation, and focused test project.')
