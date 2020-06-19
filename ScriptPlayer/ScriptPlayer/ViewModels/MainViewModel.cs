using FMUtils.KeyboardHook;
using JetBrains.Annotations;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes;
using ScriptPlayer.Shared.Helpers;
using ScriptPlayer.Shared.Scripts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ScriptPlayer.Dialogs;
using ScriptPlayer.Generators;
using ScriptPlayer.Shared.Devices;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using ScriptPlayer.Shared.Devices.TheHandy;
using System.Runtime.InteropServices;

namespace ScriptPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        public event EventHandler<RequestEventArgs<VlcConnectionSettings>> RequestVlcConnectionSettings;
        public event EventHandler<RequestEventArgs<WhirligigConnectionSettings>> RequestWhirligigConnectionSettings;
        public event EventHandler<RequestEventArgs<MpcConnectionSettings>> RequestMpcConnectionSettings;
        public event EventHandler<RequestEventArgs<SamsungVrConnectionSettings>> RequestSamsungVrConnectionSettings;
        public event EventHandler<RequestEventArgs<ZoomPlayerConnectionSettings>> RequestZoomPlayerConnectionSettings;
        public event EventHandler<RequestEventArgs<KodiConnectionSettings>> RequestKodiConnectionSettings;
        public event EventHandler<RequestEventArgs<WindowStateModel>> RequestGetWindowState;
        public event EventHandler<RequestEventArgs<ThumbnailGeneratorSettings>> RequestThumbnailGeneratorSettings;
        public event EventHandler<RequestEventArgs<PreviewGeneratorSettings>> RequestPreviewGeneratorSettings;
        public event EventHandler<RequestEventArgs<HeatmapGeneratorSettings>> RequestHeatmapGeneratorSettings;
        public event EventHandler<RequestEventArgs<ThumbnailBannerGeneratorSettings>> RequestThumbnailBannerGeneratorSettings;
        public event EventHandler<RequestEventArgs<TimeSpan>> RequestScriptShiftTimespan;
        public event EventHandler<RequestEventArgs<Section>> RequestSection;
        public event EventHandler<RequestEventArgs<GeneratorSettingsViewModel>> RequestGeneratorSettings;
        
        public event EventHandler RequestActivate;
        public event EventHandler RequestShowDeviceManager;
        public event EventHandler<string> RequestShowSettings;
        public event EventHandler<ThumbnailBannerGeneratorSettings> RequestGenerateThumbnailBanner;
        public event EventHandler<WindowStateModel> RequestSetWindowState;
        
        public event EventHandler Beat;
        public event EventHandler<double> IntermediateBeat;
        public event EventHandler RequestShowGeneratorProgressDialog;

        private readonly string[] _supportedScriptExtensions;

        private readonly string[] _supportedVideoExtensions =
            {"mp4", "mpg", "mpeg", "m4v", "avi", "mkv", "mp4v", "mov", "wmv", "asf", "webm", "flv", "m2ts"};

        private readonly string[] _supportedAudioExtensions =
            {"mp3", "wav"};

        private readonly string[] _supportedMediaExtensions;

        private string _buttplugApiVersion = "Unknown";

        private List<ConversionMode> _conversionModes;

        private Brush _heatMap;
        private ThumbnailGeneratorSettings _lastThumbnailSettings;
        private ThumbnailBannerGeneratorSettings _lastThumbnailBannerSettings;
        private PreviewGeneratorSettings _lastPreviewSettings;
        private HeatmapGeneratorSettings _lastHeatmapSettings;

        private TimeSpan _loopA = TimeSpan.MinValue;
        private TimeSpan _loopB = TimeSpan.MinValue;

        private int _lastScriptFilterIndex = 1;
        private int _lastVideoFilterIndex = 1;
        private byte _maxScriptPosition;
        private byte _minScriptPosition;

        public GeneratorWorkQueue WorkQueue { get; }

        public HandyController Handy { get; set; } = null;

        public WindowStateModel InitialPlayerState { get; private set; }

        public bool IsFullscreen
        {
            get => _isFullscreen;
            set
            {
                if (value == _isFullscreen) return;
                _isFullscreen = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RepeatablePattern> Patterns
        {
            get => _patterns;
            set
            {
                if (Equals(value, _patterns)) return;
                _patterns = value;
                OnPropertyChanged();
            }
        }

        public VideoThumbnailCollection Thumbnails
        {
            get => _thumbnails;
            set
            {
                if (Equals(value, _thumbnails)) return;
                _thumbnails = value;
                OnPropertyChanged();
            }
        }

        public RepeatablePattern SelectedPattern
        {
            get => _selectedPattern;
            set
            {
                if (Equals(value, _selectedPattern)) return;
                _selectedPattern = value;
                UpdatePattern(CommandSource);
                OnPropertyChanged();
            }
        }

        public double CurrentPosition
        {
            get => _currentPosition;
            set
            {
                if (value.Equals(_currentPosition)) return;
                _currentPosition = value;
                OnPropertyChanged();
            }
        }

        public string LoadedScript
        {
            get => _loadedScript;
            private set
            {
                if (value == _loadedScript) return;
                _loadedScript = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoadedFiles));

                Playlist.SetCurrentEntry(LoadedFiles);
                UpdateAutoReloadScript();
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

                Playlist.SetCurrentEntry(LoadedFiles);
            }
        }

        public string LoadedAudio   
        {
            get => _loadedAudio;
            set
            {
                if (value == _loadedAudio) return;
                _loadedAudio = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LoadedFiles));
            }
        }

        public Section SelectedRange
        {
            get => _selectedRange;
            set
            {
                if (Equals(value, _selectedRange)) return;
                _selectedRange = value;
                OnPropertyChanged();
                UpdateDisplayedSelection();
            }
        }

        public Section DisplayedRange
        {
            get => _displayedRange;
            set
            {
                if (Equals(value, _displayedRange)) return;
                _displayedRange = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan DisplayedProgress
        {
            get => _displayedProgress;
            set
            {
                if (value.Equals(_displayedProgress)) return;
                _displayedProgress = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan DisplayedDuration
        {
            get => _displayedDuration;
            set
            {
                if (value.Equals(_displayedDuration)) return;
                _displayedDuration = value;
                OnPropertyChanged();
            }
        }

        private PatternGenerator _pattern;
        private CommandSource _commandSource = CommandSource.Video;
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

        private bool IsSeeking
        {
            get
            {
                if (PlaybackMode != PlaybackMode.Local) return false;
                return _videoPlayer.IsSeeking;
            }
        }

        private bool IsTransistioning
        {
            get
            {
                if (PlaybackMode != PlaybackMode.Local) return false;
                return _videoPlayer.IsTransistioning;
            }
        }

        private bool _loading;
        private double _currentPosition;
        private RepeatablePattern _selectedPattern;
        private ObservableCollection<RepeatablePattern> _patterns;
        private VideoThumbnailCollection _thumbnails;
        private CommandSource _previousCommandSource;
        private string _lastFolder;
        private Section _selectedRange;
        private string _randomChapterToolTip;
        private Section _displayedRange;

        //TODO Make this configurable (5-20s?)

        /// <summary>
        /// Gap Duration always >=, not just >
        /// </summary>
        private readonly TimeSpan _gapDuration = TimeSpan.FromSeconds(10);

        private TimeSpan _displayedDuration;
        private TimeSpan _displayedProgress;
        private List<Section> _chapters;
        private TimeSpan _previousProgress = TimeSpan.MinValue;
        private bool _loopSelection;
        private PlaybackMode _initialPlaybackMode = PlaybackMode.Local;
        private TimeSpan _previousShiftTimeSpan = TimeSpan.FromSeconds(10);
        private GeneratorSettingsViewModel _previousGeneratorSettings;
        private IOnScreenDisplay _mainOsd;
        private IOnScreenDisplay[] _usedOsds;
        private string _previouslyOpenedVideoFile = "";
        private bool _isFullscreen;
        private FileSystemWatcher _fileWatcher;
        private AudioFileTimeSource _audioHandler;
        private string _loadedAudio;

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
            // Can get rather intense even for low values, keep it at 1 for now (Ffmpeg is multi-threaded anyways)
            int threadCount = 1; //Environment.ProcessorCount;

            WorkQueue = new GeneratorWorkQueue();
            WorkQueue.JobFinished += WorkQueueOnJobFinished;
            WorkQueue.Start(threadCount);

            _supportedMediaExtensions = _supportedVideoExtensions.ToArray();

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

            InitializeEstimController();

            InitializeScriptHandler();
            GeneratePatterns();

            LoadSettings();
        }

        private void LoadAudio(string file)
        {
            try
            {
                DisposeAudioHandler();
                _audioHandler = new AudioFileTimeSource(file, Settings.EstimAudioDevice);
                LoadedAudio = file;

                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OsdShowMessage($"Loaded {Path.GetFileName(file)}", TimeSpan.FromSeconds(4),
                        "AudioLoaded");
            }
            catch (Exception)
            {
                LoadedAudio = null;
                if(Settings.NotifyFileLoaded)
                    OsdShowMessage($"Couln't load {Path.GetFileName(file)}", TimeSpan.FromSeconds(4), "AudioLoaded");
            }
        }

        private void DisposeAudioHandler()
        {
            if (_audioHandler != null)
            {
                _audioHandler.Dispose();
                _audioHandler = null;
            }
        }


        private void WorkQueueOnJobFinished(object sender, GeneratorJobEventArgs eventArgs)
        {
            if (eventArgs.Result.Success)
                RecheckForAdditionalFiles(false);
        }

        private void GeneratePatterns()
        {
            Patterns = new ObservableCollection<RepeatablePattern>
            {
                new RepeatablePattern(0, 99) {Name = "Up / Down"},
                new RepeatablePattern(0, -1, 66, 33, -1, 99, -1, 33, 66) {Name = "Zig Zag"},
                new RepeatablePattern(99, 0, -1, -1, 0) {Name = "Slow Pulse"},
                new RepeatablePattern(99, 99, 0) {Name = "Fast Pulse"},
                new RepeatablePattern(50,99,0,50,50){Name = "Heartbeat"},
                new RepeatablePattern(99, 0, 99, 0, -1, 99, -1, 0, -1, 99, -1, 0, -1) {Name = "Fast / Slow"},
                new RepeatablePattern(0,20,10,30,20,40,30,50,40,60,50,70,60,80,70,90,80,99,-1,-1,-1){Name = "Stairs"},
                new RepeatablePattern(0,10,0,20,0,30,0,40,0,50,0,60,0,70,0,80,0,90,0,99){Name="Saw"},
                new RepeatablePattern(50,60,40,70,30,80,20,90,10,99,0,99,10,90,20,80,30,70,40,60){Name ="Wave"}
            };

            SelectedPattern = Patterns.First();
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
                Playlist.RequestMediaFileName += Playlist_RequestMediaFileName;
                Playlist.RequestVideoFileName += Playlist_RequestVideoFileName;
                Playlist.RequestScriptFileName += Playlist_RequestScriptFileName;
                Playlist.RequestGenerateThumbnails += PlaylistOnRequestGenerateThumbnails;
                Playlist.RequestGenerateThumbnailBanners += PlaylistOnRequestGenerateThumbnailBanners;
                Playlist.RequestGeneratePreviews += PlaylistOnRequestGeneratePreviews;
                Playlist.RequestGenerateHeatmaps += PlaylistOnRequestGenerateHeatmaps;
                Playlist.RequestGenerateAll += PlaylistOnRequestGenerateAll;
                Playlist.EntriesChanged += PlaylistOnEntriesChanged;
                Playlist.NextOrPreviousChanged += PlaylistOnNextOrPreviousChanged;
            }

            if (Settings.RememberPlaylist)
                LoadPlaylist();

            Playlist.Repeat = Settings.RepeatPlaylist;
            Playlist.RandomChapters = Settings.RandomChapters;
            Playlist.Shuffle = Settings.ShufflePlaylist;

            if (!GlobalCommandManager.LoadMappings(GetCommandMappingsFilePath()))
                GlobalCommandManager.BuildDefaultShortcuts();

            UpdateRandomChapterTooltip();

            LoadGeneratorSettings();
        }

        private void PlaylistOnNextOrPreviousChanged(object sender, EventArgs eventArgs1)
        {
            UpdateGeneratorPriority();
        }

        private void PlaylistOnEntriesChanged(object sender, bool entriesAdded)
        {
            if(entriesAdded)
                CheckAutoGenerateAll();
        }

        private void CheckAutoGenerateAll()
        {
            if (Settings.AutogenerateAllForPlaylist)
                Playlist.GenerateAllForAll(false);
        }

        private void PlaylistOnRequestGenerateAll(object sender, Tuple<string[], bool> tuple)
        {
            bool visible = tuple.Item2;
            string[] videos = tuple.Item1;

            GenerateAll(visible, videos);
        }

        private void PlaylistOnRequestGenerateHeatmaps(object sender, string[] videos)
        {
            GenerateHeatmaps(videos);
        }

        private void PlaylistOnRequestGenerateThumbnails(object sender, string[] videos)
        {
            GenerateThumbnails(true, true, videos);
        }

        private void PlaylistOnRequestGenerateThumbnailBanners(object sender, string[] videos)
        {
            GenerateThumbnailBanners(videos);
        }

        private void PlaylistOnRequestGeneratePreviews(object sender, string[] videos)
        {
            GeneratePreviews(false, videos);
        }

        private void Playlist_RequestScriptFileName(object sender, RequestEventArgs<string> e)
        {
            e.Handled = true;
            e.Value = GetScriptFile(e.Value);
        }

        private void Playlist_RequestMediaFileName(object sender, RequestEventArgs<string> e)
        {
            e.Handled = true;
            e.Value = GetMediaFile(e.Value);
        }

        private void Playlist_RequestVideoFileName(object sender, RequestEventArgs<string> e)
        {
            e.Handled = true;
            e.Value = GetVideoFile(e.Value);
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
                case nameof(PlaylistViewModel.RepeatSingleFile):
                    {
                        Settings.RepeatSingleFile = Playlist.RepeatSingleFile;
                        break;
                    }
                case nameof(PlaylistViewModel.RandomChapters):
                    {
                        Settings.RandomChapters = Playlist.RandomChapters;
                        if (!Playlist.RandomChapters)
                            SelectedRange = null;
                        break;
                    }
            }
        }


        private void SavePlaylist(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = GetDefaultPlaylistFile();

            M3uPlaylist playlist = new M3uPlaylist();
            playlist.Entries.AddRange(Playlist.Entries.Select(ToM3uEntry));
            playlist.Save(filename);
        }

        private static M3uPlaylist.M3uEntry ToM3uEntry(PlaylistEntry entry)
        {
            return new M3uPlaylist.M3uEntry
            {
                FilePath = entry.Fullname,
                DisplayName = entry.Shortname,
                DurationInSeconds = (int?)entry.Duration?.TotalSeconds ?? 0
            };
        }

        private static PlaylistEntry ToPlaylistEntry(M3uPlaylist.M3uEntry entry)
        {
            return new PlaylistEntry
            {
                Fullname = entry.FilePath,
                Shortname = entry.DisplayName,
                Duration = TimeSpan.FromSeconds(entry.DurationInSeconds)
            };
        }

        private void SaveSettings()
        {
            Settings.Save(GetSettingsFilePath());

            PlayerStateModel playerState = new PlayerStateModel();
            if (Settings.RememberVolume)
                playerState.Volume = Volume;
            if (Settings.RememberPlaybackMode)
                playerState.PlaybackMode = PlaybackMode;
            if (Settings.RememberWindowPosition)
                playerState.WindowState = OnRequestWindowState();

            playerState.Save(GetPlayerStateFilePath());

            GlobalCommandManager.SaveMappings(GetCommandMappingsFilePath());
        }

        private static string GetPlayerStateFilePath()
        {
            return GetAppDataFile("PlayerState.xml");
        }

        private static string GetGeneratorSettingsFilePath()
        {
            return GetAppDataFile("GeneratorSettings.xml");
        }

        private static string GetSettingsFilePath()
        {
            return GetAppDataFile("Settings.xml");
        }

        private static string GetCommandMappingsFilePath()
        {
            return GetAppDataFile("CommandMappings.xml");
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
                    case PlaybackMode.ZoomPlayer:
                        {
                            HideBanner();

                            if (string.IsNullOrWhiteSpace(Settings.ZoomPlayerEndpoint))
                            {
                                ZoomPlayerConnectionSettings settings =
                                    OnRequestZoomPlayerConnectionSettings(new ZoomPlayerConnectionSettings
                                    {
                                        IpAndPort = ZoomPlayerConnectionSettings.DefaultEndpoint
                                    });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }

                                Settings.ZoomPlayerEndpoint = settings.IpAndPort;
                            }

                            TimeSource = new ZoomPlayerTimeSource(new DispatcherClock(
                                Dispatcher.FromThread(Thread.CurrentThread),
                                TimeSpan.FromMilliseconds(10)), new ZoomPlayerConnectionSettings
                                {
                                    IpAndPort = Settings.ZoomPlayerEndpoint
                                });

                            ((ZoomPlayerTimeSource)TimeSource).FileOpened += OnVideoFileOpened;

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
                    case PlaybackMode.MpcHc:
                        {
                            HideBanner();

                            if (string.IsNullOrWhiteSpace(Settings.MpcHcEndpoint))
                            {
                                MpcConnectionSettings settings =
                                    OnRequestMpcConnectionSettings(new MpcConnectionSettings
                                    {
                                        IpAndPort = MpcConnectionSettings.DefaultEndpoint
                                    });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }

                                Settings.MpcHcEndpoint = settings.IpAndPort;
                            }

                            TimeSource = new MpcTimeSource(
                            new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                TimeSpan.FromMilliseconds(10)), new MpcConnectionSettings
                                {
                                    IpAndPort = Settings.MpcHcEndpoint
                                });

                            ((MpcTimeSource)TimeSource).FileOpened += OnVideoFileOpened;

                            break;
                        }
                    case PlaybackMode.SamsungVr:
                        {
                            HideBanner();

                            if (Settings.SamsungVrUdpPort == 0)
                            {
                                SamsungVrConnectionSettings settings =
                                    OnRequestSamsungVrConnectionSettings(new SamsungVrConnectionSettings
                                    {
                                        UdpPort = SamsungVrConnectionSettings.DefaultPort
                                    });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }

                                Settings.SamsungVrUdpPort = settings.UdpPort;
                            }

                            TimeSource = new SamsungVrTimeSource(
                                new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                    TimeSpan.FromMilliseconds(10)), new SamsungVrConnectionSettings
                                    {
                                        UdpPort = Settings.SamsungVrUdpPort
                                    });

                            ((SamsungVrTimeSource)TimeSource).FileOpened += OnVideoFileOpened;
                            RefreshManualDuration();
                            break;
                        }
                    case PlaybackMode.Kodi:
                        {
                            HideBanner();
                            if (Settings.KodiHttpPort == 0)
                            {
                                KodiConnectionSettings settings =
                                    OnRequestKodiConnectionSettings(new KodiConnectionSettings
                                    {
                                        HttpPort = KodiConnectionSettings.DefaultHttpPort,
                                        TcpPort = KodiConnectionSettings.DefaultTcpPort,
                                        Ip = KodiConnectionSettings.DefaultIp,
                                        Password = KodiConnectionSettings.DefaultPassword,
                                        User = KodiConnectionSettings.DefaultUser
                                    });

                                if (settings == null)
                                {
                                    PlaybackMode = PlaybackMode.Local;
                                    return;
                                }
                                Settings.KodiIp = settings.Ip;
                                Settings.KodiHttpPort = settings.HttpPort;
                                Settings.KodiTcpPort = settings.TcpPort;
                                Settings.KodiUser = settings.User;
                                Settings.KodiPassword = settings.Password;

                            }
                            TimeSource = new KodiTimeSource(
                                new DispatcherClock(Dispatcher.FromThread(Thread.CurrentThread),
                                    TimeSpan.FromMilliseconds(10)), new KodiConnectionSettings
                                    {
                                        Ip = Settings.KodiIp,
                                        HttpPort = Settings.KodiHttpPort,
                                        TcpPort = Settings.KodiTcpPort,
                                        User = Settings.KodiUser,
                                        Password = Settings.KodiPassword
                                    });

                            ((KodiTimeSource)TimeSource).FileOpened += OnVideoFileOpened;
                            RefreshManualDuration();
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
            RefreshChapters();
        }

        private VlcConnectionSettings OnRequestVlcConnectionSettings(VlcConnectionSettings currentSettings)
        {
            RequestEventArgs<VlcConnectionSettings> args = new RequestEventArgs<VlcConnectionSettings>(currentSettings);
            RequestVlcConnectionSettings?.Invoke(this, args);

            if (!args.Handled)
                return null;
            return args.Value;
        }

        private ZoomPlayerConnectionSettings OnRequestZoomPlayerConnectionSettings(ZoomPlayerConnectionSettings currentSettings)
        {
            RequestEventArgs<ZoomPlayerConnectionSettings> args = new RequestEventArgs<ZoomPlayerConnectionSettings>(currentSettings);
            RequestZoomPlayerConnectionSettings?.Invoke(this, args);

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

        private SamsungVrConnectionSettings OnRequestSamsungVrConnectionSettings(SamsungVrConnectionSettings currentSettings)
        {
            RequestEventArgs<SamsungVrConnectionSettings> args = new RequestEventArgs<SamsungVrConnectionSettings>(currentSettings);
            RequestSamsungVrConnectionSettings?.Invoke(this, args);

            if (!args.Handled)
                return null;
            return args.Value;
        }

        private MpcConnectionSettings OnRequestMpcConnectionSettings(MpcConnectionSettings currentSettings)
        {
            RequestEventArgs<MpcConnectionSettings> args = new RequestEventArgs<MpcConnectionSettings>(currentSettings);
            RequestMpcConnectionSettings?.Invoke(this, args);

            if (!args.Handled)
                return null;
            return args.Value;
        }

        private KodiConnectionSettings OnRequestKodiConnectionSettings(KodiConnectionSettings currentSettings)
        {
            RequestEventArgs<KodiConnectionSettings> args = new RequestEventArgs<KodiConnectionSettings>(currentSettings);
            RequestKodiConnectionSettings?.Invoke(this, args);

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

            if (ReferenceEquals(TimeSource, _videoPlayer?.TimeSource))
                return;

            if (TimeSource is IDisposable disposable)
                disposable.Dispose();
        }

        private void OnVideoFileOpened(object sender, string videoFileName)
        {
            if (_previouslyOpenedVideoFile == videoFileName)
                return;

            _previouslyOpenedVideoFile = videoFileName;

            if (Settings.FuzzyMatchingEnabled)
            {
                try
                {
                    Regex regex = new Regex(Settings.FuzzyMatchingPattern);

                    if (regex.IsMatch(videoFileName))
                    {
                        videoFileName = regex.Replace(videoFileName, Settings.FuzzyMatchingReplacement);
                    }
                }
                catch
                {
                    //
                }
            }

            TryFindMatchingScript(videoFileName);
            TryFindMatchingThumbnails(videoFileName, true);

            UpdateGeneratorPriority();
        }

        private void UpdateGeneratorPriority()
        {
            List<string> prioritizedVideos = new List<string>();

            if(!string.IsNullOrEmpty(LoadedVideo))
                prioritizedVideos.Add(LoadedVideo);

            if (Playlist.NextEntry != null)
            {
                string mediaFile = GetMediaFile(Playlist.NextEntry.Fullname);
                if(!string.IsNullOrEmpty(mediaFile))
                    prioritizedVideos.Add(mediaFile);
            }

            if (Playlist.PreviousEntry != null)
            {
                string mediaFile = GetMediaFile(Playlist.PreviousEntry.Fullname);
                if(!string.IsNullOrEmpty(mediaFile))
                    prioritizedVideos.Add(mediaFile);
            }

            WorkQueue.Prioritize(prioritizedVideos.ToArray());
        }


        private void RefreshManualDuration()
        {
            if (TimeSource is ManualTimeSource source)
                source.SetDuration(_scriptHandler.GetOriginalScriptDuration().Add(TimeSpan.FromSeconds(5)));

            if (TimeSource is WhirligigTimeSource whirli)
                whirli.SetDuration(_scriptHandler.GetOriginalScriptDuration().Add(TimeSpan.FromSeconds(5)));

            if (TimeSource is SamsungVrTimeSource samsung)
                samsung.SetDuration(_scriptHandler.GetOriginalScriptDuration().Add(TimeSpan.FromSeconds(5)));
        }

        private void TimeSourceChanged()
        {
            _scriptHandler.SetTimesource(TimeSource);
            TimeSourceDurationChanged();
            RefreshOsds();
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

            PlaybackMode = _initialPlaybackMode;

            AutoStartButtplug();
            AutoConnectButtplug();

            InstanceHandler.CommandLineReceived += InstanceHandlerOnCommandLineReceived;
            InstanceHandler.EnableEvents();

            UpdateVideoCrop();
        }

        private void AutoConnectButtplug()
        {
            if (!Settings.AutoConnectToButtplug)
                return;

            ConnectButtplug();
        }

        private void AutoStartButtplug()
        {
            try
            {
                if (!Settings.AutoStartButtplug)
                    return;

                if (string.IsNullOrEmpty(Settings.ButtplugExePath))
                {
                    OsdShowMessage("Can't autostart Buttplug - path not set", TimeSpan.FromSeconds(5), "Buttplug");
                    return;
                }

                if (!File.Exists(Settings.ButtplugExePath))
                {
                    OsdShowMessage("Can't autostart Buttplug - file not found (or inaccessible)", TimeSpan.FromSeconds(5), "Buttplug");
                    return;
                }

                bool alreadyRunning = ProcessHelper.IsExecutableRunning(Settings.ButtplugExePath);

                if (alreadyRunning)
                {
                    OsdShowMessage("Buttplug is already running - no autostart needed", TimeSpan.FromSeconds(5),
                        "Buttplug");
                    return;
                }

                OsdShowMessage("Starting Buttplug ...", TimeSpan.FromSeconds(5), "Buttplug");
                Process pro = Process.Start(Settings.ButtplugExePath);
                bool waitedForIt = pro.WaitForInputIdle(5000);

                if (waitedForIt)
                    OsdShowMessage("Buttplug started", TimeSpan.FromSeconds(5), "Buttplug");
                else
                    OsdShowMessage("Buttplug is not responding", TimeSpan.FromSeconds(5), "Buttplug");
            }
            catch (Exception ex)
            {
                OsdShowMessage("Couldn't start Buttplug: " + ex.Message, TimeSpan.FromSeconds(5), "Buttplug");
            }
        }

        private void InstanceHandlerOnCommandLineReceived(object sender, string commandLine)
        {
            Debug.WriteLine("Received Commandline: " + commandLine);
            PassCommandLineAlong(commandLine);
        }

        private void PassCommandLineAlong(string commandLine)
        {
            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    PassCommandLineAlong(commandLine);
                }));
                return;
            }

            string[] args = CommandLineSplitter.CommandLineToArgs(commandLine);
            ProcessArguments(args, false);
        }

        public void LoadPlayerState()
        {
            PlayerStateModel playerState = PlayerStateModel.FromFile(GetPlayerStateFilePath());

            if (playerState != null)
            {
                if (playerState.Volume != null)
                    Volume = (double)playerState.Volume;

                if (playerState.PlaybackMode != null)
                    _initialPlaybackMode = (PlaybackMode)playerState.PlaybackMode;

                if (playerState.WindowState != null)
                    if (playerState.WindowState.Width > 0 && playerState.WindowState.Height > 0)
                        InitialPlayerState = playerState.WindowState;
            }
        }

        private void HookUpMediaKeys()
        {
            _hook = new Hook("ScriptPlayer");
            _hook.KeyDownEvent += Hook_KeyDownEvent;
        }

        private void Hook_KeyDownEvent(KeyboardHookEventArgs e)
        {
            KeyAndModifiers keyAndModifiers = KeyAndModifiers.FromKeyboardHookEventArgs(e);
            GlobalCommandManager.ProcessInput(keyAndModifiers.Key, keyAndModifiers.Modifiers, KeySource.Global);
        }

        public void SetMainWindow(Window window)
        {
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
            source.AddHook(new HwndSourceHook(WndProc));
        }

        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // The virtual-key code is in the message's wParam parameter

            if (msg == WM_KEYDOWN)
            {
                KeyAndModifiers keys = KeyAndModifiers.FromWndProc((int) wParam);

                
                //Debug.WriteLine("Received a WndProc WM_KEYDOWN Message: " + key + " (Modifiers = " + modifiers);

                bool processed = GlobalCommandManager.ProcessInput(keys.Key, keys.Modifiers, KeySource.WndProc);
                handled = processed;
            }
            return IntPtr.Zero;
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
            {
                oldValue.ProgressChanged -= TimeSourceOnProgressChanged;
                oldValue.DurationChanged -= TimeSourceOnDurationChanged;
                oldValue.PlaybackRateChanged -= TimeSourceOnPlaybackRateChanged;
                oldValue.IsPlayingChanged -= TimeSourceIsPlayingChanged;
            }

            if (newValue != null)
            {
                newValue.ProgressChanged += TimeSourceOnProgressChanged;
                newValue.DurationChanged += TimeSourceOnDurationChanged;
                newValue.PlaybackRateChanged += TimeSourceOnPlaybackRateChanged;
                newValue.IsPlayingChanged += TimeSourceIsPlayingChanged;
            }
        }

        private void TimeSourceIsPlayingChanged(object sender, bool playing)
        {
            Handy?.Play(playing, TimeSource.Progress.TotalMilliseconds);
            if(!playing)
            {
                StopDevices();
                AutoHomeDevices();

                _audioHandler?.Pause();
            }
            else
            {
                _audioHandler?.Play();
            }
        }

        private void TimeSourceOnPlaybackRateChanged(object sender, double d)
        {
            UpdateHeatMap();
        }

        private void TimeSourceOnProgressChanged(object sender, TimeSpan e)
        {
            UpdateDisplayedProgress();

            if (!TimeSource.IsPlaying || IsSeeking || IsTransistioning)
                return;

            if (_loopSelection)
            {
                if (_loopA != TimeSpan.MinValue && _loopB != TimeSpan.MinValue)
                {
                    TimeSpan loopStart = _loopA < _loopB ? _loopA : _loopB;
                    TimeSpan loopEnd = _loopA > _loopB ? _loopA : _loopB;

                    if (e >= loopEnd)
                    {
                        SkipTo(loopStart, Settings.SoftSeekLoops, Settings.SoftSeekLoopsDuration);
                        return;
                    }
                }
            }

            Handy?.Resync(e);
            _audioHandler?.Resync(e);

            if (Settings.SoftSeekFiles)
            {
                //Don't Seek more than a third of the entire video 
                // (workaround for very short videos combined with verly high soft seek values)
                var softSeek = Settings.SoftSeekFilesDuration;
                var thirdVideoDuration = TimeSource.Duration.Divide(2);

                if (thirdVideoDuration < softSeek)
                    softSeek = thirdVideoDuration;

                if (e + softSeek >= TimeSource.Duration)
                {
                    MediaCanBeConsideredEnded();
                }
            }
        }

        private bool SimilarTimeSpan(TimeSpan t1, TimeSpan t2)
        {
            return (int)t1.TotalSeconds == (int)t2.TotalSeconds;
        }

        private void UpdateDisplayedProgress()
        {
            if (_chapters == null)
                RefreshChapters();

            if (TimeSource == null)
                return;

            if (SimilarTimeSpan(TimeSource.Progress, _previousProgress))
                return;

            DisplayedProgress = TranslateMediaPosition(_chapters, TimeSource.Progress, Settings.TimeDisplayMode);
            _previousProgress = TimeSource.Progress;
        }

        private void TimeSourceOnDurationChanged(object sender, TimeSpan timeSpan)
        {
            TimeSourceDurationChanged();
        }

        private void TimeSourceDurationChanged()
        {
            _scriptHandler.Duration = TimeSource.Duration;
            UpdateHeatMap();
            UpdateDisplayedDuration();
        }

        private void UpdateDisplayedDuration()
        {
            if (_chapters == null)
                RefreshChapters();

            if (TimeSource == null)
                return;

            DisplayedDuration = TranslateMediaPosition(_chapters, TimeSource.Duration, Settings.TimeDisplayMode);

            _previousProgress = TimeSpan.MinValue;

            UpdateDisplayedProgress();
        }

        private void RefreshChapters()
        {
            _chapters = GetChapters(TimeSpan.Zero, _gapDuration, false).Cast<Section>().ToList();

            UpdateHeatMap();
            UpdateDisplayedDuration();
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
                    OsdShowMessage($"Volume: {Volume:f0}%", TimeSpan.FromSeconds(4), "Volume");
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

        public CommandSource CommandSource
        {
            get => _commandSource;
            set
            {
                if (value == _commandSource) return;
                _commandSource = value;
                UpdatePattern(_commandSource);
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

        public bool LoopSelection
        {
            get => _loopSelection;
            set
            {
                if (value == _loopSelection) return;
                _loopSelection = value;
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

        public string[] LoadedFiles => new[] { LoadedVideo, LoadedScript, LoadedAudio };

        public ScriptplayerCommand ExecuteSelectedTestPatternCommand { get; set; }

        public ScriptplayerCommand VolumeDownCommand { get; set; }

        public ScriptplayerCommand VolumeUpCommand { get; set; }

        public ScriptplayerCommand TogglePlaybackCommand { get; set; }

        public ScriptplayerCommand SkipToNextEventCommand { get; set; }

        public ScriptplayerCommand StartScanningButtplugCommand { get; set; }

        public ScriptplayerCommand SetLoopACommand { get; set; }

        public ScriptplayerCommand SetLoopBCommand { get; set; }

        public ScriptplayerCommand ClearLoopCommand { get; set; }

        public ScriptplayerCommand ConnectButtplugCommand { get; set; }

        public ScriptplayerCommand DisconnectButtplugCommand { get; set; }

        public ScriptplayerCommand ToggleFullScreenCommand { get; set; }

        public ScriptplayerCommand ConnectLaunchDirectlyCommand { get; set; }

        public ScriptplayerCommand ConnectHandyDirectlyCommand { get; set; }

        public ScriptplayerCommand AddEstimAudioCommand { get; set; }

        public ScriptplayerCommand AddScriptsToPlaylistCommand { get; set; }

        public ScriptplayerCommand AddFolderToPlaylistCommand { get; set; }

        public ScriptplayerCommand RemoveMissingEntriesFromPlaylistCommand { get; set; }

        public ScriptplayerCommand RemoveIncompleteEntriesFromPlaylistCommand { get; set; }

        public ScriptplayerCommand LoadPlaylistCommand { get; set; }

        public ScriptplayerCommand SavePlaylistCommand { get; set; }

        public ScriptplayerCommand OpenVideoCommand { get; set; }

        public ScriptplayerCommand OpenScriptCommand { get; set; }

        public ScriptplayerCommand GenerateThumbnailsForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateThumbnailBannerForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GeneratePreviewForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateHeatmapForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand GenerateAllForLoadedVideoCommand { get; set; }

        public ScriptplayerCommand ShowSettingsCommand { get; set; }

        public ScriptplayerCommand ShowDeviceManagerCommand { get; set; }

        public RelayCommand<TimeDisplayMode> SetTimeDisplayModeCommand { get; set; }

        public RelayCommand<bool> SetShowTimeLeftCommand { get; set; }

        public ScriptplayerCommand SaveScriptAsCommand { get; set; }

        public ScriptplayerCommand ShiftScriptCommand { get; set; }

        public ScriptplayerCommand TrimScriptCommand { get; set; }

        public ScriptplayerCommand ShowGeneratorSettingsCommand { get; set; }

        public ScriptplayerCommand ShowGeneratorProgressCommand { get; set; }

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
            UpdateDeviceSettings();
            UpdateConversionMode();
            UpdatePlaylistShuffle();
            UpdatePlaylistRepeat();
            UpdatePlaylistRepeatSingleFile();
            UpdatePlaylistRandomChapter();
            UpdateFillGaps();
            UpdateFallbackScriptModifiers();
            UpdateHeatMap();
            UpdatePatternSpeed();
            UpdateOsd();
            UpdateVideoCrop();
            UpdateAutoReloadScript();
            ReloadAudio();
        }

        private void UpdatePlaylistRandomChapter()
        {
            if (Playlist == null) return;
            Playlist.RandomChapters = Settings.RandomChapters;
        }

        private void UpdatePlaylistRepeatSingleFile()
        {
            if (Playlist == null) return;
            Playlist.RepeatSingleFile = Settings.RepeatSingleFile;
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
                case nameof(SettingsViewModel.VibratorConversionMode):
                case nameof(SettingsViewModel.CommandDelay):
                    {
                        UpdateDeviceSettings();
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
                case nameof(SettingsViewModel.RepeatSingleFile):
                    {
                        UpdatePlaylistRepeatSingleFile();
                        break;
                    }
                case nameof(SettingsViewModel.RandomChapters):
                    {
                        UpdatePlaylistRandomChapter();
                        break;
                    }
                case nameof(SettingsViewModel.RangeExtender):
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
                case nameof(SettingsViewModel.LoopFallbackScript):
                case nameof(SettingsViewModel.RemoveGapsFromFallbackScript):
                {
                    UpdateFallbackScriptModifiers();
                    break;
                }
                case nameof(SettingsViewModel.PatternSpeed):
                    {
                        UpdatePatternSpeed();
                        break;
                    }
                case nameof(SettingsViewModel.ShowTimeLeft):
                case nameof(SettingsViewModel.LimitDisplayedTimeToSelection):
                case nameof(SettingsViewModel.TimeDisplayMode):
                    {
                        UpdateDisplayedDuration();
                        break;
                    }
                case nameof(SettingsViewModel.UseExternalOsd):
                {
                    UpdateOsd();
                        break;
                }
                case nameof(SettingsViewModel.AutogenerateAllForPlaylist):
                {
                    CheckAutoGenerateAll();
                    break;
                }
                case nameof(SettingsViewModel.CropRect):
                case nameof(SettingsViewModel.CropVideo):
                {
                    UpdateVideoCrop();
                    break;
                }
                case nameof(SettingsViewModel.AutoReloadScript):
                {
                    UpdateAutoReloadScript();
                    break;
                }
                case nameof(SettingsViewModel.EstimAudioDevice):
                {
                    ReloadAudio();
                    break;
                }
                case nameof(SettingsViewModel.AudioDelay):
                {
                    UpdateAudioDelay();
                    break;
                }
            }
        }

        private void UpdateAudioDelay()
        {
            if (_audioHandler == null)
                return;

            _audioHandler.Delay = Settings.AudioDelay;
        }

        private void ReloadAudio()
        {
            TryFindMatchingAudio(LoadedVideo);
            if(TimeSource?.IsPlaying ?? false)
                _audioHandler?.Play();

            UpdateAudioDelay();
        }

        private void UpdateAutoReloadScript()
        {
            StopFileWatcher();

            if (!Settings.AutoReloadScript)
                return;

            if(!string.IsNullOrEmpty(LoadedScript))
                StartFileWatcher(LoadedScript);
        }

        private void StartFileWatcher(string file)
        {
            string path = Path.GetDirectoryName(file);
            string filename = Path.GetFileName(file);
            _fileWatcher = new FileSystemWatcher(path, filename);
            _fileWatcher.Changed += FileWatcherOnChanged;
            _fileWatcher.Created += FileWatcherOnChanged;
            _fileWatcher.Deleted += FileWatcherOnChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Thread.Sleep(100);
            if (Application.Current.Dispatcher != null)
                Application.Current.Dispatcher.BeginInvoke(new Action(ReloadScript));
        }

        private void StopFileWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }
        }

        private void UpdateVideoCrop()
        {
            if(VideoPlayer != null)
                VideoPlayer.ViewPort = Settings.CropVideo ? Settings.CropRect : Rect.Empty;
        }

        private void UpdateOsd()
        {
            RefreshOsds();
        }

        private void UpdatePatternSpeed()
        {
            if (_pattern is EasyGridPatternGenerator gen)
                gen.Duration = Settings.PatternSpeed;
        }

        private void UpdateFillGaps()
        {
            _scriptHandler.RangeExtender = Settings.RangeExtender;
            _scriptHandler.FillGapIntervall = Settings.FillGapIntervall;
            _scriptHandler.FillGapGap = Settings.FillGapGap;
            _scriptHandler.MinGapDuration = Settings.MinGapDuration;
            _scriptHandler.FillFirstGap = Settings.FillFirstGap;
            _scriptHandler.FillLastGap = Settings.FillLastGap;
            _scriptHandler.FillGaps = Settings.FillGaps;

            RefreshChapters();
        }

        private void UpdateFallbackScriptModifiers()
        {
            _scriptHandler.SquashScriptGaps = Settings.RemoveGapsFromFallbackScript;
            _scriptHandler.LoopScriptToDuration = Settings.LoopFallbackScript;

            RefreshChapters();
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
            WorkQueue.Dispose();

            Playlist.Dispose();

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

        public event EventHandler<RequestEventArgs<string>> RequestFolder;
        public event EventHandler<RequestFileEventArgs> RequestFile;
        public event EventHandler<MessageBoxEventArgs> RequestMessageBox;
        public event EventHandler<ButtplugUrlRequestEventArgs> RequestButtplugUrl;
        public event EventHandler RequestToggleFullscreen;
        public event EventHandler<bool> RequestSetFullscreen;

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

        public void ToggleCommandSource()
        {
            if (CommandSource != CommandSource.None)
            {
                _previousCommandSource = CommandSource;
                CommandSource = CommandSource.None;
            }
            else
            {
                CommandSource = _previousCommandSource;
            }
        }

        private void UpdateDeviceSettings()
        {
            _scriptHandler.MinIntermediateCommandDuration = Settings.CommandDelay;
            foreach (Device device in _devices)
            {
                device.SetMinCommandDelay(Settings.CommandDelay);
            }

            foreach (var buttplugAdapter in _controllers.OfType<ButtplugAdapter>())
            {
                buttplugAdapter.VibratorConversionMode = Settings.VibratorConversionMode;
            }
        }

        public void OpenVideo()
        {
            string videoFilters =
                $"Video Files|{string.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"))}|" +
                "All Files|*.*";

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
                    OsdShowMessage("Play", TimeSpan.FromSeconds(2), "Playback");
            }
            else if (Playlist.EntryCount > 0)
            {
                Playlist.PlayNextEntry();
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
                TimeSource.Pause();
                
                if (Settings.NotifyPlayPause)
                    OsdShowMessage("Pause", TimeSpan.FromSeconds(2), "Playback");
            }
        }

        private void AutoHomeDevices()
        {
            if (!Settings.AutoHomingEnabled) return;

            TimeSpan duration = TimeSpan.FromMilliseconds(500);
            byte currentPos = (byte)(CurrentPosition*100.0);
            byte homePos = 0;
            DeviceCommandInformation homeInfo = new DeviceCommandInformation
            {
                Duration = duration,
                DurationStretched = duration.Divide(TimeSource.PlaybackRate),
                PlaybackRate = TimeSource.PlaybackRate,
                PositionFromOriginal = currentPos,
                PositionToOriginal = homePos,
                PositionFromTransformed = TransformPosition(currentPos, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                PositionToTransformed = TransformPosition(homePos, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                SpeedMultiplier = Settings.SpeedMultiplier,
                SpeedMin = Settings.MinSpeed / 99.0,
                SpeedMax = Settings.MaxSpeed / 99.0,
            };

            homeInfo.SpeedOriginal = SpeedPredictor.PredictSpeed2(homeInfo.PositionFromOriginal, homeInfo.PositionToOriginal, duration);
            homeInfo.SpeedTransformed = ClampSpeed(SpeedPredictor.PredictSpeed2(homeInfo.PositionFromTransformed, homeInfo.PositionToTransformed, duration));

            SetDevices(homeInfo, false);
        }

        private void TryFindMatchingThumbnails(string videoFileName, bool generateIfMissing)
        {
            if (string.IsNullOrEmpty(videoFileName))
                return;

            Thumbnails = null;
            string thumbnailFile = FileFinder.FindFile(videoFileName, new[] { "thumbs" }, GetAdditionalPaths());

            if (string.IsNullOrWhiteSpace(thumbnailFile))
            {
                if (Settings.AutogenerateThumbnails && generateIfMissing)
                    GenerateThumbnails(false, false, videoFileName);
                return;
            }

            LoadThumbnails(thumbnailFile);
        }

        private void LoadThumbnails(string thumbnailFile)
        {
            try
            {
                VideoThumbnailCollection thumbnails = new VideoThumbnailCollection();
                using (FileStream stream = new FileStream(thumbnailFile, FileMode.Open, FileAccess.Read))
                    thumbnails.Load(stream);

                Thumbnails = thumbnails;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in LoadThumbnails (" + thumbnailFile + "): " + ex.Message);
            }
        }

        private void TryFindMatchingScript(string videoFileName)
        {
            if (IsMatchingScriptLoaded(videoFileName))
            {
                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OsdShowMessage("Matching script alreadly loaded", TimeSpan.FromSeconds(6));
                return;
            }

            string scriptFile = FileFinder.FindFile(videoFileName, GetScriptExtensions(), GetAdditionalPaths());
            if (string.IsNullOrWhiteSpace(scriptFile))
            {
                if (Settings.NotifyFileLoaded)
                    OsdShowMessage($"No script for '{Path.GetFileName(videoFileName)}' found!", TimeSpan.FromSeconds(6));

                ExecuteFallbackBehaviour();
                return;
            }

            LoadScriptOrFallback(scriptFile, false);
        }

        private string[] GetScriptExtensions()
        {
            return SortExtensionsByPreference(ScriptLoaderManager.GetSupportedExtensions(), Settings.ScriptFormatPreference);
        }

        private string[] SortExtensionsByPreference(string[] extensions, string preference)
        {
            string[] preferences;

            if (string.IsNullOrWhiteSpace(preference))
                preferences = new string[0];
            else
                preferences = preference
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ext => ext.TrimStart('.').ToLowerInvariant())
                .ToArray();

            return extensions
                .OrderBy(ext => preferences.Contains(ext) ? Array.IndexOf(preferences, ext) : int.MaxValue)
                .ThenBy(ext => ext).ToArray();
        }

        private string[] GetAdditionalPaths()
        {
            string[] paths = Settings?.AdditionalPaths?.ToArray();
            if (paths == null)
                return null;

            List<string> result = new List<string>();

            foreach (string path in paths)
            {
                result.AddRange(ExpandPath(path));
            }

            return result.ToArray();
        }

        private List<string> ExpandPath(string path)
        {
            List<string> result = new List<string>();

            if (path.EndsWith("*"))
            {
                string actualPath = path.Substring(0, path.Length - 1);

                result.AddRange(GetSubPaths(actualPath));
            }
            else
            {
                result.Add(path);
            }

            return result;
        }

        private static IEnumerable<string> GetSubPaths(string path)
        {
            List<string> result = new List<string>();
            try
            {
                if (Directory.Exists(path))
                {
                    result.Add(path);

                    foreach (string directory in Directory.EnumerateDirectories(path))
                    {
                        result.AddRange(GetSubPaths(directory));
                    }
                }
            }
            catch { }

            return result;
        }

        private void TryFindMatchingVideo(string scriptFileName)
        {
            if (IsMatchingVideoLoaded(scriptFileName))
            {
                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OsdShowMessage("Matching media file alreadly loaded", TimeSpan.FromSeconds(6));
                return;
            }

            string videoFile = FileFinder.FindFile(scriptFileName, GetMediaExtensions(), GetAdditionalPaths());
            if (string.IsNullOrWhiteSpace(videoFile))
            {
                if (Settings.NotifyFileLoaded)
                    OsdShowMessage($"No media file for '{Path.GetFileName(scriptFileName)}' found!", TimeSpan.FromSeconds(6));
                return;
            }

            LoadVideo(videoFile, false);
        }

        private string[] GetMediaExtensions()
        {
            return SortExtensionsByPreference(_supportedMediaExtensions, Settings.MediaFormatPreference);
        }

        private string[] GetVideoExtensions()
        {
            return SortExtensionsByPreference(_supportedVideoExtensions, Settings.MediaFormatPreference);
        }

        public string GetVideoFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

            if (_supportedVideoExtensions.Contains(extension))
                return fileName;

            string videoFile = FileFinder.FindFile(fileName, GetVideoExtensions(), GetAdditionalPaths());
            if (string.IsNullOrWhiteSpace(videoFile))
                return null;

            return videoFile;
        }

        public string GetRelatedFile(string filename, string[] supportedOutput, string[] supportedInput = null)
        {
            string extension = Path.GetExtension(filename);
            if (string.IsNullOrWhiteSpace(extension))
                return null;

            extension = extension.TrimStart('.').ToLower();

            if (supportedOutput.Contains(extension))
                return filename;

            if (supportedInput != null && !supportedInput.Contains(extension))
                return null;

            return FileFinder.FindFile(filename, supportedOutput, GetAdditionalPaths());
        }

        public string GetMediaFile(string filename)
        {
            return GetRelatedFile(filename, GetMediaExtensions(), GetScriptExtensions());
        }

        public string GetScriptFile(string filename)
        {
            return GetRelatedFile(filename, GetScriptExtensions(), GetMediaExtensions());
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
                            DurationStretched = transistion.Duration.Divide(TimeSource.PlaybackRate),
                            PlaybackRate = TimeSource.PlaybackRate,
                            PositionFromOriginal = transistion.From,
                            PositionToOriginal = transistion.To,
                            PositionFromTransformed = TransformPosition(transistion.From, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                            PositionToTransformed = TransformPosition(transistion.To, 0, 99, DateTime.Now.TimeOfDay.TotalSeconds),
                            SpeedMultiplier = Settings.SpeedMultiplier,
                            SpeedMin = Settings.MinSpeed / 99.0,
                            SpeedMax = Settings.MaxSpeed / 99.0,
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

        private void UpdatePattern(CommandSource commandSource)
        {
            _scriptHandler.FillGapPattern = SelectedPattern;

            switch (commandSource)
            {
                case CommandSource.Video:
                    StopPattern();
                    break;
                case CommandSource.None:
                    StopPattern();
                    break;
                case CommandSource.Pattern:
                    StartPattern(new EasyGridPatternGenerator(SelectedPattern, Settings.PatternSpeed));
                    break;
                case CommandSource.Random:
                    StartPattern(new RandomPatternGenerator());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(commandSource), commandSource, null);
            }
        }

        private void InitializeCommands()
        {
            SetTimeDisplayModeCommand = new RelayCommand<TimeDisplayMode>(SetTimeDisplayMode);

            SetShowTimeLeftCommand = new RelayCommand<bool>(SetShowTimeLeft);

            ShiftScriptCommand = new ScriptplayerCommand(ShiftScript, IsScriptLoaded);

            TrimScriptCommand = new ScriptplayerCommand(TrimScript, IsScriptLoaded);

            ShowGeneratorSettingsCommand = new ScriptplayerCommand(ModifyGeneratorSettings);

            ShowGeneratorProgressCommand = new ScriptplayerCommand(ShowGeneratorProgress);

            SaveScriptAsCommand = new ScriptplayerCommand(SaveScriptAs, IsScriptLoaded)
            {
                CommandId = "SaveScriptAs",
                DisplayText = "Save Script As"
            };

            ShowSettingsCommand = new ScriptplayerCommand(ShowSettings)
            {
                CommandId = "ShowSettings",
                DisplayText = "Show Settings"
            };

            ShowDeviceManagerCommand = new ScriptplayerCommand(ShowDeviceManager)
            {
                CommandId = "ShowDeviceManager",
                DisplayText = "Show Device Manager"
            };

            GenerateThumbnailsForLoadedVideoCommand = new ScriptplayerCommand(GenerateThumbnailsForLoadedVideo,
                IsVideoLoaded)
            {
                CommandId = "GenerateThumbnailsForLoadedVideo",
                DisplayText = "Generate Thumbnails for loaded Video"
            };

            GenerateThumbnailBannerForLoadedVideoCommand = new ScriptplayerCommand(GenerateThumbnailBannerForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateThumbnailBannerForLoadedVideo",
                DisplayText = "Generate Thumbnail Banner for loaded Video"
            };

            GeneratePreviewForLoadedVideoCommand = new ScriptplayerCommand(GeneratePreviewForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GeneratePreviewForLoadedVideo",
                DisplayText = "Generate Preview GIF for loaded Video"
            };

            GenerateHeatmapForLoadedVideoCommand = new ScriptplayerCommand(GenerateHeatmapForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateHeatmapForLoadedVideo",
                DisplayText = "Generate Heatmap for loaded Video"
            };

            GenerateAllForLoadedVideoCommand = new ScriptplayerCommand(GenerateAllForLoadedVideo, IsVideoLoaded)
            {
                CommandId = "GenerateAllForLoadedVideo",
                DisplayText = "Generate All for loaded Video"
            };

            OpenScriptCommand = new ScriptplayerCommand(OpenScript)
            {
                CommandId = "OpenScriptFile",
                DisplayText = "Open Script"
            };

            OpenVideoCommand = new ScriptplayerCommand(OpenVideo)
            {
                CommandId = "OpenVideoFile",
                DisplayText = "Open Video"
            };

            AddScriptsToPlaylistCommand = new ScriptplayerCommand(AddFileToPlaylist)
            {
                CommandId = "AddFileToPlaylist",
                DisplayText = "Add File To Playlist"
            };

            AddFolderToPlaylistCommand = new ScriptplayerCommand(AddFolderToPlaylist)
            {
                CommandId = "AddFolderToPlaylist",
                DisplayText = "Add Folder To Playlist"
            };

            ConnectLaunchDirectlyCommand = new ScriptplayerCommand(ConnectLaunchDirectly)
            {
                CommandId = "ConnectLaunchDirectly",
                DisplayText = "Connect Launch Directly"
            };

            ConnectHandyDirectlyCommand = new ScriptplayerCommand(ConnectHandyDirectly)
            {
                CommandId = "ConenctHandyDirectly",
                DisplayText = "Connect to Handy directly"
            };

            AddEstimAudioCommand = new ScriptplayerCommand(AddEstimAudioDevice)
            {
                CommandId = "AddEstimAudioDevice",
                DisplayText = "Add E-Stim Audio Device"
            };

            ConnectButtplugCommand = new ScriptplayerCommand(ConnectButtplug)
            {
                CommandId = "ConnectButtplug",
                DisplayText = "Connect Buttplug"
            };

            DisconnectButtplugCommand = new ScriptplayerCommand(DisconnectButtplug)
            {
                CommandId = "DisconnectButtplug",
                DisplayText = "Disconnect Buttplug"
            };

            StartScanningButtplugCommand = new ScriptplayerCommand(StartScanningButtplug)
            {
                CommandId = "StartScanningButtplug",
                DisplayText = "Start Scanning Buttplug"
            };

            SkipToNextEventCommand = new ScriptplayerCommand(SkipToNextEvent, CanSkipToNextEvent)
            {
                CommandId = "SkipToNextEvent",
                DisplayText = "Skip To Next Event"
            };

            TogglePlaybackCommand = new ScriptplayerCommand(TogglePlayback, CanTogglePlayback)
            {
                CommandId = "TogglePlayback",
                DisplayText = "Toggle Play / Pause",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Space, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaPlayPause, ModifierKeys.None, true) 
                }
            };

            VolumeUpCommand = new ScriptplayerCommand(VolumeUp)
            {
                CommandId = "VolumeUp",
                DisplayText = "Volume Up",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Up, ModifierKeys.None) }
            };

            VolumeDownCommand = new ScriptplayerCommand(VolumeDown)
            {
                CommandId = "VolumeDown",
                DisplayText = "Volume Down",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Down, ModifierKeys.None) }
            };

            ExecuteSelectedTestPatternCommand = new ScriptplayerCommand(ExecuteSelectedTestPattern, CanExecuteSelectedTestPattern);
            ToggleFullScreenCommand = new ScriptplayerCommand(ExecuteToggleFullScreen)
            {
                CommandId = "ToggleFullscreen",
                DisplayText = "Toggle Fullscreen",
                DefaultShortCuts = { GlobalCommandManager.GetShortcut(Key.Enter, ModifierKeys.None) }
            };

            LoadPlaylistCommand = new ScriptplayerCommand(ExecuteLoadPlaylist);
            SavePlaylistCommand = new ScriptplayerCommand(ExecuteSavePlaylist);
            RemoveMissingEntriesFromPlaylistCommand = new ScriptplayerCommand(ExecuteRemoveMissingEntriesFromPlaylist);
            RemoveIncompleteEntriesFromPlaylistCommand = new ScriptplayerCommand(ExecuteRemoveIncompleteEntriesFromPlaylist);

            SetLoopACommand = new ScriptplayerCommand(ExecuteSetLoopA);
            SetLoopBCommand = new ScriptplayerCommand(ExecuteSetLoopB);
            ClearLoopCommand = new ScriptplayerCommand(ExecuteClearLoop);

            GlobalCommandManager.RegisterCommand(OpenScriptCommand);
            GlobalCommandManager.RegisterCommand(OpenVideoCommand);
            GlobalCommandManager.RegisterCommand(AddScriptsToPlaylistCommand);
            GlobalCommandManager.RegisterCommand(ConnectLaunchDirectlyCommand);
            GlobalCommandManager.RegisterCommand(ConnectHandyDirectlyCommand);
            GlobalCommandManager.RegisterCommand(ConnectButtplugCommand);
            GlobalCommandManager.RegisterCommand(DisconnectButtplugCommand);
            GlobalCommandManager.RegisterCommand(StartScanningButtplugCommand);
            GlobalCommandManager.RegisterCommand(SkipToNextEventCommand);
            GlobalCommandManager.RegisterCommand(TogglePlaybackCommand);
            GlobalCommandManager.RegisterCommand(VolumeUpCommand);
            GlobalCommandManager.RegisterCommand(VolumeDownCommand);
            GlobalCommandManager.RegisterCommand(ToggleFullScreenCommand);

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(ToggleCommandSourceVideoPattern)
            {
                CommandId = "ToggleCommandSourceVideoPattern",
                DisplayText = "Toggle Source Video/Pattern"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(ToggleCommandSourceVideoNone)
            {
                CommandId = "ToggleCommandSourceVideoNone",
                DisplayText = "Toggle Source Video/None",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.S, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaStop, ModifierKeys.None, true) 
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseUpperRange)
            {
                CommandId = "IncreaseUpperRange",
                DisplayText = "Increase Upper Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseUpperRange)
            {
                CommandId = "DecreaseUpperRange",
                DisplayText = "Decrease Upper Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseLowerRange)
            {
                CommandId = "IncreaseLowerRange",
                DisplayText = "Increase Lower Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseLowerRange)
            {
                CommandId = "DecreaseLowerRange",
                DisplayText = "Decrease Lower Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreasePlaybackSpeed)
            {
                CommandId = "IncreasePlaybackRate",
                DisplayText = "Increase Playback Rate"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreasePlaybackSpeed)
            {
                CommandId = "DecreasePlaybackRate",
                DisplayText = "Decrease Playback Rate"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseFilterRange)
            {
                CommandId = "IncreaseFilterRange",
                DisplayText = "Increase Filter Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseFilterRange)
            {
                CommandId = "DecreaseFilterRange",
                DisplayText = "Decrease Filter Range"
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseScriptDelay)
            {
                CommandId = "IncreaseScriptDelay",
                DisplayText = "Increase Script Delay",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.OemPlus, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.Add, ModifierKeys.None)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseScriptDelay)
            {
                CommandId = "DecreaseScriptDelay",
                DisplayText = "Decrease Script Delay",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.OemMinus, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.Subtract, ModifierKeys.None)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(IncreaseAudioDelay)
            {
                CommandId = "IncreaseAudioDelay",
                DisplayText = "Increase Audio Delay",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(DecreaseAudioDelay)
            {
                CommandId = "DecreaseAudioDelay",
                DisplayText = "Decrease Audio Delay",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Play, CanTogglePlayback)
            {
                CommandId = "Play",
                DisplayText = "Play",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Play, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Pause, CanTogglePlayback)
            {
                CommandId = "Pause",
                DisplayText = "Pause",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Pause, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { OnRequestSetFullscreen(false); })
            {
                CommandId = "ExitFullscreen",
                DisplayText = "Exit Fullscreen",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Escape, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { OnRequestSetFullscreen(true); })
            {
                CommandId = "EnterFullscreen",
                DisplayText = "Enter Fullscreen",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { ShiftPosition(TimeSpan.FromSeconds(-5)); })
            {
                CommandId = "Seek[-5]",
                DisplayText = "Go back 5 seconds",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Left, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { ShiftPosition(TimeSpan.FromSeconds(5)); })
            {
                CommandId = "Seek[5]",
                DisplayText = "Go forwards 5 seconds",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.Right, ModifierKeys.None),
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(Mute)
            {
                CommandId = "Mute",
                DisplayText = "Mute",
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { Playlist.PlayNextEntry(); })
            {
                CommandId = "PlayNextEntry",
                DisplayText = "Play next playlist entry",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.PageDown, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaNextTrack, ModifierKeys.None, true)
                }
            });

            GlobalCommandManager.RegisterCommand(new ScriptplayerCommand(() => { Playlist.PlayPreviousEntry(); })
            {
                CommandId = "PlayPreviousEntry",
                DisplayText = "Play previous playlist entry",
                DefaultShortCuts =
                {
                    GlobalCommandManager.GetShortcut(Key.PageUp, ModifierKeys.None),
                    GlobalCommandManager.GetShortcut(Key.MediaPreviousTrack, ModifierKeys.None, true)
                }
            });
        }

        private void AddEstimAudioDevice()
        {
            var controller = _controllers.OfType<EStimAudioController>().FirstOrDefault();

            var devices = controller.GetAudioDevices();
            AudioDeviceSelector dialog = new AudioDeviceSelector(devices);

            if(dialog.ShowDialog() != true)
                return;

            controller.SetDevice(dialog.SelectedDevice, dialog.Parameters);
        }

        private void DecreaseFilterRange()
        {
            ChangeFilterRange(-0.05);
        }

        private void IncreaseFilterRange()
        {
            ChangeFilterRange(0.05);
        }

        private void ChangeFilterRange(double d)
        {
            Settings.FilterRange = Math.Max(0.1, Math.Min(0.9, Settings.FilterRange + d));
            OsdShowMessage($"Filter Range: {Settings.FilterRange:p0}", TimeSpan.FromSeconds(2), "FilterRange");
        }

        private void ShowGeneratorProgress()
        {
            OnRequestShowGeneratorProgressDialog();
        }

        private void PrintRange()
        {
            OsdShowMessage($"Range: {Settings.MinPosition} - {Settings.MaxPosition}", TimeSpan.FromSeconds(2), "Range");
        }

        private void DecreaseLowerRange()
        {
            Settings.MinPosition = (byte) Math.Max(0, Settings.MinPosition - 5);
            PrintRange();
        }

        private void IncreaseLowerRange()
        {
            Settings.MinPosition = (byte)Math.Min(Settings.MaxPosition, Settings.MinPosition + 5);
            PrintRange();
        }

        private void DecreaseUpperRange()
        {
            Settings.MaxPosition = (byte)Math.Max(Settings.MinPosition, Settings.MaxPosition - 5);
            PrintRange();
        }

        private void IncreaseUpperRange()
        {
            Settings.MaxPosition = (byte)Math.Min(99, Settings.MaxPosition + 5);
            PrintRange();
        }

        private void ShiftScript()
        {
            TimeSpan timeSpan = OnRequestScriptShiftTimespan(_previousShiftTimeSpan);
            if (timeSpan == TimeSpan.Zero)
                return;

            _previousShiftTimeSpan = timeSpan;
            _scriptHandler.ShiftScript(timeSpan);
            RefreshChapters();
        }

        private void TrimScript()
        {
            Section initialValue = DisplayedRange;

            if (initialValue == null)
            {
                initialValue = new Section(TimeSpan.Zero, TimeSource.Duration);
            }

            var section = OnRequestSection(initialValue);
            if (section == null)
                return;

            _scriptHandler.TrimScript(section.Start, section.End);
            RefreshChapters();
        }


        private void ShowDeviceManager()
        {
            OnRequestShowDeviceManager();
        }

        private bool IsScriptLoaded()
        {
            return _scriptHandler.IsScriptLoaded();
        }

        private void SaveScriptAs()
        {
            int filterId = 0;
            string fileName = OnRequestFile("Funscript|*.funscript", ref filterId, true);
            if (string.IsNullOrEmpty(fileName))
                return;

            _scriptHandler.SaveAs(fileName);
        }

        private void SetShowTimeLeft(bool showTimeLeft)
        {
            Settings.ShowTimeLeft = showTimeLeft;
        }

        private void SetTimeDisplayMode(TimeDisplayMode mode)
        {
            Settings.TimeDisplayMode = mode;
        }

        private void ExecuteRemoveMissingEntriesFromPlaylist()
        {
            var toRemove = Playlist.Entries.Where(entry => !File.Exists(entry.Fullname)).ToList();
            foreach (var entry in toRemove)
                Playlist.Entries.Remove(entry);
        }

        private void ExecuteRemoveIncompleteEntriesFromPlaylist()
        {
            var toRemove = Playlist.Entries.Where(entry => entry.Status == PlaylistEntryStatus.MissingFile).ToList();
            foreach (var entry in toRemove)
                Playlist.Entries.Remove(entry);
        }

        private void DecreaseScriptDelay()
        {
            if (Settings.ScriptDelay > TimeSpan.FromMilliseconds(-2500))
                Settings.ScriptDelay -= TimeSpan.FromMilliseconds(25);

            OsdShowMessage("Script Delay: " + Settings.ScriptDelay.TotalMilliseconds.ToString("F0") + " ms", TimeSpan.FromSeconds(2), "ScriptDelay");
        }

        private void IncreaseScriptDelay()
        {
            if (Settings.ScriptDelay < TimeSpan.FromMilliseconds(2500))
                Settings.ScriptDelay += TimeSpan.FromMilliseconds(25);

            OsdShowMessage("Script Delay: " + Settings.ScriptDelay.TotalMilliseconds.ToString("F0") + " ms", TimeSpan.FromSeconds(2), "ScriptDelay");
        }

        private void DecreaseAudioDelay()
        {
            if (Settings.AudioDelay > TimeSpan.FromMilliseconds(-2500))
                Settings.AudioDelay -= TimeSpan.FromMilliseconds(25);

            OsdShowMessage("Audio Delay: " + Settings.AudioDelay.TotalMilliseconds.ToString("F0") + " ms", TimeSpan.FromSeconds(2), "AudioDelay");
        }

        private void IncreaseAudioDelay()
        {
            if (Settings.AudioDelay < TimeSpan.FromMilliseconds(2500))
                Settings.AudioDelay += TimeSpan.FromMilliseconds(25);

            OsdShowMessage("Audio Delay: " + Settings.AudioDelay.TotalMilliseconds.ToString("F0") + " ms", TimeSpan.FromSeconds(2), "AudioDelay");
        }

        private void DecreasePlaybackSpeed()
        {
            double currentValue = Math.Round(TimeSource.PlaybackRate, 1);
            if (currentValue > 0.1)
                TimeSource.PlaybackRate = currentValue - 0.1;

            OsdShowMessage("Playback Rate: " + TimeSource.PlaybackRate.ToString("F1", CultureInfo.InvariantCulture), TimeSpan.FromSeconds(2), "PlaybackRate");
        }

        private void IncreasePlaybackSpeed()
        {
            double currentValue = Math.Round(TimeSource.PlaybackRate, 1);
            if (currentValue < 2.0)
                TimeSource.PlaybackRate = currentValue + 0.1;

            OsdShowMessage("Playback Rate: x" + TimeSource.PlaybackRate.ToString("F1"), TimeSpan.FromSeconds(2), "PlaybackRate");
        }

        private void ToggleCommandSourceVideoPattern()
        {
            if (CommandSource == CommandSource.Video)
                CommandSource = CommandSource.Pattern;
            else
                CommandSource = CommandSource.Video;
        }

        private void ToggleCommandSourceVideoNone()
        {
            if (CommandSource == CommandSource.Video)
                CommandSource = CommandSource.None;
            else
                CommandSource = CommandSource.Video;
        }

        private void ExecuteClearLoop()
        {
            _loopA = TimeSpan.MinValue;
            _loopB = TimeSpan.MinValue;
            UpdateDisplayedSelection();
        }

        private void ExecuteSetLoopB()
        {
            _loopB = TimeSource.Progress;
            UpdateDisplayedSelection();
        }

        private void ExecuteSetLoopA()
        {
            _loopA = TimeSource.Progress;
            UpdateDisplayedSelection();
        }

        private void UpdateDisplayedSelection()
        {
            if (_loopA != TimeSpan.MinValue && _loopB != TimeSpan.MinValue)
                DisplayedRange = new Section(_loopA, _loopB);
            else
                DisplayedRange = SelectedRange;

            UpdateDisplayedDuration();
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
                    DurationStretched = delay,
                    PlaybackRate = 1,
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
                    DurationStretched = delay,
                    PlaybackRate = 1,
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
                OsdShowMessage("Device Removed: " + device.Name, TimeSpan.FromSeconds(8));
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
                OsdShowMessage("Device Connected: " + device.Name, TimeSpan.FromSeconds(8));
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
            /*if (EntryLoaded())
                Play();*/
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        private void InitializeEstimController()
        {
            EStimAudioController audioController = _controllers.OfType<EStimAudioController>().FirstOrDefault();
            if (audioController != null) return;

            audioController = new EStimAudioController();
            audioController.DeviceFound += DeviceController_DeviceFound;
            _controllers.Add(audioController);
        }

        private void CheckForArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            ProcessArguments(args, true);
        }

        private void ProcessArguments(string[] args, bool original)
        {
            if (args.Length <= 1) return;

            if (!original)
                OnRequestActivate();

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
            else if (_supportedMediaExtensions.Contains(extension))
                LoadVideo(fileToLoad, true);
            else if (_supportedScriptExtensions.Contains(extension))
                LoadScriptOrFallback(fileToLoad, true);
        }

        private void LoadPlaylist(string filename = null)
        {
            if (string.IsNullOrWhiteSpace(filename))
                filename = GetDefaultPlaylistFile();

            M3uPlaylist playlist = M3uPlaylist.FromFile(filename);

            Playlist.Clear();

            if (playlist == null) return;

            Playlist.AddEntries(playlist.Entries.Select(ToPlaylistEntry));
        }

        private void LoadVideo(string videoFileName, bool checkForScript)
        {
            try
            {
                _loading = true;

                if (checkForScript)
                    TryFindMatchingScript(videoFileName);

                TryFindMatchingAudio(videoFileName);

                TryFindMatchingThumbnails(videoFileName, true);

                LoadedVideo = videoFileName;

                TimeSpan start = TimeSpan.Zero;
                SelectedRange = null;

                if (Settings.RandomChapters)
                {
                    var chapter = GetRandomChapter(Settings.ChapterMode);
                    SelectedRange = chapter;
                    start = chapter.Start - TimeSpan.FromSeconds(1);

                    Debug.WriteLine($"{DateTime.Now:T}: Selected Range = {chapter.Start:g} - {chapter.End:g}");
                }
                else if (Settings.AutoSkip)
                {
                    start = GetFirstEvent();
                }

                if (PlaybackMode == PlaybackMode.Local)
                {
                    HideBanner();
                    VideoPlayer.Open(videoFileName, start, Settings.SoftSeekFiles ? Settings.SoftSeekFilesDuration : TimeSpan.Zero);
                }
                
                Title = Path.GetFileNameWithoutExtension(videoFileName);

                if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                    OsdShowMessage($"Loaded {Path.GetFileName(videoFileName)}", TimeSpan.FromSeconds(4),
                        "VideoLoaded");

                //Play();

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

        private void TryFindMatchingAudio(string videoFile)
        {
            if (string.IsNullOrEmpty(videoFile))
            {
                DisposeAudioHandler();
                return;
            }

            if (string.IsNullOrEmpty(Settings.EstimAudioDevice))
            {
                DisposeAudioHandler();
                return;
            }

            string audioFile = GetRelatedFile(videoFile, _supportedAudioExtensions, _supportedVideoExtensions);
            if (!string.IsNullOrEmpty(audioFile))
            {
                LoadAudio(audioFile);
                UpdateAudioDelay();
            }
            else
            {
                DisposeAudioHandler();
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
            Brush heatmap = HeatMapGenerator.Generate2(timeStamps, _gapDuration, TimeSpan.Zero, TimeSource.Duration, TimeSource.PlaybackRate);
            HeatMap = heatmap;
        }

        //TODO very similar to HeatMapGenerator.GetSegments --> unify?
        private List<CommandSection> GetChapters(TimeSpan minChapterDuration, TimeSpan gapDuration, bool includePositions, bool includeFilledGaps = false)
        {
            List<CommandSection> result = new List<CommandSection>();

            if (TimeSource == null)
                return result;

            IEnumerable<ScriptAction> actions = includeFilledGaps ? _scriptHandler.GetScript() : _scriptHandler.GetUnfilledScript();

            List<TimeSpan> timeStamps = FilterDuplicates(actions.ToList()).Select(s => s.TimeStamp).ToList();

            if (timeStamps.Count < 2)
                return result;

            int chapterBegin = int.MinValue;
            int chapterEnd = int.MinValue;

            for (int index = 0; index < timeStamps.Count; index++)
            {
                if (chapterBegin == int.MinValue)
                {
                    chapterBegin = index;
                    chapterEnd = index;
                }
                else if (timeStamps[index] - timeStamps[chapterEnd] < gapDuration)
                {
                    chapterEnd = index;
                }
                else
                {
                    result.Add(new CommandSection(GetRange(timeStamps, chapterBegin, chapterEnd), includePositions));

                    chapterBegin = index;
                    chapterEnd = index;
                }
            }

            if (chapterBegin != int.MinValue && chapterEnd != int.MinValue)
            {
                result.Add(new CommandSection(GetRange(timeStamps, chapterBegin, chapterEnd), includePositions));
            }

            return result.Where(t => t.Duration >= minChapterDuration).ToList();
        }

        private List<T> GetRange<T>(List<T> source, int firstIndex, int lastIndex)
        {
            return source.Skip(firstIndex).Take(lastIndex - firstIndex + 1).ToList();
        }

        public List<ScriptAction> FilterDuplicates(List<ScriptAction> timestamps)
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
            _scriptHandler.InstantUpdateRaised += ScriptHandlerOnInstantUpdateRaised;
            _scriptHandler.Delay = TimeSpan.FromMilliseconds(0);
        }

        private void ScriptHandlerOnInstantUpdateRaised(object sender, IntermediateScriptActionEventArgs e)
        {
            OnIntermediateBeat(e.Progress);
        }

        private void ScriptHandlerOnPositionsChanged(object sender, PositionCollection positionCollection)
        {
            Positions = positionCollection;
        }

        private void ScriptHandlerOnIntermediateScriptActionRaised(object sender, IntermediateScriptActionEventArgs eventArgs)
        {
            if (CommandSource != CommandSource.Video)
                return;

            if (eventArgs.RawPreviousAction is FunScriptAction)
                HandleIntermediateFunScriptAction(eventArgs.Cast<FunScriptAction>());
        }

        private void HandleIntermediateFunScriptAction(IntermediateScriptActionEventArgs<FunScriptAction> e)
        {
            TimeSpan duration = e.NextAction.TimeStamp - e.PreviousAction.TimeStamp;
            TimeSpan durationStretched = duration.Divide(TimeSource.PlaybackRate);
            byte currentPositionTransformed = TransformPosition(e.PreviousAction.Position, e.PreviousAction.TimeStamp);
            byte nextPositionTransformed = TransformPosition(e.NextAction.Position, e.NextAction.TimeStamp);

            CurrentPosition = (1 - e.Progress) * (currentPositionTransformed / 99.0) + (e.Progress) * (nextPositionTransformed / 99.0);

            if (currentPositionTransformed == nextPositionTransformed) return;

            byte speedOriginal =
                SpeedPredictor.PredictSpeed(
                    (byte)Math.Abs(e.PreviousAction.Position - e.NextAction.Position), durationStretched);
            byte speedTransformed =
                SpeedPredictor.PredictSpeed((byte)Math.Abs(currentPositionTransformed - nextPositionTransformed), durationStretched);
            speedTransformed = ClampSpeed(speedTransformed * Settings.SpeedMultiplier);

            //Debug.WriteLine($"{nextPositionTransformed} @ {speedTransformed}");

            DeviceCommandInformation info = new DeviceCommandInformation
            {
                Duration = duration,
                DurationStretched = durationStretched,
                PlaybackRate = TimeSource.PlaybackRate,
                SpeedTransformed = speedTransformed,
                SpeedOriginal = speedOriginal,
                PositionFromTransformed = currentPositionTransformed,
                PositionToTransformed = nextPositionTransformed,
                PositionFromOriginal = e.PreviousAction.Position,
                PositionToOriginal = e.NextAction.Position,
                SpeedMultiplier = Settings.SpeedMultiplier,
                SpeedMin = Settings.MinSpeed / 99.0,
                SpeedMax = Settings.MaxSpeed / 99.0
            };

            IntermediateCommandInformation intermediateInfo = new IntermediateCommandInformation
            {
                DeviceInformation = info,
                Progress = e.Progress
            };

            SetDevices(intermediateInfo);
        }

        private void ScriptHandlerOnScriptActionRaised(object sender, ScriptActionEventArgs eventArgs)
        {
            if (CommandSource != CommandSource.Video)
                return;

            if (_loading)
                return;

            if (eventArgs.RawCurrentAction is FunScriptAction)
                HandleFunScriptAction(eventArgs.Cast<FunScriptAction>());
        }

        private void HandleFunScriptAction(ScriptActionEventArgs<FunScriptAction> eventArgs)
        {
            if (IsSeeking) return;

            SkipState skipState;
            TimeSpan timeToNextOriginalEvent = TimeSpan.Zero;

            OnBeat();

            if (eventArgs.NextAction == null)
            {
                // Script Ended
                skipState = Playlist.CanPlayNextEntry() ? SkipState.EndNext : SkipState.End;
            }
            else
            {
                skipState = SkipState.Available;

                // Determine next movement

                TimeSpan duration = eventArgs.NextAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
                TimeSpan durationStretched = duration.Divide(TimeSource.PlaybackRate);

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
                            durationStretched);
                    byte speedTransformed =
                        SpeedPredictor.PredictSpeed(
                            (byte)Math.Abs(currentPositionTransformed - nextPositionTransformed),
                            durationStretched);
                    speedTransformed = ClampSpeed(speedTransformed * Settings.SpeedMultiplier);

                    DeviceCommandInformation info = new DeviceCommandInformation
                    {
                        Duration = duration,
                        DurationStretched = durationStretched,
                        PlaybackRate = TimeSource.PlaybackRate,
                        SpeedTransformed = speedTransformed,
                        SpeedOriginal = speedOriginal,
                        PositionFromTransformed = currentPositionTransformed,
                        PositionToTransformed = nextPositionTransformed,
                        PositionFromOriginal = eventArgs.CurrentAction.Position,
                        PositionToOriginal = eventArgs.NextAction.Position,
                        SpeedMultiplier = Settings.SpeedMultiplier,
                        SpeedMin = Settings.MinSpeed / 99.0,
                        SpeedMax = Settings.MaxSpeed / 99.0,
                    };

                    SetDevices(info);
                }

                if (eventArgs.NextAction.OriginalAction)
                {
                    timeToNextOriginalEvent = duration;

                    if (SelectedRange != null && eventArgs.NextAction.TimeStamp > SelectedRange.End)
                    {
                        Debug.WriteLine($"{DateTime.Now:T}: Selected Range about to end");
                        skipState = SkipState.Gap;
                    }
                    else if (timeToNextOriginalEvent >= _gapDuration)
                    {
                        Debug.WriteLine($"{DateTime.Now:T}: Gap Detected");
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
                        skipState = Playlist.CanPlayNextEntry() ? SkipState.EndFillerNext : SkipState.EndFiller;
                    }
                    else
                    {
                        timeToNextOriginalEvent = nextOriginalAction.TimeStamp - eventArgs.CurrentAction.TimeStamp;
                        skipState = timeToNextOriginalEvent >= _gapDuration ? SkipState.FillerGap : SkipState.Filler;
                    }
                }
            }

            if (!TimeSource.IsPlaying || IsTransistioning)
                return;

            if (skipState != SkipState.Available)
            {
                Debug.WriteLine("SkipState = " + skipState + "@" + eventArgs.CurrentAction.TimeStamp.ToString("g"));
            }

            switch (skipState)
            {
                case SkipState.Available:
                    {
                        OsdHideSkipButton();
                        break;
                    }
                case SkipState.Gap:
                    {
                        if (Settings.AutoSkip || Settings.RandomChapters)
                        {
                            SkipToNextEvent();
                        }
                        else
                        {
                            if(!Settings.FillGaps)
                            {
                                AutoHomeDevices();
                            }
                            if (Settings.NotifyGaps)
                                OsdShowMessage($"Next event in {timeToNextOriginalEvent.TotalSeconds:f0}s",
                                    TimeSpan.FromSeconds(4), "Events");
                            if (Settings.ShowSkipButton)
                                OsdShowSkipButton();
                        }
                        break;
                    }
                case SkipState.Filler:
                    {
                        OsdHideSkipButton();
                        break;
                    }
                case SkipState.FillerGap:
                    {
                        if (Settings.NotifyGaps)
                            OsdShowMessage($"Next original event in {timeToNextOriginalEvent.TotalSeconds:f0}s", TimeSpan.FromSeconds(4), "Events");
                        if (Settings.ShowSkipButton)
                            OsdShowSkipButton();
                        break;
                    }
                case SkipState.EndFillerNext:
                    {
                        if (Settings.NotifyGaps)
                            OsdShowMessage("No more original events available", TimeSpan.FromSeconds(4), "Events");
                        if (Settings.ShowSkipButton)
                            OsdShowSkipNextButton();
                        break;
                    }
                case SkipState.EndFiller:
                    {
                        if (Settings.NotifyGaps)
                            OsdShowMessage("No more original events available", TimeSpan.FromSeconds(4), "Events");
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
                                OsdShowMessage("No more events available", TimeSpan.FromSeconds(4), "Events");
                            if (Settings.ShowSkipButton)
                                OsdShowSkipNextButton();
                        }
                        break;
                    }
                case SkipState.End:
                    {
                        if (Settings.NotifyGaps)
                            OsdShowMessage("No more events available", TimeSpan.FromSeconds(4), "Events");
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

            CurrentPosition = information.PositionFromTransformed / 99.0;

            foreach (Device device in _devices)
                device.Enqueue(information);
        }

        private void SetDevices(IntermediateCommandInformation intermediateInfo, bool requirePlaying = true)
        {
            //TODO Handle this better!
            // (Hush is too slow)

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

        protected virtual string OnRequestFolder(string initialPath)
        {
            RequestEventArgs<string> e = new RequestEventArgs<string>(initialPath);

            RequestFolder?.Invoke(this, e);

            if (e.Handled)
                return e.Value;

            return null;
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
            MediaCanBeConsideredEnded();
        }

        private void MediaCanBeConsideredEnded()
        {
            if (IsSeeking || IsTransistioning)
                return;

            StopDevices();
            PlayNextPlaylistEntry();
        }

        private void PlayNextPlaylistEntry()
        {
            if (!TimeSource.CanOpenMedia) return;
            Playlist.PlayNextEntry();
        }

        private void PlayPreviousPlaylistEntry()
        {
            if (!TimeSource.CanOpenMedia) return;
            Playlist.PlayPreviousEntry();
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
                    OsdShowMessage("Logged marker at " + line, TimeSpan.FromSeconds(5), "Log");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void SkipToNextEvent()
        {
            if (Settings.RandomChapters)
                Playlist.PlayNextEntry();
            else
                SkipToNextEventInternal();
        }

        private TimeSpan TranslateMediaPosition(List<Section> sections, TimeSpan rawProgress, TimeDisplayMode mode)
        {
            if (Settings.LimitDisplayedTimeToSelection && SelectedRange != null)
            {
                sections = CutSections(sections, SelectedRange);
                rawProgress = rawProgress - SelectedRange.Start;
            }

            switch (mode)
            {
                case TimeDisplayMode.Original:
                    return rawProgress;
                case TimeDisplayMode.ContentAndGaps:
                    {
                        if (sections.Count == 0)
                            return TimeSpan.Zero;

                        if (rawProgress < sections.First().Start)
                            return TimeSpan.Zero;

                        if (rawProgress > sections.Last().End)
                            return sections.Last().End;

                        return rawProgress - sections.First().Start;
                    }
                case TimeDisplayMode.ContentOnly:
                    {
                        if (sections.Count == 0)
                            return TimeSpan.Zero;

                        if (rawProgress < sections.First().Start)
                            return TimeSpan.Zero;

                        TimeSpan total = TimeSpan.Zero;

                        foreach (Section section in sections)
                        {
                            if (section.Start >= rawProgress)
                                break;

                            if (section.End <= rawProgress)
                                total += section.Duration;
                            else
                                total += rawProgress - section.Start;
                        }

                        return total;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        private List<Section> CutSections(List<Section> sections, Section range)
        {
            List<Section> newSections = new List<Section>();

            foreach (Section section in sections)
            {
                if (section.End <= range.Start)
                    continue;

                if (section.Start >= range.End)
                    continue;

                TimeSpan start = section.Start < range.Start ? TimeSpan.Zero : section.Start - range.Start;
                TimeSpan end = section.End > range.End ? range.Duration : section.End - range.Start;

                newSections.Add(new Section(start, end));
            }

            return newSections;
        }

        private Section GetRandomChapter(ChapterMode mode)
        {
            TimeSpan duration = Settings.ChapterTargetDuration;
            var minDuration = TimeSpan.FromSeconds(30);

            if (duration < minDuration)
                minDuration = duration.Multiply(0.9);

            bool includeFilledGaps = mode == ChapterMode.RandomTimeSpan &&
                               Settings.IncludeFilledGapsInRandomSelection && 
                               Settings.FillGaps;

            var chapters = GetChapters(minDuration, _gapDuration, true, includeFilledGaps);
            Random r = new Random();

            if (chapters.Count == 0)
            {
                chapters = GetChapters(TimeSpan.FromSeconds(1), _gapDuration, true);
            }

            switch (mode)
            {
                case ChapterMode.RandomChapter:
                    {
                        return chapters[r.Next(chapters.Count)];
                    }
                case ChapterMode.FastestChapter:
                    {
                        return GetFastestChapter(chapters);
                    }
                case ChapterMode.FastestChapterLimitedDuration:
                    {
                        var fastestChapter = GetFastestChapter(chapters);

                        if (fastestChapter.Duration <= duration)
                            return fastestChapter;

                        Section fastestSection = GetFastestSection(new List<CommandSection> { fastestChapter },
                            duration.Multiply(0.8), duration);

                        if (!fastestSection.IsEmpty)
                            return fastestSection;

                        TimeSpan maxOffset = fastestChapter.Duration - duration;

                        TimeSpan start = fastestChapter.Start + TimeSpan.FromSeconds(r.NextDouble() * maxOffset.TotalSeconds);
                        return new Section(start, start + duration);
                    }
                case ChapterMode.RandomChapterLimitedDuration:
                    {
                        var chapter = chapters[r.Next(chapters.Count)];

                        if (chapter.Duration <= duration)
                            return chapter;

                        TimeSpan maxOffset = chapter.Duration - duration;

                        TimeSpan start = chapter.Start + TimeSpan.FromSeconds(r.NextDouble() * maxOffset.TotalSeconds);
                        return new Section(start, start + duration);
                    }
                case ChapterMode.RandomTimeSpan:
                    {
                        var longEnoughChapters = chapters.Where(c => c.Duration >= duration).ToList();
                        if (longEnoughChapters.Count == 0)
                            return chapters.FirstOrDefault(c => c.Duration == chapters.Max(c2 => c2.Duration));

                        var chapter = chapters[r.Next(chapters.Count)];
                        TimeSpan maxOffset = chapter.Duration - duration;

                        TimeSpan start = chapter.Start + TimeSpan.FromSeconds(r.NextDouble() * maxOffset.TotalSeconds);
                        return new Section(start, start + duration);
                    }
                case ChapterMode.FastestTimeSpan:
                    {
                        TimeSpan span2 = duration.Multiply(0.8);

                        var fastestSection = GetFastestSection(chapters, span2, duration);

                        //Can happen if all chapters are too short ...
                        if (fastestSection.IsEmpty)
                            return GetRandomChapter(ChapterMode.FastestChapter);

                        return fastestSection;
                    }
                default:
                    return Section.Empty;
            }
        }

        private CommandSection GetFastestSection(List<CommandSection> chapters, TimeSpan minDuration, TimeSpan maxDuration)
        {
            CommandSection fastestSection = CommandSection.Empty;

            // Preparation for "Any fast section"
            List<CommandSection> candidates = new List<CommandSection>();

            foreach (var chapter in chapters)
            {
                int startIndex = 0;
                int endIndex = 0;

                List<TimeSpan> timeStamps = new List<TimeSpan>();
                timeStamps.Add(chapter.Positions[0]);

                while (startIndex < chapter.Positions.Count)
                {
                    while (chapter.Positions[endIndex] - chapter.Positions[startIndex] < maxDuration)
                    {
                        if (endIndex + 1 >= chapter.Positions.Count)
                            break;

                        if (chapter.Positions[endIndex + 1] - chapter.Positions[startIndex] > maxDuration)
                            break;

                        endIndex++;

                        if (timeStamps.Count > 0)
                            if (chapter.Positions[endIndex] - timeStamps.Last() >= _gapDuration)
                            {
                                startIndex = endIndex;
                                timeStamps.Clear();
                                timeStamps.Add(chapter.Positions[endIndex]);
                                continue;
                            }

                        timeStamps.Add(chapter.Positions[endIndex]);
                    }

                    CommandSection currentSection = new CommandSection(timeStamps, false);

                    if (currentSection.Duration >= minDuration)
                    {
                        if (currentSection.CommandsPerSecond > fastestSection.CommandsPerSecond)
                        {
                            if (!fastestSection.IsEmpty && !fastestSection.Overlaps(currentSection, true))
                            {
                                candidates.Add(fastestSection);
                            }

                            fastestSection = currentSection;
                        }
                    }

                    if (startIndex + 1 >= chapter.Positions.Count)
                        break;

                    startIndex++;
                    timeStamps.RemoveAt(0);
                }
            }

            return fastestSection;
        }

        private CommandSection GetFastestChapter(List<CommandSection> chapters)
        {
            double mostCommandsPerSecond = 0.0;
            CommandSection fastestChapter = CommandSection.Empty;
            Random r = new Random();

            foreach (var chapter in chapters)
            {
                if (chapter.CommandsPerSecond > mostCommandsPerSecond)
                {
                    mostCommandsPerSecond = chapter.CommandsPerSecond;
                    fastestChapter = chapter;
                }
            }

            return fastestChapter;
        }

        public void SkipToNextEventInternal()
        {
            OsdHideSkipButton();
            OsdHideNotification("Events");

            if (IsSeeking || IsTransistioning)
                return;

            //TODO Skip duplicates too!

            TimeSpan currentPosition = TimeSource.Progress;

            //ScriptAction nextAction = _scriptHandler.FirstOriginalEventAfter(currentPosition - _scriptHandler.Delay);
            ScriptAction nextAction = FindNextChapterStart(currentPosition - _scriptHandler.Delay);

            if (nextAction == null)
            {
                Playlist.PlayNextEntry();
                return;
            }

            TimeSpan skipTo = nextAction.TimeStamp - TimeSpan.FromSeconds(1);

            if (skipTo < currentPosition)
                return;

            SkipTo(skipTo, Settings.SoftSeekGaps, Settings.SoftSeekGapDuration);
        }

        private ScriptAction FindNextChapterStart(TimeSpan timeSpan)
        {
            var chapters = GetChapters(TimeSpan.Zero, _gapDuration, false);
            var nextChapter = chapters.FirstOrDefault(c => c.Start > timeSpan);

            if (nextChapter == null)
                return null;

            return new FunScriptAction
            {
                TimeStamp = nextChapter.Start
            };
        }

        private void SkipTo(TimeSpan position, bool softSeek, TimeSpan duration)
        {
            TimeSpan from = TimeSource.Progress;

            if (PlaybackMode == PlaybackMode.Local && softSeek)
                VideoPlayer.SoftSeek(position, duration);
            else
                TimeSource.SetPosition(position);

            if (Settings.NotifyGaps)
                ShowPosition($"Skipped {(position - from).TotalSeconds:f0}s - ");
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

        public void AddFolderToPlaylist()
        {
            string folder = OnRequestFolder(_lastFolder);
            if (string.IsNullOrWhiteSpace(folder))
                return;
            _lastFolder = folder;

            string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            List<PlaylistEntry> entries = new List<PlaylistEntry>();

            foreach (string file in files)
            {
                if (_supportedMediaExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLower()))
                    entries.Add(new PlaylistEntry(file));
            }

            Playlist.AddEntries(entries);
        }

        public string GetVideoFileOpenDialog()
        {
            string mediaFilters = $"Video Files|{string.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"))}";

            int x = 0;
            return OnRequestFile(mediaFilters, ref x, false);
        }

        public string GetGifFileSaveDialog()
        {
            string mediaFilters = $"GIF Files|*.gif";

            int x = 0;
            return OnRequestFile(mediaFilters, ref x, true);
        }

        public void AddFileToPlaylist()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string mediaFilters =
                $"Video Files|{string.Join(";", _supportedVideoExtensions.Select(v => $"*.{v}"))}";

            string scriptFilters = formats.BuildFilter(true);

            string filters = mediaFilters + "|" + scriptFilters;

            string[] files = OnRequestFiles(filters, ref _lastScriptFilterIndex);
            if (files == null)
                return;

            Playlist.AddEntries(files);
        }

        public void OpenScript()
        {
            ScriptFileFormatCollection formats = ScriptLoaderManager.GetFormats();

            string scriptFileName = OnRequestFile(formats.BuildFilter(true), ref _lastScriptFilterIndex);
            if (scriptFileName == null)
                return;

            LoadScriptOrFallback(scriptFileName, true);
        }


        public void ReloadScript()
        {
            if (!String.IsNullOrEmpty(_loadedScript))
                LoadScriptOrFallback(_loadedScript, false, false);
        }

        public List<ScriptAction> LoadScriptActions(string scriptFileName)
        {
            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(scriptFileName);
            if (loaders == null)
                return null;

            var result = LoadScriptActions(loaders, scriptFileName);
            if (result != null)
                return result;

            ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
            return LoadScriptActions(otherLoaders, scriptFileName);
        }

        private List<ScriptAction> LoadScriptActions(ScriptLoader[] loaders, string fileName)
        {
            const long maxScriptSize = 4 * 1024 * 1024; //4 MB

            if (!File.Exists(fileName)) return null;
            if (new FileInfo(fileName).Length > maxScriptSize) return null;

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

                    Debug.WriteLine("Script with {0} actions successfully loaded with {1}", actions.Count, loader.GetType().Name);
                    break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Loader {0} failed to open {1}: {2}", loader.GetType().Name, Path.GetFileName(fileName), e.Message);
                }
            }

            if (actions == null) return null;

            return actions;
        }

        private bool LoadScriptOrFallback(string scriptFileName, bool checkForVideo, bool isFallbackScript = false)
        {
            ScriptLoader[] loaders = ScriptLoaderManager.GetLoaders(scriptFileName);
            if (loaders == null)
                return false;

            if (!LoadScript(loaders, scriptFileName, isFallbackScript))
            {
                ScriptLoader[] otherLoaders = ScriptLoaderManager.GetAllLoaders().Except(loaders).ToArray();
                if (!LoadScript(otherLoaders, scriptFileName, isFallbackScript))
                {
                    if (Settings.NotifyFileLoaded)
                        OsdShowMessage($"The script file '{scriptFileName}' could not be loaded!", TimeSpan.FromSeconds(6));

                    if (isFallbackScript)
                        return false;

                    if (!ExecuteFallbackBehaviour())
                        return false;
                }
            }

            if (Settings.NotifyFileLoaded && !Settings.NotifyFileLoadedOnlyFailed)
                OsdShowMessage($"Loaded {Path.GetFileName(scriptFileName)}", TimeSpan.FromSeconds(4), "ScriptLoaded");

            Title = Path.GetFileNameWithoutExtension(scriptFileName);

            if (checkForVideo)
                TryFindMatchingVideo(scriptFileName);

            return true;
        }

        private bool ExecuteFallbackBehaviour()
        {
            switch (Settings.NoScriptBehavior)
            {
                case NoScriptBehaviors.KeepLastScript:
                    {
                        return false;
                    }
                case NoScriptBehaviors.ClearScript:
                    {
                        _scriptHandler.Clear();
                        return false;
                    }
                case NoScriptBehaviors.FallbackScript:
                    {
                        string scriptFile = Settings.FallbackScriptFile;

                        if (Directory.Exists(scriptFile.TrimEnd('*')))
                            scriptFile = PickRandomFile(scriptFile, _supportedScriptExtensions);

                        if (!string.IsNullOrEmpty(scriptFile))
                            return LoadScriptOrFallback(scriptFile, false, true);

                        return false;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string PickRandomFile(string directory, string[] extensions)
        {
            Random r = new Random();
            List<string> paths = ExpandPath(directory);
            List<string> files = paths.SelectMany(path => extensions.SelectMany(ext => Directory.EnumerateFiles(path, "*." + ext))).ToList();

            if (files.Count == 0)
                return null;

            return files[r.Next(files.Count)];
        }

        private bool LoadScript(ScriptLoader[] loaders, string fileName, bool isFallback)
        {
            //const long maxScriptSize = 4 * 1024 * 1024; //4 MB
            if (!File.Exists(fileName)) return false;

            long fileSize = new FileInfo(fileName).Length;
            
            List<ScriptAction> actions = null;

            foreach (ScriptLoader loader in loaders)
            {
                if (fileSize > loader.MaxFileSize)
                    continue;

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

            _scriptHandler.SetScript(actions, isFallback);

            RefreshChapters();
            RefreshManualDuration();
            LoadedScript = fileName;

            FindMaxPositions();
            UpdateHeatMap();

            Handy?.PrepareNewFunscript(fileName, actions);

            return true;
        }

        public void ConnectLaunchDirectly()
        {
            if (LaunchBluetooth.IsLaunchPaired())
            {
                MessageBox.Show(
                    "It appears that you have paired your Launch. Since the Launch is a BLE-device, you don't have to pair it to use it - in fact it will probably not work if you do. Unpair your Launch and try again.",
                    "Launch paired", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            LaunchBluetooth controller = _controllers.OfType<LaunchBluetooth>().Single();
            controller.Start();
        }

        private bool AskForHandyDeviceId()
        {
            HandyDeviceIdSettingsDialog deviceIdDialog = new HandyDeviceIdSettingsDialog(Settings.HandyDeviceId, Handy.LocalIp, Handy.ServeScriptPort.ToString(), !Handy.HttpServerRunning);
            if (deviceIdDialog.ShowDialog() != true) return false;           
            Settings.HandyDeviceId = deviceIdDialog.DeviceId;
            Handy.LocalIp = deviceIdDialog.LocalIp;

            if(int.TryParse(deviceIdDialog.Port, out int parsedPort))
                Handy.ServeScriptPort = parsedPort;

            return true;
        }

        public void ConnectHandyDirectly()
        {
            HandyHelper.DeviceId = Settings.HandyDeviceId;
            if(!HandyHelper.IsDeviceIdSet)
            {
                // TODO: set device Id
                Debug.WriteLine("NO HANDY DEVICE_ID SET");
                MessageBox.Show("Set device handy and try again.");
                AskForHandyDeviceId();
            }
            else
            {
                if(Handy == null)
                {
                    Handy = new HandyController();
                    AskForHandyDeviceId();
                }
                Handy.StartHttpServer();
                Handy.CheckConnected(connected =>
                {
                    if(!connected)
                    {
                        Application.Current.Dispatcher.Invoke(AskForHandyDeviceId);
                    }
                });
            }
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

        public void Mute()
        {
            Volume = 0;
        }

        public void ShiftPosition(TimeSpan timeSpan)
        {
            TimeSource.SetPosition(TimeSource.Progress + timeSpan);
            ShowPosition();
        }

        private void ShowPosition(string prefix = "")
        {
            if (Settings.NotifyPosition)
                OsdShowMessage($@"{prefix}{TimeSource.Progress:h\:mm\:ss} / {TimeSource.Duration:h\:mm\:ss}",
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
            controller.VibratorConversionMode = Settings.VibratorConversionMode;

            _controllers.Add(controller);

            bool success = false;

            for (int i = 1; i <= Settings.ButtplugConnectionAttempts; i++)
            {
                if (Settings.NotifyDevices)
                    OsdShowMessage($"Connecting to Buttplug (Attempt {i}/{Settings.ButtplugConnectionAttempts})", TimeSpan.FromSeconds(6), "Buttplug Connection");

                success = await controller.Connect();
                if (success)
                    break;

                await Task.Delay(TimeSpan.FromSeconds(Settings.ButtplugConnectionDelay));
            }

            if (success)
            {
                if (Settings.NotifyDevices)
                    OsdShowMessage("Connected to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");

                if (Settings.AutoSearchForButtplugDevices)
                    StartScanningButtplug();

                if (Settings.AutoShowDeviceManager)
                    ShowDeviceManager();
            }
            else
            {
                _controllers.Remove(controller);
                controller.DeviceFound -= DeviceController_DeviceFound;
                controller.DeviceRemoved -= DeviceController_DeviceRemoved;
                if (Settings.NotifyDevices)
                    OsdShowMessage("Could not connect to Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
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

            OsdShowMessage("Disconnected from Buttplug", TimeSpan.FromSeconds(6), "Buttplug Connection");
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
        }

        protected virtual WindowStateModel OnRequestWindowState()
        {
            RequestEventArgs<WindowStateModel> eventArgs = new RequestEventArgs<WindowStateModel>();

            RequestGetWindowState?.Invoke(this, eventArgs);

            if (eventArgs.Handled)
                return eventArgs.Value;
            return null;
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
            InstanceHandler.CommandLineReceived -= InstanceHandlerOnCommandLineReceived;
            InstanceHandler.Shutdown();
            Handy?.Exit();
            SaveSettings();
            SavePlaylist();
        }

        protected virtual void OnRequestToggleFullscreen()
        {
            RequestToggleFullscreen?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRequestSetFullscreen(bool fullscreen)
        {
            RequestSetFullscreen?.Invoke(this, fullscreen);
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
            UpdateRandomChapterTooltip();

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
                case PlaybackMode.MpcHc:
                    if (TimeSource is MpcTimeSource mpc)
                        mpc.UpdateConnectionSettings(new MpcConnectionSettings
                        {
                            IpAndPort = settings.MpcHcEndpoint
                        });
                    break;
                case PlaybackMode.ZoomPlayer:
                    if (TimeSource is ZoomPlayerTimeSource zoom)
                        zoom.UpdateConnectionSettings(new ZoomPlayerConnectionSettings
                        {
                            IpAndPort = settings.ZoomPlayerEndpoint
                        });
                    break;
                case PlaybackMode.SamsungVr:
                    if (TimeSource is SamsungVrTimeSource samsung)
                        samsung.UpdateConnectionSettings(new SamsungVrConnectionSettings
                        {
                            UdpPort = settings.SamsungVrUdpPort
                        });
                    break;
                case PlaybackMode.Kodi:
                    if (TimeSource is KodiTimeSource kodi)
                        kodi.UpdateConnectionSettings(new KodiConnectionSettings
                        {
                            Ip = settings.KodiIp,
                            HttpPort = settings.KodiHttpPort,
                            TcpPort = settings.KodiTcpPort,
                            User = settings.KodiUser,
                            Password = settings.KodiPassword
                        });
                    break;

            }
        }

        private void UpdateRandomChapterTooltip()
        {
            switch (Settings.ChapterMode)
            {
                case ChapterMode.RandomChapter:
                    RandomChapterToolTip = "Random Chapter\r\nWill only play a single random chapter of the script";
                    break;
                case ChapterMode.FastestChapter:
                    RandomChapterToolTip = "Fastest Chapter\r\nWill only play the fastest chapter of the script";
                    break;
                case ChapterMode.FastestTimeSpan:
                    RandomChapterToolTip = $"Fastest Section\r\nWill only play the fastest {Settings.ChapterTargetDuration.TotalSeconds:f0}s of the script";
                    break;
                case ChapterMode.RandomTimeSpan:
                    RandomChapterToolTip = $"Random Section\r\nWill only play a random {Settings.ChapterTargetDuration.TotalSeconds:f0}s section of the script";
                    break;
                case ChapterMode.RandomChapterLimitedDuration:
                    RandomChapterToolTip = $"Random Chapter (limited)\r\nWill only play up to {Settings.ChapterTargetDuration.TotalSeconds:f0}s of a random chapter of the script";
                    break;
                case ChapterMode.FastestChapterLimitedDuration:
                    RandomChapterToolTip = $"Fastest Chapter (limited)\r\nWill only play up to {Settings.ChapterTargetDuration.TotalSeconds:f0}s of the fastest chapter of the script";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RandomChapterToolTip += "\nRight-click to modify";
        }

        public string RandomChapterToolTip
        {
            get => _randomChapterToolTip;
            set
            {
                if (value == _randomChapterToolTip) return;
                _randomChapterToolTip = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OsdShowMessage(string text, TimeSpan duration, string designation = null)
        {
            if (_usedOsds == null || _usedOsds.Length == 0)
                return;

            Debug.WriteLine(text);

            foreach (IOnScreenDisplay osd in _usedOsds)
            {
                osd?.ShowMessage(designation, text, duration);
            }
        }

        protected virtual void OsdShowSkipButton()
        {
            if (_usedOsds == null || _usedOsds.Length == 0)
                return;

            foreach (IOnScreenDisplay osd in _usedOsds)
            {
                osd.ShowSkipButton();
            }
        }

        protected virtual void OsdShowSkipNextButton()
        {
            if (_usedOsds == null || _usedOsds.Length == 0)
                return;

            foreach (IOnScreenDisplay osd in _usedOsds)
            {
                osd.ShowSkipNextButton();
            }
        }

        protected virtual void OsdHideSkipButton()
        {
            if (_usedOsds == null || _usedOsds.Length == 0)
                return;

            foreach (IOnScreenDisplay osd in _usedOsds)
            {
                osd.HideSkipButton();
            }
        }

        protected virtual void OsdHideNotification(string designation)
        {

            if (_usedOsds == null || _usedOsds.Length == 0)
                return;

            foreach (IOnScreenDisplay osd in _usedOsds)
            {
                osd.HideMessage(designation);
            }
        }

        protected virtual void OnBeat()
        {
            Beat?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnIntermediateBeat(double e)
        {
            IntermediateBeat?.Invoke(this, e);
        }

        protected virtual void OnRequestSetWindowState(WindowStateModel e)
        {
            RequestSetWindowState?.Invoke(this, e);
        }

        public void RecheckForAdditionalFiles(bool generateIfMissing)
        {
            if (Application.Current == null)
                return;

            if (!Application.Current.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => RecheckForAdditionalFiles(generateIfMissing)));
                return;
            }

            TryFindMatchingThumbnails(LoadedVideo, generateIfMissing);
        }

        public void GenerateThumbnailsForLoadedVideo()
        {
            if (!IsVideoLoaded())
                return;

            GenerateThumbnails(true, true, LoadedVideo);
        }

        public void GenerateThumbnailBannerForLoadedVideo()
        {
            if (!IsVideoLoaded())
                return;

            GenerateThumbnailBanners(LoadedVideo);
        }

        public void GeneratePreviewForLoadedVideo()
        {
            if (!IsVideoLoaded())
                return;

            GeneratePreviews(true, LoadedVideo);
        }

        public void GenerateHeatmapForLoadedVideo()
        {
            if (!IsVideoLoaded())
                return;

            GenerateHeatmaps(LoadedVideo);
        }

        public void GenerateAllForLoadedVideo()
        {
            if (!IsVideoLoaded())
                return;

            GenerateAll(true, LoadedVideo);
        }

        private void GenerateThumbnails(bool askUserForSettings, bool showProgressDialog, params string[] videos)
        {
            if (!CheckFfmpeg(askUserForSettings))
                return;

            ThumbnailGeneratorSettings settings = _lastThumbnailSettings?.Duplicate();

            if (askUserForSettings)
            {
                settings = OnRequestThumbnailGeneratorSettings(settings);
            }
            else if (settings == null)
            {
                settings = new ThumbnailGeneratorSettings();
            }

            if (settings == null)
                return;

            _lastThumbnailSettings = settings;

            foreach (string video in videos)
            {
                if (string.IsNullOrEmpty(video))
                    continue;

                if (!File.Exists(video))
                    continue;

                var videoSettings = settings.Duplicate();
                videoSettings.VideoFile = video;
                videoSettings.OutputFile = Path.ChangeExtension(video, "thumbs");

                ThumbnailGenerator generator = new ThumbnailGenerator(Settings.FfmpegPath);

                WorkQueue.Enqueue(generator.CreateJob(videoSettings));
            }

            if (showProgressDialog)
                AutoShowGeneratorProgress();
        }

        private void GenerateThumbnailBanners(params string[] videos)
        {
            if (!CheckFfmpeg())
                return;

            ThumbnailBannerGeneratorSettings settings = _lastThumbnailBannerSettings?.Duplicate();
            settings = OnRequestThumbnailBannerGeneratorSettings(settings);

            if (settings == null)
                return;

            _lastThumbnailBannerSettings = settings;

            foreach (string video in videos)
            {
                if (string.IsNullOrEmpty(video))
                    continue;

                if (!File.Exists(video))
                    continue;

                var videoSettings = settings.Duplicate();
                videoSettings.VideoFile = video;
                videoSettings.OutputFile = Path.ChangeExtension(video, "jpg");

                ThumbnailBannerGenerator generator = new ThumbnailBannerGenerator(Settings.FfmpegPath);

                WorkQueue.Enqueue(generator.CreateJob(videoSettings));
            }

            AutoShowGeneratorProgress();
        }

        private void GenerateAll(bool visible, params string[] videos)
        {
            if (!CheckFfmpeg())
                return;

            var settings = GetDefaultGeneratorSettings();

            GeneralGeneratorSettingsViewModel generalSettings = settings.General;
            HeatmapGeneratorSettings defaultHeatmapSettings = settings.Heatmap.GetSettings(out string[] _);
            PreviewGeneratorSettings defaultPreviewSettings = settings.Preview.GetSettings(out string[] _);
            ThumbnailGeneratorSettings defaultThumbnailSettings = settings.Thumbnails.GetSettings(out string[] _);
            ThumbnailBannerGeneratorSettings defaultThumbnailBannerSettings = settings.Banner.GetSettings(out string[] _);

            foreach (string video in videos)
            {
                if (string.IsNullOrEmpty(video))
                    continue;

                if (!File.Exists(video))
                    continue;

                if (generalSettings.GenerateHeatmap)
                {
                    HeatmapGeneratorSettings heatmapSettings = defaultHeatmapSettings.Duplicate();
                    bool skipHeatmap = generalSettings.ApplyToOrSkip(video, heatmapSettings, "png");
                    if (!skipHeatmap)
                    {
                        HeatmapGenerator heatmapGenerator = new HeatmapGenerator(this, Settings.FfmpegPath);
                        WorkQueue.Enqueue(heatmapGenerator.CreateJob(heatmapSettings));
                    }
                }

                if (generalSettings.GeneratePreview)
                {
                    PreviewGeneratorSettings previewSettings = defaultPreviewSettings.Duplicate();
                    bool skipPreview = generalSettings.ApplyToOrSkip(video, previewSettings, "gif");
                    if (!skipPreview)
                    {
                        PreviewGenerator previewGenerator = new PreviewGenerator(Settings.FfmpegPath);
                        WorkQueue.Enqueue(previewGenerator.CreateJob(previewSettings));
                    }
                }

                if (generalSettings.GenerateThumbnails)
                {
                    ThumbnailGeneratorSettings thumbnailSettings = defaultThumbnailSettings.Duplicate();
                    bool skipThumbnails = generalSettings.ApplyToOrSkip(video, thumbnailSettings, "thumbs");
                    if (!skipThumbnails)
                    {
                        ThumbnailGenerator thumbnailGenerator = new ThumbnailGenerator(Settings.FfmpegPath);
                        WorkQueue.Enqueue(thumbnailGenerator.CreateJob(thumbnailSettings));
                    }
                }

                if (generalSettings.GenerateThumbnailBanner)
                {
                    ThumbnailBannerGeneratorSettings bannerSettings = defaultThumbnailBannerSettings.Duplicate();
                    bool skipBanner = generalSettings.ApplyToOrSkip(video, bannerSettings, "jpg");
                    if (!skipBanner)
                    {
                        ThumbnailBannerGenerator bannerGenerator = new ThumbnailBannerGenerator(Settings.FfmpegPath);
                        WorkQueue.Enqueue(bannerGenerator.CreateJob(bannerSettings));
                    }
                }
            }

            if(visible)
                AutoShowGeneratorProgress();
        }

        private GeneratorSettingsViewModel GetDefaultGeneratorSettings()
        {
            if(_previousGeneratorSettings == null)
                _previousGeneratorSettings = new GeneratorSettingsViewModel();

            return _previousGeneratorSettings;
        }

        private void GenerateHeatmaps(params string[] videos)
        {
            if (!CheckFfmpeg())
                return;

            HeatmapGeneratorSettings settings = _lastHeatmapSettings?.Duplicate();
            settings = OnRequestHeatmapGeneratorSettings(settings);

            if (settings == null)
                return;

            _lastHeatmapSettings = settings;

            foreach (string video in videos)
            {
                if (string.IsNullOrEmpty(video))
                    continue;

                if (!File.Exists(video))
                    continue;

                HeatmapGeneratorSettings videoSettings = settings.Duplicate();
                videoSettings.VideoFile = video;
                videoSettings.OutputFile = Path.ChangeExtension(video, ".png");

                HeatmapGenerator generator = new HeatmapGenerator(this, Settings.FfmpegPath);

                WorkQueue.Enqueue(generator.CreateJob(videoSettings));
            }

            AutoShowGeneratorProgress();
        }

        private void GeneratePreviews(bool currentlyLoaded, params string[] videos)
        {
            if (!CheckFfmpeg())
                return;

            PreviewGeneratorSettings settings = _lastPreviewSettings?.Duplicate();

            if (currentlyLoaded)
            {
                if (_loopA != TimeSpan.MinValue && _loopB != TimeSpan.MinValue)
                {
                    if(settings == null)
                        settings = new PreviewGeneratorSettings();

                    TimeSpan tFrom = _loopB > _loopA ? _loopA : _loopB;
                    TimeSpan tTo = _loopB > _loopA ? _loopB : _loopA;

                    settings.TimeFrames.Add(new TimeFrame
                    {
                        StartTimeSpan = tFrom,
                        Duration = tTo - tFrom
                    });
                }
            }

            settings = OnRequestPreviewGeneratorSettings(settings);

            if (settings == null)
                return;

            _lastPreviewSettings = settings;

            foreach (string video in videos)
            {
                if (string.IsNullOrEmpty(video))
                    continue;

                if (!File.Exists(video))
                    continue;

                PreviewGeneratorSettings videoSettings = settings.Duplicate();
                videoSettings.VideoFile = video;
                videoSettings.OutputFile = Path.ChangeExtension(video, ".gif");

                //videoSettings.GenerateRelativeTimeFrames(12, TimeSpan.FromSeconds(0.8));

                PreviewGenerator generator = new PreviewGenerator(Settings.FfmpegPath);

                WorkQueue.Enqueue(generator.CreateJob(videoSettings));
            }

            AutoShowGeneratorProgress();
        }

        public void ModifyGeneratorSettings()
        {
            var generatorSettings = OnRequestGeneratorSettings(_previousGeneratorSettings);
            if (generatorSettings == null)
                return;

            _previousGeneratorSettings = generatorSettings;
            SaveGeneratorSettings();
        }

        private void SaveGeneratorSettings()
        {
            if(_previousGeneratorSettings == null)
                return;

            _previousGeneratorSettings.Save(GetGeneratorSettingsFilePath());
        }

        private void LoadGeneratorSettings()
        {
            string file = GetGeneratorSettingsFilePath();
            if (File.Exists(file))
            {
                _previousGeneratorSettings = GeneratorSettingsViewModel.Load(file);
            }
        }

        private bool IsVideoLoaded()
        {
            return !string.IsNullOrEmpty(LoadedVideo);
        }

        protected virtual TimeSpan OnRequestScriptShiftTimespan(TimeSpan initialValue)
        {
            var eventArgs = new RequestEventArgs<TimeSpan>(initialValue);
            RequestScriptShiftTimespan?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return TimeSpan.Zero;

            return eventArgs.Value;
        }

        protected virtual Section OnRequestSection(Section initialValue)
        {
            var eventArgs = new RequestEventArgs<Section>(initialValue);
            RequestSection?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return null;

            return eventArgs.Value;
        }

        protected virtual ThumbnailGeneratorSettings OnRequestThumbnailGeneratorSettings(ThumbnailGeneratorSettings initialSettings)
        {
            var eventArgs = new RequestEventArgs<ThumbnailGeneratorSettings>(initialSettings);
            RequestThumbnailGeneratorSettings?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return null;

            return eventArgs.Value;
        }

        protected virtual PreviewGeneratorSettings OnRequestPreviewGeneratorSettings(PreviewGeneratorSettings initialSettings)
        {
            var eventArgs = new RequestEventArgs<PreviewGeneratorSettings>(initialSettings);
            RequestPreviewGeneratorSettings?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return null;

            return eventArgs.Value;
        }

        protected virtual HeatmapGeneratorSettings OnRequestHeatmapGeneratorSettings(HeatmapGeneratorSettings initialSettings)
        {
            var eventArgs = new RequestEventArgs<HeatmapGeneratorSettings>(initialSettings);
            RequestHeatmapGeneratorSettings?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return null;

            return eventArgs.Value;
        }

        protected virtual void OnRequestGenerateThumbnailBanner(ThumbnailBannerGeneratorSettings e)
        {
            RequestGenerateThumbnailBanner?.Invoke(this, e);
        }

        protected virtual ThumbnailBannerGeneratorSettings OnRequestThumbnailBannerGeneratorSettings(ThumbnailBannerGeneratorSettings initialSettings)
        {
            var eventArgs = new RequestEventArgs<ThumbnailBannerGeneratorSettings>(initialSettings);
            RequestThumbnailBannerGeneratorSettings?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
                return null;

            return eventArgs.Value;
        }

        public void ShowSettings()
        {
            ShowSettings(null);
        }

        public void ShowSettings(string settingsId)
        {
            OnRequestShowSettings(settingsId);
        }

        protected virtual void OnRequestShowSettings(string settingsId)
        {
            RequestShowSettings?.Invoke(this, settingsId);
        }

        public bool CheckFfmpeg(bool showWarning = true)
        {
            if (!String.IsNullOrEmpty(Settings.FfmpegPath) && File.Exists(Settings.FfmpegPath))
                return true;

            if (!showWarning)
                return false;

            var response =
                OnRequestMessageBox(
                    "You need to configure FFmpeg before you can generate thumbnails and gifs. Would you like to open the settings now?",
                    "FFmpeg.exe not found", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (response != MessageBoxResult.Yes)
                return false;

            ShowSettings("FFmpeg");

            return !String.IsNullOrEmpty(Settings.FfmpegPath) && File.Exists(Settings.FfmpegPath);
        }

        protected virtual void OnRequestActivate()
        {
            RequestActivate?.Invoke(this, EventArgs.Empty);
        }

        protected void AutoShowGeneratorProgress()
        {
            if(Settings.AutoShowGeneratorProgress)
                OnRequestShowGeneratorProgressDialog();
        }

        protected virtual void OnRequestShowGeneratorProgressDialog()
        {
            RequestShowGeneratorProgressDialog?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnRequestShowDeviceManager()
        {
            RequestShowDeviceManager?.Invoke(this, EventArgs.Empty);
        }

        protected virtual GeneratorSettingsViewModel OnRequestGeneratorSettings(GeneratorSettingsViewModel initialValue)
        {
            var e = new RequestEventArgs<GeneratorSettingsViewModel>(initialValue?.Duplicate());
            RequestGeneratorSettings?.Invoke(this, e);

            if (!e.Handled)
                return null;

            return e.Value;
        }

        public void SetMainWindowOsd(IOnScreenDisplay osd)
        {
            _mainOsd = osd;
            RefreshOsds();
        }

        private void RefreshOsds()
        {
            List<IOnScreenDisplay> osds = new List<IOnScreenDisplay>();

            if(_mainOsd != null)
                osds.Add(_mainOsd);

            if(Settings.UseExternalOsd)
            {
                if (TimeSource?.OnScreenDisplay != null)
                    osds.Add(TimeSource.OnScreenDisplay);
            }

            _usedOsds = osds.ToArray();
        }
    }
}