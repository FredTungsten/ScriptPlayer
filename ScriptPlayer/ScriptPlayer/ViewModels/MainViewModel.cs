using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Microsoft.Win32;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Scripts;
using Application = System.Windows.Application;

namespace ScriptPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public delegate void RequestOverlayEventHandler(object sender, string text, TimeSpan duration,
            string designation);

        public event EventHandler<RequestEventArgs<VlcConnectionSettings>> RequestVlcConnectionSettings;
        public event EventHandler<RequestEventArgs<WhirligigConnectionSettings>> RequestWhirligigConnectionSettings;

        private readonly string[] _supportedScriptExtensions;

        private readonly string[] _supportedVideoExtensions =
            {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf"};

        private bool _autoSkip;

        private string _buttplugApiVersion = "Unknown";
        private TimeSpan _commandDelay = TimeSpan.FromMilliseconds(166);

        private ConversionMode _conversionMode = ConversionMode.UpOrDown;

        private List<ConversionMode> _conversionModes;

        private Brush _heatMap;

        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;

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
        private PlaybackMode _playbackMode;
        private TimeSource _timeSource;
        private bool _showHeatMap;
        private PositionFilterMode _filterMode = PositionFilterMode.FullRange;
        private double _filterRange = 0.5;
        private List<Range> _filterRanges;
        private PositionCollection _positions;
        private bool _showScriptPositions;
        private TimeSpan _positionsViewport = TimeSpan.FromSeconds(5);
        private bool _showTimeLeft;
        private bool _displayEventNotifications;

        private readonly List<DeviceController> _controllers = new List<DeviceController>();
        private readonly ObservableCollection<Device> _devices = new ObservableCollection<Device>();
        private bool _showBanner = true;
        private string _scriptPlayerVersion;
        private bool _blurVideo;

        private bool _canDirectConnectLaunch = false;

        public ObservableCollection<Device> Devices => _devices;

        public bool ShowTimeLeft
        {
            get { return _showTimeLeft; }
            set
            {
                if (value == _showTimeLeft) return;
                _showTimeLeft = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan PositionsViewport
        {
            get { return _positionsViewport; }
            set
            {
                if (value.Equals(_positionsViewport)) return;
                _positionsViewport = value;

                if (_positionsViewport > TimeSpan.FromSeconds(20))
                    _positionsViewport = TimeSpan.FromSeconds(20);

                if (_positionsViewport < TimeSpan.FromSeconds(2))
                    _positionsViewport = TimeSpan.FromSeconds(2);

                OnPropertyChanged();
            }
        }

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

        public Boolean CanDirectConnectLaunch
        {
            get { return _canDirectConnectLaunch; }
        }

        public MainViewModel()
        {
            ButtplugApiVersion = ButtplugAdapter.GetButtplugApiVersion();
            ScriptPlayerVersion = GetScriptPlayerVersion();
            int _releaseId = 0;
            try
            {
                _releaseId = int.Parse(Registry
                    .GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ReleaseId", string.Empty)
                    .ToString());
            }
            catch (Exception)
            {
                // If we can't retreive a version, just skip the perm check entirely and don't allow Bluetooth usage.
                Debug.WriteLine("Can't get version!");
            }

            LoadPlaylist();

            ConversionModes = Enum.GetValues(typeof(ConversionMode)).Cast<ConversionMode>().ToList();
            _supportedScriptExtensions = ScriptLoaderManager.GetSupportedExtensions();

            InitializeCommands();
            InitializeTestPatterns();
            if (_releaseId >= 1703)
            {
                // Only initialize LaunchFinder on Win10 15063+
                InitializeLaunchFinder();
            }

            InitializeScriptHandler();

            LoadSettings();
            
        }

        private string GetScriptPlayerVersion()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(location))
                return "?";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(location);
            return fileVersionInfo.ProductVersion;
        }

        public string ScriptPlayerVersion
        {
            get { return _scriptPlayerVersion; }
            set
            {
                if (value == _scriptPlayerVersion) return;
                _scriptPlayerVersion = value;
                OnPropertyChanged();
            }
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
                DisplayEventNotifications = settings.DisplayEventNotifications;
                ConversionMode = settings.ConversionMode;
                ShowHeatMap = settings.ShowHeatMap;
                LogMarkers = settings.LogMarkers;
                FilterMode = settings.FilterMode;
                FilterRange = settings.FilterRange;
                ShowScriptPositions = settings.ShowScriptPositions;
            }
        }

        private void LoadPlaylist(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = GetDefaultPlaylistFile();

            M3uPlaylist playlist = M3uPlaylist.FromFile(filename);

            if (Playlist == null)
            {
                Playlist = new PlaylistViewModel();
                Playlist.PlayEntry += PlaylistOnPlayEntry;
            }
            else
            {
                Playlist.Clear();
            }

            if (playlist == null) return;

            foreach(string entry in playlist.Entries)
                Playlist.AddEntry(new PlaylistEntry(entry));
        }

        private void SavePlaylist(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = GetDefaultPlaylistFile();

            M3uPlaylist playlist = new M3uPlaylist();
            playlist.Entries.AddRange(Playlist.Entries.Select(e => e.Fullname));
            playlist.Save(filename);
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
            settings.DisplayEventNotifications = DisplayEventNotifications;
            settings.ConversionMode = ConversionMode;
            settings.ShowHeatMap = ShowHeatMap;
            settings.LogMarkers = LogMarkers;
            settings.ScriptDelay = ScriptDelay.TotalMilliseconds;
            settings.CommandDelay = CommandDelay.TotalMilliseconds;
            settings.FilterMode = FilterMode;
            settings.FilterRange = FilterRange;
            settings.ShowScriptPositions = ShowScriptPositions;

            settings.Save(GetSettingsFile());
        }

        private string GetSettingsFile()
        {
            return Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\Settings.xml");
        }

        private string GetDefaultPlaylistFile()
        {
            return Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\Playlist.m3u");
        }

        private void PlaybackModeChanged(PlaybackMode oldValue, PlaybackMode newValue)
        {
            try
            {
                DisposeTimeSource();
                ClearScript();

                Title = "";
                OpenedScript = null;
                _openVideo = null;

                switch (newValue)
                {
                    case PlaybackMode.Local:
                    {
                        TimeSource = VideoPlayer.TimeSource;
                        break;
                    }
                    case PlaybackMode.Blind:
                    {
                        HideBanner();
                        TimeSource = new ManualTimeSource(
                            new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                TimeSpan.FromMilliseconds(10)));

                        RefreshManualDuration();
                        break;
                    }
                    case PlaybackMode.Whirligig:
                    {
                        HideBanner();

                        WhirligigConnectionSettings settings =
                            OnRequestWhirligigConnectionSettings(new WhirligigConnectionSettings
                            {
                                IpAndPort = "127.0.0.1:2000"
                            });

                        if (settings == null)
                        {
                            PlaybackMode = PlaybackMode.Local;
                            return;
                        }

                        TimeSource = new WhirligigTimeSource(new DispatcherClock(
                            Dispatcher.FromThread(Thread.CurrentThread),
                            TimeSpan.FromMilliseconds(10)), settings);

                        ((WhirligigTimeSource) TimeSource).FileOpened += OnVideoFileOpened;

                        RefreshManualDuration();
                        break;
                    }
                    case PlaybackMode.Vlc:
                    {
                        HideBanner();

                        VlcConnectionSettings settings = OnRequestVlcConnectionSettings(new VlcConnectionSettings
                        {
                            IpAndPort = "127.0.0.1:8080",
                            Password = "test"
                        });

                        if (settings == null)
                        {
                            PlaybackMode = PlaybackMode.Local;
                            return;
                        }

                        TimeSource = new VlcTimeSource(
                            new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                TimeSpan.FromMilliseconds(10)),
                            settings);

                        ((VlcTimeSource) TimeSource).FileOpened += OnVideoFileOpened;

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private void ClearScript()
        {
            _scriptHandler.Clear();
        }

        private VlcConnectionSettings OnRequestVlcConnectionSettings(VlcConnectionSettings currentSettings)
        {
            RequestEventArgs<VlcConnectionSettings> args = new RequestEventArgs<VlcConnectionSettings>(currentSettings);
            RequestVlcConnectionSettings?.Invoke(this, args);

            if (!args.Handled)
                return null;
            return args.Value;
        }

        private WhirligigConnectionSettings OnRequestWhirligigConnectionSettings(WhirligigConnectionSettings currentSettings)
        {
            RequestEventArgs<WhirligigConnectionSettings> args = new RequestEventArgs<WhirligigConnectionSettings>(currentSettings);
            RequestWhirligigConnectionSettings?.Invoke(this, args);

            if (!args.Handled)
                return null;
            return args.Value;
        }

        private void HideBanner()
        {
            ShowBanner = false;
        }

        private void DisposeTimeSource()
        {
            TimeSource?.Pause();

            if (TimeSource is IDisposable disposable)
                disposable.Dispose();
        }

        private void OnVideoFileOpened(object sender, string videoFileName)
        {
            SilentlyFindMatchingScript(videoFileName);
        }

        private void SilentlyFindMatchingScript(string videoFileName)
        {
            if (IsMatchingScriptLoaded(videoFileName))
            {
                OnRequestOverlay("Matching script alreadly loaded", TimeSpan.FromSeconds(6));
                return;
            }

            string scriptFile = FindFile(videoFileName, ScriptLoaderManager.GetSupportedExtensions());
            if (string.IsNullOrWhiteSpace(scriptFile))
            {
                OnRequestOverlay($"No script for '{Path.GetFileName(videoFileName)}' found!", TimeSpan.FromSeconds(6));
                return;
            }

            LoadScript(scriptFile, false);
        }

        private void RefreshManualDuration()
        {
            if (TimeSource is ManualTimeSource source)
                source.SetDuration(_scriptHandler.GetDuration().Add(TimeSpan.FromSeconds(5)));

            if (TimeSource is WhirligigTimeSource whirli)
                whirli.SetDuration(_scriptHandler.GetDuration().Add(TimeSpan.FromSeconds(5)));
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
                    switch (PlaybackMode)
                    {
                        case PlaybackMode.Local:
                            {
                                PlayNextPlaylistEntry();
                                break;
                            }
                        case PlaybackMode.Blind:
                            {
                                TimeSource.SetPosition(TimeSource.Progress + TimeSpan.FromMilliseconds(50));
                                break;
                            }
                        case PlaybackMode.Whirligig:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Keys.MediaPreviousTrack:
                    switch (PlaybackMode)
                    {
                        case PlaybackMode.Local:
                            {
                                PlayPreviousPlaylistEntry();
                                break;
                            }
                        case PlaybackMode.Blind:
                            {
                                TimeSource.SetPosition(TimeSource.Progress - TimeSpan.FromMilliseconds(50));
                                break;
                            }
                        case PlaybackMode.Whirligig:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Keys.MediaStop:
                    Pause();
                    TimeSource.SetPosition(TimeSpan.Zero);
                    break;
            }
        }

        public PositionCollection Positions
        {
            get { return _positions; }
            set
            {
                if (Equals(value, _positions)) return;
                _positions = value;
                OnPropertyChanged();
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

        public bool BlurVideo
        {
            get { return _blurVideo; }
            set
            {
                if (value == _blurVideo) return;
                _blurVideo = value;
                OnPropertyChanged();
            }
        }

        public PlaybackMode PlaybackMode
        {
            get => _playbackMode;
            set
            {
                if (value == _playbackMode) return;
                PlaybackMode oldValue = _playbackMode;
                _playbackMode = value;
                PlaybackModeChanged(oldValue, _playbackMode);
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

        public bool ShowBanner
        {
            get => _showBanner;
            set
            {
                if (value == _showBanner) return;
                _showBanner = value;
                OnPropertyChanged();
            }
        }

        public bool ShowScriptPositions
        {
            get => _showScriptPositions;
            set
            {
                if (value == _showScriptPositions) return;
                _showScriptPositions = value;
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

        public RelayCommand ToggleFullScreenCommand { get; set; }

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

        public bool DisplayEventNotifications
        {
            get { return _displayEventNotifications; }
            set
            {
                if (value == _displayEventNotifications) return;
                _displayEventNotifications = value;
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

            StopPattern();

            foreach (DeviceController controller in _controllers)
            {
                if (controller is IDisposable disposable)
                    disposable.Dispose();
            }
            _controllers.Clear();

            foreach (Device device in _devices)
            {
                device.Dispose();
            }
            _devices.Clear();

            DisposeTimeSource();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public event RequestOverlayEventHandler RequestOverlay;
        public event EventHandler<RequestFileEventArgs> RequestFile;
        public event EventHandler<MessageBoxEventArgs> RequestMessageBox;
        public event EventHandler<ButtplugUrlRequestEventArgs> RequestButtplugUrl;
        public event EventHandler RequestToggleFullscreen;

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
            foreach (Device device in _devices)
                device.MinDelayBetweenCommands = CommandDelay;
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

        private void LoadVideo(string videoFileName, bool checkForScript)
        {
            if (checkForScript)
                TryFindMatchingScript(videoFileName);

            _openVideo = videoFileName;

            if (PlaybackMode == PlaybackMode.Local)
            {
                HideBanner();
                VideoPlayer.Open(videoFileName);
            }

            Title = Path.GetFileNameWithoutExtension(videoFileName);

            OnRequestOverlay($"Loaded {Path.GetFileName(videoFileName)}", TimeSpan.FromSeconds(4), "VideoLoaded");

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
            if (PlaybackMode != PlaybackMode.Local)
                return !string.IsNullOrWhiteSpace(_openedScript);

            return !string.IsNullOrWhiteSpace(_openVideo);
        }

        public void TogglePlayback()
        {
            if (!TimeSource.CanPlayPause)
                return;

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

        private void TryFindMatchingScript(string videoFileName)
        {
            if (IsMatchingScriptLoaded(videoFileName))
                return;

            string scriptFile = FindFile(videoFileName, ScriptLoaderManager.GetSupportedExtensions());
            if (!string.IsNullOrWhiteSpace(scriptFile))
            {
                /*string nameOnly = Path.GetFileName(scriptFile);
                if (OnRequestMessageBox($"Do you want to also load '{nameOnly}'?", "Also load Script?",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;*/

                LoadScript(scriptFile, false);
            }
        }

        private void TryFindMatchingVideo(string scriptFileName)
        {
            if (IsMatchingVideoLoaded(scriptFileName))
                return;

            string videoFile = FindFile(scriptFileName, _supportedVideoExtensions);
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

        private bool IsMatchingScriptLoaded(string videoFileName)
        {
            if (string.IsNullOrWhiteSpace(OpenedScript))
                return false;

            if (string.IsNullOrWhiteSpace(videoFileName))
                return false;

            if (Path.GetDirectoryName(OpenedScript) != Path.GetDirectoryName(videoFileName))
                return false;

            return Path.GetFileNameWithoutExtension(OpenedScript).Equals(Path.GetFileNameWithoutExtension(videoFileName));
        }

        private bool IsMatchingVideoLoaded(string scriptFileName)
        {
            if (string.IsNullOrWhiteSpace(scriptFileName))
                return false;

            if (string.IsNullOrWhiteSpace(_openVideo))
                return false;

            if (Path.GetDirectoryName(scriptFileName) != Path.GetDirectoryName(_openVideo))
                return false;

            return Path.GetFileNameWithoutExtension(scriptFileName).Equals(Path.GetFileNameWithoutExtension(_openVideo));
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

                        info.SpeedOriginal = SpeedPredictor.PredictSpeed2(info.PositionFromOriginal, info.PositionToOriginal,
                            transistion.Duration);
                        info.SpeedTransformed = ClampSpeed(SpeedPredictor.PredictSpeed2(info.PositionFromTransformed,
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
                case PatternSource.Medium:
                    StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(250));
                    break;
                case PatternSource.Fast:
                    StartRegularPattern(0, 99, TimeSpan.FromMilliseconds(100));
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
            AddScriptsToPlaylistCommand = new RelayCommand(AddToPlaylist);
            ConnectLaunchDirectlyCommand = new RelayCommand(ConnectLaunchDirectly);
            ConnectButtplugCommand = new RelayCommand(ConnectButtplug);
            StartScanningButtplugCommand = new RelayCommand(StartScanningButtplug);
            SkipToNextEventCommand = new RelayCommand(SkipToNextEvent, CanSkipToNextEvent);
            TogglePlaybackCommand = new RelayCommand(TogglePlayback, CanTogglePlayback);
            VolumeUpCommand = new RelayCommand(VolumeUp);
            VolumeDownCommand = new RelayCommand(VolumeDown);
            ExecuteSelectedTestPatternCommand = new RelayCommand(ExecuteSelectedTestPattern, CanExecuteSelectedTestPattern);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);
        }

        private bool CanSkipToNextEvent()
        {
            if (TimeSource == null) return false;
            return TimeSource.CanSeek;
        }

        private bool CanTogglePlayback()
        {
            if (TimeSource == null) return false;
            return TimeSource.CanPlayPause;
        }

        private void ExecuteToggleFullScreen()
        {
            OnRequestToggleFullscreen();
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

                info.SpeedOriginal = SpeedPredictor.PredictSpeed2(info.PositionFromOriginal, info.PositionToOriginal, delay);
                info.SpeedTransformed =
                    ClampSpeed(SpeedPredictor.PredictSpeed2(info.PositionFromTransformed, info.PositionToTransformed,
                        delay));

                SetDevices(info, false);

                if (i + 1 < positions.Length)
                    await Task.Delay(delay);
            }
        }

        private void DevicecController_DeviceRemoved(object sender, Device device)
        {
            _devices.Remove(device);

            OnRequestOverlay("Device Removed: " + device.Name, TimeSpan.FromSeconds(8));
        }

        private void DeviceController_DeviceFound(object sender, Device device)
        {
            _devices.Add(device);
            device.IsEnabled = true;
            //TODO Handle Disconnect
            //_launch.Disconnected += LaunchOnDisconnected;

            OnRequestOverlay("Device Connected: " + device.Name, TimeSpan.FromSeconds(8));
        }

        /*private void LaunchOnDisconnected(object sender, Exception exception)
        {
            _launch.Disconnected -= LaunchOnDisconnected;
            _launch = null;

            OnRequestOverlay("Launch Disconnected", TimeSpan.FromSeconds(8), "Launch");
        }*/

        private void PlaylistOnPlayEntry(object sender, PlaylistEntry playlistEntry)
        {
            if (!TimeSource.CanOpenMedia) return;
            LoadFile(playlistEntry.Fullname);
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
            LaunchBluetooth launchController = _controllers.OfType<LaunchBluetooth>().FirstOrDefault();
            if (launchController != null) return;

            launchController = new LaunchBluetooth();
            launchController.DeviceFound += DeviceController_DeviceFound;
            _controllers.Add(launchController);
            _canDirectConnectLaunch = true;
        }

        private void CheckForArguments()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length <= 1) return;

            string fileToLoad = args[1];
            if (File.Exists(fileToLoad))
                LoadFile(fileToLoad);
        }

        public void LoadFile(string fileToLoad)
        {
            string extension = Path.GetExtension(fileToLoad);
            if (string.IsNullOrWhiteSpace(extension))
                return;

            extension = extension.TrimStart('.').ToLower();

            if(extension == "m3u")
                LoadPlaylist(fileToLoad);
            else if (_supportedVideoExtensions.Contains(extension))
                LoadVideo(fileToLoad, true);
            else if (_supportedScriptExtensions.Contains(extension))
                LoadScript(fileToLoad, true);
        }

        private bool LoadScript(ScriptLoader[] loaders, string fileName)
        {
            const long maxScriptSize = 4 * 1024 * 1024; //4 MB

            if (!File.Exists(fileName)) return false;
            if (new FileInfo(fileName).Length > maxScriptSize) return false;

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

                    Debug.WriteLine("Script with {0} actions successfully loaded with {1}", actions.Count,
                        loader.GetType().Name);
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

            List<TimeSpan> timeStamps = FilterDuplicates(_scriptHandler.GetScript().ToList()).Select(s => s.TimeStamp).ToList();
            Brush heatmap = HeatMapGenerator.Generate2(timeStamps, TimeSpan.Zero, TimeSource.Duration);
            HeatMap = heatmap;
        }

        private List<ScriptAction> FilterDuplicates(List<ScriptAction> timestamps)
        {
            List<ScriptAction> result = new List<ScriptAction>();

            foreach (ScriptAction action in timestamps)
            {
                if (result.Count == 0 || !result.Last().IsSameAction(action))
                    result.Add(action);
            }
            return result;
        }


        private void InitializeScriptHandler()
        {
            _scriptHandler = new ScriptHandler();
            _scriptHandler.ScriptActionRaised += ScriptHandlerOnScriptActionRaised;
            _scriptHandler.IntermediateScriptActionRaised += ScriptHandlerOnIntermediateScriptActionRaised;
            _scriptHandler.PositionsChanged += ScriptHandlerOnPositionsChanged;
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(0);
        }

        private void ScriptHandlerOnPositionsChanged(object sender, PositionCollection positionCollection)
        {
            Positions = positionCollection;
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
                SpeedPredictor.PredictSpeed(
                    (byte)Math.Abs(eventArgs.PreviousAction.Position - eventArgs.NextAction.Position), duration);
            byte speedTransformed =
                SpeedPredictor.PredictSpeed((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed), duration);
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
                else if (DisplayEventNotifications)
                    OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                return;
            }

            TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
            byte currentPositionTransformed = TransformPosition(eventArgs.CurrentAction.Position, eventArgs.CurrentAction.TimeStamp);
            byte nextPositionTransformed = TransformPosition(eventArgs.NextAction.Position, eventArgs.NextAction.TimeStamp);

            if (currentPositionTransformed != nextPositionTransformed)
            {

                byte speedOriginal =
                    SpeedPredictor.PredictSpeed(
                        (byte)Math.Abs(eventArgs.CurrentAction.Position - eventArgs.NextAction.Position), duration);
                byte speedTransformed =
                    SpeedPredictor.PredictSpeed((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed),
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
                else if (DisplayEventNotifications)
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

            return SpeedPredictor.ClampValue(absolute);
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
                foreach (Device device in _devices)
                    device.Enqueue(information);
            }
        }

        private void SetDevices(IntermediateCommandInformation intermediateInfo, bool requirePlaying = true)
        {
            if (TimeSource.IsPlaying || !requirePlaying)
            {
                foreach (Device device in _devices)
                    device.Set(intermediateInfo);
            }
        }

        private void StopDevices()
        {
            foreach (Device device in _devices)
                device.Stop();
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
            if (!TimeSource.CanOpenMedia) return;
            Playlist.PlayNextEntryCommand.Execute(OpenedScript);
        }

        private void PlayPreviousPlaylistEntry()
        {
            if (!TimeSource.CanOpenMedia) return;
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
            SkipToNextEvent(false);
        }

        public async void SkipToNextEvent(bool isInitialSkip)
        {
            TimeSpan currentPosition = TimeSource.Progress;
            ScriptAction nextAction = _scriptHandler.FirstEventAfter(currentPosition - _scriptHandler.Delay);
            if (nextAction == null)
            {
                if (DisplayEventNotifications)
                    OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                return;
            }

            TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(2);
            TimeSpan duration = skipTo - currentPosition;

            if (skipTo < currentPosition)
                return;

            if (PlaybackMode == PlaybackMode.Local)
                await VideoPlayer.SoftSeek(skipTo, isInitialSkip);
            else
                TimeSource.SetPosition(skipTo);

            if (DisplayEventNotifications)
                ShowPosition($"Skipped {duration.TotalSeconds:f0}s - ");
        }

        private void VideoPlayer_MediaOpened(object sender, EventArgs e)
        {
            if (AutoSkip)
                SkipToNextEvent(true);

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

        public void AddToPlaylist()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string videoFilters = $"Videos|{string.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"))}";
            string scriptFilters = formats.BuildFilter(true);

            string filters = videoFilters + "|" + scriptFilters;

            string[] files = OnRequestFiles(filters, ref _lastScriptFilterIndex);
            if (files == null)
                return;

            foreach (string filename in files)
                Playlist.AddEntry(new PlaylistEntry(filename));
        }

        public void OpenScript()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string scriptFileName = OnRequestFile(formats.BuildFilter(true), ref _lastScriptFilterIndex);
            if (scriptFileName == null)
                return;

            ScriptFileFormat[] format = formats.GetFormats(_lastScriptFilterIndex - 1, scriptFileName);

            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(format);

            if (!LoadScript(loaders, scriptFileName))
            {
                ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
                if (!LoadScript(otherLoaders, scriptFileName))
                {
                    OnRequestOverlay($"The script file '{scriptFileName}' could not be loaded!", TimeSpan.FromSeconds(6));
                    return;
                }
            }

            TryFindMatchingVideo(scriptFileName);

            UpdateHeatMap();
        }

        //TODO: Maybe merge some code with OpenScript()?
        private void LoadScript(string scriptFileName, bool checkForVideo)
        {
            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(scriptFileName);
            if (loaders == null)
                return;

            if (!LoadScript(loaders, scriptFileName))
            {
                ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
                if (!LoadScript(otherLoaders, scriptFileName))
                {
                    OnRequestOverlay($"The script file '{scriptFileName}' could not be loaded!", TimeSpan.FromSeconds(6));
                    return;
                }
            }

            OnRequestOverlay($"Loaded {Path.GetFileName(scriptFileName)}", TimeSpan.FromSeconds(4), "ScriptLoaded");

            Title = Path.GetFileNameWithoutExtension(scriptFileName);

            if (checkForVideo)
                TryFindMatchingVideo(scriptFileName);
        }

        public void ConnectLaunchDirectly()
        {
            LaunchBluetooth controller = _controllers.OfType<LaunchBluetooth>().Single();
            controller.Start();
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
            var controller = _controllers.OfType<ButtplugAdapter>().SingleOrDefault();
            controller?.StartScanning();
        }

        public async void ConnectButtplug()
        {
            var controller = _controllers.OfType<ButtplugAdapter>().SingleOrDefault();

            if (controller != null)
            {
                controller.DeviceFound -= DeviceController_DeviceFound;
                controller.DeviceRemoved -= DevicecController_DeviceRemoved;
                await controller.Disconnect();
            }

            _controllers.Remove(controller);

            string url = OnRequestButtplugUrl(ButtplugAdapter.DefaultUrl);
            if (url == null)
                return;

            controller = new ButtplugAdapter(url);
            controller.DeviceFound += DeviceController_DeviceFound;
            controller.DeviceRemoved += DevicecController_DeviceRemoved;

            _controllers.Add(controller);

            bool success = await controller.Connect();

            if (success)
            {
                OnRequestOverlay("Connected to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
            }
            else
            {
                _controllers.Remove(controller);
                controller.DeviceFound -= DeviceController_DeviceFound;
                controller.DeviceRemoved -= DevicecController_DeviceRemoved;
                OnRequestOverlay("Could not connect to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
            }
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
            SavePlaylist();
        }

        protected virtual void OnRequestToggleFullscreen()
        {
            RequestToggleFullscreen?.Invoke(this, EventArgs.Empty);
        }

        public void FilesDropped(string[] files)
        {
            if (files == null || files.Length == 0)
                return;

            Playlist.AddEntries(files);
            LoadFile(files[0]);
        }
    }

    public enum PlaybackMode
    {
        Local,
        Blind,
        Whirligig,
        Vlc
    }
}