from pathlib import Path
root=Path(r'M:\Nexa');stage=Path(__file__).parent
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
write('MainWindow.Feedback.cs',(stage/'MainWindow.Feedback.cs').read_text(encoding='utf-8'))
text=read('NexaReliability.cs').replace('    public Guid Id { get; set; } = Guid.NewGuid();\n    public string Key','    public Guid Id { get; set; } = Guid.NewGuid();\n    public Guid? WidgetId { get; set; }\n    public string Key',1)
text=text.replace('DateTimeOffset now, bool once = true)','DateTimeOffset now, bool once = true, Guid? widgetId = null)')
text=text.replace('new() { Key = key, Category = category,','new() { WidgetId = widgetId, Key = key, Category = category,')
text=text.replace('void Tasks(IEnumerable<TaskItem> old, IEnumerable<TaskItem> current)','void Tasks(IEnumerable<TaskItem> old, IEnumerable<TaskItem> current, Guid? widgetId = null)')
start=text.index('        void Tasks(');end=text.index('        void Note(',start)
text=text[:start]+text[start:end].replace(', now);',', now, widgetId: widgetId);')+text[end:]
text=text.replace('void Note(string key, string old, string text)','void Note(string key, string old, string text, Guid? widgetId = null)')
text=text.replace('"Meaningful note saved", 20, now);','"Meaningful note saved", 20, now, widgetId: widgetId);')
text=text.replace('Tasks(old.Items, w.Items);','Tasks(old.Items, w.Items, w.Id);').replace('Note(w.Id.ToString(), old.Text, w.Text);','Note(w.Id.ToString(), old.Text, w.Text, w.Id);')
start=text.index('        foreach (var w in after.Widgets)');end=text.index('        if (after.Session is',start)
text=text[:start]+text[start:end].replace(', now);',', now, widgetId: w.Id);').replace(', snapshot.CapturedAt);',', snapshot.CapturedAt, widgetId: w.Id);')+text[end:]
text=text.replace('w.Title + " completed", 75, now);','w.Title + " completed", 75, now, widgetId: w.Id);')
write('NexaReliability.cs',text)
text=read('MainWindow.Reliability.cs').replace('notifications = new NexaNotifications((Grid)Content);','notifications = new NexaNotifications((Grid)Content, () => NexaMotion.Allowed(Options));\n        InitializeFeedback();')
text=text.replace('        ApplySeasonalOptions();\n        if (!Options.ShowQuickTips)', '        ApplySeasonalOptions();\n        feedbackOverlay?.ReconcileOptions(); notifications?.ReconcileMotion();\n        if (!Options.ShowQuickTips)')
write('MainWindow.Reliability.cs',text)
text=read('MainWindow.Continuity.cs')
text=text.replace('        if(text=="saved") { lastSuccessfulSaveAt','        if(text=="saved") { SavedFeedback(); lastSuccessfulSaveAt')
text=text.replace('if(saving) rotation.BeginAnimation','if(saving && NexaMotion.Allowed(Options)) rotation.BeginAnimation')
write('MainWindow.Continuity.cs',text)
text=read('MainWindow.Productivity.cs').replace('    {\n        var menu = new ContextMenu();','    {\n        checkbox.Checked += (_, _) => NexaMotion.Pop(checkbox, Options);\n        var menu = new ContextMenu();')
write('MainWindow.Productivity.cs',text)
text=read('MainWindow.xaml.cs')
text=text.replace('        var head = NexaTime.LitPixels(', '        UpdateFrenzyFeedback();\n        var head = NexaTime.LitPixels(')
text=text.replace('if (milestones.Count > 0) { flashUntil', 'if (milestones.Count > 0) { if(FeedbackBounds(state.Widgets.FirstOrDefault(w=>w.PanelKey=="pixels")?.Id) is {} milestoneBounds) feedbackOverlay?.Show("milestone",milestoneBounds,milestones.Last()); flashUntil')
write('MainWindow.xaml.cs',text)
text=read('SettingsDialog.Reliability.cs')
text=text.replace('        page.Children.Add(tips); return page;','''        page.Children.Add(tips);
        var reduced=new CheckBox{Content="Reduce motion",IsChecked=ReliabilityOptions.ReduceMotion,Margin=new Thickness(0,14,0,8)};
        var effects=new CheckBox{Content="Completion celebrations and score effects",IsChecked=ReliabilityOptions.CelebrationEffects,Margin=new Thickness(0,0,0,8)};
        void SaveEffects(){ReliabilityOptions.ReduceMotion=reduced.IsChecked==true;ReliabilityOptions.CelebrationEffects=effects.IsChecked==true;space?.ApplyReliabilityOptions();NexaEcosystem.UpdateSeason(store,ReliabilityOptions);QueueSave("effects",SaveState);}
        reduced.Click+=(_,_)=>SaveEffects();effects.Click+=(_,_)=>SaveEffects();page.Children.Add(reduced);page.Children.Add(effects);
        page.Children.Add(new TextBlock{Text="Windows reduced-animation preferences are always respected. Important feedback remains available with animation disabled.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,0)});
        return page;''')
write('SettingsDialog.Reliability.cs',text)
text=read('NexaMotion.cs').replace('new DoubleAnimation(1.16, 1, TimeSpan.FromMilliseconds(220)) {','new DoubleAnimation(1.16, 1, TimeSpan.FromMilliseconds(220)) { FillBehavior=FillBehavior.Stop,')
write('NexaMotion.cs',text)
text=read('NexaNotifications.cs').replace('using System.Windows.Media.Animation;','using System.Windows.Media.Animation;\nusing System.Windows.Media.Effects;')
text=text.replace('record Entry(Border Card, TextBlock Glyph, DateTimeOffset? Until, int Priority)','record Entry(Border Card, TextBlock Glyph, string Message, DateTimeOffset? Until, int Priority)')
text=text.replace('    private readonly Grid owner;','    private readonly Grid owner;\n    private readonly Func<bool> motionAllowed;')
text=text.replace('internal NexaNotifications(Grid owner)','internal NexaNotifications(Grid owner, Func<bool>? motionAllowed = null)')
text=text.replace('        this.owner = owner;','        this.motionAllowed=motionAllowed ?? (()=>SystemParameters.ClientAreaAnimation);\n        this.owner = owner;')
text=text.replace('entry.Glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null);','entry.Glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty, null); entry.Glyph.BeginAnimation(OpacityProperty,null);')
start=text.index('        if (key == "autosave" && message == "Autosaving…"');end=text.index('        DockPanel.SetDock(glyph, Dock.Left);',start)
text=text[:start]+'        ConfigureGlyph(key,glyph,message);\n'+text[end:]
text=text.replace('entries[key] = new(card, glyph, seconds','entries[key] = new(card, glyph, message, seconds')
needle='    private void Tick('
index=text.index(needle)
text=text[:index]+'''    private void ConfigureGlyph(string key,TextBlock glyph,string message)
    {
        glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,null);glyph.BeginAnimation(OpacityProperty,null);glyph.Opacity=1;glyph.Effect=null;
        if(!motionAllowed())return;
        if(key=="autosave" && message=="Autosaving…")glyph.RenderTransform.BeginAnimation(RotateTransform.AngleProperty,new DoubleAnimation(0,360,TimeSpan.FromSeconds(1)){RepeatBehavior=RepeatBehavior.Forever});
        if(key=="tip")
        {
            glyph.Effect=new DropShadowEffect{Color=Color.FromRgb(250,204,21),BlurRadius=8,ShadowDepth=0,Opacity=.4};
            glyph.BeginAnimation(OpacityProperty,new DoubleAnimation(.65,1,TimeSpan.FromSeconds(1.6)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever});
        }
    }
    internal void ReconcileMotion(){foreach(var pair in entries)ConfigureGlyph(pair.Key,pair.Value.Glyph,pair.Value.Message);}
'''+text[index:]
write('NexaNotifications.cs',text)
# Additional anatomical and lifecycle corrections to the new animation path.
text=read('BugAnimation.cs')
text=text.replace('var count = FillLegs(bug.Species, phase, resting, poses);','var count = FillLegs(bug.Species, bug.Activity==BugActivity.Grooming?0:phase, resting, poses);')
text=text.replace('            var p = poses[i];','''            var p = poses[i];
            if(bug.Activity==BugActivity.Grooming && i>=count-2)
            {var side=i%2==0?-1:1;var x=bug.Species==BugSpecies.Centipedes?18:6;p=new(p.Root,new Point(x-1,side*2),new Point(x,side),new Point(x+1+Math.Sin(phase)*.5,side*.35),false);}
            if(bug.State==BugState.Flying && FunctionalWings(bug.Species)>0)p=new(p.Root,p.Root+(p.Knee-p.Root)*.45,p.Root+(p.Ankle-p.Root)*.3,p.Root+(p.Foot-p.Root)*.3,false);''')
write('BugAnimation.cs',text)
text=read('BugVisuals.Animation.cs').replace('double movementScale)','double movementScale, bool activeEyes=true)')
text=text.replace('var elapsed = bug.LastEyeTime <= 0 ? .033 : time - bug.LastEyeTime; bug.LastEyeTime = time;','var elapsed = bug.LastEyeTime <= 0 ? .033 : time - bug.LastEyeTime; if(activeEyes)bug.LastEyeTime=time;')
text=text.replace('bug.Gaze = BugAnimation.SmoothGaze(bug.Gaze, local, elapsed);','if(activeEyes)bug.Gaze = BugAnimation.SmoothGaze(bug.Gaze, local, elapsed);')
write('BugVisuals.Animation.cs',text)
text=read('BugOverlay.cs').replace('NexaMotion.Allowed(Manager.VisualOptions) && !Manager.WorldPaused','NexaMotion.Allowed(Manager.VisualOptions)')
text=text.replace('Manager.SimulationAge,look,motionScale);','Manager.SimulationAge,look,motionScale,Window.GetWindow(this)?.IsActive==true);')
write('BugOverlay.cs',text)
text=read('NexaEcosystem.Simulation.cs').replace('bug.State = BugState.Paused; bug.Energy','bug.State = BugState.Paused; bug.FlightHeight=Math.Max(0,bug.FlightHeight-step*3); bug.Energy')
write('NexaEcosystem.Simulation.cs',text)
text=read('Nexaween.Animation.cs').replace('    private int lastSurprise = -1;','    private int lastSurprise = -1;\n    private Point? cursor;\n    internal void SetCursor(Point point){cursor=point;}')
text=text.replace('lastSurprise >= 0 && animationOptions.SpookySurprises && animationOptions.SeasonalSounds','lastSurprise >= 0 && animationOptions.SeasonalSounds')
needle='            dc.PushTransform(new TranslateTransform(x, y));'
text=text.replace(needle,'''            if(actor.Kind==2 && cursor is {} pointer){var away=new Point(x+actor.Bounds.Width/2,y+actor.Bounds.Height/2)-pointer;if(away.Length>1 && away.Length<110){away.Normalize();x+=away.X*5;y+=away.Y*5;}}
'''+needle)
write('Nexaween.Animation.cs',text)
print('Connected saved-event celebrations, Bug Frenzy witchfire, reduced-motion controls, animated tips, and species-correct flight/grooming details.')
