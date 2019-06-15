using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Generators
{
    public class GeneratorWorkQueue : IDisposable, INotifyPropertyChanged
    {
        public double TotalProgress
        {
            get => _totalProgress;
            set
            {
                if (value.Equals(_totalProgress)) return;
                _totalProgress = value;
                OnPropertyChanged();
            }
        }

        private readonly BlockingQueue<GeneratorJob> _unprocessedJobs = new BlockingQueue<GeneratorJob>();

        private Thread _workerThread;
        private bool _running;
        private GeneratorJob _activeJob;
        private double _totalProgress;

        public ObservableCollection<GeneratorEntry> Entries { get; }

        public GeneratorWorkQueue()
        {
            Entries = new ObservableCollection<GeneratorEntry>();
            Entries.CollectionChanged += EntriesOnCollectionChanged;
        }

        private void EntriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            if (eventArgs.OldItems != null)
            {
                foreach (GeneratorEntry entry in eventArgs.OldItems)
                    RemoveEvents(entry);
            }

            if (eventArgs.NewItems != null)
            {
                foreach (GeneratorEntry entry in eventArgs.NewItems)
                    AddEvents(entry);
            }
        }

        private void RemoveEvents(GeneratorEntry entry)
        {
            entry.PropertyChanged -= EntryOnPropertyChanged;
        }

        private void AddEvents(GeneratorEntry entry)
        {
            entry.PropertyChanged += EntryOnPropertyChanged;
        }

        private void EntryOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            double progress = Math.Round(GetProgress(), 3);
            TotalProgress = Math.Min(Math.Max(0, progress), 1);
        }
        
        public double GetProgress()
        {
            var entries = Entries.ToList();

            if (entries.Count == 0)
                return 0;

            return entries.Sum(e => e.Progress) / entries.Count;
        }
        
        public void Enqueue(GeneratorJob job)
        {
            Entries.Add(job.CreateEntry());

            job.CheckSkip();

            _unprocessedJobs.Enqueue(job);
        }

        public void Start()
        {
            _workerThread = new Thread(WorkerLoop);
            _running = true;
            _workerThread.Start();
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                _activeJob = _unprocessedJobs.Deqeue();
                if (_activeJob == null)
                    return;

                _activeJob.Process();
                _activeJob = null;
            }
        }

        public void Dispose()
        {
            _unprocessedJobs.Close();
            _running = false;

            _activeJob?.Cancel();

            _workerThread.Join(1000);
        }

        public void RemoveDone()
        {
            var toRemove = Entries.Where(e => e.State == JobStates.Done).ToList();

            foreach (var entry in toRemove)
                Entries.Remove(entry);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}