param([Parameter(Mandatory=$true)][string]$Application,[Parameter(Mandatory=$true)][string]$FixtureRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$verification=[IO.Path]::GetFullPath((Join-Path $repo 'verification'))+[IO.Path]::DirectorySeparatorChar
$fixture=[IO.Path]::GetFullPath($FixtureRoot)
if(-not $fixture.StartsWith($verification,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture must stay inside verification'}
if(Test-Path -LiteralPath $fixture){throw 'Choose a new isolated cleanup fixture'}
$binary=(Resolve-Path -LiteralPath $Application).Path
$legacy=(Resolve-Path -LiteralPath (Join-Path $repo 'verification/release010-beta/Application/Nexa.exe')).Path
$script:checks=0
function Check([bool]$ok,[string]$message){if(-not $ok){throw ('FAIL: '+$message)};$script:checks++;Write-Output ('PASS: '+$message)}
$target=Join-Path $fixture 'app';$data=Join-Path $fixture 'data';$oldA=Join-Path $fixture 'legacy/alpha-beta';$oldB=Join-Path $fixture 'nondefault/old-nexa'
New-Item -ItemType Directory -Force -Path $target,$data,$oldA,$oldB | Out-Null
'Isolated cleanup fixture' | Set-Content -LiteralPath (Join-Path $fixture '.nexa-installer-fixture')
Copy-Item -LiteralPath $binary -Destination (Join-Path $target 'Nexa.exe')
foreach($folder in @($oldA,$oldB)){Copy-Item -LiteralPath $legacy -Destination (Join-Path $folder 'Nexa.exe');'private content' | Set-Content -LiteralPath (Join-Path $folder 'notes.txt')}
'not Nexa' | Set-Content -LiteralPath (Join-Path $oldA 'unrelated.exe')
$plan=Join-Path $fixture 'inventory.json'
function Scan {
 $arguments=@('--installer-legacy-scan',('"'+$plan+'"'),('"'+$data+'"'),('"'+$target+'"'),'cleanup','fixture',('"'+(Join-Path $fixture 'legacy')+'"'),('"'+(Join-Path $fixture 'nondefault')+'"'))
 $process=Start-Process -FilePath $binary -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
 Check ($process.ExitCode -eq 0) 'Packaged cleanup-only scan exits successfully'

}
function Remove([int]$expected=0){$process=Start-Process -FilePath $binary -ArgumentList @('--installer-legacy-remove',('"'+$plan+'"')) -WindowStyle Hidden -Wait -PassThru;Check ($process.ExitCode -eq $expected) ('Packaged cleanup removal returns truthful status '+$expected)}
Scan

$inventory=Get-Content -LiteralPath $plan -Raw | ConvertFrom-Json
Check (@($inventory.Candidates | Where-Object Eligible).Count -eq 2) 'Native scan finds both obsolete copies including the nondefault reference'
Remove
$inventory=Get-Content -LiteralPath $plan -Raw | ConvertFrom-Json
Check (@($inventory.Candidates | Where-Object Outcome -eq 'Removed and verified').Count -eq 2) 'Both obsolete binaries are verified removed'
Check ((Get-FileHash -LiteralPath (Join-Path $target 'Nexa.exe')).Hash -eq (Get-FileHash -LiteralPath $binary).Hash) 'Cleanup-only preserves the current application bytes'
Check ((Test-Path -LiteralPath (Join-Path $oldA 'notes.txt')) -and (Test-Path -LiteralPath (Join-Path $oldB 'notes.txt')) -and (Test-Path -LiteralPath (Join-Path $oldA 'unrelated.exe'))) 'Cleanup-only preserves adjacent private content and unrelated binaries'
Check (-not (Test-Path -LiteralPath (Join-Path $target 'installation.json'))) 'Cleanup-only does not install or register an application'
Scan;Remove
Check ((Test-Path -LiteralPath (Join-Path $target 'Nexa.exe')) -and -not (Test-Path -LiteralPath (Join-Path $oldA 'Nexa.exe'))) 'Repeated cleanup remains safe'
Copy-Item -LiteralPath $legacy -Destination (Join-Path $oldA 'Nexa.exe')
Scan
$locked=[IO.File]::Open((Join-Path $oldA 'Nexa.exe'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None)
try{Remove 1;$failed=Get-Content -LiteralPath $plan -Raw | ConvertFrom-Json;Check (@($failed.Candidates | Where-Object {$_.Outcome -like 'Failed:*'}).Count -eq 1) 'Locked removal is reported as a failure'}finally{$locked.Dispose()}
Remove
Check (-not (Test-Path -LiteralPath (Join-Path $oldA 'Nexa.exe'))) 'Retry succeeds after the locked file is released'
[ordered]@{Assertions=$script:checks;Application=$binary;Fixture=$fixture;CompletedUtc=[DateTimeOffset]::UtcNow} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'native-summary.json')
Write-Output ('PASS: '+$script:checks+' native cleanup-only checks')
