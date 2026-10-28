using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class HomeWindow
{
    private int recentRows = 3;
    private void RefreshRecentSpaces()
    {
        var spaces = (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).ToArray();
        if(recentChevron.Parent is Panel previous)previous.Children.Remove(recentChevron);
        Recent.Children.Clear();
        var column = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 280 };
        Recent.Children.Add(column);
        var cards = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        column.Children.Add(cards);
        foreach (var space in spaces.Take(recentRows * 2))
        {
            var text = new StackPanel(); text.Children.Add(new TextBlock { Text = space.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var detail = new TextBlock { Text = $"{space.PresetOrigin} · {space.Current.LastAccessedAt.LocalDateTime:g}", FontSize = 12, Margin = new Thickness(0, 5, 0, 0) }; detail.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); text.Children.Add(detail);
            var button = new Button { Tag = space.DashboardId, Content = text, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(4, 0, 4, 8), ToolTip = space.Name };
            button.Click += (_, _) => OpenDashboard(space.DashboardId); cards.Children.Add(button);
        }
        if (spaces.Length == 0) column.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap });
        recentChevron.Content = "Show More Spaces"; recentChevron.ToolTip = "Show the next three rows of Spaces"; recentChevron.HorizontalAlignment = HorizontalAlignment.Center; recentChevron.Width = double.NaN;
        recentChevron.Visibility = spaces.Length > recentRows * 2 ? Visibility.Visible : Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(recentChevron, "Show More Spaces");
        column.Children.Add(recentChevron);
    }
}
