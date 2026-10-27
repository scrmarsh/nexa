using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SessionDashboard;

internal enum OfficeSuccession { Hereditary, Election, Appointment }
// Political offices deliberately carry no sex, fertility or BugEntity promotion rules.
internal sealed record PoliticalOffice(string Id,string Title,OfficeSuccession Succession);
internal sealed record CulturalGovernment(string Id,string AtlasGovernmentId,ImmutableArray<PoliticalOffice> Offices);
internal sealed record CulturalConstruction(CivicPurpose Purpose,string Archetype,double Materials,int MinimumStage);
internal sealed record FoundingCulture
{
    public string Id {get;init;}="";
    public string Species {get;init;}="";
    public string Name {get;init;}="";
    public string CivilizationId {get;init;}="";
    public string CultureId {get;init;}="";
    public string GovernmentId {get;init;}="";
    public ImmutableArray<string> AlternativeGovernments {get;init;}=[];
    public string StartingEra {get;init;}="";
    public string Layout {get;init;}="";
    public string DefensePreference {get;init;}="";
    public string MilitaryMetadata {get;init;}="";
    public ImmutableArray<string> Architecture {get;init;}=[];
    public ImmutableArray<string> PersonalNames {get;init;}=[];
    public ImmutableArray<string> SettlementNames {get;init;}=[];
    public ImmutableArray<CulturalConstruction> Construction {get;init;}=[];
}
internal sealed record FoundingCultureData(int SchemaVersion,string Scenario,string Provenance,
    ImmutableArray<CulturalGovernment> Governments,ImmutableArray<FoundingCulture> Profiles);
// A proposed assignment is a value, never a mutation of a nation, leader or world save.
internal sealed record NationCulturePlan(Guid NationId,string ProfileId,string CivilizationId,string CultureId,
    string GovernmentId,string OfficeId,string PersonalName);

internal sealed class LivingCultures
{
    private static readonly Lazy<LivingCultures> shared=new(LoadEmbedded);
    internal static LivingCultures Default=>shared.Value;
    internal FoundingCultureData Data {get;}
    internal FrozenDictionary<string,FoundingCulture> Profiles {get;}
    internal FrozenDictionary<string,CulturalGovernment> Governments {get;}
    private readonly FrozenDictionary<string,FoundingCulture> species;
    private readonly HistoricalAtlas atlas;
    internal LivingCultures(FoundingCultureData data,HistoricalAtlas atlas)
    {
        Validate(data,atlas);Data=data;this.atlas=atlas;
        Profiles=data.Profiles.ToFrozenDictionary(p=>p.Id,StringComparer.Ordinal);
        species=data.Profiles.ToFrozenDictionary(p=>p.Species,StringComparer.Ordinal);
        Governments=data.Governments.ToFrozenDictionary(g=>g.Id,StringComparer.Ordinal);
    }
    private static LivingCultures LoadEmbedded()
    {
        using var stream=typeof(LivingCultures).Assembly.GetManifestResourceStream("SessionDashboard.History.FoundingCultures")
            ??throw new InvalidDataException("Age of Kingdoms culture profiles are missing.");
        return Read(stream,HistoricalAtlas.Default);
    }
    internal static LivingCultures Read(Stream stream,HistoricalAtlas atlas)
    {
        using var bytes=new MemoryStream();var buffer=new byte[4096];int count;
        while((count=stream.Read(buffer))>0){if(bytes.Length+count>128*1024)throw new InvalidDataException("Culture profiles exceed their loading budget.");bytes.Write(buffer,0,count);}
        try {return new(JsonSerializer.Deserialize<FoundingCultureData>(bytes.GetBuffer().AsSpan(0,(int)bytes.Length),HistoricalAtlas.Json)
            ??throw new InvalidDataException("Empty culture profiles."),atlas);}
        catch(JsonException ex){throw new InvalidDataException("Culture profiles could not be read.",ex);}
    }
    internal FoundingCulture? ForSpecies(string key)=>species.GetValueOrDefault(key);
    internal AtlasCivilization Reference(FoundingCulture profile)=>atlas.Entities[profile.CivilizationId];
    internal IEnumerable<AtlasRelationship> Relationships(FoundingCulture profile)=>atlas.Data.Relationships.Where(r=>r.FromId==profile.CivilizationId || r.ToId==profile.CivilizationId);
    internal NationCulturePlan Plan(LivingNation nation,Guid individualId,string? government=null,bool historicallyDated=false)
    {
        if(historicallyDated)throw new NotSupportedException("Verified dated name pools and chronology are not available; these themes support a fictional mixed-era scenario only.");
        var profile=ForSpecies(nation.Species)??throw new InvalidDataException("No supported founding culture for this species.");
        if(nation.Id==Guid.Empty || individualId==Guid.Empty)throw new ArgumentException("Stable nation and individual identities are required.");
        if(nation.CivilizationId!=null && nation.CivilizationId!=profile.CivilizationId)throw new InvalidOperationException("An existing civilization assignment requires an explicit reviewed conversion.");
        var governmentId=government??profile.GovernmentId;
        if(governmentId!=profile.GovernmentId && !profile.AlternativeGovernments.Contains(governmentId))throw new InvalidDataException("Government is not supported by this culture profile.");
        var office=Governments[governmentId].Offices[0];
        return new(nation.Id,profile.Id,profile.CivilizationId,profile.CultureId,governmentId,office.Id,Name(profile.PersonalNames,individualId,profile.Id+"/person"));
    }
    internal static string Name(ImmutableArray<string> pool,Guid identity,string domain)
    {
        if(pool.IsDefaultOrEmpty || identity==Guid.Empty || string.IsNullOrWhiteSpace(domain))throw new ArgumentException("A name pool, identity and domain are required.");
        var hash=SHA256.HashData(Encoding.UTF8.GetBytes(domain+"/"+identity.ToString("N")));
        // Explicit byte order avoids platform-dependent GetHashCode/BitConverter behavior.
        var value=((uint)hash[0]<<24)|((uint)hash[1]<<16)|((uint)hash[2]<<8)|hash[3];return pool[(int)(value%(uint)pool.Length)];
    }
    internal static void Validate(FoundingCultureData data,HistoricalAtlas atlas)
    {
        static void Need(bool value,string message){if(!value)throw new InvalidDataException("Invalid founding cultures: "+message);}
        static bool Text(string? value)=>!string.IsNullOrWhiteSpace(value) && value.Length<=240;
        static bool Names(ImmutableArray<string> values)=>!values.IsDefaultOrEmpty && values.Length<=64 && values.All(Text) && values.Distinct(StringComparer.Ordinal).Count()==values.Length;
        Need(data!=null && data.SchemaVersion==1 && data.Scenario=="FictionalMixedEra" && Text(data.Provenance),"schema/scenario/provenance");
        Need(!data!.Profiles.IsDefaultOrEmpty && data.Profiles.Length==6 && data.Profiles.All(p=>p!=null),"six supported themes required");
        Need(!data.Governments.IsDefaultOrEmpty && data.Governments.Length<=16 && data.Governments.All(g=>g!=null),"government profiles");
        Need(data.Governments.Select(g=>g.Id).Distinct(StringComparer.Ordinal).Count()==data.Governments.Length,"duplicate governments");
        var governments=data.Governments.Select(g=>g.Id).ToHashSet(StringComparer.Ordinal);
        foreach(var g in data.Governments)
        {
            Need(Text(g.Id) && atlas.Data.Governments.Any(a=>a.Id==g.AtlasGovernmentId),"atlas government reference");
            Need(!g.Offices.IsDefaultOrEmpty && g.Offices.Length<=4 && g.Offices.All(o=>o!=null && Text(o.Id) && Text(o.Title) && Enum.IsDefined(o.Succession)) && g.Offices.Select(o=>o.Id).Distinct(StringComparer.Ordinal).Count()==g.Offices.Length,"political offices");
        }
        Need(data.Profiles.Select(p=>p.Id).Distinct(StringComparer.Ordinal).Count()==6 && data.Profiles.Select(p=>p.Species).ToHashSet(StringComparer.Ordinal).SetEquals(LivingSpecies.Map.Keys),"duplicate or unsupported theme/species");
        foreach(var p in data.Profiles)
        {
            Need(Text(p.Id) && Text(p.Name) && Text(p.Layout) && Text(p.DefensePreference) && Text(p.StartingEra) && Text(p.MilitaryMetadata),"profile metadata");
            Need(atlas.Entities.TryGetValue(p.CivilizationId,out var reference) && reference.Status==AtlasEntityStatus.HistoricalReference,"historical catalog reference");
            Need(atlas.Data.Cultures.Any(c=>c.Id==p.CultureId) && (reference!.CultureId==null || reference.CultureId==p.CultureId),"culture reference");
            Need(governments.Contains(p.GovernmentId) && !p.AlternativeGovernments.IsDefault && p.AlternativeGovernments.All(governments.Contains) && p.AlternativeGovernments.Distinct().Count()==p.AlternativeGovernments.Length,"supported governments");
            Need(Names(p.PersonalNames) && Names(p.SettlementNames) && Names(p.Architecture),"names/architectural vocabulary");
            Need(!p.Construction.IsDefault && p.Construction.Length==7 && p.Construction.Select(r=>r.Purpose).Distinct().Count()==7 && p.Construction.All(r=>Enum.IsDefined(r.Purpose) && p.Architecture.Contains(r.Archetype) && double.IsFinite(r.Materials) && r.Materials>0 && r.Materials<=100 && r.MinimumStage>=0 && r.MinimumStage<=3),"construction design rules");
        }
    }
}
