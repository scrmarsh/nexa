using System.Reflection;
using System.Text.RegularExpressions;
namespace SessionDashboard;
internal sealed record NexaReleaseIdentity(Version Version,string Stage,string OriginalTag="")
{
    // Confirmed pre-migration Alpha history. Never apply this table to Beta.
    internal static readonly IReadOnlyDictionary<Version,Version> LegacyAlpha=Enumerable.Range(1,26).ToDictionary(n=>new Version(0,1,n),n=>new Version(0,0,n));
    internal Version Canonical=>Canonicalize(Version,Stage);
    internal string Label=>$"nexa v{Canonical.ToString(3)} {Stage.ToLowerInvariant()}";
    internal string Tag=>$"v{Canonical.ToString(3)}-{Stage.ToLowerInvariant()}";
    internal static NexaReleaseIdentity Current
    {
        get
        {
            var version=typeof(App).Assembly.GetName().Version??new(0,0,0);
            var suffix=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0].Split('-').LastOrDefault();
            var stage=suffix switch{"alpha"=>"Alpha","beta"=>"Beta","stable"=>"Stable",_=>version.Major>=1?"Stable":version.Minor==1?"Beta":"Alpha"};
            return new(version,stage);
        }
    }
    internal static Version Canonicalize(Version version,string stage)=>stage.Equals("Alpha",StringComparison.OrdinalIgnoreCase) && LegacyAlpha.TryGetValue(new(version.Major,version.Minor,Math.Max(0,version.Build)),out var canonical)?canonical:new(version.Major,version.Minor,Math.Max(0,version.Build));
    internal static int Rank(string stage)=>stage.ToLowerInvariant() switch{"alpha"=>0,"beta"=>1,"stable"=>2,_=>-1};
    internal static bool Newer(Version candidate,string stage,Version installed,string installedStage)
    {
        var nextRank=Rank(stage);var oldRank=Rank(installedStage);
        var canonical=Canonicalize(candidate,stage);
        if(nextRank==0 && (canonical.Major!=0 || canonical.Minor!=0) || nextRank==1 && (candidate.Major!=0 || candidate.Minor!=1) || nextRank==2 && candidate.Major<1)return false;
        return nextRank>=0 && oldRank>=0 && (nextRank>oldRank || nextRank==oldRank && Canonicalize(candidate,stage)>Canonicalize(installed,installedStage));
    }
    private static readonly Regex Pattern=new(@"^(?:v|Nexa[- ])?(?<version>\d+\.\d+\.\d+)(?:[-. ](?<stage>alpha|beta|stable))?$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
    internal static NexaReleaseIdentity? Parse(string tag,bool prerelease)
    {
        var match=Pattern.Match(tag);if(!match.Success || !System.Version.TryParse(match.Groups["version"].Value,out var version))return null;
        var stage=match.Groups["stage"].Success?System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(match.Groups["stage"].Value.ToLowerInvariant()):prerelease?(version.Major==0 && version.Minor==1?"Beta":"Alpha"):"Stable";
        var canonical=Canonicalize(version,stage);
        if(stage=="Alpha" && (canonical.Major!=0 || canonical.Minor!=0) || stage=="Beta" && (version.Major!=0 || version.Minor!=1) || stage=="Stable" && version.Major<1)return null;
        return new(version,stage,tag);
    }
}
