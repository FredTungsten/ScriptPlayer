using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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
using ScriptPlayer.Generators;
using ScriptPlayer.Shared.Classes;
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
            ViewModel = new MainViewModel();
            ViewModel.LoadPlayerState();
            RestoreWindowState(ViewModel.InitialPlayerState);   
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if(!_fullscreen)
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
            ViewModel.RequestGenerateThumbnailBanner += ViewModelOnRequestGenerateThumbnailBanner;
            ViewModel.RequestThumbnailBannerGeneratorSettings += ViewModelOnRequestThumbnailBannerGeneratorSettings;
            ViewModel.RequestPreviewGeneratorSettings += ViewModelOnRequestPreviewGeneratorSettings;
            ViewModel.RequestHeatmapGeneratorSettings += ViewModelOnRequestHeatmapGeneratorSettings;
            ViewModel.RequestScriptShiftTimespan += ViewModelOnRequestScriptShiftTimespan;
            ViewModel.RequestSection += ViewModelOnRequestSection;
            ViewModel.RequestGeneratorSettings += ViewModelOnRequestGeneratorSettings;

            ViewModel.RequestShowGeneratorProgressDialog += ViewModelOnRequestShowGeneratorProgressDialog;
            ViewModel.RequestActivate += ViewModelOnRequestActivate;
            ViewModel.RequestShowSettings += ViewModelOnRequestShowSettings;
            ViewModel.RequestSetWindowState += ViewModelOnRequestSetWindowState;
            ViewModel.RequestMessageBox += ViewModelOnRequestMessageBox;
            ViewModel.RequestFile += ViewModelOnRequestFile;
            ViewModel.RequestFolder += ViewModelOnRequestFolder;
            ViewModel.RequestShowSkipButton += ViewModelOnRequestShowSkipButton;
            ViewModel.RequestShowSkipNextButton += ViewModelOnRequestShowSkipNextButton;
            ViewModel.RequestHideSkipButton += ViewModelOnRequestHideSkipButton;
            ViewModel.RequestHideNotification += ViewModelOnRequestHideNotification;
            ViewModel.RequestShowDeviceManager += ViewModelOnRequestShowDeviceManager;

            ViewModel.Beat += ViewModel_Beat;

            ViewModel.VideoPlayer = VideoPlayer;
            ViewModel.Load();

            if (ViewModel.InitialPlayerState != null)
            {
                WindowState = ViewModel.InitialPlayerState.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                SetFullscreen(ViewModel.InitialPlayerState.IsFullscreen, false);
            }

            ViewModel.SetMainWindow(this);
        }

        private void ViewModelOnRequestGeneratorSettings(object sender, RequestEventArgs<GeneratorSettingsViewModel> eventArgs)
        {
            GeneratorSettingsDialog dialog = new GeneratorSettingsDialog(ViewModel, eventArgs.Value, 
                GeneratedElements.All, GeneratedElements.Thumbnails);
            dialog.Owner = this;

            if (dialog.ShowDialog() != true)
                return;

            eventArgs.Value = dialog.Settings;
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestSection(object sender, RequestEventArgs<Section> eventArgs)
        {
            SectionDialog dialog = new SectionDialog
            {
                Owner = this,
                TimeFrom = eventArgs.Value.Start,
                TimeTo = eventArgs.Value.End
            };

            if (dialog.ShowDialog() != true)
                return;

            eventArgs.Value = new Section(dialog.TimeFrom, dialog.TimeTo);
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestScriptShiftTimespan(object sender, RequestEventArgs<TimeSpan> eventArgs)
        {
            TimeShiftDialog dialog = new TimeShiftDialog(eventArgs.Value){Owner = this};
            if (dialog.ShowDialog() != true)
                return;

            eventArgs.Value = dialog.Result;
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestShowDeviceManager(object sender, EventArgs eventArgs)
        {
            ShowDevices();
        }

        private void ViewModel_Beat(object sender, EventArgs e)
        {
            //VideoPlayer.Zoom = VideoPlayer.Zoom == 1 ? 1.1 : 1;
        }

        private void ViewModelOnRequestHeatmapGeneratorSettings(object sender, RequestEventArgs<HeatmapGeneratorSettings> eventArgs)
        {
            //TODO
            eventArgs.Handled = true;
            eventArgs.Value = new HeatmapGeneratorSettings();
        }

        private void ViewModelOnRequestPreviewGeneratorSettings(object sender, RequestEventArgs<PreviewGeneratorSettings> eventArgs)
        {
            PreviewGeneratorSettingsDialog dialog = new PreviewGeneratorSettingsDialog(eventArgs.Value);
            if (dialog.ShowDialog() != true)
                return;

            eventArgs.Value = dialog.Result;
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestShowGeneratorProgressDialog(object sender, EventArgs eventArgs)
        {
            ShowGeneratorProgress();
        }

        private void ViewModelOnRequestThumbnailBannerGeneratorSettings(object sender, RequestEventArgs<ThumbnailBannerGeneratorSettings> eventArgs)
        {
            ThumbnailBannerGeneratorSettingsDialog dialog = new ThumbnailBannerGeneratorSettingsDialog(eventArgs.Value) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            eventArgs.Value = dialog.Settings;
            eventArgs.Handled = true;
        }

        private void ViewModelOnRequestGenerateThumbnailBanner(object sender, ThumbnailBannerGeneratorSettings settings)
        {
            MessageBox.Show("Not done yet ...");
        }

        private void ViewModelOnRequestActivate(object sender, EventArgs eventArgs)
        {
            Activate();
        }

        private void ViewModelOnRequestShowSettings(object sender, string settingsId)
        {
            SettingsDialog settings = new SettingsDialog(ViewModel.Settings, settingsId) { Owner = this };
            if (settings.ShowDialog() != true) return;

            ViewModel.ApplySettings(settings.Settings);
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

            _windowPosition = windowStateModel.GetPosition();
            _windowState = windowStateModel.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            
            RestoreWindowRect(IsInitialized);

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

            bool currentlyFullScreen = _fullscreen;

            _fullscreen = isFullscreen;

            if (_fullscreen)
            {
                if(updateRestorePosition && !currentlyFullScreen)
                    SaveCurrentWindowRect();

                var screenBounds = FindScreenWithMostOverlap();

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;

                Width = screenBounds.Width;
                Height = screenBounds.Height;
                Left = screenBounds.Left;
                Top = screenBounds.Top;
                
                HideOnHover.SetIsActive(MnuMain, true);
                HideOnHover.SetIsActive(PlayerControls, true);

                Grid.SetRow(GridVideo, 0);
                Grid.SetRowSpan(GridVideo, 3);
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;

                RestoreWindowRect(true);

                HideOnHover.SetIsActive(MnuMain, false);
                HideOnHover.SetIsActive(PlayerControls, false);

                Grid.SetRow(GridVideo, 1);
                Grid.SetRowSpan(GridVideo, 1);
            }

            _fullscreen = isFullscreen;
        }

        public Rectangle GetWindowRectangle()
        {
            if (WindowState == WindowState.Maximized)
            {
                var handle = new WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                return screen.WorkingArea;
            }

            return new Rectangle((int)Left, (int)Top,(int)ActualWidth, (int)ActualHeight);
        }

        private Rect FindScreenWithMostOverlap()
        {
            Rectangle windowBounds = GetWindowRectangle();

            Rectangle screenBounds = System.Windows.Forms.Screen.FromRectangle(windowBounds).Bounds;

            return new Rect(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height);
        }

        private void RestoreWindowRect(bool includeWindowState)
        {
            Left = _windowPosition.Left;
            Top = _windowPosition.Top;
            Width = _windowPosition.Width;
            Height = _windowPosition.Height;

            if(includeWindowState)
                WindowState = _windowState;
        }

        private void SaveCurrentWindowRect()
        {
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
            Key[] modifiers = {Key.LeftAlt, Key.LeftCtrl, Key.LeftShift, Key.RightAlt, Key.RightCtrl, Key.RightShift};

            if (modifiers.Contains(e.Key))
                return;

            ModifierKeys activeMods = GlobalCommandManager.GetActiveModifierKeys();
            
            bool handled = GlobalCommandManager.ProcessInput(e.Key, activeMods, KeySource.DirectInput);

            if (handled)
            {
                e.Handled = true;
                return;
            }

            handled = true;

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
                        ViewModel.Playlist.PlayPreviousEntry();
                        break;
                    }
                case Key.PageDown:
                    {
                        ViewModel.Playlist.PlayNextEntry();
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

        private void ShowGeneratorProgress()
        {
            GeneratorProgressDialog existing = Application.Current.Windows.OfType<GeneratorProgressDialog>().FirstOrDefault();

            if (existing == null || !existing.IsLoaded)
            {
                GeneratorProgressDialog progressDialog = new GeneratorProgressDialog(ViewModel);
                progressDialog.Show();
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

        private void mnuVersion_Click(object sender, RoutedEventArgs e)
        {
            VersionDialog dialog = new VersionDialog(ViewModel.Version) { Owner = this };
            dialog.ShowDialog();
        }

        private void mnuDocs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/FredTungsten/ScriptPlayer/wiki");
        }

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

        //private void MnuCreateScenes_OnClick(object sender, RoutedEventArgs e)
        //{
        //    if (!ViewModel.CheckFfmpeg())
        //        return;

        //    new SceneSelectorDialog(ViewModel, ViewModel.LoadedVideo).ShowDialog();
        //}

        //private void ToolTipNext_OnOpened(object sender, RoutedEventArgs e)
        //{
        //    //var entry = ViewModel.Playlist.NextEntry;
        //    //if (entry == null)
        //    //{
        //    //    playerNext.Close();
        //    //    titleNext.Text = "Unknown";
        //    //    return;
        //    //}

        //    //string heatmap = ViewModel.GetRelatedFile(entry.Fullname, new[] { "png" });
        //    //if (!string.IsNullOrEmpty(heatmap))
        //    //{
        //    //    var image = new BitmapImage();
        //    //    image.BeginInit();
        //    //    image.CacheOption = BitmapCacheOption.OnLoad;
        //    //    image.UriSource = new Uri(heatmap, UriKind.Absolute);
        //    //    image.EndInit();

        //    //    heatMapNext.Source = image;
        //    //}

        //    //string gifFile = ViewModel.GetRelatedFile(entry.Fullname, new[] { "gif" });
        //    //if (!string.IsNullOrEmpty(gifFile))
        //    //{
        //    //    playerNext.Load(gifFile);
        //    //}

        //    //titleNext.Text = entry.Shortname + " [" + (entry.Duration?.ToString("hh\\:mm\\:ss") ?? "?") + "]";
        //}

        //private void ToolTipPrevious_OnOpened(object sender, RoutedEventArgs e)
        //{
        //    var entry = ViewModel.Playlist.PreviousEntry;
        //    if (entry == null)
        //    {
        //        playerPrevious.Close();
        //        titlePrevious.Text = "Unknown";
        //        return;
        //    }

        //    string heatmap = ViewModel.GetRelatedFile(entry.Fullname, new[]{"png"});
        //    if (!string.IsNullOrEmpty(heatmap))
        //    {
        //        var image = new BitmapImage();
        //        image.BeginInit();
        //        image.CacheOption = BitmapCacheOption.OnLoad;
        //        image.UriSource = new Uri(heatmap, UriKind.Absolute);
        //        image.EndInit();

        //        heatMapPrevious.Source = image;
        //    }

        //    string gifFile = ViewModel.GetRelatedFile(entry.Fullname, new[] { "gif" });
        //    if (!string.IsNullOrEmpty(gifFile))
        //    {
        //        playerPrevious.Load(gifFile);
        //    }

        //    titlePrevious.Text = entry.Shortname + " [" + (entry.Duration?.ToString("hh\\:mm\\:ss") ?? "?") + "]";
        //}

        //private void ToolTip_OnClosed(object sender, RoutedEventArgs e)
        //{
        //    //playerNext.Close();
        //    playerPrevious.Close();

        //    //heatMapNext.Source = null;
        //    heatMapPrevious.Source = null;
        //}

        private void MnuDownloadButtplug_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ButtplugAdapter.GetDownloadUrl());
        }

        private void mnuAttributions_Click(object sender, RoutedEventArgs e)
        {
            new AttributionDialog(){Owner = this}.Show();
        }

        private void mnuShowGeneratorProgress_OnClick(object sender, RoutedEventArgs e)
        {
            ShowGeneratorProgress();
        }
    }
}
