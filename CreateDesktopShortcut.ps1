$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appExe=Join-Path $repoRoot 'Builds\Latest\Nexa.exe'
if (-not (Test-Path -LiteralPath $appExe)) { throw 'No approved build exists in Builds\Latest. Use Builds\Testing\Nexa.exe explicitly for candidate testing.' }
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
