namespace SessionDashboard;

// The same flow drives startup notices and Settings; no network/helper calls in tests.
internal sealed class UpdateFlow
{
    private readonly Func<UpdatePreferences> preferences;
    private readonly Action<UpdatePreferences> save;
    private readonly Func<UpdatePreferences, CancellationToken, Task<AvailableUpdate?>> check;
    private readonly Func<AvailableUpdate, IProgress<double>, CancellationToken, Task<string>> prepare;
    private readonly Func<string, Task<bool>> restart;
    private readonly Func<DateTimeOffset> now;
    private readonly CancellationTokenSource cancellation = new();
    private bool postponed;
    internal AvailableUpdate? Available { get; private set; }
    internal string Status { get; private set; } = "Update notices are always enabled. Automatic installation follows your choice.";
    internal string? Prepared { get; private set; }
    internal DateTimeOffset? RestartAt { get; private set; }
    internal int RemainingSeconds => RestartAt is {} deadline ? Math.Max(0,(int)Math.Ceiling((deadline-now()).TotalSeconds)) : 0;
    internal bool Busy { get; private set; }
    internal bool NoticeVisible { get; private set; }
    internal bool Restarting { get; private set; }
    internal event Action? Changed;
    internal UpdateFlow(Func<UpdatePreferences> preferences, Action<UpdatePreferences> save,
        Func<UpdatePreferences,CancellationToken,Task<AvailableUpdate?>> check,
        Func<AvailableUpdate,IProgress<double>,CancellationToken,Task<string>> prepare,
        Func<string,Task<bool>> restart, Func<DateTimeOffset>? now = null)
    { this.preferences=preferences;this.save=save;this.check=check;this.prepare=prepare;this.restart=restart;this.now=now??(()=>DateTimeOffset.UtcNow); }
    private void Publish(string status) { Status=status;Changed?.Invoke(); }
    internal async Task CheckAsync()
    {
        if(Busy || RestartAt!=null || Restarting)return;
        Busy=true;Available=null;Publish("Checking published releases…");
        try
        {
            var release=await check(preferences(),cancellation.Token);
            var current=preferences();current.LastSuccessfulCheckUtc=now();current.LastCheckError="";
            current.LastOfferedVersion=release?.DisplayLabel??"";save(current);Available=release;
            if(release==null){Publish("No newer eligible published package is available.");return;}
            NoticeVisible=true;Publish(release.DisplayLabel+" is available.");
        }
        catch(Exception ex) when(Recoverable(ex)) { StoreFailure(ex,"Update check unavailable: "); }
        finally{Busy=false;Changed?.Invoke();}
        var settings=preferences();
        if(Available!=null && settings.UpdateChoiceMade && settings.AutomaticUpdates)await InstallAsync(automatic:true);
    }
    internal async Task InstallAsync(bool automatic=false)
    {
        if(Busy || Restarting || RestartAt!=null || Available is not {} release)return;
        Busy=true;NoticeVisible=true;postponed=false;
        try
        {
            Publish("Downloading and verifying "+release.DisplayLabel+"…");
            var progress=new Progress<double>(fraction=>{if(Busy)Publish($"Downloading update… {Math.Clamp(fraction,0,1):P0}");});
            Prepared=await prepare(release,progress,cancellation.Token);
            var choice=preferences();
            if(!postponed && (!automatic || choice.UpdateChoiceMade && choice.AutomaticUpdates)){RestartAt=now().AddSeconds(30);Publish("Update verified. nexa will save and restart in 30 seconds.");}
            else {RestartAt=null;Publish("Update verified. Select Restart now to save and install.");}
        }
        catch(Exception ex) when(Recoverable(ex)){RestartAt=null;StoreFailure(ex,"Update was not installed: ");}
        finally{Busy=false;Changed?.Invoke();}
    }
    internal async Task TickAsync()
    {
        if(RestartAt==null || Busy || Restarting)return;
        if(RemainingSeconds>0){Publish($"Update verified. nexa will save and restart in {RemainingSeconds} seconds.");return;}
        await RestartNowAsync();
    }
    internal async Task RestartNowAsync()
    {
        if(Prepared==null || Busy || Restarting)return;
        Restarting=true;RestartAt=null;Publish("Saving your work before restarting…");
        try
        {
            if(!await restart(Prepared)){NoticeVisible=true;Publish("Restart paused because your work could not be saved. Resolve the save error, then select Restart now.");return;}
            Publish("Restarting nexa to install the update…");
        }
        catch(Exception ex) when(Recoverable(ex)){StoreFailure(ex,"Could not restart: ");}
        finally{Restarting=false;Changed?.Invoke();}
    }
    internal void Later()
    {postponed=true;RestartAt=null;NoticeVisible=false;Publish(Prepared==null?"Update available. You can install from the next notice or Settings → Updates.":"Update ready. Restart now when you are ready.");}
    internal void Reveal(){if(Available!=null || Prepared!=null)NoticeVisible=true;Changed?.Invoke();}
    internal void AutomaticDisabled(){RestartAt=null;Publish(Prepared==null?Status:"Automatic restart canceled. The verified update is ready when you choose Restart now.");}
    internal void Cancel(){cancellation.Cancel();RestartAt=null;}
    private void StoreFailure(Exception ex,string prefix)
    {
        var message=ExperimentalUpdater.DescribeFailure(ex);
        try{var current=preferences();current.LastCheckError=message;save(current);}catch(Exception saved)when(Recoverable(saved)){}
        Publish(prefix+message);
    }
    private static bool Recoverable(Exception ex)=>ex is System.IO.InvalidDataException or System.IO.IOException or UnauthorizedAccessException or System.Net.Http.HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or InvalidOperationException or System.ComponentModel.Win32Exception or FormatException or KeyNotFoundException or TimeoutException;
}
