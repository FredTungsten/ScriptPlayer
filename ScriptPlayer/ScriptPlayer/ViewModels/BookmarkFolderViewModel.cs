using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace ScriptPlayer.ViewModels
{
    public class BookmarkFolderViewModel : INotifyPropertyChanged
    {
        private string _label;
        private ObservableCollection<BookmarkViewModel> _bookmarks;
        private ObservableCollection<BookmarkFolderViewModel> _folders;
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

        public ObservableCollection<BookmarkViewModel> Bookmarks
        {
            get => _bookmarks;
            set
            {
                if (Equals(value, _bookmarks)) return;
                _bookmarks = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<BookmarkFolderViewModel> Folders
        {
            get => _folders;
            set
            {
                if (Equals(value, _folders)) return;
                _folders = value;
                OnPropertyChanged();
            }
        }

        public BookmarkFolder ToModel()
        {
            BookmarkFolder root = new BookmarkFolder {Label = Label};

            if (Bookmarks != null && Bookmarks.Count > 0)
            {
                root.Bookmarks = new List<Bookmark>();
                foreach(BookmarkViewModel bookmark in Bookmarks)
                    root.Bookmarks.Add(bookmark.ToModel());
            }

            if (Folders != null && Folders.Count > 0)
            {
                root.Folders = new List<BookmarkFolder>();
                foreach(BookmarkFolderViewModel folder in Folders)
                    root.Folders.Add(folder.ToModel());
            }

            return root;
        }

        public static BookmarkFolderViewModel FromModel(BookmarkFolder model)
        {
            BookmarkFolderViewModel root = new BookmarkFolderViewModel();
            root.Label = model.Label;

            if (model.Bookmarks != null && model.Bookmarks.Count > 0)
            {
                root.Bookmarks = new ObservableCollection<BookmarkViewModel>();
                foreach(Bookmark bookmark in model.Bookmarks)
                    root.Bookmarks.Add(BookmarkViewModel.FromModel(bookmark));
            }

            if (model.Folders != null && model.Folders.Count > 0)
            {
                root.Folders = new ObservableCollection<BookmarkFolderViewModel>();
                foreach (BookmarkFolder folder in model.Folders)
                {
                    root.Folders.Add(BookmarkFolderViewModel.FromModel(folder));
                }
            }

            return root;
        }

        public void Save(string filePath)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BookmarkFolder root = this.ToModel();
                root.Save(stream);

                byte[] file = stream.ToArray();

                File.WriteAllBytes(filePath, file);
            }
        }

        public static BookmarkFolderViewModel Load(string filePath)
        {
            BookmarkFolder root = BookmarkFolder.FromFile(filePath);
            return BookmarkFolderViewModel.FromModel(root);
        }
    }
}