$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$env:APPDATA = Join-Path $PSScriptRoot 'obj\publish-appdata'
New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
if (-not ${env:ProgramFiles(x86)}) { ${env:ProgramFiles(x86)} = 'C:\Program Files (x86)' }
if (-not $env:ProgramData) { $env:ProgramData = 'C:\ProgramData' }
dotnet publish .\SessionDashboard.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:BaseIntermediateOutputPath=obj/standalone/ -p:DebugType=None -p:DebugSymbols=false --configfile .\tools\Publish.NuGet.Config -o .\publish\win-x64 -p:UseSharedCompilation=false -nodeReuse:false -m:1
if ($LASTEXITCODE -ne 0) { throw 'Nexa publish failed.' }
Write-Host "Ready: $PSScriptRoot\publish\win-x64\Nexa.exe"
