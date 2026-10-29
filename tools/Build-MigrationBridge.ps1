param([Parameter(Mandatory=$true)][string]$Installer,[string]$OutputDirectory='verification/bridge035',[switch]$NativeFixture)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$setup=(Resolve-Path -LiteralPath $Installer).Path
$hash=(Get-FileHash -LiteralPath $setup -Algorithm SHA256).Hash
$generated=Join-Path $repoRoot 'installer/bridge/InstallerIdentity.g.cs'
$source='[assembly: System.Reflection.AssemblyMetadata("InstallerSHA256", "'+$hash+'")]'
Set-Content -LiteralPath $generated -Value $source -Encoding utf8
try {
 & dotnet publish (Join-Path $repoRoot 'installer/bridge/NexaMigrationBridge.csproj') -c Release "-p:InstallerPath=$setup" "-p:BridgeTest=$($NativeFixture.IsPresent.ToString().ToLowerInvariant())" -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:UseSharedCompilation=false -nodeReuse:false -m:1 -o (Join-Path $repoRoot $OutputDirectory)
 if($LASTEXITCODE -ne 0){throw 'Legacy migration bridge build failed'}
} finally {Remove-Item -LiteralPath $generated -ErrorAction SilentlyContinue}
