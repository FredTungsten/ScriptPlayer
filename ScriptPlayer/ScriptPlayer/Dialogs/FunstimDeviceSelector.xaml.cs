using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NAudio.Wave;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for FunstimDeviceSelector.xaml
    /// </summary>
    public partial class FunstimDeviceSelector : Window
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

        public FunstimDeviceSelector(List<DirectSoundDeviceInfo> devices)
        {
            Devices = devices;

            SelectedDevice = Devices.FirstOrDefault();

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDevice == null)
                return;

            DialogResult = true;
        }
    }
}
