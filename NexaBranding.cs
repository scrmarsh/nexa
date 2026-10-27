using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
namespace SessionDashboard;
internal static class NexaBranding
{
    internal static string Tagline=>Theme.SeasonalEnabled?"Nexa — A space for whatever haunts you next.":"a space for whatever comes next.";
    internal static void ApplyTagline(TextBlock target,string text)
    {
        target.Inlines.Clear();var index=Theme.SeasonalEnabled?text.IndexOf("haunts",StringComparison.Ordinal):-1;
        if(index<0){target.Inlines.Add(new Run(text));return;}
        target.Inlines.Add(new Run(text[..index]));target.Inlines.Add(new Run("haunts"){Foreground=BugVisuals.B("#F97316")});target.Inlines.Add(new Run(text[(index+6)..]));
    }
}
