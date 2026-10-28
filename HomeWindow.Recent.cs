using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
namespace SessionDashboard;
internal sealed partial class HomeWindow
{
    private bool togglingRecent;
    private Border? recentViewport;
    private async void ToggleRecentSpaces()
    {
        if(togglingRecent)return;
        togglingRecent=true;recentChevron.IsEnabled=false;
        try
        {
            var options=Saved.Workspace?.Settings??new();
            if(recentRows==3)
            {
                var oldHeight=DescendantsOfRecent().FirstOrDefault()?.Parent is FrameworkElement oldGrid?oldGrid.ActualHeight:0;
                recentRows=int.MaxValue;RefreshRecentSpaces();
                if(NexaMotion.Allowed(options) && DescendantsOfRecent().FirstOrDefault()?.Parent is FrameworkElement grid)
                {
                    grid.UpdateLayout();var fullHeight=grid.ActualHeight;var duration=RecentDelay(Math.Max(0,DescendantsOfRecent().Count()-7))+360;
                    grid.Height=fullHeight;recentViewport!.Height=fullHeight;
                    recentViewport.BeginAnimation(HeightProperty,new DoubleAnimation(oldHeight,fullHeight,TimeSpan.FromMilliseconds(duration)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});
                    await Task.Delay(duration);await Dispatcher.InvokeAsync(()=>{recentViewport.BeginAnimation(HeightProperty,null);recentViewport.Height=double.NaN;grid.Height=double.NaN;});
                }
            }
            else
            {
                var extra=DescendantsOfRecent().Skip(6).Reverse().ToArray();
                await Task.WhenAll(extra.Select(async (card,index)=>{
                    await NexaMotion.CardTransition(card,false,options,RecentDelay(index));
                }));
                var shrinking=await Dispatcher.InvokeAsync(()=>
                {
                    if(!NexaMotion.Allowed(options) || recentViewport==null || DescendantsOfRecent().FirstOrDefault()?.Parent is not FrameworkElement grid)return false;
                    var full=grid.ActualHeight;var collapsed=full*3/Math.Ceiling(DescendantsOfRecent().Count()/2d);
                    grid.Height=full;recentViewport.Height=collapsed;
                    recentViewport.BeginAnimation(HeightProperty,new DoubleAnimation(full,collapsed,TimeSpan.FromMilliseconds(280)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.Stop});
                    return true;
                });
                if(shrinking)await Task.Delay(280);
                // Keep all row slots stable while the reverse sequence runs.
                // Removing individual children would shift every later UniformGrid cell.
                await Dispatcher.InvokeAsync(()=>{recentRows=3;RefreshRecentSpaces();});
            }
        }
        catch(TaskCanceledException){}
        finally{togglingRecent=false;if(!Dispatcher.HasShutdownStarted)await Dispatcher.InvokeAsync(()=>recentChevron.IsEnabled=true);}
    }
    internal static int RecentDelay(int index)=>Math.Min(2400,index<12?index*120:1440+(index-12)*12);

    private int recentRows = 3;
    private IEnumerable<Button> DescendantsOfRecent()=>recentViewport?.Child is System.Windows.Controls.Primitives.UniformGrid cards?cards.Children.OfType<Button>():[];
    private void RefreshRecentSpaces()
    {
        var spaces = (Saved.Workspace?.Dashboards ?? []).Where(d => d.PresetOrigin != "Developer mode").OrderByDescending(d => d.Current.LastAccessedAt).ToArray();
        var previousCards=DescendantsOfRecent().ToDictionary(b=>(Guid)b.Tag);
        if(recentViewport?.Child is System.Windows.Controls.Primitives.UniformGrid oldCards)oldCards.Children.Clear();
        if(recentChevron.Parent is Panel previous)previous.Children.Remove(recentChevron);
        Recent.Children.Clear();
        var column = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 280 };
        Recent.Children.Add(column);
        var cards = new System.Windows.Controls.Primitives.UniformGrid { Columns = 2 };
        recentViewport=new Border{Child=cards,ClipToBounds=true,VerticalAlignment=VerticalAlignment.Top};column.Children.Add(recentViewport);
        foreach (var space in spaces.Take(recentRows == 3 ? 6 : spaces.Length))
        {
            var text=new Grid{Height=78};
            text.RowDefinitions.Add(new());text.RowDefinitions.Add(new(){Height=GridLength.Auto});
            var group=new Grid{HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Center};
            group.ColumnDefinitions.Add(new(){Width=new GridLength(56)});group.ColumnDefinitions.Add(new());group.ColumnDefinitions.Add(new(){Width=new GridLength(56)});
            var name=new TextBlock{Text=space.Name,FontSize=19,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,MaxWidth=260};Grid.SetColumn(name,1);group.Children.Add(name);
            var icon=SpaceIcons.Create(SpaceIcons.Resolve(space.Current.Data.SpaceIcon,space.PresetOrigin),38);icon.Margin=new Thickness(0);icon.VerticalAlignment=VerticalAlignment.Center;group.Children.Add(icon);text.Children.Add(group);
            text.SizeChanged+=(_,_)=>name.MaxWidth=Math.Max(40,text.ActualWidth-112);
            var detail=new TextBlock{Text=NexaTime.Local(space.Current.LastAccessedAt,Saved.Workspace?.Settings.TimeZoneId??"Automatic").ToString("g"),FontSize=11,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,7,0,2)};detail.SetResourceReference(TextBlock.ForegroundProperty,"MutedBrush");Grid.SetRow(detail,1);text.Children.Add(detail);
            var button = previousCards.GetValueOrDefault(space.DashboardId) ?? new Button { Tag = space.DashboardId, Content = text, HorizontalContentAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(4, 0, 4, 8), ToolTip = space.Name };
            if(previousCards.ContainsKey(space.DashboardId))button.Content=text;
            if(!previousCards.ContainsKey(space.DashboardId))button.Click += (_, _) => OpenDashboard(space.DashboardId); cards.Children.Add(button);
        }
        if (spaces.Length == 0) column.Children.Add(new TextBlock { Text = "Your first Space starts here. Choose a template or a blank canvas.", TextWrapping = TextWrapping.Wrap });
        recentChevron.Content = recentRows == 3 ? "↓ Show More Spaces" : "↑ Hide More Spaces"; recentChevron.MinWidth=260;recentChevron.FontSize=17;recentChevron.FontWeight=FontWeights.Bold;recentChevron.ToolTip = "Expand or collapse additional Spaces"; recentChevron.HorizontalAlignment = HorizontalAlignment.Center; recentChevron.Width = double.NaN;
        recentChevron.Visibility = spaces.Length > 6 ? Visibility.Visible : Visibility.Collapsed;
        System.Windows.Automation.AutomationProperties.SetName(recentChevron, recentRows==3?"Show More Spaces":"Hide More Spaces");
        recentChevron.Margin=new Thickness(0,20,0,8);column.Children.Add(recentChevron);
        void Reveal(){var index=0;foreach(var card in cards.Children.OfType<Button>().Where(b=>!previousCards.ContainsKey((Guid)b.Tag)))_ = NexaMotion.CardTransition(card,true,Saved.Workspace?.Settings??new(),RecentDelay(index++));}
        if(IsLoaded)Reveal();else cards.Loaded+=(_,_)=>Reveal();
    }
}
