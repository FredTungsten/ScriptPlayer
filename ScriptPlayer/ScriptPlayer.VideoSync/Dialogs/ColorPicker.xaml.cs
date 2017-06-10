using System;
using System.Windows;
using System.Windows.Media;

namespace ScriptPlayer.VideoSync
{
    /// <summary>
    /// Interaction logic for ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : Window
    {
        public static readonly DependencyProperty RedProperty = DependencyProperty.Register(
            "Red", typeof(Byte), typeof(ColorPicker), new PropertyMetadata(default(Byte), OnColorPropertyChanged));

        public Byte Red
        {
            get { return (Byte) GetValue(RedProperty); }
            set { SetValue(RedProperty, value); }
        }

        public static readonly DependencyProperty GreenProperty = DependencyProperty.Register(
            "Green", typeof(Byte), typeof(ColorPicker), new PropertyMetadata(default(Byte), OnColorPropertyChanged));

        public Byte Green
        {
            get { return (Byte) GetValue(GreenProperty); }
            set { SetValue(GreenProperty, value); }
        }

        public static readonly DependencyProperty BlueProperty = DependencyProperty.Register(
            "Blue", typeof(Byte), typeof(ColorPicker), new PropertyMetadata(default(Byte), OnColorPropertyChanged));

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorPicker) d).UpdateColor();
        }

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            "SelectedColor", typeof(Color), typeof(ColorPicker), new PropertyMetadata(default(Color)));

        public Color SelectedColor
        {
            get { return (Color) GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
            "SelectedBrush", typeof(SolidColorBrush), typeof(ColorPicker), new PropertyMetadata(default(SolidColorBrush)));

        public SolidColorBrush SelectedBrush
        {
            get { return (SolidColorBrush) GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        private void UpdateColor()
        {
            SelectedColor = Color.FromRgb(Red, Green, Blue);
            SelectedBrush = new SolidColorBrush(SelectedColor);
        }

        public Byte Blue
        {
            get { return (Byte) GetValue(BlueProperty); }
            set { SetValue(BlueProperty, value); }
        }

        public ColorPicker(Color initialColor)
        {
            Red = initialColor.R;
            Green = initialColor.G;
            Blue = initialColor.B;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
