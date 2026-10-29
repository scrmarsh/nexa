using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal static class NexaChoices
{
    internal static string Show(Window owner,string title,string message,params string[] choices)
    {
        var result=choices.Last();var dialog=new NexaDialog{Owner=owner,Title=title,Width=510,Height=290,MinWidth=350,MinHeight=240,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize};
        var root=new DockPanel{Margin=new Thickness(22)};var buttons=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,20,0,0)};DockPanel.SetDock(buttons,Dock.Bottom);root.Children.Add(buttons);
        root.Children.Add(new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Content=new TextBlock{Text=message,TextWrapping=TextWrapping.Wrap,FontSize=15}});
        foreach(var choice in choices){var button=new Button{Content=choice,Margin=new Thickness(8,0,0,8),MinWidth=95,IsCancel=choice==choices.Last()};button.Click+=(_,_)=>{result=choice;dialog.DialogResult=choice!=choices.Last();};buttons.Children.Add(button);}dialog.Content=root;dialog.ShowDialog();return result;
    }
}
