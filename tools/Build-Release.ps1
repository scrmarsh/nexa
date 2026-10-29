param([string]$OutputDirectory='verification/release012-beta',[string]$Compiler)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
 $out=[IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
 $allowed=[IO.Path]::GetFullPath((Join-Path $repoRoot 'verification'))+[IO.Path]::DirectorySeparatorChar
 if(-not $out.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Unapproved release builds must stay under verification'}
 if(-not $Compiler){$Compiler=& tools/Get-InstallerCompiler.ps1}
 $payload=Join-Path $out 'Application'
 & dotnet publish SessionDashboard.csproj -c Release -r win-x64 --self-contained true -p:NexaDistribution=Installed -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:BaseOutputPath=bin/Release035Application/ -p:UseSharedCompilation=false -nodeReuse:false -m:1 -o $payload
 if($LASTEXITCODE -ne 0){throw 'Application publish failed'}
 & tools/Build-Installer.ps1 -PublishDir $payload -Compiler $Compiler -OutputDirectory $OutputDirectory
 Copy-Item -LiteralPath (Join-Path $payload 'Nexa.exe') -Destination (Join-Path $out 'nexa-portable.exe')
 & tools/Build-MigrationBridge.ps1 -Installer (Join-Path $out 'NexaSetup.exe') -OutputDirectory (Join-Path $OutputDirectory 'LegacyBridge')
 Copy-Item -LiteralPath (Join-Path $out 'LegacyBridge/Nexa.exe') -Destination (Join-Path $out 'Nexa.exe')
 $lines=foreach($name in @('NexaSetup.exe','nexa-portable.exe','Nexa.exe')){(Get-FileHash -LiteralPath (Join-Path $out $name) -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$name}
 $lines | Set-Content -LiteralPath (Join-Path $out 'SHA256SUMS.txt') -Encoding utf8NoBOM
 Write-Output "Local release candidates only: $out"
} finally {Pop-Location}
