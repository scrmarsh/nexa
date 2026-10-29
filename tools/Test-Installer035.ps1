param([Parameter(Mandatory=$true)][string]$Setup,[Parameter(Mandatory=$true)][string]$FixtureRoot)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$verification=[IO.Path]::GetFullPath((Join-Path $repoRoot 'verification'))+[IO.Path]::DirectorySeparatorChar
$fixture=[IO.Path]::GetFullPath($FixtureRoot)
if(-not $fixture.StartsWith($verification,[StringComparison]::OrdinalIgnoreCase)){throw 'Installer fixture must stay within verification'}
$setupPath=(Resolve-Path -LiteralPath $Setup).Path
$app=Join-Path $fixture 'app'
$data=Join-Path $fixture 'data'
$registry='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B80F06DA-5764-44B7-B632-68ACB4BE0330}_is1'
if(Test-Path -LiteralPath $registry){throw 'An existing native test registration is present; inspect it before continuing'}
$script:assertions=0
function Check([bool]$condition,[string]$message){if(-not $condition){throw "FAIL: $message"};$script:assertions++;Write-Output "PASS: $message"}
function Install([string]$tasks='desktopicon,startmenu'){
 $process=Start-Process -FilePath $setupPath -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/TASKS='+$tasks),('/LOG="'+(Join-Path $fixture ('setup-'+[Guid]::NewGuid().ToString('N')+'.log'))+'"')) -WindowStyle Hidden -Wait -PassThru
 Check ($process.ExitCode -eq 0) 'Native Setup completes without elevation or restart'
}
function Uninstall([switch]$RemoveData){
 $uninstaller=Get-ChildItem -LiteralPath $app -Filter 'unins*.exe' | Select-Object -First 1
 if(-not $uninstaller){throw 'Fixture uninstaller missing'}
 $uninstallArgs=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $fixture ('uninstall-'+[Guid]::NewGuid().ToString('N')+'.log'))+'"'))
 if($RemoveData){$uninstallArgs+=@('/REMOVEUSERDATA=1','/CONFIRMREMOVEDATA=YES')}
 $process=Start-Process -FilePath $uninstaller.FullName -ArgumentList $uninstallArgs -WindowStyle Hidden -Wait -PassThru
 Check ($process.ExitCode -eq 0) 'Native uninstaller exits successfully'
 $watch=[Diagnostics.Stopwatch]::StartNew()
 while((Test-Path -LiteralPath $registry) -and $watch.Elapsed.TotalSeconds -lt 30){Start-Sleep -Milliseconds 200}
 Check (-not (Test-Path -LiteralPath $registry)) 'Installed Apps test registration is removed'
 Check (-not (Test-Path -LiteralPath (Join-Path $fixture 'desktop/nexa.lnk'))) 'Installer-created desktop shortcut is removed'
 Check (-not (Test-Path -LiteralPath (Join-Path $fixture 'start-menu/nexa.lnk'))) 'Installer-created Start menu shortcut is removed'
}
New-Item -ItemType Directory -Force -Path $data | Out-Null
$state=Join-Path $data 'state.json'
$original='{"Notes":"Keep isolated installer acceptance data","Tasks":[],"Presets":[]}'
[IO.File]::WriteAllText($state,$original)
Install
Check (Test-Path -LiteralPath (Join-Path $app 'Nexa.exe')) 'Fresh install creates the application at the isolated install location'
$entry=Get-ItemProperty -LiteralPath $registry
Check ($entry.DisplayName -eq 'nexa' -and $entry.DisplayVersion -eq '0.1.1') 'Windows Installed Apps records the correct name and version'
Check ([IO.Path]::TrimEndingDirectorySeparator($entry.InstallLocation) -eq $app) 'Windows registration points to the actual installation'
Check (Test-Path -LiteralPath (Join-Path $app 'installation.json')) 'Installed distribution marker exists'
Check (Test-Path -LiteralPath (Join-Path $app 'setup-cache/NexaSetup.exe')) 'Repair and rollback installer is retained'
$shell=New-Object -ComObject WScript.Shell
foreach($path in @((Join-Path $fixture 'desktop/nexa.lnk'),(Join-Path $fixture 'start-menu/nexa.lnk'))){
 Check (Test-Path -LiteralPath $path) 'Requested native shortcut is created'
 $shortcut=$shell.CreateShortcut($path)
 Check ($shortcut.TargetPath -eq (Join-Path $app 'Nexa.exe')) 'Shortcut targets installed nexa rather than a build or portable copy'
}
Check ([IO.File]::ReadAllText($state) -eq $original) 'Installation preserves existing private save bytes'
$health=Join-Path $fixture 'startup-health'
$process=Start-Process -FilePath (Join-Path $app 'Nexa.exe') -ArgumentList @('--verify-startup',('"'+$health+'"')) -WindowStyle Hidden -Wait -PassThru
Check ($process.ExitCode -eq 0 -and (Test-Path -LiteralPath (Join-Path $health 'startup.json'))) 'Installed executable completes real Windows startup with isolated state'
Install
Check ((Get-ChildItem (Join-Path $fixture 'desktop') -Filter '*.lnk').Count -eq 1) 'Repeated installation does not duplicate desktop shortcuts'
$payloadDigest=(Get-FileHash -LiteralPath (Join-Path $app 'Nexa.exe') -Algorithm SHA256).Hash
$integrity=Get-Content -LiteralPath (Join-Path $app 'installer-integrity.json') -Raw | ConvertFrom-Json
Check ($integrity.PayloadSHA256 -eq $payloadDigest) 'Installed integrity manifest matches the real application payload'
$beforeRepair=(Get-Item -LiteralPath (Join-Path $app 'Nexa.exe')).LastWriteTimeUtc
Install
Check ((Get-Item -LiteralPath (Join-Path $app 'Nexa.exe')).LastWriteTimeUtc -eq $beforeRepair) 'Repair keeps an already correct payload without replacing it'
$repairLegacy=Join-Path $fixture 'legacy/repair-preserved/Nexa.exe'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $repairLegacy) | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'verification/release010-beta/Application/Nexa.exe') -Destination $repairLegacy
$shortcutBefore=@{};foreach($name in @('desktop','start-menu')){$shortcutBefore[$name]=(Get-FileHash -LiteralPath (Join-Path $fixture ($name+'/nexa.lnk'))).Hash}
$repairLog=Join-Path $fixture 'explicit-repair.log'
$repairProcess=Start-Process -FilePath $setupPath -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/MAINTENANCE=repair','/TASKS=!desktopicon,!startmenu',('/LOG="'+$repairLog+'"')) -WindowStyle Hidden -Wait -PassThru
Check ($repairProcess.ExitCode -eq 0) 'Explicit Repair Files completes through the registered-install validation path'
Check (Test-Path -LiteralPath $repairLegacy) 'Explicit repair does not invoke legacy cleanup'
foreach($name in @('desktop','start-menu')){Check ((Get-FileHash -LiteralPath (Join-Path $fixture ($name+'/nexa.lnk'))).Hash -eq $shortcutBefore[$name]) 'Repair skips shortcut creation/removal even when task switches are supplied'}
Check ([IO.File]::ReadAllText($state) -eq $original) 'Explicit repair preserves exact user save bytes'
[IO.File]::WriteAllText((Join-Path $app 'Nexa.exe'),'deliberately corrupted fixture payload')
Install
Check ((Get-FileHash -LiteralPath (Join-Path $app 'Nexa.exe') -Algorithm SHA256).Hash -eq $payloadDigest) 'Repair replaces corrupt payload bytes using the trusted embedded manifest'
Remove-Item -LiteralPath (Join-Path $app 'Nexa.exe')
Install
Check (Test-Path -LiteralPath (Join-Path $app 'Nexa.exe')) 'Rerunning Setup repairs a missing application file'
Check ([IO.File]::ReadAllText($state) -eq $original) 'Repair preserves private saves'
Uninstall
Check (Test-Path -LiteralPath $state) 'Default uninstall keeps Spaces, settings and history'
Check ([IO.File]::ReadAllText($state) -eq $original) 'Preserve-data uninstall keeps exact save bytes'
$oldPortable=Join-Path $repoRoot 'verification/release034-final/Application/Nexa.exe'
$oldPortableHash=(Get-FileHash -LiteralPath $oldPortable -Algorithm SHA256).Hash
New-Item -ItemType Directory -Path $app -Force | Out-Null
$unregisteredCopy=Join-Path $app 'Nexa.exe'
Copy-Item -LiteralPath $oldPortable -Destination $unregisteredCopy
$blocked=Start-Process -FilePath $setupPath -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $fixture 'unregistered-destination.log')+'"')) -WindowStyle Hidden -Wait -PassThru
Check ($blocked.ExitCode -eq 0) 'Mandatory clean-install cleanup replaces the verified obsolete destination before installing'
Check ((Get-FileHash -LiteralPath $unregisteredCopy -Algorithm SHA256).Hash -eq $payloadDigest -and (Test-Path -LiteralPath $registry)) 'Cleanup completes before the new payload is installed and registered'
Check ([IO.File]::ReadAllText($state) -eq $original) 'Mandatory legacy cleanup preserves private saves'
if([IO.Path]::GetFullPath($unregisteredCopy) -ne (Join-Path $fixture 'app/Nexa.exe')){throw 'Unexpected fixture cleanup target'}
Remove-Item -LiteralPath $unregisteredCopy
Check ((Get-FileHash -LiteralPath $oldPortable -Algorithm SHA256).Hash -eq $oldPortableHash) 'Approved .34 source payload remains unchanged'
Install 'startmenu'
Check (-not (Test-Path -LiteralPath (Join-Path $fixture 'desktop/nexa.lnk'))) 'Desktop-shortcut opt-out is honored on reinstall'
Check (Test-Path -LiteralPath (Join-Path $fixture 'start-menu/nexa.lnk')) 'Start menu entry is available after reinstall'
Uninstall -RemoveData
Check (-not (Test-Path -LiteralPath $data)) 'Explicit doubly confirmed data removal removes only the fixture data store'
$identity=[Security.Principal.WindowsIdentity]::GetCurrent()
$principal=[Security.Principal.WindowsPrincipal]::new($identity)
[ordered]@{Assertions=$script:assertions;Setup=$setupPath;Fixture=$fixture;CurrentTokenIsAdministrator=$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);OS=[Environment]::OSVersion.VersionString;CompletedUtc=[DateTimeOffset]::UtcNow} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'native-result.json')
Write-Output "PASS: $script:assertions native installer checks"
