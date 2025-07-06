using iNKORE.UI.WPF.Modern;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SilverMation
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadTheme();
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedTheme = selectedItem.Content.ToString();
                RegistryHelper.SaveTheme(selectedTheme);

                ThemeManager.Current.ApplicationTheme = RegistryHelper.GetApplicationTheme();
                Application.Current.Resources["PrimaryTextColor"] = RegistryHelper.GetPrimaryTextColor();
            }
        }

        private void LoadTheme()
        {
            string savedTheme = RegistryHelper.LoadTheme();

            switch (savedTheme)
            {
                case "Light":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    ThemeComboBox.SelectedIndex = 0;
                    break;
                case "Dark":
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    ThemeComboBox.SelectedIndex = 1;
                    break;
                case "Use system setting":
                default:
                    ThemeManager.Current.ApplicationTheme = RegistryHelper.GetApplicationTheme();
                    ThemeComboBox.SelectedIndex = 2;
                    break;
            }

            Application.Current.Resources["PrimaryTextColor"] = RegistryHelper.GetPrimaryTextColor();
        }
    }
}
