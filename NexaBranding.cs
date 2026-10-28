using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
namespace SessionDashboard;
internal static class NexaBranding
{
    internal static string Tagline=>Theme.SeasonalEnabled?"a space for whatever haunts you next":"a space for whatever comes next.";
    internal static void ApplyTagline(TextBlock target,string text)
    {
        target.Inlines.Clear();
        const string seasonal = "a space for whatever haunts you next";
        var index = seasonal.IndexOf("haunts", StringComparison.Ordinal);
        if (!Theme.SeasonalEnabled || text.Length <= index || !seasonal.StartsWith(text, StringComparison.Ordinal)) { target.Inlines.Add(new Run(text)); return; }
        var end = Math.Min(text.Length, index + "haunts".Length);
        target.Inlines.Add(new Run(text[..index]));
        target.Inlines.Add(new Run(text[index..end]) { Foreground = BugVisuals.B("#F97316") });
        if (text.Length > end) target.Inlines.Add(new Run(text[end..]));
    }
}
