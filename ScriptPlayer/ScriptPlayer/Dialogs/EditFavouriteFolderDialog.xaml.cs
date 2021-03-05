using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for EditFavouriteFolderDialog.xaml
    /// </summary>
    public partial class EditFavouriteFolderDialog : Window
    {
        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
            "Path", typeof(string), typeof(EditFavouriteFolderDialog), new PropertyMetadata(default(string)));

        public string Path
        {
            get => (string) GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public static readonly DependencyProperty FolderNameProperty = DependencyProperty.Register(
            "FolderName", typeof(string), typeof(EditFavouriteFolderDialog), new PropertyMetadata(default(string)));

        public string FolderName
        {
            get => (string) GetValue(FolderNameProperty);
            set => SetValue(FolderNameProperty, value);
        }

        public static readonly DependencyProperty IsDefaultProperty = DependencyProperty.Register(
            "IsDefault", typeof(bool), typeof(EditFavouriteFolderDialog), new PropertyMetadata(default(bool)));

        public bool IsDefault
        {
            get => (bool) GetValue(IsDefaultProperty);
            set => SetValue(IsDefaultProperty, value);
        }

        public EditFavouriteFolderDialog(FavouriteFolder initialValues = null)
        {
            InitializeComponent();

            if (initialValues != null)
            {
                Path = initialValues.Path;
                FolderName = initialValues.Name;
                IsDefault = initialValues.IsDefault;

                if (IsDefault)
                    cckDefault.IsEnabled = false;
            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ((Button) sender).Focus();

            if (string.IsNullOrWhiteSpace(Path) || string.IsNullOrWhiteSpace(FolderName))
            {
                MessageBox.Show(this, "Enter or select a path and choose a name for the favourite folder", "Input missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(Path))
            {
                MessageBox.Show(this, "This path doesn't exist or is not accessible.", "Input error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSelectPath_Click(object sender, RoutedEventArgs e)
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

            Path = dlg.FileName;
            FolderName = System.IO.Path.GetFileName(dlg.FileName);
        }
    }
}
