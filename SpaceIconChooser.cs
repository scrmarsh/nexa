using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
namespace SessionDashboard;
internal sealed class SpaceIconChooser:NexaPanel
{
    internal string Selected {get;private set;}
    internal SpaceIconChooser(Window owner,string selected,Func<string,bool> save)
    {
        Owner=owner;Title="nexa · Space Icon";Width=650;Height=520;MinWidth=440;MinHeight=380;WindowStartupLocation=WindowStartupLocation.CenterOwner;Selected=selected;
        var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Choose a Space icon",FontSize=22,Margin=new Thickness(0,0,0,16)});
        var preview=new ContentControl{Content=SpaceIcons.Create(selected,48),Margin=new Thickness(0,0,0,16)};panel.Children.Add(preview);
        var choices=new UniformGrid{Columns=4};var buttons=new Dictionary<string,Button>();
        foreach(var name in SpaceIcons.Paths.Keys)
        {
            var content=new StackPanel();content.Children.Add(SpaceIcons.Create(name,34));content.Children.Add(new TextBlock{Text=name,FontSize=11,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap});
            var button=new Button{Content=content,Tag=name,Margin=new Thickness(4),MinHeight=72,ToolTip=name};System.Windows.Automation.AutomationProperties.SetName(button,name);
            button.Click+=(_,_)=>{Selected=name;preview.Content=SpaceIcons.Create(name,48);};choices.Children.Add(button);buttons[name]=button;
        }
        // An absent implicit Button style is valid; use its resource key rather than a string.
        void SelectStyles(){foreach(var item in buttons)if(item.Key==Selected)item.Value.SetResourceReference(StyleProperty,"PrimaryButton");else item.Value.ClearValue(StyleProperty);}
        foreach(var button in buttons.Values)button.Click+=(_,_)=>SelectStyles();SelectStyles();
        panel.Children.Add(choices);var actions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,18,0,0)};
        var apply=new Button{Content="Save icon",IsDefault=true,Margin=new Thickness(0,0,10,0)};apply.Click+=(_,_)=>{if(save(Selected))Close();};
        var cancel=new Button{Content="Cancel",IsCancel=true};cancel.Click+=(_,_)=>Close();actions.Children.Add(apply);actions.Children.Add(cancel);panel.Children.Add(actions);
        Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
    }
}
