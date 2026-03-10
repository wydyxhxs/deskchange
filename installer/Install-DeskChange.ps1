$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$productName = "DeskChange"
$productVersion = "1.0.0"
$installRoot = Join-Path $env:LOCALAPPDATA "Programs\DeskChange"
$startMenuRoot = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$startMenuFolder = Join-Path $startMenuRoot "DeskChange"
$uninstallRegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskChange"
$sourceFiles = @(
    "DeskChange.exe",
    "VirtualDesktopHelper.exe",
    "VirtualDesktopHelper.LICENSE.txt",
    "Uninstall-DeskChange.cmd",
    "Uninstall-DeskChange.ps1"
)

function Get-InstalledExecutablePath {
    return Join-Path $installRoot "DeskChange.exe"
}

function New-Shortcut {
    param(
        [string]$ShortcutPath,
        [string]$TargetPath,
        [string]$WorkingDirectory,
        [string]$Description,
        [string]$IconLocation
    )

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description

    if (-not [string]::IsNullOrWhiteSpace($IconLocation)) {
        $shortcut.IconLocation = $IconLocation
    }

    $shortcut.Save()
}

function Register-UninstallEntry {
    $installedExePath = Get-InstalledExecutablePath
    $uninstallScriptPath = Join-Path $installRoot "Uninstall-DeskChange.ps1"
    $estimatedSizeKb = [Math]::Ceiling(((Get-ChildItem -Path $installRoot -File | Measure-Object -Property Length -Sum).Sum) / 1KB)
    $uninstallCommand = 'powershell.exe -NoProfile -ExecutionPolicy Bypass -File "' + $uninstallScriptPath + '"'

    New-Item -Path $uninstallRegistryPath -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "DisplayName" -Value $productName -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "DisplayVersion" -Value $productVersion -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "Publisher" -Value $productName -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "DisplayIcon" -Value $installedExePath -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "InstallLocation" -Value $installRoot -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "InstallDate" -Value (Get-Date -Format "yyyyMMdd") -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "UninstallString" -Value $uninstallCommand -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "QuietUninstallString" -Value $uninstallCommand -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "EstimatedSize" -Value ([int]$estimatedSizeKb) -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "NoModify" -Value 1 -PropertyType DWord -Force | Out-Null
    New-ItemProperty -Path $uninstallRegistryPath -Name "NoRepair" -Value 1 -PropertyType DWord -Force | Out-Null
}

function Show-InstallError {
    param([Exception]$Exception)

    [System.Windows.Forms.MessageBox]::Show(
        "DeskChange install failed.`r`n`r`n" + $Exception.Message,
        $productName,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
}

function Stop-InstalledProcess {
    $installedExePath = Get-InstalledExecutablePath

    Get-Process -Name "DeskChange" -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            if ($_.Path -eq $installedExePath) {
                Stop-Process -Id $_.Id -Force
            }
        }
        catch {
        }
    }
}

try {
    Stop-InstalledProcess

    New-Item -Path $installRoot -ItemType Directory -Force | Out-Null
    New-Item -Path $startMenuFolder -ItemType Directory -Force | Out-Null

    foreach ($fileName in $sourceFiles) {
        Copy-Item -Path (Join-Path $PSScriptRoot $fileName) -Destination (Join-Path $installRoot $fileName) -Force
    }

    $installedExePath = Get-InstalledExecutablePath
    $uninstallCmdPath = Join-Path $installRoot "Uninstall-DeskChange.cmd"
    New-Shortcut -ShortcutPath (Join-Path $startMenuFolder "DeskChange.lnk") -TargetPath $installedExePath -WorkingDirectory $installRoot -Description "DeskChange" -IconLocation ($installedExePath + ",0")
    New-Shortcut -ShortcutPath (Join-Path $startMenuFolder "Uninstall DeskChange.lnk") -TargetPath $uninstallCmdPath -WorkingDirectory $installRoot -Description "Uninstall DeskChange" -IconLocation "shell32.dll,131"

    Register-UninstallEntry
    Start-Process -FilePath $installedExePath -WorkingDirectory $installRoot
    exit 0
}
catch {
    Show-InstallError $_.Exception
    exit 1
}
