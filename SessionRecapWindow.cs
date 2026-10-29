using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed class SessionRecapWindow:NexaDialog
{
    // Read only: use the saved recap snapshot; viewing never finalizes or awards anything.
    internal static UIElement Compact(string spaceName,DashboardSession session,NexaOptions options)
    {
        var body=new StackPanel{Margin=new Thickness(16)};
        body.Children.Add(new TextBlock{Text=spaceName,FontSize=22,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
        if(session.Title!=spaceName)body.Children.Add(new TextBlock{Text=session.Title,FontSize=15,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,5,0,0)});
        if(session.Recap is {} recap)
        {
            var seconds=double.IsFinite(recap.ActualSeconds)?Math.Clamp(recap.ActualSeconds,0,3155760000):0;
            body.Children.Add(new TextBlock{Text=$"{NexaTime.Local(recap.StartedAt,options.TimeZoneId):g} – {NexaTime.Local(recap.EndedAt,options.TimeZoneId):g}\n{TimeSpan.FromSeconds(seconds).TotalMinutes:0.#} minutes together · {recap.MeaningfulActions:N0} meaningful actions",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)});
            var cards=new WrapPanel();
            foreach(var item in recap.Accomplishments.Where(p=>p.Value>0))cards.Children.Add(Accomplishment(item.Key,item.Value,true));
            body.Children.Add(cards);
            body.Children.Add(new TextBlock{Text=$"Productivity {recap.ScoreStart:N0} → {recap.ScoreEnd:N0} ({recap.ScoreEnd-recap.ScoreStart:+0;-0;0} points)",FontSize=16,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap});
        }
        else body.Children.Add(new TextBlock{Text=$"{NexaTime.Local(session.OpenedAt,session.TimeZoneId):g}\nNo saved recap snapshot for this older session. Complete recorded details follow below.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)});
        var frame=new Border{Child=body,CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1)};frame.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");return frame;
    }
    private static Border Accomplishment(string name,int value,bool compact=false)
    {
        var illustration=name switch{"Tasks completed"=>"Task","Notes captured"=>"Notes","Water bottles recorded"=>"Water","Candy collected"=>"Candy","Bugs squashed"=>"Bug",_=>"Clock"};
        var detail=new StackPanel{Orientation=Orientation.Horizontal};var art=(FrameworkElement)NexaIcons.Create(illustration);art.Width=art.Height=compact?20:26;art.Margin=new Thickness(0,0,10,0);detail.Children.Add(art);
        detail.Children.Add(new TextBlock{Text=$"{value:N0} {name.ToLowerInvariant()}",FontSize=compact?13:16,VerticalAlignment=VerticalAlignment.Center});
        var card=new Border{CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Padding=new Thickness(compact?9:16),Margin=new Thickness(0,0,10,10),Child=detail};card.SetResourceReference(Border.BorderBrushProperty,"AccentBrush");return card;
    }
    internal SessionRecapWindow(Window owner,string spaceName,string icon,DashboardSession session,NexaOptions options,Action? home=null)
    {
        Owner=owner;Title="nexa - Session recap";Width=790;Height=700;MinWidth=570;MinHeight=450;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var root=new DockPanel{Margin=new Thickness(30)};
        var navigation=new Grid{Margin=new Thickness(0,18,0,0)};navigation.ColumnDefinitions.Add(new());navigation.ColumnDefinitions.Add(new());
        var back=new Button{Content=owner is MainWindow?"← Return to Dashboard":"Return to History",HorizontalAlignment=HorizontalAlignment.Left,FontWeight=FontWeights.Bold};back.Click+=(_,_)=>CloseWithFade();navigation.Children.Add(back);
        if(home!=null){var goHome=new Button{Content="⌂ Return to Home",FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Right};goHome.Click+=(_,_)=>{Close();home();};Grid.SetColumn(goHome,1);navigation.Children.Add(goHome);}
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
                cards.Children.Add(Accomplishment(item.Key,item.Value));
            }
            body.Children.Add(cards);Loaded+=(_,_)=>{var index=0;foreach(var card in cards.Children.OfType<FrameworkElement>())_ = NexaMotion.CardTransition(card,true,options,index++*120);};
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
