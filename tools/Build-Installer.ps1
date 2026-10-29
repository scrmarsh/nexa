param(
 [Parameter(Mandatory=$true)][string]$PublishDir,
 [Parameter(Mandatory=$true)][string]$Compiler,
 [string]$OutputDirectory='verification/installer033',
 [string]$TestRoot,
 [string]$Version='0.0.33'
)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$payload=(Resolve-Path -LiteralPath $PublishDir).Path
$compilerPath=(Resolve-Path -LiteralPath $Compiler).Path
$binary=Join-Path $payload 'Nexa.exe'
if(-not (Test-Path -LiteralPath $binary -PathType Leaf)){throw 'Published Nexa.exe missing'}
$info=(Get-Item -LiteralPath $binary).VersionInfo
if($info.FileVersion -ne "$Version.0" -or $info.ProductName -ne 'Nexa'){throw 'Application version/identity does not match requested installer'}
$outputPath=[IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null
$arguments=@("/DPublishDir=$payload","/DOutputDirPath=$outputPath","/DAppVersion=$Version")
if($TestRoot){
 $testPath=[IO.Path]::GetFullPath($TestRoot)
 $verificationRoot=[IO.Path]::GetFullPath((Join-Path $repoRoot 'verification'))+[IO.Path]::DirectorySeparatorChar
 if(-not $testPath.StartsWith($verificationRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Native fixture root must remain under verification'}
 $arguments+="/DTestRoot=$testPath"
}
& $compilerPath @arguments (Join-Path $repoRoot 'installer/nexa.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
$setup=Get-Item -LiteralPath (Join-Path $outputPath 'NexaSetup.exe')
[ordered]@{Version=$Version;Framework='Inno Setup 7.1.1';SourceCommit=(& git -C $repoRoot rev-parse HEAD);PayloadSHA256=(Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash;InstallerSHA256=(Get-FileHash -LiteralPath $setup.FullName -Algorithm SHA256).Hash;NativeFixture=[bool]$TestRoot;Signed=$false} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outputPath 'build-identity.json')
$setup.FullName
