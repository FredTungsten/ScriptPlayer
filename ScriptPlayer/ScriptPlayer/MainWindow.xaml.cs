using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
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
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private bool _fullscreen;
        private Rect _windowPosition;
        private WindowState _windowState;
        private DateTime _doubleClickTimeStamp = DateTime.MinValue;

        public MainWindow()
        {
            Closed += OnClosed;
            ViewModel = new MainViewModel();
        }
        

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            ViewModel.Unload();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RequestToggleFullscreen += ViewModelOnRequestToggleFullscreen;
            ViewModel.RequestOverlay += ViewModelOnRequestOverlay;
            ViewModel.RequestButtplugUrl += ViewModelOnRequestButtplugUrl;
            ViewModel.RequestVlcConnectionSettings += ViewModelOnRequestVlcConnectionSettings;
            ViewModel.RequestWhirligigConnectionSettings += ViewModelOnRequestWhirligigConnectionSettings;
            ViewModel.RequestMessageBox += ViewModelOnRequestMessageBox;
            ViewModel.RequestFile += ViewModelOnRequestFile;
            ViewModel.VideoPlayer = VideoPlayer;
            ViewModel.Load();
            LaunchDirectConnectItem.IsEnabled = ViewModel.CanDirectConnectLaunch;
        }

        private void ViewModelOnRequestWhirligigConnectionSettings(object sender, RequestEventArgs<WhirligigConnectionSettings> args)
        {
            WhirligigConnectionSettingsDialog dialog = new WhirligigConnectionSettingsDialog(args.Value.IpAndPort) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new WhirligigConnectionSettings
            {
                IpAndPort = dialog.IpAndPort
            };
        }

        private void ViewModelOnRequestVlcConnectionSettings(object sender, RequestEventArgs<VlcConnectionSettings> args)
        {
            VlcConnectionSettingsDialog dialog = new VlcConnectionSettingsDialog(args.Value.IpAndPort, args.Value.Password){Owner = this};
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new VlcConnectionSettings
            {
                IpAndPort = dialog.IpAndPort,
                Password = dialog.Password
            };
        }

        private void ViewModelOnRequestToggleFullscreen(object sender, EventArgs eventArgs)
        {
            ToggleFullscreen();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            ViewModel.Dispose();
        }

        private void ViewModelOnRequestOverlay(object sender, string text, TimeSpan timeSpan, string designation)
        {
            Notifications.AddNotification(text, timeSpan, designation);
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
            e.Result = MessageBox.Show(this, e.Text, e.Title, e.Buttons, e.Icon);
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

                Grid.SetRow(GridVideo, 0);
                Grid.SetRowSpan(GridVideo, 3);
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;

                RestoreWindowRect();

                HideOnHover.SetIsActive(MnuMain, false);
                HideOnHover.SetIsActive(PlayerControls, false);

                Grid.SetRow(GridVideo, 1);
                Grid.SetRowSpan(GridVideo, 1);
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

        private async void VideoPlayer_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Debug.WriteLine("Click?");
            DateTime click = DateTime.Now;
            await Task.Delay(TimeSpan.FromMilliseconds(350));

            if (_doubleClickTimeStamp >= click)
                return;

            e.Handled = true;

            //Debug.WriteLine("Click!");
            ViewModel.TogglePlayback();
        }


        private void VideoPlayer_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;

            //Debug.WriteLine("DoubleClick!");
            _doubleClickTimeStamp = DateTime.Now;

            ToggleFullscreen();
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
                    {
                        VideoPlayer.ChangeZoom(0.02);
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
            ShowPlaylist();
        }

        private void mnuShowDevices_Click(object sender, RoutedEventArgs e)
        {
            ShowDevices();
        }

        private void ShowDevices()
        {
            var existing = Application.Current.Windows.OfType<DeviceManagerDialog>().FirstOrDefault();

            if (existing == null || !existing.IsLoaded)
            {
                DeviceManagerDialog deviceManager = new DeviceManagerDialog(ViewModel.Devices);
                deviceManager.Show();
            }
            else
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                existing.Focus();
            }
        }

        private void ShowPlaylist()
        {
            var existing = Application.Current.Windows.OfType<PlaylistWindow>().FirstOrDefault();

            if (existing == null || !existing.IsLoaded)
            {
                PlaylistWindow playlist = new PlaylistWindow(ViewModel.Playlist);
                playlist.Show();
            }
            else
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                existing.Focus();
            }
        }

        private void mnuVersion_Click(object sender, RoutedEventArgs e)
        {
            VersionDialog dialog = new VersionDialog(){Owner = this};
            dialog.ShowDialog();
        }

        private void mnuDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/FredTungsten/ScriptPlayer/wiki");
        }

        /*
        private void mnuDownloadScript_Click(object sender, RoutedEventArgs e)
        {
            //Process.Start("https://github.com/FredTungsten/Scripts");
            ScriptDownloadDialog dialog = new ScriptDownloadDialog(){Owner = this};
            dialog.ShowDialog();
        }
        */

        private void TimeDisplay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ViewModel.ShowTimeLeft ^= true;
        }

        private void GridVideo_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.FilesDropped(files);
        }
    }
}
