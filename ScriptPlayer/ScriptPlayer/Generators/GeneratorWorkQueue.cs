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
        public event EventHandler<GeneratorJobEventArgs> JobFinished;

        public event EventHandler<GeneratorJobEventArgs> JobStarted;

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

        private Thread[] _workerThreads;
        private GeneratorJob[] _activeJobs;

        private bool _running;
        private double _totalProgress;

        public ObservableCollection<GeneratorEntry> Entries { get; }

        public GeneratorWorkQueue()
        {
            Entries = new ObservableCollection<GeneratorEntry>();
            Entries.CollectionChanged += EntriesOnCollectionChanged;
        }

        public int UnprocessedJobCount => _unprocessedJobs.Count + _activeJobs.Count(job => job != null);

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

        public void Start(int threadCount = 1)
        {
            _workerThreads = new Thread[threadCount];
            _activeJobs = new GeneratorJob[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                _workerThreads[i] = new Thread(WorkerLoop)
                {
                    Name = "Generator Work Thread #" + i
                };
            }

            _running = true;
            for (int i = 0; i < threadCount; i++)
            {
                int processIndex = i;
                _workerThreads[i].Start(processIndex);
            }
        }

        private void WorkerLoop(object args)
        {
            int processIndex = (int) args;

            while (_running)
            {
                _activeJobs[processIndex] = _unprocessedJobs.Deqeue();
                if (_activeJobs[processIndex] == null)
                    return;

                OnJobStarted(_activeJobs[processIndex]);

                var result = _activeJobs[processIndex].Process();
                var job = _activeJobs[processIndex];
                _activeJobs[processIndex] = null;

                OnJobFinished(job, result);
            }
        }

        public void Dispose()
        {
            _unprocessedJobs.Close();
            _running = false;

            foreach(GeneratorJob job in _activeJobs)
                job?.Cancel();

            foreach(Thread thread in _workerThreads)
                thread.Join(200);
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

        protected virtual void OnJobFinished(GeneratorJob job, GeneratorResult result)
        {
            JobFinished?.Invoke(this, new GeneratorJobEventArgs(job, result));
        }

        protected virtual void OnJobStarted(GeneratorJob job)
        {
            JobStarted?.Invoke(this, new GeneratorJobEventArgs(job, null));    
        }
    }

    public class GeneratorJobEventArgs : EventArgs
    {
        public GeneratorJobEventArgs(GeneratorJob job, GeneratorResult result)
        {
            Job = job;
            Result = result;
        }

        public GeneratorResult Result { get; set; }

        public GeneratorJob Job { get; set; }
    }
}