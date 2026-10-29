using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SessionDashboard;

internal static partial class Program
{
    private static void CheckAnimation()
    {
        var a = new BugLegPose[30]; var b = new BugLegPose[30];
        foreach (var species in BugManager.AllSpecies)
        {
            var expected = species == BugSpecies.Spiders ? 8 : species == BugSpecies.Centipedes ? 30 : 6;
            var count = BugAnimation.FillLegs(species, .7, false, a);
            BugAnimation.FillLegs(species, 1.9, false, b);
            Assert(count == expected, species + " has the correct number of legs");
            Assert(a.Take(count).All(p => double.IsFinite(p.Knee.X) && double.IsFinite(p.Foot.Y)) && a.Take(count).Where((p,i) => p.Foot != b[i].Foot).Count() > count / 2, species + " has finite articulated joints and a moving gait");
            Assert(BugAnimation.FunctionalWings(species) == (species is BugSpecies.Flies or BugSpecies.Ladybugs ? 2 : 0), species + " has the correct functional flight wings");
            var bug = new BugEntity { Species = species };
            Assert(BugArtwork.BodyOnly(bug).IsFrozen && BugHabitatVisuals.For(species).IsFrozen, species + " reuses frozen body and habitat artwork");
        }
        BugAnimation.FillLegs(BugSpecies.Ants, 1.2, false, a);
        Assert(a[0].Stance == a[3].Stance && a[3].Stance == a[4].Stance && a[1].Stance == a[2].Stance && a[2].Stance == a[5].Stance && a[0].Stance != a[1].Stance, "Insect gait alternates coordinated tripods");
        BugAnimation.FillLegs(BugSpecies.Spiders, .7, false, a);
        Assert(a.Take(8).All(p => p.Root.X < 4 && Math.Pow((p.Root.X-4)/4.5,2)+Math.Pow(p.Root.Y/4.2,2)<=1), "All eight spider legs attach to the cephalothorax");
        var spiderSteps = new string[8];
        var supported = true;
        for (var frame = 0; frame < 64; frame++)
        {
            BugAnimation.FillLegs(BugSpecies.Spiders, frame * Math.PI / 32, false, a);
            supported &= a.Take(8).Count(p => p.Stance) >= 5;
            for (var leg = 0; leg < 8; leg++) spiderSteps[leg] += a[leg].Stance ? "1" : "0";
        }
        Assert(spiderSteps.Distinct().Count() == 8 && spiderSteps.All(s => s.Contains('0') && s.Contains('1')), "Every spider leg lifts and plants on its own distinct walking schedule");
        Assert(supported, "Staggered spider steps retain at least five planted feet throughout a walking cycle");
        BugAnimation.FillLegs(BugSpecies.Spiders, .7, true, a);
        BugAnimation.FillLegs(BugSpecies.Spiders, 2.1, true, b);
        Assert(a.Take(8).Select(p => p.Foot).SequenceEqual(b.Take(8).Select(p => p.Foot)), "Sleeping spider feet stay still despite independent walking phases");
        BugAnimation.FillLegs(BugSpecies.Centipedes, .7, false, a);
        Assert((a[0].Foot-a[0].Root).Length > (a[4].Foot-a[4].Root).Length && !a[0].Stance, "Centipede ultimate legs trail as specialized appendages");
        Assert(Math.Abs(BugAnimation.SmoothAngle(179,-179,.1)+179)<.001, "Turning crosses the angle boundary by the shortest route");
        Assert(Math.Abs(BugAnimation.SmoothAngle(0,90,.01)-2.8)<.001, "Orientation changes are bounded by elapsed time");
        var gaze = new Vector(); for(var i=0;i<100;i++)gaze=BugAnimation.SmoothGaze(gaze,new Vector(100,100),.033);
        Assert(gaze.Length <= .801 && gaze.Length > .79, "Eye tracking eases toward a bounded pupil offset");
        Assert(BugAnimation.SmoothGaze(gaze,new Vector(1,1),0)==gaze, "Zero elapsed time does not jump eye position");
        var sleeping = new BugEntity { Activity=BugActivity.Sleeping };
        Assert(BugAnimation.EyeOpening(sleeping,100)==.05, "Sleeping creatures keep cartoon eyes closed");
        var resting = Enumerable.Range(0,15).Select(i=>BugAnimation.CentipedeSegment(i,0,true)).ToArray();
        Assert(resting.Max(p=>p.X)-resting.Min(p=>p.X)>25 && resting.Max(p=>p.Y)<6, "Resting centipede uses an open curve rather than a millipede spiral");
        CheckAnimationFeedback(); CheckCarrierAnimation(); RenderAnimationFrames();
    }
    private static void CheckAnimationFeedback()
    {
        var options=new NexaOptions(); using var feedback=new NexaFeedback(()=>options,_=>null,()=>true);
        for(var i=0;i<40;i++)feedback.Show("task",new Rect(0,0,200,100),"+100");
        Assert(feedback.EffectCount<=8 && feedback.ParticleCount<=72 && !feedback.IsHitTestVisible,"Celebrations cap effects and particles without intercepting controls");
        options.ReduceMotion=true; feedback.ReconcileOptions();
        Assert(feedback.EffectCount==0 && !feedback.IsAnimating,"Reduced motion clears celebrations and stops their timer immediately");
        feedback.Dispose(); feedback.Show("timer",new Rect(0,0,200,100));
        Assert(!feedback.IsAnimating,"Disposed feedback cannot restart its animation timer");
        var grid=new Grid(); var motion=true; using var notifications=new NexaNotifications(grid,()=>motion);
        notifications.Show("autosave",NexaNotifications.SavingMessage,"🔄",seconds:null);
        var glyph=Descendants(grid).OfType<TextBlock>().Single(x=>x.Text=="🔄");
        Assert(((RotateTransform)glyph.RenderTransform).HasAnimatedProperties,"Autosave rotation starts for the actual saving state");
        notifications.Show("autosave","Autosave complete","✓",seconds:2);
        var success=Descendants(grid).OfType<TextBlock>().Single(x=>x.Text=="✓");
        Assert(!((RotateTransform)success.RenderTransform).HasAnimatedProperties && !((RotateTransform)glyph.RenderTransform).HasAnimatedProperties,"Success replaces the saving notification and stops the old rotation");
        notifications.Show("autosave",NexaNotifications.FailureMessage,"⚠",seconds:null); Pump(TimeSpan.FromMilliseconds(300));
        Assert(Descendants(grid).OfType<TextBlock>().Any(x=>x.Text==NexaNotifications.FailureMessage),"Save failure remains visible until resolved or acknowledged");
        notifications.Show("tip","Animation test","💡"); motion=false;notifications.ReconcileMotion();
        Assert(Descendants(grid).OfType<TextBlock>().All(x=>!x.HasAnimatedProperties && !x.RenderTransform.HasAnimatedProperties),"Reduced motion removes running notification clocks");
        using var art=new NexaweenDecorations {IsHitTestVisible=false};
        art.ConfigureAnimation(options,new Rect(0,0,1200,1000));art.Update(1200,1000,[],"High");
        Assert(art.ActorCount<=52 && art.ActorCount>0 && !art.IsAnimating,"Seasonal decorations remain bounded and do not animate while unloaded");
        art.Dispose();Assert(!art.IsAnimating,"Disposing seasonal art stops its timer");
    }
    private static void CheckCarrierAnimation()
    {
        var world=CreateWorld(BugBehavior.Cozy); Step(world,20);
        var ants=world.Bugs.Where(x=>x.Species==BugSpecies.Ants).Take(2).ToArray();
        var food=world.Resources.First(x=>x.Kind==BugResourceKind.Candy); food.X=800;food.Y=500;food.Amount=10;
        foreach(var ant in ants){ant.Position=food.Position;ant.Activity=BugActivity.Gathering;ant.ResourceId=food.Id;ant.Decision=30;ant.Carrying=0;}
        world.Advance(.1);
        Assert(ants[0].PartnerId==ants[1].InteractionId && ants[1].PartnerId==ants[0].InteractionId,"Nearby ants form a real cooperative candy-carrying pair");
        Assert(Math.Abs(ants.Sum(x=>x.Carrying)+food.Amount-10)<.001,"Cooperative carrying conserves the shared candy load");
        var saved=world.CaptureEcosystem("synthetic-owner");var restored=new BugManager(7);restored.RestoreEcosystem(Continuity.Clone(saved));
        Assert(ants.All(a=>restored.Bugs.Single(b=>b.InteractionId==a.InteractionId).PartnerId==a.PartnerId),"Cooperative carrying links survive world save and restore");
        var fly=world.Bugs.First(x=>x.Species==BugSpecies.Flies);var fruit=world.Resources.First(x=>x.Kind==BugResourceKind.Fruit);
        fly.Position=fruit.Position;fly.Activity=BugActivity.Gathering;fly.ResourceId=fruit.Id;fly.Decision=30;fly.Carrying=0;world.Advance(.1);
        Assert(fly.Carrying==0 && fly.Activity==BugActivity.Grooming && fly.State==BugState.Paused,"Flies land to feed and groom instead of hauling food like ants");
    }
    private static void RenderAnimationFrames()
    {
        var frameDirectory=Path.Combine(output,"animation-frames");Directory.CreateDirectory(frameDirectory);
        var species=new[]{BugSpecies.Ants,BugSpecies.Spiders,BugSpecies.Ladybugs,BugSpecies.Roaches,BugSpecies.Centipedes,BugSpecies.Flies};
        var bugs=species.Select((s,i)=>new BugEntity{Species=s,InteractionId=Guid.Parse($"00000000-0000-0000-0000-{i+1:000000000000}"),Scale=2}).ToArray();
        var options=new EcosystemOptions{MatchAppearance=false,Appearance=BugAppearance.Creepy,Quality=BugAnimationQuality.High};
        var watch=Stopwatch.StartNew();
        for(var frame=0;frame<48;frame++)
        {
            var visual=new DrawingVisual();var time=frame/12d;
            using(var dc=visual.RenderOpen())
            {
                dc.DrawRectangle(BugVisuals.B("#100D19"),null,new Rect(0,0,960,620));
                for(var i=0;i<bugs.Length;i++)
                {
                    var bug=bugs[i];var x=160+i%3*320;var y=155+i/3*295;
                    bug.CrawlPhase=time*5;bug.Activity=BugActivity.Exploring;bug.State=bug.Species is BugSpecies.Flies or BugSpecies.Ladybugs && frame>=24?BugState.Flying:BugState.Crossing;
                    dc.PushTransform(new TranslateTransform(x,y));dc.PushTransform(new ScaleTransform(bug.Species==BugSpecies.Centipedes?4:5,bug.Species==BugSpecies.Centipedes?4:5));
                    BugVisuals.Draw(dc,bug,options,true,time,new Point(100*Math.Cos(time),100*Math.Sin(time)),1);dc.Pop();dc.Pop();
                    var label=new FormattedText(species[i].ToString(),CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),20,Brushes.PaleGreen,1);
                    dc.DrawText(label,new Point(x-label.Width/2,y+90));
                }
            }
            var image=new RenderTargetBitmap(960,620,96,96,PixelFormats.Pbgra32);image.Render(visual);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var stream=File.Create(Path.Combine(frameDirectory,$"{frame:000}.png"));encoder.Save(stream);
        }
        watch.Stop();File.WriteAllText(Path.Combine(output,"animation-render-measurement.txt"),$"48 offline frames of six creatures at 960x620, including PNG encoding: {watch.Elapsed.TotalMilliseconds:0} ms. This is not an interactive FPS, GPU or extended-session benchmark.");
        Assert(Directory.GetFiles(frameDirectory,"*.png").Length==48,"All six species render across 48 walking and flight preview frames");
    }
}
