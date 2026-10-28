from pathlib import Path
import shutil
root=Path('M:/Nexa'); stage=Path(__file__).parent
backup=Path('M:/Aevoria-old/Nexa-embedded-20261026/legacy-outputs-20261027/previous-reports')
backup.mkdir(parents=True,exist_ok=True)
for name in ['NEXAWEEN024_IMPLEMENTATION.md','README.md','NEXA024_CONTINUATION.md','CODEX_SESSION_STATE.md','CreateDesktopShortcut.ps1']:
    p=root/name
    if p.exists() and not (backup/name).exists():shutil.copy2(p,backup/name)
for name in ['NEXAWEEN024_IMPLEMENTATION.md','ANIMATION_NOTES.md']:
    shutil.copy2(stage/name,root/name)
(root/'README.md').write_text('''# nexa v0.1.24 alpha — NEXAWEEN

Your dashboard just got a little spooky.

Windows desktop dashboard built with .NET 10/WPF. Launch `Nexa.exe` from this folder; it is self-contained. `run.bat` builds and launches the source version.

The update adds weekly class scheduling (elapsed digits during class, countdown between classes), continuous saved history, configurable autosave and protected resets, time zones, floating notifications, productivity widgets and a shared seasonal bug ecosystem. Six creatures use distinct anatomy/gaits, appearances and Halloween costumes; animations support reduced motion.

Read [the implementation and verification report](NEXAWEEN024_IMPLEMENTATION.md) for the 259 checks, files changed and outstanding manual acceptance. [Species animation notes](ANIMATION_NOTES.md) document anatomical choices and remaining polish.

Build: `dotnet build SessionDashboard.csproj -c Release`. Checks: build `tests/SessionDashboard.Checks.csproj`; run its executable with no argument or `--reliability`, `--core`, `--continuity`, `--shared`, `--menus`, `--ecosystem`, `--animation`. Artifacts/logs live in `verification`.

The original Aevoria/Colony Wars/Living Worlds code and obsolete outputs are preserved in `M:/Aevoria-old/Nexa-embedded-20261026`. The new ecosystem is independent. The separate Aevoria project is unchanged. Historical changelog entries remain for reference.
''',encoding='utf-8')
state='''# Nexa 0.1.24 current handoff — October 27, 2026

The complete master prompt was supplied. Current implementation/verification is documented in NEXAWEEN024_IMPLEMENTATION.md and ANIMATION_NOTES.md. Earlier statements that the prompt is missing or the ecosystem is unimplemented are superseded.

Actual source: M:/Nexa. Helper scripts: C:/Users/<user>/Documents/ChatGPT/Nexa/implementation. Many helper scripts are non-idempotent staging steps; M: is authoritative. Do not rerun old stages over the integrated source.

User overrides: version stays 0.1.24; lowercase splash/home label nexa v0.1.24 alpha with no suffix; green branding and requested drink colors; full NEW Cozy/Creepy/Combative ecosystem; timer digits UP in class and DOWN between classes. Legacy games remain archived, not dependencies. Latest user emphasis: each species' animation must be anatomically appropriate.

259 checks passed, release build zero warnings/errors, self-contained win-x64 startup verified. Logs: verification/release-acceptance. Published root Nexa.exe and publish/win-x64/Nexa.exe. Package hash/backup/archive details: verification/release-package.json. The old running candidate is closed normally before installation; production profile backed up. No commit/push/online release.

Remaining acceptance: physical hibernation/power loss, mixed-DPI monitors/fullscreen, live credentials/microphone integrations and prolonged memory/GPU tests. Further 3D foot locking, articulated cooperative lifting and detailed construction/social choreography remain animation polish. Do not claim exact biological simulation or exhaustive completion of every advanced prompt detail.
'''
for name in ['NEXA024_CONTINUATION.md','CODEX_SESSION_STATE.md']:(root/name).write_text(state,encoding='utf-8')
Path('C:/Users/<user>/Documents/ChatGPT/Nexa/NEXA024_CONTINUATION.md').write_text(state,encoding='utf-8')
(root/'CreateDesktopShortcut.ps1').write_text('''$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appExe = Join-Path $repoRoot 'Nexa.exe'
if (-not (Test-Path -LiteralPath $appExe)) { throw 'Build or publish Nexa.exe first.' }
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Nexa.lnk'
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $appExe
$shortcut.WorkingDirectory = $repoRoot
$shortcut.IconLocation = "$appExe,0"
$shortcut.Description = 'Nexa'
$shortcut.WindowStyle = 1
$shortcut.Save()
Write-Output "Desktop shortcut created: $shortcutPath"
''',encoding='utf-8-sig')
print('Updated release report, anatomical references, current handoff and shortcut helper.')
