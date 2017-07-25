using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;

namespace ScriptPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(MainWindow), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get { return (MainViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        private bool _fullscreen;
        private Rect _windowPosition;
        private WindowState _windowState;

        public MainWindow()
        {
            ViewModel = new MainViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RequestOverlay += ViewModelOnRequestOverlay;
            ViewModel.RequestButtplugUrl += ViewModelOnRequestButtplugUrl;
            ViewModel.RequestMessageBox += ViewModelOnRequestMessageBox;
            ViewModel.RequestFile += ViewModelOnRequestFile;
            ViewModel.VideoPlayer = VideoPlayer;

            ViewModel.Load();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            ViewModel.Dispose();
        }

        private void ViewModelOnRequestOverlay(object sender, string text, TimeSpan timeSpan, string designation)
        {
            OverlayText.SetText(text, timeSpan);
        }

        private void SeekBar_OnSeek(object sender, double relative, TimeSpan absolute, int downmoveup)
        {
            ViewModel.Seek(absolute, downmoveup);
        }

        private void ViewModelOnRequestFile(object sender, RequestFileEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = e.Filter,
                FilterIndex = e.FilterIndex,
                Multiselect = e.MultiSelect,
            };

            if (dialog.ShowDialog(this) != true)
                return;

            e.Handled = true;
            e.SelectedFile = dialog.FileName;
            e.SelectedFiles = dialog.FileNames;
            e.FilterIndex = dialog.FilterIndex;
        }

        private void ViewModelOnRequestMessageBox(object sender, MessageBoxEventArgs e)
        {
            e.Result = MessageBox.Show(this, e.Text, e.Text, e.Buttons, e.Icon);
            e.Handled = true;
        }

        private void ViewModelOnRequestButtplugUrl(object sender, ButtplugUrlRequestEventArgs e)
        {
            ButtplugConnectionSettingsDialog dialog = new ButtplugConnectionSettingsDialog
            {
                Url = e.Url,
                Owner = this
            };

            if (dialog.ShowDialog() != true)
                return;

            e.Handled = true;
            e.Url = dialog.Url;
        }

        private void VideoPlayer_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            SetFullscreen(!_fullscreen);
        }

        private void SetFullscreen(bool isFullscreen)
        {
            _fullscreen = isFullscreen;

            if (_fullscreen)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;

                SaveCurrentWindowRect();

                Width = Screen.PrimaryScreen.Bounds.Width;
                Height = Screen.PrimaryScreen.Bounds.Height;
                Left = 0;
                Top = 0;
                WindowState = WindowState.Normal;

                HideOnHover.SetIsActive(MnuMain, true);
                HideOnHover.SetIsActive(PlayerControls, true);

                Grid.SetRow(VideoPlayer, 0);
                Grid.SetRowSpan(VideoPlayer, 3);

                Grid.SetRow(Shade, 0);
                Grid.SetRowSpan(Shade, 3);
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;

                RestoreWindowRect();

                HideOnHover.SetIsActive(MnuMain, false);
                HideOnHover.SetIsActive(PlayerControls, false);

                Grid.SetRow(VideoPlayer, 1);
                Grid.SetRowSpan(VideoPlayer, 1);

                Grid.SetRow(Shade, 1);
                Grid.SetRowSpan(Shade, 1);
            }
        }

        private void RestoreWindowRect()
        {
            Left = _windowPosition.Left;
            Top = _windowPosition.Top;
            Width = _windowPosition.Width;
            Height = _windowPosition.Height;
            WindowState = _windowState;
        }

        private void SaveCurrentWindowRect()
        {
            _windowPosition = new Rect(Left, Top, Width, Height);
            _windowState = WindowState;
        }

        private void VideoPlayer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.TogglePlayback();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
                ViewModel.VolumeUp();
            else
                ViewModel.VolumeDown();
        }

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = true;
            switch (e.Key)
            {
                case Key.Enter:
                {
                    ToggleFullscreen();
                    break;
                }
                case Key.Escape:
                {
                    SetFullscreen(false);
                    break;
                }
                case Key.Space:
                {
                    ViewModel.TogglePlayback();
                    break;
                }
                case Key.Left:
                {
                    ViewModel.ShiftPosition(TimeSpan.FromSeconds(-5));
                    break;
                }
                case Key.Right:
                {
                    ViewModel.ShiftPosition(TimeSpan.FromSeconds(5));
                        break;
                }
                case Key.Up:
                {
                    ViewModel.VolumeUp();
                    break;
                }
                case Key.Down:
                {
                    ViewModel.VolumeDown();
                    break;
                }
                case Key.NumPad0:
                {

                    break;
                }
                case Key.NumPad1:
                {
                    VideoPlayer.ChangeZoom(-0.02);
                    break;
                }
                case Key.NumPad2:
                {
                    VideoPlayer.Move(new Point(0, 1));
                    break;
                }
                case Key.NumPad3:
                {
                    break;
                }
                case Key.NumPad4:
                {
                    VideoPlayer.Move(new Point(-1, 0));
                    break;
                }
                case Key.NumPad5:
                {
                    VideoPlayer.ResetTransform();
                    break;
                }
                case Key.NumPad6:
                {
                    VideoPlayer.Move(new Point(1, 0));
                    break;
                }
                case Key.NumPad7:
                {
                    VideoPlayer.SideBySide ^= true;
                    break;
                }
                case Key.NumPad8:
                {
                    VideoPlayer.Move(new Point(0, -1));
                    break;
                }
                case Key.NumPad9:
                    VideoPlayer.ChangeZoom(0.02);
                {
                    break;
                }
                default:
                {
                    handled = false;
                    break;
                }
            }

            e.Handled = handled;
        }

        private void mnuShowPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistWindow playlist = new PlaylistWindow(ViewModel.Playlist);
            playlist.Show();
        }
    }
}
