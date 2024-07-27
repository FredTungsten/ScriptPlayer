using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.Shared.Scripts;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for BeatConversionSettingsDialog.xaml
    /// </summary>
    public partial class BeatConversionSettingsDialog : Window
    {
        public event EventHandler ResultChanged;

        public static readonly DependencyProperty ConversionModesProperty = DependencyProperty.Register(
            "ConversionModes", typeof(List<ConversionMode>), typeof(BeatConversionSettingsDialog), new PropertyMetadata(default(List<ConversionMode>)));

        public List<ConversionMode> ConversionModes
        {
            get => (List<ConversionMode>) GetValue(ConversionModesProperty);
            set => SetValue(ConversionModesProperty, value);
        }

        public static readonly DependencyProperty ConversionModeProperty = DependencyProperty.Register(
            "ConversionMode", typeof(ConversionMode), typeof(BeatConversionSettingsDialog), new PropertyMetadata(default(ConversionMode)));

        public ConversionMode ConversionMode
        {
            get => (ConversionMode) GetValue(ConversionModeProperty);
            set => SetValue(ConversionModeProperty, value);
        }

        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
            "MinValue", typeof(byte), typeof(BeatConversionSettingsDialog), new PropertyMetadata(default(byte)));

        public byte MinValue
        {
            get => (byte) GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            "MaxValue", typeof(byte), typeof(BeatConversionSettingsDialog), new PropertyMetadata(default(byte)));

        public byte MaxValue
        {
            get => (byte) GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(ConversionSettings), typeof(BeatConversionSettingsDialog), new PropertyMetadata(default(ConversionSettings)));
        
        public ConversionSettings Result
        {
            get => (ConversionSettings) GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        public BeatConversionSettingsDialog(ConversionSettings initialValues = null)
        {
            ConversionModes = Enum.GetValues(typeof(ConversionMode))
                    .Cast<ConversionMode>()
                    .Except(new[]{ConversionMode.Custom})
                    .ToList();

            if (initialValues == null)
            {
                ConversionMode = ConversionMode.UpOrDown;
                MinValue = 0;
                MaxValue = 99;
            }
            else
            {
                ConversionMode = initialValues.Mode;
                MinValue = initialValues.Min;
                MaxValue = initialValues.Max;
            }

            InitializeComponent();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

            UpdateResult();
            
            DialogResult = true;
        }

        private void UpdateResult()
        {
            Result = new ConversionSettings
            {
                Min = MinValue,
                Max = MaxValue,
                Mode = ConversionMode
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MinValue = 0;
            MaxValue = 99;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MinValue = 50;
            MaxValue = 99;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MinValue = 0;
            MaxValue = 50;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateResult();
            OnResultChanged();
        }

        protected virtual void OnResultChanged()
        {
            ResultChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BeatConversionSettingsDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            OnResultChanged();
        }
    }
}
