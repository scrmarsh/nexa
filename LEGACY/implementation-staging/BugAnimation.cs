using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal readonly record struct BugLegPose(Point Root, Point Knee, Point Ankle, Point Foot, bool Stance);
internal static class BugAnimation
{
    internal static int LegPairs(BugSpecies species) => species == BugSpecies.Spiders ? 4 : species == BugSpecies.Centipedes ? 15 : 3;
    internal static int FunctionalWings(BugSpecies species) => species is BugSpecies.Flies or BugSpecies.Ladybugs ? 2 : 0;
    internal static double SmoothAngle(double current, double target, double seconds, double degreesPerSecond = 280)
    {
        var delta = ((target - current + 540) % 360) - 180;
        return ((current + Math.Clamp(delta, -degreesPerSecond * seconds, degreesPerSecond * seconds) + 540) % 360) - 180;
    }
    internal static double GaitPhase(BugEntity bug, double movementScale = 1)
    {
        var stride = bug.Species switch { BugSpecies.Spiders => 6.5, BugSpecies.Roaches => 6, BugSpecies.Centipedes => 3.2, _ => 4.2 };
        return bug.CrawlPhase * Math.PI * 2 / (.35 * stride * Math.Max(.5, bug.SizeScale)) * movementScale;
    }
    internal static Point CentipedeSegment(int index, double phase, bool resting)
    {
        var fraction = index / 14d;
        if (resting)
        {
            var angle = (fraction - .5) * 1.35;
            return new(Math.Sin(angle) * 24, (1 - Math.Cos(angle)) * 24);
        }
        return new(-15 + index * 2.15, Math.Sin(phase * .45 - index * .55) * 1.6);
    }
    internal static int FillLegs(BugSpecies species, double phase, bool resting, Span<BugLegPose> output)
    {
        var pairs = LegPairs(species); var count = 0;
        for (var index = 0; index < pairs; index++) for (var side = -1; side <= 1; side += 2)
        {
            var centipede = species == BugSpecies.Centipedes;
            var spider = species == BugSpecies.Spiders;
            var x = centipede ? CentipedeSegment(index, phase, resting).X : spider ? 1 + index * 1.55 : species switch { BugSpecies.Roaches => 2 + index * 1.6, BugSpecies.Ladybugs => 2 + index * 1.2, _ => -1 + index };
            var y = centipede ? CentipedeSegment(index, phase, resting).Y : 0;
            var root = new Point(x, y + side * (centipede ? 2.1 : spider ? 2.2 : 1.5));
            var offset = centipede ? -index * .65 + (side == 1 ? Math.PI : 0) : (index % 2) * Math.PI + (side == 1 ? Math.PI : 0);
            var cycle = ((phase + offset) / (2 * Math.PI) % 1 + 1) % 1;
            var duty = spider ? .68 : .62;
            var stance = cycle < duty;
            var swing = stance ? 0 : (cycle - duty) / (1 - duty);
            var stride = centipede ? 3.2 : spider ? 6.5 : 4.2;
            var travel = resting ? 0 : stance ? stride * (.5 - cycle / duty) : stride * (-.5 + swing);
            var span = centipede ? 6.8 : spider ? 13 : species == BugSpecies.Roaches ? 7 : 6;
            var spread = spider ? (index - 1.5) * 5.8 : (index - (pairs - 1) / 2d) * .8;
            if (centipede && index == 0) { span = 9; spread = -8; travel = Math.Sin(phase * .15 + side) * .6; stance = false; }
            var lift = resting ? 0 : Math.Sin(swing * Math.PI) * (spider ? 2.3 : 1.1);
            var foot = new Point(x + spread + travel, y + side * (span - lift));
            var delta = foot - root; var distance = Math.Max(.001, delta.Length);
            var upper = span * .63; var lower = Math.Max(span * .65, distance - upper + .1);
            var cosine = Math.Clamp((upper * upper + distance * distance - lower * lower) / (2 * upper * distance), -1, 1);
            var direction = Math.Atan2(delta.Y, delta.X);
            var bend = spider && index >= 2 || !spider && !centipede && index == 2 ? -side : side;
            var knee = root + new Vector(Math.Cos(direction + bend * Math.Acos(cosine)), Math.Sin(direction + bend * Math.Acos(cosine))) * upper;
            var ankle = knee + (foot - knee) * .83;
            output[count++] = new(root, knee, ankle, foot, stance);
        }
        return count;
    }
    internal static void DrawLegs(DrawingContext dc, BugEntity bug, EcosystemOptions options, double phase)
    {
        Span<BugLegPose> poses = stackalloc BugLegPose[30];
        var resting = bug.Activity == BugActivity.Sleeping;
        if (options.Quality == BugAnimationQuality.Performance) phase = Math.Round(phase * 4 / Math.PI) * Math.PI / 4;
        var count = FillLegs(bug.Species, phase, resting, poses);
        var color = options.EffectiveAppearance switch { BugAppearance.Cozy => "#B5957E", BugAppearance.Creepy => "#75625F", _ => "#806D56" };
        for (var i = 0; i < count; i++)
        {
            var p = poses[i];
            dc.DrawLine(BugVisuals.P(color, options.EffectiveAppearance == BugAppearance.Cozy ? 1.15 : .7), p.Root, p.Knee);
            if (options.Quality == BugAnimationQuality.High)
            {
                dc.DrawLine(BugVisuals.P(color, .55), p.Knee, p.Ankle); dc.DrawLine(BugVisuals.P(color, .4), p.Ankle, p.Foot);
                if (options.EffectiveAppearance == BugAppearance.Creepy)
                { var mid = p.Root + (p.Knee - p.Root) * .5; dc.DrawLine(BugVisuals.P("#9C8B7B", .3), mid, mid + new Vector(-.7, i % 2 == 0 ? -.9 : .9)); }
            }
            else dc.DrawLine(BugVisuals.P(color, .6), p.Knee, p.Foot);
        }
    }
    internal static Point DrawCentipede(DrawingContext dc, BugEntity bug, EcosystemOptions options, bool costume, double phase)
    {
        var resting = bug.Activity == BugActivity.Sleeping;
        for (var i = 0; i < 15; i++)
        {
            var point = CentipedeSegment(i, phase, resting);
            var color = options.EffectiveAppearance switch { BugAppearance.Cozy => i % 2 == 0 ? "#D4B677" : "#BE9858", BugAppearance.Creepy => i % 2 == 0 ? "#4A4434" : "#353124", _ => "#4F3B30" };
            dc.DrawEllipse(BugVisuals.B(color), BugVisuals.P(options.EffectiveAppearance == BugAppearance.Normal ? "#D8CAA6" : "#28231F", .45), point, 1.8, options.EffectiveAppearance == BugAppearance.Cozy ? 3.2 : 2.7);
            dc.DrawEllipse(BugVisuals.B("#35FFFFFF"), null, point + new Vector(-.3, -.8), .9, 1.25);
            if (costume)
            {
                dc.DrawEllipse(BugVisuals.B("#886B21A8"), null, point, 1.65, 2.5);
                dc.DrawLine(BugVisuals.P("#E9D5FF", .8), point + new Vector(0, -2.2), point + new Vector(0, 2.2));
                dc.DrawLine(BugVisuals.P("#84CC16", .45), point + new Vector(0, -2.2), point + new Vector(-.8, -3));
            }
        }
        var tailward = CentipedeSegment(13, phase, resting); var last = CentipedeSegment(14, phase, resting);
        var tangent = last - tailward; if (tangent.Length > .01) tangent.Normalize();
        var head = last + tangent * 2.8;
        dc.DrawEllipse(BugVisuals.B(options.EffectiveAppearance == BugAppearance.Cozy ? "#AC7951" : "#392B25"), BugVisuals.P("#877256", .5), head, 2.4, 2.8);
        return head;
    }
    internal static void DrawAppendages(DrawingContext dc, BugEntity bug, EcosystemOptions options, double time, Point head)
    {
        var phase = time * 2 + (uint)bug.InteractionId.GetHashCode() % 17;
        if (bug.Species == BugSpecies.Spiders)
        {
            for (var side = -1; side <= 1; side += 2) dc.DrawLine(BugVisuals.P("#927F85", .6), head + new Vector(.5, side), head + new Vector(2, side * (1.7 + Math.Sin(phase) * .2)));
            return; // Pedipalps, never insect antennae.
        }
        for (var side = -1; side <= 1; side += 2)
        {
            var length = bug.Species is BugSpecies.Roaches or BugSpecies.Centipedes ? 8 : bug.Species == BugSpecies.Ladybugs ? 3.4 : bug.Species == BugSpecies.Flies ? 2 : 5;
            var root = head + new Vector(1, side * 1.3); var tip = root + new Vector(length, side * (2 + Math.Sin(phase + side) * .7));
            dc.DrawLine(BugVisuals.P("#9D8C74", .55), root, root + (tip - root) * .45); dc.DrawLine(BugVisuals.P("#9D8C74", .4), root + (tip - root) * .45, tip);
            if (bug.Species == BugSpecies.Ladybugs) dc.DrawEllipse(BugVisuals.B("#756C52"), null, tip, .55, .7);
        }
    }
    internal static void DrawWings(DrawingContext dc, BugEntity bug, EcosystemOptions options, double time)
    {
        if (FunctionalWings(bug.Species) == 0) return;
        var flying = bug.State == BugState.Flying;
        if (bug.Species == BugSpecies.Ladybugs && !flying) return;
        for (var side = -1; side <= 1; side += 2)
        {
            var open = flying ? bug.Species == BugSpecies.Ladybugs ? 48 : 17 : 5;
            var flap = flying ? Math.Sin(time * Math.PI * 10) * 7 : 0;
            dc.PushTransform(new RotateTransform(side * (open + flap), 0, side));
            dc.DrawEllipse(BugVisuals.B(flying ? "#A0D5E3ED" : "#B8BCCDDA"), BugVisuals.P("#ACC1C9", .35), new(-5, side * 3.2), bug.Species == BugSpecies.Ladybugs ? 10 : 7, 3);
            if (options.Quality != BugAnimationQuality.Performance) dc.DrawLine(BugVisuals.P("#708DA0AC", .3), new(0, side), new(-11, side * 3.5));
            dc.Pop();
            if (bug.Species == BugSpecies.Flies)
            {
                var haltere = new Point(-3, side * (3.4 - Math.Sin(time * Math.PI * 10) * .5));
                dc.DrawLine(BugVisuals.P("#9C9586", .45), new(-1, side * 2), haltere); dc.DrawEllipse(BugVisuals.B("#B9AB8E"), null, haltere, .6, .6);
            }
        }
    }
    internal static void DrawElytra(DrawingContext dc, BugEntity bug)
    {
        if (bug.Species != BugSpecies.Ladybugs || bug.State != BugState.Flying) return;
        for (var side = -1; side <= 1; side += 2)
        {
            dc.PushTransform(new RotateTransform(side * 43, 3, 0));
            dc.DrawEllipse(BugVisuals.B("#CE5361"), BugVisuals.P("#4D2534", .5), new(-2, side * 3), 6, 2.5);
            foreach (var x in new[] { -5d, -2d, 1d }) dc.DrawEllipse(BugVisuals.B("#302133"), null, new(x, side * 3), .8, .7);
            dc.Pop();
        }
    }
    internal static double EyeOpening(BugEntity bug, double time)
    {
        if (bug.Activity == BugActivity.Sleeping) return .05;
        var seed = (uint)bug.InteractionId.GetHashCode(); var period = 3.5 + seed % 35 / 10d;
        var cycle = (time + seed % 100 / 30d) % period;
        return cycle < .18 ? Math.Max(.05, 1 - Math.Sin(cycle / .18 * Math.PI)) : 1;
    }
    internal static Vector SmoothGaze(Vector current, Vector target, double seconds)
    { if (target.Length > .001) { target.Normalize(); target *= .8; } return current + (target - current) * (1 - Math.Exp(-Math.Clamp(seconds, 0, .1) * 9)); }
}
