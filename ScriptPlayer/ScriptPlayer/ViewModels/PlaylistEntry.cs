using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class PlaylistEntry : INotifyPropertyChanged
    {
        private TimeSpan? _duration;

        public PlaylistEntry()
        { }

        public PlaylistEntry(string filename)
        {
            Fullname = filename;
            Shortname = System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        public string Shortname { get; set; }
        public string Fullname { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}