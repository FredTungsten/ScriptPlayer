using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for DirectorySelectorDialog.xaml
    /// </summary>
    public partial class DirectorySelectorDialog : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(DirectorySelectorDialog), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty SelectedDirectoryProperty = DependencyProperty.Register(
            "SelectedDirectory", typeof(string), typeof(DirectorySelectorDialog), new PropertyMetadata(default(string)));

        public string SelectedDirectory
        {
            get { return (string) GetValue(SelectedDirectoryProperty); }
            set { SetValue(SelectedDirectoryProperty, value); }
        }

        public DirectorySelectorDialog(MainViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }

        private void btnSelectFavourite_Click(object sender, RoutedEventArgs e)
        {
            if (!(((Button) sender).DataContext is FavouriteFolder folder))
                return;

            SelectedDirectory = folder.Path;
            DialogResult = true;
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                AddToMostRecentlyUsedList = true,
                AllowNonFileSystemItems = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog() != CommonFileDialogResult.Ok)
                return;

            SelectedDirectory = dlg.FileName;
            DialogResult = true;
        }

        private void btnEditFavourites_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShowSettings("Favourite Folders");
        }
    }
}
