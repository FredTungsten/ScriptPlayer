using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window
    {
        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register(
            "Filename", typeof(string), typeof(RenameDialog), new PropertyMetadata(default(string)));

        public string Filename
        {
            get => (string) GetValue(FilenameProperty);
            set => SetValue(FilenameProperty, value);
        }

        public RenameDialog(string filename)
        {
            Filename = filename;
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

            char[] illegalChars = Path.GetInvalidFileNameChars();

            if (string.IsNullOrEmpty(Filename))
            {
                MessageBox.Show("The filename mustn't be empty", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Filename.Any(c => illegalChars.Contains(c)))
            {
                MessageBox.Show("This filename contains invalid characters", "Input error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.Focus();
            txtInput.Select(Filename.Length,0);
        }
    }
}
