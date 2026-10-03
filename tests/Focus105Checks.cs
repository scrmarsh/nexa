using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckFocus105()
    {
        CheckContinuityModels(); CheckSwarmModels();
        foreach (var name in new[] { "Class", "Study", "Focus", "Church", "Meeting" })
        {
            var template = DashboardTemplates.All.Single(t=>t.Name==name); var data = DashboardTemplates.Build(template);
            Assert(data.Widgets.Count<=4 && data.Widgets.Count(w=>w.Kind=="Pixel timer")==1, name+" has one session banner and calm defaults");
            WidgetLibrary.Validate(data);
            var presetStore = new StateStore(Path.Combine(output,name+".json"));
            presetStore.Save(JsonSerializer.Deserialize<AppState>(JsonSerializer.Serialize(data))!);
            var presetWindow = new MainWindow(presetStore); presetWindow.Show(); Pump(TimeSpan.FromMilliseconds(120));
            Assert(presetWindow.PixelBannerHost.Content==presetWindow.PixelPanel && presetWindow.PixelPanel.ActualWidth >= presetWindow.WidgetCanvas.ActualWidth-2,name+" displays full-width banner");
            Assert(presetWindow.WidgetCanvas.Children.OfType<Border>().Count(b=>b.Tag is Guid)==data.Widgets.Count-1,name+" has no duplicate timer card");
            if(name=="Class") RenderPreview((FrameworkElement)presetWindow.Content,"focus105-class");
            presetWindow.Close();
        }
        var store = new StateStore(Path.Combine(output,"focus105.json"));
        var original = new AppState { WidgetLayoutInitialized=true, Notes="Decision trees split based on information gain.", Session = new Session { Title="Class", Start=DateTimeOffset.Now.AddMinutes(-5), End=DateTimeOffset.Now.AddMinutes(24), IntervalMinutes=5 }, Widgets=[WidgetLibrary.Create("Pixel timer","pixels"),WidgetLibrary.Create("Notes","notes"),WidgetLibrary.Create("Clock")] };
        WidgetLibrary.Arrange(original.Widgets,false); store.Save(original);
        var window=new MainWindow(store); window.Show(); window.Activate(); Pump(TimeSpan.FromMilliseconds(250));
        Border Frame(WidgetInstance w)=>window.WidgetCanvas.Children.OfType<Border>().Single(b=>Equals(b.Tag,w.Id));
        Rect RectFor(WidgetInstance w) { var frame=Frame(w); return new Rect(Canvas.GetLeft(frame),Canvas.GetTop(frame),frame.Width,frame.Height); }
        var notes=window.state.Widgets.Single(w=>w.Kind=="Notes"); var clock=window.state.Widgets.Single(w=>w.Kind=="Clock");
        var neighbor=RectFor(clock); var unit=(window.WidgetCanvas.ActualWidth+8)/20;
        var grip=Descendants(Frame(notes)).OfType<Thumb>().Single();
        grip.RaiseEvent(new DragStartedEventArgs(0,0));
        for(var i=1;i<=12;i++) grip.RaiseEvent(new DragDeltaEventArgs(unit*i/12,72*i/12));
        Assert(RectFor(clock)==neighbor,"Slow drag does not move neighbor during preview");
        grip.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(100));
        Assert(RectFor(clock)==neighbor,"Drop keeps neighboring widget in place");
        Assert(notes.PlacementX!=null && Math.Abs(notes.PlacementY%24)<.01,"Drop stores grid-aligned placement");
        Assert(!RectFor(notes).IntersectsWith(RectFor(clock)),"Move resolves overlap without displacing neighbor");
        var placed=RectFor(notes);
        grip.RaiseEvent(new DragStartedEventArgs(0,0)); grip.RaiseEvent(new DragDeltaEventArgs(800,300)); grip.RaiseEvent(new DragCompletedEventArgs(0,0,true));
        Assert(RectFor(notes)==placed,"Canceled fast drag leaves layout intact");
        var resize=window.WidgetCanvas.Children.OfType<Thumb>().Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Resize "+notes.Title);
        resize.RaiseEvent(new DragStartedEventArgs(0,0));
        for(var i=1;i<=10;i++) resize.RaiseEvent(new DragDeltaEventArgs(unit*i/10,48*i/10));
        resize.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(100));
        Assert(notes.CustomSize && notes.Height>=WidgetLibrary.Definition(notes.Kind).MinHeight && notes.Height%24==0,"Resize saves useful grid dimensions");
        Assert(RectFor(clock)==neighbor && !RectFor(notes).IntersectsWith(RectFor(clock)),"Resize avoids overlap and preserves neighbors");
        grip.RaiseEvent(new DragStartedEventArgs(0,0)); grip.RaiseEvent(new DragDeltaEventArgs(-unit*3,240)); grip.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(80));
        Assert(!RectFor(notes).IntersectsWith(RectFor(clock)),"Drag after resize remains disjoint");
        grip.RaiseEvent(new DragStartedEventArgs(0,0)); grip.RaiseEvent(new DragDeltaEventArgs(unit*5,120)); grip.RaiseEvent(new DragCompletedEventArgs(0,0,false)); Pump(TimeSpan.FromMilliseconds(80));
        Assert(!RectFor(notes).IntersectsWith(RectFor(clock)) && RectFor(clock)==neighbor,"Fast multi-cell drop keeps neighbors stationary and avoids overlap");
        var savedNotes=JsonSerializer.Serialize(notes); var savedWidgets=JsonSerializer.Serialize(window.state.Widgets); var session=window.state.Session;
        window.StoryButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump(TimeSpan.FromMilliseconds(150));
        Assert(window.swarmGame!=null && window.SwarmGameZone.Visibility==Visibility.Visible,"Pixel Swarm launches from banner into whole window");
        window.UpdateClock(DateTimeOffset.Now.AddMinutes(1));
        Assert(window.state.Session==session && window.Countdown.Text.StartsWith("00:22"),"Game uses advancing session timing");
        window.ExitBoredomMode();
        Assert(JsonSerializer.Serialize(window.state.Widgets)==savedWidgets && window.state.Notes==original.Notes,"Game return preserves widget data and notes");
        Assert(window.PixelPanel.Parent==window.PixelBannerHost,"Returning from game restores banner");
        var reset=typeof(MainWindow).GetMethod("FocusResetClick",BindingFlags.NonPublic|BindingFlags.Instance)!;
        reset.Invoke(window,[window,new RoutedEventArgs()]); Pump(TimeSpan.FromMilliseconds(80));
        Assert(Descendants((FrameworkElement)window.Content).OfType<TextBlock>().Any(t=>t.Text.StartsWith("Breathe in gently")),"Focus Reset begins breath guide");
        Pump(TimeSpan.FromSeconds(45.5));
        Assert(Descendants((FrameworkElement)window.Content).OfType<TextBlock>().Any(t=>t.Text.Contains("You're back.") && t.Text.Contains("Decision trees")),"Five breaths and look-away prompt finish with local notes recap");
        Click(window,"Return to session"); Pump(TimeSpan.FromMilliseconds(100));
        Assert(JsonSerializer.Serialize(window.state.Widgets)==savedWidgets && window.state.Notes==original.Notes,"Focus Reset returns without changing session data");
        window.state.InterfaceSoundsEnabled=true; window.state.InterfaceSoundVolume=.1; window.PlayInterfaceSound();
        var audio=(MemoryStream)typeof(MainWindow).GetField("interfaceAudio",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
        Assert(audio.Length==8864,"Optional sound produces short original PCM chime");
        var awayField=typeof(MainWindow).GetField("awaySince",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var nudgeField=typeof(MainWindow).GetField("lastNudge",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Continuity.Current(window.state.Workspace!,store.UserId).PresetOrigin="Class";
        window.Activate(); Pump(TimeSpan.FromMilliseconds(100)); var nudgeNow=DateTimeOffset.Now;
        window.state.FocusNudgesEnabled=false; awayField.SetValue(window,nudgeNow.AddMinutes(-4)); window.UpdateClock(nudgeNow);
        Assert((DateTimeOffset)nudgeField.GetValue(window)! ==DateTimeOffset.MinValue,"Disabled nudges stay silent");
        window.state.FocusNudgesEnabled=true; awayField.SetValue(window,nudgeNow.AddMinutes(-4)); window.UpdateClock(nudgeNow);
        Assert((DateTimeOffset)nudgeField.GetValue(window)! ==nudgeNow,"Eligible return shows one nudge");
        awayField.SetValue(window,nudgeNow.AddMinutes(-4)); window.UpdateClock(nudgeNow.AddMinutes(1));
        Assert((DateTimeOffset)nudgeField.GetValue(window)! ==nudgeNow,"Nudge cooldown prevents repeat reminders");
        window.SaveFocusPreferences(); window.Close();
        var reopened=new MainWindow(store); reopened.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Assert(JsonSerializer.Serialize(reopened.state.Widgets.Single(w=>w.Id==notes.Id))==savedNotes,"Moved and resized widget reloads unchanged");
        Assert(reopened.state.Notes==original.Notes && reopened.state.FocusNudgesEnabled && reopened.state.InterfaceSoundsEnabled,"Notes and focus preferences survive reopen");
        var otherState=new DashboardData(); store.LoadInteractionPreferences(otherState);
        Assert(otherState.InterfaceSoundsEnabled && otherState.FocusNudgesEnabled && Math.Abs(otherState.InterfaceSoundVolume-.1)<.001,"Master interaction preferences apply to another Space");
        reopened.Close();
    }
}
