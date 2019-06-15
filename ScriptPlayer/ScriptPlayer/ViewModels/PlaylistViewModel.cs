using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Helpers;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<PlaylistEntry> PlayEntry;

        private List<PlaylistEntry> _previousEntries = new List<PlaylistEntry>();

        private readonly BlockingQueue<PlaylistEntry> _uncheckedPlaylistEntries = new BlockingQueue<PlaylistEntry>();

        private readonly Dispatcher _dispatcher;
        private readonly Random _rng = new Random();
        private readonly Thread _mediaInfoThread;

        private bool _disposed;

        private bool _randomChapters;
        private bool _repeatSingleFile;
        private bool _shuffle;
        private bool _repeat;

        private ObservableCollection<PlaylistEntry> _entries;
        private List<PlaylistEntry> _selectedEntries;
        private List<PlaylistEntry> _filteredEntries;
        private PlaylistEntry _selectedEntry;
        
        private string _filter;
        private TimeSpan _totalDuration;
        private TimeSpan _selectedDuration;
        private string _totalDurationString = "0:00:00";
        private string _selectedDurationString = "0:00:00";
        private PlaylistEntry _currentEntry;
        private PlaylistEntry _previousEntry;
        private PlaylistEntry _nextEntry;

        public TimeSpan SelectedDuration
        {
            get => _selectedDuration;
            private set
            {
                if (value.Equals(_selectedDuration)) return;
                _selectedDuration = value;
                SelectedDurationString = $"{_selectedDuration.Days * 24 + _selectedDuration.Hours:0}:{_selectedDuration.Minutes:00}:{_selectedDuration.Seconds:00}";
                OnPropertyChanged();
            }
        }

        public event EventHandler<RequestEventArgs<string>> RequestMediaFileName;
        public event EventHandler<RequestEventArgs<string>> RequestVideoFileName;
        public event EventHandler<RequestEventArgs<string>> RequestScriptFileName;

        public event EventHandler<string[]> RequestGenerateThumbnails;
        public event EventHandler<string[]> RequestGeneratePreviews;
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
            UpdateTotalDuration();
            UpdateNextAndPrevious();
        }

        private void UpdateTotalDuration()
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(UpdateTotalDuration));
                return;
            }

            TotalDuration = Entries?.Aggregate(TimeSpan.Zero, (span, entry) => span + (entry.Duration ?? TimeSpan.Zero)) ?? TimeSpan.Zero;

            UpdateSelectedDuration();
        }

        private void UpdateSelectedDuration()
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(UpdateSelectedDuration));
                return;
            }

            SelectedDuration = SelectedEntries?.Aggregate(TimeSpan.Zero, (span, entry) => span + entry.Duration ?? TimeSpan.Zero) ?? TimeSpan.Zero;
        }

        public TimeSpan TotalDuration   
        {
            get => _totalDuration;
            private set
            {
                if (value.Equals(_totalDuration)) return;
                _totalDuration = value;
                TotalDurationString = $"{_totalDuration.Days * 24 + _totalDuration.Hours:0}:{_totalDuration.Minutes:00}:{_totalDuration.Seconds:00}";
                OnPropertyChanged();

            }
        }

        public string SelectedDurationString
        {
            get => _selectedDurationString;
            private set
            {
                if (value == _selectedDurationString) return;
                _selectedDurationString = value;
                OnPropertyChanged();
            }
        }

        public string TotalDurationString
        {
            get => _totalDurationString;
            private set
            {
                if (value == _totalDurationString) return;
                _totalDurationString = value;
                OnPropertyChanged();
            }
        }

        public bool Shuffle
        {
            get => _shuffle;
            set
            {
                if (value == _shuffle) return;
                _shuffle = value;

                UpdateNextAndPrevious();
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        private void UpdateNextEntry()
        {
            NextEntry = GetNextEntry();

            if (PreviousEntry == null || PreviousEntry == NextEntry)
                PreviousEntry = GetPreviousEntry();
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

                UpdateNextEntry();
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

                UpdateNextAndPrevious();
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }

        private void UpdateNextAndPrevious()
        {
            PreviousEntry = GetPreviousEntry();
            NextEntry = GetNextEntry();
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
        public RelayCommand PlayNextEntryCommand { get; set; }
        public RelayCommand PlayPreviousEntryCommand { get; set; }
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
        public RelayCommand GeneratePreviewsForSelectedVideosCommand { get; set; }
        public RelayCommand RecheckAllCommand { get; set; }
        public int EntryCount => Entries.Count;

        
        public PlaylistViewModel()
        {
            Entries = new ObservableCollection<PlaylistEntry>();
            SelectedEntries = new List<PlaylistEntry>();

            OpenInExplorerCommand = new RelayCommand<PlaylistEntry>(ExecuteOpenInExplorer, EntryNotNull);
            MoveSelectedEntryDownCommand = new RelayCommand(ExecuteMoveSelectedEntryDown, CanMoveSelectedEntryDown);
            MoveSelectedEntryUpCommand = new RelayCommand(ExecuteMoveSelectedEntryUp, CanMoveSelectedEntryUp);
            MoveSelectedEntryLastCommand = new RelayCommand(ExecuteMoveSelectedEntryLast, CanMoveSelectedEntryDown);
            MoveSelectedEntryFirstCommand = new RelayCommand(ExecuteMoveSelectedEntryFirst, CanMoveSelectedEntryUp);
            RemoveSelectedEntryCommand = new RelayCommand(ExecuteRemoveSelectedEntry, CanRemoveSelectedEntry);
            ClearPlaylistCommand = new RelayCommand(ExecuteClearPlaylist, CanClearPlaylist);
            PlayNextEntryCommand = new RelayCommand(ExecutePlayNextEntry, CanPlayNextEntry);
            PlayPreviousEntryCommand = new RelayCommand(ExecutePlayPreviousEntry, CanPlayPreviousEntry);
            SortByDurationCommand = new RelayCommand<bool>(ExecuteSortByDuration, CanSort);
            SortByNameCommand = new RelayCommand<bool>(ExecuteSortByName, CanSort);
            SortByPathCommand = new RelayCommand<bool>(ExecuteSortByPath, CanSort);
            SortShuffleCommand = new RelayCommand(ExecuteSortShuffle, CanSort);
            GenerateThumbnailsForSelectedVideosCommand = new RelayCommand(ExecuteGenerateThumbnailsForSelectedVideos, AreEntriesSelected);
            GeneratePreviewsForSelectedVideosCommand = new RelayCommand(ExecuteGeneratePreviewsForSelectedVideos, AreEntriesSelected);
            RecheckAllCommand = new RelayCommand(ExecuteRecheckAll);

            _dispatcher = Dispatcher.CurrentDispatcher;
            _mediaInfoThread = new Thread(MediaInfoLoop);
            _mediaInfoThread.Start();
        }

        private void ExecuteRecheckAll()
        {
            RecheckAllEntries();
        }

        private void ExecuteOpenInExplorer(PlaylistEntry obj)
        {
            OpenInExplorer(obj.Fullname);
        }

        private bool EntryNotNull(PlaylistEntry arg)
        {
            return arg != null;
        }

        private bool AreEntriesSelected()
        {
            if (_selectedEntries == null)
                return false;
            return _selectedEntries.Count > 0;
        }

        private void ExecuteGenerateThumbnailsForSelectedVideos()
        {
            string[] videos = _selectedEntries.Select(e => OnRequestVideoFileName(e.Fullname)).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            if (videos.Length == 0)
                return;

            OnRequestGenerateThumbnails(videos);
        }

        private void ExecuteGeneratePreviewsForSelectedVideos()
        {
            string[] videos = _selectedEntries.Select(e => OnRequestVideoFileName(e.Fullname)).Where(v => !string.IsNullOrEmpty(v)).ToArray();
            if (videos.Length == 0)
                return;

            OnRequestGeneratePreviews(videos);
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

                if (entry.Removed)
                    continue;

                string mediaFile = OnRequestMediaFileName(entry.Fullname);
                if (!string.IsNullOrEmpty(mediaFile) && File.Exists(mediaFile))
                {
                    entry.HasMedia = true;

                    if (entry.Duration == null)
                    {
                        entry.Duration = MediaHelper.GetDuration(mediaFile);
                        if (entry.Duration != null)
                            UpdateTotalDuration();
                    }
                }
                else
                {
                    entry.HasMedia = false;
                    entry.Duration = null;
                }

                string scriptFile = OnRequestScriptFileName(entry.Fullname);
                entry.HasScript = !string.IsNullOrWhiteSpace(scriptFile) && File.Exists(scriptFile);

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

        private bool CanPlayPreviousEntry()
        {
            bool canPlaySameAgain = CurrentEntry != null;

            if (Repeat)
                return Entries.Count > 0 || canPlaySameAgain;

            if (RepeatSingleFile)
                return canPlaySameAgain;

            if (Entries.Count == 0)
                return false;

            if (CurrentEntry == null)
                return true;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(CurrentEntry);

            return index > 0;
        }

        public bool CanPlayNextEntry()
        {
            bool canPlaySameAgain = CurrentEntry != null;

            if (Repeat)
                return Entries.Count > 0 || canPlaySameAgain;

            if (RepeatSingleFile)
                return canPlaySameAgain;

            if (Entries.Count == 0)
                return false;

            if (CurrentEntry == null)
                return true;

            if (Shuffle)
                return Entries.Count > 1;

            int index = Entries.IndexOf(CurrentEntry);

            return index < Entries.Count -1;
        }

        private void ExecutePlayPreviousEntry()
        {
            PlayPreviousEntry();
        }

        public void PlayPreviousEntry()
        {
            if (!CanPlayPreviousEntry())
                return;

            PlaylistEntry entry = PreviousEntry;
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }


        public void PlayNextEntry()
        {
            if (!CanPlayNextEntry())
                return;

            PlaylistEntry entry = NextEntry;
            if (entry == null)
                return;

            OnPlayEntry(entry);
        }

        private void ExecutePlayNextEntry()
        {
            PlayNextEntry();
        }

        private bool CanRemoveSelectedEntry()
        {
            return !SelectionIsEmpty();
        }

        private void ExecuteRemoveSelectedEntry()
        {
            if (!CanRemoveSelectedEntry())
                return;

            var itemsToRemove = _selectedEntries.OrderBy(i => Entries.IndexOf(i)).ToList();
            int currentIndex = Entries.IndexOf(_selectedEntries.First());

            foreach (var item in itemsToRemove)
            {
                item.Removed = true;
                Entries.Remove(item);

                if (NextEntry == item)
                    NextEntry = null;

                if (PreviousEntry == item)
                    PreviousEntry = null;

                _previousEntries.Remove(item);
            }

            if (currentIndex < Entries.Count)
                SelectedEntry = Entries[currentIndex];
            else if (Entries.Count > 0)
                SelectedEntry = Entries[Entries.Count - 1];
            else
                SelectedEntry = null;

            UpdateNextAndPreviousIfNull();

            CommandManager.InvalidateRequerySuggested();
        }
        
        private bool SelectionHasGap()
        {
            List<int> indices = _selectedEntries.Select(i => Entries.IndexOf(i)).ToList();
            return indices.Max() - indices.Min() + 1 != indices.Count;
        }

        private bool SelectionIsEmpty()
        {
            if (_selectedEntries == null)
                return true;

            return _selectedEntries.Count <= 0;
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

            return !_selectedEntries.Contains(up ? Entries.First() : Entries.Last());
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

            var orderedSelection = _selectedEntries.OrderBy(i => Entries.IndexOf(i)).ToList();
            
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
            EnsureMediaInfo(entry);
            Entries.Add(entry);

            UpdateNextAndPreviousIfNull();

            CommandManager.InvalidateRequerySuggested();
        }

        private void RecheckAllEntries()
        {
            _uncheckedPlaylistEntries.Clear();

            foreach (PlaylistEntry entry in Entries)
            {
                entry.Reset();
                _uncheckedPlaylistEntries.Enqueue(entry);
            }
        }

        private void EnsureMediaInfo(PlaylistEntry entry)
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
                EnsureMediaInfo(entry);
                Entries.Add(entry);
            }

            UpdateNextAndPreviousIfNull();

            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateNextAndPreviousIfNull()
        {
            if (NextEntry == null)
                NextEntry = GetNextEntry();

            if (PreviousEntry == null || PreviousEntry == NextEntry)
                PreviousEntry = GetPreviousEntry();
        }

        public void AddEntries(IEnumerable<PlaylistEntry> entries)
        {
            foreach (PlaylistEntry entry in entries)
            {
                EnsureMediaInfo(entry);
                Entries.Add(entry);
            }

            UpdateNextAndPreviousIfNull();

            CommandManager.InvalidateRequerySuggested();
        }

        public void Clear()
        {
            foreach (var entry in Entries)
                entry.Removed = true;

            Entries.Clear();
            _previousEntries.Clear();
            PreviousEntry = null;
            NextEntry = null;
            CommandManager.InvalidateRequerySuggested();
        }

        public void SetCurrentEntry(string[] files)
        {
            PlaylistEntry existingEntry = GetEntry(files);
            if (existingEntry == CurrentEntry)
                return;

            if (existingEntry == null)
            {

            }

            CurrentEntry = existingEntry;
            PreviousEntry = GetPreviousEntry();
            NextEntry = GetNextEntry();
        }

        public PlaylistEntry CurrentEntry
        {
            get => _currentEntry;
            set
            {
                if (Equals(value, _currentEntry)) return;
                _currentEntry = value;

                _previousEntries.Remove(_currentEntry);
                _previousEntries.Insert(0,_currentEntry);
                OnPropertyChanged();
            }
        }

        public PlaylistEntry PreviousEntry
        {
            get => _previousEntry;
            set
            {
                if (Equals(value, _previousEntry)) return;
                _previousEntry = value;
                OnPropertyChanged();
            }
        }

        public PlaylistEntry NextEntry
        {
            get => _nextEntry;
            set
            {
                if (Equals(value, _nextEntry)) return;
                _nextEntry = value;
                OnPropertyChanged();
            }
        }

        private PlaylistEntry GetEntry(string[] files)
        {
            return Entries.FirstOrDefault(p => files.Contains(p.Fullname));
        }

        public PlaylistEntry FirstEntry()
        {
            return Entries.FirstOrDefault();
        }

        public PlaylistEntry GetNextEntry()
        {
            if(RepeatSingleFile && CurrentEntry != null)
                return CurrentEntry;

            if(Shuffle)
                return GetRandomEntry();

            if (CurrentEntry == null)
                return FirstEntry();

            int currentIndex = Entries.IndexOf(CurrentEntry);

            if (currentIndex == Entries.Count - 1)
            {
                if (Repeat)
                {
                    if (Entries.Count > 0)
                        return Entries.First();
                    if (CurrentEntry != null)
                        return CurrentEntry;
                }
                return null;
            }

            var nextEntry = Entries[currentIndex + 1];

            return nextEntry;
        }

        public PlaylistEntry GetPreviousEntry()
        {
            if (RepeatSingleFile && CurrentEntry != null)
                return CurrentEntry;

            if (Shuffle)
                return GetRandomEntry();

            if (CurrentEntry == null)
            {
                if (Repeat)
                    return LastEntry();

                return null;
            }


            int currentIndex = Entries.IndexOf(CurrentEntry);

            if(currentIndex == 0)
            {
                if (Repeat)
                {
                    if (Entries.Count > 0)
                        return Entries.First();
                    if(CurrentEntry != null)
                        return CurrentEntry;
                }
                return null;
            }

            if (currentIndex == -1)
            {
                return FirstEntry();
            }

            var previousEntry = Entries[currentIndex - 1];

            return previousEntry;
        }

        private PlaylistEntry LastEntry()
        {
            return Entries.LastOrDefault();
        }

        private PlaylistEntry GetRandomEntry()
        {
            if (Entries.Count == 0)
                return null;

            if (Entries.Count == 1)
                return Entries.Single();

            var otherEntries = Entries.ToList();
            if(CurrentEntry != null)
                otherEntries.Remove(CurrentEntry);

            //Makes selection a little less random
            const double factor = 0.5;
            int entryCount = (int)Math.Floor(otherEntries.Count * factor);
            var recentEntries = _previousEntries.Take(entryCount).ToList();

            otherEntries.RemoveAll(p => recentEntries.Contains(p));

            return otherEntries[_rng.Next(0, otherEntries.Count)];
        }

        protected virtual void OnPlayEntry(PlaylistEntry entry)
        {
            PreviousEntry = CurrentEntry;

            SelectedEntry = entry;
            CurrentEntry = entry;
            UpdateNextEntry();
            
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
            SelectedEntries = entries.ToList();
            UpdateSelectedDuration();
        }

        public List<PlaylistEntry> SelectedEntries
        {
            get => _selectedEntries;
            private set
            {
                if (Equals(value, _selectedEntries)) return;
                _selectedEntries = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnRequestGenerateThumbnails(string[] videos)
        {
            RequestGenerateThumbnails?.Invoke(this, videos);
        }

        protected virtual void OnRequestGeneratePreviews(string[] videos)
        {
            RequestGeneratePreviews?.Invoke(this, videos);
        }

        public static void OpenInExplorer(string path)
        {
            ProcessStartInfo info = new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"");
            Process.Start(info);
        }
    }
}
