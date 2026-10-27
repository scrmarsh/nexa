using System.Collections.Immutable;

namespace SessionDashboard;
internal enum AtlasEntityKind { Unclassified, Culture, Settlement, State, StatePhase, Dynasty, Aggregate, NarrativeTradition }
internal enum AtlasEntityStatus { HistoricalReference, TraditionalReference, FictionalScenario }
internal enum AtlasSourceClass { ReferenceUnverified, Attested, CulturalUmbrella, Debated, BiblicalTradition, MythicLegendary, FictionalSpeculative }
internal enum AtlasRelationKind { PoliticalContinuity, FictionalBranch, ContributedTo, UnionComponent, Fragmentation }
internal enum AtlasSymbolUsage { AtlasArtDirection, GameplayAvatar, VerifiedHistoricalSymbol }
internal sealed record AtlasOccurrence(string Name,int BlockId,string? EraId,AtlasSourceClass Classification);
internal sealed record AtlasCulture(string Id,string Name);
internal sealed record AtlasDynasty(string Id,string Name,string CultureId);
internal sealed record AtlasGovernment(string Id,string Name);
internal sealed record AtlasRepresentation(string Id,string Name,AtlasSymbolUsage Usage,bool Playable,int SourceBlockId);
internal sealed record AtlasEra(string Id,string Name,long? StartYear,long? EndYear,bool Fictional,string Features,int BlockId);
internal sealed record AtlasRelationship(string Id,string FromId,string ToId,AtlasRelationKind Kind,string Provenance,bool Forced);
internal sealed record AtlasWar(string Id,string Name,long StartYear,long EndYear,string Description,int BlockId,bool Fictional,bool Automatic);
internal sealed record AtlasTechnology(string Id,long StartYear,long EndYear,string Vehicles,string Weapons,string Rule,int BlockId,bool Fictional);
internal sealed record AtlasBranch(string Id,string Thread,string NearText,string FarText,ImmutableArray<string> EntityIds,int BlockId,bool Fictional);
internal sealed record AtlasSourceBlock(int Index,string Kind,ImmutableArray<string> Cells);
internal sealed record AtlasSource(int SchemaVersion,string Document,string Sha256,ImmutableArray<AtlasSourceBlock> Blocks);
internal sealed record AtlasCivilization
{
    public string Id {get;init;}="";
    public string Name {get;init;}="";
    public ImmutableArray<string> Aliases {get;init;}=[];
    public AtlasEntityKind Kind {get;init;}
    public AtlasEntityStatus Status {get;init;}
    public string? CultureId {get;init;}
    public string? DynastyId {get;init;}
    public string? GovernmentId {get;init;}
    public ImmutableArray<string> RulerTitles {get;init;}=[];
    public string? Architecture {get;init;}
    public string? RepresentationId {get;init;}
    public string? Origin {get;init;}
    public long? EmergenceYear {get;init;}
    public long? EndYear {get;init;}
    public ImmutableArray<string> EraIds {get;init;}=[];
    public ImmutableArray<string> MilitaryCharacteristics {get;init;}=[];
    public ImmutableArray<AtlasOccurrence> Occurrences {get;init;}=[];
    public string? DiplomaticStatus {get;init;}
    public ImmutableArray<string> Notes {get;init;}=[];
}
internal sealed record HistoricalAtlasData
{
    public int SchemaVersion {get;init;}
    public string SourceDocument {get;init;}="";
    public string SourceSha256 {get;init;}="";
    public string ProposedVersion {get;init;}="";
    public ImmutableArray<AtlasCivilization> Entities {get;init;}=[];
    public ImmutableArray<AtlasCulture> Cultures {get;init;}=[];
    public ImmutableArray<AtlasDynasty> Dynasties {get;init;}=[];
    public ImmutableArray<AtlasGovernment> Governments {get;init;}=[];
    public ImmutableArray<AtlasRepresentation> Representations {get;init;}=[];
    public ImmutableArray<AtlasEra> Eras {get;init;}=[];
    public ImmutableArray<AtlasRelationship> Relationships {get;init;}=[];
    public ImmutableArray<AtlasWar> Wars {get;init;}=[];
    public ImmutableArray<AtlasTechnology> Technologies {get;init;}=[];
    public ImmutableArray<AtlasBranch> Branches {get;init;}=[];
}
