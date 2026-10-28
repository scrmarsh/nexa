param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
 if(-not $SkipBuild){dotnet build .\tests\SessionDashboard.Checks.csproj -c Release -p:UseSharedCompilation=false -nodeReuse:false -m:1;if($LASTEXITCODE -ne 0){throw 'Verification build failed'}}
 $runner=Join-Path $repoRoot 'tests\bin\Nexa024\Release\net10.0-windows\SessionDashboard.Checks.exe'
 $logs=Join-Path $repoRoot ('verification\acceptance-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
 New-Item -ItemType Directory -Force -Path $logs | Out-Null
 foreach($route in @('audit025','final','followup-design','followup','master','reliability','core','continuity','shared','menus','ecosystem','animation')){
  if($route -eq 'master'){$result=& $runner 2>&1}else{$routeArgument="--$route";$result=& $runner $routeArgument 2>&1}
  $exitCode=$LASTEXITCODE;$result | Set-Content -LiteralPath (Join-Path $logs ($route+'.log')) -Encoding utf8
  Write-Output ($route+': '+($result | Select-Object -Last 1))
  if($exitCode -ne 0){throw "Verification failed: $route; see $logs"}
 }
 Write-Output "Acceptance logs: $logs"
} finally {Pop-Location}
