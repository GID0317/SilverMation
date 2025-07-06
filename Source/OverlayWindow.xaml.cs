using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;

namespace SilverMation
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OverlayWindow_Loaded;
            Closed += OverlayWindow_Closed;
            MakeFullScreen();
        }

        private void OverlayWindow_Closed(object sender, EventArgs e)
        {
            //HomePage.ResetOverlayWindow();
        }

        private void MakeFullScreen()
        {
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;
        }

        public void HideOverlayWindow()
        {
            this.Hide();
        }

        public void UpdateText(string text)
        {
            UpdateText(text, (Brush)new BrushConverter().ConvertFromString("#FFFFFF")); // Default color
        }

        public void UpdateText(string text, Brush color, string logLevel = "INF")
        {
            string currentTime = DateTime.Now.ToString("HH:mm:ss");

            // Determine the color for the log level
            Brush logLevelColor;
            switch (logLevel)
            {
                case "WRN":
                    logLevelColor = (Brush)new BrushConverter().ConvertFromString("#e9ed02");
                    break;
                case "ERR":
                    logLevelColor = (Brush)new BrushConverter().ConvertFromString("#ce4539");
                    break;
                default:
                    logLevelColor = (Brush)new BrushConverter().ConvertFromString("#df9337");
                    break;
            }

            Run timeRun = new Run($"[{currentTime} ") { Foreground = (Brush)new BrushConverter().ConvertFromString("#a7b3b0") };
            Run logLevelRun = new Run($"{logLevel}") { Foreground = logLevelColor, FontWeight = FontWeights.SemiBold };
            Run closingBracketRun = new Run("] ") { Foreground = (Brush)new BrushConverter().ConvertFromString("#a7b3b0") };
            TextBlock textBlock = new TextBlock();
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.Inlines.Add(timeRun);
            textBlock.Inlines.Add(logLevelRun);
            textBlock.Inlines.Add(closingBracketRun);

            // Split the text into parts to highlight specific parts
            string[] parts = text.Split(new char[] { '{', '}', '\'', '\"', '(', ')' });
            for (int i = 0; i < parts.Length; i++)
            {
                if (i % 2 == 1) // Highlighted part
                {
                    Run highlightRun = new Run(parts[i]) { Foreground = (Brush)new BrushConverter().ConvertFromString("#dbc291") };
                    textBlock.Inlines.Add(highlightRun);
                }
                else // Regular part
                {
                    Run regularRun = new Run(parts[i]) { Foreground = color };
                    textBlock.Inlines.Add(regularRun);
                }
            }

            LogStackPanel.Children.Add(textBlock);

            // Scroll to the bottom
            LogScrollViewer.ScrollToEnd();
        }

        public void LogApplicationVersion(string version)
        {
            UpdateText($"SilverMation Build {{{version}}}", (Brush)new BrushConverter().ConvertFromString("#FFFFFF"));
        }

        public void LogScreenResolution(int width, int height)
        {
            UpdateText($"Started monitoring with Screen Width and Height {{{width}}}X{{{height}}}", (Brush)new BrushConverter().ConvertFromString("#FFFFFF"));
        }

        public void LogButtonClickSuccess()
        {
            UpdateText("Button click successful", (Brush)new BrushConverter().ConvertFromString("#18ffcb"));
        }

        public void LogButtonClickFailure()
        {
            UpdateText("Button click failed", (Brush)new BrushConverter().ConvertFromString("#FF0000"));
        }

        public void LogScreenshotSuccess()
        {
            UpdateText("Successfully initiating the screenshot test. Now starting the automation system.", (Brush)new BrushConverter().ConvertFromString("#18ffcb"));
        }

        public void LogScreenshotFailure()
        {
            UpdateText("Screenshot failed", (Brush)new BrushConverter().ConvertFromString("#FF0000"));
        }

        private void OverlayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
    }
}
