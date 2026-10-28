namespace SessionDashboard;
public sealed class SessionRecap
{
    public DateTimeOffset StartedAt {get;set;}
    public DateTimeOffset EndedAt {get;set;}
    public DateTimeOffset ScheduledEnd {get;set;}
    public double ActualSeconds {get;set;}
    public long ScoreStart {get;set;}
    public long ScoreEnd {get;set;}
    public int MeaningfulActions {get;set;}
    public Dictionary<string,int> Accomplishments {get;set;}=[];
}
public static class SpaceScores
{
    public static void Reconcile(DashboardRecord record)
    {
        record.CreditedScoreEvents ??= [];
        void Credit(ProductivityLedger ledger)
        {
            foreach(var item in ledger.Events)
                if(record.CreditedScoreEvents.Add(item.Id))record.SpaceProductivityScore=checked(record.SpaceProductivityScore+item.BasePoints+item.BonusPoints);
        }
        if(!record.SpaceScoreInitialized)
        {
            // Older snapshots/continued sessions can share events. Each event is credited only once.
            record.SpaceProductivityScore=0;record.CreditedScoreEvents.Clear();
            foreach(var old in record.History)Credit(old.Data.Productivity);
            record.Current.ScoreAtStart ??=record.SpaceProductivityScore;
            record.SpaceScoreInitialized=true;
        }
        record.Current.ScoreAtStart ??=record.SpaceProductivityScore;
        Credit(record.Current.Data.Productivity);
        if(record.Current.Recap!=null)record.Current.ScoreAtEnd=record.Current.Recap.ScoreEnd;
    }
    public static long Total(DashboardRecord record,ProductivityLedger live)
    {
        Reconcile(record);
        return record.SpaceProductivityScore+live.Events.Where(e=>!record.CreditedScoreEvents.Contains(e.Id)).Sum(e=>(long)e.BasePoints+e.BonusPoints);
    }
}
public static class SessionFinalization
{
    public static SessionRecap ArchiveAndReset(DashboardRecord record, DateTimeOffset now)
    {
        var recap=Complete(record,now);
        if(record.Current.SessionValuesReset)return recap;
        Continuity.Archive(record);
        var stopped=Continuity.Clone(record.Current.Data.Session!);
        record.Current.Data=WeeklySchedule.ResetSessionData(record.Current.Data);
        // Keep the finalized schedule seed for recurrence reconciliation; never restart recording.
        stopped.IntervalNotes.Clear();stopped.HedyIntervalNotes.Clear();stopped.IntervalSummaries.Clear();
        stopped.TimelineEntries.Clear();stopped.HedyTranscript="";stopped.HedyBaseline="";stopped.HedySessionId="";
        stopped.LocalRecordingPath="";stopped.LocalRecordingSeconds=0;
        record.Current.Data.Session=stopped;
        record.Current.ContinuedFrom=record.Current.SessionId;record.Current.SessionId=Guid.NewGuid();record.Current.SessionValuesReset=true;
        return recap;
    }
    public static SessionRecap Complete(DashboardRecord record,DateTimeOffset now)
    {
        if(record.Current.Recap is {} recap)return recap;
        var session=record.Current.Data.Session??throw new InvalidOperationException("Start a session before ending it.");
        var start=session.Start;
        var end=session.End;
        if(now<start)throw new InvalidOperationException("This session has not started yet.");
        var actualEnd=now>end?end:now;
        session.StoppedAt=actualEnd;session.FinalizedAt=actualEnd;
        SpaceScores.Reconcile(record);
        var events=record.Current.Data.Productivity.Events;
        var highlights=new Dictionary<string,int>();
        void Add(string key,int count){if(count>0)highlights[key]=count;}
        Add("Tasks completed",events.Where(e=>e.Category=="Task completed").Select(e=>e.Key).Distinct().Count());
        Add("Notes captured",events.Count(e=>e.Category=="Notes"));
        Add("Water bottles recorded",events.Where(e=>e.Category=="Water").Sum(e=>Math.Max(0,e.BasePoints)/10));
        Add("Timers completed",events.Count(e=>e.Category=="Timer"));
        Add("Candy collected",events.Count(e=>e.Category=="Candy"));
        Add("Bugs squashed",record.Current.Data.Productivity.BugsSquashed);
        recap=new SessionRecap{StartedAt=start,EndedAt=actualEnd,ScheduledEnd=end,ActualSeconds=Math.Max(0,(actualEnd-start).TotalSeconds),ScoreStart=record.Current.ScoreAtStart??0,ScoreEnd=record.SpaceProductivityScore,MeaningfulActions=events.Count,Accomplishments=highlights};
        record.Current.Recap=recap;record.Current.ScoreAtEnd=recap.ScoreEnd;record.Current.IsActive=false;record.Current.CompletedCheckpointAt=actualEnd;
        return recap;
    }
}
