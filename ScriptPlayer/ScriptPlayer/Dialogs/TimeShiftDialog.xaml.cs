using System;
using System.Globalization;
using System.Windows;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for TimeShiftDialog.xaml
    /// </summary>
    public partial class TimeShiftDialog : Window
    {
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(TimeSpan), typeof(TimeShiftDialog), new PropertyMetadata(default(TimeSpan)));

        public TimeSpan Result
        {
            get => (TimeSpan) GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        public TimeShiftDialog(TimeSpan initialValue)
        {
            InitializeComponent();

            txtValue.Text = initialValue.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(txtValue.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                MessageBox.Show("Invalid Input\r\nMake sure to use '.' as your decimal separator", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = TimeSpan.FromSeconds(value);
            DialogResult = true;
        }

        private void TimeShiftDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            txtValue.Focus();
            txtValue.SelectAll();
        }
    }
}
