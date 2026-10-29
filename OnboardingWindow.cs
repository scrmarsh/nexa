using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SessionDashboard;

internal sealed class OnboardingWindow : NexaDialog
{
    private readonly StateStore store;
    internal readonly UserProfile Profile;
    internal DashboardRecord? Result;
    internal int Step {get;private set;}
    private readonly StackPanel body=new();
    private readonly TextBlock title=new(){FontSize=28,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)};
    private readonly TextBlock error=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.IndianRed};
    private readonly Button back=new(){Content="Back",Margin=new Thickness(0,0,12,0)};
    private readonly Button next=new(){Content="Continue"};
    private readonly ThemeMode originalMode=Theme.CurrentMode;
    private readonly AppTheme originalTheme=Theme.CurrentTheme;
    private readonly bool originalSeason=Theme.SeasonalEnabled;
    private readonly HashSet<string> tools;
    private string name="My Space",spaceIcon="";
    private bool finishing,seasonAsked;
    internal NexaOptions Options {get;}
    internal static readonly string[] FeaturedWidgets=["Pixel timer","Clock","Focus Reset","Task List","Notes","Hedy Notes","Water Bottle Counter"];
    internal OnboardingWindow(StateStore store,UserProfile? initialProfile=null)
    {
        this.store=store;Profile=Continuity.Clone(initialProfile??store.LoadProfile());
        Options=File.Exists(store.FilePath)?Continuity.Clone(store.Load().Workspace?.Settings??new()):new NexaOptions{SeasonalMode="Off"};
        if(!GuidedSetup.Types.Contains(Profile.PreferredDashboard))Profile.PreferredDashboard="Class";
        name=Profile.PreferredDashboard+" space";
        if(!Profile.OnboardingCompleted)Options.SeasonalMode="Off";
        var available=WidgetLibrary.Definitions.Select(d=>d.Name).ToHashSet();
        tools=(Profile.PreferredWidgets.Count>0?Profile.PreferredWidgets:GuidedSetup.Recommendations(Profile.PreferredDashboard).ToList()).Where(available.Contains).ToHashSet();
        Title="Welcome to Nexa";Width=790;Height=720;MinWidth=620;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        NexaWindowBounds.RequestInitialPlacement(this,true);
        var root=new DockPanel{Margin=new Thickness(24)};
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};
        actions.Children.Add(back);actions.Children.Add(next);DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);
        var content=new StackPanel();content.Children.Add(title);content.Children.Add(body);content.Children.Add(error);
        root.Children.Add(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});Content=root;
        back.Click+=(_,_)=>{if(Step>0){Step--;Render();}};next.Click+=(_,_)=>Advance();
        Closed+=(_,_)=>{if(DialogResult!=true){Theme.SetSeasonalEnabled(originalSeason);Theme.Apply(Application.Current.Resources,originalMode,originalTheme);}};
        if(!Profile.OnboardingCompleted)Preview();Render();
    }
    private void Text(string value)=>body.Children.Add(new TextBlock{Text=value,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)});
    private void Preview(){Theme.SetSeasonalEnabled(NexaSeason.Active(Options,DateTimeOffset.UtcNow));Theme.Apply(Application.Current.Resources,Profile.ThemeMode,Profile.Theme);}
    internal void ChooseSeason(bool enabled){Options.SeasonalMode=enabled?"On":"Off";if(enabled)Profile.ThemeMode=SessionDashboard.ThemeMode.Dark;Preview();Render();}
    internal void Advance()
    {
        if(Step<4){Step++;Render();return;}
        if(finishing)return;
        if(string.IsNullOrWhiteSpace(name)){error.Text="Name your Space before continuing.";return;}
        finishing=true;
        try{Profile.PreferredWidgets=tools.ToList();Profile.OnboardingCompleted=true;Result=GuidedSetup.Create(Profile,name,tools,store.UserId);Result.Current.Data.OfferQuickStart=true;Result.Current.Data.SpaceIcon=SpaceIcons.Resolve(spaceIcon,Profile.PreferredDashboard);DialogResult=true;}
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or ArgumentException){finishing=false;error.Text="Could not finish setup. Your existing Spaces are safe. "+ex.Message;}
    }
    private void OfferSeason()
    {
        if(seasonAsked||Step!=4||!IsVisible)return;seasonAsked=true;
        var prompt=new NexaDialog{Owner=this,Title="Nexaween",Width=440,Height=230,MinWidth=320,MinHeight=200,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner};
        var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Would you like to enable the Nexaween theme?",TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,FontSize=20,Margin=new Thickness(0,0,0,22)});
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Center};var yes=new Button{Content="OK",MinWidth=100,Margin=new Thickness(0,0,12,0)};var no=new Button{Content="No Thanks",MinWidth=100,IsCancel=true};actions.Children.Add(yes);actions.Children.Add(no);panel.Children.Add(actions);prompt.Content=panel;
        yes.Click+=(_,_)=>prompt.DialogResult=true;no.Click+=(_,_)=>prompt.DialogResult=false;
        ChooseSeason(prompt.ShowDialog()==true);
    }
    private void Render()
    {
        body.Children.Clear();error.Text="";back.Visibility=Step==0?Visibility.Collapsed:Visibility.Visible;next.Content=Step==4?"Create My Space":"Continue";
        title.Text=new[]{"Welcome to Nexa","What are you doing?","Pick Your Tools","Make It Feel Like You","Your Space Is Ready"}[Step];
        switch(Step)
        {
            case 0:
                Text("A little space to stay present. Make it yours in a few quick steps.");Text("What should we call you? (optional)");
                var display=new TextBox{Text=Profile.DisplayName,TextAlignment=TextAlignment.Center,MaxLength=80,FontSize=22,MinHeight=48};display.TextChanged+=(_,_)=>Profile.DisplayName=display.Text.Trim();body.Children.Add(display);break;
            case 1:
                foreach(var type in GuidedSetup.Types){var option=new RadioButton{Content=type,IsChecked=Profile.PreferredDashboard==type,GroupName="spaceType",Margin=new Thickness(0,10,0,10),FontSize=18};option.Checked+=(_,_)=>{Profile.PreferredDashboard=type;};body.Children.Add(option);}break;
            case 2:
                Text("Choose the widgets for your Space. Connected tools can be configured later in Settings.");
                void Widgets(string heading,IEnumerable<string> names){Text(heading);var grid=new WrapPanel();foreach(var id in names){var definition=WidgetLibrary.Definition(id);var box=new CheckBox{Content=id switch{"Pixel timer"=>"Pixel Timer","Task List"=>"To-Do List","Hedy Notes"=>"HEDY Notes",_=>id},Tag=id,IsChecked=tools.Contains(id),Width=195,Margin=new Thickness(0,0,14,14),ToolTip=definition.Description};System.Windows.Automation.AutomationProperties.SetName(box,id);box.Click+=(_,_)=>{if(box.IsChecked==true)tools.Add(id);else tools.Remove(id);};grid.Children.Add(box);}body.Children.Add(grid);}
                Widgets("Featured Widgets",FeaturedWidgets);Widgets("All Widgets",WidgetLibrary.Definitions.Select(d=>d.Name).Distinct().Except(FeaturedWidgets));break;
            case 3:
                Text("Choose a standard appearance. See it immediately, and fine-tune colors and sounds later in Settings.");
                var modes=new WrapPanel();foreach(var mode in new[]{SessionDashboard.ThemeMode.Light,SessionDashboard.ThemeMode.Dark}){var choice=new RadioButton{Content=mode+" Mode",GroupName="setupTheme",IsChecked=(Profile.ThemeMode==SessionDashboard.ThemeMode.System?(Theme.IsLightMode?SessionDashboard.ThemeMode.Light:SessionDashboard.ThemeMode.Dark):Profile.ThemeMode)==mode,FontSize=18,Margin=new Thickness(0,0,24,16)};choice.Checked+=(_,_)=>{Profile.ThemeMode=mode;Options.SeasonalMode="Off";Preview();};modes.Children.Add(choice);}body.Children.Add(modes);
                Text("Theme Colors");var palettes=new WrapPanel();
                foreach(var palette in Theme.OfficialThemes)
                {
                    var card=new StackPanel();card.Children.Add(new Border{Width=124,Height=64,CornerRadius=new CornerRadius(10),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(Theme.AppPalette(palette).Accent)),Child=new TextBlock{Text="00:25",Foreground=Brushes.Black,FontSize=23,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center}});card.Children.Add(new TextBlock{Text=AppearancePalettes.Name(palette),Margin=new Thickness(0,7,0,0)});
                    var choice=new RadioButton{Content=card,GroupName="setupPalette",IsChecked=Theme.OfficialTheme(Profile.Theme)==palette,Margin=new Thickness(0,0,18,18),ToolTip=AppearancePalettes.Name(palette)};System.Windows.Automation.AutomationProperties.SetName(choice,AppearancePalettes.Name(palette));choice.Checked+=(_,_)=>{Profile.Theme=palette;Preview();};palettes.Children.Add(choice);
                }
                body.Children.Add(palettes);var sounds=new CheckBox{Content="Enable interface sounds",IsChecked=Profile.Sounds,Margin=new Thickness(0,8,0,0)};sounds.Click+=(_,_)=>Profile.Sounds=sounds.IsChecked==true;body.Children.Add(sounds);break;
            case 4:
                Text("Name Your Space");var input=new TextBox{Text=name,MaxLength=100,FontSize=23,TextAlignment=TextAlignment.Center,MinHeight=48,Margin=new Thickness(0,0,0,16)};input.TextChanged+=(_,_)=>name=input.Text.Trim();body.Children.Add(input);
                Text("Choose an Icon");body.Children.Add(SpaceIcons.Picker(SpaceIcons.Resolve(spaceIcon,Profile.PreferredDashboard),value=>spaceIcon=value));
                var selected=new WrapPanel{Margin=new Thickness(0,16,0,0)};foreach(var tool in tools){var chip=new Border{Padding=new Thickness(10),Margin=new Thickness(0,0,8,8),CornerRadius=new CornerRadius(8),BorderThickness=new Thickness(1),Child=new TextBlock{Text=tool}};chip.SetResourceReference(Border.BorderBrushProperty,"LineBrush");selected.Children.Add(chip);}body.Children.Add(selected);
                Text("Nexaween: "+(NexaSeason.Active(Options,DateTimeOffset.UtcNow)?"On":"Off"));Text("Ready when you are. Every tool can be changed later.");
                if(NexaSeason.Active(Options,DateTimeOffset.UtcNow))
                {
                    var decoration=new NexaweenDecorations{Height=80,IsHitTestVisible=false};body.Children.Add(decoration);
                    var quiet=Continuity.Clone(Options);quiet.SeasonalAnimation=false;
                    void Decorate(){var width=Math.Max(1,body.ActualWidth);decoration.ConfigureAnimation(quiet,new Rect(0,0,width,80));decoration.Update(width,80,[],"Low");}
                    decoration.Loaded+=(_,_)=>Decorate();decoration.SizeChanged+=(_,_)=>Decorate();Decorate();
                }
                Dispatcher.BeginInvoke(new Action(OfferSeason));break;
        }
    }
}
internal static class SetupNavigation
{
    internal static void TryRun(Window owner,StateStore store,MainWindow? space=null)
    {
        try{Run(owner,store,space);}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {MessageBox.Show(owner,"Setup could not finish. Your existing Spaces and preferences were retained. "+ex.Message,"nexa setup");}
    }
    internal static void Run(Window owner,StateStore store,MainWindow? space=null)
    {
        var wizard=new OnboardingWindow(store) { Owner=owner };
        if(wizard.ShowDialog()!=true || wizard.Result==null) return;
        if(space!=null) { if(space.AcceptGuidedSpace(wizard.Result,wizard.Options)) { store.SaveProfile(wizard.Profile); Theme.Apply(Application.Current.Resources,wizard.Profile.ThemeMode,wizard.Profile.Theme); store.LoadInteractionPreferences(space.state); } return; }
        var saved=store.Load(); var workspace=saved.Workspace??new WorkspaceDocument { UserId=store.UserId };
        workspace.Settings=Continuity.Clone(wizard.Options);workspace.Dashboards.Add(wizard.Result); workspace.ActiveDashboardId=wizard.Result.DashboardId;
        store.Save(Continuity.Activate(workspace,store.UserId,saved.HedyRegion,saved.HedyProtectedKey));
        store.SaveProfile(wizard.Profile); Theme.Apply(Application.Current.Resources,wizard.Profile.ThemeMode,wizard.Profile.Theme);
        var window=new MainWindow(store); Application.Current.MainWindow=window; window.Show(); owner.Close();
    }
}

public partial class MainWindow
{
    internal bool AcceptGuidedSpace(DashboardRecord record,NexaOptions? options=null)
    {
        if(!SaveNow()) return false;
        var candidate=CopyWorkspaceState().Workspace!;if(options!=null)candidate.Settings=Continuity.Clone(options); candidate.Dashboards.Add(record); candidate.ActiveDashboardId=record.DashboardId;
        return CommitWorkspace(candidate);
    }
}
