using System;
using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for PreviewGeneratorSettings.xaml
    /// </summary>
    public partial class PreviewGeneratorSettingsDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            "FrameRate", typeof(int), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameRate
        {
            get => (int)GetValue(FrameRateProperty);
            set => SetValue(FrameRateProperty, value);
        }

        public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register(
            "FrameWidth", typeof(int), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameWidth
        {
            get => (int)GetValue(FrameWidthProperty);
            set => SetValue(FrameWidthProperty, value);
        }

        public static readonly DependencyProperty FrameHeightProperty = DependencyProperty.Register(
            "FrameHeight", typeof(int), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(int)));

        public int FrameHeight
        {
            get => (int)GetValue(FrameHeightProperty);
            set => SetValue(FrameHeightProperty, value);
        }

        public static readonly DependencyProperty FrameAutoHeightProperty = DependencyProperty.Register(
            "FrameAutoHeight", typeof(bool), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(bool), OnFrameAutoHeightPropertyChanged));

        private static void OnFrameAutoHeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                ((PreviewGeneratorSettingsDialog)d).FrameAutoWidth = false;
        }

        public bool FrameAutoHeight
        {
            get => (bool)GetValue(FrameAutoHeightProperty);
            set => SetValue(FrameAutoHeightProperty, value);
        }

        public static readonly DependencyProperty FrameAutoWidthProperty = DependencyProperty.Register(
            "FrameAutoWidth", typeof(bool), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(bool), OnFrameAutoWidthPropertyChanged));

        private static void OnFrameAutoWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                ((PreviewGeneratorSettingsDialog)d).FrameAutoHeight = false;
        }

        public bool FrameAutoWidth
        {
            get => (bool)GetValue(FrameAutoWidthProperty);
            set => SetValue(FrameAutoWidthProperty, value);
        }

        public static readonly DependencyProperty StartProperty = DependencyProperty.Register(
            "Start", typeof(TimeSpan), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Start
        {
            get => (TimeSpan) GetValue(StartProperty);
            set => SetValue(StartProperty, value);
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Duration
        {
            get => (TimeSpan) GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly DependencyProperty VideoPathProperty = DependencyProperty.Register(
            "VideoPath", typeof(string), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(string)));

        public string VideoPath
        {
            get => (string) GetValue(VideoPathProperty);
            set => SetValue(VideoPathProperty, value);
        }

        public static readonly DependencyProperty GifPathProperty = DependencyProperty.Register(
            "GifPath", typeof(string), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(string)));

        public string GifPath
        {
            get => (string) GetValue(GifPathProperty);
            set => SetValue(GifPathProperty, value);
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(PreviewGeneratorSettings), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(PreviewGeneratorSettings)));

        public PreviewGeneratorSettings Result
        {
            get => (PreviewGeneratorSettings)GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        public PreviewGeneratorSettingsDialog(PreviewGeneratorSettings initialSettings)
        {
            VideoPath = initialSettings.Video;
            GifPath = initialSettings.Destination;

            FrameWidth = initialSettings.Width;
            FrameAutoWidth = initialSettings.Width <= 0;
            FrameHeight = initialSettings.Height;
            FrameAutoHeight = initialSettings.Height <= 0;
            FrameRate = initialSettings.Framerate;
            Start = initialSettings.Start;
            Duration = initialSettings.Duration;   

            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

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

            if (FrameRate <= 1)
            {
                MessageBox.Show("Framerate must be greater than 1!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Result = new PreviewGeneratorSettings
            {
                Height = FrameAutoHeight ? -1 : FrameHeight,
                Width = FrameAutoWidth ? -1 : FrameWidth,
                Framerate = FrameRate,
                Start = Start,
                Duration = Duration,
                Destination = GifPath,
                Video = VideoPath
            };

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnSelectVideo_Click(object sender, RoutedEventArgs e)
        {
            string video = ViewModel.GetVideoFileOpenDialog();
            if (!string.IsNullOrEmpty(video))
                VideoPath = video;
        }

        private void BtnSelectGif_Click(object sender, RoutedEventArgs e)
        {
            string gif = ViewModel.GetGifFileSaveDialog();
            if (!string.IsNullOrEmpty(gif))
                GifPath = gif;
        }
    }
}
