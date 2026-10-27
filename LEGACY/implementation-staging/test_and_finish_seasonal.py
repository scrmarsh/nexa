from pathlib import Path
root=Path(r'M:\Nexa')
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
text=read('NexaEcosystem.Store.cs')
text=text.replace('    internal void Interaction() { Save(); Changed?.Invoke(); }','    internal void Interaction() { Save(); Changed?.Invoke(); }\n    internal void RetryHistory() { FlushScores(); Save(); Changed?.Invoke(); }')
text=text.replace('        pendingSquashes.Add(new(){BugId=bug.InteractionId','        if(pendingSquashes.Any(s=>s.BugId==bug.InteractionId)) {FlushScores(owner);Interaction();return;}\n        pendingSquashes.Add(new(){BugId=bug.InteractionId')
text=text.replace('blocked = false; Status = null; Changed?.Invoke(); return true;','blocked = false; Status = scoreWarning; Changed?.Invoke(); return true;')
write('NexaEcosystem.Store.cs',text)
text=read('tests/Nexa024EcosystemChecks.cs')
needle='        CheckEcosystemUi(store, preferences);'
text=text.replace(needle,'''        Step(world.Manager, 2); var squashed = world.Manager.Bugs.First(b=>b.Species==BugSpecies.Ants); Assert(world.Manager.Squash(squashed), "A direct ant squash is accepted exactly once");
        using(var locked = new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.None))
        {
            world.RecordSquash(squashed,null);
            var pending=JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!;
            Assert(pending.PendingSquashes.Count==1 && pending.PendingSquashes[0].BugId==squashed.InteractionId, "A failed session-history write leaves a durable squash event in the ecosystem");
        }
        world.RetryHistory(); var scored=store.Load();
        Assert(scored.Productivity.BugsSquashed==1 && scored.Workspace!.AllTimeSquashes==1 && JsonSerializer.Deserialize<EcosystemDocument>(File.ReadAllText(world.FilePath))!.PendingSquashes.Count==0, "Recovered storage commits pending squash statistics once and clears the durable queue");
        world.RecordSquash(squashed,null); Assert(store.Load().Workspace!.AllTimeSquashes==1,"Replaying an acknowledged squash never duplicates all-time totals");
        var populationBefore=world.Manager.Bugs.Count;
        var uiPreferences=store.LoadBugPreferences(); world.ApplyPreferences(uiPreferences with {Ecosystem=uiPreferences.Ecosystem! with {Quality=BugAnimationQuality.Performance}});
        Assert(world.FrameInterval==TimeSpan.FromSeconds(1d/15) && world.Manager.Bugs.Count==populationBefore,"Performance quality changes cadence without resetting the population");
        world.ApplyPreferences(uiPreferences);
'''+needle)
text=text.replace('new Rect(0, 0, 1200, 1000)','new Rect(0, 0, 1200, 1150)').replace('95 + row * 155','95 + row * 180').replace('145 + row * 155','158 + row * 180').replace('new RenderTargetBitmap(1200, 1000','new RenderTargetBitmap(1200, 1150')
write('tests/Nexa024EcosystemChecks.cs',text)
# Changelog reflects the now-working core and static seasonal/ecosystem subsystem, not unfinished animation.
text=read('ReleaseNotes.cs')
text=text.replace('Your dashboard just got a little spooky. Core reliability and static seasonal appearance milestone; advanced ecosystem features remain in development.','Your dashboard just got a little spooky. Reliable class schedules, readable history, productivity tools, and a saved seasonal bug ecosystem.')
text=text.replace('Direct user squashes now contribute to saved session statistics and the exclusive Add Bugs totals widget. New behavior modes, texture styles, costumes and locomotion are still pending.','Cozy gathering and nesting, Creepy hiding and occasional hunting, and Combative defense and resource raids. Independent Normal/Cozy/Creepy appearances, Adorable Mode, size and quality controls, six Halloween costumes, and one saved ecosystem shared with its own view.')
text=text.replace('Deterministic countdown pixels, opt-in weekday recurrence, selected time zones, DST policies and three-decimal progress.','Sequential pixels, a direct weekly class editor, elapsed class-time digits, automatic next-class countdowns, selected time zones, DST handling and three-decimal progress.')
write('ReleaseNotes.cs',text)
print('Added durable squash retry and quality checks; updated the implemented seasonal changelog claims.')
