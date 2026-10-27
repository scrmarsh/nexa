using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;

internal sealed partial class BattlefieldWindow
{
    private Border? performancePanel;
    private TextBlock? performanceText;
    internal AevoriaPerformanceTelemetry? Performance {get;private set;}
    internal bool PerformanceVisible=>performancePanel?.Visibility==Visibility.Visible;
    private void InitializePerformanceOverlay()
    {
        performanceText=new TextBlock{FontFamily=new FontFamily("Consolas"),FontSize=11,Foreground=Brushes.White,TextWrapping=TextWrapping.Wrap};
        var body=new StackPanel();body.Children.Add(new TextBlock{Text="AEVORIA — PERFORMANCE",FontSize=12,FontWeight=FontWeights.SemiBold,Foreground=Brushes.Gold,Margin=new Thickness(0,0,0,7)});body.Children.Add(performanceText);
        performancePanel=new Border{Tag="AevoriaHud",Child=body,Width=350,Padding=new Thickness(12),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(12,90,12,12),CornerRadius=new CornerRadius(6),Background=new SolidColorBrush(Color.FromArgb(225,10,17,25)),IsHitTestVisible=false,Visibility=Visibility.Collapsed};
        viewport.Children.Add(performancePanel);Performance=new(this,Battlefield,World);Performance.Changed+=UpdatePerformanceText;
        PreviewKeyDown+=PerformanceKey;Closed+=(_,_)=>Performance.Dispose();
        gameHud!.SizeChanged+=(_,_)=>performancePanel.Margin=new Thickness(12,gameHud.ActualHeight+20,12,12);
    }
    private void PerformanceKey(object sender,KeyEventArgs e)
    {
        if(e.Key!=Key.F3 || e.IsRepeat || Keyboard.Modifiers!=ModifierKeys.None)return;
        TogglePerformance();e.Handled=true;
    }
    internal void TogglePerformance()
    {
        if(performancePanel==null || Performance==null)return;
        performancePanel.Visibility=PerformanceVisible?Visibility.Collapsed:Visibility.Visible;
        Performance.SetEnabled(PerformanceVisible);UpdatePerformanceText();
    }
    private void UpdatePerformanceText()
    {
        if(!PerformanceVisible || performanceText==null || Performance==null)return;
        var s=Performance.Snapshot;var r=s.Resources;
        static string Number(double? value,string format="F1")=>value?.ToString(format,System.Globalization.CultureInfo.InvariantCulture)??"N/A";
        static string Percent(double? value)=>value is {} n?n.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"%":"N/A";
        string text;
        if(!s.Active)text="Inactive — sampling suspended\nRender FPS / CPU / GPU / RAM: N/A";
        else
        {
            var fresh=r.CapturedAt>0 && System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency-r.CapturedAt<3;
            var stages=Battlefield.TerrainView?.LastDrawingTimes??default;
            text=$"Render FPS (WPF callbacks) {Number(s.Frames.Fps)}\nFrame interval mean/p95   {Number(s.Frames.MeanMs)}/{Number(s.Frames.P95Ms)} ms\n{s.Frames.State}\n"+
                $"CPU (Aevoria)             {Percent(fresh?r.Cpu:null)}\nGPU (Aevoria, peak engine) {Percent(fresh?r.Gpu.Percent:null)}\n"+
                $"RAM (Aevoria, working set) {(fresh?(r.WorkingSet/1048576d).ToString("F0"):"N/A")} MiB\nManaged heap              {(fresh?(r.ManagedBytes/1048576d).ToString("F0"):"N/A")} MiB\nSystem RAM                {Percent(fresh?r.SystemRam:null)}\n"+
                $"Simulation                {s.SimulationTicks:F0} ticks/s · {s.SimulationMs:F2} ms\nUI delay / drawing        {s.DispatcherDelayMs:F1} / {Battlefield.LastRenderMilliseconds:F2} ms\n"+
                $"Landscape / politics      {stages.Landscape:F2} / {stages.Politics:F2} ms\nWater / settlements       {stages.Water:F2} / {stages.Settlements:F2} ms\n"+
                $"{Battlefield.MapDetail} · {Battlefield.Camera.M11:F2}× · {Living!.Info.Quality}\n{Living.Info.Tiles}×{Living.Info.HeightTiles} · {World.Manager.Bugs.Count} entities\nWPF tier {RenderCapability.Tier>>16} · {RenderOptions.ProcessRenderMode}";
        }
        if(performanceText.Text!=text)performanceText.Text=text;
        performanceText.ToolTip="Render FPS counts distinct WPF rendering callbacks only when the gameplay drawing changed; not confirmed GPU presents. CPU is process time divided by elapsed time and logical processors. "+r.Gpu.Reason+$" Observed adapters: {r.Gpu.Adapters}; engines: {r.Gpu.Engines}. Simulation timing covers Advance and post-step work; it excludes earlier pre-step handlers.";
    }
}
