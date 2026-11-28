# Run with Windows PowerShell 5.1: Windows OCR's WinRT projection is not available in PowerShell 7.
param([string]$Output = 'docs\gates\r2-board')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
Add-Type -AssemblyName System.Drawing
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
$null = [Windows.Storage.Streams.IRandomAccessStream, Windows.Storage.Streams, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
$null = [Windows.Graphics.Imaging.SoftwareBitmap, Windows.Graphics.Imaging, ContentType = WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Media.Ocr.OcrResult, Windows.Foundation, ContentType = WindowsRuntime]
$null = [Windows.Globalization.Language, Windows.Globalization, ContentType = WindowsRuntime]
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
  $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and
  $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
} | Select-Object -First 1
function Await-WinRt($Operation, [Type]$ResultType) {
  $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
  $task.Wait()
  $task.Result
}
function Read-Ocr([string]$Path) {
  $file = Await-WinRt ([Windows.Storage.StorageFile]::GetFileFromPathAsync($Path)) ([Windows.Storage.StorageFile])
  $stream = Await-WinRt ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
  $bitmap = $null
  try {
    $decoder = Await-WinRt ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
    $bitmap = Await-WinRt ($decoder.GetSoftwareBitmapAsync(
      [Windows.Graphics.Imaging.BitmapPixelFormat]::Bgra8, [Windows.Graphics.Imaging.BitmapAlphaMode]::Ignore)) ([Windows.Graphics.Imaging.SoftwareBitmap])
    $result = Await-WinRt ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
    $report = @{
      text = $result.Text
      words = @($result.Lines | ForEach-Object { $_.Words } | ForEach-Object {
        @{ text = $_.Text; x = $_.BoundingRect.X; y = $_.BoundingRect.Y; width = $_.BoundingRect.Width; height = $_.BoundingRect.Height }
      })
    }
  }
  finally {
    if ($bitmap) { $bitmap.Dispose() }
    $stream.Dispose()
  }
}
function Resize-Image([string]$Source, [string]$Destination, [int]$Width, [int]$Height) {
  $image = [Drawing.Image]::FromFile($Source)
  $bitmap = New-Object Drawing.Bitmap $Width, $Height
  $graphics = [Drawing.Graphics]::FromImage($bitmap)
  try {
    $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.DrawImage($image, 0, 0, $Width, $Height)
    $bitmap.Save($Destination, [Drawing.Imaging.ImageFormat]::Png)
  }
  finally { $graphics.Dispose(); $bitmap.Dispose(); $image.Dispose() }
}
function Normalise([string]$Text) { ($Text.ToLowerInvariant() -replace '[^\p{L}\p{N}\s]', '' -replace '\s+', ' ').Trim() }
function Measure-GlyphInk([string]$Path) {
  $pixels = [Drawing.Bitmap]::FromFile($Path)
  try {
    $rows = @(for ($y = 0; $y -lt $pixels.Height; $y++) {
      for ($x = 0; $x -lt $pixels.Width; $x++) {
        if ($pixels.GetPixel($x, $y).A -ge 128) { $y; break }
      }
    })
    if ($rows.Count) { return ($rows | Measure-Object -Maximum).Maximum - ($rows | Measure-Object -Minimum).Minimum + 1 }
    return 0
  } finally { $pixels.Dispose() }
}
$root = Split-Path $PSScriptRoot -Parent
$out = [IO.Path]::GetFullPath((Join-Path $root $Output))
$checksPath = Join-Path $root 'docs\gates\r2-board-fixtures.json'
$checks = Get-Content $checksPath -Raw | ConvertFrom-Json
$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage((New-Object Windows.Globalization.Language 'en-US'))
if (-not $engine) { throw 'Windows en-US OCR is unavailable; no success-shaped substitute is permitted.' }
$assets = Join-Path $root 'src\tests\Zava.Scrum.MediaBot.Tests\BoardGoldens'
$reports = @()
$sourceInk = Measure-GlyphInk (Join-Path $out 'source\minimum-glyph.png')
$glyphs = @(@{ resolution = 720; inkHeight = $sourceInk; minimum = 12; pass = $sourceInk -ge 12 })
foreach ($size in @(@{ width = 1920; height = 1080; glyphMinimum = 18 }, @{ width = 854; height = 480; glyphMinimum = 8 })) {
  $sheet = New-Object Drawing.Bitmap ($size.width * 2), (($size.height + 48) * $checks.Count)
  $graphics = [Drawing.Graphics]::FromImage($sheet)
  $font = New-Object Drawing.Font 'Segoe UI', 24
  try {
    $graphics.Clear([Drawing.Color]::FromArgb(15, 23, 42))
    $index = 0
    foreach ($check in $checks) {
      foreach ($adapter in @('chromium', 'native')) {
        $source = if ($adapter -eq 'chromium') {
          Join-Path $assets "chromium\$($check.sourceFixture).png"
        } else { Join-Path $out "source\$($check.name).png" }
        $target = Join-Path $out "$adapter-$($check.name)-$($size.height).png"
        Resize-Image $source $target $size.width $size.height
        $ocr = Read-Ocr $target
        $normal = Normalise $ocr.text
        # Chromium's completion truthfully promises saved follow-ups; native is explicitly non-recording.
        $expected = @($check.expected | ForEach-Object {
          if ($adapter -eq 'chromium' -and $_ -eq 'Meeting-derived data is not saved') { 'Follow-ups are in Teams for approval' }
          elseif ($adapter -eq 'chromium' -and $_ -eq 'Current item') { } else { $_ }
        })
        $missing = @($expected | Where-Object { -not $normal.Contains((Normalise $_)) })
        $reports += @{
          adapter = $adapter; state = $check.name; width = $size.width; height = $size.height
          expected = $expected; missing = $missing; recognised = $expected.Count - $missing.Count
          total = $expected.Count; pass = $missing.Count -eq 0; ocr = $ocr
          sourceSha256 = (Get-FileHash $source -Algorithm SHA256).Hash
          imageSha256 = (Get-FileHash $target -Algorithm SHA256).Hash
        }
        $image = [Drawing.Image]::FromFile($target)
        try {
          $x = if ($adapter -eq 'chromium') { 0 } else { $size.width }
          $y = $index * ($size.height + 48)
          $graphics.DrawString("$adapter / $($check.name) / $($size.height)p", $font, [Drawing.Brushes]::White, $x + 16, $y)
          $graphics.DrawImage($image, $x, $y + 48, $size.width, $size.height)
        } finally { $image.Dispose() }
      }
      $index++
    }
    $sheet.Save((Join-Path $out "contact-$($size.height).png"), [Drawing.Imaging.ImageFormat]::Png)
  } finally { $graphics.Dispose(); $font.Dispose(); $sheet.Dispose() }
  $glyph = Join-Path $out "glyph-$($size.height).png"
  $dimension = [int][Math]::Round(40 * $size.height / 720)
  Resize-Image (Join-Path $out 'source\minimum-glyph.png') $glyph $dimension $dimension
  $height = Measure-GlyphInk $glyph
  $glyphs += @{ resolution = $size.height; inkHeight = $height; minimum = $size.glyphMinimum; pass = $height -ge $size.glyphMinimum }
}
$nativePass = @($reports | Where-Object { $_.adapter -eq 'native' -and -not $_.pass }).Count -eq 0 -and
  @($glyphs | Where-Object { -not $_.pass }).Count -eq 0
@{
  measuredUtc = [DateTimeOffset]::UtcNow.ToString('o')
  recogniser = 'Windows.Media.Ocr en-US'; resampling = 'System.Drawing HighQualityBicubic'
  frozenChecksSha256 = (Get-FileHash (Join-Path $root 'docs\gates\r2-board-checks.md') -Algorithm SHA256).Hash
  frozenFixturesSha256 = (Get-FileHash $checksPath -Algorithm SHA256).Hash
  frames = $reports; minimumGlyph = $glyphs
  nativePass = $nativePass
} | ConvertTo-Json -Depth 12
[IO.File]::WriteAllText((Join-Path $out 'ocr.json'), ($report -replace "`r`n", "`n") + "`n")
$reports | ForEach-Object { '{0}/{1}/{2}p: {3}/{4}; missing: {5}' -f $_.adapter, $_.state, $_.height, $_.recognised, $_.total, ($_.missing -join ', ') }
$glyphs | ConvertTo-Json -Compress
Write-Output "NATIVE_OCR_PASS=$nativePass"
if (-not $nativePass) { exit 1 }
