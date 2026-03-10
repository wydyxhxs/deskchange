using System;
using System.IO;
using System.Text;

namespace DeskChange.Services
{
    internal sealed class PortableSettingsStore
    {
        private const string DesktopCountKey = "desktop_count";
        private const string EnableSwitchAnimationKey = "enable_switch_animation";
        private const string StartHiddenOnStartupKey = "start_hidden_on_startup";
        private const string Desktop1HotkeyKey = "desktop_1_hotkey";
        private const string Desktop2HotkeyKey = "desktop_2_hotkey";
        private const string Desktop3HotkeyKey = "desktop_3_hotkey";
        private const string Desktop4HotkeyKey = "desktop_4_hotkey";

        private readonly string _fallbackPath;
        private readonly string _portablePath;

        public PortableSettingsStore()
        {
            _portablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DeskChange.settings.ini");
            _fallbackPath = Path.Combine(AppLog.LogDirectory, "DeskChange.settings.ini");
        }

        public string ActivePath
        {
            get
            {
                if (File.Exists(_portablePath))
                {
                    return _portablePath;
                }

                if (File.Exists(_fallbackPath))
                {
                    return _fallbackPath;
                }

                return _portablePath;
            }
        }

        public AppSettings Load()
        {
            string path = File.Exists(_portablePath) ? _portablePath : _fallbackPath;
            AppSettings settings = AppSettings.CreateDefault();

            if (!File.Exists(path))
            {
                return settings;
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            int index;

            for (index = 0; index < lines.Length; index++)
            {
                ApplyLine(settings, lines[index]);
            }

            settings.Normalize();
            return settings;
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.Normalize();
            string content = BuildContent(settings);

            try
            {
                WriteAllText(_portablePath, content);
            }
            catch (UnauthorizedAccessException)
            {
                WriteAllText(_fallbackPath, content);
            }
            catch (IOException)
            {
                WriteAllText(_fallbackPath, content);
            }
        }

        private static void ApplyLine(AppSettings settings, string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            {
                return;
            }

            int separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
            {
                return;
            }

            string key = line.Substring(0, separatorIndex).Trim();
            string value = line.Substring(separatorIndex + 1).Trim();

            if (string.Equals(key, DesktopCountKey, StringComparison.OrdinalIgnoreCase))
            {
                settings.DesktopCount = ParseInteger(value, settings.DesktopCount);
                return;
            }

            if (string.Equals(key, EnableSwitchAnimationKey, StringComparison.OrdinalIgnoreCase))
            {
                settings.EnableSwitchAnimation = ParseBoolean(value, settings.EnableSwitchAnimation);
                return;
            }

            if (string.Equals(key, StartHiddenOnStartupKey, StringComparison.OrdinalIgnoreCase))
            {
                settings.StartHiddenOnStartup = ParseBoolean(value, true);
                return;
            }

            if (string.Equals(key, Desktop1HotkeyKey, StringComparison.OrdinalIgnoreCase))
            {
                ApplyHotkey(settings, 0, value);
                return;
            }

            if (string.Equals(key, Desktop2HotkeyKey, StringComparison.OrdinalIgnoreCase))
            {
                ApplyHotkey(settings, 1, value);
                return;
            }

            if (string.Equals(key, Desktop3HotkeyKey, StringComparison.OrdinalIgnoreCase))
            {
                ApplyHotkey(settings, 2, value);
                return;
            }

            if (string.Equals(key, Desktop4HotkeyKey, StringComparison.OrdinalIgnoreCase))
            {
                ApplyHotkey(settings, 3, value);
            }
        }

        private static string BuildContent(AppSettings settings)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# DeskChange portable settings");
            builder.Append(DesktopCountKey);
            builder.Append('=');
            builder.AppendLine(settings.DesktopCount.ToString());
            builder.Append(EnableSwitchAnimationKey);
            builder.Append('=');
            builder.AppendLine(settings.EnableSwitchAnimation ? "true" : "false");
            builder.Append(StartHiddenOnStartupKey);
            builder.Append('=');
            builder.AppendLine(settings.StartHiddenOnStartup ? "true" : "false");
            AppendHotkey(builder, Desktop1HotkeyKey, settings.Desktop1Hotkey);
            AppendHotkey(builder, Desktop2HotkeyKey, settings.Desktop2Hotkey);
            AppendHotkey(builder, Desktop3HotkeyKey, settings.Desktop3Hotkey);
            AppendHotkey(builder, Desktop4HotkeyKey, settings.Desktop4Hotkey);
            return builder.ToString();
        }

        private static void AppendHotkey(StringBuilder builder, string key, HotkeyBinding binding)
        {
            builder.Append(key);
            builder.Append('=');
            builder.AppendLine(binding == null || binding.IsEmpty ? string.Empty : binding.ToDisplayString());
        }

        private static void ApplyHotkey(AppSettings settings, int desktopIndex, string value)
        {
            HotkeyBinding binding;

            if (HotkeyBinding.TryParse(value, out binding))
            {
                settings.SetHotkey(desktopIndex, binding);
            }
        }

        private static bool ParseBoolean(string value, bool defaultValue)
        {
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return defaultValue;
        }

        private static int ParseInteger(string value, int defaultValue)
        {
            int parsedValue;

            if (int.TryParse(value, out parsedValue))
            {
                return parsedValue;
            }

            return defaultValue;
        }

        private static void WriteAllText(string path, string content)
        {
            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(path, content, Encoding.UTF8);
        }
    }
}
