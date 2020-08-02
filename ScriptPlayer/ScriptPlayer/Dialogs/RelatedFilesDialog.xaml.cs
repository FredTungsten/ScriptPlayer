using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for RelatedFilesDialog.xaml
    /// </summary>
    public partial class RelatedFilesDialog : Window
    {
        public static readonly DependencyProperty FileGroupsProperty = DependencyProperty.Register(
            "FileGroups", typeof(List<FileGroup>), typeof(RelatedFilesDialog), new PropertyMetadata(default(List<FileGroup>)));

        public List<FileGroup> FileGroups
        {
            get => (List<FileGroup>) GetValue(FileGroupsProperty);
            set => SetValue(FileGroupsProperty, value);
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(PlaylistViewModel), typeof(RelatedFilesDialog), new PropertyMetadata(default(PlaylistViewModel)));

        public PlaylistViewModel ViewModel
        {
            get => (PlaylistViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public RelatedFilesDialog(IEnumerable<string> filePaths, PlaylistViewModel viewModel, string titlePrefix)
        {
            ViewModel = viewModel;

            FileGroups = BuildGroups(filePaths);

            InitializeComponent();

            Title = titlePrefix + " all related files";
        }

        private List<FileGroup> BuildGroups(IEnumerable<string> filePaths)
        {
            return filePaths.Select(BuildFileGroup).ToList();
        }

        private FileGroup BuildFileGroup(string filePath)
        {
            string[] files = ViewModel.GetAllRelatedFiles(filePath);
            var group = new FileGroup
            {
                CommonName = Path.GetFileNameWithoutExtension(filePath),
            };

            if (files != null && files.Length > 0)
                group.Files = files.Select(f => new RelatedFile(f)).ToList();

            return group;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();
            DialogResult = true;
        }
    }

    public class FileGroup : INotifyPropertyChanged
    {
        private string _commonName;

        public string CommonName
        {
            get => _commonName;
            set
            {
                if (value == _commonName) return;
                _commonName = value;
                OnPropertyChanged();
            }
        }

        public List<RelatedFile> Files { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelatedFile
    {
        public RelatedFile(string filePath)
        {
            FullPath = filePath;
            FileName = Path.GetExtension(filePath);
        }

        public string FullPath { get; set; }
        public string FileName { get; set; }
    }
}
