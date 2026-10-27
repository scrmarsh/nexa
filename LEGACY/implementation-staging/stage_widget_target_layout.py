from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('Nexaween.cs',lambda t:t.replace('''        var occupied = widgetFrames.Values.Select(f => new Rect(Canvas.GetLeft(f), Canvas.GetTop(f), f.Width, f.Height)).ToList();''','''        var widgetTargets=widgetFrames.Values.Select(f=>f.TransformToVisual(WidgetCanvas).TransformBounds(new Rect(0,0,f.Width,f.Height))).ToArray();
        var occupied=widgetTargets.ToList();''').replace('widgetTargets:widgetFrames.Values.Select(f=>new Rect(Canvas.GetLeft(f),Canvas.GetTop(f),f.Width,f.Height)).ToArray()','widgetTargets:widgetTargets'))
edit('Nexaween.Placement.cs',lambda t:t.replace('new Rect(rect.Right-size*.4,rect.Bottom-size*.18,size,size)','new Rect(Math.Clamp(rect.Right-size*.4,0,Math.Max(0,Width-size)),rect.Bottom-size*.18,size,size)'))
edit('MainWindow.WidgetPlacement.cs',lambda t:t.replace('''    private void UpdateWidgetDropPreview(WidgetInstance moved, double left, double top)
    {''','''    private void UpdateWidgetDropPreview(WidgetInstance moved, double left, double top)
    {
        RefreshSeasonalWorkspace();''').replace('''            placementPreview.Width = active.Width; placementPreview.Height = active.Height;
        }
    }''','''            placementPreview.Width = active.Width; placementPreview.Height = active.Height;
        }
        RefreshSeasonalWorkspace();
    }''',1).replace('if (ReferenceEquals(frame.RenderTransform, transform)) frame.RenderTransform = Transform.Identity;','if (ReferenceEquals(frame.RenderTransform, transform)) frame.RenderTransform = Transform.Identity; RefreshSeasonalWorkspace();'))
edit('MainWindow.Widgets.cs',lambda t:t.replace('            ResizeWidgetVisual(widget,candidate,resizeOrigin);','            ResizeWidgetVisual(widget,candidate,resizeOrigin);RefreshSeasonalWorkspace();').replace('''                UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);
                return;''','''                UpdateWidgetDropPreview(widget, origin.X + dx, origin.Y + dy);RefreshSeasonalWorkspace();
                return;'''))
print('Kept attachment rectangles in canvas coordinates through existing drag, resize and transition-completion hooks.')
