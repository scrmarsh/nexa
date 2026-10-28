using System.Windows;
using System.Windows.Media;

namespace SessionDashboard;
internal static partial class BugVisuals
{
    private static readonly RadialGradientBrush contactShadow = Shadow();
    private static RadialGradientBrush Shadow()
    { var brush = new RadialGradientBrush((Color)ColorConverter.ConvertFromString("#66000000"), Colors.Transparent); brush.Freeze(); return brush; }
    internal static void DrawContactShadow(DrawingContext dc,BugEntity bug,bool flying)
    {dc.DrawEllipse(contactShadow,null,new Point(-1,3),bug.Species==BugSpecies.Centipedes?18:10,flying?2.5:3.4);}
    internal static void Draw(DrawingContext dc, BugEntity bug, EcosystemOptions options, bool costume, double time, Point look, double movementScale, bool activeEyes=true, bool drawShadow=true)
    {
        var phase = BugAnimation.GaitPhase(bug, movementScale);
        if (bug.Activity == BugActivity.Grooming) phase = time * 3;
        var flying = BugAnimation.Airborne(bug);
        if(drawShadow)DrawContactShadow(dc,bug,flying);
        BugAnimation.DrawLegs(dc, bug, options, phase);
        BugAnimation.DrawWings(dc, bug, options, time, costume);
        Point head;
        if (bug.Species == BugSpecies.Centipedes) head = BugAnimation.DrawCentipede(dc, bug, options, costume, phase);
        else
        {
            if(bug.Species==BugSpecies.Ladybugs && flying)
            {
                dc.DrawEllipse(B("#392E36"),P("#806D65",.4),new Point(-1,0),5.6,3.2);
                for(var x=-4;x<4;x+=2)dc.DrawLine(P("#72606D",.35),new Point(x,-2.5),new Point(x,2.5));
                dc.DrawEllipse(B("#302833"),null,new Point(3,0),2,2);
            }
            else dc.DrawDrawing(options.EffectiveAppearance == BugAppearance.Normal ? BugArtwork.BodyOnly(bug) : Body(bug.Species, options.EffectiveAppearance, false));
            var torso = bug.Species == BugSpecies.Spiders ? new Point(-4, -1.5) : new Point(-3, -1.4);
            var width = bug.Species is BugSpecies.Roaches or BugSpecies.Spiders ? 4.8 : 3;
            dc.DrawEllipse(B("#32E3F0DE"), null, torso, width, bug.Species is BugSpecies.Spiders or BugSpecies.Ladybugs ? 1.6 : .9);
            if (costume) dc.DrawDrawing(Costume(bug.Species, false));
            head = new(bug.Species == BugSpecies.Roaches ? 9 : 5, 0);
        }
        if(bug.HitFlash>0 || bug.State==BugState.Squashed)
        { dc.PushOpacity(Math.Clamp(bug.HitFlash/.35,.12,.55));dc.DrawEllipse(B("#EF4444"),null,new Point(-1,0),bug.Species==BugSpecies.Centipedes?17:9,4);dc.Pop(); }
        BugAnimation.DrawElytra(dc, bug);
        BugAnimation.DrawAppendages(dc, bug, options, time, head);
        if (options.Adorable)
        {
            var elapsed = bug.LastEyeTime <= 0 ? .033 : time - bug.LastEyeTime; if(activeEyes)bug.LastEyeTime=time;
            var delta = look - bug.Position; var radians = -bug.Angle * Math.PI / 180;
            var local = new Vector(delta.X * Math.Cos(radians) - delta.Y * Math.Sin(radians), delta.X * Math.Sin(radians) + delta.Y * Math.Cos(radians));
            if(activeEyes)bug.Gaze = BugAnimation.SmoothGaze(bug.Gaze, local, elapsed);
            var opening = BugAnimation.EyeOpening(bug, time);
            if (bug.HitFlash > 0) opening = 1.12;
            for (var side = -1; side <= 1; side += 2)
            {
                var center = head + new Vector(0, side * 1.8);
                if (opening < .15) { dc.DrawLine(P("#49344D", .6), center + new Vector(-1.6, 0), center + new Vector(1.6, 0)); continue; }
                dc.DrawEllipse(B("#FFFDF4"), P("#312334", .4), center, 2.1, 1.8 * opening);
                var pupil = center + bug.Gaze;
                dc.DrawEllipse(B("#24152F"), null, pupil, bug.HitFlash > 0 ? .65 : .9, Math.Min(1.1, 1.4 * opening));
                dc.DrawEllipse(B("#FFFFFF"), null, pupil + new Vector(.22, -.25), .27, .27);
            }
            if(bug.State==BugState.Squashed)
            {
                var age=Math.Clamp(bug.SquashAge/.4,0,1);dc.PushOpacity(1-age);
                for(var side=-1;side<=1;side+=2)dc.DrawEllipse(B("#7DD3FC"),null,head+new Vector(-age*4,side*(3+age*5)),.6,.9);
                dc.DrawGeometry(null,P("#573745",.5),Geometry.Parse(FormattableString.Invariant($"M{head.X+2},-2 Q{head.X},0 {head.X+2},2")));dc.Pop();
            }
            if (bug.HitFlash > 0) dc.DrawEllipse(null, P("#8C5C72", .35), head + new Vector(2.3, 0), .45, .65);
            else if (bug.Personality == BugPersonality.Curious && bug.Activity == BugActivity.Gathering)
                dc.DrawLine(P("#8D6D67", .35), head + new Vector(-1, -4), head + new Vector(1.3, -4.4));
        }
        else if (options.EffectiveAppearance != BugAppearance.Normal || bug.Species == BugSpecies.Centipedes)
        {
            // Natural eyes remain fixed: insects have compound eyes; spider ocelli do not acquire cartoon pupils.
            if (bug.Species == BugSpecies.Spiders)
                for (var row = 0; row < 2; row++) for (var column = 0; column < 4; column++) dc.DrawEllipse(B("#B1A08E"), null, head + new Vector(row * .65, -1.8 + column * 1.2), .34, .38);
            else for (var side = -1; side <= 1; side += 2) dc.DrawEllipse(B("#1C1822"), P("#5C534C", .2), head + new Vector(1, side * 1.5), .65, .85);
        }
        if (bug.Carrying > 0 && bug.PartnerId == null)
        {
            var load = bug.Species == BugSpecies.Ants && costume ? "#F97316" : bug.Species == BugSpecies.Ladybugs ? "#84A85A" : "#C6A65D";
            dc.DrawRoundedRectangle(B(load), P("#715132", .5), new Rect(head.X + 2, head.Y - 2, bug.Carrying > 1 ? 5.5 : 3.5, 4), 1, 1);
        }
    }
}
