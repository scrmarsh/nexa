using System.Windows;
using System.Windows.Controls;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private NexaOptions ReliabilityOptions
    {
        get { state.Workspace ??= Continuity.Migrate(state, store.UserId); return state.Workspace.Settings; }
    }
    private UIElement BuildSavingHistory()
    {
        var page = Page("Saving & Session History", "Important edits save promptly. Checkpoints preserve the same session identity. Completed sessions remain visible until the next scheduled reset.");
        var interval = new ComboBox { ItemsSource = new[] { "30 seconds", "1 minute", "3 minutes", "5 minutes", "10 minutes", "15 minutes", "30 minutes" }, SelectedIndex = Array.IndexOf(NexaOptions.SaveIntervals, ReliabilityOptions.EffectiveSaveSeconds) };
        Field(page, "Checkpoint interval", interval);
        interval.SelectionChanged += (_, _) =>
        {
            if (interval.SelectedIndex < 0) return;
            ReliabilityOptions.AutosaveSeconds = NexaOptions.SaveIntervals[interval.SelectedIndex];
            space?.ApplyReliabilityOptions(); QueueSave("checkpoint", SaveState);
        };
        var routine = new CheckBox { Content = "Show routine autosave notifications", IsChecked = ReliabilityOptions.ShowAutosaveNotifications, Margin = new Thickness(0, 16, 0, 8) };
        routine.Click += (_, _) => { ReliabilityOptions.ShowAutosaveNotifications = routine.IsChecked == true; space?.ApplyReliabilityOptions(); QueueSave("save-feedback", SaveState); };
        page.Children.Add(routine);
        page.Children.Add(new TextBlock { Text = "Save failures always remain visible and retry automatically. Manual saving is available in the dashboard header.", TextWrapping = TextWrapping.Wrap });
        if(space!=null){var finish=new Button{Content="Complete postponed reset",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,16,0,8)};finish.Click+=(_,_)=>space.CompleteDeferredReset();page.Children.Add(finish);}
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
    private UIElement BuildTimeZoneClock()
    {
        var page = Page("Time Zone & Clock", "Schedules use local wall times in the selected zone. Elapsed time and saved activity use UTC instants.");
        var choices=TimeZoneCatalog.Choices;
        var search=new TextBox {ToolTip="Search cities, regions, time zone names or identifiers"};
        System.Windows.Automation.AutomationProperties.SetName(search,"Search time zones");
        var zones=new ComboBox {ItemsSource=choices,DisplayMemberPath="Label",IsEditable=false,IsTextSearchEnabled=false,MaxDropDownHeight=260};
        zones.SelectedItem=choices.FirstOrDefault(c=>c.Id==TimeZoneCatalog.Canonical(ReliabilityOptions.TimeZoneId))??choices[0];
        Field(page,"Search time zones",search);Field(page,"Time zone",zones);
        var synchronizing=false;
        search.TextChanged+=(_,_)=>{
            if(synchronizing)return;var selected=zones.SelectedItem;
            zones.ItemsSource=TimeZoneCatalog.Search(search.Text).ToArray();
            if(zones.Items.Contains(selected))zones.SelectedItem=selected;
        };
        zones.SelectionChanged+=(_,_)=>{
            if(synchronizing || zones.SelectedItem is not TimeZoneChoice picked)return;
            synchronizing=true;search.Text="";zones.ItemsSource=choices;zones.SelectedItem=picked;synchronizing=false;
        };
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 14) }; page.Children.Add(status);
        var apply = new Button { Content = "Use selected time zone", HorizontalAlignment = HorizontalAlignment.Left };
        apply.Click += (_, _) =>
        {
            if(zones.SelectedItem is not TimeZoneChoice selected){status.Text="Select a matching time zone.";return;}
            var id = selected.Id;
            try
            {
                if (id != "Automatic") TimeZoneInfo.FindSystemTimeZoneById(id);
                foreach (var scheduled in state.Workspace!.Dashboards.Select(d => d.Current.Data.Session).Append(state.Session).Where(s => s?.AutomaticScheduling == true).Cast<Session>())
                    WeeklySchedule.ChangeTimeZone(scheduled, id);
                ReliabilityOptions.TimeZoneId = id; QueueSave("time-zone", SaveState); Refresh();
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { status.Text = "Choose a valid Windows or IANA time zone identifier."; }
        };
        page.Children.Add(apply);
        var format = new CheckBox { Content = "Use 24-hour clock", IsChecked = ReliabilityOptions.Use24HourClock, Margin = new Thickness(0, 16, 0, 0) };
        format.Click += (_, _) => { ReliabilityOptions.Use24HourClock = format.IsChecked == true; QueueSave("clock-format", SaveState); Refresh(); }; page.Children.Add(format);
        void Refresh()
        {
            var zone = NexaTime.Zone(ReliabilityOptions.TimeZoneId); var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);
            status.Text = now.ToString(ReliabilityOptions.Use24HourClock ? "yyyy-MM-dd HH:mm:ss" : "yyyy-MM-dd h:mm:ss tt") + $"\n{zone.DisplayName}\nUTC {now:zzz} · Daylight saving: {(zone.IsDaylightSavingTime(now) ? "Active" : "Inactive")}";
        }
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) => Refresh(); Loaded += (_, _) => timer.Start(); Closed += (_, _) => timer.Stop();if(IsLoaded)timer.Start();Refresh();
        return new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
    private UIElement BuildEffects()
    {
        var page = Page("Effects", "Control interface motion and celebrations. Tips are configured in Session.");
        var reduced=new CheckBox{Content="Reduce motion",IsChecked=ReliabilityOptions.ReduceMotion,Margin=new Thickness(0,14,0,8)};
        var effects=new CheckBox{Content="Completion celebrations and score effects",IsChecked=ReliabilityOptions.CelebrationEffects,Margin=new Thickness(0,0,0,8)};
        void SaveEffects(){ReliabilityOptions.ReduceMotion=reduced.IsChecked==true;ReliabilityOptions.CelebrationEffects=effects.IsChecked==true;space?.ApplyReliabilityOptions();NexaEcosystem.UpdateSeason(store,ReliabilityOptions);QueueSave("effects",SaveState);}
        reduced.Click+=(_,_)=>SaveEffects();effects.Click+=(_,_)=>SaveEffects();page.Children.Add(reduced);page.Children.Add(effects);
        page.Children.Add(new TextBlock{Text="Windows reduced-animation preferences are always respected. Important feedback remains available with animation disabled.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,0)});
        return page;
    }
}
