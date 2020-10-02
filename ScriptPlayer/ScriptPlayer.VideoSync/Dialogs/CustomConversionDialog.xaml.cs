using System.Linq;
using System.Windows;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for CustomConversionDialog.xaml
    /// </summary>
    public partial class CustomConversionDialog : Window
    {
        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(RelativePositionCollection), typeof(CustomConversionDialog), new PropertyMetadata(default(RelativePositionCollection)));

        public RelativePositionCollection Positions
        {
            get => (RelativePositionCollection) GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }

        public static readonly DependencyProperty BeatPatternProperty = DependencyProperty.Register(
            "BeatPattern", typeof(bool[]), typeof(CustomConversionDialog), new PropertyMetadata(default(bool[])));

        public bool[] BeatPattern
        {
            get { return (bool[]) GetValue(BeatPatternProperty); }
            set { SetValue(BeatPatternProperty, value); }
        }

        public CustomConversionDialog(RelativePositionCollection collection, bool[] beatPattern)
        {
            Positions = collection;
            BeatPattern = beatPattern;
            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Positions.Count < 2)
            {
                MessageBox.Show("You need at least two points!", "Invalid Pattern", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var first = Positions.First();
            var last = Positions.Last();

            if (first.Position != last.Position && first.Position != (99 - last.Position))
            {
                MessageBox.Show("The first and last point must be identical or inverse to each other!", "Invalid Pattern", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PatternFillOptionsDialog dialog = new PatternFillOptionsDialog(BeatPattern);
            if (dialog.ShowDialog() != true)
                return;

            BeatPattern = dialog.Result;
        }
    }
}
