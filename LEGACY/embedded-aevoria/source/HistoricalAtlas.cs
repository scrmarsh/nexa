using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SessionDashboard;
internal sealed class HistoricalAtlas
{
    private static readonly Lazy<HistoricalAtlas> shared=new(LoadEmbedded);
    internal static HistoricalAtlas Default=>shared.Value;
    internal static readonly JsonSerializerOptions Json=new()
    {
        UnmappedMemberHandling=JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations=true,
        Converters={new JsonStringEnumConverter(allowIntegerValues:false)}
    };
    internal HistoricalAtlasData Data {get;}
    internal AtlasSource Source {get;}
    internal FrozenDictionary<string,AtlasCivilization> Entities {get;}
    internal FrozenDictionary<string,AtlasEra> Eras {get;}
    internal FrozenDictionary<int,AtlasSourceBlock> SourceBlocks {get;}
    internal HistoricalAtlas(HistoricalAtlasData data,AtlasSource source)
    {
        Validate(data,source);Data=data;Source=source;
        Entities=data.Entities.ToFrozenDictionary(e=>e.Id,StringComparer.Ordinal);
        Eras=data.Eras.ToFrozenDictionary(e=>e.Id,StringComparer.Ordinal);
        SourceBlocks=source.Blocks.ToFrozenDictionary(b=>b.Index);
    }
    private static HistoricalAtlas LoadEmbedded()
    {
        var assembly=typeof(HistoricalAtlas).Assembly;
        using var catalog=assembly.GetManifestResourceStream("SessionDashboard.History.Catalog")??throw new InvalidDataException("Civilization atlas is missing.");
        using var source=assembly.GetManifestResourceStream("SessionDashboard.History.Source")??throw new InvalidDataException("Atlas reference text is missing.");
        return Read(catalog,source);
    }
    internal static HistoricalAtlas Read(Stream catalog,Stream source)
    {
        try {return new(ReadBounded<HistoricalAtlasData>(catalog),ReadBounded<AtlasSource>(source));}
        catch(JsonException ex) {throw new InvalidDataException("Civilization atlas could not be read.",ex);}
    }
    private static T ReadBounded<T>(Stream stream)
    {
        using var bytes=new MemoryStream();var buffer=new byte[8192];int count;
        while((count=stream.Read(buffer))>0) {if(bytes.Length+count>8*1024*1024)throw new InvalidDataException("Atlas exceeds its loading budget.");bytes.Write(buffer,0,count);}
        return JsonSerializer.Deserialize<T>(bytes.GetBuffer().AsSpan(0,(int)bytes.Length),Json)??throw new InvalidDataException("Empty civilization atlas.");
    }
    internal IEnumerable<AtlasCivilization> Search(string query,string? eraId=null,AtlasEntityStatus? status=null)
    {
        query=(query??"").Trim();if(query.Length>200)query=query[..200];
        return Data.Entities.Where(e=>(eraId==null || e.EraIds.Contains(eraId)) && (status==null || e.Status==status) &&
            (Matches(e.Name,query) || e.Aliases.Any(a=>Matches(a,query)) || Matches(e.Origin??"",query)));
    }
    internal static bool Matches(string text,string query)=>CultureInfo.InvariantCulture.CompareInfo.IndexOf(text,query,CompareOptions.IgnoreCase|CompareOptions.IgnoreNonSpace)>=0;
    internal static string Year(long? year)=>year switch {null=>"Not specified",0=>throw new ArgumentOutOfRangeException(nameof(year),"Historical dates have no year zero."),<0=>$"{year.ToString()![1..]} BCE",_=>$"{year} CE"};
    internal static string SourceLabel(AtlasSourceClass value)=>value switch
    {
        AtlasSourceClass.ReferenceUnverified=>"Reference inventory; not independently verified",
        AtlasSourceClass.BiblicalTradition=>"Biblical textual tradition; evaluate independent evidence separately",
        AtlasSourceClass.MythicLegendary=>"Mythic / legendary narrative; no archaeological date assigned",
        AtlasSourceClass.FictionalSpeculative=>"Fictional / speculative worldbuilding",
        AtlasSourceClass.CulturalUmbrella=>"Broad cultural grouping",
        AtlasSourceClass.Debated=>"Debated identification or chronology",
        _=>"Attested (requires supporting source)"
    };
    internal static void Validate(HistoricalAtlasData data,AtlasSource source)
    {
        static void Need(bool condition,string message) {if(!condition)throw new InvalidDataException("Invalid civilization atlas: "+message);}
        static bool Text(string? value)=>!string.IsNullOrWhiteSpace(value);
        static bool Date(long? value)=>value!=0;
        static HashSet<string> Ids(IEnumerable<string> values)
        {
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var id in values)if(string.IsNullOrWhiteSpace(id) || !ids.Add(id))throw new InvalidDataException("Atlas identity is missing or duplicated.");return ids;
        }
        Need(data.SchemaVersion==1 && source.SchemaVersion==1,"unsupported schema");
        Need(Text(data.SourceDocument) && data.SourceDocument==source.Document && data.SourceSha256==source.Sha256 && data.SourceSha256.Length==64 && data.SourceSha256.All(Uri.IsHexDigit),"reference provenance");
        Need(!data.Entities.IsDefaultOrEmpty && data.Entities.Length<=50000 && !source.Blocks.IsDefaultOrEmpty,"empty catalog or reference");
        Need(!data.Cultures.IsDefault && !data.Dynasties.IsDefault && !data.Governments.IsDefault && !data.Representations.IsDefault && !data.Eras.IsDefault && !data.Relationships.IsDefault && !data.Wars.IsDefault && !data.Technologies.IsDefault && !data.Branches.IsDefault,"missing catalog collections");
        Need(data.Entities.All(e=>e!=null) && data.Cultures.All(e=>e!=null) && data.Dynasties.All(e=>e!=null) && data.Governments.All(e=>e!=null) && data.Representations.All(e=>e!=null) && data.Eras.All(e=>e!=null) && data.Relationships.All(e=>e!=null) && data.Wars.All(e=>e!=null) && data.Technologies.All(e=>e!=null) && data.Branches.All(e=>e!=null) && source.Blocks.All(e=>e!=null),"null catalog records");
        var entities=Ids(data.Entities.Select(e=>e.Id));var cultures=Ids(data.Cultures.Select(e=>e.Id));var dynasties=Ids(data.Dynasties.Select(e=>e.Id));
        var governments=Ids(data.Governments.Select(e=>e.Id));var symbols=Ids(data.Representations.Select(e=>e.Id));var eras=Ids(data.Eras.Select(e=>e.Id));
        _=Ids(data.Wars.Select(e=>e.Id));_=Ids(data.Technologies.Select(e=>e.Id));_=Ids(data.Branches.Select(e=>e.Id));_=Ids(data.Relationships.Select(e=>e.Id));
        var blocks=source.Blocks.Select(b=>b.Index).ToHashSet();Need(blocks.Count==source.Blocks.Length && source.Blocks.All(b=>b.Index>0 && !b.Cells.IsDefaultOrEmpty && b.Cells.All(c=>c!=null) && b.Kind is "paragraph" or "row"),"reference blocks");
        foreach(var e in data.Entities)
        {
            Need(Text(e.Name) && Enum.IsDefined(e.Kind) && Enum.IsDefined(e.Status),"civilization identity");
            Need((e.CultureId==null || cultures.Contains(e.CultureId)) && (e.DynastyId==null || dynasties.Contains(e.DynastyId)) && (e.GovernmentId==null || governments.Contains(e.GovernmentId)) && (e.RepresentationId==null || symbols.Contains(e.RepresentationId)),"identity references");
            Need(!e.EraIds.IsDefault && e.EraIds.All(eras.Contains) && Date(e.EmergenceYear) && Date(e.EndYear) && (e.EmergenceYear==null || e.EndYear==null || e.EmergenceYear<=e.EndYear),"civilization chronology");
            Need(!e.Aliases.IsDefault && e.Aliases.All(Text) && !e.RulerTitles.IsDefault && e.RulerTitles.All(Text) && !e.MilitaryCharacteristics.IsDefault && !e.Notes.IsDefault && e.Notes.All(Text),"civilization attributes");
            Need(!e.Occurrences.IsDefaultOrEmpty && e.Occurrences.All(o=>o!=null && Text(o.Name) && blocks.Contains(o.BlockId) && (o.EraId==null || eras.Contains(o.EraId)) && Enum.IsDefined(o.Classification)),"source classification");
            if(e.Status==AtlasEntityStatus.FictionalScenario)Need(e.EmergenceYear>=2027 && e.Occurrences.All(o=>o.Classification==AtlasSourceClass.FictionalSpeculative),"future fiction cannot be labeled established history");
            else Need(e.Occurrences.All(o=>o.Classification!=AtlasSourceClass.FictionalSpeculative),"historical inventory status");
            if(e.Status==AtlasEntityStatus.TraditionalReference)Need(e.EmergenceYear==null && e.EndYear==null && e.Occurrences.All(o=>o.Classification is AtlasSourceClass.BiblicalTradition or AtlasSourceClass.MythicLegendary),"undated textual tradition");
        }
        foreach(var e in data.Eras)Need(Text(e.Name) && blocks.Contains(e.BlockId) && Date(e.StartYear) && Date(e.EndYear) && (e.StartYear==null || e.EndYear==null || e.StartYear<=e.EndYear) && (!e.Fictional || e.StartYear>=2027),"era chronology");
        foreach(var d in data.Dynasties)Need(Text(d.Name) && cultures.Contains(d.CultureId),"dynasty continuity");
        foreach(var s in data.Representations)Need(Text(s.Name) && Enum.IsDefined(s.Usage) && !s.Playable && blocks.Contains(s.SourceBlockId),"symbolic identity is not an implemented animal unit");
        foreach(var r in data.Relationships)Need(entities.Contains(r.FromId) && entities.Contains(r.ToId) && r.FromId!=r.ToId && Enum.IsDefined(r.Kind) && Text(r.Provenance) && !r.Forced,"successor relationship");
        foreach(var w in data.Wars)Need(Text(w.Name) && Text(w.Description) && w.StartYear>=2027 && w.StartYear<=w.EndYear && blocks.Contains(w.BlockId) && w.Fictional && !w.Automatic,"fictional war template");
        foreach(var t in data.Technologies)Need(t.StartYear>=2027 && t.StartYear<=t.EndYear && Text(t.Vehicles) && Text(t.Weapons) && Text(t.Rule) && blocks.Contains(t.BlockId) && t.Fictional,"technology scenario");
        foreach(var b in data.Branches)Need(Text(b.Thread) && !b.EntityIds.IsDefaultOrEmpty && b.EntityIds.All(entities.Contains) && blocks.Contains(b.BlockId) && b.Fictional,"possible successor branch");
    }
}
