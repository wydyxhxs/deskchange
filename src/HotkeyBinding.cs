using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DeskChange
{
    internal sealed class HotkeyBinding
    {
        public HotkeyBinding()
        {
            Key = Keys.None;
            Modifiers = HotkeyModifiers.None;
        }

        public Keys Key { get; set; }

        public HotkeyModifiers Modifiers { get; set; }

        public bool IsEmpty
        {
            get { return Key == Keys.None; }
        }

        public HotkeyBinding Clone()
        {
            return new HotkeyBinding
            {
                Key = Key,
                Modifiers = Modifiers
            };
        }

        public string ToDisplayString()
        {
            if (IsEmpty)
            {
                return "未设置";
            }

            List<string> parts = new List<string>();

            if ((Modifiers & HotkeyModifiers.Control) == HotkeyModifiers.Control)
            {
                parts.Add("Ctrl");
            }

            if ((Modifiers & HotkeyModifiers.Alt) == HotkeyModifiers.Alt)
            {
                parts.Add("Alt");
            }

            if ((Modifiers & HotkeyModifiers.Shift) == HotkeyModifiers.Shift)
            {
                parts.Add("Shift");
            }

            parts.Add(GetKeyDisplayName(Key));
            return string.Join("+", parts.ToArray());
        }

        public override string ToString()
        {
            return ToDisplayString();
        }

        public override bool Equals(object obj)
        {
            HotkeyBinding other = obj as HotkeyBinding;

            if (other == null)
            {
                return false;
            }

            return Key == other.Key && Modifiers == other.Modifiers;
        }

        public override int GetHashCode()
        {
            return ((int)Modifiers << 16) ^ (int)Key;
        }

        public static bool IsSupported(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return true;
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return true;
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return true;
            }

            if (key >= Keys.F1 && key <= Keys.F12)
            {
                return true;
            }

            switch (key)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Insert:
                case Keys.Delete:
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryCreate(Keys keyCode, Keys modifiers, out HotkeyBinding binding)
        {
            binding = null;

            if (!IsSupported(keyCode))
            {
                return false;
            }

            HotkeyModifiers hotkeyModifiers = HotkeyModifiers.None;

            if ((modifiers & Keys.Control) == Keys.Control)
            {
                hotkeyModifiers |= HotkeyModifiers.Control;
            }

            if ((modifiers & Keys.Alt) == Keys.Alt)
            {
                hotkeyModifiers |= HotkeyModifiers.Alt;
            }

            if ((modifiers & Keys.Shift) == Keys.Shift)
            {
                hotkeyModifiers |= HotkeyModifiers.Shift;
            }

            if (hotkeyModifiers == HotkeyModifiers.None)
            {
                return false;
            }

            binding = new HotkeyBinding();
            binding.Key = keyCode;
            binding.Modifiers = hotkeyModifiers;
            return true;
        }

        public static bool TryParse(string text, out HotkeyBinding binding)
        {
            binding = new HotkeyBinding();

            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            string[] parts = text.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            int index;
            bool hasKey = false;

            for (index = 0; index < parts.Length; index++)
            {
                string part = parts[index].Trim();

                if (string.Equals(part, "Ctrl", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(part, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    binding.Modifiers |= HotkeyModifiers.Control;
                    continue;
                }

                if (string.Equals(part, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    binding.Modifiers |= HotkeyModifiers.Alt;
                    continue;
                }

                if (string.Equals(part, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    binding.Modifiers |= HotkeyModifiers.Shift;
                    continue;
                }

                if (hasKey)
                {
                    binding = null;
                    return false;
                }

                Keys parsedKey;

                if (!TryParseKey(part, out parsedKey))
                {
                    binding = null;
                    return false;
                }

                binding.Key = parsedKey;
                hasKey = true;
            }

            if (!hasKey)
            {
                binding = null;
                return false;
            }

            return true;
        }

        private static string GetKeyDisplayName(Keys key)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                return key.ToString();
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((int)(key - Keys.D0)).ToString();
            }

            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return "NumPad" + (int)(key - Keys.NumPad0);
            }

            if (key >= Keys.F1 && key <= Keys.F12)
            {
                return key.ToString();
            }

            switch (key)
            {
                case Keys.Up:
                    return "Up";
                case Keys.Down:
                    return "Down";
                case Keys.Left:
                    return "Left";
                case Keys.Right:
                    return "Right";
                case Keys.Home:
                    return "Home";
                case Keys.End:
                    return "End";
                case Keys.PageUp:
                    return "PageUp";
                case Keys.PageDown:
                    return "PageDown";
                case Keys.Insert:
                    return "Insert";
                case Keys.Delete:
                    return "Delete";
                default:
                    return key.ToString();
            }
        }

        private static bool TryParseKey(string part, out Keys key)
        {
            key = Keys.None;

            if (part.Length == 1 && part[0] >= '0' && part[0] <= '9')
            {
                key = Keys.D0 + (part[0] - '0');
                return true;
            }

            if (part.Length == 1 && part[0] >= 'A' && part[0] <= 'Z')
            {
                key = Keys.A + (part[0] - 'A');
                return true;
            }

            if (part.Length == 1 && part[0] >= 'a' && part[0] <= 'z')
            {
                key = Keys.A + (part[0] - 'a');
                return true;
            }

            if (part.StartsWith("NumPad", StringComparison.OrdinalIgnoreCase))
            {
                int number;

                if (int.TryParse(part.Substring(6), out number) && number >= 0 && number <= 9)
                {
                    key = Keys.NumPad0 + number;
                    return true;
                }
            }

            if (part.StartsWith("F", StringComparison.OrdinalIgnoreCase))
            {
                int number;

                if (int.TryParse(part.Substring(1), out number) && number >= 1 && number <= 12)
                {
                    key = Keys.F1 + (number - 1);
                    return true;
                }
            }

            switch (part.ToUpperInvariant())
            {
                case "UP":
                    key = Keys.Up;
                    return true;
                case "DOWN":
                    key = Keys.Down;
                    return true;
                case "LEFT":
                    key = Keys.Left;
                    return true;
                case "RIGHT":
                    key = Keys.Right;
                    return true;
                case "HOME":
                    key = Keys.Home;
                    return true;
                case "END":
                    key = Keys.End;
                    return true;
                case "PAGEUP":
                    key = Keys.PageUp;
                    return true;
                case "PAGEDOWN":
                    key = Keys.PageDown;
                    return true;
                case "INSERT":
                    key = Keys.Insert;
                    return true;
                case "DELETE":
                    key = Keys.Delete;
                    return true;
                default:
                    return false;
            }
        }
    }
}
