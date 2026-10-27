using System.Windows;
namespace SessionDashboard;
internal static class LegacyBugHabitats
{
    internal static readonly Dictionary<string,BugSpecies> KindSpecies=new()
    {
        ["Ant Bed"]=BugSpecies.Ants, ["Centipede Den"]=BugSpecies.Centipedes,
        ["Ladybug Shelter"]=BugSpecies.Ladybugs, ["Fly Feeding Ground"]=BugSpecies.Flies,
        ["Roach Hideout"]=BugSpecies.Roaches, ["Spider Retreat"]=BugSpecies.Spiders
    };
    internal static readonly BugSpecies[] Species=KindSpecies.Values.ToArray();
}
