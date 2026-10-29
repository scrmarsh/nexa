using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    internal bool SessionInterface {get;private set;}
    private Border? tipsTarget;
    private readonly List<CheckBox> seasonalSwitches=[];
    private CheckBox? appearanceSeasonSwitch;
    internal UIElement BuildSettingsFooter(Button close)
    {
        var row = new Grid(); row.ColumnDefinitions.Add(new() { Width = new GridLength(140) }); row.ColumnDefinitions.Add(new()); row.ColumnDefinitions.Add(new() { Width = new GridLength(140) });
        row.Children.Add(close); AutoSaveStatus.HorizontalAlignment = HorizontalAlignment.Center; AutoSaveStatus.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(AutoSaveStatus, 1); row.Children.Add(AutoSaveStatus); return row;
    }
    private UIElement BuildAppearancePage()
    {
        seasonalSwitches.Clear();
        var active = NexaSeason.Active(ReliabilityOptions, DateTimeOffset.UtcNow);
        var page = (StackPanel)BuildAppearance();
        page.Margin=new Thickness(0,0,8,28);
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 12) };
        row.Children.Add(new TextBlock { Text = "Nexaween", Foreground = (Brush)new BrushConverter().ConvertFromString("#84CC16")!, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), FontWeight = FontWeights.SemiBold });
        row.Children.Add(MakeSeasonSwitch()); page.Children.Insert(1, row);
        page.Children.Add(BuildWallpaperControls(ReliabilityOptions.HomeWallpaper,true));
        if (active) { var seasonal = BuildSeasonalEvents(); page.Children.Add(seasonal); }
        var effects=(StackPanel)BuildEffects();foreach(var child in effects.Children.Cast<UIElement>().ToArray()){effects.Children.Remove(child);page.Children.Add(child);}
        page.Children.Add(BuildDisplaySettings());
        var scroll=new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,CanContentScroll=false,PanningMode=PanningMode.VerticalOnly,Tag="AppearanceScroll"};
        var layout=new DockPanel();var heading=new TextBlock{Text="Appearance",FontSize=26,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,14)};DockPanel.SetDock(heading,Dock.Top);layout.Children.Add(heading);layout.Children.Add(scroll);
        return SeasonalBackdrop.Wrap(layout,()=>ReliabilityOptions);
    }
    private CheckBox MakeSeasonSwitch()
    {
        if(appearanceSeasonSwitch is {} existing)
        {
            if(existing.Parent is Panel parent)parent.Children.Remove(existing);
            existing.IsChecked=NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow);seasonalSwitches.Add(existing);return existing;
        }
        var toggle=new CheckBox{IsChecked=NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow),VerticalAlignment=VerticalAlignment.Center,ToolTip="Turn Nexaween on or off",Margin=new Thickness(6)};
        toggle.Template=new ControlTemplate(typeof(CheckBox)){VisualTree=SwitchTemplate()};
        var initialized=false;
        void Visual(bool animate)
        {
            toggle.ApplyTemplate();if(toggle.Template.FindName("Knob",toggle) is not Border knob || toggle.Template.FindName("Track",toggle) is not Border track)return;
            var active=toggle.IsChecked==true;var duration=TimeSpan.FromMilliseconds(animate && NexaMotion.Allowed(ReliabilityOptions)?220:0);
            var translation=(TranslateTransform)knob.RenderTransform;if(translation.IsFrozen){translation=translation.CloneCurrentValue();knob.RenderTransform=translation;}var from=translation.X;translation.BeginAnimation(TranslateTransform.XProperty,null);translation.X=active?24:0;
            translation.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(from,translation.X,duration){FillBehavior=FillBehavior.Stop,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut}});
            var brush=new SolidColorBrush(active?Color.FromRgb(101,163,13):Color.FromRgb(153,27,27));var previous=(track.Background as SolidColorBrush)?.Color??brush.Color;track.Background=brush;
            brush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(previous,brush.Color,duration){FillBehavior=FillBehavior.Stop});
        }
        toggle.Loaded+=(_,_)=>{if(!initialized){Visual(false);initialized=true;}};
        toggle.Checked+=(_,_)=>Visual(true);toggle.Unchecked+=(_,_)=>Visual(true);
        System.Windows.Automation.AutomationProperties.SetName(toggle,"Nexaween enabled");
        toggle.Click+=(_,_)=>SetNexaween(toggle.IsChecked==true);appearanceSeasonSwitch=toggle;seasonalSwitches.Add(toggle);return toggle;
    }
    private static FrameworkElementFactory SwitchTemplate()
    {
        var grid=new FrameworkElementFactory(typeof(Grid));grid.SetValue(FrameworkElement.WidthProperty,54d);grid.SetValue(FrameworkElement.HeightProperty,30d);
        var track=new FrameworkElementFactory(typeof(Border));track.Name="Track";track.SetValue(Border.CornerRadiusProperty,new CornerRadius(15));track.SetValue(Border.BackgroundProperty,new SolidColorBrush(Color.FromRgb(153,27,27)));
        grid.AppendChild(track);
        var knob=new FrameworkElementFactory(typeof(Border));knob.Name="Knob";knob.SetValue(FrameworkElement.WidthProperty,22d);knob.SetValue(FrameworkElement.HeightProperty,22d);knob.SetValue(Border.CornerRadiusProperty,new CornerRadius(11));knob.SetValue(Border.BackgroundProperty,Brushes.White);knob.SetValue(FrameworkElement.MarginProperty,new Thickness(4));knob.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Left);knob.SetValue(UIElement.RenderTransformProperty,new TranslateTransform());grid.AppendChild(knob);return grid;
    }
    private void SetNexaween(bool active)
    {
        if(!FlushPendingChanges())return;
        var wasAppearance=pages.TryGetValue("Appearance",out var oldPage) && ReferenceEquals(host.Content,oldPage);
        var wasBugs=pages.TryGetValue("Add Bugs",out var oldBugs) && ReferenceEquals(host.Content,oldBugs);
        ReliabilityOptions.SeasonalMode=active?"On":"Off";
        foreach(var toggle in seasonalSwitches)toggle.IsChecked=active;
        Theme.BeginTransition(ReliabilityOptions);Theme.SetSeasonalEnabled(active);space?.ApplyReliabilityOptions();NexaEcosystem.UpdateSeason(store,ReliabilityOptions);
        if(tabs.TryGetValue("Appearance",out var tab))tab.Opacity=1;
        if(pages.ContainsKey("Appearance"))pages["Appearance"]=BuildAppearancePage();
        if(wasAppearance)host.Content=pages["Appearance"];
        if(pages.ContainsKey("Add Bugs")){pages["Add Bugs"]=BuildBugs();if(wasBugs)host.Content=pages["Add Bugs"];}
        if(SessionInterface){pages["Session"]=BuildSession();host.Content=pages["Session"];sessionStatusPage=pages["Session"];}
        QueueSave("nexaween",SaveState);
    }
    private void SelectSeasonalPanel()
    {if(!FlushPendingChanges())return;host.Content=BuildSeasonalEvents();}
    private Border BuildTipsSetting()
    {
        var tips=new CheckBox{Content="Enable Tips",IsChecked=ReliabilityOptions.ShowQuickTips,Margin=new Thickness(12)};
        tips.Click+=(_,_)=>{ReliabilityOptions.ShowQuickTips=tips.IsChecked==true;space?.ApplyReliabilityOptions();QueueSave("tips",SaveState);};
        tipsTarget=new Border{Child=tips,CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(2),Margin=new Thickness(0,22,0,0)};return tipsTarget;
    }
    private void FocusTips()
    {
        Dispatcher.BeginInvoke(new Action(()=>{tipsTarget?.BringIntoView();if(tipsTarget==null)return;tipsTarget.BorderBrush=Brushes.Gold;if(tipsTarget.Child is Control input)input.Focus();var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};timer.Tick+=(_,_)=>{timer.Stop();if(tipsTarget!=null)tipsTarget.BorderBrush=Brushes.Transparent;};timer.Start();Closed+=(_,_)=>timer.Stop();}),DispatcherPriority.Loaded);
    }
    private UIElement BuildSounds()
    {
        var page=Page("Sounds","Interface, timer and seasonal sounds. Sounds remain available while Nexaween is enabled.");store.LoadInteractionPreferences(state);
        var enabled=new CheckBox{Content="Enable interface, timer and game sounds",IsChecked=state.InterfaceSoundsEnabled};page.Children.Add(enabled);
        var volume=new Slider{Minimum=0,Maximum=1,Value=state.InterfaceSoundVolume};Field(page,"Sound volume",volume);
        var preview = new Button { Content = "Test Sound", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 6) };
        preview.Click += (_, _) => InterfaceAudio.PreviewCue(volume.Value,"positive",state.PositiveFeedbackSound);
        var styles=new ComboBox{ItemsSource=new[]{"Classic","Bell garden","Soft tap","Glass","Soft"},SelectedIndex=-1};
        Field(page,"Sound style preset (optional)",styles);page.Children.Add(preview);
        var positive=new ComboBox{ItemsSource=InterfaceAudio.PositiveChoices,SelectedItem=state.PositiveFeedbackSound};
        var negative=new ComboBox{ItemsSource=InterfaceAudio.NegativeChoices,SelectedItem=state.NegativeFeedbackSound};
        Field(page,"Positive feedback",positive);var positivePreview=new Button{Content="Preview positive",HorizontalAlignment=HorizontalAlignment.Left};positivePreview.Click+=(_,_)=>InterfaceAudio.PreviewCue(volume.Value,"positive",positive.SelectedItem?.ToString());page.Children.Add(positivePreview);
        Field(page,"Negative feedback",negative);var negativePreview=new Button{Content="Preview negative",HorizontalAlignment=HorizontalAlignment.Left};negativePreview.Click+=(_,_)=>InterfaceAudio.PreviewCue(volume.Value,"negative",negative.SelectedItem?.ToString());page.Children.Add(negativePreview);
        page.Children.Add(new TextBlock{Text="Positive: saved productive actions. Negative: saved configured penalties. Interface pop: selected controls. Playback is bounded to one short sound at a time.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,8)});
        var nudges=new CheckBox{Content="Playful focus nudges after 3 minutes away",IsChecked=state.FocusNudgesEnabled,Margin=new Thickness(0,12,0,12)};page.Children.Add(nudges);
        var gameSounds=new CheckBox{Content="Pixel Timer game effects (shooting, power-ups, death)",IsChecked=state.GameSoundsEnabled};page.Children.Add(gameSounds);
        var bugSounds=new CheckBox{Content="Quiet dashboard bug crawling",IsChecked=state.BugSoundsEnabled};page.Children.Add(bugSounds);
        var effectChoice=new ComboBox{ItemsSource=NexaEffectsAudio.Names,SelectedIndex=4};Field(page,"Game and nexaween sound previews",effectChoice);
        var effectPreview=new Button{Content="Preview effect",HorizontalAlignment=HorizontalAlignment.Left};effectPreview.Click+=(_,_)=>NexaEffectsAudio.Play((NexaSoundEffect)Math.Clamp(effectChoice.SelectedIndex,0,8),volume.Value,true);page.Children.Add(effectPreview);
        var seasonal=new CheckBox{Content="Seasonal sounds",IsChecked=ReliabilityOptions.SeasonalSounds};page.Children.Add(seasonal);
        void Save(){state.PositiveFeedbackSound=positive.SelectedItem?.ToString()??"Chime";state.NegativeFeedbackSound=negative.SelectedItem?.ToString()??"Eerie";state.InterfaceSoundsEnabled=enabled.IsChecked==true;state.GameSoundsEnabled=gameSounds.IsChecked==true;state.BugSoundsEnabled=bugSounds.IsChecked==true;state.InterfaceSoundVolume=volume.Value;state.FocusNudgesEnabled=nudges.IsChecked==true;ReliabilityOptions.SeasonalSounds=seasonal.IsChecked==true;NexaEcosystem.UpdateSeason(store,ReliabilityOptions);QueueSave("sounds",()=>{SaveInteractionOptions();SaveState();});}
        var applyingPreset=false;
        var applyPreset=new Button{Content="Apply preset to both feedback sounds",HorizontalAlignment=HorizontalAlignment.Left};
        applyPreset.Click+=(_,_)=>{if(styles.SelectedIndex<0)return;applyingPreset=true;try{positive.SelectedIndex=styles.SelectedIndex;negative.SelectedIndex=styles.SelectedIndex;}finally{applyingPreset=false;}Save();};page.Children.Insert(page.Children.IndexOf(preview)+1,applyPreset);
        positive.SelectionChanged+=(_,_)=>{if(!applyingPreset)Save();};negative.SelectionChanged+=(_,_)=>{if(!applyingPreset)Save();};
        gameSounds.Click+=(_,_)=>Save();bugSounds.Click+=(_,_)=>Save();enabled.Click+=(_,_)=>Save();volume.ValueChanged+=(_,_)=>Save();nudges.Click+=(_,_)=>Save();seasonal.Click+=(_,_)=>Save();return new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
internal sealed class SeasonSwitchBrush:System.Windows.Data.IValueConverter
{public object Convert(object value,Type target,object parameter,System.Globalization.CultureInfo culture)=>value is true?BugVisuals.B("#65A30D"):BugVisuals.B("#991B1B");public object ConvertBack(object value,Type target,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();}
internal sealed class SeasonSwitchAlignment:System.Windows.Data.IValueConverter
{public object Convert(object value,Type target,object parameter,System.Globalization.CultureInfo culture)=>value is true?HorizontalAlignment.Right:HorizontalAlignment.Left;public object ConvertBack(object value,Type target,object parameter,System.Globalization.CultureInfo culture)=>throw new NotSupportedException();}
internal sealed class NexaSeasonArt(bool ghost):FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth/100,ActualHeight/70));
        if(ghost)
        {
            foreach(var x in new[]{22d,62d}){var shape=Geometry.Parse($"M{x},55 L{x},28 C{x},8 {x+26},8 {x+26},28 L{x+26},55 L{x+20},50 L{x+13},56 L{x+6},50 Z");dc.DrawGeometry(BugVisuals.B("#E9D5FF"),null,shape);dc.DrawEllipse(BugVisuals.B("#1C102A"),null,new(x+8,28),2,3);dc.DrawEllipse(BugVisuals.B("#1C102A"),null,new(x+18,28),2,3);}
        }
        else
        {
            dc.PushTransform(new TranslateTransform(18,2));dc.DrawDrawing(NexaweenDecorations.Asset(1));dc.Pop();
        }
        dc.Pop();
    }
}
