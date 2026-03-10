using System;
using System.Threading;
using System.Windows.Forms;
using DeskChange.Services;

namespace DeskChange
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = @"Local\DeskChange.SingleInstance";

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            bool startHidden = ShouldStartHidden(args);

            using (Mutex mutex = new Mutex(true, SingleInstanceMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                DeskChangeApplicationContext context = null;
                string errorMessage = null;

                try
                {
                    AppLog.Info("Launching DeskChange.");
                    context = new DeskChangeApplicationContext(startHidden);
                    Application.Run(context);
                }
                catch (Exception ex)
                {
                    AppLog.Error("Unhandled startup error.", ex);
                    errorMessage = ex.Message;
                }
                finally
                {
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MessageBox.Show(
                        errorMessage,
                        "DeskChange 启动失败",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private static bool ShouldStartHidden(string[] args)
        {
            if (args == null)
            {
                return false;
            }

            int index;

            for (index = 0; index < args.Length; index++)
            {
                if (string.Equals(args[index], "--background", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
