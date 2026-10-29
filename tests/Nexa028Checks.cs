using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static int SaveWriter028(string path)
    {
        path=Path.GetFullPath(path);
        if(!path.Contains(Path.DirectorySeparatorChar+"verification"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Disposable verification stores only.");
        var store=new StateStore(path);var state=store.Load();state.Workspace!.CleanShutdown=false;
        File.WriteAllText(path+".ready","ready");
        for(var generation=0;;generation++){state.Notes="generation "+generation+" "+new string('x',20000);store.Save(state);}
    }
    private static void Check028()
    {
        Assert(App.AppVersion=="0.1.0" && App.VersionLabel=="nexa v0.1.0 beta" && ReleaseNotes.All[0].Title=="Jump into Nexa for Halloween!","Current .28 identity uses the existing version infrastructure");
        CheckRecovery028();CheckSchedule028();CheckEcosystem028();CheckLargeWorkspace028();
        var at=DateTimeOffset.UtcNow;var ledger=new ProductivityLedger();
        Assert(ProductivityScoring.DirectSquash(ledger,"boost",at) && ledger.BugFrenzyUntil==at.AddSeconds(20) && ledger.EffectiveMultiplier(at)==2,"Direct squash starts the independent twenty-second 2x window");
        Assert(ledger.Multiplier(at)==1.5 && ledger.FrenzyUntil==at.AddMinutes(2),"Existing shared two-minute half-step streak remains intact");
        var until=ledger.BugFrenzyUntil;
        Assert(!ProductivityScoring.DirectSquash(ledger,"boost",at.AddSeconds(10)) && ledger.BugFrenzyUntil==until,"Duplicate events cannot refresh Bug Frenzy");
        ProductivityScoring.Add(ledger,"work","Task","Work",100,at.AddSeconds(1));
        Assert(ledger.Events.Last().BonusPoints==100,"Eligible points receive a non-stacking 2x boost");
        Assert(!ledger.BugFrenzyActive(at.AddSeconds(20)) && ledger.EffectiveMultiplier(at.AddSeconds(20))==1.5,"Exact expiry restores the still-active shared streak after resume");
        ProductivityScoring.DirectSquash(ledger,"refresh",at.AddSeconds(10));
        Assert(ledger.BugFrenzyUntil==at.AddSeconds(30),"A new direct squash refreshes twenty seconds");
        ProductivityScoring.DirectSquash(ledger,"old",at.AddSeconds(2));
        Assert(ledger.BugFrenzyUntil==at.AddSeconds(30),"Delayed historical events cannot move the current boost backward");
        var candy=new ProductivityLedger();ProductivityScoring.CollectCandy(candy,Guid.NewGuid(),at);
        Assert(!candy.BugFrenzyActive(at) && candy.Multiplier(at)==1.5,"Candy advances the shared streak without activating direct-only Bug Frenzy");
        var capped=new ProductivityLedger();ProductivityScoring.DirectSquash(capped,"cap",at,1.5);
        Assert(capped.EffectiveMultiplier(at)==1.5,"Configured bonus cap also bounds the short boost");
        Assert(Continuity.Clone(ledger).BugFrenzyUntil==ledger.BugFrenzyUntil && !ledger.BugFrenzyActive(at.AddSeconds(-1)),"Boost timestamps persist and do not grant future bonuses retroactively");
    }
    private static void CheckRecovery028()
    {
        var oldLedger=JsonSerializer.Deserialize<ProductivityLedger>("{\"FrenzyMultiplier\":1.5,\"Events\":[{\"Id\":\"00000000-0000-0000-0000-000000000001\",\"Key\":\"old-task\",\"Category\":\"Task completed\",\"Description\":\"Recorded award\",\"At\":\"2026-10-01T12:00:00Z\",\"BasePoints\":100,\"BonusPoints\":50}]}")!;
        Assert(oldLedger.Score==150 && oldLedger.Events.Single().Id==Guid.Parse("00000000-0000-0000-0000-000000000001") && oldLedger.BugFrenzyStartedAt==null,"Pre-.28 ledger JSON preserves recorded score/event identity without inventing a new boost");
        var directory=Path.Combine(output,"writer028");Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"state.json");var store=new StateStore(path);
        var widget=WidgetLibrary.Create("Counter");widget.Value=42;widget.PlacementX=.2;widget.PlacementY=1200;
        var state=new AppState{Notes="baseline",WidgetLayoutInitialized=true,Widgets=[widget],Tasks=[new(){Text="Permanent",Permanent=true}]};store.Save(state);
        var id=Continuity.Current(state.Workspace!,store.UserId).Current.SessionId;
        using(var child=new Process{StartInfo=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true}})
        {
            child.StartInfo.ArgumentList.Add("--save-writer028");child.StartInfo.ArgumentList.Add(path);child.Start();
            try
            {
                var wait=Stopwatch.StartNew();while(!File.Exists(path+".ready") && !child.HasExited && wait.Elapsed<TimeSpan.FromSeconds(10))System.Threading.Thread.Sleep(5);
                Assert(File.Exists(path+".ready") && !child.HasExited,"Disposable child writer starts without opening the user's workspace");
                wait.Restart();while(!Directory.EnumerateFiles(directory,"*.tmp").Any() && !child.HasExited && wait.Elapsed<TimeSpan.FromSeconds(2))System.Threading.Thread.Sleep(1);
                child.Kill(true);child.WaitForExit();
            }
            finally{if(!child.HasExited){child.Kill(true);child.WaitForExit();}}
        }
        File.WriteAllText(path+".orphan.tmp","{partial");
        var restarted=new StateStore(path);var recovered=restarted.Load();
        Assert(Continuity.Current(recovered.Workspace!,restarted.UserId).Current.SessionId==id && recovered.Widgets.Single().Id==widget.Id && recovered.Widgets.Single().Value==42 && recovered.Tasks.Single().Permanent,"Process termination during repeated atomic saves preserves session/widget identities, counters and permanent tasks");
        Assert(recovered.Widgets.Single().PlacementY==1200 && (recovered.Notes=="baseline" || recovered.Notes.StartsWith("generation ")),"Restart ignores orphan partial temp files and retains a committed layout/note generation");
        restarted.Save(recovered);recovered.Notes="second checkpoint";restarted.Save(recovered);
        var backup=File.ReadAllText(path+".bak");File.WriteAllText(path,"{broken primary");
        var fallback=new StateStore(path);var fromBackup=fallback.Load();
        Assert(Continuity.Current(fromBackup.Workspace!,fallback.UserId).Current.SessionId==id && fallback.Warning!=null,"Corrupt primary restores the existing same-owner backup");
        fallback.Save(fromBackup);
        Assert(File.ReadAllText(path+".bak")==backup && Directory.EnumerateFiles(directory,"*.recovered-*").Any(),"Saving recovered data preserves both valid backup and unreadable primary evidence");
        var before=File.ReadAllText(path);using(var locked=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.None))
            Throws<IOException>(()=>fallback.Save(fromBackup),"An exclusive file lock reports save failure");
        Assert(File.ReadAllText(path)==before,"Failed write leaves the committed primary unchanged");
        fromBackup.Notes="retry accepted";fallback.Save(fromBackup);
        Assert(new StateStore(path).Load().Notes=="retry accepted","A later retry persists pending edits without replacing session identity");
    }
    private static void CheckSchedule028()
    {
        var start=NexaTime.Instant(new DateTime(2030,1,7,13,0,0),"America/Chicago");
        var session=new Session{Title="Mon/Wed",Start=start,End=start.AddMinutes(75),AutomaticScheduling=true,TimeZoneId="America/Chicago",ScheduledDays=[DayOfWeek.Monday,DayOfWeek.Wednesday]};
        var tasks=WidgetLibrary.Create("Task List");tasks.Items=[new(){Text="Keep",Permanent=true},new(){Text="Session only"}];
        var store=new StateStore(Path.Combine(output,"schedule028.json"));
        var record=Continuity.NewDashboard("Schedule","Study",new DashboardData{Session=session,WidgetLayoutInitialized=true,Widgets=[tasks]},store.UserId);
        var model=Continuity.Activate(new WorkspaceDocument{UserId=store.UserId,ActiveDashboardId=record.DashboardId,Dashboards=[record],Settings=new(){ReduceMotion=true}},store.UserId);store.Save(model);
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(50));
        void Reconcile(DateTimeOffset time)=>typeof(MainWindow).GetMethod("ReconcileSchedule",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(main,[time]);
        Reconcile(session.End);var current=Continuity.Current(main.state.Workspace!,store.UserId).Current;var oldId=current.SessionId;
        Assert(current.Recap!=null && current.CompletedCheckpointAt==session.End,"Monday 2:15 PM finalizes exactly the recorded session");
        Reconcile(session.End.AddMinutes(5));Assert(current.CompletedCheckpointAt==session.End.AddMinutes(5),"Monday 2:20 PM commits the five-minute checkpoint");
        var next=WeeklySchedule.Next(session,session.End)!;
        Reconcile(next.Start.AddMinutes(-10));
        Assert(Descendants(main).OfType<Button>().Any(b=>Equals(b.Content,"Postpone Reset")),"Wednesday 12:50 PM exposes the actual postpone-reset action");
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))Reconcile(next.Start.AddMinutes(-5));
        Assert(Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId==oldId,"Failed pre-reset save cannot clear the previous session");
        current.ResetDeferred=true;current.ResetPostponedFor=next.Start;main.SaveNow();main.Close();
        main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(40));
        Assert(Continuity.Current(main.state.Workspace!,store.UserId).Current.ResetDeferred,"Postponed reset survives application restart");
        main.CompleteDeferredResetAt(next.Start.AddMinutes(1));var dashboard=Continuity.Current(main.state.Workspace!,store.UserId);
        Assert(dashboard.Current.SessionId!=oldId && dashboard.History.Count(s=>s.SessionId==oldId)==1 && dashboard.Current.Data.Session!.Start==next.Start,"Completing postponed reset archives one old identity and starts the correct Wednesday occurrence");
        Assert(dashboard.Current.Data.Widgets.Single(w=>w.Kind=="Task List").Items.Count==2 && dashboard.Current.Data.Widgets.Single(w=>w.Kind=="Task List").Items.Any(t=>t.Permanent),"Reset preserves all reusable tasks without changing permanence");
        var newId=dashboard.Current.SessionId;main.CompleteDeferredResetAt(next.Start.AddMinutes(2));
        Assert(Continuity.Current(main.state.Workspace!,store.UserId).Current.SessionId==newId,"Repeated completion cannot duplicate an already active occurrence");main.Close();
    }
    private static void CheckEcosystem028()
    {
        var creepy=CreateWorld(BugBehavior.Creepy);var activities=new HashSet<BugActivity>();
        for(var tick=0;tick<2400;tick++){creepy.Advance(.1);foreach(var bug in creepy.Bugs)activities.Add(bug.Activity);}
        Assert(creepy.EcosystemSettings.Behavior==BugBehavior.Creepy && activities.Contains(BugActivity.Hiding) && !activities.Contains(BugActivity.Raiding),"Restored Creepy executes actual hiding/low-conflict AI without raids");
        var saved=creepy.CaptureEcosystem("synthetic-owner");saved.Validate("synthetic-owner");var restored=new BugManager(4);restored.RestoreEcosystem(Continuity.Clone(saved));
        Assert(restored.WorldId==creepy.WorldId && restored.EcosystemSettings.Behavior==BugBehavior.Creepy && restored.Bugs.Select(b=>b.InteractionId).SequenceEqual(creepy.Bugs.Select(b=>b.InteractionId)),"Creepy mode and original world/bug identities persist");
        var world=CreateWorld(BugBehavior.Combative);Step(world,60);
        var attacker=world.Bugs.First(b=>b.Species==BugSpecies.Spiders);var defender=world.Bugs.First(b=>b.Species==BugSpecies.Ladybugs);
        world.Bugs.RemoveAll(b=>b!=attacker && b!=defender);world.WorldSpawnDelay=1000;
        attacker.Position=new(800,500);defender.Position=new(810,500);attacker.Target=defender.Position;attacker.EnemyId=defender.InteractionId;attacker.Activity=BugActivity.Raiding;attacker.Decision=100;attacker.Health=100;
        defender.Target=defender.Position;defender.EnemyId=null;defender.Activity=BugActivity.Sleeping;defender.Decision=100;defender.Health=100;
        world.Advance(.1);
        Assert(defender.EnemyId==attacker.InteractionId && defender.Activity==BugActivity.Defending,"Attacked non-spider immediately receives a defensive retaliation order");
        Step(world,.5);Assert(attacker.Health<100,"A previously passive defender actually damages its attacker");
        Assert(new EcosystemOptions().Adorable && !new EcosystemOptions().AdvancedAnimations,"Advanced locomotion remains optional/off with static cute fallback");
        var store=new StateStore(Path.Combine(output,"mode-controls028.json"));var state=new AppState();store.Save(state);state.Workspace!.Settings.SeasonalMode="Off";store.Save(state);
        store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.Low,new EcosystemOptions{Behavior=BugBehavior.Classic}));
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(40));
        var settings=new SettingsDialog(main,store,main.state,main,"Add Bugs");settings.Show();
        var behavior=Descendants(settings).OfType<ComboBox>().Single(c=>c.Items.OfType<BugBehavior>().Count()==3);
        var appearance=Descendants(settings).OfType<ComboBox>().Single(c=>c.Items.OfType<string>().SequenceEqual(new[]{"Classic","Creepy","Halloween"}));
        behavior.SelectedItem=BugBehavior.Cozy;
        Assert(Equals(appearance.SelectedItem,"Classic"),"Behavior changes keep the visible appearance selector synchronized with legacy automatic matching");
        appearance.SelectedItem="Classic";behavior.SelectedItem=BugBehavior.Cozy;settings.FlushPendingChanges();
        Assert(NexaEcosystem.For(store).Manager.EcosystemSettings.EffectiveAppearance==BugAppearance.Normal && store.LoadBugPreferences().Ecosystem?.MatchAppearance==false,"An explicit appearance remains independent of later behavior changes and persists");
        settings.Close();main.Close();
    }
    private static void CheckLargeWorkspace028()
    {
        var store=new StateStore(Path.Combine(output,"large028.json"));
        var widgets=Enumerable.Range(0,36).Select(i=>{var w=WidgetLibrary.Create("Notes");w.Text="Saved note "+i;w.PlacementX=0;w.PlacementY=i*360;return w;}).ToList();
        store.Save(new AppState{WidgetLayoutInitialized=true,Widgets=widgets});
        var main=OffscreenHandoff(new MainWindow(store));main.Show();Pump(TimeSpan.FromMilliseconds(100));
        Assert(main.DeferredWidgetBodies>20 && main.state.Widgets.Count(w=>w.Kind=="Notes")==36 && main.state.Widgets.Count(w=>w.Kind=="Pixel timer")==1,"Large Space defers offscreen bodies while preserving every widget model");
        main.WidgetScroller.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(150));
        Assert(Descendants(main.WidgetCanvas).OfType<TextBox>().Any(t=>t.Text=="Saved note 35"),"Scrolling to a deferred final widget constructs its real saved editor");
        main.SaveNow();Assert(store.Load().Widgets.Where(w=>w.Kind=="Notes").Select(w=>w.Text).SequenceEqual(widgets.Select(w=>w.Text)),"Saving a partially materialized visual tree preserves all offscreen notes");
        Assert(main.WidgetLayoutPasses>0 && main.WidgetLayoutMilliseconds>=0,"Layout phase instrumentation remains available");main.Close();
    }
    private static void Diagnostics028()
    {
        var lines=new List<string>();var watch=Stopwatch.StartNew();var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
        var template=CreateWorld(BugBehavior.Combative);Step(template,60);var doc=template.CaptureEcosystem("synthetic-owner");
        var wins=BugManager.AllSpecies.ToDictionary(s=>s,s=>0);var deaths=BugManager.AllSpecies.ToDictionary(s=>s,s=>0);var draws=0;
        foreach(var first in BugManager.AllSpecies)foreach(var second in BugManager.AllSpecies.Where(s=>s!=first))
        {
            var world=new BugManager(124);world.RestoreEcosystem(Continuity.Clone(doc));world.WorldSpawnDelay=1000;
            var a=world.Bugs.First(b=>b.Species==first);var b=world.Bugs.First(b=>b.Species==second);world.Bugs.RemoveAll(bug=>bug!=a && bug!=b);
            foreach(var bug in world.Bugs){bug.Health=100;bug.Carrying=0;bug.PartnerId=null;bug.RaidTarget=null;bug.Decision=100;bug.State=BugState.Crossing;}
            a.Position=new(800,500);b.Position=new(810,500);a.EnemyId=b.InteractionId;a.Target=b.Position;a.Activity=BugActivity.Raiding;b.EnemyId=null;b.Target=b.Position;b.Activity=BugActivity.Sleeping;
            for(var tick=0;tick<600 && a.Health>0 && b.Health>0;tick++)world.Advance(.1);
            if(a.Health<=0){deaths[first]++;wins[second]++;}else if(b.Health<=0){deaths[second]++;wins[first]++;}else draws++;
            lines.Add($"Duel {first} attacks {second}: health {a.Health:F2}/{b.Health:F2}; defending {b.EnemyId==a.InteractionId}; kills {a.Kills}/{b.Kills}");
        }
        lines.Add("Thirty ordered pair fixtures, same seed and initial health; not army/leader/territory balance certification. First-attacker and species damage differences are intentional scenario inputs.");
        foreach(var species in BugManager.AllSpecies)lines.Add($"{species}: fixture wins {wins[species]}, deaths {deaths[species]}, survivors {10-deaths[species]}/10");
        lines.Add("Unfinished duels: "+draws);
        var store=new StateStore(Path.Combine(output,"cycle028.json"));
        store.Save(new AppState{WidgetLayoutInitialized=true,Widgets=Enumerable.Range(0,30).Select(i=>WidgetLibrary.Create(i%2==0?"Notes":"Counter")).ToList()});
        var references=new List<WeakReference>();
        for(var cycle=0;cycle<8;cycle++)
        {
            var advanced=cycle%2==1;store.SaveBugPreferences(new(true,BugSpecies.AllBugs,BugDensity.High,new EcosystemOptions{AdvancedAnimations=advanced,Behavior=BugBehavior.Cozy}));
            references.Add(OpenClose028(store));Pump(TimeSpan.FromMilliseconds(60));GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
            process.Refresh();
            Assert(NexaEcosystem.For(store).ViewCount==0,"Closed large workspace releases its ecosystem view, cycle "+cycle);
            lines.Add($"Cycle {cycle}: advanced {advanced}; elapsed {watch.Elapsed.TotalSeconds:F2}s; managed {GC.GetTotalMemory(false)} bytes; working set {process.WorkingSet64} bytes; closed windows still reachable {references.Count(r=>r.IsAlive)}");
        }
        lines.Add($"Diagnostic duration {watch.Elapsed.TotalSeconds:F2}s; process CPU {(process.TotalProcessorTime-cpu).TotalSeconds:F2}s. Alternating animation windows were brief and offscreen: no physical FPS/GPU or long-session stability claim. Reachability snapshots are observations, not proof of a leak.");
        File.WriteAllLines(Path.Combine(output,"diagnostics028.txt"),lines);foreach(var line in lines)Console.WriteLine(line);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference OpenClose028(StateStore store)
    {
        var window=OffscreenHandoff(new MainWindow(store));Application.Current.MainWindow=window;window.Show();Pump(TimeSpan.FromMilliseconds(180));
        var reference=new WeakReference(window);window.Close();Application.Current.MainWindow=null;return reference;
    }
}
