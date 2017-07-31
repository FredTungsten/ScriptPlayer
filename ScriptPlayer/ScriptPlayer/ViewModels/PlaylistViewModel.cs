using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;
using ScriptPlayer.Shared;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PlaylistEntry> _entries;
        private bool _shuffle;
        private PlaylistEntry _selectedEntry;
        private bool _repeat;

        public ObservableCollection<PlaylistEntry> Entries
        {
            get { return _entries; }
            set
            {
                if (Equals(value, _entries)) return;
                UpdateEntryEvents(_entries, value);
                _entries = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        private void UpdateEntryEvents(ObservableCollection<PlaylistEntry> oldValue, ObservableCollection<PlaylistEntry> newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= EntriesChanged;
            }
            if (newValue != null)
            {
                newValue.CollectionChanged += EntriesChanged;
            }
        }

        private void EntriesChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool Shuffle
        {
            get { return _shuffle; }
            set
            {
                if (value == _shuffle) return;
                _shuffle = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                if (value == _repeat) return;
                _repeat = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public PlaylistEntry SelectedEntry
        {
            get { return _selectedEntry; }
            set
            {
                if (Equals(value, _selectedEntry)) return;
                _selectedEntry = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public RelayCommand<string> PlayNextEntryCommand { get; set; }
        public RelayCommand<string> PlayPreviousEntryCommand { get; set; }
        public RelayCommand MoveSelectedEntryUpCommand { get; set; }
        public RelayCommand MoveSelectedEntryDownCommand { get; set; }
        public RelayCommand RemoveSelectedEntryCommand { get; set; }
        public int EntryCount => Entries.Count;

        public PlaylistViewModel()
        {
            Entries = new ObservableCollection<PlaylistEntry>();

            MoveSelectedEntryDownCommand = new RelayCommand(ExecuteMoveSelectedEntryDown, CanMoveSelectedEntryDown);
            MoveSelectedEntryUpCommand = new RelayCommand(ExecuteMoveSelectedEntryUp, CanMoveSelectedEntryUp);
            RemoveSelectedEntryCommand = new RelayCommand(ExecuteRemoveSelectedEntry, CanRemoveSelectedEntry);
            PlayNextEntryCommand = new RelayCommand<string>(ExecutePlayNextEntry, CanPlayNextEntry);
            PlayPreviousEntryCommand = new RelayCommand<string>(ExecutePlayPreviousEntry, CanPlayPreviousEntry);
        }

        private bool CanPlayPreviousEntry(string currentFile)
        {
            if (Entries.Count == 0)
                return false;

            if (Repeat)
                return true;

            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentFile);
            if (currentEntry == null)
                return false;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(currentEntry);

            return index > 0;
        }

        private bool CanPlayNextEntry(string currentFile)
        {
            if (Entries.Count == 0)
                return false;

            if (Repeat)
                return true;

            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentFile);
            if (currentEntry == null)
                return true;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(currentEntry);

            return index < Entries.Count -1;
        }
        private void ExecutePlayPreviousEntry(string currentFile)
        {
            if (!CanPlayPreviousEntry(currentFile))
                return;

            PlaylistEntry entry = GetPreviousEntry(currentFile);
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }

        private void ExecutePlayNextEntry(string currentFile)
        {
            if (!CanPlayNextEntry(currentFile))
                return;

            PlaylistEntry entry = GetNextEntry(currentFile);
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }

        private bool CanRemoveSelectedEntry()
        {
            return SelectedEntry != null;
        }

        private void ExecuteRemoveSelectedEntry()
        {
            if (!CanRemoveSelectedEntry())
                return;

            int currentIndex = Entries.IndexOf(SelectedEntry);
            Entries.Remove(SelectedEntry);

            if (currentIndex < Entries.Count)
                SelectedEntry = Entries[currentIndex];
            else if (Entries.Count > 0)
                SelectedEntry = Entries[Entries.Count - 1];
            else
                SelectedEntry = null;

            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanMoveSelectedEntryUp()
        {
            if (SelectedEntry == null)
                return false;
            int currentIndex = Entries.IndexOf(SelectedEntry);
            if (currentIndex <= 0)
                return false;
            return true;
        }

        private bool CanMoveSelectedEntryDown()
        {
            if (SelectedEntry == null)
                return false;
            int currentIndex = Entries.IndexOf(SelectedEntry);
            if (currentIndex + 1 >= Entries.Count)
                return false;
            return true;
        }

        private void ExecuteMoveSelectedEntryUp()
        {
            if (!CanMoveSelectedEntryUp()) return;

            int currentIndex = Entries.IndexOf(SelectedEntry);
            Entries.Move(currentIndex, currentIndex - 1);
        }

        private void ExecuteMoveSelectedEntryDown()
        {
            if (!CanMoveSelectedEntryDown()) return;

            int currentIndex = Entries.IndexOf(SelectedEntry);
            Entries.Move(currentIndex, currentIndex+1);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PlaylistEntry> PlayEntry; 

        public void AddEntry(PlaylistEntry entry)
        {
            Entries.Add(entry);
            CommandManager.InvalidateRequerySuggested();
        }

        public PlaylistEntry FirstEntry()
        {
            return Entries.FirstOrDefault();
        }

        public PlaylistEntry GetNextEntry(string currentEntryFile)
        {
            if(Shuffle)
                return AnythingButThis(currentEntryFile);

            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentEntryFile);

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);

            if (currentIndex == Entries.Count - 1)
            {
                return Repeat ? Entries.First() : null;
            }

            var nextEntry = Entries[currentIndex + 1];

            return nextEntry;
        }

        public PlaylistEntry GetPreviousEntry(string currentEntryFile)
        {
            if (Shuffle)
                return AnythingButThis(currentEntryFile);

            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentEntryFile);

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);

            if(currentIndex == 0)
            {
                return Repeat ? Entries.Last() : null;
            }

            var previousEntry = Entries[currentIndex - 1];

            return previousEntry;
        }

        readonly Random _rng = new Random();

        private PlaylistEntry AnythingButThis(string currentEntryFile)
        {
            if (Entries.Count == 0)
                return null;
            if (Entries.Count == 1)
                return Entries.Single();

            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentEntryFile);
            var otherEntries = Entries.ToList();
            if(currentEntry != null)
                otherEntries.Remove(currentEntry);

            return otherEntries[_rng.Next(0, otherEntries.Count)];
        }

        protected virtual void OnPlayEntry(PlaylistEntry entry)
        {
            SelectedEntry = entry;
            PlayEntry?.Invoke(this, entry);
        }


        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RequestPlayEntry(PlaylistEntry entry)
        {
            OnPlayEntry(entry);
        }
    }
}
