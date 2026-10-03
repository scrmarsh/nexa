using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private void RefreshRecentSpaces()
    {
        var spaces = (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).ToArray();
        Recent.Children.Clear();
        var currentColumn = new StackPanel { Width = 288 };
        Recent.Children.Add(currentColumn);
        recentChevron.Visibility = spaces.Length > 4 ? Visibility.Visible : Visibility.Collapsed;
        recentChevron.Content = recentExpanded ? "<" : ">";
        recentChevron.ToolTip = recentExpanded ? "Hide older spaces" : "Show older spaces";
        System.Windows.Automation.AutomationProperties.SetName(recentChevron, recentExpanded ? "Collapse Recent Spaces" : "Expand Recent Spaces");
        for (var index = 0; index < (recentExpanded ? spaces.Length : Math.Min(4, spaces.Length)); index++)
        {
            if (index > 0 && index % 4 == 0)
            { currentColumn = new StackPanel { Width = 288 }; Recent.Children.Add(currentColumn); }
            var space = spaces[index];
            var content = new StackPanel();
            content.Children.Add(new TextBlock { Text = space.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var detail = new TextBlock { Text = $"{space.PresetOrigin} · {space.Current.LastAccessedAt.LocalDateTime:g}", FontSize = 12, Margin = new Thickness(0, 6, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis };
            detail.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); content.Children.Add(detail);
            var button = new Button { Tag = space.DashboardId, Content = content, Width = 280, HorizontalContentAlignment = HorizontalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 8, 8), ToolTip = space.Name };
            button.Click += (_, _) => OpenDashboard(space.DashboardId); currentColumn.Children.Add(button);
        }
        if (spaces.Length == 0) currentColumn.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap, MaxWidth = 280 });
        Recent.Children.Insert(1, recentChevron);
    }
}
