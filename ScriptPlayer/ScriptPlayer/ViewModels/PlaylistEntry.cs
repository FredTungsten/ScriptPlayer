using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistEntry : INotifyPropertyChanged
    {
        private TimeSpan? _duration;
        private bool _hasMedia;
        private bool _hasScript;
        private PlaylistEntryStatus _status;
        private string _shortname;
        private string _fullname;
        private bool _removed;
        private ImageSource _heatMap;
        private DateTime _mediaCreationTime;
        private DateTime _scriptCreationTime;

        public PlaylistEntry()
        {
            Status = PlaylistEntryStatus.Loading;
        }

        public PlaylistEntry(string filename)
        {
            Fullname = filename;
        }

        public string Shortname
        {
            get => _shortname;
            set
            {
                if (value == _shortname) return;
                _shortname = value;
                OnPropertyChanged();
            }
        }

        public string Fullname
        {
            get => _fullname;
            set
            {
                if (value == _fullname) return;
                _fullname = value;
                Shortname = System.IO.Path.GetFileNameWithoutExtension(_fullname);
                OnPropertyChanged();
            }
        }

        public bool Removed
        {
            get => _removed;
            set
            {
                if (value == _removed) return;
                _removed = value;
                OnPropertyChanged();
            }
        }

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

        public ImageSource HeatMap
        {
            get => _heatMap;
            set
            {
                if (Equals(value, _heatMap)) return;
                _heatMap = value;
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

        public DateTime MediaCreationTime
        {
            get => _mediaCreationTime;
            set
            {
                if (value.Equals(_mediaCreationTime)) return;
                _mediaCreationTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime ScriptCreationTime
        {
            get => _scriptCreationTime;
            set
            {
                if (value.Equals(_scriptCreationTime)) return;
                _scriptCreationTime = value;
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
            HeatMap = null;
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