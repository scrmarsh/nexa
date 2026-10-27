from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
shutil.copy2(s/'NexaFollowupDesignChecks.cs',r/'tests/NexaFollowupDesignChecks.cs')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
for n in ['SessionDashboard.Checks.csproj','Nexa024.Checks.csproj']:edit('tests/'+n,lambda t:t.replace('NexaFollowupChecks.cs;','NexaFollowupChecks.cs;NexaFollowupDesignChecks.cs;'))
edit('tests/Nexa024Runner.cs',lambda t:t.replace('if(args.Contains("--followup"))','if(args.Contains("--followup-design"))CheckFollowupDesign();\n            else if(args.Contains("--followup"))').replace('settings.Owner=null;settings.ShowInTaskbar=true;settings.Show();','settings.Owner=null;settings.ShowInTaskbar=true;settings.WindowStartupLocation=WindowStartupLocation.Manual;settings.Left=100;settings.Top=100;settings.Show();'))
print('Added persisted signed scoring, seasonal navigation, candy/web/notification placement and real-startup regression checks.')
