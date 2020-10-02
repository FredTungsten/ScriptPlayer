using System;
using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.Generators;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for PreviewGeneratorSettings.xaml
    /// </summary>
    public partial class PreviewGeneratorSettingsDialog : Window
    {
        public static readonly DependencyProperty FrameRateProperty = DependencyProperty.Register(
            "FrameRate", typeof(double), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(double)));

        public double FrameRate
        {
            get => (double)GetValue(FrameRateProperty);
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
            get => (TimeSpan) GetValue(DurationEachProperty);
            set => SetValue(DurationEachProperty, value);
        }

        public static readonly DependencyProperty SectionCountProperty = DependencyProperty.Register(
            "SectionCount", typeof(int), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(12));

        public int SectionCount
        {
            get => (int) GetValue(SectionCountProperty);
            set => SetValue(SectionCountProperty, value);
        }

        public static readonly DependencyProperty SkipExistingProperty = DependencyProperty.Register(
            "SkipExisting", typeof(bool), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(bool)));

        public bool SkipExisting
        {
            get { return (bool) GetValue(SkipExistingProperty); }
            set { SetValue(SkipExistingProperty, value); }
        }

        public static readonly DependencyProperty ClipLeftProperty = DependencyProperty.Register(
            "ClipLeft", typeof(bool), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(bool)));

        public bool ClipLeft
        {
            get { return (bool) GetValue(ClipLeftProperty); }
            set { SetValue(ClipLeftProperty, value); }
        }

        public static readonly DependencyProperty OverlayScriptPositionsProperty = DependencyProperty.Register(
            "OverlayScriptPositions", typeof(bool), typeof(PreviewGeneratorSettingsDialog), new PropertyMetadata(default(bool)));

        public bool OverlayScriptPositions
        {
            get { return (bool) GetValue(OverlayScriptPositionsProperty); }
            set { SetValue(OverlayScriptPositionsProperty, value); }
        }

        public PreviewGeneratorSettingsDialog(PreviewGeneratorSettings initialSettings)
        {
            if(initialSettings == null)
                initialSettings = new PreviewGeneratorSettings();
            
            FrameWidth = initialSettings.Width;
            FrameAutoWidth = initialSettings.Width <= 0;
            FrameHeight = initialSettings.Height;
            FrameAutoHeight = initialSettings.Height <= 0;
            FrameRate = initialSettings.Framerate;
            SkipExisting = initialSettings.SkipIfExists;
            ClipLeft = initialSettings.ClipLeft;
            OverlayScriptPositions = initialSettings.OverlayScriptPositions;

            InitializeComponent();

            if (initialSettings.TimeFrames.Count != 1)
            {
                Start = TimeSpan.Zero;
                Duration = TimeSpan.FromSeconds(5);
            }
            else
            {
                Start = initialSettings.TimeFrames[0].StartTimeSpan;
                Duration = initialSettings.TimeFrames[0].Duration;

                rbSingleSection.IsChecked = true;
            }
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
                SkipIfExists = SkipExisting,
                ClipLeft = ClipLeft,
                OverlayScriptPositions = OverlayScriptPositions,
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
    }
}
