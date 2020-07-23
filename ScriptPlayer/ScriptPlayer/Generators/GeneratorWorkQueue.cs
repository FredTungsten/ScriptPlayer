using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Generators
{
    public class GeneratorWorkQueue : DependencyObject, IDisposable
    {
        public event EventHandler<GeneratorJobEventArgs> JobFinished;

        public event EventHandler<GeneratorJobEventArgs> JobStarted;

        public static readonly DependencyProperty TotalProgressProperty = DependencyProperty.Register(
            "TotalProgress", typeof(double), typeof(GeneratorWorkQueue), new PropertyMetadata(default(double)));

        public double TotalProgress
        {
            get => (double) GetValue(TotalProgressProperty);
            set => SetValue(TotalProgressProperty, value);
        }

        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register(
            "IsEmpty", typeof(bool), typeof(GeneratorWorkQueue), new PropertyMetadata(true));

        public bool IsEmpty
        {
            get => (bool) GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }

        public static readonly DependencyProperty IsDoneProperty = DependencyProperty.Register(
            "IsDone", typeof(bool), typeof(GeneratorWorkQueue), new PropertyMetadata(default(bool)));

        public bool IsDone
        {
            get => (bool) GetValue(IsDoneProperty);
            set => SetValue(IsDoneProperty, value);
        }
        
        private readonly BlockingQueue<GeneratorJob> _unprocessedJobs = new BlockingQueue<GeneratorJob>();

        private Thread[] _workerThreads;
        private GeneratorJob[] _activeJobs;

        private bool _running;
        
        public ObservableCollection<GeneratorEntry> Entries { get; }

        public GeneratorWorkQueue()
        {
            Entries = new ObservableCollection<GeneratorEntry>();
            Entries.CollectionChanged += EntriesOnCollectionChanged;
        }

        public void Prioritize(string[] videoFiles)
        {
            _unprocessedJobs.Prioritize(
                (a,b) => Array.IndexOf(videoFiles, a.VideoFileName).CompareTo(Array.IndexOf(videoFiles, b.VideoFileName)), 
                p => videoFiles.Contains(p.VideoFileName));
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

            UpdateProgress();
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
            UpdateProgress();
        }
        
        public void UpdateProgress()
        {
            var entries = Entries.ToList();

            double prog = 0;
            if (entries.Count > 0)
            {
                prog = entries.Sum(e => e.Progress) / entries.Count;
                IsEmpty = false;
                IsDone = entries.All(e => e.DoneType != JobDoneTypes.NotDone);
            }
            else
            {
                IsDone = false;
                IsEmpty = true;
            }

            double progress = Math.Round(prog, 3);

            TotalProgress = Math.Min(Math.Max(0, progress), 1);
        }
        
        public void Enqueue(GeneratorJob job)
        {
            GeneratorEntry newEntry = job.CreateEntry();

            if (UnfinishedEntryExists(newEntry.Job))
                return;

            Entries.Add(newEntry);

            job.CheckSkip();

            _unprocessedJobs.Enqueue(job);
        }

        private bool UnfinishedEntryExists(GeneratorJob job)
        {
            return Entries.Any(e => e.DoneType == JobDoneTypes.NotDone && e.Job.HasIdenticalSettings(job));
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
                _activeJobs[processIndex] = _unprocessedJobs.Dequeue();
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