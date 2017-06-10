using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for ColorSummary.xaml
    /// </summary>
    public partial class ColorSummary : UserControl
    {
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Color), typeof(ColorSummary), new PropertyMetadata(default(Color), OnColorPropertyChanged));

        private static void OnColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ColorSummary) d).UpdateColors();
        }

        private void UpdateColors()
        {
            rectColor.Fill = new SolidColorBrush(Color);
            txtR.Text = Color.R.ToString("D");
            txtG.Text = Color.G.ToString("D");
            txtB.Text = Color.B.ToString("D");

            var hsl = HslConversion.FromRgb(Color.R, Color.G, Color.B);

            txtH.Text = hsl.Item1.ToString("f0");
            txtS.Text = hsl.Item2.ToString("f0");
            txtL.Text = hsl.Item3.ToString("f0");
        }

        public Color Color
        {
            get { return (Color) GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        public ColorSummary()
        {
            InitializeComponent();
        }
    }
}
