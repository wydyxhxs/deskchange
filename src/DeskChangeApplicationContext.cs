using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DeskChange.Services;

namespace DeskChange
{
    internal sealed class DeskChangeApplicationContext : ApplicationContext
    {
        private const int HotkeyIdBase = 100;

        private readonly IVirtualDesktopSwitcher _desktopSwitcher;
        private readonly DesktopSwitchDispatcher _switchDispatcher;
        private readonly HotkeyWindow _hotkeyWindow;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly IStartupRegistration _startupRegistration;
        private readonly PortableSettingsStore _settingsStore;
        private readonly DeskChangeMainForm _mainForm;
        private readonly Icon _appIcon;
        private readonly bool _startHidden;
        private readonly Dictionary<int, int> _hotkeyDesktopMap;

        private AppSettings _appSettings;
        private bool _disposed;
        private int _lastKnownCurrentDesktopIndex = -1;
        private int _lastKnownDesktopCount = -1;

        public DeskChangeApplicationContext(bool startHidden)
        {
            try
            {
                string helperExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VirtualDesktopHelper.exe");

                _startHidden = startHidden;
                _hotkeyDesktopMap = new Dictionary<int, int>();
                _settingsStore = new PortableSettingsStore();
                _appSettings = _settingsStore.Load();
                _appSettings.Normalize();
                _desktopSwitcher = new VirtualDesktopCliSwitcher(helperExePath);
                _switchDispatcher = new DesktopSwitchDispatcher(_desktopSwitcher);
                _switchDispatcher.SwitchCompleted += OnSwitchCompleted;
                _startupRegistration = new RunKeyStartupRegistration("DeskChange");
                _appIcon = AppIconProvider.Load();
                _hotkeyWindow = new HotkeyWindow();
                _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;
                _mainForm = BuildMainForm(_appIcon);

                if (_startHidden)
                {
                    _mainForm.PrepareForHiddenLaunch();
                }

                MainForm = _mainForm;
                _trayMenu = BuildTrayMenu();
                _notifyIcon = BuildNotifyIcon(_trayMenu, _appIcon);
                _notifyIcon.MouseDoubleClick += OnNotifyIconMouseDoubleClick;
                RefreshDesktopState();

                string startupMessage = InitializeHotkeys();
                _mainForm.ApplyRuntimeState(
                    _appSettings,
                    _startupRegistration.IsEnabled(),
                    _lastKnownDesktopCount,
                    _lastKnownCurrentDesktopIndex);

                if (!string.IsNullOrWhiteSpace(startupMessage))
                {
                    _mainForm.SetInlineMessage(startupMessage, true);
                }
                else if (CountActiveHotkeys(_appSettings) == 0)
                {
                    _mainForm.SetInlineMessage("当前没有启用任何快捷键，配置后保存即可开始使用。", false);
                }

                if (_startHidden)
                {
                    AppLog.Info("DeskChange started in background mode.");
                }
                else
                {
                    ShowMainWindow();
                    AppLog.Info("DeskChange started.");
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("DeskChange failed to start.", ex);
                DisposeResources();
                throw;
            }
        }

        protected override void ExitThreadCore()
        {
            DisposeResources();
            base.ExitThreadCore();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeResources();
            }

            base.Dispose(disposing);
        }

        private static NotifyIcon BuildNotifyIcon(ContextMenuStrip trayMenu, Icon appIcon)
        {
            NotifyIcon notifyIcon = new NotifyIcon();
            notifyIcon.ContextMenuStrip = trayMenu;
            notifyIcon.Icon = appIcon;
            notifyIcon.Text = "DeskChange";
            notifyIcon.Visible = true;
            return notifyIcon;
        }

        private List<HotkeyRegistration> BuildHotkeyRegistrations(AppSettings settings)
        {
            List<HotkeyRegistration> registrations = new List<HotkeyRegistration>();
            int index;

            for (index = 0; index < settings.DesktopCount; index++)
            {
                HotkeyBinding binding = settings.GetHotkey(index);

                if (binding == null || binding.IsEmpty)
                {
                    continue;
                }

                registrations.Add(new HotkeyRegistration(HotkeyIdBase + index, index, binding));
            }

            return registrations;
        }

        private static string BuildHotkeyRegistrationMessage(HotkeyRegistration failedRegistration, int errorCode)
        {
            string displayText = failedRegistration == null
                ? "未知快捷键"
                : failedRegistration.Binding.ToDisplayString();
            string desktopText = failedRegistration == null
                ? "未识别"
                : (failedRegistration.DesktopIndex + 1).ToString();

            if (errorCode == 1409)
            {
                return string.Format(
                    "无法注册桌面 {0} 的快捷键 {1}。该组合键已被系统或其他程序占用。",
                    desktopText,
                    displayText);
            }

            return string.Format(
                "无法注册桌面 {0} 的快捷键 {1}。Win32 错误代码：{2}。",
                desktopText,
                displayText,
                errorCode);
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip();

            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += OnExitMenuItemClick;
            trayMenu.Items.Add(exitMenuItem);

            return trayMenu;
        }

        private string BuildSaveSuccessMessage(AppSettings settings, int systemDesktopCount)
        {
            int activeHotkeyCount = CountActiveHotkeys(settings);
            string hotkeyMessage = activeHotkeyCount == 0
                ? "当前没有启用任何快捷键。"
                : string.Format("已启用 {0} 个桌面快捷键。", activeHotkeyCount);
            string animationMessage = settings.EnableSwitchAnimation
                ? "切换动画已开启。"
                : "切换动画已关闭。";

            if (systemDesktopCount > 0 && settings.DesktopCount > systemDesktopCount)
            {
                return string.Format(
                    "已保存并应用。{0} {1} Windows 当前只有 {2} 个桌面，超出部分需先在系统中创建后才会生效。",
                    hotkeyMessage,
                    animationMessage,
                    systemDesktopCount);
            }

            return "已保存并应用。" + hotkeyMessage + " " + animationMessage;
        }

        private DeskChangeMainForm BuildMainForm(Icon appIcon)
        {
            DeskChangeMainForm mainForm = new DeskChangeMainForm(appIcon);
            mainForm.SaveRequested += OnSaveRequested;
            mainForm.CreateDesktopRequested += OnCreateDesktopRequested;
            mainForm.RemoveCurrentDesktopRequested += OnRemoveCurrentDesktopRequested;
            return mainForm;
        }

        private static int CountActiveHotkeys(AppSettings settings)
        {
            int count = 0;
            int index;

            for (index = 0; index < settings.DesktopCount; index++)
            {
                HotkeyBinding binding = settings.GetHotkey(index);

                if (binding != null && !binding.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }

        private void DisposeResources()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_hotkeyWindow != null)
            {
                _hotkeyWindow.HotkeyPressed -= OnHotkeyPressed;
                _hotkeyWindow.Dispose();
            }

            if (_switchDispatcher != null)
            {
                _switchDispatcher.SwitchCompleted -= OnSwitchCompleted;
                _switchDispatcher.Dispose();
            }

            if (_mainForm != null)
            {
                _mainForm.SaveRequested -= OnSaveRequested;
                _mainForm.CreateDesktopRequested -= OnCreateDesktopRequested;
                _mainForm.RemoveCurrentDesktopRequested -= OnRemoveCurrentDesktopRequested;
                _mainForm.PrepareForExit();
                _mainForm.Close();
                _mainForm.Dispose();
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.MouseDoubleClick -= OnNotifyIconMouseDoubleClick;
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
            }

            if (_appIcon != null)
            {
                _appIcon.Dispose();
            }
        }

        private string InitializeHotkeys()
        {
            string validationMessage;

            if (!ValidateSettings(_appSettings, out validationMessage))
            {
                AppLog.Info("Loaded settings contain invalid hotkey configuration: " + validationMessage);
                return validationMessage;
            }

            string hotkeyMessage;

            if (!TryApplyHotkeys(_appSettings, out hotkeyMessage))
            {
                AppLog.Info(hotkeyMessage);
                return hotkeyMessage;
            }

            return string.Empty;
        }

        private void OnCreateDesktopRequested(object sender, EventArgs e)
        {
            try
            {
                _desktopSwitcher.CreateDesktop();
                RefreshDesktopState();
                _mainForm.SetInlineMessage(
                    string.Format("已新建桌面。当前系统桌面数：{0}。", _lastKnownDesktopCount),
                    false);
                AppLog.Info("Created a new virtual desktop.");
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to create a virtual desktop.", ex);
                RefreshDesktopState();
                _mainForm.SetInlineMessage("新建桌面失败：" + ex.Message, true);
            }
        }

        private void OnExitMenuItemClick(object sender, EventArgs e)
        {
            AppLog.Info("DeskChange exiting.");
            ExitThread();
        }

        private void OnHotkeyPressed(object sender, HotkeyPressedEventArgs e)
        {
            int desktopIndex;

            if (_hotkeyDesktopMap.TryGetValue(e.HotkeyId, out desktopIndex))
            {
                AppLog.Info("Hotkey pressed for desktop " + (desktopIndex + 1) + ".");
                _switchDispatcher.Enqueue(desktopIndex, _appSettings.EnableSwitchAnimation);
            }
        }

        private void OnNotifyIconMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowMainWindow();
            }
        }

        private void OnRemoveCurrentDesktopRequested(object sender, EventArgs e)
        {
            if (_lastKnownDesktopCount <= 1 || _lastKnownCurrentDesktopIndex < 0)
            {
                RefreshDesktopState();
                _mainForm.SetInlineMessage("当前无法删除桌面。", true);
                return;
            }

            DialogResult result = MessageBox.Show(
                _mainForm,
                string.Format("确认删除当前桌面 {0} 吗？", _lastKnownCurrentDesktopIndex + 1),
                "删除当前桌面",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            int removedDesktopIndex = _lastKnownCurrentDesktopIndex;

            try
            {
                _desktopSwitcher.RemoveDesktop(removedDesktopIndex);
                RefreshDesktopState();
                _mainForm.SetInlineMessage(
                    string.Format("已删除桌面 {0}。当前系统桌面数：{1}。", removedDesktopIndex + 1, _lastKnownDesktopCount),
                    false);
                AppLog.Info("Removed virtual desktop " + (removedDesktopIndex + 1) + ".");
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to remove the current virtual desktop.", ex);
                RefreshDesktopState();
                _mainForm.SetInlineMessage("删除桌面失败：" + ex.Message, true);
            }
        }

        private void OnSaveRequested(object sender, SaveSettingsRequestedEventArgs e)
        {
            AppSettings proposedSettings = e.Settings == null
                ? AppSettings.CreateDefault()
                : e.Settings.Clone();
            proposedSettings.Normalize();

            string validationMessage;

            if (!ValidateSettings(proposedSettings, out validationMessage))
            {
                _mainForm.SetInlineMessage(validationMessage, true);
                return;
            }

            AppSettings previousSettings = _appSettings.Clone();
            bool previousStartupEnabled = _startupRegistration.IsEnabled();
            string hotkeyMessage;

            if (!TryApplyHotkeys(proposedSettings, out hotkeyMessage))
            {
                _mainForm.SetInlineMessage(hotkeyMessage, true);
                return;
            }

            try
            {
                ApplyStartupRegistration(e.StartupEnabled, proposedSettings.StartHiddenOnStartup);
                _settingsStore.Save(proposedSettings);
                _appSettings = proposedSettings.Clone();
                RefreshDesktopState();
                _mainForm.ApplyRuntimeState(
                    _appSettings,
                    e.StartupEnabled,
                    _lastKnownDesktopCount,
                    _lastKnownCurrentDesktopIndex);
                _mainForm.SetInlineMessage(BuildSaveSuccessMessage(_appSettings, _lastKnownDesktopCount), false);
                AppLog.Info("Settings saved and applied.");
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to save settings.", ex);
                RestoreRuntimeState(previousSettings, previousStartupEnabled);
                RefreshDesktopState();
                _mainForm.ApplyRuntimeState(
                    _appSettings,
                    previousStartupEnabled,
                    _lastKnownDesktopCount,
                    _lastKnownCurrentDesktopIndex);
                _mainForm.SetInlineMessage("保存失败：" + ex.Message, true);
            }
        }

        private void OnSwitchCompleted(object sender, DesktopSwitchCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                if (e.AvailableDesktopCount > 0)
                {
                    _lastKnownDesktopCount = e.AvailableDesktopCount;
                }

                _lastKnownCurrentDesktopIndex = e.DesktopIndex;
                RunOnUiThread(
                    delegate
                    {
                        _mainForm.UpdateDesktopRuntimeInfo(_lastKnownDesktopCount, _lastKnownCurrentDesktopIndex);
                    });
                return;
            }

            if (e.AvailableDesktopCount > 0)
            {
                _lastKnownDesktopCount = e.AvailableDesktopCount;
            }

            _lastKnownCurrentDesktopIndex = QueryCurrentDesktopIndex();
            AppLog.Info(e.Message);
            RunOnUiThread(
                delegate
                {
                    _mainForm.UpdateDesktopRuntimeInfo(_lastKnownDesktopCount, _lastKnownCurrentDesktopIndex);
                    _mainForm.SetInlineMessage(e.Message, true);

                    if (!_mainForm.Visible)
                    {
                        _notifyIcon.ShowBalloonTip(
                            2500,
                            "DeskChange",
                            e.Message,
                            ToolTipIcon.Warning);
                    }
                });
        }

        private int QuerySystemDesktopCount()
        {
            try
            {
                int desktopCount = _desktopSwitcher.GetDesktopCount();
                AppLog.Info("Detected desktop count: " + desktopCount);
                return desktopCount;
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to query desktop count.", ex);

                if (_lastKnownDesktopCount > 0)
                {
                    return _lastKnownDesktopCount;
                }

                return -1;
            }
        }

        private int QueryCurrentDesktopIndex()
        {
            try
            {
                int currentDesktopIndex = _desktopSwitcher.GetCurrentDesktopIndex();
                AppLog.Info("Detected current desktop index: " + currentDesktopIndex);
                return currentDesktopIndex;
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to query current desktop index.", ex);

                if (_lastKnownCurrentDesktopIndex >= 0)
                {
                    return _lastKnownCurrentDesktopIndex;
                }

                return -1;
            }
        }

        private void RefreshDesktopState()
        {
            _lastKnownDesktopCount = QuerySystemDesktopCount();
            _lastKnownCurrentDesktopIndex = QueryCurrentDesktopIndex();

            RunOnUiThread(
                delegate
                {
                    _mainForm.UpdateDesktopRuntimeInfo(_lastKnownDesktopCount, _lastKnownCurrentDesktopIndex);
                });
        }

        private void ReplaceHotkeyMap(IList<HotkeyRegistration> registrations)
        {
            _hotkeyDesktopMap.Clear();
            int index;

            for (index = 0; index < registrations.Count; index++)
            {
                HotkeyRegistration registration = registrations[index];

                if (registration != null)
                {
                    _hotkeyDesktopMap[registration.Id] = registration.DesktopIndex;
                }
            }
        }

        private void RestoreRuntimeState(AppSettings previousSettings, bool previousStartupEnabled)
        {
            _appSettings = previousSettings.Clone();

            try
            {
                ApplyStartupRegistration(previousStartupEnabled, _appSettings.StartHiddenOnStartup);
            }
            catch (Exception ex)
            {
                AppLog.Error("Failed to restore startup registration.", ex);
            }

            string hotkeyMessage;

            if (!TryApplyHotkeys(_appSettings, out hotkeyMessage))
            {
                AppLog.Info("Failed to restore hotkeys after save error: " + hotkeyMessage);
            }
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null || _mainForm == null || _mainForm.IsDisposed)
            {
                return;
            }

            try
            {
                if (_mainForm.InvokeRequired)
                {
                    _mainForm.BeginInvoke(action);
                    return;
                }

                action();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ApplyStartupRegistration(bool enabled, bool startHiddenOnStartup)
        {
            if (enabled)
            {
                _startupRegistration.Enable(Application.ExecutablePath, startHiddenOnStartup);
                return;
            }

            _startupRegistration.Disable();
        }

        private void ShowMainWindow()
        {
            RefreshDesktopState();
            _mainForm.ShowFromTray();
        }

        private bool TryApplyHotkeys(AppSettings settings, out string errorMessage)
        {
            List<HotkeyRegistration> registrations = BuildHotkeyRegistrations(settings);
            HotkeyRegistration failedRegistration;
            int errorCode;

            if (_hotkeyWindow.TryReplaceHotkeys(registrations, out failedRegistration, out errorCode))
            {
                ReplaceHotkeyMap(registrations);
                AppLog.Info("Applied hotkeys. Active count: " + registrations.Count);
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = BuildHotkeyRegistrationMessage(failedRegistration, errorCode);
            return false;
        }

        private bool ValidateSettings(AppSettings settings, out string errorMessage)
        {
            Dictionary<string, int> seenHotkeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int index;

            for (index = 0; index < settings.DesktopCount; index++)
            {
                HotkeyBinding binding = settings.GetHotkey(index);

                if (binding == null || binding.IsEmpty)
                {
                    continue;
                }

                string displayText = binding.ToDisplayString();
                int previousDesktopIndex;

                if (seenHotkeys.TryGetValue(displayText, out previousDesktopIndex))
                {
                    errorMessage = string.Format(
                        "桌面 {0} 与桌面 {1} 使用了相同快捷键 {2}，请调整后再保存。",
                        previousDesktopIndex + 1,
                        index + 1,
                        displayText);
                    return false;
                }

                seenHotkeys.Add(displayText, index);
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
