using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for SerialPortSelector.xaml
    /// </summary>
    public partial class SerialPortSelector : Window
    {
        public static readonly DependencyProperty PortsProperty = DependencyProperty.Register(
            "Ports", typeof(List<string>), typeof(SerialPortSelector), new PropertyMetadata(default(List<string>)));

        public List<string> Ports
        {
            get { return (List<string>) GetValue(PortsProperty); }
            set { SetValue(PortsProperty, value); }
        }

        public static readonly DependencyProperty SelectedPortProperty = DependencyProperty.Register(
            "SelectedPort", typeof(string), typeof(SerialPortSelector), new PropertyMetadata(default(string)));

        public string SelectedPort
        {
            get { return (string) GetValue(SelectedPortProperty); }
            set { SetValue(SelectedPortProperty, value); }
        }

        public SerialPortSelector(List<string> ports)
        {
            Ports = ports;

            SelectedPort = Ports.FirstOrDefault();

            InitializeComponent();
        }

        public SerialPortSelector(string[] ports)
        {
            List<string> conv = new List<string>();
            foreach (string str in ports) conv.Add(str);

            Ports = conv;

            SelectedPort = Ports.FirstOrDefault();

            InitializeComponent();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPort == null)
                return;

            DialogResult = true;
        }
    }
}
