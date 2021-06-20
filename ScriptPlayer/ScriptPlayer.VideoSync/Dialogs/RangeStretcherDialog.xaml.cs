using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for RangeStretcherDialog.xaml
    /// </summary>
    public partial class RangeStretcherDialog : Window
    {
        public static readonly DependencyProperty MultiplyProperty = DependencyProperty.Register(
            "Multiply", typeof(bool), typeof(RangeStretcherDialog), new PropertyMetadata(default(bool)));

        public bool Multiply
        {
            get => (bool) GetValue(MultiplyProperty);
            set => SetValue(MultiplyProperty, value);
        }

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

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process(e.ChangedButton, ((Button) sender).Tag.ToString());
        }

        private void Process(MouseButton mouseButton, string tag)
        {
            int min;
            int max;

            int range = int.Parse(tag.Substring(1));

            switch (tag[0])
            {
                case 'B':
                    min = 0;
                    max = range;
                    break;
                case 'T':
                    min = 100 - range;
                    max = 100;
                    break;
                case 'C':
                    min = 50-range/2;
                    max = 50+range/2;
                    break;
                default:
                    return;
            }

            byte bmin = (byte)Math.Max(0, Math.Min(99, min));
            byte bmax = (byte)Math.Max(0, Math.Min(99, max));

            if (mouseButton == MouseButton.Left)
            {
                MinValueFrom = 0;
                MaxValueFrom = 99;

                MaxValueFrom = bmax;
                MinValueFrom = bmin;
            }

            MinValueTo = 0;
            MaxValueTo = 99;

            MinValueTo = bmin;
            MaxValueTo = bmax;


        }
    }
}
