using System;
using System.Diagnostics;
using System.IO;

namespace DeskChange.Services
{
    internal static class AppLog
    {
        private static readonly object SyncRoot = new object();
        private static readonly string LogDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskChange");
        private static readonly string LogFilePathValue = Path.Combine(LogDirectoryPath, "deskchange.log");

        public static string LogDirectory
        {
            get { return LogDirectoryPath; }
        }

        public static string LogFilePath
        {
            get { return LogFilePathValue; }
        }

        public static void Error(string message, Exception exception)
        {
            Write("ERROR", message + Environment.NewLine + exception);
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        private static void Write(string level, string message)
        {
            string line = string.Format(
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}",
                DateTime.Now,
                level,
                message);

            lock (SyncRoot)
            {
                Directory.CreateDirectory(LogDirectoryPath);
                File.AppendAllText(LogFilePathValue, line + Environment.NewLine);
            }

            Trace.WriteLine(line);
        }
    }
}
