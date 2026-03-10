$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms

$productName = "DeskChange"
$installRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$installedExePath = Join-Path $installRoot "DeskChange.exe"
$startMenuFolder = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\DeskChange"
$desktopShortcutPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "DeskChange.lnk"
$runRegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$uninstallRegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskChange"

function Remove-StartupEntry {
    $startupValue = (Get-ItemProperty -Path $runRegistryPath -Name "DeskChange" -ErrorAction SilentlyContinue)."DeskChange"

    if (-not [string]::IsNullOrWhiteSpace($startupValue) -and $startupValue.IndexOf($installedExePath, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Remove-ItemProperty -Path $runRegistryPath -Name "DeskChange" -ErrorAction SilentlyContinue
    }
}

function Show-UninstallError {
    param([Exception]$Exception)

    [System.Windows.Forms.MessageBox]::Show(
        "DeskChange uninstall failed.`r`n`r`n" + $Exception.Message,
        $productName,
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
}

function Stop-InstalledProcess {
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
    Remove-StartupEntry
    Remove-Item -Path $startMenuFolder -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $desktopShortcutPath -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $uninstallRegistryPath -Recurse -Force -ErrorAction SilentlyContinue

    $cleanupScriptPath = Join-Path $env:TEMP ("DeskChange-Cleanup-" + [Guid]::NewGuid().ToString("N") + ".cmd")
    $cleanupScript = @"
@echo off
ping 127.0.0.1 -n 3 >nul
rmdir /s /q "$installRoot"
del /f /q "%~f0"
"@

    Set-Content -Path $cleanupScriptPath -Value $cleanupScript -Encoding Ascii
    Start-Process -FilePath $env:ComSpec -ArgumentList "/c", "`"$cleanupScriptPath`"" -WindowStyle Hidden
    exit 0
}
catch {
    Show-UninstallError $_.Exception
    exit 1
}
