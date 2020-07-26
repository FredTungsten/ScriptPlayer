using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class ZoomPlayerConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty IpAndPortProperty = DependencyProperty.Register(
            "IpEndpoint", typeof(string), typeof(ZoomPlayerConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string IpAndPort
        {
            get { return (string) GetValue(IpAndPortProperty); }
            set { SetValue(IpAndPortProperty, value); }
        }

        public ZoomPlayerConnectionSettingsDialog(string ipAndPort)
        {
            InitializeComponent();

            txtIpPort.Text = ipAndPort;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

            IpAndPort = txtIpPort.Text;

            DialogResult = true;
        }
    }
}
