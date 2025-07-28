using System;
using System.Windows;
using System.Windows.Media.Imaging;
using iNKORE.UI.WPF.Modern.Controls;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Net;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using System.Diagnostics;
using iNKORE.UI.WPF.Modern.Media.Animation;

namespace SilverMation
{
    public partial class MainWindow : Window
    {
        private HomePage _homePage;
        private SettingsPage _settingsPage;

        public MainWindow()
        {
            InitializeComponent();
            NavView.SelectionChanged += NavView_SelectionChanged;

            // Set the window icon
            Uri iconUri = new Uri("pack://application:,,,/Assets/IMG_SilverMation_Icon.ico", UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            // Initialize pages
            _homePage = new HomePage();
            _settingsPage = new SettingsPage();

            // Navigate to HomePage on startup
            ContentFrame.Navigate(_homePage, new EntranceNavigationTransitionInfo());

            // Handle the Closed event
            this.Closed += MainWindow_Closed;

            // Check for updates
            CheckForUpdates();
        }

        private async Task<string> FetchLatestVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://raw.githubusercontent.com/GID0317/SilverMation/main/UpdateHelper/Version.config";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string latestVersion = await response.Content.ReadAsStringAsync();
                return latestVersion.Trim();
            }
        }

        private bool IsUpdateAvailable(string currentVersion, string latestVersion)
        {
            // Clean up the version strings to handle any whitespace or newlines
            currentVersion = currentVersion.Trim();
            latestVersion = latestVersion.Trim();

            if (Version.TryParse(currentVersion, out Version current) && 
                Version.TryParse(latestVersion, out Version latest))
            {
                // Compare major, minor, build, and revision numbers
                if (current.Major != latest.Major) return current.Major < latest.Major;
                if (current.Minor != latest.Minor) return current.Minor < latest.Minor;
                if (current.Build != latest.Build) return current.Build < latest.Build;
                if (current.Revision != latest.Revision) return current.Revision < latest.Revision;
                return false; // Versions are equal
            }

            Debug.WriteLine($"Failed to parse versions - Current: {currentVersion}, Latest: {latestVersion}");
            return false; // If we can't parse the versions, don't show update
        }

        private string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private async void CheckForUpdates()
        {
            if (IsInternetAvailable())
            {
                string currentVersion = GetCurrentVersion();
                string latestVersion = await FetchLatestVersionAsync();

                Debug.WriteLine($"Current Version: {currentVersion}");
                Debug.WriteLine($"Latest Version: {latestVersion}");

                Version current = new Version(currentVersion);
                Version latest = new Version(latestVersion);
                Debug.WriteLine($"Version Comparison Result: {current.CompareTo(latest)}");

                if (IsUpdateAvailable(currentVersion, latestVersion))
                {
                    Debug.WriteLine("Update is available, showing InfoBar");
                    // Show the InfoBar on the HomePage
                    _homePage.ShowUpdateInfoBar();
                }
                else
                {
                    Debug.WriteLine("No update needed");
                }
            }
            else
            {
                Debug.WriteLine("Application Started on Offline Environment. Skipping update check");
            }
        }

        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send("www.google.com");
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(_settingsPage, new EntranceNavigationTransitionInfo());
            }
            else if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag)
                {
                    case "Start":
                        ContentFrame.Navigate(_homePage, new EntranceNavigationTransitionInfo());
                        break;
                }
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            HomePage.CloseOverlayWindow();
            _homePage.Dispose();
            Application.Current.Shutdown();
        }
    }
}
