using System.Windows;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ThumbnailGeneratorSettingsDialog.xaml
    /// </summary>
    public partial class ThumbnailGeneratorSettingsDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get => (MainViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty FrameIntervallProperty = DependencyProperty.Register(
            "FrameIntervall", typeof(int), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameIntervall
        {
            get => (int) GetValue(FrameIntervallProperty);
            set => SetValue(FrameIntervallProperty, value);
        }

        public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register(
            "FrameWidth", typeof(int), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameWidth
        {
            get => (int) GetValue(FrameWidthProperty);
            set => SetValue(FrameWidthProperty, value);
        }

        public static readonly DependencyProperty FrameHeightProperty = DependencyProperty.Register(
            "FrameHeight", typeof(int), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameHeight
        {
            get => (int) GetValue(FrameHeightProperty);
            set => SetValue(FrameHeightProperty, value);
        }

        public static readonly DependencyProperty FrameAutoHeightProperty = DependencyProperty.Register(
            "FrameAutoHeight", typeof(bool), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(bool), OnFrameAutoHeightPropertyChanged));

        private static void OnFrameAutoHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue)
                ((ThumbnailGeneratorSettingsDialog) d).FrameAutoWidth = false;
        }

        public bool FrameAutoHeight
        {
            get => (bool) GetValue(FrameAutoHeightProperty);
            set => SetValue(FrameAutoHeightProperty, value);
        }

        public static readonly DependencyProperty FrameAutoWidthProperty = DependencyProperty.Register(
            "FrameAutoWidth", typeof(bool), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(bool), OnFrameAutoWidthPropertyChanged));

        private static void OnFrameAutoWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                ((ThumbnailGeneratorSettingsDialog)d).FrameAutoHeight = false;
        }

        public bool FrameAutoWidth
        {
            get => (bool) GetValue(FrameAutoWidthProperty);
            set => SetValue(FrameAutoWidthProperty, value);
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(ThumbnailGeneratorSettings), typeof(ThumbnailGeneratorSettingsDialog), new PropertyMetadata(default(ThumbnailGeneratorSettings)));

        public ThumbnailGeneratorSettings Result
        {
            get { return (ThumbnailGeneratorSettings) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public ThumbnailGeneratorSettingsDialog(MainViewModel viewModel, ThumbnailGeneratorSettings initialValues)
        {
            ViewModel = viewModel;
            InitializeComponent();

            if (initialValues != null)
            {
                FrameWidth = initialValues.Width;
                if (FrameWidth <= 0)
                    FrameAutoWidth = true;

                FrameHeight = initialValues.Height;
                if (FrameHeight <= 0)
                    FrameAutoHeight = true;

                FrameIntervall = initialValues.Intervall;
            }
            else
            {
                FrameWidth = 200;
                FrameHeight = -1;

                FrameAutoWidth = false;
                FrameAutoHeight = true;

                FrameIntervall = 10;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (FrameWidth <= 0 && !FrameAutoWidth)
            {
                MessageBox.Show("Width must be greater than zero or automatic!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (FrameHeight <= 0 && !FrameAutoHeight)
            {
                MessageBox.Show("Height must be greater than zero or automatic!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (FrameIntervall <= 0)
            {
                MessageBox.Show("Intervall must be greater than zero!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Result = new ThumbnailGeneratorSettings
            {
                Height = FrameAutoHeight ? -1 : FrameHeight,
                Width = FrameAutoWidth ? -1 : FrameWidth,
                Intervall = FrameIntervall
            };

            DialogResult = true;
        }
    }

    public class ThumbnailGeneratorSettings
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int Intervall { get; set; }

        public string[] Videos { get; set; }

        public ThumbnailGeneratorSettings DuplicateWithoutVideos()
        {
            return new ThumbnailGeneratorSettings
            {
                Width =  Width,
                Height = Height,
                Intervall = Intervall
            };
        }
    }
}
