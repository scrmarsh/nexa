param([switch]$SkipBuild,[string]$OutputBase="bin/Nexa024/")
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
 $failures=@()
 if(-not $SkipBuild){dotnet build .\tests\SessionDashboard.Checks.csproj -c Release "-p:BaseOutputPath=$OutputBase" -p:UseSharedCompilation=false -nodeReuse:false -m:1;if($LASTEXITCODE -ne 0){throw 'Verification build failed'}}
 $runner=Join-Path $repoRoot (Join-Path 'tests' (Join-Path $OutputBase 'Release/net10.0-windows/SessionDashboard.Checks.exe'))
 $logs=Join-Path $repoRoot ('verification\acceptance-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
 New-Item -ItemType Directory -Force -Path $logs | Out-Null
 foreach($route in @('nexa034','nexa033','nexa032','laptop-audio','update-automation','nexa031','nexa030','nexa029','nexaween028','nexaween027','handoff026','pass026','alpha026','arrivals','audit025','final','followup-design','followup','master','reliability','core','continuity','shared','menus','ecosystem','animation')){
  $start=[Diagnostics.ProcessStartInfo]::new($runner)
  $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
  if($route -ne 'master'){$start.ArgumentList.Add('--'+$route)}
  $process=[Diagnostics.Process]::Start($start)
  $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
  $finished=$process.WaitForExit(180000)
  if(-not $finished){$process.Kill($true);$process.WaitForExit();$exitCode=124}else{$exitCode=$process.ExitCode}
  $result=@($stdout.GetAwaiter().GetResult() -split "`r?`n")
  $result+=@($stderr.GetAwaiter().GetResult() -split "`r?`n")
  $result=@($result | Where-Object {$_ -ne ''})
  if(-not $finished){$result+='FAIL: Isolated route exceeded its 180-second deadline.'}
  $result | Set-Content -LiteralPath (Join-Path $logs ($route+'.log')) -Encoding utf8
  $process.Dispose()
  Write-Output ($route+': '+($result | Select-Object -Last 1))
  if($exitCode -ne 0){$failures+=$route}
 }
 Write-Output "Acceptance logs: $logs"
 if($failures.Count -gt 0){throw "Failed routes: $($failures -join ', ')"}
} finally {Pop-Location}
