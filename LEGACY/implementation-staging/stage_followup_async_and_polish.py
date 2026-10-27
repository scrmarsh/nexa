from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('SplashWindow.cs',lambda t:t.replace('        Report(.6,"Workspace loaded and validated");','        return await Dispatcher.InvokeAsync(async ()=>\n        {\n        Report(.6,"Workspace loaded and validated");').replace('home.Opacity=1;Close();return home;','home.Opacity=1;Close();return home;\n        }).Task.Unwrap();'))
edit('MainWindow.xaml.cs',lambda t:t.replace('    internal void UpdateClock(DateTimeOffset? at = null)','    private DateTimeOffset displayedClockInstant;\n    internal void UpdateClock(DateTimeOffset? at = null)').replace('        var now = at ?? DateTimeOffset.UtcNow;\n        ReconcileSchedule(now);','        var now = at ?? DateTimeOffset.UtcNow;displayedClockInstant=now;\n        ReconcileSchedule(now);'))
edit('MainWindow.Widgets.cs',lambda t:t.replace('foreach (var tick in widgetTicks) tick(now);','foreach (var tick in widgetTicks) tick(displayedClockInstant==default?now:displayedClockInstant);'))
edit('PixelTiming.cs',lambda t:t.replace('reading.CountUp?Math.Floor(fraction*count)','reading.CountUp || reading.Status=="SESSION ENDED"?Math.Floor(fraction*count)'))
edit('HomeWindow.About.cs',lambda t:t.replace('    private void BuildHomeHeader','    private Action? refreshBranding;\n    internal void RefreshBranding()=>refreshBranding?.Invoke();\n    private void BuildHomeHeader').replace('        typing.Tick += (_, _) =>','        refreshBranding=()=>{var phrase=NexaBranding.Tagline;position=Math.Min(position,phrase.Length);NexaBranding.ApplyTagline(tagline,phrase[..position]);};\n        typing.Tick += (_, _) =>'))
edit('Theme.cs',lambda t:t.replace('                window.Icon=NexaChrome.Icon;','                window.Icon=NexaChrome.Icon;if(window is HomeWindow home)home.RefreshBranding();'))
# Flames use the central score area; motion only starts/stops on active-state changes.
edit('MainWindow.Reliability.cs',lambda t:t.replace('        TextBlock? prominent=null,multiplier=null;','        TextBlock? prominent=null,multiplier=null;\n        System.Windows.Shapes.Path? lightning=null;bool boltActive=false;').replace('bolt.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"PixelBrush");center.Children.Add(bolt);','''bolt.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"PixelBrush");center.Children.Add(bolt);lightning=bolt;bolt.RenderTransformOrigin=new Point(.5,.5);bolt.RenderTransform=new System.Windows.Media.ScaleTransform(1,1);
            bolt.Unloaded+=(_,_)=>{if(bolt.RenderTransform is System.Windows.Media.ScaleTransform scale){scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty,null);scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty,null);}};''').replace('            if(prominent!=null){prominent.Text=ledger.Score.ToString("N0");multiplier!.Text=ledger.Multiplier(now).ToString("0.0")+"×";}','''            if(prominent!=null)
            {
                prominent.Text=ledger.Score.ToString("N0");multiplier!.Text=ledger.Multiplier(now).ToString("0.0")+"×";
                var active=ledger.Multiplier(now)>1 && NexaMotion.Allowed(Options) && Options.CelebrationEffects;
                if(active!=boltActive && lightning?.RenderTransform is System.Windows.Media.ScaleTransform scale)
                {
                    boltActive=active;var pulse=active?new System.Windows.Media.Animation.DoubleAnimation(.96,1.06,TimeSpan.FromSeconds(1.2)){AutoReverse=true,RepeatBehavior=System.Windows.Media.Animation.RepeatBehavior.Forever}:null;
                    scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty,pulse);scale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty,pulse);
                }
            }'''))
edit('NexaFeedback.cs',lambda t:t.replace('for (var i = 0; i < 6; i++)','for (var i = 0; i < 16; i++)').replace('var x = frame.Left + 20 + i * Math.Min(22, Math.Max(8, (frame.Width - 40) / 6)); var y = frame.Bottom - 10;','''var x=i<8?frame.Left+16+i*Math.Max(1,(frame.Width-32)/7):i<12?frame.Left+8:frame.Right-8;
                var y=i<8?frame.Bottom-10:frame.Top+25+(i%4)*Math.Max(1,(frame.Height-45)/3);''').replace('dc.DrawGeometry(BugVisuals.B(green), null, shape);','dc.DrawGeometry(BugVisuals.B(Theme.SeasonalEnabled && i%4==0?"#F97316":green), null, shape);'))
print('Marshaled startup UI work to its dispatcher; synchronized incidental widget refresh, live tagline restoration and centered streak effects.')
