using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckCivilizations024()
    {
        var store=new StateStore(Path.Combine(output,"civilizations","state.json"),gameContext:true);var note=WidgetLibrary.Create("Notes");note.Text="Civilization editing probe";store.Save(new AppState{WidgetLayoutInitialized=true,Widgets=[note]});var session=Complete023(LivingWorlds.Create(store,"Living courts","kingdoms",1024,576,civilizations:true));session.Controller.SetPaused(true);
        var m=session.Controller.Manager;var civic=m.Civilization!;var politics=m.Nations!;var directory=Path.GetDirectoryName(session.Controller.SavePath)!;
        Assert(session.Info.CivilizationMode && civic!=null && civic.State.Nations.Length==6,"Enabled new worlds establish six functional cultural governments");
        Assert(civic!.State.Nations.All(n=>politics.Find(n.NationId)!.CivilizationId==n.CivilizationId) && civic.State.Nations.Select(n=>n.CultureId).Distinct().Count()==6,"Nation civilization/culture references agree with the authoritative political layer");
        Assert(civic.State.Royals.Length==6 && civic.State.Royals.All(r=>m.Bugs.Any(b=>b.UnitId==r.UnitId && b.Name==r.Name)),"Actual founding leaders receive persistent culturally named biographies");
        Assert(civic.State.Dynasties.Length==6 && civic.State.Dynasties.All(d=>d.Id!=d.NationId && d.Rulers.Length==1),"Founding dynasties remain separate from nations and individual IDs");
        Assert(civic.State.Nations.All(n=>n.CapitalSettlementId!=null) && civic.State.Settlements.All(s=>politics.Land.IsCapital(s.Position) && politics.OwnerAt(s.Position)==s.OwnerId),"Six administrative capitals occupy actually owned dry land");
        Assert(civic.State.Settlements.All(s=>s.Stage==SettlementStage.Outpost),"Capital designation does not give a free advanced development stage");
        Assert(civic.State.RecentEvents.Count(e=>e.Kind==CivicEventKind.SettlementFounded)==6 && civic.State.RecentEvents.All(e=>e.Time==m.MatchAge),"Founding events describe committed initial settlements at actual simulation time");
        var raw=civic.CaptureJson();var head=civic.State.JournalHead;var files=Directory.GetFiles(Path.Combine(directory,"civilization-history"));civic.CaptureJson();
        Assert(head!=null && files.SequenceEqual(Directory.GetFiles(Path.Combine(directory,"civilization-history"))),"Repeated saves do not duplicate durable event segments");
        var clone=LivingCivilization.Restore(raw,politics,directory);
        Assert(clone.CaptureJson().GetRawText()==raw.GetRawText(),"Cultural governments, biographies, dynasties, settlements and chronology round-trip exactly");
        var first=civic.State.Settlements[0];var live=m.Bugs.Count(BugManager.Alive);m.WorldPaused=false;civic.Tick(m);
        Assert(m.Bugs.Count(BugManager.Alive)==live && civic.State.Buildings.Length==0,"Civil development neither invents population nor processes before its cadence");
        var initialStage=first.Stage;var initialSequence=civic.State.Sequence;
        m.Configure(m.Preferences with{Arcade=m.BattleOptions with{EnableCampaigns=false,WarFrequency=.25,Aggression=.25}});
        var timing=Stopwatch.StartNew();var civicTicks=new List<double>();
        void Steps(int count)
        {for(var i=0;i<count;i++){m.PrepareLivingStep();m.Advance(.08);m.ResolveLivingStep(session.Terrain,.08);politics.Tick(m);var due=m.MatchAge-civic.State.LastTick>=1;var started=Stopwatch.GetTimestamp();civic.Tick(m);if(due)civicTicks.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);}}
        Steps(40);var job=civic.State.Buildings.First();
        Assert(!job.Completed && job.Progress<1 && job.MaterialCost>0,"Construction begins unfinished and reserves a real material cost");
        Assert(civic.State.Settlements.Sum(s=>s.Materials)<18*6,"Starting construction consumes actual settlement materials");
        Assert(civic.State.Tasks.Length>0 && civic.State.Tasks.Select(t=>t.UnitId).Distinct().Count()==civic.State.Tasks.Length,"Existing eligible units receive unique functional civilian tasks");
        var resource=first.Materials;var sequence=civic.State.Sequence;civic.Tick(m);civic.Tick(m);
        Assert(first.Materials==resource && civic.State.Sequence==sequence,"Repeated callbacks at the same clock do not duplicate production or events");
        var paused=m.WorldPaused;m.WorldPaused=true;Steps(30);m.WorldPaused=paused;
        Assert(first.Materials==resource,"Paused world time prevents resource production");
        Steps(8000);timing.Stop();
        Console.WriteLine($"Civilization development sample: {m.MatchAge:F1}s, population={m.Bugs.Count(BugManager.Alive)}, buildings={civic.State.Buildings.Count(b=>b.Completed)}, maxStage={civic.State.Settlements.Max(s=>s.Stage)}, tasks={civic.State.Tasks.Length}, wall={timing.Elapsed.TotalMilliseconds:F1}ms");
        foreach(var s in civic.State.Settlements)Console.WriteLine($"DEVELOPMENT {s.StyleId}: pop={s.Population}, workers={s.Workforce}, food={s.Food:F1}, materials={s.Materials:F1}, stage={s.Stage}, buildings={civic.State.Buildings.Count(b=>b.SettlementId==s.Id && b.Completed)}");
        Assert(m.Bugs.Count(BugManager.Alive)>6 && m.Bugs.Count(BugManager.Alive)<=m.BattleOptions.HardCap,"Existing biological reproduction grows real insects within the global population budget");
        Assert(civic.State.Settlements.Any(s=>s.Stage>initialStage) && civic.State.RecentEvents.Any(e=>e.Kind==CivicEventKind.BuildingCompleted || e.Kind==CivicEventKind.SettlementUpgraded),"Population, production and completed construction produce real settlement development");
        Assert(civic.State.Buildings.Any(b=>b.Completed) && civic.State.Settlements.Any(s=>civic.Capacity(s)>6),"Completed housing adds functional population capacity");
        Assert(civic.State.Buildings.Where(b=>b.Completed).Select(b=>b.StyleId).Distinct().Count()==6,"All six cultural styles construct their stored architectural archetypes");
        Assert(civic.State.Settlements.Sum(s=>s.Population)==m.Bugs.Count(BugManager.Alive),"Settlement population accounting counts each actual living insect once");
        Assert(civic.State.Settlements.Where(s=>s.Active).All(s=>politics.OwnerAt(s.Position)==s.OwnerId),"Development and colonization preserve actual territorial ownership");
        Assert(civic.State.Settlements.Length<=18 && civic.State.RecentEvents.Length<=64,"Settlement work and in-memory historical window remain bounded");
        civic.CaptureJson(); // Exercise the ordinary save boundary after manual simulation steps.
        Assert(Directory.GetFiles(Path.Combine(directory,"civilization-history"),"*.json").Length>=2,"History is archived incrementally without retaining all events in memory");
        var completed=civic.State.Buildings.First(b=>b.Completed && b.Condition>0);var ownerSettlement=civic.State.Settlements.Single(s=>s.Id==completed.SettlementId);var purpose=completed.Purpose;var benefit=civic.Completed(ownerSettlement,purpose);
        civic.Damage(completed.Id,1,m.MatchAge);
        Assert(civic.Completed(ownerSettlement,purpose)==benefit-1 && completed.Condition==0,"Destroyed buildings immediately stop providing their functional benefit");
        Assert(civic.State.RecentEvents.Last().Kind==CivicEventKind.BuildingDestroyed,"Committed building destruction records a factual event");
        var occupied=civic.State.Settlements.First(s=>s.Active && civic.State.Buildings.Any(b=>b.SettlementId==s.Id && !b.Completed && !b.Cancelled));var oldOwner=occupied.OwnerId;var newOwner=civic.State.Nations.First(n=>n.NationId!=oldOwner).NationId;
        var region=politics.Land.At(occupied.Position);var pending=civic.State.Buildings.Where(b=>b.SettlementId==occupied.Id && !b.Completed).ToArray();politics.Commit(region,oldOwner,newOwner,m.MatchAge,OwnershipReason.Conquest);Steps(14);
        Assert(occupied.OwnerId==newOwner && pending.All(b=>b.Cancelled),"Actual territorial capture transfers settlements and terminates old construction jobs");
        Assert(civic.State.Nations.All(n=>n.CapitalSettlementId==null || civic.State.Settlements.Any(s=>s.Id==n.CapitalSettlementId && s.OwnerId==n.NationId)),"Lost capitals are relocated or leave a lawful vacancy");
        var ant=m.Colonies[BugSpecies.Ants];var antLeader=ant.Leader;var attacker=m.Colonies[BugSpecies.Flies].Leader;var royal=civic.State.Royals.First(r=>r.UnitId==antLeader.UnitId);var others=m.Bugs.Where(b=>b.Species==BugSpecies.Ants && !b.IsLeader && BugManager.Alive(b)).ToArray();var eligibility=others.ToDictionary(b=>b.UnitId,b=>b.CanReproduce);
        antLeader.Protection=0;Assert(m.ApplyBattleDamage(attacker,antLeader,99999),"An actual battle event can kill the political ruler");Steps(20);
        Assert(royal.DeathAt!=null && royal.DeathCause=="Battle" && civic.State.RecentEvents.Any(e=>e.Kind==CivicEventKind.RoyalDeath && e.RulerId==royal.Id),"Royal death records the actual identity, time and supported battle cause");
        Assert(civic.State.Nations.Single(n=>n.NationId==royal.NationId).OfficeVacant && others.All(b=>b.CanReproduce==eligibility[b.UnitId]),"Queen loss leaves an office vacancy without creating a reproductive soldier");
        Assert(!ant.Eliminated && politics.Find(royal.NationId)!.Status==NationExistence.Active,"Leader death alone does not extinguish surviving national population");
        var spider=m.Colonies[BugSpecies.Spiders];var oldSpider=spider.Leader;var spiderPerson=civic.State.Royals.Single(r=>r.UnitId==oldSpider.UnitId);var eligible=m.Bugs.Where(b=>b.Species==BugSpecies.Spiders && !b.IsLeader && b.CanReproduce && BugManager.Alive(b)).ToArray();
        Assert(eligible.Length>0,"Existing species reproduction supplies actual eligible recovery adults");foreach(var adult in eligible){adult.Position=spider.Territory;adult.Health=adult.MaxHealth;adult.Protection=100;}
        oldSpider.Protection=0;m.ApplyBattleDamage(attacker,oldSpider,99999);Steps(600);
        Assert(BugManager.Alive(spider.Leader) && spider.Leader.UnitId!=oldSpider.UnitId && spider.Leader.CanReproduce,"Core biological recovery chooses a real reproductive successor");
        var successor=civic.State.Royals.First(r=>r.UnitId==spider.Leader.UnitId);
        Assert(successor.PredecessorId==spiderPerson.Id && spiderPerson.SuccessorId==successor.Id && successor.DynastyId!=spiderPerson.DynastyId,"Legitimate recovery preserves succession links and explicitly starts a new house without invented descent");
        Assert(civic.State.Dynasties.First(d=>d.Id==spiderPerson.DynastyId).EndedAt!=null,"Previous dynastic continuity ends as a recorded political fact");
        var snapshot=m.CaptureWorld();var rawAfter=snapshot.LivingCivilization!.Value;var restoredCore=BugManager.RestoreWorld(snapshot);restoredCore.Nations=LivingNationWorld.Restore(politics.Land,m.WorldId,snapshot.LivingPolitics!.Value);restoredCore.Nations.BindWriter();restoredCore.PendingPolitics=null;restoredCore.Civilization=LivingCivilization.Restore(rawAfter,restoredCore.Nations,directory);restoredCore.PendingCivilization=null;
        Assert(restoredCore.Civilization.CaptureJson().GetRawText()==rawAfter.GetRawText(),"Developed resources, jobs, buildings, succession and history head survive restoration");
        var cursor=civic.State.JournalHead;var high=civic.State.Sequence;var eventCount=0;
        while(cursor!=null){var segment=JsonSerializer.Deserialize<LivingCivilization.CivicJournal>(File.ReadAllText(Path.Combine(directory,"civilization-history",cursor+".json")),ArcadeWorldStore.JsonOptions)!;Assert(segment.WorldId==m.WorldId && segment.Events[^1].Sequence==high,"Durable history segments preserve chronological continuity");high=segment.Events[0].Sequence-1;eventCount+=segment.Events.Length;cursor=segment.Previous;}
        Assert(high==0 && eventCount==civic.State.Sequence,"History chain retains all committed facts beyond the in-memory window");
        bool Reject(JsonElement value){try{LivingCivilization.Restore(value,politics,directory);return false;}catch(Exception ex) when(ex is InvalidDataException or JsonException or ArgumentException){return true;}}
        var invalid=JsonNode.Parse(rawAfter.GetRawText())!;invalid["SchemaVersion"]=99;Assert(Reject(JsonSerializer.SerializeToElement(invalid)),"Unsupported civilization save schema fails explicitly");
        invalid=JsonNode.Parse(rawAfter.GetRawText())!;invalid["WorldId"]=Guid.NewGuid();Assert(Reject(JsonSerializer.SerializeToElement(invalid)),"Civilization data cannot attach to an unrelated world");
        invalid=JsonNode.Parse(rawAfter.GetRawText())!;invalid["JournalHead"]=new string('0',64);Assert(Reject(JsonSerializer.SerializeToElement(invalid)),"Missing durable history head is rejected rather than fabricated");
        invalid=JsonNode.Parse(rawAfter.GetRawText())!;invalid["Settlements"]![0]!["Materials"]=-1;Assert(Reject(JsonSerializer.SerializeToElement(invalid)),"Invalid economic state fails validation");
        m.WorldPaused=true;session.Controller.SetPaused(true);session.Controller.SaveNow();
        var home=new HomeWindow(store);home.Show();var main=new MainWindow(store){Width=1200,Height=820};main.Show();var board=BattlefieldWindow.ShowFor(main,store);board.Activate();board.FitWorld();Pump(TimeSpan.FromMilliseconds(160));
        Assert(ReferenceEquals(main.livingWorld!.Civilization,civic) && ReferenceEquals(board.Living!.Civilization,civic),"Dashboard and battlefield share one authoritative civilization model");
        var settlement=civic.State.Settlements.First(s=>s.Active && civic.State.Buildings.Any(b=>b.SettlementId==s.Id && b.Completed && b.Condition>0));var map=board.Battlefield.TerrainView!.SettlementView;
        Assert(map.Hit(board.Battlefield.WorldToScreen(settlement.Position))==settlement.Id,"Native city marker hit testing resolves the correct settlement");
        Until023(()=>board.Battlefield.TerrainView!.DetailReady,"Terrain settles before measuring settlement interaction");
        var edits=0;var editor=Descendants(main).OfType<TextBox>().First(t=>t.Text==note.Text);var input=new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Input){Interval=TimeSpan.FromMilliseconds(30)};input.Tick+=(_,_)=>editor.Text="City edits "+edits++;input.Start();
        board.SelectSettlement(settlement.Id);Pump(TimeSpan.FromMilliseconds(500));input.Stop();Assert(edits>=5,"Dashboard editing remains responsive during settlement focus");
        Until023(()=>!board.FocusingNation && board.Battlefield.TerrainView!.DetailReady,"Focused settlement camera and requested terrain detail settle");Pump(TimeSpan.FromMilliseconds(100));
        Assert(board.SelectedSettlementId==settlement.Id && board.FocusDestination==settlement.Position,"Settlement focus centers valid stored coordinates");
        Assert(Descendants(board).OfType<TextBlock>().Any(t=>t.Text.Contains("Workforce:") && t.Text.Contains("Construction:")),"Settlement selection shows actual resources, workforce and construction");
        var built=map.GeometryBuilds;board.Battlefield.RefreshFrame();Pump(TimeSpan.FromMilliseconds(100));Console.WriteLine($"Settlement renderer: builds={built}->{map.GeometryBuilds}, visible={map.VisibleSettlements}, total={civic.State.Settlements.Count(s=>s.Active)}, edits={edits}");Assert(map.GeometryBuilds==built && map.VisibleSettlements<civic.State.Settlements.Count(s=>s.Active),"Unchanged building geometry is cached and offscreen settlements are culled");
        Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"civilization-settlement024");
        var renderSamples=new Dictionary<string,double>();foreach(var scale in new[]{1d,1.5,2d}){var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)(board.ActualWidth*scale),(int)(board.ActualHeight*scale),96*scale,96*scale,System.Windows.Media.PixelFormats.Pbgra32);var samples=new List<double>();for(var i=0;i<8;i++){var started=Stopwatch.GetTimestamp();image.Clear();image.Render(board);if(i>1)samples.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);}renderSamples[$"{scale*100:F0}% ({image.PixelWidth}x{image.PixelHeight})"]=Percentile(samples,.95);}
        board.FitWorld();Pump(TimeSpan.FromMilliseconds(150));Save018(board,(int)board.ActualWidth,(int)board.ActualHeight,"civilization-world024");board.Close();main.Close();home.Close();
        var badStore=new StateStore(Path.Combine(output,"civilization-recovery","state.json"));var badDir=Path.Combine(LivingWorlds.Root(badStore),session.Info.WorldId.ToString("N"));LivingFiles.Save(Path.Combine(badDir,"world.json"),session.Info);snapshot.LivingCivilization=JsonSerializer.SerializeToElement("future-version");new ArcadeWorldStore(Path.Combine(badDir,"arcade-world.json")).Save(snapshot);
        var bad=LivingWorlds.Open(badStore,session.Info.WorldId);Until023(()=>bad.PoliticsReady.IsCompleted,"Invalid optional civilization data is examined asynchronously");
        Assert(bad.Civilization==null && bad.CivilizationWarning!=null && !bad.Controller.LoadBlocked,"Incompatible optional civilization data displays a warning and preserves the base world");bad.Controller.SaveNow();
        Assert(new ArcadeWorldStore(bad.Controller.SavePath).Load().World!.LivingCivilization!.Value.GetRawText()==snapshot.LivingCivilization.Value.GetRawText(),"Saving preserves unsupported civilization data without deleting it");
        var colony=Infinite021(97);Assert(colony.Civilization==null && colony.CaptureWorld().LivingCivilization==null,"Colony Wars remains independent of the Living Worlds extension");
        var metrics=new{SimulationSeconds=m.MatchAge,SimulationWallMs=timing.Elapsed.TotalMilliseconds,CivicTickP95Ms=Percentile(civicTicks,.95),CivicTickMaxMs=civicTicks.Max(),SoftwareRenderP95Ms=renderSamples,FocusEdits=edits,Population=m.Bugs.Count(BugManager.Alive),Settlements=civic.State.Settlements.Length,Buildings=civic.State.Buildings.Length,Royals=civic.State.Royals.Length,HistorySequence=civic.State.Sequence,HistoryResidentEvents=civic.State.RecentEvents.Length,HistorySegments=Directory.GetFiles(Path.Combine(directory,"civilization-history"),"*.json").Length,SnapshotBytes=JsonSerializer.SerializeToUtf8Bytes(civic.State).Length};File.WriteAllText(Path.Combine(output,"civilizations024.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(metrics));
    }
}
