using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckUpdateAutomation()
    {
        CheckIndependentAudio();CheckUpdateGameSaveGate();
        var prefs=new UpdatePreferences{AutomaticUpdates=false,UpdateChoiceMade=true,CheckOnStartup=false};
        var at=new DateTimeOffset(2026,10,28,20,0,0,TimeSpan.Zero);var now=at;
        var checks=0;var downloads=0;var restarts=0;var saved=0;bool saveSucceeds=true;
        var available=new AvailableUpdate(new(0,0,99),"Alpha","nexa test update","Isolated fake feed",now,new Uri("https://github.com/scrmarsh/nexa/releases/download/v0.0.99-alpha/Nexa.exe"),100,new string('a',64));
        var flow=new UpdateFlow(()=>prefs,p=>{prefs=p;saved++;},(p,ct)=>{checks++;return Task.FromResult<AvailableUpdate?>(available);},(r,p,ct)=>{downloads++;return Task.FromResult("isolated-plan");},plan=>{restarts++;return Task.FromResult(saveSucceeds);},()=>now);
        RunFlow(flow.CheckAsync());
        Assert(checks==1 && flow.NoticeVisible && flow.Available==available && downloads==0,"Updates notify even with automatic installation and the old startup toggle disabled");
        Assert(saved==1 && prefs.LastOfferedVersion==available.DisplayLabel,"Checks persist offered version and successful-check state");
        RunFlow(flow.CheckAsync());Assert(flow.NoticeVisible && checks==2,"Previously offered release still gets a notice on the next check");
        prefs.AutomaticUpdates=true;RunFlow(flow.CheckAsync());
        Assert(downloads==1 && flow.Prepared=="isolated-plan" && flow.RemainingSeconds==30 && restarts==0,"Automatic choice downloads/prepares and begins a 30-second countdown without installing early");
        now=at.AddSeconds(29);RunFlow(flow.TickAsync());Assert(flow.RemainingSeconds==1 && restarts==0,"Countdown waits the full thirty seconds");
        now=at.AddSeconds(30);RunFlow(flow.TickAsync());Assert(restarts==1,"Countdown saves and restarts automatically at its deadline");
        RunFlow(flow.CheckAsync());now=now.AddSeconds(2);RunFlow(flow.RestartNowAsync());Assert(restarts==2,"Restart now bypasses remaining countdown through the same save gate");
        RunFlow(flow.CheckAsync());flow.Later();now=now.AddMinutes(2);RunFlow(flow.TickAsync());Assert(restarts==2 && flow.RestartAt==null && !flow.NoticeVisible,"Later cancels the automatic restart without discarding the prepared update");
        flow.Reveal();saveSucceeds=false;RunFlow(flow.RestartNowAsync());Assert(restarts==3 && flow.RestartAt==null && flow.Status.Contains("could not be saved") && flow.NoticeVisible,"Save failure blocks installation/restart and leaves a visible retry action");
        saveSucceeds=true;RunFlow(flow.RestartNowAsync());Assert(restarts==4,"Save failure can be retried without redownloading");
        prefs.AutomaticUpdates=true;RunFlow(flow.CheckAsync());prefs.AutomaticUpdates=false;flow.AutomaticDisabled();now=now.AddMinutes(1);RunFlow(flow.TickAsync());Assert(restarts==4 && flow.RestartAt==null,"Turning automatic updates off cancels a pending restart");
        prefs.UpdateChoiceMade=false;prefs.AutomaticUpdates=true;
        var undecidedDownloads=0;var undecided=new UpdateFlow(()=>prefs,_=>{},(_,_)=>Task.FromResult<AvailableUpdate?>(available),(_,_,_)=>{undecidedDownloads++;return Task.FromResult("plan");},_=>Task.FromResult(true));RunFlow(undecided.CheckAsync());
        Assert(undecided.NoticeVisible && undecidedDownloads==0,"No automatic download before the first-run choice is saved");
        prefs.UpdateChoiceMade=true;prefs.AutomaticUpdates=false;RunFlow(undecided.InstallAsync());Assert(undecidedDownloads==1 && undecided.RemainingSeconds==30,"Opt-out users can start the complete countdown flow directly from the update notice");
        prefs.AutomaticUpdates=true;
        var failed=new UpdateFlow(()=>prefs,_=>{},(_,_)=>Task.FromResult<AvailableUpdate?>(available),(_,_,_)=>throw new InvalidDataException("Checksum mismatch"),_=>throw new Exception("must never restart"));RunFlow(failed.CheckAsync());
        Assert(failed.Prepared==null && failed.RestartAt==null && failed.Status.Contains("Checksum mismatch"),"Invalid download never reaches countdown or replacement");
        var offline=new UpdateFlow(()=>prefs,_=>{},(_,_)=>throw new System.Net.Http.HttpRequestException("offline"),(_,_,_)=>throw new Exception("must never download"),_=>throw new Exception("must never restart"));RunFlow(offline.CheckAsync());
        Assert(offline.Available==null && offline.Status.Contains("unavailable"),"Offline checks report an error without claiming the app is up to date or starting a download");
        var completion=new TaskCompletionSource<string>();var racing=new UpdateFlow(()=>prefs,_=>{},(_,_)=>Task.FromResult<AvailableUpdate?>(available),(_,_,_)=>completion.Task,_=>Task.FromResult(true));var active=racing.CheckAsync();
        Assert(racing.Busy,"Automatic download reports busy while the verifier is running");RunFlow(racing.InstallAsync());prefs.AutomaticUpdates=false;completion.SetResult("verified");RunFlow(active);Assert(racing.RestartAt==null,"Disabling automatic installation during a download prevents automatic restart");
        prefs.AutomaticUpdates=true;var laterCompletion=new TaskCompletionSource<string>();var laterFlow=new UpdateFlow(()=>prefs,_=>{},(_,_)=>Task.FromResult<AvailableUpdate?>(available),(_,_,_)=>laterCompletion.Task,_=>Task.FromResult(true));var downloading=laterFlow.CheckAsync();laterFlow.Later();laterCompletion.SetResult("verified");RunFlow(downloading);Assert(laterFlow.RestartAt==null && !laterFlow.NoticeVisible,"Later during download cannot start an invisible restart countdown");
        var preferenceStore=new StateStore(Path.Combine(output,"updates-choice.json"));preferenceStore.SaveUpdatePreferences(new(){AutomaticUpdates=false,UpdateChoiceMade=true});var loaded=preferenceStore.LoadUpdatePreferences();Assert(loaded.UpdateChoiceMade && !loaded.AutomaticUpdates,"First-run update choice persists across application starts");
        var owner=new Window{Content=new Grid(),Width=500,Height=350};owner.Show();
        var choice=AutomaticUpdates.BuildChoice(owner,preferenceStore);Assert(Descendants(choice).OfType<CheckBox>().Single().IsChecked==true && Descendants(choice).OfType<TextBlock>().Any(t=>t.Text.Contains("still receive update notices")),"First-run prompt offers enabled automatic updates and explains mandatory notices");choice.Show();Pump(TimeSpan.FromMilliseconds(30));CaptureFollowup(choice,"update-choice.png");choice.Close();owner.Close();
    }
    private static void CheckUpdateGameSaveGate()
    {
        var store=new StateStore(Path.Combine(output,"game-restart-save.json"));var main=new MainWindow(store);main.Show();main.StartPixelSwarm();main.swarmGame!.Resize(new Size(640,480));main.swarmGame.Start();main.swarmGame.Advance(.05,new System.Windows.Vector(),main.swarmGame.Player);store.SaveSwarmStats(new());
        using(var locked=new FileStream(Path.Combine(store.UserPreferencesDirectory,"pixel-swarm-stats.json"),FileMode.Open,FileAccess.Read,FileShare.None))
            Assert(!main.PrepareForUpdateRestart() && main.IsVisible,"Automatic restart is blocked while the live game score cannot be saved");
        Assert(main.PrepareForUpdateRestart() && store.LoadSwarmStats().LongestRun>0,"Retrying the restart saves the game score and workspace before installation");main.ExitBoredomMode();main.Close();
    }
    private static void CaptureFollowup(Window window,string filename)
    {
        window.UpdateLayout();var root=(FrameworkElement)window.Content;var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(root);
        var png=new System.Windows.Media.Imaging.PngBitmapEncoder();png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(output,filename));png.Save(file);
    }
    private static void RunFlow(Task task){WaitHandoff(()=>task.IsCompleted,"Update flow operation completes");task.GetAwaiter().GetResult();}
    private static void CheckIndependentAudio()
    {
        var store=new StateStore(Path.Combine(output,"independent-sounds.json"));var home=new HomeWindow(store);home.Show();Assert(AutomaticUpdates.NoticeRoot(home) is Grid,"Update notice attaches to the real Home body beneath its caption wrapper");
        var settings=new SettingsDialog(home,store,home.Saved,null,"Sounds");settings.Show();
        var combos=Descendants(settings).OfType<ComboBox>().ToArray();var styles=combos.Single(c=>c.Items.OfType<string>().Contains("Bell garden"));
        var positive=combos.Single(c=>c.ItemsSource==InterfaceAudio.PositiveChoices);var negative=combos.Single(c=>c.ItemsSource==InterfaceAudio.NegativeChoices);
        positive.SelectedIndex=1;negative.SelectedIndex=4;settings.FlushPendingChanges();
        styles.SelectedIndex=2;settings.FlushPendingChanges();
        var state=store.Load();store.LoadInteractionPreferences(state);Assert(state.PositiveFeedbackSound=="Bells" && state.NegativeFeedbackSound=="Dip","Selecting a preset never copies over independent positive/negative sounds");
        negative.SelectedIndex=2;settings.FlushPendingChanges();state=store.Load();store.LoadInteractionPreferences(state);Assert(state.PositiveFeedbackSound=="Bells" && state.NegativeFeedbackSound=="Tap","Editing negative sound preserves the independent positive selection");
        positive.SelectedIndex=3;settings.FlushPendingChanges();state=store.Load();store.LoadInteractionPreferences(state);Assert(state.PositiveFeedbackSound=="Crystal" && state.NegativeFeedbackSound=="Tap","Editing positive sound preserves the independent negative selection");
        Assert(!Descendants(settings).OfType<ComboBox>().SelectMany(c=>c.Items.OfType<string>()).Any(s=>s=="Warm wood" || s=="Wood"),"Sound controls no longer contain Warm wood or Wood");
        var apply=Descendants(settings).OfType<Button>().Single(b=>Equals(b.Content,"Apply preset to both feedback sounds"));apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));settings.FlushPendingChanges();Assert(home.Saved.PositiveFeedbackSound=="Marimba" && home.Saved.NegativeFeedbackSound=="Tap","Changing both sounds requires the explicit Apply preset button");
        CaptureFollowup(settings,"independent-sounds.png");settings.Close();home.Close();
    }
}
