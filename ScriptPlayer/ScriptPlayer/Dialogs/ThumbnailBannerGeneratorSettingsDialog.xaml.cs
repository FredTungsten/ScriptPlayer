using System.Windows;

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
    }

    public class ThumbnailBannerGeneratorSettings
    {
        public int Rows { get; set; }

        public int Columns { get; set; }

        public int TotalWidth { get; set; }

        public string Video { get; set; }

        public ThumbnailBannerGeneratorSettings()
        {
            Rows = 5;
            Columns = 4;
            TotalWidth = 1024;
        }

        public ThumbnailBannerGeneratorSettings Clone()
        {
            return new ThumbnailBannerGeneratorSettings
            {
                Rows = Rows,
                Columns = Columns,
                TotalWidth = TotalWidth
            };
        }
    }
}
