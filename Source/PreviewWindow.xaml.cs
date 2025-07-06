using System.Windows;
using System.Windows.Media.Imaging;

namespace SilverMation
{
    public partial class PreviewWindow : Window
    {
        public PreviewWindow()
        {
            InitializeComponent();
        }

        public void UpdateImage(BitmapImage bitmapImage)
        {
            PreviewImage.Source = bitmapImage;
        }
    }
}
