using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Buttplug.Client;
using ScriptPlayer.Dialogs;
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
        public static readonly DependencyProperty ButtplugApiVersionProperty = DependencyProperty.Register(
            "ButtplugApiVersion", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string ButtplugApiVersion
        {
            get { return (string) GetValue(ButtplugApiVersionProperty); }
            set { SetValue(ButtplugApiVersionProperty, value); }
        }

        public static readonly DependencyProperty PatternSourceProperty = DependencyProperty.Register(
            "PatternSource", typeof(PatternSource), typeof(MainWindow), new PropertyMetadata(PatternSource.Video));

        public PatternSource PatternSource
        {
            get { return (PatternSource)GetValue(PatternSourceProperty); }
            set { SetValue(PatternSourceProperty, value); }
        }

        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            "Playlist", typeof(ObservableCollection<PlaylistEntry>), typeof(MainWindow), new PropertyMetadata(default(ObservableCollection<PlaylistEntry>)));

        public ObservableCollection<PlaylistEntry> Playlist
        {
            get { return (ObservableCollection<PlaylistEntry>)GetValue(PlaylistProperty); }
            set { SetValue(PlaylistProperty, value); }
        }

        public static readonly DependencyProperty CommandDelayProperty = DependencyProperty.Register(
            "CommandDelay", typeof(double), typeof(MainWindow), new PropertyMetadata(166.0, OnCommandDelayPropertyChanged));

        private static void OnCommandDelayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow)d).CommandDelayChanged();
        }

        private void CommandDelayChanged()
        {
            if (_launch != null)
                _launch.MinDelayBetweenCommands = TimeSpan.FromMilliseconds(CommandDelay);
        }

        public double CommandDelay
        {
            get { return (double)GetValue(CommandDelayProperty); }
            set { SetValue(CommandDelayProperty, value); }
        }

        public static readonly DependencyProperty SpeedMultiplierProperty = DependencyProperty.Register(
            "SpeedMultiplier", typeof(double), typeof(MainWindow), new PropertyMetadata(1.0));

        public double SpeedMultiplier
        {
            get { return (double)GetValue(SpeedMultiplierProperty); }
            set { SetValue(SpeedMultiplierProperty, value); }
        }

        public static readonly DependencyProperty VolumeProperty = DependencyProperty.Register(
            "Volume", typeof(double), typeof(MainWindow), new PropertyMetadata(50.0, OnVolumePropertyChanged));

        private static void OnVolumePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow)d).OnVolumeChanged();
        }

        private void OnVolumeChanged()
        {
            OverlayText.SetText($"Volume: {Volume:f0}%", TimeSpan.FromSeconds(4));
        }

        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        public static readonly DependencyProperty MinPositionProperty = DependencyProperty.Register(
            "MinPosition", typeof(byte), typeof(MainWindow), new PropertyMetadata((byte)15));

        public byte MinPosition
        {
            get { return (byte)GetValue(MinPositionProperty); }
            set { SetValue(MinPositionProperty, value); }
        }

        public static readonly DependencyProperty MaxPositionProperty = DependencyProperty.Register(
            "MaxPosition", typeof(byte), typeof(MainWindow), new PropertyMetadata((byte)95));

        public byte MaxPosition
        {
            get { return (byte)GetValue(MaxPositionProperty); }
            set { SetValue(MaxPositionProperty, value); }
        }

        public static readonly DependencyProperty ConversionModeProperty = DependencyProperty.Register(
            "ConversionMode", typeof(BeatsToFunScriptConverter.ConversionMode), typeof(MainWindow), new PropertyMetadata(BeatsToFunScriptConverter.ConversionMode.UpOrDown, OnConversionModePropertyChanged));

        private static void OnConversionModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MainWindow) d).OnConversionModeChanged();
        }

        private void OnConversionModeChanged()
        {
            _scriptHandler.ConversionMode = ConversionMode;
            UpdateHeatMap();
        }

        public BeatsToFunScriptConverter.ConversionMode ConversionMode
        {
            get { return (BeatsToFunScriptConverter.ConversionMode) GetValue(ConversionModeProperty); }
            set { SetValue(ConversionModeProperty, value); }
        }

        public static readonly DependencyProperty ConversionModesProperty = DependencyProperty.Register(
            "ConversionModes", typeof(List<BeatsToFunScriptConverter.ConversionMode>), typeof(MainWindow), new PropertyMetadata(default(List<BeatsToFunScriptConverter.ConversionMode>)));

        public List<BeatsToFunScriptConverter.ConversionMode> ConversionModes
        {
            get { return (List<BeatsToFunScriptConverter.ConversionMode>) GetValue(ConversionModesProperty); }
            set { SetValue(ConversionModesProperty, value); }
        }

        private string _openVideo;
        private string _openScript;

        private bool _wasPlaying;
        private ScriptHandler _scriptHandler;

        private Launch _launch;
        private LaunchBluetooth _launchConnect;

        private ButtplugAdapter _connector;
        private int _lastPos = 100;

        private bool _fullscreen;
        private Rect _windowPosition;
        private WindowState _windowState;

        public MainWindow()
        {
            Playlist = new ObservableCollection<PlaylistEntry>();
            _supportedScriptExtensions = ScriptLoaderManager.GetSupportedExtensions();
            ConversionModes = Enum.GetValues(typeof(BeatsToFunScriptConverter.ConversionMode)).Cast<BeatsToFunScriptConverter.ConversionMode>().ToList();

            ButtplugApiVersion = GetButtplugApiVersion();

            InitializeComponent();

            cmbPattern.Items.Add("Go to 0");
            cmbPattern.Items.Add("Go to 99");
            cmbPattern.Items.Add("SawTooth Up");
            cmbPattern.Items.Add("SawTooth Down");
            cmbPattern.Items.Add("Stairs Up");
            cmbPattern.Items.Add("Stairs Down");
        }

        private string GetButtplugApiVersion()
        {
            string location = typeof(ButtplugWSClient).Assembly.Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        private void btnTestPattern_Click(object sender, RoutedEventArgs e)
        {
            switch (cmbPattern.SelectedIndex)
            {
                case 0:
                    TestPattern(TimeSpan.FromMilliseconds(200), 0);
                    break;
                case 1:
                    TestPattern(TimeSpan.FromMilliseconds(200), 99);
                    break;
                case 2:
                    TestPattern(TimeSpan.FromMilliseconds(200), 0, 10, 0, 20, 0, 30, 0, 40, 0, 50, 0, 60, 0, 70, 0, 80, 0, 90, 0, 99);
                    break;
                case 3:
                    TestPattern(TimeSpan.FromMilliseconds(200), 99, 90, 99, 80, 99, 70, 99, 60, 99, 50, 99, 40, 99, 30, 99, 20, 99, 10, 99, 0);
                    break;
                case 4:
                    TestPattern(TimeSpan.FromMilliseconds(200), 0, 20, 10, 30, 20, 40, 30, 50, 40, 60, 50, 70, 60, 80, 70, 90, 80, 99, 90);
                    break;
                case 5:
                    TestPattern(TimeSpan.FromMilliseconds(200), 99, 80, 90, 70, 80, 60, 70, 50, 60, 40, 50, 30, 40, 20, 30, 10, 20, 0);
                    break;

            }
        }

        private async void TestPattern(TimeSpan delay, params byte[] positions)
        {
            SetLaunch(
                new DeviceCommandInformation
                {
                    Duration = delay,
                    SpeedTransformed = 20,
                    SpeedOriginal = 20,
                    PositionFromOriginal = 0,
                    PositionFromTransformed = 0,
                    PositionToOriginal = positions[0],
                    PositionToTransformed = TransformPosition(positions[0], 0, 99)
                }
            );
            await Task.Delay(300);

            for (int i = 1; i < positions.Length; i++)
            {

                var info = new DeviceCommandInformation
                {
                    Duration = delay,
                    PositionFromOriginal = positions[i - 1],
                    PositionToOriginal = positions[i],
                    PositionFromTransformed = TransformPosition(positions[i - 1], 0, 99),
                    PositionToTransformed = TransformPosition(positions[i], 0, 99)
                };

                info.SpeedOriginal = SpeedPredictor.Predict2(info.PositionFromOriginal, info.PositionToOriginal, delay);
                info.SpeedTransformed = SpeedPredictor.Predict2(info.PositionFromTransformed, info.PositionToTransformed, delay);

                SetLaunch(info, false);

                if (i + 1 < positions.Length)
                    await Task.Delay(delay);
            }
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

        private readonly string[] _supportedVideoExtensions = { "mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf" };
        private readonly string[] _supportedScriptExtensions;

        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;
        private byte _minScriptPosition;
        private byte _maxScriptPosition;

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
            if (checkForScript)
                TryFindMatchingScript(filename);

            _openVideo = filename;
            VideoPlayer.Open(filename);

            SetTitle(filename);

            OverlayText.SetText($"Loaded {Path.GetFileName(filename)}", TimeSpan.FromSeconds(4));

            Play();
        }

        public void SetTitle(string filePath)
        {
            Title = "ScriptPlayer - " + Path.GetFileNameWithoutExtension(filePath);
        }

        private void TryFindMatchingScript(string filename)
        {
            if (VideoAndScriptNamesMatch())
                return;

            string scriptFile = FindFile(filename, ScriptLoaderManager.GetSupportedExtensions());
            if (!String.IsNullOrWhiteSpace(scriptFile))
            {
                string nameOnly = Path.GetFileName(scriptFile);
                if (MessageBox.Show(this, $"Do you want to also load '{nameOnly}'?", "Also load Script?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                //if (MessageBox.Show(this, $"Do you want to also load '{nameOnly}'?", "Also load Video?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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

            LoadScript(loader, fileToLoad);

            OverlayText.SetText($"Loaded {Path.GetFileName(fileToLoad)}", TimeSpan.FromSeconds(4));

            if (checkForVideo)
                TryFindMatchingVideo(fileToLoad);
        }

        private void LoadScript(ScriptLoader loader, string fileName)
        {
            var actions = loader.Load(fileName);
            _scriptHandler.SetScript(actions);
            _openScript = fileName;

            FindMaxPositions();
            UpdateHeatMap();
        }

        private void FindMaxPositions()
        {
            var actions = _scriptHandler.GetScript();

            byte minPos = 99;
            byte maxPos = 0;

            foreach (var action in actions)
            {
                if (action is FunScriptAction)
                {
                    byte position = ((FunScriptAction)action).Position;
                    minPos = Math.Min(minPos, position);
                    maxPos = Math.Max(maxPos, position);
                }
                else if (action is RawScriptAction)
                {
                    byte position = ((RawScriptAction)action).Position;
                    minPos = Math.Min(minPos, position);
                    maxPos = Math.Max(maxPos, position);
                }
            }

            _minScriptPosition = minPos;
            _maxScriptPosition = maxPos;
        }

        private void UpdateHeatMap()
        {
            if (cckHeatMap.IsChecked != true)
                SeekBar.Background = System.Windows.Media.Brushes.Black;
            else
            {
                var timeStamps = _scriptHandler.GetScript().Select(s => s.TimeStamp).ToList();
                int segments = Math.Max(20, (int)VideoPlayer.Duration.Divide(TimeSpan.FromSeconds(10)));
                var heatmap = HeatMapGenerator.Generate(timeStamps, TimeSpan.Zero, VideoPlayer.Duration, segments, true, 0.1);
                heatmap.Opacity = 0.8;
                SeekBar.Background = heatmap;
            }
        }


        private void InitializeScriptHandler()
        {
            _scriptHandler = new ScriptHandler();
            _scriptHandler.ScriptActionRaised += ScriptHandlerOnScriptActionRaised;
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(0);
            _scriptHandler.SetTimesource(VideoPlayer.TimeSource);
        }

        private void ScriptHandlerOnScriptActionRaised(object sender, ScriptActionEventArgs eventArgs)
        {
            if (PatternSource != PatternSource.Video)
                return;

            flash.Now();

            if (eventArgs.RawCurrentAction is FunScriptAction)
            {
                HandleFunScriptAction(eventArgs.Cast<FunScriptAction>());
            }
        }

        private void HandleFunScriptAction(ScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            if (eventArgs.NextAction == null)
            {
                if (cckAutoSkip.IsChecked == true)
                {
                    LoadNextPlaylistEntry();
                }
                else
                {
                    OverlayText.SetText("No more events available", TimeSpan.FromSeconds(4));
                }
                return;
            }

            TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
            byte currentPositionTransformed = TransformPosition(eventArgs.CurrentAction.Position);
            byte nextPositionTransformed = TransformPosition(eventArgs.NextAction.Position);

            if (currentPositionTransformed == nextPositionTransformed) return;

            byte speedOriginal = SpeedPredictor.Predict((byte)Math.Abs(eventArgs.CurrentAction.Position - eventArgs.NextAction.Position), duration);
            byte speedTransformed = SpeedPredictor.Predict((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed), duration);
            speedTransformed = (byte)Math.Min(99, Math.Max(0, speedTransformed * SpeedMultiplier));


            DeviceCommandInformation info = new DeviceCommandInformation
            {
                Duration = duration,
                SpeedTransformed = speedTransformed,
                SpeedOriginal = speedOriginal,
                PositionFromTransformed = currentPositionTransformed,
                PositionToTransformed = nextPositionTransformed,
                PositionFromOriginal = eventArgs.CurrentAction.Position,
                PositionToOriginal = eventArgs.NextAction.Position
            };

            if (duration > TimeSpan.FromSeconds(10) && VideoPlayer.IsPlaying)
            {
                if (cckAutoSkip.IsChecked == true)
                {
                    SkipToNextEvent();
                }
                else
                {
                    OverlayText.SetText($"Next event in {duration.TotalSeconds:f0}s", TimeSpan.FromSeconds(4));
                }
            }

            SetLaunch(info);
        }

        private PatternGenerator _pattern;
        private Thread _repeaterThread;
        //private ButtplugClientDevice _device;

        private byte TransformPosition(byte pos, byte inMin, byte inMax)
        {
            double relative = (double)(pos - inMin) / (inMax - inMin);
            relative = Math.Min(1, Math.Max(0, relative));
            byte absolute = (byte)(MinPosition + (MaxPosition - MinPosition) * relative);
            return SpeedPredictor.Clamp(absolute);
        }

        private byte TransformPosition(byte pos)
        {
            return TransformPosition(pos, _minScriptPosition, _maxScriptPosition);
        }

        private void SetLaunch(DeviceCommandInformation information, bool requirePlaying = true)
        {
            if (VideoPlayer.IsPlaying || !requirePlaying)
            {
                _launch?.EnqueuePosition(information.PositionToTransformed, information.SpeedTransformed);
                _connector?.Set(information);
            }
        }

        private void mnuConnectLaunch_OnClick(object sender, RoutedEventArgs e)
        {
            _launchConnect.Start();
        }

        private void LaunchConnectOnDeviceFound(object sender, Launch device)
        {
            _launch = device;
            _launch.Disconnected += LaunchOnDisconnected;

            OverlayText.SetText("Launch Connected", TimeSpan.FromSeconds(8));
        }

        private void LaunchOnDisconnected(object sender, Exception exception)
        {
            _launch.Disconnected -= LaunchOnDisconnected;
            _launch = null;

            OverlayText.SetText("Launch Disconnected", TimeSpan.FromSeconds(8));
        }

        private void mnuAddScripts_Click(object sender, RoutedEventArgs e)
        {
            var formats = ScriptLoaderManager.GetFormats();

            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = formats.BuildFilter(true),
                FilterIndex = _lastScriptFilterIndex,
                Multiselect = true
            };

            if (dialog.ShowDialog(this) != true) return;

            foreach (string filename in dialog.FileNames)
            {
                Playlist.Add(new PlaylistEntry(filename));
            }
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
            var format = formats.GetFormat(dialog.FilterIndex - 1, dialog.FileName);

            var loader = ScriptLoaderManager.GetLoader(format);

            LoadScript(loader, dialog.FileName);

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

                Grid.SetRow(flash, 0);
                Grid.SetRowSpan(flash, 3);
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

                Grid.SetRow(flash, 1);
                Grid.SetRowSpan(flash, 1);
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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = true;

            if (e.Delta > 0)
                VolumeUp();
            else
                VolumeDown();
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(e);
        }

        private void VolumeDown()
        {
            int oldVolume = (int)Math.Round(Volume);
            int newVolume = (int)(5.0 * Math.Floor(Volume / 5.0));
            if (oldVolume == newVolume)
                newVolume -= 5;

            newVolume = Math.Min(100, Math.Max(0, newVolume));

            Volume = newVolume;
        }

        private void VolumeUp()
        {
            int oldVolume = (int)Math.Round(Volume);
            int newVolume = (int)(5.0 * Math.Ceiling(Volume / 5.0));
            if (oldVolume == newVolume)
                newVolume += 5;

            newVolume = Math.Min(100, Math.Max(0, newVolume));

            Volume = newVolume;
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
                case Key.Up:
                    {
                        VolumeUp();
                        break;
                    }
                case Key.Down:
                    {
                        VolumeDown();
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

        private void ShowPosition(string prefix = "")
        {
            OverlayText.SetText($@"{prefix}{VideoPlayer.TimeSource.Progress:h\:mm\:ss} / {VideoPlayer.Duration:h\:mm\:ss}", TimeSpan.FromSeconds(3));
        }

        private void TogglePlayback()
        {
            if (VideoPlayer.IsPlaying)
                Pause();
            else
                Play();
        }

        private void Pause()
        {
            if (!String.IsNullOrWhiteSpace(_openVideo))
            {
                VideoPlayer.Pause();
                btnPlayPause.Content = "4";
                OverlayText.SetText("Pause", TimeSpan.FromSeconds(2));
            }
        }

        private void Play()
        {
            if (!String.IsNullOrWhiteSpace(_openVideo))
            {
                VideoPlayer.Play();
                OverlayText.SetText("Play", TimeSpan.FromSeconds(2));
                btnPlayPause.Content = ";";
            }
            else if (Playlist.Count > 0)
            {
                LoadScript(Playlist[0].Fullname, true);
            }
        }


        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayback();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            VideoPlayer.Pause();
            _launch?.Close();

            try
            {
                _connector?.Disconnect();
            }
            catch { }
        }

        private void sldDelay_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(e.NewValue);
        }

        private void CckHeatMap_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateHeatMap();
        }

        private async void mnuConnectButtplug_OnClick(object sender, RoutedEventArgs e)
        {
            if (_connector != null)
            {
                await _connector?.Disconnect();
            }

            ButtplugConnectionSettingsDialog dialog = new ButtplugConnectionSettingsDialog
            {
                Url= "ws://localhost:12345/buttplug",
                Owner = this,
            };

            if (dialog.ShowDialog() != true)
                return;

            _connector = new ButtplugAdapter(dialog.Url);
            _connector.DeviceAdded += ConnectorOnDeviceAdded;

            bool success = await _connector.Connect();

            if(success)
                OverlayText.SetText("Connected to Buttplug", TimeSpan.FromSeconds(6));
            else
            {
                _connector = null;
                OverlayText.SetText("Could not connect to Buttplug", TimeSpan.FromSeconds(6));
            }
        }

        private void ConnectorOnDeviceAdded(object sender, string deviceName)
        {
            OverlayText.SetText("Device found: " + deviceName, TimeSpan.FromSeconds(5));
        }


        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            SkipToNextEvent();
        }

        private void SkipToNextEvent()
        {
            var currentPosition = VideoPlayer.GetPosition();
            ScriptAction nextAction = _scriptHandler.FirstEventAfter(currentPosition - _scriptHandler.Delay);
            if (nextAction == null)
            {
                OverlayText.SetText("No more events available", TimeSpan.FromSeconds(4));
                return;
            }

            TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(3);
            TimeSpan duration = skipTo - currentPosition;

            if (skipTo < currentPosition)
                return;

            VideoPlayer.SetPosition(skipTo);
            ShowPosition($"Skipped {duration.TotalSeconds:f0}s - ");
        }

        private void mnuShowPlaylist_Click(object sender, RoutedEventArgs e)
        {
            PlaylistWindow playlist = new PlaylistWindow(Playlist);
            playlist.EntrySelected += PlaylistOnEntrySelected;
            playlist.Show();
        }

        private void PlaylistOnEntrySelected(object sender, PlaylistEntry playlistEntry)
        {
            LoadScript(playlistEntry.Fullname, true);
        }

        private void VideoPlayer_MediaEnded(object sender, EventArgs e)
        {
            LoadNextPlaylistEntry();
        }

        private void LoadNextPlaylistEntry()
        {
            var currentEntry = Playlist.FirstOrDefault(p => p.Fullname == _openScript);

            if (currentEntry == null) return;

            int currentIndex = Playlist.IndexOf(currentEntry);
            var nextEntry = Playlist[(currentIndex + 1) % Playlist.Count];

            LoadScript(nextEntry.Fullname, true);
        }

        private void LoadPreviousPlaylistEntry()
        {
            var currentEntry = Playlist.FirstOrDefault(p => p.Fullname == _openScript);

            if (currentEntry == null) return;

            int currentIndex = Playlist.IndexOf(currentEntry);
            var nextEntry = Playlist[(currentIndex - 1 + Playlist.Count) % Playlist.Count];

            LoadScript(nextEntry.Fullname, true);
        }

        private void VideoPlayer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (cckLogMarkers.IsChecked != true)
                return;

            try
            {
                if (String.IsNullOrWhiteSpace(_openVideo))
                    return;

                var position = VideoPlayer.GetPosition();

                var logFile = Path.ChangeExtension(_openVideo, ".log");

                var line = position.ToString("hh\\:mm\\:ss");

                File.AppendAllLines(logFile, new[] { line });

                OverlayText.SetText("Logged marker at " + line, TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void VideoPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (cckAutoSkip.IsChecked == true)
            {
                SkipToNextEvent();
            }

            UpdateHeatMap();
        }

        private void btnPatternFast_Click(object sender, RoutedEventArgs e)
        {
            PatternSource = PatternSource.Generator;
            StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(50));
        }
        private void btnPatternSlow_Click(object sender, RoutedEventArgs e)
        {
            PatternSource = PatternSource.Generator;
            StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(600));
        }

        private void StartRegularPattern(byte positionFrom, byte positionTo, TimeSpan intervall)
        {
            var pattern = new RepeatingPatternGenerator();

            pattern.Add(positionFrom, intervall);
            pattern.Add(positionTo, intervall);

            StartPattern(pattern);
        }

        private void StartPattern(PatternGenerator generator)
        {
            StopPattern();

            _pattern = generator;

            _repeaterThread = new Thread(() =>
            {
                var enumerator = _pattern.Get();

                while (enumerator.MoveNext())
                {
                    var transistion = enumerator.Current;

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var info = new DeviceCommandInformation
                        {
                            Duration = transistion.Duration,
                            PositionFromOriginal = transistion.From,
                            PositionToOriginal = transistion.To,
                            PositionFromTransformed = TransformPosition(transistion.From, 0, 99),
                            PositionToTransformed = TransformPosition(transistion.To, 0, 99)
                        };

                        info.SpeedOriginal = SpeedPredictor.Predict2(info.PositionFromOriginal, info.PositionToOriginal, transistion.Duration);
                        info.SpeedTransformed = SpeedPredictor.Predict2(info.PositionFromTransformed, info.PositionToTransformed, transistion.Duration);

                        SetLaunch(info, false);
                    }));
                }

            });
            _repeaterThread.Start();
        }

        private void StopPattern()
        {
            if (_pattern != null)
            {
                _pattern.Stop();
                _pattern = null;
                _repeaterThread.Join();
            }
        }

        private void btnPatternVideo_Click(object sender, RoutedEventArgs e)
        {
            PatternSource = PatternSource.Video;
            StopPattern();
        }

        private void btnPatternNone_Click(object sender, RoutedEventArgs e)
        {
            PatternSource = PatternSource.None;
            StopPattern();
        }

        private void btnPatternRandom_Click(object sender, RoutedEventArgs e)
        {
            PatternSource = PatternSource.Generator;
            StartPattern(new RandomPatternGenerator());
        }

        private void btnNextVideo_Click(object sender, RoutedEventArgs e)
        {
            LoadNextPlaylistEntry();
        }

        private void btnPreviousVideo_Click(object sender, RoutedEventArgs e)
        {
            LoadPreviousPlaylistEntry();
        }

        private void mnuButtplugScan_OnClick(object sender, RoutedEventArgs e)
        {
            _connector.StartScanning();
        }
    }

    public enum PatternSource
    {
        Video,
        None,
        Generator
    }
}
