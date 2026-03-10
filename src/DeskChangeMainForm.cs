using System;
using System.Drawing;
using System.Windows.Forms;
using DeskChange.Services;

namespace DeskChange
{
    internal sealed class SaveSettingsRequestedEventArgs : EventArgs
    {
        public SaveSettingsRequestedEventArgs(AppSettings settings, bool startupEnabled)
        {
            Settings = settings;
            StartupEnabled = startupEnabled;
        }

        public AppSettings Settings { get; private set; }

        public bool StartupEnabled { get; private set; }
    }

    internal sealed class DeskChangeMainForm : Form
    {
        private static readonly Color WindowBackgroundColor = Color.FromArgb(242, 246, 247);
        private static readonly Color CardBackgroundColor = Color.FromArgb(255, 255, 255);
        private static readonly Color CardBorderColor = Color.FromArgb(220, 229, 233);
        private static readonly Color AccentColor = Color.FromArgb(19, 110, 120);
        private static readonly Color AccentHoverColor = Color.FromArgb(12, 92, 101);
        private static readonly Color AccentSoftColor = Color.FromArgb(226, 242, 244);
        private static readonly Color TitleColor = Color.FromArgb(29, 37, 43);
        private static readonly Color MutedColor = Color.FromArgb(118, 129, 139);
        private static readonly Color ErrorColor = Color.FromArgb(185, 72, 54);

        private readonly ComboBox _desktopCountComboBox;
        private readonly Panel[] _desktopRowPanels;
        private readonly HotkeyTextBox[] _hotkeyTextBoxes;
        private readonly CheckBox _startupCheckBox;
        private readonly CheckBox _startHiddenCheckBox;
        private readonly CheckBox _switchAnimationCheckBox;
        private readonly Button _createDesktopButton;
        private readonly Button _removeCurrentDesktopButton;
        private readonly Label _currentDesktopLabel;
        private readonly Label _systemDesktopCountLabel;
        private readonly Label _desktopWarningLabel;
        private readonly Label _messageLabel;

        private bool _allowClose;
        private int _currentDesktopIndex;
        private bool _suspendEvents;
        private int _systemDesktopCount;

        public DeskChangeMainForm(Icon appIcon)
        {
            _desktopRowPanels = new Panel[AppSettings.MaxDesktopCount];
            _hotkeyTextBoxes = new HotkeyTextBox[AppSettings.MaxDesktopCount];

            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = WindowBackgroundColor;
            ClientSize = new Size(760, 650);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = appIcon;
            Margin = new Padding(0);
            MaximizeBox = false;
            MinimizeBox = true;
            MinimumSize = new Size(776, 689);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "DeskChange 设置";

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.BackColor = WindowBackgroundColor;
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Padding = new Padding(20);
            rootLayout.RowCount = 4;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 300F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(BuildHeaderCard(appIcon), 0, 0);
            rootLayout.Controls.Add(
                BuildDesktopCard(
                    out _desktopCountComboBox,
                    out _systemDesktopCountLabel,
                    out _currentDesktopLabel,
                    out _createDesktopButton,
                    out _removeCurrentDesktopButton,
                    out _desktopWarningLabel),
                0,
                1);
            rootLayout.Controls.Add(BuildStartupCard(out _startupCheckBox, out _startHiddenCheckBox, out _switchAnimationCheckBox), 0, 2);
            rootLayout.Controls.Add(BuildActionCard(out _messageLabel), 0, 3);

            Controls.Add(rootLayout);
        }

        public event EventHandler<SaveSettingsRequestedEventArgs> SaveRequested;

        public event EventHandler CreateDesktopRequested;

        public event EventHandler RemoveCurrentDesktopRequested;

        public void ApplyRuntimeState(AppSettings settings, bool startupEnabled, int systemDesktopCount, int currentDesktopIndex)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<AppSettings, bool, int, int>(ApplyRuntimeState), settings, startupEnabled, systemDesktopCount, currentDesktopIndex);
                return;
            }

            if (settings == null)
            {
                return;
            }

            settings = settings.Clone();
            settings.Normalize();

            _suspendEvents = true;
            _startupCheckBox.Checked = startupEnabled;
            _startHiddenCheckBox.Checked = settings.StartHiddenOnStartup;
            _startHiddenCheckBox.Enabled = startupEnabled;
            _switchAnimationCheckBox.Checked = settings.EnableSwitchAnimation;
            _desktopCountComboBox.SelectedIndex = settings.DesktopCount - AppSettings.MinDesktopCount;

            int index;

            for (index = 0; index < AppSettings.MaxDesktopCount; index++)
            {
                _hotkeyTextBoxes[index].Binding = settings.GetHotkey(index);
            }

            _suspendEvents = false;

            UpdateDesktopRuntimeInfo(systemDesktopCount, currentDesktopIndex);
            UpdateDesktopRows();
        }

        public void PrepareForExit()
        {
            _allowClose = true;
        }

        public void SetInlineMessage(string text, bool isError)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, bool>(SetInlineMessage), text, isError);
                return;
            }

            _messageLabel.ForeColor = isError ? ErrorColor : AccentColor;
            _messageLabel.Text = string.IsNullOrWhiteSpace(text) ? " " : text;
        }

        public void ShowFromTray()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ShowFromTray));
                return;
            }

            ShowInTaskbar = true;

            if (!Visible)
            {
                Show();
            }

            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
            BringToFront();
        }

        public void UpdateDesktopRuntimeInfo(int systemDesktopCount, int currentDesktopIndex)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int>(UpdateDesktopRuntimeInfo), systemDesktopCount, currentDesktopIndex);
                return;
            }

            _systemDesktopCount = systemDesktopCount;
            _currentDesktopIndex = currentDesktopIndex;
            UpdateDesktopMetaText();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
            }

            base.OnFormClosing(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (!_allowClose && WindowState == FormWindowState.Minimized)
            {
                HideToTray();
            }
        }

        private Panel BuildActionCard(out Label messageLabel)
        {
            Panel card = CreateCardPanel();

            messageLabel = new Label();
            messageLabel.AutoEllipsis = true;
            messageLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            messageLabel.ForeColor = AccentColor;
            messageLabel.Location = new Point(20, 22);
            messageLabel.Size = new Size(430, 34);
            messageLabel.Text = " ";
            card.Controls.Add(messageLabel);

            Button restoreButton = CreateGhostButton("恢复默认");
            restoreButton.Location = new Point(490, 18);
            restoreButton.Click += OnRestoreDefaultsButtonClick;
            card.Controls.Add(restoreButton);

            Button saveButton = CreatePrimaryButton("保存并应用");
            saveButton.Location = new Point(606, 18);
            saveButton.Click += OnSaveButtonClick;
            card.Controls.Add(saveButton);

            return card;
        }

        private Panel BuildDesktopCard(
            out ComboBox desktopCountComboBox,
            out Label systemDesktopCountLabel,
            out Label currentDesktopLabel,
            out Button createDesktopButton,
            out Button removeCurrentDesktopButton,
            out Label desktopWarningLabel)
        {
            Panel card = CreateCardPanel();

            Label titleLabel = CreateTitleLabel("桌面与快捷键");
            titleLabel.Location = new Point(20, 16);
            card.Controls.Add(titleLabel);

            Label desktopCountLabel = CreateMutedLabel("桌面数量");
            desktopCountLabel.Location = new Point(22, 56);
            desktopCountLabel.Size = new Size(70, 20);
            card.Controls.Add(desktopCountLabel);

            desktopCountComboBox = new ComboBox();
            desktopCountComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            desktopCountComboBox.FlatStyle = FlatStyle.Flat;
            desktopCountComboBox.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            desktopCountComboBox.Items.AddRange(new object[] { "1", "2", "3", "4" });
            desktopCountComboBox.Location = new Point(94, 52);
            desktopCountComboBox.Size = new Size(86, 30);
            desktopCountComboBox.SelectedIndexChanged += OnDesktopCountComboBoxSelectedIndexChanged;
            card.Controls.Add(desktopCountComboBox);

            systemDesktopCountLabel = CreateMutedLabel("系统桌面数：读取中");
            systemDesktopCountLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            systemDesktopCountLabel.ForeColor = AccentColor;
            systemDesktopCountLabel.Location = new Point(208, 56);
            systemDesktopCountLabel.Size = new Size(210, 20);
            card.Controls.Add(systemDesktopCountLabel);

            currentDesktopLabel = CreateMutedLabel("当前桌面：读取中");
            currentDesktopLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            currentDesktopLabel.ForeColor = AccentColor;
            currentDesktopLabel.Location = new Point(376, 56);
            currentDesktopLabel.Size = new Size(130, 20);
            card.Controls.Add(currentDesktopLabel);

            createDesktopButton = CreateGhostButton("新建桌面");
            createDesktopButton.Location = new Point(520, 48);
            createDesktopButton.Size = new Size(84, 30);
            createDesktopButton.Click += OnCreateDesktopButtonClick;
            card.Controls.Add(createDesktopButton);

            removeCurrentDesktopButton = CreateGhostButton("删除当前桌面");
            removeCurrentDesktopButton.Location = new Point(612, 48);
            removeCurrentDesktopButton.Size = new Size(96, 30);
            removeCurrentDesktopButton.Click += OnRemoveCurrentDesktopButtonClick;
            card.Controls.Add(removeCurrentDesktopButton);

            desktopWarningLabel = CreateMutedLabel(" ");
            desktopWarningLabel.Location = new Point(22, 84);
            desktopWarningLabel.Size = new Size(690, 18);
            card.Controls.Add(desktopWarningLabel);

            int top = 108;
            int index;

            for (index = 0; index < AppSettings.MaxDesktopCount; index++)
            {
                Panel rowPanel = BuildDesktopRow(index);
                rowPanel.Location = new Point(22, top + (index * 42));
                card.Controls.Add(rowPanel);
                _desktopRowPanels[index] = rowPanel;
            }

            return card;
        }

        private Panel BuildDesktopRow(int index)
        {
            Panel rowPanel = new Panel();
            rowPanel.BackColor = Color.Transparent;
            rowPanel.Size = new Size(690, 40);

            Label nameLabel = new Label();
            nameLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            nameLabel.ForeColor = TitleColor;
            nameLabel.Location = new Point(0, 8);
            nameLabel.Size = new Size(72, 20);
            nameLabel.Text = "桌面 " + (index + 1);
            rowPanel.Controls.Add(nameLabel);

            HotkeyTextBox hotkeyTextBox = new HotkeyTextBox();
            hotkeyTextBox.BackColor = Color.FromArgb(250, 252, 252);
            hotkeyTextBox.Location = new Point(80, 5);
            hotkeyTextBox.Size = new Size(260, 30);
            rowPanel.Controls.Add(hotkeyTextBox);
            _hotkeyTextBoxes[index] = hotkeyTextBox;

            Button clearButton = CreateGhostButton("清空");
            clearButton.Location = new Point(352, 5);
            clearButton.Size = new Size(70, 30);
            clearButton.Click += CreateClearHotkeyHandler(index);
            rowPanel.Controls.Add(clearButton);

            return rowPanel;
        }

        private Panel BuildHeaderCard(Icon appIcon)
        {
            Panel card = CreateCardPanel();

            Panel iconBadge = new Panel();
            iconBadge.BackColor = AccentSoftColor;
            iconBadge.Location = new Point(20, 20);
            iconBadge.Size = new Size(56, 56);
            iconBadge.Paint += DrawBadgeBorder;

            PictureBox iconBox = new PictureBox();
            iconBox.Image = appIcon.ToBitmap();
            iconBox.Location = new Point(14, 14);
            iconBox.Size = new Size(28, 28);
            iconBox.SizeMode = PictureBoxSizeMode.StretchImage;
            iconBadge.Controls.Add(iconBox);
            card.Controls.Add(iconBadge);

            Label titleLabel = new Label();
            titleLabel.AutoSize = true;
            titleLabel.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            titleLabel.ForeColor = TitleColor;
            titleLabel.Location = new Point(92, 18);
            titleLabel.Text = "DeskChange";
            card.Controls.Add(titleLabel);

            Label subtitleLabel = new Label();
            subtitleLabel.AutoSize = true;
            subtitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular);
            subtitleLabel.ForeColor = MutedColor;
            subtitleLabel.Location = new Point(94, 50);
            subtitleLabel.Text = "虚拟桌面热键与启动设置";
            card.Controls.Add(subtitleLabel);

            return card;
        }

        private Panel BuildStartupCard(
            out CheckBox startupCheckBox,
            out CheckBox startHiddenCheckBox,
            out CheckBox switchAnimationCheckBox)
        {
            Panel card = CreateCardPanel();

            Label titleLabel = CreateTitleLabel("启动与切换");
            titleLabel.Location = new Point(20, 16);
            card.Controls.Add(titleLabel);

            startupCheckBox = CreateCheckBox("开机自启动");
            startupCheckBox.Location = new Point(22, 56);
            startupCheckBox.CheckedChanged += OnStartupCheckBoxCheckedChanged;
            card.Controls.Add(startupCheckBox);

            startHiddenCheckBox = CreateCheckBox("随系统启动时隐藏到托盘");
            startHiddenCheckBox.Location = new Point(22, 90);
            card.Controls.Add(startHiddenCheckBox);

            switchAnimationCheckBox = CreateCheckBox("显示桌面切换动画");
            switchAnimationCheckBox.Location = new Point(320, 56);
            card.Controls.Add(switchAnimationCheckBox);

            return card;
        }

        private static Panel CreateCardPanel()
        {
            Panel panel = new Panel();
            panel.BackColor = CardBackgroundColor;
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, 0, 16);
            panel.Paint += DrawCardBorder;
            return panel;
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
            button.FlatAppearance.BorderColor = CardBorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(242, 246, 247);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(247, 250, 251);
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.ForeColor = TitleColor;
            button.Size = new Size(104, 36);
            button.Text = text;
            return button;
        }

        private static Label CreateMutedLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            label.ForeColor = MutedColor;
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
            button.FlatAppearance.MouseDownBackColor = AccentHoverColor;
            button.FlatAppearance.MouseOverBackColor = AccentHoverColor;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            button.ForeColor = Color.White;
            button.Size = new Size(118, 36);
            button.Text = text;
            return button;
        }

        private static Label CreateTitleLabel(string text)
        {
            Label label = new Label();
            label.AutoSize = true;
            label.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
            label.ForeColor = TitleColor;
            label.Text = text;
            return label;
        }

        private static void DrawBadgeBorder(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;

            if (panel == null)
            {
                return;
            }

            using (Pen pen = new Pen(Color.FromArgb(180, 214, 221)))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }

        private static void DrawCardBorder(object sender, PaintEventArgs e)
        {
            Panel panel = sender as Panel;

            if (panel == null)
            {
                return;
            }

            using (Pen pen = new Pen(CardBorderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }

        private EventHandler CreateClearHotkeyHandler(int desktopIndex)
        {
            return delegate(object sender, EventArgs e)
            {
                _hotkeyTextBoxes[desktopIndex].ClearBinding();
            };
        }

        private AppSettings BuildSettingsFromControls()
        {
            AppSettings settings = AppSettings.CreateDefault();
            settings.DesktopCount = GetSelectedDesktopCount();
            settings.EnableSwitchAnimation = _switchAnimationCheckBox.Checked;
            settings.StartHiddenOnStartup = _startHiddenCheckBox.Checked;

            int index;

            for (index = 0; index < AppSettings.MaxDesktopCount; index++)
            {
                settings.SetHotkey(index, _hotkeyTextBoxes[index].Binding);
            }

            settings.Normalize();
            return settings;
        }

        private int GetSelectedDesktopCount()
        {
            int desktopCount;

            if (int.TryParse(Convert.ToString(_desktopCountComboBox.SelectedItem), out desktopCount))
            {
                return desktopCount;
            }

            return 2;
        }

        private void HideToTray()
        {
            ShowInTaskbar = false;
            Hide();
        }

        private void OnDesktopCountComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendEvents)
            {
                return;
            }

            UpdateDesktopRows();
        }

        private void OnCreateDesktopButtonClick(object sender, EventArgs e)
        {
            EventHandler handler = CreateDesktopRequested;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnRemoveCurrentDesktopButtonClick(object sender, EventArgs e)
        {
            EventHandler handler = RemoveCurrentDesktopRequested;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void OnRestoreDefaultsButtonClick(object sender, EventArgs e)
        {
            ApplyRuntimeState(AppSettings.CreateDefault(), false, _systemDesktopCount, _currentDesktopIndex);
            SetInlineMessage("已恢复默认草稿，保存后生效。", false);
        }

        private void OnSaveButtonClick(object sender, EventArgs e)
        {
            EventHandler<SaveSettingsRequestedEventArgs> handler = SaveRequested;

            if (handler != null)
            {
                handler(this, new SaveSettingsRequestedEventArgs(BuildSettingsFromControls(), _startupCheckBox.Checked));
            }
        }

        private void OnStartupCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            if (_suspendEvents)
            {
                return;
            }

            _startHiddenCheckBox.Enabled = _startupCheckBox.Checked;
        }

        private void UpdateDesktopMetaText()
        {
            if (_systemDesktopCount < 0)
            {
                _systemDesktopCountLabel.Text = "系统桌面数：读取失败";
            }
            else
            {
                _systemDesktopCountLabel.Text = "系统桌面数：" + _systemDesktopCount;
            }

            if (_currentDesktopIndex < 0)
            {
                _currentDesktopLabel.Text = "当前桌面：读取失败";
            }
            else
            {
                _currentDesktopLabel.Text = "当前桌面：" + (_currentDesktopIndex + 1);
            }

            _removeCurrentDesktopButton.Enabled = _systemDesktopCount > 1 && _currentDesktopIndex >= 0;
            int desktopCount = GetSelectedDesktopCount();

            if (_systemDesktopCount >= 0 && desktopCount > _systemDesktopCount)
            {
                _desktopWarningLabel.ForeColor = ErrorColor;
                _desktopWarningLabel.Text = string.Format(
                    "当前系统只有 {0} 个桌面，超出的配置暂不会生效。",
                    _systemDesktopCount);
                return;
            }

            _desktopWarningLabel.ForeColor = MutedColor;
            _desktopWarningLabel.Text = "留空表示未启用。";
        }

        private void UpdateDesktopRows()
        {
            int desktopCount = GetSelectedDesktopCount();
            int index;

            for (index = 0; index < AppSettings.MaxDesktopCount; index++)
            {
                _desktopRowPanels[index].Enabled = index < desktopCount;
            }

            UpdateDesktopMetaText();
        }
    }
}
