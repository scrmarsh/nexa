using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    internal bool SessionInterface {get;private set;}
    private Border? tipsTarget;
    private readonly List<CheckBox> seasonalSwitches=[];
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
        var page = active ? Page("Appearance", "Nexaween is using the seasonal palette. Your normal appearance is preserved.") : (StackPanel)BuildAppearance();
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 12) };
        row.Children.Add(new TextBlock { Text = "Nexaween", Foreground = (Brush)new BrushConverter().ConvertFromString("#84CC16")!, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0), FontWeight = FontWeights.SemiBold });
        row.Children.Add(MakeSeasonSwitch()); page.Children.Insert(1, row);
        if (active) { var seasonal = BuildSeasonalEvents(); page.Children.Add(seasonal); }
        return SeasonalBackdrop.Wrap(page,()=>ReliabilityOptions);
    }
    private CheckBox MakeSeasonSwitch()
    {
        var toggle=new CheckBox{IsChecked=NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow),VerticalAlignment=VerticalAlignment.Center,ToolTip="Turn Nexaween on or off",Margin=new Thickness(6)};
        toggle.Template=new ControlTemplate(typeof(CheckBox)){VisualTree=SwitchTemplate()};
        System.Windows.Automation.AutomationProperties.SetName(toggle,"Nexaween enabled");
        toggle.Click+=(_,_)=>SetNexaween(toggle.IsChecked==true);seasonalSwitches.Add(toggle);return toggle;
    }
    private static FrameworkElementFactory SwitchTemplate()
    {
        var grid=new FrameworkElementFactory(typeof(Grid));grid.SetValue(FrameworkElement.WidthProperty,54d);grid.SetValue(FrameworkElement.HeightProperty,30d);
        var track=new FrameworkElementFactory(typeof(Border));track.Name="Track";track.SetValue(Border.CornerRadiusProperty,new CornerRadius(15));track.SetValue(Border.BackgroundProperty,new SolidColorBrush(Color.FromRgb(153,27,27)));
        track.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("IsChecked"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent),Converter=new SeasonSwitchBrush()});grid.AppendChild(track);
        var knob=new FrameworkElementFactory(typeof(Border));knob.SetValue(FrameworkElement.WidthProperty,22d);knob.SetValue(FrameworkElement.HeightProperty,22d);knob.SetValue(Border.CornerRadiusProperty,new CornerRadius(11));knob.SetValue(Border.BackgroundProperty,Brushes.White);knob.SetValue(FrameworkElement.MarginProperty,new Thickness(4));knob.SetBinding(FrameworkElement.HorizontalAlignmentProperty,new System.Windows.Data.Binding("IsChecked"){RelativeSource=new(System.Windows.Data.RelativeSourceMode.TemplatedParent),Converter=new SeasonSwitchAlignment()});grid.AppendChild(knob);return grid;
    }
    private void SetNexaween(bool active)
    {
        var wasAppearance=pages.TryGetValue("Appearance",out var oldPage) && ReferenceEquals(host.Content,oldPage);
        var wasBugs=pages.TryGetValue("Add Bugs",out var oldBugs) && ReferenceEquals(host.Content,oldBugs);
        ReliabilityOptions.SeasonalMode=active?"On":"Off";
        foreach(var toggle in seasonalSwitches)toggle.IsChecked=active;
        Theme.SetSeasonalEnabled(active);space?.ApplyReliabilityOptions();NexaEcosystem.UpdateSeason(store,ReliabilityOptions);
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
        var enabled=new CheckBox{Content="Enable interface and timer completion sounds",IsChecked=state.InterfaceSoundsEnabled};page.Children.Add(enabled);
        var volume=new Slider{Minimum=0,Maximum=1,Value=state.InterfaceSoundVolume};Field(page,"Sound volume",volume);
        var preview = new Button { Content = "Test Sound", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 6) };
        preview.Click += (_, _) => InterfaceAudio.Preview(volume.Value); page.Children.Insert(page.Children.IndexOf(volume), preview);
        page.Children.Add(new TextBlock { Text = "Pop: selected interface interactions. Angelic chime: saved positive productivity events. Eerie tone: saved negative events when a penalty is configured. Automatic sounds respect the enable switch and volume.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 8) });
        var previews = new WrapPanel();
        foreach (var cue in new[] { ("Preview positive chime", "positive"), ("Preview negative tone", "negative") })
        { var button = new Button { Content = cue.Item1, Margin = new Thickness(0, 0, 8, 6) }; button.Click += (_, _) => InterfaceAudio.PreviewCue(volume.Value, cue.Item2); previews.Children.Add(button); }
        page.Children.Add(previews);
        var nudges=new CheckBox{Content="Playful focus nudges after 3 minutes away",IsChecked=state.FocusNudgesEnabled,Margin=new Thickness(0,12,0,12)};page.Children.Add(nudges);
        var seasonal=new CheckBox{Content="Seasonal sounds",IsChecked=ReliabilityOptions.SeasonalSounds};page.Children.Add(seasonal);
        void Save(){state.InterfaceSoundsEnabled=enabled.IsChecked==true;state.InterfaceSoundVolume=volume.Value;state.FocusNudgesEnabled=nudges.IsChecked==true;ReliabilityOptions.SeasonalSounds=seasonal.IsChecked==true;NexaEcosystem.UpdateSeason(store,ReliabilityOptions);QueueSave("sounds",()=>{SaveInteractionOptions();SaveState();});}
        enabled.Click+=(_,_)=>Save();volume.ValueChanged+=(_,_)=>Save();nudges.Click+=(_,_)=>Save();seasonal.Click+=(_,_)=>Save();return page;
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
            dc.DrawGeometry(null,BugVisuals.P("#4D7C0F",2),Geometry.Parse("M90,66 C45,62 98,42 76,38 C61,34 67,50 80,44 M95,65 C72,40 98,17 86,12"));
            dc.DrawEllipse(BugVisuals.B("#C2410C"),null,new(49,39),37,24);dc.DrawEllipse(BugVisuals.B("#F97316"),BugVisuals.P("#EA580C",1),new(36,38),19,24);dc.DrawEllipse(BugVisuals.B("#FB923C"),BugVisuals.P("#EA580C",1),new(58,38),18,25);
            dc.DrawGeometry(null,BugVisuals.P("#65A30D",5),Geometry.Parse("M49,16 Q43,5 56,8"));
            var text=new FormattedText("Nexaween",System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI Semibold"),11,BugVisuals.B("#661C102A"),1);dc.DrawText(text,new(23,34));
        }
        dc.Pop();
    }
}
