using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FMUtils.KeyboardHook;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;
using Application = System.Windows.Application;

namespace ScriptPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public delegate void RequestOverlayEventHandler(object sender, string text, TimeSpan duration,
            string designation);

        private readonly string[] _supportedScriptExtensions;

        private readonly string[] _supportedVideoExtensions =
            {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf"};

        private bool _autoSkip;

        private string _buttplugApiVersion = "Unknown";
        private TimeSpan _commandDelay = TimeSpan.FromMilliseconds(166);

        private ButtplugAdapter _connector;

        private ConversionMode _conversionMode = ConversionMode.UpOrDown;

        private List<ConversionMode> _conversionModes;

        private Brush _heatMap;

        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;

        private Launch _launch;
        private LaunchBluetooth _launchConnect;

        private bool _logMarkers;

        private byte _maxPosition = 95;
        private byte _maxScriptPosition;

        private byte _minPosition = 5;
        private byte _minScriptPosition;

        private string _openedScript;
        private string _openVideo;

        private PatternGenerator _pattern;
        private PatternSource _patternSource = PatternSource.Video;
        private PlaylistViewModel _playlist;
        private Thread _repeaterThread;

        private TimeSpan _scriptDelay;
        private ScriptHandler _scriptHandler;

        private TestPatternDefinition _selectedTestPattern;
        private double _speedMultiplier = 1;

        private List<TestPatternDefinition> _testPatterns;

        private string _title = "";
        private VideoPlayer _videoPlayer;

        private double _volume = 50;

        private bool _wasPlaying;
        private bool _loaded;
        private byte _minSpeed = 20;
        private byte _maxSpeed = 95;
        private Hook _hook;
        private bool _blindMode;
        private TimeSource _timeSource;
        private bool _showHeatMap;
        private PositionFilterMode _filterMode = PositionFilterMode.FullRange;
        private double _filterRange = 0.5;
        private List<Range> _filterRanges;

        public List<Range> FilterRanges
        {
            get { return _filterRanges; }
            set
            {
                if (Equals(value, _filterRanges)) return;
                _filterRanges = value;
                OnPropertyChanged();
            }
        }

        public double FilterRange
        {
            get { return _filterRange; }
            set
            {
                if (value.Equals(_filterRange)) return;
                _filterRange = value;
                UpdateFilter();
                OnPropertyChanged();
            }
        }

        public PositionFilterMode FilterMode
        {
            get { return _filterMode; }
            set
            {
                if (value == _filterMode) return;
                _filterMode = value;
                UpdateFilter();
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            ButtplugApiVersion = ButtplugAdapter.GetButtplugApiVersion();
            Playlist = new PlaylistViewModel();
            Playlist.PlayEntry += PlaylistOnPlayEntry;

            ConversionModes = Enum.GetValues(typeof(ConversionMode)).Cast<ConversionMode>().ToList();
            _supportedScriptExtensions = ScriptLoaderManager.GetSupportedExtensions();

            InitializeCommands();
            InitializeTestPatterns();
            InitializeLaunchFinder();
            InitializeScriptHandler();

            LoadSettings();
        }

        private void LoadSettings()
        {
            Settings settings = Settings.FromFile(GetSettingsFile());
            if (settings != null)
            {
                MinSpeed = settings.MinSpeed;
                MaxSpeed = settings.MaxSpeed;
                MinPosition = settings.MinPosition;
                MaxPosition = settings.MaxPosition;
                ScriptDelay = TimeSpan.FromMilliseconds(settings.ScriptDelay);
                SpeedMultiplier = settings.SpeedMultiplier;
                CommandDelay = TimeSpan.FromMilliseconds(settings.CommandDelay);
                AutoSkip = settings.AutoSkip;
                ConversionMode = settings.ConversionMode;
                ShowHeatMap = settings.ShowHeatMap;
                LogMarkers = settings.LogMarkers;
                FilterMode = settings.FilterMode;
                FilterRange = settings.FilterRange;
            }
        }

        private void SaveSettings()
        {
            Settings settings = new Settings();
            settings.MinSpeed = MinSpeed;
            settings.MaxSpeed = MaxSpeed;
            settings.MinPosition = MinPosition;
            settings.MaxPosition = MaxPosition;
            settings.SpeedMultiplier = SpeedMultiplier;
            settings.AutoSkip = AutoSkip;
            settings.ConversionMode = ConversionMode;
            settings.ShowHeatMap = ShowHeatMap;
            settings.LogMarkers = LogMarkers;
            settings.ScriptDelay = ScriptDelay.TotalMilliseconds;
            settings.CommandDelay = CommandDelay.TotalMilliseconds;
            settings.FilterMode = FilterMode;
            settings.FilterRange = FilterRange;

            settings.Save(GetSettingsFile());
        }

        private string GetSettingsFile()
        {
            return Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\Settings.xml");
        }

        private void BlindModeChanged()
        {
            TimeSource?.Pause();

            if (BlindMode)
            {
                TimeSource = new WhirlygigTimeSource(new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                    TimeSpan.FromMilliseconds(10)));

                ((WhirlygigTimeSource)TimeSource).FileOpened += OnFileOpened;

                //TimeSource = new ManualTimeSource(new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                //    TimeSpan.FromMilliseconds(10)));
                RefreshManualDuration();
            }
            else
            {
                TimeSource = VideoPlayer.TimeSource;
            }
        }

        private void OnFileOpened(object sender, string filename)
        {
            TryFindMatchingScript(filename, false);
        }

        private void RefreshManualDuration()
        {
            if (TimeSource is ManualTimeSource source)
                source.SetDuration(_scriptHandler.GetDuration().Add(TimeSpan.FromSeconds(5)));

            if (TimeSource is WhirlygigTimeSource whirly)
                whirly.SetDuration(_scriptHandler.GetDuration().Add(TimeSpan.FromSeconds(5)));
        }

        private void TimeSourceChanged()
        {
            _scriptHandler.SetTimesource(TimeSource);
            UpdateHeatMap();
        }

        public void Load()
        {
            if (_loaded)
                return;

            _loaded = true;
            HookUpMediaKeys();
            CheckForArguments();
        }

        private void HookUpMediaKeys()
        {
            _hook = new Hook("ScriptPlayer");
            _hook.KeyDownEvent += Hook_KeyDownEvent;
        }

        private void Hook_KeyDownEvent(KeyboardHookEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.Play:
                    Play();
                    break;
                case Keys.Pause:
                    Pause();
                    break;
                case Keys.MediaPlayPause:
                    TogglePlayback();
                    break;
                case Keys.MediaNextTrack:
                    if (BlindMode)
                        TimeSource.SetPosition(TimeSource.Progress + TimeSpan.FromMilliseconds(50));
                    else
                        PlayNextPlaylistEntry();
                    break;
                case Keys.MediaPreviousTrack:
                    if (BlindMode)
                        TimeSource.SetPosition(TimeSource.Progress - TimeSpan.FromMilliseconds(50));
                    else
                        PlayPreviousPlaylistEntry();
                    break;
                case Keys.MediaStop:
                    Pause();
                    TimeSource.SetPosition(TimeSpan.Zero);
                    break;
            }
        }

        public TimeSource TimeSource
        {
            get => _timeSource;
            protected set
            {
                if (Equals(value, _timeSource)) return;
                HandleTimeSourceEvents(_timeSource, value);
                _timeSource = value;
                TimeSourceChanged();
                OnPropertyChanged();
            }
        }

        private void HandleTimeSourceEvents(TimeSource oldValue, TimeSource newValue)
        {
            if (oldValue != null)
                oldValue.DurationChanged -= TimeSourceOnDurationChanged;

            if (newValue != null)
                newValue.DurationChanged += TimeSourceOnDurationChanged;
        }

        private void TimeSourceOnDurationChanged(object sender, TimeSpan timeSpan)
        {
            UpdateHeatMap();
        }

        public ConversionMode ConversionMode
        {
            get => _conversionMode;
            set
            {
                if (value == _conversionMode) return;
                _conversionMode = value;
                _scriptHandler.ConversionMode = _conversionMode;
                UpdateHeatMap();
                OnPropertyChanged();
            }
        }

        public List<ConversionMode> ConversionModes
        {
            get => _conversionModes;
            set
            {
                if (Equals(value, _conversionModes)) return;
                _conversionModes = value;
                OnPropertyChanged();
            }
        }

        public byte MinPosition
        {
            get => _minPosition;
            set
            {
                if (value == _minPosition) return;
                _minPosition = value;
                UpdateFilter();
                OnPropertyChanged();
            }
        }

        public byte MinSpeed
        {
            get => _minSpeed;
            set
            {
                if (value == _minSpeed) return;
                _minSpeed = value;
                OnPropertyChanged();
            }
        }

        public byte MaxSpeed
        {
            get => _maxSpeed;
            set
            {
                if (value == _maxSpeed) return;
                _maxSpeed = value;
                OnPropertyChanged();
            }
        }

        public byte MaxPosition
        {
            get => _maxPosition;
            set
            {
                if (value == _maxPosition) return;
                _maxPosition = value;
                UpdateFilter();
                OnPropertyChanged();
            }
        }

        private void UpdateFilter()
        {
            List<Range> newRange = new List<Range>();

            for (double i = 0; i < 10; i += 0.1)
            {
                newRange.Add(new Range
                {
                    Min = TransformPosition(0, MinPosition, MaxPosition, i) / 99.0,
                    Max = TransformPosition(255, MinPosition, MaxPosition, i) / 99.0
                });
            }

            FilterRanges = newRange;
        }

        public double Volume
        {
            get => _volume;
            set
            {
                if (value.Equals(_volume)) return;
                _volume = value;
                OnRequestOverlay($"Volume: {Volume:f0}%", TimeSpan.FromSeconds(4), "Volume");
                OnPropertyChanged();
            }
        }

        public VideoPlayer VideoPlayer
        {
            get => _videoPlayer;
            set
            {
                if (Equals(value, _videoPlayer)) return;
                HandleVideoPlayerEvents(_videoPlayer, value);
                _videoPlayer = value;
                OnPropertyChanged();
            }
        }

        public double SpeedMultiplier
        {
            get => _speedMultiplier;
            set
            {
                if (value.Equals(_speedMultiplier)) return;
                _speedMultiplier = value;
                OnPropertyChanged();
            }
        }

        public string ButtplugApiVersion
        {
            get => _buttplugApiVersion;
            set
            {
                if (value == _buttplugApiVersion) return;
                _buttplugApiVersion = value;
                OnPropertyChanged();
            }
        }

        public PatternSource PatternSource
        {
            get => _patternSource;
            set
            {
                if (value == _patternSource) return;
                _patternSource = value;
                UpdatePattern(_patternSource);
                OnPropertyChanged();
            }
        }

        public bool BlindMode
        {
            get => _blindMode;
            set
            {
                if (value == _blindMode) return;
                _blindMode = value;
                BlindModeChanged();
                OnPropertyChanged();
            }
        }

        public TimeSpan ScriptDelay
        {
            get => _scriptDelay;
            set
            {
                if (value.Equals(_scriptDelay)) return;
                _scriptDelay = value;
                _scriptHandler.Delay = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan CommandDelay
        {
            get => _commandDelay;
            set
            {
                if (value.Equals(_commandDelay)) return;
                _commandDelay = value;
                CommandDelayChanged();
                OnPropertyChanged();
            }
        }

        public Brush HeatMap
        {
            get => _heatMap;
            set
            {
                if (Equals(value, _heatMap)) return;
                _heatMap = value;
                OnPropertyChanged();
            }
        }

        public List<TestPatternDefinition> TestPatterns
        {
            get => _testPatterns;
            set
            {
                if (Equals(value, _testPatterns)) return;
                _testPatterns = value;
                OnPropertyChanged();
            }
        }

        public bool ShowHeatMap
        {
            get => _showHeatMap;
            set
            {
                if (value == _showHeatMap) return;
                _showHeatMap = value;
                OnPropertyChanged();
            }
        }

        public TestPatternDefinition SelectedTestPattern
        {
            get => _selectedTestPattern;
            set
            {
                if (Equals(value, _selectedTestPattern)) return;
                _selectedTestPattern = value;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged();
            }
        }


        public string Title
        {
            get => _title;
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }

        public string OpenedScript
        {
            get => _openedScript;
            set
            {
                if (value == _openedScript) return;
                _openedScript = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand ExecuteSelectedTestPatternCommand { get; set; }

        public RelayCommand VolumeDownCommand { get; set; }

        public RelayCommand VolumeUpCommand { get; set; }

        public RelayCommand TogglePlaybackCommand { get; set; }

        public RelayCommand SkipToNextEventCommand { get; set; }

        public RelayCommand StartScanningButtplugCommand { get; set; }

        public RelayCommand ConnectButtplugCommand { get; set; }

        public RelayCommand ConnectLaunchDirectlyCommand { get; set; }

        public RelayCommand AddScriptsToPlaylistCommand { get; set; }

        public RelayCommand OpenVideoCommand { get; set; }

        public RelayCommand OpenScriptCommand { get; set; }

        public PlaylistViewModel Playlist
        {
            get => _playlist;
            set
            {
                if (Equals(value, _playlist)) return;
                _playlist = value;
                OnPropertyChanged();
            }
        }

        public bool LogMarkers
        {
            get => _logMarkers;
            set
            {
                if (value == _logMarkers) return;
                _logMarkers = value;
                OnPropertyChanged();
            }
        }

        public bool AutoSkip
        {
            get => _autoSkip;
            set
            {
                if (value == _autoSkip) return;
                _autoSkip = value;
                OnPropertyChanged();
            }
        }

        public void Dispose()
        {
            _videoPlayer?.TimeSource.Pause();
            _videoPlayer?.Dispose();
            _launch?.Close();

            try
            {
                _connector?.Disconnect();
            }
            catch (Exception e)
            {
                Debug.WriteLine("MainViewModel.Dispose: " + e.Message);
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public event RequestOverlayEventHandler RequestOverlay;
        public event EventHandler<RequestFileEventArgs> RequestFile;
        public event EventHandler<MessageBoxEventArgs> RequestMessageBox;
        public event EventHandler<ButtplugUrlRequestEventArgs> RequestButtplugUrl;

        private void HandleVideoPlayerEvents(VideoPlayer oldValue, VideoPlayer newValue)
        {
            if (oldValue != null)
            {
                oldValue.MediaOpened -= VideoPlayer_MediaOpened;
                oldValue.MediaEnded -= VideoPlayer_MediaEnded;
                oldValue.MouseRightButtonDown -= VideoPlayer_MouseRightButtonDown;
            }

            if (newValue != null)
            {
                newValue.MediaOpened += VideoPlayer_MediaOpened;
                newValue.MediaEnded += VideoPlayer_MediaEnded;
                newValue.MouseRightButtonDown += VideoPlayer_MouseRightButtonDown;

                TimeSource = newValue.TimeSource;
            }
        }

        private void CommandDelayChanged()
        {
            if (_launch != null)
                _launch.MinDelayBetweenCommands = CommandDelay;
        }

        private void StartRegularPattern(byte positionFrom, byte positionTo, TimeSpan intervall)
        {
            RepeatingPatternGenerator pattern = new RepeatingPatternGenerator();

            pattern.Add(positionFrom, intervall);
            pattern.Add(positionTo, intervall);

            StartPattern(pattern);
        }

        public void OpenVideo()
        {
            string videoFilters =
                $"Videos|{string.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"))}|All Files|*.*";

            string selectedFile = OnRequestFile(videoFilters, ref _lastVideoFilterIndex);
            if (selectedFile == null)
                return;

            LoadVideo(selectedFile, true);
        }

        private void LoadVideo(string filename, bool checkForScript)
        {
            if (checkForScript)
                TryFindMatchingScript(filename);

            _openVideo = filename;

            if (!BlindMode)
                VideoPlayer.Open(filename);

            Title = Path.GetFileNameWithoutExtension(filename);

            OnRequestOverlay($"Loaded {Path.GetFileName(filename)}", TimeSpan.FromSeconds(4), "VideoLoaded");

            Play();
        }

        private void Play()
        {
            if (EntryLoaded())
            {
                TimeSource.Play();
                OnRequestOverlay("Play", TimeSpan.FromSeconds(2), "Playback");
                //btnPlayPause.Content = ";";
            }
            else if (Playlist.EntryCount > 0)
            {
                LoadScript(Playlist.FirstEntry().Fullname, true);
            }
        }

        private bool EntryLoaded()
        {
            if (BlindMode)
                return !string.IsNullOrWhiteSpace(_openedScript);

            return !string.IsNullOrWhiteSpace(_openVideo);
        }

        public void TogglePlayback()
        {
            if (TimeSource.IsPlaying)
                Pause();
            else
                Play();
        }

        private void Pause()
        {
            if (EntryLoaded())
            {
                StopDevices();
                TimeSource.Pause();
                //btnPlayPause.Content = "4";
                OnRequestOverlay("Pause", TimeSpan.FromSeconds(2), "Playback");
            }
        }

        private void TryFindMatchingScript(string filename, bool ask = true)
        {
            if (VideoAndScriptNamesMatch())
                return;

            string scriptFile = FindFile(filename, ScriptLoaderManager.GetSupportedExtensions());
            if (!string.IsNullOrWhiteSpace(scriptFile))
            {
                string nameOnly = Path.GetFileName(scriptFile);
                if(ask)
                    if (OnRequestMessageBox($"Do you want to also load '{nameOnly}'?", "Also load Script?",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                        return;

                LoadScript(scriptFile, false);
            }
        }

        private void TryFindMatchingVideo(string filename)
        {
            if (VideoAndScriptNamesMatch())
                return;

            string videoFile = FindFile(filename, _supportedVideoExtensions);
            if (!string.IsNullOrWhiteSpace(videoFile))
                LoadVideo(videoFile, false);
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
            if (string.IsNullOrWhiteSpace(OpenedScript))
                return false;

            if (string.IsNullOrWhiteSpace(_openVideo))
                return false;

            if (Path.GetDirectoryName(OpenedScript) != Path.GetDirectoryName(_openVideo))
                return false;

            return Path.GetFileNameWithoutExtension(OpenedScript).Equals(Path.GetFileNameWithoutExtension(_openVideo));
        }

        private void StartPattern(PatternGenerator generator)
        {
            StopPattern();

            _pattern = generator;

            _repeaterThread = new Thread(() =>
            {
                IEnumerator<PatternGenerator.PositionTransistion> enumerator = _pattern.Get();

                var app = Application.Current;
                if (app == null) return;

                while (enumerator.MoveNext())
                {
                    PatternGenerator.PositionTransistion transistion = enumerator.Current;
                    if (transistion == null) break;

                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        DeviceCommandInformation info = new DeviceCommandInformation
                        {
                            Duration = transistion.Duration,
                            PositionFromOriginal = transistion.From,
                            PositionToOriginal = transistion.To,
                            PositionFromTransformed = TransformPosition(transistion.From, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                            PositionToTransformed = TransformPosition(transistion.To, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds)
                        };

                        info.SpeedOriginal = SpeedPredictor.Predict2(info.PositionFromOriginal, info.PositionToOriginal,
                            transistion.Duration);
                        info.SpeedTransformed = ClampSpeed(SpeedPredictor.Predict2(info.PositionFromTransformed,
                            info.PositionToTransformed, transistion.Duration));

                        SetDevices(info, false);
                    }));
                }
            });
            _repeaterThread.Start();
        }

        private void StopPattern()
        {
            StopDevices();
            if (_pattern != null)
            {
                _pattern.Stop();
                _pattern = null;
                _repeaterThread.Join();
            }
        }

        private void UpdatePattern(PatternSource patternSource)
        {
            switch (patternSource)
            {
                case PatternSource.Video:
                    StopPattern();
                    break;
                case PatternSource.None:
                    StopPattern();
                    break;
                case PatternSource.Slow:
                    StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(600));
                    break;
                case PatternSource.Fast:
                    StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(50));
                    break;
                case PatternSource.ZigZag:
                    var zigzag = new RepeatingPatternGenerator();
                    zigzag.Add(99, TimeSpan.FromMilliseconds(200));
                    zigzag.Add(50, TimeSpan.FromMilliseconds(200));
                    zigzag.Add(99, TimeSpan.FromMilliseconds(200));
                    zigzag.Add(0, TimeSpan.FromMilliseconds(200));
                    zigzag.Add(50, TimeSpan.FromMilliseconds(200));
                    zigzag.Add(0, TimeSpan.FromMilliseconds(200));
                    StartPattern(zigzag);
                    break;
                case PatternSource.Random:
                    StartPattern(new RandomPatternGenerator());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(patternSource), patternSource, null);
            }
        }

        private void InitializeCommands()
        {
            OpenScriptCommand = new RelayCommand(OpenScript);
            OpenVideoCommand = new RelayCommand(OpenVideo);
            AddScriptsToPlaylistCommand = new RelayCommand(AddScriptsToPlaylist);
            ConnectLaunchDirectlyCommand = new RelayCommand(ConnectLaunchDirectly);
            ConnectButtplugCommand = new RelayCommand(ConnectButtplug);
            StartScanningButtplugCommand = new RelayCommand(StartScanningButtplug);
            SkipToNextEventCommand = new RelayCommand(SkipToNextEvent);
            TogglePlaybackCommand = new RelayCommand(TogglePlayback);
            VolumeUpCommand = new RelayCommand(VolumeUp);
            VolumeDownCommand = new RelayCommand(VolumeDown);
            ExecuteSelectedTestPatternCommand =
                new RelayCommand(ExecuteSelectedTestPattern, CanExecuteSelectedTestPattern);
        }

        private bool CanExecuteSelectedTestPattern()
        {
            return SelectedTestPattern != null;
        }

        private void InitializeTestPatterns()
        {
            TestPatterns = new List<TestPatternDefinition>
            {
                new TestPatternDefinition
                {
                    Name = "Go to 0",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[] {0}
                },
                new TestPatternDefinition
                {
                    Name = "Go to 99",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[] {99}
                },
                new TestPatternDefinition
                {
                    Name = "SawTooth Up",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[] {0, 10, 0, 20, 0, 30, 0, 40, 0, 50, 0, 60, 0, 70, 0, 80, 0, 90, 0, 99}
                },
                new TestPatternDefinition
                {
                    Name = "SawTooth Down",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[]
                        {99, 90, 99, 80, 99, 70, 99, 60, 99, 50, 99, 40, 99, 30, 99, 20, 99, 10, 99, 0}
                },
                new TestPatternDefinition
                {
                    Name = "Stairs Up",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[] {0, 20, 10, 30, 20, 40, 30, 50, 40, 60, 50, 70, 60, 80, 70, 90, 80, 99, 90}
                },
                new TestPatternDefinition
                {
                    Name = "Stairs Down",
                    Duration = TimeSpan.FromMilliseconds(200),
                    Positions = new byte[] {99, 80, 90, 70, 80, 60, 70, 50, 60, 40, 50, 30, 40, 20, 30, 10, 20, 0}
                }
            };

            SelectedTestPattern = TestPatterns.FirstOrDefault();
        }

        private async void TestPattern(TestPatternDefinition pattern)
        {
            TimeSpan delay = pattern.Duration;
            byte[] positions = pattern.Positions;

            SetDevices(
                new DeviceCommandInformation
                {
                    Duration = delay,
                    SpeedTransformed = 20,
                    SpeedOriginal = 20,
                    PositionFromOriginal = 0,
                    PositionFromTransformed = 0,
                    PositionToOriginal = positions[0],
                    PositionToTransformed = TransformPosition(positions[0], 0, 99, DateTime.Now.TimeOfDay.TotalSeconds)
                }
                , false);
            await Task.Delay(300);

            for (int i = 1; i < positions.Length; i++)
            {
                DeviceCommandInformation info = new DeviceCommandInformation
                {
                    Duration = delay,
                    PositionFromOriginal = positions[i - 1],
                    PositionToOriginal = positions[i],
                    PositionFromTransformed = TransformPosition(positions[i - 1], 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                    PositionToTransformed = TransformPosition(positions[i], 0, 99, DateTime.Now.TimeOfDay.TotalSeconds + delay.TotalSeconds)
                };

                info.SpeedOriginal = SpeedPredictor.Predict2(info.PositionFromOriginal, info.PositionToOriginal, delay);
                info.SpeedTransformed =
                    ClampSpeed(SpeedPredictor.Predict2(info.PositionFromTransformed, info.PositionToTransformed,
                        delay));

                SetDevices(info, false);

                if (i + 1 < positions.Length)
                    await Task.Delay(delay);
            }
        }


        private void LaunchConnectOnDeviceFound(object sender, Launch device)
        {
            _launch = device;
            _launch.Disconnected += LaunchOnDisconnected;

            OnRequestOverlay("Launch Connected", TimeSpan.FromSeconds(8), "Launch");
        }

        private void LaunchOnDisconnected(object sender, Exception exception)
        {
            _launch.Disconnected -= LaunchOnDisconnected;
            _launch = null;

            OnRequestOverlay("Launch Disconnected", TimeSpan.FromSeconds(8), "Launch");
        }

        private void PlaylistOnPlayEntry(object sender, PlaylistEntry playlistEntry)
        {
            LoadScript(playlistEntry.Fullname, true);

            Play();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnRequestOverlay(string text, TimeSpan duration, string designation = null)
        {
            RequestOverlay?.Invoke(this, text, duration, designation ?? "");
        }

        public void Seek(TimeSpan absolute, int downmoveup)
        {
            switch (downmoveup)
            {
                case 0:
                    _wasPlaying = TimeSource.IsPlaying;
                    TimeSource.Pause();
                    TimeSource.SetPosition(absolute);
                    ShowPosition();
                    break;
                case 1:
                    TimeSource.SetPosition(absolute);
                    ShowPosition();
                    break;
                case 2:
                    TimeSource.SetPosition(absolute);
                    if (_wasPlaying)
                        TimeSource.Play();
                    ShowPosition();
                    break;
            }
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
            if (string.IsNullOrWhiteSpace(extension))
                return;

            extension = extension.TrimStart('.').ToLower();

            if (_supportedVideoExtensions.Contains(extension))
                LoadVideo(fileToLoad, true);
            else if (_supportedScriptExtensions.Contains(extension))
                LoadScript(fileToLoad, true);
        }

        private bool LoadScript(ScriptLoader[] loaders, string fileName)
        {
            List<ScriptAction> actions = null;

            foreach (ScriptLoader loader in loaders)
            {
                try
                {
                    actions = loader.Load(fileName);
                    if (actions == null)
                        continue;
                    if (actions.Count == 0)
                        continue;

                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Loader {0} failed to open {1}: {2}", loader.GetType().Name, Path.GetFileName(fileName), e.Message);
                }
            }

            if (actions == null) return false;

            _scriptHandler.SetScript(actions);
            RefreshManualDuration();
            OpenedScript = fileName;

            FindMaxPositions();
            UpdateHeatMap();

            return true;
        }

        private void FindMaxPositions()
        {
            IEnumerable<ScriptAction> actions = _scriptHandler.GetScript();

            byte minPos = 99;
            byte maxPos = 0;

            foreach (ScriptAction action in actions)
                if (action is FunScriptAction funscript)
                {
                    byte position = funscript.Position;
                    minPos = Math.Min(minPos, position);
                    maxPos = Math.Max(maxPos, position);
                }
                else if (action is RawScriptAction rawscript)
                {
                    byte position = rawscript.Position;
                    minPos = Math.Min(minPos, position);
                    maxPos = Math.Max(maxPos, position);
                }

            _minScriptPosition = minPos;
            _maxScriptPosition = maxPos;
        }

        private void UpdateHeatMap()
        {
            if (TimeSource == null)
                return;

            List<TimeSpan> timeStamps = _scriptHandler.GetScript().Select(s => s.TimeStamp).ToList();
            Brush heatmap = HeatMapGenerator.Generate2(timeStamps, TimeSpan.Zero, TimeSource.Duration);
            HeatMap = heatmap;
        }


        private void InitializeScriptHandler()
        {
            _scriptHandler = new ScriptHandler();
            _scriptHandler.ScriptActionRaised += ScriptHandlerOnScriptActionRaised;
            _scriptHandler.IntermediateScriptActionRaised += ScriptHandlerOnIntermediateScriptActionRaised;
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(0);
        }

        private void ScriptHandlerOnIntermediateScriptActionRaised(object sender, IntermediateScriptActionEventArgs eventArgs)
        {
            if (PatternSource != PatternSource.Video)
                return;

            if (eventArgs.RawPreviousAction is FunScriptAction)
                HandleIntermediateFunScriptAction(eventArgs.Cast<FunScriptAction>());
        }

        private void HandleIntermediateFunScriptAction(IntermediateScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.PreviousAction.TimeStamp;
            byte currentPositionTransformed = TransformPosition(eventArgs.PreviousAction.Position, eventArgs.PreviousAction.TimeStamp);
            byte nextPositionTransformed = TransformPosition(eventArgs.NextAction.Position, eventArgs.NextAction.TimeStamp);

            if (currentPositionTransformed == nextPositionTransformed) return;

            byte speedOriginal =
                SpeedPredictor.Predict(
                    (byte)Math.Abs(eventArgs.PreviousAction.Position - eventArgs.NextAction.Position), duration);
            byte speedTransformed =
                SpeedPredictor.Predict((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed), duration);
            speedTransformed = ClampSpeed(speedTransformed * SpeedMultiplier);

            //Debug.WriteLine($"{nextPositionTransformed} @ {speedTransformed}");

            DeviceCommandInformation info = new DeviceCommandInformation
            {
                Duration = duration,
                SpeedTransformed = speedTransformed,
                SpeedOriginal = speedOriginal,
                PositionFromTransformed = currentPositionTransformed,
                PositionToTransformed = nextPositionTransformed,
                PositionFromOriginal = eventArgs.PreviousAction.Position,
                PositionToOriginal = eventArgs.NextAction.Position
            };

            IntermediateCommandInformation intermediateInfo = new IntermediateCommandInformation
            {
                DeviceInformation = info,
                Progress = eventArgs.Progress
            };

            SetDevices(intermediateInfo);
        }

        private void ScriptHandlerOnScriptActionRaised(object sender, ScriptActionEventArgs eventArgs)
        {
            if (PatternSource != PatternSource.Video)
                return;

            if (eventArgs.RawCurrentAction is FunScriptAction)
                HandleFunScriptAction(eventArgs.Cast<FunScriptAction>());
        }

        private void HandleFunScriptAction(ScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            if (eventArgs.NextAction == null)
            {
                if (AutoSkip)
                    Playlist.PlayNextEntryCommand.Execute(OpenedScript);
                else
                    OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                return;
            }

            TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
            byte currentPositionTransformed = TransformPosition(eventArgs.CurrentAction.Position, eventArgs.CurrentAction.TimeStamp);
            byte nextPositionTransformed = TransformPosition(eventArgs.NextAction.Position, eventArgs.NextAction.TimeStamp);

            if (currentPositionTransformed != nextPositionTransformed)
            {

                byte speedOriginal =
                    SpeedPredictor.Predict(
                        (byte)Math.Abs(eventArgs.CurrentAction.Position - eventArgs.NextAction.Position), duration);
                byte speedTransformed =
                    SpeedPredictor.Predict((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed),
                        duration);
                speedTransformed = ClampSpeed(speedTransformed * SpeedMultiplier);

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

                SetDevices(info);
            }

            if (duration > TimeSpan.FromSeconds(10) && TimeSource.IsPlaying)
            {
                if (AutoSkip)
                    SkipToNextEvent();
                else
                    OnRequestOverlay($"Next event in {duration.TotalSeconds:f0}s", TimeSpan.FromSeconds(4), "Events");
            }
        }

        private byte ClampSpeed(double speed)
        {
            return (byte)Math.Min(MaxSpeed, Math.Max(MinSpeed, speed));
        }

        private byte TransformPosition(byte pos, byte inMin, byte inMax, double timestamp)
        {
            double relative = (double)(pos - inMin) / (inMax - inMin);
            relative = Math.Min(1, Math.Max(0, relative));

            byte minPosition = MinPosition;
            byte maxPosition = MaxPosition;

            const double secondsPercycle = 10.0;
            double cycle = timestamp / secondsPercycle;
            double range = FilterRange;

            switch (FilterMode)
            {
                case PositionFilterMode.FullRange:
                    break;
                case PositionFilterMode.Top:
                    GetRange(ref minPosition, ref maxPosition, range, 1.0);
                    break;
                case PositionFilterMode.Middle:
                    GetRange(ref minPosition, ref maxPosition, range, 0.5);
                    break;
                case PositionFilterMode.Bottom:
                    GetRange(ref minPosition, ref maxPosition, range, 0);
                    break;
                case PositionFilterMode.SineWave:
                    {
                        double factor = (1 + Math.Sin(cycle * Math.PI * 2.0)) / 2.0;
                        GetRange(ref minPosition, ref maxPosition, range, factor);
                        break;
                    }
                case PositionFilterMode.TopBottom:
                    {
                        double progress = cycle - Math.Floor(cycle);
                        double factor = progress >= 0.5 ? 1 : 0;
                        GetRange(ref minPosition, ref maxPosition, range, factor);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            range = (byte)(maxPosition - minPosition);

            byte absolute = (byte)(minPosition + range * relative);

            return SpeedPredictor.Clamp(absolute);
        }

        private void GetRange(ref byte minPosition, ref byte maxPosition, double range, double factor)
        {
            double actualRange = (maxPosition - minPosition) * (1.0 - range);
            double newMin = minPosition + actualRange * factor;
            double newMax = maxPosition - actualRange * (1 - factor);

            minPosition = (byte)newMin;
            maxPosition = (byte)newMax;
        }

        private byte TransformPosition(byte pos, TimeSpan timeStamp)
        {
            return TransformPosition(pos, _minScriptPosition, _maxScriptPosition, timeStamp.TotalSeconds);
        }

        private void SetDevices(DeviceCommandInformation information, bool requirePlaying = true)
        {
            if (TimeSource.IsPlaying || !requirePlaying)
            {
                _launch?.EnqueuePosition(information.PositionToTransformed, information.SpeedTransformed);
                _connector?.Set(information);
            }
        }

        private void SetDevices(IntermediateCommandInformation intermediateInfo, bool requirePlaying = true)
        {
            if (TimeSource.IsPlaying || !requirePlaying)
            {
                _connector?.Set(intermediateInfo);
            }
        }

        private void StopDevices()
        {
            _connector?.Stop();
        }

        protected virtual string OnRequestFile(string filter, ref int filterIndex)
        {
            RequestFileEventArgs e = new RequestFileEventArgs
            {
                Filter = filter,
                FilterIndex = filterIndex,
                Handled = false,
                MultiSelect = false
            };

            RequestFile?.Invoke(this, e);

            if (e.Handled)
            {
                filterIndex = e.FilterIndex;
                return e.SelectedFile;
            }

            return null;
        }

        protected virtual string[] OnRequestFiles(string filter, ref int filterIndex)
        {
            RequestFileEventArgs e = new RequestFileEventArgs
            {
                Filter = filter,
                FilterIndex = filterIndex,
                Handled = false,
                MultiSelect = true
            };

            RequestFile?.Invoke(this, e);

            if (e.Handled)
            {
                filterIndex = e.FilterIndex;
                return e.SelectedFiles;
            }

            return null;
        }

        private void VideoPlayer_MediaEnded(object sender, EventArgs e)
        {
            StopDevices();
            PlayNextPlaylistEntry();
        }

        private void PlayNextPlaylistEntry()
        {
            Playlist.PlayNextEntryCommand.Execute(OpenedScript);
        }

        private void PlayPreviousPlaylistEntry()
        {
            Playlist.PlayPreviousEntryCommand.Execute(OpenedScript);
        }

        private void VideoPlayer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (LogMarkers != true)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(_openVideo))
                    return;

                TimeSpan position = TimeSource.Progress;
                string logFile = Path.ChangeExtension(_openVideo, ".log");
                if (logFile == null)
                    return;

                string line = position.ToString("hh\\:mm\\:ss\\.fff");
                File.AppendAllLines(logFile, new[] { line });
                OnRequestOverlay("Logged marker at " + line, TimeSpan.FromSeconds(5), "Log");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void SkipToNextEvent()
        {
            TimeSpan currentPosition = TimeSource.Progress;
            ScriptAction nextAction = _scriptHandler.FirstEventAfter(currentPosition - _scriptHandler.Delay);
            if (nextAction == null)
            {
                OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                return;
            }

            TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(3);
            TimeSpan duration = skipTo - currentPosition;

            if (skipTo < currentPosition)
                return;

            TimeSource.SetPosition(skipTo);
            ShowPosition($"Skipped {duration.TotalSeconds:f0}s - ");
        }

        private void VideoPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (AutoSkip)
                SkipToNextEvent();

            UpdateHeatMap();
        }

        protected virtual MessageBoxResult OnRequestMessageBox(string text, string title, MessageBoxButton buttons,
            MessageBoxImage icon = MessageBoxImage.None)
        {
            MessageBoxEventArgs e = new MessageBoxEventArgs
            {
                Buttons = buttons,
                Handled = false,
                Icon = icon,
                Result = MessageBoxResult.None,
                Text = text,
                Title = title
            };

            RequestMessageBox?.Invoke(this, e);
            if (e.Handled)
                return e.Result;

            return MessageBoxResult.None;
        }

        public void AddScriptsToPlaylist()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string[] files = OnRequestFiles(formats.BuildFilter(true), ref _lastScriptFilterIndex);
            if (files == null)
                return;

            foreach (string filename in files)
                Playlist.AddEntry(new PlaylistEntry(filename));
        }

        public void OpenScript()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string file = OnRequestFile(formats.BuildFilter(true), ref _lastScriptFilterIndex);
            if (file == null)
                return;

            ScriptFileFormat[] format = formats.GetFormats(_lastScriptFilterIndex - 1, file);

            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(format);

            if (!LoadScript(loaders, file))
            {
                ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
                if (!LoadScript(otherLoaders, file))
                {
                    OnRequestMessageBox($"The script file '{file}' could not be loaded!", "Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
            }

            TryFindMatchingVideo(file);

            UpdateHeatMap();
        }

        //TODO: Maybe merge some code with OpenScript()?
        private void LoadScript(string file, bool checkForVideo)
        {
            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(file);
            if (loaders == null)
                return;

            if (!LoadScript(loaders, file))
            {
                ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
                if (!LoadScript(otherLoaders, file))
                {
                    OnRequestMessageBox($"The script file '{file}' could not be loaded!", "Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                }
            }

            OnRequestOverlay($"Loaded {Path.GetFileName(file)}", TimeSpan.FromSeconds(4), "ScriptLoaded");

            if (checkForVideo)
                TryFindMatchingVideo(file);
        }

        public void ConnectLaunchDirectly()
        {
            _launchConnect.Start();
        }

        public void VolumeDown()
        {
            int oldVolume = (int)Math.Round(Volume);
            int newVolume = (int)(5.0 * Math.Floor(Volume / 5.0));
            if (oldVolume == newVolume)
                newVolume -= 5;

            newVolume = Math.Min(100, Math.Max(0, newVolume));

            Volume = newVolume;
        }

        public void VolumeUp()
        {
            int oldVolume = (int)Math.Round(Volume);
            int newVolume = (int)(5.0 * Math.Ceiling(Volume / 5.0));
            if (oldVolume == newVolume)
                newVolume += 5;

            newVolume = Math.Min(100, Math.Max(0, newVolume));

            Volume = newVolume;
        }

        public void ShiftPosition(TimeSpan timeSpan)
        {
            TimeSource.SetPosition(TimeSource.Progress + timeSpan);
            ShowPosition();
        }

        private void ShowPosition(string prefix = "")
        {
            OnRequestOverlay($@"{prefix}{TimeSource.Progress:h\:mm\:ss} / {TimeSource.Duration:h\:mm\:ss}",
                TimeSpan.FromSeconds(3), "Position");
        }

        public void StartScanningButtplug()
        {
            _connector?.StartScanning();
        }

        public async void ConnectButtplug()
        {
            if (_connector != null)
                await _connector?.Disconnect();

            string url = OnRequestButtplugUrl(ButtplugAdapter.DefaultUrl);
            if (url == null)
                return;

            _connector = new ButtplugAdapter(url);
            _connector.DeviceAdded += ConnectorOnDeviceAdded;

            bool success = await _connector.Connect();

            if (success)
            {
                OnRequestOverlay("Connected to Buttplug", TimeSpan.FromSeconds(6), "Buttplug");
            }
            else
            {
                _connector = null;
                OnRequestOverlay("Could not connect to Buttplug", TimeSpan.FromSeconds(6), "Buttplug");
            }
        }

        private void ConnectorOnDeviceAdded(object sender, string deviceName)
        {
            OnRequestOverlay("Device found: " + deviceName, TimeSpan.FromSeconds(5), "Buttplug");
        }

        protected virtual string OnRequestButtplugUrl(string defaultValue)
        {
            ButtplugUrlRequestEventArgs e = new ButtplugUrlRequestEventArgs
            {
                Url = defaultValue
            };

            RequestButtplugUrl?.Invoke(this, e);

            if (e.Handled)
                return e.Url;
            return null;
        }

        public void ExecuteSelectedTestPattern()
        {
            TestPattern(SelectedTestPattern);
        }

        public void Unload()
        {
            SaveSettings();
        }
    }
}