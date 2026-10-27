using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace SessionDashboard;
internal static class NexaMotion
{
    internal static bool Allowed(NexaOptions options) => SystemParameters.ClientAreaAnimation && !options.ReduceMotion;
    internal static void Pop(FrameworkElement element, NexaOptions options)
    {
        if (!Allowed(options) || !options.CelebrationEffects) return;
        var scale = new ScaleTransform(1, 1); element.RenderTransformOrigin = new Point(.2, .5); element.RenderTransform = scale;
        var pop = new DoubleAnimation(1.16, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop); scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }
}
