using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using ScriptPlayer.ButtplugConnector;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;
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
        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(MainWindow), new PropertyMetadata(50.0, OnVolumePropertyChanged));

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            OverlayText.SetText($"Volume: {Volume:f0}%", TimeSpan.FromSeconds(2));
        }

        public double Volume
        {
            get { return (double) GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty MinPositionProperty = DependencyProperty.Register(
            "MinPosition", typeof(byte), typeof(MainWindow), new PropertyMetadata(default(byte)));

        public byte MinPosition
        {
            get { return (byte) GetValue(MinPositionProperty); }
            set { SetValue(MinPositionProperty, value); }
        }

        public static readonly DependencyProperty MaxPositionProperty = DependencyProperty.Register(
            "MaxPosition", typeof(byte), typeof(MainWindow), new PropertyMetadata(default(byte)));

        public byte MaxPosition
        {
            get { return (byte) GetValue(MaxPositionProperty); }
            set { SetValue(MaxPositionProperty, value); }
        }

        private bool _wasPlaying;
        private ScriptHandler _scriptHandler;

        private Launch _launch;
        private LaunchBluetooth _launchConnect;

        private ButtplugWebSocketConnector _connector;

        private bool _fullscreen;
        private Rect _windowPosition;
        private WindowState _windowState;

        public MainWindow()
        {
            _supportedScriptExtensions = ScriptLoaderManager.GetSupportedExtensions();

            InitializeComponent();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
            OverlayText.SetText("Play", TimeSpan.FromSeconds(1));
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Pause();
            OverlayText.SetText("Pause", TimeSpan.FromSeconds(1));
        }

        private void SeekBar_OnSeek(object sender, double relative, TimeSpan absolute, int downmoveup)
        {
            switch (downmoveup)
            {
                case 0:
                    _wasPlaying = VideoPlayer.IsPlaying;
                    VideoPlayer.Pause();
                    VideoPlayer.SetPosition(absolute);
                    break;
                case 1:
                    VideoPlayer.SetPosition(absolute);
                    break;
                case 2:
                    VideoPlayer.SetPosition(absolute);
                    if (_wasPlaying)
                        VideoPlayer.Play();
                    break;
            }   
        }

        private readonly string[] _supportedVideoExtensions = {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v","mov", "wmv","asf"};
        private readonly string[] _supportedScriptExtensions;

        private string _openVideo;
        private string _openScript;
        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;

        private void mnuOpenVideo_Click(object sender, RoutedEventArgs e)
        {
            string videoFilters = String.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"));

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = $"Videos|{videoFilters}|All Files|*.*",
                FilterIndex = _lastVideoFilterIndex
            };

            if (dialog.ShowDialog(this) != true) return;

            _lastVideoFilterIndex = dialog.FilterIndex;
            string filename = dialog.FileName;

            LoadVideo(filename, true);
        }

        private void LoadVideo(string filename, bool checkForScript)
        {
            _openVideo = filename;
            VideoPlayer.Open(filename);

            if(checkForScript)
                TryFindMatchingScript(filename);

            VideoPlayer.Play();
        }

        private void TryFindMatchingScript(string filename)
        {
            if (VideoAndScriptNamesMatch())
                return;

            string scriptFile = FindFile(filename, ScriptLoaderManager.GetSupportedExtensions());
            if (!String.IsNullOrWhiteSpace(scriptFile))
            {
                string nameOnly = Path.GetFileName(scriptFile);
                if(MessageBox.Show(this, $"Do you want to also load '{nameOnly}'?","Also load Script?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    LoadScript(scriptFile, false);
            }
        }

        private void TryFindMatchingVideo(string filename)
        {
            if (VideoAndScriptNamesMatch())
                return;

            string videoFile = FindFile(filename, _supportedVideoExtensions);
            if (!String.IsNullOrWhiteSpace(videoFile))
            {
                string nameOnly = Path.GetFileName(videoFile);
                if (MessageBox.Show(this, $"Do you want to also load '{nameOnly}'?", "Also load Video?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    LoadVideo(videoFile, false);
            }
        }

        private string FindFile(string filename, string[] extensions)
        {
            foreach (string extension in extensions)
            {
                string path = Path.ChangeExtension(filename, extension);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private bool VideoAndScriptNamesMatch()
        {
            if (String.IsNullOrWhiteSpace(_openScript))
                return false;

            if (String.IsNullOrWhiteSpace(_openVideo))
                return false;

            if (Path.GetDirectoryName(_openScript) != Path.GetDirectoryName(_openVideo))
                return false;

            return Path.GetFileNameWithoutExtension(_openScript).Equals(Path.GetFileNameWithoutExtension(_openVideo));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeLaunchFinder();
            InitializeScriptHandler();
            CheckForArguments();
        }

        private void InitializeLaunchFinder()
        {
            _launchConnect = new LaunchBluetooth();
            _launchConnect.DeviceFound += LaunchConnectOnDeviceFound;
        }

        private void CheckForArguments()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length <= 1) return;

            string fileToLoad = args[1];
            if (File.Exists(fileToLoad))
                LoadFile(fileToLoad);
        }

        private void LoadFile(string fileToLoad)
        {
            string extension = Path.GetExtension(fileToLoad);
            if (String.IsNullOrWhiteSpace(extension))
                return;

            extension = extension.TrimStart('.').ToLower();

            if (_supportedVideoExtensions.Contains(extension))
                LoadVideo(fileToLoad, true);
            else if (_supportedScriptExtensions.Contains(extension))
                LoadScript(fileToLoad, true);
        }

        private void LoadScript(string fileToLoad, bool checkForVideo)
        {
            ScriptLoader loader = ScriptLoaderManager.GetLoader(fileToLoad);
            if (loader == null)
                return;

            var actions = loader.Load(fileToLoad);
            _scriptHandler.SetScript(actions);
            _openScript = fileToLoad;

            if(checkForVideo)
                TryFindMatchingVideo(fileToLoad);

            UpdateHeatMap();
        }

        private void UpdateHeatMap()
        {
            if (cckHeatMap.IsChecked != true)
                SeekBar.Background = System.Windows.Media.Brushes.Black;
            else
            {
                var timeStamps = _scriptHandler.GetScript().Select(s => s.TimeStamp).ToList();
                int segments = Math.Max(20, (int)VideoPlayer.Duration.Divide(TimeSpan.FromSeconds(10)));
                SeekBar.Background = HeatMapGenerator.Generate(timeStamps, TimeSpan.Zero, VideoPlayer.Duration, segments, true);
            }
        }


        private void InitializeScriptHandler()
        {
            _scriptHandler = new ScriptHandler();
            _scriptHandler.ScriptActionRaised += ScriptHandlerOnScriptActionRaised;
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(-300);
            _scriptHandler.SetTimesource(VideoPlayer.TimeSource);
        }

        private void ScriptHandlerOnScriptActionRaised(object sender, ScriptActionEventArgs eventArgs)
        {
            if (eventArgs.RawCurrentAction is RawScriptAction)
            {
                HandleRawScriptAction(eventArgs.Cast<RawScriptAction>());
            }
            else if (eventArgs.RawCurrentAction is FunScriptAction)
            {
                HandleFunScriptAction(eventArgs.Cast<FunScriptAction>());
            }
        }

        private void HandleFunScriptAction(ScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            if (eventArgs.NextAction == null)
                return;

            eventArgs.CurrentAction.Position = TransformPosition(eventArgs.CurrentAction.Position);
            eventArgs.NextAction.Position = TransformPosition(eventArgs.NextAction.Position);


            byte position = eventArgs.NextAction.Position;
            TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
            byte speed = SpeedPredictor.Predict2((byte) Math.Abs(eventArgs.CurrentAction.Position - position), duration);
            SetLaunch(position, speed);
        }

        private byte TransformPosition(byte pos)
        {
            //TODO: Add proper Settings in MainWindow
            //return (byte) (pos > 50 ? 95 : 5);
            return pos;
        }

        private void HandleRawScriptAction(ScriptActionEventArgs<RawScriptAction> eventArgs)
        {
            SetLaunch(eventArgs.CurrentAction.Position, eventArgs.CurrentAction.Speed);
        }

        private void SetLaunch(byte position, byte speed)
        {
            if (VideoPlayer.IsPlaying)
            {
                _launch?.EnqueuePosition(position, speed);
                _connector?.SetPosition(position, speed);
            }
        }

        private void btnConnectLaunch_OnClick(object sender, RoutedEventArgs e)
        {
            _launchConnect.Start();
        }

        private void LaunchConnectOnDeviceFound(object sender, Launch device)
        {
            _launch = device;
            _launch.Disconnected += LaunchOnDisconnected;

            OverlayText.SetText("Launch Connected", TimeSpan.FromSeconds(1));
        }

        private void LaunchOnDisconnected(object sender, Exception exception)
        {
            _launch.Disconnected -= LaunchOnDisconnected;
            _launch = null;

            OverlayText.SetText("Launch Disconnected", TimeSpan.FromSeconds(1));
        }

        private void mnuOpenScript_Click(object sender, RoutedEventArgs e)
        {
            var formats = ScriptLoaderManager.GetFormats();

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = formats.BuildFilter(true),
                FilterIndex = _lastScriptFilterIndex
            };

            if (dialog.ShowDialog(this) != true) return;
            _lastScriptFilterIndex = dialog.FilterIndex;
            var format = formats.GetFormat(dialog.FilterIndex -1, dialog.FileName);

            var loader = ScriptLoaderManager.GetLoader(format);
            var actions = loader.Load(dialog.FileName);

            _scriptHandler.SetScript(actions);
            _openScript = dialog.FileName;

            TryFindMatchingVideo(dialog.FileName);

            UpdateHeatMap();
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
            TogglePlayback();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;
            Volume = Math.Min(100.0, Math.Max(0, 5 * ((int)(Volume/5) + Math.Sign(e.Delta))));
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
                    TogglePlayback();
                    break;
                }
                case Key.Left:
                {
                    VideoPlayer.SetPosition(VideoPlayer.GetPosition() - TimeSpan.FromSeconds(5));
                    ShowPosition();
                    break;
                }
                case Key.Right:
                {
                    VideoPlayer.SetPosition(VideoPlayer.GetPosition() + TimeSpan.FromSeconds(5));
                    ShowPosition();
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

        private void ShowPosition()
        {
            OverlayText.SetText($@"{VideoPlayer.TimeSource.Progress:h\:mm\:ss} / {VideoPlayer.Duration:h\:mm\:ss}", TimeSpan.FromSeconds(1));
        }

        private void TogglePlayback()
        {
            if(VideoPlayer.IsPlaying)
                VideoPlayer.Pause();
            else
                VideoPlayer.Play();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            VideoPlayer.Pause();
            _launch?.Close();
            _connector?.Disconnect();
        }

        private void sldDelay_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(e.NewValue);
        }

        private void CckHeatMap_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateHeatMap();
        }

        private async void btnConnectButtplug_OnClick(object sender, RoutedEventArgs e)
        {
            _connector = new ButtplugWebSocketConnector();
            bool success = await _connector.Connect();

            if(success)
                OverlayText.SetText("Connected to Buttplug", TimeSpan.FromSeconds(1));
            else
                OverlayText.SetText("Could not connect to Buttplug", TimeSpan.FromSeconds(2));
        }

        private void cckWithResponse_Checked(object sender, RoutedEventArgs e)
        {
            if(_launch != null)
                _launch.SendCommandsWithResponse = cckWithResponse.IsChecked == true;
        }

        private void btnMeasureSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (_launch == null)
                return;
            for (int i = 0; i < 10; i++)
            {
                _launch.EnqueuePosition((byte) (i%2 == 0?20:70),50);
            }
           
        }
    }
}
