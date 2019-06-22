using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace ScriptPlayer.Generators
{
    public class GeneratorEntry : INotifyPropertyChanged
    {
        public JobDoneTypes DoneType { get; set; }

        public JobStates State { get; set; }

        public string Type { get; set; }

        public string Filename { get; set; }

        private double _progress;
        private string _status;

        public GeneratorJob Job { get; internal set; }

        private readonly Dispatcher _dispatcher;

        public string Status
        {
            get => _status;
            private set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public void Update(string status, double progress)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(() => { Update(status, progress); }));
                return;
            }

            if(Progress >= 0)
                Progress = progress;

            if(status != null)
                Status = status;
        }

        public GeneratorEntry(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            Status = "Queued";
            Progress = 0;
            DoneType = JobDoneTypes.NotDone;
            State = JobStates.Queued;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}