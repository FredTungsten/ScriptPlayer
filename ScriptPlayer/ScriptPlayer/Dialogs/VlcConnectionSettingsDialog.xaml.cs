using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class VlcConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty IpAndPortProperty = DependencyProperty.Register(
            "IpAndPort", typeof(string), typeof(VlcConnectionSettingsDialog), new PropertyMetadata(default(string)));

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
