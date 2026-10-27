using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;
using ThemeMode = SessionDashboard.ThemeMode;

internal static partial class Program
{
    private static void CheckRelease017()
    {
        Assert(App.AppVersion == "0.1.18" && App.VersionLabel == "Nexa 0.1.18 alpha" && ReleaseNotes.All[0].Version == "0.1.18", "0.1.18 alpha assembly, visible label and release notes agree");
        var beer = WidgetLibrary.Create("Beer Counter");
        var water = WidgetLibrary.Create("Water Bottle Counter");
        var stopwatch = WidgetLibrary.Create("Stopwatch");
        var notes = WidgetLibrary.Create("Notes"); notes.Text = "Preserve this note.";
        var store = new StateStore(Path.Combine(output,"release017.json"));
        store.Save(new AppState { WidgetLayoutInitialized = true, PixelColor = "#E69A43", AccentColor = "#A881D4", Widgets = [WidgetLibrary.Create("Pixel timer","pixels"), beer, water, stopwatch, notes] });
        var window = new MainWindow(store) { Width=1250, Height=850, Left=-10000, Top=-10000, ShowActivated=false };
        window.Show(); Pump(TimeSpan.FromMilliseconds(180));
        Border Frame(Guid id) => window.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,id));
        Button ButtonIn(DependencyObject parent,string content) => Descendants(parent).OfType<Button>().Single(b=>Equals(b.Content,content));
        void Press(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        WidgetInstance State(Guid id) => window.state.Widgets.Single(w=>w.Id==id);
        var beerFrame=Frame(beer.Id); var waterFrame=Frame(water.Id); var watchFrame=Frame(stopwatch.Id);
        var beerArt=Descendants(beerFrame).OfType<DrinkArtwork>().Single();
        var waterArt=Descendants(waterFrame).OfType<DrinkArtwork>().Single();
        Assert(beerArt.Beer && !waterArt.Beer && beerArt.ActualHeight>=200 && waterArt.ActualHeight>=200,"Large bottle and glass artwork are distinct and primary");
        Assert(DrinkArtwork.BottleShape.Bounds.Width < DrinkArtwork.GlassShape.Bounds.Width,"Beer artwork has a distinct narrow bottle silhouette");
        Assert(DrinkArtwork.BeerBottleBrown.R>DrinkArtwork.BeerBottleBrown.G*1.5 && DrinkArtwork.BeerLiquidYellow.R>220 && DrinkArtwork.BeerLiquidYellow.G>150 && DrinkArtwork.BottleLiquidShape.Bounds.Width<DrinkArtwork.BottleShape.Bounds.Width,"Beer artwork uses a brown bottle shell with a separate visible yellow liquid area");
        var plus=ButtonIn(beerFrame,"+ 1 drink");
        Assert(plus.ActualHeight<=32 && plus.FontSize<=11,"Count buttons are compact and usable");
        var options=Descendants(beerFrame).OfType<Expander>().Single();
        Assert(!options.IsExpanded,"Counter options begin collapsed");
        var warning=Descendants(beerFrame).OfType<TextBlock>().Single(t=>t.Text=="Slow down—consider drinking less.");
        var reminder=Descendants(beerFrame).OfType<TextBlock>().Single(t=>t.Text=="Take a sip of water.");
        Assert(warning.Visibility==Visibility.Collapsed && !reminder.IsVisible,"Beer goal one starts without a warning or reminder");
        Press(plus); Assert(State(beer.Id).Value==1 && State(beer.Id).BeerLoggedSinceReminder==1 && !reminder.IsVisible,"First beer logs progress without a water reminder");
        Press(plus); Assert(State(beer.Id).Value==2 && reminder.IsVisible,"Second beer shows the water reminder");
        var dismiss=Descendants(beerFrame).OfType<Button>().Single(b=>Equals(b.ToolTip,"Dismiss water reminder"));
        Press(dismiss); Assert(!reminder.IsVisible,"Water reminder can be dismissed");
        Press(plus); Assert(!reminder.IsVisible,"Third beer keeps the reminder hidden");
        Press(plus); Assert(reminder.IsVisible,"Fourth beer prompts water again");
        var ticks=(List<Action<DateTimeOffset>>)typeof(MainWindow).GetField("widgetTicks",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(window)!;
        foreach(var tick in ticks) tick(DateTimeOffset.Now.AddSeconds(9));
        Assert(!reminder.IsVisible,"Reminder disappears automatically after eight seconds");
        options.IsExpanded=true; Pump(TimeSpan.FromMilliseconds(30));
        var more=Descendants(beerFrame).OfType<Button>().Single(b=>Equals(b.ToolTip,"Increase goal"));
        var less=Descendants(beerFrame).OfType<Button>().Single(b=>Equals(b.ToolTip,"Decrease goal"));
        for(var i=0;i<3;i++) Press(more);
        Assert(State(beer.Id).BeerGoal==4 && warning.Visibility==Visibility.Collapsed,"Beer goal four has no warning");
        Press(more); Assert(State(beer.Id).BeerGoal==5 && warning.Visibility==Visibility.Visible,"Selecting five beers displays the exact gentle warning");
        Press(less); Assert(warning.Visibility==Visibility.Collapsed,"Lowering the goal to four clears the warning");
        for(var i=0;i<8;i++) Press(less);
        Assert(State(beer.Id).BeerGoal==1 && State(water.Id).WaterBottleGoal==8,"Beer goal stops at one and stays separate from water");
        Press(ButtonIn(waterFrame,"+ 1 bottle")); Assert(State(water.Id).Value==1 && State(beer.Id).Value==4,"Water and beer counts remain independent");
        for(var i=0;i<12;i++) Press(Descendants(waterFrame).OfType<Button>().Single(b=>Equals(b.ToolTip,"Decrease goal")));
        Assert(State(water.Id).WaterBottleGoal==4,"Water goal minimum remains four");
        // A reset dismisses the reminder and resets only this widget's count/progress.
        Press(plus); Press(plus); Press(ButtonIn(beerFrame,"Reset count"));
        Assert(State(beer.Id).Value==0 && State(beer.Id).BeerLoggedSinceReminder==0 && !reminder.IsVisible && State(water.Id).Value==1,"Count reset clears beer reminder progress independently");
        Press(plus); // persist one logged beer toward the next reminder
        var lapList=Descendants(watchFrame).OfType<ScrollViewer>().Single(v=>System.Windows.Automation.AutomationProperties.GetName(v)=="Stopwatch laps");
        var lap=ButtonIn(watchFrame,"Lap");
        Assert(lapList.Visibility==Visibility.Collapsed && !lap.IsEnabled,"Lap list stays hidden and Lap is disabled before Start");
        Press(ButtonIn(watchFrame,"Start")); Pump(TimeSpan.FromMilliseconds(70)); Press(lap);
        var first=State(stopwatch.Id).StopwatchLaps[0];
        Assert(first>0 && lapList.Visibility==Visibility.Visible,"First lap captures elapsed time and reveals the list");
        Pump(TimeSpan.FromMilliseconds(60)); Press(lap);
        Assert(State(stopwatch.Id).StopwatchLaps.Count==2 && State(stopwatch.Id).StopwatchLaps[1]>first,"Each Lap records another increasing elapsed time");
        Press(ButtonIn(watchFrame,"Pause")); var paused=State(stopwatch.Id).StopwatchElapsedSeconds;
        Assert(!lap.IsEnabled && State(stopwatch.Id).StopwatchLaps.Count==2,"Pause preserves laps and disables recording until Resume");
        Press(ButtonIn(watchFrame,"Resume")); Pump(TimeSpan.FromMilliseconds(50)); Press(lap);
        Assert(State(stopwatch.Id).StopwatchLaps[2]>paused,"Resumed lap includes accumulated elapsed time");
        Press(ButtonIn(watchFrame,"Pause"));
        var color=(Color)ColorConverter.ConvertFromString("#E69A43");
        window.StartPixelSwarm(); Pump(TimeSpan.FromMilliseconds(50));
        var arena=Descendants(window.StoryArea).OfType<SwarmArena>().Single();
        var neutral=(SolidColorBrush)typeof(SwarmArena).GetField("neutral",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(arena)!;
        Assert(window.SwarmPixelColor==color && neutral.Color==color,"Swarm seed pixels retain the dashboard's exact pre-click pixel color");
        window.ExitBoredomMode();
        Assert(window.state.PixelColor=="#E69A43" && State(notes.Id).Text=="Preserve this note.","Returning from Swarm preserves Space colors and notes");
        window.SaveNow(); window.Close();
        var saved=store.Load();
        Assert(saved.Widgets.Single(w=>w.Id==stopwatch.Id).StopwatchLaps.Count==3,"Stopwatch laps persist in the existing Space store");
        Assert(saved.Widgets.Single(w=>w.Id==beer.Id).BeerLoggedSinceReminder==1,"Beer reminder progress persists across saves");
        var reopened=new MainWindow(store) { Width=1250,Height=850,Left=-10000,Top=-10000,ShowActivated=false };
        reopened.Show(); Pump(TimeSpan.FromMilliseconds(120));
        var restoredWatch=reopened.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,stopwatch.Id));
        Assert(Descendants(restoredWatch).OfType<TextBlock>().Count(t=>t.Text.StartsWith("Lap "))==3,"Reopening displays all saved laps");
        Press(ButtonIn(restoredWatch,"Reset"));
        Assert(reopened.state.Widgets.Single(w=>w.Id==stopwatch.Id).StopwatchLaps.Count==0 && Descendants(restoredWatch).OfType<ScrollViewer>().Single(v=>System.Windows.Automation.AutomationProperties.GetName(v)=="Stopwatch laps").Visibility==Visibility.Collapsed,"Stopwatch Reset clears laps and hides the list");
        var restoredBeer=reopened.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,beer.Id));
        Press(ButtonIn(restoredBeer,"+ 1 drink"));
        Assert(Descendants(restoredBeer).OfType<TextBlock>().Single(t=>t.Text=="Take a sip of water.").IsVisible,"Next beer after reopening completes the two-beer reminder cadence");
        foreach(var mode in new[] { ThemeMode.Dark,ThemeMode.Light })
        {
            Theme.Apply(Application.Current.Resources,mode,AppTheme.Nexa);
            Snapshot(reopened,"release017-"+mode,1250,850);
            foreach(var id in new[] { beer.Id,water.Id })
            {
                var f=reopened.WidgetCanvas.Children.OfType<Border>().Single(b=>Equals(b.Tag,id));
                var drawing=new DrawingVisual();
                using(var dc=drawing.RenderOpen()) dc.DrawRectangle(new VisualBrush(f) { Stretch=Stretch.Fill },null,new Rect(0,0,f.ActualWidth,f.ActualHeight));
                var bitmap=new RenderTargetBitmap((int)f.ActualWidth,(int)f.ActualHeight,96,96,PixelFormats.Pbgra32); bitmap.Render(drawing);
                var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream=File.Create(Path.Combine(output,$"drink-{id==beer.Id}-{mode}.png")); encoder.Save(stream);
            }
        }
        // Older compact counters may have saved a fixed 240-pixel height.
        var compactBeer=reopened.state.Widgets.Single(w=>w.Id==beer.Id); compactBeer.CustomSize=true; compactBeer.Height=240; compactBeer.DisplayMode="Compact";
        reopened.RenderWidgets(); Pump(TimeSpan.FromMilliseconds(70));
        restoredBeer=reopened.WidgetCanvas.Children.OfType<Border>().Single(f=>Equals(f.Tag,beer.Id));
        var compactArt=Descendants(restoredBeer).OfType<DrinkArtwork>().Single();
        var compactAdd=ButtonIn(restoredBeer,"+ 1 drink");
        Assert(compactArt.ActualHeight>=84 && compactAdd.TranslatePoint(new Point(0,compactAdd.ActualHeight),restoredBeer).Y<=restoredBeer.ActualHeight,"Saved compact counters adapt artwork to keep their count buttons reachable");
        typeof(MainWindow).GetMethod("AddWidgetClick",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(reopened,[reopened,new RoutedEventArgs()]);
        Pump(TimeSpan.FromMilliseconds(60));
        var catalog=Application.Current.Windows.OfType<Window>().Single(w=>w!=reopened && Descendants(w).OfType<TextBlock>().Any(t=>t.Text=="Add Widgets"));
        var icons=Descendants(catalog).OfType<DrinkArtwork>().ToArray();
        Assert(icons.Length==2 && icons.Count(i=>i.Beer)==1 && icons.Count(i=>!i.Beer)==1,"Widget menu shows a glass of water and a distinct beer bottle icon");
        catalog.Close(); reopened.Close();
        var legacy = System.Text.Json.JsonSerializer.Deserialize<WidgetInstance>("{\"Kind\":\"Stopwatch\"}")!;
        Assert(legacy.StopwatchLaps.Count==0 && legacy.BeerLoggedSinceReminder==0,"Older widget JSON gets safe defaults for new fields");
        CheckSharedNest018();
    }
    private static void CheckBugs017()
    {
        var events=0; var sizes=new HashSet<int>();
        for(var seed=0;seed<2000;seed++)
        {
            var manager=new BugManager(seed); manager.Configure(new(true,BugSpecies.AllBugs,BugDensity.Low));
            manager.SetSurfaces([],new Size(800,600));
            var victim=new BugEntity { Species=BugSpecies.Ants,State=BugState.Crossing,Position=new Point(400,300) }; manager.Bugs.Add(victim);
            manager.Squash(victim);
            if(manager.LastBurstCount==0) continue;
            events++; sizes.Add(manager.LastBurstCount);
            if(events==1)
            {
                var children=manager.Bugs.Where(b=>b!=victim).ToArray();
                Assert(children.Length==manager.LastBurstCount && children.All(b=>b.Position==victim.Position && b.State==BugState.Bursting),"Infestation starts with 4–16 new bugs at the squash position");
                manager.Advance(.08);
                Assert(children.All(b=>(b.Position-victim.Position).Length>10),"New bugs visibly burst outward on the next animation frame");
                var count=manager.Bugs.Count; Assert(!manager.Squash(victim) && manager.Bugs.Count==count,"Repeated squash clicks cannot reroll or duplicate a burst");
                for(var i=0;i<10;i++) manager.Advance(.08);
                Assert(children.All(b=>b.State!=BugState.Bursting),"Burst animation hands bugs back to normal movement");
            }
        }
        Assert(events is >440 and <560 && sizes.Min()==4 && sizes.Max()==16 && sizes.Count==13,$"Seeded squash trials follow the 25% probability and full inclusive 4–16 range ({events}/2000)");
        var spiders=new BugManager(17); spiders.Configure(new(true,BugSpecies.Spiders,BugDensity.Normal)); spiders.SetSurfaces([new("card",new Rect(30,30,300,200))],new Size(800,600));
        for(var i=0;i<600;i++) spiders.Advance(.033);
        Assert(spiders.Bugs.All(b=>b.Species==BugSpecies.Spiders) && spiders.Webs.Count>0 && spiders.Webs.Count<=24,"Spider mode naturally spins a bounded set of tiny webs");
        spiders.Bugs.Clear(); spiders.Webs.Clear();
        var ant=new BugEntity { Species=BugSpecies.Ants,State=BugState.Crossing,Position=new Point(400,300),Target=new Point(400,300),Decision=10 };
        var spider=new BugEntity { Species=BugSpecies.Spiders,State=BugState.Paused,Position=new Point(300,300),Target=new Point(300,300),Speed=90,Decision=10 };
        var feedingWeb=new BugWeb { Position=new Point(400,300),Owner=spider };
        spiders.Webs.Add(feedingWeb);
        spiders.Bugs.Add(ant); spiders.Bugs.Add(spider); spiders.Advance(.033);
        Assert(ant.State==BugState.Trapped && ReferenceEquals(feedingWeb.Captive,ant) && spider.State==BugState.ApproachingWeb,"A web captures a bug and sends its spider toward the captive");
        var trapped=ant.Position; spiders.Advance(.08); Assert(ant.Position==trapped,"Trapped bugs stop moving briefly");
        for(var i=0;i<12;i++) spiders.Advance(.08);
        Assert(ant.State==BugState.Trapped && spiders.Webs.Contains(feedingWeb),"A caught bug cannot escape and its web remains active while the spider approaches");
        for(var i=0;i<55;i++) spiders.Advance(.08);
        Assert(!spiders.Bugs.Contains(ant) && !spiders.Webs.Contains(feedingWeb) && spider.State is not (BugState.Feeding or BugState.ApproachingWeb),"The spider visibly feeds before the eaten bug and used web are removed");
        var persistent=new BugWeb { Position=new Point(1,1),Age=20 }; spiders.Webs.Add(persistent);
        for(var i=0;i<200;i++) spiders.Advance(.033);
        Assert(spiders.Webs.Contains(persistent),"An unused web remains active until it catches a bug");
        spiders.Webs.Remove(persistent);
        for(var i=0;i<600 && spiders.Webs.Count==0;i++) spiders.Advance(.033);
        Assert(spiders.Webs.Count>0 && spiders.Webs.All(w=>w.Position!=feedingWeb.Position),"A spider can spin another web at a new random location after feeding");
        spiders.Configure(new(true,BugSpecies.AllBugs,BugDensity.Infestation));
        for(var i=0;i<350;i++) spiders.Advance(.033);
        Assert(spiders.Bugs.Select(b=>b.Species).Distinct().Count()==6,"All Bugs includes all six individual bug species");
        for(var i=0;i<2000;i++)
        {
            var victim=spiders.Bugs.FirstOrDefault(b=>b.State is not (BugState.Squashed or BugState.Hiding));
            if(victim!=null) spiders.Squash(victim);
            spiders.Advance(.08);
        }
        Assert(spiders.Bugs.Count<=BugManager.MaximumPopulation && spiders.Webs.Count<=24,"Repeated chained infestation bursts remain bounded");
        spiders.Configure(new(false)); Assert(spiders.Bugs.Count==0 && spiders.Webs.Count==0 && spiders.Splats.Count==0,"Disabling removes all bugs, webs and splats");
    }
}
