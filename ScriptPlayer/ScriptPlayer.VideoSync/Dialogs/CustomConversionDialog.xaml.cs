using System;
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
        public event EventHandler ResultChanged;

        public static readonly DependencyProperty PositionsProperty = DependencyProperty.Register(
            "Positions", typeof(RelativePositionCollection), typeof(CustomConversionDialog), new PropertyMetadata(default(RelativePositionCollection), OnSettingsChanged));

        private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CustomConversionDialog)d).OnResultChanged();
        }

        public RelativePositionCollection Positions
        {
            get => (RelativePositionCollection) GetValue(PositionsProperty);
            set => SetValue(PositionsProperty, value);
        }

        public static readonly DependencyProperty BeatPatternProperty = DependencyProperty.Register(
            "BeatPattern", typeof(bool[]), typeof(CustomConversionDialog), new PropertyMetadata(default(bool[]), OnSettingsChanged));

        public bool[] BeatPattern
        {
            get => (bool[]) GetValue(BeatPatternProperty);
            set => SetValue(BeatPatternProperty, value);
        }

        private static RelativePositionCollection _previousPattern;
        private static bool[] _previousBeats;

        public static void SetPattern(RelativePositionCollection collection)
        {
            _previousPattern = collection;
        }

        public CustomConversionDialog()
        {
            Positions = _previousPattern ?? new RelativePositionCollection();
            BeatPattern = _previousBeats ?? new[] { true, true };
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

            _previousBeats = BeatPattern;
            _previousPattern = Positions;

            DialogResult = true;
        }

        private void btnSetPattern_Click(object sender, RoutedEventArgs e)
        {
            PatternFillOptionsDialog dialog = new PatternFillOptionsDialog(BeatPattern);
            if (dialog.ShowDialog() != true)
                return;

            BeatPattern = dialog.Result;
        }

        private void btnClearPattern_Click(object sender, RoutedEventArgs e)
        {
            BeatPattern = new[] {true, true};
            Positions.Clear();
        }

        protected virtual void OnResultChanged()
        {
            ResultChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionBar_OnPositionsChanged(object sender, EventArgs e)
        {
            OnResultChanged();
        }
    }
}
