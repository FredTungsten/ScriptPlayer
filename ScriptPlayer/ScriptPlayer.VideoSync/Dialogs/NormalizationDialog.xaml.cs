using System.Windows;
using System.Windows.Controls;
using Accord.Imaging.Filters;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for NormalizationDialog.xaml
    /// </summary>
    public partial class NormalizationDialog : Window
    {
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtInput.SelectAll();
            txtInput.Focus();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DivideBeats(4);
        }

        private void DivideBeats(int i)
        {
            if ((InitialBeats - 1) % i != 0)
            {
                MessageBox.Show(this, "Can't divide beats by " + i, "Not possible", MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            int targetCount = (InitialBeats - 1) / i + 1;
            AdditionalBeats = targetCount - InitialBeats;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DivideBeats(3);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            DivideBeats(2);
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MultiplyBeats(2);
        }

        private void MultiplyBeats(int i)
        {
            AdditionalBeats = (InitialBeats - 1) * (i - 1);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            MultiplyBeats(3);
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            MultiplyBeats(4);
        }
    }
}
