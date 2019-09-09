using System;
using System.Windows;
using ScriptPlayer.Generators;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for GeneratorSettingsDialog.xaml
    /// </summary>
    public partial class GeneratorSettingsDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(GeneratorSettingsDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get => (MainViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(GeneratorSettingsViewModel), typeof(GeneratorSettingsDialog), new PropertyMetadata(default(GeneratorSettingsViewModel)));

        public GeneratorSettingsViewModel Settings
        {
            get => (GeneratorSettingsViewModel) GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public GeneratorSettingsDialog(MainViewModel viewModel, GeneratorSettingsViewModel initialValues, GeneratedElements elements)
        {
            ViewModel = viewModel;
            Settings = initialValues ?? new GeneratorSettingsViewModel();

            InitializeComponent();

            tabThumbnails.IsEnabled = elements.HasFlag(GeneratedElements.Thumbnails);
            tabThumbnailBanner.IsEnabled = elements.HasFlag(GeneratedElements.ThumbnailBanner);
            tabPreview.IsEnabled = elements.HasFlag(GeneratedElements.Preview);
            tabHeatmap.IsEnabled = elements.HasFlag(GeneratedElements.Heatmap);
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            ThumbnailBannerGeneratorSettings settings = new ThumbnailBannerGeneratorSettings
            {
                Columns = Settings.Banner.Columns,
                Rows = Settings.Banner.Rows,
                TotalWidth = Settings.Banner.TotalWidth
            };

            var dialog = new ThumbnailBannerGeneratorSettingsPreviewDialog(settings) {Owner = this};
            dialog.ShowDialog();
        }
    }

    [Flags]
    public enum GeneratedElements
    {
        Thumbnails = 1,
        ThumbnailBanner = 2,
        Preview = 4,
        Heatmap = 8
    }
}
