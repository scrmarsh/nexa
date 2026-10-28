using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
namespace SessionDashboard;
public partial class MainWindow
{
    internal void CloseAfterNavigation(){closingAfterTransition=true;Close();}
    internal void RestoreAfterNavigationFailure(){state.Workspace!.CleanShutdown=false;SaveNow();}
    private Window? recapWindow;
    private bool finalizingSession;
    private double? completionVisualProgress;
    private readonly DispatcherTimer completionFill=new(){Interval=TimeSpan.FromMilliseconds(20)};
    internal bool FinalizeSession(DateTimeOffset? at=null,bool showRecap=true)
    {
        if(finalizingSession)return false;
        if(state.Session==null){Error("Start a session before ending it.");return false;}
        var record=Continuity.Current(state.Workspace!,store.UserId);
        if(record.Current.Recap!=null){if(showRecap)ShowSessionRecap();return true;}
        finalizingSession=true;
        try
        {
            if(!StopLocalRecording(true) || !SaveNow())return false;
            var now=at??DateTimeOffset.UtcNow;var animate=showRecap && now<state.Session.End && NexaMotion.Allowed(Options);var before=PixelTiming.Read(state.Session,now).VisualFraction;
            var candidate=CopyWorkspaceState().Workspace!;
            var completing=Continuity.Current(candidate,store.UserId);SessionFinalization.Complete(completing,now);if(now>=completing.Current.Data.Session!.End.AddMinutes(5))completing.Current.CompletedCheckpointAt=now;
            // Prevent the commit's first clock refresh from jumping to full before the fill animation.
            completionVisualProgress=animate?before:null;
            if(!CommitWorkspace(candidate)){completionVisualProgress=null;return false;}
            var watch=Stopwatch.StartNew();
            completionFill.Stop();
            void Fill(object? sender,EventArgs e)
            {
                var fraction=Math.Clamp(watch.Elapsed.TotalSeconds,0,1);
                completionVisualProgress=before+(1-before)*fraction;UpdateClock();
                if(fraction>=1){completionFill.Stop();completionFill.Tick-=Fill;completionVisualProgress=null;if(showRecap)ShowSessionRecap();}
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
