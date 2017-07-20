using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Shared;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PlaylistEntry> _entries;
        private bool _shuffle;
        private PlaylistEntry _selectedEntry;

        public ObservableCollection<PlaylistEntry> Entries
        {
            get { return _entries; }
            set
            {
                if (Equals(value, _entries)) return;
                _entries = value;
                OnPropertyChanged();
            }
        }

        public bool Shuffle
        {
            get { return _shuffle; }
            set
            {
                if (value == _shuffle) return;
                _shuffle = value;
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

        public RelayCommand<string> PlayNextEntry { get; set; }
        public RelayCommand<string> PlayPreviousEntry { get; set; }
        public RelayCommand MoveSelectedEntryUp { get; set; }
        public RelayCommand MoveSelectedEntryDown { get; set; }
        public RelayCommand RemoveSelectedEntry { get; set; }
        public int EntryCount => Entries.Count;

        public PlaylistViewModel()
        {
            Entries = new ObservableCollection<PlaylistEntry>();

            MoveSelectedEntryDown = new RelayCommand(ExecuteMoveSelectedEntryDown, CanMoveSelectedEntryDown);
            MoveSelectedEntryUp = new RelayCommand(ExecuteMoveSelectedEntryUp, CanMoveSelectedEntryUp);
            RemoveSelectedEntry = new RelayCommand(ExecuteRemoveSelectedEntry, CanRemoveSelectedEntry);
            PlayNextEntry = new RelayCommand<string>(ExecutePlayNextEntry, CanPlayNextEntry);
            PlayPreviousEntry = new RelayCommand<string>(ExecutePlayPreviousEntry, CanPlayPreviousEntry);
        }

        private bool CanPlayPreviousEntry(string currentFile)
        {
            return Entries.Count > 0;
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

        private bool CanPlayNextEntry(string currentFile)
        {
            return Entries.Count > 0;
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
            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentEntryFile);

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);
            var nextEntry = Entries[(currentIndex + 1) % Entries.Count];

            SelectedEntry = nextEntry;

            return nextEntry;
        }

        public PlaylistEntry GetPreviousEntry(string currentEntryFile)
        {
            var currentEntry = Entries.FirstOrDefault(p => p.Fullname == currentEntryFile);

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);
            var previousEntry = Entries[(currentIndex +Entries.Count - 1) % Entries.Count];

            SelectedEntry = previousEntry;

            return previousEntry;
        }

        protected virtual void OnPlayEntry(PlaylistEntry e)
        {
            PlayEntry?.Invoke(this, e);
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
