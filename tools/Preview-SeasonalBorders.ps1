$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,PresentationFramework,WindowsBase
$repoRoot=Split-Path -Parent $PSScriptRoot
$outputRoot=Join-Path $repoRoot 'verification\border-options'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$panels=@()
foreach($option in @(@{Name='A-double';Width=12;Label='A · Double'},@{Name='B-quadruple';Width=24;Label='B · Quadruple'})){
 [xml]$svg=Get-Content -LiteralPath (Join-Path $repoRoot 'assets\nexa-icon-seasonal.svg') -Raw
 foreach($element in $svg.DocumentElement.ChildNodes){
  if($element.stroke -eq '#000000'){
   $element.SetAttribute('stroke-width',[string]$option.Width)
   if($element.LocalName -eq 'rect'){
    $inset=$option.Width/2
    foreach($entry in @{x=$inset;y=$inset;width=512-$option.Width;height=512-$option.Width;rx=116-$inset}.GetEnumerator()){$element.SetAttribute($entry.Key,[string]$entry.Value)}
   }
  }
 }
 $svg.Save((Join-Path $outputRoot ($option.Name+'.svg')))
 $visual=[System.Windows.Media.DrawingVisual]::new();$dc=$visual.RenderOpen()
 $dc.PushClip([System.Windows.Media.RectangleGeometry]::new([System.Windows.Rect]::new(0,0,512,512),116,116))
 foreach($element in $svg.DocumentElement.ChildNodes){
  $geometry=$null
  switch($element.LocalName){
   'rect' {$geometry=[System.Windows.Media.RectangleGeometry]::new([System.Windows.Rect]::new([double]$element.x,[double]$element.y,[double]$element.width,[double]$element.height),[double]$element.rx,[double]$element.rx)}
   'path' {$geometry=[System.Windows.Media.Geometry]::Parse($element.d)}
   'polygon' {$geometry=[System.Windows.Media.Geometry]::Parse('M'+$element.points+' Z')}
   'polyline' {$geometry=[System.Windows.Media.Geometry]::Parse('M'+$element.points)}
   'line' {$geometry=[System.Windows.Media.LineGeometry]::new([System.Windows.Point]::new([double]$element.x1,[double]$element.y1),[System.Windows.Point]::new([double]$element.x2,[double]$element.y2))}
  }
  if(-not $geometry){continue}
  $fill=$null;$pen=$null
  if($element.fill -and $element.fill -ne 'none'){$fill=[System.Windows.Media.BrushConverter]::new().ConvertFromString($element.fill)}
  if($element.stroke){$pen=[System.Windows.Media.Pen]::new([System.Windows.Media.BrushConverter]::new().ConvertFromString($element.stroke),[double]$element.'stroke-width');$pen.StartLineCap='Round';$pen.EndLineCap='Round';$pen.LineJoin='Round'}
  $dc.DrawGeometry($fill,$pen,$geometry)
 }
 $dc.Pop();$dc.Close()
 $bitmap=[System.Windows.Media.Imaging.RenderTargetBitmap]::new(512,512,96,96,[System.Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($visual);$bitmap.Freeze()
 $encoder=[System.Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
 $stream=[System.IO.File]::Create((Join-Path $outputRoot ($option.Name+'.png')));try{$encoder.Save($stream)}finally{$stream.Dispose()}
 $panels+=,@{Image=$bitmap;Label=$option.Label}
}
$comparison=[System.Windows.Media.DrawingVisual]::new();$dc=$comparison.RenderOpen()
$dc.DrawRectangle([System.Windows.Media.BrushConverter]::new().ConvertFromString('#1C102A'),$null,[System.Windows.Rect]::new(0,0,1120,640))
for($index=0;$index -lt $panels.Count;$index++){
 $left=32+$index*560
 $caption=[System.Windows.Media.FormattedText]::new($panels[$index].Label,[System.Globalization.CultureInfo]::InvariantCulture,[System.Windows.FlowDirection]::LeftToRight,[System.Windows.Media.Typeface]::new('Segoe UI Semibold'),26,[System.Windows.Media.Brushes]::White,1)
 $dc.DrawText($caption,[System.Windows.Point]::new($left,22))
 $dc.DrawImage($panels[$index].Image,[System.Windows.Rect]::new($left,80,496,496))
}
$dc.Close()
$bitmap=[System.Windows.Media.Imaging.RenderTargetBitmap]::new(1120,640,96,96,[System.Windows.Media.PixelFormats]::Pbgra32);$bitmap.Render($comparison)
$encoder=[System.Windows.Media.Imaging.PngBitmapEncoder]::new();$encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
$stream=[System.IO.File]::Create((Join-Path $outputRoot 'comparison.png'));try{$encoder.Save($stream)}finally{$stream.Dispose()}
Write-Output (Join-Path $outputRoot 'comparison.png')
