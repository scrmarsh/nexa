using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private void RefreshRecentSpaces()
    {
        var spaces = (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).ToArray();
        Recent.Children.Clear(); Recent.MaxWidth = recentExpanded ? double.PositiveInfinity : 600;
        recentMore.Visibility = spaces.Length > 4 ? Visibility.Visible : Visibility.Collapsed;
        recentMore.Content = recentExpanded ? "Less" : "More";
        System.Windows.Automation.AutomationProperties.SetName(recentMore, recentExpanded ? "Collapse Recent Spaces" : "More Recent Spaces");
        foreach (var space in recentExpanded ? spaces : spaces.Take(4))
        {
            var content = new StackPanel();
            content.Children.Add(new TextBlock { Text = space.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var detail = new TextBlock { Text = $"{space.PresetOrigin} · {space.Current.LastAccessedAt.LocalDateTime:g}", FontSize = 12, Margin = new Thickness(0, 6, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); content.Children.Add(detail);
            var button = new Button { Tag = space.DashboardId, Content = content, Width = 280, HorizontalContentAlignment = HorizontalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 8), ToolTip = space.Name };
            button.Click += (_, _) => OpenDashboard(space.DashboardId); Recent.Children.Add(button);
        }
        if (spaces.Length == 0) Recent.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap, MaxWidth = 280 });
    }
}
