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
using ScriptPlayer.Generators;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.Shared.Helpers;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using FileDialog = Microsoft.Win32.FileDialog;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace ScriptPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IOnScreenDisplay
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel", typeof(MainViewModel), typeof(MainWindow), new PropertyMetadata(default(MainViewModel)));

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private WindowStateModel _windowStateModel;

        private DateTime _doubleClickTimeStamp = DateTime.MinValue;
        

        public MainWindow()
        {
            ViewModel = new MainViewModel();
            ViewModel.LoadPlayerState();
            RestoreWindowState(ViewModel.InitialPlayerState);   
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if(!ViewModel.IsFullscreen)
                SaveCurrentWindowRect();

            SaveSidePanels();

            ViewModel.Dispose();
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            ViewModel.Unload();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RequestToggleFullscreen += ViewModelOnRequestToggleFullscreen;
            ViewModel.RequestSetFullscreen += ViewModelOnRequestSetFullscreen;
            ViewModel.RequestButtplugUrl += ViewModelOnRequestButtplugUrl;
            ViewModel.RequestVlcConnectionSettings += ViewModelOnRequestVlcConnectionSettings;
            ViewModel.RequestZoomPlayerConnectionSettings += ViewModelOnRequestZoomPlayerConnectionSettings;
            ViewModel.RequestWhirligigConnectionSettings += ViewModelOnRequestWhirligigConnectionSettings;
            ViewModel.RequestSimpleTcpConnectionSettings += ViewModelOnRequestSimpleTcpConnectionSettings;
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
            ViewModel.RequestShowDeviceManager += ViewModelOnRequestShowDeviceManager;

            ViewModel.Beat += ViewModel_Beat;

            ViewModel.VideoPlayer = VideoPlayer;
            ViewModel.Load();

            if (ViewModel.InitialPlayerState != null)
            {
                RestoreSidePanels(true);
                WindowState = ViewModel.InitialPlayerState.IsMaximized ? WindowState.Maximized : WindowState.Normal;
                SetFullscreen(ViewModel.InitialPlayerState.IsFullscreen, false);
            }

            ViewModel.SetMainWindow(this);
            
            var hideonhoverDescriptor = DependencyPropertyDescriptor.FromProperty(HideOnHover.IsActiveProperty, typeof(Grid));
            hideonhoverDescriptor.AddValueChanged(GridPlaylist, HideOnHoverChanged);
            hideonhoverDescriptor.AddValueChanged(GridSettings, HideOnHoverChanged);

            UpdateVideoColumns();
        }

        public void SettingsPage_Handler(string pagename)
        {
            ShowSettings(pagename);
        }

        private void HideOnHoverChanged(object sender, EventArgs eventArgs)
        {
            UpdateVideoColumns();
        }

        private void UpdateVideoColumns()
        {
            bool hidePlaylist = HideOnHover.GetIsActive(GridPlaylist);
            bool hideSettings = HideOnHover.GetIsActive(GridSettings);

            int gridColumn = hidePlaylist ? 0 : 1;
            int gridColumnSpan = 3 - (hidePlaylist ? 0 : 1) - (hideSettings ? 0 : 1);

            Grid.SetColumn(GridVideo, gridColumn);
            Grid.SetColumnSpan(GridVideo, gridColumnSpan);
        }

        private void ViewModelOnRequestGeneratorSettings(object sender, RequestEventArgs<GeneratorSettingsViewModel> eventArgs)
        {
            GeneratorSettingsDialog dialog = new GeneratorSettingsDialog(ViewModel, eventArgs.Value,
                GeneratedElements.All, GeneratedElements.Thumbnails) {Owner = this};

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
            ShowSettings(settingsId);
        }

        private void ShowSettings(string settingsId)
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

        private void SidePanel_FinishedDragging(object sender, EventArgs e)
        {
            SaveSidePanels();
        }

        private void SaveSidePanels()
        {
            if(_windowStateModel == null)
                _windowStateModel = new WindowStateModel();
            
            _windowStateModel.PlaylistWidth = GirdPlaylistInner.Width;
            _windowStateModel.SettingsWidth = GridSettingsInner.Width;

            _windowStateModel.HidePlaylist = HideOnHover.GetIsActive(GridPlaylist);
            _windowStateModel.HideSettings = HideOnHover.GetIsActive(GridSettings);

            _windowStateModel.ExpandPlaylist = ExpanderPlaylist.IsExpanded;
            _windowStateModel.ExpandSettings = ExpanderSettings.IsExpanded;
        }

        private void RestoreWindowState(WindowStateModel windowStateModel)
        {
            if (windowStateModel == null)
                return;

            _windowStateModel = windowStateModel;

            RestoreWindowRect(IsInitialized);
            RestoreSidePanels(IsInitialized);

            if (IsInitialized)
                SetFullscreen(windowStateModel.IsFullscreen, false);
        }

        private void RestoreSidePanels(bool isInitialized)
        {
            if (!isInitialized)
                return;

            if (_windowStateModel == null)
                return;

            GirdPlaylistInner.Width = _windowStateModel.PlaylistWidth;
            GridSettingsInner.Width = _windowStateModel.SettingsWidth;

            HideOnHover.SetIsActive(GridPlaylist, _windowStateModel.HidePlaylist);
            HideOnHover.SetIsActive(GridSettings, _windowStateModel.HideSettings);

            ExpanderPlaylist.IsExpanded = _windowStateModel.ExpandPlaylist;
            ExpanderSettings.IsExpanded = _windowStateModel.ExpandSettings;
        }

        private void ViewModelOnRequestGetWindowState(object sender, RequestEventArgs<WindowStateModel> e)
        {
            WindowStateModel stateModel = _windowStateModel.Duplicate();
            stateModel.IsFullscreen = ViewModel.IsFullscreen;

            e.Value = stateModel;
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

        private void ViewModelOnRequestSimpleTcpConnectionSettings(object sender, RequestEventArgs<SimpleTcpConnectionSettings> args)
        {
            SimpleTcpConnectionSettingsDialog dialog = new SimpleTcpConnectionSettingsDialog(args.Value.IpAndPort) { Owner = this };
            if (dialog.ShowDialog() != true) return;

            args.Handled = true;
            args.Value = new SimpleTcpConnectionSettings
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

        private void ViewModelOnRequestSetFullscreen(object sender, bool fullscreen)
        {
            SetFullscreen(fullscreen);
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
            SetFullscreen(!ViewModel.IsFullscreen);
        }

        private void SetFullscreen(bool isFullscreen, bool updateRestorePosition = true)
        {
            if (ViewModel.IsFullscreen == isFullscreen)
                return;

            bool currentlyFullScreen = ViewModel.IsFullscreen;

            ViewModel.IsFullscreen = isFullscreen;

            if (ViewModel.IsFullscreen)
            {
                if(updateRestorePosition && !currentlyFullScreen)
                    SaveCurrentWindowRect();

                var screenBounds = FindScreenWithMostOverlap();

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;

                //GridVideo.Visibility = Visibility.Hidden;

                Width = screenBounds.Width;
                Height = screenBounds.Height;
                Left = screenBounds.Left;
                Top = screenBounds.Top;

                var offset = this.PointFromScreen(new System.Windows.Point(0, 0));
                
                HideOnHover.SetIsActive(MnuMain, true);
                HideOnHover.SetIsActive(PlayerControls, true);

                Grid.SetRow(GridVideo, 0);
                Grid.SetRowSpan(GridVideo, 3);
            }
            else
            {
                //GridVideo.Visibility = Visibility.Visible;

                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;

                RestoreWindowRect(true);

                HideOnHover.SetIsActive(MnuMain, false);
                HideOnHover.SetIsActive(PlayerControls, false);

                Grid.SetRow(GridVideo, 1);
                Grid.SetRowSpan(GridVideo, 1);
            }

            ViewModel.IsFullscreen = isFullscreen;
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
            if (_windowStateModel == null)
                return;

            Rect windowPosition = _windowStateModel.GetPosition();
            WindowState windowState = _windowStateModel.IsMaximized ? WindowState.Maximized : WindowState.Normal;

            Left = windowPosition.Left;
            Top = windowPosition.Top;
            Width = windowPosition.Width;
            Height = windowPosition.Height;

            if(includeWindowState)
                WindowState = windowState;
        }

        private void SaveCurrentWindowRect()
        {
            if(_windowStateModel == null)
                _windowStateModel = new WindowStateModel();

            _windowStateModel.IsMaximized = WindowState == WindowState.Maximized;

            Rect windowRect = WindowState != WindowState.Normal 
                ? new Rect(RestoreBounds.Left, RestoreBounds.Top, RestoreBounds.Width, RestoreBounds.Height) 
                : new Rect(Left, Top, Width, Height);

            _windowStateModel.SetPosition(windowRect);
        }


        private void VideoPlayer_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            bool handled = GlobalCommandManager.ProcessInput(e);
            if (handled)
            {
                e.Handled = true;
            }
            else 
            {
                switch (e.ChangedButton)
                {
                    case MouseButton.Left:
                    {
                        VideoPlayer_OnMouseLeftButtonDown(sender, e);
                        break;
                    }
                    case MouseButton.Right:
                    {
                        e.Handled = true;
                        ViewModel.LogMarkerNow();
                        break;
                    }
                }
            }
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
            bool handled = GlobalCommandManager.ProcessInput(e);

            if (handled)
            {
                e.Handled = true;
            }
            else
            {
                if (e.Delta > 0)
                    ViewModel.VolumeUp();
                else
                    ViewModel.VolumeDown();
            }
        }

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key[] modifiers = {Key.LeftAlt, Key.LeftCtrl, Key.LeftShift, Key.RightAlt, Key.RightCtrl, Key.RightShift};

            if (modifiers.Contains(e.Key))
                return;

            ModifierKeys activeMods = GlobalCommandManager.GetActiveModifierKeys();
            
            bool handled = GlobalCommandManager.ProcessInput(e.Key, activeMods, KeySource.DirectInput);

            if (!handled) return;
                e.Handled = true;
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

        private void MnuDownloadButtplug_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(ButtplugAdapter.GetDownloadUrl());
        }

        private void mnuAttributions_Click(object sender, RoutedEventArgs e)
        {
            new AttributionDialog(){Owner = this}.Show();
        }

        #region IOnScreenDisplay

        public void ShowMessage(string designation, string text, TimeSpan duration)
        {
            Notifications.AddNotification(text, duration, designation);
        }

        public void HideMessage(string designation)
        {
            Notifications.RemoveNotification(designation);
        }

        public void ShowSkipButton()
        {
            Notifications.AddNotification(((DataTemplate)Resources["SkipButton"]).LoadContent(), TimeSpan.MaxValue, "SkipButton", ViewModel.SkipToNextEventCommand);
        }

        public void ShowSkipNextButton()
        {
            Notifications.AddNotification(((DataTemplate)Resources["SkipNextButton"]).LoadContent(), TimeSpan.MaxValue, "SkipButton", ViewModel.SkipToNextEventCommand);
        }

        public void HideSkipButton()
        {
            Notifications.RemoveNotification("SkipButton");
        }

        private void Notifications_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.SetMainWindowOsd(this);
        }

        #endregion

        private void BtnShowLoadedFiles_Click(object sender, RoutedEventArgs e)
        {
            string message 
                = "Video:\n" + (ViewModel.LoadedVideo ?? "None") + "\n\n"
                + "Script:\n" + (ViewModel.LoadedScript ?? "None") + "\n\n"
                + "Audio:\n" + (ViewModel.LoadedAudio ?? "None");

            MessageBox.Show(message, "Loaded Files", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCompactControls_Click(object sender, RoutedEventArgs e)
        {
            CompactControlWindow existing = Application.Current.Windows.OfType<CompactControlWindow>().FirstOrDefault();

            if (existing == null || !existing.IsLoaded)
            {
                CompactControlWindow controls = new CompactControlWindow(ViewModel);
                controls.Show();
                controls.Topmost = false;
                controls.Topmost = true;
            }
            else
            {
                if (existing.WindowState == WindowState.Minimized)
                    existing.WindowState = WindowState.Normal;

                existing.Activate();
                existing.Focus();

                existing.Topmost = false;
                existing.Topmost = true;
            }
        }
    }
}
