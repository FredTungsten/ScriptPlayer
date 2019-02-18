using ScriptPlayer.Shared;
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
    /// Interaction logic for KodiConnectionSettingsDialog.xaml
    /// </summary>
    public partial class KodiConnectionSettingsDialog : Window
    {

        public static readonly DependencyProperty IpProperty = DependencyProperty.Register(
            "Ip", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string Ip
        {
            get { return (string)GetValue(IpProperty); }
            set { SetValue(IpProperty, value); }
        }

        public static readonly DependencyProperty HttpPortProperty = DependencyProperty.Register(
            "HttpPort", typeof(int), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(int)));

        public int HttpPort
        {
            get { return (int)GetValue(HttpPortProperty); }
            set { SetValue(HttpPortProperty, value); }
        }
        public static readonly DependencyProperty TcpPortProperty = DependencyProperty.Register(
            "TcpPort", typeof(int), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(int)));

        public int TcpPort
        {
            get { return (int)GetValue(TcpPortProperty); }
            set { SetValue(TcpPortProperty, value); }
        }

        public static readonly DependencyProperty UserProperty = DependencyProperty.Register(
            "User", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string User
        {
            get { return (string)GetValue(UserProperty); }
            set { SetValue(UserProperty, value); }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            "Password", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set { SetValue(PasswordProperty, value); }
        }

        public KodiConnectionSettingsDialog(string ip, int http_port, int tcp_port, string user, string password)
        {
            InitializeComponent();
            txtIp.Text = ip;
            txtHttpPort.Text = http_port.ToString();
            txtTcpPort.Text = tcp_port.ToString();
            txtUser.Text = user;
            txtPassword.Password = password;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();

            Ip = txtIp.Text;

            int http_port;
            if(int.TryParse(txtHttpPort.Text, out http_port))
            {
                HttpPort = http_port;
            }
            else
            {
                HttpPort = KodiConnectionSettings.DefaultHttpPort;
            }

            int tcp_port;
            if (int.TryParse(txtTcpPort.Text, out tcp_port))
            {
                TcpPort = tcp_port;
            }
            else
            {
                TcpPort = KodiConnectionSettings.DefaultTcpPort;
            }

            User = txtUser.Text;
            Password = txtPassword.Password;

            DialogResult = true;
        }
    }
}
