using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckNations024()
    {
        var metrics=new Dictionary<string,object>();var store=new StateStore(Path.Combine(output,"nations","state.json"),gameContext:true);
        var note=WidgetLibrary.Create("Notes");note.Text="Nations do not change notes";note.CustomSize=true;store.Save(new AppState{WidgetLayoutInitialized=true,Widgets=[note]});
        var clock=Stopwatch.StartNew();var session=Complete023(LivingWorlds.Create(store,"Kingdom map","kingdoms",1024,576));clock.Stop();var world=session.Nations!;var manager=session.Controller.Manager;session.Controller.SetPaused(true);
        Assert(world!=null && world.Nations.Count==6 && world.Nations.Select(n=>n.Id).Distinct().Count()==6,"Six founding nations have unique persistent IDs");
        Assert(world!.Nations.All(n=>session.Terrain.At(n.Capital).Biome is not (TerrainBiome.DeepOcean or TerrainBiome.ShallowOcean or TerrainBiome.River or TerrainBiome.Lake)),"Founding capitals occupy dry navigable land");
        Assert(world.Nations.All(n=>world.OwnerAt(n.Capital)==n.Id && world.Regions(n.Id).Length==9),"Connected starting claims include each capital without overlap");
        Assert(world.Nations.All(n=>world.Components(n.Id).Count==1),"Founding territories connect through actual land and fordable rivers");
        Assert(Enumerable.Range(0,world.Land.Count).Where(i=>world.Owner(i)!=null).All(i=>world.Land.Land[i]!=0),"Ordinary ownership never claims ocean-only regions");
        Assert(world.Nations.Select(n=>n.Color).Distinct().Count()==6 && world.Nations.All(n=>n.CivilizationId==null),"Stable nation colors are distinct and historical identities remain opt-in");
        Assert(LivingSpecies.Map.Values.Select(v=>v.Letter).Order().SequenceEqual(new[]{"A","S","L","R","C","F"}.Order()),"Species map letters use the centralized six-species mapping");
        var initCopy=BugManager.RestoreWorld(manager.CaptureWorld());var initTime=Stopwatch.StartNew();var initialized=LivingNationWorld.Initialize(world.Land,initCopy,true);initTime.Stop();metrics["NationInitializationMs"]=initTime.Elapsed.TotalMilliseconds;
        Assert(initialized.CaptureJson().GetRawText()==world.CaptureJson().GetRawText(),"Founding identities, names, colors and placement reproduce deterministically without combat RNG");
        var captureTime=Stopwatch.StartNew();var json=world.CaptureJson();captureTime.Stop();var allocated=GC.GetAllocatedBytesForCurrentThread();var restoreTime=Stopwatch.StartNew();var restored=LivingNationWorld.Restore(world.Land,manager.WorldId,json);restoreTime.Stop();metrics["PoliticalCaptureMs"]=captureTime.Elapsed.TotalMilliseconds;metrics["PoliticalRestoreMs"]=restoreTime.Elapsed.TotalMilliseconds;metrics["RestoreAllocatedBytes"]=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Assert(restored.CaptureJson().GetRawText()==json.GetRawText(),"Nation names, colors, capitals, owners, facts and revisions round-trip exactly");
        var first=world.Nations[0];var second=world.Nations[1];var neutral=Enumerable.Range(0,world.Land.Count).First(i=>world.Owner(i)==null && world.Land.Ink[i]!=0 && (world.Land.Center(i)-first.Capital).Length>session.Info.Extent*.3);
        var extra=world.AddNation(new(Guid.NewGuid(),"Western Ant Republic","Ants",0xff9070b0,world.Land.Center(neutral),null,manager.MatchAge,CivilizationId:"historical:roman-republic",GovernmentId:"republic"));
        world.Commit(neutral,null,extra.Id,manager.MatchAge,OwnershipReason.Settlement,77);
        Assert(extra.Species=="Ants" && extra.Id!=world.Nations.First(n=>n.Species=="Ants").Id && world.Owner(neutral)==extra.Id,"A second same-species nation retains its own identity, color and territory");
        world.Rename(extra.Id,"The Western Republic");Assert(world.Owner(neutral)==extra.Id && world.Find(extra.Id)!.Name=="The Western Republic","Renaming does not detach ownership from nation identity");
        var initialArea=world.Area(first.Id);OwnershipFact? observed=null;world.OwnershipChanged+=e=>observed=e;
        world.Commit(neutral,extra.Id,first.Id,manager.MatchAge,OwnershipReason.PoliticalTransfer);
        Assert(world.Area(first.Id)==initialArea+world.Land.Area(neutral) && world.Area(extra.Id)==0 && observed is {PreviousOwner:var old,NewOwner:var next} && old==extra.Id && next==first.Id,"Committed transfers update statistics and publish previous/new owner facts");
        world.Contest(neutral,second.Id);Assert(world.IsContested(neutral),"Explicit contested state is represented independently of ownership");
        var contestCopy=LivingNationWorld.Restore(world.Land,manager.WorldId,world.CaptureJson());Assert(contestCopy.IsContested(neutral),"Contested ownership survives snapshot reload");world.Contest(neutral,null);
        bool Rejected(Action action){try{action();return false;}catch(Exception e) when(e is InvalidDataException or ArgumentException or InvalidOperationException){return true;}}
        Assert(Rejected(()=>world.Commit(-1,null,first.Id,0,OwnershipReason.Conquest)) && Rejected(()=>world.Commit(neutral,second.Id,first.Id,0,OwnershipReason.Conquest)),"Out-of-bounds and stale-owner mutations fail validation");
        Assert(Rejected(()=>world.Commit(neutral,first.Id,Guid.NewGuid(),0,OwnershipReason.Conquest)),"Unknown nation targets are rejected");
        Assert(Task.Run(()=>Rejected(()=>world.Rename(first.Id,"Worker mutation"))).GetAwaiter().GetResult(),"Background threads cannot mutate the authoritative political world");
        var analysis=world.Components(first.Id);var analysisCount=world.AnalysisCount;for(var i=0;i<50;i++)world.Components(first.Id);
        Assert(world.AnalysisCount==analysisCount && analysis.All(c=>world.OwnerAt(c.Anchor)==first.Id),"Connected-component anchors remain on owned land and are cached across unchanged queries");
        var ruler=manager.Colonies.Values.First(c=>c.Id==first.FactionId).Leader;var rulerPosition=ruler.Position;ruler.Position+=new Vector(50,50);
        Assert(world.Components(first.Id)[0].Anchor==analysis[0].Anchor,"Territorial labels do not follow moving rulers");ruler.Position=rulerPosition;
        var detached=world.Components(first.Id);Assert(detached.Count>=2 && detached[0].Tiles.Length>detached[1].Tiles.Length,"Disconnected land components retain separate focus bounds and a largest-region label");
        world.Commit(neutral,first.Id,null,manager.MatchAge,OwnershipReason.Abandonment);
        Assert(world.Components(first.Id).All(c=>world.OwnerAt(c.Anchor)==first.Id) && world.AnalysisCount>analysisCount,"Ownership revisions invalidate affected anchors and retain valid centers");
        var mutations=Stopwatch.StartNew();for(var i=0;i<100;i++){world.Commit(neutral,null,first.Id,0,OwnershipReason.Occupation);world.Commit(neutral,first.Id,null,0,OwnershipReason.Abandonment);}mutations.Stop();metrics["Ownership200ChangesMs"]=mutations.Elapsed.TotalMilliseconds;
        var snapshot=world.Capture();
        bool Bad(LivingPoliticsSnapshot s)=>Rejected(()=>LivingNationWorld.Restore(world.Land,manager.WorldId,JsonSerializer.SerializeToElement(s,ArcadeWorldStore.JsonOptions)));
        Assert(Bad(snapshot with{SchemaVersion=99}) && Bad(snapshot with{WorldId=Guid.NewGuid()}),"Optional political version and world identity mismatches are rejected");
        Assert(Bad(snapshot with{Nations=snapshot.Nations.Append(snapshot.Nations[0]).ToArray()}) && Bad(snapshot with{Revision=-1}),"Duplicate nation identities and invalid revisions are rejected");
        var badOwners=snapshot.Owners.ToArray();badOwners[0]=900;Assert(Bad(snapshot with{Owners=badOwners}),"Unknown serialized owner slots are rejected");
        Assert(Bad(snapshot with{Nations=snapshot.Nations.Select((n,i)=>i==0 ? n with{Capital=new Point(-1,0)} : n).ToArray()}),"Out-of-bounds capitals are rejected");
        Assert(Bad(snapshot with{Nations=snapshot.Nations.Select((n,i)=>i==0 ? n with{Color=0} : n).ToArray()}),"Invalid nation colors are rejected");
        var home=new HomeWindow(store);home.Show();var main=new MainWindow(store){Width=1200,Height=820};main.Show();var board=BattlefieldWindow.ShowFor(main,store);board.Activate();Pump(TimeSpan.FromMilliseconds(200));board.FitWorld();Pump(TimeSpan.FromMilliseconds(100));
        Assert(ReferenceEquals(main.livingWorld!.Nations,board.Living!.Nations) && ReferenceEquals(board.World.Manager.Nations,world),"Dashboard and battlefield share one authoritative nation world");
        var map=board.Battlefield.TerrainView!.NationView;var component=world.Components(first.Id)[0];var anchor=board.Battlefield.WorldToScreen(component.Anchor);
        Assert(map.Hit(anchor)?.NationId==first.Id,"Clicking a native map letter resolves its nation without color sampling");
        var capScreen=board.Battlefield.WorldToScreen(first.Capital);Assert(map.Hit(capScreen)?.NationId==first.Id,"Territory hit testing uses authoritative land ownership");
        var oceanCell=Enumerable.Range(0,world.Land.Count).First(i=>world.Land.Land[i]==0);var ocean=new Point((oceanCell%world.Land.Columns*8+4)*6,(oceanCell/world.Land.Columns*8+4)*6);
        Assert(!map.FillContains(ocean) && world.OwnerAt(ocean)==null,"Rendered ownership geometry is clipped away from ocean tiles");
        var cell=world.Regions(first.Id).First(i=>world.Land.Land[i]==ulong.MaxValue);var internalPoint=new Point((cell%world.Land.Columns*8+3)*6,(cell/world.Land.Columns*8+3.5)*6);
        Assert(!map.BorderContains(internalPoint),"No political border is drawn between same-owner interior tiles");
        var rebuilds=map.Rebuilds;board.Battlefield.RefreshFrame();Pump(TimeSpan.FromMilliseconds(50));Assert(map.Rebuilds==rebuilds,"Unchanged political chunks reuse cached border and fill geometry");
        Until023(()=>board.Battlefield.TerrainView!.DetailReady && main.bugsOverlay!.TerrainView!.DetailReady,"Both map views settle before measuring camera interaction");
        var focusWatch=Stopwatch.StartNew();board.SelectNation(new(first.Id,component.Key,true));Assert(board.FocusingNation && board.SelectedNationId==first.Id && board.FollowedUnitId==null,"Territory selection starts a nonblocking camera transition and exits bug following");
        var editor=Descendants(main).OfType<TextBox>().First(t=>t.Text==note.Text);var edits=0;var input=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(30)};input.Tick+=(_,_)=>editor.Text="Political map edits "+edits++;input.Start();
        Pump(TimeSpan.FromMilliseconds(500));input.Stop();Console.WriteLine($"Focus accepted edits: {edits}");Assert(edits>=5,"Dashboard editing remains responsive while both views and camera interpolation are active");metrics["FocusEditTicks"]=edits;
        Assert(!board.FocusingNation && world.OwnerAt(board.FocusDestination!.Value)==first.Id,"Focus completes at a valid owned interior center");
        var topLeft=board.Battlefield.WorldToScreen(component.Bounds.TopLeft);var bottomRight=board.Battlefield.WorldToScreen(component.Bounds.BottomRight);
        Assert(topLeft.X>=0 && topLeft.Y>=0 && bottomRight.X<=board.Battlefield.ActualWidth && bottomRight.Y<=board.Battlefield.ActualHeight,"Focused connected territory fits the rectangular viewport");
        board.FocusNation();board.Pan(new Vector(5,2));Assert(!board.FocusingNation,"Manual pan cancels camera focus immediately");board.FocusNation();board.ZoomAt(new Point(100,100),120);Assert(!board.FocusingNation,"Mouse zoom takes over from automatic camera focus");
        board.FollowBug(manager.Bugs.First(BugManager.Alive));Assert(board.SelectedNationId==null && board.FollowedUnitId!=null,"Follow Bug still works after selecting territory");
        board.FitWorld();var corner=board.Battlefield.WorldToScreen(new Point(session.Info.Extent,session.Info.HeightExtent));Assert(corner.X<=board.Battlefield.ActualWidth && corner.Y<=board.Battlefield.ActualHeight,"Return to World still fits the full widescreen map");
        var hitWatch=Stopwatch.StartNew();for(var i=0;i<10000;i++)world.OwnerAt(first.Capital);hitWatch.Stop();
        Until023(()=>board.Battlefield.TerrainView!.DetailReady,"World overview streams to its requested terrain detail");Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"nations-world-map");board.SelectNation(new(first.Id,component.Key,true));Pump(TimeSpan.FromMilliseconds(450));Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"nation-selection");
        var savedZoom=board.Battlefield.Camera.M11;board.Close();board=BattlefieldWindow.ShowFor(main,store);Pump(TimeSpan.FromMilliseconds(80));
        Assert(board.SelectedNationId==first.Id && Math.Abs(board.Battlefield.Camera.M11-savedZoom)<.001,"Saved nation selection and camera restore without an automatic jump");
        foreach(var r in world.Regions(first.Id))world.Commit(r,first.Id,second.Id,manager.MatchAge,OwnershipReason.PoliticalTransfer);session.Controller.SetPaused(true);
        Assert(board.SelectedNationId==null,"Losing all controlled territory clears selection safely");
        foreach(var r in snapshot.Owners.Select((owner,index)=>(owner,index)).Where(p=>p.owner==1).Select(p=>p.index))world.Commit(r,second.Id,first.Id,manager.MatchAge,OwnershipReason.PoliticalTransfer);
        board.Close();main.Close();home.Close();session.Controller.SaveNow();var saved=new ArcadeWorldStore(session.Controller.SavePath).Load();
        Assert(saved.World?.LivingPolitics!=null && saved.World.WorldId==manager.WorldId && !saved.Blocked,"Atomic world save includes one optional political section and preserves world identity");
        var loaded=BugManager.RestoreWorld(saved.World!);var loadedNations=LivingNationWorld.Restore(world.Land,loaded.WorldId,loaded.PendingPolitics!.Value);
        Assert(loadedNations.CaptureJson().GetRawText()==world.CaptureJson().GetRawText(),"Political ownership, identity and event facts survive actual save/load");
        foreach(var generator in new[]{1,2})
        {
            var info=new LivingWorldInfo{Name="Legacy "+generator,Seed="living-023",Tiles=256,GeneratorVersion=generator,WorldId=Guid.NewGuid()};var terrain=new LivingTerrain(info);var legacy=Infinite021(247+generator);legacy.WorldId=info.WorldId;legacy.LivingGeneratorVersion=generator==2 ? 2 : 0;legacy.InitializeInfiniteWorld(new Size(info.Extent,info.HeightExtent));legacy.SeedLivingColonies(terrain);legacy.WorldPaused=true;
            var dir=Path.Combine(output,"migration-"+generator);Directory.CreateDirectory(dir);var oldStore=new StateStore(Path.Combine(dir,"state.json"));var target=Path.Combine(LivingWorlds.Root(oldStore),info.WorldId.ToString("N"));LivingFiles.Save(Path.Combine(target,"world.json"),info);var file=Path.Combine(target,"arcade-world.json");new ArcadeWorldStore(file).Save(legacy.CaptureWorld());var before=File.ReadAllBytes(file);var original=legacy.CaptureWorld();
            var migrated=LivingWorlds.Open(oldStore,info.WorldId);Until023(()=>migrated.PoliticsReady.IsCompleted,"Legacy political compatibility initialization completes asynchronously");
            Assert(migrated.Nations!=null && migrated.Controller.Manager.WorldId==original.WorldId && migrated.Controller.Manager.Bugs.Select(b=>b.UnitId).SequenceEqual(original.Units.Where(u=>u.InScene).Select(u=>u.UnitId)),$"Generator {generator}: migration preserves world and bug identities");
            Assert(migrated.Controller.Manager.CaptureWorld().RandomState==original.RandomState && File.ReadAllBytes(file+".pre-nations").SequenceEqual(before),$"Generator {generator}: migration preserves combat RNG and the pre-migration save");
            var firstMigration=migrated.Nations!.CaptureJson().GetRawText();Assert(LivingWorlds.Open(oldStore,info.WorldId).Nations!.CaptureJson().GetRawText()==firstMigration,$"Generator {generator}: repeated load is idempotent");
            Assert(TerrainChunks.Generate(terrain,new(1,1)).Colors.SequenceEqual(TerrainChunks.Generate(migrated.Terrain,new(1,1)).Colors),$"Generator {generator}: political initialization does not regenerate different terrain");
            migrated.Controller.SaveNow();
        }
        var battle=Infinite021(824);battle.InitializeInfiniteWorld(new Size(1536,1536));var battleTerrain=new LivingTerrain(new LivingWorldInfo{Name="Occupation test",Seed="living-023",Tiles=256});battle.SeedLivingColonies(battleTerrain);battle.Configure(battle.Preferences with{Arcade=battle.BattleOptions with{EnableCampaigns=false}});
        battle.Nations=LivingNationWorld.Initialize(new PoliticalLand(battleTerrain),battle,false);var defender=battle.Colonies[BugSpecies.Ants];var attacker=battle.Colonies[BugSpecies.Centipedes];var nest=battle.Settlements.Single(s=>s.OwnerId==defender.Id);var defended=battle.Nations.OwnerAt(nest.Position);
        foreach(var c in battle.Colonies.Values)c.Resources=-1000;foreach(var b in battle.Bugs){b.Position=b.OrderPoint=new Point(100,100);b.ThinkTimer=b.AttackTimer=10000;b.Protection=1000;}
        var invaders=Enumerable.Range(0,4).Select(i=>Fighter(battle,attacker.Species,nest.Position+new Vector(i*2,0))).ToArray();foreach(var b in invaders){b.ThinkTimer=b.AttackTimer=10000;b.Protection=1000;}
        var coarse=battle.RegionAt(nest.Position);battle.Territories[coarse%12>0 ? coarse-1 : coarse+1].OwnerId=attacker.Id;
        for(var i=0;i<100;i++)battle.Advance(.033);Assert(battle.Nations.OwnerAt(nest.Position)==defended,"A brief military presence cannot immediately take political ownership");
        for(var i=0;i<800;i++)battle.Advance(.033);
        Assert(battle.Nations.OwnerAt(nest.Position)==LivingNationWorld.Identity(battle.WorldId,attacker.Id) && battle.Nations.RecentChanges.Any(e=>e.Reason==OwnershipReason.Conquest && e.SettlementId==nest.Id),"Actual sustained occupation commits conquest facts into Living Worlds ownership");
        // An unreadable optional layer must not block an otherwise valid simulation snapshot.
        var malformed=saved.World!;malformed.LivingPolitics=JsonSerializer.SerializeToElement("future-or-damaged-section");var badDir=Path.Combine(output,"optional-corruption");Directory.CreateDirectory(badDir);var badStore=new StateStore(Path.Combine(badDir,"state.json"));var badPath=Path.Combine(LivingWorlds.Root(badStore),session.Info.WorldId.ToString("N"));LivingFiles.Save(Path.Combine(badPath,"world.json"),session.Info);new ArcadeWorldStore(Path.Combine(badPath,"arcade-world.json")).Save(malformed);
        var recovery=LivingWorlds.Open(badStore,session.Info.WorldId);Until023(()=>recovery.PoliticsReady.IsCompleted,"Damaged optional layer is examined without losing the base world");
        Assert(recovery.Nations==null && recovery.PoliticsWarning!=null && !recovery.Controller.LoadBlocked && recovery.Controller.Manager.Bugs.Count==manager.Bugs.Count,"Malformed optional politics leaves the base world available and displays a recovery warning");
        recovery.Controller.SaveNow();Assert(new ArcadeWorldStore(recovery.Controller.SavePath).Load().World!.LivingPolitics!.Value.GetRawText()==malformed.LivingPolitics.Value.GetRawText(),"Saving preserves the unrecognized optional section rather than replacing it");
        var baseFile=Path.Combine(output,"base-backup-recovery.json");var baseStore=new ArcadeWorldStore(baseFile);baseStore.Save(saved.World!);File.Copy(baseFile,baseFile+".bak");var badBase=JsonSerializer.SerializeToNode(saved.World!,ArcadeWorldStore.JsonOptions)!;badBase["SchemaVersion"]=999;File.WriteAllText(baseFile,badBase.ToJsonString());var baseResult=baseStore.Load();
        Assert(baseResult.Blocked && baseResult.HasBackup && baseResult.World!=null,"Invalid base schema reaches the existing explicit-backup recovery path instead of escaping its error handler");
        metrics["CreateMs"]=clock.Elapsed.TotalMilliseconds;metrics["TopologyMs"]=world.Land.BuildMilliseconds;metrics["TopologyBytes"]=world.Land.Bytes;metrics["SnapshotBytes"]=JsonSerializer.SerializeToUtf8Bytes(world.Capture()).Length;metrics["Hit10000Ms"]=hitWatch.Elapsed.TotalMilliseconds;metrics["BorderBuildMs"]=map.RebuildMilliseconds;metrics["BorderRebuilds"]=map.Rebuilds;metrics["CachedAnalyses"]=world.AnalysisCount;metrics["AnalysisMs"]=world.AnalysisMilliseconds;
        Console.WriteLine(JsonSerializer.Serialize(metrics));File.WriteAllText(Path.Combine(output,"nations024.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
    }
}
