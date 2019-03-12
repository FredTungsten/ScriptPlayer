using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Shared;
using ScriptPlayer.ViewModels;
using Microsoft.Win32;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Pipes;

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

        private static Mutex SingleInstanceMutex = null;
        private static NamedPipeServerStream PipeServer = null;
        // https://stackoverflow.com/questions/184084/how-to-force-c-sharp-net-app-to-run-only-one-instance-in-windows
        //[DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool SetForegroundWindow(IntPtr hWnd);

        public MainWindow()
        {
            bool createdNew = true;
            SingleInstanceMutex = new Mutex(true, "ScriptPlayer", out createdNew);
            if(!createdNew)
            {
                // another instance is already running
                // find process
                Process current = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                {
                    if (process.Id != current.Id)
                    {
                        // bring other window into foreground and exit
                        // SetForegroundWindow(process.MainWindowHandle); // we can just let the other instance bring itself into the foreground
                        // get current start parameters
                        string[] args = Environment.GetCommandLineArgs();
                        string fileToLoad = "?"; // ? because it's not an allowed character by NTFS
                        if (args.Length > 1)
                        {
                            fileToLoad = args[1];
                        }
                        // somehow tell the other process to open the file
                        using (NamedPipeClientStream client = new NamedPipeClientStream(".", "ScriptPlayer-pipe", PipeDirection.Out, PipeOptions.Asynchronous))
                        using (StreamWriter writer = new StreamWriter(client))
                        {
                            client.Connect(3000); // 3000ms timeout
                            writer.Write(fileToLoad);
                        }

                        Environment.Exit(0);
                    }
                }
            }
            ViewModel = new MainViewModel();
            ViewModel.LoadPlayerState();
            RestoreWindowState(ViewModel.InitialPlayerState);
            
            // pipeserver listens to messages from other ScriptPlayer processes
            PipeServer = new NamedPipeServerStream("ScriptPlayer-pipe", PipeDirection.In, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            PipeServer.BeginWaitForConnection(new AsyncCallback(PipeConnection), PipeServer);
        }

        private void PipeConnection(IAsyncResult result)
        {
            PipeServer.EndWaitForConnection(result);
            using(StreamReader reader = new StreamReader(PipeServer))
            {
                string file = reader.ReadLine();
                if(File.Exists(file))
                    PipeLoadFile(file);
                PipeBringIntoForeground();
            }           
        }

        private void PipeBringIntoForeground()
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => { PipeBringIntoForeground(); }));
                return;
            }
            // bring the window in the foreground
            Activate();
            // wait for next connection not sure how to do this better
            PipeServer = new NamedPipeServerStream("ScriptPlayer-pipe", PipeDirection.In, 10, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            PipeServer.BeginWaitForConnection(new AsyncCallback(PipeConnection), PipeServer);
        }

        private void PipeLoadFile(string file)
        {
            if (!CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => { PipeLoadFile(file); }));
                return;
            }
            ViewModel.LoadFile(file);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            SaveCurrentWindowRect();
            ViewModel.Dispose();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            ViewModel.Unload();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RequestToggleFullscreen += ViewModelOnRequestToggleFullscreen;
            ViewModel.RequestOverlay += ViewModelOnRequestOverlay;
            ViewModel.RequestButtplugUrl += ViewModelOnRequestButtplugUrl;
            ViewModel.RequestVlcConnectionSettings += ViewModelOnRequestVlcConnectionSettings;
            ViewModel.RequestZoomPlayerConnectionSettings += ViewModelOnRequestZoomPlayerConnectionSettings;
            ViewModel.RequestWhirligigConnectionSettings += ViewModelOnRequestWhirligigConnectionSettings;
            ViewModel.RequestMpcConnectionSettings += ViewModelOnRequestMpcConnectionSettings;
            ViewModel.RequestSamsungVrConnectionSettings += ViewModelOnRequestSamsungVrConnectionSettings;
            ViewModel.RequestKodiConnectionSettings += ViewModelOnRequestKodiConnectionSettings;
            ViewModel.RequestGetWindowState += ViewModelOnRequestGetWindowState;
            ViewModel.RequestThumbnailGeneratorSettings += ViewModelOnRequestThumbnailGeneratorSettings;
            ViewModel.RequestGenerateThumbnails += ViewModelOnRequestGenerateThumbnails;

            ViewModel.RequestShowSettings += ViewModelOnRequestShowSettings;
            ViewModel.RequestSetWindowState += ViewModelOnRequestSetWindowState;
            ViewModel.RequestMessageBox += ViewModelOnRequestMessageBox;
            ViewModel.RequestFile += ViewModelOnRequestFile;
            ViewModel.RequestFolder += ViewModelOnRequestFolder;
            ViewModel.RequestShowSkipButton += ViewModelOnRequestShowSkipButton;
            ViewModel.RequestShowSkipNextButton += ViewModelOnRequestShowSkipNextButton;
            ViewModel.RequestHideSkipButton += ViewModelOnRequestHideSkipButton;
            ViewModel.RequestHideNotification += ViewModelOnRequestHideNotification;
            ViewModel.Beat += ViewModelOnBeat;
            ViewModel.IntermediateBeat += ViewModelOnIntermediateBeat;
            ViewModel.VideoPlayer = VideoPlayer;
            ViewModel.Load();

            if(ViewModel.InitialPlayerState != null)
                SetFullscreen(ViewModel.InitialPlayerState.IsFullscreen, false);
        }

        private void ViewModelOnRequestShowSettings(object sender, string settingsId)
        {
            SettingsDialog settings = new SettingsDialog(ViewModel.Settings, settingsId) { Owner = this };
            if (settings.ShowDialog() != true) return;

            ViewModel.ApplySettings(settings.Settings);
        }

        private void ViewModelOnRequestGenerateThumbnails(object sender, ThumbnailGeneratorSettings settings)
        {
            var createDialog = new CreateThumbnailsDialog(ViewModel, settings) {Owner = this};
            if (createDialog.ShowDialog() != true)
                return;

            ViewModel.RecheckForAdditionalFiles();
        }

        private void ViewModelOnRequestThumbnailGeneratorSettings(object sender, RequestEventArgs<ThumbnailGeneratorSettings> eventArgs)
        {
            var settingsDialog = new ThumbnailGeneratorSettingsDialog(ViewModel, eventArgs.Value) {Owner = this};
            if (settingsDialog.ShowDialog() != true)
                return;

            eventArgs.Value = settingsDialog.Result;
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestSetWindowState(object sender, WindowStateModel windowStateModel)
        {
            RestoreWindowState(windowStateModel);
        }

        private void RestoreWindowState(WindowStateModel windowStateModel)
        {
            if (windowStateModel == null)
                return;

            _windowState = windowStateModel.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            _windowPosition = windowStateModel.GetPosition();
            
            RestoreWindowRect();

            if (IsInitialized)
                SetFullscreen(windowStateModel.IsFullscreen, false);
        }

        private void ViewModelOnRequestGetWindowState(object sender, RequestEventArgs<WindowStateModel> e)
        {
            e.Value = new WindowStateModel
            {
                IsMaximized = _windowState == WindowState.Maximized,
                IsFullscreen = _fullscreen,
                WindowPosition = _windowPosition
            };
            e.Handled = true;
        }

        private void ViewModelOnRequestFolder(object sender, RequestEventArgs<string> e)
        {
            FolderBrowserDialogEx x = new FolderBrowserDialogEx();

            if (!string.IsNullOrWhiteSpace(e.Value))
                x.SelectedPath = e.Value;

            if (x.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            e.Handled = true;
            e.Value = x.SelectedPath;
        }

        private void ViewModelOnIntermediateBeat(object sender, double d)
        {

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


        private void ViewModelOnRequestZoomPlayerConnectionSettings(object sender, RequestEventArgs<ZoomPlayerConnectionSettings> args)
        {
            ZoomPlayerConnectionSettingsDialog dialog = new ZoomPlayerConnectionSettingsDialog(args.Value.IpAndPort) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new ZoomPlayerConnectionSettings
            {
                IpAndPort = dialog.IpAndPort
            };
        }

        private void ViewModelOnRequestSamsungVrConnectionSettings(object sender, RequestEventArgs<SamsungVrConnectionSettings> args)
        {
            SamsungVrConnectionSettingsDialog dialog = new SamsungVrConnectionSettingsDialog(args.Value.UdpPort) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new SamsungVrConnectionSettings
            {
                UdpPort = dialog.UdpPort
            };
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

        private void ViewModelOnRequestMpcConnectionSettings(object sender, RequestEventArgs<MpcConnectionSettings> args)
        {
            MpcConnectionSettingsDialog dialog = new MpcConnectionSettingsDialog(args.Value.IpAndPort) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new MpcConnectionSettings
            {
                IpAndPort = dialog.IpAndPort
            };
        }

        private void ViewModelOnRequestVlcConnectionSettings(object sender, RequestEventArgs<VlcConnectionSettings> args)
        {
            VlcConnectionSettingsDialog dialog = new VlcConnectionSettingsDialog(args.Value.IpAndPort, args.Value.Password) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new VlcConnectionSettings
            {
                IpAndPort = dialog.IpAndPort,
                Password = dialog.Password
            };
        }

        private void ViewModelOnRequestKodiConnectionSettings(object sender, RequestEventArgs<KodiConnectionSettings> args)
        {
            var settings = args.Value;
            KodiConnectionSettingsDialog dialog = new KodiConnectionSettingsDialog(settings.Ip, settings.HttpPort, settings.TcpPort, settings.User, settings.Password) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new KodiConnectionSettings
            {
                Ip = dialog.Ip,
                HttpPort = dialog.HttpPort,
                TcpPort = dialog.TcpPort,
                User = dialog.User,
                Password = dialog.Password
            };
        }

        private void ViewModelOnRequestToggleFullscreen(object sender, EventArgs eventArgs)
        {
            ToggleFullscreen();
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

        private void SetFullscreen(bool isFullscreen, bool updateRestorePosition = true)
        {
            if (_fullscreen == isFullscreen)
                return;

            if (!_fullscreen)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;

                if(updateRestorePosition)
                    SaveCurrentWindowRect();

                var screenBounds = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;

                Width = screenBounds.Width;
                Height = screenBounds.Height;
                Left = screenBounds.Left;
                Top = screenBounds.Top;
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

            _fullscreen = isFullscreen;
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
            if (_fullscreen)
                return;

            _windowState = WindowState;

            if (_windowState != WindowState.Normal)
                _windowPosition = new Rect(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height);
            else
                _windowPosition = new Rect(Left, Top, Width, Height);
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
                    if (secondClick)
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

            Key[] modifiers = new[] {Key.LeftAlt, Key.LeftCtrl, Key.LeftShift, Key.RightAlt, Key.RightCtrl, Key.RightShift};

            if (modifiers.Contains(e.Key))
                return;

            ModifierKeys activeMods = GlobalCommandManager.GetActiveModifierKeys();

            handled = GlobalCommandManager.ProcessInput(e.Key, activeMods);
            if (handled)
            {
                e.Handled = true;
                return;
            }

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
                case Key.PageUp:
                    {
                        ViewModel.Playlist.PlayPreviousEntry(ViewModel.LoadedFiles);
                        break;
                    }
                case Key.PageDown:
                    {
                        ViewModel.Playlist.PlayNextEntry(ViewModel.LoadedFiles);
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
                case Key.S:
                    {
                        ViewModel.ToggleCommandSource();
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
            ViewModel.ShowSettings();
        }

        private void mnuVersion_Click(object sender, RoutedEventArgs e)
        {
            VersionDialog dialog = new VersionDialog(ViewModel.Version) { Owner = this };
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

        private void btnReloadScript_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ReloadScript();
        }

        private void MnuCreatePreview_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.CheckFfmpeg())
                return;

            var settings = new PreviewGeneratorSettings
            {
                Video = ViewModel.LoadedVideo,
            };

            settings.Destination = settings.SuggestDestination();

            if (ViewModel.DisplayedRange != null)
            {
                settings.Start = ViewModel.DisplayedRange.Start;
                settings.Duration = ViewModel.DisplayedRange.Duration;
            }
            else
            {
                settings.Start = ViewModel.TimeSource.Progress;
                settings.Duration = TimeSpan.FromSeconds(5);
            }

            PreviewGeneratorSettingsDialog settingsDialog = new PreviewGeneratorSettingsDialog(settings);
            settingsDialog.Owner = this;
            if (settingsDialog.ShowDialog() != true)
                return;

            settings = settingsDialog.Result;

            var dialog = new CreatePreviewDialog(ViewModel, settings) {Owner = this};
            dialog.ShowDialog();

            ViewModel.RecheckForAdditionalFiles();
        }
    }
}
