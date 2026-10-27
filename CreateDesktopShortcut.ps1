$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appExe=@('Nexa.exe','publish\win-x64\Nexa.exe','bin\Release\net10.0-windows\Nexa.exe') | ForEach-Object {Join-Path $repoRoot $_} | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
if (-not $appExe) { throw 'Build or publish Nexa.exe first.' }
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
