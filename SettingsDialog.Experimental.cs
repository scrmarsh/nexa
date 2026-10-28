using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildExperimental()
    {
        var page=Page("Experimental Features","Optional and unfinished capabilities. These do not affect ordinary local Spaces.");
        page.Children.Add(new TextBlock{Text="Account & Data",FontSize=20,FontWeight=FontWeights.SemiBold});
        page.Children.Add(new TextBlock{Text="Local Spaces, exports and backups work without an account. Cloud accounts and synchronization are not available.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,22)});
        page.Children.Add(new TextBlock{Text="Future Integrations",FontSize=20,FontWeight=FontWeights.SemiBold});
        foreach(var provider in new[]{"Google Tasks","Google Calendar","Outlook Calendar"})
            page.Children.Add(new TextBlock{Text=provider+" · Coming Soon",FontSize=16,Margin=new Thickness(0,12,0,8)});
        page.Children.Add(new TextBlock{Text="Previously configured provider credentials remain saved. No new connection is offered for unfinished providers.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,18,0,0)});
        var vault=new IntegrationVault(store);var saved=new StackPanel();
        foreach(var provider in new[]{"Google Tasks","Google Calendar","Outlook Calendar"}.Where(vault.Connected))
        {
            var row=new StackPanel{Margin=new Thickness(0,8,0,12)};row.Children.Add(new TextBlock{Text=provider+" · saved authorization, not verified"});
            var disconnect=new Button{Content="Disconnect "+provider,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,6,0,0)};
            disconnect.Click+=(_,_)=>{try{vault.Disconnect(provider);row.Visibility=Visibility.Collapsed;space?.RenderWidgets();}catch(System.IO.IOException){disconnect.ToolTip="Could not disconnect. Try again.";}};row.Children.Add(disconnect);saved.Children.Add(row);
        }
        if(saved.Children.Count>0)page.Children.Add(new Expander{Header="Manage saved provider connections",Content=saved,Margin=new Thickness(0,16,0,0)});
        return new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
