using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SessionDashboard;

// Additive workspace settings: old version-2 files retain their content and identities.
public sealed class NexaOptions
{
    public bool EnableF11 {get;set;} = true;
    public bool FullscreenNotification {get;set;} = true;
    public bool ShowSystemIndicators {get;set;} = true;
    public bool EnableVolumeSlider {get;set;} = true;
    public bool EscExitsFullscreen {get;set;} = true;
    public SpaceBackground HomeWallpaper {get;set;} = new();
    public bool QuickStartCompleted {get;set;}
    public bool QuickStartSkipped {get;set;}
    public int QuickStartStep {get;set;}
    public Guid? QuickStartWidget {get;set;}
    public double StreakMultiplierCap {get;set;} = 3;
    public double EffectiveStreakCap=>double.IsFinite(StreakMultiplierCap)?Math.Clamp(Math.Floor(StreakMultiplierCap*2)/2,1.5,10):3;
    public int DistractionPenalty {get;set;}
    public int AlcoholPenalty {get;set;}
    public int AutosaveSeconds { get; set; } = 300;
    public string SeasonalMode { get; set; } = "Automatic";
    public string DecorationDensity { get; set; } = "Medium";
    public bool SeasonalDecorations { get; set; } = true;
    public bool SeasonalAnimation { get; set; } = true;
    public bool SpookySurprises { get; set; } = true;
    public bool SeasonalSounds { get; set; }
    public bool HalloweenBugCostumes { get; set; } = true;
    public bool CelebrationEffects { get; set; } = true;
    public bool ReduceMotion { get; set; }
    public bool ShowQuickTips { get; set; } = true;
    public bool ShowAutosaveNotifications { get; set; } = true;
    public string TimeZoneId { get; set; } = "Automatic";
    public bool Use24HourClock { get; set; }
    public static readonly int[] SaveIntervals = [30, 60, 180, 300, 600, 900, 1800];
    public int EffectiveSaveSeconds => SaveIntervals.Contains(AutosaveSeconds) ? AutosaveSeconds : 300;
}
public static class NexaTime
{
    public static TimeZoneInfo Zone(string? id)
    {
        if (string.IsNullOrEmpty(id) || id == "Automatic") return TimeZoneInfo.Local;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }
        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out var windows))
            return TimeZoneInfo.FindSystemTimeZoneById(windows);
        return TimeZoneInfo.Local;
    }
    public static DateTimeOffset Local(DateTimeOffset instant, string? zone) => TimeZoneInfo.ConvertTime(instant, Zone(zone));
    // Spring gaps advance to the first valid wall minute. Fall overlaps choose standard time.
    public static DateTimeOffset Instant(DateTime wall, string? zone)
    {
        var tz = Zone(zone);
        wall = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
        for (var i = 0; tz.IsInvalidTime(wall) && i < 180; i++) wall = wall.AddMinutes(1);
        var offset = tz.IsAmbiguousTime(wall) ? tz.GetAmbiguousTimeOffsets(wall).Min() : tz.GetUtcOffset(wall);
        return new DateTimeOffset(wall, offset).ToUniversalTime();
    }
    public static string Percentage(double progress)
    {
        progress = double.IsFinite(progress) ? Math.Clamp(progress, 0, 1) : 0;
        var percent = progress >= 1 ? 100 : Math.Min(99.999, progress * 100);
        return percent.ToString("0.000", CultureInfo.CurrentCulture) + "%";
    }
    public static string Clock(DateTimeOffset instant, NexaOptions options, bool date = false) =>
        Local(instant, options.TimeZoneId).ToString((date ? "MMM d, " : "") + (options.Use24HourClock ? "HH:mm" : "h:mm tt"), CultureInfo.CurrentCulture);
    public static int LitPixels(double remaining, int count) =>
        Math.Clamp((int)Math.Ceiling(Math.Clamp(remaining, 0, 1) * count), 0, count);
}
public sealed record SessionOccurrence(DateTimeOffset Start, DateTimeOffset End);
public static class WeeklySchedule
{
    public static void ChangeTimeZone(Session seed, string zone)
    {
        var from = NexaTime.Local(seed.Start, seed.TimeZoneId);
        var to = NexaTime.Local(seed.End, seed.TimeZoneId);
        seed.RecurringStartMinutes ??= (int)from.TimeOfDay.TotalMinutes;
        seed.RecurringEndMinutes ??= (int)to.TimeOfDay.TotalMinutes;
        seed.RecurringEndDayOffset ??= (to.Date - from.Date).Days;
        seed.ScheduleTimeZoneId = zone;
    }
    public static SessionOccurrence? Next(Session seed, DateTimeOffset after)
    {
        if (!seed.AutomaticScheduling || seed.ScheduledDays.Count == 0) return null;
        var recordedStart = NexaTime.Local(seed.Start, seed.TimeZoneId);
        var recordedEnd = NexaTime.Local(seed.End, seed.TimeZoneId);
        var zone = seed.ScheduleTimeZoneId ?? seed.TimeZoneId;
        var start = TimeSpan.FromMinutes(seed.RecurringStartMinutes ?? (int)recordedStart.TimeOfDay.TotalMinutes);
        var end = TimeSpan.FromMinutes(seed.RecurringEndMinutes ?? (int)recordedEnd.TimeOfDay.TotalMinutes);
        var dayOffset = seed.RecurringEndDayOffset ?? (recordedEnd.Date - recordedStart.Date).Days;
        var date = NexaTime.Local(after, zone).Date;
        for (var i = 0; i <= 8; i++)
        {
            var day = date.AddDays(i);
            if (!seed.ScheduledDays.Contains(day.DayOfWeek)) continue;
            var from = NexaTime.Instant(day + start, zone);
            var to = NexaTime.Instant(day.AddDays(dayOffset) + end, zone);
            if (from > after && day>=recordedStart.Date && to > from) return new(from, to);
        }
        return null;
    }
    public static SessionOccurrence? Latest(Session seed, DateTimeOffset now)
    {
        if (!seed.AutomaticScheduling || seed.ScheduledDays.Count == 0) return null;
        var candidate = Next(seed, now.AddDays(-8));
        SessionOccurrence? latest = null;
        for (var i = 0; i < 10 && candidate != null && candidate.Start <= now; i++)
        { latest = candidate; candidate = Next(seed, candidate.Start); }
        return latest;
    }
    public static DashboardData ResetSessionData(DashboardData source)
    {
        var next = Continuity.Clone(source);
        if(!next.Widgets.Any(w=>w.PanelKey=="notes" && !w.SessionScoped))next.Notes = ""; // Local and external task lists persist across session boundaries.
        next.Productivity = new();
        foreach (var w in next.Widgets)
        {
            // Reference libraries, integrations, settings and lifetime tracker ledgers are permanent.
            if (!w.SessionScoped || w.Kind is "Bible" or "Flashcards" || w.Distribution != null) continue;
            w.IntegrationSnapshots.Clear();
            if (w.Kind == "Scripture pins") { w.ScripturePins.RemoveAll(p => !p.Permanent); continue; }
            if (w.Kind == "Task List") continue;
            if (w.Kind is "Notes" or "Hedy Notes" or "Scripture pins" or "Counter" or "Water" or "Water Bottle" or "Water bottles" or "Alcoholic Beverage" or "Timer" or "Break timer" or "Stopwatch" ||
                WidgetLibrary.Definition(w.Kind).Editor is "waterBottles" or "beer" or "counter" or "timer" or "stopwatch")
                WidgetLibrary.ClearContent(w);
        }
        next.Widgets.Where(w => w.SessionScoped && w.Kind == "Session timeline").ToList().ForEach(w => { w.Text = ""; w.Items.Clear(); });
        next.Session = null;
        return next;
    }
}
public sealed class ProductivityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WidgetId { get; set; }
    public string Key { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public int BasePoints { get; set; }
    public int BonusPoints { get; set; }
}
public sealed class ProductivityLedger
{
    public List<ProductivityEvent> Events { get; set; } = [];
    public DateTimeOffset? FrenzyUntil { get; set; }
    public DateTimeOffset? FrenzyStartedAt {get;set;}
    public DateTimeOffset? LastDirectSquashAt {get;set;}
    public double FrenzyMultiplier {get;set;} = 1;
    public DateTimeOffset? BugFrenzyStartedAt {get;set;}
    public DateTimeOffset? BugFrenzyUntil {get;set;}
    public double BugFrenzyCap {get;set;} = 2;
    public bool BugFrenzyActive(DateTimeOffset now) => BugFrenzyStartedAt is {} start && BugFrenzyUntil is {} end && start<=now && now<end && end<=start.AddSeconds(20);
    // Preserve the shared candy/squash streak; the short direct-only boost never
    // multiplies an existing multiplier or revises previously recorded awards.
    public double EffectiveMultiplier(DateTimeOffset now) => Math.Max(Multiplier(now), BugFrenzyActive(now)?Math.Clamp(double.IsFinite(BugFrenzyCap)?BugFrenzyCap:2,1.5,2):1);
    public double Multiplier(DateTimeOffset now)=>FrenzyUntil>now && (FrenzyStartedAt==null || now>=FrenzyStartedAt)?Math.Clamp(double.IsFinite(FrenzyMultiplier)?FrenzyMultiplier:1.5,1.5,10):1;
    public Dictionary<Guid, decimal> CounterHighWater { get; set; } = [];
    public int FrenzyActivations { get; set; }
    public int BugsSquashed { get; set; }
    public Dictionary<string,long> SquashesBySpecies {get;set;} = [];
    public int Score => Events.Sum(e => e.BasePoints + e.BonusPoints);
}
public static class ProductivityScoring
{
    public static bool Add(ProductivityLedger ledger, string key, string category, string description, int points, DateTimeOffset now, bool once = true, Guid? widgetId = null)
    {
        if (ledger.Events.Count >= 10000 || once && ledger.Events.Any(e => e.Key == key)) return false;
        if (ledger.Events.Count(e => now >= e.At && now - e.At < TimeSpan.FromSeconds(1)) >= 20) return false;
        var bonus = category != "Bug squash" && points>0 ? (int)Math.Round(points*(ledger.EffectiveMultiplier(now)-1),MidpointRounding.AwayFromZero) : 0;
        ledger.Events.Add(new() { WidgetId = widgetId, Key = key, Category = category, Description = description, BasePoints = points, BonusPoints = bonus, At = now.ToUniversalTime() });
        return true;
    }
    public static bool DirectSquash(ProductivityLedger ledger, string identity, DateTimeOffset now, double maximum=3, string? species=null)
    {
        if (!Add(ledger, "squash:" + identity, "Bug squash", "Direct bug squash", 10, now)) return false;
        maximum=double.IsFinite(maximum)?Math.Clamp(Math.Floor(maximum*2)/2,1.5,10):3;
        if(species!=null && BugManager.AllSpecies.Any(s=>s.ToString()==species))ledger.SquashesBySpecies[species]=ledger.SquashesBySpecies.GetValueOrDefault(species)+1;
        ledger.BugsSquashed++;ledger.FrenzyActivations++;
        if(ledger.LastDirectSquashAt==null || now>=ledger.LastDirectSquashAt)
        {
            var active=ledger.FrenzyUntil>now;
            ledger.FrenzyMultiplier=Math.Min(maximum,active?ledger.Multiplier(now)+.5:1.5);
            if(!active)ledger.FrenzyStartedAt=now.ToUniversalTime();
            ledger.LastDirectSquashAt=now.ToUniversalTime();ledger.FrenzyUntil=now.AddMinutes(2).ToUniversalTime();
            ledger.BugFrenzyStartedAt=now.ToUniversalTime();ledger.BugFrenzyUntil=now.AddSeconds(20).ToUniversalTime();ledger.BugFrenzyCap=maximum;
        }
        return true;
    }
    public static bool CollectCandy(ProductivityLedger ledger,Guid identity,DateTimeOffset now,double maximum=3)
    {
        var key="candy:"+identity;
        if(ledger.Events.Count>=10000 || ledger.Events.Any(e=>e.Key==key) || ledger.Events.Count(e=>now>=e.At && now-e.At<TimeSpan.FromSeconds(1))>=20)return false;
        maximum=double.IsFinite(maximum)?Math.Clamp(Math.Floor(maximum*2)/2,1.5,10):3;
        var active=ledger.FrenzyUntil>now;
        ledger.FrenzyMultiplier=Math.Min(maximum,active?ledger.Multiplier(now)+.5:1.5);
        if(!active){ledger.FrenzyStartedAt=now;ledger.FrenzyActivations++;}
        ledger.LastDirectSquashAt=now;ledger.FrenzyUntil=now.AddMinutes(2);
        return Add(ledger,key,"Candy","Candy collected",1,now);
    }
    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static void Observe(DashboardData before, DashboardData after, DateTimeOffset now, NexaOptions? options=null)
    {
        var ledger = after.Productivity;
        if(before.Session?.FinalizedAt!=null)return;
        void Tasks(IEnumerable<TaskItem> old, IEnumerable<TaskItem> current, Guid? widgetId = null)
        {
            var previous = old.ToDictionary(t => t.Id);
            foreach (var task in current)
            {
                if (!previous.TryGetValue(task.Id, out var prior)) Add(ledger, "task:add:" + task.Id, "Task created", task.Text, 0, now, widgetId: widgetId);
                if (task.Done && (prior == null || !prior.Done)) Add(ledger, "task:done:" + task.Id, "Task completed", task.Text, 100, now, widgetId: widgetId);
                if (!task.Done && prior?.Done == true) Add(ledger, "task:reopen:" + task.Id + ":" + now.UtcTicks, "Task reopened", task.Text, 0, now, widgetId: widgetId);
            }
        }
        void Note(string key, string old, string text, Guid? widgetId = null)
        {
            if (old == text || text.Trim().Length < 20 || Math.Abs(text.Length - old.Length) < 20) return;
            var last = ledger.Events.LastOrDefault(e => e.Category == "Notes");
            if (last != null && now - last.At < TimeSpan.FromSeconds(30)) return;
            Add(ledger, "note:" + key + ":" + Fingerprint(text), "Notes", "Meaningful note saved", 20, now, widgetId: widgetId);
        }
        Tasks(before.Tasks, after.Tasks); Note("main", before.Notes, after.Notes);
        foreach (var w in after.Widgets)
        {
            var old = before.Widgets.FirstOrDefault(x => x.Id == w.Id);
            if (old == null) { Add(ledger, "widget:add:" + w.Id, "Widget", "Added " + w.Title, 5, now, widgetId: w.Id); continue; }
            foreach (var deck in w.FlashcardDecks)
                foreach (var card in deck.Cards)
                {
                    var prior = old.FlashcardDecks.SelectMany(d => d.Cards).FirstOrDefault(c => c.Id == card.Id);
                    if (prior == null) Add(ledger, "study:add:" + card.Id, "Study", "Added a card to " + deck.Name, 0, now, widgetId: w.Id);
                    else if (prior.StudyState != card.StudyState) Add(ledger, "study:review:" + card.Id + ":" + card.StudyState, "Study", deck.Name + ": " + card.StudyState, 0, now, widgetId: w.Id);
                }
            foreach (var snapshot in w.IntegrationSnapshots.Where(s => !old.IntegrationSnapshots.Any(p => p.Fingerprint == s.Fingerprint)))
                Add(ledger, "integration:" + w.Id + ":" + snapshot.Fingerprint, "Integrations", snapshot.Service + " · " + snapshot.ListName + ": " + snapshot.TotalItems + " items observed", 0, snapshot.CapturedAt, widgetId: w.Id);
            if (old.Title != w.Title || old.Height != w.Height || old.Span != w.Span || old.PlacementX != w.PlacementX || old.PlacementY != w.PlacementY)
                Add(ledger, "widget:configure:" + w.Id, "Widget", "Configured " + w.Title, 5, now, widgetId: w.Id);
            Tasks(old.Items, w.Items, w.Id);
            if (w.PanelKey != "notes") Note(w.Id.ToString(), old.Text, w.Text, w.Id);
            var editor = WidgetLibrary.Definition(w.Kind).Editor;
            if (w.TimerStart != old.TimerStart || w.TimerStopped != old.TimerStopped)
                Add(ledger, "timer:control:" + w.Id + ":" + w.TimerStart?.UtcTicks + ":" + w.TimerStopped?.UtcTicks, "Timer activity", w.Title + (w.TimerStart == null ? " reset" : w.TimerStopped == null ? " started/resumed" : " paused"), 0, now, widgetId: w.Id);
            if (w.StopwatchStartedAt != old.StopwatchStartedAt)
                Add(ledger, "stopwatch:control:" + w.Id + ":" + w.StopwatchStartedAt?.UtcTicks + ":" + w.StopwatchElapsedSeconds, "Timer activity", w.Title + (w.StopwatchStartedAt == null ? " paused/reset" : " started/resumed"), 0, now, widgetId: w.Id);
            if (w.Value != old.Value)
            {
                var water = editor == "waterBottles";
                var high = Math.Max(old.Value, ledger.CounterHighWater.GetValueOrDefault(w.Id, old.Value));
                var gained = Math.Max(0, w.Value - high);
                ledger.CounterHighWater[w.Id] = Math.Max(high, w.Value);
                // High-water keys prevent decrement/increment farming.
                Add(ledger, "count:" + w.Id + ":" + w.Value.ToString(CultureInfo.InvariantCulture),
                    w.CounterMode=="Distraction"?"Distraction":editor=="beer"?"Alcohol":water?"Water":"Counter",w.Title+": "+old.Value+" → "+w.Value,
                    gained>0?w.CounterMode=="Distraction"?-Math.Clamp(options?.DistractionPenalty ?? 0,0,1000):editor=="beer"?-Math.Clamp(options?.AlcoholPenalty ?? 0,0,1000):water?(int)Math.Min(1000,gained*10):5:0,now,widgetId:w.Id);
            }
            foreach (var pin in w.ScripturePins.Where(p => !old.ScripturePins.Any(x => x.Id == p.Id)))
                Add(ledger, "pin:" + pin.Id, "Scripture", pin.Reference, 15, now, widgetId: w.Id);
        }
        if (after.Session is { } timeline)
            foreach (var entry in timeline.TimelineEntries.Where(e => e.Source == "manual" && before.Session?.TimelineEntries.Any(old => old.Id == e.Id) != true))
                Add(ledger, "timeline:" + entry.Id, "Notes", entry.Text, 20, now);
        if (after.Session is { } session)
            foreach (var summary in session.IntervalSummaries.Where(p => !string.IsNullOrWhiteSpace(p.Value) && before.Session?.IntervalSummaries.GetValueOrDefault(p.Key) != p.Value))
                Add(ledger, "summary:" + session.Start.UtcTicks + ":" + summary.Key, "Hedy summary", "Interval " + (summary.Key + 1), 25, now);
    }
    public static bool CaptureCompletions(DashboardData data, DateTimeOffset now)
    {
        bool changed = false;
        foreach (var w in data.Widgets.Where(w => w.TimerEnd <= now && w.TimerStart != null && w.TimerStopped == null))
            changed |= Add(data.Productivity, "timer:" + w.Id + ":" + w.TimerStart!.Value.UtcTicks, "Timer", w.Title + " completed", 75, now, widgetId: w.Id);
        if (data.Session is { StoppedAt: null } s && now >= s.End)
            changed |= Add(data.Productivity, "session:" + s.Start.UtcTicks, "Timer", s.Title + " completed", 75, now);
        return changed;
    }
}
