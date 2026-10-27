from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'SettingsDialog.Navigation.cs',r/'SettingsDialog.Navigation.cs')
p=r/'SettingsDialog.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('        AddPage(nav, "Appearance",');b=t.index('        Content = root;',a)
t=t[:a]+'''        SessionInterface=initialSection is "Session" or "Tips";
        if(SessionInterface)
        {
            AddPage(nav,"Session",BuildSession());
            AddPage(nav,"Time Zone & Clock",BuildTimeZoneClock());
        }
        else
        {
            AddPage(nav,"Appearance",NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)?AppearanceExplanation():BuildAppearance());
            AddPage(nav,"Sounds",BuildSounds());AddPage(nav,"Time Zone & Clock",BuildTimeZoneClock());
            AddPage(nav,"Widgets",BuildWidgetPreferences());AddPage(nav,"Profile",BuildProfile());
            AddPage(nav,"Integrations",BuildIntegrations());AddPage(nav,"Updates",BuildUpdates());AddPage(nav,"Add Bugs",BuildBugs());
            AddPage(nav,"Account & Data",ComingSoon("Account & Data","Optional account tools are in development. Local import/export remains available."));
        }
'''+t[b:]
t=t.replace('tab.Click += (_, _) => SelectSection(name);','tab.Click += (_, _) => {if(name=="Appearance" && NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)){ExplainLockedAppearance();return;}SelectSection(name);}; if(name=="Appearance" && NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow))tab.Opacity=.42;')
t=t.replace('        var hedy=name=="Hedy";', '        var tips=name=="Tips";if(tips)name="Session";\n        var hedy=name=="Hedy";').replace('if(!pages.ContainsKey(name)) name="Appearance";','if(!pages.ContainsKey(name)) name=SessionInterface?"Session":"Appearance";').replace('        if(hedy) Dispatcher.BeginInvoke','        if(tips)FocusTips();\n        if(hedy) Dispatcher.BeginInvoke')
# Move sounds out of Appearance and merge the effects controls into this page.
a=t.index('        {\n            var sounds =',t.index('private UIElement BuildAppearance'));b=t.index('        var selectedMode',a);t=t[:a]+t[b:]
needle='        return page;\n    }\n    private UIElement BuildHedy()'
t=t.replace(needle,'''        var effects=(StackPanel)BuildEffects();foreach(var child in effects.Children.Cast<UIElement>().ToArray()){effects.Children.Remove(child);page.Children.Add(child);}
        return page;
    }
    private UIElement BuildHedy()''')
p.write_text(t,encoding='utf-8-sig')
p=r/'SettingsDialog.Session.cs';t=p.read_text(encoding='utf-8-sig');tail=t[t.index('        page.Children.Add(new Border { Height = 1,'):];tail=tail.replace('page.Children.Add','spaceAppearance.Children.Add').replace('Field(page,','Field(spaceAppearance,')
tail=tail.replace('        return new ScrollViewer', '        page.Children.Add(BuildTipsSetting());\n        return new ScrollViewer')
head='''using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace SessionDashboard;
internal sealed partial class SettingsDialog
{
    private UIElement BuildSession()
    {
        var page=Page("Session","Schedule, save and review the current dashboard. Recurring days are edited only in Pixel Timer.");
        var weekly=new Button{Content="Edit Pixel Timer schedule",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        weekly.Click+=(_,_)=>{if(!FlushPendingChanges())return;Close();space?.ShowWeeklySchedule();};page.Children.Add(weekly);
        var history=new Button{Content="Notes & History",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};
        history.Click+=(_,_)=>{if(!FlushPendingChanges())return;Close();space?.ShowNotesHistory();};page.Children.Add(history);
        var save=new Button{Content="Save Now",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};save.Click+=(_,_)=>{space?.SaveNow();};page.Children.Add(save);
        var saving=(ScrollViewer)BuildSavingHistory();var content=(UIElement)saving.Content;saving.Content=null;page.Children.Add(content);
        var cap=new Slider{Minimum=1.5,Maximum=10,Value=Math.Clamp(ReliabilityOptions.StreakMultiplierCap,1.5,10),TickFrequency=.5,IsSnapToTickEnabled=true};Field(page,"Maximum squash streak multiplier",cap);
        var capText=new TextBlock{Text=cap.Value.ToString("0.0")+"×"};page.Children.Add(capText);cap.ValueChanged+=(_,_)=>{ReliabilityOptions.StreakMultiplierCap=cap.Value;capText.Text=cap.Value.ToString("0.0")+"×";QueueSave("streak-limit",SaveState);};
        var distraction=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.DistractionPenalty};Field(page,"Points deducted per new distraction (0 disables)",distraction);
        var alcohol=new ComboBox{ItemsSource=new[]{0,5,10,20,50,100},SelectedItem=ReliabilityOptions.AlcoholPenalty};Field(page,"Points deducted per logged alcoholic drink (0 disables)",alcohol);
        distraction.SelectionChanged+=(_,_)=>{ReliabilityOptions.DistractionPenalty=(int)(distraction.SelectedItem??0);QueueSave("penalties",SaveState);};alcohol.SelectionChanged+=(_,_)=>{ReliabilityOptions.AlcoholPenalty=(int)(alcohol.SelectedItem??0);QueueSave("penalties",SaveState);};
        var status=new TextBlock{TextWrapping=TextWrapping.Wrap};page.Children.Add(status);
        var spaceAppearance=new StackPanel{IsEnabled=!NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow)};page.Children.Add(spaceAppearance);
'''
p.write_text(head+tail,encoding='utf-8-sig')
edit('SettingsDialog.Reliability.cs',lambda t:t[:t.index('        if (space != null)')]+'''        if(space!=null){var finish=new Button{Content="Complete postponed reset",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,16,0,8)};finish.Click+=(_,_)=>space.CompleteDeferredReset();page.Children.Add(finish);}
'''+t[t.index('        return new ScrollViewer'): ] if False else t)
# Remove the duplicate recurrence controls without changing the time-zone page.
p=r/'SettingsDialog.Reliability.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('        if (space != null)');b=t.index('        return new ScrollViewer',a);t=t[:a]+'''        if(space!=null){var finish=new Button{Content="Complete postponed reset",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,16,0,8)};finish.Click+=(_,_)=>space.CompleteDeferredReset();page.Children.Add(finish);}
'''+t[b:]
a=t.index('        var tips = new CheckBox',t.index('private UIElement BuildEffects'));b=t.index('        var reduced=',a);t=t[:a]+t[b:];t=t.replace('Appearance & Effects','Effects').replace('Quick tips are optional. Important warnings and save errors remain enabled.','Control interface motion and celebrations. Tips are configured in Session.');p.write_text(t,encoding='utf-8-sig')
edit('NexaPanels.cs',lambda t:t.replace('''            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(close); settings.AutoSaveStatus.Margin = new Thickness(0, 0, 18, 0);
            settings.AutoSaveStatus.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(settings.AutoSaveStatus); content = row;''','''            content=settings.BuildSettingsFooter(close);'''))
edit('MainWindow.xaml',lambda t:'\n'.join(line for line in t.split('\n') if 'Content="Notes &amp; History"' not in line))
edit('MainWindow.NotesHistory.cs',lambda t:t.replace('    private void NotesHistoryClick','    internal void ShowNotesHistory()=>NotesHistoryClick(this,new RoutedEventArgs());\n    private void NotesHistoryClick'))
edit('MainWindow.Settings.cs',lambda t:t.replace('''        if (settingsWindow is { IsVisible: true }) { settingsWindow.SelectSection(section); settingsWindow.Activate(); return; }''','''        if(settingsWindow is {IsVisible:true})
        {
            if(settingsWindow.SessionInterface==(section is "Session" or "Tips")){settingsWindow.SelectSection(section);settingsWindow.Activate();return;}
            if(!settingsWindow.FlushPendingChanges())return;settingsWindow.Close();
        }'''))
edit('MainWindow.Navigation.cs',lambda t:t.replace('seconds: 11);','seconds:11,action:()=>OpenMainSettings("Tips"),actionLabel:"Tip settings");'))
edit('NexaNotifications.cs',lambda t:t.replace('entries[key] = new(card, glyph, message,','if(key=="tip" && action!=null)card.MouseLeftButtonUp+=(_,e)=>{if(e.OriginalSource is not Button){action();e.Handled=true;}};\n        entries[key] = new(card, glyph, message,'))
edit('Nexaween.cs',lambda t:t.replace('''        Field(page, "Halloween", mode);''','''        Field(page,"Nexaween enabled",MakeSeasonSwitch());''').replace('mode.SelectionChanged += (_, _) => Save();','').replace('ReliabilityOptions.SeasonalMode = mode.SelectedItem?.ToString() ?? "Automatic";',''))
print('Consolidated Session access/history/tips/saving, merged Appearance, added Sounds and separate pumpkin/toggle controls.')
