param([string]$Directory='verification/tooling/inno-7.1.1')
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$root=[IO.Path]::GetFullPath((Join-Path $repoRoot $Directory))
$allowed=[IO.Path]::GetFullPath((Join-Path $repoRoot 'verification'))+[IO.Path]::DirectorySeparatorChar
if(-not $root.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Compiler extraction must stay within verification'}
$compiler=Join-Path $root 'portable/ISCC.exe'
if(Test-Path -LiteralPath $compiler){return $compiler}
New-Item -ItemType Directory -Force -Path $root | Out-Null
$download=Join-Path $root 'innosetup-7.1.1-x64.exe'
Invoke-WebRequest -Uri 'https://github.com/jrsoftware/issrc/releases/download/is-7_1_1/innosetup-7.1.1-x64.exe' -OutFile $download
if((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne '8A5A6711C3766A6E5FB74F280BA0E3551E719CE94538D901FDBD1F8F9920B3F0'){throw 'Compiler download checksum mismatch'}
$signature=Get-AuthenticodeSignature -LiteralPath $download
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*Pyrsys B.V.*'){throw 'Unexpected compiler publisher/signature'}
$destination=Join-Path $root 'portable'
$process=Start-Process -FilePath $download -ArgumentList @('/PORTABLE=1','/CURRENTUSER','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS',('/DIR="'+$destination+'"')) -WindowStyle Hidden -Wait -PassThru
if($process.ExitCode -ne 0){throw 'Compiler portable extraction failed'}
$compiler
