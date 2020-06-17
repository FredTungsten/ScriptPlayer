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
    /// Interaction logic for HandyDeviceIdSettingsDialog.xaml
    /// </summary>
    public partial class HandyDeviceIdSettingsDialog : Window
    {
        public static readonly DependencyProperty DeviceIdProperty = DependencyProperty.Register(
            "DeviceId", typeof(string), typeof(KodiConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        public HandyDeviceIdSettingsDialog(string currentId)
        {
            InitializeComponent();
            DeviceId = currentId;
            deviceId.Text = currentId;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            DeviceId = deviceId.Text;
            DialogResult = true;
        }
    }
}
