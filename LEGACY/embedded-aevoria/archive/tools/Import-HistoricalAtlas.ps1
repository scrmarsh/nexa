param([string]$SourcePath = (Join-Path $PSScriptRoot '..\docs\Nexa_Grand_Historical_Update_Civilizations_to_4000.docx'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = (Resolve-Path -LiteralPath $SourcePath).Path
$docs = Join-Path $workspace 'docs'
[IO.Directory]::CreateDirectory($docs) | Out-Null
$reference = Join-Path $docs 'Nexa_Grand_Historical_Update_Civilizations_to_4000.docx'
if ($source -ne $reference) { Copy-Item -LiteralPath $source -Destination $reference }
$zip = [IO.Compression.ZipFile]::OpenRead($reference)
try {
    $entry = $zip.GetEntry('word/document.xml')
    if ($null -eq $entry) { throw 'The reference has no Word document body.' }
    $reader = [IO.StreamReader]::new($entry.Open())
    try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
    $ns = [Xml.XmlNamespaceManager]::new($xml.NameTable)
    $ns.AddNamespace('w','http://schemas.openxmlformats.org/wordprocessingml/2006/main')
    $index = 0; $captured = 0
    $blocks = @($xml.SelectNodes('//w:body/w:p | //w:body/w:tbl/w:tr',$ns) | ForEach-Object {
        $index++; $captured += $_.SelectNodes('.//w:t',$ns).Count
        if ($_.LocalName -eq 'tr') {
            $cells = @($_.SelectNodes('w:tc',$ns) | ForEach-Object { ($_.SelectNodes('.//w:t',$ns) | ForEach-Object {$_.InnerText}) -join '' })
            [pscustomobject]@{Index=$index;Kind='row';Cells=$cells}
        } else {
            [pscustomobject]@{Index=$index;Kind='paragraph';Cells=@(($_.SelectNodes('.//w:t',$ns) | ForEach-Object {$_.InnerText}) -join '')}
        }
    })
    if ($captured -ne $xml.SelectNodes('//w:body//w:t',$ns).Count) { throw 'Unsupported document structure: body text would be lost.' }
    $result = [ordered]@{SchemaVersion=1;Document='Nexa_Grand_Historical_Update_Civilizations_to_4000.docx';Sha256=(Get-FileHash -LiteralPath $reference -Algorithm SHA256).Hash.ToLowerInvariant();Blocks=$blocks}
    $json = $result | ConvertTo-Json -Depth 10
    [IO.File]::WriteAllText((Join-Path $docs 'historical-atlas-source.json'),$json,[Text.UTF8Encoding]::new($false))
} finally { $zip.Dispose() }
& node (Join-Path $PSScriptRoot 'Build-HistoricalAtlas.mjs')
if ($LASTEXITCODE -ne 0) { throw 'Atlas import failed validation.' }
