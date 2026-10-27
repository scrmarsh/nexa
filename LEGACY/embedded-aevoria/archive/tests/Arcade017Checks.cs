using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SessionDashboard;

internal static partial class Program
{
    private static BugManager BugTestManager(int seed,BugSpecies species=BugSpecies.AllBugs,BugDensity density=BugDensity.Infestation)
    {
        var m=new BugManager(seed); m.Configure(new(true,species,density));
        m.SetSurfaces(Enumerable.Range(0,12).Select(i=>new BugSurface(i.ToString(),new Rect(30+i%4*300,40+i/4*190,250,150))),new Size(1280,720));
        return m;
    }
    private static void CheckArcade017()
    {
        Assert(App.AppVersion=="0.1.17", "Additional bug features retain version 0.1.17");
        var trials=0;var bursts=0;
        for(var seed=0;seed<2000;seed++)
        {
            var manager=BugTestManager(seed,BugSpecies.Ladybugs,BugDensity.Normal);
            var lady=new BugEntity { Species=BugSpecies.Ladybugs,State=BugState.Crossing,Position=new Point(400,300) }; manager.Bugs.Add(lady);
            var original=BugArtwork.For(lady);
            var first=manager.Squash(lady);
            if(first || lady.Hits!=1 || lady.State==BugState.Squashed || manager.LastBurstCount!=0 || manager.Bugs.Count!=1 || manager.Splats.Count!=0) throw new Exception("Ladybug first hit squashed or spawned bugs.");
            if(seed==0)
            {
                Assert(lady.HitFlash>0 && !ReferenceEquals(original,BugArtwork.For(lady)),"First ladybug hit flashes and shows a persistently cracked shell");
                for(var i=0;i<6;i++)manager.Advance(.08);
                Assert(lady.Hits==1 && lady.State!=BugState.Squashed && lady.HitFlash==0,"First-hit damage survives feedback expiry and continued movement");
            }
            if(!manager.Squash(lady) || lady.Hits!=2 || lady.State!=BugState.Squashed)throw new Exception("Second ladybug hit did not squash.");
            trials++;if(manager.LastBurstCount>0)bursts++;
            var count=manager.Bugs.Count;var heat=manager.ClickHeat;
            if(manager.Squash(lady) || manager.Bugs.Count!=count || manager.ClickHeat!=heat)throw new Exception("Squashed ladybug accepts repeated hits.");
        }
        Assert(trials==2000 && bursts is >440 and <560,$"Ladybugs need two hits and roll infestation only once on squash ({bursts}/2000)");
        var sampleCounts=new Dictionary<BugSpecies,int>();
        for(var seed=0;seed<10;seed++)
        {
            var mixed=BugTestManager(seed);
            for(var i=0;i<350;i++)mixed.Advance(.033);
            Assert(mixed.Bugs.Count==600,$"Infestation seed {seed} fills the increased 600-bug limit");
            foreach(var b in mixed.Bugs)sampleCounts[b.Species]=sampleCounts.GetValueOrDefault(b.Species)+1;
        }
        var centipedes=sampleCounts.GetValueOrDefault(BugSpecies.Centipedes);
        Assert(sampleCounts.Count==6 && centipedes is >60 and <180 && sampleCounts.Where(p=>p.Key!=BugSpecies.Centipedes).All(p=>p.Value>centipedes*5),$"Mixed centipedes are much rarer than every other species ({centipedes}/6000)");
        var cent=BugTestManager(6,BugSpecies.Centipedes,BugDensity.Normal);
        for(var i=0;i<100;i++)cent.Advance(.033);
        Assert(cent.Bugs.Count==2 && cent.Cap==2,"Selecting only centipedes keeps activity sparse");
        var crawler=cent.Bugs[0];var before=crawler.Position;var phase=crawler.CrawlPhase;var pose=BugArtwork.For(crawler);
        cent.Advance(.08);
        Assert(crawler.Position!=before && crawler.CrawlPhase>phase && !ReferenceEquals(pose,BugArtwork.For(crawler)),"Centipede crawl advances its segmented body and alternating legs");
        Assert(BugArtwork.For(crawler).Bounds.Width>BugArtwork.For(new BugEntity { Species=BugSpecies.Roaches }).Bounds.Width,"Centipedes have a larger elongated silhouette");
        Assert(BugArtwork.For(crawler).IsFrozen && BugArtwork.Web.IsFrozen,"High-count rendering shares frozen sprites and web drawings");
        BugManager Arcade(int seed)=>BugTestManager(seed,BugSpecies.AllBugs,BugDensity.Arcade);
        var idle=Arcade(17);var slow=Arcade(17);var fast=Arcade(17);
        Assert(idle.Cap==24 && idle.ArcadeProgress==0 && !idle.ArcadeInfestation,"Arcade starts with normal bug activity");
        void HitOne(BugManager m)
        {
            var bug=m.Bugs.FirstOrDefault(b=>b.State is not (BugState.Squashed or BugState.Hiding));
            if(bug!=null)m.Squash(bug);
        }
        for(var i=0;i<900;i++)
        {
            idle.Advance(1d/30);slow.Advance(1d/30);fast.Advance(1d/30);
            if(i%90==0)HitOne(slow);if(i%5==0)HitOne(fast);
        }
        Assert(fast.ArcadeProgress>slow.ArcadeProgress && slow.ArcadeProgress>idle.ArcadeProgress,"More frequent successful hits accelerate Arcade progression");
        Assert(fast.ArcadeInfestation && fast.Cap==600 && fast.Preferences.Density==BugDensity.Arcade,"Rapid squashing automatically reaches increased infestation without changing mode");
        var oldHeat=fast.ClickHeat;for(var i=0;i<300;i++)fast.Advance(1d/30);
        Assert(fast.ClickHeat<oldHeat && fast.ArcadeInfestation,"Click pressure cools down but Arcade retains its reached infestation stage");
        for(var i=0;i<4800;i++)idle.Advance(1d/30);
        Assert(idle.ArcadeInfestation && idle.Cap==600,"Arcade also escalates gradually with ordinary activity and no clicks");
        var invalid=new BugEntity { Species=BugSpecies.Ladybugs };var progress=slow.ArcadeProgress;
        slow.Squash(invalid);Assert(slow.ArcadeProgress==progress,"Invalid or repeated hit events cannot inflate Arcade intensity");
        fast.Configure(new(false));Assert(fast.Bugs.Count==0 && fast.Webs.Count==0 && fast.ArcadeProgress==0,"Disabling cleans up the larger infestation and Arcade run state");
        CheckArcadeSettings017();
        CheckArcadeResponsiveness017();
        BenchmarkBugs017();
    }
    private static void CheckArcadeSettings017()
    {
        var store=new StateStore(Path.Combine(output,"arcade-settings.json"));store.Save(new AppState { Notes="Keep existing content." });
        var w=new MainWindow(store) { Width=1100,Height=760,Left=-10000,Top=-10000,ShowActivated=false };w.Show();Pump(TimeSpan.FromMilliseconds(100));
        var settings=new SettingsDialog(w,store,w.state,w,"Add Bugs");settings.Show();Pump(TimeSpan.FromMilliseconds(50));
        var choices=Descendants(settings).OfType<RadioButton>().ToArray();
        void Choose(string name)
        {
            var option=choices.Single(c=>Equals(c.Content,name));option.IsChecked=true;
        }
        Choose("Ladybugs");Choose("Arcade");
        var enabled=Descendants(settings).OfType<CheckBox>().Single(c=>Equals(c.Content,"Add Bugs"));enabled.IsChecked=true;enabled.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
        settings.FlushPendingChanges();
        Assert(w.bugsOverlay?.Manager.Preferences==new BugPreferences(true,BugSpecies.Ladybugs,BugDensity.Arcade),"Ladybugs and Arcade are selectable and apply immediately");
        Choose("Centipedes");settings.FlushPendingChanges();settings.Close();
        Assert(store.LoadBugPreferences()==new BugPreferences(true,BugSpecies.Centipedes,BugDensity.Arcade),"New species and Arcade persist through existing preference storage");
        w.WindowState=WindowState.Minimized;Pump(TimeSpan.FromMilliseconds(50));Assert(!w.BugsRunning,"High-count Arcade stops updating while minimized");
        w.WindowState=WindowState.Normal;Pump(TimeSpan.FromMilliseconds(50));
        Assert(w.BugsRunning && w.state.Notes=="Keep existing content.","Restoring resumes Arcade while preserving the Space");
        w.Close();
        var restored=new MainWindow(store) { Left=-10000,Top=-10000,ShowActivated=false };restored.Show();Pump(TimeSpan.FromMilliseconds(80));
        Assert(restored.bugsOverlay?.Manager.Preferences.Species==BugSpecies.Centipedes && restored.bugsOverlay.Manager.Preferences.Density==BugDensity.Arcade,"Reopening restores the new settings with a fresh Arcade run");restored.Close();
    }
    private static void CheckArcadeResponsiveness017()
    {
        var store=new StateStore(Path.Combine(output,"input-stress","state.json"));
        var note=WidgetLibrary.Create("Notes");store.Save(new AppState { WidgetLayoutInitialized=true,Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),note] });
        store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Infestation));
        var w=new MainWindow(store) { Width=1280,Height=850,Left=-10000,Top=-10000,ShowActivated=false };w.Show();
        w.ApplyBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Infestation));
        Pump(TimeSpan.FromMilliseconds(200));
        var m=w.bugsOverlay!.Manager;
        for(var attempt=0;attempt<15 && (m.Bounds.Width<30 || m.Bounds.Height<30);attempt++)Pump(TimeSpan.FromMilliseconds(80));
        for(var i=0;i<350;i++)m.Advance(.033);
        Assert(m.Bugs.Count==600,$"Actual dashboard reaches the higher infestation population ({m.Bugs.Count}, {m.Preferences}, bounds {m.Bounds})");
        var frame=w.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,note.Id));
        var text=Descendants(frame).OfType<TextBox>().First(t=>t.AcceptsReturn);
        var latency=new List<double>();var pulse=new DispatcherTimer(DispatcherPriority.Input) { Interval=TimeSpan.FromMilliseconds(40) };
        pulse.Tick+=(_,_)=>
        {
            var start=Stopwatch.GetTimestamp();
            w.Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(()=> { text.AppendText("x");latency.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }));
        };
        pulse.Start();Pump(TimeSpan.FromMilliseconds(1100));pulse.Stop();Pump(TimeSpan.FromMilliseconds(50));
        var p95=Percentile(latency,.95);
        Assert(latency.Count>=10 && p95<80,$"Input callbacks remain responsive while 600 bugs animate (p95 {p95:F2} ms)");
        Assert(w.BugFrameInterval.TotalMilliseconds<=50 && w.state.Widgets.Single(v=>v.Id==note.Id).Text.Length==latency.Count,"Animation cadence remains bounded and notes still accept and save edits");
        w.SaveNow();w.Close();Assert(store.Load().Widgets.Single(v=>v.Id==note.Id).Text.Length==latency.Count,"High bug activity preserves saved note edits");
    }
    private static double Percentile(List<double> values,double fraction)
    { values.Sort();return values[(int)Math.Min(values.Count-1,Math.Floor(values.Count*fraction))]; }
    private static void BenchmarkBugs017()
    {
        var overlay=new BugOverlay();var manager=overlay.Manager;
        manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Infestation));
        manager.SetSurfaces(Enumerable.Range(0,12).Select(i=>new BugSurface(i.ToString(),new Rect(30+i%4*300,40+i/4*190,250,150))),new Size(1280,720));
        for(var i=0;i<600;i++)manager.Advance(.033);
        var cpu=new List<double>(1800);var allocated=GC.GetAllocatedBytesForCurrentThread();
        for(var i=0;i<1800;i++)
        { var start=Stopwatch.GetTimestamp();manager.Advance(1d/30);cpu.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var cpu95=Percentile(cpu,.95);var cpuMean=cpu.Average();
        Assert(manager.Bugs.Count==600 && manager.Webs.Count<=24 && cpu95<12,$"600-bug simulation stays responsive (mean {cpuMean:F3} ms, p95 {cpu95:F3} ms)");
        Assert(allocated/1800<10000,$"Steady high-count simulation keeps allocations low ({allocated/1800:N0} bytes/frame)");
        var hitStart=Stopwatch.GetTimestamp();for(var i=0;i<10000;i++)manager.Hit(new Point(i%1280,i%720));
        var hitMs=Stopwatch.GetElapsedTime(hitStart).TotalMilliseconds/10000;
        Assert(hitMs<1,$"600-bug hit testing stays fast ({hitMs:F4} ms/query)");
        var host=new Grid { Background=new SolidColorBrush(Color.FromRgb(12,16,24)) };host.Children.Add(overlay);
        var window=new Window { Content=host,Width=1280,Height=720,Left=-10000,Top=-10000,ShowActivated=false };window.Show();Pump(TimeSpan.FromMilliseconds(60));
        var render=new RenderTargetBitmap(1280,720,96,96,PixelFormats.Pbgra32);var times=new List<double>();
        for(var i=0;i<185;i++)
        {
            var start=Stopwatch.GetTimestamp();manager.Advance(1d/30);overlay.RefreshFrame();overlay.UpdateLayout();render.Clear();render.Render(host);
            if(i>=5)times.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        var render95=Percentile(times,.95);var renderMean=times.Average();
        Assert(render95<33,$"600-bug WPF software render fits the 33 ms animation interval (mean {renderMean:F2} ms, p95 {render95:F2} ms)");
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(render));using(var stream=File.Create(Path.Combine(output,"infestation-600.png")))encoder.Save(stream);
        // Stress rapid squash/burst interactions at the absolute safety bound.
        for(var i=0;i<1500;i++)
        { var b=manager.Bugs.FirstOrDefault(b=>b.State is not (BugState.Squashed or BugState.Hiding));if(b!=null)manager.Squash(b);manager.Advance(.033); }
        Assert(manager.Bugs.Count<=768 && manager.Splats.Count<=60 && manager.Webs.Count<=24,"Chained bursts remain bounded at the increased safety limit");
        while(manager.Bugs.Count<768)manager.Bugs.Add(new BugEntity { Species=BugSpecies.Ants,State=BugState.Crossing,Position=new Point(100+manager.Bugs.Count%1100,100+manager.Bugs.Count%500),Target=new Point(400,300),Decision=10,Speed=30 });
        var peak=new List<double>();
        for(var i=0;i<60;i++)
        { var start=Stopwatch.GetTimestamp();overlay.InvalidateVisual();overlay.UpdateLayout();render.Clear();render.Render(host);peak.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
        var peak95=Percentile(peak,.95);
        Assert(peak95<33,$"768-bug burst safety limit stays within the frame budget (p95 {peak95:F2} ms)");
        var visible=new List<BugEntity>(manager.Bugs);foreach(var b in visible)b.Position=new Point(5000,5000);
        overlay.InvalidateVisual();overlay.UpdateLayout();render.Clear();render.Render(host);
        Assert(overlay.LastRenderMilliseconds<5,"Offscreen populations are culled before sprite drawing");
        window.Close();
        var report=new { Population=600,CpuMeanMs=cpuMean,CpuP95Ms=cpu95,AllocatedBytesPerFrame=allocated/1800,HitTestMeanMs=hitMs,SoftwareRenderMeanMs=renderMean,SoftwareRenderP95Ms=render95,PeakPopulation=768,PeakRenderP95Ms=peak95,Viewport="1280x720",SimulationFrames=1800,RenderFrames=180 };
        File.WriteAllText(Path.Combine(output,"bug-performance.json"),System.Text.Json.JsonSerializer.Serialize(report,new System.Text.Json.JsonSerializerOptions { WriteIndented=true }));
        SaveBugArtwork017();
    }
    private static void SaveBugArtwork017()
    {
        var sheet=new DrawingVisual();using(var dc=sheet.RenderOpen())
        {
            dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(12,16,24)),null,new Rect(0,0,800,260));
            var examples=new[] { BugSpecies.Ants,BugSpecies.Roaches,BugSpecies.Flies,BugSpecies.Spiders,BugSpecies.Ladybugs,BugSpecies.Centipedes };
            for(var i=0;i<examples.Length;i++)
            {
                var bug=new BugEntity { Species=examples[i],CrawlPhase=2 };
                dc.PushTransform(new TranslateTransform(65+i*130,75));dc.PushTransform(new ScaleTransform(4,4));dc.DrawDrawing(BugArtwork.For(bug));dc.Pop();dc.Pop();
                if(bug.Species==BugSpecies.Ladybugs)
                { bug.Hits=1;dc.PushTransform(new TranslateTransform(65+i*130,175));dc.PushTransform(new ScaleTransform(4,4));dc.DrawDrawing(BugArtwork.For(bug));dc.Pop();dc.Pop(); }
                if(bug.Species==BugSpecies.Centipedes)
                { bug.CrawlPhase=5;dc.PushTransform(new TranslateTransform(65+i*130,175));dc.PushTransform(new ScaleTransform(4,4));dc.DrawDrawing(BugArtwork.For(bug));dc.Pop();dc.Pop(); }
            }
        }
        var bitmap=new RenderTargetBitmap(800,260,96,96,PixelFormats.Pbgra32);bitmap.Render(sheet);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(Path.Combine(output,"new-bug-artwork.png"));png.Save(stream);
    }
}
