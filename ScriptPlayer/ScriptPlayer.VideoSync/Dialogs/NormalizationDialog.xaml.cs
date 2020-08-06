using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for NormalizationDialog.xaml
    /// </summary>
    public partial class NormalizationDialog : Window
    {
        public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
            "Input", typeof(string), typeof(NormalizationDialog), new PropertyMetadata(default(string)));

        public string Input
        {
            get { return (string) GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public static readonly DependencyProperty AdditionalBeatsProperty = DependencyProperty.Register(
            "AdditionalBeats", typeof(int), typeof(NormalizationDialog), new PropertyMetadata(default(int)));

        public int AdditionalBeats
        {
            get { return (int) GetValue(AdditionalBeatsProperty); }
            set { SetValue(AdditionalBeatsProperty, value); }
        }

        public static readonly DependencyProperty InitialBeatsProperty = DependencyProperty.Register(
            "InitialBeats", typeof(int), typeof(NormalizationDialog), new PropertyMetadata(default(int)));

        public int InitialBeats
        {
            get { return (int) GetValue(InitialBeatsProperty); }
            set { SetValue(InitialBeatsProperty, value); }
        }

        public NormalizationDialog(int selectedBeats)
        {
            InitialBeats = selectedBeats;
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            Accept();
        }

        private void Accept()
        {
            if (UpdateAdditionalBeats())
                DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.SelectAll();
            txtInput.Focus();
        }

        private void btnPresets_Click(object sender, RoutedEventArgs e)
        {
            Input = ((Button) sender).Content.ToString();
            if (UpdateAdditionalBeats())
                Accept();
        }

        private bool UpdateAdditionalBeats()
        {
            if(string.IsNullOrWhiteSpace(Input))
            {
                MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Input.StartsWith("/"))
            {
                if (Input.Length < 2)
                {
                    MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                int divisor;
                if (!int.TryParse(Input.Substring(1), out divisor))
                {
                    MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                return DivideBeats(divisor);
            }

            if (Input.StartsWith("*"))
            {
                if (Input.Length < 2)
                {
                    MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                int multiplier;
                if (!int.TryParse(Input.Substring(1), out multiplier))
                {
                    MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                MultiplyBeats(multiplier);
                return true;
            }

            int newNumber;

            bool result = int.TryParse(Input, out newNumber);
            if (!result)
            {
                MessageBox.Show("Invalid Input!", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            AdditionalBeats = newNumber;
            return true;
        }

        private bool DivideBeats(int i)
        {
            if (i <= 0 || (InitialBeats - 1) % i != 0)
            {
                MessageBox.Show(this, "Can't divide beats by " + i, "Not possible", MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }

            int targetCount = (InitialBeats - 1) / i + 1;
            AdditionalBeats = targetCount - InitialBeats;

            return true;
        }

        private void MultiplyBeats(int i)
        {
            AdditionalBeats = (InitialBeats - 1) * (i - 1);
        }
    }
}
