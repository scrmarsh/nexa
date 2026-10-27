$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath('M:\Nexa')
$archive=[IO.Path]::GetFullPath('M:\Aevoria-old\Nexa-embedded-20261026\legacy-outputs-20261027')
$candidate=Join-Path $root 'publish\nexa024-current\Nexa.exe'
if(-not (Test-Path -LiteralPath $candidate)){throw 'Verified candidate is missing'}
$startup=Get-Content -LiteralPath (Join-Path $root 'verification\published-startup\startup.json') -Raw | ConvertFrom-Json
if(-not $startup.Success -or $startup.AppVersion -ne '0.1.24'){throw 'Published startup check did not pass'}
foreach($route in @('animation','ecosystem','master','reliability','core','continuity','shared','menus')) {
 $last=Get-Content -LiteralPath (Join-Path $root "verification\release-acceptance\$route.log") -Tail 1
 if($last -notmatch '^PASS: \d+ checks\. Artifacts:'){throw "Acceptance route failed: $route"}
}
$old=Get-Process Nexa -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $root 'test-output\nexaween\candidate\Nexa.exe')}
foreach($process in $old){
 if(-not $process.CloseMainWindow()){throw 'The old Nexa window did not accept a normal close'}
 if(-not $process.WaitForExit(15000)){throw 'Nexa is still open; resolve its save/recording prompt before replacing the build'}
}
# Copy the saved profile after the old application has completed its normal save.
$data=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'SessionDashboard'
$backup=Join-Path $root 'verification\production-backup-before-024-20261027'
if(Test-Path -LiteralPath $data){
 if(Test-Path -LiteralPath $backup){throw 'Production backup already exists; do not overwrite it'}
 Copy-Item -LiteralPath $data -Destination $backup -Recurse
}
function Archive-Output([string]$source,[string]$relative){
 $source=[IO.Path]::GetFullPath($source)
 $destination=[IO.Path]::GetFullPath((Join-Path $archive $relative))
 if(-not $source.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Archive source escaped Nexa'}
 if(-not $destination.StartsWith($archive+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Archive target escaped the explicitly named archive'}
 if(-not (Test-Path -LiteralPath $source)){return}
 if(Test-Path -LiteralPath $destination){throw "Archive target already exists: $destination"}
 New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
 Move-Item -LiteralPath $source -Destination $destination
 Add-Content -LiteralPath (Join-Path $archive 'manifest.txt') -Value ($source+' -> '+$destination)
}
foreach($name in @('Nexa.exe','Nexa-0.1.19.zip','test-output','%SystemDrive%')){Archive-Output (Join-Path $root $name) $name}
foreach($directory in Get-ChildItem -LiteralPath (Join-Path $root 'bin') -Directory){if($directory.Name -ne 'Release'){Archive-Output $directory.FullName ('bin\'+$directory.Name)}}
foreach($directory in Get-ChildItem -LiteralPath (Join-Path $root 'publish') -Directory){if($directory.Name -ne 'nexa024-current'){Archive-Output $directory.FullName ('publish\'+$directory.Name)}}
foreach($directory in Get-ChildItem -LiteralPath (Join-Path $root 'obj') -Directory){if($directory.Name -notin @('Release','standalone')){Archive-Output $directory.FullName ('obj\'+$directory.Name)}}
Move-Item -LiteralPath (Join-Path $root 'publish\nexa024-current') -Destination (Join-Path $root 'publish\win-x64')
Copy-Item -LiteralPath (Join-Path $root 'publish\win-x64\Nexa.exe') -Destination (Join-Path $root 'Nexa.exe')
$hash=Get-FileHash -LiteralPath (Join-Path $root 'Nexa.exe') -Algorithm SHA256
[pscustomobject]@{Executable=$hash.Path;SHA256=$hash.Hash;Version=(Get-Item -LiteralPath $hash.Path).VersionInfo.FileVersion;Backup=$backup;Archive=$archive} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'verification\release-package.json') -Encoding utf8
Write-Output 'Archived legacy outputs, preserved the saved profile, and installed verified Nexa 0.1.24.'
