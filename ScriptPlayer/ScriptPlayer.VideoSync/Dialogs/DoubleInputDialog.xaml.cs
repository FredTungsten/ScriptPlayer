using System.Globalization;
using System.Windows;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for DoubleInputDialog.xaml
    /// </summary>
    public partial class DoubleInputDialog : Window
    {
        public CultureInfo en_us = new CultureInfo("en-us");

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(double), typeof(DoubleInputDialog), new PropertyMetadata(default(double)));

        public double Result
        {
            get { return (double) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        private readonly double _defaultValue;

        public DoubleInputDialog(double defaultValue)
        {
            Loaded += OnLoaded;
            _defaultValue = defaultValue;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            txtInput.Text = _defaultValue.ToString();
            txtInput.SelectAll();
            txtInput.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double result;
            if (!double.TryParse(txtInput.Text, NumberStyles.Any, en_us, out result))
                return;

            Result = result;
            DialogResult = true;
        }
    }
}
