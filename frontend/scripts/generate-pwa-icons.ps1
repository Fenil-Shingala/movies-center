Add-Type -AssemblyName System.Drawing

function Create-IconBitmap {
    param(
        [int]$Size,
        [bool]$IsMaskable = $false,
        [bool]$IsFaviconSmall = $false
    )

    $bmp = [System.Drawing.Bitmap]::new($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Dark background
    $bgBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 10, 11, 16))
    $g.FillRectangle($bgBrush, 0, 0, $Size, $Size)

    $center = $Size / 2
    $radius = if ($IsMaskable) { $Size * 0.38 } elseif ($IsFaviconSmall) { $Size * 0.46 } else { $Size * 0.43 }

    # Outer Red Circle with gradient
    $pStart = [System.Drawing.PointF]::new(0, 0)
    $pEnd = [System.Drawing.PointF]::new($Size, $Size)
    $cRed1 = [System.Drawing.Color]::FromArgb(255, 255, 42, 59)
    $cRed2 = [System.Drawing.Color]::FromArgb(255, 178, 7, 16)
    $redBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($pStart, $pEnd, $cRed1, $cRed2)
    $g.FillEllipse($redBrush, [float]($center - $radius), [float]($center - $radius), [float]($radius * 2), [float]($radius * 2))

    # Inner Dark Core
    $innerRadius = $radius * 0.64
    $innerBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 14, 16, 24))
    $g.FillEllipse($innerBrush, [float]($center - $innerRadius), [float]($center - $innerRadius), [float]($innerRadius * 2), [float]($innerRadius * 2))

    # Gold Accent Ring
    $ringThickness = [Math]::Max(1.0, [float]($Size * 0.02))
    $goldPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(230, 251, 191, 36), $ringThickness)
    $g.DrawEllipse($goldPen, [float]($center - $innerRadius), [float]($center - $innerRadius), [float]($innerRadius * 2), [float]($innerRadius * 2))

    # White Play Triangle
    $triWidth = $innerRadius * 0.95
    $p1 = [System.Drawing.PointF]::new([float]($center - $triWidth * 0.32), [float]($center - $triWidth * 0.52))
    $p2 = [System.Drawing.PointF]::new([float]($center + $triWidth * 0.58), [float]$center)
    $p3 = [System.Drawing.PointF]::new([float]($center - $triWidth * 0.32), [float]($center + $triWidth * 0.52))
    $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $points = [System.Drawing.PointF[]]@($p1, $p2, $p3)
    $g.FillPolygon($whiteBrush, $points)

    # 4 Film Sprockets (only on larger sizes >= 48px to prevent blurriness on 16/32px)
    if ($Size -ge 48) {
        $sprocketRadius = [Math]::Max(1.5, [float]($radius * 0.09))
        $sprocketBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(240, 255, 255, 255))
        $g.FillEllipse($sprocketBrush, [float]($center - $sprocketRadius), [float]($center - $radius * 0.85 - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
        $g.FillEllipse($sprocketBrush, [float]($center - $sprocketRadius), [float]($center + $radius * 0.85 - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
        $g.FillEllipse($sprocketBrush, [float]($center - $radius * 0.85 - $sprocketRadius), [float]($center - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
        $g.FillEllipse($sprocketBrush, [float]($center + $radius * 0.85 - $sprocketRadius), [float]($center - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
    }

    $g.Dispose()
    return $bmp
}

function Save-MultiSizeIco {
    param(
        [System.Drawing.Bitmap[]]$Bitmaps,
        [string]$IcoPath
    )
    $msList = [System.Collections.Generic.List[System.IO.MemoryStream]]::new()
    foreach ($b in $Bitmaps) {
        $ms = [System.IO.MemoryStream]::new()
        $b.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $msList.Add($ms)
    }

    $fs = [System.IO.FileStream]::new($IcoPath, [System.IO.FileMode]::Create)
    $bw = [System.IO.BinaryWriter]::new($fs)

    # ICONDIR header
    $bw.Write([uint16]0)
    $bw.Write([uint16]1) # Icon type
    $bw.Write([uint16]$Bitmaps.Length)

    $offset = 6 + (16 * $Bitmaps.Length)

    for ($i = 0; $i -lt $Bitmaps.Length; $i++) {
        $bmp = $Bitmaps[$i]
        $w = if ($bmp.Width -ge 256) { [byte]0 } else { [byte]$bmp.Width }
        $h = if ($bmp.Height -ge 256) { [byte]0 } else { [byte]$bmp.Height }
        $len = [uint32]$msList[$i].Length

        # ICONDIRENTRY
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0) # Colors
        $bw.Write([byte]0) # Reserved
        $bw.Write([uint16]1) # Color Planes
        $bw.Write([uint16]32) # BPP
        $bw.Write([uint32]$len)
        $bw.Write([uint32]$offset)

        $offset += $len
    }

    foreach ($ms in $msList) {
        $bytes = $ms.ToArray()
        $bw.Write($bytes)
        $ms.Dispose()
    }

    $bw.Close()
    $fs.Close()
    Write-Host "Multi-size ICO saved to $IcoPath"
}

function Generate-OgBanner {
    param([string]$Path)
    $w = 1200
    $h = 630
    $bmp = [System.Drawing.Bitmap]::new($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    # Background gradient
    $bgBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        [System.Drawing.PointF]::new(0, 0),
        [System.Drawing.PointF]::new($w, $h),
        [System.Drawing.Color]::FromArgb(255, 14, 17, 27),
        [System.Drawing.Color]::FromArgb(255, 8, 9, 13)
    )
    $g.FillRectangle($bgBrush, 0, 0, $w, $h)

    # Accent Red Glow in corner
    $glowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(40, 229, 9, 20))
    $g.FillEllipse($glowBrush, -100, -100, 500, 500)

    # Draw Brand Logo Icon on Left
    $iconSize = 220
    $iconBmp = Create-IconBitmap -Size $iconSize
    $g.DrawImage($iconBmp, 100, 205)
    $iconBmp.Dispose()

    # Draw Text
    $titleFont = [System.Drawing.Font]::new("Segoe UI", 48, [System.Drawing.FontStyle]::Bold)
    $subFont = [System.Drawing.Font]::new("Segoe UI", 26, [System.Drawing.FontStyle]::Regular)
    $tagFont = [System.Drawing.Font]::new("Segoe UI", 18, [System.Drawing.FontStyle]::Bold)

    # Tag pill
    $tagBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 229, 9, 20))
    $g.FillRectangle($tagBrush, 360, 180, 240, 36)
    $tagTextBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $g.DrawString("STREAMING PLATFORM", $tagFont, $tagTextBrush, 370, 184)

    # Main Title
    $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $redBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 42, 59))
    $goldBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 251, 191, 36))
    $mutedBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 160, 165, 185))

    $g.DrawString("MOVIES ", $titleFont, $whiteBrush, 355, 228)
    $g.DrawString("CENTER", $titleFont, $redBrush, 560, 228)

    # Tagline
    $g.DrawString("Watch Over 15,000+ Free HD Movies & TV Series", $subFont, $goldBrush, 360, 305)
    $g.DrawString("Fast Multi-Server Streaming • Instant Playback • Zero Ads", $subFont, $mutedBrush, 360, 355)

    # Domain Footer
    $domainFont = [System.Drawing.Font]::new("Segoe UI", 22, [System.Drawing.FontStyle]::Bold)
    $g.DrawString("https://moviescenter.site", $domainFont, $whiteBrush, 360, 425)

    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated OG Banner: $Path"
}

$publicDir = "d:\Angular\movies-center\frontend\public"
$iconsDir = "$publicDir\icons"
$imagesDir = "$publicDir\images"

if (-not (Test-Path $iconsDir)) { New-Item -ItemType Directory -Path $iconsDir -Force }
if (-not (Test-Path $imagesDir)) { New-Item -ItemType Directory -Path $imagesDir -Force }

# 1. Generate Favicon PNGs
$bmp16 = Create-IconBitmap -Size 16 -IsFaviconSmall $true
$bmp16.Save("$iconsDir\favicon-16x16.png", [System.Drawing.Imaging.ImageFormat]::Png)

$bmp32 = Create-IconBitmap -Size 32 -IsFaviconSmall $true
$bmp32.Save("$iconsDir\favicon-32x32.png", [System.Drawing.Imaging.ImageFormat]::Png)

$bmp48 = Create-IconBitmap -Size 48
$bmp48.Save("$iconsDir\favicon-48x48.png", [System.Drawing.Imaging.ImageFormat]::Png)

# 2. Generate Multi-size favicon.ico (16, 32, 48)
Save-MultiSizeIco -Bitmaps @($bmp16, $bmp32, $bmp48) -IcoPath "$publicDir\favicon.ico"

# 3. Generate Apple Touch Icon & PWA Icons
$bmp180 = Create-IconBitmap -Size 180
$bmp180.Save("$iconsDir\apple-touch-icon.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp180.Dispose()

$bmp192 = Create-IconBitmap -Size 192
$bmp192.Save("$iconsDir\icon-192x192.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp192.Dispose()

$bmp512 = Create-IconBitmap -Size 512
$bmp512.Save("$iconsDir\icon-512x512.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp512.Dispose()

$bmpMaskable = Create-IconBitmap -Size 512 -IsMaskable $true
$bmpMaskable.Save("$iconsDir\maskable-icon-512x512.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpMaskable.Dispose()

$bmp16.Dispose()
$bmp32.Dispose()
$bmp48.Dispose()

# 4. Generate OG Banner (1200x630)
Generate-OgBanner -Path "$imagesDir\og-banner.png"

Write-Host "All icons, favicon.ico, and SEO banners generated successfully!"
