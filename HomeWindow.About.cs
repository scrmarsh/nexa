using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SessionDashboard;

internal sealed partial class HomeWindow
{
    private Action? refreshBranding;
    internal void RefreshBranding()=>refreshBranding?.Invoke();
    private void BuildHomeHeader(StackPanel panel)
    {
        var header = new Grid{HorizontalAlignment=HorizontalAlignment.Center,MaxWidth=900}; header.ColumnDefinitions.Add(new() { Width = new GridLength(300) }); header.ColumnDefinitions.Add(new() { Width = new GridLength(32) }); header.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});
        var brand=new NexaWordmark(300);header.Children.Add(brand);
        var tagline = new TextBlock { Text = "", FontSize = 24, TextWrapping = TextWrapping.Wrap, TextAlignment=TextAlignment.Center, Foreground = (Brush)FindResource("TextBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0), MinHeight = 64 };
        System.Windows.Automation.AutomationProperties.SetName(tagline, "Home tagline"); Grid.SetColumn(tagline, 2); header.Children.Add(tagline); panel.Children.Add(header);

        var position = 0; var hold = 0; var restartDelay = 0; var fading = false; var typing = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(55) };
        refreshBranding=()=>{
            var phrase=NexaBranding.Tagline;position=hold=restartDelay=0;fading=false;
            tagline.BeginAnimation(OpacityProperty,null);tagline.Opacity=1;
            tagline.Visibility=phrase.Length==0?Visibility.Collapsed:Visibility.Visible;
            header.ColumnDefinitions[1].Width=new GridLength(phrase.Length==0?0:32);
            header.ColumnDefinitions[2].Width=phrase.Length==0?new GridLength(0):new GridLength(1,GridUnitType.Star);
            NexaBranding.ApplyTagline(tagline,NexaMotion.Allowed(Saved.Workspace?.Settings??new())?"":phrase);
            if(IsLoaded && IsVisible && WindowState!=WindowState.Minimized && phrase.Length>0 && NexaMotion.Allowed(Saved.Workspace?.Settings??new()))typing.Start();else typing.Stop();
        };refreshBranding();
        typing.Tick += (_, _) =>
        {
            var phrase=NexaBranding.Tagline;if(phrase.Length==0)return;
            if(position>phrase.Length)position=phrase.Length;
            if (fading) return;
            if (restartDelay > 0)
            {
                if (--restartDelay > 0) return;
                tagline.BeginAnimation(OpacityProperty, null); tagline.Opacity = 1; tagline.Text = ""; position = hold = 0;
            }
            if (position < phrase.Length) { NexaBranding.ApplyTagline(tagline,phrase[..++position]); return; }
            if (++hold < 28) return;
            fading = true;
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(650));
            fade.Completed += (_, _) => { fading = false; restartDelay = 14; };
            tagline.BeginAnimation(OpacityProperty, fade);
        };
        void ReconcileTyping()
        {
            if(IsLoaded && IsVisible && WindowState!=WindowState.Minimized && NexaBranding.Tagline.Length>0 && NexaMotion.Allowed(Saved.Workspace?.Settings??new()))typing.Start();
            else{typing.Stop();tagline.BeginAnimation(OpacityProperty,null);tagline.Opacity=1;}
        }
        Loaded+=(_,_)=>{refreshBranding?.Invoke();ReconcileTyping();};
        IsVisibleChanged+=(_,_)=>ReconcileTyping();StateChanged+=(_,_)=>ReconcileTyping();Closed+=(_,_)=>typing.Stop();
    }

    private NexaDialog InfoDialog(string title, double width = 720, double height = 620) => new NexaPanel { Title = "Nexa · " + title, Owner = this, Width = width, Height = height, MinWidth = 540, MinHeight = 440, WindowStartupLocation = WindowStartupLocation.CenterOwner };
    private TextBlock Copy(string text, double size = 14) => new() { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 0, 0, 16), LineHeight = size * 1.55 };

    private void OpenAbout()
    {
        var dialog = InfoDialog("About Nexa"); var root = new DockPanel();
        var footer = new Grid { Margin = new Thickness(28, 16, 28, 24) }; footer.ColumnDefinitions.Add(new()); footer.ColumnDefinitions.Add(new() { Width = new GridLength(12) }); footer.ColumnDefinitions.Add(new());
        var preview = new Button { Content = "Preview Nexa", MinHeight = 42, Style = (Style)FindResource("PrimaryButton") }; preview.Click += (_, _) => { dialog.Close(); OpenPreview(); }; footer.Children.Add(preview);
        var changes = new Button { Content = "Changelog", MinHeight = 42 }; changes.Click += (_, _) => { dialog.Close(); OpenChangelog(); }; Grid.SetColumn(changes, 2); footer.Children.Add(changes); DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var body = new StackPanel { Margin = new Thickness(28) };
        body.Children.Add(new NexaLogo { Width = 260, Height = 82, HorizontalAlignment = HorizontalAlignment.Left });
        body.Children.Add(Copy(App.VersionLabel, 13));
        body.Children.Add(Copy("a space for whatever comes next",24));
        void Section(string icon,string title,string description)
        {
            var content=new StackPanel();var heading=new StackPanel{Orientation=Orientation.Horizontal};
            heading.Children.Add(NexaIcons.Create(icon));heading.Children.Add(new TextBlock{Text=title,FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(12,0,0,10)});
            content.Children.Add(heading);var copy=Copy(description);copy.Margin=new Thickness(0);content.Children.Add(copy);
            var card=new Border{Child=content,Padding=new Thickness(20),CornerRadius=new CornerRadius(14),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,14)};
            card.SetResourceReference(Border.BackgroundProperty,"PanelBrush");card.SetResourceReference(Border.BorderBrushProperty,"LineBrush");body.Children.Add(card);
        }
        Section("Home","About nexa","A customizable local workspace for studying, work, meetings, planning and everyday progress. Keep useful tools together and arrange them around the way you spend your time.");
        Section("Clock","Spaces and Sessions","Create named Spaces with icons and saved layouts. Use session presets and recurring schedules, keep notes and tasks, and review recorded history without reopening an old session.");
        Section("Widgets","Widgets",string.Join("\n",WidgetLibrary.Definitions.Where(d=>d.Name!="Bugs Squashed").GroupBy(d=>d.Group).Select(g=>g.Key+": "+string.Join(", ",g.Select(d=>d.Name))))+"\nBugs Squashed is available separately in Add Bugs. Calendar and Google Tasks require existing experimental provider configuration.");
        Section("Productivity","Productivity","Cumulative per-Space scores survive sessions. Qualifying recorded actions award points; candy collection and direct bug squashes share a bounded streak multiplier. End Session preserves actual elapsed time and saves an accomplishment/score recap.");
        Section("Settings","Integrations","Todoist connects tasks. Hedy provides recording notes, transcripts and interval summaries through its own connection. Hevy is a separate optional workout-account connection. Google Tasks, Google Calendar and Outlook Calendar are listed separately as future/experimental integrations. Accounts are optional.");
        Section("Settings","Customization","Choose normal appearance and sound preferences, arrange or resize widgets, and choose Space icons.");
        Section("Candy","Seasonal Features","Nexaween adds optional pumpkins, webs, vines and collectible candy. Add Bugs has its own independent controls and appearance preferences. Turning Nexaween off removes seasonal decorations while keeping user-enabled bugs available.");
        Section("History","Data and Privacy","Spaces, notes, settings, history and scores are saved locally in JSON. Space import/export and local recovery copies are supported. Ordinary notes are not encrypted. Integration secrets use Windows account protection where the provider supports stored credentials. Configured providers receive the data needed for their features; cloud synchronization is not provided.");
        var build=typeof(App).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute),false).OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion??App.VersionLabel;
        Section("Settings","Development",App.VersionLabel+" · Alpha testing\nBuild: "+build+"\n"+ReleaseNotes.Description);
        var project=new Button{Content="GitHub project",HorizontalAlignment=HorizontalAlignment.Left};
        project.Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/scrmarsh/nexa"){UseShellExecute=true});body.Children.Add(project);
        dialog.Loaded+=(_,_)=>NexaMotion.Stagger(body.Children.OfType<FrameworkElement>(),Saved.Workspace?.Settings??new());
        root.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); dialog.Content = root; dialog.ShowDismissible();
    }

    private void OpenChangelog()
    {
        var dialog = InfoDialog("Changelog", 800, 720); var body = new StackPanel { Margin = new Thickness(28) };
        body.Children.Add(new TextBlock{Text=App.VersionLabel+" — "+ReleaseNotes.All[0].Title+" · prerelease",FontSize=22,FontWeight=FontWeights.Bold,TextWrapping=TextWrapping.Wrap});
        body.Children.Add(Copy("This Nexaween Alpha prerelease focuses on interface polish. Manual acceptance remains open; v0.1.0 is reserved for the first approved Beta.",15));
        body.Children.Add(new TextBlock { Text = "What’s new in Nexa", FontSize = 27, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        body.Children.Add(Copy("Release history · Windows desktop", 13));
        foreach (var release in ReleaseNotes.All)
        {
            var section = new StackPanel();
            var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var date = Copy(release.Date, 12); date.Margin = new Thickness(12, 4, 0, 0); DockPanel.SetDock(date, Dock.Right); heading.Children.Add(date);
            heading.Children.Add(new TextBlock { Text = new NexaReleaseIdentity(new Version(release.Version),release.Stage).Label + " \u2014 " + release.Title, TextWrapping = TextWrapping.Wrap, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("AccentBrush") }); section.Children.Add(heading);
            section.Children.Add(Copy(release.Summary, 15));
            foreach (var update in release.Updates)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 14) }; row.ColumnDefinitions.Add(new() { Width = new GridLength(22) }); row.ColumnDefinitions.Add(new());
                row.Children.Add(new System.Windows.Shapes.Ellipse { Width = 5, Height = 5, Fill = (Brush)FindResource("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(2, 8, 0, 0) });
                var text = Copy(update); text.Margin = new Thickness(0); Grid.SetColumn(text, 1); row.Children.Add(text); section.Children.Add(row);
            }
            var seasonalEntry = release.Version is "0.1.24" or "0.0.27" or "0.0.28" && Theme.SeasonalEnabled;
            if (seasonalEntry)
            {
                void Tint(DependencyObject element)
                {
                    if (element is TextBlock text) text.Foreground = (Brush)new BrushConverter().ConvertFromString("#84CC16")!;
                    for (var i=0; i<VisualTreeHelper.GetChildrenCount(element); i++) Tint(VisualTreeHelper.GetChild(element,i));
                }
                Tint(section);
                date.Foreground = (Brush)new BrushConverter().ConvertFromString("#D9F99D")!;
                section.Children.Insert(0, new TextBlock { Text = "🎃  NEXAWEEN  👻", FontSize = 15, FontWeight = FontWeights.Bold, Foreground = (Brush)new BrushConverter().ConvertFromString("#A855F7")!, Margin = new Thickness(0,0,0,12) });
            }
            body.Children.Add(new Border { Child = section, Background = seasonalEntry ? (Brush)new BrushConverter().ConvertFromString("#1C102A")! : (Brush)FindResource("PanelBrush"), BorderBrush = seasonalEntry ? (Brush)new BrushConverter().ConvertFromString("#F97316")! : (Brush)FindResource("LineBrush"), BorderThickness = new Thickness(seasonalEntry ? 2 : 1), CornerRadius = new CornerRadius(12), Padding = new Thickness(22), Margin = new Thickness(0, 8, 0, 16) });
        }
        dialog.Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; dialog.ShowDismissible();
    }
}
