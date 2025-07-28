using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Drawing = System.Drawing;
using Imaging = System.Drawing.Imaging;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows.Media;
using System.Reflection;
using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using System.Linq;

namespace SilverMation
{
    public partial class HomePage : UserControl
    {
        private static OverlayWindow _overlayWindow;
        private PaddleOcrService _ocrService;
        private DispatcherTimer _timer;
        private int _interval = 50; // Default interval in milliseconds
        private IntPtr _targetWindowHandle;
        private string _selectedExecutablePath;
        private PreviewWindow _previewWindow;

        public HomePage()
        {
            InitializeComponent();
            _ocrService = new PaddleOcrService();
            InitializeTimer();
            LoadSavedSettings();
        }

        private void LoadSavedSettings()
        {
            _selectedExecutablePath = RegistryHelper.LoadExecutablePath();
            if (!string.IsNullOrEmpty(_selectedExecutablePath))
            {
                _targetWindowHandle = FindWindowByFilePath(_selectedExecutablePath);
                ExeLocationCard.Description = _selectedExecutablePath;
                ExeLocation.Text = "";
            }

            _interval = RegistryHelper.LoadInterval();
            IntervalTextBox.Text = _interval.ToString();

            AutoShowPreviewToggleSwitch.IsOn = RegistryHelper.LoadAutoShowPreview();
        }

        private void SaveSettings()
        {
            if (AutoShowPreviewToggleSwitch != null)
            {
                RegistryHelper.SaveSettings(_selectedExecutablePath, _interval, AutoShowPreviewToggleSwitch.IsOn);
            }
        }


        public void ShowUpdateInfoBar()
        {
            UpdateInfobar.IsOpen = true;
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer = null;
            _ocrService = null;
            _overlayWindow?.Close();
            _overlayWindow = null;
            _previewWindow?.Close();
            _previewWindow = null;
        }

        private void ShowOverlayWindow()
        {
            if (_overlayWindow == null)
            {
                _overlayWindow = new OverlayWindow();
                _overlayWindow.Show();
            }
            else
            {
                _overlayWindow.Show();
            }
        }

        public static void ResetOverlayWindow()
        {
            _overlayWindow = null;
        }

        public IntPtr GetTargetWindowHandle()
        {
            return _targetWindowHandle;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(_interval);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("Timer ticked");
            if (!IsTargetExecutableRunning())
            {
                _timer.Stop();
                ShowProcessStoppedDialog();
                StartMonitoringToggleButton.IsChecked = false;
                return;
            }

            DetectAndClickButton();
        }

        private async void ShowProcessStoppedDialog()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Process Stopped",
                    Content = "The target executable has stopped running. Monitoring has been stopped.",
                    CloseButtonText = "OK"
                };

                dialog.Loaded += (s, args) =>
                {
                    var border = (Border)VisualTreeHelper.GetChild(dialog, 0);
                    var panel = (Grid)VisualTreeHelper.GetChild(border, 0);
                    panel.Margin = new Thickness(0);
                };

                await dialog.ShowAsync();
            });
        }

        private async void StartMonitoringToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            StartMonitoringToggleButtonIcon.Glyph = "\uE71A";
            StartMonitoringToggleButtonText.Text = "Stop";

            if (string.IsNullOrEmpty(_selectedExecutablePath))
            {
                var dialog = new ContentDialog
                {
                    Title = "Select Target HonkaiStarRail.exe",
                    Content = "Please select the HonkaiStarRail.exe target first before starting.",
                    PrimaryButtonText = "Browse",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Filter = "HonkaiStarRail|StarRail.exe|All Files (*.*)|*.*"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        _selectedExecutablePath = openFileDialog.FileName;
                        _targetWindowHandle = FindWindowByFilePath(_selectedExecutablePath);
                        SaveSettings();
                        Debug.WriteLine($"Selected file path: {_selectedExecutablePath}");
                        Debug.WriteLine($"Target window handle: {_targetWindowHandle}");

                        ExeLocation.Text = _selectedExecutablePath;
                    }
                }

                StartMonitoringToggleButton.IsEnabled = true;
                StartMonitoringToggleButton.IsChecked = false;
                return;
            }

            if (!IsTargetExecutableRunning())
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "The target HonkaiStarRail.exe is not running. Please start the game and try again.",
                    CloseButtonText = "OK"
                };

                dialog.Loaded += (s, args) =>
                {
                    var border = (Border)VisualTreeHelper.GetChild(dialog, 0);
                    var panel = (Grid)VisualTreeHelper.GetChild(border, 0);
                    panel.Margin = new Thickness(0);
                };

                await dialog.ShowAsync();
                StartMonitoringToggleButton.IsEnabled = true;
                StartMonitoringToggleButton.IsChecked = false;
                return;
            }

            ShowOverlayWindow();
            // Log application version and screen resolution
            string appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
            _overlayWindow.LogApplicationVersion(appVersion);
            _overlayWindow.LogScreenResolution(screenWidth, screenHeight);

            // Capture the specific window once to log screenshot success or failure
            Drawing.Bitmap screenCapture = WindowCapture.CaptureWindow(_targetWindowHandle);
            if (screenCapture != null)
            {
                _overlayWindow.LogScreenshotSuccess();
            }
            else
            {
                _overlayWindow.LogScreenshotFailure();
            }

            _timer.Start();
            StartMonitoringToggleButton.IsEnabled = true;
        }

        private void StartMonitoringToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            StartMonitoringToggleButtonIcon.Glyph = "\uE768";
            StartMonitoringToggleButtonText.Text = "Start";

            _timer.Stop();
            if (_previewWindow != null && _previewWindow.IsVisible)
            {
                _previewWindow.Hide();
            }

            if (_overlayWindow != null && _overlayWindow.IsVisible)
            {
                _overlayWindow.HideOverlayWindow();
            }
        }

        private bool IsTargetExecutableRunning()
        {
            return _targetWindowHandle != IntPtr.Zero && Process.GetProcesses().Any(p => p.MainWindowHandle == _targetWindowHandle);
        }

        private void IntervalTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(IntervalTextBox.Text, out int interval))
            {
                _interval = interval;
                if (_timer != null)
                {
                    _timer.Interval = TimeSpan.FromMilliseconds(_interval);
                }
                SaveSettings();
            }
        }

        private async void SelectTargetExecutable_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                var dialog = new ContentDialog
                {
                    Title = "Warning",
                    Content = "Automation is still running. Please stop it before changing the HonkaiStarRail.exe target.",
                    CloseButtonText = "OK"
                };

                await dialog.ShowAsync();
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "HonkaiStarRail|StarRail.exe|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedExecutablePath = openFileDialog.FileName;
                _targetWindowHandle = FindWindowByFilePath(_selectedExecutablePath);
                SaveSettings();
                Debug.WriteLine($"Selected file path: {_selectedExecutablePath}");
                Debug.WriteLine($"Target window handle: {_targetWindowHandle}");

                // Update the card description with the full path and clear the ExeLocation text
                ExeLocationCard.Description = _selectedExecutablePath;
                ExeLocation.Text = "";
            }
        }

        private IntPtr FindWindowByFilePath(string filePath)
        {
            var processName = Path.GetFileNameWithoutExtension(filePath);
            Debug.WriteLine($"Process name: {processName}");

            var processes = Process.GetProcessesByName(processName);
            Debug.WriteLine($"Number of processes found: {processes.Length}");

            var process = processes.FirstOrDefault();
            if (process != null)
            {
                Debug.WriteLine($"Process ID: {process.Id}");
                Debug.WriteLine($"Main window handle: {process.MainWindowHandle}");
            }
            else
            {
                Debug.WriteLine("Process not found.");
            }

            return process?.MainWindowHandle ?? IntPtr.Zero;
        }

        private void AutoShowPreviewToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        public void CheckLanguageAndWarn(string text)
        {
            // Detect the language of the text
            string detectedLanguage = DetectLanguage(text);

            // Display a warning if the detected language is not English
            if (detectedLanguage != "en")
            {
                _overlayWindow.UpdateText("Warning: The game language is not set to English. Please note that some functions may not work properly.", System.Windows.Media.Brushes.Orange, "WRN");
            }
        }

        public string DetectLanguage(string text)
        {
            // Simple language detection based on common characters
            if (text.Any(c => c >= 0x4E00 && c <= 0x9FFF)) // Chinese characters
            {
                return "zh";
            }
            else if (text.Any(c => c >= 0xAC00 && c <= 0xD7AF)) // Korean characters
            {
                return "ko";
            }
            else if (text.Any(c => c >= 0x3040 && c <= 0x30FF)) // Japanese characters
            {
                return "ja";
            }
            else if (text.Contains("Tiếng Việt"))
            {
                return "vi";
            }
            else if (text.Contains("Bahasa Indonesia") || text.Contains("Tantang") || text.Contains("Pertempuran Dimulai"))
            {
                return "id";
            }
            else if (text.Contains("Français"))
            {
                return "fr";
            }
            else if (text.Contains("Deutsch"))
            {
                return "de";
            }
            else if (text.Contains("Português"))
            {
                return "pt";
            }
            else if (text.Contains("Español"))
            {
                return "es";
            }
            else if (text.Contains("Русский"))
            {
                return "ru";
            }
            else if (text.Contains("ภาษาไทย"))
            {
                return "th";
            }
            else if (text.Contains("简体中文"))
            {
                return "zh-Hans";
            }
            else if (text.Contains("繁體中文"))
            {
                return "zh-Hant";
            }
            else
            {
                return "en"; // Default to English
            }
        }

        private void DetectAndClickButton()
        {
            if (_targetWindowHandle == IntPtr.Zero)
            {
                return;
            }

            // Capture the specific window
            using (var screenCapture = WindowCapture.CaptureWindow(_targetWindowHandle))
            {
                try
                {
                    // Perform OCR to detect text within the button area
                    using (var memoryStream = new MemoryStream())
                    {
                        screenCapture.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
                        memoryStream.Position = 0; // Reset the position to the beginning

                        using (var mat = Mat.FromStream(memoryStream, ImreadModes.Color))
                        {
                            var ocrResult = _ocrService.OcrResult(mat);
                            var text = ocrResult.Text;
                            Debug.WriteLine("OCR Text: " + text); // Add this line to debug OCR text

                            // Check the language and display a warning if necessary
                            CheckLanguageAndWarn(text);

                            if (!string.IsNullOrEmpty(text) && text.Contains("Challenge Completed"))
                            {
                                Debug.WriteLine("Challenge Completed detected. Focusing on button area.");

                                // Extract Trailblazer Power from the top right
                                string trailblazerPower = ExtractTrailblazerPower(text);
                                Debug.WriteLine("Trailblazer Power: " + trailblazerPower);

                                // Extract Required Power using the '×' symbol
                                string requiredPower = ExtractRequiredPower(text, mat);
                                Debug.WriteLine("Required Power: " + requiredPower);

                                // Check Trailblazer Power
                                if (int.TryParse(trailblazerPower, out int availablePower) && int.TryParse(requiredPower, out int neededPower))
                                {
                                    if (availablePower < neededPower)
                                    {
                                        _overlayWindow.UpdateText($"Trailblazer power is lower than required ({neededPower}). Please replenish your power.");
                                        Debug.WriteLine($"Trailblazer power is lower than required ({neededPower}). Please replenish your power.");
                                        return;
                                    }
                                    else
                                    {
                                        _overlayWindow.UpdateText("Sufficient Trailblazer power detected, Detecting One More Time Button Location. Proceeding 'One More Time'.");
                                        Debug.WriteLine("Trailblazer power is sufficient. Proceeding with 'One More Time'.");
                                    }
                                }

                                // Focus on the button area to find the "One More Time" button
                                var buttonRect = FindButtonRectangle(screenCapture, text);
                                if (buttonRect != System.Drawing.Rectangle.Empty)
                                {
                                    MoveCursorAndClick(buttonRect);
                                    //_overlayWindow.UpdateText($"Clicked button at {buttonRect.Location}");
                                    //_overlayWindow.LogButtonClickSuccess(); // Log button click success
                                }
                                else
                                {
                                    //_overlayWindow.LogButtonClickFailure(); // Log button click failure
                                    Debug.WriteLine("LogButtonClickFailure Phase2");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("LogButtonClickFailure Phase1");
                            }
                        }

                        // Display the captured image in the Preview Window if Auto Show is enabled
                        if (AutoShowPreviewToggleSwitch.IsOn == true)
                        {
                            ShowPreviewWindow();
                        }

                        if (_previewWindow != null && _previewWindow.IsVisible)
                        {
                            try
                            {
                                // Display the captured image
                                var bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.EndInit();
                                _previewWindow.UpdateImage(bitmapImage);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error displaying captured image: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during OCR processing: {ex.Message}");
                    _overlayWindow.LogButtonClickFailure(); // Log button click failure in case of exception
                }
            }
        }

        private System.Drawing.Rectangle FindButtonRectangle(System.Drawing.Bitmap screenCapture, string buttonText)
        {
            // Define the approximate area where the button is located based on reference texts
            int buttonAreaTop = (int)(screenCapture.Height * 0.85);
            int buttonAreaBottom = screenCapture.Height;
            int buttonAreaLeft = (int)(screenCapture.Width * 0.6);
            int buttonAreaRight = screenCapture.Width;

            // Check if the reference texts are detected
            if (buttonText.Contains("One More Time"))
            {
                // Calculate the position of the "One More Time" button based on reference texts
                int oneMoreTimeIndex = buttonText.IndexOf("One More Time");

                // Assuming the "One More Time" button is located in the bottom right area
                buttonAreaLeft = (int)(screenCapture.Width * 0.6); // Adjust as needed
                buttonAreaTop = (int)(screenCapture.Height * 0.85); // Adjust as needed

                // Return the rectangle of the button area
                return new System.Drawing.Rectangle(buttonAreaLeft, buttonAreaTop, 200, 50); // Adjust width and height as needed
            }

            // Return an empty rectangle if the button is not found
            return System.Drawing.Rectangle.Empty;
        }

        private void MoveCursorAndClick(System.Drawing.Rectangle buttonRect)
        {
            // Get the window's position and size
            WindowCapture.GetWindowRect(_targetWindowHandle, out WindowCapture.RECT windowRect);
            WindowCapture.GetClientRect(_targetWindowHandle, out WindowCapture.RECT clientRect);

            // Log the window and client rect sizes
            Debug.WriteLine($"Window Rect: Left={windowRect.Left}, Top={windowRect.Top}, Right={windowRect.Right}, Bottom={windowRect.Bottom}");
            Debug.WriteLine($"Client Rect: Left={clientRect.Left}, Top={clientRect.Top}, Right={clientRect.Right}, Bottom={clientRect.Bottom}");

            // Calculate the absolute position of the button
            int x = windowRect.Left + buttonRect.X + buttonRect.Width / 2;
            int y = windowRect.Top + buttonRect.Y + buttonRect.Height / 2;

            // Adjust for the client area offset
            int clientWidth = clientRect.Right - clientRect.Left;
            int clientHeight = clientRect.Bottom - clientRect.Top;
            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            x += (windowWidth - clientWidth) / 2;
            y += (windowHeight - clientHeight) - (windowWidth - clientWidth) / 2;

            // Log the calculated cursor position
            Debug.WriteLine($"Calculated Cursor Position: X={x}, Y={y}");

            //_overlayWindow.UpdateText($"Setting cursor position to: ({x}, {y})");
            Debug.WriteLine($"Setting cursor position to: ({x}, {y})");
            SetCursorPos(x, y);
            _overlayWindow.UpdateText("Simulating mouse click.");
            Debug.WriteLine("Simulating mouse click.");
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
        }

        private void ShowPreviewWindow()
        {
            if (_previewWindow == null || !_previewWindow.IsVisible)
            {
                _previewWindow = new PreviewWindow();
                _previewWindow.Show();
            }
        }

        private async void ShowPreview_Click(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                ShowPreviewWindow();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "The target executable is not running. Please start the executable and try again.",
                    CloseButtonText = "OK"
                };

                dialog.Loaded += (s, args) =>
                {
                    var border = (Border)VisualTreeHelper.GetChild(dialog, 0);
                    var panel = (Grid)VisualTreeHelper.GetChild(border, 0);
                    panel.Margin = new Thickness(0);
                };

                await dialog.ShowAsync();
            }
        }

        private string ExtractTrailblazerPower(string ocrText)
        {
            // Extract the trailblazer power from the top right
            var lines = ocrText.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("/"))
                {
                    var parts = line.Split('/');
                    if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int power))
                    {
                        return parts[0].Trim();
                    }
                }
            }
            return "0";
        }

        private string ExtractRequiredPower(string ocrText, Mat mat)
        {
            // Split the OCR text into lines
            var lines = ocrText.Split('\n');
            Debug.WriteLine("OCR Text Lines:");
            foreach (var line in lines)
            {
                Debug.WriteLine(line);
            }

            foreach (var line in lines)
            {
                // Check if the line contains the required power values directly
                if (line.Trim() == "10" || line.Trim() == "40")
                {
                    Debug.WriteLine($"Line containing required power: {line}");
                    if (int.TryParse(line.Trim(), out int requiredPower))
                    {
                        Debug.WriteLine($"Parsed required power: {requiredPower}");
                        // Check if the required power is highlighted in red
                        if (IsRequiredPowerRed(mat, requiredPower))
                        {
                            Debug.WriteLine($"Required power {requiredPower} is highlighted in red (insufficient).");
                            return requiredPower.ToString();
                        }
                        else
                        {
                            Debug.WriteLine($"Required power {requiredPower} is highlighted in black (sufficient).");
                            return requiredPower.ToString();
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to parse required power from: {line.Trim()}");
                    }
                }
            }
            return "0";
        }

        private bool IsRequiredPowerRed(Mat mat, int requiredPower)
        {
            // Define the color range for red
            Scalar lowerRed = new Scalar(0, 0, 100);
            Scalar upperRed = new Scalar(50, 50, 255);

            // Convert the image to HSV color space
            Mat hsvMat = new Mat();
            Cv2.CvtColor(mat, hsvMat, ColorConversionCodes.BGR2HSV);

            // Create a mask for the red color
            Mat mask = new Mat();
            Cv2.InRange(hsvMat, lowerRed, upperRed, mask);

            // Find contours in the mask
            Cv2.FindContours(mask, out Point[][] contours, out HierarchyIndex[] hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            // Check if any contour matches the required power area
            foreach (var contour in contours)
            {
                Rect boundingRect = Cv2.BoundingRect(contour);
                Debug.WriteLine($"Bounding Rect: {boundingRect}");
                if (boundingRect.Contains(new Point(requiredPower * 10, mat.Height - 50))) // Adjust the position as needed
                {
                    Debug.WriteLine($"Required power {requiredPower} is highlighted in red.");
                    return true;
                }
            }

            Debug.WriteLine($"Required power {requiredPower} is not highlighted in red.");
            return false;
        }

        private void InfoBarButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/GID0317/SilverMation";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        public static void CloseOverlayWindow()
        {
            if (_overlayWindow != null)
            {
                _overlayWindow.Close();
                _overlayWindow = null;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
    }

    public class WindowCapture
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static Drawing.Bitmap CaptureWindow(IntPtr hWnd)
        {
            GetWindowRect(hWnd, out RECT rect);
            Drawing.Rectangle bounds = new Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            Drawing.Bitmap screenshot = new Drawing.Bitmap(bounds.Width, bounds.Height, Imaging.PixelFormat.Format32bppArgb);

            using (Drawing.Graphics graphics = Drawing.Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(bounds.Location, Drawing.Point.Empty, bounds.Size, Drawing.CopyPixelOperation.SourceCopy);
            }

            return screenshot;
        }
    }
}
