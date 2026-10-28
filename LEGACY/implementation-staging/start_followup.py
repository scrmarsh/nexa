from pathlib import Path
import shutil
r=Path('M:/Nexa'); backup=r/'verification/before-followup-20261027'
backup.mkdir(parents=True,exist_ok=True)
for p in r.glob('*'):
 if p.is_file() and p.suffix.lower() in ['.cs','.xaml','.md','.csproj','.ps1'] and not (backup/p.name).exists():shutil.copy2(p,backup/p.name)
for p in (r/'tests').glob('*.cs'):
 (backup/'tests').mkdir(exist_ok=True)
 if not (backup/'tests'/p.name).exists():shutil.copy2(p,backup/'tests'/p.name)
shutil.copy2(Path('C:/Users/<user>/.codex/attachments/d26a6164-b433-4ca6-8a19-0fa7ab1676d6/Pasted text.txt'),r/'NEXAWEEN_FOLLOWUP_REQUIREMENTS.md')
p=r/'tests/Nexa024Runner.cs'
s=p.read_text(encoding='utf-8-sig').replace('            if(args.Contains("--animation"))','''            if(args.Contains("--ui-controls"))
            {
                var store=new StateStore(Path.Combine(output,"isolated-controls.json"));
                var main=new MainWindow(store);app.MainWindow=main;main.Show();
                var settings=new SettingsDialog(main,store,main.state,main,"Add Bugs");settings.ShowDismissible();
                main.Closed+=(_,_)=>app.Shutdown();app.Run();return 0;
            }
            if(args.Contains("--animation"))''')
p.write_text(s,encoding='utf-8-sig')
print('Preserved current source and loaded the follow-up requirements; added isolated live-control verification.')
