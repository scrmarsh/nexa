using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckCultures024()
    {
        var memory=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();var cultures=LivingCultures.Default;timer.Stop();var loadMs=timer.Elapsed.TotalMilliseconds;var allocated=GC.GetAllocatedBytesForCurrentThread()-memory;
        var atlas=HistoricalAtlas.Default;var data=cultures.Data;
        var warmBytes=JsonSerializer.SerializeToUtf8Bytes(data,HistoricalAtlas.Json);var warmMemory=GC.GetAllocatedBytesForCurrentThread();var warmClock=Stopwatch.StartNew();
        using(var stream=new MemoryStream(warmBytes)){var read=LivingCultures.Read(stream,atlas);Assert(JsonSerializer.Serialize(read.Data,HistoricalAtlas.Json)==JsonSerializer.Serialize(data,HistoricalAtlas.Json),"Valid configurable culture profiles round-trip through the bounded reader");}
        warmClock.Stop();var warmAllocated=GC.GetAllocatedBytesForCurrentThread()-warmMemory;
        Assert(data.Profiles.Length==6 && atlas.Data.Entities.Length==714,"Six design profiles reference the existing 714-identity catalog");
        var expected=new Dictionary<string,string>{{"Ants","culture:roman"},{"Spiders","culture:greek"},{"Ladybugs","culture:french"},{"Roaches","culture:mongol"},{"Centipedes","culture:japanese"},{"Flies","culture:egyptian"}};
        foreach(var (species,culture) in expected)
        {
            var profile=cultures.ForSpecies(species)!;
            Assert(profile.CultureId==culture,$"{species} uses its requested founding culture");
            Assert(ReferenceEquals(cultures.Reference(profile),atlas.Entities[profile.CivilizationId]),$"{species} uses the authoritative atlas record without copying it");
            Assert(profile.Architecture.Length>=5 && profile.PersonalNames.Length>=3 && profile.SettlementNames.Length>=3,$"{species} provides configurable architectural and naming vocabulary");
        }
        Assert(cultures.ForSpecies("Bees")==null,"Unsupported biological species are not implicitly activated");
        Assert(atlas.Data.Entities.Where(e=>e.Status==AtlasEntityStatus.FictionalScenario).All(e=>!data.Profiles.Any(p=>p.CivilizationId==e.Id)),"Future catalog civilizations are not founding culture profiles");
        Assert(data.Profiles.Select(p=>p.Layout).Distinct().Count()==6 && data.Profiles.Select(p=>p.DefensePreference).Distinct().Count()==6,"Each theme has distinct planned settlement and defense preferences");
        Assert(data.Profiles.All(p=>p.MilitaryMetadata.Contains("no combat modifier")),"Theme metadata grants no permanent species combat advantage");
        Assert(cultures.Reference(cultures.ForSpecies("Spiders")!).Architecture==null && cultures.Reference(cultures.ForSpecies("Spiders")!).GovernmentId==null,"Scenario choices do not fabricate missing atlas architecture or government facts");
        Assert(cultures.Relationships(cultures.ForSpecies("Ants")!).Any() && cultures.Relationships(cultures.ForSpecies("Spiders")!).Count()==0,"Existing cultural relationships are reused and absent relationships remain absent");
        var store=new StateStore(Path.Combine(output,"cultures","state.json"));var session=Complete023(LivingWorlds.Create(store,"Isolated profiles","kingdoms",256,256));session.Controller.SetPaused(true);session.Controller.SaveNow();
        var manager=session.Controller.Manager;var initial=JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions);var saved=File.ReadAllBytes(session.Controller.SavePath);var rng=manager.CaptureWorld().RandomState;
        var nation=session.Nations!.Nations.Single(n=>n.Species=="Ants");var person=Guid.Parse("c5909fa0-c3ba-4abe-a94f-0165b8cfb1c5");var plan=cultures.Plan(nation,person);
        Assert(plan.CultureId=="culture:roman" && plan.GovernmentId=="aok:roman-imperial" && cultures.Governments[plan.GovernmentId].Offices[0].Title=="Emperor","Roman default plan uses an imperial political office");
        var republic=cultures.Plan(nation,person,"aok:roman-republic");
        Assert(cultures.Governments[republic.GovernmentId].Offices.Single().Title=="Consul" && republic.PersonalName==plan.PersonalName,"Republic office changes preserve personal identity and naming");
        Assert(cultures.Plan(nation,person)==plan,"Repeated naming plans are deterministic for stable IDs");
        Assert(cultures.Plan(nation with{Id=Guid.NewGuid()},Guid.NewGuid()).CultureId==plan.CultureId,"Multiple political nations can share one culture");
        var restored=JsonSerializer.Deserialize<NationCulturePlan>(JsonSerializer.Serialize(plan,HistoricalAtlas.Json),HistoricalAtlas.Json)!;
        Assert(restored==plan,"Proposed assignment values round-trip without rerolling names or IDs");
        Assert(typeof(PoliticalOffice).GetProperties().All(p=>!p.Name.Contains("Reproduc") && !p.Name.Contains("Fertility")),"Political office contracts contain no biological reproduction eligibility");
        bool Fails(Action action){try{action();return false;}catch(Exception ex) when(ex is InvalidDataException or ArgumentException or InvalidOperationException or NotSupportedException){return true;}}
        Assert(Fails(()=>cultures.Plan(nation,person,historicallyDated:true)),"Unverified dated historical naming cannot silently use mixed-era pools");
        Assert(Fails(()=>cultures.Plan(nation with{CivilizationId="historical:athens"},person)),"Existing incompatible civilization assignments require explicit conversion");
        Assert(Fails(()=>cultures.Plan(nation,person,"aok:egyptian-court")),"Cross-culture government selection fails explicitly");
        Assert(Fails(()=>cultures.Plan(nation,Guid.Empty)) && Fails(()=>cultures.Plan(nation with{Id=Guid.Empty},person)),"Missing stable individual or nation IDs are rejected");
        Assert(Fails(()=>cultures.Plan(nation with{Species="Bees"},person)),"Unsupported species cannot receive an accidental default culture");
        Assert(Fails(()=>new LivingCultures(data with{SchemaVersion=2},atlas)),"Unknown culture schema is rejected");
        Assert(Fails(()=>new LivingCultures(data with{Scenario="HistoricallyDated"},atlas)),"Profiles cannot misrepresent a dated historical scenario");
        Assert(Fails(()=>new LivingCultures(data with{Profiles=data.Profiles.SetItem(1,data.Profiles[0])},atlas)),"Duplicate theme identities and species are rejected");
        void RejectProfile(FoundingCulture profile,string description)=>Assert(Fails(()=>new LivingCultures(data with{Profiles=data.Profiles.SetItem(0,profile)},atlas)),description);
        RejectProfile(data.Profiles[0] with{CivilizationId="missing"},"Dangling atlas civilization references are rejected");
        RejectProfile(data.Profiles[0] with{CivilizationId=atlas.Data.Entities.First(e=>e.Status==AtlasEntityStatus.FictionalScenario).Id},"Fictional future references cannot become historical founding themes");
        RejectProfile(data.Profiles[0] with{CultureId="missing"},"Dangling culture references are rejected");
        RejectProfile(data.Profiles[0] with{CultureId="culture:mongol"},"Known atlas cultural identity cannot be contradicted by a profile");
        RejectProfile(data.Profiles[0] with{GovernmentId="missing"},"Dangling government references are rejected");
        RejectProfile(data.Profiles[0] with{AlternativeGovernments=["missing"]},"Unresolved alternative governments are rejected");
        RejectProfile(data.Profiles[0] with{PersonalNames=[]},"Empty personal-name pools fail validation");
        RejectProfile(data.Profiles[0] with{Architecture=["Villa","Villa"]},"Duplicate architectural vocabulary is rejected");
        Assert(Fails(()=>new LivingCultures(data with{Governments=data.Governments.SetItem(0,data.Governments[0] with{Offices=[new("head","",OfficeSuccession.Hereditary)]})},atlas)),"Invalid office definitions fail validation");
        Assert(Fails(()=>new LivingCultures(data with{Governments=data.Governments.SetItem(0,data.Governments[0] with{AtlasGovernmentId="missing"})},atlas)),"Government type must resolve through the existing atlas");
        using(var bad=new MemoryStream(Encoding.UTF8.GetBytes("{broken")))Assert(Fails(()=>LivingCultures.Read(bad,atlas)),"Malformed profile JSON fails with a recoverable data error");
        using(var oversized=new MemoryStream(new byte[128*1024+1]))Assert(Fails(()=>LivingCultures.Read(oversized,atlas)),"Profile loading has a bounded byte budget");
        var json=JsonSerializer.Serialize(data,HistoricalAtlas.Json).Replace("\"Title\":\"Emperor\"","\"Title\":\"Emperor\",\"CanReproduce\":true");
        using(var bad=new MemoryStream(Encoding.UTF8.GetBytes(json)))Assert(Fails(()=>LivingCultures.Read(bad,atlas)),"Unknown biological fields cannot enter political-office data");
        var owner=new HomeWindow(store);owner.Show();var chooser=new LivingWorldDialog(owner,store);chooser.Show();var browser=HistoricalAtlasWindow.ShowFor(chooser);Pump(TimeSpan.FromMilliseconds(80));
        browser.Category.SelectedItem="Age of Kingdoms themes";Assert(browser.Results.Items.Count==6,"Atlas browser exposes exactly six isolated theme previews");
        browser.Query.Text="Roman";Pump(TimeSpan.FromMilliseconds(80));Assert(browser.Results.Items.Count==1 && browser.Detail.Text.Contains("Emperor") && browser.Detail.Text.Contains("culture:roman"),"Theme preview resolves its culture and political offices");
        Assert(browser.Detail.Text.Contains("not applied to worlds") && browser.Detail.Text.Contains("FICTIONAL MIXED-ERA"),"Preview clearly states activation and chronology limits");
        Descendants(browser).OfType<Button>().Single(b=>Equals(b.Content,"Show reference excerpt")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert(Descendants(browser).OfType<TextBlock>().Any(t=>t.Text.Contains("Atlas block")),"Theme preview links to original atlas reference excerpts");
        Pump(TimeSpan.FromMilliseconds(40));Save018(browser,(int)browser.ActualWidth,(int)browser.ActualHeight,"culture-preview024");browser.Close();chooser.Close();owner.Close();
        Assert(JsonSerializer.Serialize(manager.CaptureWorld(),ArcadeWorldStore.JsonOptions)==initial && manager.CaptureWorld().RandomState==rng,"Planning and browsing leave nations, reproduction, population and RNG unchanged");
        Assert(saved.SequenceEqual(File.ReadAllBytes(session.Controller.SavePath)) && session.Nations.Nations.All(n=>n.CivilizationId==null),"Isolated Phase 3A does not migrate worlds or activate cultural gameplay");
        var batch=Stopwatch.StartNew();for(var i=0;i<10000;i++)cultures.Plan(nation,person);batch.Stop();
        var metrics=new{ColdAtlasAndProfilesLoadMs=loadMs,ColdAtlasAndProfilesAllocatedBytes=allocated,WarmReadAndValidationMs=warmClock.Elapsed.TotalMilliseconds,WarmReadAndValidationAllocatedBytes=warmAllocated,Plan10000Ms=batch.Elapsed.TotalMilliseconds,ActiveSimulationHooks=0,NewWorldSaveFields=0};Console.WriteLine(JsonSerializer.Serialize(metrics));File.WriteAllText(Path.Combine(output,"cultures024.json"),JsonSerializer.Serialize(metrics,new JsonSerializerOptions{WriteIndented=true}));
    }
}
