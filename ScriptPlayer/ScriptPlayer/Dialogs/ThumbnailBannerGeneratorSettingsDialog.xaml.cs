using System.Windows;
using ScriptPlayer.Generators;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ThumbnailBannerGeneratorSettingsDialog.xaml
    /// </summary>
    public partial class ThumbnailBannerGeneratorSettingsDialog : Window
    {
        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(ThumbnailBannerGeneratorSettings), typeof(ThumbnailBannerGeneratorSettingsDialog), new PropertyMetadata(default(ThumbnailBannerGeneratorSettings)));

        public ThumbnailBannerGeneratorSettings Settings
        {
            get => (ThumbnailBannerGeneratorSettings) GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public ThumbnailBannerGeneratorSettingsDialog(ThumbnailBannerGeneratorSettings initialSettings)
        {
            Settings = initialSettings?.Clone() ?? new ThumbnailBannerGeneratorSettings();
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PreviewThumbnailBanner(Settings);
        }

        private void PreviewThumbnailBanner(ThumbnailBannerGeneratorSettings settings)
        {
            new ThumbnailBannerGeneratorSettingsPreviewDialog(settings).ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
