using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScriptPlayer.VideoSync.Annotations;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for PatternFillOptionsDialog.xaml
    /// </summary>
    public partial class PatternFillOptionsDialog : Window
    {
        public event EventHandler<PatternEventArgs> PatternChanged; 

        public static readonly DependencyProperty RecentlyUsedPatternsProperty = DependencyProperty.Register(
            "RecentlyUsedPatterns", typeof(ObservableCollection<bool[]>), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(ObservableCollection<bool[]>)));

        public ObservableCollection<bool[]> RecentlyUsedPatterns
        {
            get => (ObservableCollection<bool[]>)GetValue(RecentlyUsedPatternsProperty);
            set => SetValue(RecentlyUsedPatternsProperty, value);
        }

        public static readonly DependencyProperty TicksInPatternProperty = DependencyProperty.Register(
            "TicksInPattern", typeof(int), typeof(PatternFillOptionsDialog), new PropertyMetadata(9, TicksInPatternPropertyChanged));

        private static void TicksInPatternPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((PatternFillOptionsDialog)dependencyObject).UpdateBeatCount();
        }

        private void UpdateBeatCount()
        {
            if (IsInitialized && !_multiplying)
                SetBeatCount(TicksInPattern);
        }

        public int TicksInPattern
        {
            get => (int)GetValue(TicksInPatternProperty);
            set => SetValue(TicksInPatternProperty, value);
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(bool[]), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(bool[])));

        public bool[] Result
        {
            get => (bool[])GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(ObservableCollection<IndexedBoolean>), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(ObservableCollection<IndexedBoolean>), OnBeatsPropertyChanged));

        private static void OnBeatsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PatternFillOptionsDialog) d).BeatsPropertyChanged(
                e.OldValue as ObservableCollection<IndexedBoolean>,
                e.NewValue as ObservableCollection<IndexedBoolean>);
        }

        private void BeatsPropertyChanged(ObservableCollection<IndexedBoolean> oldValue, ObservableCollection<IndexedBoolean> newValue)
        {
            if (oldValue != null)
            {
                newValue.CollectionChanged -= Beats_CollectionChanged;

                foreach (IndexedBoolean indexedBoolean in newValue)
                {
                    indexedBoolean.PropertyChanged -= Beat_Changed;
                }
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += Beats_CollectionChanged;

                foreach (IndexedBoolean indexedBoolean in newValue)
                {
                    indexedBoolean.PropertyChanged += Beat_Changed;
                }
            }

            BeatHasChanged();
        }

        private void Beat_Changed(object sender, PropertyChangedEventArgs e)
        {
            BeatHasChanged();
        }

        private void BeatHasChanged()
        {
            OnPatternChanged(GetResult());
            UpdateActiveBeatCount();
        }

        private void Beats_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null)
            {
                foreach (IndexedBoolean indexedBoolean in args.OldItems)
                    indexedBoolean.PropertyChanged -= Beat_Changed;
            }

            if (args.NewItems != null)
            {
                foreach (IndexedBoolean indexedBoolean in args.NewItems)
                    indexedBoolean.PropertyChanged += Beat_Changed;
            }

            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (IndexedBoolean indexedBoolean in Beats)
                {
                    indexedBoolean.PropertyChanged -= Beat_Changed;
                    indexedBoolean.PropertyChanged += Beat_Changed;
                }
            }
        }

        private static readonly ObservableCollection<bool[]> RecentlyUsed = new ObservableCollection<bool[]>();
        private bool _multiplying;

        public ObservableCollection<IndexedBoolean> Beats
        {
            get => (ObservableCollection<IndexedBoolean>)GetValue(BeatsProperty);
            set => SetValue(BeatsProperty, value);
        }

        public PatternFillOptionsDialog(bool[] initialValues = null)
        {
            Beats = new ObservableCollection<IndexedBoolean>();

            if (initialValues != null)
            {
                SetPattern(initialValues);
            }

            RecentlyUsedPatterns = RecentlyUsed;

            Initialized += OnInitialized;
            InitializeComponent();
        }

        private void SetPattern(bool[] values)
        {
            TicksInPattern = values.Length;
            SetBeatCount(TicksInPattern);
            for (int i = 0; i < TicksInPattern; i++)
                Beats[i].Value = values[i];
        }

        private void OnInitialized(object sender, EventArgs eventArgs)
        {
            UpdateBeatCount();
        }

        private void SetBeatCount(int ticksInPattern)
        {
            while (Beats.Count > ticksInPattern)
            {
                Beats.RemoveAt(Beats.Count - 1);
            }

            while (Beats.Count < ticksInPattern)
            {
                var beat = new IndexedBoolean
                {
                    CanEdit = true,
                    Caption = ((Beats.Count % (ticksInPattern + 1)) + 1).ToString(),
                    Value = true
                };
                Beats.Add(beat);
            }

            for (int i = 0; i < Beats.Count; i++)
            {
                if (i == 0 || i == Beats.Count - 1)
                {
                    Beats[i].CanEdit = false;
                    Beats[i].Value = true;
                }
                else
                {
                    Beats[i].CanEdit = true;
                }
            }

            OnPatternChanged(GetResult());
            UpdateActiveBeatCount();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            ReturnResult();
        }

        private void ReturnResult()
        {
            Result = GetResult();
            SavePatternToMRU(Result);
            DialogResult = true;
        }

        private bool[] GetResult()
        {
            return Beats.Select(b => b.Value).ToArray();
        }

        private void SavePatternToMRU(bool[] result)
        {
            for (int i = 0; i < RecentlyUsed.Count; i++)
            {
                if (RecentlyUsed[i].SequenceEqual(result))
                {
                    RecentlyUsed.RemoveAt(i);
                    break;
                }
            }

            RecentlyUsed.Insert(0, result);
        }

        private void btnPlus_Click(object sender, RoutedEventArgs e)
        {
            TicksInPattern++;
        }

        private void btnMinus_Click(object sender, RoutedEventArgs e)
        {
            if (TicksInPattern > 2)
                TicksInPattern--;
        }

        private void ListItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ReturnResult();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool[] selection = ((ListBox) sender).SelectedItem as bool[];
            if (selection == null) return;

            SetPattern(selection);
        }

        private void Remove(bool[] selection)
        {
            RecentlyUsedPatterns.Remove(selection);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            bool[] pattern = ((Button) sender).DataContext as bool[];
            Remove(pattern);
        }

        protected virtual void OnPatternChanged(bool[] pattern)
        {
            PatternChanged?.Invoke(this, new PatternEventArgs(){Pattern = pattern});
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OnPatternChanged(GetResult());
            UpdateActiveBeatCount();
        }

        private void btnTimes2_Click(object sender, RoutedEventArgs e)
        {
            _multiplying = true;

            ObservableCollection<IndexedBoolean> newBeats = new ObservableCollection<IndexedBoolean>();

            for (int i = 0; i < Beats.Count; i++)
            {
                newBeats.Add(new IndexedBoolean
                {
                    Value = Beats[i].Value,
                    CanEdit = (i != 0 && i != Beats.Count - 1),
                    Caption = (newBeats.Count + 1).ToString()
                });

                if (i != Beats.Count - 1)
                    newBeats.Add(new IndexedBoolean
                    {
                        Value = false,
                        CanEdit = true,
                        Caption = (newBeats.Count + 1).ToString()
                    });
            }

            Beats = newBeats;
            TicksInPattern = newBeats.Count;
            _multiplying = false;
        }

        private void UpdateActiveBeatCount()
        {
            if (!IsInitialized)
                return;

            int beats = Beats.Count(b => b.Value) - 1;
            txtBeats.Text = $"{beats} beats / measure";
        }

        private void btnBy2_Click(object sender, RoutedEventArgs e)
        {
            if (Beats.Count < 4)
                return;

            _multiplying = true;

            ObservableCollection<IndexedBoolean> newBeats = new ObservableCollection<IndexedBoolean>();

            for (int i = 0; i < Beats.Count; i++)
            {
                if (i % 2 > 0)
                    continue;

                newBeats.Add(new IndexedBoolean
                {
                    Value = Beats[i].Value,
                    CanEdit = (i != 0 && i != Beats.Count - 1),
                    Caption = (newBeats.Count + 1).ToString()
                });
            }

            Beats = newBeats;
            TicksInPattern = newBeats.Count;
            _multiplying = false;
        }
    }

    public class PatternEventArgs : EventArgs
    {
        public bool[] Pattern { get; set; }
    }

    public class IndexedBoolean : INotifyPropertyChanged
    {
        private string _caption;
        private bool _value;
        private bool _canEdit;

        public string Caption
        {
            get => _caption;
            set
            {
                if (value == _caption) return;
                _caption = value;
                OnPropertyChanged();
            }
        }

        public bool Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                if (value == _canEdit) return;
                _canEdit = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
