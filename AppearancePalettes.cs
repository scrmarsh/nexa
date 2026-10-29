namespace SessionDashboard;

internal static class AppearancePalettes
{
    // Exact original session palette from 5fba224:Theme.cs and its four-entry
    // MainWindow.xaml selector. Reuse these colors through SolidColor backgrounds;
    // they are not claimed to be a recovered historical wallpaper-preset table.
    internal sealed record Preset(string Name,string Hex);
    internal static readonly Preset[] Backgrounds =
    [new("nexa blue","#69ACFF"),new("nexa violet","#C6A5FF"),new("nexa amber","#FFCA78"),new("nexa green","#6AE58F")];
    internal static string Name(AppTheme theme)=>theme switch
    {AppTheme.Nexa=>"nexa",AppTheme.Violet=>"Violet",AppTheme.Amber=>"nexa amber",AppTheme.Green=>"nexa green",AppTheme.Aurora=>"Aurora",AppTheme.Ember=>"Ember",_=>theme.ToString()};
}
