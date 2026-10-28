namespace SessionDashboard;
internal sealed record TimeZoneChoice(string Id,string Label,string SearchText)
{
    public override string ToString()=>Label;
}
internal static class TimeZoneCatalog
{
    internal static IReadOnlyList<TimeZoneChoice> Choices {get;}=Create();
    private static IReadOnlyList<TimeZoneChoice> Create()
    {
        var result=new List<TimeZoneChoice>{new("Automatic","Use Windows time zone","automatic windows system local")};
        foreach(var zone in TimeZoneInfo.GetSystemTimeZones())
        {
            var iana=TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id,out var mapped)?mapped:zone.Id;
            var aliases=iana.Replace('_',' ').Replace('/',' ');
            result.Add(new(zone.Id,zone.DisplayName+" · "+iana,zone.Id+" "+zone.DisplayName+" "+zone.StandardName+" "+zone.DaylightName+" "+aliases));
        }
        return result;
    }
    internal static string Canonical(string id)=>TimeZoneInfo.TryConvertIanaIdToWindowsId(id,out var windows)?windows:id;
    internal static IEnumerable<TimeZoneChoice> Search(string query)=>Choices.Where(c=>query.Split(' ',StringSplitOptions.RemoveEmptyEntries).All(word=>c.SearchText.Contains(word,StringComparison.OrdinalIgnoreCase)));
}
