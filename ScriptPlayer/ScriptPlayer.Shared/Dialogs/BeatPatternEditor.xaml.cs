using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ScriptPlayer.Shared.Properties;

namespace ScriptPlayer.Shared.Dialogs
{
    /// <summary>
    /// Interaction logic for BeatPatternEditor.xaml
    /// </summary>
    public partial class BeatPatternEditor : Window
    {
        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            "Entries", typeof(ObservableCollection<BeatEntry>), typeof(BeatPatternEditor), new PropertyMetadata(default(ObservableCollection<BeatEntry>)));

        public ObservableCollection<BeatEntry> Entries
        {
            get { return (ObservableCollection<BeatEntry>) GetValue(EntriesProperty); }
            set { SetValue(EntriesProperty, value); }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(bool[]), typeof(BeatPatternEditor), new PropertyMetadata(default(bool[])));

        public bool[] Result
        {
            get { return (bool[]) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public BeatPatternEditor(bool[] initialPattern)
        {
            Entries = new ObservableCollection<BeatEntry>();
            int i = 1;

            foreach (bool active in initialPattern)
            {
                Entries.Add(new BeatEntry(i.ToString("D"), active));
                i++;
            }

            InitializeComponent();
        }

        private void btnOk_Click(object sender, RoutedEventArgs args)
        {
            Result = Entries.Select(e => e.Active).ToArray();
            DialogResult = true;
        }
    }

    public class BeatEntry : INotifyPropertyChanged
    {
        private bool _active;
        private string _label;

        public BeatEntry(string label, bool active)
        {
            Label = label;
            Active = active;
        }

        public bool Active
        {
            get { return _active; }
            set
            {
                if (value == _active) return;
                _active = value;
                OnPropertyChanged();
            }
        }

        public string Label
        {
            get { return _label; }
            set
            {
                if (value == _label) return;
                _label = value;
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
