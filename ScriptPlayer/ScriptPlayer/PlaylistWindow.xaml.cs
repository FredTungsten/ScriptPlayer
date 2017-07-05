using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScriptPlayer
{
    /// <summary>
    /// Interaction logic for PlaylistWindow.xaml
    /// </summary>
    public partial class PlaylistWindow : Window
    {
        public event EventHandler<PlaylistEntry> EntrySelected; 

        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            "Entries", typeof(ObservableCollection<PlaylistEntry>), typeof(PlaylistWindow), new PropertyMetadata(default(ObservableCollection<PlaylistEntry>)));

        public ObservableCollection<PlaylistEntry> Entries
        {
            get { return (ObservableCollection<PlaylistEntry>) GetValue(EntriesProperty); }
            set { SetValue(EntriesProperty, value); }
        }
        public PlaylistWindow(ObservableCollection<PlaylistEntry> entries)
        {
            Entries = entries;
            InitializeComponent();
        }

        protected virtual void OnEntrySelected(PlaylistEntry e)
        {
            EntrySelected?.Invoke(this, e);
        }

        private void EventSetter_OnHandler(object sender, MouseButtonEventArgs e)
        {
            OnEntrySelected(((ListBoxItem)sender).DataContext as PlaylistEntry);
        }
    }

    public class PlaylistEntry
    {
        public PlaylistEntry(string filename)
        {
            Fullname = filename;
            Shortname = System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        public string Shortname { get; set; }
        public string Fullname { get; set; }
    }
}
