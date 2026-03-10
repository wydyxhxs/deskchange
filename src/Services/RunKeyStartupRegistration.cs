using System;
using Microsoft.Win32;

namespace DeskChange.Services
{
    internal sealed class RunKeyStartupRegistration : IStartupRegistration
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupArgument = "--background";

        private readonly string _valueName;

        public RunKeyStartupRegistration(string valueName)
        {
            if (string.IsNullOrEmpty(valueName))
            {
                throw new ArgumentException("A startup value name is required.", "valueName");
            }

            _valueName = valueName;
        }

        public void Disable()
        {
            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (runKey != null)
                {
                    runKey.DeleteValue(_valueName, false);
                }
            }
        }

        public void Enable(string exePath, bool startHidden)
        {
            if (string.IsNullOrEmpty(exePath))
            {
                throw new ArgumentException("An executable path is required.", "exePath");
            }

            using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                if (runKey == null)
                {
                    throw new InvalidOperationException("Unable to open the current user Run registry key.");
                }

                runKey.SetValue(_valueName, BuildStartupCommand(exePath, startHidden), RegistryValueKind.String);
            }
        }

        public bool IsEnabled()
        {
            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
            {
                if (runKey == null)
                {
                    return false;
                }

                object value = runKey.GetValue(_valueName);
                return value is string && !string.IsNullOrEmpty((string)value);
            }
        }

        private static string BuildStartupCommand(string exePath, bool startHidden)
        {
            if (startHidden)
            {
                return "\"" + exePath + "\" " + StartupArgument;
            }

            return "\"" + exePath + "\"";
        }
    }
}
