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
    internal int Step { get; private set; }
    private readonly StackPanel body = new();
    private readonly TextBlock title = new() { FontSize=30, FontWeight=FontWeights.SemiBold, Margin=new Thickness(0,0,0,12) };
    private readonly TextBlock error = new() { TextWrapping=TextWrapping.Wrap, Foreground=Brushes.IndianRed };
    private readonly Button back = new() { Content="Back", Margin=new Thickness(0,0,10,0) };
    private readonly Button next = new() { Content="Continue" };
    private HashSet<string> tools;
    private string name = "Class space";
    private string spaceIcon="";
    internal NexaOptions Options {get;}
    private bool finishing;
    internal OnboardingWindow(StateStore store)
    {
        this.store=store; Profile=Continuity.Clone(store.LoadProfile());
        Options=File.Exists(store.FilePath)?Continuity.Clone(store.Load().Workspace?.Settings??new()):new NexaOptions{SeasonalMode="On"};
        if(!GuidedSetup.Types.Contains(Profile.PreferredDashboard)) Profile.PreferredDashboard="Class";
        name=Profile.PreferredDashboard+" space";
        var available=GuidedSetup.Recommendations(Profile.PreferredDashboard).Concat(new[] { "Tasks","Calendar","Focus Reset","Flashcards","Session timeline" }).ToHashSet();
        tools=Profile.PreferredWidgets.Count>0?Profile.PreferredWidgets.Where(available.Contains).ToHashSet():GuidedSetup.Recommendations(Profile.PreferredDashboard).ToHashSet();
        var inputStyle=new Style(typeof(TextBox),Application.Current.TryFindResource(typeof(TextBox)) as Style);
        var template=new ControlTemplate(typeof(TextBox));var border=new FrameworkElementFactory(typeof(Border));border.Name="InputOutline";border.SetValue(Border.CornerRadiusProperty,new CornerRadius(12));border.SetResourceReference(Border.BackgroundProperty,"PanelBrush");border.SetResourceReference(Border.BorderBrushProperty,"LineBrush");border.SetValue(Border.BorderThicknessProperty,new Thickness(1));
        var content=new FrameworkElementFactory(typeof(ScrollViewer));content.Name="PART_ContentHost";content.SetValue(FrameworkElement.MarginProperty,new Thickness(12));border.AppendChild(content);template.VisualTree=border;var focus=new Trigger{Property=UIElement.IsKeyboardFocusWithinProperty,Value=true};focus.Setters.Add(new Setter(Border.BorderBrushProperty,new DynamicResourceExtension("AccentBrush"),"InputOutline"));template.Triggers.Add(focus);
        inputStyle.Setters.Add(new Setter(Control.TemplateProperty,template));Resources[typeof(TextBox)]=inputStyle;
        Title="Welcome to Nexa"; Width=740; Height=640; MinWidth=620; MinHeight=540; WindowStartupLocation=WindowStartupLocation.CenterScreen;
        var root=new DockPanel { Margin=new Thickness(34) };
        var actions=new StackPanel { Orientation=Orientation.Horizontal, HorizontalAlignment=HorizontalAlignment.Right, Margin=new Thickness(0,18,0,0) };
        actions.Children.Add(back); actions.Children.Add(next); DockPanel.SetDock(actions,Dock.Bottom); root.Children.Add(actions);
        var skip=new Button { Content="Skip setup for now", HorizontalAlignment=HorizontalAlignment.Left, Margin=new Thickness(0,10,0,0) };
        skip.Click+=(_,_)=> { Profile.OnboardingCompleted=true; try { store.SaveProfile(Profile); DialogResult=false; } catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) { error.Text="Could not save setup. Try again."; } };
        DockPanel.SetDock(skip,Dock.Bottom); root.Children.Add(skip);
        var panel=new StackPanel(); panel.Children.Add(title); panel.Children.Add(body); panel.Children.Add(error);
        root.Children.Add(new ScrollViewer { Content=panel, VerticalScrollBarVisibility=ScrollBarVisibility.Auto }); Content=root;
        back.Click+=(_,_)=> { Step=Step==3?1:Step-1; Render(); };
        next.Click+=(_,_)=>Advance(); Render();
    }
    private void Text(string text) => body.Children.Add(new TextBlock { Text=text, TextWrapping=TextWrapping.Wrap, Margin=new Thickness(0,0,0,18) });
    private void Choice(string text,bool selected,Action<bool> change)
    {
        var box=new CheckBox { Content=text, IsChecked=selected, Margin=new Thickness(0,7,0,7),FontSize=16 };
        if(WidgetLibrary.Definitions.Any(d=>d.Name==text)){var row=new StackPanel{Orientation=Orientation.Horizontal};row.Children.Add(MainWindow.ToolPreview(text));row.Children.Add(new TextBlock{Text=text,Margin=new Thickness(12,0,0,0),VerticalAlignment=VerticalAlignment.Center});box.Content=row;}
        var card=new Border{Child=box,Padding=new Thickness(12),CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,8)};
        card.SetResourceReference(Border.BorderBrushProperty,selected?"AccentBrush":"LineBrush");
        box.Click+=(_,_)=>{change(box.IsChecked==true);card.SetResourceReference(Border.BorderBrushProperty,box.IsChecked==true?"AccentBrush":"LineBrush");NexaMotion.Pop(card,Options);};
        body.Children.Add(card);
    }
    internal void Advance()
    {
        if(Step<5)
        {
            if(Step==1 && Profile.UseCases.Count>0)
            {
                Profile.PreferredDashboard=Profile.UseCases[0] switch { "Studying / Focus"=>"Study","Church / Small Group"=>"Church","Meetings" or "Work"=>"Meeting","Personal use" or "Other"=>"Custom",_=>"Class" };
                tools=GuidedSetup.Recommendations(Profile.PreferredDashboard).ToHashSet(); name=Profile.PreferredDashboard+" space";
            }
            Step=Step==1?3:Step+1; Render(); return;
        }
        if(finishing)return;
        if(string.IsNullOrWhiteSpace(name)){error.Text="Name your space before continuing.";return;}
        finishing=true;
        try
        {
            Profile.PreferredWidgets=tools.ToList(); Profile.OnboardingCompleted=true;
            Result=GuidedSetup.Create(Profile,name,tools,store.UserId);Result.Current.Data.OfferQuickStart=true;Result.Current.Data.SpaceIcon=SpaceIcons.Resolve(spaceIcon,Profile.PreferredDashboard);
            DialogResult=true;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException) { finishing=false; error.Text="Could not finish setup. Your existing spaces are safe. "+ex.Message; }
    }
    private void Render()
    {
        body.Children.Clear(); NexaMotion.Entrance(body,Options); error.Text=""; back.Visibility=Step==0?Visibility.Collapsed:Visibility.Visible; next.Content=Step==5?"Create My Space":"Continue";
        title.Text=new[] { "Welcome to nexa", "What will you use Nexa for?", "What are you doing?", "Pick your tools", "Make it feel like you", "Your space is ready!" }[Step];
        switch(Step)
        {
            case 0:
                Text("A little space to stay present. Five quick steps, no account needed."); Text("What should we call you? (optional)");
                var display=new TextBox { Text=Profile.DisplayName, TextAlignment=TextAlignment.Center, MaxLength=80, FontSize=22,MinHeight=52,Padding=new Thickness(14) }; display.TextChanged+=(_,_)=>Profile.DisplayName=display.Text.Trim(); body.Children.Add(display); break;
            case 1:
                Text("Pick any that fit.");
                foreach(var use in GuidedSetup.UseCases) Choice(use,Profile.UseCases.Contains(use),on=> { if(on && !Profile.UseCases.Contains(use)) Profile.UseCases.Add(use); else if(!on) Profile.UseCases.Remove(use); });
                Choice("Usually start a timed session",Profile.TimedSessions,on=>Profile.TimedSessions=on);
                Choice("Gentle focus reminders",Profile.FocusNudges,on=>Profile.FocusNudges=on); break;
            case 2:
                foreach(var type in GuidedSetup.Types)
                {
                    var option=new RadioButton { Content=type switch { "Study"=>"Study / Focus", "Church"=>"Church / Small Group", "Meeting"=>"Meeting / Work", _=>type }, IsChecked=Profile.PreferredDashboard==type, GroupName="spaceType", Margin=new Thickness(0,10,0,10), FontSize=18 };
                    option.Checked+=(_,_)=> { if(Profile.PreferredDashboard!=type) { Profile.PreferredDashboard=type; tools=GuidedSetup.Recommendations(type).ToHashSet(); name=type+" space"; } }; body.Children.Add(option);
                    option.SetResourceReference(Control.ForegroundProperty,"TextBrush");
                }
                break;
            case 3:
                Text("A few useful tools. You can add anything else later.");
                foreach(var tool in GuidedSetup.Recommendations(Profile.PreferredDashboard).Concat(new[] { "Tasks", "Calendar", "Focus Reset", "Flashcards", "Session timeline" }).Distinct())
                    Choice(tool=="Session timeline"?"Session / Hedy Notes":tool,tools.Contains(tool),on=> { if(on) tools.Add(tool); else tools.Remove(tool); });
                break;
            case 4:
                var seasonal=new CheckBox{Content="Nexaween",IsChecked=NexaSeason.Active(Options,DateTimeOffset.UtcNow),FontSize=18,Margin=new Thickness(0,0,0,16)};body.Children.Add(seasonal);
                var swatches=new WrapPanel(); body.Children.Add(swatches);
                foreach(var theme in Enum.GetValues<AppTheme>())
                {
                    var color=theme switch { AppTheme.Violet=>"#AA80FF", AppTheme.Aurora=>"#63D8BC", AppTheme.Ember=>"#FFAA70", _=>"#7395FF" };
                    var option=new RadioButton { GroupName="theme", IsChecked=Profile.Theme==theme, Margin=new Thickness(0,0,14,14), Content=new StackPanel { Children={ new Border { Width=112, Height=65, CornerRadius=new CornerRadius(8), Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), Child=new TextBlock { Text="00:25", FontSize=24, Foreground=Brushes.Black, HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Center } },new TextBlock { Text=theme.ToString(), Margin=new Thickness(0,6,0,0) } } } };
                    option.Checked+=(_,_)=>Profile.Theme=theme; swatches.Children.Add(option);
                    option.SetResourceReference(Control.ForegroundProperty,"TextBrush");
                }
                var mode=new ComboBox { ItemsSource=Enum.GetValues<ThemeMode>(),SelectedItem=Profile.ThemeMode,Margin=new Thickness(0,0,0,14) }; mode.SelectionChanged+=(_,_)=>Profile.ThemeMode=(ThemeMode)mode.SelectedItem; body.Children.Add(mode);
                void RefreshSeason(){var on=seasonal.IsChecked==true;swatches.Visibility=mode.Visibility=on?Visibility.Collapsed:Visibility.Visible;}
                seasonal.Click+=(_,_)=>{Options.SeasonalMode=seasonal.IsChecked==true?"On":"Off";RefreshSeason();NexaMotion.Entrance(body,Options);};RefreshSeason();
                Text("Enable Nexa sounds?");
                Choice("On (short, soft chimes)",Profile.Sounds,on=>Profile.Sounds=on);
                var preview=new Button { Content="Preview sound", HorizontalAlignment=HorizontalAlignment.Left };
                preview.Click+=(_,_)=> { if(Profile.Sounds) InterfaceAudio.Preview(Profile.Volume); else error.Text="Turn sounds on first to preview."; }; body.Children.Add(preview);
                break;
            case 5:
                Text("Name Your Space");
                var spaceName=new TextBox { Text=name,TextAlignment=TextAlignment.Center,FontSize=24,MinHeight=56,Padding=new Thickness(14), MaxLength=100, Margin=new Thickness(0,0,0,18) }; spaceName.TextChanged+=(_,_)=>name=spaceName.Text.Trim(); body.Children.Add(spaceName);
                body.Children.Add(SpaceIcons.Picker(SpaceIcons.Resolve(spaceIcon,Profile.PreferredDashboard),icon=>spaceIcon=icon));
                var selectedTools=new WrapPanel();foreach(var tool in tools){var previewRow=new StackPanel{Orientation=Orientation.Horizontal};previewRow.Children.Add(MainWindow.ToolPreview(tool));previewRow.Children.Add(new TextBlock{Text=tool,Margin=new Thickness(10,0,0,0),VerticalAlignment=VerticalAlignment.Center});var card=new Border{CornerRadius=new CornerRadius(10),Padding=new Thickness(14),Margin=new Thickness(0,8,8,0),BorderThickness=new Thickness(1),Child=previewRow};card.SetResourceReference(Border.BorderBrushProperty,"LineBrush");selectedTools.Children.Add(card);}body.Children.Add(selectedTools);
                Text("Nexaween: "+(NexaSeason.Active(Options,DateTimeOffset.UtcNow)?"On":"Off")); Text($"Theme: {Profile.Theme}\nSounds: {(Profile.Sounds?"On":"Off")}\nSession: {(Profile.TimedSessions?$"{Profile.DefaultMinutes} minutes":"Untimed")}");
                Text("Ready when you are. Every tool can be changed later."); break;
        }
    }
}

internal static class SetupNavigation
{
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
