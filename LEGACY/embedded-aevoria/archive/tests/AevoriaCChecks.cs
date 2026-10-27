using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using SessionDashboard;
using Aevoria.Desktop;
internal static partial class Program
{
    private static void CStep(LivingWorldSession world,double dt=.08)
    {var m=world.Controller.Manager;m.PrepareLivingStep();m.Advance(dt);m.ResolveLivingStep(world.Terrain,dt);m.Nations!.Tick(m);m.Civilization!.Tick(m);}
    private static double CCoverage(LivingNationWorld politics)=>politics.Nations.Sum(n=>(double)politics.Area(n.Id))/politics.Land.Land.Sum(mask=>(double)System.Numerics.BitOperations.PopCount(mask));
    private static int[] CMainland(LivingNationWorld p)
    {
        var remaining=Enumerable.Range(0,p.Land.Count).Where(p.Expansion!.ConnectedCell).ToHashSet();var queue=new Queue<int>();var biggest=Array.Empty<int>();var area=0;
        while(remaining.Count>0){var first=remaining.First();remaining.Remove(first);queue.Enqueue(first);var cells=new List<int>();var total=0;while(queue.TryDequeue(out var c)){cells.Add(c);total+=p.Land.Area(c);foreach(var n in p.Land.Neighbors(c))if(remaining.Remove(n))queue.Enqueue(n);}if(total>area){area=total;biggest=cells.ToArray();}}
        return biggest;
    }
    private static double CMainlandCoverage(LivingNationWorld p,int[] mainland)=>mainland.Where(c=>p.Owner(c)!=null).Sum(c=>(double)p.Land.Area(c))/mainland.Sum(c=>(double)p.Land.Area(c));
    private static void CheckExpansionBaseline()
    {
        var rows=new List<object>();
        foreach(var size in GrandGeneration.Sizes.Where(s=>s.Width>=1024))
        {
            var library=new AevoriaLibrary(Path.Combine(output,"baseline-"+size.Width));var world=GrandWait(library.Create("Baseline","kingdoms",size.Width,size.Height,new(){PopulationBudget=120}));
            world.Controller.SetPaused(true);var m=world.Controller.Manager;m.WorldPaused=false;var curve=new List<object>{new{Seconds=0d,Coverage=CCoverage(m.Nations!)}};
            for(var i=1;i<=3750;i++){CStep(world);if(i%1250==0)curve.Add(new{Seconds=m.MatchAge,Coverage=CCoverage(m.Nations!)});}
            rows.Add(new{size.Width,size.Height,Curve=curve,Population=m.Bugs.Count(BugManager.Alive),Settlements=m.Civilization!.State.Settlements.Length});
            m.WorldPaused=true;world.Controller.SaveNow();Console.WriteLine(JsonSerializer.Serialize(rows[^1]));
        }
        File.WriteAllText(Path.Combine(output,"growth-baseline.json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static void CheckAevoriaC()
    {
        var metrics=new List<object>();
        foreach(var size in GrandGeneration.Sizes)
        {
            var world=GrandWait(new AevoriaLibrary(Path.Combine(output,"curve-"+size.Width)).Create("Kingdoms","kingdoms",size.Width,size.Height,new(){PopulationBudget=120}));
            var m=world.Controller.Manager;world.Controller.SetPaused(true);m.WorldPaused=false;var p=m.Nations!;var expansion=p.Expansion!;var start=CCoverage(p);var connectedClaims=true;var expansionFacts=0;
            p.OwnershipChanged+=fact=>{if(fact.Reason==OwnershipReason.PeacefulExpansion){expansionFacts++;connectedClaims&=fact.PreviousOwner==null && p.Land.Neighbors(fact.Region).Any(n=>p.Owner(n)==fact.NewOwner && expansion.ConnectedCell(n));}};
            Assert(p.Nations.Count==6 && p.Nations.All(n=>p.Components(n.Id).Count==1),$"{size.Name}: six connected founding homelands");
            Assert(start<.03 && p.Expansion!=null,$"{size.Name}: initial claims stay small, expansion opts in explicitly");
            var mainland=CMainland(p);var curve=new List<object>{new{Seconds=0d,Coverage=start,AccessibleMainlandCoverage=CMainlandCoverage(p,mainland)}};var ticks=new List<double>();var activeTicks=new List<double>();var until=size.Width>=6144?600:1200;
            var watch=Stopwatch.StartNew();var evaluations=expansion.Evaluations;
            for(var step=1;step<=until/.08;step++)
            {
                var before=expansion.State.Turn;var began=Stopwatch.GetTimestamp();CStep(world);var took=Stopwatch.GetElapsedTime(began).TotalMilliseconds;ticks.Add(took);
                if(before!=expansion.State.Turn)activeTicks.Add(expansion.LastMilliseconds);
                if(step%1250==0){var coverage=CCoverage(p);curve.Add(new{Seconds=m.MatchAge,Coverage=coverage,AccessibleMainlandCoverage=CMainlandCoverage(p,mainland)});Console.WriteLine($"{size.Name} {m.MatchAge:F0}s: {coverage:P2}; expansion p95={Percentile(activeTicks,.95):F2}ms");}
            }
            Assert(expansion.Claims>0 && CCoverage(p)>start*3,$"{size.Name}: automatic headless expansion increases real ownership");
            Assert(connectedClaims && expansionFacts==expansion.Claims,$"{size.Name}: each peaceful claim uses a connected frontier and one ownership event");
            Assert(p.Capture().Owners.Select((owner,cell)=>owner==0 || p.Land.Land[cell]!=0).All(x=>x),$"{size.Name}: ownership remains land-clipped");
            Assert(expansion.Evaluations-evaluations<=expansion.State.Turn*6*expansion.State.Tuning.EvaluationsPerNation,$"{size.Name}: bounded candidate evaluations");
            Assert(m.Civilization!.State.Settlements.All(s=>s.Food>=0 && s.Materials>=0 && s.Production>=0),$"{size.Name}: costs never overdraw existing resources");
            Assert(p.Nations.Where(n=>p.Area(n.Id)>0).All(n=>p.Components(n.Id).All(c=>p.OwnerAt(c.Anchor)==n.Id)),$"{size.Name}: revisioned labels remain inside owned land");
            m.WorldPaused=true;var revision=p.Revision;var turn=expansion.State.Turn;expansion.Tick(m,m.Civilization);p.Tick(m);Assert(p.Revision==revision && expansion.State.Turn==turn,$"{size.Name}: paused worlds cannot expand");
            var saveWatch=Stopwatch.StartNew();world.Controller.SaveNow();saveWatch.Stop();var snapshot=new ArcadeWorldStore(world.Controller.SavePath).Load().World!;var restoreWatch=Stopwatch.StartNew();var restored=LivingNationWorld.Restore(p.Land,p.WorldId,snapshot.LivingPolitics!.Value);restoreWatch.Stop();
            Assert(restored.Capture().Owners.SequenceEqual(p.Capture().Owners) && restored.Expansion!.State.Turn==turn,$"{size.Name}: save/reload retains ownership and scheduling");
            Assert(new FileInfo(world.Controller.SavePath).Length<8*1024*1024,$"{size.Name}: snapshot respects existing size protection");
            metrics.Add(new{size.Name,size.Width,size.Height,Curve=curve,WallMs=watch.Elapsed.TotalMilliseconds,StepP95Ms=Percentile(ticks,.95),ExpansionP95Ms=Percentile(activeTicks,.95),ExpansionMaxMs=expansion.MaximumMilliseconds,expansion.Evaluations,expansion.Claims,RegionBytes=expansion.RegionBytes+p.Land.Bytes,ManagedBytes=GC.GetTotalMemory(false),SaveMs=saveWatch.Elapsed.TotalMilliseconds,RestoreMs=restoreWatch.Elapsed.TotalMilliseconds,BorderUpdateMs=p.Frontiers.UpdateMilliseconds,BorderUpdates=p.Frontiers.Updates,BorderPairs=p.Frontiers.Relationships.Count,NationAreas=p.Nations.Select(n=>new{n.Name,Area=p.Area(n.Id),n.Status}).ToArray(),Settlements=m.Civilization.State.Settlements.Length,Population=m.Bugs.Count(BugManager.Alive),SnapshotBytes=new FileInfo(world.Controller.SavePath).Length});
            if(size.Width==1024)CheckCIntegration(world);
        }
        CheckCGeography();
        File.WriteAllText(Path.Combine(output,"kingdom-metrics.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
    }
    private static void CheckCIntegration(LivingWorldSession world)
    {
        var m=world.Controller.Manager;var p=m.Nations!;var civic=m.Civilization!;var expansion=p.Expansion!;
        var snapshot=m.CaptureWorld();var second=BugManager.RestoreWorld(snapshot);second.Nations=LivingNationWorld.Restore(p.Land,p.WorldId,snapshot.LivingPolitics!.Value);second.Civilization=LivingCivilization.Restore(snapshot.LivingCivilization!.Value,second.Nations,Path.GetDirectoryName(world.Controller.SavePath)!);
        var random=snapshot.RandomState;var unitIds=m.Bugs.Select(b=>b.UnitId).ToArray();m.WorldPaused=second.WorldPaused=false;
        for(var i=0;i<8;i++){m.MatchAge+=2;second.MatchAge+=2;expansion.Tick(m,civic);second.Nations.Expansion!.Tick(second,second.Civilization);}
        Assert(p.Capture().Owners.SequenceEqual(second.Nations.Capture().Owners),"Save/restore candidate rebuilding reproduces fixed-condition expansion");
        Assert(civic.State.Sequence==second.Civilization.State.Sequence && expansion.State.KnownBorders.SequenceEqual(second.Nations.Expansion!.State.KnownBorders),"Territorial milestones and first-border facts do not duplicate on reload");
        Assert(unitIds.SequenceEqual(m.Bugs.Select(b=>b.UnitId)) && m.CaptureWorld().RandomState==random,"Regional influence creates no units and consumes no combat RNG");
        var nation=p.Nations.First(n=>p.Area(n.Id)>0);var town=civic.State.Settlements.First(s=>s.Active && s.OwnerId==nation.Id);var cell=p.Land.At(town.Position);var influence=expansion.Influence(nation.Id,cell,civic);var active=town.Active;town.Active=false;
        Assert(expansion.Influence(nation.Id,cell,civic)<influence,"Settlement loss reduces actual administrative influence");town.Active=active;
        var original=town.Stage;town.Stage=SettlementStage.Outpost;var far=p.Regions(nation.Id).MaxBy(c=>(p.Land.Center(c)-town.Position).LengthSquared);var before=expansion.Influence(nation.Id,far,civic);town.Stage=SettlementStage.City;
        Assert(expansion.Influence(nation.Id,far,civic)>=before,"A City supports a wider administrative region than an Outpost");town.Stage=original;
        var reach=expansion.AdministrativeReach(town,civic);var infrastructure=new CivicBuilding{Id=Guid.NewGuid(),SettlementId=town.Id,OwnerId=town.OwnerId,StyleId=town.StyleId,Purpose=CivicPurpose.Infrastructure,Archetype="Test infrastructure",Position=town.Position,Completed=true,Progress=1};civic.State.Buildings=civic.State.Buildings.Append(infrastructure).ToArray();
        Assert(expansion.AdministrativeReach(town,civic)>reach,"Completed infrastructure measurably extends administrative reach");infrastructure.Condition=0;Assert(expansion.AdministrativeReach(town,civic)==reach,"Destroyed infrastructure removes its administrative support");civic.State.Buildings=civic.State.Buildings.Where(b=>b.Id!=infrastructure.Id).ToArray();
        var capital=nation.Capital;var relocation=p.Regions(nation.Id).Select(p.Land.Center).First(point=>point!=town.Position && p.Land.IsCapital(point));p.RelocateCapital(nation.Id,relocation);
        Assert(expansion.AdministrativeReach(town,civic)<reach,"Capital relocation updates the former capital's administrative influence");p.RelocateCapital(nation.Id,capital);
        var emptyMaterials=town.Materials;town.Materials=0;var polity=civic.State.Nations.First(n=>n.NationId==nation.Id);Assert(civic.Found(polity,p.Land.Center(far),m.MatchAge)==null,"Additional civic settlements cannot bypass actual founding prerequisites");town.Materials=emptyMaterials;
        Assert(expansion.State.Milestones.Length>0 && civic.State.Sequence>0,"Significant territorial growth records bounded factual milestones");
        Assert(p.Frontiers.Relationships.All(r=>r.A!=r.B && r.Length>0 && r.Edges.All(e=>p.Land.Neighbors(e.CellA).Contains(e.CellB) && p.Owner(e.CellA)!=p.Owner(e.CellB))),"Shared frontiers derive from adjacent owned land with measured edge lengths");
        var allBorders=p.Frontiers.Relationships.ToArray();Assert(allBorders.Length>0,"Growing kingdoms establish actual shared frontiers");
        Assert(p.Frontiers.Settlements(civic).All(s=>s.Distance<=288 && civic.State.Settlements.Any(t=>t.Id==s.SettlementId && t.Active)),"Border-settlement designations use real settlements near shared edges");
        var neutral=Enumerable.Range(0,p.Land.Count).FirstOrDefault(c=>p.Owner(c)==null && expansion.ConnectedCell(c) && p.Land.Neighbors(c).Any(n=>p.Owner(n)==nation.Id && expansion.ConnectedCell(n)),-1);
        if(neutral<0){neutral=p.Regions(nation.Id).First(c=>expansion.ConnectedCell(c) && p.Land.Neighbors(c).Any(n=>p.Owner(n)==nation.Id && expansion.ConnectedCell(n)) && civic.State.Settlements.All(s=>!s.Active || p.Land.At(s.Position)!=c));p.ReleaseRegion(neutral,nation.Id,m.MatchAge);}
        Assert(!p.ClaimRegion(-1,nation.Id,m.MatchAge),"Territory command rejects invalid geography");
        var ocean=Enumerable.Range(0,p.Land.Count).First(c=>p.Land.Land[c]==0);Assert(!p.ClaimRegion(ocean,nation.Id,m.MatchAge),"Territory command rejects open ocean");
        if(neutral>=0){var owned=p.Area(nation.Id);Assert(p.ClaimRegion(neutral,nation.Id,m.MatchAge) && p.Area(nation.Id)>owned && p.RecentChanges[^1].Reason==OwnershipReason.Intervention,"Explicit claims validate connectivity and carry a distinct intervention reason");Assert(!p.ClaimRegion(neutral,nation.Id,m.MatchAge),"Repeated claims cannot double-process ownership");Assert(p.ReleaseRegion(neutral,nation.Id,m.MatchAge) && p.Owner(neutral)==null,"Release operations preserve an authoritative ownership fact");}
        var pocket=p.Regions(nation.Id).Where(c=>p.Land.Neighbors(c).Count(n=>p.Owner(n)==nation.Id)==4 && expansion.ConnectedCell(c) && civic.State.Settlements.All(s=>!s.Active || p.Land.At(s.Position)!=c)).OrderBy(c=>(p.Land.Center(c)-town.Position).LengthSquared).FirstOrDefault(-1);
        if(pocket>=0){p.ReleaseRegion(pocket,nation.Id,m.MatchAge);var pool=town.Materials;var food=town.Food;var population=town.Population;town.Materials=Math.Max(1,pool);town.Food=Math.Max(4,food);town.Population=Math.Max(120,population);m.MatchAge+=2;expansion.Tick(m,civic);Assert(p.Owner(pocket)==nation.Id,"Administratively supported and funded enclosed pocket returns through a paid expansion operation");town.Materials=pool;town.Food=food;town.Population=population;}
        var different=p.Nations.First(n=>n.Id!=nation.Id);var target=p.Regions(nation.Id).First();var borderRevision=p.Frontiers.Revision;var renderingRevision=p.CellRevisions[target];
        Assert(!p.TransferRegion(target,different.Id,nation.Id,m.MatchAge),"Transfers reject stale expected ownership");p.TransferRegion(target,nation.Id,different.Id,m.MatchAge);
        Assert(p.Owner(target)==different.Id && p.CellRevisions[target]>renderingRevision && p.Frontiers.Revision>borderRevision,"Ownership changes invalidate local rendering and frontier caches");p.TransferRegion(target,different.Id,nation.Id,m.MatchAge);
        var same=new LivingNation(Guid.NewGuid(),"Separate polity",nation.Species,0xff9966aa,p.Land.Center(target),null,m.MatchAge);p.AddNation(same);Assert(!p.Frontiers.Neighbors(same.Id).Any(),"Capital proximity alone does not establish political adjacency");p.TransferRegion(target,nation.Id,same.Id,m.MatchAge);
        Assert(p.Owner(target)==same.Id && p.Frontiers.Neighbors(same.Id).Contains(nation.Id),"Same-species nations retain separate political ownership and adjacency");p.TransferRegion(target,same.Id,nation.Id,m.MatchAge);
        var regions=p.Regions(nation.Id);var route=p.Frontiers.FriendlyRoute(nation.Id,regions[0],regions[0]);Assert(route.SequenceEqual(new[]{regions[0]}),"Bounded friendly-route query reuses the political graph");
        var cached=p.Frontiers.Updates;_ =p.Frontiers.Relationships;_ =p.Frontiers.Relationships;Assert(cached==p.Frontiers.Updates,"Read-only frontier queries do not rescan or modify ownership");
        m.WorldPaused=true;var board=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Show();board.FitWorld();Pump(TimeSpan.FromMilliseconds(80));
        var units=board.Battlefield.TerrainView!.ArmyView.Groups.SelectMany(g=>g.Units).Order().ToArray();var beforeDraw=p.Revision;board.Battlefield.RefreshFrame();CaptureB(board,"kingdoms-established",1);
        Assert(p.Revision==beforeDraw && units.SequenceEqual(board.Battlefield.TerrainView.ArmyView.Groups.SelectMany(g=>g.Units).Order()),"Political presentation does not change ownership or military group membership");
        board.SelectNation(new(nation.Id,p.Components(nation.Id)[0].Key,false));board.FocusNation();Assert(board.FocusingNation && board.FocusDestination is {} focus && p.OwnerAt(focus)==nation.Id,"Camera focus still targets actual owned land");board.Close();
        Assert(AevoriaBuild.Label==AevoriaBuild.Version+" Alpha" && App.AppVersion=="0.1.23" && File.Exists(AevoriaLauncher.Resolve(Path.GetFullPath("bin/Release/net10.0-windows"))),"Independent development identity and Nexa launch resolution remain intact");
    }
    private static void CheckCGeography()
    {
        var world=GrandWait(new AevoriaLibrary(Path.Combine(output,"difficult-geography")).Create("Islands","narrow-channels",1024,576,new(){Profile=GeographyProfile.Archipelago,PopulationBudget=120}));var m=world.Controller.Manager;world.Controller.SetPaused(true);m.WorldPaused=false;var p=m.Nations!;var e=p.Expansion!;
        var ui=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};var gaps=new List<double>();var pulse=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(20)};var last=Stopwatch.GetTimestamp();pulse.Tick+=(_,_)=>{gaps.Add(Stopwatch.GetElapsedTime(last).TotalMilliseconds);last=Stopwatch.GetTimestamp();};pulse.Start();ui.Show();ui.FitWorld();Pump(TimeSpan.FromSeconds(3.2));pulse.Stop();
        Assert(gaps.Count>=10 && e.State.Turn>0,"Actual standalone battlefield dispatches UI work while authoritative expansion runs");CaptureB(ui,"islands-expanding",1);world.Controller.SetPaused(true);ui.Close();
        File.WriteAllText(Path.Combine(output,"responsiveness.json"),JsonSerializer.Serialize(new{PulseCount=gaps.Count,DispatcherGapP95Ms=Percentile(gaps,.95),DispatcherGapMaxMs=gaps.Max(),e.LastMilliseconds,ExpansionMaxMs=e.MaximumMilliseconds}));m.WorldPaused=false;
        var access=p.Nations.ToDictionary(n=>n.Id,n=>{var seen=p.Regions(n.Id).Where(e.ConnectedCell).ToHashSet();var queue=new Queue<int>(seen);while(queue.TryDequeue(out var c))foreach(var next in p.Land.Neighbors(c))if(e.ConnectedCell(next) && seen.Add(next))queue.Enqueue(next);return seen;});
        for(var i=0;i<3750;i++)CStep(world);
        Assert(p.RecentChanges.Where(f=>f.Reason==OwnershipReason.PeacefulExpansion).All(f=>access[f.NewOwner!.Value].Contains(f.Region)),"Difficult archipelago growth stays on originally accessible land components");
        var costs=Enumerable.Range(0,p.Land.Count).Where(c=>p.Land.Land[c]!=0).Select(e.TerrainCost).ToArray();Assert(costs.Max()>costs.Min()*2,"Cached geographic costs distinguish mountain barriers and plains");
        var repeat=new LivingTerrain(world.Info);Assert(Enumerable.Range(0,100).All(i=>world.Terrain.Sample(i*9,i*5)==repeat.Sample(i*9,i*5)),"Generator-3 terrain remains deterministic after territorial development");
        var raw=p.CaptureJson();var legacy=p.Capture() with{SchemaVersion=1,Expansion=null};var old=LivingNationWorld.Restore(p.Land,p.WorldId,JsonSerializer.SerializeToElement(legacy,ArcadeWorldStore.JsonOptions));Assert(old.Expansion==null,"Pre-C worlds without the optional extension retain their existing behavior");
        Assert(p.Capture().SchemaVersion==2 && old.Capture().SchemaVersion==1,"Explicit political version 2 protects new expansion worlds from older readers; legacy worlds remain version 1");
        bool invalid=false;try{LivingNationWorld.Restore(p.Land,p.WorldId,JsonSerializer.SerializeToElement(p.Capture() with{Expansion=new(){SchemaVersion=999}},ArcadeWorldStore.JsonOptions));}catch(InvalidDataException){invalid=true;}Assert(invalid,"Unsupported expansion data is rejected without silently replacing ownership");
        m.WorldPaused=true;world.Controller.SaveNow();
    }
    private static void CheckCUi(string fixtures)
    {
        var results=new List<object>();
        foreach(var size in GrandGeneration.Sizes)
        {
            // Probe a copy, retaining the measured fixed-time fixtures for review.
            var source=new AevoriaLibrary(Path.Combine(Path.GetFullPath(fixtures),"curve-"+size.Width));var sourceInfo=source.Worlds().Single();var library=new AevoriaLibrary(Path.Combine(output,"ui-"+size.Width));var imported=GrandWait(library.Import(Path.Combine(LivingWorlds.Root(source.Store),sourceInfo.WorldId.ToString("N"))));
            var world=library.Open(imported);Until023(()=>world.PoliticsReady.IsCompleted,"Live UI world topology finishes loading");world.PoliticsReady.GetAwaiter().GetResult();
            var m=world.Controller.Manager;var expansion=world.Nations!.Expansion!;var civic=m.Civilization!;var originalUnits=m.Bugs.Select(b=>b.UnitId).ToArray();var pools=civic.State.Settlements.Select(s=>(Town:s,s.Food,s.Materials)).ToArray();m.WorldPaused=false;
            foreach(var pool in pools){pool.Town.Food=0;pool.Town.Materials=0;}var oldClaims=expansion.Claims;m.MatchAge+=2;expansion.Tick(m,civic);
            Assert(expansion.Claims==oldClaims,$"{size.Name}: exhausted settlements cannot fund peaceful claims");foreach(var pool in pools){pool.Town.Food=pool.Food;pool.Town.Materials=pool.Materials;}
            var foodBefore=civic.State.Settlements.Sum(s=>s.Food);var materialsBefore=civic.State.Settlements.Sum(s=>s.Materials);m.MatchAge+=2;expansion.Tick(m,civic);
            if(expansion.Claims>oldClaims)Assert(civic.State.Settlements.Sum(s=>s.Food)<foodBefore && civic.State.Settlements.Sum(s=>s.Materials)<materialsBefore,$"{size.Name}: controlled expansion consumes both real food and materials");
            Assert(originalUnits.SequenceEqual(m.Bugs.Select(b=>b.UnitId)),$"{size.Name}: economic frontier work does not spawn population");m.WorldPaused=true;
            var before=m.MatchAge;var claims=expansion.Claims;var board=new BattlefieldWindow(world.Controller,world,strategic:true){WindowState=WindowState.Normal,Width=1200,Height=800};board.Show();board.FitWorld();
            var gaps=new List<double>();var last=Stopwatch.GetTimestamp();var pulse=new System.Windows.Threading.DispatcherTimer{Interval=TimeSpan.FromMilliseconds(20)};pulse.Tick+=(_,_)=>{gaps.Add(Stopwatch.GetElapsedTime(last).TotalMilliseconds);last=Stopwatch.GetTimestamp();};pulse.Start();world.Controller.SetPaused(false);Pump(TimeSpan.FromSeconds(3.2));pulse.Stop();world.Controller.SetPaused(true);
            Assert(m.MatchAge>before && gaps.Count>0,$"{size.Name}: live UI dispatcher and authoritative clock advance together");CaptureB(board,"political-"+size.Width,1);var view=board.Battlefield.TerrainView!.NationView;
            results.Add(new{size.Name,size.Width,SimulatedSeconds=m.MatchAge-before,UiPulses=gaps.Count,UiGapP95Ms=Percentile(gaps,.95),UiGapMaxMs=gaps.Max(),ClaimsDuringUi=expansion.Claims-claims,BorderBuildMs=view.RebuildMilliseconds,view.Rebuilds,AnalysisMs=world.Nations!.AnalysisMilliseconds,ManagedBytes=GC.GetTotalMemory(false),WorkingSetBytes=Process.GetCurrentProcess().WorkingSet64});board.Close();Console.WriteLine(JsonSerializer.Serialize(results[^1]));
        }
        File.WriteAllText(Path.Combine(output,"live-ui-metrics.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
    }
}
