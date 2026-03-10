Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$repoRoot = Split-Path -Parent $PSScriptRoot
$settingsPath = Join-Path $repoRoot 'artifacts\Release\DeskChange.settings.ini'
$backupPath = Join-Path $repoRoot 'artifacts\Release\DeskChange.settings.ini.bak'
$outputDirectory = Join-Path $repoRoot 'docs\screenshots'
$executablePath = Join-Path $repoRoot 'artifacts\Release\DeskChange.exe'

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

if (Test-Path $backupPath)
{
    Remove-Item $backupPath -Force
}

Copy-Item $settingsPath $backupPath -Force

@'
# DeskChange portable settings
desktop_count=2
enable_switch_animation=true
start_hidden_on_startup=true
desktop_1_hotkey=Ctrl+Alt+1
desktop_2_hotkey=Ctrl+Alt+2
desktop_3_hotkey=
desktop_4_hotkey=
'@ | Set-Content -Path $settingsPath -Encoding UTF8

$captureType = @"
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;

public static class ReadmeShot
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    public static void Capture(string processName, string fullPath, string desktopPath, string startupPath)
    {
        DateTime deadline = DateTime.Now.AddSeconds(15);
        System.Diagnostics.Process target = null;

        while (DateTime.Now < deadline)
        {
            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    target = process;
                    break;
                }
            }

            if (target != null)
            {
                break;
            }

            Thread.Sleep(250);
        }

        if (target == null)
        {
            throw new InvalidOperationException("DeskChange window was not found.");
        }

        ShowWindowAsync(target.MainWindowHandle, 5);
        SetForegroundWindow(target.MainWindowHandle);
        Thread.Sleep(800);

        RECT rect;
        if (!GetWindowRect(target.MainWindowHandle, out rect))
        {
            throw new InvalidOperationException("Unable to get DeskChange window bounds.");
        }

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        using (Bitmap bitmap = new Bitmap(width, height))
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(target.MainWindowHandle, hdc, 0))
                    {
                        graphics.ReleaseHdc(hdc);
                        hdc = IntPtr.Zero;
                        graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
                    }
                }
                finally
                {
                    if (hdc != IntPtr.Zero)
                    {
                        graphics.ReleaseHdc(hdc);
                    }
                }
            }

            bitmap.Save(fullPath, ImageFormat.Png);

            Rectangle desktopRect = new Rectangle(18, 96, Math.Min(width - 36, 724), Math.Min(height - 344, 320));
            using (Bitmap detail = bitmap.Clone(desktopRect, bitmap.PixelFormat))
            {
                detail.Save(desktopPath, ImageFormat.Png);
            }

            Rectangle startupRect = new Rectangle(18, Math.Max(0, height - 246), Math.Min(width - 36, 724), 130);
            using (Bitmap startup = bitmap.Clone(startupRect, bitmap.PixelFormat))
            {
                startup.Save(startupPath, ImageFormat.Png);
            }
        }
    }
}
"@

Add-Type -TypeDefinition $captureType -ReferencedAssemblies System.Drawing,System.Windows.Forms

$process = $null

try
{
    $process = Start-Process -FilePath $executablePath -PassThru
    [ReadmeShot]::Capture(
        'DeskChange',
        (Join-Path $outputDirectory 'main-window.png'),
        (Join-Path $outputDirectory 'desktop-card.png'),
        (Join-Path $outputDirectory 'startup-card.png'))
}
finally
{
    if ($process -and !$process.HasExited)
    {
        Stop-Process -Id $process.Id -Force
    }

    if (Test-Path $backupPath)
    {
        Move-Item $backupPath $settingsPath -Force
    }
}
