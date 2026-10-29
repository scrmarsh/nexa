using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SessionDashboard;
internal static partial class Program
{
    private static void Snapshot012(Window window,string name)
    {
        window.UpdateLayout();var content=(FrameworkElement)window.Content;var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(content.ActualWidth),(int)Math.Ceiling(content.ActualHeight),96,96,PixelFormats.Pbgra32);bitmap.Render(content);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));Directory.CreateDirectory("docs/media/beta012");using var file=File.Create("docs/media/beta012/"+name+".png");encoder.Save(file);
    }
    private static void CheckInstallerCache012(string setup)
    {
        var folder=Path.Combine(output,"repair-cache012");Directory.CreateDirectory(Path.Combine(folder,"setup-cache"));var cached=Path.Combine(folder,"setup-cache","NexaSetup.exe");File.Copy(Path.GetFullPath(setup),cached);var manifest=Path.Combine(folder,"installer-integrity.json");
        File.WriteAllText(manifest,System.Text.Json.JsonSerializer.Serialize(new InstallerRepair.Integrity(1,App.AppVersion,new string('0',64),UpdateTransaction.Hash(cached))));
        Assert(InstallerRepair.Cached(Path.Combine(folder,"Nexa.exe"))==cached,"Rerun Installer resolves the exact verified official cache without downloading");
        using(var altered=new FileStream(cached,FileMode.Append,FileAccess.Write))altered.WriteByte(0);var refused=false;try{InstallerRepair.Cached(Path.Combine(folder,"Nexa.exe"));}catch(InvalidDataException){refused=true;}Assert(refused,"An altered cached installer is rejected before it can run");File.Delete(cached);Assert(InstallerRepair.Cached(Path.Combine(folder,"Nexa.exe"))==null,"A missing official installer returns the download-prompt path");
        Console.WriteLine($"PASS: {passed} installer-cache checks");
    }
    private static void Check012()
    {
        Assert(App.AppVersion=="0.1.2" && typeof(App).Assembly.GetName().Version==new Version(0,1,2,0),"Beta 0.1.2 numeric and displayed identities agree");
        var exactFeed="["+Feed("NexaSetup.exe",version:"0.1.9",channel:"beta").Trim('[',']')+","+Feed("NexaSetup.exe",version:"0.1.2",channel:"beta").Trim('[',']')+"]";
        using(var client=new System.Net.Http.HttpClient(new AuditHandler((_,_)=>new(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent(exactFeed)})))
        {
            var exact=new ExperimentalUpdater(client).DiscoverAsync(currentChannel:"Beta",requiredVersion:new Version(0,1,2)).GetAwaiter().GetResult();
            Assert(exact?.CanonicalVersion==new Version(0,1,2) && exact.Portable==false,"Repair discovery selects the matching official installer even when a newer release exists");
        }
        var clock=new ClockPicker();var events=0;var seen=new List<TimeSpan>();clock.TimeChanged+=(_,_)=>{events++;seen.Add(clock.Value);};clock.Value=new TimeSpan(21,30,0);
        Assert(events==1 && seen.Single()==new TimeSpan(21,30,0),"Clock assignment emits exactly one final hour/minute/AM-PM value");clock.Value=clock.Value;Assert(events==1,"Writing the same clock value does not trigger save loops");
        var days=new[]{DayOfWeek.Monday,DayOfWeek.Wednesday};var now=new DateTimeOffset(2026,10,29,10,0,0,TimeSpan.Zero);
        var daytime=PixelTiming.WeeklyClass("Day",days,TimeSpan.FromHours(9.5),TimeSpan.FromHours(12.5),"UTC",now);var overnight=PixelTiming.WeeklyClass("Night",days,TimeSpan.FromHours(21.5),TimeSpan.FromHours(12.5),"UTC",now);
        Assert(daytime.ConfiguredDurationMinutes==180 && (daytime.End-daytime.Start).TotalMinutes==180,"Monday/Wednesday 9:30 AM to 12:30 PM is 180 minutes");
        Assert(overnight.ConfiguredDurationMinutes==900 && overnight.RecurringEndDayOffset==1,"9:30 PM to 12:30 PM deliberately spans midnight for 900 minutes");
        var store=Store035("beta012");var saved=store.Load();saved.Workspace!.Settings.SeasonalMode="Off";saved.Workspace.Settings.ReduceMotion=true;store.Save(saved);
        Theme.SetSeasonalEnabled(false);Theme.Apply(Application.Current.Resources,SessionDashboard.ThemeMode.Light,AppTheme.Nexa);
        var icon=(SolidColorBrush)Application.Current.Resources["SpaceIconBrush"];Assert(icon.Color.R<80 && icon.Color.G<90,"Light-mode shared icon brush is dark and visible");
        foreach(var shape in PixelShapes.Names){var state=store.Load();state.PixelShape=shape;store.SaveInteractionPreferences(state);var reread=store.Load();store.LoadInteractionPreferences(reread);Assert(reread.PixelShape==shape,"Pixel shape persists: "+shape);if(shape is not ("Square" or "Circle"))Assert(PixelShapes.Clip(shape,12) is {IsFrozen:true} geometry && geometry.Bounds.Width>0,"Pixel geometry is cached and recognizable: "+shape);}
        store.SavePreferences(SessionDashboard.ThemeMode.Light,AppTheme.Nexa);
        var home=new HomeWindow(store);home.Show();Pump(TimeSpan.FromMilliseconds(80));var create=new DashboardCreation(home,store.UserId);create.Show();Pump(TimeSpan.FromMilliseconds(80));
        var primary=Descendants(create).OfType<Button>().Single(b=>Equals(b.Content,"Create Space"));Assert(primary.HorizontalContentAlignment==HorizontalAlignment.Center && primary.Foreground is SolidColorBrush,"Create Space has a centered, themed text label");
        Assert(Descendants(create).OfType<ScrollViewer>().First(s=>s.Content is StackPanel).HorizontalScrollBarVisibility==ScrollBarVisibility.Disabled,"Create Space gives wrapping widget rows a bounded viewport");
        var picker=Descendants(create).OfType<System.Windows.Controls.Primitives.Popup>().FirstOrDefault(p=>p.Child is Border b && b.Width==300);Assert(picker!=null,"Space icons use one compact anchored popup");Snapshot012(create,"create-space-light");var createScroll=Descendants(create).OfType<ScrollViewer>().First(s=>s.Content is StackPanel);createScroll.ScrollToEnd();Pump(TimeSpan.FromMilliseconds(30));Snapshot012(create,"create-space-actions-light");create.Close();
        var settings=new SettingsDialog(home,store,home.Saved,null,"Profile");settings.Show();Pump(TimeSpan.FromMilliseconds(40));var name=Descendants(settings).OfType<Button>().First(b=>b.ToolTip as string=="Edit display name");Assert(name.BorderThickness==new Thickness(0),"Profile keeps the outer card and removes the inner name border");Snapshot012(settings,"profile-light");settings.Close();
        var profile=store.LoadProfile();profile.OnboardingCompleted=false;var priorMode=Theme.CurrentMode;var priorSeason=Theme.SeasonalEnabled;var welcome=new OnboardingWindow(store,profile){Owner=home};welcome.Show();Pump(TimeSpan.FromMilliseconds(50));welcome.Advance();Assert(welcome.Step==1,"Welcome advances to Space type, with no unused use-case page");welcome.Advance();
        var tools=Descendants(welcome).OfType<CheckBox>().Where(c=>c.Tag is string).ToArray();Assert(tools.Select(c=>(string)c.Tag).Distinct().Count()==WidgetLibrary.Definitions.Select(d=>d.Name).Distinct().Count(),"Pick Your Tools includes every registered widget exactly once");Assert(tools.Take(7).Select(c=>(string)c.Tag).SequenceEqual(OnboardingWindow.FeaturedWidgets),"Featured widgets have the requested canonical order");
        welcome.Advance();var light=Descendants(welcome).OfType<RadioButton>().Single(b=>Equals(b.Content,"Light Mode"));light.IsChecked=true;Assert(Theme.IsLightMode && !Theme.SeasonalEnabled,"Onboarding Light Mode previews immediately using shared resources");Assert(!Descendants(welcome).OfType<Button>().Any(b=>Equals(b.Content,"Preview sound")),"Onboarding omits sound preview UI");Snapshot012(welcome,"onboarding-appearance-light");
        var declined=false;var dismiss=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(30)};dismiss.Tick+=(_,_)=>{var modal=Application.Current.Windows.OfType<NexaDialog>().FirstOrDefault(w=>w.Title=="Nexaween");if(modal!=null){declined=true;Click035(modal,"No Thanks");dismiss.Stop();}};dismiss.Start();welcome.Advance();Pump(TimeSpan.FromMilliseconds(80));dismiss.Stop();Assert(declined && welcome.Step==4 && Theme.IsLightMode && !Theme.SeasonalEnabled,"Final Nexaween confirmation No Thanks retains Light Mode");
        welcome.ChooseSeason(true);Assert(Theme.SeasonalEnabled && !Theme.IsLightMode && welcome.Options.SeasonalMode=="On","Nexaween acceptance immediately applies dark seasonal resources");Assert(Descendants(welcome).OfType<NexaweenDecorations>().Any(),"Final page reuses existing seasonal decorations");Pump(TimeSpan.FromMilliseconds(40));Assert(Descendants(welcome).OfType<NexaweenDecorations>().Single().Anchors.Any(),"Seasonal final-page artwork has visible layout actors");Snapshot012(welcome,"onboarding-ready-nexaween");welcome.Close();Assert(Theme.CurrentMode==priorMode && Theme.SeasonalEnabled==priorSeason,"Canceling rerun restores the original application appearance");home.Close();
        var main=new MainWindow(store);main.Show();Pump(TimeSpan.FromMilliseconds(60));main.OpenMainSettings("Session");var sessionSettings=Application.Current.Windows.OfType<SettingsDialog>().Single();
        var mode=Descendants(sessionSettings).OfType<ComboBox>().Single(c=>System.Windows.Automation.AutomationProperties.GetName(c)=="Pixel Timer operating mode");mode.SelectedIndex=1;
        var clocks=Descendants(sessionSettings).OfType<ClockPicker>().ToArray();clocks[0].Value=TimeSpan.FromHours(9.5);clocks[1].Value=TimeSpan.FromHours(12.5);Click035(sessionSettings,"M/W");
        Assert(sessionSettings.FlushPendingChanges(),"Session Settings draft autosave succeeds without recursive writes: "+sessionSettings.AutoSaveStatus.Text);
        Click035(sessionSettings,"Apply timer settings");Pump(TimeSpan.FromMilliseconds(50));Assert(main.state.Session?.ConfiguredDurationMinutes==180 && store.Load().Session?.ConfiguredDurationMinutes==180,"Applying Session Settings immediately updates and persists the active Pixel Timer");
        if(sessionSettings.IsVisible)sessionSettings.Close();
        main.OpenMainSettings("Session");var failedSettings=Application.Current.Windows.OfType<SettingsDialog>().Single();var failedName=Descendants(failedSettings).OfType<TextBox>().First(t=>t.MaxLength==100);var committedBytes=File.ReadAllBytes(store.FilePath);
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            failedName.Text="Pending retry fixture";Assert(!failedSettings.FlushPendingChanges() && failedSettings.AutoSaveStatus.Text.Contains("Could not save changes:"),"A locked save retains pending edits and displays the real storage error");
            var cancelPrompt=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(25)};cancelPrompt.Tick+=(_,_)=>{var modal=Application.Current.Windows.OfType<NexaDialog>().FirstOrDefault(w=>w.Title=="Unsaved Settings");if(modal!=null){Click035(modal,"Cancel");cancelPrompt.Stop();}};cancelPrompt.Start();failedSettings.Close();cancelPrompt.Stop();
            Assert(failedSettings.IsVisible && File.ReadAllBytes(store.FilePath).SequenceEqual(committedBytes),"Canceling failed-save closure keeps Settings open and committed bytes intact");
        }
        Assert(failedSettings.FlushPendingChanges(),"Retry succeeds after storage becomes writable without reopening Settings");failedSettings.Close();
        main.OpenMainSettings("Session");var discardSettings=Application.Current.Windows.OfType<SettingsDialog>().Single();var discardName=Descendants(discardSettings).OfType<TextBox>().First(t=>t.MaxLength==100);var beforeDraft=store.Load().ScheduleTimerDraft?.Title;
        using(var locked=new FileStream(store.FilePath,FileMode.Open,FileAccess.Read,FileShare.Read))
        {
            discardName.Text="Never persist this fixture";Assert(!discardSettings.FlushPendingChanges(),"Second locked Settings edit remains pending for explicit discard");
            var discardPrompt=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(25)};discardPrompt.Tick+=(_,_)=>{var modal=Application.Current.Windows.OfType<NexaDialog>().FirstOrDefault(w=>w.Title=="Unsaved Settings");if(modal!=null){Click035(modal,"Discard");discardPrompt.Stop();}};discardPrompt.Start();discardSettings.Close();Pump(TimeSpan.FromMilliseconds(60));discardPrompt.Stop();
            Assert(!discardSettings.IsVisible && main.state.ScheduleTimerDraft?.Title==beforeDraft,"Explicit Discard closes Settings and restores the saved draft after a failed save");
        }
        Assert(!NexaProcesses.VerifiedApplication(Environment.ProcessPath!),"The checks/installer host is never identified by a broad Nexa name match");Assert(NexaProcesses.VerifiedApplication(Path.GetFullPath("verification/release010-beta/Application/Nexa.exe")),"Released application identity is independently hash verified");main.Close();
        var guidedStore=Store035("guided-title012");var guided=GuidedSetup.Create(guidedStore.LoadProfile(),"My chosen Space",new[]{"Pixel timer","Notes"},guidedStore.UserId);var guidedState=guidedStore.Load();guidedState.Workspace!.Dashboards.Clear();guidedState.Workspace.Dashboards.Add(guided);guidedState.Workspace.ActiveDashboardId=guided.DashboardId;guidedStore.Save(Continuity.Activate(guidedState.Workspace,guidedStore.UserId));
        var guidedWindow=new MainWindow(guidedStore);guidedWindow.Show();Pump(TimeSpan.FromMilliseconds(40));Assert(guidedWindow.TitleInput.Text=="My chosen Space","The first dashboard retains the chosen Space/session title instead of replacing it with the first preset");guidedWindow.Close();
        var choiceOwner=new HomeWindow(store);choiceOwner.Show();var updateChoice=AutomaticUpdates.BuildChoice(choiceOwner,store);updateChoice.Show();Pump(TimeSpan.FromMilliseconds(40));var updateScroll=Descendants(updateChoice).OfType<ScrollViewer>().First(s=>s.Content is StackPanel);Assert(updateScroll.HorizontalScrollBarVisibility==ScrollBarVisibility.Disabled,"First-launch update preferences wrap inside the dialog instead of hiding actions offscreen");Snapshot012(updateChoice,"update-preference-light");updateChoice.Close();choiceOwner.Close();
        Console.WriteLine($"PASS: {passed} Beta012 checks");
    }
}
