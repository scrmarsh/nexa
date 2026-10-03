using System.IO;

namespace SessionDashboard;

// Sample workspaces use their own store. Generating and editing a preview never writes
// to the store used for the user's dashboards or credentials.
internal static class PreviewDashboards
{
    internal static readonly string[] Scenarios = ["Class", "Argo Pantry", "Church", "Study", "Meeting", "Focus"];
    private sealed record Sample(string Title, string Notes, string[] Tasks, string[] Topics);

    internal static StateStore CreateStore(StateStore homeStore, string? directory = null)
    {
        var folder = Path.Combine(directory ?? Path.Combine(Path.GetTempPath(), "Nexa", "Previews"), Guid.NewGuid().ToString("N"));
        var store = new StateStore(Path.Combine(folder, "state.json"), homeStore.UserId);
        store.SaveThemeMode(homeStore.LoadThemeMode());
        store.Save(Continuity.Activate(Generate(homeStore.UserId), homeStore.UserId));
        return store;
    }

    internal static WorkspaceDocument Generate(string userId, string? previous = null, Random? random = null, DateTimeOffset? at = null, string? scenario = null)
    {
        random ??= Random.Shared;
        var now = at ?? DateTimeOffset.Now;
        var choices = Scenarios.Where(s => "Preview: " + s != previous).ToArray();
        var kind = scenario ?? choices[random.Next(choices.Length)];
        var sample = PickSample(kind, random.Next(2));
        var template = DashboardTemplates.All.Single(t => t.Name == kind);
        var data = DashboardTemplates.Build(template);
        var colors = kind switch
        {
            "Class" => new[] { "#69ACFF", "#78B8E8", "#8BA8FF" },
            "Argo Pantry" => new[] { "#6AE58F", "#58C8AD", "#89D185" },
            "Church" => new[] { "#AFA9EC", "#B99ADE", "#8DADE8" },
            "Study" => new[] { "#C6A5FF", "#BA8FE8", "#9DA5FF" },
            "Meeting" => new[] { "#FFCA78", "#EDA776", "#E5BE8A" },
            _ => new[] { "#6AE58F", "#75C9D5", "#8FBBFF" }
        };
        data.AccentColor = data.PixelColor = colors[random.Next(colors.Length)];
        data.Notes = sample.Notes;
        data.Tasks = sample.Tasks.Select((text, i) => new TaskItem { Text = text, Done = i == 0 }).ToList();
        var minutes = new[] { 45, 60, 75, 90 }[random.Next(4)];
        var start = now.AddMinutes(-random.Next(12, minutes / 2));
        data.Session = new Session { Title = sample.Title, Start = start, End = start.AddMinutes(minutes), IntervalMinutes = 10 };
        data.Session.IntervalNotes[0] = "Session goal: " + sample.Topics[0];
        data.Session.IntervalNotes[1] = "Key takeaway: " + sample.Topics[1];
        Milestones.Collect(data.Session, now);
        data.Draft = new DashboardDraft { Initialized = true, Title = sample.Title, Duration = minutes.ToString(), Interval = "10",
            StartDate = start.LocalDateTime.Date, EndDate = data.Session.End.LocalDateTime.Date,
            StartMinutes = start.LocalDateTime.Hour * 60 + start.LocalDateTime.Minute,
            EndMinutes = data.Session.End.LocalDateTime.Hour * 60 + data.Session.End.LocalDateTime.Minute };

        string[] extras = kind switch
        {
            "Class" => ["Assignment tracker", "Topics", "Quick links"],
            "Argo Pantry" => ["Tasks", "Custom fields", "Clock"],
            "Church" => ["Agenda", "Clock", "Tasks"],
            "Study" => ["Assignment tracker", "Questions to ask", "Quick links"],
            "Meeting" => ["Checklist", "Custom fields", "Clock"],
            _ => ["Timer", "Distraction counter", "Quick links"]
        };
        foreach (var extra in extras.OrderBy(_ => random.Next()).Take(2))
        {
            var panel = extra == "Tasks" ? "tasks" : "";
            data.Widgets.Add(WidgetLibrary.Create(extra, panel));
        }
        foreach (var widget in data.Widgets)
        {
            switch (WidgetLibrary.Definition(widget.Kind).Editor)
            {
                case "text":
                    widget.Text = widget.Kind switch
                    {
                        "Agenda" => "1. " + sample.Topics[0] + "\n2. " + sample.Topics[1] + "\n3. Next steps and open questions",
                        "Custom fields" => kind == "Argo Pantry" ? "Shift: Afternoon\nTeam: Sample volunteer crew\nStation: Distribution desk" : "Location: Studio 2\nFacilitator: Sample team\nGoal: " + sample.Topics[0],
                        "Main passage" => "Theme: Encouragement and growth\nDiscussion: What can we put into practice this week?\nReflection: " + sample.Topics[1],
                        _ => sample.Notes
                    };
                    break;
                case "list":
                    var items = widget.Kind switch
                    {
                        "Topics" => sample.Topics,
                        "Questions to ask" => new[] { "How does " + sample.Topics[0].ToLowerInvariant() + " connect to the larger topic?", "Which example should we revisit next?" },
                        "Prayer requests" => new[] { "Encouragement for students during exams", "Strength for community volunteers", "Time to support a neighbor this week" },
                        _ => sample.Tasks
                    };
                    widget.Items = items.Select((text, i) => new TaskItem { Text = text, Done = i == 0 }).ToList();
                    break;
                case "counter": widget.Value = widget.Kind == "Distraction counter" ? random.Next(1, 4) : random.Next(8, 25); break;
                case "timer":
                    widget.TimerMinutes = widget.Kind == "Break timer" ? 5 : 25;
                    widget.TimerStart = now.AddMinutes(-random.Next(2, 9));
                    widget.TimerEnd = widget.TimerStart.Value.AddMinutes(widget.TimerMinutes);
                    break;
                case "tracker":
                    widget.Categories = ["Freezer", "Fridge", "Fruit", "Canned goods", "Bread"];
                    widget.Fields = ["Student ID", "Items"];
                    widget.Records = Enumerable.Range(1, random.Next(4, 7)).Select(i => new TrackerRecord
                    {
                        Name = "Sample student " + i, CreatedAt = now.AddMinutes(-i * 4),
                        Categories = [widget.Categories[i % 5], widget.Categories[(i + 2) % 5]],
                        Values = new() { ["Student ID"] = $"DEMO-{1000 + i}", ["Items"] = random.Next(3, 8).ToString() }
                    }).ToList();
                    widget.Height = 528;
                    break;
                case "links": widget.Text = kind == "Study" || kind == "Class" ? "https://www.khanacademy.org/\nhttps://www.wikipedia.org/" : "https://www.openlibrary.org/"; break;
                case "bible": widget.Text = "John 15:1–8"; break;
                case "scripture":
                    var passage = BibleLibrary.Load("NLT").Chapter("John", 15).First(v => v.Verse == 5);
                    widget.ScripturePins.Add(new() { Book = passage.Book, Chapter = passage.Chapter, Verse = passage.Verse, EndVerse = passage.Verse,
                        Translation = "NLT", Text = passage.Text, Note = "A passage for this week's group reflection." });
                    break;
                case "hedy":
                    data.Session.HedyTranscript = "Sample transcript: Today's discussion focused on " + string.Join(", ", sample.Topics).ToLowerInvariant() + ". We compared examples and agreed on the next steps.";
                    data.Session.HedyIntervalNotes[1] = "Sample imported note: " + sample.Topics[1];
                    break;
            }
        }
        WidgetLibrary.Arrange(data.Widgets, false);
        var dashboard = Continuity.NewDashboard(sample.Title, "Preview: " + kind, data, userId);
        for (var day = 1; day <= 2; day++)
        {
            var history = Continuity.Clone(dashboard.Current);
            history.SessionId = Guid.NewGuid(); history.IsActive = false;
            history.OpenedAt = start.AddDays(-day);
            history.LastAccessedAt = history.UpdatedAt = start.AddDays(-day).AddMinutes(minutes);
            history.ClosedAt = history.UpdatedAt;
            history.Title = day == 1 ? "Previous session · Review and next steps" : "Earlier session · Getting started";
            history.Data.Notes = "Sample previous-session notes\n\n" + sample.Notes + "\n\nFollow-up: " + sample.Tasks[1];
            history.Data.Session!.Start = start.AddDays(-day); history.Data.Session.End = start.AddDays(-day).AddMinutes(minutes);
            history.Data.Session.Title = history.Title;
            foreach (var widget in history.Data.Widgets)
            {
                if (widget.TimerStart is { } timerStart) widget.TimerStart = timerStart.AddDays(-day);
                if (widget.TimerEnd is { } timerEnd) widget.TimerEnd = timerEnd.AddDays(-day);
                foreach (var record in widget.Records) record.CreatedAt = record.CreatedAt.AddDays(-day);
            }
            dashboard.History.Add(history);
        }
        var workspace = new WorkspaceDocument { UserId = userId, Dashboards = [dashboard], ActiveDashboardId = dashboard.DashboardId };
        Continuity.ValidateWorkspace(workspace, userId);
        return workspace;
    }

    private static Sample PickSample(string kind, int variant) => (kind, variant) switch
    {
        ("Class", 0) => new("Biology · Cellular energy", "Lecture notes\n\nATP carries usable energy between cellular processes. Compare the inputs and outputs of photosynthesis and respiration.\n\nNext: draw a concept map and review the lab questions.", ["Review the lecture outline", "Sketch the energy-cycle concept map", "Prepare two questions for the next lab"], ["Cellular respiration", "Photosynthesis", "Energy transfer"]),
        ("Class", _) => new("History · Cities and industry", "Lecture notes\n\nToday's discussion compares life before and after industrialization. Organize examples by work, transport and urban growth.\n\nNext: compare two primary sources and note whose perspective is missing.", ["Read the source packet", "Compare two primary sources", "Draft a discussion response"], ["Urban growth", "Changing work", "Primary-source analysis"]),
        ("Argo Pantry", 0) => new("Pantry · Afternoon welcome shift", "Shift notes\n\nWelcome desk is ready. Keep the fruit and bread shelves organized and record each sample visit.\n\nHandoff: restock reusable bags and check the fridge station before closing.", ["Set up the welcome station", "Restock bags and canned goods", "Finish the closing handoff"], ["Welcoming visitors", "Shelf availability", "Volunteer handoff"]),
        ("Argo Pantry", _) => new("Pantry · Restock and distribution", "Shift notes\n\nSort the incoming delivery by storage area. The sample tracker shows visits and categories selected.\n\nHandoff: update shelf signs and check supplies for the next shift.", ["Sort the delivery", "Update shelf signs", "Prepare the next shift's supplies"], ["Stock organization", "Distribution flow", "Closing checklist"]),
        ("Church", 0) => new("Small group · Growth and connection", "Group notes\n\nMain passage: John 15:1–8. Discuss growth, connection and everyday habits.\n\nThis week's practice: set aside a quiet moment and encourage someone in the community.", ["Prepare the discussion outline", "Choose a weekly practice", "Share follow-up encouragement"], ["Growth and connection", "Everyday habits", "Community support"]),
        ("Church", _) => new("Community group · Encouragement", "Group notes\n\nBegin with a check-in and time for reflection. Invite everyone to share one encouraging moment from the week.\n\nFollow-up: organize a small act of service and review the prayer list.", ["Prepare the group check-in", "Plan a small act of service", "Review the prayer list"], ["Encouragement", "Reflection", "Serving together"]),
        ("Study", 0) => new("Study · Exam practice lab", "Study plan\n\nUse one focused block for worked examples, then explain the method without looking at the notes. Mark questions that need another pass.\n\nNext: check practice answers and create a short revision sheet.", ["Review the key definitions", "Complete six practice questions", "Make a one-page revision sheet"], ["Worked examples", "Active recall", "Practice feedback"]),
        ("Study", _) => new("Study · Research and reading", "Reading notes\n\nCapture the main claim, supporting evidence and one open question for each source. Connect the strongest findings to the draft outline.\n\nNext: revise the outline and write the introduction.", ["Read the first source", "Compare supporting evidence", "Draft the introduction"], ["Main claims", "Source comparison", "Writing the outline"]),
        ("Meeting", 0) => new("Design team · Sprint planning", "Meeting notes\n\nDecision: prioritize the onboarding improvements this sprint. Keep the scope small and review progress at the next check-in.\n\nOpen question: which step needs the clearest explanation?", ["Review the sprint goals", "Assign the onboarding follow-up", "Prepare the next check-in"], ["Sprint goals", "Onboarding improvements", "Owners and next steps"]),
        ("Meeting", _) => new("Project team · Weekly check-in", "Meeting notes\n\nCompleted: the first draft is ready for review. Discuss feedback, confirm owners and choose the next milestone.\n\nNext: share the revised draft and a short progress update.", ["Review progress since last week", "Confirm milestone owners", "Share the revised draft"], ["Progress review", "Feedback and blockers", "Next milestone"]),
        ("Focus", 0) => new("Focus · Build a clear first draft", "Focus notes\n\nGoal: turn the outline into a clear first draft. Work through one section at a time and park new ideas for later.\n\nFinish with a quick review of the opening paragraph.", ["Open the outline", "Draft the main sections", "Review the opening paragraph"], ["Clear structure", "Writing momentum", "Reviewing the draft"]),
        _ => new("Focus · Personal project hour", "Focus notes\n\nChoose one small outcome for this session. Break it into three steps, keep the notes nearby and record distractions without switching tasks.\n\nNext: summarize what worked and choose the following step.", ["Choose a small outcome", "Complete the main task", "Write the next-step summary"], ["A clear outcome", "Sustained attention", "A useful next step"])
    };
}
