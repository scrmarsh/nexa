from pathlib import Path
import shutil
root=Path('M:/Nexa')
stage=Path(__file__).parent
shutil.copy2(stage/'Nexa024AnimationChecks.cs',root/'tests/Nexa024AnimationChecks.cs')
for name in ['SessionDashboard.Checks.csproj','Nexa024.Checks.csproj']:
    path=root/'tests'/name
    text=path.read_text(encoding='utf-8-sig').replace('Nexa024EcosystemChecks.cs;','Nexa024EcosystemChecks.cs;Nexa024AnimationChecks.cs;')
    path.write_text(text,encoding='utf-8-sig')
path=root/'tests/Nexa024Runner.cs'
text=path.read_text(encoding='utf-8-sig').replace('if(args.Contains("--ecosystem"))','if(args.Contains("--animation"))CheckAnimation();\n            else if(args.Contains("--ecosystem"))')
path.write_text(text,encoding='utf-8-sig')
# Flying wings beat too quickly to depict faithfully at dashboard frame rates.
# A translucent swept area avoids presenting a slow flap as a real wing beat.
path=root/'BugAnimation.cs'
text=path.read_text(encoding='utf-8-sig').replace('var flap = flying ? Math.Sin(time * Math.PI * 10) * 7 : 0;','var flap = flying ? Math.Sin(time * 2) * 1.5 : 0;\n            // This is a motion-blur envelope, not a slowed-down wingbeat.\n            if(flying)for(var sweep=-1;sweep<=1;sweep++)\n            {\n                dc.PushTransform(new RotateTransform(side*(open+sweep*11),0,side));\n                dc.DrawEllipse(BugVisuals.B("#25D5E3ED"),null,new(-5,side*3.2),bug.Species==BugSpecies.Ladybugs?10:7,3.4);dc.Pop();\n            }')
path.write_text(text,encoding='utf-8-sig')
print('Installed animation checks and flight motion-blur rendering.')
