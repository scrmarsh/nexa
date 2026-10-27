from pathlib import Path
import shutil
root=Path(r'M:\Nexa');stage=Path(__file__).parent
backup=Path(r'M:\Aevoria-old\Nexa-embedded-20261026\before-new-ecosystem');backup.mkdir(parents=True,exist_ok=True)
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):
    p=root/n;dest=backup/n
    if p.exists() and not dest.exists():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    p.write_text(t,encoding='utf-8')
for n in ['NexaEcosystem.Models.cs','NexaEcosystem.Simulation.cs','NexaEcosystem.Store.cs','NexaEcosystem.Window.cs','BugVisuals.cs','BugOverlay.cs']:write(n,(stage/n).read_text(encoding='utf-8'))
write('MainWindow.Bugs.cs',(stage/'MainWindow.Ecosystem.cs').read_text(encoding='utf-8'))
write('SettingsDialog.Bugs.cs',(stage/'SettingsDialog.Ecosystem.cs').read_text(encoding='utf-8'))
text=read('DashboardBugs.cs');text=text[:text.index('internal sealed class BugOverlay')]
text=text.replace('internal sealed class BugManager','internal sealed partial class BugManager')
text=text.replace('BugDensity Density=BugDensity.Normal);','BugDensity Density=BugDensity.Normal, EcosystemOptions? Ecosystem=null);')
text=text.replace('internal readonly Guid InteractionId','internal Guid InteractionId')
text=text.replace('    internal BugSpecies Species;','''    internal double Scale=2, Health=100, Energy=1, Carrying, OffscreenTime;
    internal int Kills;
    internal BugActivity Activity;
    internal BugPersonality Personality;
    internal Guid? ResourceId, EnemyId;
    internal BugSpecies Species;''',1)
text=text.replace('internal double SizeScale=>Species switch','internal double SizeScale=>Scale*(Species switch')
text=text.replace('BugSpecies.Centipedes=>1.4,_=>1.25};','BugSpecies.Centipedes=>1.4,_=>1.25});')
text=text.replace('        Preferences=preferences;Clear();','        Preferences=preferences; if(!EcosystemActive) Clear();')
text=text.replace('    internal void Advance(double dt)\n    {','    internal void Advance(double dt)\n    {\n        if(EcosystemActive){AdvanceEcosystem(dt);return;}')
text=text.replace('if(!Bugs.Contains(bug)||bug.State','if(bug.Health<=0||!Bugs.Contains(bug)||bug.State')
write('DashboardBugs.cs',text)
text=read('Models.cs')
text=text.replace('&& Enum.IsDefined(density) ? density : BugDensity.Normal);','&& Enum.IsDefined(density) ? density : BugDensity.Normal,\n                values.TryGetValue("EcosystemSettings",out var eco) ? JsonSerializer.Deserialize<EcosystemOptions>(eco,Options)?.Safe() : new());')
text=text.replace('        values["AddBugs"] = bugs.Enabled.ToString();','        values["EcosystemSettings"] = JsonSerializer.Serialize((bugs.Ecosystem ?? new()).Safe(),Options);\n        values["AddBugs"] = bugs.Enabled.ToString();')
write('Models.cs',text)
text=read('NexaReliability.cs').replace('    public bool SeasonalDecorations { get; set; } = true;','''    public bool SeasonalDecorations { get; set; } = true;
    public bool SeasonalAnimation { get; set; } = true;
    public bool SpookySurprises { get; set; } = true;
    public bool SeasonalSounds { get; set; }
    public bool HalloweenBugCostumes { get; set; } = true;
    public bool CelebrationEffects { get; set; } = true;
    public bool ReduceMotion { get; set; }''')
write('NexaReliability.cs',text)
text=read('App.xaml.cs').replace('protected override void OnExit(ExitEventArgs e) {','protected override void OnExit(ExitEventArgs e) { NexaEcosystem.FlushAll();')
write('App.xaml.cs',text)
text=read('NexaWindowBounds.cs').replace('var full = window is MainWindow main && main.IsFullscreen;','var full = window is MainWindow main && main.IsFullscreen || window is EcosystemWindow ecosystem && ecosystem.IsFullscreen;')
write('NexaWindowBounds.cs',text)
text=read('Nexaween.cs').replace('        Theme.SetSeasonalEnabled(active);','        Theme.SetSeasonalEnabled(active);\n        NexaEcosystem.UpdateSeason(store, Options);',1)
text=text.replace('Draw(dc, count % 13);','Draw(dc, count % 15);')
needle='            default: // Web'
text=text.replace(needle,'''            case 13: // Candy bucket
                dc.DrawEllipse(null, P("#A855F7", 4), new(32, 24), 20, 20);
                Shape(dc, "M10,25 L54,25 L49,60 L15,60 Z", "#F97316", "#9A3412");
                dc.DrawEllipse(B("#24152F"), null, new(32,25), 22,6);
                for(var i=0;i<5;i++) {dc.PushTransform(new RotateTransform(i*21-40,32,20));dc.DrawRoundedRectangle(B(i%2==0?"#84CC16":"#A855F7"),P("#D9F99D",1),new Rect(20+i*3,10,8,16),2,2);dc.Pop();}
                Shape(dc, "M20,39 L25,34 L28,41 Z M37,41 L41,34 L46,39 Z M21,49 Q33,59 45,48 Z", "#24152F"); break;
            case 14: // Standalone green slime
                Shape(dc,"M4,52 Q9,41 18,48 Q22,31 30,44 Q37,37 41,48 Q56,39 61,53 Q61,63 32,61 Q4,63 4,52 Z","#65A30D");
                Shape(dc,"M10,50 Q19,46 24,51 Q31,42 36,50 Q46,46 54,53 Q29,58 10,50 Z","#84CC16");
                dc.DrawEllipse(B("#D9F99D"),null,new(27,30),4,4);dc.DrawEllipse(B("#84CC16"),null,new(42,22),3,3); break;
'''+needle)
write('Nexaween.cs',text)
# Correct small issues in the draft before it enters the working build.
text=read('NexaEcosystem.Models.cs').replace('Nests.Any(n => !species.Contains','Nests.Any(n => n == null || !species.Contains')
write('NexaEcosystem.Models.cs',text)
text=read('NexaEcosystem.Simulation.cs')
text=text.replace('var nest = Nest(bug.Species); bug.Position = nest.Position + new Vector(Range(-35, 35), Range(-35, 35)); bug.Surface = null;\n            if (Bugs.Count <= 6 && Preferences.Species == BugSpecies.AllBugs) bug.Species = AllSpecies[(Bugs.Count - 1) % 6];','''if (Bugs.Count <= 6 && Preferences.Species == BugSpecies.AllBugs) bug.Species = AllSpecies[(Bugs.Count - 1) % 6];
            bug.Speed = bug.Species switch { BugSpecies.Ants => Range(18,34), BugSpecies.Roaches => Range(75,125), BugSpecies.Ladybugs => Range(22,38), BugSpecies.Centipedes => Range(38,58), _ => Range(50,90) };
            var nest = Nest(bug.Species); bug.Position = nest.Position + new Vector(Range(-35, 35), Range(-35, 35)); bug.Surface = null;''')
text=text.replace('BugActivity.Sleeping or BugActivity.Grooming or BugActivity.Socializing','BugActivity.Sleeping or BugActivity.Grooming')
text=text.replace('            if (bug.Activity == BugActivity.Hiding)', '            if (bug.Activity == BugActivity.Socializing && (bug.Target-bug.Position).Length < 30) { bug.Energy=Math.Min(1,bug.Energy+step*.01); bug.State=BugState.Paused; continue; }\n            if (bug.Activity == BugActivity.Hiding)')
text=text.replace('if (prey != null && (prey.Position - bug.Position).Length < 180) { bug.Activity', 'if (prey != null && (prey.Position - bug.Position).Length < 180) { NaturalEncounters++; bug.Activity')
text=text.replace('{ damage *= .25; NaturalEncounters++; }','{ damage *= .25; }')
text=text.replace('var mode = EcosystemSettings.Behavior; var roll = random.NextDouble();','var mode = EcosystemSettings.Behavior; var roll = random.NextDouble(); if (bug.Personality == BugPersonality.Social && mode == BugBehavior.Cozy) roll *= .75;')
text=text.replace('bug.EnemyId = enemy.InteractionId; bug.Target = enemy.Position; bug.Decision = Range(2, 5); return;', '''bug.EnemyId = enemy.InteractionId; bug.Target = enemy.Position; bug.Decision = Range(2, 5);
                foreach(var ally in Bugs.Where(b=>b!=bug && b.Species==bug.Species && b.Health>0 && b.Activity!=BugActivity.Defending && (b.Position-bug.Position).Length<200).Take(2))
                {ally.Activity=BugActivity.Raiding;ally.EnemyId=enemy.InteractionId;ally.Target=enemy.Position;ally.Decision=4;}
                return;''')
write('NexaEcosystem.Simulation.cs',text)
text=read('NexaEcosystem.Store.cs').replace('    internal int ViewCount => views.Count;','    internal int ViewCount => views.Count;\n    internal TimeSpan FrameInterval => timer.Interval;\n    private NexaOptions seasonalOptions = new();')
text=text.replace('        Manager.Configure(preferences); ApplySeason','        preferences = preferences with { Ecosystem = preferences.Ecosystem ?? loaded?.Options ?? new() };\n        Manager.Configure(preferences); ApplySeason')
text=text.replace('        SetCadence(); timer.Tick += Tick;','        SetCadence(); timer.Tick += Tick;\n        if (!blocked && !File.Exists(FilePath)) Save();')
text=text.replace('        SetCadence(); Save(); Changed?.Invoke();','        SetCadence(); ApplySeason(seasonalOptions); Changed?.Invoke();')
text=text.replace('        var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);','        seasonalOptions = options;\n        var active = NexaSeason.Active(options, DateTimeOffset.UtcNow);')
write('NexaEcosystem.Store.cs',text)
text=read('NexaEcosystem.Window.cs').replace('        var header = new DockPanel { LastChildFill = true };\n','').replace('viewport.SetResourceReference(BackgroundProperty,','viewport.SetResourceReference(Border.BackgroundProperty,')
write('NexaEcosystem.Window.cs',text)
text=read('BugVisuals.cs').replace('        var key = (species, appearance);','        if (appearance == BugAppearance.Normal) return BugArtwork.For(new BugEntity { Species = species });\n        var key = (species, appearance);',1)
text=text.replace('Shape(dc, $"M{x - 2},-3 L{x + 1},-4 L{x + 2},0 L{x + 1},4 L{x - 2},3 Z",','Shape(dc, FormattableString.Invariant($"M{x - 2},-3 L{x + 1},-4 L{x + 2},0 L{x + 1},4 L{x - 2},3 Z"),')
write('BugVisuals.cs',text)
print('Connected the independent shared ecosystem, behavior simulation, static styles/costumes, settings and native ecosystem view.')
