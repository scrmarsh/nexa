param([Parameter(Mandatory=$true)][string]$Setup,[Parameter(Mandatory=$true)][string]$FixtureRoot,[ValidateSet('Prepare','VerifyAndClose')][string]$Stage='Prepare')
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot;$fixture=[IO.Path]::GetFullPath($FixtureRoot);$scope=[IO.Path]::GetFullPath((Join-Path $repo 'verification'))+[IO.Path]::DirectorySeparatorChar
if(-not $fixture.StartsWith($scope,[StringComparison]::OrdinalIgnoreCase)){throw 'First-launch fixture must stay within verification'}
$app=Join-Path $fixture 'app/Nexa.exe';$data=Join-Path $fixture 'data';$registry='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B80F06DA-5764-44B7-B632-68ACB4BE0330}_is1';$pidFile=Join-Path $fixture 'first-launch-process.json'
function Check([bool]$ok,[string]$message){if(-not $ok){throw ('FAIL: '+$message)};Write-Output ('PASS: '+$message)}
function RunHelper([string[]]$arguments){$process=Start-Process -FilePath $app -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru;return $process.ExitCode}
if($Stage -eq 'Prepare'){
 if(Test-Path -LiteralPath $registry){throw 'A TestAppId installation is already active'}
 if(Test-Path -LiteralPath (Join-Path $data 'state.json')){throw 'First-launch fixture must have no saved workspace'}
 $setupPath=(Resolve-Path -LiteralPath $Setup).Path
 $setupProcess=Start-Process -FilePath $setupPath -ArgumentList @('/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $fixture 'first-launch-setup.log')+'"')) -WindowStyle Hidden -Wait -PassThru
 Check ($setupProcess.ExitCode -eq 0) 'Fresh isolated Setup completes for first-launch verification'
 $marker=Get-Content -LiteralPath (Join-Path $fixture 'app/installation.json') -Raw | ConvertFrom-Json
 Check ($marker.AppId -eq '{B80F06DA-5764-44B7-B632-68ACB4BE0330}' -and [IO.Path]::GetFullPath($marker.DataRoot) -eq $data) 'First-launch application uses only the isolated TestAppId data root'
 Check (-not (Test-Path -LiteralPath (Join-Path $data 'state.json'))) 'Setup has not created a completed workspace before onboarding'
 $process=Start-Process -FilePath $app -WorkingDirectory (Split-Path -Parent $app) -WindowStyle Normal -PassThru
 $watch=[Diagnostics.Stopwatch]::StartNew();do{$process.Refresh();if($process.HasExited){throw ('Application exited before onboarding: '+$process.ExitCode)};if($process.MainWindowHandle -ne 0){break};Start-Sleep -Milliseconds 100}while($watch.Elapsed.TotalSeconds -lt 30)
 Check ($process.MainWindowTitle -eq 'Welcome to Nexa') 'First normal packaged launch shows Welcome to Nexa'
 [ordered]@{Pid=$process.Id;Application=$app;Fixture=$fixture;InitialTitle=$process.MainWindowTitle;StartedUtc=[DateTimeOffset]::UtcNow} | ConvertTo-Json | Set-Content -LiteralPath $pidFile
 Write-Output 'First-launch fixture is ready for native visual acceptance.'
 return
}
$identity=Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json;$target=Get-Process -Id $identity.Pid
Check ($target.Path -eq $app) 'Acceptance process still belongs to the isolated application path'
$inventory=Join-Path $fixture 'process-inventory.json';[ordered]@{Candidates=@([ordered]@{Executable=$app;Eligible=$true})} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $inventory
$report=Join-Path $fixture 'running-apps.json'
$exitCode=RunHelper @('--installer-running-apps',('"'+$report+'"'),('"'+$inventory+'"'))
Check ($exitCode -eq 3) 'Packaged helper identifies the verified running application'
$running=@(Get-Content -LiteralPath $report -Raw | ConvertFrom-Json)
Check ($running.Count -eq 1 -and $running[0].Pid -eq $target.Id -and $running[0].Path -eq $app) 'Process inventory names only the requested application, excluding Setup and helper processes'
Check (-not $target.HasExited) 'Read-only process detection leaves the application running until closure is confirmed'
$close=RunHelper @('--installer-close-apps',('"'+$report+'"'))
Check ($close -eq 0 -and $target.WaitForExit(5000)) 'Verified Close Nexa sends graceful shutdown and confirms exit'
Check ((RunHelper @('--installer-running-apps',('"'+$report+'"'),('"'+$inventory+'"'))) -eq 0) 'Repeated detection reports no application after verified exit'
$uninstaller=Get-ChildItem -LiteralPath (Split-Path -Parent $app) -Filter 'unins*.exe' | Select-Object -First 1
$remove=Start-Process -FilePath $uninstaller.FullName -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -Wait -PassThru
Check ($remove.ExitCode -eq 0 -and -not (Test-Path -LiteralPath $registry)) 'Data-preserving fixture uninstall removes only TestAppId registration'
[ordered]@{Verified=$true;Fixture=$fixture;Application=$app;Pid=$target.Id;CompletedUtc=[DateTimeOffset]::UtcNow} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixture 'first-launch-result.json')
