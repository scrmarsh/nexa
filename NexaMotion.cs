using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace SessionDashboard;
internal static class NexaMotion
{
    private sealed class AnimationState {internal int Generation;}
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<FrameworkElement,AnimationState> animations=new();
    internal static bool Allowed(NexaOptions options) => SystemParameters.ClientAreaAnimation && !options.ReduceMotion;
    internal static async Task Fade(FrameworkElement element,double opacity,NexaOptions options,int milliseconds=220)
    {
        var state=animations.GetOrCreateValue(element);var generation=++state.Generation;
        element.BeginAnimation(UIElement.OpacityProperty,null);
        if(!Allowed(options)){element.Opacity=opacity;return;}
        var animation=new DoubleAnimation(element.Opacity,opacity,TimeSpan.FromMilliseconds(milliseconds)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.Stop};
        element.BeginAnimation(UIElement.OpacityProperty,animation);
        await Task.Delay(milliseconds);await element.Dispatcher.InvokeAsync(()=>{if(state.Generation!=generation)return;element.BeginAnimation(UIElement.OpacityProperty,null);element.Opacity=opacity;});
    }
    internal static void Entrance(FrameworkElement element,NexaOptions options,int delay=0)
    {
        animations.GetOrCreateValue(element).Generation++;
        element.BeginAnimation(UIElement.OpacityProperty,null);
        if(!Allowed(options)){element.Opacity=1;return;}
        element.Opacity=1;
        element.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(280)){BeginTime=TimeSpan.FromMilliseconds(delay),FillBehavior=FillBehavior.Stop,EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}});
    }
    internal static void Stagger(IEnumerable<FrameworkElement> children,NexaOptions options)
    {var index=0;foreach(var child in children)Entrance(child,options,Math.Min(index++,6)*70);}
    internal static void Pop(FrameworkElement element, NexaOptions options)
    {
        if (!Allowed(options) || !options.CelebrationEffects) return;
        var scale = new ScaleTransform(1, 1); element.RenderTransformOrigin = new Point(.2, .5); element.RenderTransform = scale;
        var pop = new DoubleAnimation(1.16, 1, TimeSpan.FromMilliseconds(220)) { FillBehavior=FillBehavior.Stop, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop); scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }
}
