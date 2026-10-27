from pathlib import Path
import shutil
r=Path('M:/Nexa');s=Path(__file__).parent
shutil.copy2(s/'NexaFollowupChecks.cs',r/'tests/NexaFollowupChecks.cs')
def edit(n,f):
 p=r/n;p.write_text(f(p.read_text(encoding='utf-8-sig')),encoding='utf-8-sig')
for n in ['SessionDashboard.Checks.csproj','Nexa024.Checks.csproj']:edit('tests/'+n,lambda t:t.replace('Nexa024AnimationChecks.cs;','Nexa024AnimationChecks.cs;NexaFollowupChecks.cs;'))
edit('tests/Nexa024Runner.cs',lambda t:t.replace('if(args.Contains("--animation"))','if(args.Contains("--followup"))CheckFollowup();\n            else if(args.Contains("--animation"))'))
edit('tests/NexaweenCoreChecks.cs',lambda t:t.replace('ledger.Score == 210 && ledger.Events.Last().BonusPoints == 100','ledger.Score == 160 && ledger.Events.Last().BonusPoints == 50').replace('exactly two times points during Frenzy','the initial 1.5 times points during a streak').replace('at.AddSeconds(21)','at.AddSeconds(121)').replace('at.AddSeconds(20)','at.AddMinutes(2)'))
edit('tests/Nexa024CoreChecks.cs',lambda t:t.replace('start.RemainingFraction == 1','start.VisualFraction == 0').replace('pixels refill','pixels start empty').replace('Math.Abs(during.RemainingFraction - 55d / 75)','Math.Abs(during.VisualFraction - 20d / 75)').replace('while pixels count down','while pixels fill upward'))
edit('tests/Nexa024EcosystemChecks.cs',lambda t:t.replace('OfType<BugBehavior>().Count() == 3','OfType<BugBehavior>().Count() == 4').replace('OfType<BugBehavior>().Count()==3','OfType<BugBehavior>().Count()==4').replace('Appearance = (BugAppearance)Math.Min(column, 2), Adorable = true','Appearance = (BugAppearance)Math.Min(column, 2), Behavior=column==1?BugBehavior.Cozy:column>=2?BugBehavior.Creepy:BugBehavior.Classic, Adorable = true'))
edit('tests/NexaweenMoreChecks.cs',lambda t:t.replace('window, "Saving & History"','window, "Session"').replace('Descendants(window).OfType<Button>().Single(b => Equals(b.Content, "Notes & History")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));','window.ShowNotesHistory();'))
print('Added functional follow-up regression checks and updated superseded behavior expectations.')
