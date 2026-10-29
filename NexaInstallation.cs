using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace SessionDashboard;

internal static class NexaInstallation
{
    internal const string AppId="{70F24EC8-70DA-4EF6-AF57-68ACB4BE0330}";
    internal const string TestAppId="{B80F06DA-5764-44B7-B632-68ACB4BE0330}";
    internal sealed record Marker(string AppId,string Scope,string? DataRoot=null);
    internal static bool IsInstalled(string? executable=null)
    {
        try
        {
            var folder=Path.GetDirectoryName(Path.GetFullPath(executable??Environment.ProcessPath!))!;
            var marker=JsonSerializer.Deserialize<Marker>(File.ReadAllText(Path.Combine(folder,"installation.json")));
            if(marker is not {Scope:"CurrentUser"} || marker.AppId is not (AppId or TestAppId))return false;
            using var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\"+marker.AppId+"_is1");
            return key?.GetValue("InstallLocation") is string location && Path.TrimEndingDirectorySeparator(Path.GetFullPath(location)).Equals(folder,StringComparison.OrdinalIgnoreCase);
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException){return false;}
    }
    internal static string? FixtureDataRoot
    {
        get
        {
            try
            {
                var folder=Path.GetDirectoryName(Environment.ProcessPath!)!;
                var marker=JsonSerializer.Deserialize<Marker>(File.ReadAllText(Path.Combine(folder,"installation.json")));
                var expected=Path.Combine(Path.GetDirectoryName(folder)!,"data");
                return marker is {AppId:TestAppId,DataRoot:{} data} && IsInstalled() && Path.GetFullPath(data).Equals(expected,StringComparison.OrdinalIgnoreCase)?expected:null;
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException){return null;}
        }
    }
    internal static bool UpdaterIncluded=>typeof(App).Assembly.GetType("SessionDashboard.UpdateFlow")!=null;
    internal static string Distribution=>IsInstalled()?"Installed":"Portable";
    internal static bool UpdatesEnabled
    {
        get
        {
#if NEXA_CHECKS
            return true; // Checks assembly only; never used in either shipping executable.
#elif NEXA_UPDATES
            return true;
#else
            return false;
#endif
        }
    }
}
