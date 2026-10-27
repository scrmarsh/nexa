using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckHistoricalAtlas()
    {
        var watch=Stopwatch.StartNew();var atlas=HistoricalAtlas.Default;watch.Stop();var data=atlas.Data;
        Assert(data.Entities.Length==714 && data.Eras.Length==19,"Embedded catalog imports all 714 identities and 19 reference eras");
        Assert(data.Entities.Count(e=>e.Status==AtlasEntityStatus.FictionalScenario)==55 && data.Wars.Length==13 && data.Technologies.Length==6 && data.Branches.Length==8,"All fictional civilizations, conflicts, technology periods and successor examples are imported");
        Assert(atlas.Source.Blocks.Length==199 && atlas.Source.Blocks.Any(b=>b.Cells.Any(c=>c.Contains("PART III"))),"Complete reference text includes historical, fictional and implementation sections");
        var reference=Path.Combine("docs",atlas.Source.Document);
        Assert(File.Exists(reference) && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(reference))).Equals(data.SourceSha256,StringComparison.OrdinalIgnoreCase),"Catalog provenance matches the retained original Word document");
        foreach(var b in atlas.Source.Blocks.Where(b=>b.Kind=="row" && b.Cells.Length==4 && b.Cells[0].Length==4 && int.TryParse(b.Cells[0],out _)))
        {
            var e=data.Entities.Single(e=>e.Name==b.Cells[1] && e.Status==AtlasEntityStatus.FictionalScenario);
            Assert(e.EmergenceYear==long.Parse(b.Cells[0]) && e.Origin==b.Cells[2].Split('•')[0].Trim() && e.MilitaryCharacteristics.Single()==b.Cells[3],$"Future identity preserved: {e.Name}");
        }
        var listedNames=atlas.Source.Blocks.Where(b=>b.Index is 50 or 52 or 54 or 56 or 58 or 60).SelectMany(b=>b.Cells[0].Split('•').Select(s=>s.Trim())).ToArray();
        var countries=data.Entities.Where(e=>e.DiplomaticStatus=="UN member in reference inventory").ToArray();
        Assert(listedNames.Length==193 && countries.Length==193 && listedNames.All(n=>countries.Any(e=>e.Name==n)),"All 193 countries are retained as the document's contemporary coverage inventory");
        Assert(data.Entities.Count(e=>e.DiplomaticStatus=="UN non-member observer in reference inventory")==2,"Observer-status references are distinguished from the member inventory");
        Assert(atlas.Entities["historical:israel"].Id!=atlas.Entities["country:israel"].Id,"Ancient Israel and contemporary Israel retain different identities");
        var roman=new[]{"historical:roman-kingdom","historical:roman-republic","historical:roman-empire"}.Select(id=>atlas.Entities[id]).ToArray();
        Assert(roman.Select(e=>e.CultureId).Distinct().Count()==1 && roman.All(e=>e.Kind==AtlasEntityKind.StatePhase) && roman.Select(e=>e.GovernmentId).Distinct().Count()==3,"Roman political phases share cultural identity while governments remain distinct");
        Assert(roman[1].RulerTitles.SequenceEqual(["Consul"]) && atlas.Entities["country:united-states"].RulerTitles.SequenceEqual(["President"]),"Reference titles do not impose hereditary monarchs on republics");
        Assert(atlas.Entities["historical:ming"].CultureId==atlas.Entities["historical:qing"].CultureId && atlas.Entities["historical:ming"].DynastyId!=atlas.Entities["historical:qing"].DynastyId,"Chinese dynastic records remain separate within a broad cultural thread");
        Assert(atlas.Entities["historical:england"].CultureId!=atlas.Entities["historical:scotland"].CultureId && data.Relationships.Count(r=>r.ToId=="historical:great-britain" && r.Kind==AtlasRelationKind.UnionComponent)==2,"Political union relationships preserve distinct cultural identities");
        var renamed=roman[0] with{Name="Alternate display name"};
        Assert(renamed.Id==roman[0].Id && data.Relationships.Any(r=>r.FromId==renamed.Id),"Display-name changes preserve relationship identity keys");
        Assert(data.Entities.Where(e=>e.Status!=AtlasEntityStatus.FictionalScenario).All(e=>e.EmergenceYear==null && e.EndYear==null),"Approximate inventory periods never become invented founding or collapse dates");
        Assert(atlas.Entities["historical:wilusa-troy"].Occurrences.Any(o=>o.Classification==AtlasSourceClass.Debated) && atlas.Entities["historical:wilusa-troy"].Notes.Length>0,"Troy retains the document's archaeological and legendary distinction");
        var primeval=atlas.Entities.Values.Single(e=>e.Kind==AtlasEntityKind.NarrativeTradition);
        Assert(primeval.Status==AtlasEntityStatus.TraditionalReference && primeval.EmergenceYear==null && primeval.Occurrences.All(o=>o.Classification==AtlasSourceClass.MythicLegendary),"Primeval traditions remain undated narrative references");
        Assert(atlas.Entities["historical:israel"].Occurrences.Any(o=>o.Classification==AtlasSourceClass.BiblicalTradition) && atlas.Entities["historical:israel"].Occurrences.Any(o=>o.Classification==AtlasSourceClass.ReferenceUnverified),"Biblical source associations do not erase the separate historical inventory occurrence");
        Assert(data.Entities.Where(e=>e.Status==AtlasEntityStatus.FictionalScenario).All(e=>e.Occurrences.All(o=>o.Classification==AtlasSourceClass.FictionalSpeculative)) && data.Wars.All(w=>w.Fictional && !w.Automatic) && data.Relationships.All(r=>!r.Forced),"Fictional scenarios and successors cannot force historical or simulated outcomes");
        Assert(data.Representations.All(s=>!s.Playable && s.Usage!=AtlasSymbolUsage.VerifiedHistoricalSymbol),"Art-direction symbols are not new playable biological species or verified official emblems");
        Assert(HistoricalAtlas.Year(-3500)=="3500 BCE" && HistoricalAtlas.Year(500)=="500 CE" && HistoricalAtlas.Year(10000)=="10000 CE","Reference calendar formatting supports BCE, CE and years beyond 4000 without a forced endpoint");
        var zero=false;try {HistoricalAtlas.Year(0);}catch(ArgumentOutOfRangeException) {zero=true;}Assert(zero,"Historical year zero is rejected rather than confused with elapsed world time");
        Assert(atlas.Search("Celadon Matrix").Any(e=>e.Name=="Zhong-Xi Celadon Matrix") && atlas.Search("nefertiti").Count()==0,"Search resolves documented alternate names without inventing ruler records");
        Assert(atlas.Search("",status:AtlasEntityStatus.FictionalScenario).Count()==55 && atlas.Search("",eraId:"future-3800").Count()>0,"Catalog search filters by fiction status and reference era");
        bool Reject(HistoricalAtlasData invalid)
        {try {new HistoricalAtlas(invalid,atlas.Source);return false;}catch(InvalidDataException) {return true;}}
        Assert(Reject(data with{SchemaVersion=2}),"Unknown catalog schemas fail closed");
        Assert(Reject(data with{Entities=data.Entities.Add(data.Entities[0])}),"Duplicate immutable IDs are rejected");
        Assert(Reject(data with{SourceSha256=new string('0',64)}),"Mismatched reference provenance is rejected");
        Assert(Reject(data with{Entities=data.Entities.SetItem(0,data.Entities[0] with{CultureId="missing"})}),"Dangling culture references are rejected");
        Assert(Reject(data with{Relationships=data.Relationships.SetItem(0,data.Relationships[0] with{ToId="missing"})}),"Dangling successor references are rejected");
        Assert(Reject(data with{Wars=data.Wars.SetItem(0,data.Wars[0] with{Automatic=true})}),"Fictional war templates cannot become compulsory events");
        Assert(Reject(data with{Entities=data.Entities.SetItem(0,data.Entities[0] with{EmergenceYear=0})}),"Ambiguous reference year zero is rejected");
        var futureIndex=Enumerable.Range(0,data.Entities.Length).First(i=>data.Entities[i].Status==AtlasEntityStatus.FictionalScenario);
        Assert(Reject(data with{Entities=data.Entities.SetItem(futureIndex,data.Entities[futureIndex] with{Status=AtlasEntityStatus.HistoricalReference})}),"Invented future entities cannot be relabeled established history");
        using(var bad=new MemoryStream(Encoding.UTF8.GetBytes("not json")))using(var source=new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(atlas.Source,HistoricalAtlas.Json)))
        {var rejected=false;try {HistoricalAtlas.Read(bad,source);}catch(InvalidDataException) {rejected=true;}Assert(rejected,"Malformed catalog JSON produces a recoverable data error");}
        using(var bad=new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data,HistoricalAtlas.Json).Replace("\"Cultures\":[","\"Cultures\":null,\"Unexpected\":["))))using(var source=new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(atlas.Source,HistoricalAtlas.Json)))
        {var rejected=false;try {HistoricalAtlas.Read(bad,source);}catch(InvalidDataException) {rejected=true;}Assert(rejected,"Null collections and unknown JSON fields cannot bypass validation");}
        var path=Path.Combine(output,"atlas-ui");Directory.CreateDirectory(path);var store=new StateStore(Path.Combine(path,"state.json"));var colony=ArcadeWorlds.For(store);colony.SetPaused(true);colony.SaveNow();var before=File.ReadAllBytes(colony.SavePath);
        var owner=new HomeWindow(store);owner.Show();var chooser=new LivingWorldDialog(owner,store);chooser.Show();Pump(TimeSpan.FromMilliseconds(50));
        Descendants(chooser).OfType<Button>().Single(b=>Equals(b.Content,"Civilization Atlas")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump(TimeSpan.FromMilliseconds(60));
        var browser=Application.Current.Windows.OfType<HistoricalAtlasWindow>().Single();
        Assert(ReferenceEquals(HistoricalAtlasWindow.ShowFor(chooser),browser),"Arcade chooser opens and reuses the read-only civilization browser");
        browser.Category.SelectedItem="Fictional civilizations";browser.Query.Text="Grand Concord";Pump(TimeSpan.FromMilliseconds(30));
        Assert(browser.Results.Items.Count==1 && browser.Detail.Text.Contains("The Veyr’Xhae Grand Concord") && browser.Detail.Text.Contains("Fictional / speculative"),"Browser preserves exotic names and visibly distinguishes future fiction");
        Descendants(browser).OfType<Button>().Single(b=>Equals(b.Content,"Show reference excerpt")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Descendants(browser).OfType<TextBlock>().Any(t=>t.Text.Contains("Atlas block") && t.Text.Contains("Tenuous peace")),"Browser exposes original reference excerpts for review");
        Save018(browser,(int)browser.ActualWidth,(int)browser.ActualHeight,"historical-atlas-future");
        browser.Category.SelectedItem="Historical inventory";browser.Query.Text="Roman Republic";Pump(TimeSpan.FromMilliseconds(30));
        Assert(browser.Results.Items.Count>=1 && browser.Detail.Text.Contains("Consul") && browser.Detail.Text.Contains("not independently verified"),"Historical browser shows appropriate titles and verification limits");
        browser.Category.SelectedItem="Fictional conflicts";browser.Query.Text="Last Meridian";Pump(TimeSpan.FromMilliseconds(30));
        Assert(browser.Results.Items.Count==1 && browser.Detail.Text.Contains("FICTIONAL SCENARIO") && browser.Detail.Text.Contains("not a prediction"),"War lore is separate from the world's actual history");
        browser.Query.Text="NoSuchAtlasEntry123";
        Assert(browser.Results.Items.Count==0 && browser.Detail.Text.Contains("No matching"),"Atlas search handles empty results");
        browser.Close();chooser.Close();owner.Close();
        Assert(before.SequenceEqual(File.ReadAllBytes(colony.SavePath)) && !File.Exists(Path.Combine(LivingWorlds.Root(store),"selection.json")),"Atlas browsing does not modify saves, select a scenario or migrate a world");
        Console.WriteLine($"Atlas load and validation: {watch.Elapsed.TotalMilliseconds:F2}ms; catalog records={data.Entities.Length}");
        File.WriteAllText(Path.Combine(output,"historical-atlas.json"),JsonSerializer.Serialize(new{Identities=data.Entities.Length,FictionalPolities=55,Eras=data.Eras.Length,Conflicts=data.Wars.Length,LoadMs=watch.Elapsed.TotalMilliseconds,data.SourceSha256},new JsonSerializerOptions{WriteIndented=true}));
    }
}
