param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class DeskChangeNativeIcon
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool DestroyIcon(IntPtr handle);
}
"@

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

$bitmap = New-Object System.Drawing.Bitmap 64, 64
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$backgroundRectangle = New-Object System.Drawing.Rectangle 0, 0, 63, 63
$backgroundBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $backgroundRectangle,
    [System.Drawing.Color]::FromArgb(18, 87, 101),
    [System.Drawing.Color]::FromArgb(43, 145, 167),
    45
)
$graphics.FillEllipse($backgroundBrush, 0, 0, 63, 63)

$outlinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(140, 232, 244, 248), 2)
$graphics.DrawEllipse($outlinePen, 1, 1, 61, 61)

$panelBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(248, 255, 255, 255))
$panelPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(170, 19, 88, 101), 1.5)
$graphics.FillRectangle($panelBrush, 12, 16, 16, 12)
$graphics.DrawRectangle($panelPen, 12, 16, 16, 12)
$graphics.FillRectangle($panelBrush, 36, 16, 16, 12)
$graphics.DrawRectangle($panelPen, 36, 16, 16, 12)

$dividerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 214, 235, 239), 2)
$graphics.DrawLine($dividerPen, 21, 32, 21, 37)
$graphics.DrawLine($dividerPen, 43, 32, 43, 37)

$arrowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 241, 188, 74))
$arrowPoints = [System.Drawing.Point[]]@(
    (New-Object System.Drawing.Point 16, 44),
    (New-Object System.Drawing.Point 34, 44),
    (New-Object System.Drawing.Point 34, 39),
    (New-Object System.Drawing.Point 50, 50),
    (New-Object System.Drawing.Point 34, 61),
    (New-Object System.Drawing.Point 34, 56),
    (New-Object System.Drawing.Point 16, 56)
)
$graphics.FillPolygon($arrowBrush, $arrowPoints)

$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(60, 0, 0, 0))
$graphics.FillEllipse($shadowBrush, 13, 48, 28, 8)

$iconHandle = $bitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($iconHandle)
$fileStream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create)
$icon.Save($fileStream)
$fileStream.Dispose()
$icon.Dispose()
[DeskChangeNativeIcon]::DestroyIcon($iconHandle) | Out-Null

$shadowBrush.Dispose()
$arrowBrush.Dispose()
$dividerPen.Dispose()
$panelPen.Dispose()
$panelBrush.Dispose()
$outlinePen.Dispose()
$backgroundBrush.Dispose()
$graphics.Dispose()
$bitmap.Dispose()
