param([string]$ReferencePath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase
$repoRoot=Split-Path -Parent $PSScriptRoot
if(-not $ReferencePath){$ReferencePath=Join-Path $repoRoot 'assets\references\nexa-halloween-icon-1024.png'}
if((Get-FileHash -LiteralPath $ReferencePath -Algorithm SHA256).Hash -ne 'F08A21EC47C7814BC0A5112A480C002094DB3BE1753B32E8404026C8365BD6BF'){
 throw 'Use the authoritative October 28 reference; do not substitute a redesigned icon.'
}
# Retain the established 512-unit geometry. The supplied raster is its exact
# 2x color variant: #84CC16 / #9333EA / #F97316 / #FDBA74.
$roundPen=[System.Windows.Media.Pen]::new([System.Windows.Media.Brushes]::Black,38)
$roundPen.StartLineCap='Round';$roundPen.EndLineCap='Round';$roundPen.LineJoin='Round'
$orange=[System.Windows.Media.Geometry]::Parse('M128,384 L128,128 L384,384 L384,128').GetWidenedPathGeometry($roundPen,.001,[System.Windows.Media.ToleranceType]::Absolute)
$light=[System.Windows.Media.Geometry]::Parse('M128,384 L384,128').GetWidenedPathGeometry($roundPen,.001,[System.Windows.Media.ToleranceType]::Absolute)
$silhouette=[System.Windows.Media.Geometry]::Combine($orange,$light,[System.Windows.Media.GeometryCombineMode]::Union,$null,.001,[System.Windows.Media.ToleranceType]::Absolute)
$outlinePath=$silhouette.ToString([System.Globalization.CultureInfo]::InvariantCulture) -replace '^F[01]\s*',''
# One outline of the UNION avoids a black seam where the two orange layers cross.
$iconBody=@"
    <rect width="512" height="512" rx="116" fill="#84CC16"/>
    <polygon points="512,0 512,512 0,512" fill="#9333EA" clip-path="url(#tile)"/>
    <path d="$outlinePath" fill="#000000" stroke="#000000" stroke-width="24" stroke-linejoin="round"/>
    <line x1="128" y1="384" x2="384" y2="128" stroke="#FDBA74" stroke-width="38" stroke-linecap="round"/>
    <polyline points="128,384 128,128 384,384 384,128" fill="none" stroke="#F97316" stroke-width="38" stroke-linecap="round" stroke-linejoin="round"/>
    <rect x="12" y="12" width="488" height="488" rx="104" fill="none" stroke="#000000" stroke-width="24"/>
"@
$standalone=@"
<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512" role="img" aria-label="Nexaween icon">
  <defs><clipPath id="tile"><rect width="512" height="512" rx="116"/></clipPath></defs>
$iconBody
</svg>
"@
Set-Content -LiteralPath (Join-Path $repoRoot 'assets\nexa-icon-seasonal.svg') -Value (($standalone -replace '\r\n',"`n").TrimEnd()+"`n") -Encoding utf8 -NoNewline
foreach($name in @('nexa-logo-full-dark-seasonal.svg','nexa-logo-full-seasonal.svg')){
 $path=Join-Path $repoRoot ('assets\'+$name)
 $svg=Get-Content -LiteralPath $path -Raw
 $svg=[regex]::Replace($svg,'(?s)(<g transform="translate\(40,40\) scale\(0\.546875\)">).*?(</g>)',{param($match) $match.Groups[1].Value+"`n"+$iconBody+"`n  "+$match.Groups[2].Value})
 # Keep the classic wordmark byte-for-byte in geometry, layering and base text
 # colors. Only the originally purple X stroke receives the seasonal accent.
 $classicName=if($name -eq 'nexa-logo-full-dark-seasonal.svg'){'nexa-logo-full-dark.svg'}else{'nexa-logo-full.svg'}
 [xml]$classicSvg=Get-Content -LiteralPath (Join-Path $repoRoot ('assets\'+$classicName)) -Raw
 $classicLetters=$classicSvg.DocumentElement.ChildNodes | Where-Object {$_.LocalName -eq 'g' -and -not $_.HasAttribute('transform')}
 $wordmark=[regex]::Replace($classicLetters.OuterXml,'<path stroke="[^"]+" d="M875 112 L755 250"\s*/>','<path stroke="#FDBA74" d="M875 112 L755 250" />')
 $wordmark=$wordmark.Replace(' xmlns="http://www.w3.org/2000/svg"','') -replace '><',">`n    <"
 $wordmark=$wordmark.Replace(' />','/>').Replace("`n    </g>","`n  </g>")
 $svg=[regex]::Replace($svg,'(?s)<g fill="none" stroke-width="30".*?</g>',{param($match) $wordmark})
 Set-Content -LiteralPath $path -Value (($svg -replace '\r\n',"`n").TrimEnd()+"`n") -Encoding utf8 -NoNewline
}
& (Join-Path $PSScriptRoot 'Export-SeasonalIcon.ps1')
Write-Output 'Reference geometry and exact sampled colors retained; only icon perimeter/silhouette outlined. Wordmark letters remain borderless.'
