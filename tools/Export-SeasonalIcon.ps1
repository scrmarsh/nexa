$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase
$repoRoot=Split-Path -Parent $PSScriptRoot
[xml]$svg=Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'assets\nexa-icon-seasonal.svg')
$frames=@()
foreach($size in @(64,128,256)) {
 $visual=[System.Windows.Media.DrawingVisual]::new()
 $dc=$visual.RenderOpen()
 $dc.PushTransform([System.Windows.Media.ScaleTransform]::new($size/512.0,$size/512.0))
 $dc.PushClip([System.Windows.Media.RectangleGeometry]::new([System.Windows.Rect]::new(0,0,512,512),116,116))
 foreach($element in $svg.DocumentElement.ChildNodes) {
  $geometry=$null
  switch($element.LocalName) {
   'rect' {$geometry=[System.Windows.Media.RectangleGeometry]::new([System.Windows.Rect]::new(0,0,512,512),116,116)}
   'polygon' {$geometry=[System.Windows.Media.Geometry]::Parse('M'+$element.points+' Z')}
   'line' {$geometry=[System.Windows.Media.LineGeometry]::new([System.Windows.Point]::new([double]$element.x1,[double]$element.y1),[System.Windows.Point]::new([double]$element.x2,[double]$element.y2))}
   'polyline' {$geometry=[System.Windows.Media.Geometry]::Parse('M'+$element.points)}
  }
  if($null -eq $geometry){continue}
  $fill=$null;$pen=$null
  if($element.fill -and $element.fill -ne 'none'){$fill=[System.Windows.Media.BrushConverter]::new().ConvertFromString($element.fill)}
  if($element.stroke){$pen=[System.Windows.Media.Pen]::new([System.Windows.Media.BrushConverter]::new().ConvertFromString($element.stroke),[double]$element.'stroke-width');$pen.StartLineCap='Round';$pen.EndLineCap='Round';$pen.LineJoin='Round'}
  $dc.DrawGeometry($fill,$pen,$geometry)
 }
 $dc.Pop();$dc.Pop();$dc.Close()
 $bitmap=[System.Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[System.Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual)
 $encoder=[System.Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $memory=[System.IO.MemoryStream]::new();$encoder.Save($memory);$bytes=$memory.ToArray();$memory.Dispose()
 $frames+=,@{Size=$size;Bytes=$bytes}
 if($size -eq 256){[System.IO.File]::WriteAllBytes((Join-Path $repoRoot 'assets\nexa-icon-seasonal-256.png'),$bytes)}
}
$stream=[System.IO.File]::Create((Join-Path $repoRoot 'assets\nexa-icon-seasonal.ico'))
$writer=[System.IO.BinaryWriter]::new($stream)
try {
 $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count);$offset=6+16*$frames.Count
 foreach($frame in $frames){$dimension=if($frame.Size -eq 256){0}else{$frame.Size};$writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0);$writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame.Bytes.Length);$writer.Write([uint32]$offset);$offset+=$frame.Bytes.Length}
 foreach($frame in $frames){$writer.Write([byte[]]$frame.Bytes)}
} finally {$writer.Dispose();$stream.Dispose()}
Write-Output 'Seasonal SVG, PNG and multi-size ICO synchronized.'
