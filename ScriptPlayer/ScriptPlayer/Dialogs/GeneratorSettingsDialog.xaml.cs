using System;
using System.Linq;
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

        public GeneratorSettingsDialog(MainViewModel viewModel, GeneratorSettingsViewModel initialValues, GeneratedElements availableElements, GeneratedElements initialElement)
        {
            ViewModel = viewModel;
            Settings = initialValues ?? new GeneratorSettingsViewModel();

            InitializeComponent();

            tabThumbnails.IsEnabled = availableElements.HasFlag(GeneratedElements.Thumbnails);
            tabThumbnailBanner.IsEnabled = availableElements.HasFlag(GeneratedElements.ThumbnailBanner);
            tabPreview.IsEnabled = availableElements.HasFlag(GeneratedElements.Preview);
            tabHeatmap.IsEnabled = availableElements.HasFlag(GeneratedElements.Heatmap);

            switch (initialElement)
            {
                case GeneratedElements.Thumbnails:
                    tabControl.SelectedItem = tabThumbnails;
                    break;
                case GeneratedElements.ThumbnailBanner:
                    tabControl.SelectedItem = tabThumbnailBanner;
                    break;
                case GeneratedElements.Preview:
                    tabControl.SelectedItem = tabPreview;
                    break;
                case GeneratedElements.Heatmap:
                    tabControl.SelectedItem = tabHeatmap;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(initialElement), initialElement, null);
            }
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

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.HasErrors(out string[] errors))
            {
                MessageBox.Show("Some of your settings are not ok:\n" + string.Join("\n", errors.Select(err => " - " + err)), "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }

    [Flags]
    public enum GeneratedElements
    {
        Thumbnails = 1,
        ThumbnailBanner = 2,
        Preview = 4,
        Heatmap = 8,

        All = Thumbnails | ThumbnailBanner | Preview | Heatmap
    }
}
