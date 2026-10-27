from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'BugNavigation.cs',r/'BugNavigation.cs')
edit('NexaEcosystem.Simulation.cs',lambda t:t.replace('var delta = bug.Target - bug.Position;','''var navigationTarget=VisibleWaypoint(bug.Position,bug.Target);
            // A moved/resized widget can engulf a creature; place it on a free edge once.
            if(Surfaces.Values.Any(rect=>rect.Contains(bug.Position)) && !Surfaces.Values.Any(rect=>rect.Contains(navigationTarget)))bug.Position=navigationTarget;
            var delta = navigationTarget - bug.Position;''').replace('bug.State=BugState.Paused; continue;','bug.State=BugState.Paused; bug.FlightHeight=Math.Max(0,bug.FlightHeight-step*3); continue;'))
edit('BugOverlay.cs',lambda t:t.replace('{ ContentOrigin = origin; scaleX = size.Width / BugManager.WorldWidth;','{ if(ContentOrigin==origin && Math.Abs(scaleX-size.Width/BugManager.WorldWidth)<.000001 && Math.Abs(scaleY-size.Height/BugManager.WorldHeight)<.000001)return; ContentOrigin = origin; scaleX = size.Width / BugManager.WorldWidth;').replace('{protectedWidgets=rectangles.ToArray();RebuildProtectedClip();}','{var next=rectangles.ToArray();if(protectedWidgets.SequenceEqual(next))return;protectedWidgets=next;RebuildProtectedClip();}'))
edit('MainWindow.Bugs.cs',lambda t:t.replace('var surfaces = widgetFrames.Select','IEnumerable<BugSurface> surfaces = widgetFrames.Select').replace('            bugsOverlay.Manager.SetSurfaces(surfaces,','            if(PixelObstacle is {} pixelBounds)surfaces=surfaces.Append(new BugSurface("pixel-timer",new Rect(pixelBounds.X*scaleX,pixelBounds.Y*scaleY,pixelBounds.Width*scaleX,pixelBounds.Height*scaleY)));\n            bugsOverlay.Manager.SetSurfaces(surfaces,'))
print('Added widget-edge path routing, engulfed-creature recovery and cached projection/clip geometry.')
