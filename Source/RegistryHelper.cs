using Microsoft.Win32;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern;
using Windows.UI.ViewManagement;

namespace SilverMation
{
    public static class RegistryHelper
    {
        private const string RegistryKeyPath = @"Software\SilverMation";
        private const string ThemeValueName = "AppTheme";
        private const string ExecutablePathValueName = "ExecutablePath";
        private const string IntervalValueName = "Interval";
        private const string AutoShowPreviewValueName = "AutoShowPreview";

        public static void SaveSettings(string executablePath, int interval, bool autoShowPreview)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                if (!string.IsNullOrEmpty(executablePath))
                {
                    key.SetValue(ExecutablePathValueName, executablePath);
                }
                key.SetValue(IntervalValueName, interval);
                key.SetValue(AutoShowPreviewValueName, autoShowPreview ? 1 : 0);
            }
        }

        public static void SaveTheme(string theme)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key.SetValue(ThemeValueName, theme);
            }
        }

        public static string LoadTheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(ThemeValueName, "Use system setting") as string;
            }
        }

        public static string LoadExecutablePath()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(ExecutablePathValueName) as string;
            }
        }

        public static int LoadInterval()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key != null ? (int)key.GetValue(IntervalValueName, 50) : 50;
            }
        }

        public static bool LoadAutoShowPreview()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                if (key != null)
                {
                    var value = key.GetValue(AutoShowPreviewValueName, "0").ToString();
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        return boolValue;
                    }
                    else if (int.TryParse(value, out int intValue))
                    {
                        return intValue == 1;
                    }
                }
                return false;
            }
        }

        public static bool IsSystemThemeDark()
        {
            var uiSettings = new UISettings();
            var backgroundColor = uiSettings.GetColorValue(UIColorType.Background);
            return backgroundColor.R < 128 && backgroundColor.G < 128 && backgroundColor.B < 128;
        }

        public static ApplicationTheme GetApplicationTheme()
        {
            string savedTheme = LoadTheme();

            switch (savedTheme)
            {
                case "Light":
                    return ApplicationTheme.Light;
                case "Dark":
                    return ApplicationTheme.Dark;
                case "Use system setting":
                default:
                    return IsSystemThemeDark() ? ApplicationTheme.Dark : ApplicationTheme.Light;
            }
        }

        public static SolidColorBrush GetPrimaryTextColor()
        {
            var theme = GetApplicationTheme();
            return theme == ApplicationTheme.Dark ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);
        }
    }
}
