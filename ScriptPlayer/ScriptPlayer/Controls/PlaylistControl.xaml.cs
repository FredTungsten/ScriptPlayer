using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScriptPlayer.Dialogs;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Controls
{
    /// <summary>
    /// Interaction logic for PlaylistControl.xaml
    /// </summary>
    public partial class PlaylistControl : UserControl
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(PlaylistControl), new PropertyMetadata(default(MainViewModel), OnViewModelPropertyChanged));

        private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PlaylistControl) d).ViewModelChanged((MainViewModel) e.OldValue, (MainViewModel) e.NewValue);
        }

        private void ViewModelChanged(MainViewModel oldValue, MainViewModel newValue)
        {
            if (oldValue != null)
            {
                oldValue.Playlist.SelectedEntryMoved -= PlaylistOnSelectedEntryMoved;
            }

            if (newValue != null)
            {
                newValue.Playlist.SelectedEntryMoved += PlaylistOnSelectedEntryMoved;
            }
        }

        public PlaylistControl()
        {
            InitializeComponent();
        }

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private void PlaylistOnSelectedEntryMoved(object sender, EventArgs eventArgs)
        {
            if (ViewModel.Playlist.SelectedEntry == null)
                return;

            lstEntries.ScrollIntoView(ViewModel.Playlist.SelectedEntry);
        }

        private void PlaylistEntry_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            if (!(item?.DataContext is PlaylistEntry entry))
                return;

            ViewModel.Playlist.RequestPlayEntry(entry);
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.Playlist.AddEntries(files);
        }

        private void LstEntries_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.Playlist.SetSelectedItems(((ListBox)sender).SelectedItems.Cast<PlaylistEntry>());
        }

        private void ToolTip_OnOpened(object sender, RoutedEventArgs e)
        {
            var tooltip = (ToolTip)sender;
            PlaylistEntry entry = ((FrameworkElement)tooltip.PlacementTarget).DataContext as PlaylistEntry;

            VideoDetailsPreview preview = tooltip.GetChildOfType<VideoDetailsPreview>();
            preview.ViewModel = ViewModel;
            preview.Entry = entry;
        }

        private void MnuScrollToCurrent(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Playlist?.FilteredEntries == null || ViewModel.LoadedFiles == null)
                return;

            var entry = ViewModel.Playlist.FilteredEntries.FirstOrDefault(en =>
                ViewModel.LoadedFiles.Contains(en.Fullname));

            if (entry == null)
                return;

            ViewModel.Playlist.SelectedEntry = entry;
            ViewModel.Playlist.SetSelectedItems(new[] { entry });
            lstEntries.ScrollIntoView(entry);
        }

        private void UserControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ViewModel.Playlist.RemoveSelectedEntryCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
