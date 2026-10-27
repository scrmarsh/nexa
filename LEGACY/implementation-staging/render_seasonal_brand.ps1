$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$taskAssets='M:\Nexa\assets'
function New-NexaPng([int]$taskSize) {
    $taskVisual=New-Object Windows.Media.DrawingVisual
    $taskDrawing=$taskVisual.RenderOpen()
    $taskDrawing.PushTransform((New-Object Windows.Media.ScaleTransform ($taskSize/512),($taskSize/512)))
    $taskClip=New-Object Windows.Media.RectangleGeometry (New-Object Windows.Rect 0,0,512,512),116,116
    $taskDrawing.PushClip($taskClip)
    $taskDrawing.DrawRectangle(([Windows.Media.BrushConverter]::new().ConvertFromString('#1C102A')),$null,(New-Object Windows.Rect 0,0,512,512))
    $taskDrawing.DrawGeometry(([Windows.Media.BrushConverter]::new().ConvertFromString('#A855F7')),$null,[Windows.Media.Geometry]::Parse('M512,0 L512,512 0,512 Z'))
    $taskDiagonal=New-Object Windows.Media.Pen ([Windows.Media.BrushConverter]::new().ConvertFromString('#A855F7')),38
    $taskDiagonal.StartLineCap=$taskDiagonal.EndLineCap=[Windows.Media.PenLineCap]::Round
    $taskDrawing.DrawLine($taskDiagonal,(New-Object Windows.Point 128,384),(New-Object Windows.Point 384,128))
    $taskPen=New-Object Windows.Media.Pen ([Windows.Media.Brushes]::White),38
    $taskPen.StartLineCap=$taskPen.EndLineCap=[Windows.Media.PenLineCap]::Round
    $taskPen.LineJoin=[Windows.Media.PenLineJoin]::Round
    $taskDrawing.DrawGeometry($null,$taskPen,[Windows.Media.Geometry]::Parse('M128,384 L128,128 384,384 384,128'))
    $taskDrawing.Pop();$taskDrawing.Pop();$taskDrawing.Close()
    $taskBitmap=New-Object Windows.Media.Imaging.RenderTargetBitmap $taskSize,$taskSize,96,96,([Windows.Media.PixelFormats]::Pbgra32)
    $taskBitmap.Render($taskVisual)
    $taskEncoder=New-Object Windows.Media.Imaging.PngBitmapEncoder
    $taskEncoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($taskBitmap))
    $taskStream=New-Object IO.MemoryStream
    $taskEncoder.Save($taskStream)
    return ,$taskStream.ToArray()
}
[IO.File]::WriteAllBytes((Join-Path $taskAssets 'nexa-icon-seasonal-256.png'),(New-NexaPng 256))
[IO.File]::WriteAllBytes((Join-Path $taskAssets 'nexa-icon-seasonal-1024.png'),(New-NexaPng 1024))
$taskSizes=@(16,24,32,48,64,128,256)
$taskImages=@();foreach($taskSize in $taskSizes){$taskImages+=,(New-NexaPng $taskSize)}
$taskIconStream=New-Object IO.MemoryStream
$taskWriter=New-Object IO.BinaryWriter $taskIconStream
$taskWriter.Write([uint16]0);$taskWriter.Write([uint16]1);$taskWriter.Write([uint16]$taskSizes.Count)
$taskOffset=6+16*$taskSizes.Count
for($taskIndex=0;$taskIndex -lt $taskSizes.Count;$taskIndex++){
    $taskDimension=if($taskSizes[$taskIndex] -eq 256){0}else{$taskSizes[$taskIndex]}
    $taskWriter.Write([byte]$taskDimension);$taskWriter.Write([byte]$taskDimension);$taskWriter.Write([byte]0);$taskWriter.Write([byte]0)
    $taskWriter.Write([uint16]1);$taskWriter.Write([uint16]32);$taskWriter.Write([uint32]$taskImages[$taskIndex].Length);$taskWriter.Write([uint32]$taskOffset)
    $taskOffset+=$taskImages[$taskIndex].Length
}
foreach($taskImage in $taskImages){$taskWriter.Write([byte[]]$taskImage)}
[IO.File]::WriteAllBytes((Join-Path $taskAssets 'nexa-icon-seasonal.ico'),$taskIconStream.ToArray())
$taskWriter.Dispose();$taskIconStream.Dispose()
Write-Output 'Updated vector logo, header/splash image, and seven-resolution executable icon.'
