using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckColonyUi019()
    {
        var store=new StateStore(Path.Combine(output,"colony-ui019.json"));var note=WidgetLibrary.Create("Notes");note.Text="Existing work";note.CustomSize=true;note.PlacementX=0;note.PlacementY=0;
        var data=new AppState {WidgetLayoutInitialized=true,Widgets=[note]};
        foreach(var kind in BugHabitats.KindSpecies.Keys)data.Widgets.Add(new WidgetInstance {Kind=kind,Title=kind});
        store.Save(data);
        var options=new ArcadeSettings {Mode=ArcadeRuleSet.ClassicRounds,SoftCap=72,HardCap=110,GroupSize=5,SpawnInterval=.4,Species=new Dictionary<BugSpecies,SpeciesCombatStats>(ArcadeSettings.Defaults)};
        options.Species[BugSpecies.Spiders]=options.Species[BugSpecies.Spiders] with{Damage=4};
        store.SaveBugPreferences(new(true,BugSpecies.Ants,BugDensity.Arcade,options));
        var loaded=store.LoadBugPreferences().Arcade!;
        Assert(loaded.HardCap==110 && loaded.SpawnInterval==.4 && loaded.Species![BugSpecies.Spiders].Damage==4,"Arcade pacing and per-species balance round-trip through existing preferences");
        var w=new MainWindow(store) {Width=1200,Height=820,Left=-10000,Top=-10000,ShowActivated=false};w.Show();Pump(TimeSpan.FromMilliseconds(250));
        var manager=w.bugsOverlay!.Manager;
        Assert(w.BugsRunning && manager.Colonies.Count==6 && manager.Bugs.Count>=6,"Live Arcade starts all six colonies immediately instead of waiting three seconds");
        Assert(w.state.Widgets.Count==1 && w.state.Widgets[0].Id==note.Id && w.state.Widgets[0].Text=="Existing work","Obsolete habitat migration preserves unrelated widget content and identity");
        Assert(!WidgetLibrary.Definitions.Any(d=>BugHabitats.KindSpecies.ContainsKey(d.Name)),"Stationary colonies are removed from the widget catalog");
        var editor=Descendants(w.WidgetCanvas).OfType<TextBox>().First(t=>t.AcceptsReturn);editor.AppendText(" remains editable.");
        Pump(TimeSpan.FromSeconds(3));Assert(manager.Bugs.Count>6 && editor.Text.Contains("editable"),"Protective armies spawn while normal dashboard editing remains responsive");
        var before=manager.MatchNumber;var settings=new SettingsDialog(w,store,w.state,w,"Add Bugs");settings.Show();Pump(TimeSpan.FromMilliseconds(80));
        Assert(Descendants(settings).OfType<Expander>().Any(e=>Equals(e.Header,"Colony Wars settings")),"Existing Add Bugs settings exposes Colony Wars pacing and species balance");settings.Close();
        Pump(TimeSpan.FromMilliseconds(350));
        var startFrames=w.BugFrameCount;var clock=Stopwatch.StartNew();
        var inputTimer=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Input) {Interval=TimeSpan.FromMilliseconds(30)};
        var editCount=0;inputTimer.Tick+=(_,_)=> {editor.AppendText(".");editCount++;};inputTimer.Start();Pump(TimeSpan.FromSeconds(1.2));inputTimer.Stop();
        var fps=(w.BugFrameCount-startFrames)/clock.Elapsed.TotalSeconds;
        Console.WriteLine($"Live Colony Wars cadence: {fps:F1} FPS, input edits={editCount}");
        Assert(fps>=25 && editCount>=20,"Live battle rendering and editing remain responsive with graceful cadence fallback");
        Save018(w,1200,820,"colony-wars-dashboard019");
        w.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(60));Assert(!w.BugsRunning,"Minimizing pauses the shared simulation timer");
        w.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(60));Assert(w.BugsRunning && manager.MatchNumber==before,"Restoring resumes the current match without respawning leaders");
        w.ApplyBugPreferences(new(false));Assert(!w.BugsRunning && w.bugsOverlay==null && manager.Colonies.Count==0 && manager.Projectiles.Count==0,"Disabling Arcade immediately stops the timer and releases battle objects");
        w.SaveNow();w.Close();Assert(store.Load().Widgets.Single().Text.Contains("editable"),"Dashboard edits persist independently of battle simulation");
        foreach(var density in new[]{BugDensity.Low,BugDensity.Normal,BugDensity.Infestation})
        {
            var decoration=new BugManager(19);decoration.Configure(new(true,BugSpecies.AllBugs,density));decoration.SetSurfaces([],new Size(1100,700));
            for(var i=0;i<1000;i++)decoration.Advance(.033);
            Assert(decoration.Colonies.Count==0 && decoration.Projectiles.Count==0 && decoration.Bugs.All(b=>!b.IsLeader),density+" remains a decorative mode without Colony Wars combat");
        }
        CheckCounterVessels018();
    }
    private static void CheckBattlePerformance019()
    {
        var overlay=new BugOverlay();var manager=overlay.Manager;manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{Mode=ArcadeRuleSet.ClassicRounds}));manager.SetSurfaces([],new Size(1200,760));manager.Advance(.016);
        for(var i=0;i<174;i++)Fighter(manager,BugHabitats.Species[i%6],new Point(55+i*71%1100,55+i*43%650));
        var host=new Grid {Background=new SolidColorBrush(Color.FromRgb(8,11,22))};host.Children.Add(overlay);
        var window=new Window {Content=host,Width=1200,Height=760,Left=-10000,Top=-10000,ShowActivated=false};window.Show();Pump(TimeSpan.FromMilliseconds(100));
        var bitmap=new RenderTargetBitmap(1200,760,96,96,PixelFormats.Pbgra32);var cpu=new List<double>();var render=new List<double>();
        for(var i=0;i<100;i++)
        {
            var start=Stopwatch.GetTimestamp();manager.Advance(1d/60);cpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            start=Stopwatch.GetTimestamp();overlay.RefreshFrame();overlay.UpdateLayout();bitmap.Clear();bitmap.Render(host);if(i>10)render.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        var cp=Percentile(cpu,.95);var rp=Percentile(render,.95);Console.WriteLine($"Battle performance: CPU p95={cp:F2}ms, software render p95={rp:F2}ms");
        Assert(cp<5 && cp+rp<33.4,"Maximum-population simulation and software rendering fit the graceful 30 FPS fallback budget");
        File.WriteAllText(Path.Combine(output,"performance019.json"),System.Text.Json.JsonSerializer.Serialize(new {CpuP95Ms=cp,SoftwareRenderP95Ms=rp,TargetFps=60,LiveUnits=manager.Bugs.Count(BugManager.Alive)},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
        Save018(host,1200,760,"colony-wars-performance019");
        manager.StartBattle();for(var i=0;i<750;i++)manager.Advance(1d/30);
        overlay.RefreshFrame();overlay.UpdateLayout();Save018(host,1200,760,"colony-wars-battle019");
        manager.StartBattle();var mama=manager.Colonies[BugSpecies.Spiders].Leader;
        var queen=manager.Colonies[BugSpecies.Ants].Leader;queen.Protection=0;manager.ApplyBattleDamage(mama,queen,9999);
        manager.Advance(.016);manager.Advance(.08);overlay.RefreshFrame();overlay.UpdateLayout();Save018(host,1200,760,"colony-wars-conquest019");
        foreach(var c in manager.Colonies.Values)if(c.Species!=BugSpecies.Spiders) {c.Leader.Protection=0;manager.ApplyBattleDamage(mama,c.Leader,9999);}
        for(var i=0;i<1800;i++) {manager.Advance(.016);if(manager.CurrentAnnouncement?.Victory==true && manager.AnnouncementAge>.4)break;}
        overlay.RefreshFrame();overlay.UpdateLayout();Save018(host,1200,760,"colony-wars-victory019");window.Close();
    }
}
