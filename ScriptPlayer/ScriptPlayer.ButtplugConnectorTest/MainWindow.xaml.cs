using System.Windows;

namespace ScriptPlayer.ButtplugConnectorTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ButtplugConnector.ButtplugWebSocketConnector _connector;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            _connector = new ButtplugConnector.ButtplugWebSocketConnector();
            await _connector.Connect();
        }

        private async void btnStartScanning_Click(object sender, RoutedEventArgs e)
        {
            await _connector.StartScanning();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await _connector.SetPosition(99, 30);
        }
    }
}
