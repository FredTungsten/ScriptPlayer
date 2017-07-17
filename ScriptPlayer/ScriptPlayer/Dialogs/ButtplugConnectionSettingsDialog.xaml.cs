using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    public partial class ButtplugConnectionSettingsDialog : Window
    {
        public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(
            "Url", typeof(string), typeof(ButtplugConnectionSettingsDialog), new PropertyMetadata(default(string)));

        public string Url
        {
            get { return (string) GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }
        public ButtplugConnectionSettingsDialog()
        {
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUrl.Focus();
            txtUrl.SelectAll();
        }
    }
}
