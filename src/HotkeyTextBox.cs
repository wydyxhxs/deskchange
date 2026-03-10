using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeskChange
{
    internal sealed class HotkeyTextBox : TextBox
    {
        private HotkeyBinding _binding = new HotkeyBinding();

        public HotkeyTextBox()
        {
            AutoSize = false;
            BorderStyle = BorderStyle.FixedSingle;
            Cursor = Cursors.Hand;
            Font = new Font("Consolas", 10F, FontStyle.Bold);
            ReadOnly = true;
            ShortcutsEnabled = false;
            TextAlign = HorizontalAlignment.Center;
            UpdateText();
        }

        public event EventHandler BindingChanged;

        public HotkeyBinding Binding
        {
            get { return _binding.Clone(); }
            set
            {
                HotkeyBinding binding = value == null ? new HotkeyBinding() : value.Clone();

                if (!_binding.Equals(binding))
                {
                    _binding = binding;
                    UpdateText();
                    OnBindingChanged();
                    return;
                }

                UpdateText();
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;

            if (keyCode == Keys.Tab)
            {
                return false;
            }

            return true;
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Select(0, 0);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Tab)
            {
                return;
            }

            if ((e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back) && e.Modifiers == Keys.None)
            {
                SetBinding(new HotkeyBinding());
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            HotkeyBinding binding;

            if (HotkeyBinding.TryCreate(e.KeyCode, e.Modifiers, out binding))
            {
                SetBinding(binding);
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        public void ClearBinding()
        {
            SetBinding(new HotkeyBinding());
        }

        private void OnBindingChanged()
        {
            EventHandler handler = BindingChanged;

            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void SetBinding(HotkeyBinding binding)
        {
            Binding = binding;
        }

        private void UpdateText()
        {
            Text = _binding.ToDisplayString();
            ForeColor = _binding.IsEmpty
                ? Color.FromArgb(132, 144, 156)
                : Color.FromArgb(17, 96, 109);
        }
    }
}
