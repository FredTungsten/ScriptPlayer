using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
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
            get => (ObservableCollection<Device>) GetValue(DevicesProperty);
            set => SetValue(DevicesProperty, value);
        }

        public static readonly DependencyProperty FallbackListProperty = DependencyProperty.Register(
            "FallbackList", typeof(List<Device>), typeof(DeviceManagerDialog), new PropertyMetadata(default(List<Device>)));

        public List<Device> FallbackList
        {
            get => (List<Device>) GetValue(FallbackListProperty);
            set => SetValue(FallbackListProperty, value);
        }

        public DeviceManagerDialog(ObservableCollection<Device> devices)
        {
            FallbackList = new List<Device>
            {
                new FakeDevice("No Devices connected :(")
            };

            Devices = devices;

            InitializeComponent();
        }

        private void btnFunjack_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/funjack/");
        }
    }

    public class FakeDevice : Device
    {
        public FakeDevice(string name)
        {
            Name = name;
        }

        public override Task Set(DeviceCommandInformation information)
        {
            throw new System.NotImplementedException();
        }

        public override Task Set(IntermediateCommandInformation information)
        {
            throw new System.NotImplementedException();
        }

        public override void Stop()
        {
            throw new System.NotImplementedException();
        }
    }
}
