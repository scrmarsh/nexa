from pathlib import Path
root=Path(r'M:\Nexa')
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
text=read('NexaEcosystem.Simulation.cs').replace('    internal EcosystemOptions EcosystemSettings => (Preferences.Ecosystem ?? new()).Safe();','    private static readonly EcosystemOptions defaultEcosystemOptions = new();\n    internal EcosystemOptions EcosystemSettings => Preferences.Ecosystem ?? defaultEcosystemOptions;')
needle='            if (bug.Activity == BugActivity.Carrying) bug.Target = nest.Position;'
text=text.replace(needle,needle+'''
            if (bug.Activity == BugActivity.Raiding && bug.RaidTarget is { } raided)
            {
                var targetNest = Nest(raided); bug.Target = targetNest.Position;
                if ((bug.Position-targetNest.Position).Length < 28)
                {
                    bug.Carrying = Math.Min(2,targetNest.Food); targetNest.Food -= bug.Carrying;
                    bug.RaidTarget = null; bug.EnemyId = null; bug.Activity = BugActivity.Carrying; bug.Target = nest.Position; bug.Decision = 30;
                }
                else if (bug.EnemyId == null)
                    bug.EnemyId = Bugs.FirstOrDefault(b=>b.Species==raided && b.Health>0 && (b.Position-bug.Position).Length<65)?.InteractionId;
            }''')
text=text.replace('bug.State = BugState.Crossing; bug.ResourceId = null; bug.EnemyId = null; bug.Decision = Range(5, 16);','bug.State = BugState.Crossing; bug.ResourceId = null; bug.EnemyId = null; bug.RaidTarget = null; bug.Decision = Range(5, 16);')
needle='        if (mode == BugBehavior.Combative)\n        {'
text=text.replace(needle,needle+'''
            if (Nest(bug.Species).Food < 15 && roll < .35)
            {
                var targetNest = Nests.Where(n=>n.Species!=bug.Species && n.Food>1).OrderByDescending(n=>n.Food).FirstOrDefault();
                if(targetNest!=null)
                {
                    bug.Activity=BugActivity.Raiding;bug.RaidTarget=targetNest.Species;bug.Target=targetNest.Position;bug.Decision=30;
                    foreach(var ally in Bugs.Where(b=>b!=bug && b.Species==bug.Species && b.Health>0 && (b.Position-bug.Position).Length<180).Take(2))
                    {ally.Activity=BugActivity.Raiding;ally.RaidTarget=targetNest.Species;ally.EnemyId=null;ally.Target=targetNest.Position;ally.Decision=30;}
                    return;
                }
            }''')
text=text.replace('if (enemy == null) { bug.Decision = 0; return; }','if (enemy == null) { bug.EnemyId = null; if (bug.RaidTarget == null) bug.Decision = 0; return; }')
text=text.replace('ResourceId = b.ResourceId, EnemyId = b.EnemyId, Carrying','ResourceId = b.ResourceId, EnemyId = b.EnemyId, RaidTarget = b.RaidTarget, Carrying')
text=text.replace('Activity = b.Activity, Personality = b.Personality, ResourceId = b.ResourceId, EnemyId = b.EnemyId, Carrying','Activity = b.Activity, Personality = b.Personality, ResourceId = b.ResourceId, EnemyId = b.EnemyId, RaidTarget = b.RaidTarget, Carrying')
write('NexaEcosystem.Simulation.cs',text)
text=read('NexaEcosystem.Models.cs').replace('    public Guid? EnemyId { get; set; }','    public Guid? EnemyId { get; set; }\n    public BugSpecies? RaidTarget { get; set; }')
text=text.replace('!Enum.IsDefined(b.Activity) ||','b.RaidTarget != null && !species.Contains(b.RaidTarget.Value) || !Enum.IsDefined(b.Activity) ||')
text=text.replace('Nests.Select(n => n.Species).Distinct().Count() != 6 || Nests.Any(n => n == null ||','Nests.Any(n => n == null) || Nests.Select(n => n.Species).Distinct().Count() != 6 || Nests.Any(n =>')
write('NexaEcosystem.Models.cs',text)
text=read('DashboardBugs.cs').replace('    internal Guid? ResourceId, EnemyId;','    internal Guid? ResourceId, EnemyId;\n    internal BugSpecies? RaidTarget;')
text=text.replace('        if(Preferences==preferences)return;','        preferences=preferences with {Ecosystem=(preferences.Ecosystem ?? new()).Safe()};\n        if(Preferences==preferences)return;')
text=text.replace('        Preferences=preferences; if(!EcosystemActive) Clear();','''        var modeChanged=Preferences.Ecosystem?.Behavior!=preferences.Ecosystem?.Behavior;
        Preferences=preferences; if(!EcosystemActive) Clear();
        else if(modeChanged) foreach(var bug in Bugs){bug.Decision=0;bug.EnemyId=null;bug.RaidTarget=null;bug.Activity=BugActivity.Exploring;}''')
write('DashboardBugs.cs',text)
text=read('Nexaween.cs')
needle='        var density = new ComboBox { ItemsSource = new[] { "Off", "Low", "Medium", "High" }, SelectedItem = ReliabilityOptions.DecorationDensity }; Field(page, "Decoration density", density);'
text=text.replace(needle,needle+'''
        var animation = new CheckBox { Content = "Seasonal animation", IsChecked = ReliabilityOptions.SeasonalAnimation, Margin = new Thickness(0,12,0,8) };
        var surprises = new CheckBox { Content = "Random spooky surprises", IsChecked = ReliabilityOptions.SpookySurprises, Margin = new Thickness(0,0,0,8) };
        var sounds = new CheckBox { Content = "Seasonal sounds", IsChecked = ReliabilityOptions.SeasonalSounds, Margin = new Thickness(0,0,0,8) };
        var costumes = new CheckBox { Content = "Halloween bug costumes", IsChecked = ReliabilityOptions.HalloweenBugCostumes, Margin = new Thickness(0,0,0,8) };
        foreach(var toggle in new[]{animation,surprises,sounds,costumes})page.Children.Add(toggle);''')
text=text.replace('            ReliabilityOptions.DecorationDensity = density.SelectedItem?.ToString() ?? "Medium";','''            ReliabilityOptions.DecorationDensity = density.SelectedItem?.ToString() ?? "Medium";
            ReliabilityOptions.SeasonalAnimation = animation.IsChecked == true; ReliabilityOptions.SpookySurprises = surprises.IsChecked == true;
            ReliabilityOptions.SeasonalSounds = sounds.IsChecked == true; ReliabilityOptions.HalloweenBugCostumes = costumes.IsChecked == true;
            NexaEcosystem.UpdateSeason(store, ReliabilityOptions);''')
text=text.replace('        mode.SelectionChanged += (_, _) => Update();','        foreach(var toggle in new[]{animation,surprises,sounds,costumes})toggle.Click+=(_,_)=>Update();\n        mode.SelectionChanged += (_, _) => Update();')
write('Nexaween.cs',text)
print('Added resource raids, immediate behavior switching, cached options and the full seasonal settings toggles.')
