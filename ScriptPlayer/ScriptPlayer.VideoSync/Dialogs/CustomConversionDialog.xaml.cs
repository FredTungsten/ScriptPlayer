using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ScriptPlayer.Shared;
using ScriptPlayer.VideoSync.Annotations;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for CustomConversionDialog.xaml
    /// </summary>
    public partial class CustomConversionDialog : Window
    {
        public event EventHandler ResultChanged;

        private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CustomConversionDialog)d).OnResultChanged();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(
            "Settings", typeof(CustomConversionSettings), typeof(CustomConversionDialog), new PropertyMetadata(default(CustomConversionSettings), OnSettingsChanged));

        public CustomConversionSettings Settings
        {
            get => (CustomConversionSettings) GetValue(SettingsProperty);
            set => SetValue(SettingsProperty, value);
        }

        public static readonly DependencyProperty KnownPatternsProperty = DependencyProperty.Register(
            "KnownPatterns", typeof(List<CustomConversionSettings>), typeof(CustomConversionDialog), new PropertyMetadata(default(List<CustomConversionSettings>)));

        public List<CustomConversionSettings> KnownPatterns
        {
            get => (List<CustomConversionSettings>) GetValue(KnownPatternsProperty);
            set { SetValue(KnownPatternsProperty, value); }
        }

        private static CustomConversionSettings _previousSettings = new CustomConversionSettings();
        private static List<CustomConversionSettings> _knownPatterns = new List<CustomConversionSettings>();

        public static void SetPattern(RelativePositionCollection collection)
        {
            _previousSettings.Positions = collection;
        }

        public static void AddSettings(CustomConversionSettings settings)
        {
            foreach (CustomConversionSettings existing in _knownPatterns.ToList())
            {
                if (settings.IsIdenticalTo(existing))
                    _knownPatterns.Remove(existing);
            }

            _knownPatterns.Insert(0, settings);
        }

        public CustomConversionDialog()
        {
            Settings = _previousSettings.Duplicate();
            KnownPatterns = _knownPatterns;

            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Positions.Count < 2)
            {
                MessageBox.Show("You need at least two points!", "Invalid Pattern", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var first = Settings.Positions.First();
            var last = Settings.Positions.Last();

            if (first.Position != last.Position && first.Position != (99 - last.Position))
            {
                MessageBox.Show("The first and last point must be identical or inverse to each other!", "Invalid Pattern", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _previousSettings = new CustomConversionSettings
            {
                Pattern = Settings.Pattern,
                Positions = Settings.Positions
            };

            AddSettings(_previousSettings.Duplicate());
            
            DialogResult = true;
        }

        private void btnSetPattern_Click(object sender, RoutedEventArgs e)
        {
            PatternFillOptionsDialog dialog = new PatternFillOptionsDialog(Settings.Pattern);
            if (dialog.ShowDialog() != true)
                return;

            Settings.Pattern = dialog.Result;
            OnResultChanged();
        }

        private void btnClearPattern_Click(object sender, RoutedEventArgs e)
        {
            Settings.Pattern = new[] {true, true};
            OnResultChanged();
        }

        protected virtual void OnResultChanged()
        {
            positionBar?.InvalidateVisual();
            ResultChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PositionBar_OnPositionsChanged(object sender, EventArgs e)
        {
            OnResultChanged();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnResultChanged();
        }

        private void btnClearPositions_Click(object sender, RoutedEventArgs e)
        {
            Settings.Positions.Clear();
            OnResultChanged();
        }

        private void UIElement_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var bar = ((BeatAndPositionBar) sender);
            CustomConversionSettings settings = new CustomConversionSettings
            {
                Pattern = bar.BeatPattern.ToArray(),
                Positions = bar.Positions
            };

            Settings = settings.Duplicate();
        }

        private void btnInvertPositions_Click(object sender, RoutedEventArgs e)
        {
            RelativePositionCollection positions = new RelativePositionCollection();
            foreach (var pos in Settings.Positions)
            {
                byte newPos;
                if (pos.Position == 99)
                    newPos = 0;
                else if (pos.Position == 0)
                    newPos = 99;
                else
                    newPos = (byte)(100 - pos.Position);

                positions.Add(new RelativePosition
                {
                    Position = newPos,
                    RelativeTime = pos.RelativeTime
                });
            }

            Settings.Positions = positions;
            OnResultChanged();
        }
    }

    public class CustomConversionSettings : INotifyPropertyChanged
    {
        private RelativePositionCollection _positions;
        private bool[] _pattern;

        public RelativePositionCollection Positions
        {
            get => _positions;
            set
            {
                if (Equals(value, _positions)) return;
                _positions = value;
                OnPropertyChanged();
            }
        }

        public bool[] Pattern
        {
            get => _pattern;
            set
            {
                if (Equals(value, _pattern)) return;
                _pattern = value;
                OnPropertyChanged();
            }
        }

        public CustomConversionSettings()
        {
            Positions = new RelativePositionCollection
            {
                new RelativePosition {Position = 0, RelativeTime = 0},
                new RelativePosition {Position = 99, RelativeTime = 1}
            };
            Pattern = new[] {true, true};
        }

        public bool IsIdenticalTo(CustomConversionSettings other)
        {
            if (!Positions.IsIdenticalTo(other.Positions))
                return false;

            return Pattern.SequenceEqual(other.Pattern);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CustomConversionSettings Duplicate()
        {
            CustomConversionSettings settings = new CustomConversionSettings();
            settings.Positions = new RelativePositionCollection();

            foreach(var position in Positions)
                settings.Positions.Add(new RelativePosition
                {
                    Position = position.Position,
                    RelativeTime = position.RelativeTime
                });

            settings.Pattern = new bool[Pattern.Length];
            Array.Copy(Pattern, settings.Pattern, settings.Pattern.Length);

            return settings;
        }
    }
}
