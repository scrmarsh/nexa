using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void AuditPoliticalRendering()
    {
        var store=new StateStore(System.IO.Path.Combine(output,"audit","state.json"));var session=Complete023(LivingWorlds.Create(store,"Render audit","kingdoms",1024,576));session.Controller.SetPaused(true);
        var overlay=new BugOverlay(session.Controller.Manager){ShowHud=false};overlay.TerrainView=new LivingTerrainView(session,overlay);overlay.Measure(new Size(1920,1080));overlay.Arrange(new Rect(0,0,1920,1080));
        var zoom=.96*Math.Min(1920/session.Info.Extent,1080/session.Info.HeightExtent);overlay.Camera=new Matrix(zoom,0,0,zoom,(1920-session.Info.Extent*zoom)/2,(1080-session.Info.HeightExtent*zoom)/2);
        var image=new RenderTargetBitmap(1920,1080,96,96,PixelFormats.Pbgra32);
        void Render(){overlay.RefreshFrame();overlay.UpdateLayout();image.Clear();image.Render(overlay);}
        var watch=Stopwatch.StartNew();do{Pump(TimeSpan.FromMilliseconds(100));Render();}while(!overlay.TerrainView.DetailReady && watch.Elapsed.TotalSeconds<15);
        var m=session.Controller.Manager;
        foreach(var part in new[]{"off","fill","border","labels","all"})
        {
            overlay.TerrainView.NationView.Enabled=part!="off";m.Configure(m.Preferences with{Arcade=m.BattleOptions with{ShowTerritories=part is "fill" or "all",ShowTerritoryBorders=part is "border" or "all",ShowFactionLabels=part is "labels" or "all"}});
            Render();var samples=new List<double>();for(var i=0;i<8;i++){var t=Stopwatch.GetTimestamp();Render();samples.Add(Stopwatch.GetElapsedTime(t).TotalMilliseconds);}Console.WriteLine($"POLITICAL {part}: mean={samples.Average():F2}ms p95={Percentile(samples,.95):F2}ms");
        }
        overlay.TerrainView.Dispose();
    }
}
