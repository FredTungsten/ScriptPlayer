using System.Windows;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for RangeStretcherDialog.xaml
    /// </summary>
    public partial class RangeStretcherDialog : Window
    {
        public static readonly DependencyProperty MinValueFromProperty = DependencyProperty.Register(
            "MinValueFrom", typeof(byte), typeof(RangeStretcherDialog), new PropertyMetadata(default(byte)));

        public byte MinValueFrom
        {
            get => (byte) GetValue(MinValueFromProperty);
            set => SetValue(MinValueFromProperty, value);
        }

        public static readonly DependencyProperty MinValueToProperty = DependencyProperty.Register(
            "MinValueTo", typeof(byte), typeof(RangeStretcherDialog), new PropertyMetadata(default(byte)));

        public byte MinValueTo
        {
            get => (byte) GetValue(MinValueToProperty);
            set => SetValue(MinValueToProperty, value);
        }

        public static readonly DependencyProperty MaxValueFromProperty = DependencyProperty.Register(
            "MaxValueFrom", typeof(byte), typeof(RangeStretcherDialog), new PropertyMetadata(default(byte)));

        public byte MaxValueFrom
        {
            get => (byte) GetValue(MaxValueFromProperty);
            set => SetValue(MaxValueFromProperty, value);
        }

        public static readonly DependencyProperty MaxValueToProperty = DependencyProperty.Register(
            "MaxValueTo", typeof(byte), typeof(RangeStretcherDialog), new PropertyMetadata(default(byte)));

        public byte MaxValueTo
        {
            get => (byte) GetValue(MaxValueToProperty);
            set => SetValue(MaxValueToProperty, value);
        }

        public RangeStretcherDialog()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
