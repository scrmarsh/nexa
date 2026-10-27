from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
shutil.copy2(s/'Nexaween.Placement.cs',r/'Nexaween.Placement.cs')
edit('Nexaween.cs',lambda t:t.replace('seasonalArtwork.Update(Math.Max(1, WidgetCanvas.ActualWidth), Math.Max(1, WidgetCanvas.Height), occupied, Options.DecorationDensity);','seasonalArtwork.Update(Math.Max(1, WidgetCanvas.ActualWidth), Math.Max(1, WidgetCanvas.Height), occupied, Options.DecorationDensity,widgetFrames.Values.Select(f=>new Rect(Canvas.GetLeft(f),Canvas.GetTop(f),f.Width,f.Height)).ToArray());').replace('List<Rect> occupied, string density)','List<Rect> occupied, string density, IReadOnlyList<Rect>? widgetCorners=null)').replace('actors.Clear();','actors.Clear();vineCorners.Clear();').replace('''            var count = 0;
            for (var attempt''','''            var reserve=density=="High"?8:density=="Low"?2:4;
            if(budget>0 && widgetCorners!=null)AddContextual(dc,widgetCorners,occupied,Math.Min(reserve,budget));
            var count = 0;var candyBudget=budget-actors.Count;
            for (var attempt''').replace('count < budget','count < candyBudget').replace('var kind=count%15;','var kind=CandyKinds[count%CandyKinds.Length];').replace('''        switch (kind)
        {''','''        if(kind>=15){DrawExtraCandy(dc,kind);return;}
        switch (kind)
        {'''))
edit('Nexaween.Animation.cs',lambda t:t.replace('var time = elapsed.Elapsed.TotalSeconds; var particles = 0;','var time = elapsed.Elapsed.TotalSeconds; var particles = 0;\n        foreach(var corner in vineCorners)DrawVine(dc,corner,time);'))
print('Replaced generic repeating decorations with ten candy types and contextual widget-corner art/vines.')
