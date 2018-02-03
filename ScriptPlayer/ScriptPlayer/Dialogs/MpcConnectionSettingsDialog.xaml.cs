using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class MpcConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty IpAndPortProperty = DependencyProperty.Register(
            "IpAndPort", typeof(string), typeof(MpcConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string IpAndPort
        {
            get { return (string) GetValue(IpAndPortProperty); }
            set { SetValue(IpAndPortProperty, value); }
        }

        public MpcConnectionSettingsDialog(string ipAndPort)
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
