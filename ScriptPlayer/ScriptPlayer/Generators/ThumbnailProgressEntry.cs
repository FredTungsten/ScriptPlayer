using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.Generators
{
    public class ThumbnailProgressEntry : INotifyPropertyChanged
    {
        private double _progress;
        private string _status;

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public bool SkipThis { get; set; }

        public double Progress
        {
            get => _progress;
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public ThumbnailProgressEntry(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Progress = 0;
            Status = "Queued";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}