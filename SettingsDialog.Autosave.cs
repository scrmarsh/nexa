using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SessionDashboard;

internal sealed partial class SettingsDialog
{
    private readonly DispatcherTimer autosave = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly Dictionary<string, Action> pendingChanges = [];
    private readonly TextBlock saveStatus = new() { Text = "Changes save automatically.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
    private bool flushing;
    private void InitializeAutosave()
    {
        // Pages are constructed before listeners run, so opening Settings is read-only.
        autosave.Tick += (_, _) => FlushPendingChanges();
        Closing += (_, e) => { if (!FlushPendingChanges()) e.Cancel = true; };
        Closed += (_, _) => { autosave.Stop(); pendingChanges.Clear(); };
    }
    private void QueueSave(string key, Action change)
    {
        pendingChanges[key] = change;
        saveStatus.Text = "Saving changes…";
        autosave.Stop(); autosave.Start();
    }
    internal bool FlushPendingChanges()
    {
        if (flushing) return true; // SaveNow also flushes Settings before workspace transitions.
        autosave.Stop();
        if (pendingChanges.Count == 0) return true;
        if (!ReferenceEquals(state, boundState))
        { saveStatus.Text = "The Space changed. Reopen Settings to edit it."; pendingChanges.Clear(); return true; }
        flushing = true;
        try
        {
            foreach (var entry in pendingChanges.ToArray())
            {
                entry.Value();
                pendingChanges.Remove(entry.Key);
            }
            saveStatus.Text = "Changes saved.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException)
        {
            // Keep pending edits for retry. Closing cannot silently discard a failed save.
            saveStatus.Text = "Could not save changes. Close Settings to retry.";
            return false;
        }
        finally { flushing = false; }
    }
    private void SaveState()
    {
        if (space != null)
        {
            space.SettingsChanged();
            if (!space.SaveNow()) throw new IOException("Could not save Space settings.");
        }
        else store.Save(state);
    }
}
