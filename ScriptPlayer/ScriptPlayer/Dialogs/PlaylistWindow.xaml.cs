using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for PlaylistWindow.xaml
    /// </summary>
    public partial class PlaylistWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(PlaylistWindow), new PropertyMetadata(default(PlaylistViewModel)));

        public MainViewModel ViewModel
        {
            get => (MainViewModel) GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        public PlaylistWindow(MainViewModel viewmodel)
        {
            ViewModel = viewmodel;
            ViewModel.Playlist.SelectedEntryMoved += PlaylistOnSelectedEntryMoved;
            InitializeComponent();
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
            ViewModel.Playlist.SetSelectedItems(((ListBox) sender).SelectedItems.Cast<PlaylistEntry>());
        }
    }
}
