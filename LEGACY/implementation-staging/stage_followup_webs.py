from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'NexaWebGeometry.cs',r/'NexaWebGeometry.cs')
edit('NexaEcosystem.Models.cs',lambda t:t.replace('internal sealed class EcosystemWeb\n{','internal sealed class EcosystemWeb\n{\n    public string? AnchorId {get;set;}\n    public double AnchorOffsetX {get;set;}\n    public double AnchorOffsetY {get;set;}'))
edit('NexaEcosystem.Simulation.cs',lambda t:t.replace('Webs[i].Age > 900','Webs[i].Age > 86400 && !Bugs.Any(b=>b.InteractionId==Webs[i].Owner)').replace('(bug.Position-bug.Target).Length<20','(bug.Position-bug.Target).Length<42').replace('''{ Webs.Add(new() { Owner = bug.InteractionId, X = bug.Position.X, Y = bug.Position.Y }); bug.Activity = BugActivity.Nesting; bug.Decision = 8; return; }''','''{
            var anchor=Surfaces.Count>0?Surfaces.MinBy(p=>(new Point(p.Value.Right,p.Value.Top)-bug.Position).LengthSquared):default;
            var origin=Surfaces.Count>0?new Point(anchor.Value.Right+12,anchor.Value.Top-12):new Point(bug.Position.X<WorldWidth/2?24:WorldWidth-24,Math.Clamp(bug.Position.Y,24,WorldHeight-24));
            Webs.Add(new(){Owner=bug.InteractionId,X=origin.X,Y=origin.Y,AnchorId=Surfaces.Count>0?anchor.Key:null,AnchorOffsetX=12,AnchorOffsetY=-12});bug.Activity=BugActivity.Nesting;bug.Target=origin;bug.Decision=8;return;
        }'''))
edit('DashboardBugs.cs',lambda t:t.replace('{Bounds=bounds;Surfaces.Clear();foreach(var s in surfaces)Surfaces[s.Id]=s.Bounds;surfaceChoicesDirty=true;}','''{
        var next=surfaces.ToArray();if(Bounds==bounds && Surfaces.Count==next.Length && next.All(s=>Surfaces.TryGetValue(s.Id,out var prior)&&prior==s.Bounds))return;
        Bounds=bounds;Surfaces.Clear();foreach(var s in next)Surfaces[s.Id]=s.Bounds;surfaceChoicesDirty=true;
        if(EcosystemActive)
        {
            foreach(var web in Webs)if(web.AnchorId is {} id && Surfaces.TryGetValue(id,out var rect)){web.X=Math.Clamp(rect.Right+web.AnchorOffsetX,12,WorldWidth-12);web.Y=Math.Clamp(rect.Top+web.AnchorOffsetY,12,WorldHeight-12);}
            foreach(var resource in Resources)if(Surfaces.Values.Any(rect=>rect.Contains(resource.Position))){var p=VisibleWaypoint(resource.Position,resource.Position);resource.X=p.X;resource.Y=p.Y;}
            foreach(var nest in Nests)if(Surfaces.Values.Any(rect=>rect.Contains(nest.Position))){var p=VisibleWaypoint(nest.Position,nest.Position);nest.X=p.X;nest.Y=p.Y;}
        }
    }'''))
p=r/'BugOverlay.cs';t=p.read_text(encoding='utf-8-sig');a=t.index('        foreach (var web in Manager.Webs)');b=t.index('\n    }\n}',a)
t=t[:a]+'''        foreach(var web in Manager.Webs)
        {
            var center=WorldToScreen(new(web.X,web.Y));var radius=34d;var progress=Math.Clamp(web.Construction,0,1);
            var spokes=(int)Math.Floor(Math.Min(1,progress*2)*8);
            for(var i=0;i<spokes;i++)dc.DrawLine(BugVisuals.P("#9D8CAE",.45),center,WorldToScreen(NexaWebGeometry.Spoke(web,i,radius)));
            for(var ring=1;ring<=4;ring++)for(var i=0;i<8;i++)
            {
                if(progress<.35+(ring-1)*.14+i*.012)continue;
                dc.DrawLine(BugVisuals.P(Manager.SeasonalCostumes?"#84CC16":"#9D8CAE",.4),WorldToScreen(NexaWebGeometry.Spoke(web,i,radius*ring/4)),WorldToScreen(NexaWebGeometry.Spoke(web,(i+1)%8,radius*ring/4)));
            }
            if(progress<1 && Manager.Bugs.FirstOrDefault(b=>b.InteractionId==web.Owner && b.Activity==BugActivity.Nesting) is {} spinner && (spinner.Position-new Point(web.X,web.Y)).Length<45)
                dc.DrawLine(BugVisuals.P("#AFD9CBE2",.35),WorldToScreen(spinner.Position),WorldToScreen(NexaWebGeometry.Spoke(web,(int)(progress*40)%8,radius)));
        }'''+t[b:];p.write_text(t,encoding='utf-8-sig')
edit('BugHabitatVisuals.cs',lambda t:t.replace('''                    for(var i=0;i<8;i++){var a=i*Math.PI/4;dc.DrawLine(BugVisuals.P("#9A8CA9",.5),new(0,0),new(Math.Cos(a)*14,Math.Sin(a)*14));}
                    for(var r=5;r<=13;r+=4)dc.DrawEllipse(null,BugVisuals.P("#9A8CA9",.4),new(),r,r);break;''','''                    dc.DrawEllipse(BugVisuals.B("#3B2C45"),null,new(-10,-9),3,3);break;'''))
print('Spider-owned irregular webs now build progressively at persistent widget anchors; removed prebuilt habitat webs.')
