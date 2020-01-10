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

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnFull_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 5;
            MinValueTo = 5;
            MaxValueFrom = 95;
            MaxValueTo = 95;
        }

        private void btnTop2_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 35;
            MinValueTo = 35;
            MaxValueFrom = 95;
            MaxValueTo = 95;
        }

        private void btnBottom2_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 5;
            MinValueTo = 5;
            MaxValueFrom = 65;
            MaxValueTo = 65;
        }

        private void btnTop1_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 65;
            MinValueTo = 65;
            MaxValueFrom = 95;
            MaxValueTo = 95;
        }

        private void btnBottom1_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 5;
            MinValueTo = 5;
            MaxValueFrom = 35;
            MaxValueTo = 35;
        }

        private void btnCenter1_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 35;
            MinValueTo = 35;
            MaxValueFrom = 65;
            MaxValueTo = 65;
        }

        private void btnCenter2_Click(object sender, RoutedEventArgs e)
        {
            MinValueFrom = 20;
            MinValueTo = 20;
            MaxValueFrom = 80;
            MaxValueTo = 80;
        }
    }
}
