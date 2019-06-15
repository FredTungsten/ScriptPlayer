using System;
using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.Generators;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Helpers;
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

        public static readonly DependencyProperty DurationEachProperty = DependencyProperty.Register(
            "DurationEach", typeof(TimeSpan), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(TimeSpan.FromSeconds(0.8)));

        public TimeSpan DurationEach
        {
            get { return (TimeSpan) GetValue(DurationEachProperty); }
            set { SetValue(DurationEachProperty, value); }
        }

        public static readonly DependencyProperty SectionCountProperty = DependencyProperty.Register(
            "SectionCount", typeof(int), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(12));

        public int SectionCount
        {
            get { return (int) GetValue(SectionCountProperty); }
            set { SetValue(SectionCountProperty, value); }
        }

        public PreviewGeneratorSettingsDialog(MainViewModel viewModel, PreviewGeneratorSettings initialSettings)
        {
            ViewModel = viewModel;
            VideoPath = initialSettings.VideoFile;
            GifPath = initialSettings.OutputFile;

            FrameWidth = initialSettings.Width;
            FrameAutoWidth = initialSettings.Width <= 0;
            FrameHeight = initialSettings.Height;
            FrameAutoHeight = initialSettings.Height <= 0;
            FrameRate = initialSettings.Framerate;
            Start = initialSettings.TimeFrames[0].StartTimeSpan;
            Duration = initialSettings.TimeFrames[0].Duration;   

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
                Height = FrameAutoHeight ? -2 : FrameHeight,
                Width = FrameAutoWidth ? -2 : FrameWidth,
                Framerate = FrameRate,
                OutputFile = GifPath,
                VideoFile = VideoPath
            };

            if (rbMultiSections.IsChecked == true)
            {
                Result.GenerateRelativeTimeFrames(SectionCount, DurationEach);
            }
            else
            { 
                Result.TimeFrames.Add(new TimeFrame
                {
                    StartTimeSpan = Start,
                    Duration = Duration
                });
            }

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
