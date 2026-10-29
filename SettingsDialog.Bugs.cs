using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildBugs()
    {
        var page = Page("Add Bugs", "Optional dashboard overlays. Low, Normal and High have population limits. Infestation adapts spawning to available performance. Choose behavior, appearance and Adorable Mode independently.");
        var world=NexaEcosystem.For(store);world.ApplySeason(ReliabilityOptions);
        var preferences=world.Manager.Preferences;var options=world.Manager.EcosystemSettings.Safe();
        var enabled = new CheckBox { Content = "Add Bugs to the dashboard", IsChecked = preferences.Enabled, Margin = new Thickness(0, 10, 0, 14) }; page.Children.Add(enabled);
        var species = new ComboBox { ItemsSource = new[] { BugSpecies.AllBugs }.Concat(BugManager.AllSpecies).ToArray(), SelectedItem = preferences.Species };
        Field(page, "Bug type", species);
        var density = new ComboBox { ItemsSource = new[] { "Low", "Normal", "High", "Infestation" }, SelectedIndex = (int)preferences.Density }; Field(page, "Density", density);
        var behavior = new ComboBox { ItemsSource = new[]{BugBehavior.Classic,BugBehavior.Cozy,BugBehavior.Combative}, SelectedItem = options.Behavior==BugBehavior.Creepy?BugBehavior.Classic:options.Behavior };
        Field(page,"Behavior",behavior);
        var appearance = new ComboBox { ItemsSource = new[] { "Classic", "Creepy", "Halloween" }, SelectedIndex = options.EffectiveAppearance == BugAppearance.Creepy ? 1 : options.EffectiveAppearance == BugAppearance.Halloween ? 2 : 0 };
        Field(page, "Appearance", appearance);
        var updatingAppearance = false;
        var description = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 12) }; page.Children.Add(description);
        var adorable = new CheckBox { Content = "Adorable Mode", IsChecked = options.Adorable, Margin = new Thickness(0, 16, 20, 10) };
        var advanced = new CheckBox { Content = "Advanced Animations", IsChecked = options.AdvancedAnimations, Margin = new Thickness(0, 16, 0, 10) };
        var animationToggles = new WrapPanel(); animationToggles.Children.Add(adorable); animationToggles.Children.Add(advanced); page.Children.Add(animationToggles);
        var costumes = new CheckBox { Content = "Halloween costumes", IsChecked = options.Costumes, Margin = new Thickness(0, 0, 0, 10) }; page.Children.Add(costumes);
        var size = new Slider { Minimum = .75, Maximum = 4, Value = options.Size, TickFrequency = .25, IsSnapToTickEnabled = true, IsMoveToPointEnabled = true };
        var sizeLabel = new TextBlock(); Field(page, "Bug size", size); page.Children.Add(sizeLabel);
        var clickPadding=new Slider{Minimum=1.4,Maximum=1.6,Value=options.HitboxExpansion,TickFrequency=.05,IsSnapToTickEnabled=true};
        Field(page,"Bug Hitbox Size",clickPadding);
        page.Children.Add(new TextBlock{Text="Adjusts the clickable area around each bug.",TextWrapping=TextWrapping.Wrap});
        var hitboxEnds=new DockPanel();var small=new TextBlock{Text="Small"};var large=new TextBlock{Text="Large",HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(small,Dock.Left);hitboxEnds.Children.Add(small);hitboxEnds.Children.Add(large);page.Children.Add(hitboxEnds);
        var quality = new ComboBox { ItemsSource = Enum.GetValues<BugAnimationQuality>(), SelectedItem = options.Quality }; Field(page, "Animation quality", quality);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 12) }; page.Children.Add(status);
        void Refresh()
        {
            if(options.MatchAppearance)
            {
                updatingAppearance=true;
                try{appearance.SelectedIndex=options.EffectiveAppearance==BugAppearance.Creepy?1:options.EffectiveAppearance==BugAppearance.Halloween?2:0;}
                finally{updatingAppearance=false;}
            }
            sizeLabel.Text = $"{size.Value:P0} · default 200%";
            description.Text = (BugBehavior?)behavior.SelectedItem switch
            {
                BugBehavior.Classic => "Original bug appearance with exploratory walking, edge crawling, flight and occasional grooming. No colony-war progression.",
                BugBehavior.Creepy => "Hiding, sudden scuttling, web building and occasional natural hunting. Predominantly peaceful.",
                BugBehavior.Combative => "Territorial defense, coordinated raids and resource competition. No forced matches or automatic world resets.",
                _ => "Friendly exploration, species-specific gathering, nest building, socializing, grooming and sleeping."
            };
            status.Text = world.Status ?? $"{world.Manager.Bugs.Count} saved creatures · {world.Manager.Nests.Sum(n => n.Deliveries)} resource deliveries · {world.Manager.EcosystemSettings.EffectiveAppearance} appearance";
        }
        void Changed(bool appearanceChanged = false)
        {
            options = options.ChooseBehavior(behavior.SelectedItem is BugBehavior mode ? mode : BugBehavior.Classic,NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)) with {
                MatchAppearance=false, Appearance=appearance.SelectedIndex==1?BugAppearance.Creepy:appearance.SelectedIndex==2?BugAppearance.Halloween:BugAppearance.Normal, Adorable=adorable.IsChecked==true, AdvancedAnimations=advanced.IsChecked==true, Size=size.Value, HitboxExpansion=clickPadding.Value, Quality=quality.SelectedItem is BugAnimationQuality chosen?chosen:BugAnimationQuality.Balanced, Costumes=costumes.IsChecked==true };
            preferences = new(enabled.IsChecked == true, species.SelectedItem is BugSpecies type ? type : BugSpecies.Ants, density.SelectedIndex is >=0 and <=3 ? (BugDensity)density.SelectedIndex : BugDensity.Normal, options.Safe());
            if (preferences.Enabled) world.Manager.WorldPaused = false;
            world.ApplyPreferences(preferences); world.ApplySeason(ReliabilityOptions);
            foreach (var window in Application.Current.Windows.OfType<MainWindow>().Where(w => w.SettingsStore.UserId == store.UserId && w.SettingsStore.FilePath == store.FilePath)) window.ApplyBugPreferences(preferences);
            foreach(var home in Application.Current.Windows.OfType<HomeWindow>())home.RefreshCompanionOptions(store,options);
            QueueSave("bugs", () => { store.SaveBugPreferences(preferences); if (!world.Save()) throw new System.IO.IOException(world.Status ?? "Ecosystem could not be saved."); });
            Refresh();
        }
        enabled.Click += (_, _) => Changed(); species.SelectionChanged += (_, _) => Changed(); density.SelectionChanged += (_, _) => Changed(); behavior.SelectionChanged += (_, _) => Changed(); appearance.SelectionChanged += (_, _) => {if(!updatingAppearance)Changed(true);};
        adorable.Click += (_, _) => Changed(); advanced.Click += (_, _) => Changed(); costumes.Click += (_, _) => Changed(); size.ValueChanged += (_, _) => Changed();clickPadding.ValueChanged+=(_,_)=>Changed(); quality.SelectionChanged += (_, _) => Changed();
        if (space != null)
        {
            var totals = new Button { Content = "Add Bugs Squashed widget", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 8) };
            totals.Click += (_, _) => space.AddSquashesWidget(); page.Children.Add(totals);
        }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => Refresh(); Loaded += (_, _) => timer.Start(); Closed += (_, _) => timer.Stop(); Refresh();
        return new ScrollViewer { Tag="HiddenPageScroll",Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
