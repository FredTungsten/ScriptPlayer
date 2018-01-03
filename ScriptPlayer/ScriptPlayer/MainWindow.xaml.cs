using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;
using Microsoft.Win32;

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
            ViewModel.RequestShowSkipButton += ViewModelOnRequestShowSkipButton;
            ViewModel.RequestShowSkipNextButton += ViewModelOnRequestShowSkipNextButton;
            ViewModel.RequestHideSkipButton += ViewModelOnRequestHideSkipButton;
            ViewModel.RequestHideNotification += ViewModelOnRequestHideNotification;
            ViewModel.Beat += ViewModelOnBeat;
            ViewModel.VideoPlayer = VideoPlayer;
            ViewModel.Load();
        }

        private void ViewModelOnBeat(object sender, EventArgs eventArgs)
        {
            return;

            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => { ViewModelOnBeat(sender, eventArgs); }));
                return;
            }

            FlashOverlay.Flash();
        }

        private void ViewModelOnRequestHideNotification(object sender, string designation)
        {
            Notifications.RemoveNotification(designation);
        }

        private void ViewModelOnRequestHideSkipButton(object sender, EventArgs eventArgs)
        {
            Notifications.RemoveNotification("SkipButton");
        }

        private void ViewModelOnRequestShowSkipNextButton(object sender, EventArgs eventArgs)
        {
            Notifications.AddNotification(((DataTemplate)Resources["SkipNextButton"]).LoadContent(), TimeSpan.MaxValue, "SkipButton", ViewModel.SkipToNextEventCommand);
        }

        private void ViewModelOnRequestShowSkipButton(object sender, EventArgs eventArgs)
        {
            Notifications.AddNotification(((DataTemplate)Resources["SkipButton"]).LoadContent(), TimeSpan.MaxValue, "SkipButton", ViewModel.SkipToNextEventCommand);
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
            FileDialog dialog;

            if (e.SaveFile)
            {
                dialog = new SaveFileDialog
                {
                    Filter = e.Filter,
                    FilterIndex = e.FilterIndex,
                };
            }
            else
            {
                dialog = new OpenFileDialog
                {
                    Filter = e.Filter,
                    FilterIndex = e.FilterIndex,
                    Multiselect = e.MultiSelect,
                };
            }

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
            if (_fullscreen == isFullscreen) return;

            _fullscreen = isFullscreen;

            if (_fullscreen)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;

                SaveCurrentWindowRect();

                Width = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                Height = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
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
            SettingsViewModel s = ViewModel.Settings;
            bool secondClick = e.ClickCount % 2 == 0;

            if (s.DoubleClickToFullscreen && s.ClickToPlayPause)
            {
                if (s.FilterDoubleClicks)
                {
                    if (secondClick)
                        HandleDoubleClick(e);
                    else
                        await HandleSingleClick(e);
                }
                else
                {
                    await HandleSingleClick(e);
                    if(secondClick)
                        HandleDoubleClick(e);
                }
            }
            else if (s.DoubleClickToFullscreen && secondClick)
            {
                HandleDoubleClick(e);
            }
            else if (s.ClickToPlayPause)
            {
                await HandleSingleClick(e);
            }
        }

        private void HandleDoubleClick(MouseButtonEventArgs e)
        {
            e.Handled = true;

            Debug.WriteLine("DoubleClick!");
            _doubleClickTimeStamp = DateTime.Now;

            ToggleFullscreen();
        }

        private async Task HandleSingleClick(MouseButtonEventArgs e)
        {
            if (ViewModel.Settings.FilterDoubleClicks)
            {
                Debug.WriteLine("Click?");
                DateTime click = DateTime.Now;
                await Task.Delay(TimeSpan.FromMilliseconds(350));

                if (_doubleClickTimeStamp >= click)
                    return;
            }

            e.Handled = true;

            Debug.WriteLine("Click!");
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
            DeviceManagerDialog existing = Application.Current.Windows.OfType<DeviceManagerDialog>().FirstOrDefault();

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
            PlaylistWindow existing = Application.Current.Windows.OfType<PlaylistWindow>().FirstOrDefault();

            if (existing == null || !existing.IsLoaded)
            {
                PlaylistWindow playlist = new PlaylistWindow(ViewModel);
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


        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsDialog settings = new SettingsDialog(ViewModel.Settings) {Owner = this};
            if (settings.ShowDialog() != true) return;

            ViewModel.ApplySettings(settings.Settings);
        }

        private void mnuVersion_Click(object sender, RoutedEventArgs e)
        {
            VersionDialog dialog = new VersionDialog(ViewModel.Version){Owner = this};
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
            ViewModel.Settings.ShowTimeLeft ^= true;
        }

        private void GridVideo_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ViewModel.FilesDropped(files);
        }
    }
}
