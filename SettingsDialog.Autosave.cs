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
    private readonly TextBlock saveStatus = new() { Text = "", Opacity = 0, IsHitTestVisible = false, TextWrapping = TextWrapping.Wrap };
    private readonly DispatcherTimer savedNotice = new(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(2) };
    private readonly System.Diagnostics.Stopwatch savedFeedbackClock = new();
    private bool showingSavedFeedback, fadingSavedFeedback;
    private void SettingsFeedback(string text, bool success = false)
    {
        savedNotice.Stop(); showingSavedFeedback = success; fadingSavedFeedback = false; savedFeedbackClock.Reset();
        saveStatus.BeginAnimation(OpacityProperty, null); saveStatus.Text = text;
        saveStatus.Foreground = Brush(success ? "TextBrush" : "MutedBrush"); saveStatus.Opacity = 1;
        if (success) { savedFeedbackClock.Start(); savedNotice.Interval = TimeSpan.FromSeconds(2); saveStatus.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))); savedNotice.Start(); }
    }
    internal TextBlock AutoSaveStatus => saveStatus;
    private bool flushing,discardClose,closePrompt;
    private AppState? lastSavedSettings;
    private void InitializeAutosave()
    {
        lastSavedSettings=Continuity.Clone(state);
        // Pages are constructed before listeners run, so opening Settings is read-only.
        autosave.Tick += (_, _) => FlushPendingChanges();
        savedNotice.Tick += (_, _) =>
        {
            if (!showingSavedFeedback) { savedNotice.Stop(); return; }
            var elapsed = savedFeedbackClock.Elapsed.TotalSeconds;
            if (elapsed >= 2.2)
            {
                savedNotice.Stop(); savedFeedbackClock.Stop(); showingSavedFeedback = false;
                saveStatus.BeginAnimation(OpacityProperty, null); saveStatus.Opacity = 0; saveStatus.Text = ""; return;
            }
            if (elapsed < 2) { savedNotice.Interval = TimeSpan.FromSeconds(2 - elapsed); return; }
            var remaining = TimeSpan.FromSeconds(Math.Max(.001, 2.2 - elapsed));
            if (!fadingSavedFeedback) { fadingSavedFeedback = true; saveStatus.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(1, 0, remaining)); }
            // A dispatcher deadline also clears the base value when composition is occluded.
            savedNotice.Interval = remaining;
        };
        Closing += (_, e) => {
            if(discardClose || FlushPendingChanges())return;
            e.Cancel=true;if(closePrompt)return;closePrompt=true;
            try{var choice=NexaChoices.Show(this,"Unsaved Settings","Your changes could not be saved. Retry, discard unsaved Settings edits, or cancel to keep editing.","Retry","Discard","Cancel");
                if(choice=="Retry" && FlushPendingChanges())Dispatcher.BeginInvoke(new Action(Close));
                if(choice=="Discard"){DiscardPendingChanges();discardClose=true;Dispatcher.BeginInvoke(new Action(Close));}}
            catch(Exception ex)when(HistoryArchive.Recoverable(ex)){SettingsFeedback("Could not close Settings safely: "+ex.Message);}
            finally{closePrompt=false;}
        };
        Closed += (_, _) => { autosave.Stop(); savedNotice.Stop(); savedFeedbackClock.Stop(); pendingChanges.Clear(); };
    }
    private void QueueSave(string key, Action change)
    {
        pendingChanges[key] = change;
        SettingsFeedback("Saving changes…");
        autosave.Stop(); autosave.Start();
    }
    internal bool FlushPendingChanges()
    {
        if (flushing) return true; // SaveNow also flushes Settings before workspace transitions.
        autosave.Stop();
        if (pendingChanges.Count == 0) return true;
        if (!ReferenceEquals(state, boundState))
        { SettingsFeedback("The Space changed. Reopen Settings to edit it."); pendingChanges.Clear(); return true; }
        flushing = true;
        try
        {
            foreach (var entry in pendingChanges.ToArray())
            {
                entry.Value();
                pendingChanges.Remove(entry.Key);
            }
            lastSavedSettings=Continuity.Clone(state);SettingsFeedback("Changes saved.", true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or System.Text.Json.JsonException or InvalidOperationException or ArgumentException)
        {
            // Keep pending edits for retry. Closing cannot silently discard a failed save.
            SettingsFeedback("Could not save changes: "+ex.Message+" Retry or close Settings for recovery options.");
            return false;
        }
        finally { flushing = false; }
    }
    internal void DiscardPendingChanges()
    {
        AppState saved;try{saved=store.Load();store.LoadInteractionPreferences(saved);}catch(Exception ex)when(HistoryArchive.Recoverable(ex)){saved=lastSavedSettings??throw new IOException("Saved Settings are unavailable. Cancel and retry when storage is accessible.",ex);}
        foreach(var name in new[]{"AccentColor","PixelColor","PixelShape","SpaceIcon","ManualTimerDraft","ScheduleTimerDraft","FocusPulseEnabled","FocusPulseIntensity","DistractionEscalationEnabled","FocusNudgesEnabled","InterfaceSoundsEnabled","GameSoundsEnabled","BugSoundsEnabled","PositiveFeedbackSound","NegativeFeedbackSound","InterfaceSoundVolume","Background","HedyRegion","HedyProtectedKey"})
        {var property=typeof(AppState).GetProperty(name);if(property?.CanWrite==true)property.SetValue(state,property.GetValue(saved));}
        if(state.Workspace!=null && saved.Workspace!=null)state.Workspace.Settings=Continuity.Clone(saved.Workspace.Settings);
        pendingChanges.Clear();autosave.Stop();Theme.SetSeasonalEnabled(NexaSeason.Active(ReliabilityOptions,DateTimeOffset.UtcNow));Theme.Apply(Application.Current.Resources,store.LoadThemeMode(),store.LoadAppTheme());space?.SettingsChanged();space?.RefreshWidgetPreferences();
    }
    private void SaveState()
    {
        if (space != null)
        {
            space.SettingsChanged();
            if (!space.SaveNow()) throw new IOException("Could not save Space settings. "+space.Feedback.Text);
        }
        else store.Save(state);
    }
}
