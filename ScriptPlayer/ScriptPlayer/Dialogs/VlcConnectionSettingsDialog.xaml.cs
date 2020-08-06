using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class VlcConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty IpAndPortProperty = DependencyProperty.Register(
            "IpEndpoint", typeof(string), typeof(VlcConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string IpAndPort
        {
            get { return (string) GetValue(IpAndPortProperty); }
            set { SetValue(IpAndPortProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            "Password", typeof(string), typeof(VlcConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string Password
        {
            get { return (string) GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public VlcConnectionSettingsDialog(string ipAndPort, string password)
        {
            InitializeComponent();

            txtIpPort.Text = ipAndPort;
            txtPassword.Password = password;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

            Password = txtPassword.Password;
            IpAndPort = txtIpPort.Text;

            DialogResult = true;
        }
    }
}
