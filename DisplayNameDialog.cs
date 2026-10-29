using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace SessionDashboard;
internal sealed class DisplayNameDialog:NexaDialog
{
    internal TextBox NameInput {get;}
    internal DisplayNameDialog(Window owner,StateStore store,Action<string> saved)
    {
        Owner=owner;Title="nexa · Display name";Width=440;Height=340;MinWidth=300;MinHeight=220;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new StackPanel{Margin=new Thickness(26)};
        root.Children.Add(new TextBlock{Text="Display name",FontSize=22,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,18)});
        NameInput=new TextBox{Text=store.LoadProfile().DisplayName,MaxLength=80,FontSize=23,TextAlignment=TextAlignment.Center,Padding=new Thickness(12),VerticalContentAlignment=VerticalAlignment.Center};
        System.Windows.Automation.AutomationProperties.SetName(NameInput,"Display name");root.Children.Add(NameInput);
        var error=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,10)};root.Children.Add(error);
        var buttons=new Grid();buttons.ColumnDefinitions.Add(new());buttons.ColumnDefinitions.Add(new());
        var cancel=new Button{Content="Cancel",HorizontalAlignment=HorizontalAlignment.Left};cancel.Click+=(_,_)=>Close();buttons.Children.Add(cancel);
        var save=new Button{Content="Save",IsDefault=true,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetColumn(save,1);buttons.Children.Add(save);root.Children.Add(buttons);
        save.Click+=(_,_)=>
        {
            var value=NameInput.Text.Trim();if(value.Length==0){error.Text="Enter a display name.";NameInput.Focus();return;}
            try{var profile=store.LoadProfile();profile.DisplayName=value;store.SaveProfile(profile);saved(value);Close();}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){error.Text="Your name could not be saved. Try again.";}
        };
        Content=new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Hidden};
        Loaded+=(_,_)=>{NameInput.Focus();NameInput.SelectAll();};
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){Close();e.Handled=true;}};
    }
}
