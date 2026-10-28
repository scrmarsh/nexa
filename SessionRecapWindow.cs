using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed class SessionRecapWindow:NexaDialog
{
    internal SessionRecapWindow(Window owner,string spaceName,string icon,DashboardSession session,NexaOptions options,Action? home=null)
    {
        Owner=owner;Title="nexa - Session recap";Width=790;Height=700;MinWidth=570;MinHeight=450;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(30)};
        var navigation=new Grid{Margin=new Thickness(0,18,0,0)};navigation.ColumnDefinitions.Add(new());navigation.ColumnDefinitions.Add(new());
        var back=new Button{Content=owner is MainWindow?"Return to Dashboard":"Return to History",HorizontalAlignment=HorizontalAlignment.Left,FontWeight=FontWeights.Bold};back.Click+=(_,_)=>CloseWithFade();navigation.Children.Add(back);
        if(home!=null){var goHome=new Button{Content=NexaIcons.Label("Home"),FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Right};goHome.Click+=(_,_)=>{Close();home();};Grid.SetColumn(goHome,1);navigation.Children.Add(goHome);}
        DockPanel.SetDock(navigation,Dock.Bottom);root.Children.Add(navigation);
        var body=new StackPanel();body.Children.Add(new TextBlock{Text=session.Recap!=null?"Session Complete!":"Session Report",FontSize=34,FontWeight=FontWeights.Bold,Margin=new Thickness(0,0,0,14)});
        var identity=new StackPanel{Orientation=Orientation.Horizontal};identity.Children.Add(SpaceIcons.Create(SpaceIcons.Resolve(icon,""),26));identity.Children.Add(new TextBlock{Text=spaceName,FontSize=22,Margin=new Thickness(12,0,0,12)});body.Children.Add(identity);
        if(session.Recap is {} recap)
        {
            var seconds=double.IsFinite(recap.ActualSeconds)?Math.Clamp(recap.ActualSeconds,0,3155760000):0;
            body.Children.Add(new TextBlock{Text=$"{TimeSpan.FromSeconds(seconds).TotalMinutes:0.#} minutes together",FontSize=22,Margin=new Thickness(0,4,0,8)});
            body.Children.Add(new TextBlock{Text=$"{NexaTime.Local(recap.StartedAt,options.TimeZoneId):g} - {NexaTime.Local(recap.EndedAt,options.TimeZoneId):t}",Margin=new Thickness(0,0,0,14)});
            var cards=new WrapPanel();
            foreach(var item in recap.Accomplishments.Where(p=>p.Value>0))
            {
                var card=new Border{CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Padding=new Thickness(16),Margin=new Thickness(0,0,10,10),Child=new TextBlock{Text=$"{item.Value:N0} {item.Key.ToLowerInvariant()}",FontSize=16}};
                card.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");cards.Children.Add(card);
            }
            body.Children.Add(cards);
            body.Children.Add(new TextBlock{Text=$"{recap.MeaningfulActions:N0} meaningful actions",Margin=new Thickness(0,10,0,10)});
            var change=recap.ScoreEnd-recap.ScoreStart;
            body.Children.Add(new TextBlock{Text=change>0?"Productivity increased!":change<0?"Productivity changed":"Your productivity",FontSize=20,FontWeight=FontWeights.SemiBold});
            body.Children.Add(new TextBlock{Text=$"{recap.ScoreStart:N0} to {recap.ScoreEnd:N0}  ({change:+0;-0;0} points this session)",FontSize=19,Margin=new Thickness(0,8,0,8)});
            var ring=new ProductivityRing();body.Children.Add(ring);ring.Update(recap.ScoreStart,new NexaOptions{ReduceMotion=true});
            Loaded+=(_,_)=>ring.Update(recap.ScoreEnd,options);
        }
        else body.Children.Add(new TextBlock{Text="This older report has no saved recap snapshot. Its original recorded notes and widget data remain available in History.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,16)});
        root.Children.Add(new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});Content=root;
        Loaded+=(_,_)=>NexaMotion.Stagger(body.Children.OfType<FrameworkElement>(),options);
    }
}
