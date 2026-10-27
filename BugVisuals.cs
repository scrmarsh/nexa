using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal static partial class BugVisuals
{
    private static readonly Dictionary<(BugSpecies, BugAppearance, bool), Drawing> bodies = [];
    private static readonly Dictionary<(BugSpecies,bool), Drawing> costumes = [];
    private static readonly Dictionary<string, SolidColorBrush> brushes = [];
    private static readonly Dictionary<(string, double), Pen> pens = [];
    internal static Brush B(string color)
    { if (!brushes.TryGetValue(color, out var value)) { value = new((Color)ColorConverter.ConvertFromString(color)); value.Freeze(); brushes[color] = value; } return value; }
    internal static Pen P(string color, double width = 1)
    { var key = (color, width); if (!pens.TryGetValue(key, out var pen)) { pen = new(B(color), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round }; pen.Freeze(); pens[key] = pen; } return pen; }
    private static void Shape(DrawingContext dc, string path, string fill, string? edge = null, double width = .6) => dc.DrawGeometry(B(fill), edge == null ? null : P(edge, width), Geometry.Parse(path));
    internal static double HitRadius(BugEntity bug) => bug.Species switch { BugSpecies.Centipedes => 28, BugSpecies.Spiders => 20, BugSpecies.Roaches => 18, BugSpecies.Ladybugs => 22, _ => 18 };
    internal static Drawing Body(BugSpecies species, BugAppearance appearance, bool limbs=true)
    {
        if (appearance == BugAppearance.Normal) return limbs ? BugArtwork.For(new BugEntity { Species = species }) : BugArtwork.BodyOnly(new BugEntity { Species=species });
        var key = (species, appearance, limbs);
        if (bodies.TryGetValue(key, out var cached)) return cached;
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            var cozy = appearance == BugAppearance.Cozy;
            var body = cozy ? species switch { BugSpecies.Ants => "#B57A50", BugSpecies.Roaches => "#CC9760", BugSpecies.Spiders => "#A185B6", BugSpecies.Ladybugs => "#F18178", BugSpecies.Centipedes => "#C7A35E", _ => "#8BAAB5" } : species switch { BugSpecies.Ants => "#392922", BugSpecies.Roaches => "#33251C", BugSpecies.Spiders => "#241D2C", BugSpecies.Ladybugs => "#711B34", BugSpecies.Centipedes => "#3E3829", _ => "#334748" };
            var edge = cozy ? "#765547" : "#17141B"; var legColor = cozy ? "#B18970" : "#75615B";
            var pairs = species == BugSpecies.Spiders ? 4 : species == BugSpecies.Centipedes ? 15 : 3;
            if(limbs) for (var leg = 0; leg < pairs; leg++) for (var side = -1; side <= 1; side += 2)
            {
                var x = species == BugSpecies.Centipedes ? -15 + leg * 2.15 : species == BugSpecies.Spiders ? 1 + leg * 1.55 : (leg - (pairs - 1) / 2d) * 3;
                var length = species == BugSpecies.Spiders ? cozy ? 10 : 16 : species == BugSpecies.Centipedes ? 7 : 6;
                var geometry = new StreamGeometry();
                using (var g = geometry.Open()) { g.BeginFigure(new Point(x, side * 2), false, false); if (cozy) g.QuadraticBezierTo(new(x - 3, side * length), new(x + 2, side * length), true, false); else { g.LineTo(new(x - 4, side * length * .65), true, false); g.LineTo(new(x + 1, side * length), true, false); } }
                geometry.Freeze(); dc.DrawGeometry(null, P(legColor, cozy ? 1.3 : .7), geometry);
            }
            if (species == BugSpecies.Centipedes)
            {
                for (var i = 0; i < 15; i++)
                {
                    var x = -15 + i * 2.15;
                    if (cozy) dc.DrawEllipse(B(body), P(edge, .5), new(x, 0), 1.8, 3.4);
                    else Shape(dc, FormattableString.Invariant($"M{x - 2},-3 L{x + 1},-4 L{x + 2},0 L{x + 1},4 L{x - 2},3 Z"), body, edge);
                }
                dc.DrawEllipse(B(cozy ? "#AC7951" : "#292620"), P(edge, .6), new(16, 0), 3, 3.5);
                dc.DrawLine(P(legColor, .8), new(17, -2), new(23, -6)); dc.DrawLine(P(legColor, .8), new(17, 2), new(23, 6));
            }
            else if (species == BugSpecies.Ants)
            {
                dc.DrawEllipse(B(body), P(edge, .6), new(-5, 0), cozy ? 4 : 4.8, cozy ? 3.4 : 2.8);
                dc.DrawEllipse(B(body), P(edge, .6), new(0, 0), 2.2, cozy ? 2.8 : 1.5);
                dc.DrawEllipse(B(body), P(edge, .6), new(5, 0), cozy ? 3.2 : 2.5, cozy ? 3 : 2.2);
                if (!cozy) for (var x = -7; x <= -3; x += 2) dc.DrawLine(P("#79604A", .45), new(x, -2), new(x + .5, 2));
            }
            else if (species == BugSpecies.Roaches)
            {
                if (cozy) dc.DrawRoundedRectangle(B(body), P(edge, .6), new Rect(-10, -5, 20, 10), 4.8, 4.8);
                else Shape(dc, "M11,-3 Q2,-8 -10,-5 L-13,0 L-10,5 Q2,8 11,3 Z", body, edge);
                dc.DrawLine(P(cozy ? "#EDC79A" : "#8D7151", .6), new(-9, 0), new(8, 0));
                if (!cozy) for (var x = -8; x <= 4; x += 2) { dc.DrawLine(P("#66513A", .4), new(x, -4), new(x + 3, -1)); dc.DrawLine(P("#66513A", .4), new(x, 4), new(x + 3, 1)); }
            }
            else if (species == BugSpecies.Spiders)
            {
                dc.DrawEllipse(B(body), P(edge, .7), new(-4, 0), cozy ? 6.2 : 7.5, cozy ? 5 : 5.7);
                dc.DrawEllipse(B(cozy ? "#B69BCA" : "#372335"), P(edge, .6), new(4, 0), 3.8, 3.5);
                if (!cozy) { Shape(dc, "M-8,-2 L-4,-4 L0,-2 L-4,0 Z M-8,2 L-4,4 L0,2 L-4,0 Z", "#72615F"); dc.DrawLine(P("#A3998F", .45), new(6, -1), new(10, -2)); dc.DrawLine(P("#A3998F", .45), new(6, 1), new(10, 2)); }
            }
            else if (species == BugSpecies.Ladybugs)
            {
                dc.DrawEllipse(B(body), P(edge, .8), new(-1, 0), cozy ? 7.5 : 7, cozy ? 6.3 : 5.5);
                dc.DrawLine(P(edge, .6), new(-8, 0), new(6, 0));
                foreach (var x in new[] { -4d, 0d, 3d }) for (var side = -1; side <= 1; side += 2) dc.DrawEllipse(B(cozy ? "#FFE2BF" : "#181C22"), null, new(x, side * 2.7), cozy ? 1.2 : .9, cozy ? 1.3 : .9);
                dc.DrawEllipse(B(cozy ? "#694F51" : "#222028"), P(edge, .4), new(6, 0), 2.5, 3.2);
            }
            else
            {
                if(limbs) dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, -4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);
                if(limbs) dc.DrawEllipse(B(cozy ? "#E0EAF2" : "#9EAFB2"), P(edge, .4), new(-2, 4), cozy ? 6 : 7, cozy ? 3.5 : 2.8);
                dc.DrawEllipse(B(body), P(edge, .7), new(-2, 0), 5.5, cozy ? 3.2 : 2.6);
                dc.DrawEllipse(B(body), P(edge, .7), new(4, 0), cozy ? 3 : 2.6, cozy ? 3.6 : 3);
                if (!cozy) { for (var x = -6; x <= 0; x += 2) dc.DrawLine(P("#9FAEAD", .4), new(x, -2), new(x, 2)); if(limbs) dc.DrawLine(P("#4C666D", .4), new(-7, -5), new(3, -3)); if(limbs) dc.DrawLine(P("#4C666D", .4), new(-7, 5), new(3, 3)); }
            }
            if (limbs && species is not (BugSpecies.Centipedes or BugSpecies.Spiders))
            { var head = species == BugSpecies.Roaches ? 9 : 6; dc.DrawLine(P(legColor, .7), new(head, -1), new(head + 5, -5)); dc.DrawLine(P(legColor, .7), new(head, 1), new(head + 5, 5)); }
        }
        group.Freeze(); bodies[key] = group; return group;
    }
    internal static Drawing Costume(BugSpecies species, bool wings=true)
    {
        if (costumes.TryGetValue((species,wings), out var cached)) return cached;
        var group = new DrawingGroup();
        using (var dc = group.Open())
        {
            switch (species)
            {
                case BugSpecies.Ants:
                    Shape(dc, "M0,-7 L5,-16 L9,-7 L12,-6 L-2,-6 Z", "#24152F", "#A855F7"); dc.DrawLine(P("#F97316", 1.6), new(3, -9), new(8, -9));
                    dc.DrawEllipse(B("#F97316"), P("#9A3412", .5), new(-6, 5), 3.5, 3); dc.DrawLine(P("#84CC16", 1), new(-6, 2), new(-5, 0)); dc.DrawEllipse(null, P("#F97316", .7), new(-4, 0), 3, 2); break;
                case BugSpecies.Spiders:
                    dc.DrawEllipse(B("#AA6B21A8"), P("#84CC16", .5), new(-4, 0), 6.5, 5);
                    Shape(dc, "M-8,-2 L-4,-4 L0,-2 L-4,0 Z M-8,2 L-4,4 L0,2 L-4,0 Z", "#84CC16");
                    Shape(dc, "M1,-6 L4,-12 L7,-6 L10,-5 L-1,-5 Z", "#24152F", "#A855F7"); break;
                case BugSpecies.Ladybugs:
                    Shape(dc, "M-4,-3 L-16,-11 L-13,-3 L-17,1 L-12,4 L-15,11 L-4,3 Z", "#3B143F", "#991B3A", 1);
                    Shape(dc, "M4,-4 L8,-7 L7,-1 Z M4,4 L8,7 L7,1 Z", "#6B164A"); Shape(dc, "M7,-1 L10,-1 L8,2 Z M7,2 L9,2 L8,4 Z", "#FFF7ED"); break;
                case BugSpecies.Roaches:
                    Shape(dc, "M-9,-3 Q-4,-6 1,-3 L-2,0 L-8,1 Z M1,1 L7,0 L6,4 L0,3 Z", "#AA65A30D");
                    dc.DrawLine(P("#365314", .7), new(-6, -1), new(5, 2)); for (var x = -4; x <= 3; x += 2) dc.DrawLine(P("#D9F99D", .6), new(x, -1), new(x - 1, 2));
                    dc.DrawEllipse(B("#84CC16"), null, new(-8, 4), 1.3, 2); break;
                case BugSpecies.Centipedes:
                    for (var x = -13; x <= 13; x += 3) { dc.DrawEllipse(B("#886B21A8"), null, new(x, 0), 2.2, 2.7); dc.DrawLine(P("#E9D5FF", 1), new(x, -2.4), new(x, 2.4)); dc.DrawLine(P("#84CC16", .6), new(x, -2.4), new(x - 1.2, -3.3)); }
                    dc.DrawLine(P("#E9D5FF", 1), new(-13, 0), new(13, 0)); break;
                case BugSpecies.Flies:
                    if(wings) dc.DrawEllipse(B("#70D8D1F0"), P("#84CC16", .6), new(-3, -5), 8, 4); if(wings) dc.DrawEllipse(B("#70D8D1F0"), P("#A855F7", .6), new(-3, 5), 8, 4);
                    Shape(dc, "M-8,-3 L-13,-4 L-11,0 L-14,3 L-7,3 Z", "#70E9D5FF"); Shape(dc, "M-2,-2 L1,0 L-2,2 L-4,0 Z", "#84CC16"); break;
            }
        }
        group.Freeze(); costumes[(species,wings)] = group; return group;
    }
    internal static void Draw(DrawingContext dc, BugEntity bug, EcosystemOptions options, bool costume)
    {
        dc.DrawEllipse(B("#38000000"), null, new(-1, 3), bug.Species == BugSpecies.Centipedes ? 17 : 9, 3);
        if (options.EffectiveAppearance == BugAppearance.Normal) dc.DrawDrawing(BugArtwork.For(bug));
        else dc.DrawDrawing(Body(bug.Species, options.EffectiveAppearance));
        if (costume) dc.DrawDrawing(Costume(bug.Species));
        var head = bug.Species == BugSpecies.Centipedes ? 16 : bug.Species == BugSpecies.Roaches ? 9 : bug.Species == BugSpecies.Spiders ? 5 : 5;
        if (options.Adorable)
        {
            foreach (var side in new[] { -1, 1 })
            { dc.DrawEllipse(B("#FFFDF4"), P("#312334", .4), new(head, side * 1.8), 2.1, 1.8); dc.DrawEllipse(B("#24152F"), null, new(head + .7, side * 1.8), .9, 1.1); dc.DrawEllipse(B("#FFFFFF"), null, new(head + .9, side * 1.8 - .3), .28, .3); }
        }
        else if (options.EffectiveAppearance != BugAppearance.Normal)
        {
            if (bug.Species == BugSpecies.Spiders) for (var i = 0; i < 4; i++) dc.DrawEllipse(B("#B9A991"), null, new(head + 1 - i * .6, -2 + i * 1.3), .45, .55);
            else { dc.DrawEllipse(B("#1C1822"), null, new(head + 1, -1.5), .6, .8); dc.DrawEllipse(B("#1C1822"), null, new(head + 1, 1.5), .6, .8); }
        }
        if (bug.Carrying > 0 && bug.PartnerId == null) { dc.DrawRoundedRectangle(B(bug.Species == BugSpecies.Ants && costume ? "#F97316" : "#C6A65D"), P("#715132", .5), new Rect(head + 2, -2, 4, 4), 1, 1); }
    }
}
