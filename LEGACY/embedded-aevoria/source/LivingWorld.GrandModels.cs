using System.IO;
namespace SessionDashboard;
internal enum GeographyProfile { ContinentalKingdoms, BalancedContinents, Archipelago, Custom }
internal sealed record GrandGeneration
{
    public int SchemaVersion {get;init;}=1;
    public GeographyProfile Profile {get;init;}=GeographyProfile.ContinentalKingdoms;
    public double LandTarget {get;init;}=.80;
    public int PopulationBudget {get;init;}=180;
    internal double LandFraction=>Profile switch {GeographyProfile.ContinentalKingdoms=>.80,GeographyProfile.BalancedContinents=>.56,GeographyProfile.Archipelago=>.36,_=>LandTarget};
    internal void Validate()
    {if(SchemaVersion!=1 || !Enum.IsDefined(Profile) || !double.IsFinite(LandTarget) || LandTarget is <.35 or >.85 || PopulationBudget is <60 or >180)throw new InvalidDataException("Unsupported grand-world generation settings.");}
    internal static readonly (string Name,int Width,int Height)[] Sizes=[("Small",512,512),("Standard",1024,576),("Large",2048,1152),("Very Large",3072,1728),("Huge",4096,2304),("Massive — Experimental",6144,3456)];
}
