using System;
using System.Windows.Forms;

namespace DeskChange.Services
{
    internal sealed class AppSettings
    {
        public const int MinDesktopCount = 1;
        public const int MaxDesktopCount = 4;

        public AppSettings()
        {
            DesktopCount = 2;
            EnableSwitchAnimation = false;
            StartHiddenOnStartup = true;
            Desktop1Hotkey = CreateDefaultHotkey(Keys.D1);
            Desktop2Hotkey = CreateDefaultHotkey(Keys.D2);
            Desktop3Hotkey = new HotkeyBinding();
            Desktop4Hotkey = new HotkeyBinding();
        }

        public int DesktopCount { get; set; }

        public bool EnableSwitchAnimation { get; set; }

        public bool StartHiddenOnStartup { get; set; }

        public HotkeyBinding Desktop1Hotkey { get; set; }

        public HotkeyBinding Desktop2Hotkey { get; set; }

        public HotkeyBinding Desktop3Hotkey { get; set; }

        public HotkeyBinding Desktop4Hotkey { get; set; }

        public AppSettings Clone()
        {
            AppSettings copy = new AppSettings();
            copy.DesktopCount = DesktopCount;
            copy.EnableSwitchAnimation = EnableSwitchAnimation;
            copy.StartHiddenOnStartup = StartHiddenOnStartup;
            copy.Desktop1Hotkey = CloneBinding(Desktop1Hotkey, CreateDefaultHotkey(Keys.D1));
            copy.Desktop2Hotkey = CloneBinding(Desktop2Hotkey, CreateDefaultHotkey(Keys.D2));
            copy.Desktop3Hotkey = CloneBinding(Desktop3Hotkey, new HotkeyBinding());
            copy.Desktop4Hotkey = CloneBinding(Desktop4Hotkey, new HotkeyBinding());
            copy.Normalize();
            return copy;
        }

        public HotkeyBinding GetHotkey(int desktopIndex)
        {
            switch (desktopIndex)
            {
                case 0:
                    return CloneBinding(Desktop1Hotkey, CreateDefaultHotkey(Keys.D1));
                case 1:
                    return CloneBinding(Desktop2Hotkey, CreateDefaultHotkey(Keys.D2));
                case 2:
                    return CloneBinding(Desktop3Hotkey, new HotkeyBinding());
                case 3:
                    return CloneBinding(Desktop4Hotkey, new HotkeyBinding());
                default:
                    throw new ArgumentOutOfRangeException("desktopIndex");
            }
        }

        public void Normalize()
        {
            if (DesktopCount < MinDesktopCount)
            {
                DesktopCount = MinDesktopCount;
            }
            else if (DesktopCount > MaxDesktopCount)
            {
                DesktopCount = MaxDesktopCount;
            }

            if (Desktop1Hotkey == null)
            {
                Desktop1Hotkey = CreateDefaultHotkey(Keys.D1);
            }

            if (Desktop2Hotkey == null)
            {
                Desktop2Hotkey = CreateDefaultHotkey(Keys.D2);
            }

            if (Desktop3Hotkey == null)
            {
                Desktop3Hotkey = new HotkeyBinding();
            }

            if (Desktop4Hotkey == null)
            {
                Desktop4Hotkey = new HotkeyBinding();
            }
        }

        public void SetHotkey(int desktopIndex, HotkeyBinding binding)
        {
            HotkeyBinding value = binding == null ? new HotkeyBinding() : binding.Clone();

            switch (desktopIndex)
            {
                case 0:
                    Desktop1Hotkey = value;
                    return;
                case 1:
                    Desktop2Hotkey = value;
                    return;
                case 2:
                    Desktop3Hotkey = value;
                    return;
                case 3:
                    Desktop4Hotkey = value;
                    return;
                default:
                    throw new ArgumentOutOfRangeException("desktopIndex");
            }
        }

        public static AppSettings CreateDefault()
        {
            AppSettings settings = new AppSettings();
            settings.Normalize();
            return settings;
        }

        private static HotkeyBinding CloneBinding(HotkeyBinding binding, HotkeyBinding fallback)
        {
            if (binding != null)
            {
                return binding.Clone();
            }

            return fallback.Clone();
        }

        private static HotkeyBinding CreateDefaultHotkey(Keys key)
        {
            HotkeyBinding binding = new HotkeyBinding();
            binding.Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt;
            binding.Key = key;
            return binding;
        }
    }
}
