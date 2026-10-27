exec((__import__('pathlib').Path(__file__).parent/'stage_core.py').read_text(encoding='utf-8').split("for name in ['PixelTiming.cs'")[0])
write('WidgetActivity.cs',(stage/'WidgetActivity.cs').read_text(encoding='utf-8'))
text=read('BibleLibrary.cs').replace('public sealed class ScripturePin\n{','public sealed class ScripturePin\n{\n    public bool Permanent { get; set; }')
write('BibleLibrary.cs',text)
text=read('MainWindow.Bible.cs').replace('            var version = new TextBlock { Text = pin.Translation,','''            var permanent = new CheckBox { Content = "Permanent bookmark", IsChecked = pin.Permanent, Margin = new Thickness(0, 8, 0, 0) };
            permanent.Click += (_, _) => { pin.Permanent = permanent.IsChecked == true; Changed(); SaveNow(); }; card.Children.Add(permanent);
            var version = new TextBlock { Text = pin.Translation,''')
write('MainWindow.Bible.cs',text)
text=read('Widgets.cs').replace('    public List<ScripturePin> ScripturePins { get; set; } = [];','''    public List<ScripturePin> ScripturePins { get; set; } = [];
    public List<IntegrationSnapshot> IntegrationSnapshots { get; set; } = [];
    public List<string> HiddenProductivityCategories { get; set; } = [];''')
text=text.replace('Distribution != null || Text.Length > 0','IntegrationSnapshots.Count > 0 || Distribution != null || Text.Length > 0',1)
write('Widgets.cs',text)
text=read('NexaReliability.cs').replace('            if (w.Kind == "Task List")','            if (w.Kind == "Scripture pins") { w.ScripturePins.RemoveAll(p => !p.Permanent); continue; }\n            if (w.Kind == "Task List")')
text=text.replace('            if (old == null) { Add(ledger, "widget:add:" + w.Id, "Widget", "Added " + w.Title, 5, now); continue; }','''            if (old == null) { Add(ledger, "widget:add:" + w.Id, "Widget", "Added " + w.Title, 5, now); continue; }
            foreach (var deck in w.FlashcardDecks)
                foreach (var card in deck.Cards)
                {
                    var prior = old.FlashcardDecks.SelectMany(d => d.Cards).FirstOrDefault(c => c.Id == card.Id);
                    if (prior == null) Add(ledger, "study:add:" + card.Id, "Study", "Added a card to " + deck.Name, 0, now);
                    else if (prior.StudyState != card.StudyState) Add(ledger, "study:review:" + card.Id + ":" + card.StudyState, "Study", deck.Name + ": " + card.StudyState, 0, now);
                }
            foreach (var snapshot in w.IntegrationSnapshots.Where(s => !old.IntegrationSnapshots.Any(p => p.Fingerprint == s.Fingerprint)))
                Add(ledger, "integration:" + w.Id + ":" + snapshot.Fingerprint, "Integrations", snapshot.Service + " · " + snapshot.ListName + ": " + snapshot.TotalItems + " items observed", 0, snapshot.CapturedAt);''')
write('NexaReliability.cs',text)
text=read('MainWindow.Integrations.cs')
text=text.replace('tasks.Remove(task); RenderTasks(); PlayInterfaceSound();','TrackExternalCompletion(widget,service,task.Id,task.Title); tasks.Remove(task); RenderTasks(); PlayInterfaceSound();')
text=text.replace('events=schedule; content.Children.Clear();','events=schedule; CaptureCalendarSnapshot(widget,service,chosen.Name,events); content.Children.Clear();')
text=text.replace('tasks=result; RenderTasks();','tasks=result; CaptureTaskSnapshot(widget,service,chosen.Name,tasks); RenderTasks();')
text=text.replace('var today=DateOnly.FromDateTime(DateTime.Now);','var today=DateOnly.FromDateTime(NexaTime.Local(DateTimeOffset.UtcNow,Options.TimeZoneId).DateTime);')
text=text.replace('item.Start.LocalDateTime.ToString("h:mm tt")+"–"+item.End.LocalDateTime.ToString("h:mm tt")','NexaTime.Clock(item.Start,Options)+"–"+NexaTime.Clock(item.End,Options)')
write('MainWindow.Integrations.cs',text)
text=read('MainWindow.Widgets.cs').replace('widget.HasData || widget.Kind is "Productivity Meter" or "Productivity Score" or "Session timeline" or "Hedy Notes"','WidgetActivity.HasHistory(widget)')
write('MainWindow.Widgets.cs',text)
text=read('MainWindow.Continuity.cs').replace('ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException','ex is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or InvalidOperationException')
text=text.replace('session.OpenedAt.LocalDateTime:F','NexaTime.Local(session.OpenedAt, session.TimeZoneId):F').replace('session.LastAccessedAt.LocalDateTime:g','NexaTime.Local(session.LastAccessedAt, session.TimeZoneId):g')
text=text.replace('timer.Start.LocalDateTime:g','NexaTime.Local(timer.Start, session.TimeZoneId):g').replace('timer.End.LocalDateTime:g','NexaTime.Local(timer.End, session.TimeZoneId):g')
text=text.replace('record.CreatedAt.LocalDateTime:g','NexaTime.Local(record.CreatedAt, session.TimeZoneId):g')
needle='            foreach (var pin in widget.ScripturePins)'
text=text.replace(needle,'''            foreach (var deck in widget.FlashcardDecks)
                foreach (var card in deck.Cards) text.AppendLine($"Study · {deck.Name}: {card.Front} · {card.StudyState}");
            foreach (var snapshot in widget.IntegrationSnapshots)
            {
                text.AppendLine($"Observed {NexaTime.Local(snapshot.CapturedAt, session.TimeZoneId):g} · {snapshot.Service} · {snapshot.ListName} · {snapshot.TotalItems} items");
                foreach (var item in snapshot.Items) text.AppendLine($"  {item.Title} · {item.Status} · Due {item.Due} · {item.Start:O} → {item.End:O}");
                if (snapshot.TotalItems > snapshot.Items.Count) text.AppendLine($"  Snapshot limited to the first {snapshot.Items.Count} items.");
            }
'''+needle)
text=text.replace('{pin.Reference} · {pin.Translation}\\n{pin.Text}','{pin.Reference} · {pin.Translation} · {(pin.Permanent ? "Permanent" : "Session")}\\n{pin.Text}')
write('MainWindow.Continuity.cs',text)
text=read('MainWindow.SessionWidgets.cs').replace('e.CreatedAt.LocalDateTime:t','NexaTime.Local(e.CreatedAt, Options.TimeZoneId):t')
write('MainWindow.SessionWidgets.cs',text)
text=read('MainWindow.Reliability.cs')
needle='        string previous = "";\n        widgetTicks.Add(now =>'
replacement='''        var categories = new[] { "Task created", "Task completed", "Task reopened", "Notes", "Water", "Scripture", "Timer", "Timer activity", "Counter", "Widget", "Study", "Integrations", "Bug squash" };
        var filters = new WrapPanel();
        foreach (var category in categories)
        {
            var check = new CheckBox { Content = category, IsChecked = !widget.HiddenProductivityCategories.Contains(category), Margin = new Thickness(0, 0, 12, 8) };
            check.Click += (_, _) => { if (check.IsChecked == true) widget.HiddenProductivityCategories.Remove(category); else if (!widget.HiddenProductivityCategories.Contains(category)) widget.HiddenProductivityCategories.Add(category); Changed(); };
            filters.Children.Add(check);
        }
        panel.Children.Add(new Expander { Header = "Statistics to show", Content = filters, Margin = new Thickness(0, 8, 0, 8) });
        string previous = "";
        widgetTicks.Add(now =>'''
assert needle in text;text=text.replace(needle,replacement)
text=text.replace('            var ledger = state.Productivity;\n            var value = score ?', '            var ledger = state.Productivity;\n            var visible = ledger.Events.Where(e => !widget.HiddenProductivityCategories.Contains(e.Category)).ToArray();\n            var value = score ?')
text=text.replace('ledger.Events.GroupBy(e => e.Category)','visible.GroupBy(e => e.Category)').replace('ledger.Events.TakeLast(expanded.IsChecked == true ? 12 : 4)','visible.TakeLast(expanded.IsChecked == true ? 12 : 4)')
write('MainWindow.Reliability.cs',text)
text=read('SettingsDialog.Reliability.cs')
old='var choices = new[] { "Automatic" }.Concat(TimeZoneInfo.GetSystemTimeZones().Select(z => z.Id)).Concat(new[] { "America/Chicago", "America/New_York", "America/Los_Angeles", "Europe/London", "Asia/Tokyo", "Australia/Sydney" }).Distinct().ToArray();'
new='''var installed = TimeZoneInfo.GetSystemTimeZones();
        var choices = new[] { "Automatic" }.Concat(installed.Select(z => z.Id)).Concat(installed.Select(z => TimeZoneInfo.TryConvertWindowsIdToIanaId(z.Id, out var iana) ? iana : z.Id)).Distinct().ToArray();'''
text=text.replace(old,new)
text=text.replace('choices.Where(x => x.Contains(search.Text, StringComparison.OrdinalIgnoreCase))','choices.Where(x => x.Contains(search.Text, StringComparison.OrdinalIgnoreCase) || (x != "Automatic" && NexaTime.Zone(x).DisplayName.Contains(search.Text, StringComparison.OrdinalIgnoreCase)))')
write('SettingsDialog.Reliability.cs',text)
text=read('DashboardContinuity.cs')
needle='        WidgetLibrary.Validate(data);'
text=text.replace(needle,'''        if (data.Widgets.Any(w => w.IntegrationSnapshots == null || w.HiddenProductivityCategories == null || w.HiddenProductivityCategories.Count > 100 ||
            w.IntegrationSnapshots.Count > 12 || w.IntegrationSnapshots.Any(s => s == null || s.Service == null || s.ListName == null || s.Fingerprint == null || s.Items == null || s.Items.Count > 200 || s.TotalItems < 0 || s.Items.Any(i => i == null || i.Id == null || i.Title == null || i.Status == null))))
            throw new InvalidDataException("Invalid widget activity snapshots.");
'''+needle)
write('DashboardContinuity.cs',text)
text=read('tests/Nexa024CoreChecks.cs').replace('window.FullContent.Resources["PixelBrush"]','window.FullContent.Resources["DashboardPixelBrush"]')
write('tests/Nexa024CoreChecks.cs',text)
text=read('tests/NexaweenCoreChecks.cs').replace('window.FullContent.Resources["PixelBrush"]','window.FullContent.Resources["DashboardPixelBrush"]')
write('tests/NexaweenCoreChecks.cs',text)
print('Applied Scripture permanence, widget history snapshots, study activity, configurable productivity statistics, and clock/report zone fixes.')
