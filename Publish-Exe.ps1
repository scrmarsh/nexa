param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$testingRoot=[System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Builds\Testing'))
if(-not $OutputDirectory){$OutputDirectory=$testingRoot}
$OutputDirectory=[System.IO.Path]::GetFullPath($OutputDirectory)
if(-not ($OutputDirectory.Equals($testingRoot,[StringComparison]::OrdinalIgnoreCase) -or $OutputDirectory.StartsWith($testingRoot+[System.IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase))){throw 'Candidate output must stay within Builds\Testing.'}
$env:APPDATA = Join-Path $PSScriptRoot 'obj\publish-appdata'
New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
if (-not ${env:ProgramFiles(x86)}) { ${env:ProgramFiles(x86)} = 'C:\Program Files (x86)' }
if (-not $env:ProgramData) { $env:ProgramData = 'C:\ProgramData' }
dotnet publish .\SessionDashboard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:BaseIntermediateOutputPath=obj/standalone/ -p:DebugType=None -p:DebugSymbols=false --configfile .\tools\Publish.NuGet.Config -o $OutputDirectory -p:UseSharedCompilation=false -nodeReuse:false -m:1
if ($LASTEXITCODE -ne 0) { throw 'Nexa publish failed.' }
Write-Host "Ready: $OutputDirectory\Nexa.exe"
