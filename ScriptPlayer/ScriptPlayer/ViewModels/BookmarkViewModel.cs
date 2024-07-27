using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class BookmarkViewModel : INotifyPropertyChanged
    {
        private string _label;
        private string _filePath;
        private TimeSpan _timestamp;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Label
        {
            get => _label;
            set
            {
                if (value == _label) return;
                _label = value;
                OnPropertyChanged();
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (value == _filePath) return;
                _filePath = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan Timestamp
        {
            get => _timestamp;
            set
            {
                if (value.Equals(_timestamp)) return;
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public Bookmark ToModel()
        {
            return new Bookmark
            {
                Label = Label,
                FilePath = FilePath,
                Timestamp = Timestamp
            };
        }

        public static BookmarkViewModel FromModel(Bookmark bookmark)
        {
            return new BookmarkViewModel
            {
                Label = bookmark.Label,
                Timestamp = bookmark.Timestamp,
                FilePath = bookmark.FilePath
            };
        }
    }
}