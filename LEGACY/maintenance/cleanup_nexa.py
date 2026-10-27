from pathlib import Path
import shutil, re, json

root=Path(r'M:\Session Dashboard')
archive=Path(r'M:\Aevoria-old\Nexa-embedded-20261026')
assert root.resolve()==Path(r'M:\Session Dashboard') and archive.resolve().is_relative_to(Path(r'M:\Aevoria-old'))
archive.mkdir(parents=True,exist_ok=True)
snapshot=archive/'source-before-cleanup'
snapshot.mkdir(exist_ok=True)
for p in root.iterdir():
    if p.is_file() and p.suffix in {'.cs','.csproj','.xaml','.md','.ps1','.bat','.manifest'}:
        dest=snapshot/p.name
        if not dest.exists(): shutil.copy2(p,dest)

def read(name): return (root/name).read_text(encoding='utf-8-sig')
def write(name,text): (root/name).write_text(text,encoding='utf-8')
def method(text,signature):
    start=text.index(signature); brace=text.index('{',start); depth=1; end=brace+1
    while depth:
        if text[end]=='{': depth+=1
        elif text[end]=='}': depth-=1
        end+=1
    return text[start:end]
def remove_method(text,signature): return text.replace(method(text,signature),'')

original=read('DashboardBugs.cs')
helpers=[]
for sig in ['private double Range(', 'private Point FreePoint(', 'private void Detach(', 'private bool PickSurface(', 'internal static Point EdgePoint(', 'private BugEntity Spawn(', 'private BugSpecies MixedSpecies(', 'private void Decide(', 'internal BugEntity? Hit(']:
    value=method(original,sig)
    value='\n'.join(line for line in value.splitlines() if 'IsArcade' not in line)
    helpers.append(value)
header='''using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace SessionDashboard;
internal enum BugSpecies { Ants, Roaches, Flies, AllThree, Spiders, AllBugs, Ladybugs, Centipedes }
internal enum BugDensity { Low, Normal, Infestation }
internal enum BugVariant { None, QueenRoach, QueenFly, QueenLadybug, MamaSpider, KingCentipede }
internal enum BugState { Edge, Crossing, Flying, Landed, Paused, Hiding, Squashed, Bursting, Trapped }
internal sealed record BugPreferences(bool Enabled=false, BugSpecies Species=BugSpecies.Ants, BugDensity Density=BugDensity.Normal);
internal readonly record struct BugSurface(string Id, Rect Bounds);
internal sealed class BugEntity
{
    internal readonly Guid InteractionId=Guid.NewGuid();
    internal BugSpecies Species;
    internal BugVariant Variant;
    internal BugState State;
    internal Point Position, Target;
    internal double Angle,Speed,Decision,EdgeProgress,SquashAge,HitFlash,CrawlPhase,Age;
    internal int Direction=1,Hits;
    internal Vector BurstVelocity;
    internal double BurstRemaining,ExcessLifetime;
    internal string? Surface;
    internal double SizeScale=>Species switch {BugSpecies.Ants=>.75,BugSpecies.Spiders=>1.65,BugSpecies.Roaches=>1.45,BugSpecies.Centipedes=>1.4,_=>1.25};
}
internal sealed class BugSplat { internal Point Position; internal BugSpecies Species; internal double Age; }
internal sealed class BugManager
{
    private readonly Random random;
    internal readonly List<BugEntity> Bugs=[];
    internal readonly List<BugSplat> Splats=[];
    internal readonly Dictionary<string,Rect> Surfaces=[];
    private bool surfaceChoicesDirty=true;
    private BugSurface[] surfaceChoices=[];
    private double replacementDelay;
    internal BugPreferences Preferences {get;private set;}=new();
    internal Size Bounds;
    internal Rect VisibleBounds {get;set;}
    internal bool SpawnPressure;
    internal const int InfestationCap=600;
    internal int Cap=>Preferences.Species==BugSpecies.Centipedes ? Math.Max(1,BaseCap/12) : BaseCap;
    private int BaseCap=>Preferences.Density switch {BugDensity.Low=>9,BugDensity.Infestation=>InfestationCap,_=>24};
    internal double SpawnInterval=>(Preferences.Density==BugDensity.Infestation?.08:.65)*(Preferences.Species==BugSpecies.Centipedes?6:1)*(Preferences.Density==BugDensity.Low?2:1)*(SpawnPressure?3:1);
    internal BugManager(int? seed=null) {random=seed is {} value ? new Random(value) : new Random();}
    internal void Configure(BugPreferences preferences)
    {
        preferences=preferences with {Species=preferences.Species==BugSpecies.AllThree?BugSpecies.AllBugs:preferences.Species};
        if(Preferences==preferences)return;
        Preferences=preferences;Clear();
    }
    internal void Clear(){Bugs.Clear();Splats.Clear();replacementDelay=0;}
    internal void SetSurfaces(IEnumerable<BugSurface> surfaces,Size bounds)
    {Bounds=bounds;Surfaces.Clear();foreach(var s in surfaces)Surfaces[s.Id]=s.Bounds;surfaceChoicesDirty=true;}
'''
advance='''
    internal void Advance(double dt)
    {
        if(!Preferences.Enabled || Bounds.Width<30 || Bounds.Height<30)return;
        dt=Math.Clamp(dt,0,.08);replacementDelay=Math.Max(-.08,replacementDelay-dt);
        for(var count=0;replacementDelay<=0 && Bugs.Count<Cap && count<2;count++){Spawn();replacementDelay+=SpawnInterval;}
        for(var i=Splats.Count-1;i>=0;i--){Splats[i].Age+=dt;if(Splats[i].Age>=4)Splats.RemoveAt(i);}
        for(var i=Bugs.Count-1;i>=0;i--)
        {
            var bug=Bugs[i];bug.Age+=dt;
            if(bug.State==BugState.Squashed)
            {
                bug.SquashAge+=dt;
                if(bug.SquashAge>=.16){if(Splats.Count>=60)Splats.RemoveAt(0);Splats.Add(new(){Position=bug.Position,Species=bug.Species});Bugs.RemoveAt(i);replacementDelay=Math.Max(replacementDelay,1.5);}
                continue;
            }
            bug.HitFlash=Math.Max(0,bug.HitFlash-dt);
            if(bug.Surface is {} missing && !Surfaces.ContainsKey(missing))Detach(bug);
            bug.Decision-=dt;if(bug.Decision<=0)Decide(bug);
            var old=bug.Position;
            if(bug.State==BugState.Edge && bug.Surface is {} key && Surfaces.TryGetValue(key,out var rect))
            {bug.EdgeProgress+=bug.Direction*bug.Speed*dt/Math.Max(1,2*(rect.Width+rect.Height));bug.Position=EdgePoint(rect,bug.EdgeProgress);}
            else if(bug.State is BugState.Flying or BugState.Crossing)
            {
                if(bug.Surface is {} id && Surfaces.TryGetValue(id,out var target))bug.Target=EdgePoint(target,bug.EdgeProgress);
                var delta=bug.Target-bug.Position;
                if(delta.Length<=bug.Speed*dt+1)
                {
                    bug.Position=bug.Target;
                    if(bug.Surface!=null){bug.State=bug.Species==BugSpecies.Flies?BugState.Landed:bug.Species==BugSpecies.Roaches?BugState.Hiding:BugState.Edge;bug.Decision=bug.Species==BugSpecies.Roaches?Range(.6,2.2):Range(1,3);}
                    else {bug.Target=FreePoint();bug.Decision=Range(.3,1);}
                }
                else {delta.Normalize();bug.Position+=delta*bug.Speed*dt;}
            }
            var movement=bug.Position-old;
            if(movement.LengthSquared>.001){bug.Angle=Math.Atan2(movement.Y,movement.X)*180/Math.PI;bug.CrawlPhase+=movement.Length*.35;}
            bug.Position=new(Math.Clamp(bug.Position.X,7,Bounds.Width-7),Math.Clamp(bug.Position.Y,7,Bounds.Height-7));
        }
    }
    internal bool Squash(BugEntity bug)
    {
        if(!Bugs.Contains(bug)||bug.State is BugState.Squashed or BugState.Hiding)return false;
        var squash=bug.Hits>=(bug.Species==BugSpecies.Roaches?4:bug.Species==BugSpecies.Ladybugs?1:0);bug.Hits++;
        if(!squash){bug.HitFlash=.35;bug.State=BugState.Paused;bug.Surface=null;bug.Decision=.4;return false;}
        bug.State=BugState.Squashed;bug.Surface=null;
        foreach(var other in Bugs)if(other!=bug && other.State!=BugState.Squashed && (other.Position-bug.Position).LengthSquared<6400)Detach(other);
        return true;
    }
}
internal sealed class BugOverlay : FrameworkElement
{
    internal event Action<BugEntity>? DirectSquashed;
    internal BugManager Manager {get;}=new();
    internal Point ContentOrigin;
    internal double LastRenderMilliseconds {get;private set;}
    internal long RenderedFrames {get;private set;}
    internal void SetViewOrigin(Point origin){ContentOrigin=origin;RefreshFrame();}
    internal Point ScreenToWorld(Point point)=>point-(Vector)ContentOrigin;
    internal BugOverlay(){ClipToBounds=true;Focusable=false;}
    protected override HitTestResult? HitTestCore(PointHitTestParameters p)=>Manager.Hit(ScreenToWorld(p.HitPoint))!=null?new PointHitTestResult(this,p.HitPoint):null;
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {if(Manager.Hit(ScreenToWorld(e.GetPosition(this))) is {} bug){if(Manager.Squash(bug))DirectSquashed?.Invoke(bug);e.Handled=true;RefreshFrame();}}
    internal void RefreshFrame()=>InvalidateVisual();
    protected override void OnRender(DrawingContext dc)
    {
        var start=System.Diagnostics.Stopwatch.GetTimestamp();RenderedFrames++;
        var view=new Rect(-180-ContentOrigin.X,-180-ContentOrigin.Y,ActualWidth+360,ActualHeight+360);
        foreach(var splat in Manager.Splats)
        {if(!view.Contains(splat.Position))continue;dc.PushOpacity(.6*Math.Clamp((4-splat.Age)/2,0,1));dc.DrawEllipse(BugArtwork.Ant,null,splat.Position+(Vector)ContentOrigin,splat.Species==BugSpecies.Centipedes?9:4,2);dc.Pop();}
        foreach(var bug in Manager.Bugs)
        {
            if(bug.State==BugState.Hiding||!view.Contains(bug.Position))continue;
            var sx=bug.SizeScale*(bug.State==BugState.Squashed?1+bug.SquashAge*2:1);
            var sy=bug.SizeScale*(bug.State==BugState.Squashed?Math.Max(.15,1-bug.SquashAge*5):1);
            var p=bug.Position+(Vector)ContentOrigin;var angle=bug.Angle*Math.PI/180;
            dc.PushTransform(new MatrixTransform(new Matrix(sx,0,0,sy,0,0)*new Matrix(Math.Cos(angle),Math.Sin(angle),-Math.Sin(angle),Math.Cos(angle),p.X,p.Y)));
            dc.DrawDrawing(BugArtwork.For(bug));dc.Pop();
            if(bug.HitFlash>0)dc.DrawEllipse(null,BugArtwork.HitRing,p,10*bug.SizeScale,9*bug.SizeScale);
        }
        LastRenderMilliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
    }
}
'''
write('DashboardBugs.cs',header+'\n'.join(helpers)+advance)
write('MainWindow.Bugs.cs','''using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    internal BugOverlay? bugsOverlay;
    private readonly DispatcherTimer bugsTicker=new(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(31)};
    private readonly Stopwatch bugsClock=new();
    private double bugLastFrameAt;
    internal double BugUpdateMilliseconds {get;private set;}
    internal TimeSpan BugFrameInterval=>bugsTicker.Interval;
    internal long BugFrameCount {get;private set;}
    internal bool BugsRunning=>bugsTicker.IsEnabled;
    private void InitializeBugs()
    {
        bugsTicker.Tick+=BugsTick;Loaded+=(_,_)=>ApplyBugPreferences(store.LoadBugPreferences());
        IsVisibleChanged+=(_,_)=>UpdateBugActivity();StateChanged+=(_,_)=>UpdateBugActivity();
        DashboardShell.IsVisibleChanged+=(_,_)=>UpdateBugActivity();Closed+=(_,_)=>RemoveBugs();
    }
    internal void ApplyBugPreferences(BugPreferences preferences)
    {
        if(!preferences.Enabled){RemoveBugs();return;}
        if(bugsOverlay==null)
        {
            bugsOverlay=new BugOverlay();bugsOverlay.DirectSquashed+=TrackDirectSquash;
            Panel.SetZIndex(bugsOverlay,40);((Grid)Content).Children.Add(bugsOverlay);
        }
        bugsOverlay.Manager.Configure(preferences);UpdateBugActivity();
    }
    private void UpdateBugActivity()
    {
        var active=bugsOverlay!=null && IsVisible && WindowState!=WindowState.Minimized && DashboardShell.IsVisible;
        if(bugsOverlay!=null)bugsOverlay.Visibility=active?Visibility.Visible:Visibility.Collapsed;
        if(active && !bugsTicker.IsEnabled){bugLastFrameAt=0;bugsClock.Restart();bugsTicker.Start();}
        else if(!active){bugsTicker.Stop();bugsClock.Reset();}
    }
    private void BugsTick(object? sender,EventArgs e)
    {
        if(bugsOverlay==null)return;var began=Stopwatch.GetTimestamp();var now=bugsClock.Elapsed.TotalSeconds;
        var dt=Math.Clamp(now-bugLastFrameAt,0,.08);bugLastFrameAt=now;
        var origin=WidgetCanvas.TransformToVisual(bugsOverlay).Transform(new Point());bugsOverlay.SetViewOrigin(origin);
        var visible=WidgetScroller.TransformToVisual(WidgetCanvas).TransformBounds(new Rect(WidgetScroller.RenderSize));
        bugsOverlay.Manager.VisibleBounds=visible;
        var surfaces=widgetFrames.Select(pair=>new BugSurface(pair.Key.ToString(),new Rect(Canvas.GetLeft(pair.Value),Canvas.GetTop(pair.Value),Math.Max(0,pair.Value.ActualWidth),Math.Max(0,pair.Value.ActualHeight))));
        bugsOverlay.Manager.SetSurfaces(surfaces,new Size(Math.Max(40,WidgetCanvas.ActualWidth),Math.Max(40,WidgetCanvas.ActualHeight)));
        bugsOverlay.Manager.Advance(dt);bugsOverlay.RefreshFrame();BugFrameCount++;
        BugUpdateMilliseconds=Stopwatch.GetElapsedTime(began).TotalMilliseconds;
    }
    private void RemoveBugs()
    {bugsTicker.Stop();bugsClock.Reset();if(bugsOverlay==null)return;bugsOverlay.DirectSquashed-=TrackDirectSquash;((Panel)bugsOverlay.Parent).Children.Remove(bugsOverlay);bugsOverlay=null;}
}
''')
text=read('HomeWindow.cs')
start=text.index('        var bugs=new Button');end=text.index('        footer.Children.Add(left);',start)
text=text[:start]+text[end:]
text=remove_method(text,'    internal BattlefieldWindow? OpenBattlefield(')
write('HomeWindow.cs',text)
text=read('MainWindow.Continuity.cs').replace('        if(!SaveArcadeForClose())return false;','')
text=remove_method(text,'    private bool SaveArcadeForClose(');write('MainWindow.Continuity.cs',text)
text=read('App.xaml.cs').replace('LivingWorlds.Flush();ArcadeWorlds.FlushAll(); ','')
text=text.replace('ReleaseLabel(AppVersion) + (AppVersion == "0.1.24" ? " — NEXAWEEN" : "")','ReleaseLabel(AppVersion)')
text=text.replace('version == "0.1.24" ? "Nexa v0.1.24 Alpha" : $"Nexa {version} alpha"','$"nexa v{version} alpha"')
text=text.replace('e.Args[0] is "--verify-startup" or "--verify-colony"','e.Args[0] == "--verify-startup"').replace(', e.Args[0]=="--verify-colony"','')
write('App.xaml.cs',text)
text=read('Models.cs')
text=text.replace('Enum.IsDefined(density) ? density : BugDensity.Normal,\n                values.TryGetValue("ArcadeSettings",out var arcade) ? JsonSerializer.Deserialize<ArcadeSettings>(arcade,Options)?.Safe() : null)','Enum.IsDefined(density) ? density : BugDensity.Normal)')
text='\n'.join(line for line in text.splitlines() if 'if(bugs.Arcade!=null)' not in line)
write('Models.cs',text)
text=read('MainWindow.Widgets.cs').replace('if(TryStartWebSweep(e))return; ','').replace(' && HabitatVisible(w.Kind)','').replace(' && HabitatVisible(d.Name)','')
text='\n'.join(line for line in text.splitlines() if 'case "habitat":' not in line)
write('MainWindow.Widgets.cs',text)
text=read('Widgets.cs').replace('BugHabitats.KindSpecies','LegacyBugHabitats.KindSpecies')
write('Widgets.cs',text)
oldhab=read('BugHabitats.cs');start=oldhab.index('internal static class BugHabitats');value=method(oldhab,start and 'internal static class BugHabitats')
# Keep only the old widget-name mapping needed to read existing documents.
mapping=value[:value.index('    internal static bool Includes')]+'}\n'
mapping=mapping.replace('class BugHabitats','class LegacyBugHabitats')
write('LegacyBugHabitatMigration.cs','using System.Windows;\nnamespace SessionDashboard;\n'+mapping)
text=read('StartupVerification.cs').replace(', bool arcade = false','')
text='\n'.join(line for line in text.splitlines() if 'if(arcade)' not in line)
text=re.sub(r'ArcadeColonies=window\.bugsOverlay\?\.Manager\.Colonies\.Count\?\?0, ','',text)
write('StartupVerification.cs',text)
text=read('AssemblyInfo.cs');write('AssemblyInfo.cs','\n'.join(line for line in text.splitlines() if 'InternalsVisibleTo("Aevoria")' not in line))
text=read('SessionDashboard.csproj')
text='\n'.join(line for line in text.splitlines() if '<Compile Remove="Aevoria.Desktop' not in line)
write('SessionDashboard.csproj',text)
write('SettingsDialog.Bugs.cs','''using System.Windows;
using System.Windows.Controls;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildBugs()
    {
        var page=Page("Add Bugs","Decorative bugs on your dashboard.");var preferences=store.LoadBugPreferences();
        var enabled=new CheckBox{Content="Add Bugs",IsChecked=preferences.Enabled,Margin=new Thickness(0,12,0,12)};page.Children.Add(enabled);
        var species=new ComboBox{ItemsSource=new[]{BugSpecies.Ants,BugSpecies.Roaches,BugSpecies.Flies,BugSpecies.Spiders,BugSpecies.Ladybugs,BugSpecies.Centipedes,BugSpecies.AllBugs},SelectedItem=preferences.Species};Field(page,"Bug type",species);
        var density=new ComboBox{ItemsSource=new[]{"Low","Normal","High"},SelectedIndex=preferences.Density==BugDensity.Low?0:preferences.Density==BugDensity.Infestation?2:1};Field(page,"Density",density);
        void Changed()
        {
            preferences=new(enabled.IsChecked==true,species.SelectedItem is BugSpecies selected?selected:BugSpecies.Ants,density.SelectedIndex==0?BugDensity.Low:density.SelectedIndex==2?BugDensity.Infestation:BugDensity.Normal);
            foreach(var window in Application.Current.Windows.OfType<MainWindow>().Where(w=>w.SettingsStore.UserId==store.UserId && w.SettingsStore.FilePath==store.FilePath))window.ApplyBugPreferences(preferences);
            QueueSave("bugs",()=>store.SaveBugPreferences(preferences));
        }
        enabled.Click+=(_,_)=>Changed();species.SelectionChanged+=(_,_)=>Changed();density.SelectionChanged+=(_,_)=>Changed();
        if(space!=null){var totals=new Button{Content="Add Bugs Squashed widget",Margin=new Thickness(0,16,0,0)};totals.Click+=(_,_)=>space.AddSquashesWidget();page.Children.Add(totals);}
        return page;
    }
}
''')
text=read('DrinkArtwork.cs').replace('Ale=Gradient("#FFDC48","#FFC226"), Wine=Gradient("#C34C70","#701F40"), Spirit=Gradient("#FFE6A0","#DDAE54")','Ale=Gradient("#C084FC","#7E22CE"), Wine=Gradient("#BEF264","#65A30D"), Spirit=Gradient("#FDBA74","#EA580C")')
text=text.replace('if(FillFraction>.01)dc.DrawLine','if(wine && FillFraction>.01)\n        {foreach(var bubble in new[]{new Point(29,45),new Point(43,31),new Point(39,52),new Point(49,43)})if(bubble.Y>top)dc.DrawEllipse(null,Shine,bubble,2,2);}\n        if(FillFraction>.01)dc.DrawLine')
write('DrinkArtwork.cs',text)

patterns=['Aevoria*.cs','LivingWorld*.cs','ColonyWars*.cs','ArcadeWorld*.cs','BattlefieldWindow*.cs','HistoricalAtlas*.cs','SettingsDialog.ColonyWars.cs','SettingsDialog.ArcadeWorld.cs','DashboardBugs.Arcade.cs','DashboardBugs.Interaction.cs','DashboardBugs.Camera.cs','MainWindow.WebSweep.cs','BugHabitats.cs','ArcadeRandom.cs']
moved=[]
def move(p):
    relative=p.relative_to(root);dest=archive/'removed'/relative
    if dest.exists(): raise RuntimeError(f'Refusing to overwrite {dest}')
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.move(str(p),str(dest));moved.append(str(relative))
for pattern in patterns:
    for p in list(root.glob(pattern)): move(p)
if (root/'Aevoria.Desktop').exists():move(root/'Aevoria.Desktop')
(archive/'manifest.json').write_text(json.dumps({'source':str(root),'moved':moved,'snapshot':str(snapshot)},indent=2))
print(f'Archived {len(moved)} game source entries; retained decorative bugs and legacy widget migration.')
