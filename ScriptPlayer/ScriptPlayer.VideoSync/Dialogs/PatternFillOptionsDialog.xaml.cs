using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public static readonly DependencyProperty RecentlyUsedPatternsProperty = DependencyProperty.Register(
            "RecentlyUsedPatterns", typeof(List<bool[]>), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(List<bool[]>)));

        public List<bool[]> RecentlyUsedPatterns
        {
            get { return (List<bool[]>)GetValue(RecentlyUsedPatternsProperty); }
            set { SetValue(RecentlyUsedPatternsProperty, value); }
        }

        public static readonly DependencyProperty TicksInPatternProperty = DependencyProperty.Register(
            "TicksInPattern", typeof(int), typeof(PatternFillOptionsDialog), new PropertyMetadata(9, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((PatternFillOptionsDialog)dependencyObject).UpdateBeatCount();
        }

        private void UpdateBeatCount()
        {
            if (IsInitialized)
                SetBeatCount(TicksInPattern);
        }

        public int TicksInPattern
        {
            get { return (int)GetValue(TicksInPatternProperty); }
            set { SetValue(TicksInPatternProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(bool[]), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(bool[])));

        public bool[] Result
        {
            get { return (bool[])GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(ObservableCollection<IndexedBoolean>), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(ObservableCollection<IndexedBoolean>)));

        private static List<bool[]> _recentlyUsed = new List<bool[]>();

        public ObservableCollection<IndexedBoolean> Beats
        {
            get { return (ObservableCollection<IndexedBoolean>)GetValue(BeatsProperty); }
            set { SetValue(BeatsProperty, value); }
        }

        public PatternFillOptionsDialog(bool[] initialValues = null)
        {
            Beats = new ObservableCollection<IndexedBoolean>();

            if (initialValues != null)
            {
                SetPattern(initialValues);
            }

            RecentlyUsedPatterns = _recentlyUsed;

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
                Beats.RemoveAt(Beats.Count - 1);

            while (Beats.Count < ticksInPattern)
                Beats.Add(new IndexedBoolean
                {
                    CanEdit = true,
                    Caption = ((Beats.Count % (ticksInPattern + 1)) + 1).ToString(),
                    Value = true
                });

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
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).Focus();
            ReturnResult();
        }

        private void ReturnResult()
        {
            Result = Beats.Select(b => b.Value).ToArray();
            SavePatternToMRU(Result);
            DialogResult = true;
        }

        private void SavePatternToMRU(bool[] result)
        {
            for (int i = 0; i < _recentlyUsed.Count; i++)
            {
                if (_recentlyUsed[i].SequenceEqual(result))
                {
                    _recentlyUsed.RemoveAt(i);
                    break;
                }
            }

            _recentlyUsed.Insert(0, result);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TicksInPattern++;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
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
            RecentlyUsedPatterns = new List<bool[]>(RecentlyUsedPatterns);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            bool[] pattern = ((Button) sender).DataContext as bool[];
            Remove(pattern);
        }
    }

    public class IndexedBoolean : INotifyPropertyChanged
    {
        private string _caption;
        private bool _value;
        private bool _canEdit;

        public string Caption
        {
            get { return _caption; }
            set
            {
                if (value == _caption) return;
                _caption = value;
                OnPropertyChanged();
            }
        }

        public bool Value
        {
            get { return _value; }
            set
            {
                if (value == _value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        public bool CanEdit
        {
            get { return _canEdit; }
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
