from pathlib import Path
root=Path(r'M:\Nexa');stage=Path(__file__).parent
def read(n):return (root/n).read_text(encoding='utf-8-sig')
def write(n,t):(root/n).write_text(t,encoding='utf-8')
write('BugHabitatVisuals.cs',(stage/'BugHabitatVisuals.cs').read_text(encoding='utf-8'))
text=read('DashboardBugs.cs').replace('internal Guid? ResourceId, EnemyId;','internal Guid? ResourceId, EnemyId, PartnerId;')
write('DashboardBugs.cs',text)
text=read('NexaEcosystem.Models.cs').replace('    public Guid? EnemyId { get; set; }','    public Guid? EnemyId { get; set; }\n    public Guid? PartnerId { get; set; }')
write('NexaEcosystem.Models.cs',text)
text=read('NexaEcosystem.Simulation.cs').replace('EnemyId = b.EnemyId, RaidTarget','EnemyId = b.EnemyId, PartnerId = b.PartnerId, RaidTarget')
needle='                    // Ants recruit a nearby partner for large candy loads.'
start=text.index(needle);end=text.index('\n                }',start)
text=text[:start]+'''                    var partner = bug.Species==BugSpecies.Ants && resource.Kind==BugResourceKind.Candy ? Bugs.FirstOrDefault(b=>b!=bug && b.Species==BugSpecies.Ants && b.State!=BugState.Squashed && b.Health>0 && b.Carrying<=0 && (b.Position-bug.Position).Length<35) : null;
                    var amount=Math.Min(resource.Amount,partner!=null?2:.5);resource.Amount-=amount;
                    bug.Carrying=partner==null?amount:amount/2;bug.Activity=BugActivity.Carrying;bug.Target=nest.Position;bug.Decision=30;
                    if(partner!=null){bug.PartnerId=partner.InteractionId;partner.PartnerId=bug.InteractionId;partner.Carrying=amount/2;partner.Activity=BugActivity.Carrying;partner.Target=nest.Position;partner.Decision=30;partner.CrawlPhase=bug.CrawlPhase;}
'''+text[end:]
text=text.replace('            if (bug.Activity == BugActivity.Carrying) bug.Target = nest.Position;','''            if (bug.Activity == BugActivity.Carrying)
            {
                bug.Target=nest.Position;
                if(bug.PartnerId is {} id)
                {
                    var partner=Bugs.FirstOrDefault(b=>b.InteractionId==id && b.State!=BugState.Squashed && b.Health>0 && b.Carrying>0);
                    if(partner==null)bug.PartnerId=null;
                    else bug.Target+=new Vector(0,bug.InteractionId.CompareTo(id)<0?-6:6);
                }
            }''')
text=text.replace('bug.Carrying = 0; bug.Activity = BugActivity.Nesting;','bug.Carrying = 0; bug.PartnerId=null; bug.Activity = BugActivity.Nesting;')
text=text.replace('            if (EcosystemSettings.Behavior == BugBehavior.Creepy && bug.Species == BugSpecies.Roaches', '''            if(bug.PartnerId is {} partnerId && Bugs.FirstOrDefault(b=>b.InteractionId==partnerId) is {} carrier)speed=(bug.Speed+carrier.Speed)*.5*.55*.65;
            if (EcosystemSettings.Behavior == BugBehavior.Creepy && bug.Species == BugSpecies.Roaches''')
write('NexaEcosystem.Simulation.cs',text)
for name in ['BugVisuals.cs','BugVisuals.Animation.cs']:
    text=read(name).replace('if (bug.Carrying > 0)','if (bug.Carrying > 0 && bug.PartnerId == null)')
    if name=='BugVisuals.cs':text=text.replace('BugSpecies.Centipedes => 23','BugSpecies.Centipedes => 28').replace('BugSpecies.Ladybugs => 17','BugSpecies.Ladybugs => 22').replace('_ => 14','_ => 18')
    write(name,text)
text=read('BugOverlay.cs')
needle='        if (protectedClip != null) dc.Pop();'
text=text.replace(needle,'''        foreach(var bug in VisibleBugs.Where(b=>b.PartnerId!=null && b.Carrying>0 && b.State!=BugState.Squashed))
        {
            if(bug.InteractionId.CompareTo(bug.PartnerId!.Value)>=0)continue;
            var partner=Manager.Bugs.FirstOrDefault(b=>b.InteractionId==bug.PartnerId && b.Carrying>0 && b.State!=BugState.Squashed);if(partner==null)continue;
            var a=WorldToScreen(bug.Position);var b=WorldToScreen(partner.Position);if((a-b).Length>100)continue;
            var center=a+(b-a)*.5;var size=3*ViewScale*options.Size;
            dc.DrawLine(BugVisuals.P("#B98D54",.6),a,center);dc.DrawLine(BugVisuals.P("#B98D54",.6),b,center);
            dc.DrawRoundedRectangle(BugVisuals.B(Manager.SeasonalCandy?"#F97316":"#C6A65D"),BugVisuals.P("#E9D5FF",.6),new Rect(center.X-size,center.Y-size*.6,size*2,size*1.2),1.5,1.5);
        }
'''+needle)
text=text.replace('                dc.DrawEllipse(BugVisuals.B("#413327"), BugVisuals.P(color, 1), p, (12 + nest.Construction * 12) * ViewScale, 9 * ViewScale);\n                dc.DrawEllipse(BugVisuals.B("#18131A"), null, p, 7 * ViewScale, 4 * ViewScale);','                dc.PushTransform(new TranslateTransform(p.X,p.Y));dc.PushTransform(new ScaleTransform(ViewScale,ViewScale));dc.DrawDrawing(BugHabitatVisuals.For(nest.Species));dc.Pop();dc.Pop();')
write('BugOverlay.cs',text)
text=read('Nexaween.cs').replace('safe.Inflate(10, 10)','safe.Inflate(28, 28)')
write('Nexaween.cs',text)
print('Added cooperative ant carrying, species-specific habitat visuals, complete accessory hitboxes and safe animation placement margins.')
