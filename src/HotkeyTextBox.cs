using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeskChange
{
    internal sealed class HotkeyTextBox : Control
    {
        private static readonly Color EmptyTextColor = Color.FromArgb(132, 144, 156);
        private static readonly Color ValueTextColor = Color.FromArgb(17, 96, 109);
        private static readonly Color BorderColor = Color.FromArgb(144, 154, 161);
        private static readonly Color FocusBorderColor = Color.FromArgb(19, 110, 120);

        private HotkeyBinding _binding = new HotkeyBinding();

        public HotkeyTextBox()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.UserPaint,
                true);

            BackColor = Color.FromArgb(250, 252, 252);
            Cursor = Cursors.Hand;
            Font = new Font("Consolas", 10F, FontStyle.Bold);
            ForeColor = ValueTextColor;
            Size = new Size(260, 30);
            TabStop = true;
            UpdateColors();
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
                    UpdateColors();
                    Invalidate();
                    OnBindingChanged();
                    return;
                }

                UpdateColors();
                Invalidate();
            }
        }

        public void ClearBinding()
        {
            SetBinding(new HotkeyBinding());
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
            Invalidate();
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

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(BackColor);

            Rectangle textBounds = new Rectangle(6, 0, Math.Max(0, Width - 12), Height);
            TextRenderer.DrawText(
                e.Graphics,
                _binding.ToDisplayString(),
                Font,
                textBounds,
                ForeColor,
                TextFormatFlags.HorizontalCenter
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.EndEllipsis
                | TextFormatFlags.SingleLine);

            using (Pen pen = new Pen(Focused ? FocusBorderColor : BorderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
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

        private void UpdateColors()
        {
            ForeColor = _binding.IsEmpty ? EmptyTextColor : ValueTextColor;
        }
    }
}
