from pathlib import Path
r=Path('M:/Nexa')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
edit('Nexaween.cs',lambda t:t.replace('Options.DecorationDensity,widgetFrames.Values.Select','Options.DecorationDensity,widgetTargets:widgetFrames.Values.Select').replace('IReadOnlyList<Rect>? widgetCorners=null','IReadOnlyList<Rect>? widgetTargets = null').replace('        foreach (var rect in occupied) hash.Add(rect);','''        foreach (var rect in occupied) hash.Add(rect);
        hash.Add(widgetTargets?.Count??-1);if(widgetTargets!=null)foreach(var target in widgetTargets)hash.Add(target);''').replace('var result = new DrawingGroup(); actors.Clear();vineCorners.Clear();','var collisionAreas=occupied.ToList();\n        var result = new DrawingGroup(); actors.Clear();vineCorners.Clear();').replace('if(budget>0 && widgetCorners!=null)AddContextual(dc,widgetCorners,occupied,Math.Min(reserve,budget));','if(budget>0 && widgetTargets!=null)AddContextual(dc,widgetTargets,collisionAreas,Math.Min(reserve,budget));').replace('if (occupied.Any(r => r.IntersectsWith(safe)))','if (collisionAreas.Any(r => r.IntersectsWith(safe)))').replace('dc.Pop(); dc.Pop(); occupied.Add(bounds);','dc.Pop(); dc.Pop(); collisionAreas.Add(bounds);'))
edit('Nexaween.Placement.cs',lambda t:t.replace('    internal int ContextualActorCount=>actors.Count-CandyActorCount;','    internal int ContextualActorCount=>actors.Count-CandyActorCount;\n    internal IReadOnlyList<Rect> AttachmentBounds=>actors.Where(a=>!CandyKinds.Contains(a.Kind)).Select(a=>a.Bounds).ToArray();').replace('''            if(bounds.Right>Width || bounds.Bottom>Height || widgets.Any(other=>other!=rect && other.IntersectsWith(bounds)))continue;''','''            var safe=bounds;safe.Inflate(14,14);
            // The target's own body is exempt; every other occupied/reserved region is a collision.
            if(count%5==0)safe.Union(new Rect(rect.Right-70,rect.Bottom-100,102,116));
            if(bounds.X<0 || bounds.Y<0 || bounds.Right>Width || bounds.Bottom>Height || occupied.Any(other=>other!=rect && other.IntersectsWith(safe)))continue;'''))
edit('Nexaween.Animation.cs',lambda t:t.replace('''    internal NexaweenDecorations()
    {
        animation.Tick''','''    internal NexaweenDecorations()
    {
        IsHitTestVisible=false;
        animation.Tick'''))
edit('tests/NexaFollowupDesignChecks.cs',lambda t:t.replace('''        Assert(candy.ContextualActorCount>0 && candy.ActorCount<=32,"Contextual widget-corner decorations share the bounded budget");''','''        Assert(candy.ContextualActorCount>0 && candy.ActorCount<=32,"Contextual widget-corner decorations share the bounded budget");
        var reserved=new List<Rect>{new Rect(80,80,400,250),new Rect(620,80,400,250)};
        candy.Update(1600,1500,reserved,"High");Assert(candy.ContextualActorCount==0,"Occupied reserved regions never become inferred attachment targets");
        var occupiedCopy=reserved.ToArray();var target=new Rect(80,500,400,250);
        candy.Update(1600,1500,reserved,"High",widgetTargets:[target]);var originalAttachment=candy.AttachmentBounds.Single();
        Assert(reserved.SequenceEqual(occupiedCopy),"Decoration placement does not modify the caller's occupied collision list");
        var resized=new Rect(100,520,500,280);candy.Update(1600,1500,reserved,"High",widgetTargets:[resized]);
        Assert(candy.AttachmentBounds.Single()!=originalAttachment && Math.Abs(candy.AttachmentBounds.Single().X-(resized.Right-16.8))<.001,"Only widget target changes are enough to reposition attached art on resize/move");
        var beforeScroll=candy.AttachmentBounds.Single();candy.SetViewport(new Rect(0,400,1200,800));Assert(candy.AttachmentBounds.Single()==beforeScroll,"Viewport scrolling preserves the decoration's canvas attachment coordinates");
        candy.Update(1600,1500,reserved,"High");Assert(candy.ContextualActorCount==0 && !candy.IsHitTestVisible,"Missing optional targets clear contextual art gracefully and never intercept controls");
        candy.Update(1600,1500,[new Rect(target.Right,target.Bottom,100,100)],"High",widgetTargets:[target]);Assert(candy.ContextualActorCount==0,"Reserved UI collision regions prevent corner attachments even for a real widget target");'''))
print('Separated optional widgetTargets from collision-only occupied rectangles and added missing-target, resize, scroll and reserved-region regression checks.')
