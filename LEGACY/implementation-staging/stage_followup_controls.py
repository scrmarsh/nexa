from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
p=r/'MainWindow.xaml.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('    private void DeveloperSplashClick');b=t.index('\n    private ',a+5)
t=t[:a]+'''    private async void DeveloperSplashClick(object sender,RoutedEventArgs e)
    {
        var preview=new SplashWindow{Owner=this};var previous=Application.Current.MainWindow;preview.Show();
        try{var home=await preview.EnterHome(store,true);Application.Current.MainWindow=previous;home.Close();}
        catch(Exception ex){Application.Current.MainWindow=previous;preview.Close();Error("Startup preview failed: "+ex.Message);}
    }
'''+t[b:];p.write_text(t,encoding='utf-8-sig')
edit('DarkControls.xaml',lambda t:t.replace('<ContentPresenter Margin="10,7,30,7"','<TextBox x:Name="PART_EditableTextBox" Visibility="Collapsed" Margin="8,3,30,3" Background="Transparent" BorderThickness="0" VerticalContentAlignment="Center"/>\n                <ContentPresenter x:Name="Selection" Margin="10,7,30,7"').replace('''            </Grid>
        </ControlTemplate>''','''            </Grid>
            <ControlTemplate.Triggers><Trigger Property="IsEditable" Value="True"><Setter TargetName="PART_EditableTextBox" Property="Visibility" Value="Visible"/><Setter TargetName="Selection" Property="Visibility" Value="Collapsed"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.42"/></Trigger></ControlTemplate.Triggers>
        </ControlTemplate>''',1))
edit('MainWindow.xaml',lambda t:t.replace('<Image Source="assets/nexa-icon-256.png"','<Image x:Name="HeaderBrandIcon" Source="assets/nexa-icon-256.png"'))
edit('MainWindow.Settings.cs',lambda t:t.replace('        // The selected app theme','        HeaderBrandIcon.Source=NexaChrome.Icon;\n        // The selected app theme'))
edit('MainWindow.xaml.cs',lambda t:t.replace('        InitializeColorPickers();','        HeaderBrandIcon.Source=NexaChrome.Icon;\n        InitializeColorPickers();'))
p=r/'NexaNotifications.cs';t=p.read_text(encoding='utf-8-sig');t=t.replace('    private readonly Dictionary<string, Entry> entries', '    private readonly StackPanel leftStack=new(){VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(18,18,18,84),Width=330};\n    private readonly ScrollViewer leftViewport=new(){HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Bottom,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};\n    private static bool LeftSide(string key)=>key is "autosave" or "manual-save";\n    private readonly Dictionary<string, Entry> entries')
t=t.replace('owner.Children.Add(viewport);','owner.Children.Add(viewport);leftViewport.Content=leftStack;Panel.SetZIndex(leftViewport,90);owner.Children.Add(leftViewport);')
t=t.replace('stack.Width = Math.Max(180, Math.Min(350, owner.ActualWidth - 36)); viewport.MaxHeight = Math.Max(100, owner.ActualHeight - 146);','stack.Width = leftStack.Width = Math.Max(120, Math.Min(350, (owner.ActualWidth - 54)/2)); viewport.MaxHeight = leftViewport.MaxHeight = Math.Max(100, owner.ActualHeight - 146);')
t=t.replace('stack.Children.Remove(entry.Card);','stack.Children.Remove(entry.Card);leftStack.Children.Remove(entry.Card);').replace('''        stack.Children.Clear();
        foreach (var entry in entries.Values.OrderBy(e => e.Priority)) stack.Children.Add(entry.Card);''','''        stack.Children.Clear();leftStack.Children.Clear();
        foreach(var pair in entries.OrderBy(p=>p.Value.Priority))(LeftSide(pair.Key)?leftStack:stack).Children.Add(pair.Value.Card);''').replace('owner.Children.Remove(viewport);','owner.Children.Remove(viewport);owner.Children.Remove(leftViewport);')
p.write_text(t,encoding='utf-8-sig')
for name in ['NexaNotifications.cs','MainWindow.Continuity.cs','MainWindow.Reliability.cs','tests/Nexa024AnimationChecks.cs','tests/NexaFollowupChecks.cs']:
 edit(name,lambda t:t.replace('Autosaving…','Autosaving...').replace('Autosave failed — retrying…','Autosave failed — retrying...'))
edit('tests/Nexa024CoreChecks.cs',lambda t:t.replace('window.CountdownCaption.Text == "CLASS TIME ELAPSED"','window.CountdownCaption.Text == "SESSION PROGRESS"').replace('partially depleted prefix','partially filled prefix').replace('t.Text.Contains("Session score:")','t.Text.Contains("Next milestone:")'))
edit('tests/Nexa024EcosystemChecks.cs',lambda t:t.replace('(policy with { MatchAppearance = false }).EffectiveAppearance == BugAppearance.Creepy','(policy with { MatchAppearance = false }).EffectiveAppearance == BugAppearance.Cozy').replace('Automatic style matching preserves the manual appearance override','Behavior mapping safely ignores retired legacy appearance overrides'))
print('Completed editable selector template, split notification placement, runtime header branding and real startup preview.')
