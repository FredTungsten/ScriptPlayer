using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NAudio.Wave;
using ScriptPlayer.Shared.Devices;
using ScriptPlayer.Shared.Estim;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for AudioDeviceSelector.xaml
    /// </summary>
    public partial class AudioDeviceSelector : Window
    {
        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof(List<DirectSoundDeviceInfo>), typeof(AudioDeviceSelector), new PropertyMetadata(default(List<DirectSoundDeviceInfo>)));

        public List<DirectSoundDeviceInfo> Devices
        {
            get { return (List<DirectSoundDeviceInfo>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        public static readonly DependencyProperty SelectedDeviceProperty = DependencyProperty.Register(
            "SelectedDevice", typeof(DirectSoundDeviceInfo), typeof(AudioDeviceSelector), new PropertyMetadata(default(DirectSoundDeviceInfo)));

        public DirectSoundDeviceInfo SelectedDevice
        {
            get { return (DirectSoundDeviceInfo) GetValue(SelectedDeviceProperty); }
            set { SetValue(SelectedDeviceProperty, value); }
        }

        public static readonly DependencyProperty ModesProperty = DependencyProperty.Register(
            "Modes", typeof(List<EstimConversionMode>), typeof(AudioDeviceSelector), new PropertyMetadata(default(List<EstimConversionMode>)));

        public List<EstimConversionMode> Modes
        {
            get { return (List<EstimConversionMode>) GetValue(ModesProperty); }
            set { SetValue(ModesProperty, value); }
        }

        public static readonly DependencyProperty SelectedModeProperty = DependencyProperty.Register(
            "SelectedMode", typeof(EstimConversionMode), typeof(AudioDeviceSelector), new PropertyMetadata(default(EstimConversionMode)));

        public EstimConversionMode SelectedMode
        {
            get { return (EstimConversionMode) GetValue(SelectedModeProperty); }
            set { SetValue(SelectedModeProperty, value); }
        }

        public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
            "Parameters", typeof(EstimParameters), typeof(AudioDeviceSelector), new PropertyMetadata(default(EstimParameters)));

        public EstimParameters Parameters
        {
            get { return (EstimParameters) GetValue(ParametersProperty); }
            set { SetValue(ParametersProperty, value); }
        }

        public AudioDeviceSelector(List<DirectSoundDeviceInfo> devices)
        {
            Devices = devices;

            SelectedDevice = Devices.FirstOrDefault();

            Modes = new List<EstimConversionMode>
            {
                EstimConversionMode.Volume,
                EstimConversionMode.Balance,
                EstimConversionMode.Frequency
            };

            SelectedMode = Modes.FirstOrDefault();

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDevice == null)
                return;

            Parameters = new EstimParameters
            {
                ConversionMode = SelectedMode
            };

            DialogResult = true;
        }
    }
}
