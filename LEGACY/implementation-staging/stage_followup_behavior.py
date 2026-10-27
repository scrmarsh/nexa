from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('NexaEcosystem.Models.cs',lambda t:t.replace('enum BugBehavior { Cozy, Creepy, Combative }','enum BugBehavior { Cozy = 0, Creepy = 1, Combative = 2, Classic = 3 }').replace('= BugBehavior.Cozy;','= BugBehavior.Classic;').replace('    public BugAppearance Appearance','    public BugBehavior? NonSeasonalBehavior {get;init;}\n    public BugBehavior? SeasonalBehavior {get;init;}\n    public bool? LastSeasonalActive {get;init;}\n    public BugAppearance Appearance',1).replace('=> MatchAppearance ? Behavior switch { BugBehavior.Cozy => BugAppearance.Cozy, BugBehavior.Creepy => BugAppearance.Creepy, _ => BugAppearance.Normal } : Appearance;','=> Behavior switch { BugBehavior.Cozy => BugAppearance.Cozy, BugBehavior.Creepy => BugAppearance.Creepy, _ => BugAppearance.Normal };').replace('    internal EcosystemOptions Safe()', '''    internal EcosystemOptions ForSeason(bool active)
    {
        if(LastSeasonalActive==active)return this;
        var normal=NonSeasonalBehavior;var seasonal=SeasonalBehavior;
        if(LastSeasonalActive==true)seasonal=Behavior;else if(LastSeasonalActive==false)normal=Behavior;
        var selected=active?seasonal ?? BugBehavior.Cozy:normal ?? BugBehavior.Classic;
        return this with {Behavior=selected,NonSeasonalBehavior=normal,SeasonalBehavior=seasonal,LastSeasonalActive=active,Adorable=active && seasonal==null?true:Adorable};
    }
    internal EcosystemOptions ChooseBehavior(BugBehavior behavior,bool seasonal)=>this with {Behavior=behavior,LastSeasonalActive=seasonal,NonSeasonalBehavior=seasonal?NonSeasonalBehavior:behavior,SeasonalBehavior=seasonal?behavior:SeasonalBehavior};
    internal static EcosystemOptions Existing(EcosystemOptions value)=>value.LastSeasonalActive==null && value.NonSeasonalBehavior==null && value.SeasonalBehavior==null?value with {NonSeasonalBehavior=value.Behavior,SeasonalBehavior=value.Behavior}:value;
    internal EcosystemOptions Safe()''').replace('Behavior : BugBehavior.Cozy','Behavior : BugBehavior.Classic'))
edit('Models.cs',lambda t:t.replace('JsonSerializer.Deserialize<EcosystemOptions>(eco,Options)?.Safe()','EcosystemOptions.Existing(JsonSerializer.Deserialize<EcosystemOptions>(eco,Options) ?? new()).Safe()'))
edit('SettingsDialog.Bugs.cs',lambda t:t.replace('behavior and appearance are independent.','appearance follows the selected behavior.').replace('Enum.GetValues<BugBehavior>()','new[]{BugBehavior.Classic,BugBehavior.Cozy,BugBehavior.Creepy,BugBehavior.Combative}').replace('''        var match = new CheckBox { Content = "Match Appearance to Behavior", IsChecked = options.MatchAppearance, Margin = new Thickness(0, 12, 0, 8) }; page.Children.Add(match);
        var appearance = new ComboBox { ItemsSource = Enum.GetValues<BugAppearance>(), SelectedItem = options.Appearance, IsEnabled = !options.MatchAppearance }; Field(page, "Appearance override", appearance);
''','').replace('''            options = options with { Behavior = behavior.SelectedItem is BugBehavior mode ? mode : BugBehavior.Cozy, Appearance = appearance.SelectedItem is BugAppearance style ? style : options.Appearance,
                MatchAppearance = match.IsChecked == true, Adorable = adorable.IsChecked == true, Size = size.Value, Quality = quality.SelectedItem is BugAnimationQuality chosen ? chosen : BugAnimationQuality.Balanced, Costumes = costumes.IsChecked == true };
            appearance.IsEnabled = !options.MatchAppearance;''','''            options = options.ChooseBehavior(behavior.SelectedItem is BugBehavior mode ? mode : BugBehavior.Classic,NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)) with {
                Adorable=adorable.IsChecked==true, Size=size.Value, Quality=quality.SelectedItem is BugAnimationQuality chosen?chosen:BugAnimationQuality.Balanced, Costumes=costumes.IsChecked==true };''').replace('        match.Click += (_, _) => Changed(); appearance.SelectionChanged += (_, _) => Changed();','        ').replace('BugBehavior.Creepy => "Hiding,','BugBehavior.Classic => "Original bug appearance with exploratory walking, edge crawling, flight and occasional grooming. No colony-war progression.",\n                BugBehavior.Creepy => "Hiding,'))
edit('NexaEcosystem.Store.cs',lambda t:t.replace('var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);','''var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);
        var current=Manager.EcosystemSettings;var mapped=current.ForSeason(active);
        if(mapped!=current)
        {
            var preferences=Manager.Preferences with {Ecosystem=mapped};Manager.Configure(preferences);
            try{store.SaveBugPreferences(preferences);}catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Status="Bug preferences could not be saved. Your previous settings are preserved.";}
        }'''))
edit('DashboardBugs.cs',lambda t:t.replace('Preferences=preferences; if(!EcosystemActive) Clear();','''Preferences=preferences; if(!EcosystemActive) Clear();
        if(EcosystemActive)
        {
            if(preferences.Species!=BugSpecies.AllBugs)Bugs.RemoveAll(b=>b.Species!=preferences.Species);
            if(Bugs.Count>EcosystemCap)Bugs.RemoveRange(EcosystemCap,Bugs.Count-EcosystemCap);
        }'''))
edit('NexaEcosystem.Simulation.cs',lambda t:t.replace('        var mode = EcosystemSettings.Behavior; var roll = random.NextDouble();','''        var mode = EcosystemSettings.Behavior; var roll = random.NextDouble();
        if(mode==BugBehavior.Classic)
        {
            bug.Activity=roll<.12?BugActivity.Grooming:BugActivity.Exploring;
            bug.Target=Surfaces.Count>0 && roll<.65?EdgePoint(Surfaces.Values.ElementAt(random.Next(Surfaces.Count)),random.NextDouble()):new Point(Range(25,WorldWidth-25),Range(25,WorldHeight-25));
            bug.Decision=Range(2,7);return;
        }'''))
print('Implemented four functional modes, deterministic appearance mapping and separate seasonal choices; density/species changes update the maintained population.')
