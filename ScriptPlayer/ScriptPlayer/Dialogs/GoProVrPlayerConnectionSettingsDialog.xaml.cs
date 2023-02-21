using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for VlcConnectionSettingsDialog.xaml
    /// </summary>
    public partial class GoProVrPlayerConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty UdpPortProperty = DependencyProperty.Register(
            "UdpPort", typeof(int), typeof(GoProVrPlayerConnectionSettingsDialog), new PropertyMetadata(5000));

        public int UdpPort
        {
            get => (int) GetValue(UdpPortProperty);
            set => SetValue(UdpPortProperty, value);
        }

        public GoProVrPlayerConnectionSettingsDialog(int udpPort)
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
