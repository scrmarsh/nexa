using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildSession()
    {
        var page=Page("Session","Configure the active Space, timer schedule, wallpaper and reset behavior.");
        if(space!=null)page.Children.Add(space.BuildTimerSettings(()=>QueueSave("timer-draft",SaveState)));
        var history=new Button{Content="Notes & History",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        history.Click+=(_,_)=>{if(!FlushPendingChanges())return;Close();space?.ShowNotesHistory();};page.Children.Add(history);
        var save=new Button{Content="Save Now",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};save.Click+=(_,_)=>{space?.SaveNow();};page.Children.Add(save);
        var saving=(ScrollViewer)BuildSavingHistory();var content=(UIElement)saving.Content;saving.Content=null;page.Children.Add(content);
        var cap=new Slider{Minimum=1.5,Maximum=10,Value=ReliabilityOptions.EffectiveStreakCap,TickFrequency=.5,IsSnapToTickEnabled=true};Field(page,"Maximum squash streak multiplier",cap);
        var capText=new TextBlock{Text=cap.Value.ToString("0.0")+"×"};page.Children.Add(capText);cap.ValueChanged+=(_,_)=>{ReliabilityOptions.StreakMultiplierCap=cap.Value;state.Productivity.FrenzyMultiplier=Math.Min(state.Productivity.FrenzyMultiplier,cap.Value);state.Productivity.BugFrenzyCap=cap.Value;capText.Text=cap.Value.ToString("0.0")+"×";QueueSave("streak-limit",SaveState);};
        var distraction=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.DistractionPenalty};Field(page,"Points deducted per new distraction (0 disables)",distraction);
        var alcohol=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.AlcoholPenalty};Field(page,"Points deducted per logged alcoholic drink (0 disables)",alcohol);
        distraction.SelectionChanged+=(_,_)=>{ReliabilityOptions.DistractionPenalty=(int)(distraction.SelectedItem??0);QueueSave("penalties",SaveState);};alcohol.SelectionChanged+=(_,_)=>{ReliabilityOptions.AlcoholPenalty=(int)(alcohol.SelectedItem??0);QueueSave("penalties",SaveState);};
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};page.Children.Add(status);
        var spaceAppearance=new StackPanel();page.Children.Add(spaceAppearance);
        spaceAppearance.Children.Add(new Border { Height = 1, Background = Brush("LineBrush"), Margin = new Thickness(0, 22, 0, 12) });
        spaceAppearance.Children.Add(new TextBlock { Text = "Space appearance", FontSize = 20, FontWeight = FontWeights.SemiBold });
        if(space!=null)
        {
            var iconRow=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,12)};
            var previewIcon=new ContentControl{Content=SpaceIcons.Create(SpaceIcons.Resolve(state.SpaceIcon,Continuity.Current(state.Workspace!,store.UserId).PresetOrigin),36),Margin=new Thickness(0,0,14,0)};
            var changeIcon=new Button{Content="Change Icon",ToolTip="Space Icon",VerticalAlignment=VerticalAlignment.Center};
            System.Windows.Automation.AutomationProperties.SetName(changeIcon,"Change Space Icon");
            changeIcon.Click+=(_,_)=>space.EditSpaceIcon();
            Activated+=(_,_)=>previewIcon.Content=SpaceIcons.Create(SpaceIcons.Resolve(state.SpaceIcon,Continuity.Current(state.Workspace!,store.UserId).PresetOrigin),36);
            spaceAppearance.Children.Add(new TextBlock{Text="Space Icon"});iconRow.Children.Add(previewIcon);iconRow.Children.Add(changeIcon);spaceAppearance.Children.Add(iconRow);
        }
        var mainColor = new ColorPickerButton { SelectedColor = state.AccentColor }; Field(spaceAppearance, "Space main color", mainColor);
        var accentColor = new ColorPickerButton { SelectedColor = state.PixelColor }; Field(spaceAppearance, "Space accent / pixel color", accentColor);
        mainColor.ColorChanged+=(_,_)=>QueueSave("space-colors",()=>{state.AccentColor=mainColor.SelectedColor;state.PixelColor=accentColor.SelectedColor;SaveState();});
        accentColor.ColorChanged+=(_,_)=>QueueSave("space-colors",()=>{state.AccentColor=mainColor.SelectedColor;state.PixelColor=accentColor.SelectedColor;SaveState();});
        page.Children.Add(BuildWallpaperControls(state.Background,false));
        page.Children.Add(BuildTipsSetting());
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
