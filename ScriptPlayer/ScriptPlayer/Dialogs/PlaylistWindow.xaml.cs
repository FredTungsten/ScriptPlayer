using System;
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
            InitializeComponent();
        }

        private void PlaylistEntry_DoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = sender as ListBoxItem;
            PlaylistEntry entry = item?.DataContext as PlaylistEntry;
            if (entry == null)
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
    }
}
