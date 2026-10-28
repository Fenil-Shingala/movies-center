Add-Type -AssemblyName System.Drawing

function Generate-Icon {
    param(
        [int]$Size,
        [string]$Path,
        [bool]$IsMaskable = $false
    )

    $bmp = [System.Drawing.Bitmap]::new($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Dark background
    $bgBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 14, 16, 24))
    $g.FillRectangle($bgBrush, 0, 0, $Size, $Size)

    $center = $Size / 2
    $radius = if ($IsMaskable) { $Size * 0.38 } else { $Size * 0.42 }

    # Outer Red Circle with gradient
    $pStart = [System.Drawing.PointF]::new(0, 0)
    $pEnd = [System.Drawing.PointF]::new($Size, $Size)
    $cRed1 = [System.Drawing.Color]::FromArgb(255, 235, 20, 35)
    $cRed2 = [System.Drawing.Color]::FromArgb(255, 178, 7, 16)
    $redBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new($pStart, $pEnd, $cRed1, $cRed2)
    $g.FillEllipse($redBrush, [float]($center - $radius), [float]($center - $radius), [float]($radius * 2), [float]($radius * 2))

    # Inner Dark Core
    $innerRadius = $radius * 0.65
    $innerBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 10, 11, 16))
    $g.FillEllipse($innerBrush, [float]($center - $innerRadius), [float]($center - $innerRadius), [float]($innerRadius * 2), [float]($innerRadius * 2))

    # Gold Accent Ring
    $goldPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(200, 251, 191, 36), [float]($Size * 0.015))
    $g.DrawEllipse($goldPen, [float]($center - $innerRadius), [float]($center - $innerRadius), [float]($innerRadius * 2), [float]($innerRadius * 2))

    # White Play Triangle
    $triWidth = $innerRadius * 0.9
    $p1 = [System.Drawing.PointF]::new([float]($center - $triWidth * 0.35), [float]($center - $triWidth * 0.55))
    $p2 = [System.Drawing.PointF]::new([float]($center + $triWidth * 0.55), [float]$center)
    $p3 = [System.Drawing.PointF]::new([float]($center - $triWidth * 0.35), [float]($center + $triWidth * 0.55))
    $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $points = [System.Drawing.PointF[]]@($p1, $p2, $p3)
    $g.FillPolygon($whiteBrush, $points)

    # 4 Film Sprockets
    $sprocketRadius = $radius * 0.09
    $sprocketBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
    $g.FillEllipse($sprocketBrush, [float]($center - $sprocketRadius), [float]($center - $radius * 0.85 - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
    $g.FillEllipse($sprocketBrush, [float]($center - $sprocketRadius), [float]($center + $radius * 0.85 - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
    $g.FillEllipse($sprocketBrush, [float]($center - $radius * 0.85 - $sprocketRadius), [float]($center - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))
    $g.FillEllipse($sprocketBrush, [float]($center + $radius * 0.85 - $sprocketRadius), [float]($center - $sprocketRadius), [float]($sprocketRadius * 2), [float]($sprocketRadius * 2))

    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Generated: $Path ($Size x $Size)"
}

$iconsDir = "d:\Angular\movies-center\frontend\public\icons"
if (-not (Test-Path $iconsDir)) {
    New-Item -ItemType Directory -Path $iconsDir -Force
}

Generate-Icon -Size 192 -Path "$iconsDir\icon-192x192.png"
Generate-Icon -Size 512 -Path "$iconsDir\icon-512x512.png"
Generate-Icon -Size 512 -Path "$iconsDir\maskable-icon-512x512.png" -IsMaskable $true
