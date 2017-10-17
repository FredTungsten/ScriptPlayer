using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ScriptPlayer.VideoSync.Annotations;

namespace ScriptPlayer.VideoSync.Dialogs
{
    /// <summary>
    /// Interaction logic for PatternFillOptionsDialog.xaml
    /// </summary>
    public partial class PatternFillOptionsDialog : Window
    {
        public static readonly DependencyProperty TicksInPatternProperty = DependencyProperty.Register(
            "TicksInPattern", typeof(int), typeof(PatternFillOptionsDialog), new PropertyMetadata(9, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            ((PatternFillOptionsDialog) dependencyObject).UpdateBeatCount();
        }

        private void UpdateBeatCount()
        {
            if(IsInitialized)
                SetBeatCount(TicksInPattern);
        }

        public int TicksInPattern
        {
            get { return (int) GetValue(TicksInPatternProperty); }
            set { SetValue(TicksInPatternProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(bool[]), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(bool[])));

        public bool[] Result
        {
            get { return (bool[]) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty BeatsProperty = DependencyProperty.Register(
            "Beats", typeof(ObservableCollection<IndexedBoolean>), typeof(PatternFillOptionsDialog), new PropertyMetadata(default(ObservableCollection<IndexedBoolean>)));

        public ObservableCollection<IndexedBoolean> Beats
        {
            get { return (ObservableCollection<IndexedBoolean>) GetValue(BeatsProperty); }
            set { SetValue(BeatsProperty, value); }
        }

        public PatternFillOptionsDialog(bool[] initialValues = null)
        {
            Beats = new ObservableCollection<IndexedBoolean>();

            if (initialValues != null)
            {
                TicksInPattern = initialValues.Length;
                SetBeatCount(TicksInPattern);
                for (int i = 0; i < TicksInPattern; i++)
                    Beats[i].Value = initialValues[i];
            }

            Initialized += OnInitialized;
            InitializeComponent();
        }

        private void OnInitialized(object sender, EventArgs eventArgs)
        {
            UpdateBeatCount();
        }

        private void SetBeatCount(int ticksInPattern)
        {
            while(Beats.Count > ticksInPattern)
                Beats.RemoveAt(Beats.Count - 1);

            while(Beats.Count < ticksInPattern)
                Beats.Add(new IndexedBoolean
                {
                    CanEdit = true,
                    Caption = (Beats.Count + 1).ToString(),
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
            ((Button) sender).Focus();
            Result = Beats.Select(b => b.Value).ToArray();
            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TicksInPattern++;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if(TicksInPattern > 2)
            TicksInPattern--;
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
