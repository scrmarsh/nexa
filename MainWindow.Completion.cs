using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    internal void CloseAfterNavigation(){closingAfterTransition=true;Close();}
    internal void RestoreAfterNavigationFailure(){state.Workspace!.CleanShutdown=false;SaveNow();}
    private bool SessionValuesWereReset=>state.Workspace!=null && Continuity.Current(state.Workspace,store.UserId).Current.SessionValuesReset;
    private Window? recapWindow;
    private bool finalizingSession;
    private double? completionVisualProgress;
    private readonly DispatcherTimer completionFill=new(){Interval=TimeSpan.FromMilliseconds(20)};
    internal bool FinalizeSession(DateTimeOffset? at=null,bool showRecap=true,bool resetValues=true)
    {
        if(finalizingSession || completionFill.IsEnabled)return false;
        if(state.Session==null){Error("Start a session before ending it.");return false;}
        var record=Continuity.Current(state.Workspace!,store.UserId);
        if(record.Current.Recap!=null && record.Current.SessionValuesReset){if(showRecap)ShowSessionRecap();return true;}
        finalizingSession=true;
        try
        {
            if(!StopLocalRecording(true) || !SaveNow())return false;
            var now=at??DateTimeOffset.UtcNow;var animate=showRecap && NexaMotion.Allowed(Options);var before=PixelTiming.Read(state.Session,now).VisualFraction;
            var completedVisual=animate?Continuity.Capture(state):null;
            var candidate=CopyWorkspaceState().Workspace!;
            var completing=Continuity.Current(candidate,store.UserId);if(resetValues)SessionFinalization.ArchiveAndReset(completing,now);else SessionFinalization.Complete(completing,now);if(now>=completing.Current.Data.Session!.End.AddMinutes(5))completing.Current.CompletedCheckpointAt=now;
            // Prevent the commit's first clock refresh from jumping to full before the fill animation.
            completionVisualProgress=animate?before:null;
            if(!CommitWorkspace(candidate)){completionVisualProgress=null;return false;}
            Action<double> visualUpdate=_=>{};
            try{if(completedVisual!=null)visualUpdate=BeginCompletionVisuals(completedVisual,now);}
            catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or OverflowException){RemoveCompletionVisuals();animate=false;completionVisualProgress=null;}
            var watch=Stopwatch.StartNew();
            completionFill.Stop();
            void Fill(object? sender,EventArgs e)
            {
                var fraction=Math.Clamp(watch.Elapsed.TotalSeconds/1.2,0,1);
                try{completionVisualProgress=before*(1-fraction);visualUpdate(fraction);UpdateClock();}
                catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or OverflowException){fraction=1;}
                if(fraction>=1){completionFill.Stop();completionFill.Tick-=Fill;RemoveCompletionVisuals();completionVisualProgress=null;if(showRecap)ShowSessionRecap();}
            }
            if(animate){completionFill.Tick+=Fill;completionFill.Start();}
            else {completionVisualProgress=null;UpdateClock();if(showRecap)ShowSessionRecap();}
            return true;
        }
        catch(Exception ex) when(HistoryArchive.Recoverable(ex)||ex is OverflowException){Error("Session could not be finalized: "+ex.Message);return false;}
        finally{finalizingSession=false;}
    }
    private void ShowSessionRecap()
    {
        if(recapWindow!=null){recapWindow.Activate();return;}
        var record=Continuity.Current(state.Workspace!,store.UserId);
        recapWindow=new SessionRecapWindow(this,record.Name,state.SpaceIcon,record.Current,Options,()=>HomeClick(this,new RoutedEventArgs()));
        recapWindow.Closed+=(_,_)=>recapWindow=null;recapWindow.Show();
    }
}
