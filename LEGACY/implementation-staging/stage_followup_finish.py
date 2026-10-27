from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('MainWindow.WidgetPlacement.cs',lambda t:t.replace('''    private void UpdateWidgetDropPreview(WidgetInstance moved, double x, double y)
    {''','''    private void UpdateWidgetDropPreview(WidgetInstance moved, double x, double y)
    {
        RefreshSeasonalWorkspace();'''))
edit('MainWindow.Widgets.cs',lambda t:t.replace('UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);RefreshSeasonalWorkspace();','UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);'))
edit('Widgets.cs',lambda t:t.replace('    public int? TimerDurationSeconds','    public int? SummaryIntervalMinutes {get;set;}\n    public int? TimerDurationSeconds',1).replace('w.TimerMinutes is < 1 or > 10080','w.SummaryIntervalMinutes is < 1 or > 10080 || w.TimerMinutes is < 1 or > 10080'))
edit('MainWindow.SessionWidgets.cs',lambda t:t.replace('''        var interval=new ComboBox{ItemsSource=new[]{5,10,15,20,30,state.Session?.IntervalMinutes??10}.Distinct().Order().ToArray(),SelectedItem=state.Session?.IntervalMinutes??10,ToolTip="Summary interval in minutes"};''','''        var summaryInterval=widget.SummaryIntervalMinutes??state.Session?.IntervalMinutes??10;
        var interval=new ComboBox{ItemsSource=new[]{5,10,15,20,30,summaryInterval}.Distinct().Order().ToArray(),SelectedItem=summaryInterval,ToolTip="Summary interval in minutes"};''').replace('''interval.SelectionChanged+=(_,_)=>{if(state.Session is {} session && interval.SelectedItem is int minutes){session.IntervalMinutes=minutes;IntervalInput.Text=minutes.ToString();BuildTimeline();Changed();SaveNow();}};''','''interval.SelectionChanged+=(_,_)=>{if(interval.SelectedItem is int minutes){foreach(var timeline in state.Widgets.Where(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline"))timeline.SummaryIntervalMinutes=minutes;if(state.Session is {} session)session.IntervalMinutes=minutes;IntervalInput.Text=minutes.ToString();BuildTimeline();Changed();SaveNow();}};'''))
edit('MainWindow.ClassSchedule.cs',lambda t:t.replace('session.IntervalMinutes=schedule.IntervalMinutes;','session.IntervalMinutes=current.Data.Widgets.FirstOrDefault(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline")?.SummaryIntervalMinutes??schedule.IntervalMinutes;'))
edit('MainWindow.xaml.cs',lambda t:t.replace('''    private void BeginSession(Session session)
    {''','''    private void BeginSession(Session session)
    {
        session.IntervalMinutes=state.Widgets.FirstOrDefault(w=>WidgetLibrary.Definition(w.Kind).Editor=="timeline")?.SummaryIntervalMinutes??session.IntervalMinutes;'''))
edit('SettingsDialog.Session.cs',lambda t:t.replace('ReliabilityOptions.StreakMultiplierCap=cap.Value;capText.Text','ReliabilityOptions.StreakMultiplierCap=cap.Value;state.Productivity.FrenzyMultiplier=Math.Min(state.Productivity.FrenzyMultiplier,cap.Value);capText.Text'))
edit('WidgetActivity.cs',lambda t:t.replace('"Task completed", title, 100, DateTimeOffset.UtcNow);','"Task completed", title, 100, DateTimeOffset.UtcNow,widgetId:widget.Id);'))
edit('SessionDashboard.csproj',lambda t:t.replace('NEXAWEEN — Your dashboard just got a little spooky.','NEXAWEEN — A space for whatever haunts you next.'))
p=r/'ReleaseNotes.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('        new("0.1.24"');b=t.index('        new("0.1.23"',a)
t=t[:a]+'''        new("0.1.24", "October 27, 2026", "NEXAWEEN", "A space for whatever haunts you next. Synchronized session progress, reliable saving, a consolidated interface and a seasonal bug ecosystem.",
        [
            "The Dashboard Has Been Haunted — Seasonal branding uses the checked-task accent. The animated Home tagline is retained; only haunts is orange. Candy backgrounds, contextual widget corners and curved vines stay clear of controls.",
            "Time Is Under a Spell — Session pixels fill from the top-left, clear in reverse between classes, and share timestamp-based progress with linked bars. Weekday/date/duration editing, time zones, three-decimal precision and controlled time-picker scrolling.",
            "Creepy Crawlies — Classic, Cozy, Creepy and Combative modes determine appearance together. Species and density update the maintained population. Widget-edge paths, visible flight and progressive spider-owned webs share one persistent ecosystem.",
            "Productivity Witchcraft — Prominent lightning score and centered multiplier. Direct squashes start two-minute streaks with 0.5 steps and a configurable cap. Saved positive actions get green edge feedback; configured negative categories get red feedback.",
            "The Archives Awaken — Central readable history, continuous/event-driven saving, configurable checkpoints, revision protection, saved recovery state and save-before-reset safeguards remain in place.",
            "Spells of Convenience — One Session entry for history, saving and tips; summary interval in Timeline; unified Appearance and separate Sounds. Pumpkin settings and on/off switch, real startup stages, left autosave notices, right tips, and extended dashboard scrolling."
        ]),
'''+t[b:];p.write_text(t,encoding='utf-8-sig')
p=r/'CHANGELOG.md';t=p.read_text(encoding='utf-8-sig');p.write_text('''# Changelog

## Nexa v0.1.24 Alpha — NEXAWEEN

A space for whatever haunts you next.

- **The Dashboard Has Been Haunted:** resource-matched seasonal branding and tagline, candy-only backgrounds, contextual widget corners and curved vines.
- **Time Is Under a Spell:** authoritative elapsed session progress, reverse intersession clearing, linked/independent bars, recurring/date/duration editing, clock alignment and controlled selectors.
- **Creepy Crawlies:** Classic/Cozy/Creepy/Combative, automatic appearance, effective species/density controls, widget avoidance, flight and persistent spider-owned web construction.
- **Productivity Witchcraft:** central signed scoring, prominent score/multiplier, timestamp-based two-minute streaks, bounded half-step multipliers and saved-event perimeter feedback.
- **The Archives Awaken:** retained searchable history, continuous saving, recovery and protected session resets.
- **Spells of Convenience:** consolidated Session/history/tips, unified Appearance, Sounds, pumpkin settings/toggle, genuine splash stages and split floating notifications.

See NEXAWEEN024_IMPLEMENTATION.md for verification evidence and outstanding manual acceptance. Older entries below are retained historical project notes; archived Aevoria game systems are not current Nexa dependencies.

'''+t.removeprefix('# Changelog\n\n'),encoding='utf-8')
print('Finished timeline-owned interval persistence, live attachment hooks and release-note accuracy without changing the version.')
