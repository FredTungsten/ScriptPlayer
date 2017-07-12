using System.Windows;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ButtplugConnectionSettingsDialog.xaml
    /// </summary>
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
    }
}
