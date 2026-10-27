using System.Windows;
using System.IO;
using System.Text.Json;
using SessionDashboard;
internal static partial class Program
{
    private static BugManager Infinite021(int seed=21,bool migration=false)
    {
        var m=new BugManager(seed);m.Configure(new(true,BugSpecies.AllBugs,BugDensity.Arcade,new ArcadeSettings{AllowNewCivilizations=migration}));
        m.InitializeInfiniteWorld(new Size(1600,1000));return m;
    }
    private static void CheckInfinite021()
    {
        var m=Infinite021();var world=m.WorldId;var survivor=m.Colonies[BugSpecies.Ants];var leader=survivor.Leader;var ids=m.Colonies.Values.Select(c=>c.Id).ToArray();
        foreach(var c in m.Colonies.Values)if(c!=survivor) {c.Leader.Protection=0;m.ApplyBattleDamage(leader,c.Leader,99999);}
        for(var i=0;i<3000;i++)m.Advance(.08);
        Assert(m.WorldId==world && m.MatchNumber==1 && m.MatchAge>230 && m.Phase!=BattlePhase.Victory,"Infinite victory preserves world, clock and surviving army without a round reset");
        Assert(m.Colonies.Values.Select(c=>c.Id).SequenceEqual(ids) && m.History.Count(e=>e.Type==ColonyEventType.Victory)==1,"Stable faction IDs and one genuine domination event survive an extended victory");
        var time=m.MatchAge;m.WorldPaused=true;for(var i=0;i<100;i++)m.Advance(.08);Assert(m.MatchAge==time && !m.ApplyBattleDamage(null,leader,1),"Pausing stops the entire world clock and combat damage");m.WorldPaused=false;
        m.Configure(m.Preferences with{Arcade=m.BattleOptions with{ShowScoreboard=false}});
        Assert(m.Colonies[BugSpecies.Ants]==survivor && m.MatchAge==time,"Visual settings preserve the live Infinite world");
        var settlers=Infinite021(22,true);var old=settlers.Colonies[BugSpecies.Flies].Id;
        foreach(var c in settlers.Colonies.Values)if(c.Species!=BugSpecies.Ants) {c.Leader.Protection=0;settlers.ApplyBattleDamage(null,c.Leader,99999);}
        for(var i=0;i<1600;i++)settlers.Advance(.08);
        CheckNaturalWeb021();
        CheckTerritory021();
        CheckStrategy021();
        CheckPersistence021();
        CheckEndurance021();
        CheckBattlefield021();
        var emerged=settlers.History.Where(e=>e.Type==ColonyEventType.Emerged).ToArray();
        Assert(emerged.Length==1 && emerged[0].FactionId>6 && settlers.MatchNumber==1,"Controlled migration creates a new identity without resurrecting old factions or restarting the world");
    }
    private static void CheckPersistence021()
    {
        var m=Infinite021(2121);for(var i=0;i<1200;i++)m.Advance(.033);
        var snapshot=m.CaptureWorld();var json=JsonSerializer.Serialize(snapshot,ArcadeWorldStore.JsonOptions);
        var clone=BugManager.RestoreWorld(JsonSerializer.Deserialize<ArcadeWorldSnapshot>(json,ArcadeWorldStore.JsonOptions)!);
        Assert(JsonSerializer.Serialize(clone.CaptureWorld(),ArcadeWorldStore.JsonOptions)==json,"Round trip preserves developed units, resources, identities, webs, history and pending waves");
        for(var i=0;i<600;i++) {m.Advance(.033);clone.Advance(.033);}
        Assert(JsonSerializer.Serialize(m.CaptureWorld(),ArcadeWorldStore.JsonOptions)==JsonSerializer.Serialize(clone.CaptureWorld(),ArcadeWorldStore.JsonOptions),"Saved random state and combat references resume deterministically for 600 steps");
        var directory=Path.Combine(output,"world-persistence");Directory.CreateDirectory(directory);var path=Path.Combine(directory,"arcade-world.json");var storage=new ArcadeWorldStore(path);
        storage.Save(m.CaptureWorld());var previous=m.MatchAge;m.Advance(.033);storage.Save(m.CaptureWorld());
        Assert(storage.Load().World?.Time==m.MatchAge && File.Exists(path+".bak"),"Validated atomic replacement keeps a previous valid save");
        File.WriteAllText(path,"corrupted world");var recovery=storage.Load();
        Assert(recovery.Blocked && recovery.HasBackup && recovery.World?.Time==previous && File.ReadAllText(path)=="corrupted world","Corrupt primary is preserved and a valid backup is offered without silent overwrite");
        using(var controller=new ArcadeWorldController(path))
        {
            controller.Attach(new object());controller.SetPaused(true);controller.ApplySettings(controller.Manager.BattleOptions with{ShowScoreboard=false});controller.SaveNow();
            Assert(controller.LoadBlocked && !controller.Running && File.ReadAllText(path)=="corrupted world","Blocked load cannot run, autosave or replace progress through visual settings");
            controller.RecoverBackup();Assert(!controller.LoadBlocked && controller.Manager.MatchAge==previous && Directory.GetFiles(directory,"*.unreadable-*").Length==1,"Explicit backup recovery archives unreadable data before resuming the saved identity");
            controller.SetPaused(true);controller.SaveNow();
        }
        using(var resumed=new ArcadeWorldController(path))
        {
            var t=resumed.Manager.MatchAge;var dashboard=new object();var battlefield=new object();resumed.Attach(dashboard);resumed.Attach(battlefield,10);
            Assert(resumed.Manager.WorldPaused && !resumed.Running && resumed.ActiveSurface==battlefield,"Saved pause and one authoritative controller are shared across both views");
            resumed.SetPaused(false);Pump(TimeSpan.FromMilliseconds(160));
            Assert(resumed.Manager.MatchAge>t && resumed.Manager.MatchAge-t<.3,"Two attached surfaces do not double simulation progression");
            resumed.SetPaused(true);t=resumed.Manager.MatchAge;Pump(TimeSpan.FromMilliseconds(100));Assert(resumed.Manager.MatchAge==t,"Paused controller stops movement, combat and world time");
            var identity=resumed.Manager.WorldId;resumed.Detach(battlefield);Assert(resumed.Manager.WorldId==identity && resumed.ActiveSurface==dashboard,"Returning from battlefield preserves the same live world");
            Assert(!ArcadeWorldCommands.TryReset(resumed,()=>false) && resumed.Manager.WorldId==identity,"Declining reset confirmation leaves the shared world unchanged");
            resumed.ResetConfirmed();Assert(resumed.Manager.WorldId!=identity && resumed.Manager.MatchAge==0 && resumed.Manager.Colonies.Count==6,"Confirmed world reset creates a fresh identity and preserves the prior save as backup");
            resumed.Detach(dashboard);
        }
        File.WriteAllText(path,"bad primary");File.WriteAllText(path+".bak","bad backup");
        using(var blocked=new ArcadeWorldController(path))
        {blocked.Attach(new object());blocked.SaveNow();Assert(blocked.LoadBlocked && !blocked.Running && File.ReadAllText(path)=="bad primary" && File.ReadAllText(path+".bak")=="bad backup","Two corrupt saves remain preserved; a fresh empty world is never autosaved over them");}
    }

}
