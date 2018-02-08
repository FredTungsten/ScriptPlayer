using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class SamsungVrConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty UdpPortProperty = DependencyProperty.Register(
            "UdpPort", typeof(int), typeof(SamsungVrConnectionSettingsDialog), new PropertyMetadata(5000));

        public int UdpPort
        {
            get { return (int) GetValue(UdpPortProperty); }
            set { SetValue(UdpPortProperty, value); }
        }

        public SamsungVrConnectionSettingsDialog(int udpPort)
        {
            UdpPort = udpPort;
            InitializeComponent();
        }

        private void BtnOk_OnClick(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
            DialogResult = true;
        }
    }
}
