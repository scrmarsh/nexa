using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private void BuildColonyWarsSettings(Panel page,ArcadeSettings initial,Action<ArcadeSettings> changed)
    {
        var options=initial.Safe();
        var content=new StackPanel {Margin=new Thickness(0,10,0,0)};
        var expansion=new Expander {Header="Colony Wars settings",Content=content,Margin=new Thickness(0,15,0,0)};page.Children.Add(expansion);
        content.Children.Add(new TextBlock {Text="Infinite Warfare preserves the shared local world when settings change. Classic Rounds changes restart its temporary battle. The persistent world is saved independently of your Spaces.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)});
        void Apply(ArcadeSettings value) {options=value.Safe();changed(options);}
        void Number(Panel panel,string label,double value,double min,double max,Action<double> apply)
        {
            var row=new DockPanel {Margin=new Thickness(0,3,0,3)};
            var input=new TextBox {Text=value.ToString("0.##",CultureInfo.CurrentCulture),Width=76,HorizontalContentAlignment=HorizontalAlignment.Right,ToolTip=$"{label} ({min}-{max})"};
            System.Windows.Automation.AutomationProperties.SetName(input,label);DockPanel.SetDock(input,Dock.Right);row.Children.Add(input);
            row.Children.Add(new TextBlock {Text=label,VerticalAlignment=VerticalAlignment.Center});panel.Children.Add(row);
            input.LostKeyboardFocus+=(_,_)=>
            {
                if(double.TryParse(input.Text,NumberStyles.Float,CultureInfo.CurrentCulture,out var parsed) && double.IsFinite(parsed))
                {value=Math.Clamp(parsed,min,max);apply(value);}
                input.Text=value.ToString("0.##",CultureInfo.CurrentCulture);
            };
        }
        var rules=new ComboBox {ItemsSource=new[]{"Infinite Warfare","Classic Rounds"},SelectedIndex=options.Mode==ArcadeRuleSet.ClassicRounds ? 1 : 0,Margin=new Thickness(0,4,0,8)};
        Field(content,"Arcade rules",rules);rules.SelectionChanged+=(_,_)=>Apply(options with{Mode=rules.SelectedIndex==1 ? ArcadeRuleSet.ClassicRounds : ArcadeRuleSet.InfiniteWarfare});
        var emergence=new CheckBox {Content="Allow new civilizations to migrate into safe areas",IsChecked=options.AllowNewCivilizations,Margin=new Thickness(0,4,0,8)};content.Children.Add(emergence);
        emergence.Click+=(_,_)=>Apply(options with{AllowNewCivilizations=emergence.IsChecked==true});
        var tactics=new StackPanel {Margin=new Thickness(0,6,0,8)};content.Children.Add(new Expander {Header="Warfare and territory",Content=tactics});
        void Toggle(string label,bool value,Action<bool> change)
        {var check=new CheckBox {Content=label,IsChecked=value,Margin=new Thickness(0,5,0,5)};tactics.Children.Add(check);check.Click+=(_,_)=>change(check.IsChecked==true);}
        Toggle("Allow organized campaigns",options.EnableCampaigns,v=>Apply(options with{EnableCampaigns=v}));
        Number(tactics,"Army aggression",options.Aggression,.25,2,v=>Apply(options with{Aggression=v}));
        Number(tactics,"War frequency",options.WarFrequency,.25,2,v=>Apply(options with{WarFrequency=v}));
        Toggle("Show territory tint",options.ShowTerritories,v=>Apply(options with{ShowTerritories=v}));
        Toggle("Show territory borders",options.ShowTerritoryBorders,v=>Apply(options with{ShowTerritoryBorders=v}));
        Toggle("Show faction labels",options.ShowFactionLabels,v=>Apply(options with{ShowFactionLabels=v}));
        Toggle("Highlight contested regions",options.HighlightContested,v=>Apply(options with{HighlightContested=v}));
        Number(tactics,"Territory opacity",options.TerritoryOpacity,.03,.25,v=>Apply(options with{TerritoryOpacity=v}));
        var webOptions=new StackPanel {Margin=new Thickness(0,6,0,8)};content.Children.Add(new Expander {Header="Spider webs",Content=webOptions});
        Number(webOptions,"Maximum shared webs",options.MaximumSharedWebs,1,8,v=>Apply(options with{MaximumSharedWebs=(int)v}));
        Number(webOptions,"Construction intensity",options.WebConstructionRate,.25,2,v=>Apply(options with{WebConstructionRate=v}));
        var trapping=new CheckBox {Content="Enable functional web trapping",IsChecked=options.FunctionalWebTraps,Margin=new Thickness(0,5,0,5)};webOptions.Children.Add(trapping);
        trapping.Click+=(_,_)=>Apply(options with{FunctionalWebTraps=trapping.IsChecked==true});
        Number(content,"Population soft cap",options.SoftCap,24,239,v=>Apply(options with{SoftCap=(int)v}));
        Number(content,"Population hard cap",options.HardCap,36,240,v=>Apply(options with{HardCap=(int)v}));
        Number(content,"Wave interval (seconds)",options.SpawnInterval,.3,2,v=>Apply(options with{SpawnInterval=v}));
        Number(content,"Soldiers per group",options.GroupSize,3,6,v=>Apply(options with{GroupSize=(int)v}));
        var fps=new ComboBox {ItemsSource=new[]{30,60},SelectedItem=options.TargetFps,Margin=new Thickness(0,6,0,6),ToolTip="Target frame rate"};
        Field(content,"Target frame rate",fps);fps.SelectionChanged+=(_,_)=> {if(fps.SelectedItem is int value)Apply(options with{TargetFps=value});};
        var scoreboard=new CheckBox {Content="Show compact battlefield scoreboard",IsChecked=options.ShowScoreboard,Margin=new Thickness(0,7,0,10)};content.Children.Add(scoreboard);
        scoreboard.Click+=(_,_)=>Apply(options with{ShowScoreboard=scoreboard.IsChecked==true});
        var advanced=new StackPanel();content.Children.Add(new Expander {Header="Species balance (advanced)",Content=advanced});
        foreach(var species in BugHabitats.Species)
        {
            var section=new StackPanel {Margin=new Thickness(0,4,0,10)};
            advanced.Children.Add(new Expander {Header=species.ToString(),Content=section});
            var stats=options.Species![species];
            void Stat(string label,double value,double min,double max,Func<SpeciesCombatStats,double,SpeciesCombatStats> update)=>Number(section,label,value,min,max,v=>
            {
                var table=new Dictionary<BugSpecies,SpeciesCombatStats>(options.Species!);table[species]=update(table[species],v);Apply(options with{Species=table});
            });
            Stat("Maximum health",stats.Health,5,150,(s,v)=>s with{Health=v});
            Stat("Attack damage",stats.Damage,1,30,(s,v)=>s with{Damage=v});
            Stat("Attack cooldown",stats.AttackCooldown,.25,4,(s,v)=>s with{AttackCooldown=v});
            Stat("Movement speed",stats.Speed,25,240,(s,v)=>s with{Speed=v});
            Stat("Detection range",stats.DetectionRange,60,400,(s,v)=>s with{DetectionRange=v});
            Stat("Attack range",stats.AttackRange,8,45,(s,v)=>s with{AttackRange=v});
            Stat("Ability cooldown",stats.AbilityCooldown,2,20,(s,v)=>s with{AbilityCooldown=v});
            Stat("Leader health",stats.LeaderHealth,150,1200,(s,v)=>s with{LeaderHealth=v});
            Stat("Reinforcement interval multiplier",stats.ReinforcementRate,.5,3,(s,v)=>s with{ReinforcementRate=v});
            Stat("Maximum colony units",stats.MaximumUnits,6,35,(s,v)=>s with{MaximumUnits=(int)v});
        }
    }
}
