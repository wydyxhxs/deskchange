using System;
using System.Diagnostics;
using System.IO;

namespace DeskChange.Services
{
    internal sealed class VirtualDesktopCliSwitcher : IVirtualDesktopSwitcher
    {
        private const int ProcessTimeoutMilliseconds = 5000;

        private readonly string _helperExePath;

        public VirtualDesktopCliSwitcher(string helperExePath)
        {
            if (string.IsNullOrEmpty(helperExePath))
            {
                throw new ArgumentException("A helper executable path is required.", "helperExePath");
            }

            if (!File.Exists(helperExePath))
            {
                throw new FileNotFoundException("Virtual desktop helper executable was not found.", helperExePath);
            }

            _helperExePath = helperExePath;
        }

        public int CreateDesktop()
        {
            HelperExecutionResult result = RunHelper("/New");
            return ReadDesktopIndex(result, "Unable to determine the created desktop index.");
        }

        public int GetDesktopCount()
        {
            HelperExecutionResult result = RunHelper("/Count");

            if (result.ExitCode > 0)
            {
                return result.ExitCode;
            }

            int parsedCount;

            if (TryParseTrailingInteger(result.StandardOutput, out parsedCount))
            {
                return parsedCount;
            }

            throw new InvalidOperationException(
                "Unable to determine the virtual desktop count. Output: " + result.StandardOutput);
        }

        public int GetCurrentDesktopIndex()
        {
            HelperExecutionResult result = RunHelper("/GetCurrentDesktop");
            return ReadDesktopIndex(result, "Unable to determine the current desktop index.");
        }

        public void RemoveDesktop(int desktopIndex)
        {
            if (desktopIndex < 0)
            {
                throw new ArgumentOutOfRangeException("desktopIndex");
            }

            HelperExecutionResult result = RunHelper("/Remove:" + desktopIndex);

            if (result.ExitCode < 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Virtual desktop helper failed while removing desktop {0}. Output: {1} Error: {2}",
                        desktopIndex,
                        result.StandardOutput,
                        result.StandardError));
            }
        }

        public void SwitchToDesktop(int desktopIndex, bool enableAnimation)
        {
            if (desktopIndex < 0)
            {
                throw new ArgumentOutOfRangeException("desktopIndex");
            }

            string arguments = string.Format(
                "/Animation:{0} /Switch:{1}",
                enableAnimation ? "On" : "Off",
                desktopIndex);

            HelperExecutionResult result = RunHelper(arguments);

            if (result.ExitCode < 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Virtual desktop helper failed with exit code {0}. Output: {1} Error: {2}",
                        result.ExitCode,
                        result.StandardOutput,
                        result.StandardError));
            }
        }

        private HelperExecutionResult RunHelper(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = _helperExePath;
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = Path.GetDirectoryName(_helperExePath);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                string standardOutput = process.StandardOutput.ReadToEnd().Trim();
                string standardError = process.StandardError.ReadToEnd().Trim();

                if (!process.WaitForExit(ProcessTimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                    }

                    throw new TimeoutException("Virtual desktop helper timed out.");
                }

                if (!string.IsNullOrWhiteSpace(standardOutput))
                {
                    AppLog.Info("Helper output: " + standardOutput);
                }

                if (!string.IsNullOrWhiteSpace(standardError))
                {
                    AppLog.Info("Helper error output: " + standardError);
                }

                HelperExecutionResult result = new HelperExecutionResult();
                result.ExitCode = process.ExitCode;
                result.StandardOutput = standardOutput;
                result.StandardError = standardError;
                return result;
            }
        }

        private static int ReadDesktopIndex(HelperExecutionResult result, string errorMessage)
        {
            int parsedIndex;

            if (TryParseTrailingInteger(result.StandardOutput, out parsedIndex))
            {
                return parsedIndex;
            }

            if (result.ExitCode >= 0)
            {
                return result.ExitCode;
            }

            throw new InvalidOperationException(errorMessage + " Output: " + result.StandardOutput);
        }

        private static bool TryParseTrailingInteger(string text, out int value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            int endIndex = text.Length - 1;

            while (endIndex >= 0 && !char.IsDigit(text[endIndex]))
            {
                endIndex--;
            }

            if (endIndex < 0)
            {
                return false;
            }

            int startIndex = endIndex;

            while (startIndex >= 0 && char.IsDigit(text[startIndex]))
            {
                startIndex--;
            }

            return int.TryParse(
                text.Substring(startIndex + 1, endIndex - startIndex),
                out value);
        }

        private sealed class HelperExecutionResult
        {
            public int ExitCode { get; set; }

            public string StandardError { get; set; }

            public string StandardOutput { get; set; }
        }
    }
}
