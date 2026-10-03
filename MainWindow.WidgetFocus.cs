using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SessionDashboard;

public partial class MainWindow
{
    private Border? widgetFocusBackdrop;
    private Guid? focusedWidget;
    private void FocusWidget(Guid id)
    {
        ClearWidgetFocus();
        if (storyOpen || !widgetFrames.TryGetValue(id, out var frame)) return;
        widgetFocusBackdrop = new Border { Width = WidgetCanvas.ActualWidth, Height = WidgetCanvas.Height, Background = FocusBackdropBrush(), ToolTip = "Click to return to your dashboard" };
        widgetFocusBackdrop.MouseLeftButtonDown += (_, e) => { ClearWidgetFocus(); e.Handled = true; };
        WidgetCanvas.Children.Add(widgetFocusBackdrop); Panel.SetZIndex(widgetFocusBackdrop, 2000); Panel.SetZIndex(frame, 3000); focusedWidget = id;
        if (widgetResizeHandles.TryGetValue(id, out var handles)) foreach (var handle in handles) Panel.SetZIndex(handle.Handle, 3001);
    }
    private SolidColorBrush FocusBackdropBrush()
    {
        var brush = new SolidColorBrush(Colors.Black) { Opacity = .55 };
        if (state.FocusPulseEnabled) brush.BeginAnimation(System.Windows.Media.Brush.OpacityProperty, new DoubleAnimation(.55, Math.Max(.55, state.FocusPulseIntensity), TimeSpan.FromSeconds(2.5)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
        return brush;
    }
    private void ClearWidgetFocus()
    {
        if (widgetFocusBackdrop != null) { widgetFocusBackdrop.Background.BeginAnimation(System.Windows.Media.Brush.OpacityProperty, null); WidgetCanvas.Children.Remove(widgetFocusBackdrop); widgetFocusBackdrop = null; }
        if (focusedWidget is { } id && widgetFrames.TryGetValue(id, out var frame)) { Panel.SetZIndex(frame, 0); if (widgetResizeHandles.TryGetValue(id, out var handles)) foreach (var handle in handles) Panel.SetZIndex(handle.Handle, 2001); }
        focusedWidget = null;
    }
    internal static string DistractionMessage(decimal value) => value switch
    {
        >= 30 => "LOCK. IN. NOW.", >= 25 => "WHAT are you doing?", >= 20 => "FOCUS!",
        >= 15 => "Get rid of the distractions.", >= 10 => "Okay, focus up.", >= 5 => "Lock in.", _ => ""
    };
    private void AddDistractionMessage(StackPanel panel, WidgetInstance widget)
    {
        var label = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0), FontWeight = FontWeights.SemiBold, RenderTransform = new TranslateTransform() };
        panel.Children.Add(label); var previous = DistractionMessage(widget.Value);
        widgetTicks.Add(_ =>
        {
            var message = state.DistractionEscalationEnabled ? DistractionMessage(widget.Value) : "";
            label.Text = message; label.FontSize = widget.Value >= 20 ? 17 : 13;
            if (message != previous && state.DistractionEscalationEnabled && widget.Value >= 20 && message.Length > 0)
                ((TranslateTransform)label.RenderTransform).BeginAnimation(TranslateTransform.XProperty, new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(320), KeyFrames = { new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)), new LinearDoubleKeyFrame(-3, KeyTime.FromPercent(.2)), new LinearDoubleKeyFrame(3, KeyTime.FromPercent(.4)), new LinearDoubleKeyFrame(-2, KeyTime.FromPercent(.6)), new LinearDoubleKeyFrame(2, KeyTime.FromPercent(.8)), new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1)) } });
            if (!state.DistractionEscalationEnabled) ((TranslateTransform)label.RenderTransform).BeginAnimation(TranslateTransform.XProperty, null);
            previous = message;
        });
    }
}
