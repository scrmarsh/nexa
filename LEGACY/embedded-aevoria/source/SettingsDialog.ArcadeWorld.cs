using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private void BuildArcadeWorldControls(Panel page)
    {
        page.Children.Add(new TextBlock {Text="Your shared Arcade world",FontSize=16,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,16,0,7)});
        page.Children.Add(new TextBlock {Text="Open Battlefield resumes classic Arcade / Colony Wars without dashboard widgets. Both classic views share progress. Classic Rounds is temporary.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)});
        var row=new WrapPanel();page.Children.Add(row);var status=new TextBlock {TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,8)};page.Children.Add(status);
        void Command(string label,Action<ArcadeWorldController> action)
        {
            var b=new Button {Content=label,Padding=new Thickness(9,6,9,6),Margin=new Thickness(0,0,7,7)};row.Children.Add(b);
            b.Click+=(_,_)=>
            {
                if(!FlushPendingChanges())return;
                try {var world=ArcadeWorlds.For(store);action(world);status.Text=world.Status??"Arcade world saved on this device.";}
                catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException) {status.Text=ex.Message;}
            };
        }
        Command("Open Battlefield",_=>BattlefieldWindow.ShowFor(this,store));
        Command("Pause world",w=>w.SetPaused(true));Command("Resume world",w=>w.SetPaused(false));
        Command("Restore backup",w=>ArcadeWorldCommands.RestoreBackup(this,w));Command("Reset Arcade World",w=>ArcadeWorldCommands.Reset(this,w));
    }
}
