namespace SessionDashboard;
internal sealed partial class NexaEcosystem
{
    internal void ReconcileGeneration(Guid generation)
    {
        if(Manager.SessionGeneration==generation || ownerBlocked)return;
        // Only worker I/O is drained here. Queued interaction continuations snapshot
        // the current generation later; they cannot restore captured old geometry.
        try{diskWrite.GetAwaiter().GetResult();}catch(Exception ex)when(ex is System.IO.IOException or UnauthorizedAccessException){}
        var world=Manager.WorldId;var seed=Manager.WorldSeed;var deaths=Manager.CombatDeaths;var encounters=Manager.NaturalEncounters;
        var preferences=Manager.Preferences;var options=Manager.EcosystemSettings;
        Manager.NewEcosystem(store.UserId,options,seed);
        var fresh=Manager.CaptureEcosystem(store.UserId);fresh.SessionGeneration=generation;fresh.WorldId=world;fresh.CombatDeaths=deaths;fresh.NaturalEncounters=encounters;
        fresh.Resources.RemoveAll(r=>r.Kind==BugResourceKind.Candy);
        Manager.RestoreEcosystem(fresh);Manager.Configure(preferences);Manager.BeginDashboardArrival();
        checkpoint=0;lastSessionId=Guid.Empty;
        // Pending original-session awards are deliberately retained and replayed once.
        Save();Changed?.Invoke();
    }
}
