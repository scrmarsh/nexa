$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath('M:\Nexa')
$legacyRoot=[IO.Path]::GetFullPath((Join-Path $projectRoot 'LEGACY'))
if((Test-Path -LiteralPath $legacyRoot) -and ((Get-Item -LiteralPath $legacyRoot -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'LEGACY must be an ordinary project folder'}
New-Item -ItemType Directory -Force -Path $legacyRoot | Out-Null
$manifest=[Collections.Generic.List[object]]::new()
function Destination-InLegacy([string]$relative){
 $destination=[IO.Path]::GetFullPath((Join-Path $legacyRoot $relative))
 if(-not $destination.StartsWith($legacyRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Legacy destination escaped the intended folder'}
 New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
 return $destination
}
function Move-ProjectFile([string]$relative,[string]$legacyRelative){
 $source=[IO.Path]::GetFullPath((Join-Path $projectRoot $relative));$destination=Destination-InLegacy $legacyRelative
 if(-not $source.StartsWith($projectRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Project source escaped Nexa'}
 if(-not(Test-Path -LiteralPath $source)){return}
 if(Test-Path -LiteralPath $destination){throw "A legacy file already exists: $legacyRelative"}
 Move-Item -LiteralPath $source -Destination $destination
 $manifest.Add([pscustomobject]@{Action='Move';Source=$relative;Destination='LEGACY/'+$legacyRelative.Replace('\','/')})
}
function Copy-ArchiveTree([string]$sourceRoot,[string]$legacyRelative){
 if(-not(Test-Path -LiteralPath $sourceRoot)){return}
 $resolved=[IO.Path]::GetFullPath($sourceRoot)
 if(-not($resolved.StartsWith('M:\Aevoria-old\Nexa-embedded-20261026\',[StringComparison]::OrdinalIgnoreCase) -or $resolved -eq 'C:/Users/<user>\Documents\ChatGPT\Nexa\implementation')){throw 'Unexpected archive source'}
 foreach($file in Get-ChildItem -LiteralPath $resolved -Recurse -File){
  if($file.FullName -match '\\(__pycache__|bin|obj|verification)\\'){continue}
  $relative=[IO.Path]::GetRelativePath($resolved,$file.FullName)
  $destination=Destination-InLegacy (Join-Path $legacyRelative $relative)
  if(Test-Path -LiteralPath $destination){if((Get-FileHash -LiteralPath $destination).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){throw "Conflicting legacy copy: $relative"};continue}
  Copy-Item -LiteralPath $file.FullName -Destination $destination
  $manifest.Add([pscustomobject]@{Action='Recover source';Source=([IO.Path]::GetFileName($resolved)+'/'+$relative.Replace('\','/'));Destination='LEGACY/'+$legacyRelative.Replace('\','/')+'/'+$relative.Replace('\','/')})
 }
}
# Exclude archived code before restoring it beneath the project root.
$project=Join-Path $projectRoot 'SessionDashboard.csproj'
$text=[IO.File]::ReadAllText($project)
if(-not $text.Contains('LEGACY\**\*.cs')){$text=$text.Replace('<Compile Remove="verification\**\*.cs" />','<Compile Remove="LEGACY\**\*.cs" /><Page Remove="LEGACY\**\*.xaml" /><ApplicationDefinition Remove="LEGACY\**\*.xaml" />'+[Environment]::NewLine+'    <Compile Remove="verification\**\*.cs" />');[IO.File]::WriteAllText($project,$text)}
foreach($name in @('PUSH.md','PUSH.txt','CODEX_SESSION_STATE.md','NEXA024_CONTINUATION.md','PHASE1_ACCEPTANCE.md','VALIDATION.md','LEGACY.md')){Move-ProjectFile $name ('notes\'+$name)}
Move-ProjectFile 'MainWindow.SessionSettings.cs' 'source\MainWindow.SessionSettings.cs'
Move-ProjectFile 'BugSpriteAtlas.cs' 'source\BugSpriteAtlas.cs'
Move-ProjectFile 'References' 'notes\Original-product-outline.md'
Move-ProjectFile 'package-lock.json' 'tools\package-lock.json'
Move-ProjectFile 'run.txt' 'tools\run.txt'
Move-ProjectFile 'nexa.ico' 'assets\nexa.ico'
Move-ProjectFile 'tests\Nexa024.Checks.csproj' 'tests\Nexa024.Checks.csproj'
Copy-ArchiveTree 'M:\Aevoria-old\Nexa-embedded-20261026\removed' 'embedded-aevoria\source'
Copy-ArchiveTree 'M:\Aevoria-old\Nexa-embedded-20261026\remaining-legacy' 'embedded-aevoria\archive'
Copy-ArchiveTree 'C:/Users/<user>\Documents\ChatGPT\Nexa\implementation' 'implementation-staging'
foreach($name in @('cleanup_nexa.py','green_nexa_brand.ps1')){
 $source=Join-Path 'C:/Users/<user>\Documents\ChatGPT\Nexa' $name
 $target=Destination-InLegacy ('maintenance\'+$name)
 if((Test-Path -LiteralPath $source) -and -not(Test-Path -LiteralPath $target)){Copy-Item -LiteralPath $source -Destination $target}
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $legacyRoot 'manifest.json') -Encoding utf8
Write-Output ('Organized '+$manifest.Count+' legacy project files; generated outputs and personal data were not copied.')
