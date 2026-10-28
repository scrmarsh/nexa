using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed class NexaWordmark:StackPanel
{
    internal TextBlock Version {get;}
    internal NexaWordmark(double width)
    {
        Width=width;HorizontalAlignment=HorizontalAlignment.Left;
        Children.Add(new NexaLogo{Width=width-10,Height=(width-10)*92/290,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(-10,0,0,0)});
        Version=new TextBlock{Text=App.VersionLabel,FontSize=13,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,8,10,12)};
        Version.SetResourceReference(TextBlock.ForegroundProperty,"MutedBrush");Children.Add(Version);
    }
}
