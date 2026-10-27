using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
namespace SessionDashboard;
internal static class TraditionalBugModes
{
    internal static string Label(BugDensity value)=>value switch{BugDensity.Low=>"Low",BugDensity.Normal=>"Medium",BugDensity.Infestation=>"High",BugDensity.Arcade=>"Arcade",_=>"Medium"};
}
// Nexa knows an executable contract, not the game menu, engine or save directory.
internal static class AevoriaLauncher
{
    internal static string Resolve(string? directory=null)
    {
        var root=directory??AppContext.BaseDirectory;
        var candidates=new[]{Path.Combine(root,"Aevoria","Aevoria.exe"),Path.Combine(root,"Aevoria.exe"),Path.GetFullPath(Path.Combine(root,"../../../Aevoria.Desktop/bin/Release/net10.0-windows/Aevoria.exe"))};
        return candidates.FirstOrDefault(File.Exists)??throw new FileNotFoundException("Aevoria is not installed beside Nexa. For this development checkout, build Aevoria.Desktop/Aevoria.Desktop.csproj in Release, then try again.");
    }
    internal static Process Launch()=>Process.Start(new ProcessStartInfo(Resolve()){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(Resolve())!})??throw new InvalidOperationException("Windows could not start Aevoria.");
}
internal static class AevoriaHomeButton
{
    internal static Button Create()
    {
        var gradient=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,1),GradientStops={new(Color.FromRgb(89,48,160),0),new(Color.FromRgb(48,62,162),.4),new(Color.FromRgb(12,105,121),.75),new(Color.FromRgb(104,77,139),1)}};
        var movement=new TranslateTransform();gradient.RelativeTransform=movement;
        var label=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};label.Children.Add(new TextBlock{Text="✦",FontSize=24,Foreground=Brushes.Gold,Margin=new Thickness(0,0,12,0)});label.Children.Add(new TextBlock{Text="Try Aevoria",FontSize=19,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});
        var button=new Button{Content=label,Width=280,Height=60,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,5,0,12),Background=gradient,Foreground=Brushes.White,BorderBrush=Brushes.MediumPurple,BorderThickness=new Thickness(1),RenderTransformOrigin=new Point(.5,.5),RenderTransform=new ScaleTransform(1,1),Effect=new DropShadowEffect{Color=Colors.MediumPurple,BlurRadius=20,ShadowDepth=0,Opacity=.35}};
        AutomationProperties.SetName(button,"Try Aevoria");AutomationProperties.SetHelpText(button,"Open Aevoria: The Living Ages, an independent game. Opens its main menu without loading a world.");
        var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(25));
        foreach(var pair in new[]{(Border.BackgroundProperty,"Background"),(Border.BorderBrushProperty,"BorderBrush"),(Border.BorderThicknessProperty,"BorderThickness")})border.SetBinding(pair.Item1,new Binding(pair.Item2){RelativeSource=RelativeSource.TemplatedParent});
        var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(content);
        var template=new ControlTemplate(typeof(Button)){VisualTree=border};var focus=new Trigger{Property=UIElement.IsKeyboardFocusedProperty,Value=true};focus.Setters.Add(new Setter(Control.BorderBrushProperty,Brushes.Gold));focus.Setters.Add(new Setter(Control.BorderThicknessProperty,new Thickness(2)));template.Triggers.Add(focus);var press=new Trigger{Property=Button.IsPressedProperty,Value=true};press.Setters.Add(new Setter(UIElement.OpacityProperty,.8));template.Triggers.Add(press);button.Template=template;
        bool Motion()=>SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast;
        void Animate(){movement.BeginAnimation(TranslateTransform.XProperty,null);if(button.IsVisible && Motion())movement.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-.12,.12,TimeSpan.FromSeconds(8)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});}
        void Scale(double target){var scale=(ScaleTransform)button.RenderTransform;scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);if(Motion()){scale.BeginAnimation(ScaleTransform.ScaleXProperty,new DoubleAnimation(target,TimeSpan.FromMilliseconds(150)));scale.BeginAnimation(ScaleTransform.ScaleYProperty,new DoubleAnimation(target,TimeSpan.FromMilliseconds(150)));}}
        void StopScale(){var scale=(ScaleTransform)button.RenderTransform;scale.BeginAnimation(ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(ScaleTransform.ScaleYProperty,null);}
        System.ComponentModel.PropertyChangedEventHandler changed=(_,e)=>{if(e.PropertyName is nameof(SystemParameters.ClientAreaAnimation) or nameof(SystemParameters.HighContrast)){Animate();StopScale();}};
        button.Loaded+=(_,_)=>{SystemParameters.StaticPropertyChanged+=changed;Animate();};button.Unloaded+=(_,_)=>{SystemParameters.StaticPropertyChanged-=changed;movement.BeginAnimation(TranslateTransform.XProperty,null);StopScale();};button.IsVisibleChanged+=(_,_)=>Animate();button.MouseEnter+=(_,_)=>Scale(1.035);button.MouseLeave+=(_,_)=>Scale(1);
        return button;
    }
}
