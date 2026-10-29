param([Parameter(Mandatory=$true)][string]$Setup,[Parameter(Mandatory=$true)][string]$FixtureRoot)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$fixture=[IO.Path]::GetFullPath($FixtureRoot)
$scope=[IO.Path]::GetFullPath((Join-Path $repo 'verification'))+[IO.Path]::DirectorySeparatorChar
if(-not $fixture.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture must stay inside verification'}
$registry='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B80F06DA-5764-44B7-B632-68ACB4BE0330}_is1'
if(Test-Path -LiteralPath $registry){throw 'Run after the isolated lifecycle uninstaller completes'}
$app=Join-Path $fixture 'app/Nexa.exe';if(Test-Path -LiteralPath $app){throw 'Fixture already has an application'}
$legacy=Join-Path $fixture 'legacy/blocked-clean/Nexa.exe';$data=Join-Path $fixture 'data';$state=Join-Path $data 'state.json'
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $legacy),$data | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'verification/release010-beta/Application/Nexa.exe') -Destination $legacy
[IO.File]::WriteAllText($state,'{"Notes":"Preserve this isolated mandatory-cleanup failure fixture"}')
$digest=(Get-FileHash -LiteralPath $state).Hash
$locked=[IO.File]::Open($legacy,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
try{
 $process=Start-Process -FilePath (Resolve-Path -LiteralPath $Setup).Path -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $fixture 'blocked-clean.log')+'"')) -WindowStyle Hidden -Wait -PassThru
 if($process.ExitCode -eq 0){throw 'Blocked cleanup falsely returned success'}
 if(Test-Path -LiteralPath $app){throw 'Blocked cleanup installed a new payload'}
 if(Test-Path -LiteralPath $registry){throw 'Blocked cleanup created an installation registration'}
 if(-not (Test-Path -LiteralPath $legacy)){throw 'Locked obsolete copy disappeared'}
 if((Get-FileHash -LiteralPath $state).Hash -ne $digest){throw 'User save bytes changed'}
}finally{$locked.Dispose()}
[ordered]@{Assertions=5;ExitCode=$process.ExitCode;Fixture=$fixture;SaveSHA256=$digest} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'blocked-summary.json')
Write-Output 'PASS: 5 native mandatory-cleanup blocking checks'
