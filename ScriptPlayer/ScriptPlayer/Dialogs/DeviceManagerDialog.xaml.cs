using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for DeviceManagerDialog.xaml
    /// </summary>
    public partial class DeviceManagerDialog : Window
    {
        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof(ObservableCollection<Device>), typeof(DeviceManagerDialog), new PropertyMetadata(default(ObservableCollection<Device>)));

        public ObservableCollection<Device> Devices
        {
            get { return (ObservableCollection<Device>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        public DeviceManagerDialog(ObservableCollection<Device> devices)
        {
            Devices = devices;

            InitializeComponent();
        }

        private void btnFunjack_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/funjack/");
        }
    }
}
