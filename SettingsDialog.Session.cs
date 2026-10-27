using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildSession()
    {
        var page=Page("Session","Schedule, save and review the current dashboard. Recurring days are edited only in Pixel Timer.");
        var weekly=new Button{Content="Edit Pixel Timer schedule",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        weekly.Click+=(_,_)=>{if(!FlushPendingChanges())return;Close();space?.ShowWeeklySchedule();};page.Children.Add(weekly);
        var history=new Button{Content="Notes & History",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        history.Click+=(_,_)=>{if(!FlushPendingChanges())return;Close();space?.ShowNotesHistory();};page.Children.Add(history);
        var save=new Button{Content="Save Now",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};save.Click+=(_,_)=>{space?.SaveNow();};page.Children.Add(save);
        var saving=(ScrollViewer)BuildSavingHistory();var content=(UIElement)saving.Content;saving.Content=null;page.Children.Add(content);
        var cap=new Slider{Minimum=1.5,Maximum=10,Value=ReliabilityOptions.EffectiveStreakCap,TickFrequency=.5,IsSnapToTickEnabled=true};Field(page,"Maximum squash streak multiplier",cap);
        var capText=new TextBlock{Text=cap.Value.ToString("0.0")+"×"};page.Children.Add(capText);cap.ValueChanged+=(_,_)=>{ReliabilityOptions.StreakMultiplierCap=cap.Value;state.Productivity.FrenzyMultiplier=Math.Min(state.Productivity.FrenzyMultiplier,cap.Value);capText.Text=cap.Value.ToString("0.0")+"×";QueueSave("streak-limit",SaveState);};
        var distraction=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.DistractionPenalty};Field(page,"Points deducted per new distraction (0 disables)",distraction);
        var alcohol=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.AlcoholPenalty};Field(page,"Points deducted per logged alcoholic drink (0 disables)",alcohol);
        distraction.SelectionChanged+=(_,_)=>{ReliabilityOptions.DistractionPenalty=(int)(distraction.SelectedItem??0);QueueSave("penalties",SaveState);};alcohol.SelectionChanged+=(_,_)=>{ReliabilityOptions.AlcoholPenalty=(int)(alcohol.SelectedItem??0);QueueSave("penalties",SaveState);};
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};page.Children.Add(status);
        var spaceAppearance=new StackPanel{IsEnabled=!NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)};page.Children.Add(spaceAppearance);
        spaceAppearance.Children.Add(new Border { Height = 1, Background = Brush("LineBrush"), Margin = new Thickness(0, 22, 0, 12) });
        spaceAppearance.Children.Add(new TextBlock { Text = "Space appearance", FontSize = 20, FontWeight = FontWeights.SemiBold });
        var mainColor = new ColorPickerButton { SelectedColor = state.AccentColor }; Field(spaceAppearance, "Space main color", mainColor);
        var accentColor = new ColorPickerButton { SelectedColor = state.PixelColor }; Field(spaceAppearance, "Space accent / pixel color", accentColor);
        var background = Continuity.Clone(state.Background); var mode = new ComboBox { ItemsSource = Enum.GetValues<SpaceBackgroundMode>(), SelectedItem = background.Mode, MinHeight = 40 }; Field(spaceAppearance, "Background mode", mode);
        var color = new ColorPickerButton { SelectedColor = InputRules.IsColor(background.SolidColor) ? background.SolidColor : "#080B16" }; Field(spaceAppearance, "Solid color", color);
        var imagePath = new TextBox { Text = background.ImagePath, IsReadOnly = true, MinHeight = 40 }; var browse = new Button { Content = "Choose image", Margin = new Thickness(0, 8, 0, 0) };
        Field(spaceAppearance, "Custom image", imagePath); spaceAppearance.Children.Add(browse);
        var blur = new CheckBox { Content = "Blur background", IsChecked = background.Blur, Margin = new Thickness(0, 14, 0, 0) }; spaceAppearance.Children.Add(blur);
        var blurStrength = new Slider { Minimum = 0, Maximum = 40, Value = background.BlurStrength, TickFrequency = 2, IsSnapToTickEnabled = true }; Field(spaceAppearance, "Blur strength", blurStrength);
        var overlay = new Slider { Minimum = 0, Maximum = .9, Value = background.Overlay, TickFrequency = .05, IsSnapToTickEnabled = true }; Field(spaceAppearance, "Overlay / dimming", overlay);
        void SaveBackground() => QueueSave("background", () =>
        {
            state.AccentColor = mainColor.SelectedColor; state.PixelColor = accentColor.SelectedColor;
            state.Background = new SpaceBackground { Mode = (SpaceBackgroundMode)(mode.SelectedItem ?? SpaceBackgroundMode.Default), SolidColor = color.SelectedColor, ImagePath = imagePath.Text, Blur = blur.IsChecked == true, BlurStrength = blurStrength.Value, Overlay = overlay.Value };
            SaveState(); status.Text = "Space appearance saved.";
        });
        browse.Click += (_, _) => { var picker = new OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp" }; if (picker.ShowDialog(this) == true) { imagePath.Text = picker.FileName; mode.SelectedItem = SpaceBackgroundMode.CustomImage; SaveBackground(); } };
        mainColor.ColorChanged += (_, _) => SaveBackground(); accentColor.ColorChanged += (_, _) => SaveBackground(); color.ColorChanged += (_, _) => SaveBackground();
        mode.SelectionChanged += (_, _) => SaveBackground(); blur.Click += (_, _) => SaveBackground(); blurStrength.ValueChanged += (_, _) => SaveBackground(); overlay.ValueChanged += (_, _) => SaveBackground();
        page.Children.Add(BuildTipsSetting());
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
