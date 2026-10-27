from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('NexaReliability.cs',lambda t:t.replace('    public double FrenzyMultiplier','    public DateTimeOffset? FrenzyStartedAt {get;set;}\n    public DateTimeOffset? LastDirectSquashAt {get;set;}\n    public double FrenzyMultiplier').replace('=>FrenzyUntil>now?', '=>FrenzyUntil>now && (FrenzyStartedAt==null || now>=FrenzyStartedAt)?').replace('''        ledger.FrenzyMultiplier=Math.Min(maximum,ledger.FrenzyUntil>now?ledger.Multiplier(now)+.5:1.5);
        ledger.BugsSquashed++; ledger.FrenzyActivations++; ledger.FrenzyUntil = now.AddMinutes(2).ToUniversalTime();''','''        ledger.BugsSquashed++;ledger.FrenzyActivations++;
        if(ledger.LastDirectSquashAt==null || now>=ledger.LastDirectSquashAt)
        {
            var active=ledger.FrenzyUntil>now;
            ledger.FrenzyMultiplier=Math.Min(maximum,active?ledger.Multiplier(now)+.5:1.5);
            if(!active)ledger.FrenzyStartedAt=now.ToUniversalTime();
            ledger.LastDirectSquashAt=now.ToUniversalTime();ledger.FrenzyUntil=now.AddMinutes(2).ToUniversalTime();
        }'''))
edit('MainWindow.Reliability.cs',lambda t:t.replace('    private void BuildProductivityWidget','    private readonly Dictionary<Guid,FrameworkElement> scoreAreas=[];\n    private void BuildProductivityWidget').replace('''        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) };
        panel.Children.Add(text);''','''        TextBlock? prominent=null,multiplier=null;
        if(score)
        {
            var center=new StackPanel{HorizontalAlignment=HorizontalAlignment.Stretch,Margin=new Thickness(0,8,0,10)};
            var bolt=new System.Windows.Shapes.Path{Data=System.Windows.Media.Geometry.Parse("M12,0 L2,14 L10,14 L7,27 L23,10 L14,10 L18,0 Z"),Width=30,Height=36,Stretch=System.Windows.Media.Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center};bolt.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"PixelBrush");center.Children.Add(bolt);
            prominent=new TextBlock{FontSize=42,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center};center.Children.Add(prominent);
            multiplier=new TextBlock{FontSize=22,FontWeight=FontWeights.SemiBold,HorizontalAlignment=HorizontalAlignment.Center};center.Children.Add(multiplier);
            var area=new Border{Child=center,Padding=new Thickness(12),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,0,8)};area.SetResourceReference(Border.BackgroundProperty,"SurfaceBrush");panel.Children.Add(area);scoreAreas[widget.Id]=area;
        }
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 10) };
        panel.Children.Add(text);''').replace('            var ledger = state.Productivity;','            var ledger = state.Productivity;\n            if(prominent!=null){prominent.Text=ledger.Score.ToString("N0");multiplier!.Text=ledger.Multiplier(now).ToString("0.0")+"×";}').replace('score ? $"Session score: {ledger.Score:N0}\\nBase:', 'score ? $"Base:'))
edit('MainWindow.Widgets.cs',lambda t:t.replace('WidgetCanvas.Children.Clear(); widgetFrames.Clear();','WidgetCanvas.Children.Clear(); scoreAreas.Clear(); widgetFrames.Clear();'))
edit('MainWindow.Feedback.cs',lambda t:t.replace('FrameworkElement? element = id is { } value && widgetFrames.TryGetValue(value, out var frame) ? frame : null;','FrameworkElement? element=id is {} scoreId && scoreAreas.TryGetValue(scoreId,out var scoreArea)?scoreArea:id is {} value && widgetFrames.TryGetValue(value,out var frame)?frame:null;'))
edit('NexaFeedback.cs',lambda t:t.replace('    private bool disposed;','    private bool disposed;\n    private double edgeStarted=-10;\n    private bool edgeNegative;').replace('if (item.BasePoints + item.BonusPoints <= 0) continue;','if (item.BasePoints + item.BonusPoints == 0) continue;\n            edgeNegative=item.BasePoints+item.BonusPoints<0;edgeStarted=clock.Elapsed.TotalSeconds;').replace('"+" + (item.BasePoints + item.BonusPoints)','(item.BasePoints+item.BonusPoints>0?"+":"") + (item.BasePoints + item.BonusPoints)').replace('if (effects.Count == 0 && !(frenzyUntil > DateTimeOffset.UtcNow && frenzyBounds != null)) tick.Stop();','if (effects.Count == 0 && clock.Elapsed.TotalSeconds-edgeStarted>1.2 && !(frenzyUntil > DateTimeOffset.UtcNow && frenzyBounds != null)) tick.Stop();').replace('        var particles = 0;','''        var ageOfEdge=time-edgeStarted;
        if(ageOfEdge>=0 && ageOfEdge<1.2 && ActualWidth>16 && ActualHeight>16)
        {
            var opacity=Math.Sin(ageOfEdge/1.2*Math.PI)*.55;dc.PushOpacity(opacity);
            var edge=edgeNegative?"#EF4444":"#22C55E";var perimeter=new Rect(5,5,ActualWidth-10,ActualHeight-10);
            dc.DrawRoundedRectangle(null,BugVisuals.P(edge,6),perimeter,9,9);dc.Pop();
        }
        var particles = 0;'''))
print('Added centered score/lightning/streak UI, saved-event perimeter feedback and stale-event-safe streak timestamps.')
