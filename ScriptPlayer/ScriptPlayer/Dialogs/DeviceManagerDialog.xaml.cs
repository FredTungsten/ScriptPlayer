using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Devices.Interfaces;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for DeviceManagerDialog.xaml
    /// </summary>
    public partial class DeviceManagerDialog : Window
    {
        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof(ObservableCollection<IDevice>), typeof(DeviceManagerDialog), new PropertyMetadata(default(ObservableCollection<IDevice>)));

        public ObservableCollection<IDevice> Devices
        {
            get => (ObservableCollection<IDevice>) GetValue(DevicesProperty);
            set => SetValue(DevicesProperty, value);
        }

        public DeviceManagerDialog(ObservableCollection<IDevice> devices)
        {
            Devices = devices;
            InitializeComponent();
        }

        private void BtnFunjack_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/funjack/");
        }
    }
}
