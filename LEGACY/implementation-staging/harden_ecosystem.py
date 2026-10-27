from pathlib import Path
root=Path(r'M:\Nexa')
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
text=read('NexaEcosystem.Models.cs')
index=text.index('internal sealed class EcosystemDocument')
text=text[:index]+'''internal sealed class EcosystemSquash
{
    public Guid BugId { get; set; }
    public Guid SessionId { get; set; }
    public BugSpecies Species { get; set; }
    public DateTimeOffset At { get; set; }
}
'''+text[index:]
text=text.replace('    public List<SavedEcosystemBug> Bugs { get; set; } = [];','    public List<SavedEcosystemBug> Bugs { get; set; } = [];\n    public List<EcosystemSquash> PendingSquashes { get; set; } = [];')
text=text.replace('            Bugs == null || Bugs.Count > 160','            PendingSquashes == null || PendingSquashes.Count > 500 || PendingSquashes.Any(s=>s==null || s.BugId==Guid.Empty || !species.Contains(s.Species)) || PendingSquashes.Select(s=>s.BugId).Distinct().Count()!=PendingSquashes.Count ||\n            Bugs == null || Bugs.Count > 160')
write('NexaEcosystem.Models.cs',text)
text=read('MainWindow.Bugs.cs').replace('    internal void RecordEcosystemSquash(BugEntity bug) { TrackDirectSquash(bug); SaveNow(); }','''    internal bool RecordEcosystemSquash(BugEntity bug, DateTimeOffset? at = null)
    {
        if (ProductivityScoring.DirectSquash(state.Productivity, bug.InteractionId.ToString(), at ?? DateTimeOffset.UtcNow))
        { state.Workspace!.AllTimeSquashes++; Changed(); }
        return SaveNow();
    }''')
text=text.replace('        IsVisibleChanged += (_, _) => UpdateBugActivity();','        Activated += (_, _) => { bugGeometryDirty = true; UpdateBugGeometry(); };\n        IsVisibleChanged += (_, _) => UpdateBugActivity();')
write('MainWindow.Bugs.cs',text)
text=read('NexaEcosystem.Store.cs').replace('using System.Windows.Threading;','using System.Windows.Threading;\nusing System.Windows;')
text=text.replace('    private double previous, checkpoint;','    private double previous, checkpoint, scoreRetry;\n    private readonly List<EcosystemSquash> pendingSquashes = [];\n    private Guid lastSessionId;\n    private string? scoreWarning;')
text=text.replace('        Manager.Configure(preferences); ApplySeason(store.Load().Workspace?.Settings ?? new());','''        Manager.Configure(preferences);
        var savedState = store.Load();
        if(savedState.Workspace != null) lastSessionId = Continuity.Current(savedState.Workspace,store.UserId).Current.SessionId;
        if(loaded != null) pendingSquashes.AddRange(Continuity.Clone(loaded.PendingSquashes));
        ApplySeason(savedState.Workspace?.Settings ?? new());''')
text=text.replace('        if (!blocked && !File.Exists(FilePath)) Save();','        if (!blocked && pendingSquashes.Count > 0) FlushScores();\n        if (!blocked && !File.Exists(FilePath)) Save();')
text=text.replace('        if (!views.Values.Any(v => v())) return;','''        scoreRetry += dt;
        if(pendingSquashes.Count > 0 && scoreRetry >= 5) {scoreRetry = 0; FlushScores(); Save();}
        if (!views.Values.Any(v => v()) || blocked || Manager.WorldPaused) return;''')
text=text.replace('var doc = Manager.CaptureEcosystem(store.UserId); doc.Validate(store.UserId);','var doc = Manager.CaptureEcosystem(store.UserId); doc.PendingSquashes = Continuity.Clone(pendingSquashes); doc.Validate(store.UserId);')
text=text.replace('true); Status = null; return true;','true); Status = scoreWarning; return true;')
start=text.index('    internal void RecordSquash(');end=text.index('    public void Dispose()',start)
text=text[:start]+'''    internal void RecordSquash(BugEntity bug, MainWindow? owner)
    {
        if(pendingSquashes.Count >= 500) { Status="The pending squash history is full. Resolve the save error before squashing more creatures."; Changed?.Invoke(); return; }
        var id = owner?.state.Workspace is { } workspace ? Continuity.Current(workspace,store.UserId).Current.SessionId : lastSessionId;
        pendingSquashes.Add(new(){BugId=bug.InteractionId,Species=bug.Species,SessionId=id,At=DateTimeOffset.UtcNow});
        FlushScores(owner); Interaction();
    }
    private void FlushScores(MainWindow? owner = null)
    {
        if(blocked) return;
        owner ??= Application.Current?.Windows.OfType<MainWindow>().FirstOrDefault(w=>w.IsLoaded && w.SettingsStore.FilePath==store.FilePath);
        foreach(var squash in pendingSquashes.OrderBy(s=>s.At).ToArray())
        {
            try
            {
                if(owner?.state.Workspace is { } active && (squash.SessionId==Guid.Empty || Continuity.Current(active,store.UserId).Current.SessionId==squash.SessionId))
                {
                    if(!owner.RecordEcosystemSquash(new BugEntity {InteractionId=squash.BugId,Species=squash.Species},squash.At)) throw new IOException("Session save failed.");
                }
                else
                {
                    var state = store.Load();
                    if(state.Workspace==null || store.Warning?.Contains("could not be read",StringComparison.OrdinalIgnoreCase)==true) throw new IOException("The session record is unavailable.");
                    var current = Continuity.Current(state.Workspace,store.UserId).Current; lastSessionId=current.SessionId;
                    var target = squash.SessionId==Guid.Empty ? current : state.Workspace.Dashboards.SelectMany(d=>d.History.Append(d.Current)).FirstOrDefault(s=>s.SessionId==squash.SessionId);
                    if(target==null) throw new IOException("The original session record is unavailable.");
                    var ledger = target.SessionId==current.SessionId ? state.Productivity : target.Data.Productivity;
                    var score=ledger.Score;
                    if(ProductivityScoring.DirectSquash(ledger,squash.BugId.ToString(),squash.At))
                    {state.Workspace.AllTimeSquashes++;if(target.SessionId!=current.SessionId)state.Workspace.AllTimeProductivityScore+=ledger.Score-score;}
                    store.Save(state);
                }
                pendingSquashes.Remove(squash);
            }
            catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
            {scoreWarning="A squash is awaiting its session-history save. It is kept with the ecosystem and will retry.";Status=scoreWarning;return;}
        }
        scoreWarning=null;
    }
'''+text[end:]
text=text.replace('{ if (disposed) return; Save(); disposed = true;','{ if (disposed) return; FlushScores(); Save(); disposed = true;')
# Preserve pending score events when restoring or resetting a world.
text=text.replace('var doc = ReadDocument(FilePath + ".bak"); doc.Options = Manager.EcosystemSettings;','var doc = ReadDocument(FilePath + ".bak"); doc.Options = Manager.EcosystemSettings; doc.PendingSquashes = Continuity.Clone(pendingSquashes);')
text=text.replace('var doc = candidate.CaptureEcosystem(store.UserId); doc.Validate(store.UserId);','var doc = candidate.CaptureEcosystem(store.UserId); doc.PendingSquashes = Continuity.Clone(pendingSquashes); doc.Validate(store.UserId);')
write('NexaEcosystem.Store.cs',text)
text=read('NexaEcosystem.Window.cs').replace('        viewport.SizeChanged += (_, _) => Map();','        Activated += (_, _) => Map();\n        viewport.SizeChanged += (_, _) => Map();')
write('NexaEcosystem.Window.cs',text)
print('Hardened queued squash history, paused-world rendering, and view activation mapping.')
