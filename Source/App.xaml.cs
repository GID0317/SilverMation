using iNKORE.UI.WPF.Modern;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace SilverMation
{
    public partial class App : Application
    {
        private static Mutex mutex = null;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "SilverMationUniqueAppName";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // If the mutex already exists, bring the existing instance to the foreground
                Process currentProcess = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }
                Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // Apply the theme based on the saved theme or system setting
            ThemeManager.Current.ApplicationTheme = RegistryHelper.GetApplicationTheme();

            // Apply the text color based on the theme
            Resources["PrimaryTextColor"] = RegistryHelper.GetPrimaryTextColor();
        }
    }
}
