using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for FramePicture.xaml
    /// </summary>
    public partial class FramePicture : Window
    {
        public FramePicture(ImageSource source)
        {
            InitializeComponent();
            Img.Source = source;
        }

        public static void ShowImage(ImageSource source)
        {
            new FramePicture(source).Show();
        }
    }
}
