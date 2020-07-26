using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class SimpleTcpConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty IpAndPortProperty = DependencyProperty.Register(
            "IpEndpoint", typeof(string), typeof(SimpleTcpConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string IpAndPort
        {
            get => (string)GetValue(IpAndPortProperty);
            set => SetValue(IpAndPortProperty, value);
        }

        public SimpleTcpConnectionSettingsDialog(string ipAndPort)
        {
            InitializeComponent();

            txtIpPort.Text = ipAndPort;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();

            if (!SimpleTcpConnectionSettings.Parse(txtIpPort.Text, out string _, out int _))
            {
                MessageBox.Show(
                      "Invalid Input. Make sure you enter a hostname or IP and Port, e.g. localhost:1234 or 127.0.0.1:1234",
                      "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IpAndPort = txtIpPort.Text;

            DialogResult = true;
        }
    }
}
