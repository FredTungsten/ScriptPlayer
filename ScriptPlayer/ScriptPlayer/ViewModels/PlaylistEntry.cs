using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistEntry : INotifyPropertyChanged
    {
        private TimeSpan? _duration;
        private bool _hasMedia;
        private bool _hasScript;
        private PlaylistEntryStatus _status;

        public PlaylistEntry()
        {
            Status = PlaylistEntryStatus.Loading;
        }

        public PlaylistEntry(string filename)
        {
            Fullname = filename;
            Shortname = System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        public string Shortname { get; set; }
        public string Fullname { get; set; }

        public bool Removed { get; set; }

        public PlaylistEntryStatus Status
        {
            get => _status;
            protected set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan? Duration
        {
            get => _duration;
            set
            {
                if (value.Equals(_duration)) return;
                _duration = value;
                OnPropertyChanged();
            }
        }

        public bool HasMedia
        {
            get => _hasMedia;
            set
            {
                if (value == _hasMedia) return;
                _hasMedia = value;
                OnPropertyChanged();
            }
        }

        public void UpdateStatus()
        {
            Status = HasMedia && HasScript ? PlaylistEntryStatus.FilesOk : PlaylistEntryStatus.MissingFile;
        }

        public void Reset()
        {
            Status = PlaylistEntryStatus.Loading;
            Duration = null;
        }

        public bool HasScript
        {
            get => _hasScript;
            set
            {
                if (value == _hasScript) return;
                _hasScript = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum PlaylistEntryStatus
    {
        Loading,
        MissingFile,
        FilesOk
    }
}