#!/usr/bin/env pwsh
#requires -Version 7.0

<#
.SYNOPSIS
    Universal Clipboard Bridge (모두클립) 브랜드 아이콘(ICO/PNG) 및 스토어 시각 자산을 생성한다.
#>

[CmdletBinding()]
param(
    [string] $DesktopRepoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path,
    [string] $MobileRepoRoot = 'D:\Source\BotanicSoft\Orchid.Mobile'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function New-RoundedRectPath {
    param(
        [float] $X,
        [float] $Y,
        [float] $W,
        [float] $H,
        [float] $Radius
    )

    $Path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $D = [Math]::Max(2.0, $Radius * 2.0)
    $Path.AddArc($X, $Y, $D, $D, 180, 90)
    $Path.AddArc($X + $W - $D, $Y, $D, $D, 270, 90)
    $Path.AddArc($X + $W - $D, $Y + $H - $D, $D, $D, 0, 90)
    $Path.AddArc($X, $Y + $H - $D, $D, $D, 90, 90)
    $Path.CloseFigure()
    return $Path
}

function Draw-OrchidSymbol {
    param(
        [System.Drawing.Graphics] $G,
        [float] $CenterX,
        [float] $CenterY,
        [float] $Size
    )

    $Scale = $Size / 512.0

    # Back clipboard card (Desktop)
    $BackPath = New-RoundedRectPath `
        -X ($CenterX + (116 - 256) * $Scale) `
        -Y ($CenterY + (108 - 256) * $Scale) `
        -W (184 * $Scale) `
        -H (232 * $Scale) `
        -Radius ([Math]::Max(2.0, 28 * $Scale))
    $BackFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(72, 221, 214, 254))
    $BackPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(230, 233, 213, 255), [Math]::Max(1.5, 14 * $Scale))
    $G.FillPath($BackFill, $BackPath)
    $G.DrawPath($BackPen, $BackPath)
    $BackFill.Dispose(); $BackPen.Dispose(); $BackPath.Dispose()

    # Front clipboard card (Mobile/Bridge)
    $FrontPath = New-RoundedRectPath `
        -X ($CenterX + (212 - 256) * $Scale) `
        -Y ($CenterY + (168 - 256) * $Scale) `
        -W (184 * $Scale) `
        -H (236 * $Scale) `
        -Radius ([Math]::Max(2.0, 28 * $Scale))
    $FrontFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $G.FillPath($FrontFill, $FrontPath)
    $FrontFill.Dispose(); $FrontPath.Dispose()

    # Top clip tab
    $ClipPath = New-RoundedRectPath `
        -X ($CenterX + (264 - 256) * $Scale) `
        -Y ($CenterY + (148 - 256) * $Scale) `
        -W (80 * $Scale) `
        -H (36 * $Scale) `
        -Radius ([Math]::Max(1.5, 14 * $Scale))
    $ClipFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 196, 181, 253))
    $G.FillPath($ClipFill, $ClipPath)
    $ClipFill.Dispose(); $ClipPath.Dispose()

    # Content lines on front card
    $LinePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 91, 33, 182), [Math]::Max(1.5, 16 * $Scale))
    $LinePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $LinePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    foreach ($Row in @(
            @{ Y = 248; X2 = 356 },
            @{ Y = 292; X2 = 332 },
            @{ Y = 336; X2 = 312 })) {
        $G.DrawLine(
            $LinePen,
            [float]($CenterX + (252 - 256) * $Scale),
            [float]($CenterY + ($Row.Y - 256) * $Scale),
            [float]($CenterX + ($Row.X2 - 256) * $Scale),
            [float]($CenterY + ($Row.Y - 256) * $Scale))
    }
    $LinePen.Dispose()

    # E2EE badge circle
    $BadgeR = [Math]::Max(3.0, 44 * $Scale)
    $BadgeCx = [float]($CenterX + (196 - 256) * $Scale)
    $BadgeCy = [float]($CenterY + (286 - 256) * $Scale)
    $BadgeX = [float]($BadgeCx - $BadgeR)
    $BadgeY = [float]($BadgeCy - $BadgeR)
    $BadgeBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 109, 40, 217))
    $BadgeBorder = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.2, 10 * $Scale))
    $G.FillEllipse($BadgeBrush, $BadgeX, $BadgeY, $BadgeR * 2, $BadgeR * 2)
    $G.DrawEllipse($BadgeBorder, $BadgeX, $BadgeY, $BadgeR * 2, $BadgeR * 2)

    $CheckPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.5, 13 * $Scale))
    $CheckPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $CheckPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $CheckPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $CheckPoints = [System.Drawing.PointF[]]@(
        [System.Drawing.PointF]::new([float]($CenterX + (176 - 256) * $Scale), [float]($CenterY + (286 - 256) * $Scale)),
        [System.Drawing.PointF]::new([float]($CenterX + (191 - 256) * $Scale), [float]($CenterY + (301 - 256) * $Scale)),
        [System.Drawing.PointF]::new([float]($CenterX + (218 - 256) * $Scale), [float]($CenterY + (272 - 256) * $Scale))
    )
    $G.DrawLines($CheckPen, $CheckPoints)
    $CheckPen.Dispose(); $BadgeBrush.Dispose(); $BadgeBorder.Dispose()
}

function Save-BrandCanvasPng {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [int] $Width,
        [Parameter(Mandatory)] [int] $Height,
        [string] $Title = '',
        [string] $Subtitle = ''
    )

    $Dir = Split-Path -Path $Path -Parent
    if (-not (Test-Path -LiteralPath $Dir)) {
        $null = New-Item -ItemType Directory -Path $Dir -Force
    }

    $Bmp = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $G = [System.Drawing.Graphics]::FromImage($Bmp)
    $G.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $G.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $G.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $G.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit

    $Rect = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
    $BgBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $Rect,
        [System.Drawing.Color]::FromArgb(255, 30, 27, 75),
        [System.Drawing.Color]::FromArgb(255, 124, 58, 237),
        45.0)
    $G.FillRectangle($BgBrush, $Rect)
    $BgBrush.Dispose()

    if ([string]::IsNullOrWhiteSpace($Title)) {
        $SymbolSize = [float]([Math]::Min($Width, $Height) * 0.88)
        Draw-OrchidSymbol -G $G -CenterX ($Width / 2.0) -CenterY ($Height / 2.0) -Size $SymbolSize
    }
    else {
        $IconSize = [float]([Math]::Min($Width, $Height) * 0.48)
        Draw-OrchidSymbol -G $G -CenterX ($Width * 0.19) -CenterY ($Height * 0.5) -Size $IconSize

        $TitleSize = [float]([Math]::Max(18.0, $Width * 0.032))
        $SubSize = [float]([Math]::Max(11.0, $Width * 0.0155))
        $TitleFont = [System.Drawing.Font]::new('Segoe UI', $TitleSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $SubFont = [System.Drawing.Font]::new('Segoe UI', $SubSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $WhiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
        $MutedBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 221, 214, 254))

        $TextX = [float]($Width * 0.35)
        $TitleY = [float]($Height * 0.38)
        $G.DrawString($Title, $TitleFont, $WhiteBrush, $TextX, $TitleY)
        if (-not [string]::IsNullOrWhiteSpace($Subtitle)) {
            $G.DrawString($Subtitle, $SubFont, $MutedBrush, $TextX, [float]($TitleY + $TitleSize * 1.45))
        }

        $TitleFont.Dispose(); $SubFont.Dispose(); $WhiteBrush.Dispose(); $MutedBrush.Dispose()
    }

    $G.Dispose()
    $Bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $Bmp.Dispose()
}

function Save-PngAsIco {
    param(
        [Parameter(Mandatory)] [string] $PngPath,
        [Parameter(Mandatory)] [string] $IcoPath
    )

    $PngBytes = [System.IO.File]::ReadAllBytes($PngPath)
    $Dir = Split-Path -Path $IcoPath -Parent
    if (-not (Test-Path -LiteralPath $Dir)) {
        $null = New-Item -ItemType Directory -Path $Dir -Force
    }

    $Ms = [System.IO.MemoryStream]::new()
    $Bw = [System.IO.BinaryWriter]::new($Ms)
    # ICONDIR header (Reserved=0, Type=1, Count=1)
    $Bw.Write([uint16]0)
    $Bw.Write([uint16]1)
    $Bw.Write([uint16]1)
    # ICONDIRENTRY (256x256 stored as 0,0)
    $Bw.Write([byte]0)
    $Bw.Write([byte]0)
    $Bw.Write([byte]0)
    $Bw.Write([byte]0)
    $Bw.Write([uint16]1)
    $Bw.Write([uint16]32)
    $Bw.Write([uint32]$PngBytes.Length)
    $Bw.Write([uint32]22)
    $Bw.Write($PngBytes)
    $Bw.Flush()
    [System.IO.File]::WriteAllBytes($IcoPath, $Ms.ToArray())
    $Bw.Dispose()
    $Ms.Dispose()
}

$DesktopAssets = Join-Path -Path $DesktopRepoRoot -ChildPath 'src\Assets'
Save-BrandCanvasPng -Path (Join-Path $DesktopAssets 'StoreLogo.png') -Width 50 -Height 50
Save-BrandCanvasPng -Path (Join-Path $DesktopAssets 'Square44x44Logo.png') -Width 44 -Height 44
Save-BrandCanvasPng -Path (Join-Path $DesktopAssets 'Square150x150Logo.png') -Width 150 -Height 150
Save-BrandCanvasPng -Path (Join-Path $DesktopAssets 'Wide310x150Logo.png') -Width 310 -Height 150

$StoreLogo300 = Join-Path -Path $DesktopRepoRoot -ChildPath 'docs\release\windows-store\store-logo-300x300.png'
Save-BrandCanvasPng -Path $StoreLogo300 -Width 300 -Height 300

$DesktopScreenshot = Join-Path -Path $DesktopRepoRoot -ChildPath 'docs\release\windows-store\screenshots\desktop-main.png'
Save-BrandCanvasPng `
    -Path $DesktopScreenshot `
    -Width 1366 `
    -Height 768 `
    -Title 'Universal Clipboard Bridge' `
    -Subtitle 'AES-256-GCM End-to-End Encrypted Clipboard Sync Across PC, Mobile & Web'

$TempIcoPng = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'orchid-ico-256.png'
Save-BrandCanvasPng -Path $TempIcoPng -Width 256 -Height 256
Save-PngAsIco -PngPath $TempIcoPng -IcoPath (Join-Path -Path $DesktopRepoRoot -ChildPath 'src\Resources\AppIcon\appicon.ico')
Remove-Item -LiteralPath $TempIcoPng -Force -ErrorAction SilentlyContinue

if (Test-Path -LiteralPath $MobileRepoRoot) {
    $MobileStoreDir = Join-Path -Path $MobileRepoRoot -ChildPath 'docs\release\google-play\assets'
    Save-BrandCanvasPng -Path (Join-Path $MobileStoreDir 'icon-512x512.png') -Width 512 -Height 512
    Save-BrandCanvasPng `
        -Path (Join-Path $MobileStoreDir 'feature-graphic-1024x500.png') `
        -Width 1024 `
        -Height 500 `
        -Title 'Universal Clipboard Bridge' `
        -Subtitle 'Instant E2EE Clipboard Sync Between Windows PC and Android'
}

Write-Output '[orchid-brand-assets] generated all Desktop and Mobile visual assets.'
