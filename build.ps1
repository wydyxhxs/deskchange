param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputPath = Join-Path $projectRoot ("artifacts\" + $Configuration)
$iconPath = Join-Path $projectRoot "assets\DeskChange.ico"
$portableZipPath = Join-Path $projectRoot "artifacts\DeskChange-portable.zip"
$setupExePath = Join-Path $projectRoot "artifacts\DeskChange-Setup.exe"
$installerSourcePath = Join-Path $projectRoot "installer"

if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

& (Join-Path $projectRoot "tools\Generate-AppIcon.ps1") -OutputPath $iconPath

$compilerCandidates = @(
    (Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
    (Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe")
)

$compilerPath = $compilerCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $compilerPath) {
    throw "Unable to locate csc.exe."
}

$appSourceFiles = @(
    "Properties\AssemblyInfo.cs",
    "src\AppIconProvider.cs",
    "src\DeskChangeApplicationContext.cs",
    "src\DeskChangeMainForm.cs",
    "src\HotkeyBinding.cs",
    "src\HotkeyRegistration.cs",
    "src\HotkeyTextBox.cs",
    "src\HotkeyWindow.cs",
    "src\Program.cs",
    "src\Interop\NativeMethods.cs",
    "src\Services\DesktopSwitchDispatcher.cs",
    "src\Services\AppSettings.cs",
    "src\Services\AppLog.cs",
    "src\Services\IStartupRegistration.cs",
    "src\Services\IVirtualDesktopSwitcher.cs",
    "src\Services\PortableSettingsStore.cs",
    "src\Services\RunKeyStartupRegistration.cs",
    "src\Services\VirtualDesktopCliSwitcher.cs"
) | ForEach-Object { Join-Path $projectRoot $_ }

$commonCompilerArguments = @(
    "/nologo",
    "/platform:x64",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll"
)

if ($Configuration -eq "Debug") {
    $commonCompilerArguments += "/define:DEBUG;TRACE"
    $commonCompilerArguments += "/debug:full"
    $commonCompilerArguments += "/optimize-"
}
else {
    $commonCompilerArguments += "/define:TRACE"
    $commonCompilerArguments += "/debug:pdbonly"
    $commonCompilerArguments += "/optimize+"
}

$appCompilerArguments = @(
    "/target:winexe",
    ("/out:" + (Join-Path $outputPath "DeskChange.exe")),
    ("/win32icon:" + $iconPath)
)

$appCompilerArguments += $commonCompilerArguments
$appCompilerArguments += $appSourceFiles

& $compilerPath @appCompilerArguments

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$helperSourcePath = Join-Path $projectRoot "vendor\VirtualDesktop11-24H2.cs"
$helperOutputPath = Join-Path $outputPath "VirtualDesktopHelper.exe"
$helperLicensePath = Join-Path $projectRoot "vendor\LICENSE.MScholtes.txt"

if (-not (Test-Path $helperSourcePath)) {
    throw "Missing helper source file: $helperSourcePath"
}

& $compilerPath /nologo /target:exe /platform:x64 ("/out:" + $helperOutputPath) $helperSourcePath

if ($LASTEXITCODE -ne 0) {
    throw "Helper build failed with exit code $LASTEXITCODE."
}

if (Test-Path $helperLicensePath) {
    Copy-Item $helperLicensePath (Join-Path $outputPath "VirtualDesktopHelper.LICENSE.txt") -Force
}

if (Test-Path $portableZipPath) {
    Remove-Item $portableZipPath -Force
}

if (Test-Path $setupExePath) {
    Remove-Item $setupExePath -Force
}

Compress-Archive -Path @(
    (Join-Path $outputPath "DeskChange.exe"),
    (Join-Path $outputPath "VirtualDesktopHelper.exe"),
    (Join-Path $outputPath "VirtualDesktopHelper.LICENSE.txt")
) -DestinationPath $portableZipPath -Force

$setupSourceFiles = @(
    "setup\SetupProgram.cs",
    "setup\SetupWizardForm.cs"
) | ForEach-Object { Join-Path $projectRoot $_ }

$setupResourceArguments = @(
    ("/resource:" + (Join-Path $outputPath "DeskChange.exe") + ",DeskChange.Setup.Payload.DeskChange.exe"),
    ("/resource:" + (Join-Path $outputPath "VirtualDesktopHelper.exe") + ",DeskChange.Setup.Payload.VirtualDesktopHelper.exe"),
    ("/resource:" + (Join-Path $outputPath "VirtualDesktopHelper.LICENSE.txt") + ",DeskChange.Setup.Payload.VirtualDesktopHelper.LICENSE.txt"),
    ("/resource:" + (Join-Path $installerSourcePath "Uninstall-DeskChange.cmd") + ",DeskChange.Setup.Payload.Uninstall-DeskChange.cmd"),
    ("/resource:" + (Join-Path $installerSourcePath "Uninstall-DeskChange.ps1") + ",DeskChange.Setup.Payload.Uninstall-DeskChange.ps1")
)

$setupCompilerArguments = @(
    "/target:winexe",
    ("/out:" + $setupExePath),
    ("/win32icon:" + $iconPath)
)

$setupCompilerArguments += $commonCompilerArguments
$setupCompilerArguments += $setupResourceArguments
$setupCompilerArguments += $setupSourceFiles

& $compilerPath @setupCompilerArguments

if ($LASTEXITCODE -ne 0) {
    throw "Setup build failed with exit code $LASTEXITCODE."
}

Write-Host ("Built " + (Join-Path $outputPath "DeskChange.exe"))
Write-Host ("Built " + $portableZipPath)
Write-Host ("Built " + $setupExePath)
