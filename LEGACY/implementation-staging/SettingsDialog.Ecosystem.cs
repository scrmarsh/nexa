using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildBugs()
    {
        var page = Page("Add Bugs", "One saved ecosystem shared by your dashboard and the ecosystem view. Low, Normal and High control density; behavior and appearance are independent.");
        var preferences = store.LoadBugPreferences(); var options = (preferences.Ecosystem ?? new()).Safe();
        var world = NexaEcosystem.For(store);
        var enabled = new CheckBox { Content = "Add Bugs to the dashboard", IsChecked = preferences.Enabled, Margin = new Thickness(0, 10, 0, 14) }; page.Children.Add(enabled);
        var species = new ComboBox { ItemsSource = BugManager.AllSpecies.Append(BugSpecies.AllBugs).ToArray(), SelectedItem = preferences.Species };
        Field(page, "Bug type", species);
        var density = new ComboBox { ItemsSource = new[] { "Low", "Normal", "High" }, SelectedIndex = preferences.Density == BugDensity.Low ? 0 : preferences.Density == BugDensity.Infestation ? 2 : 1 }; Field(page, "Density", density);
        var behavior = new ComboBox { ItemsSource = Enum.GetValues<BugBehavior>(), SelectedItem = options.Behavior }; Field(page, "Behavior", behavior);
        var description = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 12) }; page.Children.Add(description);
        var match = new CheckBox { Content = "Match Appearance to Behavior", IsChecked = options.MatchAppearance, Margin = new Thickness(0, 12, 0, 8) }; page.Children.Add(match);
        var appearance = new ComboBox { ItemsSource = Enum.GetValues<BugAppearance>(), SelectedItem = options.Appearance, IsEnabled = !options.MatchAppearance }; Field(page, "Appearance override", appearance);
        var adorable = new CheckBox { Content = "Adorable Mode", IsChecked = options.Adorable, Margin = new Thickness(0, 16, 0, 10) }; page.Children.Add(adorable);
        var size = new Slider { Minimum = .75, Maximum = 4, Value = options.Size, TickFrequency = .25, IsSnapToTickEnabled = true, IsMoveToPointEnabled = true };
        var sizeLabel = new TextBlock(); Field(page, "Bug size", size); page.Children.Add(sizeLabel);
        var quality = new ComboBox { ItemsSource = Enum.GetValues<BugAnimationQuality>(), SelectedItem = options.Quality }; Field(page, "Animation quality", quality);
        var costumes = new CheckBox { Content = "Halloween bug costumes when Nexaween is active", IsChecked = options.Costumes, Margin = new Thickness(0, 16, 0, 8) }; page.Children.Add(costumes);
        var controls = new WrapPanel { Margin = new Thickness(0, 14, 0, 10) };
        var view = new Button { Content = "View Ecosystem", Margin = new Thickness(0, 0, 8, 8) };
        var pause = new Button { Margin = new Thickness(0, 0, 8, 8) };
        var restore = new Button { Content = "Restore Backup", Margin = new Thickness(0, 0, 8, 8) };
        var reset = new Button { Content = "Reset Ecosystem", Margin = new Thickness(0, 0, 8, 8) };
        controls.Children.Add(view); controls.Children.Add(pause); controls.Children.Add(restore); controls.Children.Add(reset); page.Children.Add(controls);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 12) }; page.Children.Add(status);
        void Refresh()
        {
            pause.Content = world.Manager.WorldPaused ? "Resume World" : "Pause World";
            sizeLabel.Text = $"{size.Value:P0} · default 200%";
            description.Text = (BugBehavior?)behavior.SelectedItem switch
            {
                BugBehavior.Creepy => "Hiding, sudden scuttling, web building and occasional natural hunting. Predominantly peaceful.",
                BugBehavior.Combative => "Territorial defense, coordinated raids and resource competition. No forced matches or automatic world resets.",
                _ => "Friendly exploration, species-specific gathering, nest building, socializing, grooming and sleeping."
            };
            status.Text = world.Status ?? $"{world.Manager.Bugs.Count} saved creatures · {world.Manager.Nests.Sum(n => n.Deliveries)} resource deliveries · {world.Manager.EcosystemSettings.EffectiveAppearance} appearance";
        }
        void Changed()
        {
            options = options with { Behavior = behavior.SelectedItem is BugBehavior mode ? mode : BugBehavior.Cozy, Appearance = appearance.SelectedItem is BugAppearance style ? style : options.Appearance,
                MatchAppearance = match.IsChecked == true, Adorable = adorable.IsChecked == true, Size = size.Value, Quality = quality.SelectedItem is BugAnimationQuality chosen ? chosen : BugAnimationQuality.Balanced, Costumes = costumes.IsChecked == true };
            appearance.IsEnabled = !options.MatchAppearance;
            preferences = new(enabled.IsChecked == true, species.SelectedItem is BugSpecies type ? type : BugSpecies.Ants, density.SelectedIndex == 0 ? BugDensity.Low : density.SelectedIndex == 2 ? BugDensity.Infestation : BugDensity.Normal, options.Safe());
            world.ApplyPreferences(preferences); world.ApplySeason(ReliabilityOptions);
            foreach (var window in Application.Current.Windows.OfType<MainWindow>().Where(w => w.SettingsStore.UserId == store.UserId && w.SettingsStore.FilePath == store.FilePath)) window.ApplyBugPreferences(preferences);
            QueueSave("bugs", () => { store.SaveBugPreferences(preferences); if (!world.Save()) throw new System.IO.IOException(world.Status ?? "Ecosystem could not be saved."); });
            Refresh();
        }
        enabled.Click += (_, _) => Changed(); species.SelectionChanged += (_, _) => Changed(); density.SelectionChanged += (_, _) => Changed(); behavior.SelectionChanged += (_, _) => Changed();
        match.Click += (_, _) => Changed(); appearance.SelectionChanged += (_, _) => Changed(); adorable.Click += (_, _) => Changed(); size.ValueChanged += (_, _) => Changed(); quality.SelectionChanged += (_, _) => Changed(); costumes.Click += (_, _) => Changed();
        view.Click += (_, _) => { if (FlushPendingChanges()) { var host = Owner ?? this; Close(); EcosystemWindow.ShowFor(host, store); } };
        pause.Click += (_, _) => { world.TogglePause(); Refresh(); };
        restore.Click += (_, _) => { if (MessageBox.Show(this, "Restore the previous ecosystem backup? A recovery copy of the current state is kept.", "Restore ecosystem", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes) world.RestoreBackup(); Refresh(); };
        reset.Click += (_, _) => { if (MessageBox.Show(this, "Reset the ecosystem? The current world is preserved in a recovery copy. Dashboard data and history remain unchanged.", "Reset ecosystem", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes) world.Reset(); Refresh(); };
        if (space != null)
        {
            var totals = new Button { Content = "Add Bugs Squashed widget", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 8) };
            totals.Click += (_, _) => space.AddSquashesWidget(); page.Children.Add(totals);
        }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => Refresh(); Loaded += (_, _) => timer.Start(); Closed += (_, _) => timer.Stop(); Refresh();
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    }
}
