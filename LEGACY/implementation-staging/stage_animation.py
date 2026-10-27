from pathlib import Path
import shutil
root=Path(r'M:\Nexa');stage=Path(__file__).parent
backup=Path(r'M:\Aevoria-old\Nexa-embedded-20261026\before-animation');backup.mkdir(parents=True,exist_ok=True)
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):
    p=root/n;dest=backup/n
    if p.exists() and not dest.exists():dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,dest)
    p.write_text(t,encoding='utf-8')
def method(text,signature):
    start=text.index(signature);brace=text.index('{',start);depth=1;end=brace+1
    while depth:
        if text[end]=='{':depth+=1
        elif text[end]=='}':depth-=1
        end+=1
    return text[start:end]
for n in ['BugAnimation.cs','BugVisuals.Animation.cs','NexaMotion.cs','NexaFeedback.cs','Nexaween.Animation.cs']:write(n,(stage/n).read_text(encoding='utf-8'))
text=read('DashboardBugs.cs').replace('    internal double Scale=2, Health=100, Energy=1, Carrying, OffscreenTime;','    internal double Scale=2, Health=100, Energy=1, Carrying, OffscreenTime, LastEyeTime, FlightHeight;\n    internal Vector Gaze;')
write('DashboardBugs.cs',text)
text=read('NexaEcosystem.Simulation.cs').replace('    internal bool SeasonalCandy { get; set; }','    internal bool SeasonalCandy { get; set; }\n    internal NexaOptions VisualOptions { get; set; } = new();')
text=text.replace('Webs[i].Construction = Math.Min(1, Webs[i].Construction + dt * .025); ','')
needle='            bug.State = bug.Species == BugSpecies.Flies ? BugState.Flying : BugState.Crossing;'
text=text.replace(needle,'''            var flight = bug.Species == BugSpecies.Flies || bug.Species == BugSpecies.Ladybugs && bug.Activity == BugActivity.Exploring && bug.Age % 45 < 10;
            bug.State = flight ? BugState.Flying : BugState.Crossing;
            bug.FlightHeight = Math.Clamp(bug.FlightHeight + step * (flight ? 1.8 : -3), 0, 1);''')
needle='                    // Ants recruit a nearby partner for large candy loads.'
text=text.replace(needle,'''                    if (bug.Species is BugSpecies.Flies or BugSpecies.Ladybugs or BugSpecies.Spiders or BugSpecies.Centipedes)
                    {
                        resource.Amount = Math.Max(0, resource.Amount - .2); bug.Energy = Math.Min(1,bug.Energy+.2); bug.Health=Math.Min(100,bug.Health+3);
                        bug.Carrying=0;bug.Activity=BugActivity.Grooming;bug.State=BugState.Paused;bug.Decision=4;continue;
                    }
'''+needle)
needle='            if (bug.Activity == BugActivity.Nesting) { nest.Construction = Math.Min(1, nest.Construction + step * .001); bug.Target = nest.Position; }'
text=text.replace(needle,'''            if (bug.Activity == BugActivity.Nesting)
            {
                if(bug.Species==BugSpecies.Spiders && Webs.FirstOrDefault(w=>w.Owner==bug.InteractionId && w.Construction<1) is {} web)
                {bug.Target=new(web.X,web.Y);if((bug.Position-bug.Target).Length<20){web.Construction=Math.Min(1,web.Construction+step*.025);bug.State=BugState.Paused;continue;}}
                else {nest.Construction=Math.Min(1,nest.Construction+step*.001);bug.Target=nest.Position;}
            }''')
text=text.replace('bug.Angle = Math.Atan2(delta.Y, delta.X) * 180 / Math.PI;', 'bug.Angle = BugAnimation.SmoothAngle(bug.Angle,Math.Atan2(delta.Y,delta.X)*180/Math.PI,step,bug.Species==BugSpecies.Roaches?420:280);')
needle='        if (bug.Carrying > 0) { bug.Activity = BugActivity.Carrying;'
text=text.replace(needle,'''        if(bug.Species==BugSpecies.Spiders && Webs.FirstOrDefault(w=>w.Owner==bug.InteractionId && w.Construction<1) is {} unfinished)
        {bug.Activity=BugActivity.Nesting;bug.Target=new(unfinished.X,unfinished.Y);bug.Decision=8;return;}
'''+needle)
write('NexaEcosystem.Simulation.cs',text)
text=read('NexaEcosystem.Store.cs').replace('        seasonalOptions = options;','        seasonalOptions = options; Manager.VisualOptions=options;')
write('NexaEcosystem.Store.cs',text)
text=read('BugArtwork.cs')
text=text.replace('    private static Drawing Make(BugSpecies species,int frame,bool hurt)','    private static Drawing Make(BugSpecies species,int frame,bool hurt,bool limbs=true)')
text=text.replace('for(var segment=0;segment<9;segment++)','for(var segment=0;segment<15;segment++)').replace('var x=-13+segment*2.9;','var x=-15+segment*2.15;')
text=text.replace('                    for(var side=-1;side<=1;side+=2)','                    if(limbs) for(var side=-1;side<=1;side+=2)',1)
text=text.replace('new Point(x,y),2.2,2.5','new Point(x,y),1.7,2.5')
text=text.replace('new Point(13,0),2.8,3','new Point(17,0),2.8,3')
text=text.replace('                dc.DrawLine(Legs,new Point(14,-1),new Point(18,-5));','                if(limbs) dc.DrawLine(Legs,new Point(18,-1),new Point(24,-5));').replace('                dc.DrawLine(Legs,new Point(14,1),new Point(18,5));','                if(limbs) dc.DrawLine(Legs,new Point(18,1),new Point(24,5));')
text=text.replace('                    for(var leg=0;leg<4;leg++)','                    if(limbs) for(var leg=0;leg<4;leg++)')
text=text.replace('var x=-5+leg*3;','var x=1+leg*1.55;')
text=text.replace('                else for(var leg=-1;leg<=1;leg++)','                else if(limbs) for(var leg=-1;leg<=1;leg++)')
text=text.replace('if(species == BugSpecies.Flies)\n                { dc.DrawEllipse','if(species == BugSpecies.Flies && limbs)\n                { dc.DrawEllipse')
text=text.replace('                dc.DrawLine(Legs,new Point(length,-1),new Point(length+3,-4)); dc.DrawLine(Legs,new Point(length,1),new Point(length+3,4));','                if(limbs) {dc.DrawLine(Legs,new Point(length,-1),new Point(length+(species==BugSpecies.Spiders?1.5:3),species==BugSpecies.Spiders?-2:-4)); dc.DrawLine(Legs,new Point(length,1),new Point(length+(species==BugSpecies.Spiders?1.5:3),species==BugSpecies.Spiders?2:4));}')
needle='    private static Drawing MakeWeb()'
text=text.replace(needle,'''    private static readonly Dictionary<(BugSpecies,bool),Drawing> bodyOnly=[];
    internal static Drawing BodyOnly(BugEntity bug)
    {
        var key=(bug.Species,bug.Species==BugSpecies.Ladybugs && bug.Hits>0);
        if(!bodyOnly.TryGetValue(key,out var art)){art=Make(key.Item1,0,key.Item2,false);bodyOnly[key]=art;}return art;
    }
'''+needle)
write('BugArtwork.cs',text)
text=read('BugVisuals.cs').replace('internal static class BugVisuals','internal static partial class BugVisuals')
text=text.replace('Dictionary<(BugSpecies, BugAppearance), Drawing>','Dictionary<(BugSpecies, BugAppearance, bool), Drawing>')
text=text.replace('Body(BugSpecies species, BugAppearance appearance)','Body(BugSpecies species, BugAppearance appearance, bool limbs=true)')
text=text.replace('if (appearance == BugAppearance.Normal) return BugArtwork.For(new BugEntity { Species = species });','if (appearance == BugAppearance.Normal) return limbs ? BugArtwork.For(new BugEntity { Species = species }) : BugArtwork.BodyOnly(new BugEntity { Species=species });')
text=text.replace('var key = (species, appearance);','var key = (species, appearance, limbs);')
text=text.replace('species == BugSpecies.Centipedes ? 10 : 3','species == BugSpecies.Centipedes ? 15 : 3')
text=text.replace('            for (var leg = 0; leg < pairs; leg++)','            if(limbs) for (var leg = 0; leg < pairs; leg++)')
text=text.replace('-15 + leg * 3.1','-15 + leg * 2.15')
text=text.replace('for (var i = 0; i < (cozy ? 8 : 11); i++)','for (var i = 0; i < 15; i++)').replace('-15 + i * (cozy ? 4 : 2.9)','-15 + i * 2.15')
text=text.replace('new(x, 0), 2.8, 3.4','new(x, 0), 1.8, 3.4')
text=text.replace('            if (species is not (BugSpecies.Centipedes or BugSpecies.Spiders))','            if (limbs && species is not (BugSpecies.Centipedes or BugSpecies.Spiders))')
text=text.replace('                dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, -4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);','                if(limbs) dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, -4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);')
text=text.replace('                dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, 4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);','                if(limbs) dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, 4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);')
write('BugVisuals.cs',text)
text=read('BugOverlay.cs')
text=text.replace('    private Rect[] protectedWidgets = [];','    private Rect[] protectedWidgets = [];\n    private Geometry? protectedClip;\n    private Point? cursor;\n    internal IEnumerable<Point> DecorationTargets {get;set;} = [];\n    internal void SetCursor(Point point) => cursor=point;')
text=text.replace('scaleY = size.Height / BugManager.WorldHeight; RefreshFrame();','scaleY = size.Height / BugManager.WorldHeight; RebuildProtectedClip(); RefreshFrame();')
text=text.replace('    internal void SetProtectedWidgets(IEnumerable<Rect> rectangles) => protectedWidgets = rectangles.ToArray();','''    internal void SetProtectedWidgets(IEnumerable<Rect> rectangles) {protectedWidgets=rectangles.ToArray();RebuildProtectedClip();}
    private void RebuildProtectedClip()
    {
        protectedClip=null;if(protectedWidgets.Length==0 || ActualWidth<=0 || ActualHeight<=0)return;
        var holes=new GeometryGroup();foreach(var original in protectedWidgets){var rect=original;rect.Offset(ContentOrigin.X,ContentOrigin.Y);rect.Inflate(-5,-5);if(rect.Width>0&&rect.Height>0)holes.Children.Add(new RectangleGeometry(rect,6,6));}
        var clip=new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(new Rect(RenderSize)),holes);clip.Freeze();protectedClip=clip;
    }
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {base.OnRenderSizeChanged(sizeInfo);RebuildProtectedClip();}''')
start=text.index('        if (protectedWidgets.Length > 0)\n        {');end=text.index('        if (Manager.EcosystemActive) DrawFeatures(dc);',start)
text=text[:start]+'        if(protectedClip!=null)dc.PushClip(protectedClip);\n'+text[end:]
text=text.replace('            var angle = bug.Angle * Math.PI / 180;','''            var worldAngle=bug.Angle*Math.PI/180;
            var angle = Math.Atan2(Math.Sin(worldAngle)*scaleY,Math.Cos(worldAngle)*scaleX);
            var animated=NexaMotion.Allowed(Manager.VisualOptions) && !Manager.WorldPaused;
            if(animated && bug.FlightHeight>0) p.Y-=Math.Sin(Manager.SimulationAge*3+bug.Age)*1.2*bug.FlightHeight+bug.FlightHeight*3;''')
text=text.replace('            BugVisuals.Draw(dc, bug, options, Manager.SeasonalCostumes); dc.Pop();','''            if(animated)
            {
                var look=bug.Target;
                if(cursor is {} pointer && (pointer-p).Length<250)look=ScreenToWorld(pointer);
                else if(bug.ResourceId is {} id && Manager.Resources.FirstOrDefault(r=>r.Id==id) is {} resource)look=resource.Position;
                else if(DecorationTargets.FirstOrDefault(target=>(WorldToScreen(target)-p).Length<130) is var target && target!=default(Point))look=target;
                var motionScale=Math.Sqrt(Math.Pow(scaleX*Math.Cos(worldAngle),2)+Math.Pow(scaleY*Math.Sin(worldAngle),2))/Math.Max(.1,ViewScale);
                BugVisuals.Draw(dc,bug,options,Manager.SeasonalCostumes,Manager.SimulationAge,look,motionScale);
            }
            else BugVisuals.Draw(dc, bug, options, Manager.SeasonalCostumes);
            dc.Pop();''')
text=text.replace('        if (protectedWidgets.Length > 0) dc.Pop();','        if (protectedClip != null) dc.Pop();')
text=text.replace('    internal void RefreshFrame() => InvalidateVisual();','    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);cursor=e.GetPosition(this);}\n    internal void RefreshFrame() => InvalidateVisual();')
write('BugOverlay.cs',text)
text=read('MainWindow.Bugs.cs').replace('            bugsOverlay.SetProtectedWidgets(rectangles);','            bugsOverlay.SetProtectedWidgets(rectangles);')
text=text.replace('        bugsOverlay.SetProtectedWidgets(rectangles);','        bugsOverlay.SetProtectedWidgets(rectangles);\n        bugsOverlay.DecorationTargets=seasonalArtwork?.Anchors.Select(p=>new Point(p.X*BugManager.WorldWidth/WidgetCanvas.ActualWidth,p.Y*BugManager.WorldHeight/WidgetCanvas.ActualHeight)).ToArray() ?? [];')
write('MainWindow.Bugs.cs',text)
text=read('Nexaween.cs').replace('internal sealed class NexaweenDecorations : FrameworkElement','internal sealed partial class NexaweenDecorations : FrameworkElement')
text=text.replace('        var result = new DrawingGroup();','        var result = new DrawingGroup(); actors.Clear();')
text=text.replace('safe.Inflate(12,12)','safe.Inflate(24,24)').replace('safe.Inflate(12, 12)','safe.Inflate(24, 24)')
text=text.replace('                Draw(dc, count % 15); dc.Pop(); dc.Pop(); occupied.Add(bounds); count++;','                var kind=count%15;var asset=Asset(kind);dc.DrawDrawing(asset);actors.Add(new(bounds,kind,asset,count)); dc.Pop(); dc.Pop(); occupied.Add(bounds); count++;')
text=text.replace('        result.Freeze(); drawing = result; InvalidateVisual();','        result.Freeze(); drawing = result; ReconcileAnimation(); InvalidateVisual();')
text=text.replace('    protected override void OnRender(DrawingContext dc) => dc.DrawDrawing(drawing);\n','')
text=text.replace('        seasonalArtwork.Update(Math.Max(1, WidgetCanvas.ActualWidth),','        seasonalArtwork.ConfigureAnimation(Options,new Rect(WidgetScroller.HorizontalOffset,WidgetScroller.VerticalOffset,Math.Max(1,WidgetScroller.ViewportWidth),Math.Max(1,WidgetScroller.ViewportHeight)));\n        seasonalArtwork.Update(Math.Max(1, WidgetCanvas.ActualWidth),')
text=text.replace('        NexaEcosystem.UpdateSeason(store, Options);','        NexaEcosystem.UpdateSeason(store, Options);\n        seasonalArtwork?.ConfigureAnimation(Options,new Rect(WidgetScroller.HorizontalOffset,WidgetScroller.VerticalOffset,Math.Max(1,WidgetScroller.ViewportWidth),Math.Max(1,WidgetScroller.ViewportHeight)));')
write('Nexaween.cs',text)
text=read('InterfaceAudio.cs')
insert=text.rfind('}')
text=text[:insert]+'''    internal static void PreviewSeasonal() => _=Task.Run(()=>
    {
        try
        {
            const int rate=22050,count=8820;using var audio=new MemoryStream();
            using(var writer=new BinaryWriter(audio,System.Text.Encoding.UTF8,true))
            {
                writer.Write("RIFF"u8);writer.Write(36+count*2);writer.Write("WAVEfmt "u8);writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write("data"u8);writer.Write(count*2);
                for(var i=0;i<count;i++){var t=i/(double)rate;var envelope=Math.Sin(Math.PI*i/count)*Math.Exp(-t*7);var wave=(Math.Sin(2*Math.PI*220*t)+.45*Math.Sin(2*Math.PI*261.63*t)+.25*Math.Sin(2*Math.PI*311.13*t))/1.7;writer.Write((short)(wave*envelope*850));}
            }
            audio.Position=0;using var player=new SoundPlayer(audio);player.PlaySync();
        }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception){}
    });
'''+text[insert:]
write('InterfaceAudio.cs',text)
print('Connected species-specific articulated locomotion, expressive eyes, native seasonal actors, and the reusable feedback renderer.')
