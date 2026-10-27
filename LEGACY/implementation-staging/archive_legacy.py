from pathlib import Path
import shutil,json
root=Path(r'M:\Nexa').resolve()
archive=Path(r'M:\Aevoria-old\Nexa-embedded-20261026\remaining-legacy').resolve()
assert str(root)==r'M:\Nexa' and archive.is_relative_to(Path(r'M:\Aevoria-old').resolve())
archive.mkdir(parents=True,exist_ok=True)
moved=[]
def move(path):
    if not path.exists():return
    assert path.resolve().is_relative_to(root)
    rel=path.relative_to(root);dest=archive/rel
    assert dest.resolve().is_relative_to(archive)
    if dest.exists():raise RuntimeError(f'Archive destination already exists: {dest}')
    dest.parent.mkdir(parents=True,exist_ok=True);shutil.move(str(path),str(dest));moved.append(str(rel))
keep={'Nexa024Runner.cs','Nexa024CoreChecks.cs','Nexa024ReliabilityChecks.cs','NexaweenCoreChecks.cs','NexaweenMoreChecks.cs','ContinuityChecks.cs','Shared012Checks.cs','MenuRepairChecks.cs'}
for path in list((root/'tests').glob('*.cs')):
    if path.name not in keep:move(path)
project=root/'tests'/'SessionDashboard.Checks.csproj';dest=archive/'tests'/'SessionDashboard.Checks.csproj'
dest.parent.mkdir(parents=True,exist_ok=True)
if not dest.exists():shutil.copy2(project,dest)
project.write_text((root/'tests'/'Nexa024.Checks.csproj').read_text(encoding='utf-8'),encoding='utf-8')
for name in ['AEVORIA_CHANGELOG.md','CIVILIZATIONS024.md','DEVELOPMENT024.md','NATIONS024.md','HARDWARE_ACCELERATION_RESEARCH.md','HISTORICAL_COMPATIBILITY_CHECKPOINT.md','RELEASE019.md','RELEASE020.md','RELEASE021.md','RELEASE022.md','RELEASE023.md','assets/history','docs/historical-atlas-source.json','docs/Nexa_Grand_Historical_Update_Civilizations_to_4000.docx','tools/Build-HistoricalAtlas.mjs','tools/Import-HistoricalAtlas.ps1']:move(root/name)
p=root/'SessionDashboard.csproj';text=p.read_text(encoding='utf-8')
text='\n'.join(line for line in text.splitlines() if 'assets\\history\\' not in line and 'historical-atlas-source.json' not in line)
p.write_text(text,encoding='utf-8')
p=root/'README.md'
dest=archive/'README-before-master.md'
if not dest.exists():shutil.copy2(p,dest)
p.write_text('''# nexa v0.1.24 alpha — NEXAWEEN

Your dashboard just got a little spooky.

Windows desktop dashboard built with .NET 10 and WPF. The project folder is `M:/Nexa`.

Core update: weekly class scheduling, elapsed class time with sequential countdown pixels, saved notes/history, configurable autosave and safe resets, selected time zones, floating notifications, and separate productivity widgets.

Build and launch: run `run.bat`, or build `SessionDashboard.csproj` in Release and open `bin/Release/net10.0-windows/Nexa.exe`.

Tests: build `tests/SessionDashboard.Checks.csproj` and run its executable with `--core`, `--continuity`, `--shared`, `--menus`, or `--reliability`. With no argument it checks the weekly timer and productivity UI.

The original Aevoria, Colony Wars, Living Worlds, battlefield and historical game tools are archived under `M:/Aevoria-old/Nexa-embedded-20261026`. They are not dependencies of Nexa. Older in-app changelog entries remain historical records.

The complete master update is still in progress. Consult the current acceptance report before treating any unfinished subsystem as released. Existing portable executables and old build/test outputs have not yet been replaced by a final package.
''',encoding='utf-8')
(archive/'manifest.json').write_text(json.dumps({'source':str(root),'moved':moved},indent=2),encoding='utf-8')
print(f'Archived {len(moved)} obsolete source, document, asset, and tooling entries; standard Nexa test project now targets the valid core suite.')
