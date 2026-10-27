using System.IO;
using System.Text.Json;
using System.Windows;

namespace SessionDashboard;
internal enum ArcadeGame { ColonyWars, LivingWorlds }
internal enum TerrainBiome : byte { DeepOcean, ShallowOcean, River, Lake, Beach, Grassland, Forest, Desert, Hills, Mountain, Snow }
internal enum TerrainQuality { Performance, Balanced, High }
internal sealed record TerrainEdit(int X,int Y,TerrainBiome Biome);
internal sealed record LivingWorldInfo
{
    public int SchemaVersion {get;init;}=1;
    public Guid WorldId {get;init;}=Guid.NewGuid();
    public string Name {get;set;}="New World";
    public string Seed {get;init;}="";
    public int GeneratorVersion {get;init;}=1;
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public GrandGeneration? Generation {get;init;}
    public DateTimeOffset CreatedAt {get;init;}=DateTimeOffset.UtcNow;
    public int Tiles {get;init;}=512;
    public int TilesY {get;init;}
    public TerrainQuality Quality {get;set;}=TerrainQuality.Balanced;
    public int ChunkSize {get;init;}=64;
    public double TileSize {get;init;}=6;
    public double FocusX {get;set;}
    public double FocusY {get;set;}
    public double CameraZoom {get;set;}
    public double CameraX {get;set;}
    public double CameraY {get;set;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Guid? SelectedNationId {get;set;}
    [System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool CivilizationMode {get;init;}
    public TerrainEdit[] Edits {get;init;}=[];
    public double Extent=>Tiles*TileSize;
    public int HeightTiles=>TilesY==0 ? Tiles : TilesY;
    public double HeightExtent=>HeightTiles*TileSize;
    public void Validate()
    {
        var square=Tiles is 256 or 512 or 640 && (TilesY==0 || TilesY==Tiles);
        var wide=GeneratorVersion==2 && (Tiles,TilesY) is (1024,576) or (1536,864) or (2048,1152);
        if(GeneratorVersion==3){Generation?.Validate();if(Generation==null)throw new InvalidDataException("Generator 3 requires versioned generation settings.");wide=GrandGeneration.Sizes.Any(s=>s.Width==Tiles && s.Height==HeightTiles);square=false;}
        if(SchemaVersion!=1 || GeneratorVersion is not (1 or 2 or 3) || WorldId==Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length>80 || string.IsNullOrEmpty(Seed) || Seed.Length>100 || !(square || wide) || !Enum.IsDefined(Quality) || ChunkSize is not (32 or 64 or 128) || TileSize!=6 || Edits==null || Edits.Length>100000 || Edits.Any(e=>e==null || e.X<0 || e.Y<0 || e.X>=Tiles || e.Y>=HeightTiles || !Enum.IsDefined(e.Biome)) || !new[]{FocusX,FocusY,CameraZoom,CameraX,CameraY}.All(double.IsFinite) || CameraZoom<0 || CameraZoom>3 || Math.Abs(CameraX)>1e6 || Math.Abs(CameraY)>1e6)
            throw new InvalidDataException("Unsupported or invalid Living World. Its files were preserved.");
    }
}
internal sealed record ArcadeGameSelection(ArcadeGame Game=ArcadeGame.ColonyWars,Guid? WorldId=null);
internal static class LivingFiles
{
    internal static readonly JsonSerializerOptions Json=new(){IgnoreReadOnlyProperties=true};
    internal static T Read<T>(string path)
    {
        if(new FileInfo(path).Length>8*1024*1024)throw new InvalidDataException("Living World file is too large.");
        try {return JsonSerializer.Deserialize<T>(File.ReadAllText(path),Json)??throw new InvalidDataException("Empty Living World file.");}
        catch(JsonException ex) {throw new InvalidDataException("Living World data could not be read. Its files were preserved.",ex);}
    }
    internal static void Save<T>(string path,T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {JsonSerializer.Serialize(stream,value,Json);stream.Flush(true);}
            _=Read<T>(temp);
            if(File.Exists(path))File.Replace(temp,path,path+".bak",true);else File.Move(temp,path);
        }
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
}
