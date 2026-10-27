using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed class LivingWorldDialog : NexaDialog
{
    internal LivingWorldDialog(Window owner,StateStore store)
    {
        Owner=owner;Title="Nexa Arcade";Width=740;Height=680;MinWidth=600;MinHeight=480;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var page=new StackPanel {Margin=new Thickness(24)};Content=new ScrollViewer {Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        page.Children.Add(new TextBlock {Text="ARCADE",FontSize=25,FontWeight=FontWeights.SemiBold});
        page.Children.Add(new TextBlock {Text="Two worlds. Your progress stays on this device.",Margin=new Thickness(0,5,0,16)});
        var modes=new WrapPanel();page.Children.Add(modes);
        Button Button(Panel parent,string label,Action action)
        {var button=new Button {Content=label,Padding=new Thickness(12,8,12,8),Margin=new Thickness(0,0,8,8)};button.Click+=(_,_)=> {try {action();}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Text.Json.JsonException) {MessageBox.Show(this,ex.Message,"Arcade",MessageBoxButton.OK,MessageBoxImage.Warning);}};parent.Children.Add(button);return button;}
        var form=new StackPanel();
        Button(modes,"COLONY WARS",()=> {LivingWorlds.Select(store,new());BattlefieldWindow.ShowFor(owner,store);Close();});
        Button(modes,"LIVING WORLDS",()=>form.Visibility=Visibility.Visible);
        page.Children.Add(form);form.Children.Add(new TextBlock {Text="LIVING WORLDS",FontSize=18,Margin=new Thickness(0,8,0,4)});
        form.Children.Add(new TextBlock {Text="Seeded oceans, islands and miniature landscapes. Create a separate world or load an existing one.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)});
        var list=new ComboBox {DisplayMemberPath="Name",Margin=new Thickness(0,0,0,10),MinHeight=32};form.Children.Add(list);
        var fields=new Grid();fields.ColumnDefinitions.Add(new());fields.ColumnDefinitions.Add(new(){Width=new GridLength(180)});form.Children.Add(fields);
        var entries=new StackPanel();fields.Children.Add(entries);
        var preview=new Image {Width=172,Height=172,Margin=new Thickness(8),ToolTip="World overview"};Grid.SetColumn(preview,1);fields.Children.Add(preview);
        TextBox Input(string label,string text)
        {entries.Children.Add(new TextBlock {Text=label,Margin=new Thickness(0,3,0,4)});var box=new TextBox {Text=text,Margin=new Thickness(0,0,8,8),MaxLength=label=="Seed" ? 100 : 80,Padding=new Thickness(6)};entries.Children.Add(box);return box;}
        var name=Input("World Name","New World");var seed=Input("Seed","");seed.ToolTip="Enter any seed text, or leave blank for a random seed.";
        entries.Children.Add(new TextBlock {Text="World size",Margin=new Thickness(0,3,0,4)});
        var size=new ComboBox {Margin=new Thickness(0,0,8,8)};
        foreach(var (w,h) in new[]{(256,256),(512,512),(640,640),(1024,576),(1536,864),(2048,1152)})size.Items.Add(new ComboBoxItem {Content=$"{w} x {h} tiles"+(w==h ? "" : " (16:9)"),Tag=(w,h)});
        size.SelectedIndex=3;entries.Children.Add(size);
        var civilizations=new CheckBox{Content="Age of Kingdoms civilizations (fictional mixed era)",IsChecked=true,Margin=new Thickness(0,4,0,8)};entries.Children.Add(civilizations);
        var status=new TextBlock {Text="Colony Wars saves are kept separate.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,10)};
        var commands=new WrapPanel();form.Children.Add(commands);form.Children.Add(status);
        LivingWorldSession? shown=null;
        void Preview() {if(shown!=null)preview.Source=shown.Chunks.Overview;}
        void Show(LivingWorldSession session)
        {
            if(shown!=null)shown.Chunks.Changed-=Preview;shown=session;session.Chunks.Changed+=Preview;Preview();
            status.Text=$"{session.Info.Name}  |  Seed: {session.Info.Seed}  |  {session.Info.Tiles} x {session.Info.HeightTiles} tiles | Geography v{session.Info.GeneratorVersion}";
        }
        void Reload(Guid? selected=null)
        {list.ItemsSource=LivingWorlds.List(store);list.SelectedItem=list.Items.OfType<LivingWorldInfo>().FirstOrDefault(w=>w.WorldId==selected)??list.Items.OfType<LivingWorldInfo>().FirstOrDefault();}
        Button(commands,"New World",()=> {name.Text="New World";seed.Clear();name.Focus();});
        Button(commands,"Civilization Atlas",()=>HistoricalAtlasWindow.ShowFor(this));
        var generate=Button(commands,"Generate",()=>{});
        generate.Click+=async (_,_)=>
        {
            generate.IsEnabled=false;status.Text="Preparing your world...";
            try {var (w,h)=((int,int))((ComboBoxItem)size.SelectedItem).Tag;var world=await LivingWorlds.Create(store,name.Text,seed.Text,w,h,civilizations:civilizations.IsChecked==true);Reload(world.Info.WorldId);Show(world);}
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Text.Json.JsonException) {status.Text=ex.Message;}
            finally {generate.IsEnabled=true;}
        };
        Button(commands,"Load World",()=> {if(list.SelectedItem is not LivingWorldInfo info)return;var world=LivingWorlds.Open(store,info.WorldId);LivingWorlds.Select(store,new(ArcadeGame.LivingWorlds,info.WorldId));Show(world);});
        Button(commands,"Open Battlefield",()=> {if(LivingWorlds.Current(store)==null) {status.Text="Generate or load a Living World first.";return;}BattlefieldWindow.ShowFor(owner,store);});
        Button(commands,"Return to COLONY WARS",()=> {LivingWorlds.Select(store,new());status.Text="COLONY WARS selected. Its saved world and settings are unchanged.";});
        Closed+=(_,_)=> {if(shown!=null)shown.Chunks.Changed-=Preview;};
        try {Reload();if(LivingWorlds.Current(store) is {} current)Show(current);}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) {status.Text=ex.Message;}
    }
}
