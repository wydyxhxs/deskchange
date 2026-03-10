using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DeskChange.Setup
{
    internal sealed class SetupWizardForm : Form
    {
        private static readonly Color WindowBackgroundColor = Color.FromArgb(243, 246, 248);
        private static readonly Color CardBackgroundColor = Color.FromArgb(255, 255, 255);
        private static readonly Color AccentColor = Color.FromArgb(19, 110, 120);
        private static readonly Color AccentSoftColor = Color.FromArgb(224, 242, 244);
        private static readonly Color BorderColor = Color.FromArgb(219, 227, 231);
        private static readonly Color TitleColor = Color.FromArgb(29, 37, 43);
        private static readonly Color BodyColor = Color.FromArgb(93, 103, 112);
        private static readonly Color ErrorColor = Color.FromArgb(183, 69, 50);

        private readonly Panel[] _pages;
        private readonly Button _backButton;
        private readonly Button _nextButton;
        private readonly Button _cancelButton;
        private readonly TextBox _installPathTextBox;
        private readonly CheckBox _desktopShortcutCheckBox;
        private readonly CheckBox _launchAfterInstallCheckBox;
        private readonly ProgressBar _progressBar;
        private readonly Label _progressTitleLabel;
        private readonly Label _progressDetailLabel;

        private int _pageIndex;
        private bool _installing;
        private bool _installSucceeded;
        private bool _installFailed;
        private string _installedPath;

        public SetupWizardForm()
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = WindowBackgroundColor;
            ClientSize = new Size(740, 500);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DeskChange 安装向导";

            Panel headerPanel = BuildHeader();
            Panel contentPanel = new Panel();
            contentPanel.BackColor = CardBackgroundColor;
            contentPanel.Location = new Point(16, 104);
            contentPanel.Size = new Size(708, 330);
            contentPanel.Paint += DrawBorder;

            _pages = new[]
            {
                BuildWelcomePage(),
                BuildOptionsPage(out _installPathTextBox, out _desktopShortcutCheckBox),
                BuildProgressPage(out _progressBar, out _progressTitleLabel, out _progressDetailLabel, out _launchAfterInstallCheckBox)
            };

            int index;

            for (index = 0; index < _pages.Length; index++)
            {
                _pages[index].Visible = index == 0;
                contentPanel.Controls.Add(_pages[index]);
            }

            Panel footerPanel = new Panel();
            footerPanel.BackColor = CardBackgroundColor;
            footerPanel.Location = new Point(16, 442);
            footerPanel.Size = new Size(708, 42);
            footerPanel.Paint += DrawBorder;

            _backButton = CreateGhostButton("< 上一步");
            _backButton.Location = new Point(434, 6);
            _backButton.Click += OnBackButtonClick;
            footerPanel.Controls.Add(_backButton);

            _nextButton = CreatePrimaryButton("下一步 >");
            _nextButton.Location = new Point(544, 6);
            _nextButton.Click += OnNextButtonClick;
            footerPanel.Controls.Add(_nextButton);

            _cancelButton = CreateGhostButton("取消");
            _cancelButton.Location = new Point(330, 6);
            _cancelButton.Click += OnCancelButtonClick;
            footerPanel.Controls.Add(_cancelButton);

            Controls.Add(headerPanel);
            Controls.Add(contentPanel);
            Controls.Add(footerPanel);

            _installPathTextBox.Text = BuildDefaultInstallPath();
            UpdateNavigation();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_installing)
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        private static string BuildDefaultInstallPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "DeskChange");
        }

        private Panel BuildHeader()
        {
            Panel panel = new Panel();
            panel.BackColor = CardBackgroundColor;
            panel.Location = new Point(16, 16);
            panel.Size = new Size(708, 80);
            panel.Paint += DrawBorder;

            Panel badge = new Panel();
            badge.BackColor = AccentSoftColor;
            badge.Location = new Point(20, 14);
            badge.Size = new Size(52, 52);
            badge.Paint += DrawBorder;

            PictureBox iconBox = new PictureBox();
            iconBox.Image = Icon.ToBitmap();
            iconBox.Location = new Point(12, 12);
            iconBox.Size = new Size(28, 28);
            iconBox.SizeMode = PictureBoxSizeMode.StretchImage;
            badge.Controls.Add(iconBox);
            panel.Controls.Add(badge);

            Label titleLabel = new Label();
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            titleLabel.ForeColor = TitleColor;
            titleLabel.Location = new Point(92, 14);
            titleLabel.Text = "DeskChange";
            panel.Controls.Add(titleLabel);

            Label subtitleLabel = new Label();
            subtitleLabel.AutoSize = true;
            subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            subtitleLabel.ForeColor = BodyColor;
            subtitleLabel.Location = new Point(94, 46);
            subtitleLabel.Text = "安装向导";
            panel.Controls.Add(subtitleLabel);

            return panel;
        }

        private Panel BuildOptionsPage(out TextBox installPathTextBox, out CheckBox desktopShortcutCheckBox)
        {
            Panel page = CreatePagePanel();

            Label titleLabel = CreatePageTitle("选择安装位置");
            titleLabel.Location = new Point(24, 18);
            page.Controls.Add(titleLabel);

            Label pathLabel = CreateBodyLabel("安装目录");
            pathLabel.Location = new Point(26, 72);
            page.Controls.Add(pathLabel);

            installPathTextBox = new TextBox();
            installPathTextBox.BorderStyle = BorderStyle.FixedSingle;
            installPathTextBox.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            installPathTextBox.Location = new Point(28, 98);
            installPathTextBox.Size = new Size(500, 31);
            page.Controls.Add(installPathTextBox);

            Button browseButton = CreateGhostButton("浏览...");
            browseButton.Location = new Point(540, 95);
            browseButton.Click += OnBrowseButtonClick;
            page.Controls.Add(browseButton);

            desktopShortcutCheckBox = CreateCheckBox("在桌面创建快捷方式");
            desktopShortcutCheckBox.Location = new Point(28, 152);
            page.Controls.Add(desktopShortcutCheckBox);

            Label tipLabel = CreateTipLabel("默认安装到当前用户目录，无需管理员权限。");
            tipLabel.Location = new Point(28, 194);
            page.Controls.Add(tipLabel);

            return page;
        }

        private Panel BuildProgressPage(
            out ProgressBar progressBar,
            out Label progressTitleLabel,
            out Label progressDetailLabel,
            out CheckBox launchAfterInstallCheckBox)
        {
            Panel page = CreatePagePanel();

            progressTitleLabel = CreatePageTitle("准备安装");
            progressTitleLabel.Location = new Point(24, 18);
            page.Controls.Add(progressTitleLabel);

            progressDetailLabel = CreateBodyLabel("安装程序正在等待开始。");
            progressDetailLabel.Location = new Point(26, 72);
            progressDetailLabel.Size = new Size(620, 24);
            page.Controls.Add(progressDetailLabel);

            progressBar = new ProgressBar();
            progressBar.Location = new Point(28, 112);
            progressBar.Size = new Size(622, 20);
            page.Controls.Add(progressBar);

            launchAfterInstallCheckBox = CreateCheckBox("完成后立即运行 DeskChange");
            launchAfterInstallCheckBox.Checked = true;
            launchAfterInstallCheckBox.Location = new Point(28, 164);
            launchAfterInstallCheckBox.Visible = false;
            page.Controls.Add(launchAfterInstallCheckBox);

            return page;
        }

        private Panel BuildWelcomePage()
        {
            Panel page = CreatePagePanel();

            Label titleLabel = CreatePageTitle("欢迎使用 DeskChange 安装向导");
            titleLabel.Location = new Point(24, 18);
            page.Controls.Add(titleLabel);

            Label bodyLabel = CreateBodyLabel("此向导将帮助你把 DeskChange 安装到当前电脑。");
            bodyLabel.Location = new Point(26, 76);
            bodyLabel.Size = new Size(560, 24);
            page.Controls.Add(bodyLabel);

            Label infoLabel = CreateTipLabel("安装完成后会创建开始菜单快捷方式，并注册卸载项。");
            infoLabel.Location = new Point(26, 116);
            page.Controls.Add(infoLabel);

            return page;
        }

        private static Panel CreatePagePanel()
        {
            Panel panel = new Panel();
            panel.BackColor = CardBackgroundColor;
            panel.Dock = DockStyle.Fill;
            return panel;
        }

        private static Label CreateBodyLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular);
            label.ForeColor = BodyColor;
            label.Text = text;
            return label;
        }

        private static CheckBox CreateCheckBox(string text)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.AutoSize = true;
            checkBox.Cursor = Cursors.Hand;
            checkBox.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            checkBox.ForeColor = TitleColor;
            checkBox.Text = text;
            return checkBox;
        }

        private static Button CreateGhostButton(string text)
        {
            Button button = new Button();
            button.BackColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(243, 246, 248);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(247, 249, 251);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.ForeColor = TitleColor;
            button.Size = new Size(96, 30);
            button.Text = text;
            return button;
        }

        private static Label CreatePageTitle(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            label.ForeColor = TitleColor;
            label.Text = text;
            return label;
        }

        private static Button CreatePrimaryButton(string text)
        {
            Button button = new Button();
            button.BackColor = AccentColor;
            button.Cursor = Cursors.Hand;
            button.FlatAppearance.BorderColor = AccentColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(12, 92, 101);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(12, 92, 101);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.Size = new Size(96, 30);
            button.Text = text;
            return button;
        }

        private static Label CreateTipLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            label.ForeColor = BodyColor;
            label.Text = text;
            return label;
        }

        private static void DrawBorder(object sender, PaintEventArgs e)
        {
            Control control = sender as Control;

            if (control == null)
            {
                return;
            }

            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, control.Width - 1, control.Height - 1);
            }
        }

        private void FinishInstall(bool succeeded, string installedPath, string errorMessage)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string, string>(FinishInstall), succeeded, installedPath, errorMessage);
                return;
            }

            _installing = false;
            _installSucceeded = succeeded;
            _installFailed = !succeeded;
            _installedPath = installedPath;

            if (succeeded)
            {
                _progressBar.Value = 100;
                _progressTitleLabel.Text = "安装完成";
                _progressDetailLabel.ForeColor = BodyColor;
                _progressDetailLabel.Text = "DeskChange 已安装到：" + installedPath;
                _launchAfterInstallCheckBox.Visible = true;
            }
            else
            {
                _progressBar.Value = 0;
                _progressTitleLabel.Text = "安装失败";
                _progressDetailLabel.ForeColor = ErrorColor;
                _progressDetailLabel.Text = errorMessage;
                _launchAfterInstallCheckBox.Visible = false;
            }

            UpdateNavigation();
        }

        private void OnBackButtonClick(object sender, EventArgs e)
        {
            if (_pageIndex <= 0 || _installing)
            {
                return;
            }

            _pageIndex--;
            UpdateNavigation();
        }

        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择 DeskChange 的安装位置";
                dialog.SelectedPath = _installPathTextBox.Text;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _installPathTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            if (_installing)
            {
                return;
            }

            Close();
        }

        private void OnNextButtonClick(object sender, EventArgs e)
        {
            if (_installing)
            {
                return;
            }

            if (_pageIndex == 0)
            {
                _pageIndex = 1;
                UpdateNavigation();
                return;
            }

            if (_pageIndex == 1)
            {
                string installPath = _installPathTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(installPath))
                {
                    MessageBox.Show(this, "请选择安装目录。", "DeskChange 安装向导", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                StartInstall(new SetupOptions
                {
                    CreateDesktopShortcut = _desktopShortcutCheckBox.Checked,
                    InstallPath = installPath
                });
                return;
            }

            if (_installSucceeded && _launchAfterInstallCheckBox.Checked && File.Exists(Path.Combine(_installedPath, "DeskChange.exe")))
            {
                StartProcess(Path.Combine(_installedPath, "DeskChange.exe"), _installedPath);
            }

            Close();
        }

        private void ReportProgress(int value, string title, string detail)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, string, string>(ReportProgress), value, title, detail);
                return;
            }

            _progressBar.Value = Math.Max(0, Math.Min(100, value));
            _progressTitleLabel.Text = title;
            _progressDetailLabel.ForeColor = BodyColor;
            _progressDetailLabel.Text = detail;
        }

        private static void StartProcess(string filePath, string workingDirectory)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = filePath;
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }

        private void StartInstall(SetupOptions options)
        {
            _pageIndex = 2;
            _installing = true;
            _installSucceeded = false;
            _installFailed = false;
            _launchAfterInstallCheckBox.Visible = false;
            _progressBar.Value = 0;
            UpdateNavigation();

            Thread thread = new Thread(InstallWorker);
            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start(options);
        }

        private void InstallWorker(object state)
        {
            SetupOptions options = state as SetupOptions;

            try
            {
                ReportProgress(8, "正在准备", "正在检查安装环境...");
                StopInstalledProcess(options.InstallPath);
                Directory.CreateDirectory(options.InstallPath);

                ReportProgress(20, "正在安装", "正在写入程序文件...");
                WritePayloadFiles(options.InstallPath);

                ReportProgress(62, "正在安装", "正在创建快捷方式...");
                CreateShortcuts(options);

                ReportProgress(86, "正在安装", "正在注册卸载信息...");
                RegisterUninstallEntry(options);

                FinishInstall(true, options.InstallPath, string.Empty);
            }
            catch (Exception ex)
            {
                FinishInstall(false, options.InstallPath, "安装失败：" + ex.Message);
            }
        }

        private static void CreateShortcuts(SetupOptions options)
        {
            string installedExePath = Path.Combine(options.InstallPath, "DeskChange.exe");
            string uninstallCmdPath = Path.Combine(options.InstallPath, "Uninstall-DeskChange.cmd");
            string startMenuFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "DeskChange");
            string desktopShortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "DeskChange.lnk");

            Directory.CreateDirectory(startMenuFolder);

            CreateShortcut(
                Path.Combine(startMenuFolder, "DeskChange.lnk"),
                installedExePath,
                options.InstallPath,
                "DeskChange",
                installedExePath + ",0");
            CreateShortcut(
                Path.Combine(startMenuFolder, "卸载 DeskChange.lnk"),
                uninstallCmdPath,
                options.InstallPath,
                "卸载 DeskChange",
                "shell32.dll,131");

            if (options.CreateDesktopShortcut)
            {
                CreateShortcut(
                    desktopShortcutPath,
                    installedExePath,
                    options.InstallPath,
                    "DeskChange",
                    installedExePath + ",0");
            }
            else if (File.Exists(desktopShortcutPath))
            {
                File.Delete(desktopShortcutPath);
            }
        }

        private static void CreateShortcut(
            string shortcutPath,
            string targetPath,
            string workingDirectory,
            string description,
            string iconLocation)
        {
            object shellObject = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            object shortcutObject = shellObject.GetType().InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shellObject,
                new object[] { shortcutPath });

            shortcutObject.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcutObject, new object[] { targetPath });
            shortcutObject.GetType().InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcutObject, new object[] { workingDirectory });
            shortcutObject.GetType().InvokeMember("Description", BindingFlags.SetProperty, null, shortcutObject, new object[] { description });
            shortcutObject.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcutObject, new object[] { iconLocation });
            shortcutObject.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcutObject, null);
        }

        private static void RegisterUninstallEntry(SetupOptions options)
        {
            string installedExePath = Path.Combine(options.InstallPath, "DeskChange.exe");
            string uninstallScriptPath = Path.Combine(options.InstallPath, "Uninstall-DeskChange.ps1");
            string uninstallCommand = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File \"" + uninstallScriptPath + "\"";
            string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DeskChange";
            int estimatedSize = (int)Math.Ceiling(GetDirectorySize(options.InstallPath) / 1024D);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(registryPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("无法写入卸载信息。");
                }

                key.SetValue("DisplayName", "DeskChange", RegistryValueKind.String);
                key.SetValue("DisplayVersion", "1.0.0", RegistryValueKind.String);
                key.SetValue("Publisher", "DeskChange", RegistryValueKind.String);
                key.SetValue("DisplayIcon", installedExePath, RegistryValueKind.String);
                key.SetValue("InstallLocation", options.InstallPath, RegistryValueKind.String);
                key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"), RegistryValueKind.String);
                key.SetValue("UninstallString", uninstallCommand, RegistryValueKind.String);
                key.SetValue("QuietUninstallString", uninstallCommand, RegistryValueKind.String);
                key.SetValue("EstimatedSize", estimatedSize, RegistryValueKind.DWord);
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private static long GetDirectorySize(string path)
        {
            long total = 0;
            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            int index;

            for (index = 0; index < files.Length; index++)
            {
                total += new FileInfo(files[index]).Length;
            }

            return total;
        }

        private static void StopInstalledProcess(string installPath)
        {
            string installedExePath = Path.Combine(installPath, "DeskChange.exe");

            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcessesByName("DeskChange"))
            {
                try
                {
                    if (string.Equals(process.MainModule.FileName, installedExePath, StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static void WritePayloadFiles(string installPath)
        {
            WriteResourceToFile("DeskChange.Setup.Payload.DeskChange.exe", Path.Combine(installPath, "DeskChange.exe"));
            WriteResourceToFile("DeskChange.Setup.Payload.VirtualDesktopHelper.exe", Path.Combine(installPath, "VirtualDesktopHelper.exe"));
            WriteResourceToFile("DeskChange.Setup.Payload.VirtualDesktopHelper.LICENSE.txt", Path.Combine(installPath, "VirtualDesktopHelper.LICENSE.txt"));
            WriteResourceToFile("DeskChange.Setup.Payload.Uninstall-DeskChange.cmd", Path.Combine(installPath, "Uninstall-DeskChange.cmd"));
            WriteResourceToFile("DeskChange.Setup.Payload.Uninstall-DeskChange.ps1", Path.Combine(installPath, "Uninstall-DeskChange.ps1"));
        }

        private static void WriteResourceToFile(string resourceName, string outputPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("找不到安装资源：" + resourceName);
                }

                using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }

        private void UpdateNavigation()
        {
            int index;

            for (index = 0; index < _pages.Length; index++)
            {
                _pages[index].Visible = index == _pageIndex;
            }

            _backButton.Enabled = !_installing && _pageIndex > 0 && !_installSucceeded && !_installFailed;
            _cancelButton.Enabled = !_installing;

            if (_installing)
            {
                _nextButton.Enabled = false;
                _nextButton.Text = "安装中...";
                return;
            }

            _nextButton.Enabled = true;

            if (_pageIndex == 0)
            {
                _nextButton.Text = "下一步 >";
                return;
            }

            if (_pageIndex == 1)
            {
                _nextButton.Text = "安装";
                return;
            }

            _nextButton.Text = _installSucceeded ? "完成" : "关闭";
        }

        private sealed class SetupOptions
        {
            public bool CreateDesktopShortcut { get; set; }

            public string InstallPath { get; set; }
        }
    }
}
