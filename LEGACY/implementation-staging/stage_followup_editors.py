from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('Nexaween.cs',lambda t:t.replace('var page = Page("Seasonal Events", "NEXAWEEN · Your dashboard just got a little spooky. Automatic Halloween runs October 1 through November 2 in the selected time zone.");','var page = Page("Nexaween", "A space for whatever haunts you next. Control the seasonal palette, candy, costumes and effects.");').replace('        var mode = new ComboBox { ItemsSource = new[] { "Automatic", "On", "Off" }, SelectedItem = ReliabilityOptions.SeasonalMode };\n','').replace('        mode.SelectionChanged += (_, _) => Update(); density.SelectionChanged','        density.SelectionChanged'))
edit('SettingsDialog.Navigation.cs',lambda t:t.replace('        ReliabilityOptions.SeasonalMode=active?', '        var wasAppearance=pages.TryGetValue("Appearance",out var oldPage) && ReferenceEquals(host.Content,oldPage);\n        var wasBugs=pages.TryGetValue("Add Bugs",out var oldBugs) && ReferenceEquals(host.Content,oldBugs);\n        ReliabilityOptions.SeasonalMode=active?').replace('        if(tabs.TryGetValue("Appearance",out var selected) && ReferenceEquals(host.Content,appearanceLockedPage))host.Content=pages["Appearance"];','''        if(wasAppearance)host.Content=pages["Appearance"];
        if(pages.ContainsKey("Add Bugs")){pages["Add Bugs"]=BuildBugs();if(wasBugs)host.Content=pages["Add Bugs"];}
        if(SessionInterface){pages["Session"]=BuildSession();host.Content=pages["Session"];sessionStatusPage=pages["Session"];}'''))
p=r/'MainWindow.ClassSchedule.cs';t=p.read_text(encoding='utf-8-sig')
needle='        var savePreset = new CheckBox'
insert='''        var startDate=new DatePicker{SelectedDate=state.Session is {} original?NexaTime.Local(original.Start,zone).Date:NexaTime.Local(DateTimeOffset.UtcNow,zone).Date};
        var endDate=new DatePicker{SelectedDate=state.Session is {} ending?NexaTime.Local(ending.End,zone).Date:startDate.SelectedDate};
        var dates=new StackPanel{Margin=new Thickness(0,8,0,8),IsEnabled=repeat.IsChecked!=true};dates.Children.Add(new TextBlock{Text="One-time start date"});dates.Children.Add(startDate);dates.Children.Add(new TextBlock{Text="One-time end date"});dates.Children.Add(endDate);page.Children.Add(dates);
        repeat.Checked+=(_,_)=>dates.IsEnabled=false;repeat.Unchecked+=(_,_)=>dates.IsEnabled=true;
        var durationInput=new TextBox{Text=state.Session?.ConfiguredDurationMinutes.ToString("0")??"75",Width=100,HorizontalAlignment=HorizontalAlignment.Left};page.Children.Add(new TextBlock{Text="Duration (minutes)"});page.Children.Add(durationInput);
        var setDuration=new Button{Content="Use duration",HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,4,0,10)};page.Children.Add(setDuration);
        setDuration.Click+=(_,_)=>{if(int.TryParse(durationInput.Text,out var minutes) && minutes>0 && minutes<=(repeat.IsChecked==true?1439:10080)){var finish=(startDate.SelectedDate??NexaTime.Local(DateTimeOffset.UtcNow,zone).Date)+start.Value+TimeSpan.FromMinutes(minutes);end.Value=finish.TimeOfDay;endDate.SelectedDate=finish.Date;}};
'''
t=t.replace(needle,insert+needle)
t=t.replace('                var schedule = PixelTiming.WeeklyClass(name.Text, days.Children.OfType<CheckBox>().Where(c => c.IsChecked == true).Select(c => (DayOfWeek)c.Tag), start.Value, end.Value, zone, DateTimeOffset.UtcNow);','''                var selected=days.Children.OfType<CheckBox>().Where(c=>c.IsChecked==true).Select(c=>(DayOfWeek)c.Tag).ToList();
                var schedule=repeat.IsChecked==true?PixelTiming.WeeklyClass(name.Text,selected,start.Value,end.Value,zone,DateTimeOffset.UtcNow):new Session{Title=name.Text.Trim(),Start=NexaTime.Instant((startDate.SelectedDate??DateTime.Today)+start.Value,zone),End=NexaTime.Instant((endDate.SelectedDate??DateTime.Today)+end.Value,zone),TimeZoneId=zone,ScheduleTimeZoneId=zone,ScheduledDays=selected,BeforeSessionCountdownFrom=DateTimeOffset.UtcNow};
                if(string.IsNullOrWhiteSpace(schedule.Title)||schedule.End<=schedule.Start||schedule.End-schedule.Start>TimeSpan.FromDays(7))throw new ArgumentException("Choose an end after start, within seven days.");
                schedule.ConfiguredDurationMinutes=(schedule.End-schedule.Start).TotalMinutes;
                schedule.IntervalMinutes=state.Session?.IntervalMinutes??10;''')
t=t.replace('var repeat = new CheckBox { Content = "Automatically schedule and reset on these weekdays", IsChecked = true,','var repeat = new CheckBox { Content = "Automatically schedule and reset on these weekdays", IsChecked = state.Session?.AutomaticScheduling ?? true,')
p.write_text(t,encoding='utf-8-sig')
p=r/'MainWindow.Widgets.cs';t=p.read_text(encoding='utf-8-sig');needle='        if (type == "counter") { increment ='
block='''        CheckBox? linked=null;DatePicker? progressStartDate=null,progressEndDate=null;ClockPicker? progressStartTime=null,progressEndTime=null;
        if(type=="progress" && widget.PanelKey!="pixels")
        {
            linked=new CheckBox{Content="Use current session timing",IsChecked=widget.ProgressLinkedToSession,Margin=new Thickness(0,14,0,8)};panel.Children.Add(linked);
            var custom=new StackPanel{IsEnabled=!widget.ProgressLinkedToSession};panel.Children.Add(custom);
            var from=NexaTime.Local(widget.TimerStart??DateTimeOffset.UtcNow,Options.TimeZoneId);var to=NexaTime.Local(widget.TimerEnd??DateTimeOffset.UtcNow.AddHours(1),Options.TimeZoneId);
            progressStartDate=new DatePicker{SelectedDate=from.Date};progressEndDate=new DatePicker{SelectedDate=to.Date};progressStartTime=new ClockPicker{Value=from.TimeOfDay};progressEndTime=new ClockPicker{Value=to.TimeOfDay};
            custom.Children.Add(new TextBlock{Text="Independent start"});custom.Children.Add(progressStartDate);custom.Children.Add(progressStartTime);custom.Children.Add(new TextBlock{Text="Independent end"});custom.Children.Add(progressEndDate);custom.Children.Add(progressEndTime);
            linked.Checked+=(_,_)=>custom.IsEnabled=false;linked.Unchecked+=(_,_)=>custom.IsEnabled=true;
        }
'''
t=t.replace(needle,block+needle)
t=t.replace('            widget.Title = title.Text.Trim(); widget.Placeholder = placeholder.Text;','''            if(linked!=null)
            {
                var from=NexaTime.Instant((progressStartDate!.SelectedDate??DateTime.Today)+progressStartTime!.Value,Options.TimeZoneId);var to=NexaTime.Instant((progressEndDate!.SelectedDate??DateTime.Today)+progressEndTime!.Value,Options.TimeZoneId);
                if(linked.IsChecked!=true && to<=from){error.Text="Independent end must follow start.";return;}
                widget.ProgressLinkedToSession=linked.IsChecked==true;
                if(!widget.ProgressLinkedToSession){widget.TimerStart=from;widget.TimerEnd=to;widget.TimerStopped=null;}
            }
            widget.Title = title.Text.Trim(); widget.Placeholder = placeholder.Text;''')
p.write_text(t,encoding='utf-8-sig')
# Summary interval now belongs to the actual timeline widget; keep the session's saved value.
edit('MainWindow.SessionWidgets.cs',lambda t:t.replace('''        panel.Children.Add(new TextBlock { Text = state.Session?.Title ?? "Your session notes", FontWeight = FontWeights.SemiBold });''','''        panel.Children.Add(new TextBlock { Text = state.Session?.Title ?? "Your session notes", FontWeight = FontWeights.SemiBold });
        var interval=new ComboBox{ItemsSource=new[]{5,10,15,20,30,state.Session?.IntervalMinutes??10}.Distinct().Order().ToArray(),SelectedItem=state.Session?.IntervalMinutes??10,ToolTip="Summary interval in minutes"};
        panel.Children.Add(new TextBlock{Text="Summary interval (minutes)",Margin=new Thickness(0,8,0,4)});panel.Children.Add(interval);
        interval.SelectionChanged+=(_,_)=>{if(state.Session is {} session && interval.SelectedItem is int minutes){session.IntervalMinutes=minutes;IntervalInput.Text=minutes.ToString();BuildTimeline();Changed();SaveNow();}};'''))
print('Added one-time date/duration editing, independent progress ranges and timeline-only summary intervals; removed obsolete seasonal selector.')
