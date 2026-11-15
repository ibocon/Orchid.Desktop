#!/usr/bin/env pwsh
#requires -Version 7.0

<#
.SYNOPSIS
    Orchid (BotanicSoft) 보태니컬 난초 브랜드 아이콘(ICO/PNG) 및 스토어 시각 자산을 생성한다.
.DESCRIPTION
    BotanicSoft/Peony/Garden 디자인 토큰(#F1E4CA 웜 크림 배경, #6D5443 브라운 잉크, #78D978 난초 잎,
    #EADDFF/#B69DF8/#9A82DB 화피, #6750A4 카틀레야 프릴 설판, #F6C667 화통, #FFE2E4 예주)으로
    1024 기준 식물학적 카틀레야/호접란(Cattleya Orchid) 베지어 심벌을 결정론적으로 렌더링한다.
#>

[CmdletBinding()]
param(
    [string] $DesktopRepoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path,
    [string] $MobileRepoRoot = 'D:\Source\BotanicSoft\Orchid.Mobile'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function Draw-OrchidSymbol {
    param(
        [System.Drawing.Graphics] $G,
        [float] $CenterX,
        [float] $CenterY,
        [float] $Size
    )

    $Scale = ($Size / 1024.0) * 0.88

    function New-CanvasPoint {
        param([double] $Px, [double] $Py)
        [System.Drawing.PointF]::new(
            [float]($CenterX + ($Px - 512.0) * $Scale),
            [float]($CenterY + ($Py - 512.0) * $Scale))
    }

    function New-OrchidPath {
        param(
            [double[]] $Coordinates,
            [switch] $Close
        )

        $Path = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $Path.StartFigure()
        for ($I = 0; $I -lt $Coordinates.Count; $I += 8) {
            $Path.AddBezier(
                (New-CanvasPoint $Coordinates[$I] $Coordinates[$I + 1]),
                (New-CanvasPoint $Coordinates[$I + 2] $Coordinates[$I + 3]),
                (New-CanvasPoint $Coordinates[$I + 4] $Coordinates[$I + 5]),
                (New-CanvasPoint $Coordinates[$I + 6] $Coordinates[$I + 7]))
        }
        if ($Close.IsPresent) {
            $Path.CloseFigure()
        }
        return $Path
    }

    $InkColor = [System.Drawing.Color]::FromArgb(255, 109, 84, 67)        # #6D5443
    $VeinColor = [System.Drawing.Color]::FromArgb(128, 109, 84, 67)       # #6D5443 (50% alpha)

    # 1) S자 난초 줄기 (Natural S-Curve Orchid Stem)
    $StemPath = New-OrchidPath -Coordinates @(
        506, 904, 488, 818, 494, 728, 512, 630)
    $StemPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.2, 26 * $Scale))
    $StemPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $StemPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $G.DrawPath($StemPen, $StemPath)
    $StemPen.Dispose(); $StemPath.Dispose()

    # 2) 피침형 난초 잎 (Arching Lanceolate Orchid Strap Leaves, #78D978)
    $LeafPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.0, 18 * $Scale))
    $LeafPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $LeafBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 120, 217, 120))
    foreach ($LeafCoords in @(
            @(506, 884, 414, 880, 286, 824, 210, 722, 210, 722, 316, 690, 438, 742, 508, 848),
            @(508, 884, 602, 880, 732, 820, 812, 714, 812, 714, 704, 684, 580, 738, 508, 848))) {
        $LeafPath = New-OrchidPath -Coordinates $LeafCoords -Close
        $G.FillPath($LeafBrush, $LeafPath)
        $G.DrawPath($LeafPen, $LeafPath)
        $LeafPath.Dispose()
    }
    $LeafPen.Dispose(); $LeafBrush.Dispose()

    $LeafVeinPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.0, 10 * $Scale))
    $LeafVeinPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $LeafVeinPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    foreach ($VeinCoords in @(
            @(488, 858, 416, 810, 336, 768, 258, 738),
            @(528, 858, 602, 808, 684, 764, 762, 732))) {
        $VeinPath = New-OrchidPath -Coordinates $VeinCoords
        $G.DrawPath($LeafVeinPen, $VeinPath)
        $VeinPath.Dispose()
    }
    $LeafVeinPen.Dispose()

    # 3) 3장의 꽃받침 (1 Dorsal Sepal + 2 Lower Lateral Sepals, #EADDFF)
    $SepalPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.1, 20 * $Scale))
    $SepalPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $SepalBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 234, 221, 255))
    foreach ($SepalCoords in @(
            @(472, 436, 438, 334, 440, 210, 486, 116,
              486, 116, 498, 92, 510, 82, 520, 96,
              520, 96, 566, 172, 582, 310, 552, 436),
            @(482, 460, 392, 492, 308, 558, 268, 638,
              268, 638, 254, 666, 276, 690, 312, 680,
              312, 680, 404, 654, 476, 574, 504, 490),
            @(542, 460, 632, 492, 716, 558, 756, 638,
              756, 638, 770, 666, 748, 690, 712, 680,
              712, 680, 620, 654, 548, 574, 520, 490))) {
        $SepalPath = New-OrchidPath -Coordinates $SepalCoords -Close
        $G.FillPath($SepalBrush, $SepalPath)
        $G.DrawPath($SepalPen, $SepalPath)
        $SepalPath.Dispose()
    }
    $SepalPen.Dispose(); $SepalBrush.Dispose()

    # 등꽃받침 중심 맥
    $PetalVeinPen = [System.Drawing.Pen]::new($VeinColor, [float][Math]::Max(1.0, 9 * $Scale))
    $PetalVeinPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $PetalVeinPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $TopVeinPath = New-OrchidPath -Coordinates @(512, 392, 510, 298, 510, 208, 514, 132)
    $G.DrawPath($PetalVeinPen, $TopVeinPath)
    $TopVeinPath.Dispose()

    # 4) 좌우 3단 프릴 카틀레야 날개 곁꽃잎 (Broad Ruffled Cattleya Wing Petals, #B69DF8 / #9A82DB)
    $PetalPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.1, 20 * $Scale))
    $PetalPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $PetalLeftBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 182, 157, 248))
    $PetalRightBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 154, 130, 219))
    foreach ($Petal in @(
            @{
                Coords = @(
                    482, 430, 420, 302, 304, 214, 204, 232,
                    204, 232, 154, 242, 136, 288, 154, 330,
                    154, 330, 120, 360, 118, 412, 154, 446,
                    154, 446, 146, 488, 182, 526, 238, 530,
                    238, 530, 330, 536, 420, 500, 488, 462)
                Brush  = $PetalLeftBrush
            },
            @{
                Coords = @(
                    542, 430, 604, 302, 720, 214, 820, 232,
                    820, 232, 870, 242, 888, 288, 870, 330,
                    870, 330, 904, 360, 906, 412, 870, 446,
                    870, 446, 878, 488, 842, 526, 786, 530,
                    786, 530, 694, 536, 604, 500, 536, 462)
                Brush  = $PetalRightBrush
            })) {
        $PetalPath = New-OrchidPath -Coordinates ($Petal.Coords) -Close
        $G.FillPath($Petal.Brush, $PetalPath)
        $G.DrawPath($PetalPen, $PetalPath)
        $PetalPath.Dispose()
    }
    $PetalPen.Dispose(); $PetalLeftBrush.Dispose(); $PetalRightBrush.Dispose()

    foreach ($WingVein in @(
            @(452, 422, 376, 350, 290, 300, 204, 282),
            @(444, 440, 354, 402, 266, 384, 180, 386),
            @(446, 456, 366, 456, 290, 466, 220, 482),
            @(572, 422, 648, 350, 734, 300, 820, 282),
            @(580, 440, 670, 402, 758, 384, 844, 386),
            @(578, 456, 658, 456, 734, 466, 804, 482))) {
        $WingVeinPath = New-OrchidPath -Coordinates $WingVein
        $G.DrawPath($PetalVeinPen, $WingVeinPath)
        $WingVeinPath.Dispose()
    }
    $PetalVeinPen.Dispose()

    # 5) 3열편 5단 프릴 카틀레야 설판 (Three-Lobed Frilled Cattleya Labellum, #6750A4)
    $LabellumPath = New-OrchidPath -Coordinates @(
        474, 414, 418, 398, 382, 442, 404, 496,
        404, 496, 416, 524, 430, 540, 416, 566,
        416, 566, 382, 604, 398, 658, 442, 668,
        442, 668, 458, 704, 496, 710, 512, 686,
        512, 686, 528, 710, 566, 704, 582, 668,
        582, 668, 626, 658, 642, 604, 608, 566,
        608, 566, 594, 540, 608, 524, 620, 496,
        620, 496, 642, 442, 606, 398, 550, 414) -Close
    $LabellumPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.2, 22 * $Scale))
    $LabellumPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $LabellumBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 103, 80, 164))
    $G.FillPath($LabellumBrush, $LabellumPath)
    $G.DrawPath($LabellumPen, $LabellumPath)
    $LabellumPen.Dispose(); $LabellumBrush.Dispose(); $LabellumPath.Dispose()

    # 6) 카틀레야 황금빛 화통 및 줄무늬 (Labellum Golden Throat & Striations, #F6C667)
    $ThroatPath = New-OrchidPath -Coordinates @(
        474, 448, 442, 468, 440, 524, 466, 566,
        466, 566, 484, 594, 498, 610, 512, 620,
        512, 620, 526, 610, 540, 594, 558, 566,
        558, 566, 584, 524, 582, 468, 550, 448) -Close
    $ThroatPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.0, 14 * $Scale))
    $ThroatPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $ThroatBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 246, 198, 103))
    $G.FillPath($ThroatBrush, $ThroatPath)
    $G.DrawPath($ThroatPen, $ThroatPath)
    $ThroatPen.Dispose(); $ThroatBrush.Dispose(); $ThroatPath.Dispose()

    $ThroatVeinPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.0, 10 * $Scale))
    $ThroatVeinPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $ThroatVeinPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $G.DrawLine($ThroatVeinPen, (New-CanvasPoint 512 474), (New-CanvasPoint 512 590))
    foreach ($SideThroatVein in @(
            @(494, 484, 482, 514, 470, 540, 460, 558),
            @(530, 484, 542, 514, 554, 540, 564, 558))) {
        $TvPath = New-OrchidPath -Coordinates $SideThroatVein
        $G.DrawPath($ThroatVeinPen, $TvPath)
        $TvPath.Dispose()
    }
    $ThroatVeinPen.Dispose()

    # 7) 중앙 예주 (Compact Arched Orchid Column Hood, #FFE2E4)
    $ColumnPath = New-OrchidPath -Coordinates @(
        478, 430, 478, 392, 546, 392, 546, 430,
        546, 430, 546, 452, 530, 464, 512, 464,
        512, 464, 494, 464, 478, 452, 478, 430) -Close
    $ColumnPen = [System.Drawing.Pen]::new($InkColor, [float][Math]::Max(1.0, 16 * $Scale))
    $ColumnPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $ColumnBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 255, 226, 228))
    $G.FillPath($ColumnBrush, $ColumnPath)
    $G.DrawPath($ColumnPen, $ColumnPath)
    $ColumnPen.Dispose(); $ColumnBrush.Dispose(); $ColumnPath.Dispose()
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

    # BotanicSoft / Peony 공통 크림 배경 (#F1E4CA)
    $Rect = [System.Drawing.Rectangle]::new(0, 0, $Width, $Height)
    $BgBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 241, 228, 202))
    $G.FillRectangle($BgBrush, $Rect)
    $BgBrush.Dispose()

    if ([string]::IsNullOrWhiteSpace($Title)) {
        $SymbolSize = [float]([Math]::Min($Width, $Height) * 0.92)
        Draw-OrchidSymbol -G $G -CenterX ($Width / 2.0) -CenterY ($Height / 2.0) -Size $SymbolSize
    }
    else {
        $IconSize = [float]([Math]::Min($Width, $Height) * 0.56)
        Draw-OrchidSymbol -G $G -CenterX ($Width * 0.19) -CenterY ($Height * 0.5) -Size $IconSize

        $TitleSize = [float]([Math]::Max(18.0, $Width * 0.032))
        $SubSize = [float]([Math]::Max(11.0, $Width * 0.0155))
        $TitleFont = [System.Drawing.Font]::new('Segoe UI', $TitleSize, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $SubFont = [System.Drawing.Font]::new('Segoe UI', $SubSize, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
        $TitleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 109, 84, 67))    # #6D5443
        $SubtitleBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 103, 80, 164)) # #6750A4

        $TextX = [float]($Width * 0.35)
        $TitleY = [float]($Height * 0.38)
        $G.DrawString($Title, $TitleFont, $TitleBrush, $TextX, $TitleY)
        if (-not [string]::IsNullOrWhiteSpace($Subtitle)) {
            $G.DrawString($Subtitle, $SubFont, $SubtitleBrush, $TextX, [float]($TitleY + $TitleSize * 1.45))
        }

        $TitleFont.Dispose(); $SubFont.Dispose(); $TitleBrush.Dispose(); $SubtitleBrush.Dispose()
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
    foreach ($SvgRelPath in @(
            'src\Resources\AppIcon\appicon.svg',
            'src\Resources\AppIcon\appiconfg.svg',
            'src\wwwroot\logo-login.svg')) {
        $SvgSrc = Join-Path -Path $DesktopRepoRoot -ChildPath $SvgRelPath
        if (Test-Path -LiteralPath $SvgSrc -PathType Leaf) {
            $SvgDst = Join-Path -Path $MobileRepoRoot -ChildPath $SvgRelPath
            $SvgDstDir = Split-Path -Path $SvgDst -Parent
            if (-not (Test-Path -LiteralPath $SvgDstDir)) {
                $null = New-Item -ItemType Directory -Path $SvgDstDir -Force
            }
            Copy-Item -LiteralPath $SvgSrc -Destination $SvgDst -Force
        }
    }

    $MobileCsproj = Join-Path -Path $MobileRepoRoot -ChildPath 'src\Orchid.Mobile.csproj'
    if (Test-Path -LiteralPath $MobileCsproj -PathType Leaf) {
        $CsprojText = [System.IO.File]::ReadAllText($MobileCsproj)
        if ($CsprojText.Contains('Color="#2E1065"')) {
            $CsprojText = $CsprojText.Replace('Color="#2E1065"', 'Color="#F1E4CA"')
            [System.IO.File]::WriteAllText($MobileCsproj, $CsprojText)
        }
    }

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
