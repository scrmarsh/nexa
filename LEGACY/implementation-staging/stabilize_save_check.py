from pathlib import Path
p=Path('M:/Nexa/tests/ContinuityChecks.cs')
s=p.read_text(encoding='utf-8-sig')
old='''        Pump(TimeSpan.FromMilliseconds(100));
        Assert(inFlight.IsCompletedSuccessfully && window.HasUnsavedChanges, "Edits made during a background write stay dirty after the older capture completes");'''
new='''        // Isolate this capture from the independent edit-debounce save.
        ((DispatcherTimer)typeof(MainWindow).GetField("saveDelay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(window)!).Stop();
        var saveWait = System.Diagnostics.Stopwatch.StartNew();
        while (!inFlight.IsCompleted && saveWait.Elapsed < TimeSpan.FromSeconds(5)) Pump(TimeSpan.FromMilliseconds(10));
        Assert(inFlight.IsCompletedSuccessfully && window.HasUnsavedChanges && store.Load().Notes == "Captured before background write", "Edits made during a background write stay dirty after the older capture completes");'''
assert old in s
p.write_text(s.replace(old,new),encoding='utf-8-sig')
