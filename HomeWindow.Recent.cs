using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class HomeWindow
{
    private bool togglingRecent;
    private async void ToggleRecentSpaces()
    {
        if(togglingRecent)return;
        if(recentRows==3 || !NexaMotion.Allowed(Saved.Workspace?.Settings??new())){recentRows=recentRows==3?int.MaxValue:3;RefreshRecentSpaces();return;}
        togglingRecent=true;recentChevron.IsEnabled=false;
        try
        {
            await Task.WhenAll(DescendantsOfRecent().Skip(6).Select(card=>NexaMotion.Fade(card,0,Saved.Workspace?.Settings??new(),180)));
            await Dispatcher.InvokeAsync(()=>{if(IsVisible){recentRows=3;RefreshRecentSpaces();}});
        }
        catch(TaskCanceledException){}
        finally{togglingRecent=false;if(!Dispatcher.HasShutdownStarted)_ = Dispatcher.BeginInvoke(new Action(()=>recentChevron.IsEnabled=true));}
    }
    private int recentRows = 3;
    private IEnumerable<Button> DescendantsOfRecent()=>Recent.Children.OfType<StackPanel>().SelectMany(c=>c.Children.OfType<System.Windows.Controls.Primitives.UniformGrid>()).SelectMany(c=>c.Children.OfType<Button>());
    private void RefreshRecentSpaces()
    {
        var spaces = (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).ToArray();
        var previousCards=DescendantsOfRecent().ToDictionary(b=>(Guid)b.Tag);
        foreach(var grid in Recent.Children.OfType<StackPanel>().SelectMany(c=>c.Children.OfType<System.Windows.Controls.Primitives.UniformGrid>()))grid.Children.Clear();
        if(recentChevron.Parent is Panel previous)previous.Children.Remove(recentChevron);
        Recent.Children.Clear();
        var column = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 280 };
        Recent.Children.Add(column);
        var cards = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        column.Children.Add(cards);
        foreach (var space in spaces.Take(recentRows == 3 ? 6 : spaces.Length))
        {
            var text = new StackPanel(); text.Children.Add(SpaceIcons.Create(SpaceIcons.Resolve(space.Current.Data.SpaceIcon,space.PresetOrigin),24)); text.Children.Add(new TextBlock { Text = space.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var detail = new TextBlock { Text = $"{space.PresetOrigin} · {space.Current.LastAccessedAt.LocalDateTime:g}", FontSize = 12, Margin = new Thickness(0, 5, 0, 0) }; detail.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush"); text.Children.Add(detail);
            var button = previousCards.GetValueOrDefault(space.DashboardId) ?? new Button { Tag = space.DashboardId, Content = text, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(4, 0, 4, 8), ToolTip = space.Name };
            if(!previousCards.ContainsKey(space.DashboardId))button.Click += (_, _) => OpenDashboard(space.DashboardId); cards.Children.Add(button);
        }
        if (spaces.Length == 0) column.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap });
        recentChevron.Content = recentRows == 3 ? "↓ Show More Spaces" : "↑ Hide More Spaces"; recentChevron.MinWidth=260;recentChevron.FontSize=17;recentChevron.FontWeight=FontWeights.Bold;recentChevron.ToolTip = "Expand or collapse additional Spaces"; recentChevron.HorizontalAlignment = HorizontalAlignment.Center; recentChevron.Width = double.NaN;
        recentChevron.Visibility = spaces.Length > 6 ? Visibility.Visible : Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(recentChevron, "Show More Spaces");
        column.Children.Add(recentChevron);
        void Reveal()=>NexaMotion.Stagger(cards.Children.OfType<Button>().Where(b=>!previousCards.ContainsKey((Guid)b.Tag)),Saved.Workspace?.Settings??new());
        if(IsLoaded)Reveal();else cards.Loaded+=(_,_)=>Reveal();
    }
}
