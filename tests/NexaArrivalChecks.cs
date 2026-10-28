using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SessionDashboard;
internal static partial class Program
{
    private static void CheckArrivalTweaks()
    {
        var world=CreateWorld(BugBehavior.Cozy,1425);world.VisibleBounds=new Rect(100,150,1200,650);world.BeginDashboardArrival();
        Step(world,.9);Assert(world.ActivePopulation==0,"Dashboard gets an initial quiet period instead of an immediate spawn burst");
        Step(world,9);Assert(world.Bugs.Count is >=3 and <=5,"New bugs arrive gradually, rather than a dozen in the first few seconds");
        Assert(world.Bugs.All(b=>world.VisibleBounds.Contains(b.Position)),"New arrivals are distributed inside the current viewport");
        Assert(world.Bugs.All(b=>b.Activity==BugActivity.Exploring && b.Carrying==0),"Cozy newcomers explore before seeking shared nests or food");
        var positions=world.Bugs.Select(b=>b.Position).ToArray();var spread=positions.SelectMany((a,i)=>positions.Skip(i+1).Select(b=>(a-b).Length)).Max();Assert(spread>350,"Random arrival positions cover a broad area instead of one nest or central cluster");
        Step(world,75);Assert(world.Nests.Sum(n=>n.Deliveries)>0 || world.Bugs.Any(b=>b.Activity is BugActivity.Gathering or BugActivity.Socializing or BugActivity.Nesting),"Exploration eventually yields to normal Cozy gathering and social behavior");
        var saved=world.CaptureEcosystem("synthetic-owner");var restored=new BugManager(200);restored.RestoreEcosystem(saved);restored.BeginDashboardArrival();
        Assert(restored.Bugs.Count==saved.Bugs.Count && restored.ActivePopulation==0,"Restored populations are kept but do not all activate on the opening frame");
        var early=restored.CaptureEcosystem("synthetic-owner");Assert(early.Bugs.Count==saved.Bugs.Count && early.Bugs.Select(b=>b.Id).SequenceEqual(saved.Bugs.Select(b=>b.Id)),"Saving during the arrival ramp preserves every dormant saved creature and identity");
        Step(restored,6);Assert(restored.ActivePopulation is >=2 and <=4,"Saved creatures reappear one at a time across multiple frames");
        var dormant=restored.Bugs.First(b=>!restored.IsActive(b));var point=dormant.Position;Step(restored,.1);Assert(dormant.Position==point || restored.IsActive(dormant),"Dormant creatures do not run hidden simulation before their arrival");
        var pressured=CreateWorld(BugBehavior.Cozy);pressured.ObserveFrameCost(100);Step(pressured,3);Assert(pressured.Bugs.Count==0,"Frame pressure throttles spawning at ordinary densities too");
        CheckVisualBehaviorTweaks();
        var root=new Grid();root.Measure(new Size(900,650));root.Arrange(new Rect(0,0,900,650));var opened=0;
        using(var notices=new NexaNotifications(root,()=>false))
        {
            notices.Show("tip","Try moving a widget to make room for your next idea.","💡",seconds:null,action:()=>opened++,actionLabel:"Tip settings");
            root.Measure(new Size(900,650));root.Arrange(new Rect(0,0,900,650));root.UpdateLayout();
            var card=Descendants(root).OfType<Border>().Single(b=>b.Focusable);
            Assert(!Descendants(card).OfType<Button>().Any(b=>Equals(b.Content,"Tip settings")),"Compact tips have no separate settings button");
            Assert(card.MaxWidth==270 && card.ActualWidth<=270 && card.Padding.Left==10,"Tip card is narrower and more compact while retaining wrapping");
            card.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});Assert(opened==1,"Clicking the tip card opens its settings directly");
            var close=Descendants(card).OfType<Button>().Single(b=>Equals(b.Content,"×"));close.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});Assert(opened==1,"Clicking a tip close control does not open settings");
            close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert(!Descendants(root).OfType<TextBlock>().Any(t=>t.Text.StartsWith("Try moving")),"Dismiss still removes the compact tip");
            notices.Show("reset","Your dashboard will save and reset shortly.","⚠",action:()=>{},actionLabel:"Postpone Reset");Assert(Descendants(root).OfType<Button>().Any(b=>Equals(b.Content,"Postpone Reset")),"Important non-tip notification action buttons are preserved");
        }
    }
    private static void CheckVisualBehaviorTweaks()
    {
        var frame=ProductivityStreakOverlay.LayoutFrame(new Size(1200,850),new Rect(0,0,1200,810));
        Assert(Math.Abs(frame.X+frame.Width/2-600)<.01 && frame.Bottom==792,"Productivity popup is bottom-centered above the work-area margin");
        Theme.SetSeasonalEnabled(true);Assert(DrinkArtwork.WaterLiquid is LinearGradientBrush orange && orange.GradientStops[0].Color==(Color)ColorConverter.ConvertFromString("#FDBA74"),"Nexaween water uses an orange liquid palette");
        Theme.SetSeasonalEnabled(false);Assert(DrinkArtwork.WaterLiquid is LinearGradientBrush blue && blue.GradientStops[0].Color==(Color)ColorConverter.ConvertFromString("#75D4FF"),"Disabling Nexaween restores the normal blue water");Theme.SetSeasonalEnabled(true);
        var world=CreateWorld(BugBehavior.Cozy,928);world.SetSurfaces([new BugSurface("web-card",new Rect(300,200,700,500))],new Size(1600,1000));
        var spider=new BugEntity{Species=BugSpecies.Spiders,Position=new(350,230),Speed=90,Activity=BugActivity.Nesting,Decision=30};var helper=new BugEntity{Species=BugSpecies.Spiders,Position=new(380,240),Speed=90,Activity=BugActivity.Nesting,Decision=30};world.Bugs.AddRange([spider,helper]);
        var web=new EcosystemWeb{Owner=spider.InteractionId,AnchorId="web-card",X=1012,Y=188,Helpers=[helper.InteractionId]};world.Webs.Add(web);BugManager.EnsureSilk(web);
        var box=world.WebBounds(web);Assert(box.Width is >80 and <=180 && box.Height is >60 and <=150,"New web bounds remain proportional without covering the whole widget");
        Assert(web.Strands.Count is >30 and <80 && web.Strands.Select(t=>t.ToX).Distinct().Count()>10,"Web topology contains an irregular nonradial network of silk paths");
        var first=web.Strands[0];var second=web.Strands[1];spider.Position=world.SilkPoint(web,first.FromX,first.FromY);helper.Position=world.SilkPoint(web,second.FromX,second.FromY);
        world.Advance(.1);
        foreach(var builder in new[]{spider,helper}){var assigned=web.Strands[builder.WeavingSegment];builder.Position=world.SilkPoint(web,assigned.FromX,assigned.FromY);}
        world.Advance(.1);Assert(spider.SilkAttached && helper.SilkAttached && spider.WeavingSegment!=helper.WeavingSegment,"Two spiders attach silk to separate jobs on one shared web");
        var progress=web.Construction;Step(world,8);Assert(web.Construction>progress && web.Strands.Count(t=>t.Progress>0)>=2,"Silk construction advances through actual crawling by multiple spiders");
        var saved=world.CaptureEcosystem("synthetic-owner");saved.Validate("synthetic-owner");var reloaded=new BugManager();reloaded.RestoreEcosystem(Continuity.Clone(saved));var restored=reloaded.Webs.Single(w=>w.Owner==spider.InteractionId);
        Assert(restored.Helpers.Contains(helper.InteractionId) && restored.Strands.Select(t=>t.Progress).SequenceEqual(web.Strands.Select(t=>t.Progress)),"Shared web collaborators and individual strand progress survive reload");
        foreach(var strand in web.Strands)strand.Progress=1;web.Construction=1;spider.Decision=0;spider.ExploreUntil=0;var staying=0;
        for(var i=0;i<30;i++){spider.Decision=0;world.Advance(.1);if(spider.Activity==BugActivity.Nesting)staying++;}
        Assert(staying>=15,"Spiders prefer their completed home web but may still explore");
        var ants=CreateWorld(BugBehavior.Cozy);var card=new Rect(400,300,500,280);ants.SetSurfaces([new BugSurface("ant-card",card)],new Size(1600,1000));var ant=new BugEntity{Species=BugSpecies.Ants,Position=new(450,300),Target=new(500,300),Speed=30,Activity=BugActivity.Exploring,Decision=30,ExploreUntil=100};ants.Bugs.Add(ant);Step(ants,3);
        Assert(ant.State==BugState.Edge && (ant.Position.Y==card.Top || ant.Position.X==card.Right || ant.Position.Y==card.Bottom || ant.Position.X==card.Left) && ant.CrawlPhase>0,"Ants march along actual widget edges with moving legs");
        var hiding=CreateWorld(BugBehavior.Cozy);hiding.SetSurfaces([new BugSurface("hide-card",card)],new Size(1600,1000));var roach=new BugEntity{Species=BugSpecies.Roaches,Position=new(500,400),Activity=BugActivity.Hiding,Decision=5};var lady=new BugEntity{Species=BugSpecies.Ladybugs,Position=new(700,450),Activity=BugActivity.Hiding,State=BugState.Hiding,Decision=5};hiding.Bugs.AddRange([roach,lady]);hiding.Advance(.1);
        Assert(roach.State==BugState.Hiding && lady.State!=BugState.Hiding,"Only roaches may hide behind a widget; other species remain visible");
        using var art=new NexaweenDecorations();art.Update(1200,1200,[card],"High",[card]);Assert(art.AttachmentBounds.Count>0 && art.AttachmentBounds.All(b=>b.Width==88),"Visible jack-o-lanterns attach at a larger size to free widget corners");
        var gallery=WidgetLibrary.Create("Bugs Squashed");gallery.Span=20;gallery.PlacementX=0;gallery.PlacementY=16;
        var productivity=WidgetLibrary.Create("Productivity");productivity.Span=10;productivity.PlacementX=0;productivity.PlacementY=650;
        var bottle=WidgetLibrary.Create("Water Bottle Counter");bottle.Value=4;bottle.PlacementX=.55;bottle.PlacementY=650;
        var store=new StateStore(System.IO.Path.Combine(output,"visual-tweak-dashboard.json"));store.Save(new AppState{WidgetLayoutInitialized=true,Widgets=[gallery,productivity,bottle]});
        var main=new MainWindow(store){Left=-10000,Top=-10000,Width=1500,Height=1000,ShowActivated=false,ShowInTaskbar=false,WindowStartupLocation=WindowStartupLocation.Manual};main.Show();
        main.state.Workspace!.Settings.SeasonalMode="On";main.state.Workspace.Settings.SeasonalAnimation=false;main.ApplyReliabilityOptions();main.RefreshSeasonalWorkspace();
        ProductivityScoring.DirectSquash(main.state.Productivity,"preview",DateTimeOffset.UtcNow);Pump(TimeSpan.FromMilliseconds(350));
        var portraits=Descendants(main.WidgetCanvas).OfType<SquashPortrait>().ToArray();Assert(portraits.Length==6 && portraits.All(p=>p.Width==180 && p.Height==132),"The rendered squash gallery contains all six portraits at triple their former natural dimensions");
        Assert(portraits.All(p=>p.TransformToVisual(main.WidgetCanvas).TransformBounds(new Rect(p.RenderSize)).Width>=150),"The roomy squash widget displays the enlarged portraits without squeezing them back to small icons");
        Assert(WidgetLibrary.MinimumHeight(gallery)>=480,"Squash widget reserves enough height for the enlarged two-row gallery");
        Snapshot(main,"dashboard-tweaks",1500,1000);main.WidgetScroller.ScrollToVerticalOffset(550);Pump(TimeSpan.FromMilliseconds(160));Snapshot(main,"dashboard-tweaks-lower",1500,1000);
        var popup=Descendants((DependencyObject)main.Content).OfType<ProductivityStreakOverlay>().Single();popup.InvalidateVisual();Pump(TimeSpan.FromMilliseconds(150));Snapshot(main,"dashboard-tweaks-lower-refreshed",1500,1000);main.Close();
        var artImage=new System.Windows.Media.Imaging.RenderTargetBitmap(1200,900,96,96,PixelFormats.Pbgra32);var scene=new Grid();scene.Children.Add(new SeasonalCorners());scene.Measure(new Size(1200,900));scene.Arrange(new Rect(0,0,1200,900));artImage.Render(scene);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(artImage));using(var file=System.IO.File.Create(System.IO.Path.Combine(output,"jack-o-lantern-vines.png")))encoder.Save(file);
        var overlay=new BugOverlay(world);overlay.Measure(new Size(1200,850));overlay.Arrange(new Rect(0,0,1200,850));overlay.SetWorldMapping(new Point(),new Size(1200,850));overlay.SetViewport(new Rect(0,52,1200,798));var picture=new System.Windows.Media.Imaging.RenderTargetBitmap(1200,850,96,96,PixelFormats.Pbgra32);picture.Render(overlay);encoder=new();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(picture));using(var file=System.IO.File.Create(System.IO.Path.Combine(output,"shared-web.png")))encoder.Save(file);
    }

}
