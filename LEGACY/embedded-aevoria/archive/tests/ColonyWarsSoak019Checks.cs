using System.IO;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckColonySoak019()
    {
        var war=War(1919);var peak=0;var events=0;var projectiles=0;
        for(var i=0;i<90000;i++)
        {
            war.Advance(.08);peak=Math.Max(peak,war.Bugs.Count);events=Math.Max(events,war.Announcements.Count);projectiles=Math.Max(projectiles,war.Projectiles.Count);
            if(war.Bugs.Count>240 || war.Announcements.Count>7 || war.Projectiles.Count>48 || war.Webs.Count>4)throw new Exception("Soak resource bound exceeded");
        }
        Assert(war.CompletedMatches>=20 && peak<=240 && events<=7 && projectiles<=48,"Two simulated hours complete repeated matches with bounded entities, effects and announcements");
        File.WriteAllText(Path.Combine(output,"soak019.json"),System.Text.Json.JsonSerializer.Serialize(new {SimulatedSeconds=7200,war.CompletedMatches,PeakEntities=peak,PeakQueuedAnnouncements=events,PeakProjectiles=projectiles,war.Victories},new System.Text.Json.JsonSerializerOptions {WriteIndented=true}));
    }
}
