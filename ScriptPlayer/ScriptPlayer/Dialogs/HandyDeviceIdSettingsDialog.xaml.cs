using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for HandyDeviceIdSettingsDialog.xaml
    /// </summary>
    public partial class HandyDeviceIdSettingsDialog : Window
    {
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
            "DeviceId", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string DeviceId
        {
            get => (string)GetValue(DeviceIdProperty);
            set => SetValue(DeviceIdProperty, value);
        }

        public static readonly DependencyProperty LocalIpProperty = DependencyProperty.Register(
            "LocalIP", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string LocalIp
        {
            get => (string)GetValue(LocalIpProperty);
            set => SetValue(LocalIpProperty, value);
        }

        public static readonly DependencyProperty PortProperty = DependencyProperty.Register(
            "HttpPort", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string Port
        {
            get => (string)GetValue(PortProperty);
            set => SetValue(PortProperty, value);
        }

        public HandyDeviceIdSettingsDialog(string currentId, string localIp, string port, bool enableLanOverride)
        {
            InitializeComponent();
            DeviceId = currentId;
            LocalIp = localIp;
            Port = port;

            deviceId.Text = currentId;

            localIpOverride.Text = localIp;
            localIpOverride.IsEnabled = enableLanOverride;

            portOverride.Text = port;
            portOverride.IsEnabled = enableLanOverride;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            DeviceId = deviceId.Text;
            LocalIp = localIpOverride.Text;
            Port = portOverride.Text;
            DialogResult = true;
        }
    }
}
