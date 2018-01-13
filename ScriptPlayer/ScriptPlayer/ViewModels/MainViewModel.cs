using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ScriptPlayer.Shared.Helpers;
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
        public event EventHandler RequestHideSkipButton;
        public event EventHandler RequestShowSkipButton;
        public event EventHandler RequestShowSkipNextButton;
        public event EventHandler<string> RequestHideNotification;
        public event EventHandler Beat;

        private readonly string[] _supportedScriptExtensions;

        private readonly string[] _supportedVideoExtensions =
            {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf"};

        private string _buttplugApiVersion = "Unknown";

        private List<ConversionMode> _conversionModes;

        private Brush _heatMap;

        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;
        private byte _maxScriptPosition;
        private byte _minScriptPosition;

        public string LoadedScript
        {
            get => _loadedScript;
            private set
            {
                if (value == _loadedScript) return;
                _loadedScript = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoadedFiles));
            }
        }

        public string LoadedVideo
        {
            get => _loadedVideo;
            private set
            {
                if (value == _loadedVideo) return;
                _loadedVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoadedFiles));
            }
        }

        private PatternGenerator _pattern;
        private PatternSource _patternSource = PatternSource.Video;
        private PlaylistViewModel _playlist;
        private Thread _repeaterThread;

        private ScriptHandler _scriptHandler;

        private TestPatternDefinition _selectedTestPattern;

        private List<TestPatternDefinition> _testPatterns;

        private string _title = "";
        private VideoPlayer _videoPlayer;

        private double _volume = 50;

        private bool _wasPlaying;
        private bool _loaded;

        private Hook _hook;
        private PlaybackMode _playbackMode;
        private TimeSource _timeSource;

        private List<Range> _filterRanges;
        private PositionCollection _positions;
        private TimeSpan _positionsViewport = TimeSpan.FromSeconds(5);

        private readonly List<DeviceController> _controllers = new List<DeviceController>();
        private readonly ObservableCollection<Device> _devices = new ObservableCollection<Device>();
        private bool _showBanner = true;
        private string _scriptPlayerVersion;
        private bool _blurVideo;
        private bool _canDirectConnectLaunch;
        private SettingsViewModel _settings;
        private string _loadedScript;
        private string _loadedVideo;
        private bool _isSkipping;
        private bool _loading;
        public ObservableCollection<Device> Devices => _devices;
        public TimeSpan PositionsViewport
        {
            get => _positionsViewport;
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
            get => _filterRanges;
            set
            {
                if (Equals(value, _filterRanges)) return;
                _filterRanges = value;
                OnPropertyChanged();
            }
        }



        public bool CanDirectConnectLaunch
        {
            get => _canDirectConnectLaunch;
            private set
            {
                if (value == _canDirectConnectLaunch) return;
                _canDirectConnectLaunch = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            ButtplugApiVersion = ButtplugAdapter.GetButtplugApiVersion();
            Version = new VersionViewModel();

            ConversionModes = Enum.GetValues(typeof(ConversionMode)).Cast<ConversionMode>().ToList();
            _supportedScriptExtensions = ScriptLoaderManager.GetSupportedExtensions();

            InitializeCommands();
            InitializeTestPatterns();

            if (OsInformation.GetWindowsReleaseVersion() >= 1703)
            {
                // Only initialize LaunchFinder on Win10 15063+
                InitializeLaunchFinder();
            }

            InitializeScriptHandler();

            LoadSettings();
        }

        public string ScriptPlayerVersion
        {
            get => _scriptPlayerVersion;
            set
            {
                if (value == _scriptPlayerVersion) return;
                _scriptPlayerVersion = value;
                OnPropertyChanged();
            }
        }

        private void LoadSettings()
        {
            SettingsViewModel settings = SettingsViewModel.FromFile(GetSettingsFilePath());
            Settings = settings ?? new SettingsViewModel();

            if (Playlist == null)
            {
                Playlist = new PlaylistViewModel();
                Playlist.PlayEntry += PlaylistOnPlayEntry;
                Playlist.PropertyChanged += PlaylistOnPropertyChanged;
            }

            if (Settings.RememberPlaylist)
                LoadPlaylist();

            Playlist.Repeat = Settings.RepeatPlaylist;
            Playlist.RandomChapters = Settings.RandomChapters;
            Playlist.Shuffle = Settings.ShufflePlaylist;
        }

        private void PlaylistOnPropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            switch (eventArgs.PropertyName)
            {
                case nameof(PlaylistViewModel.Shuffle):
                    {
                        Settings.ShufflePlaylist = Playlist.Shuffle;
                        break;
                    }
                case nameof(PlaylistViewModel.Repeat):
                    {
                        Settings.RepeatPlaylist = Playlist.Repeat;
                        break;
                    }
                case nameof(PlaylistViewModel.RandomChapters):
                    {
                        Settings.RandomChapters = Playlist.RandomChapters;
                        break;
                    }
            }
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
            Settings.Save(GetSettingsFilePath());
        }

        private static string GetSettingsFilePath()
        {
            return GetAppDataFile("Settings.xml");
        }

        private static string GetDefaultPlaylistFile()
        {
            return GetAppDataFile("Playlist.m3u");
        }

        private static string GetAppDataFile(string filename)
        {
            return Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%\\ScriptPlayer\\"), filename);
        }

        private void PlaybackModeChanged(PlaybackMode oldValue, PlaybackMode newValue)
        {
            try
            {
                Debug.WriteLine("Changing TimeSource from {0} to {1}", oldValue, newValue);

                DisposeTimeSource();
                ClearScript();

                Title = "";

                LoadedScript = null;
                LoadedVideo = null;

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

                            if (string.IsNullOrWhiteSpace(Settings.WhirligigEndpoint))
                            {
                                WhirligigConnectionSettings settings =
                                    OnRequestWhirligigConnectionSettings(new WhirligigConnectionSettings
                                    {
                                        IpAndPort = WhirligigConnectionSettings.DefaultEndpoint
                                    });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }

                                Settings.WhirligigEndpoint = settings.IpAndPort;
                            }

                            TimeSource = new WhirligigTimeSource(new DispatcherClock(
                                Dispatcher.FromThread(Thread.CurrentThread),
                                TimeSpan.FromMilliseconds(10)), new WhirligigConnectionSettings
                                {
                                    IpAndPort = Settings.WhirligigEndpoint
                                });

                            ((WhirligigTimeSource)TimeSource).FileOpened += OnVideoFileOpened;

                            RefreshManualDuration();
                            break;
                        }
                    case PlaybackMode.Vlc:
                        {
                            HideBanner();

                            if (string.IsNullOrWhiteSpace(Settings.VlcEndpoint) ||
                                string.IsNullOrWhiteSpace(Settings.VlcPassword))
                            {
                                VlcConnectionSettings settings = OnRequestVlcConnectionSettings(new VlcConnectionSettings
                                {
                                    IpAndPort = VlcConnectionSettings.DefaultEndpoint,
                                    Password = "test"
                                });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }

                                Settings.VlcPassword = settings.Password;
                                Settings.VlcEndpoint = settings.IpAndPort;
                            }

                            TimeSource = new VlcTimeSource(
                                new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                    TimeSpan.FromMilliseconds(10)), new VlcConnectionSettings
                                    {
                                        IpAndPort = Settings.VlcEndpoint,
                                        Password = Settings.VlcPassword
                                    });

                            ((VlcTimeSource)TimeSource).FileOpened += OnVideoFileOpened;

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
            TryFindMatchingScript(videoFileName);
        }

        private void RefreshManualDuration()
        {
            if (TimeSource is ManualTimeSource source)
                source.SetDuration(_scriptHandler.GetOriginalScriptDuration().Add(TimeSpan.FromSeconds(5)));

            if (TimeSource is WhirligigTimeSource whirli)
                whirli.SetDuration(_scriptHandler.GetOriginalScriptDuration().Add(TimeSpan.FromSeconds(5)));
        }

        private void TimeSourceChanged()
        {
            _scriptHandler.SetTimesource(TimeSource);
            TimeSourceDurationChanged();
        }

        public void Load()
        {
            if (_loaded)
                return;

            _loaded = true;
            HookUpMediaKeys();
            CheckForArguments();
            if (Settings.CheckForNewVersionOnStartup)
                Version.CheckIfYouHaventAlready();
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
            get => _positions;
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
            TimeSourceDurationChanged();
        }

        private void TimeSourceDurationChanged()
        {
            _scriptHandler.Duration = TimeSource.Duration;
            UpdateHeatMap();
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

        private void UpdateFilter()
        {
            List<Range> newRange = new List<Range>();

            for (double i = 0; i < 10; i += 0.1)
            {
                newRange.Add(new Range
                {
                    Min = TransformPosition(0, Settings.MinPosition, Settings.MaxPosition, i) / 99.0,
                    Max = TransformPosition(255, Settings.MinPosition, Settings.MaxPosition, i) / 99.0
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
                if (Settings.NotifyVolume)
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

        public VersionViewModel Version { get; }

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
            get => _blurVideo;
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

        public string[] LoadedFiles => new[] { LoadedVideo, LoadedScript };

        public RelayCommand ExecuteSelectedTestPatternCommand { get; set; }

        public RelayCommand VolumeDownCommand { get; set; }

        public RelayCommand VolumeUpCommand { get; set; }

        public RelayCommand TogglePlaybackCommand { get; set; }

        public RelayCommand SkipToNextEventCommand { get; set; }

        public RelayCommand StartScanningButtplugCommand { get; set; }

        public RelayCommand ConnectButtplugCommand { get; set; }

        public RelayCommand DisconnectButtplugCommand { get; set; }

        public RelayCommand ToggleFullScreenCommand { get; set; }

        public RelayCommand ConnectLaunchDirectlyCommand { get; set; }

        public RelayCommand AddScriptsToPlaylistCommand { get; set; }

        public RelayCommand LoadPlaylistCommand { get; set; }

        public RelayCommand SavePlaylistCommand { get; set; }

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

        public SettingsViewModel Settings
        {
            get => _settings;
            set
            {
                if (value == _settings) return;
                SettingsViewModel oldValue = _settings;
                _settings = value;
                OnSettingsChanged(oldValue, _settings);
                OnPropertyChanged();
            }
        }

        private void OnSettingsChanged(SettingsViewModel oldValue, SettingsViewModel newValue)
        {
            if (oldValue != null)
                oldValue.PropertyChanged -= Settings_PropertyChanged;

            if (newValue == null) return;
            newValue.PropertyChanged += Settings_PropertyChanged;
            UpdateAllFromSettings();
        }

        private void UpdateAllFromSettings()
        {
            UpdateFilter();
            UpdateScriptDelay();
            UpdateCommandDelay();
            UpdateConversionMode();
            UpdatePlaylistShuffle();
            UpdatePlaylistRepeat();
            UpdateFillGaps();
            UpdateHeatMap();
        }

        private void UpdatePlaylistRepeat()
        {
            if (Playlist == null) return;
            Playlist.Repeat = Settings.RepeatPlaylist;
        }

        private void UpdatePlaylistShuffle()
        {
            if (Playlist == null) return;
            Playlist.Shuffle = Settings.ShufflePlaylist;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            switch (eventArgs.PropertyName)
            {
                case nameof(SettingsViewModel.ShowFilledGapsInHeatMap):
                    {
                        UpdateHeatMap();
                        break;
                    }
                case nameof(SettingsViewModel.FilterRange):
                case nameof(SettingsViewModel.FilterMode):
                case nameof(SettingsViewModel.MinPosition):
                case nameof(SettingsViewModel.MaxPosition):
                case nameof(SettingsViewModel.InvertPosition):
                    {
                        UpdateFilter();
                        break;
                    }
                case nameof(SettingsViewModel.ScriptDelay):
                    {
                        UpdateScriptDelay();
                        break;
                    }
                case nameof(SettingsViewModel.CommandDelay):
                    {
                        UpdateCommandDelay();
                        break;
                    }
                case nameof(SettingsViewModel.ConversionMode):
                    {
                        UpdateConversionMode();
                        break;
                    }
                case nameof(SettingsViewModel.ShufflePlaylist):
                    {
                        UpdatePlaylistShuffle();
                        break;
                    }
                case nameof(SettingsViewModel.RepeatPlaylist):
                    {
                        UpdatePlaylistRepeat();
                        break;
                    }
                case nameof(SettingsViewModel.FillGaps):
                case nameof(SettingsViewModel.FillFirstGap):
                case nameof(SettingsViewModel.FillLastGap):
                case nameof(SettingsViewModel.FillGapIntervall):
                case nameof(SettingsViewModel.MinGapDuration):
                case nameof(SettingsViewModel.FillGapGap):
                    {
                        UpdateFillGaps();
                        break;
                    }
            }
        }

        private void UpdateFillGaps()
        {
            _scriptHandler.FillGapIntervall = Settings.FillGapIntervall;
            _scriptHandler.FillGapGap = Settings.FillGapGap;
            _scriptHandler.MinGapDuration = Settings.MinGapDuration;
            _scriptHandler.FillFirstGap = Settings.FillFirstGap;
            _scriptHandler.FillLastGap = Settings.FillLastGap;
            _scriptHandler.FillGaps = Settings.FillGaps;
        }

        private void UpdateConversionMode()
        {
            _scriptHandler.ConversionMode = Settings.ConversionMode;
            UpdateHeatMap();
        }

        private void UpdateScriptDelay()
        {
            _scriptHandler.Delay = Settings.ScriptDelay;
        }

        public void Dispose()
        {
            _videoPlayer?.TimeSource.Pause();
            _videoPlayer?.Dispose();

            StopPattern();

            foreach (DeviceController controller in _controllers.ToList())
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

        private void UpdateCommandDelay()
        {
            foreach (Device device in _devices)
                device.MinDelayBetweenCommands = Settings.CommandDelay;
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



        private void Play()
        {
            if (EntryLoaded())
            {
                TimeSource.Play();
                if (Settings.NotifyPlayPause)
                    OnRequestOverlay("Play", TimeSpan.FromSeconds(2), "Playback");
            }
            else if (Playlist.EntryCount > 0)
            {
                Playlist.PlayNextEntry(LoadedFiles);
                //LoadFile(Playlist.FirstEntry().Fullname);
            }
        }

        private bool EntryLoaded()
        {
            if (PlaybackMode != PlaybackMode.Local)
                return !string.IsNullOrWhiteSpace(LoadedScript);

            return !string.IsNullOrWhiteSpace(LoadedVideo);
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
                if (Settings.NotifyPlayPause)
                    OnRequestOverlay("Pause", TimeSpan.FromSeconds(2), "Playback");
            }
        }

        private void TryFindMatchingScript(string videoFileName)
        {
            if (IsMatchingScriptLoaded(videoFileName))
            {
                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OnRequestOverlay("Matching script alreadly loaded", TimeSpan.FromSeconds(6));
                return;
            }

            string scriptFile = FindFile(videoFileName, GetScriptExtensions(), GetAdditionalPaths());
            if (string.IsNullOrWhiteSpace(scriptFile))
            {
                if (Settings.NotifyFileLoaded)
                    OnRequestOverlay($"No script for '{Path.GetFileName(videoFileName)}' found!", TimeSpan.FromSeconds(6));
                return;
            }

            LoadScript(scriptFile, false);
        }

        private string[] GetScriptExtensions()
        {
            return ScriptLoaderManager.GetSupportedExtensions();
        }

        private string[] GetAdditionalPaths()
        {
            return Settings?.AdditionalPaths?.ToArray();
        }

        private void TryFindMatchingVideo(string scriptFileName)
        {
            if (IsMatchingVideoLoaded(scriptFileName))
            {
                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OnRequestOverlay("Matching video alreadly loaded", TimeSpan.FromSeconds(6));
                return;
            }

            string videoFile = FindFile(scriptFileName, _supportedVideoExtensions, GetAdditionalPaths());
            if (string.IsNullOrWhiteSpace(videoFile))
            {
                if (Settings.NotifyFileLoaded)
                    OnRequestOverlay($"No video for '{Path.GetFileName(scriptFileName)}' found!", TimeSpan.FromSeconds(6));
                return;
            }

            LoadVideo(videoFile, false);
        }

        private static string FindFile(string filename, string[] extensions, string[] additionalPaths)
        {
            //Same directory, appended Extension
            foreach (string extension in extensions)
            {
                string path = AppendExtension(filename, extension);
                if (File.Exists(path))
                    return path;
            }

            //Same directory, exchanged extension
            foreach (string extension in extensions)
            {
                string path = Path.ChangeExtension(filename, extension);
                if (File.Exists(path))
                    return path;
            }

            if (additionalPaths == null)
                return null;

            //Additional Directories, appended extension
            string fileNameWithExtension = Path.GetFileName(filename);
            if (string.IsNullOrWhiteSpace(fileNameWithExtension))
                return null;

            foreach (string path in additionalPaths)
            {
                if (!Directory.Exists(path)) continue;

                string basePath = Path.Combine(path, fileNameWithExtension);

                foreach (string extension in extensions)
                {
                    string expectedPath = AppendExtension(basePath, extension);
                    if (File.Exists(expectedPath))
                        return expectedPath;
                }
            }

            //Additional Directories, exchanged extension
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return null;

            foreach (string path in additionalPaths)
            {
                if (!Directory.Exists(path)) continue;

                string basePath = Path.Combine(path, fileNameWithoutExtension);

                foreach (string extension in extensions)
                {
                    string expectedPath = AppendExtension(basePath, extension);
                    if (File.Exists(expectedPath))
                        return expectedPath;
                }
            }

            return null;
        }

        private static string AppendExtension(string filename, string extension)
        {
            string result = filename;
            if (!extension.StartsWith("."))
                result += ".";
            result += extension;
            return result;
        }

        private bool IsMatchingScriptLoaded(string videoFileName)
        {
            if (IsAnyEmpty(LoadedScript, videoFileName))
                return false;

            return CheckIfExtensionsMatchOrHaveCommonName(LoadedScript, videoFileName);
        }

        private bool IsMatchingVideoLoaded(string scriptFileName)
        {
            if (IsAnyEmpty(LoadedVideo, scriptFileName))
                return false;

            return CheckIfExtensionsMatchOrHaveCommonName(scriptFileName, LoadedVideo);
        }

        private static bool IsAnyEmpty(params string[] strings)
        {
            return strings.Any(string.IsNullOrWhiteSpace);
        }

        private static bool CheckIfExtensionsMatchOrHaveCommonName(string scriptFile, string videoFile)
        {
            string scriptWithoutExtension = Path.GetFileNameWithoutExtension(scriptFile);
            string scriptWithExtension = Path.GetFileName(scriptFile);

            string videoWithoutExtension = Path.GetFileNameWithoutExtension(videoFile);
            string videoWithExtension = Path.GetFileName(videoFile);

            // e.g.: File.mp4 - File.funscript
            if (InvariantEquals(scriptWithoutExtension, videoWithoutExtension)) return true;

            // e.g.: File.mp4 - File.mp4.funscript
            if (InvariantEquals(scriptWithoutExtension, videoWithExtension)) return true;

            // e.g.: File.funscript.mp4 - File.funscript
            if (InvariantEquals(scriptWithExtension, videoWithoutExtension)) return true;

            return false;
        }

        private static bool InvariantEquals(string stringA, string stringB)
        {
            return string.Equals(stringA, stringB, StringComparison.InvariantCultureIgnoreCase);
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
            DisconnectButtplugCommand = new RelayCommand(DisconnectButtplug);
            StartScanningButtplugCommand = new RelayCommand(StartScanningButtplug);
            SkipToNextEventCommand = new RelayCommand(SkipToNextEvent, CanSkipToNextEvent);
            TogglePlaybackCommand = new RelayCommand(TogglePlayback, CanTogglePlayback);
            VolumeUpCommand = new RelayCommand(VolumeUp);
            VolumeDownCommand = new RelayCommand(VolumeDown);
            ExecuteSelectedTestPatternCommand = new RelayCommand(ExecuteSelectedTestPattern, CanExecuteSelectedTestPattern);
            ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreen);
            LoadPlaylistCommand = new RelayCommand(ExecuteLoadPlaylist);
            SavePlaylistCommand = new RelayCommand(ExecuteSavePlaylist);
        }

        private void ExecuteSavePlaylist()
        {
            string filter = "M3U Playlist|*.m3u";
            int index = 0;
            string filename = OnRequestFile(filter, ref index, true);
            if (string.IsNullOrWhiteSpace(filename))
                return;

            SavePlaylist(filename);
        }

        private void ExecuteLoadPlaylist()
        {
            string filter = "M3U Playlist|*.m3u";
            int index = 0;
            string filename = OnRequestFile(filter, ref index);
            if (string.IsNullOrWhiteSpace(filename))
                return;

            LoadPlaylist(filename);
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

        private void DeviceController_DeviceRemoved(object sender, Device device)
        {
            RemoveDevice(device);
        }

        private void RemoveDevice(Device device)
        {
            // ReSharper disable once AccessToDisposedClosure
            if (ShouldInvokeInstead(() => RemoveDevice(device))) return;

            device.Disconnected -= Device_Disconnected;
            device.Dispose();
            _devices.Remove(device);

            if (Settings.NotifyDevices)
                OnRequestOverlay("Device Removed: " + device.Name, TimeSpan.FromSeconds(8));
        }

        private void DeviceController_DeviceFound(object sender, Device device)
        {
            AddDevice(device);
        }

        private void AddDevice(Device device)
        {
            if (ShouldInvokeInstead(() => AddDevice(device))) return;

            _devices.Add(device);
            device.IsEnabled = true;
            device.Disconnected += Device_Disconnected;

            if (Settings.NotifyDevices)
                OnRequestOverlay("Device Connected: " + device.Name, TimeSpan.FromSeconds(8));
        }

        private bool ShouldInvokeInstead(Action action)
        {
            if (Application.Current.Dispatcher.CheckAccess()) return false;

            Application.Current.Dispatcher.Invoke(action);
            return true;
        }

        private void Device_Disconnected(object sender, Exception exception)
        {
            Device device = sender as Device;
            if (device == null) return;

            RemoveDevice(device);
        }

        private void PlaylistOnPlayEntry(object sender, PlaylistEntry playlistEntry)
        {
            if (!TimeSource.CanOpenMedia) return;

            LoadFile(playlistEntry.Fullname);
            if (EntryLoaded())
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
            CanDirectConnectLaunch = true;
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

            if (extension == "m3u")
                LoadPlaylist(fileToLoad);
            else if (_supportedVideoExtensions.Contains(extension))
                LoadVideo(fileToLoad, true);
            else if (_supportedScriptExtensions.Contains(extension))
                LoadScript(fileToLoad, true);
        }

        private void LoadPlaylist(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = GetDefaultPlaylistFile();

            M3uPlaylist playlist = M3uPlaylist.FromFile(filename);

            Playlist.Clear();

            if (playlist == null) return;

            foreach (string entry in playlist.Entries)
                Playlist.AddEntry(new PlaylistEntry(entry));
        }

        private async void LoadVideo(string videoFileName, bool checkForScript)
        {
            try
            {
                _loading = true;

                if (checkForScript)
                    TryFindMatchingScript(videoFileName);

                LoadedVideo = videoFileName;

                TimeSpan start = TimeSpan.Zero;

                if (Settings.AutoSkip)
                    start = Settings.RandomChapters ? GetRandomChapter() : GetFirstEvent();

                if (PlaybackMode == PlaybackMode.Local)
                {
                    HideBanner();
                    await VideoPlayer.Open(videoFileName, start);
                }

                Title = Path.GetFileNameWithoutExtension(videoFileName);

                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OnRequestOverlay($"Loaded {Path.GetFileName(videoFileName)}", TimeSpan.FromSeconds(4),
                        "VideoLoaded");

                Play();

            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                _loading = false;
            }
        }

        private TimeSpan GetFirstEvent()
        {
            ScriptAction nextAction = _scriptHandler.FirstOriginalEventAfter(TimeSpan.MinValue);

            if (nextAction == null)
            {
                return TimeSpan.Zero;
            }

            TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(1);
            return skipTo;
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
            LoadedScript = fileName;

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

            IEnumerable<ScriptAction> actions = Settings.ShowFilledGapsInHeatMap ? _scriptHandler.GetScript() : _scriptHandler.GetUnfilledScript();

            List<TimeSpan> timeStamps = FilterDuplicates(actions.ToList()).Select(s => s.TimeStamp).ToList();
            Brush heatmap = HeatMapGenerator.Generate2(timeStamps, TimeSpan.Zero, TimeSource.Duration);
            HeatMap = heatmap;
        }

        private List<Tuple<TimeSpan, TimeSpan>> GetChapters(TimeSpan minChapterDuration, TimeSpan gapDuration)
        {
            List<Tuple<TimeSpan, TimeSpan>> result = new List<Tuple<TimeSpan, TimeSpan>>();

            if (TimeSource == null)
                return result;

            IEnumerable<ScriptAction> actions = _scriptHandler.GetUnfilledScript();

            List<TimeSpan> timeStamps = FilterDuplicates(actions.ToList()).Select(s => s.TimeStamp).ToList();

            if (timeStamps.Count < 2)
                return result;

            TimeSpan chapterBegin = TimeSpan.MinValue;
            TimeSpan chapterEnd = TimeSpan.MinValue;

            foreach (TimeSpan span in timeStamps)
            {
                if (chapterBegin == TimeSpan.MinValue)
                {
                    chapterBegin = span;
                    chapterEnd = span;
                }
                else if (span - chapterEnd < gapDuration)
                {
                    chapterEnd = span;
                }
                else
                {
                    result.Add(new Tuple<TimeSpan, TimeSpan>(chapterBegin, chapterEnd));

                    chapterBegin = span;
                    chapterEnd = span;
                }
            }

            if (chapterBegin != TimeSpan.MinValue && chapterEnd != TimeSpan.MinValue)
            {
                result.Add(new Tuple<TimeSpan, TimeSpan>(chapterBegin, chapterEnd));
            }

            return result.Where(t => t.Item2 - t.Item1 >= minChapterDuration).ToList();
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
            speedTransformed = ClampSpeed(speedTransformed * Settings.SpeedMultiplier);

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

            if (_loading)
                return;

            if (eventArgs.RawCurrentAction is FunScriptAction)
                HandleFunScriptAction(eventArgs.Cast<FunScriptAction>());
        }

        private void HandleFunScriptAction(ScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            SkipState skipState;
            TimeSpan timeToNextOriginalEvent = TimeSpan.Zero;

            OnBeat();

            if (eventArgs.NextAction == null)
            {
                // Script Ended
                skipState = Playlist.CanPlayNextEntry(LoadedFiles) ? SkipState.EndNext : SkipState.End;
            }
            else
            {
                skipState = SkipState.Available;

                // Determine next movement

                TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
                byte currentPositionTransformed =
                    TransformPosition(eventArgs.CurrentAction.Position, eventArgs.CurrentAction.TimeStamp);
                byte nextPositionTransformed =
                    TransformPosition(eventArgs.NextAction.Position, eventArgs.NextAction.TimeStamp);

                // Execute next movement

                if (currentPositionTransformed != nextPositionTransformed)
                {

                    byte speedOriginal =
                        SpeedPredictor.PredictSpeed(
                            (byte)Math.Abs(eventArgs.CurrentAction.Position - eventArgs.NextAction.Position),
                            duration);
                    byte speedTransformed =
                        SpeedPredictor.PredictSpeed(
                            (byte)Math.Abs(currentPositionTransformed - nextPositionTransformed),
                            duration);
                    speedTransformed = ClampSpeed(speedTransformed * Settings.SpeedMultiplier);

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

                if (eventArgs.NextAction.OriginalAction)
                {
                    timeToNextOriginalEvent = duration;

                    if (timeToNextOriginalEvent > TimeSpan.FromSeconds(10))
                    {
                        skipState = SkipState.Gap;
                    }
                }
                else
                {
                    //Next action was inserted (gap filler)

                    ScriptAction nextOriginalAction = _scriptHandler.FirstOriginalEventAfter(eventArgs.CurrentAction.TimeStamp);
                    if (nextOriginalAction == null)
                    {
                        // No more original actions
                        skipState = Playlist.CanPlayNextEntry(LoadedFiles) ? SkipState.EndFillerNext : SkipState.EndFiller;
                    }
                    else
                    {
                        timeToNextOriginalEvent = nextOriginalAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
                        skipState = timeToNextOriginalEvent > TimeSpan.FromSeconds(10) ? SkipState.FillerGap : SkipState.Filler;
                    }
                }
            }

            if (!TimeSource.IsPlaying || _isSkipping) return;

            switch (skipState)
            {
                case SkipState.Available:
                    {
                        OnRequestHideSkipButton();
                        break;
                    }
                case SkipState.Gap:
                    {
                        if (Settings.AutoSkip)
                        {
                            SkipToNextEvent();
                        }
                        else
                        {
                            if (Settings.NotifyGaps)
                                OnRequestOverlay($"Next event in {timeToNextOriginalEvent.TotalSeconds:f0}s",
                                    TimeSpan.FromSeconds(4), "Events");
                            if (Settings.ShowSkipButton)
                                OnRequestShowSkipButton();
                        }
                        break;
                    }
                case SkipState.Filler:
                    {
                        OnRequestHideSkipButton();
                        break;
                    }
                case SkipState.FillerGap:
                    {
                        if (Settings.NotifyGaps)
                            OnRequestOverlay($"Next original event in {timeToNextOriginalEvent.TotalSeconds:f0}s", TimeSpan.FromSeconds(4), "Events");
                        if (Settings.ShowSkipButton)
                            OnRequestShowSkipButton();
                        break;
                    }
                case SkipState.EndFillerNext:
                    {
                        if (Settings.NotifyGaps)
                            OnRequestOverlay("No more original events available", TimeSpan.FromSeconds(4), "Events");
                        if (Settings.ShowSkipButton)
                            OnRequestShowSkipNextButton();
                        break;
                    }
                case SkipState.EndFiller:
                    {
                        if (Settings.NotifyGaps)
                            OnRequestOverlay("No more original events available", TimeSpan.FromSeconds(4), "Events");
                        break;
                    }
                case SkipState.EndNext:
                    {
                        if (Settings.AutoSkip)
                        {
                            SkipToNextEvent();
                        }
                        else
                        {
                            if (Settings.NotifyGaps)
                                OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                            if (Settings.ShowSkipButton)
                                OnRequestShowSkipNextButton();
                        }
                        break;
                    }
                case SkipState.End:
                    {
                        if (Settings.NotifyGaps)
                            OnRequestOverlay("No more events available", TimeSpan.FromSeconds(4), "Events");
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private byte ClampSpeed(double speed)
        {
            return (byte)Math.Min(Settings.MaxSpeed, Math.Max(Settings.MinSpeed, speed));
        }

        private byte TransformPosition(byte pos, byte inMin, byte inMax, double timestamp)
        {
            double relative = (double)(pos - inMin) / (inMax - inMin);
            relative = Math.Min(1, Math.Max(0, relative));

            byte minPosition = Settings.MinPosition;
            byte maxPosition = Settings.MaxPosition;
            bool invert = Settings.InvertPosition;

            if (invert)
                relative = 1.0 - relative;

            const double secondsPercycle = 10.0;
            double cycle = timestamp / secondsPercycle;
            double range = Settings.FilterRange;

            switch (Settings.FilterMode)
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
            if (!TimeSource.IsPlaying && requirePlaying) return;

            foreach (Device device in _devices)
                device.Enqueue(information);
        }

        private void SetDevices(IntermediateCommandInformation intermediateInfo, bool requirePlaying = true)
        {
            if (!TimeSource.IsPlaying && requirePlaying) return;

            foreach (Device device in _devices)
                if (device.IsEnabled)
                    device.Set(intermediateInfo);
        }

        private void StopDevices()
        {
            foreach (Device device in _devices.ToList())
                device.Stop();
        }

        protected virtual string OnRequestFile(string filter, ref int filterIndex, bool save = false)
        {
            RequestFileEventArgs e = new RequestFileEventArgs
            {
                Filter = filter,
                FilterIndex = filterIndex,
                Handled = false,
                MultiSelect = false,
                SaveFile = save
            };

            RequestFile?.Invoke(this, e);

            if (!e.Handled) return null;

            filterIndex = e.FilterIndex;
            return e.SelectedFile;
        }

        protected virtual string[] OnRequestFiles(string filter, ref int filterIndex)
        {
            RequestFileEventArgs e = new RequestFileEventArgs
            {
                Filter = filter,
                FilterIndex = filterIndex,
                Handled = false,
                MultiSelect = true,
                SaveFile = false
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
            Playlist.PlayNextEntry(LoadedFiles);
        }

        private void PlayPreviousPlaylistEntry()
        {
            if (!TimeSource.CanOpenMedia) return;
            Playlist.PlayPreviousEntry(LoadedFiles);
        }

        private void VideoPlayer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Settings.LogMarkers != true)
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(LoadedVideo))
                    return;

                TimeSpan position = TimeSource.Progress;
                string logFile = Path.ChangeExtension(LoadedVideo, ".log");
                if (logFile == null)
                    return;

                string line = position.ToString("hh\\:mm\\:ss\\.fff");
                File.AppendAllLines(logFile, new[] { line });
                if (Settings.NotifyLogging)
                    OnRequestOverlay("Logged marker at " + line, TimeSpan.FromSeconds(5), "Log");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void SkipToNextEvent()
        {
            if (Settings.RandomChapters)
                Playlist.PlayNextEntry(LoadedFiles);
            else
                SkipToNextEventInternal();
        }

        //private async void SkipToRandomChapter()
        //{
        //    OnRequestHideSkipButton();
        //    OnRequestHideNotification("Events");

        //    if (_isSkipping)
        //        return;

        //    try
        //    {
        //        _isSkipping = true;

        //        TimeSpan skipTo = GetRandomChapter();
        //        if (skipTo == TimeSpan.Zero)
        //            return;

        //        await SkipTo(skipTo, true);
        //    }
        //    finally
        //    {
        //        _isSkipping = false;
        //    }
        //}

        private TimeSpan GetRandomChapter()
        {
            var chapters = GetChapters(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10));

            if (chapters.Count == 0)
                return TimeSpan.Zero;

            Random r = new Random();
            TimeSpan skipTo = chapters[r.Next(chapters.Count)].Item1 - TimeSpan.FromSeconds(1);
            return skipTo;
        }

        public async void SkipToNextEventInternal()
        {
            OnRequestHideSkipButton();
            OnRequestHideNotification("Events");

            if (_isSkipping)
                return;

            try
            {
                _isSkipping = true;

                //TODO Skip duplicates too!

                TimeSpan currentPosition = TimeSource.Progress;
                ScriptAction nextAction = _scriptHandler.FirstOriginalEventAfter(currentPosition - _scriptHandler.Delay);

                if (nextAction == null)
                {
                    Playlist.PlayNextEntry(LoadedFiles);
                    return;
                }

                TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(1);

                if (skipTo < currentPosition)
                    return;

                await SkipTo(skipTo);
            }
            finally
            {
                _isSkipping = false;
            }
        }

        private async Task SkipTo(TimeSpan position)
        {
            if (PlaybackMode == PlaybackMode.Local && Settings.SoftSeek)
                await VideoPlayer.SoftSeek(position);
            else
                TimeSource.SetPosition(position);

            if (Settings.NotifyGaps)
                ShowPosition($"Skipped {position.TotalSeconds:f0}s - ");
        }

        private void VideoPlayer_MediaOpened(object sender, EventArgs e)
        {
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
            return e.Handled ? e.Result : MessageBoxResult.None;
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
                    if (Settings.NotifyFileLoaded)
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
                    if (Settings.NotifyFileLoaded)
                        OnRequestOverlay($"The script file '{scriptFileName}' could not be loaded!", TimeSpan.FromSeconds(6));
                    return;
                }
            }

            if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
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
            if (Settings.NotifyPosition)
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
            await DisconnectButtplugAsync();

            if (string.IsNullOrWhiteSpace(Settings.ButtplugUrl))
            {
                string url = OnRequestButtplugUrl(ButtplugConnectionSettings.DefaultUrl);
                if (url == null)
                    return;

                Settings.ButtplugUrl = url;
            }

            ButtplugAdapter controller = new ButtplugAdapter(new ButtplugConnectionSettings
            {
                Url = Settings.ButtplugUrl
            });
            controller.Disconnected += DeviceController_Disconnected;
            controller.DeviceFound += DeviceController_DeviceFound;
            controller.DeviceRemoved += DeviceController_DeviceRemoved;

            _controllers.Add(controller);

            bool success = await controller.Connect();

            if (success)
            {
                if (Settings.NotifyDevices)
                    OnRequestOverlay("Connected to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
            }
            else
            {
                _controllers.Remove(controller);
                controller.DeviceFound -= DeviceController_DeviceFound;
                controller.DeviceRemoved -= DeviceController_DeviceRemoved;
                if (Settings.NotifyDevices)
                    OnRequestOverlay("Could not connect to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
            }
        }

        private void DeviceController_Disconnected(object sender, EventArgs eventArgs)
        {
            ButtplugAdapter controller = sender as ButtplugAdapter;

            if (controller == null) return;

            controller.DeviceFound -= DeviceController_DeviceFound;
            controller.Disconnected -= DeviceController_Disconnected;
            controller.DeviceRemoved -= DeviceController_DeviceRemoved;

            _controllers.Remove(controller);

            OnRequestOverlay("Disconnected from Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
        }

        private async void DisconnectButtplug()
        {
            await DisconnectButtplugAsync();
        }

        private async Task DisconnectButtplugAsync()
        {
            ButtplugAdapter controller = _controllers.OfType<ButtplugAdapter>().SingleOrDefault();

            if (controller == null) return;

            await controller.Disconnect();

            //controller.DeviceFound -= DeviceController_DeviceFound;

            //controller.DeviceRemoved -= DeviceController_DeviceRemoved;
            //controller.Disconnected -= DeviceController_Disconnected;

            //_controllers.Remove(controller);
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

        public void ApplySettings(SettingsViewModel settings)
        {
            Settings = settings;
            SaveSettings();

            switch (PlaybackMode)
            {
                case PlaybackMode.Whirligig:
                    if (TimeSource is WhirligigTimeSource whirligig)
                        whirligig.UpdateConnectionSettings(new WhirligigConnectionSettings
                        {
                            IpAndPort = settings.WhirligigEndpoint
                        });
                    break;
                case PlaybackMode.Vlc:
                    if (TimeSource is VlcTimeSource vlc)
                        vlc.UpdateConnectionSettings(new VlcConnectionSettings
                        {
                            IpAndPort = settings.VlcEndpoint,
                            Password = settings.VlcPassword
                        });
                    break;
            }
        }

        protected virtual void OnRequestShowSkipButton()
        {
            RequestShowSkipButton?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRequestShowSkipNextButton()
        {
            RequestShowSkipNextButton?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRequestHideSkipButton()
        {
            RequestHideSkipButton?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRequestHideNotification(string designation)
        {
            RequestHideNotification?.Invoke(this, designation);
        }

        protected virtual void OnBeat()
        {
            Beat?.Invoke(this, EventArgs.Empty);
        }
    }

    public enum SkipState
    {
        Unknown,
        Available,
        Gap,
        Filler,
        FillerGap,
        EndFillerNext,
        EndFiller,
        EndNext,
        End,
    }
}