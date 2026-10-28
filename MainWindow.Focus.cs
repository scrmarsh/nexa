using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SessionDashboard;

public partial class MainWindow
{
    internal void SaveFocusPreferences() { try { store.SaveInteractionPreferences(state); Changed(); SaveNow(); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Error("Could not save focus preferences: " + ex.Message); } }
    private Border? focusOverlay;
    private DateTimeOffset? awaySince;
    private DateTimeOffset lastNudge = DateTimeOffset.MinValue;
    private DateTimeOffset? lastRunningSessionEnd;

    private Rect FreeWidgetPlacement(WidgetInstance widget, double x, double y, double width, double height)
    {
        x = Math.Clamp(Math.Round(x / GridUnit) * GridUnit, 0, Math.Max(0, WidgetCanvas.ActualWidth-width));
        y = Math.Clamp(Math.Round(y / RowUnit) * RowUnit, 0, 720000);
        var candidate = new Rect(x,y,width,height);
        var others = widgetFrames.Where(p=>p.Key!=widget.Id).Select(p=>FrameRect(p.Value)).ToList();
        while (others.FirstOrDefault(r=>r.IntersectsWith(candidate)) is var hit && hit.Width>0)
            candidate.Y=Math.Ceiling((hit.Bottom+WidgetGap)/RowUnit)*RowUnit;
        return candidate;
    }
    private void CommitWidgetPlacement(WidgetInstance widget, double x, double y, double width, double height)
    {
        var candidate=FreeWidgetPlacement(widget,x,y,width,height);
        // Freeze existing visual positions on the first direct placement, avoiding row reflow.
        foreach(var other in state.Widgets.Where(w=>w.Id!=widget.Id && widgetFrames.ContainsKey(w.Id)))
        {
            var rect=FrameRect(widgetFrames[other.Id]); other.PlacementX=rect.X/Math.Max(1,WidgetCanvas.ActualWidth); other.PlacementY=rect.Y;
        }
        widget.Column = Math.Clamp((int)Math.Round(candidate.X / GridUnit), 0, 20 - widget.Span); widget.Row = Math.Clamp((int)Math.Round(candidate.Y / RowUnit), 0, 30000);
        widget.PlacementX=candidate.X/Math.Max(1,WidgetCanvas.ActualWidth); widget.PlacementY=candidate.Y;
        Changed(); SaveNow();
    }
    private void FocusResetClick(object sender, RoutedEventArgs e)
    {
        if (focusOverlay != null || storyOpen || Content is not Grid root) return;
        var message = new TextBlock { FontSize=30, TextAlignment=TextAlignment.Center, TextWrapping=TextWrapping.Wrap, Margin=new Thickness(24), Foreground=Brushes.White };
        var stack = new StackPanel { VerticalAlignment=VerticalAlignment.Center, HorizontalAlignment=HorizontalAlignment.Center, MaxWidth=620 };
        stack.Children.Add(message);
        var cancel = new Button { Content="Return to session", HorizontalAlignment=HorizontalAlignment.Center, Margin=new Thickness(0,20,0,0) }; stack.Children.Add(cancel);
        var overlay = new Border { Background=FocusBackdropBrush(), Child=stack };
        focusOverlay=overlay; root.Children.Add(overlay); Panel.SetZIndex(overlay,4000);
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        var guide=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(200) };
        var recapped=false;
        void Stop(object? s, EventArgs args) { guide.Stop(); elapsed.Stop(); overlay.Background.BeginAnimation(System.Windows.Media.Brush.OpacityProperty, null); root.Children.Remove(overlay); focusOverlay=null; Closed-=Stop; UpdateClock(); }
        void Advance()
        {
            var seconds=elapsed.Elapsed.TotalSeconds;
            if(seconds<40) { var breath=(int)(seconds/8)+1; message.Text=seconds%8<4 ? $"Breathe in gently \u00b7 {breath} of 5" : "Breathe out slowly. Relax your shoulders."; }
            else if(seconds<45) message.Text="Look away from the screen for a moment.";
            else if(!recapped)
            {
                recapped=true;
                var remaining=state.Session?.Remaining(DateTimeOffset.Now);
                var notes=new[] { state.Notes,HedyTranscriptBox.Text }.Concat(state.Widgets.Where(w=>w.Kind is "Notes" or "Hedy" or "Hedy Notes" or "Session timeline").Select(w=>w.Text));
                var last=notes.Where(n=>!string.IsNullOrWhiteSpace(n)).SelectMany(n=>n.Split('\n',StringSplitOptions.RemoveEmptyEntries)).LastOrDefault()?.Trim();
                if(last?.Length>180) last=last[..180]+"...";
                message.Text="You're back.\n"+(remaining.HasValue ? $"{Math.Ceiling(remaining.Value.TotalMinutes):0} minutes left.\n" : "")+(last==null ? state.Session?.Title ?? TitleInput.Text : "Last note:\n"+last);
                PlayInterfaceSound();
            }
            if(seconds>=51) Stop(null,EventArgs.Empty);
        }
        cancel.Click+=(_,_)=>Stop(null,EventArgs.Empty); Closed+=Stop; guide.Tick+=(_,_)=>Advance(); Advance(); guide.Start(); cancel.Focus();
    }
    private void UpdateFocusExperience(DateTimeOffset now)
    {
        if (state.Session is { } session)
        { if (session.IsRunning(now)) lastRunningSessionEnd = session.End; else if (session.StoppedAt == null && now >= session.End && lastRunningSessionEnd == session.End) { lastRunningSessionEnd = null; PlayInterfaceSound(); } }
        if (!IsActive) { awaySince ??= now; return; }
        var away = awaySince; awaySince = null;
        if (!state.FocusNudgesEnabled || away == null || now - away < TimeSpan.FromMinutes(3) || now - lastNudge < TimeSpan.FromMinutes(10) || state.Session?.IsRunning(now) != true || storyOpen || focusOverlay != null) return;
        var origin = state.Workspace == null ? "" : Continuity.Current(state.Workspace, store.UserId).PresetOrigin;
        if (origin is not ("Class" or "Study" or "Focus")) return;
        lastNudge = now;
        Feedback.Text = $"You still with us? \uD83D\uDC40 {Math.Ceiling(state.Session.Remaining(now).TotalMinutes):0} minutes left.";
    }

    internal void PlayInterfaceSound()
    {
        if (!state.InterfaceSoundsEnabled) return;
        InterfaceAudio.Preview(state.InterfaceSoundVolume);
    }
}

internal static class NexaIcons
{
    internal static UIElement Label(string action)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal }; var icon = Create(action); ((FrameworkElement)icon).Margin = new Thickness(0,0,7,0); panel.Children.Add(icon); panel.Children.Add(new TextBlock { Text = action, VerticalAlignment = VerticalAlignment.Center }); return panel;
    }
    internal static UIElement Create(string action)
    {
        var data = action switch
        {
            "Settings" => "M8,2 L12,2 12.8,4.2 14.7,5.3 17,4.8 19,8.2 17.4,10 17.4,12 19,13.8 17,17.2 14.7,16.7 12.8,17.8 12,20 8,20 7.2,17.8 5.3,16.7 3,17.2 1,13.8 2.6,12 2.6,10 1,8.2 3,4.8 5.3,5.3 7.2,4.2 Z M14,11 A4,4 0 1 0 6,11 A4,4 0 1 0 14,11",
            "Configure" => "M2,12 L6,12 M12,12 L18,12 M9,9 L9,15 M2,5 L12,5 M16,5 L18,5 M14,2 L14,8",
            "Duplicate" => "M7,7 L18,7 18,18 7,18 Z M3,13 L3,3 13,3",
            "Remove widget" => "M3,5 L17,5 M7,5 L7,2 13,2 13,5 M5,5 L6,18 14,18 15,5 M9,8 L9,15 M11,8 L11,15",
            "Home" => "M2,9 L10,2 18,9 M4,8 L4,18 8,18 8,12 12,12 12,18 16,18 16,8",
            "History" => "M3,7 A8,8 0 1 1 2,13 M3,2 L3,7 8,7 M10,6 L10,10 14,12",
            "Clock" => "M10,1 A9,9 0 1 1 10,19 A9,9 0 1 1 10,1 M10,4 L10,10 14,12",
            "Widgets" => "M1,1 L8,1 8,8 1,8 Z M12,1 L19,1 19,8 12,8 Z M1,12 L8,12 8,19 1,19 Z M12,12 L19,12 19,19 12,19 Z",
            "Productivity" => "M12,1 L3,11 9,11 7,19 17,8 11,8 Z",
            "Water" => "M10,1 C8,5 3,9 3,13 A7,7 0 0 0 17,13 C17,9 12,5 10,1 Z M6,13 Q6,16 9,16",
            "Task" => "M2,10 L7,15 18,3",
            "Notes" => "M3,1 L17,1 17,19 3,19 Z M6,5 L14,5 M6,9 L14,9 M6,13 L12,13",
            "Candy" => "M6,6 Q10,3 14,6 L19,3 18,10 19,17 14,14 Q10,17 6,14 L1,17 2,10 1,3 Z",
            "Bug" => "M6,8 Q10,2 14,8 L14,14 Q10,20 6,14 Z M10,5 L7,1 M10,5 L13,1 M6,9 L1,6 M6,12 L1,12 M6,15 L2,19 M14,9 L19,6 M14,12 L19,12 M14,15 L18,19",
            "Edit" => "M3,14 L13,4 17,8 7,18 3,18 Z M12,5 L16,9",
            "Add widget" => "M10,3 L10,17 M3,10 L17,10",
            _ => "M4,10 L16,10"
        };
        var path = new System.Windows.Shapes.Path { Data = Geometry.Parse(data), StrokeThickness = 1.5, Width = 16, Height = 16, Stretch = Stretch.Uniform, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
        if(action=="Productivity")path.Stroke=ProductivityRing.LightningBrush;else path.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty, "TextBrush"); return path;
    }
}
