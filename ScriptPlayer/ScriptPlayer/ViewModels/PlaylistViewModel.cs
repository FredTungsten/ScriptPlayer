using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PlaylistEntry> PlayEntry;

        private readonly BlockingQueue<PlaylistEntry> _uncheckedPlaylistEntries = new BlockingQueue<PlaylistEntry>();

        private readonly Random _rng = new Random();
        private readonly Thread _mediaInfoThread;

        private bool _disposed;

        private bool _randomChapters;
        private bool _repeatSingleFile;
        private bool _shuffle;
        private bool _repeat;

        private ObservableCollection<PlaylistEntry> _entries;
        private List<PlaylistEntry> _selectedItems;
        private List<PlaylistEntry> _filteredEntries;
        private PlaylistEntry _selectedEntry;
        
        private string _filter;
        
        public event EventHandler<RequestEventArgs<string>> RequestMediaFileName;
        public event EventHandler<RequestEventArgs<string>> RequestVideoFileName;
        public event EventHandler<RequestEventArgs<string>> RequestScriptFileName;

        public event EventHandler<string[]> RequestGenerateThumbnails;
        public event EventHandler SelectedEntryMoved; 

        public ObservableCollection<PlaylistEntry> Entries
        {
            get => _entries;
            set
            {
                if (Equals(value, _entries)) return;
                UpdateEntryEvents(_entries, value);
                _entries = value;
                UpdateFilter();
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public List<PlaylistEntry> FilteredEntries
        {
            get => _filteredEntries;
            set
            {
                if (Equals(value, _filteredEntries)) return;
                _filteredEntries = value;
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
            UpdateFilter();
            CommandManager.InvalidateRequerySuggested();
        }

        public bool Shuffle
        {
            get => _shuffle;
            set
            {
                if (value == _shuffle) return;
                _shuffle = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public bool RandomChapters
        {
            get => _randomChapters;
            set
            {
                if (value == _randomChapters) return;
                _randomChapters = value;
                OnPropertyChanged();
            }
        }

        public bool Repeat
        {
            get => _repeat;
            set
            {
                if (value == _repeat) return;
                _repeat = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public bool RepeatSingleFile
        {
            get => _repeatSingleFile;
            set
            {
                if (value == _repeatSingleFile) return;
                _repeatSingleFile = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public PlaylistEntry SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (Equals(value, _selectedEntry)) return;
                _selectedEntry = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        public string Filter        
        {
            get => _filter;
            set
            {
                if (value == _filter) return;
                _filter = value;
                UpdateFilter();
                OnPropertyChanged();
            }
        }

        private void UpdateFilter()
        {
            FilteredEntries = FilterEntries(Entries, Filter);
        }

        private List<PlaylistEntry> FilterEntries(IList<PlaylistEntry> entries, string filter)
        {
            List<PlaylistEntry> filtered = new List<PlaylistEntry>();

            foreach(PlaylistEntry entry in entries)
                if(MatchesFilter(entry.Shortname, filter))
                    filtered.Add(entry);

            return filtered;
        }

        private bool MatchesFilter(string text, string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return text.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public RelayCommand<PlaylistEntry> OpenInExplorerCommand { get; set; }
        public RelayCommand<string[]> PlayNextEntryCommand { get; set; }
        public RelayCommand<string[]> PlayPreviousEntryCommand { get; set; }
        public RelayCommand MoveSelectedEntryUpCommand { get; set; }
        public RelayCommand MoveSelectedEntryDownCommand { get; set; }
        public RelayCommand MoveSelectedEntryFirstCommand { get; set; }
        public RelayCommand MoveSelectedEntryLastCommand { get; set; }
        public RelayCommand RemoveSelectedEntryCommand { get; set; }
        public RelayCommand ClearPlaylistCommand { get; set; }
        public RelayCommand<bool> SortByDurationCommand { get; set; }
        public RelayCommand<bool> SortByNameCommand { get; set; }
        public RelayCommand<bool> SortByPathCommand { get; set; }
        public RelayCommand SortShuffleCommand { get; set; }
        public RelayCommand GenerateThumbnailsForSelectedVideosCommand { get; set; }
        public int EntryCount => Entries.Count;

        
        public PlaylistViewModel()
        {
            Entries = new ObservableCollection<PlaylistEntry>();

            OpenInExplorerCommand = new RelayCommand<PlaylistEntry>(ExecuteOpenInExplorer, EntryNotNull);
            MoveSelectedEntryDownCommand = new RelayCommand(ExecuteMoveSelectedEntryDown, CanMoveSelectedEntryDown);
            MoveSelectedEntryUpCommand = new RelayCommand(ExecuteMoveSelectedEntryUp, CanMoveSelectedEntryUp);
            MoveSelectedEntryLastCommand = new RelayCommand(ExecuteMoveSelectedEntryLast, CanMoveSelectedEntryDown);
            MoveSelectedEntryFirstCommand = new RelayCommand(ExecuteMoveSelectedEntryFirst, CanMoveSelectedEntryUp);
            RemoveSelectedEntryCommand = new RelayCommand(ExecuteRemoveSelectedEntry, CanRemoveSelectedEntry);
            ClearPlaylistCommand = new RelayCommand(ExecuteClearPlaylist, CanClearPlaylist);
            PlayNextEntryCommand = new RelayCommand<string[]>(ExecutePlayNextEntry, CanPlayNextEntry);
            PlayPreviousEntryCommand = new RelayCommand<string[]>(ExecutePlayPreviousEntry, CanPlayPreviousEntry);
            SortByDurationCommand = new RelayCommand<bool>(ExecuteSortByDuration, CanSort);
            SortByNameCommand = new RelayCommand<bool>(ExecuteSortByName, CanSort);
            SortByPathCommand = new RelayCommand<bool>(ExecuteSortByPath, CanSort);
            SortShuffleCommand = new RelayCommand(ExecuteSortShuffle, CanSort);
            GenerateThumbnailsForSelectedVideosCommand = new RelayCommand(ExecuteGenerateThumbnailsForSelectedVideos, CanGenerateThumbnailsForSelectedVideos);

            _mediaInfoThread = new Thread(MediaInfoLoop);
            _mediaInfoThread.Start();
        }

        private void ExecuteOpenInExplorer(PlaylistEntry obj)
        {
            OpenInExplorer(obj.Fullname);
        }

        private bool EntryNotNull(PlaylistEntry arg)
        {
            return arg != null;
        }

        private bool CanGenerateThumbnailsForSelectedVideos()
        {
            if (_selectedItems == null)
                return false;
            return _selectedItems.Count > 0;
        }

        private void ExecuteGenerateThumbnailsForSelectedVideos()
        {
            string[] videos = _selectedItems.Select(e => OnRequestVideoFileName(e.Fullname)).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            if (videos.Length == 0)
                return;

            OnRequestGenerateThumbnails(videos);
        }

        private bool CanSort()
        {
            return Entries.Count > 1;
        }

        private void ExecuteSortShuffle()
        {
            List<PlaylistEntry> entries = Entries.ToList();
            List<PlaylistEntry> newOrder = new List<PlaylistEntry>();

            Random r = new Random();
            while (entries.Count > 0)
            {
                int next = r.Next(0, entries.Count);
                newOrder.Add(entries[next]);
                entries.RemoveAt(next);
            }

            Entries = new ObservableCollection<PlaylistEntry>(newOrder);
        }

        private void ExecuteSortByDuration(bool ascending)
        {
            SetEntries(Entries.OrderBy(e => e.Duration), ascending);
        }

        private void ExecuteSortByName(bool ascending)
        {
            SetEntries(Entries.OrderBy(e => e.Shortname), ascending);
        }

        private void ExecuteSortByPath(bool ascending)
        {
            SetEntries(Entries.OrderBy(e => e.Fullname), ascending);
        }

        private void SetEntries(IOrderedEnumerable<PlaylistEntry> entries, bool sortAscending)
        {
            Entries = new ObservableCollection<PlaylistEntry>(sortAscending ? entries : entries.Reverse());
        }

        private bool CanSort(bool arg)
        {
            return CanSort();
        }

        private void MediaInfoLoop()
        {
            while (!_disposed)
            {
                PlaylistEntry entry = _uncheckedPlaylistEntries.Deqeue();
                if (entry == null)
                    return;

                string mediaFile = OnRequestMediaFileName(entry.Fullname);
                if (!string.IsNullOrEmpty(mediaFile))
                {
                    entry.HasMedia = true;

                    if(entry.Duration == null)
                        entry.Duration = MediaHelper.GetDuration(mediaFile);
                }

                string scriptFile = OnRequestScriptFileName(entry.Fullname);
                entry.HasScript = !string.IsNullOrWhiteSpace(scriptFile);

                entry.UpdateStatus();

                //TODO Generate Preview
                //Brush heatmap = HeatMapGenerator.Generate2(timeStamps, TimeSpan.Zero, TimeSource.Duration);
            }
        }

        private bool CanClearPlaylist()
        {
            return _entries.Count > 0;
        }

        private void ExecuteClearPlaylist()
        {
            Clear();
        }

        private bool CanPlayPreviousEntry(params string[] currentFiles)
        {
            bool canPlaySameAgain = currentFiles != null && currentFiles.Length > 0 && !string.IsNullOrWhiteSpace(currentFiles.First());

            if (Repeat)
                return Entries.Count > 0 || canPlaySameAgain;

            if (RepeatSingleFile)
                return canPlaySameAgain;

            if (Entries.Count == 0)
                return false;

            if (currentFiles == null)
                return true;

            var currentEntry = Entries.FirstOrDefault(p => currentFiles.Contains(p.Fullname));
            if (currentEntry == null)
                return false;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(currentEntry);

            return index > 0;
        }

        public bool CanPlayNextEntry(params string[] currentFiles)
        {
            bool canPlaySameAgain = currentFiles != null && currentFiles.Length > 0 && !string.IsNullOrWhiteSpace(currentFiles.First());

            if (Repeat)
                return Entries.Count > 0 || canPlaySameAgain;

            if (RepeatSingleFile)
                return canPlaySameAgain;

            if (Entries.Count == 0)
                return false;

            if (currentFiles == null)
                return true;

            var currentEntry = Entries.FirstOrDefault(p => currentFiles.Contains(p.Fullname));
            if (currentEntry == null)
                return true;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(currentEntry);

            return index < Entries.Count -1;
        }
        private void ExecutePlayPreviousEntry(string[] currentFiles)
        {
            PlayPreviousEntry(currentFiles);
        }

        public void PlayPreviousEntry(params string[] currentFiles)
        {
            if (!CanPlayPreviousEntry(currentFiles))
                return;

            PlaylistEntry entry = GetPreviousEntry(currentFiles);
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }


        public void PlayNextEntry(params string[] currentFiles)
        {
            if (!CanPlayNextEntry(currentFiles))
                return;

            PlaylistEntry entry = GetNextEntry(currentFiles);
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }

        private void ExecutePlayNextEntry(string[] currentFiles)
        {
            PlayNextEntry(currentFiles);
        }

        private bool CanRemoveSelectedEntry()
        {
            return !SelectionIsEmpty();
        }

        private void ExecuteRemoveSelectedEntry()
        {
            if (!CanRemoveSelectedEntry())
                return;

            var itemsToRemove = _selectedItems.OrderBy(i => Entries.IndexOf(i)).ToList();
            int currentIndex = Entries.IndexOf(_selectedItems.First());

            foreach (var item in itemsToRemove)
                Entries.Remove(item);

            if (currentIndex < Entries.Count)
                SelectedEntry = Entries[currentIndex];
            else if (Entries.Count > 0)
                SelectedEntry = Entries[Entries.Count - 1];
            else
                SelectedEntry = null;

            CommandManager.InvalidateRequerySuggested();
        }
        
        private bool SelectionHasGap()
        {
            List<int> indices = _selectedItems.Select(i => Entries.IndexOf(i)).ToList();
            return indices.Max() - indices.Min() + 1 != indices.Count;
        }

        private bool SelectionIsEmpty()
        {
            if (_selectedItems == null)
                return true;

            return _selectedItems.Count <= 0;
        }

        private bool CanMoveSelectedEntryUp()
        {
            return CanMoveSelectedEntry(true);
        }

        private bool CanMoveSelectedEntryDown()
        {
            return CanMoveSelectedEntry(false);
        }

        private bool CanMoveSelectedEntry(bool up)
        {
            if (SelectionIsEmpty())
                return false;

            if (SelectionHasGap())
                return true;

            return !_selectedItems.Contains(up ? Entries.First() : Entries.Last());
        }

        private void ExecuteMoveSelectedEntryUp()
        {
            MoveSelection(true, false);
        }

        private void ExecuteMoveSelectedEntryDown()
        {
            MoveSelection(false, false);
        }

        private void ExecuteMoveSelectedEntryFirst()
        {
            MoveSelection(true, true);
        }

        private void ExecuteMoveSelectedEntryLast()
        {
            MoveSelection(false, true);
        }

        private void MoveSelection(bool up, bool allTheWay)
        {
            if (up && !CanMoveSelectedEntryUp()) return;
            if (!up && !CanMoveSelectedEntryDown()) return;

            bool hasGap = SelectionHasGap();
            int indexShift = hasGap ? 0 : 1;

            var orderedSelection = _selectedItems.OrderBy(i => Entries.IndexOf(i)).ToList();
            
            if (up)
            {
                int firstIndex;

                if (allTheWay)
                    firstIndex = 0;
                else
                    firstIndex = Entries.IndexOf(orderedSelection.First()) - indexShift;

                for (int i = 0; i < orderedSelection.Count; i++)
                {
                    int currentIndex = Entries.IndexOf(orderedSelection[i]);
                    if (currentIndex != firstIndex + i)
                        Entries.Move(currentIndex, firstIndex + i);
                }
            }
            else
            {
                int lastIndex;

                if (allTheWay)
                    lastIndex = Entries.Count - 1;
                else
                    lastIndex = Entries.IndexOf(orderedSelection.Last()) + indexShift;

                orderedSelection.Reverse();

                for (int i = 0; i < orderedSelection.Count; i++)
                {
                    int currentIndex = Entries.IndexOf(orderedSelection[i]);
                    if (currentIndex != lastIndex - i)
                        Entries.Move(currentIndex, lastIndex - i);
                }
            }

            OnSelectedEntryMoved();
        }

        public void AddEntry(PlaylistEntry entry)
        {
            EnsureDuration(entry);
            Entries.Add(entry);
            CommandManager.InvalidateRequerySuggested();
        }

        private void EnsureDuration(PlaylistEntry entry)
        {
            if (entry.Status == PlaylistEntryStatus.Loading)
            {
                _uncheckedPlaylistEntries.Enqueue(entry);
            }
        }


        public void AddEntries(string[] filenames)
        {
            foreach (string filename in filenames)
            {
                var entry = new PlaylistEntry(filename);
                EnsureDuration(entry);
                Entries.Add(entry);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        public void AddEntries(IEnumerable<PlaylistEntry> entries)
        {
            foreach (PlaylistEntry entry in entries)
            {
                EnsureDuration(entry);
                Entries.Add(entry);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        public void Clear()
        {
            Entries.Clear();
            CommandManager.InvalidateRequerySuggested();
        }

        public PlaylistEntry FirstEntry()
        {
            return Entries.FirstOrDefault();
        }

        public PlaylistEntry GetNextEntry(params string[] currentEntryFiles)
        {
            if(RepeatSingleFile && currentEntryFiles.Length > 0)
                return new PlaylistEntry(currentEntryFiles.First());

            if(Shuffle)
                return AnythingButThis(currentEntryFiles);

            var currentEntry = Entries.FirstOrDefault(p => currentEntryFiles.Contains(p.Fullname));

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);

            if (currentIndex == Entries.Count - 1)
            {
                if (Repeat)
                {
                    if (Entries.Count > 0)
                        return Entries.First();
                    if(currentEntryFiles.Length > 0)
                        return new PlaylistEntry(currentEntryFiles.First());
                }
                return null;
            }

            var nextEntry = Entries[currentIndex + 1];

            return nextEntry;
        }

        public PlaylistEntry GetPreviousEntry(params string[] currentEntryFiles)
        {
            if (RepeatSingleFile && currentEntryFiles.Length > 0)
                return new PlaylistEntry(currentEntryFiles.First());

            if (Shuffle)
                return AnythingButThis(currentEntryFiles);

            var currentEntry = Entries.FirstOrDefault(p => currentEntryFiles.Contains(p.Fullname));

            if (currentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(currentEntry);

            if(currentIndex == 0)
            {
                if (Repeat)
                {
                    if (Entries.Count > 0)
                        return Entries.First();
                    if(currentEntryFiles.Length > 0)
                        return new PlaylistEntry(currentEntryFiles.First());
                }
                return null;
            }

            var previousEntry = Entries[currentIndex - 1];

            return previousEntry;
        }

        private PlaylistEntry AnythingButThis(params string[] currentEntryFiles)
        {
            if (Entries.Count == 0)
                return null;
            if (Entries.Count == 1)
                return Entries.Single();

            var currentEntry = Entries.FirstOrDefault(p => currentEntryFiles.Contains(p.Fullname));
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

        protected virtual string OnRequestMediaFileName(string fileName)
        {
            RequestEventArgs<string> eventArgs = new RequestEventArgs<string>(fileName);
            RequestMediaFileName?.Invoke(this, eventArgs);

            return !eventArgs.Handled ? null : eventArgs.Value;
        }

        protected virtual string OnRequestScriptFileName(string fileName)
        {
            RequestEventArgs<string> eventArgs = new RequestEventArgs<string>(fileName);
            RequestScriptFileName?.Invoke(this, eventArgs);

            return !eventArgs.Handled ? null : eventArgs.Value;
        }

        protected virtual string OnRequestVideoFileName(string fileName)
        {
            RequestEventArgs<string> eventArgs = new RequestEventArgs<string>(fileName);
            RequestVideoFileName?.Invoke(this, eventArgs);

            return !eventArgs.Handled ? null : eventArgs.Value;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _uncheckedPlaylistEntries.Close();

            try
            {

                if (!_mediaInfoThread.Join(TimeSpan.FromSeconds(1)))
                    _mediaInfoThread.Abort();
            }
            catch { }
        }

        protected virtual void OnSelectedEntryMoved()
        {
            SelectedEntryMoved?.Invoke(this, EventArgs.Empty);
        }
        
        public void SetSelectedItems(IEnumerable<PlaylistEntry> entries)
        {
            _selectedItems = entries.ToList();
        }

        protected virtual void OnRequestGenerateThumbnails(string[] videos)
        {
            RequestGenerateThumbnails?.Invoke(this, videos);
        }

        public static void OpenInExplorer(string path)
        {
            ProcessStartInfo info = new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"");
            Process.Start(info);
        }
    }
}
