using System.Windows;
using System.Windows.Media;
using ScriptPlayer.Generators;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ThumbnailBannerGeneratorSettingsPreviewDialog.xaml
    /// </summary>
    public partial class ThumbnailBannerGeneratorSettingsPreviewDialog : Window
    {
        public static readonly DependencyProperty ThumbnailBannerProperty = DependencyProperty.Register(
            "ThumbnailBanner", typeof(ImageSource), typeof(ThumbnailBannerGeneratorSettingsPreviewDialog), new PropertyMetadata(default(ImageSource)));

        public ImageSource ThumbnailBanner
        {
            get => (ImageSource)GetValue(ThumbnailBannerProperty);
            set => SetValue(ThumbnailBannerProperty, value);
        }

        public ThumbnailBannerGeneratorSettingsPreviewDialog(ThumbnailBannerGeneratorSettings settings)
        {
            InitializeComponent();
            ThumbnailBanner = ThumbnailBannerGenerator.CreatePreview(settings);
        }
    }
}
